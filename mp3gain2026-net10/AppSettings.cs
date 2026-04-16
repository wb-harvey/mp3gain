using System;
using System.IO;
using System.Text.Json;

namespace Mp3Gain2026;

public class AppSettings
{
    public bool AlwaysOnTop { get; set; } = false;
    public bool AddSubfolders { get; set; } = true;
    public bool PreserveTimestamp { get; set; } = true;
    public bool NoLayerCheck { get; set; } = false;
    public bool DontClip { get; set; } = true;
    public bool IgnoreTags { get; set; } = false;
    public bool RecalcTags { get; set; } = false;
    public bool BeepWhenFinished { get; set; } = false;

    public string ErrorLogPath { get; set; } = @"C:\ProgramData\Mp3Gain2026\error.log";
    public string AnalysisLogPath { get; set; } = @"C:\ProgramData\Mp3Gain2026\analysis.log";
    public string ChangeLogPath { get; set; } = @"C:\ProgramData\Mp3Gain2026\change.log";

    public decimal TargetVolume { get; set; } = 89.0m;

    /// <summary>
    /// 0 = Path/File, 1 = File Only, 2 = Path and File Separately
    /// </summary>
    public int FilenameDisplayMode { get; set; } = 0;

    private static string GetConfigPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "Mp3Gain2026");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return Path.Combine(dir, "settings.json");
    }

    public static AppSettings Load()
    {
        var path = GetConfigPath();
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                    return settings;
            }
            catch { }
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(GetConfigPath(), json);
        }
        catch { }
    }
}
