using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;

namespace Mp3Gain2026;

public class AboutForm : Form
{
    private readonly string _engineVersion;

    public AboutForm(string engineVersion)
    {
        _engineVersion = engineVersion;
        InitializeUI();
    }

    private void InitializeUI()
    {
        this.Text = "About MP3Gain 2026";
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterParent;
        this.Size = new Size(480, 420);
        this.BackColor = Color.FromArgb(18, 18, 24);
        this.ForeColor = Color.FromArgb(237, 237, 245);
        this.Font = new Font("Segoe UI", 10f);
        this.ShowInTaskbar = false;

        var accentPrimary = Color.FromArgb(99, 102, 241);
        var accentSecondary = Color.FromArgb(139, 92, 246);
        var textSec = Color.FromArgb(156, 156, 178);
        var textMuted = Color.FromArgb(100, 100, 130);

        // Title
        var titleLabel = new Label
        {
            Text = "MP3Gain 2026",
            Font = new Font("Segoe UI", 22f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(30, 30)
        };

        // Subtitle with gradient feel
        var subtitleLabel = new Label
        {
            Text = "Modern mp3 Volume Normalization",
            Font = new Font("Segoe UI", 11f, FontStyle.Italic),
            ForeColor = accentPrimary,
            AutoSize = true,
            Location = new Point(32, 68)
        };

        // Version info
        var versionApp = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        var versionLabel = new Label
        {
            Text = $"Application v{versionApp}\nNative Engine v{_engineVersion}",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = textSec,
            AutoSize = true,
            Location = new Point(32, 105)
        };

        // Separator
        var sep = new Panel
        {
            Location = new Point(30, 155),
            Size = new Size(this.ClientSize.Width - 60, 2),
            BackColor = Color.FromArgb(55, 55, 75)
        };

        // Credits
        var creditsLabel = new Label
        {
            Text = "Based on the original MP3Gain by Glen Sawyer\n" +
                   "ReplayGain concept by David Robinson\n" +
                   "DLL wrapper by John Zitterkopf\n" +
                   "Modernized for .NET 10 by William Harvey — 2026",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = textSec,
            AutoSize = true,
            Location = new Point(32, 172)
        };

        // License
        var licenseLabel = new Label
        {
            Text = "Licensed under the GNU Lesser General Public License v2.1",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = textMuted,
            AutoSize = true,
            Location = new Point(32, 270)
        };

        // Link
        var linkLabel = new LinkLabel
        {
            Text = "https://williamharvey.com/mp3gain",
            Font = new Font("Segoe UI", 9f),
            LinkColor = accentPrimary,
            ActiveLinkColor = accentSecondary,
            AutoSize = true,
            Location = new Point(32, 298)
        };
        linkLabel.LinkClicked += (s, e) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://williamharvey.com/mp3gain",
                    UseShellExecute = true
                });
            }
            catch { }
        };

        // Source link
        var sourceLabel = new LinkLabel
        {
            Text = "https://github.com/wb-harvey/mp3gain",
            Font = new Font("Segoe UI", 9f),
            LinkColor = accentPrimary,
            ActiveLinkColor = accentSecondary,
            AutoSize = true,
            Location = new Point(32, 323)
        };
        sourceLabel.LinkClicked += (s, e) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/wb-harvey/mp3gain",
                    UseShellExecute = true
                });
            }
            catch { }
        };

        // OK button
        var okBtn = new Button
        {
            Text = "OK",
            Size = new Size(100, 36),
            Location = new Point(this.ClientSize.Width - 140, this.ClientSize.Height - 60),
            FlatStyle = FlatStyle.Flat,
            BackColor = accentPrimary,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10f),
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.OK
        };
        okBtn.FlatAppearance.BorderSize = 0;
        okBtn.FlatAppearance.MouseOverBackColor = accentSecondary;

        this.AcceptButton = okBtn;

        this.Controls.AddRange(new Control[]
        {
            titleLabel, subtitleLabel, versionLabel,
            sep, creditsLabel, licenseLabel, linkLabel, sourceLabel, okBtn
        });
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // Draw top accent line
        using var brush = new LinearGradientBrush(
            new Point(0, 0), new Point(this.Width, 0),
            Color.FromArgb(99, 102, 241), Color.FromArgb(139, 92, 246));
        e.Graphics.FillRectangle(brush, 0, 0, this.Width, 3);
    }
}
