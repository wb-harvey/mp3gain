using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace Mp3Gain2026;

/// <summary>
/// Log options dialog – mirrors the legacy MP3Gain "Logs…" dialog.
/// Three log files: Error, Analysis, and Change logs.
/// </summary>
public class LogOptionsForm : Form
{
    /* ── Theme (matches MainForm) ─────────────────────────── */
    private static readonly Color BgDark    = Color.FromArgb(24, 24, 32);
    private static readonly Color BgPanel   = Color.FromArgb(32, 34, 46);
    private static readonly Color TextPri   = Color.FromArgb(237, 237, 245);
    private static readonly Color Accent    = Color.FromArgb(99, 102, 241);

    private TextBox _txtErrorLog = null!;
    private TextBox _txtAnalysisLog = null!;
    private TextBox _txtChangeLog = null!;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ErrorLogPath { get; set; } = @"C:\ProgramData\Mp3Gain2026\error.log";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string AnalysisLogPath { get; set; } = @"C:\ProgramData\Mp3Gain2026\analysis.log";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ChangeLogPath { get; set; } = @"C:\ProgramData\Mp3Gain2026\change.log";

    public LogOptionsForm()
    {
        InitUI();
    }

    private void InitUI()
    {
        Text = "Log Options";
        Size = new Size(600, 280);
        MinimumSize = new Size(500, 250);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = BgDark;
        ForeColor = TextPri;
        Font = new Font("Segoe UI", 9.5f);
        ShowInTaskbar = false;

        int y = 20;
        AddLogRow("Error Log:", ref _txtErrorLog, ref y);
        AddLogRow("Analysis Log:", ref _txtAnalysisLog, ref y);
        AddLogRow("Change Log:", ref _txtChangeLog, ref y);

        y += 10;

        var btnOk = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Size = new Size(90, 32),
            Location = new Point(Width - 220, y),
            BackColor = Accent,
            ForeColor = TextPri,
            FlatStyle = FlatStyle.Flat
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (s, e) =>
        {
            ErrorLogPath = _txtErrorLog.Text.Trim();
            AnalysisLogPath = _txtAnalysisLog.Text.Trim();
            ChangeLogPath = _txtChangeLog.Text.Trim();
        };

        var btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(90, 32),
            Location = new Point(Width - 115, y),
            BackColor = BgPanel,
            ForeColor = TextPri,
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 80);

        Controls.Add(btnOk);
        Controls.Add(btnCancel);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _txtErrorLog.Text = ErrorLogPath;
        _txtAnalysisLog.Text = AnalysisLogPath;
        _txtChangeLog.Text = ChangeLogPath;
    }

    private void AddLogRow(string label, ref TextBox textBox, ref int y)
    {
        var lbl = new Label
        {
            Text = label,
            Location = new Point(12, y + 4),
            Size = new Size(100, 22),
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = TextPri
        };

        textBox = new TextBox
        {
            Location = new Point(120, y),
            Size = new Size(380, 24),
            BackColor = BgPanel,
            ForeColor = TextPri,
            BorderStyle = BorderStyle.FixedSingle
        };

        var captured = textBox; // capture for lambda
        var btnBrowse = new Button
        {
            Text = "...",
            Location = new Point(508, y - 1),
            Size = new Size(36, 26),
            BackColor = BgPanel,
            ForeColor = TextPri,
            FlatStyle = FlatStyle.Flat
        };
        btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 80);
        btnBrowse.Click += (s, e) =>
        {
            using var dlg = new SaveFileDialog
            {
                Title = $"Select {label.TrimEnd(':')} File",
                Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "log",
                FileName = Path.GetFileName(captured.Text),
                InitialDirectory = string.IsNullOrEmpty(captured.Text)
                    ? @"C:\ProgramData\Mp3Gain2026"
                    : Path.GetDirectoryName(captured.Text) ?? @"C:\ProgramData\Mp3Gain2026"
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
                captured.Text = dlg.FileName;
        };

        Controls.Add(lbl);
        Controls.Add(textBox);
        Controls.Add(btnBrowse);

        y += 42;
    }
}
