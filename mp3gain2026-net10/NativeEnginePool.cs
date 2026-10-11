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
    private SemaphoreSlim _semaphore;
    private NativeEngineInstance[] _allInstances;
    private bool _disposed;

    /// <summary>Number of parallel engine instances in this pool.</summary>
    public int PoolSize { get; private set; }

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

        // Instances after the first load a renamed copy of the DLL (Windows gives each its own globals).
        // The copies used to go next to the DLL, which fails without admin rights under Program Files and
        // took the whole engine down; they now go in a per-process folder under %LOCALAPPDATA%.
        if (poolSize > 1)
        {
            _copiesDir = PrepareCopiesDirectory();
            if (_copiesDir == null)
            {
                // No writable folder for copies: one engine, loaded directly, still works
                poolSize = 1;
                PoolSize = 1;
                _semaphore.Dispose();
                _semaphore = new SemaphoreSlim(1, 1);
                _allInstances = new NativeEngineInstance[1];
            }
        }

        // Create all instances; if one fails, free the ones already loaded before giving up
        try
        {
            for (int i = 0; i < poolSize; i++)
            {
                var instance = new NativeEngineInstance(dllPath, i, _copiesDir);
                _allInstances[i] = instance;
                _available.Add(instance);
            }
        }
        catch
        {
            foreach (var instance in _allInstances)
                instance?.Dispose();
            DeleteCopiesDirectory();
            throw;
        }
    }

    private readonly string? _copiesDir;

    /// <summary>
    /// Creates %LOCALAPPDATA%\Mp3Gain2026\engines\&lt;process id&gt; and removes the folders of processes that are
    /// no longer running (left by a crash). Done before any copy is made, so the cleanup can't delete a
    /// copy that is about to be loaded. Null when no folder can be created.
    /// </summary>
    private static string? PrepareCopiesDirectory()
    {
        try
        {
            string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mp3Gain2026", "engines");
            Directory.CreateDirectory(root);
            foreach (string dir in Directory.GetDirectories(root))
            {
                if (int.TryParse(Path.GetFileName(dir), out int pid) && pid != Environment.ProcessId && !IsRunning(pid))
                {
                    try { Directory.Delete(dir, recursive: true); } catch { /* still locked */ }
                }
            }
            string mine = Path.Combine(root, Environment.ProcessId.ToString());
            Directory.CreateDirectory(mine);
            return mine;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsRunning(int pid)
    {
        try
        {
            using var p = System.Diagnostics.Process.GetProcessById(pid);
            return !p.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private void DeleteCopiesDirectory()
    {
        if (_copiesDir == null) return;
        try { Directory.Delete(_copiesDir, recursive: true); } catch { /* removed at the next start */ }
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
        DeleteCopiesDirectory();
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
