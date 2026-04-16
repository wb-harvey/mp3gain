/*
 *  mp3gain2026_api.h - Modern C API for MP3 Gain analysis and adjustment
 *
 *  Based on the original mp3gain by Glen Sawyer and contributors.
 *  Modernized wrapper Copyright (C) 2026
 *
 *  This library is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 2.1 of the License, or (at your option) any later version.
 */

#ifndef MP3GAIN2026_API_H
#define MP3GAIN2026_API_H

#ifdef __cplusplus
extern "C" {
#endif

#ifdef MP3GAIN2026_EXPORTS
#define MP3G_API __declspec(dllexport)
#else
#define MP3G_API __declspec(dllimport)
#endif

/* ── Result Codes ─────────────────────────────────────────────── */
#define MP3G_OK                0
#define MP3G_ERR_GENERIC      -1
#define MP3G_ERR_FILEOPEN     -2
#define MP3G_ERR_FILEREAD     -3
#define MP3G_ERR_FILEWRITE    -4
#define MP3G_ERR_NOTMP3       -5
#define MP3G_ERR_CANCELLED    -6
#define MP3G_ERR_INVALIDARG   -7
#define MP3G_ERR_TEMPFILE     -8

/* ── Analysis Results ─────────────────────────────────────────── */
typedef struct MP3G_AnalysisResult {
    double trackGain;       /* Track gain in dB (ReplayGain)        */
    double trackPeak;       /* Track peak amplitude (0.0 - 1.0)     */
    double albumGain;       /* Album gain in dB (if album mode)     */
    double albumPeak;       /* Album peak amplitude (if album mode) */
    int    maxGlobalGain;   /* Maximum global gain field in frames  */
    int    minGlobalGain;   /* Minimum global gain field in frames  */
    int    maxNoClipGain;   /* Max gain change without clipping     */
    int    albumMaxNoClipGain;
} MP3G_AnalysisResult;

/* ── Tag Information ──────────────────────────────────────────── */
typedef struct MP3G_TagInfo {
    int    haveTrackGain;
    int    haveTrackPeak;
    int    haveAlbumGain;
    int    haveAlbumPeak;
    int    haveUndo;
    double trackGain;
    double trackPeak;
    double albumGain;
    double albumPeak;
    int    undoLeft;
    int    undoRight;
    int    undoWrap;
    int    minGain;
    int    maxGain;
} MP3G_TagInfo;

/* ── Progress Callback ────────────────────────────────────────── */
/* Return 0 to continue, non-zero to cancel */
typedef int (*MP3G_ProgressCallback)(int percentDone, void* userData);

/* ── Library Lifecycle ────────────────────────────────────────── */
MP3G_API int  MP3G_Init(void);
MP3G_API void MP3G_Shutdown(void);
MP3G_API int  MP3G_GetVersion(char* buffer, int bufLen);

/* ── Single File Analysis ─────────────────────────────────────── */
MP3G_API int  MP3G_AnalyzeFile(const char* filePath,
                               MP3G_AnalysisResult* result,
                               int ignoreTags,
                               int recalcTags);

/* ── Album Analysis (multi-file) ──────────────────────────────── */
MP3G_API int  MP3G_AlbumInit(void);
MP3G_API int  MP3G_AlbumAnalyzeFile(const char* filePath,
                                     MP3G_AnalysisResult* result,
                                     int ignoreTags,
                                     int recalcTags);
MP3G_API int  MP3G_AlbumGetResult(double* albumGain, double* albumPeak);
MP3G_API void MP3G_AlbumFinish(void);

/* ── Gain Modification ────────────────────────────────────────── */
MP3G_API int  MP3G_ApplyGain(const char* filePath,
                             int gainChange,
                             int preserveTimestamp);

MP3G_API int  MP3G_ApplyTrackGain(const char* filePath,
                                  double targetDb,
                                  int preserveTimestamp);

MP3G_API int  MP3G_UndoGain(const char* filePath,
                            int preserveTimestamp);

/* ── Tag Operations ───────────────────────────────────────────── */
MP3G_API int  MP3G_ReadTags(const char* filePath,
                            MP3G_TagInfo* tagInfo);

MP3G_API int  MP3G_WriteTags(const char* filePath,
                             const MP3G_AnalysisResult* result,
                             double targetDb,
                             int haveUndo,
                             int undoLeft,
                             int undoRight,
                             int preserveTimestamp);

MP3G_API int  MP3G_RemoveTags(const char* filePath,
                              int preserveTimestamp);

/* ── Error Reporting ──────────────────────────────────────────── */
MP3G_API int  MP3G_GetLastError(char* buffer, int bufLen);

/* ── Progress ─────────────────────────────────────────────────── */
MP3G_API void MP3G_SetProgressCallback(MP3G_ProgressCallback callback,
                                        void* userData);

#ifdef __cplusplus
}
#endif

#endif /* MP3GAIN2026_API_H */
