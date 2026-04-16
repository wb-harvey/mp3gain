# MP3Gain 2026

Modern MP3 volume normalization tool built with **.NET 10** and a native C++ DLL.

## Overview

MP3Gain 2026 analyzes MP3 files to determine their perceived volume using ReplayGain analysis, then adjusts the volume without re-encoding. This is a lossless operation — no quality loss occurs.

## Architecture

- **mp3gain2026.dll** — Native C++ DLL containing the core gain analysis engine (based on the original mp3gain by Glen Sawyer)
- **Mp3Gain2026.App** — C# .NET 10 WinForms GUI with P/Invoke interop
- **Mp3Gain2026.App** — VB.NET 4.8 WinForms GUI with P/Invoke interop

## Features

- The C# build supports multi-threaded parallel processing with an option to enforce single thread operation
- ReplayGain analysis (track and album mode)
- Lossless volume normalization
- Undo previous gain changes
- APE and ID3 tag support
- Drag-and-drop file management
- Dark-themed modern UI
- Supports both 32-bit and 64-bit builds

## Building

### Prerequisites

- .NET 10 SDK
- Visual Studio 2026 (or just the MSVC build tools) with C++ desktop workload
- CMake 3.20+

### Quick Build

```powershell
.\build.ps1 -Configuration Release
```

### Manual Build

```powershell
# Build native DLL (x64)
cd mp3gain2026
cmake -S . -B build_x64 -A x64
cmake --build build_x64 --config Release

# Build native DLL (x86)
cmake -S . -B build_x86 -A Win32
cmake --build build_x86 --config Release

# Copy DLLs to runtime directories
cp build_x64/bin/Release/mp3gain2026.dll ../Mp3Gain2026.App/runtimes/win-x64/native/
cp build_x86/bin/Release/mp3gain2026.dll ../Mp3Gain2026.App/runtimes/win-x86/native/

# Build .NET app
cd ../Mp3Gain2026.App
dotnet build -c Release
```

## License

GNU Lesser General Public License v2.1 — see `license.txt` for details.

## Credits

- **Glen Sawyer** — Original mp3gain engine
- **David Robinson** — ReplayGain concept and filter values
- **John Zitterkopf** — Original DLL wrapper
- **William Harvey** — Rebuild of app in 2026 with modern libraries and UI 

