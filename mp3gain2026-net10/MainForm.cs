using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mp3Gain2026;

public partial class MainForm : Form
{
	private static readonly Color BgDark = Color.FromArgb(18, 18, 24);

	private static readonly Color BgPanel = Color.FromArgb(26, 26, 36);

	private static readonly Color BgCard = Color.FromArgb(32, 33, 46);

	private static readonly Color BgCardHover = Color.FromArgb(42, 43, 58);

	private static readonly Color BgToolbar = Color.FromArgb(22, 22, 32);

	private static readonly Color BorderColor = Color.FromArgb(55, 55, 75);

	private static readonly Color AccentPrimary = Color.FromArgb(99, 102, 241);

	private static readonly Color AccentSecondary = Color.FromArgb(139, 92, 246);

	private static readonly Color AccentSuccess = Color.FromArgb(34, 197, 94);

	private static readonly Color AccentWarning = Color.FromArgb(251, 191, 36);

	private static readonly Color AccentDanger = Color.FromArgb(239, 68, 68);

	private static readonly Color TextPrimary = Color.FromArgb(237, 237, 245);

	private static readonly Color TextSecondary = Color.FromArgb(156, 156, 178);

	private static readonly Color TextMuted = Color.FromArgb(100, 100, 130);

	private MenuStrip _menuStrip = null;

	private ToolStrip _toolbar = null;

	private DataGridView _grid = null;

	private StatusStrip _statusBar = null;

	private ToolStripStatusLabel _statusLabel = null;

	private ToolStripStatusLabel _fileProgressLabel = null;

	private ToolStripProgressBar _fileProgressBar = null;

	private ToolStripStatusLabel _totalProgressLabel = null;

	private ToolStripProgressBar _totalProgressBar = null;

	private ToolStripStatusLabel _fileCountLabel = null;

	private NumericUpDown _targetDbSpinner = null;

	private Label _targetDbLabel = null;

	private Panel _topPanel = null;

	private Panel _targetPanel = null;

	private readonly BindingList<Mp3FileInfo> _files = new BindingList<Mp3FileInfo>();

	private readonly BindingSource _bindingSource = new BindingSource();

	private Mp3GainEngine? _engine;

	private CancellationTokenSource? _cts;

	private bool _isProcessing;

	private AppSettings _settings = new AppSettings();

	private ToolStripMenuItem _alwaysOnTopItem = null;

	private ToolStripMenuItem _addSubfoldersItem = null;

	private ToolStripMenuItem _preserveTimestampItem = null;

	private ToolStripMenuItem _noLayerCheckItem = null;

	private ToolStripMenuItem _dontClipItem = null;

	private ToolStripMenuItem _skipTagsItem = null;

	private ToolStripMenuItem _recalcTagsItem = null;

	private ToolStripMenuItem _beepWhenFinishedItem = null;

	private ToolStripMenuItem _limitToSingleThreadItem = null;

	private ToolStripMenuItem _filenamePathFileItem = null;

	private ToolStripMenuItem _filenameFileOnlyItem = null;

	private ToolStripMenuItem _filenamePathAndFileItem = null;

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
		try
		{
			_engine = new Mp3GainEngine();
			string version = _engine.GetVersion();
			Text = "MP3Gain 2026  —  Engine v" + version;
		}
		catch (Exception ex)
		{
			_statusLabel.Text = "⚠ Native DLL not loaded: " + ex.Message;
			_statusLabel.ForeColor = AccentWarning;
		}
	}

	private void SetupTheme()
	{
		Text = "MP3Gain 2026";
		base.Size = new Size(1200, 750);
		MinimumSize = new Size(900, 500);
		base.StartPosition = FormStartPosition.CenterScreen;
		BackColor = BgDark;
		ForeColor = TextPrimary;
		Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
		DoubleBuffered = true;
	}

	private void SetupMenuStrip()
	{
		_menuStrip = new MenuStrip
		{
			BackColor = BgToolbar,
			ForeColor = TextPrimary,
			Renderer = new DarkToolStripRenderer(),
			Padding = new Padding(4, 2, 0, 2)
		};
		ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem("&File");
		toolStripMenuItem.DropDownItems.Add("Add &File(s)...", null, OnAddFiles);
		toolStripMenuItem.DropDownItems.Add("Add Fol&der...", null, OnAddFolder);
		toolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
		toolStripMenuItem.DropDownItems.Add("&Load Analysis...", null, OnLoadAnalysis);
		toolStripMenuItem.DropDownItems.Add("&Save Analysis...", null, OnSaveAnalysis);
		toolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
		toolStripMenuItem.DropDownItems.Add("Select &All", null, delegate
		{
			_grid.SelectAll();
		});
		toolStripMenuItem.DropDownItems.Add("Select &None", null, delegate
		{
			_grid.ClearSelection();
		});
		toolStripMenuItem.DropDownItems.Add("&Invert Selection", null, OnInvertSelection);
		toolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
		toolStripMenuItem.DropDownItems.Add("Clear &Selected File(s)", null, OnClearSelected);
		toolStripMenuItem.DropDownItems.Add("Clear &All Files", null, OnClear);
		toolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
		toolStripMenuItem.DropDownItems.Add("E&xit", null, delegate
		{
			Close();
		});
		ToolStripMenuItem toolStripMenuItem2 = new ToolStripMenuItem("&Analysis");
		toolStripMenuItem2.DropDownItems.Add("Analyze File(&s)", null, OnAnalyze);
		toolStripMenuItem2.DropDownItems.Add("Analyze &Album", null, OnAnalyzeAlbum);
		toolStripMenuItem2.DropDownItems.Add(new ToolStripSeparator());
		toolStripMenuItem2.DropDownItems.Add("&Clear Analysis", null, OnClearAnalysis);
		ToolStripMenuItem toolStripMenuItem3 = new ToolStripMenuItem("&Modify Gain");
		toolStripMenuItem3.DropDownItems.Add("Apply &Track Gain", null, OnApplyGain);
		toolStripMenuItem3.DropDownItems.Add("Apply &Album Gain", null, OnApplyAlbumGain);
		toolStripMenuItem3.DropDownItems.Add("Apply &Constant Gain...", null, OnApplyConstantGain);
		toolStripMenuItem3.DropDownItems.Add(new ToolStripSeparator());
		toolStripMenuItem3.DropDownItems.Add("&Undo Gain Changes", null, OnUndoGain);
		ToolStripMenuItem toolStripMenuItem4 = new ToolStripMenuItem("&Options");
		_alwaysOnTopItem = new ToolStripMenuItem("&Always on Top")
		{
			CheckOnClick = true,
			Checked = _settings.AlwaysOnTop
		};
		_alwaysOnTopItem.CheckedChanged += delegate
		{
			base.TopMost = _alwaysOnTopItem.Checked;
		};
		toolStripMenuItem4.DropDownItems.Add(_alwaysOnTopItem);
		_addSubfoldersItem = new ToolStripMenuItem("Add &Subfolders")
		{
			CheckOnClick = true,
			Checked = _settings.AddSubfolders
		};
		toolStripMenuItem4.DropDownItems.Add(_addSubfoldersItem);
		_preserveTimestampItem = new ToolStripMenuItem("&Preserve File Timestamp")
		{
			CheckOnClick = true,
			Checked = _settings.PreserveTimestamp
		};
		toolStripMenuItem4.DropDownItems.Add(_preserveTimestampItem);
		toolStripMenuItem4.DropDownItems.Add(new ToolStripSeparator());
		_noLayerCheckItem = new ToolStripMenuItem("&No Check for Layer I or II")
		{
			CheckOnClick = true,
			Checked = _settings.NoLayerCheck
		};
		toolStripMenuItem4.DropDownItems.Add(_noLayerCheckItem);
		_dontClipItem = new ToolStripMenuItem("&Don't Clip when doing Track Gain")
		{
			CheckOnClick = true,
			Checked = _settings.DontClip
		};
		toolStripMenuItem4.DropDownItems.Add(_dontClipItem);
		toolStripMenuItem4.DropDownItems.Add(new ToolStripSeparator());
		ToolStripMenuItem toolStripMenuItem5 = new ToolStripMenuItem("&Tag Options");
		_skipTagsItem = new ToolStripMenuItem("&Ignore (do not read or write tags)")
		{
			CheckOnClick = true,
			Checked = _settings.IgnoreTags
		};
		toolStripMenuItem5.DropDownItems.Add(_skipTagsItem);
		_recalcTagsItem = new ToolStripMenuItem("&Re-calculate (do not read tags)")
		{
			CheckOnClick = true,
			Checked = _settings.RecalcTags
		};
		toolStripMenuItem5.DropDownItems.Add(_recalcTagsItem);
		toolStripMenuItem5.DropDownItems.Add(new ToolStripSeparator());
		toolStripMenuItem5.DropDownItems.Add("Remove &Tags from files", null, OnRemoveTags);
		toolStripMenuItem4.DropDownItems.Add(toolStripMenuItem5);
		toolStripMenuItem4.DropDownItems.Add(new ToolStripSeparator());
		_beepWhenFinishedItem = new ToolStripMenuItem("&Beep When Finished")
		{
			CheckOnClick = true,
			Checked = _settings.BeepWhenFinished
		};
		toolStripMenuItem4.DropDownItems.Add(_beepWhenFinishedItem);
		
		_limitToSingleThreadItem = new ToolStripMenuItem("Limit &Analyzer to single thread")
		{
			CheckOnClick = true,
			Checked = _settings.LimitToSingleThread
		};
		toolStripMenuItem4.DropDownItems.Add(_limitToSingleThreadItem);
		
		toolStripMenuItem4.DropDownItems.Add(new ToolStripSeparator());
		ToolStripMenuItem toolStripMenuItem6 = new ToolStripMenuItem("Filename Displa&y");
		_filenamePathFileItem = new ToolStripMenuItem("Show &Path\\File")
		{
			CheckOnClick = true,
			Checked = (_settings.FilenameDisplayMode == 0)
		};
		_filenamePathFileItem.Click += delegate
		{
			UpdateFilenameDisplay(_filenamePathFileItem);
		};
		_filenameFileOnlyItem = new ToolStripMenuItem("Show File onl&y")
		{
			CheckOnClick = true,
			Checked = (_settings.FilenameDisplayMode == 1)
		};
		_filenameFileOnlyItem.Click += delegate
		{
			UpdateFilenameDisplay(_filenameFileOnlyItem);
		};
		_filenamePathAndFileItem = new ToolStripMenuItem("Show Path && Fi&le (as separate columns)")
		{
			CheckOnClick = true,
			Checked = (_settings.FilenameDisplayMode == 2)
		};
		_filenamePathAndFileItem.Click += delegate
		{
			UpdateFilenameDisplay(_filenamePathAndFileItem);
		};
		toolStripMenuItem6.DropDownItems.Add(_filenamePathFileItem);
		toolStripMenuItem6.DropDownItems.Add(_filenameFileOnlyItem);
		toolStripMenuItem6.DropDownItems.Add(_filenamePathAndFileItem);
		toolStripMenuItem4.DropDownItems.Add(toolStripMenuItem6);
		toolStripMenuItem4.DropDownItems.Add(new ToolStripSeparator());
		toolStripMenuItem4.DropDownItems.Add("&Logs...", null, OnLogs);
		toolStripMenuItem4.DropDownItems.Add(new ToolStripSeparator());
		toolStripMenuItem4.DropDownItems.Add("&Reset Default Column Widths", null, OnResetColumnWidths);
		ToolStripMenuItem toolStripMenuItem7 = new ToolStripMenuItem("&Help");
		toolStripMenuItem7.DropDownItems.Add("&About MP3Gain 2026", null, OnAbout);
		_menuStrip.Items.AddRange(toolStripMenuItem, toolStripMenuItem2, toolStripMenuItem3, toolStripMenuItem4, toolStripMenuItem7);
		foreach (ToolStripMenuItem item in _menuStrip.Items)
		{
			item.ForeColor = TextPrimary;
			if (item.DropDown is ToolStripDropDownMenu toolStripDropDownMenu)
			{
				toolStripDropDownMenu.BackColor = BgPanel;
				toolStripDropDownMenu.ForeColor = TextPrimary;
				toolStripDropDownMenu.Renderer = new DarkToolStripRenderer();
			}
			foreach (ToolStripItem dropDownItem in item.DropDownItems)
			{
				dropDownItem.ForeColor = TextPrimary;
				dropDownItem.BackColor = BgPanel;
				if (!(dropDownItem is ToolStripMenuItem { HasDropDownItems: not false } toolStripMenuItem9))
				{
					continue;
				}
				if (toolStripMenuItem9.DropDown is ToolStripDropDownMenu toolStripDropDownMenu2)
				{
					toolStripDropDownMenu2.BackColor = BgPanel;
					toolStripDropDownMenu2.ForeColor = TextPrimary;
					toolStripDropDownMenu2.Renderer = new DarkToolStripRenderer();
				}
				foreach (ToolStripItem dropDownItem2 in toolStripMenuItem9.DropDownItems)
				{
					dropDownItem2.ForeColor = TextPrimary;
					dropDownItem2.BackColor = BgPanel;
				}
			}
		}
		base.MainMenuStrip = _menuStrip;
		base.Controls.Add(_menuStrip);
	}

	private void SetupToolbar()
	{
		_topPanel = new Panel
		{
			Dock = DockStyle.Top,
			Height = 52,
			BackColor = BgToolbar,
			Padding = new Padding(8, 4, 8, 4)
		};
		_topPanel.Paint += delegate(object? s, PaintEventArgs e)
		{
			using LinearGradientBrush brush = new LinearGradientBrush(new Point(0, _topPanel.Height - 2), new Point(_topPanel.Width, _topPanel.Height - 2), AccentPrimary, AccentSecondary);
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
		AddToolButton("btnAddFiles", "\ud83d\udcc2  Add Files", "Add MP3 files", OnAddFiles);
		AddToolButton("btnAddFolder", "\ud83d\udcc1  Add Folder", "Add folder of MP3 files", OnAddFolder);
		_toolbar.Items.Add(new ToolStripSeparator());
		AddToolButton("btnAnalyze", "\ud83d\udd0d  Analyze File(s)", "Analyze selected files", OnAnalyze);
		AddToolButton("btnAnalyzeAll", "\ud83d\udd0d  Analyze All", "Analyze all files", OnAnalyzeAll);
		AddToolButton("btnClearAnalysis", "\ud83e\uddf9  Clear Analysis", "Clear analysis results", OnClearAnalysis);
		_toolbar.Items.Add(new ToolStripSeparator());
		AddToolButton("btnApplyGain", "\ud83c\udf9a  Apply Gain", "Apply gain for all analyzed files", OnApplyGain);
		_toolbar.Items.Add(new ToolStripSeparator());
		AddToolButton("btnClear", "\ud83d\uddd1  Clear File List", "Clear file list", OnClear);
		_toolbar.Items.Add(new ToolStripSeparator());
		AddToolButton("btnCancel", "⛔  Cancel", "Cancel current operation", OnCancel);
		_topPanel.Controls.Add(_toolbar);
		base.Controls.Add(_topPanel);
		SetToolbarEnabled(enabled: true);
	}

	private void AddToolButton(string name, string text, string tooltip, EventHandler onClick)
	{
		ToolStripButton toolStripButton = new ToolStripButton
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
		toolStripButton.Click += onClick;
		_toolbar.Items.Add(toolStripButton);
	}

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
		LinkLabel linkLabel = new LinkLabel
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
		linkLabel.LinkClicked += delegate
		{
			_targetDbSpinner.Value = 89.0m;
		};
		_targetDbSpinner.ValueChanged += delegate
		{
			double targetDb = (double)_targetDbSpinner.Value;
			foreach (Mp3FileInfo file in _files)
			{
				file.TargetDb = targetDb;
			}
			RefreshGrid();
		};
		_targetPanel.Controls.AddRange(_targetDbLabel, _targetDbSpinner, linkLabel);
		base.Controls.Add(_targetPanel);
	}

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
			RowTemplate = 
			{
				Height = 30
			}
		};
		AddColumn("colStatus", "Status", "Status", 90);
		AddColumn("colFileName", "File Name", "FileName", 200);
		AddColumn("colVolume", "Volume", "Volume", 80);
		AddColumn("colClipping", "Clipping", "Clipping", 80);
		AddColumn("colTrackGain", "Track Gain (dB)", "GainChangeDisplay", 110);
		AddColumn("colTrackPeak", "Track Peak", "TrackPeak", 90);
		AddColumn("colAlbumGain", "Album Gain (dB)", "AlbumGainDisplay", 110);
		AddColumn("colAlbumPeak", "Album Peak", "AlbumPeak", 90);
		AddColumn("colDirectory", "Path", "Directory", 180);
		AddColumn("colFileSize", "Size", "FileSizeFormatted", 70);
		_grid.DataSource = _bindingSource;
		_grid.CellFormatting += Grid_CellFormatting;
		_grid.CellPainting += Grid_CellPainting;
		_grid.KeyDown += Grid_KeyDown;
		base.Controls.Add(_grid);
		_grid.BringToFront();
		int filenameDisplayMode = _settings.FilenameDisplayMode;
		if (1 == 0)
		{
		}
		ToolStripMenuItem activeItem = filenameDisplayMode switch
		{
			1 => _filenameFileOnlyItem, 
			2 => _filenamePathAndFileItem, 
			_ => _filenamePathFileItem, 
		};
		if (1 == 0)
		{
		}
		UpdateFilenameDisplay(activeItem);
	}

	private void AddColumn(string name, string header, string propertyName, int width)
	{
		DataGridViewTextBoxColumn dataGridViewColumn = new DataGridViewTextBoxColumn
		{
			Name = name,
			HeaderText = header,
			DataPropertyName = propertyName,
			Width = width,
			SortMode = DataGridViewColumnSortMode.Automatic
		};
		_grid.Columns.Add(dataGridViewColumn);
	}

	private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
	{
		if (e.ColumnIndex < 0 || e.RowIndex < 0)
		{
			return;
		}
		string name = _grid.Columns[e.ColumnIndex].Name;
		Mp3FileInfo mp3FileInfo = _files[e.RowIndex];
		if ((name == "colTrackGain" || name == "colAlbumGain") && e.Value is double num)
		{
			e.Value = $"{num:+0.0;-0.0;0.0} dB";
			e.FormattingApplied = true;
			if (num > 0.0)
			{
				e.CellStyle.ForeColor = AccentSuccess;
			}
			else if (num < 0.0)
			{
				e.CellStyle.ForeColor = AccentWarning;
			}
		}
		else if (name == "colVolume" && e.Value is double value)
		{
			e.Value = $"{value:0.0}";
			e.FormattingApplied = true;
		}
		else if ((name == "colTrackPeak" || name == "colAlbumPeak") && e.Value is double num2)
		{
			e.Value = $"{num2:F6}";
			e.FormattingApplied = true;
			if (num2 > 1.0)
			{
				e.CellStyle.ForeColor = AccentDanger;
			}
		}
		else if (name == "colStatus" && e.Value is FileStatus fileStatus)
		{
			DataGridViewCellStyle cellStyle = e.CellStyle;
			if (1 == 0)
			{
			}
			Color foreColor;
			switch (fileStatus)
			{
			case FileStatus.Analyzed:
				foreColor = AccentSuccess;
				break;
			case FileStatus.Applied:
				foreColor = AccentPrimary;
				break;
			case FileStatus.Error:
				foreColor = AccentDanger;
				break;
			case FileStatus.Analyzing:
			case FileStatus.Applying:
				foreColor = AccentWarning;
				break;
			default:
				foreColor = TextMuted;
				break;
			}
			if (1 == 0)
			{
			}
			cellStyle.ForeColor = foreColor;
		}
		if ((name == "colFileName" || name == "colClipping") && mp3FileInfo.Clipping == "Y")
		{
			e.CellStyle.ForeColor = AccentDanger;
			e.CellStyle.SelectionForeColor = AccentDanger;
		}
	}

	private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
	{
		if (e.ColumnIndex < 0 || e.RowIndex < 0)
		{
			return;
		}
		string name = _grid.Columns[e.ColumnIndex].Name;
		if (!(name == "colStatus"))
		{
			return;
		}
		Mp3FileInfo mp3FileInfo = _files[e.RowIndex];
		if ((mp3FileInfo.Status != FileStatus.Analyzing && mp3FileInfo.Status != FileStatus.Applying) || mp3FileInfo.Progress < 0)
		{
			return;
		}
		e.PaintBackground(e.CellBounds, cellsPaintSelectionBackground: true);
		int num = 2;
		Rectangle rect = new Rectangle(e.CellBounds.X + num, e.CellBounds.Y + num, (int)((float)(e.CellBounds.Width - num * 2) * ((float)mp3FileInfo.Progress / 100f)), e.CellBounds.Height - num * 2);
		if (rect.Width > 0)
		{
			using SolidBrush brush = new SolidBrush(Color.FromArgb(40, AccentPrimary));
			e.Graphics.FillRectangle(brush, rect);
		}
		e.PaintContent(e.CellBounds);
		e.Handled = true;
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
		_fileProgressLabel = new ToolStripStatusLabel
		{
			Text = "File Progress:",
			Visible = false
		};
		_fileProgressBar = new ToolStripProgressBar
		{
			Visible = false,
			Width = 100,
			Style = ProgressBarStyle.Marquee,
			MarqueeAnimationSpeed = 30
		};
		_totalProgressLabel = new ToolStripStatusLabel
		{
			Text = "Total Progress:",
			Visible = false,
			Margin = new Padding(10, 0, 0, 0)
		};
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
		_statusBar.Items.AddRange(_statusLabel, _fileProgressLabel, _fileProgressBar, _totalProgressLabel, _totalProgressBar, _fileCountLabel);
		base.Controls.Add(_statusBar);
	}

	private void SetupDragDrop()
	{
		AllowDrop = true;
		base.DragEnter += delegate(object? s, DragEventArgs e)
		{
			IDataObject? data = e.Data;
			if (data != null && data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.Copy;
			}
		};
		base.DragDrop += delegate(object? s, DragEventArgs e)
		{
			if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
			{
				AddPaths(paths);
				BeginInvoke(delegate
				{
					_bindingSource.ResetBindings(metadataChanged: false);
					_grid.DataSource = null;
					_grid.DataSource = _bindingSource;
					_grid.Refresh();
				});
			}
		};
	}

	private void AddPaths(string[] paths)
	{
		int count = _files.Count;
		foreach (string path in paths)
		{
			if (Directory.Exists(path))
			{
				try
				{
					SearchOption searchOption = (_addSubfoldersItem.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
					string[] files = Directory.GetFiles(path, "*.mp3", searchOption);
					string[] array = files;
					foreach (string path2 in array)
					{
						AddFileIfNew(path2);
					}
				}
				catch
				{
				}
			}
			else if (File.Exists(path))
			{
				bool flag = AddFileIfNew(path);
			}
		}
		int num = _files.Count - count;
		_statusLabel.Text = ((num > 0) ? $"{num} file(s) loaded" : "No new files loaded");
		_statusLabel.ForeColor = TextSecondary;
		UpdateStatus();
	}

	private bool AddFileIfNew(string path)
	{
		string fullPath = Path.GetFullPath(path);
		if (_files.Any((Mp3FileInfo f) => f.FilePath.Equals(fullPath, StringComparison.OrdinalIgnoreCase)))
		{
			return false;
		}
		_files.Add(Mp3FileInfo.FromPath(fullPath));
		return true;
	}

	private void RemoveSelectedFiles()
	{
		List<Mp3FileInfo> list = (from DataGridViewRow r in _grid.SelectedRows
			select r.DataBoundItem as Mp3FileInfo into f
			where f != null
			select f).ToList();
		foreach (Mp3FileInfo item in list)
		{
			_files.Remove(item);
		}
		UpdateStatus();
	}

	private void UpdateStatus()
	{
		int count = _files.Count;
		int value = _files.Count((Mp3FileInfo f) => f.Status == FileStatus.Analyzed || f.Status == FileStatus.Applied);
		_fileCountLabel.Text = $"{count} file{((count != 1) ? "s" : "")}  |  {value} analyzed";
	}

	private void OnAddFiles(object? sender, EventArgs e)
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Add MP3 Files",
			Filter = "MP3 Files (*.mp3)|*.mp3|All Files (*.*)|*.*",
			Multiselect = true,
			RestoreDirectory = true
		};
		if (openFileDialog.ShowDialog() == DialogResult.OK)
		{
			AddPaths(openFileDialog.FileNames);
			BeginInvoke(delegate
			{
				_bindingSource.ResetBindings(metadataChanged: false);
				_grid.DataSource = null;
				_grid.DataSource = _bindingSource;
				_grid.Refresh();
			});
		}
	}

	private void OnAddFolder(object? sender, EventArgs e)
	{
		using FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
		{
			Description = "Select folder containing MP3 files",
			ShowNewFolderButton = false
		};
		if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
		{
			AddPaths(new string[1] { folderBrowserDialog.SelectedPath });
		}
	}

	private async void OnAnalyze(object? sender, EventArgs e)
	{
		if (_engine == null || _isProcessing)
		{
			if (_engine == null)
			{
				MessageBox.Show("The native MP3Gain engine failed to initialize. Please check the error log or console.", "Engine Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			return;
		}
		List<Mp3FileInfo> targets = GetSelectedOrAllFiles();
		if (targets.Count == 0)
		{
			return;
		}
		await ProcessFilesAsync("Analyzing", targets, async delegate(Mp3FileInfo file, CancellationToken ct)
		{
			file.Status = FileStatus.Analyzing;
			BeginInvoke(delegate
			{
				RefreshGrid();
			});
			Progress<int> progress = new Progress<int>(delegate(int pct)
			{
				file.Progress = pct;
				RefreshGrid();
			});
			AnalysisResult result = await _engine.AnalyzeFileAsync(file.FilePath, _settings.IgnoreTags, _settings.RecalcTags, progress, ct);
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
		if (_engine == null || _isProcessing)
		{
			if (_engine == null)
			{
				MessageBox.Show("The native MP3Gain engine failed to initialize. Please check the error log or console.", "Engine Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			return;
		}
		List<Mp3FileInfo> targets = _files.Where((Mp3FileInfo f) => f.Status != FileStatus.Analyzed).ToList();
		int skipped = _files.Count - targets.Count;
		if (targets.Count == 0 && skipped > 0)
		{
			_statusLabel.Text = $"Analysis complete: 0 analyzed, {skipped} skipped";
		}
		else
		{
			if (targets.Count == 0)
			{
				return;
			}
			await ProcessFilesAsync("Analyzing All", targets, async delegate(Mp3FileInfo file, CancellationToken ct)
			{
				file.Status = FileStatus.Analyzing;
				BeginInvoke(delegate
				{
					RefreshGrid();
				});
				Progress<int> progress = new Progress<int>(delegate(int pct)
				{
					file.Progress = pct;
					RefreshGrid();
				});
				AnalysisResult result = await _engine.AnalyzeFileAsync(file.FilePath, _settings.IgnoreTags, _settings.RecalcTags, progress, ct);
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
			int succesfulAnalyzed = targets.Count((Mp3FileInfo f) => f.Status == FileStatus.Analyzed);
			int totalErrors = targets.Count((Mp3FileInfo f) => f.Status == FileStatus.Error);
			if (totalErrors > 0)
			{
				_statusLabel.Text = $"Analysis complete: {succesfulAnalyzed} analyzed, {skipped} skipped, {totalErrors} error(s)";
			}
			else
			{
				_statusLabel.Text = $"Analysis complete: {succesfulAnalyzed} analyzed, {skipped} skipped";
			}
			_statusLabel.ForeColor = ((totalErrors > 0) ? AccentWarning : TextSecondary);
		}
	}

	private async void OnAnalyzeAlbum(object? sender, EventArgs e)
	{
		if (_engine == null || _isProcessing)
		{
			if (_engine == null)
			{
				MessageBox.Show("The native MP3Gain engine failed to initialize. Please check the error log or console.", "Engine Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			return;
		}
		List<Mp3FileInfo> targets = _files.ToList();
		if (targets.Count == 0)
		{
			return;
		}
		await ProcessFilesAsync("Album Analyzing", targets, async delegate(Mp3FileInfo file, CancellationToken ct)
		{
			file.Status = FileStatus.Analyzing;
			BeginInvoke(delegate
			{
				RefreshGrid();
			});
			Progress<int> progress = new Progress<int>(delegate(int pct)
			{
				file.Progress = pct;
				RefreshGrid();
			});
			AnalysisResult result = await _engine.AnalyzeFileAsync(file.FilePath, _settings.IgnoreTags, _settings.RecalcTags, progress, ct);
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
		}, forceSequential: true);
	}

	private async void OnApplyGain(object? sender, EventArgs e)
	{
		if (_engine == null || _isProcessing)
		{
			if (_engine == null)
			{
				MessageBox.Show("The native MP3Gain engine failed to initialize. Please check the error log or console.", "Engine Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			return;
		}
		List<Mp3FileInfo> targets = _files.ToList();
		if (targets.Count == 0)
		{
			MessageBox.Show("No files to apply gain to.\nPlease add files first.", "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			return;
		}
		double targetDb = (double)_targetDbSpinner.Value;
		await ProcessFilesAsync("Applying Gain", targets, async delegate(Mp3FileInfo file, CancellationToken ct)
		{
			if (file.Status != FileStatus.Analyzed && file.Status != FileStatus.Applying)
			{
				file.Status = FileStatus.Analyzing;
				BeginInvoke(delegate
				{
					RefreshGrid();
				});
				Progress<int> progress = new Progress<int>(delegate(int pct)
				{
					file.Progress = pct;
					RefreshGrid();
				});
				AnalysisResult preAnalysis = await _engine.AnalyzeFileAsync(file.FilePath, _settings.IgnoreTags, _settings.RecalcTags, progress, ct);
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
			double gainDiff = targetDb - 89.0 + file.TrackGain.Value;
			int gainChange = (int)Math.Round(gainDiff / 1.50514997831991);
			if (gainChange == 0)
			{
				file.Status = FileStatus.Applied;
			}
			else
			{
				file.Status = FileStatus.Applying;
				BeginInvoke(delegate
				{
					RefreshGrid();
				});
				if (gainChange != 0)
				{
					await _engine.ApplyGainAsync(file.FilePath, gainChange, _settings.PreserveTimestamp, ct);
					file.UndoLeft -= gainChange;
					file.UndoRight -= gainChange;
					file.HasUndo = true;
					file.TrackGain -= (double)gainChange * 1.50514997831991;
					file.TrackPeak *= Math.Pow(10.0, (double)gainChange * 1.50514997831991 / 20.0);
					if (file.AlbumGain.HasValue)
					{
						file.AlbumGain -= (double)gainChange * 1.50514997831991;
					}
					if (file.AlbumPeak.HasValue)
					{
						file.AlbumPeak *= Math.Pow(10.0, (double)gainChange * 1.50514997831991 / 20.0);
					}
				}
				if (!_settings.IgnoreTags)
				{
					await _engine.WriteTagsAsync(file.FilePath, file, targetDb, _settings.PreserveTimestamp, ct);
				}
				file.Status = FileStatus.Applied;
			}
		}, forceSequential: true);
	}

	private async void OnApplyAlbumGain(object? sender, EventArgs e)
	{
		if (_engine == null || _isProcessing)
		{
			if (_engine == null)
			{
				MessageBox.Show("The native MP3Gain engine failed to initialize. Please check the error log or console.", "Engine Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			return;
		}
		List<Mp3FileInfo> targets = (from f in GetSelectedOrAllFiles()
			where f.Status == FileStatus.Analyzed
			select f).ToList();
		if (targets.Count == 0)
		{
			MessageBox.Show("No analyzed files to apply gain to.\nAnalyze files first.", "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			return;
		}
		List<Mp3FileInfo> missingAlbumGain = targets.Where((Mp3FileInfo f) => !f.AlbumGain.HasValue).ToList();
		if (missingAlbumGain.Count > 0)
		{
			MessageBox.Show("Some files have not been analyzed together as an album yet.\nPlease run 'Analyze Album' first.", "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		double targetDb = (double)_targetDbSpinner.Value;
		await ProcessFilesAsync("Applying Album Gain", targets, async delegate(Mp3FileInfo file, CancellationToken ct)
		{
			file.Status = FileStatus.Applying;
			BeginInvoke(delegate
			{
				RefreshGrid();
			});
			double albumDbChange = targetDb - 89.0 + file.AlbumGain.Value;
			int gainChange = (int)Math.Round(albumDbChange / 1.50514997831991);
			if (gainChange != 0)
			{
				await _engine.ApplyGainAsync(file.FilePath, gainChange, _settings.PreserveTimestamp, ct);
				file.UndoLeft -= gainChange;
				file.UndoRight -= gainChange;
				file.HasUndo = true;
				file.TrackGain -= (double)gainChange * 1.50514997831991;
				file.TrackPeak *= Math.Pow(10.0, (double)gainChange * 1.50514997831991 / 20.0);
				file.AlbumGain -= (double)gainChange * 1.50514997831991;
				file.AlbumPeak *= Math.Pow(10.0, (double)gainChange * 1.50514997831991 / 20.0);
			}
			if (!_settings.IgnoreTags)
			{
				await _engine.WriteTagsAsync(file.FilePath, file, targetDb, _settings.PreserveTimestamp, ct);
			}
			file.Status = FileStatus.Applied;
		}, forceSequential: true);
	}

	private async void OnApplyConstantGain(object? sender, EventArgs e)
	{
		if (_engine == null || _isProcessing)
		{
			if (_engine == null)
			{
				MessageBox.Show("The native MP3Gain engine failed to initialize. Please check the error log or console.", "Engine Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			return;
		}
		List<Mp3FileInfo> targets = GetSelectedOrAllFiles();
		if (targets.Count == 0)
		{
			MessageBox.Show("No files selected.", "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			return;
		}
		using ConstantGainForm dlg = new ConstantGainForm();
		if (dlg.ShowDialog() != DialogResult.OK)
		{
			return;
		}
		int steps = dlg.GainSteps;
		if (steps == 0)
		{
			return;
		}
		if (dlg.ChannelMode != 0)
		{
			MessageBox.Show("Channel-specific gain modification is not yet supported by the modern MP3Gain 2026 engine.\n\nThe gain will be applied equally to both channels.", "Feature Unsupported", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
		await ProcessFilesAsync("Applying Constant Gain", targets, async delegate(Mp3FileInfo file, CancellationToken ct)
		{
			file.Status = FileStatus.Applying;
			BeginInvoke(delegate
			{
				RefreshGrid();
			});
			if (steps != 0)
			{
				await _engine.ApplyGainAsync(file.FilePath, steps, _settings.PreserveTimestamp, ct);
				file.UndoLeft -= steps;
				file.UndoRight -= steps;
				file.HasUndo = true;
				if (file.TrackGain.HasValue)
				{
					file.TrackGain -= (double)steps * 1.50514997831991;
				}
				if (file.TrackPeak.HasValue)
				{
					file.TrackPeak *= Math.Pow(10.0, (double)steps * 1.50514997831991 / 20.0);
				}
				if (file.AlbumGain.HasValue)
				{
					file.AlbumGain -= (double)steps * 1.50514997831991;
				}
				if (file.AlbumPeak.HasValue)
				{
					file.AlbumPeak *= Math.Pow(10.0, (double)steps * 1.50514997831991 / 20.0);
				}
				double targetDb = (double)_targetDbSpinner.Value;
				if (!_settings.IgnoreTags)
				{
					await _engine.WriteTagsAsync(file.FilePath, file, targetDb, _settings.PreserveTimestamp, ct);
				}
			}
			file.Status = FileStatus.Applied;
		}, forceSequential: true);
	}

	private async void OnUndoGain(object? sender, EventArgs e)
	{
		if (_engine == null || _isProcessing)
		{
			if (_engine == null)
			{
				MessageBox.Show("The native MP3Gain engine failed to initialize. Please check the error log or console.", "Engine Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			return;
		}
		List<Mp3FileInfo> targets = GetSelectedOrAllFiles();
		if (targets.Count == 0)
		{
			MessageBox.Show("No files selected.", "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			return;
		}
		DialogResult result = MessageBox.Show($"Undo gain changes on {targets.Count} file(s)?", "Confirm Undo", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
		if (result != DialogResult.Yes)
		{
			return;
		}
		await ProcessFilesAsync("Undoing Gain", targets, async delegate(Mp3FileInfo file, CancellationToken ct)
		{
			file.Status = FileStatus.Applying;
			BeginInvoke(delegate
			{
				RefreshGrid();
			});
			try
			{
				await _engine.UndoGainAsync(file.FilePath, preserveTimestamp: true, ct);
			}
			catch (Mp3GainException ex) when (ex.Message.Contains("No undo information available"))
			{
				MessageBox.Show("UNDO data not found for " + file.FileName + ". Skip?", "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			if (!_settings.IgnoreTags)
			{
				await _engine.RemoveTagsAsync(file.FilePath, _settings.PreserveTimestamp, ct);
			}
			Progress<int> progress = new Progress<int>(delegate(int pct)
			{
				file.Progress = pct;
				RefreshGrid();
			});
			AnalysisResult analysisResult = await _engine.AnalyzeFileAsync(file.FilePath, _settings.IgnoreTags, _settings.RecalcTags, progress, ct);
			file.TrackGain = analysisResult.TrackGain;
			file.TrackPeak = analysisResult.TrackPeak;
			file.AlbumGain = analysisResult.AlbumGain;
			file.AlbumPeak = analysisResult.AlbumPeak;
			file.MinGlobalGain = analysisResult.MinGlobalGain;
			file.MaxGlobalGain = analysisResult.MaxGlobalGain;
			file.MaxNoClipGain = analysisResult.MaxNoClipGain;
			file.HasUndo = false;
			file.UndoLeft = 0;
			file.UndoRight = 0;
			file.Status = FileStatus.Analyzed;
			double targetDb = (double)_targetDbSpinner.Value;
			if (!_settings.IgnoreTags)
			{
				await _engine.WriteTagsAsync(file.FilePath, file, targetDb, _settings.PreserveTimestamp, ct);
			}
		}, forceSequential: true);
	}

	private async void OnRemoveTags(object? sender, EventArgs e)
	{
		if (_engine == null || _isProcessing)
		{
			if (_engine == null)
			{
				MessageBox.Show("The native MP3Gain engine failed to initialize. Please check the error log or console.", "Engine Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			return;
		}
		List<Mp3FileInfo> targets = GetSelectedOrAllFiles().ToList();
		if (targets.Count == 0)
		{
			return;
		}
		DialogResult result = MessageBox.Show($"Remove ReplayGain tags from {targets.Count} file(s)?", "Confirm Tag Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
		if (result == DialogResult.Yes)
		{
			await ProcessFilesAsync("Removing Tags", targets, async delegate(Mp3FileInfo file, CancellationToken ct)
			{
				await _engine.RemoveTagsAsync(file.FilePath, preserveTimestamp: true, ct);
				file.HasExistingTags = false;
				file.HasUndo = false;
				file.TrackGain = null;
				file.TrackPeak = null;
				file.AlbumGain = null;
				file.AlbumPeak = null;
				file.Status = FileStatus.Pending;
			}, forceSequential: true);
		}
	}

	private void OnClear(object? sender, EventArgs e)
	{
		if (!_isProcessing)
		{
			_files.Clear();
			_statusLabel.Text = "";
			UpdateStatus();
		}
	}

	private void OnClearAnalysis(object? sender, EventArgs e)
	{
		if (_isProcessing)
		{
			return;
		}
		foreach (Mp3FileInfo file in _files)
		{
			file.ClearAnalysis();
		}
		RefreshGrid();
		UpdateStatus();
	}

	private void UpdateFilenameDisplay(ToolStripMenuItem activeItem)
	{
		_filenamePathFileItem.Checked = activeItem == _filenamePathFileItem;
		_filenameFileOnlyItem.Checked = activeItem == _filenameFileOnlyItem;
		_filenamePathAndFileItem.Checked = activeItem == _filenamePathAndFileItem;
		if (_grid.Columns.Contains("colFileName") && _grid.Columns.Contains("colDirectory"))
		{
			DataGridViewColumn dataGridViewColumn = _grid.Columns["colFileName"];
			DataGridViewColumn dataGridViewColumn2 = _grid.Columns["colDirectory"];
			if (_filenamePathFileItem.Checked)
			{
				dataGridViewColumn.DataPropertyName = "FilePath";
				dataGridViewColumn.HeaderText = "Path\\File";
				dataGridViewColumn2.Visible = false;
			}
			else if (_filenameFileOnlyItem.Checked)
			{
				dataGridViewColumn.DataPropertyName = "FileName";
				dataGridViewColumn.HeaderText = "File";
				dataGridViewColumn2.Visible = false;
			}
			else if (_filenamePathAndFileItem.Checked)
			{
				dataGridViewColumn.DataPropertyName = "FileName";
				dataGridViewColumn.HeaderText = "File";
				dataGridViewColumn2.Visible = true;
			}
			_grid.Invalidate();
		}
	}

	private void OnLogs(object? sender, EventArgs e)
	{
		using LogOptionsForm logOptionsForm = new LogOptionsForm
		{
			ErrorLogPath = _settings.ErrorLogPath,
			AnalysisLogPath = _settings.AnalysisLogPath,
			ChangeLogPath = _settings.ChangeLogPath
		};
		if (logOptionsForm.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		_settings.ErrorLogPath = logOptionsForm.ErrorLogPath;
		_settings.AnalysisLogPath = logOptionsForm.AnalysisLogPath;
		_settings.ChangeLogPath = logOptionsForm.ChangeLogPath;
		string[] array = new string[3] { _settings.ErrorLogPath, _settings.AnalysisLogPath, _settings.ChangeLogPath };
		foreach (string text in array)
		{
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			string directoryName = Path.GetDirectoryName(text);
			if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
			{
				try
				{
					Directory.CreateDirectory(directoryName);
				}
				catch
				{
				}
			}
		}
	}

	private void OnResetColumnWidths(object? sender, EventArgs e)
	{
		int[] array = new int[10] { 90, 200, 80, 80, 110, 90, 110, 90, 180, 70 };
		for (int i = 0; i < _grid.Columns.Count && i < array.Length; i++)
		{
			_grid.Columns[i].Width = array[i];
		}
	}

	private void OnClearSelected(object? sender, EventArgs e)
	{
		if (_isProcessing)
		{
			return;
		}
		List<Mp3FileInfo> list = (from DataGridViewRow r in _grid.SelectedRows
			select r.DataBoundItem as Mp3FileInfo into f
			where f != null
			select f).ToList();
		foreach (Mp3FileInfo item in list)
		{
			_files.Remove(item);
		}
		UpdateStatus();
	}

	private void OnCancel(object? sender, EventArgs e)
	{
		_cts?.Cancel();
	}

	private void OnAbout(object? sender, EventArgs e)
	{
		using AboutForm aboutForm = new AboutForm(_engine?.GetVersion() ?? "N/A");
		aboutForm.ShowDialog(this);
	}

	private void OnInvertSelection(object? sender, EventArgs e)
	{
		foreach (DataGridViewRow item in (IEnumerable)_grid.Rows)
		{
			item.Selected = !item.Selected;
		}
	}

	private void OnSaveAnalysis(object? sender, EventArgs e)
	{
		if (_files.Count == 0)
		{
			return;
		}
		using SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Title = "Save Analysis",
			Filter = "MP3Gain Analysis (*.m3g)|*.m3g|Tab-Separated Values (*.tsv)|*.tsv|All Files (*.*)|*.*",
			DefaultExt = "m3g",
			FileName = "mp3gain_analysis.m3g"
		};
		if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		using StreamWriter streamWriter = new StreamWriter(saveFileDialog.FileName);
		if (saveFileDialog.FileName.EndsWith(".m3g", StringComparison.OrdinalIgnoreCase))
		{
			foreach (Mp3FileInfo file in _files)
			{
				string value = file.Directory + "\\";
				double value2 = file.TrackPeak.GetValueOrDefault() * 32768.0;
				string value3 = (file.AlbumGain.HasValue ? file.AlbumGain.Value.ToString() : "\"?\"");
				string value4 = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				streamWriter.WriteLine($"\"{value}\",\"{file.FileName}\",#{value4}#,{file.FileSize},{value2:F3},{file.TrackGain},{value3}");
			}
		}
		else
		{
			streamWriter.WriteLine("FilePath\tTrackGain\tTrackPeak\tAlbumGain\tAlbumPeak\tVolume\tClipping\tStatus");
			foreach (Mp3FileInfo file2 in _files)
			{
				streamWriter.WriteLine($"{file2.FilePath}\t{file2.TrackGain}\t{file2.TrackPeak}\t{file2.AlbumGain}\t{file2.AlbumPeak}\t{file2.Volume}\t{file2.Clipping}\t{file2.Status}");
			}
		}
		_statusLabel.Text = "Analysis saved to " + Path.GetFileName(saveFileDialog.FileName);
		_statusLabel.ForeColor = TextSecondary;
	}

	private void OnLoadAnalysis(object? sender, EventArgs e)
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Load Analysis",
			Filter = "MP3Gain Analysis (*.m3g)|*.m3g|Tab-Separated Values (*.tsv)|*.tsv|All Files (*.*)|*.*",
			DefaultExt = "m3g"
		};
		if (openFileDialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		try
		{
			string[] array = File.ReadAllLines(openFileDialog.FileName);
			if (array.Length == 0)
			{
				return;
			}
			bool flag = array[0].StartsWith("\"");
			_files.Clear();
			if (flag)
			{
				string[] array2 = array;
				foreach (string text in array2)
				{
					if (string.IsNullOrWhiteSpace(text))
					{
						continue;
					}
					List<string> list = ParseLegacyCsvLine(text);
					if (list.Count >= 7)
					{
						string path = list[0].TrimEnd('\\');
						string path2 = list[1];
						string path3 = Path.Combine(path, path2);
						Mp3FileInfo mp3FileInfo = Mp3FileInfo.FromPath(path3);
						if (long.TryParse(list[3], out var result))
						{
							mp3FileInfo.FileSize = result;
						}
						if (double.TryParse(list[4], out var result2))
						{
							mp3FileInfo.TrackPeak = result2 / 32768.0;
						}
						if (double.TryParse(list[5], out var result3))
						{
							mp3FileInfo.TrackGain = result3;
						}
						if (list[6] != "?" && double.TryParse(list[6], out var result4))
						{
							mp3FileInfo.AlbumGain = result4;
						}
						mp3FileInfo.Status = (mp3FileInfo.TrackGain.HasValue ? FileStatus.Analyzed : FileStatus.Pending);
						_files.Add(mp3FileInfo);
					}
				}
			}
			else
			{
				for (int j = 1; j < array.Length; j++)
				{
					string[] array3 = array[j].Split('\t');
					if (array3.Length >= 1 && !string.IsNullOrWhiteSpace(array3[0]))
					{
						Mp3FileInfo mp3FileInfo2 = Mp3FileInfo.FromPath(array3[0]);
						if (array3.Length > 1 && double.TryParse(array3[1], out var result5))
						{
							mp3FileInfo2.TrackGain = result5;
						}
						if (array3.Length > 2 && double.TryParse(array3[2], out var result6))
						{
							mp3FileInfo2.TrackPeak = result6;
						}
						if (array3.Length > 3 && double.TryParse(array3[3], out var result7))
						{
							mp3FileInfo2.AlbumGain = result7;
						}
						if (array3.Length > 4 && double.TryParse(array3[4], out var result8))
						{
							mp3FileInfo2.AlbumPeak = result8;
						}
						if (array3.Length > 7 && Enum.TryParse<FileStatus>(array3[7], out var result9))
						{
							mp3FileInfo2.Status = result9;
						}
						_files.Add(mp3FileInfo2);
					}
				}
			}
			RefreshGrid();
			UpdateStatus();
			_statusLabel.Text = $"Loaded analysis from {Path.GetFileName(openFileDialog.FileName)}  ({_files.Count} files)";
			_statusLabel.ForeColor = TextSecondary;
		}
		catch (Exception ex)
		{
			MessageBox.Show("Error loading analysis:\n" + ex.Message, "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private static List<string> ParseLegacyCsvLine(string line)
	{
		List<string> list = new List<string>();
		int num = 0;
		while (num < line.Length)
		{
			if (line[num] == '"')
			{
				int num2 = line.IndexOf('"', num + 1);
				if (num2 < 0)
				{
					num2 = line.Length;
				}
				list.Add(line.Substring(num + 1, num2 - num - 1));
				num = num2 + 1;
				if (num < line.Length && line[num] == ',')
				{
					num++;
				}
			}
			else if (line[num] == '#')
			{
				int num3 = line.IndexOf('#', num + 1);
				if (num3 < 0)
				{
					num3 = line.Length;
				}
				list.Add(line.Substring(num + 1, num3 - num - 1));
				num = num3 + 1;
				if (num < line.Length && line[num] == ',')
				{
					num++;
				}
			}
			else if (line[num] == ',')
			{
				list.Add("");
				num++;
			}
			else
			{
				int num4 = line.IndexOf(',', num);
				if (num4 < 0)
				{
					num4 = line.Length;
				}
				list.Add(line.Substring(num, num4 - num));
				num = num4;
				if (num < line.Length && line[num] == ',')
				{
					num++;
				}
			}
		}
		return list;
	}

	private List<Mp3FileInfo> GetSelectedOrAllFiles()
	{
		if (_grid.SelectedRows.Count > 0 && _grid.SelectedRows.Count < _files.Count)
		{
			return (from DataGridViewRow r in _grid.SelectedRows
				select r.DataBoundItem as Mp3FileInfo into f
				where f != null
				select (f)).ToList();
		}
		return _files.ToList();
	}

	private async Task ProcessFilesAsync(string operationName, List<Mp3FileInfo> targets, Func<Mp3FileInfo, CancellationToken, Task> processFunc, bool forceSequential = false)
	{
		_isProcessing = true;
		_cts = new CancellationTokenSource();
		_fileProgressLabel.Visible = false;
		_fileProgressBar.Visible = false;
		_totalProgressLabel.Visible = true;
		_totalProgressBar.Visible = true;
		_totalProgressBar.Maximum = targets.Count;
		_totalProgressBar.Value = 0;
		SetToolbarEnabled(enabled: false);
		int processed = 0;
		int errors = 0;
		int maxConcurrency = ((forceSequential || _settings.LimitToSingleThread) ? 1 : (_engine?.Concurrency ?? 1));
		try
		{
			await Parallel.ForEachAsync(targets, new ParallelOptions
			{
				MaxDegreeOfParallelism = maxConcurrency,
				CancellationToken = _cts.Token
			}, async delegate(Mp3FileInfo file, CancellationToken ct)
			{
				file.Progress = 0;
				try
				{
					await processFunc(file, ct);
				}
				catch (OperationCanceledException)
				{
					file.Status = FileStatus.Pending;
					throw;
				}
				catch (Exception ex3)
				{
					Exception ex4 = ex3;
					file.Status = FileStatus.Error;
					file.StatusMessage = ex4.Message;
					Interlocked.Increment(ref errors);
					try
					{
						if (!string.IsNullOrWhiteSpace(_settings.ErrorLogPath))
						{
							string _dir = Path.GetDirectoryName(_settings.ErrorLogPath);
							if (!string.IsNullOrWhiteSpace(_dir) && !Directory.Exists(_dir))
							{
								Directory.CreateDirectory(_dir);
							}
							lock (_settings)
							{
								File.AppendAllText(_settings.ErrorLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error on {file.FilePath}: {ex4.Message}{Environment.NewLine}");
							}
						}
					}
					catch
					{
					}
				}
				file.Progress = -1;
				int done = Interlocked.Increment(ref processed);
				BeginInvoke(delegate
				{
					_totalProgressBar.Value = Math.Min(done, targets.Count);
					_statusLabel.Text = $"{operationName}: {done}/{targets.Count}";
					RefreshGrid();
					UpdateStatus();
				});
			});
		}
		catch (OperationCanceledException)
		{
		}
		finally
		{
			_isProcessing = false;
			_totalProgressLabel.Visible = false;
			_totalProgressBar.Visible = false;
			SetToolbarEnabled(enabled: true);
			_cts?.Dispose();
			_cts = null;
			string summary = ((errors > 0) ? $"{operationName} complete: {processed} processed, {errors} error(s)" : $"{operationName} complete: {processed} file(s) processed");
			_statusLabel.Text = summary;
			_statusLabel.ForeColor = ((errors > 0) ? AccentWarning : TextSecondary);
			_bindingSource.ResetBindings(metadataChanged: false);
			RefreshGrid();
			UpdateStatus();
			if (_settings.BeepWhenFinished && targets.Count > 1)
			{
				SystemSounds.Beep.Play();
			}
		}
	}

	private void SetToolbarEnabled(bool enabled)
	{
		foreach (ToolStripItem item in _toolbar.Items)
		{
			if (item.Name == "btnCancel")
			{
				item.Visible = !enabled;
			}
			else
			{
				item.Visible = enabled;
			}
		}
	}

	private void RefreshGrid()
	{
		if (_grid.InvokeRequired)
		{
			_grid.BeginInvoke(delegate
			{
				_grid.Invalidate();
			});
		}
		else
		{
			_grid.Invalidate();
		}
	}

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
		if (_filenamePathAndFileItem.Checked)
		{
			_settings.FilenameDisplayMode = 2;
		}
		else if (_filenameFileOnlyItem.Checked)
		{
			_settings.FilenameDisplayMode = 1;
		}
		else
		{
			_settings.FilenameDisplayMode = 0;
		}
		_settings.TargetVolume = _targetDbSpinner.Value;
		_settings.LimitToSingleThread = _limitToSingleThreadItem.Checked;
		_settings.Save();
		base.OnFormClosing(e);
	}

}
