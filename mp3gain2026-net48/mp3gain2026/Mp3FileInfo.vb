Imports System.IO

''' <summary>
''' Represents an MP3 file in the analysis/processing list.
''' </summary>
Public Class Mp3FileInfo

    Public Property FilePath As String = String.Empty

    Public ReadOnly Property FileName As String
        Get
            Return Path.GetFileName(FilePath)
        End Get
    End Property

    Public ReadOnly Property Directory As String
        Get
            Dim d = Path.GetDirectoryName(FilePath)
            Return If(d, String.Empty)
        End Get
    End Property

    Public Property FileSize As Long
    Public Property Progress As Integer = -1

    ''' <summary>Current target dB for gain calculations. Updated from the UI spinner.</summary>
    Public Property TargetDb As Double = 89.0

    ' ── Analysis Results ──
    Public Property TrackGain As Double?
    Public Property TrackPeak As Double?
    Public Property AlbumGain As Double?
    Public Property AlbumPeak As Double?
    Public Property MaxGlobalGain As Integer?
    Public Property MinGlobalGain As Integer?
    Public Property MaxNoClipGain As Integer?

    ' ── Undo Information ──
    Public Property HasUndo As Boolean
    Public Property UndoLeft As Integer
    Public Property UndoRight As Integer

    ' ── Legacy-style gain quantization ──
    ' mp3gain works in integer steps of ~1.505 dB (5 * log10(2)).
    ' The legacy GUI displayed gain quantized to these steps.
    Private Const DbPerStep As Double = 1.50514997831991 ' 5 * log10(2)

    ''' <summary>
    ''' Quantize a raw dB gain value to the nearest integer mp3gain step
    ''' and convert back to dB, matching legacy mp3gain GUI display.
    ''' </summary>
    Public Shared Function QuantizeGain(rawGain As Double) As Double
        Dim steps As Double = rawGain / DbPerStep
        Dim absSteps As Double = Math.Abs(steps)
        Dim intSteps As Integer
        If absSteps - CInt(Fix(absSteps)) < 0.5 Then
            intSteps = CInt(Fix(steps))
        Else
            intSteps = CInt(Fix(steps)) + If(steps < 0, -1, 1)
        End If
        Return Math.Round(intSteps * DbPerStep, 1)
    End Function

    ''' <summary>Track gain quantized to legacy mp3gain steps for display.</summary>
    Public ReadOnly Property TrackGainDisplay As Double?
        Get
            If Not TrackGain.HasValue Then Return Nothing
            Return QuantizeGain(TrackGain.Value)
        End Get
    End Property

    ''' <summary>Album gain quantized to legacy mp3gain steps for display.</summary>
    Public ReadOnly Property AlbumGainDisplay As Double?
        Get
            If Not AlbumGain.HasValue Then Return Nothing
            Dim raw As Double = TargetDb - 89.0 + AlbumGain.Value
            Return QuantizeGain(raw)
        End Get
    End Property

    ' ── Computed Properties ──
    Public Function GetGainChange(targetDb As Double) As Double?
        If Not TrackGain.HasValue Then Return Nothing
        Return targetDb - 89.0 + TrackGain.Value
    End Function

    ''' <summary>Gain change using the current TargetDb, for grid binding.</summary>
    Public ReadOnly Property GainChangeDisplay As Double?
        Get
            If Not TrackGain.HasValue Then Return Nothing
            Dim raw As Double = TargetDb - 89.0 + TrackGain.Value
            Return QuantizeGain(raw)
        End Get
    End Property

    Public ReadOnly Property MaxAmplitude As Double?
        Get
            If Not TrackPeak.HasValue Then Return Nothing
            Return TrackPeak.Value * 32768.0
        End Get
    End Property

    Public Function WouldClip(targetDb As Double) As Boolean?
        If Not TrackGain.HasValue OrElse Not TrackPeak.HasValue Then Return Nothing
        Dim change As Double = If(GetGainChange(targetDb), 0)
        Dim amplifiedPeak As Double = TrackPeak.Value * Math.Pow(10.0, change / 20.0)
        Return amplifiedPeak > 1.0
    End Function

    Public ReadOnly Property Volume As Double?
        Get
            If Not TrackGain.HasValue Then Return Nothing
            Return TargetDb - (TargetDb - 89.0 + TrackGain.Value)
        End Get
    End Property

    Public ReadOnly Property Clipping As String
        Get
            If Not TrackPeak.HasValue Then Return String.Empty
            Return If(TrackPeak.Value >= 1.0, "Y", "N")
        End Get
    End Property

    ' ── Tag Info ──
    Public Property HasExistingTags As Boolean

    ' ── Clear Analysis ──
    Public Sub ClearAnalysis()
        TrackGain = Nothing
        TrackPeak = Nothing
        AlbumGain = Nothing
        AlbumPeak = Nothing
        MaxGlobalGain = Nothing
        MinGlobalGain = Nothing
        MaxNoClipGain = Nothing
        Progress = -1
        Status = FileStatus.Pending
        StatusMessage = Nothing
        HasExistingTags = False
        HasUndo = False
    End Sub

    ' ── Status ──
    Public Property Status As FileStatus = FileStatus.Pending
    Public Property StatusMessage As String

    Public ReadOnly Property FileSizeFormatted As String
        Get
            If FileSize < 1024 Then Return $"{FileSize} B"
            If FileSize < 1024 * 1024 Then Return $"{FileSize / 1024.0:F1} KB"
            Return $"{FileSize / (1024.0 * 1024.0):F1} MB"
        End Get
    End Property

    Public Shared Function FromPath(path As String) As Mp3FileInfo
        Dim info As New FileInfo(path)
        Dim f As New Mp3FileInfo()
        f.FilePath = path
        f.FileSize = If(info.Exists, info.Length, 0)
        Return f
    End Function

End Class

Public Enum FileStatus
    Pending
    Analyzing
    Analyzed
    Applying
    Applied
    [Error]
    Skipped
End Enum
