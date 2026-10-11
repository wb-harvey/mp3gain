Imports System.Runtime.InteropServices
Imports System.Text

''' <summary>
''' P/Invoke declarations for the mp3gain2026 native DLL.
''' File paths go to the ...W exports as UTF-16, so files with any name (Hebrew, emoji...) work;
''' the ANSI exports could only open names in the system code page.
''' </summary>
Friend NotInheritable Class NativeMethods

    Private Const DllName As String = "mp3gain2026"

    ' ── Result Codes ──
    Public Const MP3G_OK As Integer = 0
    Public Const MP3G_ERR_GENERIC As Integer = -1
    Public Const MP3G_ERR_FILEOPEN As Integer = -2
    Public Const MP3G_ERR_FILEREAD As Integer = -3
    Public Const MP3G_ERR_FILEWRITE As Integer = -4
    Public Const MP3G_ERR_NOTMP3 As Integer = -5
    Public Const MP3G_ERR_CANCELLED As Integer = -6
    Public Const MP3G_ERR_INVALIDARG As Integer = -7

    ' ── Analysis Result Structure ──
    <StructLayout(LayoutKind.Sequential)>
    Public Structure MP3G_AnalysisResult
        Public TrackGain As Double
        Public TrackPeak As Double
        Public AlbumGain As Double
        Public AlbumPeak As Double
        Public MaxGlobalGain As Integer
        Public MinGlobalGain As Integer
        Public MaxNoClipGain As Integer
        Public AlbumMaxNoClipGain As Integer
    End Structure

    ' ── Tag Info Structure ──
    <StructLayout(LayoutKind.Sequential)>
    Public Structure MP3G_TagInfo
        Public HaveTrackGain As Integer
        Public HaveTrackPeak As Integer
        Public HaveAlbumGain As Integer
        Public HaveAlbumPeak As Integer
        Public HaveUndo As Integer
        Public TrackGain As Double
        Public TrackPeak As Double
        Public AlbumGain As Double
        Public AlbumPeak As Double
        Public UndoLeft As Integer
        Public UndoRight As Integer
        Public UndoWrap As Integer
        Public MinGain As Integer
        Public MaxGain As Integer
    End Structure

    ' ── Library Lifecycle ──
    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function MP3G_Init() As Integer
    End Function

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Sub MP3G_Shutdown()
    End Sub

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function MP3G_GetVersion(
        <MarshalAs(UnmanagedType.LPStr)> buffer As StringBuilder,
        bufLen As Integer) As Integer
    End Function

    ' ── Single File Analysis ──
    <DllImport(DllName, EntryPoint:="MP3G_AnalyzeFileW", CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Unicode)>
    Public Shared Function MP3G_AnalyzeFile(
        <MarshalAs(UnmanagedType.LPWStr)> filePath As String,
        ByRef result As MP3G_AnalysisResult,
        ignoreTags As Integer,
        recalcTags As Integer) As Integer
    End Function

    ' ── Album Analysis ──
    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function MP3G_AlbumInit() As Integer
    End Function

    <DllImport(DllName, EntryPoint:="MP3G_AlbumAnalyzeFileW", CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Unicode)>
    Public Shared Function MP3G_AlbumAnalyzeFile(
        <MarshalAs(UnmanagedType.LPWStr)> filePath As String,
        ByRef result As MP3G_AnalysisResult,
        ignoreTags As Integer,
        recalcTags As Integer) As Integer
    End Function

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function MP3G_AlbumGetResult(
        ByRef albumGain As Double,
        ByRef albumPeak As Double) As Integer
    End Function

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Sub MP3G_AlbumFinish()
    End Sub

    ' ── Gain Modification ──
    <DllImport(DllName, EntryPoint:="MP3G_ApplyGainW", CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Unicode)>
    Public Shared Function MP3G_ApplyGain(
        <MarshalAs(UnmanagedType.LPWStr)> filePath As String,
        gainChange As Integer,
        preserveTimestamp As Integer) As Integer
    End Function

    <DllImport(DllName, EntryPoint:="MP3G_ApplyTrackGainW", CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Unicode)>
    Public Shared Function MP3G_ApplyTrackGain(
        <MarshalAs(UnmanagedType.LPWStr)> filePath As String,
        targetDb As Double,
        preserveTimestamp As Integer) As Integer
    End Function

    <DllImport(DllName, EntryPoint:="MP3G_UndoGainW", CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Unicode)>
    Public Shared Function MP3G_UndoGain(
        <MarshalAs(UnmanagedType.LPWStr)> filePath As String,
        preserveTimestamp As Integer) As Integer
    End Function

    ' ── Tag Operations ──
    <DllImport(DllName, EntryPoint:="MP3G_ReadTagsW", CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Unicode)>
    Public Shared Function MP3G_ReadTags(
        <MarshalAs(UnmanagedType.LPWStr)> filePath As String,
        ByRef tagInfo As MP3G_TagInfo) As Integer
    End Function

    <DllImport(DllName, EntryPoint:="MP3G_WriteTagsW", CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Unicode)>
    Public Shared Function MP3G_WriteTags(
        <MarshalAs(UnmanagedType.LPWStr)> filePath As String,
        ByRef result As MP3G_AnalysisResult,
        targetDb As Double,
        haveUndo As Integer,
        undoLeft As Integer,
        undoRight As Integer,
        preserveTimestamp As Integer) As Integer
    End Function

    <DllImport(DllName, EntryPoint:="MP3G_RemoveTagsW", CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Unicode)>
    Public Shared Function MP3G_RemoveTags(
        <MarshalAs(UnmanagedType.LPWStr)> filePath As String,
        preserveTimestamp As Integer) As Integer
    End Function

    ' ── Error Reporting ──
    <DllImport(DllName, EntryPoint:="MP3G_GetLastErrorW", CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Unicode)>
    Public Shared Function MP3G_GetLastError(
        <MarshalAs(UnmanagedType.LPWStr)> buffer As StringBuilder,
        bufLen As Integer) As Integer
    End Function

    ' ── Progress ──
    <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
    Public Delegate Sub MP3G_ProgressCallback(percent As Integer, userData As IntPtr)

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Sub MP3G_SetProgressCallback(
        callback As MP3G_ProgressCallback,
        userData As IntPtr)
    End Sub

End Class
