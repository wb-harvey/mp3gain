using System;
using System.Runtime.InteropServices;

namespace Mp3Gain2026;

/// <summary>
/// P/Invoke declarations for the mp3gain2026 native DLL.
/// </summary>
internal static class NativeMethods
{
    private const string DllName = "mp3gain2026";

    /* ── Result Codes ─────────────────────────────────────────── */
    public const int MP3G_OK            =  0;
    public const int MP3G_ERR_GENERIC   = -1;
    public const int MP3G_ERR_FILEOPEN  = -2;
    public const int MP3G_ERR_FILEREAD  = -3;
    public const int MP3G_ERR_FILEWRITE = -4;
    public const int MP3G_ERR_NOTMP3    = -5;
    public const int MP3G_ERR_CANCELLED = -6;
    public const int MP3G_ERR_INVALIDARG = -7;

    /* ── Analysis Result Structure ────────────────────────────── */
    [StructLayout(LayoutKind.Sequential)]
    public struct MP3G_AnalysisResult
    {
        public double TrackGain;
        public double TrackPeak;
        public double AlbumGain;
        public double AlbumPeak;
        public int MaxGlobalGain;
        public int MinGlobalGain;
        public int MaxNoClipGain;
        public int AlbumMaxNoClipGain;
    }

    /* ── Tag Info Structure ───────────────────────────────────── */
    [StructLayout(LayoutKind.Sequential)]
    public struct MP3G_TagInfo
    {
        public int HaveTrackGain;
        public int HaveTrackPeak;
        public int HaveAlbumGain;
        public int HaveAlbumPeak;
        public int HaveUndo;
        public double TrackGain;
        public double TrackPeak;
        public double AlbumGain;
        public double AlbumPeak;
        public int UndoLeft;
        public int UndoRight;
        public int UndoWrap;
        public int MinGain;
        public int MaxGain;
    }

    /* ── Library Lifecycle ────────────────────────────────────── */
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int MP3G_Init();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void MP3G_Shutdown();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int MP3G_GetVersion(
        [MarshalAs(UnmanagedType.LPStr)] System.Text.StringBuilder buffer,
        int bufLen);

    /* ── Single File Analysis ─────────────────────────────────── */
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int MP3G_AnalyzeFile(
        [MarshalAs(UnmanagedType.LPStr)] string filePath,
        ref MP3G_AnalysisResult result,
        int ignoreTags,
        int recalcTags);

    /* ── Album Analysis ───────────────────────────────────────── */
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int MP3G_AlbumInit();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int MP3G_AlbumAnalyzeFile(
        [MarshalAs(UnmanagedType.LPStr)] string filePath,
        ref MP3G_AnalysisResult result,
        int ignoreTags,
        int recalcTags);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int MP3G_AlbumGetResult(
        ref double albumGain,
        ref double albumPeak);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void MP3G_AlbumFinish();

    /* ── Gain Modification ────────────────────────────────────── */
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int MP3G_ApplyGain(
        [MarshalAs(UnmanagedType.LPStr)] string filePath,
        int gainChange,
        int preserveTimestamp);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int MP3G_ApplyTrackGain(
        [MarshalAs(UnmanagedType.LPStr)] string filePath,
        double targetDb,
        int preserveTimestamp);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int MP3G_UndoGain(
        [MarshalAs(UnmanagedType.LPStr)] string filePath,
        int preserveTimestamp);

    /* ── Tag Operations ───────────────────────────────────────── */
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int MP3G_ReadTags(
        [MarshalAs(UnmanagedType.LPStr)] string filePath,
        ref MP3G_TagInfo tagInfo);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int MP3G_WriteTags(
        [MarshalAs(UnmanagedType.LPStr)] string filePath,
        ref MP3G_AnalysisResult result,
        double targetDb,
        int haveUndo,
        int undoLeft,
        int undoRight,
        int preserveTimestamp);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int MP3G_RemoveTags(
        [MarshalAs(UnmanagedType.LPStr)] string filePath,
        int preserveTimestamp);

    /* ── Error Reporting ──────────────────────────────────────── */
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int MP3G_GetLastError(
        [MarshalAs(UnmanagedType.LPStr)] System.Text.StringBuilder buffer,
        int bufLen);

    /* ── Progress ─────────────────────────────────────────────────── */
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MP3G_ProgressCallback(int percent, IntPtr userData);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void MP3G_SetProgressCallback(
        MP3G_ProgressCallback callback,
        IntPtr userData);
}
