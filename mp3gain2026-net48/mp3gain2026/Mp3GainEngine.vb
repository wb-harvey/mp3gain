Imports System.Text
Imports System.Threading

''' <summary>
''' High-level managed wrapper around the mp3gain2026 native DLL.
''' Provides async methods and type-safe results.
''' </summary>
Public NotInheritable Class Mp3GainEngine
    Implements IDisposable

    Private ReadOnly _semaphore As New SemaphoreSlim(1, 1)
    Private _disposed As Boolean
    Private _initialized As Boolean

    Public Event ProgressChanged As Action(Of Integer)
    Private Shared _progressDelegate As NativeMethods.MP3G_ProgressCallback

    Public Const DefaultTargetDb As Double = 89.0

    Public Sub New()
        Dim result As Integer = NativeMethods.MP3G_Init()
        If result <> NativeMethods.MP3G_OK Then
            Throw New InvalidOperationException("Failed to initialize mp3gain2026 native library.")
        End If
        _initialized = True

        If _progressDelegate Is Nothing Then
            _progressDelegate = New NativeMethods.MP3G_ProgressCallback(AddressOf OnNativeProgress)
            NativeMethods.MP3G_SetProgressCallback(_progressDelegate, IntPtr.Zero)
        End If
    End Sub

    Private Sub OnNativeProgress(percent As Integer, userData As IntPtr)
        RaiseEvent ProgressChanged(percent)
    End Sub

    Public Function GetVersion() As String
        Dim sb As New StringBuilder(64)
        NativeMethods.MP3G_GetVersion(sb, sb.Capacity)
        Return sb.ToString()
    End Function

    ''' <summary>
    ''' Analyzes a single MP3 file for its ReplayGain values.
    ''' </summary>
    Public Async Function AnalyzeFileAsync(filePath As String, Optional ignoreTags As Boolean = False, Optional recalcTags As Boolean = False, Optional ct As CancellationToken = Nothing) As Task(Of AnalysisResult)
        ThrowIfDisposed()
        If String.IsNullOrWhiteSpace(filePath) Then Throw New ArgumentException("File path cannot be null or empty.", NameOf(filePath))

        Return Await Task.Run(
            Async Function()
                Await _semaphore.WaitAsync(ct)
                Try
                    ct.ThrowIfCancellationRequested()

                    Dim native As New NativeMethods.MP3G_AnalysisResult()
                    Dim ret As Integer = NativeMethods.MP3G_AnalyzeFile(filePath, native, If(ignoreTags, 1, 0), If(recalcTags, 1, 0))

                    If ret <> NativeMethods.MP3G_OK Then
                        Dim err As String = GetLastErrorMessage()
                        Throw New Mp3GainException(ret, err)
                    End If

                    Dim tags As New NativeMethods.MP3G_TagInfo()
                    NativeMethods.MP3G_ReadTags(filePath, tags)

                    Dim ar As New AnalysisResult()
                    ar.TrackGain = native.TrackGain
                    ar.TrackPeak = native.TrackPeak
                    ar.AlbumGain = native.AlbumGain
                    ar.AlbumPeak = native.AlbumPeak
                    ar.MaxGlobalGain = native.MaxGlobalGain
                    ar.MinGlobalGain = native.MinGlobalGain
                    ar.MaxNoClipGain = native.MaxNoClipGain
                    ar.HasUndo = (tags.HaveUndo <> 0)
                    ar.UndoLeft = tags.UndoLeft
                    ar.UndoRight = tags.UndoRight
                    Return ar
                Finally
                    _semaphore.Release()
                End Try
            End Function, ct)
    End Function

    ''' <summary>
    ''' Reads existing ReplayGain tags from an MP3 file.
    ''' </summary>
    Public Async Function ReadTagsAsync(filePath As String, Optional ct As CancellationToken = Nothing) As Task(Of TagInfo)
        ThrowIfDisposed()
        If String.IsNullOrWhiteSpace(filePath) Then Throw New ArgumentException("File path cannot be null or empty.", NameOf(filePath))

        Return Await Task.Run(
            Async Function()
                Await _semaphore.WaitAsync(ct)
                Try
                    ct.ThrowIfCancellationRequested()

                    Dim native As New NativeMethods.MP3G_TagInfo()
                    Dim ret As Integer = NativeMethods.MP3G_ReadTags(filePath, native)

                    If ret <> NativeMethods.MP3G_OK Then
                        Dim err As String = GetLastErrorMessage()
                        Throw New Mp3GainException(ret, err)
                    End If

                    Dim ti As New TagInfo()
                    ti.HasTrackGain = (native.HaveTrackGain <> 0)
                    ti.HasTrackPeak = (native.HaveTrackPeak <> 0)
                    ti.HasAlbumGain = (native.HaveAlbumGain <> 0)
                    ti.HasAlbumPeak = (native.HaveAlbumPeak <> 0)
                    ti.HasUndo = (native.HaveUndo <> 0)
                    ti.TrackGain = native.TrackGain
                    ti.TrackPeak = native.TrackPeak
                    ti.AlbumGain = native.AlbumGain
                    ti.AlbumPeak = native.AlbumPeak
                    ti.UndoLeft = native.UndoLeft
                    ti.UndoRight = native.UndoRight
                    ti.MinGain = native.MinGain
                    ti.MaxGain = native.MaxGain
                    Return ti
                Finally
                    _semaphore.Release()
                End Try
            End Function, ct)
    End Function

    ''' <summary>
    ''' Applies a raw gain change (in mp3gain integer steps) to a file.
    ''' </summary>
    Public Async Function ApplyGainAsync(filePath As String, gainChange As Integer, Optional preserveTimestamp As Boolean = True, Optional ct As CancellationToken = Nothing) As Task
        ThrowIfDisposed()
        If String.IsNullOrWhiteSpace(filePath) Then Throw New ArgumentException("File path cannot be null or empty.", NameOf(filePath))

        Await Task.Run(
            Async Function() As Task
                Await _semaphore.WaitAsync(ct)
                Try
                    ct.ThrowIfCancellationRequested()
                    Dim ret As Integer = NativeMethods.MP3G_ApplyGain(filePath, gainChange, If(preserveTimestamp, 1, 0))
                    If ret <> NativeMethods.MP3G_OK Then Throw New Mp3GainException(ret, GetLastErrorMessage())
                Finally
                    _semaphore.Release()
                End Try
            End Function, ct)
    End Function

    ''' <summary>
    ''' Normalizes a file to the specified target dB level.
    ''' </summary>
    Public Async Function ApplyTrackGainAsync(filePath As String, Optional targetDb As Double = DefaultTargetDb, Optional preserveTimestamp As Boolean = True, Optional ct As CancellationToken = Nothing) As Task
        ThrowIfDisposed()
        If String.IsNullOrWhiteSpace(filePath) Then Throw New ArgumentException("File path cannot be null or empty.", NameOf(filePath))

        Await Task.Run(
            Async Function() As Task
                Await _semaphore.WaitAsync(ct)
                Try
                    ct.ThrowIfCancellationRequested()
                    Dim ret As Integer = NativeMethods.MP3G_ApplyTrackGain(filePath, targetDb, If(preserveTimestamp, 1, 0))
                    If ret <> NativeMethods.MP3G_OK Then Throw New Mp3GainException(ret, GetLastErrorMessage())
                Finally
                    _semaphore.Release()
                End Try
            End Function, ct)
    End Function

    ''' <summary>
    ''' Undoes a previous gain change using stored tag data.
    ''' </summary>
    Public Async Function UndoGainAsync(filePath As String, Optional preserveTimestamp As Boolean = True, Optional ct As CancellationToken = Nothing) As Task
        ThrowIfDisposed()
        If String.IsNullOrWhiteSpace(filePath) Then Throw New ArgumentException("File path cannot be null or empty.", NameOf(filePath))

        Await Task.Run(
            Async Function() As Task
                Await _semaphore.WaitAsync(ct)
                Try
                    ct.ThrowIfCancellationRequested()
                    Dim ret As Integer = NativeMethods.MP3G_UndoGain(filePath, If(preserveTimestamp, 1, 0))
                    If ret <> NativeMethods.MP3G_OK Then Throw New Mp3GainException(ret, GetLastErrorMessage())
                Finally
                    _semaphore.Release()
                End Try
            End Function, ct)
    End Function

    ''' <summary>
    ''' Removes all ReplayGain tags from a file.
    ''' </summary>
    Public Async Function RemoveTagsAsync(filePath As String, Optional preserveTimestamp As Boolean = True, Optional ct As CancellationToken = Nothing) As Task
        ThrowIfDisposed()
        If String.IsNullOrWhiteSpace(filePath) Then Throw New ArgumentException("File path cannot be null or empty.", NameOf(filePath))

        Await Task.Run(
            Async Function() As Task
                Await _semaphore.WaitAsync(ct)
                Try
                    ct.ThrowIfCancellationRequested()
                    Dim ret As Integer = NativeMethods.MP3G_RemoveTags(filePath, If(preserveTimestamp, 1, 0))
                    If ret <> NativeMethods.MP3G_OK Then Throw New Mp3GainException(ret, GetLastErrorMessage())
                Finally
                    _semaphore.Release()
                End Try
            End Function, ct)
    End Function

    ''' <summary>
    ''' Writes gain information and undo data to the APE tag.
    ''' </summary>
    Public Async Function WriteTagsAsync(filePath As String, fileInfo As Mp3FileInfo, Optional targetDb As Double = DefaultTargetDb, Optional preserveTimestamp As Boolean = True, Optional ct As CancellationToken = Nothing) As Task
        ThrowIfDisposed()
        If String.IsNullOrWhiteSpace(filePath) Then Throw New ArgumentException("File path cannot be null or empty.", NameOf(filePath))

        Await Task.Run(
            Async Function() As Task
                Await _semaphore.WaitAsync(ct)
                Try
                    ct.ThrowIfCancellationRequested()
                    Dim result As New NativeMethods.MP3G_AnalysisResult()
                    result.TrackGain = If(fileInfo.TrackGain, 0)
                    result.TrackPeak = If(fileInfo.TrackPeak, 0)
                    result.AlbumGain = If(fileInfo.AlbumGain, 0)
                    result.AlbumPeak = If(fileInfo.AlbumPeak, 0)
                    result.MinGlobalGain = If(fileInfo.MinGlobalGain, 0)
                    result.MaxGlobalGain = If(fileInfo.MaxGlobalGain, 0)
                    result.MaxNoClipGain = If(fileInfo.MaxNoClipGain, 0)

                    Dim haveUndo As Integer = If(fileInfo.HasUndo, 1, 0)
                    Dim undoLeft As Integer = fileInfo.UndoLeft
                    Dim undoRight As Integer = fileInfo.UndoRight

                    Dim ret As Integer = NativeMethods.MP3G_WriteTags(filePath, result, targetDb, haveUndo, undoLeft, undoRight, If(preserveTimestamp, 1, 0))
                    If ret <> NativeMethods.MP3G_OK Then Throw New Mp3GainException(ret, GetLastErrorMessage())
                Finally
                    _semaphore.Release()
                End Try
            End Function, ct)
    End Function

    Private Function GetLastErrorMessage() As String
        Dim sb As New StringBuilder(2048)
        NativeMethods.MP3G_GetLastError(sb, sb.Capacity)
        Dim msg As String = sb.ToString()
        Return If(String.IsNullOrEmpty(msg), "Unknown native error", msg)
    End Function

    Private Sub ThrowIfDisposed()
        If _disposed Then Throw New ObjectDisposedException(NameOf(Mp3GainEngine))
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        If _disposed Then Return
        _disposed = True
        If _initialized Then
            NativeMethods.MP3G_Shutdown()
            _initialized = False
        End If
        _semaphore.Dispose()
    End Sub

End Class

''' <summary>
''' Result of analyzing a single MP3 file.
''' </summary>
Public Class AnalysisResult
    Public Property TrackGain As Double
    Public Property TrackPeak As Double
    Public Property AlbumGain As Double
    Public Property AlbumPeak As Double
    Public Property MaxGlobalGain As Integer
    Public Property MinGlobalGain As Integer
    Public Property MaxNoClipGain As Integer
    Public Property HasUndo As Boolean
    Public Property UndoLeft As Integer
    Public Property UndoRight As Integer

    ''' <summary>
    ''' Gets the dB change needed to reach the target volume (89.0 dB default).
    ''' </summary>
    Public Function GetRequiredChange(Optional targetDb As Double = 89.0) As Double
        Return targetDb - 89.0 + TrackGain
    End Function

    ''' <summary>
    ''' Returns true if applying gain to the target would clip.
    ''' </summary>
    Public Function WouldClip(Optional targetDb As Double = 89.0) As Boolean
        Dim change As Double = GetRequiredChange(targetDb)
        Return Math.Abs(change) > MaxNoClipGain * 1.5
    End Function
End Class

''' <summary>
''' Existing ReplayGain tag data read from a file.
''' </summary>
Public Class TagInfo
    Public Property HasTrackGain As Boolean
    Public Property HasTrackPeak As Boolean
    Public Property HasAlbumGain As Boolean
    Public Property HasAlbumPeak As Boolean
    Public Property HasUndo As Boolean
    Public Property TrackGain As Double
    Public Property TrackPeak As Double
    Public Property AlbumGain As Double
    Public Property AlbumPeak As Double
    Public Property UndoLeft As Integer
    Public Property UndoRight As Integer
    Public Property MinGain As Integer
    Public Property MaxGain As Integer
End Class

''' <summary>
''' Exception thrown when a native mp3gain2026 operation fails.
''' </summary>
Public Class Mp3GainException
    Inherits Exception

    Public ReadOnly Property NativeErrorCode As Integer

    Public Sub New(errorCode As Integer, message As String)
        MyBase.New($"MP3Gain error {errorCode}: {message}")
        NativeErrorCode = errorCode
    End Sub
End Class
