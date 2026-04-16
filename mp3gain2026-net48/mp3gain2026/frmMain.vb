Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Linq
Imports System.Threading
Imports System.Windows.Forms

Public Class frmMain

    ' ── Theme Colors ──
    Private Shared ReadOnly BgDark As Color = Color.FromArgb(18, 18, 24)
    Private Shared ReadOnly BgPanel As Color = Color.FromArgb(26, 26, 36)
    Private Shared ReadOnly BgCard As Color = Color.FromArgb(32, 33, 46)
    Private Shared ReadOnly BgCardHover As Color = Color.FromArgb(42, 43, 58)
    Private Shared ReadOnly BgToolbar As Color = Color.FromArgb(22, 22, 32)
    Private Shared ReadOnly BorderColor As Color = Color.FromArgb(55, 55, 75)
    Private Shared ReadOnly AccentPrimary As Color = Color.FromArgb(99, 102, 241)   ' Indigo
    Private Shared ReadOnly AccentSecondary As Color = Color.FromArgb(139, 92, 246) ' Violet
    Private Shared ReadOnly AccentSuccess As Color = Color.FromArgb(34, 197, 94)    ' Green
    Private Shared ReadOnly AccentWarning As Color = Color.FromArgb(251, 191, 36)   ' Amber
    Private Shared ReadOnly AccentDanger As Color = Color.FromArgb(239, 68, 68)     ' Red
    Private Shared ReadOnly TextPrimary As Color = Color.FromArgb(237, 237, 245)
    Private Shared ReadOnly TextSecondary As Color = Color.FromArgb(156, 156, 178)
    Private Shared ReadOnly TextMuted As Color = Color.FromArgb(100, 100, 130)

    ' ── Controls ──
    Private _menuStrip As MenuStrip
    Private _toolbar As ToolStrip
    Private _grid As DataGridView
    Private _statusBar As StatusStrip
    Private _statusLabel As ToolStripStatusLabel
    Private _fileProgressLabel As ToolStripStatusLabel
    Private _fileProgressBar As ToolStripProgressBar
    Private _totalProgressLabel As ToolStripStatusLabel
    Private _totalProgressBar As ToolStripProgressBar
    Private _fileCountLabel As ToolStripStatusLabel
    Private _targetDbSpinner As NumericUpDown
    Private _targetDbLabel As Label
    Private _topPanel As Panel
    Private _targetPanel As Panel

    ' ── State ──
    Private ReadOnly _files As New BindingList(Of Mp3FileInfo)()
    Private ReadOnly _bindingSource As New BindingSource()
    Private _engine As Mp3GainEngine
    Private _cts As CancellationTokenSource
    Private _isProcessing As Boolean
    Private _settings As AppSettings

    Private _alwaysOnTopItem As ToolStripMenuItem
    Private _addSubfoldersItem As ToolStripMenuItem
    Private _preserveTimestampItem As ToolStripMenuItem
    Private _noLayerCheckItem As ToolStripMenuItem
    Private _dontClipItem As ToolStripMenuItem
    Private _skipTagsItem As ToolStripMenuItem
    Private _recalcTagsItem As ToolStripMenuItem
    Private _beepWhenFinishedItem As ToolStripMenuItem

    Private _filenamePathFileItem As ToolStripMenuItem
    Private _filenameFileOnlyItem As ToolStripMenuItem
    Private _filenamePathAndFileItem As ToolStripMenuItem

    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        _settings = AppSettings.Load()
        _bindingSource.DataSource = _files
        SetupTheme()
        SetupTargetPanel()
        SetupToolbar()
        SetupMenuStrip()
        SetupGrid()
        SetupStatusBar()
        SetupDragDrop()
        UpdateStatus()

        ' Try to init engine
        Try
            _engine = New Mp3GainEngine()
            Dim ver As String = _engine.GetVersion()
            Me.Text = $"MP3Gain 2026  —  Engine v{ver}"
        Catch ex As Exception
            _statusLabel.Text = $"⚠ Native DLL not loaded: {ex.Message}"
            _statusLabel.ForeColor = AccentWarning
        End Try
    End Sub

    ' ══════════════════════════════════════════════════════════════
    '  THEME & LAYOUT
    ' ══════════════════════════════════════════════════════════════

    Private Sub SetupTheme()
        Me.Text = "MP3Gain 2026"
        Me.Size = New Size(1200, 750)
        Me.MinimumSize = New Size(900, 500)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = BgDark
        Me.ForeColor = TextPrimary
        Me.Font = New Font("Segoe UI", 9.5F, FontStyle.Regular)
        Me.DoubleBuffered = True

        ' Application icon
        Try
            Dim icoPath = Path.Combine(Application.StartupPath, "mp3gain2026.ico")
            If File.Exists(icoPath) Then
                Me.Icon = New Icon(icoPath)
            End If
        Catch
        End Try
    End Sub

    ' ══════════════════════════════════════════════════════════════
    '  MENU STRIP
    ' ══════════════════════════════════════════════════════════════

    Private Sub SetupMenuStrip()
        _menuStrip = New MenuStrip() With {
            .BackColor = BgToolbar,
            .ForeColor = TextPrimary,
            .Renderer = New DarkToolStripRenderer(),
            .Padding = New Padding(4, 2, 0, 2)
        }

        ' ── File ──
        Dim fileMenu As New ToolStripMenuItem("&File")
        fileMenu.DropDownItems.Add("Add &File(s)...", Nothing, AddressOf OnAddFiles)
        fileMenu.DropDownItems.Add("Add Fol&der...", Nothing, AddressOf OnAddFolder)
        fileMenu.DropDownItems.Add(New ToolStripSeparator())
        fileMenu.DropDownItems.Add("&Load Analysis...", Nothing, AddressOf OnLoadAnalysis)
        fileMenu.DropDownItems.Add("&Save Analysis...", Nothing, AddressOf OnSaveAnalysis)
        fileMenu.DropDownItems.Add(New ToolStripSeparator())
        fileMenu.DropDownItems.Add("Select &All", Nothing, Sub(s, ev) _grid.SelectAll())
        fileMenu.DropDownItems.Add("Select &None", Nothing, Sub(s, ev) _grid.ClearSelection())
        fileMenu.DropDownItems.Add("&Invert Selection", Nothing, AddressOf OnInvertSelection)
        fileMenu.DropDownItems.Add(New ToolStripSeparator())
        fileMenu.DropDownItems.Add("Clear &Selected File(s)", Nothing, AddressOf OnClearSelected)
        fileMenu.DropDownItems.Add("Clear &All Files", Nothing, AddressOf OnClear)
        fileMenu.DropDownItems.Add(New ToolStripSeparator())
        fileMenu.DropDownItems.Add("E&xit", Nothing, Sub(s, ev) Close())

        ' ── Analysis ──
        Dim analysisMenu As New ToolStripMenuItem("&Analysis")
        analysisMenu.DropDownItems.Add("Analyze File(&s)", Nothing, AddressOf OnAnalyze)
        analysisMenu.DropDownItems.Add("Analyze &Album", Nothing, AddressOf OnAnalyzeAlbum)
        analysisMenu.DropDownItems.Add(New ToolStripSeparator())
        analysisMenu.DropDownItems.Add("&Clear Analysis", Nothing, AddressOf OnClearAnalysis)

        ' ── Modify Gain ──
        Dim gainMenu As New ToolStripMenuItem("&Modify Gain")
        gainMenu.DropDownItems.Add("Apply &Track Gain", Nothing, AddressOf OnApplyGain)
        gainMenu.DropDownItems.Add("Apply &Album Gain", Nothing, AddressOf OnApplyAlbumGain)
        gainMenu.DropDownItems.Add("Apply &Constant Gain...", Nothing, AddressOf OnApplyConstantGain)
        gainMenu.DropDownItems.Add(New ToolStripSeparator())
        gainMenu.DropDownItems.Add("&Undo Gain Changes", Nothing, AddressOf OnUndoGain)

        ' ── Options ──
        Dim optionsMenu As New ToolStripMenuItem("&Options")

        _alwaysOnTopItem = New ToolStripMenuItem("&Always on Top") With {.CheckOnClick = True, .Checked = _settings.AlwaysOnTop}
        AddHandler _alwaysOnTopItem.CheckedChanged, Sub(s, ev) Me.TopMost = _alwaysOnTopItem.Checked
        optionsMenu.DropDownItems.Add(_alwaysOnTopItem)

        _addSubfoldersItem = New ToolStripMenuItem("Add &Subfolders") With {.CheckOnClick = True, .Checked = _settings.AddSubfolders}
        optionsMenu.DropDownItems.Add(_addSubfoldersItem)

        _preserveTimestampItem = New ToolStripMenuItem("&Preserve File Timestamp") With {.CheckOnClick = True, .Checked = _settings.PreserveTimestamp}
        optionsMenu.DropDownItems.Add(_preserveTimestampItem)

        optionsMenu.DropDownItems.Add(New ToolStripSeparator())

        _noLayerCheckItem = New ToolStripMenuItem("&No Check for Layer I or II") With {.CheckOnClick = True, .Checked = _settings.NoLayerCheck}
        optionsMenu.DropDownItems.Add(_noLayerCheckItem)

        _dontClipItem = New ToolStripMenuItem("&Don't Clip when doing Track Gain") With {.CheckOnClick = True, .Checked = _settings.DontClip}
        optionsMenu.DropDownItems.Add(_dontClipItem)

        optionsMenu.DropDownItems.Add(New ToolStripSeparator())

        Dim tagOptionsItem As New ToolStripMenuItem("&Tag Options")
        _skipTagsItem = New ToolStripMenuItem("&Ignore (do not read or write tags)") With {.CheckOnClick = True, .Checked = _settings.IgnoreTags}
        tagOptionsItem.DropDownItems.Add(_skipTagsItem)
        _recalcTagsItem = New ToolStripMenuItem("&Re-calculate (do not read tags)") With {.CheckOnClick = True, .Checked = _settings.RecalcTags}
        tagOptionsItem.DropDownItems.Add(_recalcTagsItem)
        tagOptionsItem.DropDownItems.Add(New ToolStripSeparator())
        tagOptionsItem.DropDownItems.Add("Remove &Tags from files", Nothing, AddressOf OnRemoveTags)
        optionsMenu.DropDownItems.Add(tagOptionsItem)

        optionsMenu.DropDownItems.Add(New ToolStripSeparator())

        _beepWhenFinishedItem = New ToolStripMenuItem("&Beep When Finished") With {.CheckOnClick = True, .Checked = _settings.BeepWhenFinished}
        optionsMenu.DropDownItems.Add(_beepWhenFinishedItem)

        optionsMenu.DropDownItems.Add(New ToolStripSeparator())

        Dim filenameMenu As New ToolStripMenuItem("Filename Displa&y")

        _filenamePathFileItem = New ToolStripMenuItem("Show &Path\File") With {.CheckOnClick = True, .Checked = (_settings.FilenameDisplayMode = 0)}
        AddHandler _filenamePathFileItem.Click, Sub(s, ev) UpdateFilenameDisplay(_filenamePathFileItem)

        _filenameFileOnlyItem = New ToolStripMenuItem("Show File onl&y") With {.CheckOnClick = True, .Checked = (_settings.FilenameDisplayMode = 1)}
        AddHandler _filenameFileOnlyItem.Click, Sub(s, ev) UpdateFilenameDisplay(_filenameFileOnlyItem)

        _filenamePathAndFileItem = New ToolStripMenuItem("Show Path && Fi&le (as separate columns)") With {.CheckOnClick = True, .Checked = (_settings.FilenameDisplayMode = 2)}
        AddHandler _filenamePathAndFileItem.Click, Sub(s, ev) UpdateFilenameDisplay(_filenamePathAndFileItem)

        filenameMenu.DropDownItems.Add(_filenamePathFileItem)
        filenameMenu.DropDownItems.Add(_filenameFileOnlyItem)
        filenameMenu.DropDownItems.Add(_filenamePathAndFileItem)

        optionsMenu.DropDownItems.Add(filenameMenu)

        optionsMenu.DropDownItems.Add(New ToolStripSeparator())

        optionsMenu.DropDownItems.Add("&Logs...", Nothing, AddressOf OnLogs)

        optionsMenu.DropDownItems.Add(New ToolStripSeparator())

        optionsMenu.DropDownItems.Add("&Reset Default Column Widths", Nothing, AddressOf OnResetColumnWidths)

        ' ── Help ──
        Dim helpMenu As New ToolStripMenuItem("&Help")
        helpMenu.DropDownItems.Add("&About MP3Gain 2026", Nothing, AddressOf OnAbout)

        _menuStrip.Items.AddRange(New ToolStripItem() {fileMenu, analysisMenu, gainMenu, optionsMenu, helpMenu})

        ' Style all dropdown items
        For Each topItem As ToolStripMenuItem In _menuStrip.Items
            topItem.ForeColor = TextPrimary
            Dim dd = TryCast(topItem.DropDown, ToolStripDropDownMenu)
            If dd IsNot Nothing Then
                dd.BackColor = BgPanel
                dd.ForeColor = TextPrimary
                dd.Renderer = New DarkToolStripRenderer()
            End If
            For Each subItem As ToolStripItem In topItem.DropDownItems
                subItem.ForeColor = TextPrimary
                subItem.BackColor = BgPanel
                Dim subMenu = TryCast(subItem, ToolStripMenuItem)
                If subMenu IsNot Nothing AndAlso subMenu.HasDropDownItems Then
                    Dim subDd = TryCast(subMenu.DropDown, ToolStripDropDownMenu)
                    If subDd IsNot Nothing Then
                        subDd.BackColor = BgPanel
                        subDd.ForeColor = TextPrimary
                        subDd.Renderer = New DarkToolStripRenderer()
                    End If
                    For Each nested As ToolStripItem In subMenu.DropDownItems
                        nested.ForeColor = TextPrimary
                        nested.BackColor = BgPanel
                    Next
                End If
            Next
        Next

        Me.MainMenuStrip = _menuStrip
        Me.Controls.Add(_menuStrip)
    End Sub

    ' ══════════════════════════════════════════════════════════════
    '  TOOLBAR
    ' ══════════════════════════════════════════════════════════════

    Private Sub SetupToolbar()
        _topPanel = New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 52,
            .BackColor = BgToolbar,
            .Padding = New Padding(8, 4, 8, 4)
        }
        AddHandler _topPanel.Paint, Sub(s, ev)
                                        ' Bottom border gradient
                                        Using brush As New LinearGradientBrush(
                                            New Point(0, _topPanel.Height - 2),
                                            New Point(_topPanel.Width, _topPanel.Height - 2),
                                            AccentPrimary, AccentSecondary)
                                            ev.Graphics.FillRectangle(brush, 0, _topPanel.Height - 2, _topPanel.Width, 2)
                                        End Using
                                    End Sub

        _toolbar = New ToolStrip() With {
            .Dock = DockStyle.Fill,
            .GripStyle = ToolStripGripStyle.Hidden,
            .BackColor = Color.Transparent,
            .ForeColor = TextPrimary,
            .Renderer = New DarkToolStripRenderer(),
            .AutoSize = False,
            .Height = 44,
            .ImageScalingSize = New Size(20, 20),
            .Padding = New Padding(4, 2, 4, 2)
        }

        AddToolButton("btnAddFiles", "📂  Add Files", "Add MP3 files", AddressOf OnAddFiles)
        AddToolButton("btnAddFolder", "📁  Add Folder", "Add folder of MP3 files", AddressOf OnAddFolder)
        _toolbar.Items.Add(New ToolStripSeparator())

        AddToolButton("btnAnalyze", "🔍  Analyze File(s)", "Analyze selected files", AddressOf OnAnalyze)
        AddToolButton("btnAnalyzeAll", "🔍  Analyze All", "Analyze all files", AddressOf OnAnalyzeAll)
        AddToolButton("btnClearAnalysis", "🧹  Clear Analysis", "Clear analysis results", AddressOf OnClearAnalysis)

        _toolbar.Items.Add(New ToolStripSeparator())

        AddToolButton("btnApplyGain", "🎚  Apply Gain", "Apply gain for all analyzed files", AddressOf OnApplyGain)

        _toolbar.Items.Add(New ToolStripSeparator())
        AddToolButton("btnClear", "🗑  Clear File List", "Clear file list", AddressOf OnClear)
        _toolbar.Items.Add(New ToolStripSeparator())
        AddToolButton("btnCancel", "⛔  Cancel", "Cancel current operation", AddressOf OnCancel)

        _topPanel.Controls.Add(_toolbar)
        Me.Controls.Add(_topPanel)

        SetToolbarEnabled(True)
    End Sub

    Private Sub AddToolButton(name As String, text As String, tooltip As String, onClick As EventHandler)
        Dim btn As New ToolStripButton() With {
            .Name = name,
            .Text = text,
            .ToolTipText = tooltip,
            .DisplayStyle = ToolStripItemDisplayStyle.Text,
            .ForeColor = TextPrimary,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Regular),
            .Padding = New Padding(6, 4, 6, 4),
            .Margin = New Padding(2, 0, 2, 0),
            .AutoSize = True
        }
        AddHandler btn.Click, onClick
        _toolbar.Items.Add(btn)
    End Sub

    ' ══════════════════════════════════════════════════════════════
    '  TARGET DB PANEL
    ' ══════════════════════════════════════════════════════════════

    Private Sub SetupTargetPanel()
        _targetPanel = New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 42,
            .BackColor = BgPanel,
            .Padding = New Padding(12, 6, 12, 6)
        }

        _targetDbLabel = New Label() With {
            .Text = "Target Volume (dB):",
            .ForeColor = TextSecondary,
            .Font = New Font("Segoe UI", 9.5F),
            .AutoSize = True,
            .Location = New Point(12, 11)
        }

        _targetDbSpinner = New NumericUpDown() With {
            .Minimum = 75.0D,
            .Maximum = 105.0D,
            .Value = Math.Min(Math.Max(_settings.TargetVolume, 75.0D), 105.0D),
            .DecimalPlaces = 1,
            .Increment = 0.5D,
            .Width = 80,
            .Location = New Point(160, 7),
            .BackColor = BgCard,
            .ForeColor = TextPrimary,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .BorderStyle = BorderStyle.FixedSingle
        }

        Dim helpLabel As New LinkLabel() With {
            .Text = "ℹ  89.0 dB (default)",
            .LinkColor = TextMuted,
            .ActiveLinkColor = TextPrimary,
            .VisitedLinkColor = TextMuted,
            .LinkBehavior = LinkBehavior.HoverUnderline,
            .Font = New Font("Segoe UI", 8.5F),
            .AutoSize = True,
            .Location = New Point(260, 12)
        }
        AddHandler helpLabel.LinkClicked, Sub(s, ev) _targetDbSpinner.Value = 89.0D

        AddHandler _targetDbSpinner.ValueChanged, Sub(s, ev)
                                                      Dim newTarget As Double = CDbl(_targetDbSpinner.Value)
                                                      For Each file In _files
                                                          file.TargetDb = newTarget
                                                      Next
                                                      RefreshGrid()
                                                  End Sub

        _targetPanel.Controls.AddRange(New Control() {_targetDbLabel, _targetDbSpinner, helpLabel})
        Me.Controls.Add(_targetPanel)
    End Sub

    ' ══════════════════════════════════════════════════════════════
    '  DATA GRID
    ' ══════════════════════════════════════════════════════════════

    Private Sub SetupGrid()
        _grid = New DataGridView() With {
            .Dock = DockStyle.Fill,
            .AutoGenerateColumns = False,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = True,
            .AllowUserToResizeRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = True,
            .ReadOnly = True,
            .RowHeadersVisible = False,
            .BorderStyle = BorderStyle.None,
            .CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            .GridColor = BorderColor,
            .BackgroundColor = BgDark,
            .EnableHeadersVisualStyles = False,
            .ColumnHeadersHeight = 36
        }
        _grid.RowTemplate.Height = 30

        _grid.DefaultCellStyle = New DataGridViewCellStyle() With {
            .BackColor = BgDark,
            .ForeColor = TextPrimary,
            .SelectionBackColor = Color.FromArgb(45, 50, 90),
            .SelectionForeColor = Color.White,
            .Font = New Font("Segoe UI", 9.0F),
            .Padding = New Padding(4, 2, 4, 2)
        }

        _grid.ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle() With {
            .BackColor = BgPanel,
            .ForeColor = TextSecondary,
            .Font = New Font("Segoe UI Semibold", 9.0F),
            .Padding = New Padding(4, 6, 4, 6),
            .Alignment = DataGridViewContentAlignment.MiddleLeft
        }

        _grid.AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle() With {
            .BackColor = BgCard,
            .ForeColor = TextPrimary,
            .SelectionBackColor = Color.FromArgb(45, 50, 90),
            .SelectionForeColor = Color.White
        }

        AddGridColumn("colStatus", "Status", NameOf(Mp3FileInfo.Status), 90)
        AddGridColumn("colFileName", "File Name", NameOf(Mp3FileInfo.FileName), 200)
        AddGridColumn("colVolume", "Volume", NameOf(Mp3FileInfo.Volume), 80)
        AddGridColumn("colClipping", "Clipping", NameOf(Mp3FileInfo.Clipping), 80)
        AddGridColumn("colTrackGain", "Track Gain (dB)", NameOf(Mp3FileInfo.GainChangeDisplay), 110)
        AddGridColumn("colTrackPeak", "Track Peak", NameOf(Mp3FileInfo.TrackPeak), 90)
        AddGridColumn("colAlbumGain", "Album Gain (dB)", NameOf(Mp3FileInfo.AlbumGainDisplay), 110)
        AddGridColumn("colAlbumPeak", "Album Peak", NameOf(Mp3FileInfo.AlbumPeak), 90)
        AddGridColumn("colDirectory", "Path", NameOf(Mp3FileInfo.Directory), 180)
        AddGridColumn("colFileSize", "Size", NameOf(Mp3FileInfo.FileSizeFormatted), 70)

        _grid.DataSource = _bindingSource

        AddHandler _grid.CellFormatting, AddressOf Grid_CellFormatting
        AddHandler _grid.CellPainting, AddressOf Grid_CellPainting
        AddHandler _grid.KeyDown, AddressOf Grid_KeyDown

        Me.Controls.Add(_grid)
        _grid.BringToFront()

        Select Case _settings.FilenameDisplayMode
            Case 1 : UpdateFilenameDisplay(_filenameFileOnlyItem)
            Case 2 : UpdateFilenameDisplay(_filenamePathAndFileItem)
            Case Else : UpdateFilenameDisplay(_filenamePathFileItem)
        End Select
    End Sub

    Private Sub AddGridColumn(name As String, header As String, propertyName As String, width As Integer)
        Dim col As New DataGridViewTextBoxColumn() With {
            .Name = name,
            .HeaderText = header,
            .DataPropertyName = propertyName,
            .Width = width,
            .SortMode = DataGridViewColumnSortMode.Automatic
        }
        _grid.Columns.Add(col)
    End Sub

    Private Sub Grid_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.ColumnIndex < 0 OrElse e.RowIndex < 0 Then Return

        Dim colName As String = _grid.Columns(e.ColumnIndex).Name
        Dim file = _files(e.RowIndex)

        ' Format gain values
        If (colName = "colTrackGain" OrElse colName = "colAlbumGain") AndAlso TypeOf e.Value Is Double Then
            Dim gain As Double = CDbl(e.Value)
            e.Value = $"{gain:+0.0;-0.0;0.0} dB"
            e.FormattingApplied = True

            If gain > 0 Then e.CellStyle.ForeColor = AccentSuccess
            If gain < 0 Then e.CellStyle.ForeColor = AccentWarning
        ElseIf colName = "colVolume" AndAlso TypeOf e.Value Is Double Then
            Dim vol As Double = CDbl(e.Value)
            e.Value = $"{vol:0.0}"
            e.FormattingApplied = True
        ElseIf (colName = "colTrackPeak" OrElse colName = "colAlbumPeak") AndAlso TypeOf e.Value Is Double Then
            Dim peak As Double = CDbl(e.Value)
            e.Value = $"{peak:F6}"
            e.FormattingApplied = True
            If peak > 1.0 Then e.CellStyle.ForeColor = AccentDanger
        ElseIf colName = "colStatus" AndAlso TypeOf e.Value Is FileStatus Then
            Dim status As FileStatus = DirectCast(e.Value, FileStatus)
            Select Case status
                Case FileStatus.Analyzed : e.CellStyle.ForeColor = AccentSuccess
                Case FileStatus.Applied : e.CellStyle.ForeColor = AccentPrimary
                Case FileStatus.Error : e.CellStyle.ForeColor = AccentDanger
                Case FileStatus.Analyzing, FileStatus.Applying : e.CellStyle.ForeColor = AccentWarning
                Case Else : e.CellStyle.ForeColor = TextMuted
            End Select
        End If

        ' Clipping highlights
        If (colName = "colFileName" OrElse colName = "colClipping") AndAlso file.Clipping = "Y" Then
            e.CellStyle.ForeColor = AccentDanger
            e.CellStyle.SelectionForeColor = AccentDanger
        End If
    End Sub

    Private Sub Grid_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.ColumnIndex < 0 OrElse e.RowIndex < 0 Then Return

        Dim colName As String = _grid.Columns(e.ColumnIndex).Name
        If colName = "colStatus" Then
            Dim file = _files(e.RowIndex)
            If (file.Status = FileStatus.Analyzing OrElse file.Status = FileStatus.Applying) AndAlso file.Progress >= 0 Then
                e.PaintBackground(e.CellBounds, True)

                Dim padding As Integer = 2
                Dim progressRect As New Rectangle(
                    e.CellBounds.X + padding,
                    e.CellBounds.Y + padding,
                    CInt((e.CellBounds.Width - padding * 2) * (file.Progress / 100.0F)),
                    e.CellBounds.Height - padding * 2)

                If progressRect.Width > 0 Then
                    Using brush As New SolidBrush(Color.FromArgb(40, AccentPrimary))
                        e.Graphics.FillRectangle(brush, progressRect)
                    End Using
                End If

                e.PaintContent(e.CellBounds)
                e.Handled = True
            End If
        End If
    End Sub

    Private Sub Grid_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Delete AndAlso Not _isProcessing Then
            RemoveSelectedFiles()
            e.Handled = True
        ElseIf e.KeyCode = Keys.A AndAlso e.Control Then
            _grid.SelectAll()
            e.Handled = True
        End If
    End Sub

    ' ══════════════════════════════════════════════════════════════
    '  STATUS BAR
    ' ══════════════════════════════════════════════════════════════

    Private Sub SetupStatusBar()
        _statusBar = New StatusStrip() With {
            .BackColor = BgToolbar,
            .ForeColor = TextSecondary,
            .SizingGrip = True,
            .Font = New Font("Segoe UI", 9.0F)
        }

        _statusLabel = New ToolStripStatusLabel() With {
            .Text = "Ready",
            .ForeColor = TextSecondary,
            .Spring = True,
            .TextAlign = ContentAlignment.MiddleLeft
        }

        _fileProgressLabel = New ToolStripStatusLabel() With {.Text = "File Progress:", .Visible = False}
        _fileProgressBar = New ToolStripProgressBar() With {
            .Visible = False,
            .Width = 100,
            .Style = ProgressBarStyle.Marquee,
            .MarqueeAnimationSpeed = 30
        }

        _totalProgressLabel = New ToolStripStatusLabel() With {.Text = "Total Progress:", .Visible = False, .Margin = New Padding(10, 0, 0, 0)}
        _totalProgressBar = New ToolStripProgressBar() With {
            .Visible = False,
            .Width = 150,
            .Style = ProgressBarStyle.Continuous
        }

        _fileCountLabel = New ToolStripStatusLabel() With {
            .Text = "0 files",
            .ForeColor = TextMuted,
            .Alignment = ToolStripItemAlignment.Right
        }

        _statusBar.Items.AddRange(New ToolStripItem() {_statusLabel, _fileProgressLabel, _fileProgressBar, _totalProgressLabel, _totalProgressBar, _fileCountLabel})
        Me.Controls.Add(_statusBar)
    End Sub

    ' ══════════════════════════════════════════════════════════════
    '  DRAG & DROP
    ' ══════════════════════════════════════════════════════════════

    Private Sub SetupDragDrop()
        Me.AllowDrop = True
        AddHandler Me.DragEnter, Sub(s, ev)
                                     If ev.Data.GetDataPresent(DataFormats.FileDrop) Then
                                         ev.Effect = DragDropEffects.Copy
                                     End If
                                 End Sub
        AddHandler Me.DragDrop, Sub(s, ev)
                                    Dim paths = TryCast(ev.Data.GetData(DataFormats.FileDrop), String())
                                    If paths IsNot Nothing Then
                                        AddPaths(paths)
                                        Me.BeginInvoke(New Action(Sub()
                                                                      _bindingSource.ResetBindings(False)
                                                                      _grid.DataSource = Nothing
                                                                      _grid.DataSource = _bindingSource
                                                                      _grid.Refresh()
                                                                  End Sub))
                                    End If
                                End Sub
    End Sub

    ' ══════════════════════════════════════════════════════════════
    '  FILE MANAGEMENT
    ' ══════════════════════════════════════════════════════════════

    Private Sub AddPaths(paths As String())
        Dim initialCount As Integer = _files.Count

        For Each path In paths
            If System.IO.Directory.Exists(path) Then
                Try
                    Dim searchOpt = If(_addSubfoldersItem.Checked, SearchOption.AllDirectories, SearchOption.TopDirectoryOnly)
                    Dim mp3s = System.IO.Directory.GetFiles(path, "*.mp3", searchOpt)
                    For Each mp3 In mp3s
                        AddFileIfNew(mp3)
                    Next
                Catch
                End Try
            ElseIf File.Exists(path) Then
                AddFileIfNew(path)
            End If
        Next

        Dim addedCount As Integer = _files.Count - initialCount
        _statusLabel.Text = If(addedCount > 0, $"{addedCount} file(s) loaded", "No new files loaded")
        _statusLabel.ForeColor = TextSecondary

        UpdateStatus()
    End Sub

    Private Function AddFileIfNew(path As String) As Boolean
        Dim fullPath As String = IO.Path.GetFullPath(path)
        If _files.Any(Function(f) f.FilePath.Equals(fullPath, StringComparison.OrdinalIgnoreCase)) Then
            Return False
        End If
        _files.Add(Mp3FileInfo.FromPath(fullPath))
        Return True
    End Function

    Private Sub RemoveSelectedFiles()
        Dim toRemove = _grid.SelectedRows.Cast(Of DataGridViewRow)() _
            .Select(Function(r) TryCast(r.DataBoundItem, Mp3FileInfo)) _
            .Where(Function(f) f IsNot Nothing) _
            .ToList()

        For Each file In toRemove
            _files.Remove(file)
        Next

        UpdateStatus()
    End Sub

    Private Sub UpdateStatus()
        Dim total As Integer = _files.Count
        Dim analyzed As Integer = _files.Where(Function(f) f.Status = FileStatus.Analyzed OrElse f.Status = FileStatus.Applied).Count()
        _fileCountLabel.Text = $"{total} file{If(total <> 1, "s", "")}  |  {analyzed} analyzed"
    End Sub

    ' ══════════════════════════════════════════════════════════════
    '  TOOLBAR HANDLERS
    ' ══════════════════════════════════════════════════════════════

    Private Sub OnAddFiles(sender As Object, e As EventArgs)
        Using dlg As New OpenFileDialog() With {
            .Title = "Add MP3 Files",
            .Filter = "MP3 Files (*.mp3)|*.mp3|All Files (*.*)|*.*",
            .Multiselect = True,
            .RestoreDirectory = True
        }
            If dlg.ShowDialog() = DialogResult.OK Then
                AddPaths(dlg.FileNames)
                Me.BeginInvoke(New Action(Sub()
                                             _bindingSource.ResetBindings(False)
                                             _grid.DataSource = Nothing
                                             _grid.DataSource = _bindingSource
                                             _grid.Refresh()
                                         End Sub))
            End If
        End Using
    End Sub

    Private Sub OnAddFolder(sender As Object, e As EventArgs)
        Using dlg As New FolderBrowserDialog() With {
            .Description = "Select folder containing MP3 files",
            .ShowNewFolderButton = False
        }
            If dlg.ShowDialog() = DialogResult.OK Then
                AddPaths(New String() {dlg.SelectedPath})
            End If
        End Using
    End Sub

    Private Async Sub OnAnalyze(sender As Object, e As EventArgs)
        If _engine Is Nothing OrElse _isProcessing Then Return

        Dim targets = GetSelectedOrAllFiles()
        If targets.Count = 0 Then Return

        Await ProcessFilesAsync("Analyzing", targets, Async Function(file, ct)
                                                          file.Status = FileStatus.Analyzing
                                                          RefreshGrid()

                                                          Dim result = Await _engine.AnalyzeFileAsync(file.FilePath, _settings.IgnoreTags, _settings.RecalcTags, ct)

                                                          file.TrackGain = result.TrackGain
                                                          file.TrackPeak = result.TrackPeak
                                                          file.AlbumGain = result.AlbumGain
                                                          file.AlbumPeak = result.AlbumPeak
                                                          file.MinGlobalGain = result.MinGlobalGain
                                                          file.MaxGlobalGain = result.MaxGlobalGain
                                                          file.MaxNoClipGain = result.MaxNoClipGain
                                                          file.HasUndo = result.HasUndo
                                                          file.UndoLeft = result.UndoLeft
                                                          file.UndoRight = result.UndoRight
                                                          file.Status = FileStatus.Analyzed
                                                      End Function)
    End Sub

    Private Async Sub OnAnalyzeAll(sender As Object, e As EventArgs)
        If _engine Is Nothing OrElse _isProcessing Then Return

        Dim targets = _files.Where(Function(f) f.Status <> FileStatus.Analyzed).ToList()
        Dim skipped As Integer = _files.Count - targets.Count

        If targets.Count = 0 AndAlso skipped > 0 Then
            _statusLabel.Text = $"Analysis complete: 0 analyzed, {skipped} skipped"
            Return
        End If
        If targets.Count = 0 Then Return

        Await ProcessFilesAsync("Analyzing All", targets, Async Function(file, ct)
                                                              file.Status = FileStatus.Analyzing
                                                              RefreshGrid()

                                                              Dim result = Await _engine.AnalyzeFileAsync(file.FilePath, _settings.IgnoreTags, _settings.RecalcTags, ct)

                                                              file.TrackGain = result.TrackGain
                                                              file.TrackPeak = result.TrackPeak
                                                              file.AlbumGain = result.AlbumGain
                                                              file.AlbumPeak = result.AlbumPeak
                                                              file.MinGlobalGain = result.MinGlobalGain
                                                              file.MaxGlobalGain = result.MaxGlobalGain
                                                              file.MaxNoClipGain = result.MaxNoClipGain
                                                              file.HasUndo = result.HasUndo
                                                              file.UndoLeft = result.UndoLeft
                                                              file.UndoRight = result.UndoRight
                                                              file.Status = FileStatus.Analyzed
                                                          End Function)

        Dim successfulAnalyzed As Integer = targets.Where(Function(f) f.Status = FileStatus.Analyzed).Count()
        Dim totalErrors As Integer = targets.Where(Function(f) f.Status = FileStatus.Error).Count()

        If totalErrors > 0 Then
            _statusLabel.Text = $"Analysis complete: {successfulAnalyzed} analyzed, {skipped} skipped, {totalErrors} error(s)"
        Else
            _statusLabel.Text = $"Analysis complete: {successfulAnalyzed} analyzed, {skipped} skipped"
        End If

        _statusLabel.ForeColor = If(totalErrors > 0, AccentWarning, TextSecondary)
    End Sub

    Private Async Sub OnAnalyzeAlbum(sender As Object, e As EventArgs)
        If _engine Is Nothing OrElse _isProcessing Then Return

        Dim targets = _files.ToList()
        If targets.Count = 0 Then Return

        Await ProcessFilesAsync("Album Analyzing", targets, Async Function(file, ct)
                                                                file.Status = FileStatus.Analyzing
                                                                RefreshGrid()

                                                                Dim result = Await _engine.AnalyzeFileAsync(file.FilePath, _settings.IgnoreTags, _settings.RecalcTags, ct)

                                                                file.TrackGain = result.TrackGain
                                                                file.TrackPeak = result.TrackPeak
                                                                file.AlbumGain = result.AlbumGain
                                                                file.AlbumPeak = result.AlbumPeak
                                                                file.MinGlobalGain = result.MinGlobalGain
                                                                file.MaxGlobalGain = result.MaxGlobalGain
                                                                file.MaxNoClipGain = result.MaxNoClipGain
                                                                file.HasUndo = result.HasUndo
                                                                file.UndoLeft = result.UndoLeft
                                                                file.UndoRight = result.UndoRight
                                                                file.Status = FileStatus.Analyzed
                                                            End Function)
    End Sub

    Private Async Sub OnApplyGain(sender As Object, e As EventArgs)
        If _engine Is Nothing OrElse _isProcessing Then Return

        Dim targets = _files.ToList()
        If targets.Count = 0 Then
            MessageBox.Show("No files to apply gain to." & vbCrLf & "Please add files first.",
                "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim targetDb As Double = CDbl(_targetDbSpinner.Value)

        Await ProcessFilesAsync("Applying Gain", targets, Async Function(file, ct)
                                                              If file.Status <> FileStatus.Analyzed AndAlso file.Status <> FileStatus.Applying Then
                                                                  file.Status = FileStatus.Analyzing
                                                                  RefreshGrid()

                                                                  Dim preAnalysis = Await _engine.AnalyzeFileAsync(file.FilePath, _settings.IgnoreTags, _settings.RecalcTags, ct)
                                                                  file.TrackGain = preAnalysis.TrackGain
                                                                  file.TrackPeak = preAnalysis.TrackPeak
                                                                  file.AlbumGain = preAnalysis.AlbumGain
                                                                  file.AlbumPeak = preAnalysis.AlbumPeak
                                                                  file.MinGlobalGain = preAnalysis.MinGlobalGain
                                                                  file.MaxGlobalGain = preAnalysis.MaxGlobalGain
                                                                  file.MaxNoClipGain = preAnalysis.MaxNoClipGain
                                                                  file.HasUndo = preAnalysis.HasUndo
                                                                  file.UndoLeft = preAnalysis.UndoLeft
                                                                  file.UndoRight = preAnalysis.UndoRight
                                                              End If

                                                              ' Calculate exact steps needed
                                                              Dim gainDiff As Double = targetDb - 89.0 + file.TrackGain.Value
                                                              Dim gainChange As Integer = CInt(Math.Round(gainDiff / 1.50514997831991)) ' MP3Gain step

                                                              ' Skip files that need no change
                                                              If gainChange = 0 Then
                                                                  file.Status = FileStatus.Applied
                                                                  Return
                                                              End If

                                                              file.Status = FileStatus.Applying
                                                              RefreshGrid()

                                                              If gainChange <> 0 Then
                                                                  Await _engine.ApplyGainAsync(file.FilePath, gainChange, _settings.PreserveTimestamp, ct)

                                                                  file.UndoLeft -= gainChange
                                                                  file.UndoRight -= gainChange
                                                                  file.HasUndo = True

                                                                  file.TrackGain -= gainChange * 1.50514997831991
                                                                  file.TrackPeak *= Math.Pow(10.0, (gainChange * 1.50514997831991) / 20.0)
                                                                  If file.AlbumGain.HasValue Then file.AlbumGain -= gainChange * 1.50514997831991
                                                                  If file.AlbumPeak.HasValue Then file.AlbumPeak *= Math.Pow(10.0, (gainChange * 1.50514997831991) / 20.0)
                                                              End If

                                                              ' Write modified tags to preserve Undo state
                                                              If Not _settings.IgnoreTags Then
                                                                  Await _engine.WriteTagsAsync(file.FilePath, file, targetDb, _settings.PreserveTimestamp, ct)
                                                              End If

                                                              file.Status = FileStatus.Applied
                                                          End Function)
    End Sub

    Private Async Sub OnApplyAlbumGain(sender As Object, e As EventArgs)
        If _engine Is Nothing OrElse _isProcessing Then Return

        Dim targets = GetSelectedOrAllFiles().Where(Function(f) f.Status = FileStatus.Analyzed).ToList()
        If targets.Count = 0 Then
            MessageBox.Show("No analyzed files to apply gain to." & vbCrLf & "Analyze files first.",
                "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim missingAlbumGain = targets.Where(Function(f) Not f.AlbumGain.HasValue).ToList()
        If missingAlbumGain.Count > 0 Then
            MessageBox.Show("Some files have not been analyzed together as an album yet." & vbCrLf & "Please run 'Analyze Album' first.",
                "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim targetDb As Double = CDbl(_targetDbSpinner.Value)

        Await ProcessFilesAsync("Applying Album Gain", targets, Async Function(file, ct)
                                                                    file.Status = FileStatus.Applying
                                                                    RefreshGrid()

                                                                    Dim albumDbChange As Double = targetDb - 89.0 + file.AlbumGain.Value
                                                                    Dim gainChange As Integer = CInt(Math.Round(albumDbChange / 1.50514997831991))

                                                                    If gainChange <> 0 Then
                                                                        Await _engine.ApplyGainAsync(file.FilePath, gainChange, _settings.PreserveTimestamp, ct)

                                                                        file.UndoLeft -= gainChange
                                                                        file.UndoRight -= gainChange
                                                                        file.HasUndo = True

                                                                        file.TrackGain -= gainChange * 1.50514997831991
                                                                        file.TrackPeak *= Math.Pow(10.0, (gainChange * 1.50514997831991) / 20.0)
                                                                        file.AlbumGain -= gainChange * 1.50514997831991
                                                                        file.AlbumPeak *= Math.Pow(10.0, (gainChange * 1.50514997831991) / 20.0)
                                                                    End If

                                                                    If Not _settings.IgnoreTags Then
                                                                        Await _engine.WriteTagsAsync(file.FilePath, file, targetDb, _settings.PreserveTimestamp, ct)
                                                                    End If

                                                                    file.Status = FileStatus.Applied
                                                                End Function)
    End Sub

    Private Async Sub OnApplyConstantGain(sender As Object, e As EventArgs)
        If _engine Is Nothing OrElse _isProcessing Then Return

        Dim targets = GetSelectedOrAllFiles()
        If targets.Count = 0 Then
            MessageBox.Show("No files selected.",
                "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using dlg As New ConstantGainForm()
            If dlg.ShowDialog() <> DialogResult.OK Then Return

            Dim steps As Integer = dlg.GainSteps
            If steps = 0 Then Return

            If dlg.ChannelMode <> 0 Then
                MessageBox.Show("Channel-specific gain modification is not yet supported by the modern MP3Gain 2026 engine." & vbCrLf & vbCrLf & "The gain will be applied equally to both channels.",
                    "Feature Unsupported", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If

            Await ProcessFilesAsync("Applying Constant Gain", targets, Async Function(file, ct)
                                                                          file.Status = FileStatus.Applying
                                                                          RefreshGrid()

                                                                          If steps <> 0 Then
                                                                              Await _engine.ApplyGainAsync(file.FilePath, steps, _settings.PreserveTimestamp, ct)

                                                                              file.UndoLeft -= steps
                                                                              file.UndoRight -= steps
                                                                              file.HasUndo = True

                                                                              If file.TrackGain.HasValue Then file.TrackGain -= steps * 1.50514997831991
                                                                              If file.TrackPeak.HasValue Then file.TrackPeak *= Math.Pow(10.0, (steps * 1.50514997831991) / 20.0)
                                                                              If file.AlbumGain.HasValue Then file.AlbumGain -= steps * 1.50514997831991
                                                                              If file.AlbumPeak.HasValue Then file.AlbumPeak *= Math.Pow(10.0, (steps * 1.50514997831991) / 20.0)

                                                                              Dim targetDb As Double = CDbl(_targetDbSpinner.Value)
                                                                              If Not _settings.IgnoreTags Then
                                                                                  Await _engine.WriteTagsAsync(file.FilePath, file, targetDb, _settings.PreserveTimestamp, ct)
                                                                              End If
                                                                          End If

                                                                          file.Status = FileStatus.Applied
                                                                      End Function)
        End Using
    End Sub

    Private Async Sub OnUndoGain(sender As Object, e As EventArgs)
        If _engine Is Nothing OrElse _isProcessing Then Return

        Dim targets = GetSelectedOrAllFiles()
        If targets.Count = 0 Then
            MessageBox.Show("No files selected.",
                "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim result = MessageBox.Show(
            $"Undo gain changes on {targets.Count} file(s)?",
            "Confirm Undo",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question)

        If result <> DialogResult.Yes Then Return

        Await ProcessFilesAsync("Undoing Gain", targets, Async Function(file, ct)
                                                             file.Status = FileStatus.Applying
                                                             RefreshGrid()

                                                             Try
                                                                 Await _engine.UndoGainAsync(file.FilePath, True, ct)
                                                             Catch ex As Mp3GainException When ex.Message.Contains("No undo information available")
                                                                 MessageBox.Show($"UNDO data not found for {file.FileName}. Skip?", "MP3Gain 2026", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                                                 Return
                                                             End Try

                                                             ' Crucial: remove old Undo tags BEFORE re-analyzing
                                                             If Not _settings.IgnoreTags Then
                                                                 Await _engine.RemoveTagsAsync(file.FilePath, _settings.PreserveTimestamp, ct)
                                                             End If

                                                             Dim analysisResult = Await _engine.AnalyzeFileAsync(file.FilePath, _settings.IgnoreTags, _settings.RecalcTags, ct)
                                                             file.TrackGain = analysisResult.TrackGain
                                                             file.TrackPeak = analysisResult.TrackPeak
                                                             file.AlbumGain = analysisResult.AlbumGain
                                                             file.AlbumPeak = analysisResult.AlbumPeak
                                                             file.MinGlobalGain = analysisResult.MinGlobalGain
                                                             file.MaxGlobalGain = analysisResult.MaxGlobalGain
                                                             file.MaxNoClipGain = analysisResult.MaxNoClipGain

                                                             ' Explicitly clear Undo info
                                                             file.HasUndo = False
                                                             file.UndoLeft = 0
                                                             file.UndoRight = 0

                                                             file.Status = FileStatus.Analyzed

                                                             Dim targetDb As Double = CDbl(_targetDbSpinner.Value)
                                                             If Not _settings.IgnoreTags Then
                                                                 Await _engine.WriteTagsAsync(file.FilePath, file, targetDb, _settings.PreserveTimestamp, ct)
                                                             End If
                                                         End Function)
    End Sub

    Private Async Sub OnRemoveTags(sender As Object, e As EventArgs)
        If _engine Is Nothing OrElse _isProcessing Then Return

        Dim targets = GetSelectedOrAllFiles().ToList()
        If targets.Count = 0 Then Return

        Dim result = MessageBox.Show(
            $"Remove ReplayGain tags from {targets.Count} file(s)?",
            "Confirm Tag Removal",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question)

        If result <> DialogResult.Yes Then Return

        Await ProcessFilesAsync("Removing Tags", targets, Async Function(file, ct)
                                                              Await _engine.RemoveTagsAsync(file.FilePath, True, ct)
                                                              file.HasExistingTags = False
                                                              file.HasUndo = False
                                                              file.TrackGain = Nothing
                                                              file.TrackPeak = Nothing
                                                              file.AlbumGain = Nothing
                                                              file.AlbumPeak = Nothing
                                                              file.Status = FileStatus.Pending
                                                          End Function)
    End Sub

    Private Sub OnClear(sender As Object, e As EventArgs)
        If _isProcessing Then Return
        _files.Clear()
        _statusLabel.Text = ""
        UpdateStatus()
    End Sub

    Private Sub OnClearAnalysis(sender As Object, e As EventArgs)
        If _isProcessing Then Return
        For Each file In _files
            file.ClearAnalysis()
        Next
        RefreshGrid()
        UpdateStatus()
    End Sub

    Private Sub UpdateFilenameDisplay(activeItem As ToolStripMenuItem)
        _filenamePathFileItem.Checked = (activeItem Is _filenamePathFileItem)
        _filenameFileOnlyItem.Checked = (activeItem Is _filenameFileOnlyItem)
        _filenamePathAndFileItem.Checked = (activeItem Is _filenamePathAndFileItem)

        If _grid.Columns.Contains("colFileName") AndAlso _grid.Columns.Contains("colDirectory") Then
            Dim fileNameCol = _grid.Columns("colFileName")
            Dim dirCol = _grid.Columns("colDirectory")

            If _filenamePathFileItem.Checked Then
                fileNameCol.DataPropertyName = NameOf(Mp3FileInfo.FilePath)
                fileNameCol.HeaderText = "Path\File"
                dirCol.Visible = False
            ElseIf _filenameFileOnlyItem.Checked Then
                fileNameCol.DataPropertyName = NameOf(Mp3FileInfo.FileName)
                fileNameCol.HeaderText = "File"
                dirCol.Visible = False
            ElseIf _filenamePathAndFileItem.Checked Then
                fileNameCol.DataPropertyName = NameOf(Mp3FileInfo.FileName)
                fileNameCol.HeaderText = "File"
                dirCol.Visible = True
            End If
            _grid.Invalidate()
        End If
    End Sub

    Private Sub OnLogs(sender As Object, e As EventArgs)
        Using dlg As New LogOptionsForm() With {
            .ErrorLogPath = _settings.ErrorLogPath,
            .AnalysisLogPath = _settings.AnalysisLogPath,
            .ChangeLogPath = _settings.ChangeLogPath
        }
            If dlg.ShowDialog(Me) = DialogResult.OK Then
                _settings.ErrorLogPath = dlg.ErrorLogPath
                _settings.AnalysisLogPath = dlg.AnalysisLogPath
                _settings.ChangeLogPath = dlg.ChangeLogPath

                For Each path In {_settings.ErrorLogPath, _settings.AnalysisLogPath, _settings.ChangeLogPath}
                    If Not String.IsNullOrEmpty(path) Then
                        Dim dir = IO.Path.GetDirectoryName(path)
                        If Not String.IsNullOrEmpty(dir) AndAlso Not System.IO.Directory.Exists(dir) Then
                            Try : System.IO.Directory.CreateDirectory(dir) : Catch : End Try
                        End If
                    End If
                Next
            End If
        End Using
    End Sub

    Private Sub OnResetColumnWidths(sender As Object, e As EventArgs)
        Dim defaults() As Integer = {90, 200, 80, 80, 110, 90, 110, 90, 180, 70}
        For i As Integer = 0 To Math.Min(_grid.Columns.Count, defaults.Length) - 1
            _grid.Columns(i).Width = defaults(i)
        Next
    End Sub

    Private Sub OnClearSelected(sender As Object, e As EventArgs)
        If _isProcessing Then Return
        Dim selected = _grid.SelectedRows.Cast(Of DataGridViewRow)() _
            .Select(Function(r) TryCast(r.DataBoundItem, Mp3FileInfo)) _
            .Where(Function(f) f IsNot Nothing) _
            .ToList()
        For Each f In selected
            _files.Remove(f)
        Next
        UpdateStatus()
    End Sub

    Private Sub OnCancel(sender As Object, e As EventArgs)
        If _cts IsNot Nothing Then _cts.Cancel()
    End Sub

    Private Sub OnAbout(sender As Object, e As EventArgs)
        Dim ver As String = If(_engine IsNot Nothing, _engine.GetVersion(), "N/A")
        Using dlg As New AboutForm(ver)
            dlg.ShowDialog(Me)
        End Using
    End Sub

    Private Sub OnInvertSelection(sender As Object, e As EventArgs)
        For Each row As DataGridViewRow In _grid.Rows
            row.Selected = Not row.Selected
        Next
    End Sub

    Private Sub OnSaveAnalysis(sender As Object, e As EventArgs)
        If _files.Count = 0 Then Return

        Using dlg As New SaveFileDialog() With {
            .Title = "Save Analysis",
            .Filter = "MP3Gain Analysis (*.m3g)|*.m3g|Tab-Separated Values (*.tsv)|*.tsv|All Files (*.*)|*.*",
            .DefaultExt = "m3g",
            .FileName = "mp3gain_analysis.m3g"
        }
            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return

            Using writer As New StreamWriter(dlg.FileName)
                Dim isLegacy As Boolean = dlg.FileName.EndsWith(".m3g", StringComparison.OrdinalIgnoreCase)

                If isLegacy Then
                    For Each f In _files
                        Dim dir As String = f.Directory & "\"
                        Dim maxAmp As Double = If(f.TrackPeak, 0) * 32768.0
                        Dim albumStr As String = If(f.AlbumGain.HasValue, f.AlbumGain.Value.ToString(), """?""")
                        Dim ts As String = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        writer.WriteLine($"""{dir}"",""{f.FileName}"",#{ts}#,{f.FileSize},{maxAmp:F3},{f.TrackGain},{albumStr}")
                    Next
                Else
                    writer.WriteLine("FilePath" & vbTab & "TrackGain" & vbTab & "TrackPeak" & vbTab & "AlbumGain" & vbTab & "AlbumPeak" & vbTab & "Volume" & vbTab & "Clipping" & vbTab & "Status")
                    For Each f In _files
                        writer.WriteLine($"{f.FilePath}{vbTab}{f.TrackGain}{vbTab}{f.TrackPeak}{vbTab}{f.AlbumGain}{vbTab}{f.AlbumPeak}{vbTab}{f.Volume}{vbTab}{f.Clipping}{vbTab}{f.Status}")
                    Next
                End If
            End Using

            _statusLabel.Text = $"Analysis saved to {IO.Path.GetFileName(dlg.FileName)}"
            _statusLabel.ForeColor = TextSecondary
        End Using
    End Sub

    Private Sub OnLoadAnalysis(sender As Object, e As EventArgs)
        Using dlg As New OpenFileDialog() With {
            .Title = "Load Analysis",
            .Filter = "MP3Gain Analysis (*.m3g)|*.m3g|Tab-Separated Values (*.tsv)|*.tsv|All Files (*.*)|*.*",
            .DefaultExt = "m3g"
        }
            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return

            Try
                Dim lines = File.ReadAllLines(dlg.FileName)
                If lines.Length = 0 Then Return

                Dim isLegacy As Boolean = lines(0).StartsWith("""")

                _files.Clear()

                If isLegacy Then
                    For Each line In lines
                        If String.IsNullOrWhiteSpace(line) Then Continue For

                        Dim fields = ParseLegacyCsvLine(line)
                        If fields.Count < 7 Then Continue For

                        Dim dir As String = fields(0).TrimEnd("\"c)
                        Dim filename As String = fields(1)
                        Dim fullPath As String = IO.Path.Combine(dir, filename)

                        Dim f = Mp3FileInfo.FromPath(fullPath)

                        Dim sz As Long
                        If Long.TryParse(fields(3), sz) Then f.FileSize = sz

                        Dim maxAmp As Double
                        If Double.TryParse(fields(4), maxAmp) Then f.TrackPeak = maxAmp / 32768.0

                        Dim tg As Double
                        If Double.TryParse(fields(5), tg) Then f.TrackGain = tg

                        Dim ag As Double
                        If fields(6) <> "?" AndAlso Double.TryParse(fields(6), ag) Then f.AlbumGain = ag

                        f.Status = If(f.TrackGain.HasValue, FileStatus.Analyzed, FileStatus.Pending)
                        _files.Add(f)
                    Next
                Else
                    For i As Integer = 1 To lines.Length - 1
                        Dim parts = lines(i).Split(CChar(vbTab))
                        If parts.Length < 1 OrElse String.IsNullOrWhiteSpace(parts(0)) Then Continue For

                        Dim f = Mp3FileInfo.FromPath(parts(0))
                        Dim tg As Double : If parts.Length > 1 AndAlso Double.TryParse(parts(1), tg) Then f.TrackGain = tg
                        Dim tp As Double : If parts.Length > 2 AndAlso Double.TryParse(parts(2), tp) Then f.TrackPeak = tp
                        Dim ag As Double : If parts.Length > 3 AndAlso Double.TryParse(parts(3), ag) Then f.AlbumGain = ag
                        Dim ap As Double : If parts.Length > 4 AndAlso Double.TryParse(parts(4), ap) Then f.AlbumPeak = ap
                        Dim st As FileStatus
                        If parts.Length > 7 AndAlso [Enum].TryParse(parts(7), st) Then f.Status = st
                        _files.Add(f)
                    Next
                End If

                RefreshGrid()
                UpdateStatus()
                _statusLabel.Text = $"Loaded analysis from {IO.Path.GetFileName(dlg.FileName)}  ({_files.Count} files)"
                _statusLabel.ForeColor = TextSecondary
            Catch ex As Exception
                MessageBox.Show($"Error loading analysis:{vbCrLf}{ex.Message}", "MP3Gain 2026",
                    MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    ''' <summary>
    ''' Parse a legacy MP3Gain .m3g CSV line handling quoted fields and VB6 date literals.
    ''' </summary>
    Private Shared Function ParseLegacyCsvLine(line As String) As List(Of String)
        Dim fields As New List(Of String)()
        Dim i As Integer = 0
        While i < line.Length
            If line(i) = """"c Then
                ' Quoted field
                Dim [end] As Integer = line.IndexOf(""""c, i + 1)
                If [end] < 0 Then [end] = line.Length
                fields.Add(line.Substring(i + 1, [end] - i - 1))
                i = [end] + 1
                If i < line.Length AndAlso line(i) = ","c Then i += 1
            ElseIf line(i) = "#"c Then
                ' VB6 date literal #...#
                Dim [end] As Integer = line.IndexOf("#"c, i + 1)
                If [end] < 0 Then [end] = line.Length
                fields.Add(line.Substring(i + 1, [end] - i - 1))
                i = [end] + 1
                If i < line.Length AndAlso line(i) = ","c Then i += 1
            ElseIf line(i) = ","c Then
                fields.Add("")
                i += 1
            Else
                ' Unquoted field
                Dim [end] As Integer = line.IndexOf(","c, i)
                If [end] < 0 Then [end] = line.Length
                fields.Add(line.Substring(i, [end] - i))
                i = [end]
                If i < line.Length AndAlso line(i) = ","c Then i += 1
            End If
        End While
        Return fields
    End Function

    ' ══════════════════════════════════════════════════════════════
    '  PROCESSING INFRASTRUCTURE
    ' ══════════════════════════════════════════════════════════════

    Private Function GetSelectedOrAllFiles() As List(Of Mp3FileInfo)
        If _grid.SelectedRows.Count > 0 AndAlso _grid.SelectedRows.Count < _files.Count Then
            Return _grid.SelectedRows.Cast(Of DataGridViewRow)() _
                .Select(Function(r) TryCast(r.DataBoundItem, Mp3FileInfo)) _
                .Where(Function(f) f IsNot Nothing) _
                .ToList()
        End If
        Return _files.ToList()
    End Function

    Private Async Function ProcessFilesAsync(operationName As String, targets As List(Of Mp3FileInfo),
        processFunc As Func(Of Mp3FileInfo, CancellationToken, Task)) As Task

        _isProcessing = True
        _cts = New CancellationTokenSource()
        _fileProgressLabel.Visible = False
        _fileProgressBar.Visible = False
        _totalProgressLabel.Visible = True
        _totalProgressBar.Visible = True
        _totalProgressBar.Maximum = targets.Count
        _totalProgressBar.Value = 0
        SetToolbarEnabled(False)

        Dim processed As Integer = 0
        Dim errors As Integer = 0
        Dim currentProcessingFile As Mp3FileInfo = Nothing

        Dim onProgress As Action(Of Integer) = Sub(percent)
                                                    If currentProcessingFile IsNot Nothing Then
                                                        currentProcessingFile.Progress = percent
                                                        If InvokeRequired Then
                                                            BeginInvoke(New Action(Sub() RefreshGrid()))
                                                        Else
                                                            RefreshGrid()
                                                        End If
                                                    End If
                                                End Sub

        If _engine IsNot Nothing Then
            AddHandler _engine.ProgressChanged, onProgress
        End If

        Try
            For Each file In targets
                If _cts.Token.IsCancellationRequested Then Exit For

                _statusLabel.Text = $"{operationName}: {file.FileName}  ({processed + 1}/{targets.Count})"
                currentProcessingFile = file
                file.Progress = 0

                Try
                    Await processFunc(file, _cts.Token)
                Catch ex As OperationCanceledException
                    file.Status = FileStatus.Pending
                    Exit For
                Catch ex As Exception
                    file.Status = FileStatus.Error
                    file.StatusMessage = ex.Message
                    errors += 1

                    Try
                        If Not String.IsNullOrWhiteSpace(_settings.ErrorLogPath) Then
                            Dim _dir = IO.Path.GetDirectoryName(_settings.ErrorLogPath)
                            If Not String.IsNullOrWhiteSpace(_dir) AndAlso Not System.IO.Directory.Exists(_dir) Then System.IO.Directory.CreateDirectory(_dir)
                            IO.File.AppendAllText(_settings.ErrorLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error on {file.FilePath}: {ex.Message}{Environment.NewLine}")
                        End If
                    Catch
                    End Try
                End Try

                file.Progress = -1
                currentProcessingFile = Nothing
                processed += 1
                _totalProgressBar.Value = processed
                RefreshGrid()
                UpdateStatus()
            Next
        Finally
            If _engine IsNot Nothing Then
                RemoveHandler _engine.ProgressChanged, onProgress
            End If

            _isProcessing = False
            _totalProgressLabel.Visible = False
            _totalProgressBar.Visible = False
            SetToolbarEnabled(True)
            If _cts IsNot Nothing Then _cts.Dispose()
            _cts = Nothing

            Dim summary As String = If(errors > 0,
                $"{operationName} complete: {processed} processed, {errors} error(s)",
                $"{operationName} complete: {processed} file(s) processed")
            _statusLabel.Text = summary
            _statusLabel.ForeColor = If(errors > 0, AccentWarning, TextSecondary)

            _bindingSource.ResetBindings(False)
            RefreshGrid()
            UpdateStatus()

            If _settings.BeepWhenFinished AndAlso targets.Count > 1 Then
                Media.SystemSounds.Beep.Play()
            End If
        End Try
    End Function

    Private Sub SetToolbarEnabled(enabled As Boolean)
        For Each item As ToolStripItem In _toolbar.Items
            If item.Name = "btnCancel" Then
                item.Visible = Not enabled
            Else
                item.Visible = enabled
            End If
        Next
    End Sub

    Private Sub RefreshGrid()
        _grid.Invalidate()
        _grid.Update()
    End Sub

    ' ══════════════════════════════════════════════════════════════
    '  CLEANUP
    ' ══════════════════════════════════════════════════════════════

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If _cts IsNot Nothing Then _cts.Cancel()
        If _engine IsNot Nothing Then _engine.Dispose()

        _settings.AlwaysOnTop = _alwaysOnTopItem.Checked
        _settings.AddSubfolders = _addSubfoldersItem.Checked
        _settings.PreserveTimestamp = _preserveTimestampItem.Checked
        _settings.NoLayerCheck = _noLayerCheckItem.Checked
        _settings.DontClip = _dontClipItem.Checked
        _settings.IgnoreTags = _skipTagsItem.Checked
        _settings.RecalcTags = _recalcTagsItem.Checked
        _settings.BeepWhenFinished = _beepWhenFinishedItem.Checked

        If _filenamePathAndFileItem.Checked Then
            _settings.FilenameDisplayMode = 2
        ElseIf _filenameFileOnlyItem.Checked Then
            _settings.FilenameDisplayMode = 1
        Else
            _settings.FilenameDisplayMode = 0
        End If

        _settings.TargetVolume = _targetDbSpinner.Value

        _settings.Save()

        MyBase.OnFormClosing(e)
    End Sub

End Class
