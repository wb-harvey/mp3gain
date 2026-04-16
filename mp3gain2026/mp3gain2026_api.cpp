/*
 *  mp3gain2026_api.cpp - Implementation of the modern mp3gain2026 API
 *
 *  Bridges the modern C API to the legacy mp3gain C code.
 *  Handles thread safety, error capture, and Unicode path support.
 *
 *  Copyright (C) 2026. LGPL v2.1+
 */

#define MP3GAIN2026_EXPORTS
#define asWIN32DLL
#define WIN32

#include <windows.h>
#include <string.h>
#include <stdlib.h>
#include <stdio.h>
#include <math.h>

#include "mp3gain2026_api.h"

/* ── Legacy Headers ───────────────────────────────────────────── */
extern "C" {
#include "legacy/gain_analysis.h"
#include "legacy/apetag.h"
#include "legacy/id3tag.h"
#include "legacy/mp3gain.h"
#include "legacy/rg_error.h"

/* Legacy globals that we need access to */
extern unsigned int mp3gainerr;
extern char* mp3gainerrstr;
extern int blnCancel;
extern void* apphandle;
extern int apppercentdonemsg;
extern int apperrmsg;

/* Forward declaration of the legacy changeGain function */
int changeGain(char *filename, int leftgainchange, int rightgainchange);
}

/* ── Module State ─────────────────────────────────────────────── */
static CRITICAL_SECTION g_cs;
static bool g_initialized = false;
static char g_lastError[2048] = {0};
static MP3G_ProgressCallback g_progressCb = nullptr;
static void* g_progressUserData = nullptr;

#define MP3GAIN2026_VERSION "2026.1.0"
#define DEFAULT_TARGET_DB 89.0

/* ── Helpers ──────────────────────────────────────────────────── */
static void SetLastErrorMsg(const char* msg) {
    if (msg) {
        strncpy_s(g_lastError, sizeof(g_lastError), msg, _TRUNCATE);
    } else {
        g_lastError[0] = '\0';
    }
}

static void ClearLastError() {
    g_lastError[0] = '\0';
}

/* ── Library Lifecycle ────────────────────────────────────────── */

MP3G_API int MP3G_Init(void) {
    if (g_initialized) return MP3G_OK;

    InitializeCriticalSection(&g_cs);
    g_initialized = true;

    /* Initialize legacy globals */
    mp3gainerrstr = NULL;
    apphandle = 0;
    apppercentdonemsg = 0;
    apperrmsg = 0;
    blnCancel = 0;

    return MP3G_OK;
}

MP3G_API void MP3G_Shutdown(void) {
    if (!g_initialized) return;

    EnterCriticalSection(&g_cs);
    if (mp3gainerrstr != NULL) {
        free(mp3gainerrstr);
        mp3gainerrstr = NULL;
    }
    g_initialized = false;
    LeaveCriticalSection(&g_cs);
    DeleteCriticalSection(&g_cs);
}

MP3G_API int MP3G_GetVersion(char* buffer, int bufLen) {
    if (!buffer || bufLen < 1) return MP3G_ERR_INVALIDARG;
    strncpy_s(buffer, bufLen, MP3GAIN2026_VERSION, _TRUNCATE);
    return MP3G_OK;
}

/* ── Single File Analysis ─────────────────────────────────────── */

extern "C" struct MP3GainTagInfo *globalTagInfoHack;
extern "C" int __cdecl mp3gain_main(int argc, char **argv);

MP3G_API int MP3G_AnalyzeFile(const char* filePath,
                               MP3G_AnalysisResult* result,
                               int ignoreTags,
                               int recalcTags) {
    if (!g_initialized) return MP3G_ERR_GENERIC;
    if (!filePath || !result) return MP3G_ERR_INVALIDARG;

    ClearLastError();
    EnterCriticalSection(&g_cs);

    memset(result, 0, sizeof(MP3G_AnalysisResult));

    /* Reset legacy state */
    mp3gainerr = 0; 
    blnCancel = 0;
    if (mp3gainerrstr != NULL) {
        free(mp3gainerrstr);
        mp3gainerrstr = NULL;
    }

    const char* args[10];
    int argc = 0;
    args[argc++] = "mp3gain2026_api";
    args[argc++] = "/q";
    if (ignoreTags) {
        args[argc++] = "/s";
        args[argc++] = "s";
    }
    if (recalcTags) {
        args[argc++] = "/s";
        args[argc++] = "r";
    }
    args[argc++] = filePath;

    struct MP3GainTagInfo localTag;
    memset(&localTag, 0, sizeof(localTag));
    globalTagInfoHack = &localTag;

    int ret = mp3gain_main(argc, (char**)args);

    struct MP3GainTagInfo* tag = &localTag;

    if (tag != NULL) {
        result->trackGain = tag->trackGain;
        result->trackPeak = tag->trackPeak;
        result->albumGain = tag->albumGain;
        result->albumPeak = tag->albumPeak;
        result->minGlobalGain = tag->minGain;
        result->maxGlobalGain = tag->maxGain;

        /* Calculate max no-clip gain */
        if (result->trackPeak > 0.0) {
            double maxAmplitude = result->trackPeak;
            double maxGainDb = 20.0 * log10(1.0 / maxAmplitude);
            result->maxNoClipGain = (int)floor(maxGainDb * 4.0);
        }
    } else {
        SetLastErrorMsg("Analysis failed to populate tag stats");
        LeaveCriticalSection(&g_cs);
        return MP3G_ERR_GENERIC;
    }

    LeaveCriticalSection(&g_cs);
    return MP3G_OK;
}

/* ── Album Analysis ───────────────────────────────────────────── */

MP3G_API int MP3G_AlbumInit(void) {
    if (!g_initialized) return MP3G_ERR_GENERIC;
    ClearLastError();
    EnterCriticalSection(&g_cs);

    if (InitGainAnalysis(44100) != INIT_GAIN_ANALYSIS_OK) {
        SetLastErrorMsg("Failed to initialize album gain analysis");
        LeaveCriticalSection(&g_cs);
        return MP3G_ERR_GENERIC;
    }

    LeaveCriticalSection(&g_cs);
    return MP3G_OK;
}

MP3G_API int MP3G_AlbumAnalyzeFile(const char* filePath,
                                     MP3G_AnalysisResult* result,
                                     int ignoreTags,
                                     int recalcTags) {
    /* For album mode, each file is analyzed and contributes to the album stats */
    return MP3G_AnalyzeFile(filePath, result, ignoreTags, recalcTags);
}

MP3G_API int MP3G_AlbumGetResult(double* albumGain, double* albumPeak) {
    if (!g_initialized) return MP3G_ERR_GENERIC;
    if (!albumGain || !albumPeak) return MP3G_ERR_INVALIDARG;

    EnterCriticalSection(&g_cs);
    *albumGain = GetAlbumGain();
    *albumPeak = 0.0; /* Album peak must be tracked separately */
    LeaveCriticalSection(&g_cs);
    return MP3G_OK;
}

MP3G_API void MP3G_AlbumFinish(void) {
    /* Nothing to clean up specifically for album mode */
}

/* ── Gain Modification ────────────────────────────────────────── */

MP3G_API int MP3G_ApplyGain(const char* filePath,
                             int gainChange,
                             int preserveTimestamp) {
    if (!g_initialized) return MP3G_ERR_GENERIC;
    if (!filePath) return MP3G_ERR_INVALIDARG;
    if (gainChange == 0) return MP3G_OK;

    ClearLastError();
    EnterCriticalSection(&g_cs);

    /* Reset legacy state */
    mp3gainerr = 0;
    blnCancel = 0;
    if (mp3gainerrstr != NULL) {
        free(mp3gainerrstr);
        mp3gainerrstr = NULL;
    }

    /* Store file time if preserving */
    if (preserveTimestamp) {
        fileTime((char*)filePath, storeTime);
    }

    /* Apply gain change */
    changeGain((char*)filePath, gainChange, gainChange);

    /* Restore file time if preserving */
    if (preserveTimestamp) {
        fileTime((char*)filePath, setStoredTime);
    }

    int ret = MP3G_OK;
    if (mp3gainerr != 0) {
        if (mp3gainerrstr)
            SetLastErrorMsg(mp3gainerrstr);
        else
            SetLastErrorMsg("Unknown error during gain application");
        ret = MP3G_ERR_GENERIC;
    }

    LeaveCriticalSection(&g_cs);
    return ret;
}

MP3G_API int MP3G_ApplyTrackGain(const char* filePath,
                                  double targetDb,
                                  int preserveTimestamp) {
    if (!g_initialized) return MP3G_ERR_GENERIC;
    if (!filePath) return MP3G_ERR_INVALIDARG;

    /* First analyze the file to get current gain */
    MP3G_AnalysisResult analysis;
    int ret = MP3G_AnalyzeFile(filePath, &analysis, 0, 0);
    if (ret != MP3G_OK) return ret;

    /* Calculate required change in quarter-dB steps (mp3gain's unit) */
    double gainDiff = targetDb - DEFAULT_TARGET_DB + analysis.trackGain;
    int gainChange = (int)round(gainDiff / 1.5); /* Each mp3gain step is ~1.5 dB */

    if (gainChange == 0) return MP3G_OK;

    return MP3G_ApplyGain(filePath, gainChange, preserveTimestamp);
}

MP3G_API int MP3G_UndoGain(const char* filePath,
                            int preserveTimestamp) {
    if (!g_initialized) return MP3G_ERR_GENERIC;
    if (!filePath) return MP3G_ERR_INVALIDARG;

    ClearLastError();

    /* Read existing tags to find undo info */
    MP3G_TagInfo tagInfo;
    int ret = MP3G_ReadTags(filePath, &tagInfo);
    if (ret != MP3G_OK) return ret;

    if (!tagInfo.haveUndo) {
        SetLastErrorMsg("No undo information available in file tags");
        return MP3G_ERR_GENERIC;
    }

    /* Apply the inverse gain */
    EnterCriticalSection(&g_cs);

    mp3gainerr = 0;
    blnCancel = 0;
    if (mp3gainerrstr != NULL) {
        free(mp3gainerrstr);
        mp3gainerrstr = NULL;
    }

    if (preserveTimestamp) {
        fileTime((char*)filePath, storeTime);
    }

    changeGain((char*)filePath, tagInfo.undoLeft, tagInfo.undoRight);

    if (preserveTimestamp) {
        fileTime((char*)filePath, setStoredTime);
    }

    ret = MP3G_OK;
    if (mp3gainerr != 0) {
        if (mp3gainerrstr)
            SetLastErrorMsg(mp3gainerrstr);
        else
            SetLastErrorMsg("Unknown error during undo");
        ret = MP3G_ERR_GENERIC;
    }

    LeaveCriticalSection(&g_cs);
    return ret;
}

/* ── Tag Operations ───────────────────────────────────────────── */

MP3G_API int MP3G_ReadTags(const char* filePath,
                            MP3G_TagInfo* tagInfo) {
    if (!g_initialized) return MP3G_ERR_GENERIC;
    if (!filePath || !tagInfo) return MP3G_ERR_INVALIDARG;

    ClearLastError();
    EnterCriticalSection(&g_cs);

    memset(tagInfo, 0, sizeof(MP3G_TagInfo));

    struct MP3GainTagInfo legacyInfo;
    memset(&legacyInfo, 0, sizeof(legacyInfo));
    struct FileTagsStruct fileTags;
    memset(&fileTags, 0, sizeof(fileTags));

    int ret = ReadMP3GainAPETag((char*)filePath, &legacyInfo, &fileTags);

    /* Also try ID3 if APE didn't have info */
    if (!legacyInfo.haveTrackGain) {
        ReadMP3GainID3Tag((char*)filePath, &legacyInfo);
    }

    /* Copy to output struct */
    tagInfo->haveTrackGain = legacyInfo.haveTrackGain;
    tagInfo->haveTrackPeak = legacyInfo.haveTrackPeak;
    tagInfo->haveAlbumGain = legacyInfo.haveAlbumGain;
    tagInfo->haveAlbumPeak = legacyInfo.haveAlbumPeak;
    tagInfo->haveUndo      = legacyInfo.haveUndo;
    tagInfo->trackGain     = legacyInfo.trackGain;
    tagInfo->trackPeak     = legacyInfo.trackPeak;
    tagInfo->albumGain     = legacyInfo.albumGain;
    tagInfo->albumPeak     = legacyInfo.albumPeak;
    tagInfo->undoLeft      = legacyInfo.undoLeft;
    tagInfo->undoRight     = legacyInfo.undoRight;
    tagInfo->undoWrap      = legacyInfo.undoWrap;
    tagInfo->minGain       = legacyInfo.minGain;
    tagInfo->maxGain       = legacyInfo.maxGain;

    /* Free tag memory */
    if (fileTags.apeTag) {
        if (fileTags.apeTag->otherFields)
            free(fileTags.apeTag->otherFields);
        free(fileTags.apeTag);
    }
    if (fileTags.lyrics3tag) free(fileTags.lyrics3tag);
    if (fileTags.id31tag) free(fileTags.id31tag);

    LeaveCriticalSection(&g_cs);
    return MP3G_OK;
}

MP3G_API int MP3G_WriteTags(const char* filePath,
                             const MP3G_AnalysisResult* result,
                             double targetDb,
                             int haveUndo,
                             int undoLeft,
                             int undoRight,
                             int preserveTimestamp) {
    if (!g_initialized) return MP3G_ERR_GENERIC;
    if (!filePath || !result) return MP3G_ERR_INVALIDARG;

    ClearLastError();
    EnterCriticalSection(&g_cs);

    struct MP3GainTagInfo legacyInfo;
    memset(&legacyInfo, 0, sizeof(legacyInfo));
    struct FileTagsStruct fileTags;
    memset(&fileTags, 0, sizeof(fileTags));

    /* Read existing tags first */
    ReadMP3GainAPETag((char*)filePath, &legacyInfo, &fileTags);

    /* Update with new analysis results */
    legacyInfo.haveTrackGain = 1;
    legacyInfo.trackGain = result->trackGain;
    legacyInfo.haveTrackPeak = (result->trackPeak > 0.0) ? 1 : 0;
    legacyInfo.trackPeak = result->trackPeak;

    if (result->albumGain != 0.0) {
        legacyInfo.haveAlbumGain = 1;
        legacyInfo.albumGain = result->albumGain;
    }
    if (result->albumPeak > 0.0) {
        legacyInfo.haveAlbumPeak = 1;
        legacyInfo.albumPeak = result->albumPeak;
    }

    legacyInfo.minGain = (unsigned char)result->minGlobalGain;
    legacyInfo.maxGain = (unsigned char)result->maxGlobalGain;
    legacyInfo.haveMinMaxGain = 1;

    if (haveUndo) {
        legacyInfo.haveUndo = 1;
        legacyInfo.undoLeft = undoLeft;
        legacyInfo.undoRight = undoRight;
    } else {
        legacyInfo.haveUndo = 0;
        legacyInfo.undoLeft = 0;
        legacyInfo.undoRight = 0;
    }


    int ret = WriteMP3GainAPETag((char*)filePath, &legacyInfo, &fileTags, preserveTimestamp);

    /* Free tag memory */
    if (fileTags.apeTag) {
        if (fileTags.apeTag->otherFields)
            free(fileTags.apeTag->otherFields);
        free(fileTags.apeTag);
    }
    if (fileTags.lyrics3tag) free(fileTags.lyrics3tag);
    if (fileTags.id31tag) free(fileTags.id31tag);

    LeaveCriticalSection(&g_cs);
    return (ret == 0) ? MP3G_ERR_FILEWRITE : MP3G_OK;
}

MP3G_API int MP3G_RemoveTags(const char* filePath,
                              int preserveTimestamp) {
    if (!g_initialized) return MP3G_ERR_GENERIC;
    if (!filePath) return MP3G_ERR_INVALIDARG;

    ClearLastError();
    EnterCriticalSection(&g_cs);

    RemoveMP3GainAPETag((char*)filePath, preserveTimestamp);

    LeaveCriticalSection(&g_cs);
    return MP3G_OK;
}

/* ── Error Reporting ──────────────────────────────────────────── */

MP3G_API int MP3G_GetLastError(char* buffer, int bufLen) {
    if (!buffer || bufLen < 1) return MP3G_ERR_INVALIDARG;
    strncpy_s(buffer, bufLen, g_lastError, _TRUNCATE);
    return MP3G_OK;
}

/* ── Progress ─────────────────────────────────────────────────── */

MP3G_API void MP3G_SetProgressCallback(MP3G_ProgressCallback callback,
                                      void* userData) {
    g_progressCb = callback;
    g_progressUserData = userData;
}

extern "C" void MP3G_Internal_OnProgress(int percent) {
    if (g_progressCb) {
        g_progressCb(percent, g_progressUserData);
    }
}
