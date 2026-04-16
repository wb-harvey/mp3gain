/*
 *  dllmain.cpp - DLL entry point for mp3gain2026
 */

#define WIN32
#define asWIN32DLL

#include <windows.h>

BOOL APIENTRY DllMain(HMODULE hModule,
                      DWORD  dwReason,
                      LPVOID lpReserved)
{
    switch (dwReason)
    {
    case DLL_PROCESS_ATTACH:
        DisableThreadLibraryCalls(hModule);
        break;
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}
