cmake_minimum_required(VERSION 3.20)
project(mp3gain2026 LANGUAGES C CXX)

set(CMAKE_C_STANDARD 11)
set(CMAKE_CXX_STANDARD 17)

# ── Preprocessor Definitions ──────────────────────────────────────
add_definitions(-DWIN32 -D_WINDOWS -DasWIN32DLL -DMP3GAIN2026_EXPORTS -D_CRT_SECURE_NO_WARNINGS -DHAVE_CONFIG_H -DSTDC_HEADERS -DHAVE_MEMCPY)

# ── Source Files ──────────────────────────────────────────────────
set(LEGACY_SOURCES
    legacy/gain_analysis.c
    legacy/mp3gain.c
    legacy/apetag.c
    legacy/id3tag.c
    legacy/rg_error.c
    legacy/mpglibDBL/common.c
    legacy/mpglibDBL/dct64_i386.c
    legacy/mpglibDBL/decode_i386.c
    legacy/mpglibDBL/interface.c
    legacy/mpglibDBL/layer1.c
    legacy/mpglibDBL/layer2.c
    legacy/mpglibDBL/layer3.c
    legacy/mpglibDBL/tabinit.c
)

set(API_SOURCES
    dllmain.cpp
    mp3gain2026_api.cpp
)

# ── Include Directories ──────────────────────────────────────────
include_directories(
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/legacy
    ${CMAKE_CURRENT_SOURCE_DIR}/legacy/mpglibDBL
)

# ── Shared Library ────────────────────────────────────────────────
add_library(mp3gain2026 SHARED
    ${LEGACY_SOURCES}
    ${API_SOURCES}
    mp3gain2026.def
)

target_link_libraries(mp3gain2026 PRIVATE winmm)

# ── Compiler Flags ────────────────────────────────────────────────
if(MSVC)
    target_compile_options(mp3gain2026 PRIVATE /W3 /wd4996 /wd4244 /wd4267 /wd4018 /wd4101 /wd4305)
endif()

# ── Output ────────────────────────────────────────────────────────
set_target_properties(mp3gain2026 PROPERTIES
    OUTPUT_NAME "mp3gain2026"
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib"
    ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib"
)
