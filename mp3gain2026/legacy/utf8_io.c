/*
 *  utf8_io.c - Unicode file names for the legacy mp3gain code (see utf8_io.h)
 *
 *  Copyright (C) 2026. LGPL v2.1+
 */

#ifdef WIN32

#define MP3GAIN_UTF8_IO_IMPL
#include "utf8_io.h"

#include <windows.h>
#include <stdlib.h>
#include <io.h>
#include <fcntl.h>
#include <errno.h>

wchar_t* u8_to_wide(const char* utf8)
{
    if (utf8 == NULL) return NULL;
    int n = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, utf8, -1, NULL, 0);
    if (n <= 0) return NULL;
    wchar_t* wide = (wchar_t*)malloc(n * sizeof(wchar_t));
    if (wide == NULL) return NULL;
    if (MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, utf8, -1, wide, n) != n) {
        free(wide);
        return NULL;
    }
    return wide;
}

FILE* u8_fopen(const char* path, const char* mode)
{
    wchar_t* wpath = u8_to_wide(path);
    wchar_t* wmode = u8_to_wide(mode);
    FILE* f = NULL;
    if (wpath != NULL && wmode != NULL)
        f = _wfopen(wpath, wmode);
    else
        errno = EINVAL;
    free(wpath);
    free(wmode);
    return f;
}

int u8_open(const char* path, int flags)
{
    wchar_t* wpath = u8_to_wide(path);
    int fh = -1;
    if (wpath != NULL)
        fh = _wopen(wpath, flags);
    else
        errno = EINVAL;
    free(wpath);
    return fh;
}

int u8_remove(const char* path)
{
    wchar_t* wpath = u8_to_wide(path);
    int result = -1;
    if (wpath != NULL)
        result = _wremove(wpath);
    else
        errno = EINVAL;
    free(wpath);
    return result;
}

int u8_rename(const char* from, const char* to)
{
    wchar_t* wfrom = u8_to_wide(from);
    wchar_t* wto = u8_to_wide(to);
    int result = -1;
    if (wfrom != NULL && wto != NULL)
        result = _wrename(wfrom, wto);
    else
        errno = EINVAL;
    free(wfrom);
    free(wto);
    return result;
}

int u8_replace(const char* from, const char* to)
{
    wchar_t* wfrom = u8_to_wide(from);
    wchar_t* wto = u8_to_wide(to);
    int result = -1;
    if (wfrom != NULL && wto != NULL) {
        if (MoveFileExW(wfrom, wto, MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH))
            result = 0;
        else
            errno = EACCES;
    } else {
        errno = EINVAL;
    }
    free(wfrom);
    free(wto);
    return result;
}

#endif /* WIN32 */
