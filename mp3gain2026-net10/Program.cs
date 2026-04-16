using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using System.Threading;
using System.Diagnostics;
        
namespace Mp3Gain2026;

static class Program
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    private const int SW_RESTORE = 9;

    [STAThread]
    static void Main()
    {
        bool createdNew;
        using (var mutex = new Mutex(true, "Mp3Gain2026_SingleInstance_Mutex", out createdNew))
        {
            if (!createdNew)
            {
                var currentProcess = Process.GetCurrentProcess();
                foreach (var process in Process.GetProcessesByName(currentProcess.ProcessName))
                {
                    if (process.Id != currentProcess.Id && process.MainWindowHandle != IntPtr.Zero)
                    {
                        IntPtr hWnd = process.MainWindowHandle;
                        if (IsIconic(hWnd)) ShowWindow(hWnd, SW_RESTORE);
                        SetForegroundWindow(hWnd);
                        break;
                    }
                }
                return;
            }

            // Enable high-DPI support
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            ApplicationConfiguration.Initialize();

            // Resolve native DLL based on process architecture
            NativeLibraryResolver.Register();

            Application.Run(new MainForm());
        }
    }
}

/// <summary>
/// Resolves the mp3gain2026 native DLL from the correct architecture-specific directory.
/// </summary>
internal static class NativeLibraryResolver
{
    public static void Register()
    {
        NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, ResolveDll);
    }

    private static IntPtr ResolveDll(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != "mp3gain2026")
            return IntPtr.Zero;

        string arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "win-x86",
            Architecture.X64 => "win-x64",
            _ => "win-x64"
        };

        string basePath = AppContext.BaseDirectory;
        string dllPath = Path.Combine(basePath, "runtimes", arch, "native", "mp3gain2026.dll");

        // Fallback: try same directory
        if (!File.Exists(dllPath))
            dllPath = Path.Combine(basePath, "mp3gain2026.dll");

        if (NativeLibrary.TryLoad(dllPath, out IntPtr handle))
            return handle;

        return IntPtr.Zero;
    }
}
