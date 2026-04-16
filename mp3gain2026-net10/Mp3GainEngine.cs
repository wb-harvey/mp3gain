using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mp3Gain2026;

/// <summary>
/// High-level managed wrapper around the mp3gain2026 native DLL.
/// Uses a pool of DLL instances for concurrent file processing.
/// </summary>
public sealed class Mp3GainEngine : IDisposable
{
    private readonly NativeEnginePool _pool;
    private bool _disposed;

    public const double DefaultTargetDb = 89.0;

    /// <summary>Number of parallel analysis slots available.</summary>
    public int Concurrency => _pool.PoolSize;

    public Mp3GainEngine(int concurrency = 0)
    {
        _pool = new NativeEnginePool(concurrency);
    }

    public string GetVersion() => _pool.GetVersion();

    /// <summary>
    /// Analyzes a single MP3 file for its ReplayGain values.
    /// Multiple calls can execute concurrently — each gets its own DLL instance.
    /// </summary>
    public async Task<AnalysisResult> AnalyzeFileAsync(string filePath, bool ignoreTags = false, bool recalcTags = false, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        return await Task.Run(async () =>
        {
            var inst = await _pool.RentAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();

                // Set up per-instance progress callback
                NativeMethods.MP3G_ProgressCallback? progressDelegate = null;
                if (progress != null)
                {
                    progressDelegate = (percent, _) => progress.Report(percent);
                    inst.SetProgressCallback(progressDelegate, IntPtr.Zero);
                }

                var native = new NativeMethods.MP3G_AnalysisResult();
                int ret = inst.AnalyzeFile(filePath, ref native, ignoreTags ? 1 : 0, recalcTags ? 1 : 0);

                if (ret != NativeMethods.MP3G_OK)
                {
                    string error = inst.GetLastErrorMessage();
                    throw new Mp3GainException(ret, error);
                }

                var tags = new NativeMethods.MP3G_TagInfo();
                inst.ReadTags(filePath, ref tags);

                // Clear the progress callback
                if (progressDelegate != null)
                    inst.SetProgressCallback(null!, IntPtr.Zero);

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
                _pool.Return(inst);
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
            var inst = await _pool.RentAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();

                var native = new NativeMethods.MP3G_TagInfo();
                int ret = inst.ReadTags(filePath, ref native);

                if (ret != NativeMethods.MP3G_OK)
                {
                    string error = inst.GetLastErrorMessage();
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
                _pool.Return(inst);
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
            var inst = await _pool.RentAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                int ret = inst.ApplyGain(filePath, gainChange,
                    preserveTimestamp ? 1 : 0);

                if (ret != NativeMethods.MP3G_OK)
                    throw new Mp3GainException(ret, inst.GetLastErrorMessage());
            }
            finally
            {
                _pool.Return(inst);
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
            var inst = await _pool.RentAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                int ret = inst.ApplyTrackGain(filePath, targetDb,
                    preserveTimestamp ? 1 : 0);

                if (ret != NativeMethods.MP3G_OK)
                    throw new Mp3GainException(ret, inst.GetLastErrorMessage());
            }
            finally
            {
                _pool.Return(inst);
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
            var inst = await _pool.RentAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                int ret = inst.UndoGain(filePath,
                    preserveTimestamp ? 1 : 0);

                if (ret != NativeMethods.MP3G_OK)
                    throw new Mp3GainException(ret, inst.GetLastErrorMessage());
            }
            finally
            {
                _pool.Return(inst);
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
            var inst = await _pool.RentAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                int ret = inst.RemoveTags(filePath,
                    preserveTimestamp ? 1 : 0);

                if (ret != NativeMethods.MP3G_OK)
                    throw new Mp3GainException(ret, inst.GetLastErrorMessage());
            }
            finally
            {
                _pool.Return(inst);
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
            var inst = await _pool.RentAsync(ct);
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

                int ret = inst.WriteTags(filePath, ref result, targetDb, haveUndo, undoLeft, undoRight, preserveTimestamp ? 1 : 0);

                if (ret != NativeMethods.MP3G_OK)
                    throw new Mp3GainException(ret, inst.GetLastErrorMessage());
            }
            finally
            {
                _pool.Return(inst);
            }
        }, ct);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pool.Dispose();
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
