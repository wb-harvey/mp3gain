Imports System.IO
Imports System.Xml.Serialization

Public Class AppSettings

    Public Property AlwaysOnTop As Boolean = False
    Public Property AddSubfolders As Boolean = True
    Public Property PreserveTimestamp As Boolean = True
    Public Property NoLayerCheck As Boolean = False
    Public Property DontClip As Boolean = True
    Public Property IgnoreTags As Boolean = False
    Public Property RecalcTags As Boolean = False
    Public Property BeepWhenFinished As Boolean = False

    Public Property ErrorLogPath As String = "C:\ProgramData\Mp3Gain2026\error.log"
    Public Property AnalysisLogPath As String = "C:\ProgramData\Mp3Gain2026\analysis.log"
    Public Property ChangeLogPath As String = "C:\ProgramData\Mp3Gain2026\change.log"

    Public Property TargetVolume As Decimal = 89.0D

    ''' <summary>
    ''' 0 = Path/File, 1 = File Only, 2 = Path and File Separately
    ''' </summary>
    Public Property FilenameDisplayMode As Integer = 0

    Private Shared Function GetConfigPath() As String
        Dim appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        Dim dir = Path.Combine(appData, "Mp3Gain2026")
        If Not System.IO.Directory.Exists(dir) Then
            System.IO.Directory.CreateDirectory(dir)
        End If
        Return Path.Combine(dir, "settings.xml")
    End Function

    Public Shared Function Load() As AppSettings
        Dim path = GetConfigPath()
        If File.Exists(path) Then
            Try
                Dim serializer As New XmlSerializer(GetType(AppSettings))
                Using reader As New StreamReader(path)
                    Dim settings = TryCast(serializer.Deserialize(reader), AppSettings)
                    If settings IsNot Nothing Then Return settings
                End Using
            Catch
            End Try
        End If
        Return New AppSettings()
    End Function

    Public Sub Save()
        Try
            Dim serializer As New XmlSerializer(GetType(AppSettings))
            Using writer As New StreamWriter(GetConfigPath())
                serializer.Serialize(writer, Me)
            End Using
        Catch
        End Try
    End Sub

End Class
