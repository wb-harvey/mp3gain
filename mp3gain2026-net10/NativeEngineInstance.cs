using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Mp3Gain2026;

/// <summary>
/// Represents a single loaded instance of the mp3gain2026 native DLL.
/// Each instance has its own global state, enabling concurrent analysis
/// of different files from separate threads.
/// 
/// On Windows, LoadLibrary with different file paths creates independent
/// DLL images with separate global variables. We copy the DLL to temp
/// files to achieve this isolation.
/// </summary>
internal sealed class NativeEngineInstance : IDisposable
{
    private IntPtr _handle;
    private readonly string? _tempDllPath;
    private bool _disposed;
    private bool _initialized;

    /* ── Delegate types matching native function signatures ───── */
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int InitDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ShutdownDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetVersionDelegate(StringBuilder buffer, int bufLen);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate int AnalyzeFileDelegate(
        [MarshalAs(UnmanagedType.LPStr)] string filePath,
        ref NativeMethods.MP3G_AnalysisResult result,
        int ignoreTags, int recalcTags);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int AlbumInitDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate int AlbumAnalyzeFileDelegate(
        [MarshalAs(UnmanagedType.LPStr)] string filePath,
        ref NativeMethods.MP3G_AnalysisResult result,
        int ignoreTags, int recalcTags);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int AlbumGetResultDelegate(ref double albumGain, ref double albumPeak);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void AlbumFinishDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate int ApplyGainDelegate(
        [MarshalAs(UnmanagedType.LPStr)] string filePath,
        int gainChange, int preserveTimestamp);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate int ApplyTrackGainDelegate(
        [MarshalAs(UnmanagedType.LPStr)] string filePath,
        double targetDb, int preserveTimestamp);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate int UndoGainDelegate(
        [MarshalAs(UnmanagedType.LPStr)] string filePath,
        int preserveTimestamp);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate int ReadTagsDelegate(
        [MarshalAs(UnmanagedType.LPStr)] string filePath,
        ref NativeMethods.MP3G_TagInfo tagInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate int WriteTagsDelegate(
        [MarshalAs(UnmanagedType.LPStr)] string filePath,
        ref NativeMethods.MP3G_AnalysisResult result,
        double targetDb, int haveUndo, int undoLeft, int undoRight,
        int preserveTimestamp);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate int RemoveTagsDelegate(
        [MarshalAs(UnmanagedType.LPStr)] string filePath,
        int preserveTimestamp);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetLastErrorDelegate(StringBuilder buffer, int bufLen);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetProgressCallbackDelegate(
        NativeMethods.MP3G_ProgressCallback callback, IntPtr userData);

    /* ── Bound function pointers ─────────────────────────────── */
    public InitDelegate Init { get; private set; } = null!;
    public ShutdownDelegate Shutdown { get; private set; } = null!;
    public GetVersionDelegate GetVersion { get; private set; } = null!;
    public AnalyzeFileDelegate AnalyzeFile { get; private set; } = null!;
    public AlbumInitDelegate AlbumInit { get; private set; } = null!;
    public AlbumAnalyzeFileDelegate AlbumAnalyzeFile { get; private set; } = null!;
    public AlbumGetResultDelegate AlbumGetResult { get; private set; } = null!;
    public AlbumFinishDelegate AlbumFinish { get; private set; } = null!;
    public ApplyGainDelegate ApplyGain { get; private set; } = null!;
    public ApplyTrackGainDelegate ApplyTrackGain { get; private set; } = null!;
    public UndoGainDelegate UndoGain { get; private set; } = null!;
    public ReadTagsDelegate ReadTags { get; private set; } = null!;
    public WriteTagsDelegate WriteTags { get; private set; } = null!;
    public RemoveTagsDelegate RemoveTags { get; private set; } = null!;
    public GetLastErrorDelegate GetLastError { get; private set; } = null!;
    public SetProgressCallbackDelegate SetProgressCallback { get; private set; } = null!;

    /// <summary>
    /// Unique ID for this instance (for diagnostics).
    /// </summary>
    public int InstanceId { get; }

    /// <summary>
    /// Creates a new engine instance by loading a unique copy of the native DLL.
    /// </summary>
    /// <param name="sourceDllPath">Path to the original mp3gain2026.dll</param>
    /// <param name="instanceId">Unique integer identifier for this instance</param>
    public NativeEngineInstance(string sourceDllPath, int instanceId)
    {
        InstanceId = instanceId;

        if (instanceId == 0)
        {
            // Instance 0 loads the DLL directly (no copy needed)
            _handle = NativeLibrary.Load(sourceDllPath);
            _tempDllPath = null;
        }
        else
        {
            string dir = Path.GetDirectoryName(sourceDllPath)!;

            // Optional: Background cleanup of orphaned temp DLLs from previous crashes
            if (instanceId == 1) 
            {
                Task.Run(() => {
                    try {
                        foreach (var f in Directory.GetFiles(dir, "mp3gain2026_temp_*.dll")) {
                            try { File.Delete(f); } catch { /* locked or no access */ }
                        }
                    } catch { } // ignore directory access errors
                });
            }

            // Create a unique copy of the DLL so Windows gives us independent globals
            string uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
            string tempName = $"mp3gain2026_temp_{instanceId}_{uniqueId}.dll";
            _tempDllPath = Path.Combine(dir, tempName);

            // Copy the DLL file
            File.Copy(sourceDllPath, _tempDllPath, overwrite: true);

            _handle = NativeLibrary.Load(_tempDllPath);
        }

        BindFunctions();

        // Initialize this DLL instance
        int ret = Init();
        if (ret != NativeMethods.MP3G_OK)
            throw new InvalidOperationException($"Failed to initialize engine instance {instanceId}");
        _initialized = true;
    }

    private void BindFunctions()
    {
        Init = GetExport<InitDelegate>("MP3G_Init");
        Shutdown = GetExport<ShutdownDelegate>("MP3G_Shutdown");
        GetVersion = GetExport<GetVersionDelegate>("MP3G_GetVersion");
        AnalyzeFile = GetExport<AnalyzeFileDelegate>("MP3G_AnalyzeFile");
        AlbumInit = GetExport<AlbumInitDelegate>("MP3G_AlbumInit");
        AlbumAnalyzeFile = GetExport<AlbumAnalyzeFileDelegate>("MP3G_AlbumAnalyzeFile");
        AlbumGetResult = GetExport<AlbumGetResultDelegate>("MP3G_AlbumGetResult");
        AlbumFinish = GetExport<AlbumFinishDelegate>("MP3G_AlbumFinish");
        ApplyGain = GetExport<ApplyGainDelegate>("MP3G_ApplyGain");
        ApplyTrackGain = GetExport<ApplyTrackGainDelegate>("MP3G_ApplyTrackGain");
        UndoGain = GetExport<UndoGainDelegate>("MP3G_UndoGain");
        ReadTags = GetExport<ReadTagsDelegate>("MP3G_ReadTags");
        WriteTags = GetExport<WriteTagsDelegate>("MP3G_WriteTags");
        RemoveTags = GetExport<RemoveTagsDelegate>("MP3G_RemoveTags");
        GetLastError = GetExport<GetLastErrorDelegate>("MP3G_GetLastError");
        SetProgressCallback = GetExport<SetProgressCallbackDelegate>("MP3G_SetProgressCallback");
    }

    private T GetExport<T>(string name) where T : Delegate
    {
        IntPtr proc = NativeLibrary.GetExport(_handle, name);
        return Marshal.GetDelegateForFunctionPointer<T>(proc);
    }

    public string GetLastErrorMessage()
    {
        var sb = new StringBuilder(2048);
        GetLastError(sb, sb.Capacity);
        string msg = sb.ToString();
        return string.IsNullOrEmpty(msg) ? "Unknown native error" : msg;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_initialized)
        {
            try { Shutdown(); } catch { /* best effort */ }
            _initialized = false;
        }

        if (_handle != IntPtr.Zero)
        {
            NativeLibrary.Free(_handle);
            _handle = IntPtr.Zero;
        }

        // Clean up the temp DLL copy
        if (_tempDllPath != null)
        {
            try { File.Delete(_tempDllPath); } catch { /* may be locked briefly */ }
        }
    }
}
