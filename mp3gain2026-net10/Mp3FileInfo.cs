using System;
using System.IO;

namespace Mp3Gain2026;

/// <summary>
/// Represents an MP3 file in the analysis/processing list.
/// </summary>
public class Mp3FileInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);
    public string Directory => Path.GetDirectoryName(FilePath) ?? string.Empty;
    public long FileSize { get; set; }
    public int Progress { get; set; } = -1;

    /// <summary>Current target dB for gain calculations. Updated from the UI spinner.</summary>
    public double TargetDb { get; set; } = 89.0;

    /* ── Analysis Results ──────────────────────────────────────── */
    public double? TrackGain { get; set; }
    public double? TrackPeak { get; set; }
    public double? AlbumGain { get; set; }
    public double? AlbumPeak { get; set; }
    public int? MaxGlobalGain { get; set; }
    public int? MinGlobalGain { get; set; }
    public int? MaxNoClipGain { get; set; }

    /* ── Undo Information ────────────────────────────────────────── */
    public bool HasUndo { get; set; }
    public int UndoLeft { get; set; }
    public int UndoRight { get; set; }

    /* ── Legacy-style gain quantization ───────────────────────── */
    // mp3gain works in integer steps of ~1.505 dB (5 * log10(2)).
    // The legacy GUI displayed gain quantized to these steps.
    private const double DbPerStep = 1.50514997831991; // 5 * log10(2)

    /// <summary>
    /// Quantize a raw dB gain value to the nearest integer mp3gain step
    /// and convert back to dB, matching legacy mp3gain GUI display.
    /// </summary>
    public static double QuantizeGain(double rawGain)
    {
        double steps = rawGain / DbPerStep;
        double absSteps = Math.Abs(steps);
        int intSteps;
        if (absSteps - (int)absSteps < 0.5)
            intSteps = (int)steps;
        else
            intSteps = (int)steps + (steps < 0 ? -1 : 1);
        return Math.Round(intSteps * DbPerStep, 1);
    }

    /// <summary>Track gain quantized to legacy mp3gain steps for display.</summary>
    public double? TrackGainDisplay => TrackGain.HasValue ? QuantizeGain(TrackGain.Value) : null;

    /// <summary>Album gain quantized to legacy mp3gain steps for display.</summary>
    public double? AlbumGainDisplay
    {
        get
        {
            if (!AlbumGain.HasValue) return null;
            double raw = TargetDb - 89.0 + AlbumGain.Value;
            return QuantizeGain(raw);
        }
    }

    /* ── Computed Properties ───────────────────────────────────── */
    public double? GainChange(double targetDb)
    {
        if (!TrackGain.HasValue) return null;
        return targetDb - 89.0 + TrackGain.Value;
    }

    /// <summary>Gain change using the current TargetDb, for grid binding.</summary>
    public double? GainChangeDisplay
    {
        get
        {
            if (!TrackGain.HasValue) return null;
            double raw = TargetDb - 89.0 + TrackGain.Value;
            return QuantizeGain(raw);
        }
    }

    public double? MaxAmplitude
    {
        get
        {
            if (!TrackPeak.HasValue) return null;
            return TrackPeak.Value * 32768.0;
        }
    }

    public bool? WouldClip(double targetDb)
    {
        if (!TrackGain.HasValue || !TrackPeak.HasValue) return null;
        double change = GainChange(targetDb) ?? 0;
        double amplifiedPeak = TrackPeak.Value * Math.Pow(10.0, change / 20.0);
        return amplifiedPeak > 1.0;
    }

    public double? Volume
    {
        get
        {
            if (!TrackGain.HasValue) return null;
            return TargetDb - (TargetDb - 89.0 + TrackGain.Value);
        }
    }

    public string Clipping
    {
        get
        {
            if (!TrackPeak.HasValue) return string.Empty;
            return TrackPeak.Value >= 1.0 ? "Y" : "N";
        }
    }

    /* ── Tag Info ──────────────────────────────────────────────── */
    public bool HasExistingTags { get; set; }

    /* ── Clear Analysis ────────────────────────────────────────── */
    public void ClearAnalysis()
    {
        TrackGain = null;
        TrackPeak = null;
        AlbumGain = null;
        AlbumPeak = null;
        MaxGlobalGain = null;
        MinGlobalGain = null;
        MaxNoClipGain = null;
        Progress = -1;
        Status = FileStatus.Pending;
        StatusMessage = null;
        HasExistingTags = false;
        HasUndo = false;
    }

    /* ── Status ───────────────────────────────────────────────── */
    public FileStatus Status { get; set; } = FileStatus.Pending;
    public string? StatusMessage { get; set; }

    public string FileSizeFormatted
    {
        get
        {
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
            return $"{FileSize / (1024.0 * 1024.0):F1} MB";
        }
    }

    public static Mp3FileInfo FromPath(string path)
    {
        var info = new FileInfo(path);
        return new Mp3FileInfo
        {
            FilePath = path,
            FileSize = info.Exists ? info.Length : 0
        };
    }
}

public enum FileStatus
{
    Pending,
    Analyzing,
    Analyzed,
    Applying,
    Applied,
    Error,
    Skipped
}
