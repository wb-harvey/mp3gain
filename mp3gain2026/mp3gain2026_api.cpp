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
#include <string>

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
/* changeGain plus the matching tag update (undo, gains, peaks, min/max), as the mp3gain tool does it;
   0 on success */
int changeGainAndTag(char *filename, int leftgainchange, int rightgainchange,
                     struct MP3GainTagInfo *tag, struct FileTagsStruct *fileTag);

extern int QuietMode;
extern short int saveTime;
extern int mp3gain_lastFileOk;
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

/* ── Unicode paths ────────────────────────────────────────────────
 * The legacy code takes file names as char*; it now gets UTF-8 and opens files through the wide
 * Windows functions (legacy/utf8_io.h), so any name works. The ...W exports take UTF-16 paths; the
 * original exports keep taking ANSI (system code page) paths, converted here, for existing callers. */

static bool ToUtf8(const wchar_t* wide, std::string& utf8) {
    int n = WideCharToMultiByte(CP_UTF8, 0, wide, -1, NULL, 0, NULL, NULL);
    if (n <= 0) return false;
    utf8.resize(n);
    if (WideCharToMultiByte(CP_UTF8, 0, wide, -1, &utf8[0], n, NULL, NULL) != n) return false;
    utf8.resize(n - 1); /* drop the terminator that n counts */
    return true;
}

static bool AnsiToUtf8(const char* ansi, std::string& utf8) {
    int n = MultiByteToWideChar(CP_ACP, 0, ansi, -1, NULL, 0);
    if (n <= 0) return false;
    std::wstring wide(n, L'\0');
    if (MultiByteToWideChar(CP_ACP, 0, ansi, -1, &wide[0], n) != n) return false;
    return ToUtf8(wide.c_str(), utf8);
}

/* Converts the caller's path (wide = UTF-16, else ANSI) to UTF-8, or returns MP3G_ERR_INVALIDARG */
#define WITH_UTF8_PATH(path, isWide, call)                                                     \
    do {                                                                                       \
        if (!(path)) return MP3G_ERR_INVALIDARG;                                               \
        std::string u8path_;                                                                   \
        bool ok_ = (isWide) ? ToUtf8((const wchar_t*)(path), u8path_)                         \
                            : AnsiToUtf8((const char*)(path), u8path_);                       \
        if (!ok_) { SetLastErrorMsg("The file path could not be converted"); return MP3G_ERR_INVALIDARG; } \
        const char* u8_ = u8path_.c_str();                                                     \
        return call;                                                                           \
    } while (0)

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

static std::wstring Utf8ToWide(const std::string& utf8) {
    int n = MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(), -1, NULL, 0);
    if (n <= 0) return std::wstring();
    std::wstring wide(n, L'\0');
    MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(), -1, &wide[0], n);
    wide.resize(n - 1);
    return wide;
}

/* The absolute form of a UTF-8 path, still UTF-8 */
static bool FullPathU8(const char* utf8, std::string& full) {
    std::wstring wide = Utf8ToWide(utf8);
    if (wide.empty()) return false;
    DWORD n = GetFullPathNameW(wide.c_str(), 0, NULL, NULL);
    if (n == 0) return false;
    std::wstring buf(n, L'\0');
    DWORD m = GetFullPathNameW(wide.c_str(), n, &buf[0], NULL);
    if (m == 0 || m >= n) return false;
    buf.resize(m);
    return ToUtf8(buf.c_str(), full);
}

/* Makes the legacy code's error text (or the fallback, if it gave none) the last error */
static void LegacyErrorOr(const char* fallback) {
    const char* msg = (mp3gainerrstr != NULL && mp3gainerrstr[0]) ? mp3gainerrstr : fallback;
    while (*msg == '\n' || *msg == '\r' || *msg == ' ') msg++; /* legacy messages start with "\n" */
    SetLastErrorMsg(msg);
    size_t len = strlen(g_lastError);
    while (len > 0 && (g_lastError[len - 1] == '\n' || g_lastError[len - 1] == '\r')) g_lastError[--len] = '\0';
}

static void ResetLegacyState() {
    mp3gainerr = 0;
    blnCancel = 0;
    if (mp3gainerrstr != NULL) {
        free(mp3gainerrstr);
        mp3gainerrstr = NULL;
    }
}

static void FreeFileTags(struct FileTagsStruct* fileTags) {
    if (fileTags->apeTag) {
        if (fileTags->apeTag->otherFields)
            free(fileTags->apeTag->otherFields);
        free(fileTags->apeTag);
        fileTags->apeTag = NULL;
    }
    if (fileTags->lyrics3tag) { free(fileTags->lyrics3tag); fileTags->lyrics3tag = NULL; }
    if (fileTags->id31tag) { free(fileTags->id31tag); fileTags->id31tag = NULL; }
}

static int AnalyzeFileU8(const char* filePath,
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

    /* The legacy parser takes any argument starting with '/' as a switch, file names included
       ("/user/x.mp3" turned on undo mode): pass the full path, which never starts with '/' */
    std::string fullPath;
    if (!FullPathU8(filePath, fullPath)) {
        LeaveCriticalSection(&g_cs);
        SetLastErrorMsg("The file path is not valid");
        return MP3G_ERR_INVALIDARG;
    }

    const char* args[12];
    int argc = 0;
    args[argc++] = "mp3gain2026_api";
    args[argc++] = "/q";
    /* Analysis is read-only: read stored tag values, never write them (it used to add or rewrite the
       APE tag of every file it looked at, changing its modified time). Callers write tags explicitly. */
    args[argc++] = "/s";
    args[argc++] = "n";
    if (ignoreTags) {
        args[argc++] = "/s";
        args[argc++] = "s";
    }
    if (recalcTags) {
        args[argc++] = "/s";
        args[argc++] = "r";
    }
    args[argc++] = fullPath.c_str();

    struct MP3GainTagInfo localTag;
    memset(&localTag, 0, sizeof(localTag));
    globalTagInfoHack = &localTag;

    mp3gain_main(argc, (char**)args);
    globalTagInfoHack = NULL; /* it pointed at this stack variable */

    /* Not found, not an MP3, too short or too damaged to analyse: an error, not a zeroed "success"
       that callers took as "no change needed" (or used to compute a gain change) */
    if (!mp3gain_lastFileOk) {
        int code = GetFileAttributesW(Utf8ToWide(fullPath).c_str()) == INVALID_FILE_ATTRIBUTES
            ? MP3G_ERR_FILEOPEN : MP3G_ERR_NOTMP3;
        LegacyErrorOr(code == MP3G_ERR_FILEOPEN
            ? "The file could not be opened"
            : "No MP3 audio could be analysed: the file is not an MP3, is too short, or is damaged");
        LeaveCriticalSection(&g_cs);
        return code;
    }

    struct MP3GainTagInfo* tag = &localTag;
    result->trackGain = tag->trackGain;
    result->trackPeak = tag->trackPeak;
    /* Album gain isn't computed (each call analyses one file): it is reported as 0, rather than whatever
       album value an old tag held, which belonged to some earlier album analysis */
    result->albumGain = 0.0;
    result->albumPeak = 0.0;
    result->minGlobalGain = tag->minGain;
    result->maxGlobalGain = tag->maxGain;

    /* Max gain change (in mp3gain's 1.505 dB steps) that doesn't clip: 4*log2(1/peak), as mp3gain
       computes it. It used 20*log10(1/peak)*4, about 6 times too large, so it never protected. */
    if (result->trackPeak > 0.0) {
        result->maxNoClipGain = (int)floor(4.0 * log10(1.0 / result->trackPeak) / log10(2.0));
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

MP3G_API int MP3G_AlbumGetResult(double* albumGain, double* albumPeak) {
    if (!g_initialized) return MP3G_ERR_GENERIC;
    if (!albumGain || !albumPeak) return MP3G_ERR_INVALIDARG;

    /* Album gain isn't computed: every analysis resets the album totals, so GetAlbumGain() returned the
       last decoded file's gain, or GAIN_NOT_ENOUGH_SAMPLES (-24601) as a "gain". It is reported as 0. */
    *albumGain = 0.0;
    *albumPeak = 0.0;
    return MP3G_OK;
}

MP3G_API void MP3G_AlbumFinish(void) {
    /* Nothing to clean up specifically for album mode */
}

/* ── Gain Modification ────────────────────────────────────────── */

/* Reads the file's MP3Gain tag values (APE, else the ID3 copy). False when the file can't be opened. */
static bool ReadLegacyTags(const char* filePath, struct MP3GainTagInfo* info, struct FileTagsStruct* fileTags) {
    memset(info, 0, sizeof(*info));
    memset(fileTags, 0, sizeof(*fileTags));
    if (!ReadMP3GainAPETag((char*)filePath, info, fileTags))
        return false;
    if (!info->haveTrackGain)
        ReadMP3GainID3Tag((char*)filePath, info);
    return true;
}

/*
 * Changes the gain of every frame by `left`/`right` steps and updates the tag to match, the way the
 * mp3gain tool does it (changeGainAndTag): undo -= change, track/album gain -= change * 1.505 dB,
 * peaks scaled, min/max gain shifted. Before, only the frames changed: the tag still described the old
 * level, so the next analysis trusted it and the same change was applied again, and the undo
 * information never recorded the change. Call with g_cs held.
 */
static int ChangeGainWithTags(const char* filePath, int left, int right, int preserveTimestamp,
                              struct MP3GainTagInfo* info, struct FileTagsStruct* fileTags) {
    QuietMode = 1;   /* no progress text on the host's stderr */
    saveTime = 0;    /* the timestamp is kept here, around the whole change */
    if (preserveTimestamp)
        fileTime((char*)filePath, storeTime);

    int result = changeGainAndTag((char*)filePath, left, right, info, fileTags);

    if (preserveTimestamp)
        fileTime((char*)filePath, setStoredTime);

    if (result != 0 || mp3gainerr != 0) {
        LegacyErrorOr("The gain change could not be written");
        return MP3G_ERR_FILEWRITE;
    }
    return MP3G_OK;
}

static int ApplyGainU8(const char* filePath,
                       int gainChange,
                       int preserveTimestamp) {
    if (!g_initialized) return MP3G_ERR_GENERIC;
    if (!filePath) return MP3G_ERR_INVALIDARG;
    if (gainChange == 0) return MP3G_OK;

    ClearLastError();
    EnterCriticalSection(&g_cs);
    ResetLegacyState();

    struct MP3GainTagInfo info;
    struct FileTagsStruct fileTags;
    int ret;
    if (!ReadLegacyTags(filePath, &info, &fileTags)) {
        /* Nothing changed: without the tag the change couldn't be recorded for undo */
        LegacyErrorOr("The file could not be opened");
        ret = MP3G_ERR_FILEOPEN;
    } else {
        ret = ChangeGainWithTags(filePath, gainChange, gainChange, preserveTimestamp, &info, &fileTags);
    }
    FreeFileTags(&fileTags);

    LeaveCriticalSection(&g_cs);
    return ret;
}

static int ApplyTrackGainU8(const char* filePath,
                            double targetDb,
                            int preserveTimestamp) {
    if (!g_initialized) return MP3G_ERR_GENERIC;
    if (!filePath) return MP3G_ERR_INVALIDARG;

    /* First analyze the file to get current gain (an analysis failure is now reported, not a zero gain) */
    MP3G_AnalysisResult analysis;
    int ret = AnalyzeFileU8(filePath, &analysis, 0, 0);
    if (ret != MP3G_OK) return ret;

    /* The change in mp3gain's steps of 5*log10(2) = 1.505 dB, rounded half away from zero as mp3gain does */
    double gainDiff = targetDb - DEFAULT_TARGET_DB + analysis.trackGain;
    int gainChange = (int)round(gainDiff / (5.0 * log10(2.0)));

    if (gainChange == 0) return MP3G_OK;

    return ApplyGainU8(filePath, gainChange, preserveTimestamp);
}

static int UndoGainU8(const char* filePath,
                      int preserveTimestamp) {
    if (!g_initialized) return MP3G_ERR_GENERIC;
    if (!filePath) return MP3G_ERR_INVALIDARG;

    ClearLastError();
    EnterCriticalSection(&g_cs);
    ResetLegacyState();

    struct MP3GainTagInfo info;
    struct FileTagsStruct fileTags;
    int ret;
    if (!ReadLegacyTags(filePath, &info, &fileTags)) {
        LegacyErrorOr("The file could not be opened");
        ret = MP3G_ERR_FILEOPEN;
    } else if (!info.haveUndo) {
        SetLastErrorMsg("No undo information available in file tags");
        ret = MP3G_ERR_GENERIC;
    } else if (info.undoLeft == 0 && info.undoRight == 0) {
        ret = MP3G_OK; /* already at its original level */
    } else {
        /* Apply the stored undo change through the tag update too, so the stored undo becomes 0:
           before, it stayed, and undoing twice reversed the change twice */
        ret = ChangeGainWithTags(filePath, info.undoLeft, info.undoRight, preserveTimestamp, &info, &fileTags);
    }
    FreeFileTags(&fileTags);

    LeaveCriticalSection(&g_cs);
    return ret;
}

/* ── Tag Operations ───────────────────────────────────────────── */

static int ReadTagsU8(const char* filePath,
                      MP3G_TagInfo* tagInfo) {
    if (!g_initialized) return MP3G_ERR_GENERIC;
    if (!filePath || !tagInfo) return MP3G_ERR_INVALIDARG;

    ClearLastError();
    EnterCriticalSection(&g_cs);
    ResetLegacyState();

    memset(tagInfo, 0, sizeof(MP3G_TagInfo));

    struct MP3GainTagInfo legacyInfo;
    struct FileTagsStruct fileTags;
    if (!ReadLegacyTags(filePath, &legacyInfo, &fileTags)) {
        /* Was reported as success with no tags, which callers took as "no undo information" */
        FreeFileTags(&fileTags);
        LegacyErrorOr("The file could not be opened");
        LeaveCriticalSection(&g_cs);
        return MP3G_ERR_FILEOPEN;
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

    FreeFileTags(&fileTags);
    LeaveCriticalSection(&g_cs);
    return MP3G_OK;
}

static int WriteTagsU8(const char* filePath,
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
    ResetLegacyState();

    struct MP3GainTagInfo legacyInfo;
    memset(&legacyInfo, 0, sizeof(legacyInfo));
    struct FileTagsStruct fileTags;
    memset(&fileTags, 0, sizeof(fileTags));

    /* Read existing tags first: they give the tag's position. Writing after a failed read used a zero
       offset, which would put the tag over the start of the file. */
    if (!ReadMP3GainAPETag((char*)filePath, &legacyInfo, &fileTags)) {
        FreeFileTags(&fileTags);
        LegacyErrorOr("The file could not be opened");
        LeaveCriticalSection(&g_cs);
        return MP3G_ERR_FILEOPEN;
    }

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
    FreeFileTags(&fileTags);
    if (ret == 0)
        LegacyErrorOr("The tag could not be written");

    LeaveCriticalSection(&g_cs);
    return (ret == 0) ? MP3G_ERR_FILEWRITE : MP3G_OK;
}

static int RemoveTagsU8(const char* filePath,
                        int preserveTimestamp) {
    if (!g_initialized) return MP3G_ERR_GENERIC;
    if (!filePath) return MP3G_ERR_INVALIDARG;

    ClearLastError();
    EnterCriticalSection(&g_cs);
    ResetLegacyState();

    /* Its result used to be ignored: a failure was reported as success */
    int ret = RemoveMP3GainAPETag((char*)filePath, preserveTimestamp) ? MP3G_OK : MP3G_ERR_FILEWRITE;
    if (ret != MP3G_OK)
        LegacyErrorOr("The tag could not be removed");

    LeaveCriticalSection(&g_cs);
    return ret;
}

/* ── Path exports: ANSI (existing callers) and UTF-16 (...W) ──── */

MP3G_API int MP3G_AnalyzeFile(const char* filePath, MP3G_AnalysisResult* result, int ignoreTags, int recalcTags) {
    WITH_UTF8_PATH(filePath, false, AnalyzeFileU8(u8_, result, ignoreTags, recalcTags));
}
MP3G_API int MP3G_AnalyzeFileW(const wchar_t* filePath, MP3G_AnalysisResult* result, int ignoreTags, int recalcTags) {
    WITH_UTF8_PATH(filePath, true, AnalyzeFileU8(u8_, result, ignoreTags, recalcTags));
}

MP3G_API int MP3G_AlbumAnalyzeFile(const char* filePath, MP3G_AnalysisResult* result, int ignoreTags, int recalcTags) {
    /* For album mode, each file is analyzed and contributes to the album stats */
    return MP3G_AnalyzeFile(filePath, result, ignoreTags, recalcTags);
}
MP3G_API int MP3G_AlbumAnalyzeFileW(const wchar_t* filePath, MP3G_AnalysisResult* result, int ignoreTags, int recalcTags) {
    return MP3G_AnalyzeFileW(filePath, result, ignoreTags, recalcTags);
}

MP3G_API int MP3G_ApplyGain(const char* filePath, int gainChange, int preserveTimestamp) {
    WITH_UTF8_PATH(filePath, false, ApplyGainU8(u8_, gainChange, preserveTimestamp));
}
MP3G_API int MP3G_ApplyGainW(const wchar_t* filePath, int gainChange, int preserveTimestamp) {
    WITH_UTF8_PATH(filePath, true, ApplyGainU8(u8_, gainChange, preserveTimestamp));
}

MP3G_API int MP3G_ApplyTrackGain(const char* filePath, double targetDb, int preserveTimestamp) {
    WITH_UTF8_PATH(filePath, false, ApplyTrackGainU8(u8_, targetDb, preserveTimestamp));
}
MP3G_API int MP3G_ApplyTrackGainW(const wchar_t* filePath, double targetDb, int preserveTimestamp) {
    WITH_UTF8_PATH(filePath, true, ApplyTrackGainU8(u8_, targetDb, preserveTimestamp));
}

MP3G_API int MP3G_UndoGain(const char* filePath, int preserveTimestamp) {
    WITH_UTF8_PATH(filePath, false, UndoGainU8(u8_, preserveTimestamp));
}
MP3G_API int MP3G_UndoGainW(const wchar_t* filePath, int preserveTimestamp) {
    WITH_UTF8_PATH(filePath, true, UndoGainU8(u8_, preserveTimestamp));
}

MP3G_API int MP3G_ReadTags(const char* filePath, MP3G_TagInfo* tagInfo) {
    WITH_UTF8_PATH(filePath, false, ReadTagsU8(u8_, tagInfo));
}
MP3G_API int MP3G_ReadTagsW(const wchar_t* filePath, MP3G_TagInfo* tagInfo) {
    WITH_UTF8_PATH(filePath, true, ReadTagsU8(u8_, tagInfo));
}

MP3G_API int MP3G_WriteTags(const char* filePath, const MP3G_AnalysisResult* result, double targetDb,
                             int haveUndo, int undoLeft, int undoRight, int preserveTimestamp) {
    WITH_UTF8_PATH(filePath, false, WriteTagsU8(u8_, result, targetDb, haveUndo, undoLeft, undoRight, preserveTimestamp));
}
MP3G_API int MP3G_WriteTagsW(const wchar_t* filePath, const MP3G_AnalysisResult* result, double targetDb,
                              int haveUndo, int undoLeft, int undoRight, int preserveTimestamp) {
    WITH_UTF8_PATH(filePath, true, WriteTagsU8(u8_, result, targetDb, haveUndo, undoLeft, undoRight, preserveTimestamp));
}

MP3G_API int MP3G_RemoveTags(const char* filePath, int preserveTimestamp) {
    WITH_UTF8_PATH(filePath, false, RemoveTagsU8(u8_, preserveTimestamp));
}
MP3G_API int MP3G_RemoveTagsW(const wchar_t* filePath, int preserveTimestamp) {
    WITH_UTF8_PATH(filePath, true, RemoveTagsU8(u8_, preserveTimestamp));
}

/* ── Error Reporting ──────────────────────────────────────────── */

/* The message is kept as UTF-8 (it can contain a file name); this returns it in the system code page */
MP3G_API int MP3G_GetLastError(char* buffer, int bufLen) {
    if (!buffer || bufLen < 1) return MP3G_ERR_INVALIDARG;
    wchar_t wide[2048];
    if (MultiByteToWideChar(CP_UTF8, 0, g_lastError, -1, wide, 2048) <= 0 ||
        WideCharToMultiByte(CP_ACP, 0, wide, -1, buffer, bufLen, NULL, NULL) <= 0)
        strncpy_s(buffer, bufLen, g_lastError, _TRUNCATE);
    return MP3G_OK;
}

MP3G_API int MP3G_GetLastErrorW(wchar_t* buffer, int bufLen) {
    if (!buffer || bufLen < 1) return MP3G_ERR_INVALIDARG;
    if (MultiByteToWideChar(CP_UTF8, 0, g_lastError, -1, buffer, bufLen) <= 0) {
        /* Not valid UTF-8, or too long for the buffer: copy what fits, byte for byte */
        int i = 0;
        for (; i < bufLen - 1 && g_lastError[i]; i++) buffer[i] = (unsigned char)g_lastError[i];
        buffer[i] = L'\0';
    }
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
