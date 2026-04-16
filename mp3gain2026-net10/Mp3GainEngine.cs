using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mp3Gain2026;

/// <summary>
/// High-level managed wrapper around the mp3gain2026 native DLL.
/// Provides async methods and type-safe results.
/// </summary>
public sealed class Mp3GainEngine : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;
    private bool _initialized;

    public event Action<int>? ProgressChanged;
    private static NativeMethods.MP3G_ProgressCallback? _progressDelegate;

    public const double DefaultTargetDb = 89.0;

    public Mp3GainEngine()
    {
        int result = NativeMethods.MP3G_Init();
        if (result != NativeMethods.MP3G_OK)
            throw new InvalidOperationException("Failed to initialize mp3gain2026 native library.");
        _initialized = true;

        if (_progressDelegate == null)
        {
            _progressDelegate = OnNativeProgress;
            NativeMethods.MP3G_SetProgressCallback(_progressDelegate, IntPtr.Zero);
        }
    }

    private void OnNativeProgress(int percent, IntPtr userData)
    {
        ProgressChanged?.Invoke(percent);
    }

    public string GetVersion()
    {
        var sb = new StringBuilder(64);
        NativeMethods.MP3G_GetVersion(sb, sb.Capacity);
        return sb.ToString();
    }

    /// <summary>
    /// Analyzes a single MP3 file for its ReplayGain values.
    /// </summary>
    public async Task<AnalysisResult> AnalyzeFileAsync(string filePath, bool ignoreTags = false, bool recalcTags = false, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        return await Task.Run(async () =>
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();

                var native = new NativeMethods.MP3G_AnalysisResult();
                int ret = NativeMethods.MP3G_AnalyzeFile(filePath, ref native, ignoreTags ? 1 : 0, recalcTags ? 1 : 0);

                if (ret != NativeMethods.MP3G_OK)
                {
                    string error = GetLastErrorMessage();
                    throw new Mp3GainException(ret, error);
                }

                var tags = new NativeMethods.MP3G_TagInfo();
                NativeMethods.MP3G_ReadTags(filePath, ref tags);

                return new AnalysisResult
                {
                    TrackGain = native.TrackGain,
                    TrackPeak = native.TrackPeak,
                    AlbumGain = native.AlbumGain,
                    AlbumPeak = native.AlbumPeak,
                    MaxGlobalGain = native.MaxGlobalGain,
                    MinGlobalGain = native.MinGlobalGain,
                    MaxNoClipGain = native.MaxNoClipGain,
                    HasUndo = tags.HaveUndo != 0,
                    UndoLeft = tags.UndoLeft,
                    UndoRight = tags.UndoRight
                };
            }
            finally
            {
                _semaphore.Release();
            }
        }, ct);
    }

    /// <summary>
    /// Reads existing ReplayGain tags from an MP3 file.
    /// </summary>
    public async Task<TagInfo> ReadTagsAsync(string filePath, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        return await Task.Run(async () =>
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();

                var native = new NativeMethods.MP3G_TagInfo();
                int ret = NativeMethods.MP3G_ReadTags(filePath, ref native);

                if (ret != NativeMethods.MP3G_OK)
                {
                    string error = GetLastErrorMessage();
                    throw new Mp3GainException(ret, error);
                }

                return new TagInfo
                {
                    HasTrackGain = native.HaveTrackGain != 0,
                    HasTrackPeak = native.HaveTrackPeak != 0,
                    HasAlbumGain = native.HaveAlbumGain != 0,
                    HasAlbumPeak = native.HaveAlbumPeak != 0,
                    HasUndo = native.HaveUndo != 0,
                    TrackGain = native.TrackGain,
                    TrackPeak = native.TrackPeak,
                    AlbumGain = native.AlbumGain,
                    AlbumPeak = native.AlbumPeak,
                    UndoLeft = native.UndoLeft,
                    UndoRight = native.UndoRight,
                    MinGain = native.MinGain,
                    MaxGain = native.MaxGain
                };
            }
            finally
            {
                _semaphore.Release();
            }
        }, ct);
    }

    /// <summary>
    /// Applies a raw gain change (in mp3gain integer steps) to a file.
    /// </summary>
    public async Task ApplyGainAsync(string filePath, int gainChange,
        bool preserveTimestamp = true, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await Task.Run(async () =>
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                int ret = NativeMethods.MP3G_ApplyGain(filePath, gainChange,
                    preserveTimestamp ? 1 : 0);

                if (ret != NativeMethods.MP3G_OK)
                    throw new Mp3GainException(ret, GetLastErrorMessage());
            }
            finally
            {
                _semaphore.Release();
            }
        }, ct);
    }

    /// <summary>
    /// Normalizes a file to the specified target dB level.
    /// </summary>
    public async Task ApplyTrackGainAsync(string filePath, double targetDb = DefaultTargetDb,
        bool preserveTimestamp = true, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await Task.Run(async () =>
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                int ret = NativeMethods.MP3G_ApplyTrackGain(filePath, targetDb,
                    preserveTimestamp ? 1 : 0);

                if (ret != NativeMethods.MP3G_OK)
                    throw new Mp3GainException(ret, GetLastErrorMessage());
            }
            finally
            {
                _semaphore.Release();
            }
        }, ct);
    }

    /// <summary>
    /// Undoes a previous gain change using stored tag data.
    /// </summary>
    public async Task UndoGainAsync(string filePath,
        bool preserveTimestamp = true, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await Task.Run(async () =>
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                int ret = NativeMethods.MP3G_UndoGain(filePath,
                    preserveTimestamp ? 1 : 0);

                if (ret != NativeMethods.MP3G_OK)
                    throw new Mp3GainException(ret, GetLastErrorMessage());
            }
            finally
            {
                _semaphore.Release();
            }
        }, ct);
    }

    /// <summary>
    /// Removes all ReplayGain tags from a file.
    /// </summary>
    public async Task RemoveTagsAsync(string filePath,
        bool preserveTimestamp = true, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await Task.Run(async () =>
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                int ret = NativeMethods.MP3G_RemoveTags(filePath,
                    preserveTimestamp ? 1 : 0);

                if (ret != NativeMethods.MP3G_OK)
                    throw new Mp3GainException(ret, GetLastErrorMessage());
            }
            finally
            {
                _semaphore.Release();
            }
        }, ct);
    }

    /// <summary>
    /// Writes gain information and undo data to the APE tag.
    /// </summary>
    public async Task WriteTagsAsync(string filePath, Mp3FileInfo fileInfo, double targetDb = DefaultTargetDb,
        bool preserveTimestamp = true, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await Task.Run(async () =>
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                var result = new NativeMethods.MP3G_AnalysisResult
                {
                    TrackGain = fileInfo.TrackGain ?? 0,
                    TrackPeak = fileInfo.TrackPeak ?? 0,
                    AlbumGain = fileInfo.AlbumGain ?? 0,
                    AlbumPeak = fileInfo.AlbumPeak ?? 0,
                    MinGlobalGain = fileInfo.MinGlobalGain ?? 0,
                    MaxGlobalGain = fileInfo.MaxGlobalGain ?? 0,
                    MaxNoClipGain = fileInfo.MaxNoClipGain ?? 0
                };

                int haveUndo = fileInfo.HasUndo ? 1 : 0;
                int undoLeft = fileInfo.UndoLeft;
                int undoRight = fileInfo.UndoRight;

                int ret = NativeMethods.MP3G_WriteTags(filePath, ref result, targetDb, haveUndo, undoLeft, undoRight, preserveTimestamp ? 1 : 0);

                if (ret != NativeMethods.MP3G_OK)
                    throw new Mp3GainException(ret, GetLastErrorMessage());
            }
            finally
            {
                _semaphore.Release();
            }
        }, ct);
    }

    private string GetLastErrorMessage()
    {
        var sb = new StringBuilder(2048);
        NativeMethods.MP3G_GetLastError(sb, sb.Capacity);
        string msg = sb.ToString();
        return string.IsNullOrEmpty(msg) ? "Unknown native error" : msg;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_initialized)
        {
            NativeMethods.MP3G_Shutdown();
            _initialized = false;
        }
        _semaphore.Dispose();
    }
}

/// <summary>
/// Result of analyzing a single MP3 file.
/// </summary>
public class AnalysisResult
{
    public double TrackGain { get; init; }
    public double TrackPeak { get; init; }
    public double AlbumGain { get; init; }
    public double AlbumPeak { get; init; }
    public int MaxGlobalGain { get; init; }
    public int MinGlobalGain { get; init; }
    public int MaxNoClipGain { get; init; }
    public bool HasUndo { get; init; }
    public int UndoLeft { get; init; }
    public int UndoRight { get; init; }

    /// <summary>
    /// Gets the dB change needed to reach the target volume (89.0 dB default).
    /// </summary>
    public double GetRequiredChange(double targetDb = 89.0) => targetDb - 89.0 + TrackGain;

    /// <summary>
    /// Returns true if applying gain to the target would clip.
    /// </summary>
    public bool WouldClip(double targetDb = 89.0)
    {
        double change = GetRequiredChange(targetDb);
        return Math.Abs(change) > MaxNoClipGain * 1.5;
    }
}

/// <summary>
/// Existing ReplayGain tag data read from a file.
/// </summary>
public class TagInfo
{
    public bool HasTrackGain { get; init; }
    public bool HasTrackPeak { get; init; }
    public bool HasAlbumGain { get; init; }
    public bool HasAlbumPeak { get; init; }
    public bool HasUndo { get; init; }
    public double TrackGain { get; init; }
    public double TrackPeak { get; init; }
    public double AlbumGain { get; init; }
    public double AlbumPeak { get; init; }
    public int UndoLeft { get; init; }
    public int UndoRight { get; init; }
    public int MinGain { get; init; }
    public int MaxGain { get; init; }
}

/// <summary>
/// Exception thrown when a native mp3gain2026 operation fails.
/// </summary>
public class Mp3GainException : Exception
{
    public int NativeErrorCode { get; }

    public Mp3GainException(int errorCode, string message)
        : base($"MP3Gain error {errorCode}: {message}")
    {
        NativeErrorCode = errorCode;
    }
}
