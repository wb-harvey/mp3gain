Imports System.Drawing
Imports System.Windows.Forms

Public Class ConstantGainForm
    Inherits Form

    Private _sliderGain As TrackBar
    Private _lblValue As Label
    Private _rbBoth As RadioButton
    Private _rbLeft As RadioButton
    Private _rbRight As RadioButton
    Private _btnOk As Button
    Private _btnCancel As Button

    Public ReadOnly Property GainSteps As Integer
        Get
            Return _sliderGain.Value
        End Get
    End Property

    ' 0 = Both, 1 = Left, 2 = Right
    Public ReadOnly Property ChannelMode As Integer
        Get
            If _rbBoth.Checked Then Return 0
            If _rbLeft.Checked Then Return 1
            Return 2
        End Get
    End Property

    Public Sub New()
        Text = "Constant Gain Change"
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        StartPosition = FormStartPosition.CenterParent
        ClientSize = New Size(320, 240)

        Dim lblSlider As New Label() With {
            .Text = "Change gain by (1.5 dB steps):",
            .Location = New Point(15, 15),
            .AutoSize = True
        }
        Controls.Add(lblSlider)

        _sliderGain = New TrackBar() With {
            .Location = New Point(15, 35),
            .Width = 280,
            .Minimum = -6,
            .Maximum = 6,
            .Value = 0,
            .TickFrequency = 1,
            .LargeChange = 2
        }
        AddHandler _sliderGain.ValueChanged, Sub(s, e) UpdateValueLabel()
        Controls.Add(_sliderGain)

        _lblValue = New Label() With {
            .Location = New Point(15, 85),
            .Width = 280,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = New Font(Font, FontStyle.Bold)
        }
        Controls.Add(_lblValue)

        Dim grpChannel As New GroupBox() With {
            .Text = "Channel Selection",
            .Location = New Point(15, 120),
            .Size = New Size(280, 60)
        }

        _rbBoth = New RadioButton() With {.Text = "Both", .Location = New Point(20, 25), .AutoSize = True, .Checked = True}
        _rbLeft = New RadioButton() With {.Text = "Left Only", .Location = New Point(100, 25), .AutoSize = True}
        _rbRight = New RadioButton() With {.Text = "Right Only", .Location = New Point(190, 25), .AutoSize = True}

        grpChannel.Controls.Add(_rbBoth)
        grpChannel.Controls.Add(_rbLeft)
        grpChannel.Controls.Add(_rbRight)
        Controls.Add(grpChannel)

        _btnOk = New Button() With {
            .Text = "&OK",
            .Location = New Point(135, 200),
            .Width = 75,
            .DialogResult = DialogResult.OK
        }
        Controls.Add(_btnOk)

        _btnCancel = New Button() With {
            .Text = "&Cancel",
            .Location = New Point(220, 200),
            .Width = 75,
            .DialogResult = DialogResult.Cancel
        }
        Controls.Add(_btnCancel)

        AcceptButton = _btnOk
        CancelButton = _btnCancel

        UpdateValueLabel()
    End Sub

    Private Sub UpdateValueLabel()
        Dim val As Integer = _sliderGain.Value
        Dim db As Double = val * 1.5
        _lblValue.Text = $"{If(val > 0, "+", "")}{val} steps ({If(db > 0, "+", "")}{db:F1} dB)"
    End Sub

End Class
