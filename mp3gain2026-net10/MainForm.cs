using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mp3Gain2026;

public partial class MainForm : Form
{
    /* ── Theme Colors ─────────────────────────────────────────── */
    private static readonly Color BgDark         = Color.FromArgb(18, 18, 24);
    private static readonly Color BgPanel        = Color.FromArgb(26, 26, 36);
    private static readonly Color BgCard         = Color.FromArgb(32, 33, 46);
    private static readonly Color BgCardHover    = Color.FromArgb(42, 43, 58);
    private static readonly Color BgToolbar      = Color.FromArgb(22, 22, 32);
    private static readonly Color BorderColor    = Color.FromArgb(55, 55, 75);
    private static readonly Color AccentPrimary  = Color.FromArgb(99, 102, 241);  // Indigo
    private static readonly Color AccentSecondary= Color.FromArgb(139, 92, 246);  // Violet
    private static readonly Color AccentSuccess  = Color.FromArgb(34, 197, 94);   // Green
    private static readonly Color AccentWarning  = Color.FromArgb(251, 191, 36);  // Amber
    private static readonly Color AccentDanger   = Color.FromArgb(239, 68, 68);   // Red
    private static readonly Color TextPrimary    = Color.FromArgb(237, 237, 245);
    private static readonly Color TextSecondary  = Color.FromArgb(156, 156, 178);
    private static readonly Color TextMuted      = Color.FromArgb(100, 100, 130);

    /* ── Controls ─────────────────────────────────────────────── */
    private MenuStrip _menuStrip = null!;
    private ToolStrip _toolbar = null!;
    private DataGridView _grid = null!;
    private StatusStrip _statusBar = null!;
    private ToolStripStatusLabel _statusLabel = null!;
    private ToolStripStatusLabel _fileProgressLabel = null!;
    private ToolStripProgressBar _fileProgressBar = null!;
    private ToolStripStatusLabel _totalProgressLabel = null!;
    private ToolStripProgressBar _totalProgressBar = null!;
    private ToolStripStatusLabel _fileCountLabel = null!;
    private NumericUpDown _targetDbSpinner = null!;
    private Label _targetDbLabel = null!;
    private Panel _topPanel = null!;
    private Panel _targetPanel = null!;

    /* ── State ────────────────────────────────────────────────── */
    private readonly BindingList<Mp3FileInfo> _files = new();
    private readonly BindingSource _bindingSource = new BindingSource();
    private Mp3GainEngine? _engine;
    private CancellationTokenSource? _cts;
    private bool _isProcessing;
    private AppSettings _settings = new();

    private ToolStripMenuItem _alwaysOnTopItem = null!;
    private ToolStripMenuItem _addSubfoldersItem = null!;
    private ToolStripMenuItem _preserveTimestampItem = null!;
    private ToolStripMenuItem _noLayerCheckItem = null!;
    private ToolStripMenuItem _dontClipItem = null!;
    private ToolStripMenuItem _skipTagsItem = null!;
    private ToolStripMenuItem _recalcTagsItem = null!;
    private ToolStripMenuItem _beepWhenFinishedItem = null!;
    
    private ToolStripMenuItem _filenamePathFileItem = null!;
    private ToolStripMenuItem _filenameFileOnlyItem = null!;
    private ToolStripMenuItem _filenamePathAndFileItem = null!;

    public MainForm()
    {
        InitializeComponent();
        _settings = AppSettings.Load();
        _bindingSource.DataSource = _files;
        SetupTheme();
        SetupTargetPanel();
        SetupToolbar();
        SetupMenuStrip();
        SetupGrid();
        SetupStatusBar();
        SetupDragDrop();
        UpdateStatus();

        // Try to init engine
        try
        {
            _engine = new Mp3GainEngine();
            string ver = _engine.GetVersion();
            this.Text = $"MP3Gain 2026  —  Engine v{ver}";
        }
        catch (Exception ex)
        {
            _statusLabel!.Text = $"⚠ Native DLL not loaded: {ex.Message}";
            _statusLabel.ForeColor = AccentWarning;
        }
    }

    /* ══════════════════════════════════════════════════════════════
     *  THEME & LAYOUT
     * ══════════════════════════════════════════════════════════════ */

    private void SetupTheme()
    {
        this.Text = "MP3Gain 2026";
        this.Size = new Size(1200, 750);
        this.MinimumSize = new Size(900, 500);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = BgDark;
        this.ForeColor = TextPrimary;
        this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        this.DoubleBuffered = true;


    }

    /* ══════════════════════════════════════════════════════════════
     *  MENU STRIP
     * ══════════════════════════════════════════════════════════════ */

    private void SetupMenuStrip()
    {
        _menuStrip = new MenuStrip
        {
            BackColor = BgToolbar,
            ForeColor = TextPrimary,
            Renderer = new DarkToolStripRenderer(),
            Padding = new Padding(4, 2, 0, 2)
        };

        // ── File ──
        var fileMenu = new ToolStripMenuItem("&File");
        fileMenu.DropDownItems.Add("Add &File(s)...", null, OnAddFiles);
        fileMenu.DropDownItems.Add("Add Fol&der...", null, OnAddFolder);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("&Load Analysis...", null, OnLoadAnalysis);
        fileMenu.DropDownItems.Add("&Save Analysis...", null, OnSaveAnalysis);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("Select &All", null, (s, e) => _grid.SelectAll());
        fileMenu.DropDownItems.Add("Select &None", null, (s, e) => _grid.ClearSelection());
        fileMenu.DropDownItems.Add("&Invert Selection", null, OnInvertSelection);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("Clear &Selected File(s)", null, OnClearSelected);
        fileMenu.DropDownItems.Add("Clear &All Files", null, OnClear);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("E&xit", null, (s, e) => Close());

        // ── Analysis ──
        var analysisMenu = new ToolStripMenuItem("&Analysis");
        analysisMenu.DropDownItems.Add("Analyze File(&s)", null, OnAnalyze);
        analysisMenu.DropDownItems.Add("Analyze &Album", null, OnAnalyzeAlbum);
        analysisMenu.DropDownItems.Add(new ToolStripSeparator());
        analysisMenu.DropDownItems.Add("&Clear Analysis", null, OnClearAnalysis);

        // ── Modify Gain ──
        var gainMenu = new ToolStripMenuItem("&Modify Gain");
        gainMenu.DropDownItems.Add("Apply &Track Gain", null, OnApplyGain);
        gainMenu.DropDownItems.Add("Apply &Album Gain", null, OnApplyAlbumGain);
        gainMenu.DropDownItems.Add("Apply &Constant Gain...", null, OnApplyConstantGain);
        gainMenu.DropDownItems.Add(new ToolStripSeparator());
        gainMenu.DropDownItems.Add("&Undo Gain Changes", null, OnUndoGain);

        // ── Options ──
        var optionsMenu = new ToolStripMenuItem("&Options");

        _alwaysOnTopItem = new ToolStripMenuItem("&Always on Top") { CheckOnClick = true, Checked = _settings.AlwaysOnTop };
        _alwaysOnTopItem.CheckedChanged += (s, e) => this.TopMost = _alwaysOnTopItem.Checked;
        optionsMenu.DropDownItems.Add(_alwaysOnTopItem);

        _addSubfoldersItem = new ToolStripMenuItem("Add &Subfolders") { CheckOnClick = true, Checked = _settings.AddSubfolders };
        optionsMenu.DropDownItems.Add(_addSubfoldersItem);

        _preserveTimestampItem = new ToolStripMenuItem("&Preserve File Timestamp") { CheckOnClick = true, Checked = _settings.PreserveTimestamp };
        optionsMenu.DropDownItems.Add(_preserveTimestampItem);

        optionsMenu.DropDownItems.Add(new ToolStripSeparator());

        _noLayerCheckItem = new ToolStripMenuItem("&No Check for Layer I or II") { CheckOnClick = true, Checked = _settings.NoLayerCheck };
        optionsMenu.DropDownItems.Add(_noLayerCheckItem);

        _dontClipItem = new ToolStripMenuItem("&Don't Clip when doing Track Gain") { CheckOnClick = true, Checked = _settings.DontClip };
        optionsMenu.DropDownItems.Add(_dontClipItem);

        optionsMenu.DropDownItems.Add(new ToolStripSeparator());

        var tagOptionsItem = new ToolStripMenuItem("&Tag Options");
        _skipTagsItem = new ToolStripMenuItem("&Ignore (do not read or write tags)") { CheckOnClick = true, Checked = _settings.IgnoreTags };
        tagOptionsItem.DropDownItems.Add(_skipTagsItem);
        _recalcTagsItem = new ToolStripMenuItem("&Re-calculate (do not read tags)") { CheckOnClick = true, Checked = _settings.RecalcTags };
        tagOptionsItem.DropDownItems.Add(_recalcTagsItem);
        tagOptionsItem.DropDownItems.Add(new ToolStripSeparator());
        tagOptionsItem.DropDownItems.Add("Remove &Tags from files", null, OnRemoveTags);
        optionsMenu.DropDownItems.Add(tagOptionsItem);

        optionsMenu.DropDownItems.Add(new ToolStripSeparator());

        _beepWhenFinishedItem = new ToolStripMenuItem("&Beep When Finished") { CheckOnClick = true, Checked = _settings.BeepWhenFinished };
        optionsMenu.DropDownItems.Add(_beepWhenFinishedItem);

        optionsMenu.DropDownItems.Add(new ToolStripSeparator());

        var filenameMenu = new ToolStripMenuItem("Filename Displa&y");
        
        _filenamePathFileItem = new ToolStripMenuItem(@"Show &Path\File") { CheckOnClick = true, Checked = (_settings.FilenameDisplayMode == 0) };
        _filenamePathFileItem.Click += (s, e) => UpdateFilenameDisplay(_filenamePathFileItem);
        
        _filenameFileOnlyItem = new ToolStripMenuItem("Show File onl&y") { CheckOnClick = true, Checked = (_settings.FilenameDisplayMode == 1) };
        _filenameFileOnlyItem.Click += (s, e) => UpdateFilenameDisplay(_filenameFileOnlyItem);
        
        _filenamePathAndFileItem = new ToolStripMenuItem("Show Path && Fi&le (as separate columns)") { CheckOnClick = true, Checked = (_settings.FilenameDisplayMode == 2) };
        _filenamePathAndFileItem.Click += (s, e) => UpdateFilenameDisplay(_filenamePathAndFileItem);

        filenameMenu.DropDownItems.Add(_filenamePathFileItem);
        filenameMenu.DropDownItems.Add(_filenameFileOnlyItem);
        filenameMenu.DropDownItems.Add(_filenamePathAndFileItem);
        
        optionsMenu.DropDownItems.Add(filenameMenu);

        optionsMenu.DropDownItems.Add(new ToolStripSeparator());

        optionsMenu.DropDownItems.Add("&Logs...", null, OnLogs);

        optionsMenu.DropDownItems.Add(new ToolStripSeparator());

        optionsMenu.DropDownItems.Add("&Reset Default Column Widths", null, OnResetColumnWidths);

        // ── Help ──
        var helpMenu = new ToolStripMenuItem("&Help");
        helpMenu.DropDownItems.Add("&About MP3Gain 2026", null, OnAbout);

        _menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, analysisMenu, gainMenu, optionsMenu, helpMenu });

        // Style all dropdown items
        foreach (ToolStripMenuItem topItem in _menuStrip.Items)
        {
            topItem.ForeColor = TextPrimary;
            if (topItem.DropDown is ToolStripDropDownMenu dd)
            {
                dd.BackColor = BgPanel;
                dd.ForeColor = TextPrimary;
                dd.Renderer = new DarkToolStripRenderer();
            }
            foreach (ToolStripItem sub in topItem.DropDownItems)
            {
                sub.ForeColor = TextPrimary;
                sub.BackColor = BgPanel;
                if (sub is ToolStripMenuItem subMenu && subMenu.HasDropDownItems)
                {
                    if (subMenu.DropDown is ToolStripDropDownMenu subDd)
                    {
                        subDd.BackColor = BgPanel;
                        subDd.ForeColor = TextPrimary;
                        subDd.Renderer = new DarkToolStripRenderer();
                    }
                    foreach (ToolStripItem nested in subMenu.DropDownItems)
                    {
                        nested.ForeColor = TextPrimary;
                        nested.BackColor = BgPanel;
                    }
                }
            }
        }

        this.MainMenuStrip = _menuStrip;
        this.Controls.Add(_menuStrip);
    }

    /* ══════════════════════════════════════════════════════════════
     *  TOOLBAR
     * ══════════════════════════════════════════════════════════════ */

    private void SetupToolbar()
    {
        _topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = BgToolbar,
            Padding = new Padding(8, 4, 8, 4)
        };
        _topPanel.Paint += (s, e) =>
        {
            // Bottom border gradient
            using var brush = new LinearGradientBrush(
                new Point(0, _topPanel.Height - 2),
                new Point(_topPanel.Width, _topPanel.Height - 2),
                AccentPrimary, AccentSecondary);
            e.Graphics.FillRectangle(brush, 0, _topPanel.Height - 2, _topPanel.Width, 2);
        };

        _toolbar = new ToolStrip
        {
            Dock = DockStyle.Fill,
            GripStyle = ToolStripGripStyle.Hidden,
            BackColor = Color.Transparent,
            ForeColor = TextPrimary,
            Renderer = new DarkToolStripRenderer(),
            AutoSize = false,
            Height = 44,
            ImageScalingSize = new Size(20, 20),
            Padding = new Padding(4, 2, 4, 2)
        };

        AddToolButton("btnAddFiles", "📂  Add Files", "Add MP3 files", OnAddFiles);
        AddToolButton("btnAddFolder", "📁  Add Folder", "Add folder of MP3 files", OnAddFolder);
        _toolbar.Items.Add(new ToolStripSeparator());
        
        AddToolButton("btnAnalyze", "🔍  Analyze File(s)", "Analyze selected files", OnAnalyze);
        AddToolButton("btnAnalyzeAll", "🔍  Analyze All", "Analyze all files", OnAnalyzeAll);
        AddToolButton("btnClearAnalysis", "🧹  Clear Analysis", "Clear analysis results", OnClearAnalysis);
        
        _toolbar.Items.Add(new ToolStripSeparator());
        
        AddToolButton("btnApplyGain", "🎚  Apply Gain", "Apply gain for all analyzed files", OnApplyGain);
        
        
        _toolbar.Items.Add(new ToolStripSeparator());
        AddToolButton("btnClear", "🗑  Clear File List", "Clear file list", OnClear);
        _toolbar.Items.Add(new ToolStripSeparator());
        AddToolButton("btnCancel", "⛔  Cancel", "Cancel current operation", OnCancel);

        _topPanel.Controls.Add(_toolbar);
        this.Controls.Add(_topPanel);

        SetToolbarEnabled(true);
    }

    private void AddToolButton(string name, string text, string tooltip, EventHandler onClick)
    {
        var btn = new ToolStripButton
        {
            Name = name,
            Text = text,
            ToolTipText = tooltip,
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            Padding = new Padding(6, 4, 6, 4),
            Margin = new Padding(2, 0, 2, 0),
            AutoSize = true
        };
        btn.Click += onClick;
        _toolbar.Items.Add(btn);
    }

    /* ══════════════════════════════════════════════════════════════
     *  TARGET DB PANEL
     * ══════════════════════════════════════════════════════════════ */

    private void SetupTargetPanel()
    {
        _targetPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 42,
            BackColor = BgPanel,
            Padding = new Padding(12, 6, 12, 6)
        };

        _targetDbLabel = new Label
        {
            Text = "Target Volume (dB):",
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 9.5f),
            AutoSize = true,
            Location = new Point(12, 11)
        };

        _targetDbSpinner = new NumericUpDown
        {
            Minimum = 75.0m,
            Maximum = 105.0m,
            Value = Math.Clamp(_settings.TargetVolume, 75.0m, 105.0m),
            DecimalPlaces = 1,
            Increment = 0.5m,
            Width = 80,
            Location = new Point(160, 7),
            BackColor = BgCard,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            BorderStyle = BorderStyle.FixedSingle
        };

        var helpLabel = new LinkLabel
        {
            Text = "ℹ  89.0 dB (default)",
            LinkColor = TextMuted,
            ActiveLinkColor = TextPrimary,
            VisitedLinkColor = TextMuted,
            LinkBehavior = LinkBehavior.HoverUnderline,
            Font = new Font("Segoe UI", 8.5f),
            AutoSize = true,
            Location = new Point(260, 12)
        };
        helpLabel.LinkClicked += (s, e) => _targetDbSpinner.Value = 89.0m;

        _targetDbSpinner.ValueChanged += (s, e) =>
        {
            double newTarget = (double)_targetDbSpinner.Value;
            foreach(var file in _files)
            {
                file.TargetDb = newTarget;
            }
            RefreshGrid();
        };

        _targetPanel.Controls.AddRange(new Control[] { _targetDbLabel, _targetDbSpinner, helpLabel });
        this.Controls.Add(_targetPanel);
    }

    /* ══════════════════════════════════════════════════════════════
     *  DATA GRID
     * ══════════════════════════════════════════════════════════════ */

    private void SetupGrid()
    {
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = true,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            ReadOnly = true,
            RowHeadersVisible = false,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = BorderColor,
            BackgroundColor = BgDark,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = BgDark,
                ForeColor = TextPrimary,
                SelectionBackColor = Color.FromArgb(45, 50, 90),
                SelectionForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Padding = new Padding(4, 2, 4, 2)
            },
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = BgPanel,
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI Semibold", 9f),
                Padding = new Padding(4, 6, 4, 6),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = BgCard,
                ForeColor = TextPrimary,
                SelectionBackColor = Color.FromArgb(45, 50, 90),
                SelectionForeColor = Color.White
            },
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 36,
            RowTemplate = { Height = 30 }
        };

        AddColumn("colStatus",        "Status",           nameof(Mp3FileInfo.Status),            90);
        AddColumn("colFileName",      "File Name",        nameof(Mp3FileInfo.FileName),       200);
        AddColumn("colVolume",        "Volume",           nameof(Mp3FileInfo.Volume),           80);
        AddColumn("colClipping",      "Clipping",         nameof(Mp3FileInfo.Clipping),         80);
        AddColumn("colTrackGain",     "Track Gain (dB)",  nameof(Mp3FileInfo.GainChangeDisplay), 110);
        AddColumn("colTrackPeak",     "Track Peak",       nameof(Mp3FileInfo.TrackPeak),         90);
        AddColumn("colAlbumGain",     "Album Gain (dB)",  nameof(Mp3FileInfo.AlbumGainDisplay),  110);
        AddColumn("colAlbumPeak",     "Album Peak",       nameof(Mp3FileInfo.AlbumPeak),         90);
        AddColumn("colDirectory",     "Path",             nameof(Mp3FileInfo.Directory),       180);
        AddColumn("colFileSize",      "Size",             nameof(Mp3FileInfo.FileSizeFormatted),  70);

        _grid.DataSource = _bindingSource;

        _grid.CellFormatting += Grid_CellFormatting;
        _grid.CellPainting += Grid_CellPainting;
        _grid.KeyDown += Grid_KeyDown;

        this.Controls.Add(_grid);
        _grid.BringToFront();

        UpdateFilenameDisplay(_settings.FilenameDisplayMode switch
        {
            1 => _filenameFileOnlyItem,
            2 => _filenamePathAndFileItem,
            _ => _filenamePathFileItem
        });
    }



    private void AddColumn(string name, string header, string propertyName, int width)
    {
        var col = new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            DataPropertyName = propertyName,
            Width = width,
            SortMode = DataGridViewColumnSortMode.Automatic
        };
        _grid.Columns.Add(col);
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex < 0 || e.RowIndex < 0) return;

        string colName = _grid.Columns[e.ColumnIndex].Name;
        var file = _files[e.RowIndex];

        // Format gain values
        if ((colName == "colTrackGain" || colName == "colAlbumGain") && e.Value is double gain)
        {
            e.Value = $"{gain:+0.0;-0.0;0.0} dB";
            e.FormattingApplied = true;

            // Color code positive/negative
            if (gain > 0) e.CellStyle!.ForeColor = AccentSuccess;
            else if (gain < 0) e.CellStyle!.ForeColor = AccentWarning;
        }
        else if (colName == "colVolume" && e.Value is double vol)
        {
            e.Value = $"{vol:0.0}";
            e.FormattingApplied = true;
        }
        else if ((colName == "colTrackPeak" || colName == "colAlbumPeak") && e.Value is double peak)
        {
            e.Value = $"{peak:F6}";
            e.FormattingApplied = true;
            if (peak > 1.0) e.CellStyle!.ForeColor = AccentDanger;
        }
        else if (colName == "colStatus" && e.Value is FileStatus status)
        {
            e.CellStyle!.ForeColor = status switch
            {
                FileStatus.Analyzed => AccentSuccess,
                FileStatus.Applied  => AccentPrimary,
                FileStatus.Error    => AccentDanger,
                FileStatus.Analyzing or FileStatus.Applying => AccentWarning,
                _ => TextMuted
            };
        }

        // Clipping highlights
        if ((colName == "colFileName" || colName == "colClipping") && file.Clipping == "Y")
        {
            e.CellStyle!.ForeColor = AccentDanger;
            e.CellStyle.SelectionForeColor = AccentDanger;
        }
    }

    private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
        
        string colName = _grid.Columns[e.ColumnIndex].Name;
        if (colName == "colStatus")
        {
            var file = _files[e.RowIndex];
            if ((file.Status == FileStatus.Analyzing || file.Status == FileStatus.Applying) && file.Progress >= 0)
            {
                e.PaintBackground(e.CellBounds, true);
                
                int padding = 2;
                Rectangle progressRect = new Rectangle(
                    e.CellBounds.X + padding, 
                    e.CellBounds.Y + padding, 
                    (int)((e.CellBounds.Width - padding * 2) * (file.Progress / 100f)), 
                    e.CellBounds.Height - padding * 2);

                if (progressRect.Width > 0)
                {
                    using var brush = new SolidBrush(Color.FromArgb(40, AccentPrimary));
                    e.Graphics!.FillRectangle(brush, progressRect);
                }

                e.PaintContent(e.CellBounds);
                e.Handled = true;
            }
        }
    }

    private void Grid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete && !_isProcessing)
        {
            RemoveSelectedFiles();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.A && e.Control)
        {
            _grid.SelectAll();
            e.Handled = true;
        }
    }

    /* ══════════════════════════════════════════════════════════════
     *  STATUS BAR
     * ══════════════════════════════════════════════════════════════ */

    private void SetupStatusBar()
    {
        _statusBar = new StatusStrip
        {
            BackColor = BgToolbar,
            ForeColor = TextSecondary,
            SizingGrip = true,
            Font = new Font("Segoe UI", 9f)
        };

        _statusLabel = new ToolStripStatusLabel
        {
            Text = "Ready",
            ForeColor = TextSecondary,
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _fileProgressLabel = new ToolStripStatusLabel { Text = "File Progress:", Visible = false };
        _fileProgressBar = new ToolStripProgressBar
        {
            Visible = false,
            Width = 100,
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30
        };

        _totalProgressLabel = new ToolStripStatusLabel { Text = "Total Progress:", Visible = false, Margin = new Padding(10, 0, 0, 0) };
        _totalProgressBar = new ToolStripProgressBar
        {
            Visible = false,
            Width = 150,
            Style = ProgressBarStyle.Continuous
        };

        _fileCountLabel = new ToolStripStatusLabel
        {
            Text = "0 files",
            ForeColor = TextMuted,
            Alignment = ToolStripItemAlignment.Right
        };

        _statusBar.Items.AddRange(new ToolStripItem[] { _statusLabel, _fileProgressLabel, _fileProgressBar, _totalProgressLabel, _totalProgressBar, _fileCountLabel });
        this.Controls.Add(_statusBar);
    }

    /* ══════════════════════════════════════════════════════════════
     *  DRAG & DROP
     * ══════════════════════════════════════════════════════════════ */

    private void SetupDragDrop()
    {
        this.AllowDrop = true;
        this.DragEnter += (s, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        };
        this.DragDrop += (s, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
            {
                AddPaths(paths);
                this.BeginInvoke(new Action(() => {
                    _bindingSource.ResetBindings(false);
                    _grid.DataSource = null;
                    _grid.DataSource = _bindingSource;
                    _grid.Refresh();
                }));
            }
        };
    }

    /* ══════════════════════════════════════════════════════════════
     *  FILE MANAGEMENT
     * ══════════════════════════════════════════════════════════════ */

    private void AddPaths(string[] paths)
    {
        int initialCount = _files.Count;

        foreach (string path in paths)
        {
            if (System.IO.Directory.Exists(path))
            {
                try {
                    var searchOpt = _addSubfoldersItem.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    var mp3s = System.IO.Directory.GetFiles(path, "*.mp3", searchOpt);
                    foreach (string mp3 in mp3s)
                        AddFileIfNew(mp3);
                } catch { } // Ignore access denied on folders
            }
            else if (File.Exists(path))
            {
                bool added = AddFileIfNew(path);
            }
        }
        
        int addedCount = _files.Count - initialCount;
        _statusLabel.Text = addedCount > 0 ? $"{addedCount} file(s) loaded" : "No new files loaded";
        _statusLabel.ForeColor = TextSecondary;

        UpdateStatus();
    }

    private bool AddFileIfNew(string path)
    {
        string fullPath = Path.GetFullPath(path);
        if (_files.Any(f => f.FilePath.Equals(fullPath, StringComparison.OrdinalIgnoreCase)))
            return false;

        _files.Add(Mp3FileInfo.FromPath(fullPath));
        return true;
    }

    private void RemoveSelectedFiles()
    {
        var toRemove = _grid.SelectedRows.Cast<DataGridViewRow>()
            .Select(r => r.DataBoundItem as Mp3FileInfo)
            .Where(f => f != null)
            .ToList();

        foreach (var file in toRemove)
            _files.Remove(file!);

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        int total = _files.Count;
        int analyzed = _files.Count(f => f.Status == FileStatus.Analyzed || f.Status == FileStatus.Applied);
        _fileCountLabel.Text = $"{total} file{(total != 1 ? "s" : "")}  |  {analyzed} analyzed";
    }

    /* ══════════════════════════════════════════════════════════════
     *  TOOLBAR HANDLERS
     * ══════════════════════════════════════════════════════════════ */

    private void OnAddFiles(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Add MP3 Files",
            Filter = "MP3 Files (*.mp3)|*.mp3|All Files (*.*)|*.*",
            Multiselect = true,
            RestoreDirectory = true
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            AddPaths(dlg.FileNames);
            this.BeginInvoke(new Action(() => {
                _bindingSource.ResetBindings(false);
                _grid.DataSource = null;
                _grid.DataSource = _bindingSource;
                _grid.Refresh();
            }));
        }
    }

    private void OnAddFolder(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select folder containing MP3 files",
            ShowNewFolderButton = false
        };

        if (dlg.ShowDialog() == DialogResult.OK)
            AddPaths(new[] { dlg.SelectedPath });
    }

    private async void OnAnalyze(object? sender, EventArgs e)
    {
        if (_engine == null || _isProcessing) return;

        var targets = GetSelectedOrAllFiles();
        if (targets.Count == 0) return;

        await ProcessFilesAsync("Analyzing", targets, async (file, ct) =>
        {
            file.Status = FileStatus.Analyzing;
            RefreshGrid();

            var result = await _engine.AnalyzeFileAsync(file.FilePath, _settings.IgnoreTags, _settings.RecalcTags, ct);

            file.TrackGain = result.TrackGain;
            file.TrackPeak = result.TrackPeak;
            file.AlbumGain = result.AlbumGain;
            file.AlbumPeak = result.AlbumPeak;
            file.MinGlobalGain = result.MinGlobalGain;
            file.MaxGlobalGain = result.MaxGlobalGain;
            file.MaxNoClipGain = result.MaxNoClipGain;
            file.HasUndo = result.HasUndo;
            file.UndoLeft = result.UndoLeft;
            file.UndoRight = result.UndoRight;
            file.Status = FileStatus.Analyzed;
        });
    }

    private async void OnAnalyzeAll(object? sender, EventArgs e)
    {
        if (_engine == null || _isProcessing) return;

        var targets = _files.Where(f => f.Status != FileStatus.Analyzed).ToList();
        int skipped = _files.Count - targets.Count;

        if (targets.Count == 0 && skipped > 0)
        {
            _statusLabel.Text = $"Analysis complete: 0 analyzed, {skipped} skipped";
            return;
        }
        if (targets.Count == 0) return;

        await ProcessFilesAsync("Analyzing All", targets, async (file, ct) =>
        {
            file.Status = FileStatus.Analyzing;
            RefreshGrid();

            var result = await _engine.AnalyzeFileAsync(file.FilePath, _settings.IgnoreTags, _settings.RecalcTags, ct);

            file.TrackGain = result.TrackGain;
            file.TrackPeak = result.TrackPeak;
            file.AlbumGain = result.AlbumGain;
            file.AlbumPeak = result.AlbumPeak;
            file.MinGlobalGain = result.MinGlobalGain;
            file.MaxGlobalGain = result.MaxGlobalGain;
            file.MaxNoClipGain = result.MaxNoClipGain;
            file.HasUndo = result.HasUndo;
            file.UndoLeft = result.UndoLeft;
            file.UndoRight = result.UndoRight;
            file.Status = FileStatus.Analyzed;
        });

        int succesfulAnalyzed = targets.Count(f => f.Status == FileStatus.Analyzed);
        int totalErrors = targets.Count(f => f.Status == FileStatus.Error);
        
        if (totalErrors > 0)
            _statusLabel.Text = $"Analysis complete: {succesfulAnalyzed} analyzed, {skipped} skipped, {totalErrors} error(s)";
        else
            _statusLabel.Text = $"Analysis complete: {succesfulAnalyzed} analyzed, {skipped} skipped";
        
        _statusLabel.ForeColor = totalErrors > 0 ? AccentWarning : TextSecondary;
    }

    private async void OnAnalyzeAlbum(object? sender, EventArgs e)
    {
        if (_engine == null || _isProcessing) return;

        var targets = _files.ToList();
        if (targets.Count == 0) return;

        // For album mode, analyze all files then read album gain
        await ProcessFilesAsync("Album Analyzing", targets, async (file, ct) =>
        {
            file.Status = FileStatus.Analyzing;
            RefreshGrid();

            var result = await _engine.AnalyzeFileAsync(file.FilePath, _settings.IgnoreTags, _settings.RecalcTags, ct);

            file.TrackGain = result.TrackGain;
            file.TrackPeak = result.TrackPeak;
            file.AlbumGain = result.AlbumGain;
            file.AlbumPeak = result.AlbumPeak;
            file.MinGlobalGain = result.MinGlobalGain;
            file.MaxGlobalGain = result.MaxGlobalGain;
            file.MaxNoClipGain = result.MaxNoClipGain;
            file.HasUndo = result.HasUndo;
            file.UndoLeft = result.UndoLeft;
            file.UndoRight = result.UndoRight;
            file.Status = FileStatus.Analyzed;
        });
    }

    private async void OnApplyGain(object? sender, EventArgs e)
    {
        if (_engine == null || _isProcessing) return;

        var targets = _files.ToList();
        if (targets.Count == 0)
        {
            MessageBox.Show("No files to apply gain to.\nPlease add files first.",
                "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        double targetDb = (double)_targetDbSpinner.Value;


        await ProcessFilesAsync("Applying Gain", targets, async (file, ct) =>
        {
            if (file.Status != FileStatus.Analyzed && file.Status != FileStatus.Applying)
            {
                file.Status = FileStatus.Analyzing;
                RefreshGrid();
                
                var preAnalysis = await _engine.AnalyzeFileAsync(file.FilePath, _settings.IgnoreTags, _settings.RecalcTags, ct);
                file.TrackGain = preAnalysis.TrackGain;
                file.TrackPeak = preAnalysis.TrackPeak;
                file.AlbumGain = preAnalysis.AlbumGain;
                file.AlbumPeak = preAnalysis.AlbumPeak;
                file.MinGlobalGain = preAnalysis.MinGlobalGain;
                file.MaxGlobalGain = preAnalysis.MaxGlobalGain;
                file.MaxNoClipGain = preAnalysis.MaxNoClipGain;
                file.HasUndo = preAnalysis.HasUndo;
                file.UndoLeft = preAnalysis.UndoLeft;
                file.UndoRight = preAnalysis.UndoRight;
            }

            // Calculate exact steps needed
            double gainDiff = targetDb - 89.0 + file.TrackGain!.Value;
            int gainChange = (int)Math.Round(gainDiff / 1.50514997831991); // MP3Gain step

            // Skip files that need no change
            if (gainChange == 0)
            {
                file.Status = FileStatus.Applied;
                return;
            }

            file.Status = FileStatus.Applying;
            RefreshGrid();

            if (gainChange != 0)
            {
                await _engine.ApplyGainAsync(file.FilePath, gainChange, _settings.PreserveTimestamp, ct);
                
                file.UndoLeft -= gainChange;
                file.UndoRight -= gainChange;
                file.HasUndo = true;

                // Manually adjust the displayed values (no need to re-analyze entirely if we trust the math)
                // MP3Gain natively updates these directly too.
                file.TrackGain -= gainChange * 1.50514997831991;
                file.TrackPeak *= Math.Pow(10.0, (gainChange * 1.50514997831991) / 20.0);
                if (file.AlbumGain.HasValue) file.AlbumGain -= gainChange * 1.50514997831991;
                if (file.AlbumPeak.HasValue) file.AlbumPeak *= Math.Pow(10.0, (gainChange * 1.50514997831991) / 20.0);
            }

            // Write modified tags to preserve Undo state 
            if (!_settings.IgnoreTags)
            {
                await _engine.WriteTagsAsync(file.FilePath, file, targetDb, _settings.PreserveTimestamp, ct);
            }

            file.Status = FileStatus.Applied;
        });
    }

    private async void OnApplyAlbumGain(object? sender, EventArgs e)
    {
        if (_engine == null || _isProcessing) return;

        var targets = GetSelectedOrAllFiles().Where(f => f.Status == FileStatus.Analyzed).ToList();
        if (targets.Count == 0)
        {
            MessageBox.Show("No analyzed files to apply gain to.\nAnalyze files first.",
                "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var missingAlbumGain = targets.Where(f => f.AlbumGain == null).ToList();
        if (missingAlbumGain.Count > 0)
        {
            MessageBox.Show("Some files have not been analyzed together as an album yet.\nPlease run 'Analyze Album' first.",
                "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        double targetDb = (double)_targetDbSpinner.Value;



        await ProcessFilesAsync("Applying Album Gain", targets, async (file, ct) =>
        {
            file.Status = FileStatus.Applying;
            RefreshGrid();
            
            double albumDbChange = targetDb - 89.0 + file.AlbumGain!.Value;
            int gainChange = (int)Math.Round(albumDbChange / 1.50514997831991);
            
            if (gainChange != 0)
            {
                await _engine.ApplyGainAsync(file.FilePath, gainChange, _settings.PreserveTimestamp, ct);
                
                file.UndoLeft -= gainChange;
                file.UndoRight -= gainChange;
                file.HasUndo = true;

                file.TrackGain -= gainChange * 1.50514997831991;
                file.TrackPeak *= Math.Pow(10.0, (gainChange * 1.50514997831991) / 20.0);
                file.AlbumGain -= gainChange * 1.50514997831991;
                file.AlbumPeak *= Math.Pow(10.0, (gainChange * 1.50514997831991) / 20.0);
            }

            if (!_settings.IgnoreTags)
            {
                await _engine.WriteTagsAsync(file.FilePath, file, targetDb, _settings.PreserveTimestamp, ct);
            }

            file.Status = FileStatus.Applied;
        });
    }

    private async void OnApplyConstantGain(object? sender, EventArgs e)
    {
        if (_engine == null || _isProcessing) return;

        var targets = GetSelectedOrAllFiles();
        if (targets.Count == 0)
        {
            MessageBox.Show("No files selected.",
                "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new ConstantGainForm();
        if (dlg.ShowDialog() != DialogResult.OK) return;
        
        int steps = dlg.GainSteps;
        if (steps == 0) return;

        if (dlg.ChannelMode != 0)
        {
            MessageBox.Show("Channel-specific gain modification is not yet supported by the modern MP3Gain 2026 engine.\n\nThe gain will be applied equally to both channels.",
                "Feature Unsupported", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        await ProcessFilesAsync("Applying Constant Gain", targets, async (file, ct) =>
        {
            file.Status = FileStatus.Applying;
            RefreshGrid();

            if (steps != 0)
            {
                await _engine.ApplyGainAsync(file.FilePath, steps, _settings.PreserveTimestamp, ct);
                
                file.UndoLeft -= steps;
                file.UndoRight -= steps;
                file.HasUndo = true;

                if (file.TrackGain.HasValue) file.TrackGain -= steps * 1.50514997831991;
                if (file.TrackPeak.HasValue) file.TrackPeak *= Math.Pow(10.0, (steps * 1.50514997831991) / 20.0);
                if (file.AlbumGain.HasValue) file.AlbumGain -= steps * 1.50514997831991;
                if (file.AlbumPeak.HasValue) file.AlbumPeak *= Math.Pow(10.0, (steps * 1.50514997831991) / 20.0);

                double targetDb = (double)_targetDbSpinner.Value;
                if (!_settings.IgnoreTags)
                {
                    await _engine.WriteTagsAsync(file.FilePath, file, targetDb, _settings.PreserveTimestamp, ct);
                }
            }

            file.Status = FileStatus.Applied;
        });
    }

    private async void OnUndoGain(object? sender, EventArgs e)
    {
        if (_engine == null || _isProcessing) return;

        var targets = GetSelectedOrAllFiles();
        if (targets.Count == 0)
        {
            MessageBox.Show("No files selected.",
                "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Undo gain changes on {targets.Count} file(s)?",
            "Confirm Undo",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        await ProcessFilesAsync("Undoing Gain", targets, async (file, ct) =>
        {
            file.Status = FileStatus.Applying;
            RefreshGrid();

            try 
            { 
                await _engine.UndoGainAsync(file.FilePath, true, ct); 
            }
            catch (Mp3GainException ex) when (ex.Message.Contains("No undo information available"))
            {
                MessageBox.Show($"UNDO data not found for {file.FileName}. Skip?", "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // skip further processing
            }
            
            // Crucial: we MUST remove old Undo tags here BEFORE AnalyzeFileAsync reads them.
            // If AnalyzeFileAsync sees the old tag, it will skip doing a fresh acoustic analysis 
            // and simply report the old "wrong" pre-undo volume!
            if (!_settings.IgnoreTags)
            {
                await _engine.RemoveTagsAsync(file.FilePath, _settings.PreserveTimestamp, ct);
            }

            var analysisResult = await _engine.AnalyzeFileAsync(file.FilePath, _settings.IgnoreTags, _settings.RecalcTags, ct);
            file.TrackGain = analysisResult.TrackGain;
            file.TrackPeak = analysisResult.TrackPeak;
            file.AlbumGain = analysisResult.AlbumGain;
            file.AlbumPeak = analysisResult.AlbumPeak;
            file.MinGlobalGain = analysisResult.MinGlobalGain;
            file.MaxGlobalGain = analysisResult.MaxGlobalGain;
            file.MaxNoClipGain = analysisResult.MaxNoClipGain;
            
            // Explicitly clear Undo info since we just successfully undid the gain
            file.HasUndo = false;
            file.UndoLeft = 0;
            file.UndoRight = 0;
            
            file.Status = FileStatus.Analyzed;

            double targetDb = (double)_targetDbSpinner.Value;
            if (!_settings.IgnoreTags)
            {
                await _engine.WriteTagsAsync(file.FilePath, file, targetDb, _settings.PreserveTimestamp, ct);
            }
        });
    }

    private async void OnRemoveTags(object? sender, EventArgs e)
    {
        if (_engine == null || _isProcessing) return;

        var targets = GetSelectedOrAllFiles().ToList();
        if (targets.Count == 0) return;

        var result = MessageBox.Show(
            $"Remove ReplayGain tags from {targets.Count} file(s)?",
            "Confirm Tag Removal",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        await ProcessFilesAsync("Removing Tags", targets, async (file, ct) =>
        {
            await _engine.RemoveTagsAsync(file.FilePath, true, ct);
            file.HasExistingTags = false;
            file.HasUndo = false;
            file.TrackGain = null;
            file.TrackPeak = null;
            file.AlbumGain = null;
            file.AlbumPeak = null;
            file.Status = FileStatus.Pending;
        });
    }

    private void OnClear(object? sender, EventArgs e)
    {
        if (_isProcessing) return;
        _files.Clear();
        _statusLabel.Text = "";
        UpdateStatus();
    }

    private void OnClearAnalysis(object? sender, EventArgs e)
    {
        if (_isProcessing) return;
        foreach (var file in _files)
            file.ClearAnalysis();
        RefreshGrid();
        UpdateStatus();
    }

    private void UpdateFilenameDisplay(ToolStripMenuItem activeItem)
    {
        _filenamePathFileItem.Checked = (activeItem == _filenamePathFileItem);
        _filenameFileOnlyItem.Checked = (activeItem == _filenameFileOnlyItem);
        _filenamePathAndFileItem.Checked = (activeItem == _filenamePathAndFileItem);

        if (_grid.Columns.Contains("colFileName") && _grid.Columns.Contains("colDirectory"))
        {
            var fileNameCol = _grid.Columns["colFileName"]!;
            var dirCol = _grid.Columns["colDirectory"]!;

            if (_filenamePathFileItem.Checked)
            {
                fileNameCol.DataPropertyName = nameof(Mp3FileInfo.FilePath);
                fileNameCol.HeaderText = @"Path\File";
                dirCol.Visible = false;
            }
            else if (_filenameFileOnlyItem.Checked)
            {
                fileNameCol.DataPropertyName = nameof(Mp3FileInfo.FileName);
                fileNameCol.HeaderText = "File";
                dirCol.Visible = false;
            }
            else if (_filenamePathAndFileItem.Checked)
            {
                fileNameCol.DataPropertyName = nameof(Mp3FileInfo.FileName);
                fileNameCol.HeaderText = "File";
                dirCol.Visible = true;
            }
            _grid.Invalidate();
        }
    }

    private void OnLogs(object? sender, EventArgs e)
    {
        using var dlg = new LogOptionsForm
        {
            ErrorLogPath = _settings.ErrorLogPath,
            AnalysisLogPath = _settings.AnalysisLogPath,
            ChangeLogPath = _settings.ChangeLogPath
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _settings.ErrorLogPath = dlg.ErrorLogPath;
            _settings.AnalysisLogPath = dlg.AnalysisLogPath;
            _settings.ChangeLogPath = dlg.ChangeLogPath;

            // Ensure log directory exists
            foreach (var path in new[] { _settings.ErrorLogPath, _settings.AnalysisLogPath, _settings.ChangeLogPath })
            {
                if (!string.IsNullOrEmpty(path))
                {
                    var dir = System.IO.Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                    {
                        try { System.IO.Directory.CreateDirectory(dir); } catch { }
                    }
                }
            }
        }
    }

    private void OnResetColumnWidths(object? sender, EventArgs e)
    {
        int[] defaults = { 90, 200, 80, 80, 110, 90, 110, 90, 180, 70 };
        for (int i = 0; i < _grid.Columns.Count && i < defaults.Length; i++)
            _grid.Columns[i].Width = defaults[i];
    }
    private void OnClearSelected(object? sender, EventArgs e)
    {
        if (_isProcessing) return;
        var selected = _grid.SelectedRows.Cast<DataGridViewRow>()
            .Select(r => r.DataBoundItem as Mp3FileInfo)
            .Where(f => f != null)
            .ToList();
        foreach (var f in selected)
            _files.Remove(f!);
        UpdateStatus();
    }

    private void OnCancel(object? sender, EventArgs e)
    {
        _cts?.Cancel();
    }

    private void OnAbout(object? sender, EventArgs e)
    {
        using var dlg = new AboutForm(_engine?.GetVersion() ?? "N/A");
        dlg.ShowDialog(this);
    }

    private void OnInvertSelection(object? sender, EventArgs e)
    {
        foreach (DataGridViewRow row in _grid.Rows)
            row.Selected = !row.Selected;
    }

    private void OnSaveAnalysis(object? sender, EventArgs e)
    {
        if (_files.Count == 0) return;

        using var dlg = new SaveFileDialog
        {
            Title = "Save Analysis",
            Filter = "MP3Gain Analysis (*.m3g)|*.m3g|Tab-Separated Values (*.tsv)|*.tsv|All Files (*.*)|*.*",
            DefaultExt = "m3g",
            FileName = "mp3gain_analysis.m3g"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        using var writer = new System.IO.StreamWriter(dlg.FileName);
        bool isLegacy = dlg.FileName.EndsWith(".m3g", StringComparison.OrdinalIgnoreCase);

        if (isLegacy)
        {
            // Legacy .m3g format for compatibility
            foreach (var f in _files)
            {
                string dir = f.Directory + "\\";
                double maxAmp = (f.TrackPeak ?? 0) * 32768.0;
                string albumStr = f.AlbumGain.HasValue ? f.AlbumGain.Value.ToString() : "\"?\"";
                string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                writer.WriteLine($"\"{dir}\",\"{f.FileName}\",#{ts}#,{f.FileSize},{maxAmp:F3},{f.TrackGain},{albumStr}");
            }
        }
        else
        {
            writer.WriteLine("FilePath\tTrackGain\tTrackPeak\tAlbumGain\tAlbumPeak\tVolume\tClipping\tStatus");
            foreach (var f in _files)
            {
                writer.WriteLine($"{f.FilePath}\t{f.TrackGain}\t{f.TrackPeak}\t{f.AlbumGain}\t{f.AlbumPeak}\t{f.Volume}\t{f.Clipping}\t{f.Status}");
            }
        }

        _statusLabel.Text = $"Analysis saved to {System.IO.Path.GetFileName(dlg.FileName)}";
        _statusLabel.ForeColor = TextSecondary;
    }

    private void OnLoadAnalysis(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Load Analysis",
            Filter = "MP3Gain Analysis (*.m3g)|*.m3g|Tab-Separated Values (*.tsv)|*.tsv|All Files (*.*)|*.*",
            DefaultExt = "m3g"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var lines = System.IO.File.ReadAllLines(dlg.FileName);
            if (lines.Length == 0) return;

            // Detect format: legacy .m3g starts with quoted directory path
            bool isLegacy = lines[0].StartsWith("\"");
            
            _files.Clear();

            if (isLegacy)
            {
                // Legacy format: "Dir\","Filename",#Timestamp#,FileSize,MaxAmplitude,TrackGain,AlbumGain
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Parse quoted CSV fields carefully
                    var fields = ParseLegacyCsvLine(line);
                    if (fields.Count < 7) continue;

                    string dir = fields[0].TrimEnd('\\');
                    string filename = fields[1];
                    string fullPath = System.IO.Path.Combine(dir, filename);

                    var f = Mp3FileInfo.FromPath(fullPath);

                    // Field 2 = timestamp (skip), Field 3 = filesize
                    if (long.TryParse(fields[3], out long size)) f.FileSize = size;

                    // Field 4 = MaxAmplitude (peak * 32768), convert back to peak
                    if (double.TryParse(fields[4], out double maxAmp))
                        f.TrackPeak = maxAmp / 32768.0;

                    // Field 5 = TrackGain (raw dB)
                    if (double.TryParse(fields[5], out double tg))
                        f.TrackGain = tg;

                    // Field 6 = AlbumGain (raw dB or "?")
                    if (fields[6] != "?" && double.TryParse(fields[6], out double ag))
                        f.AlbumGain = ag;

                    f.Status = f.TrackGain.HasValue ? FileStatus.Analyzed : FileStatus.Pending;
                    _files.Add(f);
                }
            }
            else
            {
                // New TSV format (skip header)
                for (int i = 1; i < lines.Length; i++)
                {
                    var parts = lines[i].Split('\t');
                    if (parts.Length < 1 || string.IsNullOrWhiteSpace(parts[0])) continue;

                    var f = Mp3FileInfo.FromPath(parts[0]);
                    if (parts.Length > 1 && double.TryParse(parts[1], out double tg)) f.TrackGain = tg;
                    if (parts.Length > 2 && double.TryParse(parts[2], out double tp)) f.TrackPeak = tp;
                    if (parts.Length > 3 && double.TryParse(parts[3], out double ag)) f.AlbumGain = ag;
                    if (parts.Length > 4 && double.TryParse(parts[4], out double ap)) f.AlbumPeak = ap;
                    if (parts.Length > 7 && Enum.TryParse<FileStatus>(parts[7], out var st)) f.Status = st;
                    _files.Add(f);
                }
            }

            RefreshGrid();
            UpdateStatus();
            _statusLabel.Text = $"Loaded analysis from {System.IO.Path.GetFileName(dlg.FileName)}  ({_files.Count} files)";
            _statusLabel.ForeColor = TextSecondary;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading analysis:\n{ex.Message}", "MP3Gain 2026",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Parse a legacy MP3Gain .m3g CSV line handling quoted fields and VB6 date literals.
    /// Format: "Dir\","Filename",#Timestamp#,FileSize,MaxAmplitude,TrackGain,AlbumGain
    /// </summary>
    private static List<string> ParseLegacyCsvLine(string line)
    {
        var fields = new List<string>();
        int i = 0;
        while (i < line.Length)
        {
            if (line[i] == '"')
            {
                // Quoted field
                int end = line.IndexOf('"', i + 1);
                if (end < 0) end = line.Length;
                fields.Add(line.Substring(i + 1, end - i - 1));
                i = end + 1;
                if (i < line.Length && line[i] == ',') i++; // skip comma
            }
            else if (line[i] == '#')
            {
                // VB6 date literal #...#
                int end = line.IndexOf('#', i + 1);
                if (end < 0) end = line.Length;
                fields.Add(line.Substring(i + 1, end - i - 1));
                i = end + 1;
                if (i < line.Length && line[i] == ',') i++; // skip comma
            }
            else if (line[i] == ',')
            {
                fields.Add("");
                i++;
            }
            else
            {
                // Unquoted field
                int end = line.IndexOf(',', i);
                if (end < 0) end = line.Length;
                fields.Add(line.Substring(i, end - i));
                i = end;
                if (i < line.Length && line[i] == ',') i++; // skip comma
            }
        }
        return fields;
    }

    /* ══════════════════════════════════════════════════════════════
     *  PROCESSING INFRASTRUCTURE
     * ══════════════════════════════════════════════════════════════ */

    private List<Mp3FileInfo> GetSelectedOrAllFiles()
    {
        if (_grid.SelectedRows.Count > 0 && _grid.SelectedRows.Count < _files.Count)
        {
            return _grid.SelectedRows.Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as Mp3FileInfo)
                .Where(f => f != null)
                .Select(f => f!)
                .ToList();
        }
        return _files.ToList();
    }

    private async Task ProcessFilesAsync(string operationName, List<Mp3FileInfo> targets,
        Func<Mp3FileInfo, CancellationToken, Task> processFunc)
    {
        _isProcessing = true;
        _cts = new CancellationTokenSource();
        _fileProgressLabel.Visible = false; // Hide from footer as requested
        _fileProgressBar.Visible = false;
        _totalProgressLabel.Visible = true;
        _totalProgressBar.Visible = true;
        _totalProgressBar.Maximum = targets.Count;
        _totalProgressBar.Value = 0;
        SetToolbarEnabled(false);

        int processed = 0;
        int errors = 0;
        Mp3FileInfo? currentProcessingFile = null;

        void OnProgressChanged(int percent)
        {
            if (currentProcessingFile != null)
            {
                currentProcessingFile.Progress = percent;
                if (InvokeRequired)
                {
                    BeginInvoke(() => RefreshGrid());
                }
                else
                {
                    RefreshGrid();
                }
            }
        }

        if (_engine != null)
            _engine.ProgressChanged += OnProgressChanged;

        try
        {
            foreach (var file in targets)
            {
                if (_cts.Token.IsCancellationRequested) break;

                _statusLabel.Text = $"{operationName}: {file.FileName}  ({processed + 1}/{targets.Count})";
                currentProcessingFile = file;
                file.Progress = 0;

                try
                {
                    await processFunc(file, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    file.Status = FileStatus.Pending;
                    break;
                }
                catch (Exception ex)
                {
                    file.Status = FileStatus.Error;
                    file.StatusMessage = ex.Message;
                    errors++;

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(_settings.ErrorLogPath))
                        {
                            var _dir = Path.GetDirectoryName(_settings.ErrorLogPath);
                            if (!string.IsNullOrWhiteSpace(_dir) && !Directory.Exists(_dir)) Directory.CreateDirectory(_dir);
                            File.AppendAllText(_settings.ErrorLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error on {file.FilePath}: {ex.Message}{Environment.NewLine}");
                        }
                    } 
                    catch { /* swallow log failures to prevent crashing processing */ }
                }

                file.Progress = -1;
                currentProcessingFile = null;
                processed++;
                _totalProgressBar.Value = processed;
                RefreshGrid();
                UpdateStatus();
            }
        }
        finally
        {
            if (_engine != null)
                _engine.ProgressChanged -= OnProgressChanged;

            _isProcessing = false;
            _totalProgressLabel.Visible = false;
            _totalProgressBar.Visible = false;
            SetToolbarEnabled(true);
            _cts?.Dispose();
            _cts = null;

            string summary = errors > 0
                ? $"{operationName} complete: {processed} processed, {errors} error(s)"
                : $"{operationName} complete: {processed} file(s) processed";
            _statusLabel.Text = summary;
            _statusLabel.ForeColor = errors > 0 ? AccentWarning : TextSecondary;

            _bindingSource.ResetBindings(false);
            RefreshGrid();
            UpdateStatus();

            if (_settings.BeepWhenFinished && targets.Count > 1)
            {
                System.Media.SystemSounds.Beep.Play();
            }
        }
    }

    private void SetToolbarEnabled(bool enabled)
    {
        foreach (ToolStripItem item in _toolbar.Items)
        {
            if (item.Name == "btnCancel")
                item.Visible = !enabled;
            else
                item.Visible = enabled;
        }
    }

    private void RefreshGrid()
    {
        _grid.Invalidate();
        _grid.Update();
    }

    /* ══════════════════════════════════════════════════════════════
     *  CLEANUP
     * ══════════════════════════════════════════════════════════════ */

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cts?.Cancel();
        _engine?.Dispose();

        _settings.AlwaysOnTop = _alwaysOnTopItem.Checked;
        _settings.AddSubfolders = _addSubfoldersItem.Checked;
        _settings.PreserveTimestamp = _preserveTimestampItem.Checked;
        _settings.NoLayerCheck = _noLayerCheckItem.Checked;
        _settings.DontClip = _dontClipItem.Checked;
        _settings.IgnoreTags = _skipTagsItem.Checked;
        _settings.RecalcTags = _recalcTagsItem.Checked;
        _settings.BeepWhenFinished = _beepWhenFinishedItem.Checked;
        
        if (_filenamePathAndFileItem.Checked) _settings.FilenameDisplayMode = 2;
        else if (_filenameFileOnlyItem.Checked) _settings.FilenameDisplayMode = 1;
        else _settings.FilenameDisplayMode = 0;

        _settings.TargetVolume = _targetDbSpinner.Value;

        _settings.Save();

        base.OnFormClosing(e);
    }
}

/* ══════════════════════════════════════════════════════════════════
 *  DARK TOOLSTRIP RENDERER
 * ══════════════════════════════════════════════════════════════════ */
internal class DarkToolStripRenderer : ToolStripProfessionalRenderer
{
    private static readonly Color BgToolbar   = Color.FromArgb(22, 22, 32);
    private static readonly Color BgHover     = Color.FromArgb(45, 45, 65);
    private static readonly Color BgPressed   = Color.FromArgb(60, 60, 85);
    private static readonly Color Accent      = Color.FromArgb(99, 102, 241);
    private static readonly Color BorderClr   = Color.FromArgb(55, 55, 75);
    private static readonly Color TextClr     = Color.FromArgb(237, 237, 245);
    private static readonly Color SepColor    = Color.FromArgb(50, 50, 70);

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        e.Graphics.Clear(BgToolbar);
    }

    protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
    {
        var bounds = new Rectangle(Point.Empty, e.Item.Size);
        Color bg;

        if (e.Item.Pressed)
            bg = BgPressed;
        else if (e.Item.Selected)
            bg = BgHover;
        else
            return; // transparent

        using var brush = new SolidBrush(bg);
        using var path = RoundedRect(bounds, 6);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillPath(brush, path);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        if (e.Item.IsOnDropDown)
        {
            Rectangle rect = new Rectangle(0, 0, e.Item.Width, e.Item.Height);
            using var bgBrush = new SolidBrush(Color.FromArgb(40, 40, 40));
            e.Graphics.FillRectangle(bgBrush, rect);
            using var pen = new Pen(SepColor);
            e.Graphics.DrawLine(pen, 30, e.Item.Height / 2, e.Item.Width - 10, e.Item.Height / 2);
        }
        else
        {
            int x = e.Item.Width / 2;
            using var pen = new Pen(SepColor);
            e.Graphics.DrawLine(pen, x, 4, x, e.Item.Height - 4);
        }
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);
        if (e.Item.IsOnDropDown)
        {
            Color bg = e.Item.Selected ? BgHover : Color.FromArgb(40, 40, 40);
            using var brush = new SolidBrush(bg);
            e.Graphics.FillRectangle(brush, rect);
        }
        else
        {
            if (e.Item.Pressed)
            {
                using var brush = new SolidBrush(BgPressed);
                e.Graphics.FillRectangle(brush, rect);
            }
            else if (e.Item.Selected)
            {
                using var brush = new SolidBrush(BgHover);
                e.Graphics.FillRectangle(brush, rect);
            }
            else
            {
                using var brush = new SolidBrush(BgToolbar);
                e.Graphics.FillRectangle(brush, rect);
            }
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.IsOnDropDown ? Color.WhiteSmoke : TextClr;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) 
    { 
        if (e.ToolStrip.IsDropDown)
        {
            using var pen = new Pen(BorderClr);
            e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1));
        }
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        var arc = new Rectangle(bounds.Location, new Size(d, d));

        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - d;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - d;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}
