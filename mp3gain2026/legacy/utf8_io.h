/*
 *  utf8_io.h - Unicode file names for the legacy mp3gain code
 *
 *  The legacy code passes file names around as char*. The mp3gain2026 API now hands it UTF-8 (converted
 *  from the caller's UTF-16 or ANSI path), and these wrappers open, remove and rename files through the
 *  wide-character Windows functions, so any file name works, not only ones in the system code page.
 *
 *  Include it after the system headers in each legacy file that touches files: the macros below then
 *  route that file's fopen / _open / remove / rename calls here.
 *
 *  Copyright (C) 2026. LGPL v2.1+
 */

#ifndef MP3GAIN_UTF8_IO_H
#define MP3GAIN_UTF8_IO_H

#ifdef WIN32

#include <stdio.h>
#include <wchar.h>

#ifdef __cplusplus
extern "C" {
#endif

/* A malloc'd UTF-16 copy of a UTF-8 string (free it), or NULL if it isn't valid UTF-8 */
wchar_t* u8_to_wide(const char* utf8);

FILE* u8_fopen(const char* path, const char* mode);
int   u8_open(const char* path, int flags);
int   u8_remove(const char* path);
int   u8_rename(const char* from, const char* to);

#ifdef __cplusplus
}
#endif

#ifndef MP3GAIN_UTF8_IO_IMPL
#define fopen(path, mode)  u8_fopen(path, mode)
#define _open(path, flags) u8_open(path, flags)
#define remove(path)       u8_remove(path)
#define rename(from, to)   u8_rename(from, to)
#endif

#endif /* WIN32 */

#endif /* MP3GAIN_UTF8_IO_H */
