Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Reflection
Imports System.Windows.Forms

Public Class AboutForm
    Inherits Form

    Private ReadOnly _engineVersion As String

    Public Sub New(engineVersion As String)
        _engineVersion = engineVersion
        InitializeUI()
    End Sub

    Private Sub InitializeUI()
        Me.Text = "About MP3Gain 2026"
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Size = New Size(480, 420)
        Me.BackColor = Color.FromArgb(18, 18, 24)
        Me.ForeColor = Color.FromArgb(237, 237, 245)
        Me.Font = New Font("Segoe UI", 10.0F)
        Me.ShowInTaskbar = False

        Dim accentPrimary = Color.FromArgb(99, 102, 241)
        Dim accentSecondary = Color.FromArgb(139, 92, 246)
        Dim textSec = Color.FromArgb(156, 156, 178)
        Dim textMuted = Color.FromArgb(100, 100, 130)

        ' Title
        Dim titleLabel As New Label() With {
            .Text = "MP3Gain 2026",
            .Font = New Font("Segoe UI", 22.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(30, 30)
        }

        ' Subtitle
        Dim subtitleLabel As New Label() With {
            .Text = "Modern mp3 Volume Normalization",
            .Font = New Font("Segoe UI", 11.0F, FontStyle.Italic),
            .ForeColor = accentPrimary,
            .AutoSize = True,
            .Location = New Point(32, 68)
        }

        ' Version info
        Dim versionApp = Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        If versionApp Is Nothing Then versionApp = "1.0.0"
        Dim versionLabel As New Label() With {
            .Text = $"Application v{versionApp}" & vbCrLf & $"Native Engine v{_engineVersion}",
            .Font = New Font("Segoe UI", 9.5F),
            .ForeColor = textSec,
            .AutoSize = True,
            .Location = New Point(32, 105)
        }

        ' Separator
        Dim sep As New Panel() With {
            .Location = New Point(30, 155),
            .Size = New Size(Me.ClientSize.Width - 60, 2),
            .BackColor = Color.FromArgb(55, 55, 75)
        }

        ' Credits
        Dim creditsLabel As New Label() With {
            .Text = "Based on the original MP3Gain by Glen Sawyer" & vbCrLf &
                    "ReplayGain concept by David Robinson" & vbCrLf &
                    "DLL wrapper by John Zitterkopf" & vbCrLf &
                    "Modernized for .NET 10 by William Harvey — 2026",
            .Font = New Font("Segoe UI", 9.5F),
            .ForeColor = textSec,
            .AutoSize = True,
            .Location = New Point(32, 172)
        }

        ' License
        Dim licenseLabel As New Label() With {
            .Text = "Licensed under the GNU Lesser General Public License v2.1",
            .Font = New Font("Segoe UI", 8.5F),
            .ForeColor = textMuted,
            .AutoSize = True,
            .Location = New Point(32, 270)
        }

        ' Link
        Dim linkLabel As New LinkLabel() With {
            .Text = "https://williamharvey.com/mp3gain",
            .Font = New Font("Segoe UI", 9.0F),
            .LinkColor = accentPrimary,
            .ActiveLinkColor = accentSecondary,
            .AutoSize = True,
            .Location = New Point(32, 298)
        }
        AddHandler linkLabel.LinkClicked, Sub(s, ev)
                                              Try
                                                  Process.Start(New ProcessStartInfo() With {
                                                      .FileName = "https://williamharvey.com/mp3gain",
                                                      .UseShellExecute = True
                                                  })
                                              Catch
                                              End Try
                                          End Sub

        ' Source link
        Dim sourceLabel As New LinkLabel() With {
            .Text = "https://github.com/wb-harvey/mp3gain",
            .Font = New Font("Segoe UI", 9.0F),
            .LinkColor = accentPrimary,
            .ActiveLinkColor = accentSecondary,
            .AutoSize = True,
            .Location = New Point(32, 323)
        }
        AddHandler sourceLabel.LinkClicked, Sub(s, ev)
                                                Try
                                                    Process.Start(New ProcessStartInfo() With {
                                                        .FileName = "https://github.com/wb-harvey/mp3gain",
                                                        .UseShellExecute = True
                                                    })
                                                Catch
                                                End Try
                                            End Sub

        ' OK button
        Dim okBtn As New Button() With {
            .Text = "OK",
            .Size = New Size(100, 36),
            .Location = New Point(Me.ClientSize.Width - 140, Me.ClientSize.Height - 60),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = accentPrimary,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI Semibold", 10.0F),
            .Cursor = Cursors.Hand,
            .DialogResult = DialogResult.OK
        }
        okBtn.FlatAppearance.BorderSize = 0
        okBtn.FlatAppearance.MouseOverBackColor = accentSecondary

        Me.AcceptButton = okBtn

        Me.Controls.AddRange(New Control() {
            titleLabel, subtitleLabel, versionLabel,
            sep, creditsLabel, licenseLabel, linkLabel, sourceLabel, okBtn
        })
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        ' Draw top accent line
        Using brush As New LinearGradientBrush(
            New Point(0, 0), New Point(Me.Width, 0),
            Color.FromArgb(99, 102, 241), Color.FromArgb(139, 92, 246))
            e.Graphics.FillRectangle(brush, 0, 0, Me.Width, 3)
        End Using
    End Sub

    Private Sub InitializeComponent()
        Me.SuspendLayout()
        '
        'AboutForm
        '
        Me.ClientSize = New System.Drawing.Size(284, 261)
        Me.Name = "AboutForm"
        Me.ResumeLayout(False)

    End Sub

    Private Sub AboutForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub
End Class
