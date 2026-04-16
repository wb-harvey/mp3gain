using System;
using System.Drawing;
using System.Windows.Forms;

namespace Mp3Gain2026;

public class ConstantGainForm : Form
{
    private TrackBar _sliderGain;
    private Label _lblValue;
    private RadioButton _rbBoth;
    private RadioButton _rbLeft;
    private RadioButton _rbRight;
    private Button _btnOk;
    private Button _btnCancel;

    public int GainSteps => _sliderGain.Value;

    // 0 = Both, 1 = Left, 2 = Right
    public int ChannelMode => _rbBoth.Checked ? 0 : (_rbLeft.Checked ? 1 : 2);

    public ConstantGainForm()
    {
        Text = "Constant Gain Change";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(320, 240);

        var lblSlider = new Label
        {
            Text = "Change gain by (1.5 dB steps):",
            Location = new Point(15, 15),
            AutoSize = true
        };
        Controls.Add(lblSlider);

        _sliderGain = new TrackBar
        {
            Location = new Point(15, 35),
            Width = 280,
            Minimum = -6,
            Maximum = 6,
            Value = 0,
            TickFrequency = 1,
            LargeChange = 2
        };
        _sliderGain.ValueChanged += (s, e) => UpdateValueLabel();
        Controls.Add(_sliderGain);

        _lblValue = new Label
        {
            Location = new Point(15, 85),
            Width = 280,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font, FontStyle.Bold)
        };
        Controls.Add(_lblValue);

        var grpChannel = new GroupBox
        {
            Text = "Channel Selection",
            Location = new Point(15, 120),
            Size = new Size(280, 60)
        };
        
        _rbBoth = new RadioButton { Text = "Both", Location = new Point(20, 25), AutoSize = true, Checked = true };
        _rbLeft = new RadioButton { Text = "Left Only", Location = new Point(100, 25), AutoSize = true };
        _rbRight = new RadioButton { Text = "Right Only", Location = new Point(190, 25), AutoSize = true };
        
        grpChannel.Controls.Add(_rbBoth);
        grpChannel.Controls.Add(_rbLeft);
        grpChannel.Controls.Add(_rbRight);
        Controls.Add(grpChannel);

        _btnOk = new Button
        {
            Text = "&OK",
            Location = new Point(135, 200),
            Width = 75,
            DialogResult = DialogResult.OK
        };
        Controls.Add(_btnOk);

        _btnCancel = new Button
        {
            Text = "&Cancel",
            Location = new Point(220, 200),
            Width = 75,
            DialogResult = DialogResult.Cancel
        };
        Controls.Add(_btnCancel);

        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        UpdateValueLabel();
    }

    private void UpdateValueLabel()
    {
        int val = _sliderGain.Value;
        double db = val * 1.5;
        _lblValue.Text = $"{(val > 0 ? "+" : "")}{val} steps ({(db > 0 ? "+" : "")}{db:F1} dB)";
    }
}
