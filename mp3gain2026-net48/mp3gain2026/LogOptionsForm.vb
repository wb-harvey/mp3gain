Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' Log options dialog – mirrors the legacy MP3Gain "Logs…" dialog.
''' Three log files: Error, Analysis, and Change logs.
''' </summary>
Public Class LogOptionsForm
    Inherits Form

    ' ── Theme (matches MainForm) ──
    Private Shared ReadOnly BgDark As Color = Color.FromArgb(24, 24, 32)
    Private Shared ReadOnly BgPanel As Color = Color.FromArgb(32, 34, 46)
    Private Shared ReadOnly TextPri As Color = Color.FromArgb(237, 237, 245)
    Private Shared ReadOnly Accent As Color = Color.FromArgb(99, 102, 241)

    Private _txtErrorLog As TextBox
    Private _txtAnalysisLog As TextBox
    Private _txtChangeLog As TextBox

    Public Property ErrorLogPath As String = "C:\ProgramData\Mp3Gain2026\error.log"
    Public Property AnalysisLogPath As String = "C:\ProgramData\Mp3Gain2026\analysis.log"
    Public Property ChangeLogPath As String = "C:\ProgramData\Mp3Gain2026\change.log"

    Public Sub New()
        InitUI()
    End Sub

    Private Sub InitUI()
        Text = "Log Options"
        Size = New Size(600, 280)
        MinimumSize = New Size(500, 250)
        StartPosition = FormStartPosition.CenterParent
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        BackColor = BgDark
        ForeColor = TextPri
        Font = New Font("Segoe UI", 9.5F)
        ShowInTaskbar = False

        Dim y As Integer = 20
        AddLogRow("Error Log:", _txtErrorLog, y)
        AddLogRow("Analysis Log:", _txtAnalysisLog, y)
        AddLogRow("Change Log:", _txtChangeLog, y)

        y += 10

        Dim btnOk As New Button() With {
            .Text = "OK",
            .DialogResult = DialogResult.OK,
            .Size = New Size(90, 32),
            .Location = New Point(Width - 220, y),
            .BackColor = Accent,
            .ForeColor = TextPri,
            .FlatStyle = FlatStyle.Flat
        }
        btnOk.FlatAppearance.BorderSize = 0
        AddHandler btnOk.Click, Sub(s, e)
                                    ErrorLogPath = _txtErrorLog.Text.Trim()
                                    AnalysisLogPath = _txtAnalysisLog.Text.Trim()
                                    ChangeLogPath = _txtChangeLog.Text.Trim()
                                End Sub

        Dim btnCancel As New Button() With {
            .Text = "Cancel",
            .DialogResult = DialogResult.Cancel,
            .Size = New Size(90, 32),
            .Location = New Point(Width - 115, y),
            .BackColor = BgPanel,
            .ForeColor = TextPri,
            .FlatStyle = FlatStyle.Flat
        }
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 80)

        Controls.Add(btnOk)
        Controls.Add(btnCancel)

        AcceptButton = btnOk
        CancelButton = btnCancel
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        _txtErrorLog.Text = ErrorLogPath
        _txtAnalysisLog.Text = AnalysisLogPath
        _txtChangeLog.Text = ChangeLogPath
    End Sub

    Private Sub AddLogRow(label As String, ByRef textBox As TextBox, ByRef y As Integer)
        Dim lbl As New Label() With {
            .Text = label,
            .Location = New Point(12, y + 4),
            .Size = New Size(100, 22),
            .TextAlign = ContentAlignment.MiddleRight,
            .ForeColor = TextPri
        }

        textBox = New TextBox() With {
            .Location = New Point(120, y),
            .Size = New Size(380, 24),
            .BackColor = BgPanel,
            .ForeColor = TextPri,
            .BorderStyle = BorderStyle.FixedSingle
        }

        Dim captured = textBox ' capture for lambda
        Dim btnBrowse As New Button() With {
            .Text = "...",
            .Location = New Point(508, y - 1),
            .Size = New Size(36, 26),
            .BackColor = BgPanel,
            .ForeColor = TextPri,
            .FlatStyle = FlatStyle.Flat
        }
        btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 80)
        AddHandler btnBrowse.Click, Sub(s, ev)
                                        Using dlg As New SaveFileDialog() With {
                                            .Title = $"Select {label.TrimEnd(":"c)} File",
                                            .Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                                            .DefaultExt = "log",
                                            .FileName = Path.GetFileName(captured.Text),
                                            .InitialDirectory = If(String.IsNullOrEmpty(captured.Text),
                                                "C:\ProgramData\Mp3Gain2026",
                                                If(Path.GetDirectoryName(captured.Text), "C:\ProgramData\Mp3Gain2026"))
                                        }
                                            If dlg.ShowDialog(Me) = DialogResult.OK Then
                                                captured.Text = dlg.FileName
                                            End If
                                        End Using
                                    End Sub

        Controls.Add(lbl)
        Controls.Add(textBox)
        Controls.Add(btnBrowse)

        y += 42
    End Sub

End Class
