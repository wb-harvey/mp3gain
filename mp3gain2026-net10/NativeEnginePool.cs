using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mp3Gain2026;

/// <summary>
/// Manages a pool of N independent native DLL instances, each with its own
/// global state. Callers check out an instance, use it exclusively, then
/// return it. This gives true concurrent analysis with zero native code changes.
/// </summary>
internal sealed class NativeEnginePool : IDisposable
{
    private readonly ConcurrentBag<NativeEngineInstance> _available = new();
    private readonly SemaphoreSlim _semaphore;
    private readonly NativeEngineInstance[] _allInstances;
    private bool _disposed;

    /// <summary>Number of parallel engine instances in this pool.</summary>
    public int PoolSize { get; }

    /// <summary>
    /// Creates a pool of engine instances.
    /// </summary>
    /// <param name="poolSize">Number of parallel instances (default: ProcessorCount - 1, min 1)</param>
    public NativeEnginePool(int poolSize = 0)
    {
        if (poolSize <= 0)
            poolSize = Math.Max(1, Environment.ProcessorCount - 1);

        PoolSize = poolSize;
        _semaphore = new SemaphoreSlim(poolSize, poolSize);
        _allInstances = new NativeEngineInstance[poolSize];

        // Find the DLL path — probe RID-based subdirectories first, then base dir
        string baseDir = AppContext.BaseDirectory;
        string dllPath = ResolveDllPath(baseDir);

        if (dllPath == null)
            throw new FileNotFoundException(
                "mp3gain2026.dll not found. Searched base directory and runtimes/ subdirectories.",
                Path.Combine(baseDir, "mp3gain2026.dll"));

        // Create all instances
        for (int i = 0; i < poolSize; i++)
        {
            var instance = new NativeEngineInstance(dllPath, i);
            _allInstances[i] = instance;
            _available.Add(instance);
        }
    }

    /// <summary>
    /// Checks out an engine instance from the pool. The caller MUST return it
    /// via <see cref="Return"/> when done. Use <see cref="RentAsync"/> for the
    /// async/await pattern.
    /// </summary>
    public async Task<NativeEngineInstance> RentAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _semaphore.WaitAsync(ct);

        if (!_available.TryTake(out var instance))
        {
            // Should never happen if semaphore count matches pool size
            _semaphore.Release();
            throw new InvalidOperationException("Engine pool exhausted unexpectedly");
        }

        return instance;
    }

    /// <summary>
    /// Returns a previously rented engine instance to the pool.
    /// </summary>
    public void Return(NativeEngineInstance instance)
    {
        if (_disposed) return;
        _available.Add(instance);
        _semaphore.Release();
    }

    /// <summary>
    /// Gets the version string from the first engine instance.
    /// </summary>
    public string GetVersion()
    {
        var sb = new System.Text.StringBuilder(64);
        _allInstances[0].GetVersion(sb, sb.Capacity);
        return sb.ToString();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var instance in _allInstances)
        {
            instance?.Dispose();
        }

        _semaphore.Dispose();
    }

    private string? ResolveDllPath(string baseDir)
    {
        string dllName = "mp3gain2026.dll";
        string rid = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X64 ? "win-x64" : "win-x86";
        
        string[] searchPaths = new string[]
        {
            Path.Combine(baseDir, "runtimes", rid, "native", dllName),
            Path.Combine(baseDir, dllName)
        };

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }
}
