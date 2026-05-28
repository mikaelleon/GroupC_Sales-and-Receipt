Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Printing
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient

Public Class ReceiptForm

    Private Const PreviewReceiptWidth As Integer = 480
    Private Const MinPreviewZoom As Single = 0.75F
    Private Const MaxPreviewZoom As Single = 1.5F
    Private Const PreviewZoomStep As Single = 0.1F
    Private Const HistoryPageSize As Integer = 500

    Private Enum HistoryDateFilter
        All = 0
        Today = 1
        ThisWeek = 2
        ThisMonth = 3
        CustomRange = 4
    End Enum

    Private Enum HistorySortOption
        NewestFirst = 0
        OldestFirst = 1
        AmountHigh = 2
        AmountLow = 3
    End Enum

    Private Class SaleListItem
        Public SaleId As Integer
        Public SaleDate As DateTime
        Public TotalAmount As Decimal
        Public CashierHint As String
        Public IsVoided As Boolean

        Public Function MatchesSearch(term As String) As Boolean
            If String.IsNullOrWhiteSpace(term) Then
                Return True
            End If

            Dim t As String = term.Trim()
            If SaleId >= 0 AndAlso (SaleId.ToString(CultureInfo.InvariantCulture).Contains(t) OrElse ("#" & SaleId.ToString(CultureInfo.InvariantCulture)).Contains(t, StringComparison.OrdinalIgnoreCase)) Then
                Return True
            End If

            Dim sym As String = AppSettings.Current.CurrencySymbol
            If TotalAmount.ToString("N2", CultureInfo.CurrentCulture).Contains(t) OrElse
               (sym & TotalAmount.ToString("N2", CultureInfo.CurrentCulture)).Contains(t, StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If

            If Not String.IsNullOrWhiteSpace(CashierHint) AndAlso CashierHint.Contains(t, StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If

            Return SaleDate.ToString("MMMM d yyyy", CultureInfo.CurrentCulture).Contains(t, StringComparison.OrdinalIgnoreCase) OrElse
                   SaleDate.ToString("MMM d", CultureInfo.CurrentCulture).Contains(t, StringComparison.OrdinalIgnoreCase)
        End Function

        Public Overrides Function ToString() As String
            If SaleId < 0 Then
                Return "Latest receipt"
            End If

            Dim sym As String = AppSettings.Current.CurrencySymbol
            Dim voidPrefix As String = If(IsVoided, "[VOID] ", String.Empty)
            Return String.Format(
                CultureInfo.CurrentCulture,
                "{0}#{1}  {2:MMM d, h:mm tt}  {3}{4:N2}",
                voidPrefix,
                SaleId,
                SaleDate,
                sym,
                TotalAmount)
        End Function
    End Class

    Private dgvLines As DataGridView
    Private rtbReceipt As RichTextBox
    Private picReceiptLogo As PictureBox
    Private lblSaleMeta As Label
    Private WithEvents cmbHistory As ComboBox
    Private WithEvents lstHistory As ListBox
    Private pnlSaleChip As Panel
    Private lblChipSaleId As Label
    Private lblChipDate As Label
    Private lblChipTotal As Label
    Private lblChipCashier As Label
    Private lblHistoryEmpty As Label
    Private formToolTips As ToolTip
    Private pnlEmptyPreview As Panel
    Private pnlReceiptScroll As Panel
    Private pnlReceiptPaper As Panel
    Private WithEvents btnPrint As Button
    Private WithEvents btnSave As Button
    Private WithEvents btnSavePdf As Button
    Private WithEvents btnLoadList As Button
    Private WithEvents btnCopy As Button
    Private WithEvents btnReprint As Button
    Private WithEvents btnEmail As Button
    Private WithEvents btnDetails As Button
    Private WithEvents btnDuplicate As Button
    Private WithEvents btnExportBatch As Button
    Private WithEvents btnPrintPreview As Button
    Private WithEvents btnLoadMore As Button
    Private WithEvents btnVoid As Button
    Private WithEvents btnBack As Button
    Private WithEvents btnZoomIn As Button
    Private WithEvents btnZoomOut As Button
    Private WithEvents txtHistorySearch As TextBox
    Private WithEvents cmbDateFilter As ComboBox
    Private WithEvents cmbSort As ComboBox
    Private WithEvents dtpFilterFrom As DateTimePicker
    Private WithEvents dtpFilterTo As DateTimePicker
    Private WithEvents chkSimulatePage As CheckBox
    Private WithEvents printDocument As PrintDocument

    Private pnlPageCanvas As Panel
    Private pnlActionToolbar As FlowLayoutPanel
    Private pnlCustomRange As Panel
    Private lblZoomPct As Label
    Private ctxReceipt As ContextMenuStrip

    Private ReadOnly allHistoryItems As New List(Of SaleListItem)()
    Private receiptText As String
    Private snapshot As ReceiptSnapshot
    Private saleIdForMeta As Integer = -1
    Private currentSaleId As Integer = -1
    Private currentSaleVoided As Boolean = False
    Private historySkip As Integer = 0
    Private historyHasMore As Boolean = False
    Private printHelper As ReceiptPrintHelper
    Private suppressHistoryEvent As Boolean
    Private previewZoomScale As Single = 1.0F

    Private statusStrip As StatusStrip
    Private statusLabel As ToolStripStatusLabel
    Private WithEvents statusClearTimer As Timer

    Public Sub New()
        InitializeComponent()
        receiptText = String.Empty
    End Sub

    Public Sub New(text As String, Optional savedSaleId As Integer = -1)
        InitializeComponent()
        receiptText = If(text, String.Empty)
        saleIdForMeta = savedSaleId
    End Sub

    ''' <summary>
    ''' Initializes a receipt view with structured line data and optional sale id.
    ''' </summary>
    ''' <param name="detail">Structured receipt snapshot.</param>
    ''' <param name="savedSaleId">Database sale id when known.</param>
    Public Sub New(detail As ReceiptSnapshot, savedSaleId As Integer)
        InitializeComponent()
        snapshot = detail
        receiptText = If(detail IsNot Nothing, detail.ReceiptText, String.Empty)
        saleIdForMeta = savedSaleId
    End Sub

    Private Sub ReceiptForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' 1. FORM SETUP (Full Screen & Responsive)
        Me.Text = AppBranding.WindowTitle("Receipt Preview")
        UiTheme.ApplyMaximizedWorkspaceDefaults(Me, 900, 600)

        Try
            UiTheme.ApplyStandardWindowChrome(Me)
        Catch
        End Try

        Try
            DatabaseInitializer.EnsureDatabase()
        Catch
        End Try

        ' 2. BUILD THE RESPONSIVE UI 
        ' (This calls the massive CreateControls method we built in the previous step!)
        CreateControls()

        ' 3. YOUR ORIGINAL DATA LOADING LOGIC
        suppressHistoryEvent = True
        LoadHistoryCombo()

        If snapshot IsNot Nothing Then
            FillLineGrid(snapshot)
            Dim saleWhen As DateTime = ReceiptBranding.NormalizeStoredSaleDate(
                If(snapshot.SaleDateTime = DateTime.MinValue, DateTime.UtcNow, snapshot.SaleDateTime))
            receiptText = ReceiptBranding.AlignReceiptDateLine(receiptText, saleWhen)
            ApplyReceiptContent(receiptText, False)
            UpdateSaleMetadataFromSnapshot(snapshot, saleIdForMeta)
            suppressHistoryEvent = False

        ElseIf receiptText.Trim().Length > 0 Then
            ApplyReceiptContent(receiptText, False)
            UpdateSaleMetadataFromText(saleIdForMeta, DateTime.MinValue, receiptText, Nothing)
            suppressHistoryEvent = False

        Else
            If lstHistory.Items.Count > 0 Then
                suppressHistoryEvent = True
                lstHistory.SelectedIndex = 0
                cmbHistory.SelectedIndex = 0
                suppressHistoryEvent = False
                ProcessHistorySelection()
            Else
                suppressHistoryEvent = False
                LoadLatestReceiptFromDb()
            End If
        End If
    End Sub

    Private Sub SetupForm()
        Me.Text = AppBranding.WindowTitle("Receipt Viewer")
        Me.MinimumSize = New Size(1024, 720)
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.WindowState = FormWindowState.Maximized ' Start in Full Screen
        Me.StartPosition = FormStartPosition.CenterScreen
    End Sub

    Private Sub CreateControls()
        Me.SuspendLayout()
        Me.Controls.Clear()
        Me.BackColor = UiTheme.ColBackground

        printDocument = New PrintDocument()

        cmbHistory = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Visible = False, .Size = New Size(0, 0)}
        lstHistory = New ListBox() With {
            .IntegralHeight = False,
            .Dock = DockStyle.Fill,
            .BorderStyle = BorderStyle.FixedSingle,
            .Font = UiTheme.FontBody,
            .BackColor = UiTheme.ColSurface,
            .HorizontalScrollbar = True
        }
        txtHistorySearch = New TextBox() With {
            .Dock = DockStyle.Fill,
            .PlaceholderText = "Search receipt #, amount, date…"
        }
        UiTheme.ApplyInputStyle(txtHistorySearch)

        cmbDateFilter = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Dock = DockStyle.Fill,
            .Font = UiTheme.FontBody
        }
        cmbDateFilter.Items.AddRange(New Object() {"All dates", "Today", "This week", "This month", "Custom range"})
        cmbDateFilter.SelectedIndex = 0
        UiTheme.ApplyInputStyle(cmbDateFilter)

        cmbSort = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Dock = DockStyle.Fill,
            .Font = UiTheme.FontBody
        }
        cmbSort.Items.AddRange(New Object() {"Newest first", "Oldest first", "Amount: high to low", "Amount: low to high"})
        cmbSort.SelectedIndex = 0
        UiTheme.ApplyInputStyle(cmbSort)

        dtpFilterFrom = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Dock = DockStyle.Fill, .Value = DateTime.Today.AddDays(-7)}
        dtpFilterTo = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Dock = DockStyle.Fill, .Value = DateTime.Today}

        pnlCustomRange = New Panel() With {
            .AutoSize = True,
            .Dock = DockStyle.Top,
            .Visible = False,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        }
        Dim rangeLayout As New TableLayoutPanel() With {
            .AutoSize = True,
            .Dock = DockStyle.Top,
            .ColumnCount = 4,
            .RowCount = 1,
            .Margin = Padding.Empty
        }
        rangeLayout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        rangeLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        rangeLayout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        rangeLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        Dim lblFrom As New Label() With {
            .Text = "From",
            .AutoSize = True,
            .Anchor = AnchorStyles.Left,
            .Font = UiTheme.FontCaption,
            .ForeColor = UiTheme.ColTextSecondary,
            .Margin = New Padding(0, 0, UiTheme.PadTight, 0)
        }
        Dim lblTo As New Label() With {
            .Text = "To",
            .AutoSize = True,
            .Anchor = AnchorStyles.Left,
            .Font = UiTheme.FontCaption,
            .ForeColor = UiTheme.ColTextSecondary,
            .Margin = New Padding(UiTheme.PadControl, 0, UiTheme.PadTight, 0)
        }
        rangeLayout.Controls.Add(lblFrom, 0, 0)
        rangeLayout.Controls.Add(dtpFilterFrom, 1, 0)
        rangeLayout.Controls.Add(lblTo, 2, 0)
        rangeLayout.Controls.Add(dtpFilterTo, 3, 0)
        pnlCustomRange.Controls.Add(rangeLayout)

        btnLoadList = New Button() With {
            .Text = "↻ Refresh",
            .AutoSize = True,
            .MinimumSize = New Size(100, UiTheme.ButtonHeight),
            .Cursor = Cursors.Hand
        }
        btnLoadMore = New Button() With {
            .Text = "Load more",
            .AutoSize = True,
            .MinimumSize = New Size(100, UiTheme.ButtonHeight),
            .Cursor = Cursors.Hand,
            .Enabled = False
        }
        btnExportBatch = New Button() With {
            .Text = "Export batch",
            .AutoSize = True,
            .MinimumSize = New Size(120, UiTheme.ButtonHeight),
            .Cursor = Cursors.Hand
        }
        UiTheme.ApplySecondaryButton(btnLoadList)
        UiTheme.ApplySecondaryButton(btnLoadMore)
        UiTheme.ApplySecondaryAccentButton(btnExportBatch)

        btnVoid = CreateToolbarButton("Void sale", False)
        UiTheme.ApplyDangerButton(btnVoid)
        btnVoid.Visible = AppSession.IsAdmin()

        btnPrint = CreateToolbarButton("Print", True)
        btnPrintPreview = CreateToolbarButton("Print preview", False)
        btnReprint = CreateToolbarButton("Reprint", False)
        btnSavePdf = CreateToolbarButton("Save PDF", False)
        btnSave = CreateToolbarButton("Save text", False)
        btnCopy = CreateToolbarButton("Copy", False)
        btnEmail = CreateToolbarButton("Email", False)
        btnDetails = CreateToolbarButton("Details", False)
        btnDuplicate = CreateToolbarButton("Duplicate", False)

        lblSaleMeta = New Label() With {.Visible = False, .AutoSize = True}

        BuildSaleChipPanel()

        picReceiptLogo = New PictureBox() With {
            .Size = New Size(PreviewReceiptWidth, ReceiptBranding.ReceiptLogoHeight),
            .SizeMode = PictureBoxSizeMode.Zoom,
            .BackColor = UiTheme.ColSurface,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        }

        rtbReceipt = New RichTextBox() With {
            .Width = PreviewReceiptWidth,
            .Font = UiTheme.FontMono,
            .ReadOnly = True,
            .BackColor = UiTheme.ColSurface,
            .BorderStyle = BorderStyle.None,
            .ScrollBars = RichTextBoxScrollBars.Both,
            .WordWrap = False,
            .Margin = Padding.Empty
        }

        dgvLines = New DataGridView() With {
            .Visible = False,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .ReadOnly = True
        }
        dgvLines.Columns.Add("ProductName", "Product")
        dgvLines.Columns.Add("Qty", "Qty")
        dgvLines.Columns.Add("UnitPrice", "Unit price")
        dgvLines.Columns.Add("LineTotal", "Line total")

        statusClearTimer = New Timer() With {.Interval = FormStatusHelper.StatusShowMilliseconds}
        statusStrip = New StatusStrip()
        statusLabel = New ToolStripStatusLabel(FormStatusHelper.ReadyText) With {.Spring = True, .TextAlign = ContentAlignment.MiddleLeft}
        statusStrip.Items.Add(statusLabel)
        Try
            UiTheme.ApplyStatusStripTheme(statusStrip)
        Catch
        End Try

        btnZoomOut = CreateToolbarButton("−", False)
        btnZoomOut.MinimumSize = New Size(UiTheme.ButtonHeight, UiTheme.ButtonHeight)
        btnZoomIn = CreateToolbarButton("+", False)
        btnZoomIn.MinimumSize = New Size(UiTheme.ButtonHeight, UiTheme.ButtonHeight)
        lblZoomPct = New Label() With {
            .Text = "100%",
            .AutoSize = True,
            .Font = UiTheme.FontBodyBold,
            .ForeColor = UiTheme.ColTextPrimary,
            .Margin = New Padding(UiTheme.PadControl, UiTheme.PadControl, UiTheme.PadControl, 0)
        }
        chkSimulatePage = New CheckBox() With {
            .Text = "Show page margins",
            .AutoSize = True,
            .Font = UiTheme.FontCaption,
            .ForeColor = UiTheme.ColTextSecondary,
            .Margin = New Padding(UiTheme.PadSection, UiTheme.PadControl, 0, 0)
        }

        pnlActionToolbar = New FlowLayoutPanel() With {
            .AutoSize = True,
            .WrapContents = True,
            .Dock = DockStyle.Top,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl),
            .Padding = New Padding(0, 0, 0, UiTheme.PadTight)
        }
        pnlActionToolbar.Controls.AddRange(New Control() {
            btnPrint, btnPrintPreview, btnReprint, btnSavePdf, btnSave, btnCopy, btnEmail, btnDetails, btnDuplicate, btnVoid
        })

        BuildEmptyPreviewPanel()
        BuildPreviewArea()

        ctxReceipt = BuildReceiptContextMenu()
        rtbReceipt.ContextMenuStrip = ctxReceipt
        pnlReceiptPaper.ContextMenuStrip = ctxReceipt

        AddHandler txtHistorySearch.TextChanged, AddressOf HistoryFilterChanged
        AddHandler cmbDateFilter.SelectedIndexChanged, AddressOf HistoryFilterChanged
        AddHandler cmbSort.SelectedIndexChanged, AddressOf HistoryFilterChanged
        AddHandler dtpFilterFrom.ValueChanged, AddressOf HistoryFilterChanged
        AddHandler dtpFilterTo.ValueChanged, AddressOf HistoryFilterChanged
        AddHandler chkSimulatePage.CheckedChanged, AddressOf chkSimulatePage_CheckedChanged
        AddHandler btnZoomIn.Click, AddressOf btnZoomIn_Click
        AddHandler btnZoomOut.Click, AddressOf btnZoomOut_Click

        ' -----------------------------------------------------------
        ' SHARED SHELL + RECEIPT SPLIT LAYOUT
        ' -----------------------------------------------------------
        Dim rootTable As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .Margin = Padding.Empty,
            .BackColor = UiTheme.ColBackground
        }
        rootTable.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, UiTheme.SidebarWidth))
        rootTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

        Dim sidebar As Panel = UiTheme.BuildWorkspaceSidebarShell(WorkspaceNavigation.Target.Receipt, Me, btnBack)

        Dim rightColumn As New Panel() With {.Dock = DockStyle.Fill, .BackColor = UiTheme.ColBackground}
        Dim topBar As Panel = UiTheme.CreateTopBar("Receipt Preview", AppSession.GetReceiptOperatorName())
        Dim contentArea As Panel = UiTheme.CreateContentArea()

        Dim receiptSplit As SplitContainer = UiTheme.CreateVerticalSplit()

        ' --- LEFT: filters + history ---
        Dim leftCard As Panel = UiTheme.CreateCard()
        leftCard.Dock = DockStyle.Fill
        Dim leftCardHost As Panel = leftCard
        Try
            leftCardHost = UiTheme.GetCardContentHost(leftCard)
        Catch
        End Try

        Dim leftLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 11,
            .Margin = Padding.Empty
        }
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        leftLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        leftLayout.Controls.Add(UiTheme.CreateSectionHeader("Receipts"), 0, 0)
        leftLayout.Controls.Add(UiTheme.CreateSecondaryLabel("Select a past sale to preview, print, or export."), 0, 1)
        leftLayout.Controls.Add(UiTheme.CreateSectionHeader("Search & filters"), 0, 2)
        txtHistorySearch.Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        leftLayout.Controls.Add(txtHistorySearch, 0, 3)
        cmbDateFilter.Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        leftLayout.Controls.Add(cmbDateFilter, 0, 4)
        leftLayout.Controls.Add(pnlCustomRange, 0, 5)
        cmbSort.Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        leftLayout.Controls.Add(cmbSort, 0, 6)
        leftLayout.Controls.Add(UiTheme.CreateSectionHeader("Receipt history"), 0, 7)

        lblHistoryEmpty = UiTheme.CreateEmptyStateLabel("No receipts match the current filters.")
        lblHistoryEmpty.Visible = False
        Dim historyListHost As New Panel() With {.Dock = DockStyle.Fill}
        historyListHost.Controls.Add(lstHistory)
        lblHistoryEmpty.Dock = DockStyle.Fill
        historyListHost.Controls.Add(lblHistoryEmpty)
        leftLayout.Controls.Add(historyListHost, 0, 8)
        Dim pnlListActions As New FlowLayoutPanel() With {
            .AutoSize = True,
            .WrapContents = False,
            .Dock = DockStyle.Top,
            .Margin = New Padding(0, UiTheme.PadControl, 0, 0)
        }
        pnlListActions.Controls.AddRange(New Control() {btnLoadList, btnLoadMore, btnExportBatch})
        leftLayout.Controls.Add(pnlListActions, 0, 9)
        pnlSaleChip.Dock = DockStyle.Top
        pnlSaleChip.Margin = New Padding(0, UiTheme.PadControl, 0, 0)
        leftLayout.Controls.Add(pnlSaleChip, 0, 10)
        leftCardHost.Controls.Add(leftLayout)
        receiptSplit.Panel1.Controls.Add(leftCard)

        ' --- RIGHT: preview ---
        Dim previewCard As Panel = UiTheme.CreateCard()
        previewCard.Dock = DockStyle.Fill
        Dim previewCardHost As Panel = previewCard
        Try
            previewCardHost = UiTheme.GetCardContentHost(previewCard)
        Catch
        End Try

        Dim rightLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 4,
            .Margin = Padding.Empty
        }
        rightLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        rightLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        rightLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        rightLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

        rightLayout.Controls.Add(UiTheme.CreateSectionHeader("Receipt preview"), 0, 0)

        Dim pnlPreviewHeader As New FlowLayoutPanel() With {
            .AutoSize = True,
            .WrapContents = False,
            .Dock = DockStyle.Top,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        }
        Dim lblPreviewHint As Label = UiTheme.CreateSecondaryLabel("Monospace receipt layout. Use zoom and print preview for page layout.")
        lblPreviewHint.MaximumSize = New Size(520, 0)
        lblPreviewHint.Font = New Font(UiTheme.FontBody.FontFamily, UiTheme.FontBody.Size, FontStyle.Italic)
        lblPreviewHint.Margin = New Padding(0, UiTheme.PadTight, UiTheme.PadSection, 0)
        pnlPreviewHeader.Controls.Add(lblPreviewHint)
        pnlPreviewHeader.Controls.Add(btnZoomOut)
        pnlPreviewHeader.Controls.Add(lblZoomPct)
        pnlPreviewHeader.Controls.Add(btnZoomIn)
        pnlPreviewHeader.Controls.Add(chkSimulatePage)
        rightLayout.Controls.Add(pnlPreviewHeader, 0, 1)
        rightLayout.Controls.Add(pnlActionToolbar, 0, 2)

        Dim previewHost As New Panel() With {.Dock = DockStyle.Fill, .BackColor = UiTheme.ColBackground}
        pnlReceiptScroll.Dock = DockStyle.Fill
        pnlEmptyPreview.Dock = DockStyle.Fill
        previewHost.Controls.Add(pnlReceiptScroll)
        previewHost.Controls.Add(pnlEmptyPreview)
        rightLayout.Controls.Add(previewHost, 0, 3)
        previewCardHost.Controls.Add(rightLayout)
        receiptSplit.Panel2.Controls.Add(previewCard)

        contentArea.Controls.Add(receiptSplit)
        rightColumn.Controls.Add(contentArea)
        rightColumn.Controls.Add(topBar)

        rootTable.Controls.Add(sidebar, 0, 0)
        rootTable.Controls.Add(rightColumn, 1, 0)

        Me.Controls.Add(rootTable)
        Me.Controls.Add(statusStrip)

        AddHandler receiptSplit.SplitterMoved, Sub(s, ev) ConfigureReceiptSplit(receiptSplit)
        AddHandler Me.Resize, Sub(s, ev) ConfigureReceiptSplit(receiptSplit)
        AddHandler pnlReceiptScroll.Resize, AddressOf LayoutReceiptPaper

        formToolTips = UiTheme.CreateStandardToolTip()
        formToolTips.SetToolTip(btnEmail, "Open your email client with this receipt (may fail for very long receipts)")
        formToolTips.SetToolTip(btnZoomIn, "Zoom in on the receipt preview")
        formToolTips.SetToolTip(btnZoomOut, "Zoom out on the receipt preview")
        formToolTips.SetToolTip(btnExportBatch, "Export multiple receipts at once")
        formToolTips.SetToolTip(btnLoadList, "Reload receipt history from the database")

        UiTheme.AssignTabOrder(
            txtHistorySearch,
            cmbDateFilter,
            dtpFilterFrom,
            dtpFilterTo,
            cmbSort,
            lstHistory,
            btnLoadList,
            btnExportBatch,
            btnPrint,
            btnPrintPreview,
            btnSavePdf,
            btnSave,
            btnCopy,
            btnEmail,
            btnDetails,
            btnDuplicate,
            btnZoomOut,
            btnZoomIn,
            chkSimulatePage)

        Me.ResumeLayout(True)
        AddHandler Me.Shown, Sub(s, ev) ConfigureReceiptSplit(receiptSplit)
        UpdatePreviewVisibility(True)
        ClearSaleMetadata()
    End Sub

    Private Sub ConfigureReceiptSplit(receiptSplit As SplitContainer)
        UiTheme.ConfigureSplitDistance(receiptSplit, 0.32R, 240, 300)
    End Sub

    Private Sub BuildSaleChipPanel()
        pnlSaleChip = New Panel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .BackColor = UiTheme.InfoBackground,
            .Visible = False,
            .Padding = New Padding(UiTheme.PadCard),
            .Margin = New Padding(0, UiTheme.PadControl, 0, 0)
        }
        AddHandler pnlSaleChip.Paint, Sub(s, e)
                                          Using pen As New Pen(UiTheme.ColBorder, 1.0F)
                                              e.Graphics.DrawRectangle(pen, 0, 0, pnlSaleChip.Width - 1, pnlSaleChip.Height - 1)
                                          End Using
                                      End Sub

        Dim chipLayout As New TableLayoutPanel() With {
            .AutoSize = True,
            .Dock = DockStyle.Top,
            .ColumnCount = 1,
            .RowCount = 4,
            .Margin = Padding.Empty
        }
        lblChipSaleId = New Label() With {
            .AutoSize = True,
            .Font = UiTheme.FontSubheading,
            .ForeColor = UiTheme.ColTextPrimary,
            .Margin = New Padding(0, 0, 0, UiTheme.PadTight)
        }
        lblChipDate = New Label() With {
            .AutoSize = True,
            .Font = UiTheme.FontCaption,
            .ForeColor = UiTheme.ColTextSecondary,
            .Margin = New Padding(0, 0, 0, UiTheme.PadTight)
        }
        lblChipTotal = New Label() With {
            .AutoSize = True,
            .Font = UiTheme.FontBodyBold,
            .ForeColor = UiTheme.ColPrimary,
            .Margin = New Padding(0, 0, 0, UiTheme.PadTight)
        }
        lblChipCashier = New Label() With {
            .AutoSize = True,
            .Font = UiTheme.FontCaption,
            .ForeColor = UiTheme.ColTextSecondary,
            .Margin = Padding.Empty
        }
        chipLayout.Controls.Add(lblChipSaleId, 0, 0)
        chipLayout.Controls.Add(lblChipDate, 0, 1)
        chipLayout.Controls.Add(lblChipTotal, 0, 2)
        chipLayout.Controls.Add(lblChipCashier, 0, 3)
        pnlSaleChip.Controls.Add(chipLayout)
    End Sub

    Private Sub BuildEmptyPreviewPanel()
        pnlEmptyPreview = New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = UiTheme.ColBackground,
            .Visible = True
        }

        Dim emptyLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3
        }
        emptyLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 45.0F))
        emptyLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        emptyLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 55.0F))

        Dim lblEmptyTitle As Label = UiTheme.CreateEmptyStateLabel("No receipt loaded")
        lblEmptyTitle.Font = UiTheme.FontHeading
        lblEmptyTitle.Dock = DockStyle.Top
        lblEmptyTitle.TextAlign = ContentAlignment.BottomCenter

        Dim lblEmptySub As Label = UiTheme.CreateEmptyStateLabel("Choose a sale from the history list on the left to preview its receipt.")
        lblEmptySub.Dock = DockStyle.Top
        lblEmptySub.Margin = New Padding(UiTheme.PadSection, UiTheme.PadControl, UiTheme.PadSection, 0)

        emptyLayout.Controls.Add(New Panel(), 0, 0)
        emptyLayout.Controls.Add(lblEmptyTitle, 0, 1)
        emptyLayout.Controls.Add(lblEmptySub, 0, 2)
        pnlEmptyPreview.Controls.Add(emptyLayout)
    End Sub

    Private Sub BuildPreviewArea()
        pnlReceiptScroll = New Panel() With {
            .AutoScroll = True,
            .BackColor = UiTheme.ColBackground,
            .Visible = False
        }

        pnlPageCanvas = New Panel() With {
            .AutoSize = True,
            .BackColor = UiTheme.ColBackground,
            .Padding = New Padding(UiTheme.PadPage)
        }
        AddHandler pnlPageCanvas.Paint, AddressOf pnlPageCanvas_Paint

        pnlReceiptPaper = New Panel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .BackColor = UiTheme.ColSurface,
            .Padding = New Padding(UiTheme.PadSection, UiTheme.PadPage, UiTheme.PadSection, UiTheme.PadPage),
            .BorderStyle = BorderStyle.FixedSingle,
            .MinimumSize = New Size(PreviewReceiptWidth + 40, ReceiptBranding.PreviewMinPaperHeight)
        }
        Dim receiptStack As New TableLayoutPanel() With {
            .AutoSize = True,
            .ColumnCount = 1,
            .Width = PreviewReceiptWidth,
            .BackColor = UiTheme.ColSurface
        }
        receiptStack.Controls.Add(picReceiptLogo, 0, 0)
        receiptStack.Controls.Add(rtbReceipt, 0, 1)
        pnlReceiptPaper.Controls.Add(receiptStack)
        pnlPageCanvas.Controls.Add(pnlReceiptPaper)
        pnlReceiptScroll.Controls.Add(pnlPageCanvas)
    End Sub

    Private Sub LayoutReceiptPaper()
        If pnlReceiptScroll Is Nothing OrElse pnlReceiptPaper Is Nothing OrElse pnlPageCanvas Is Nothing Then
            Return
        End If

        pnlReceiptPaper.PerformLayout()
        pnlPageCanvas.PerformLayout()

        Dim marginPad As Integer = If(chkSimulatePage IsNot Nothing AndAlso chkSimulatePage.Checked, 56, 24)
        pnlPageCanvas.Padding = New Padding(marginPad)

        Dim paperWidth As Integer = Math.Max(pnlReceiptPaper.Width, CInt((PreviewReceiptWidth + 40) * previewZoomScale))
        Dim paperHeight As Integer = Math.Max(pnlReceiptPaper.Height, CInt(ReceiptBranding.PreviewMinPaperHeight * previewZoomScale))
        Dim canvasWidth As Integer = paperWidth + marginPad * 2
        Dim canvasHeight As Integer = paperHeight + marginPad * 2

        Dim left As Integer = Math.Max(12, (pnlReceiptScroll.ClientSize.Width - canvasWidth) \ 2)
        pnlPageCanvas.Location = New Point(left, 12)
        pnlPageCanvas.Size = New Size(canvasWidth, canvasHeight)
        pnlReceiptPaper.Location = New Point(marginPad, marginPad)

        pnlReceiptScroll.AutoScrollMinSize = New Size(left + canvasWidth + 24, canvasHeight + 36)
    End Sub

    Private Sub ShowStatus(message As String, isError As Boolean)
        If statusLabel Is Nothing OrElse statusClearTimer Is Nothing Then
            Return
        End If

        FormStatusHelper.ShowTimedStatus(statusLabel, statusClearTimer, message, isError)
    End Sub

    Private Sub statusClearTimer_Tick(sender As Object, e As EventArgs) Handles statusClearTimer.Tick
        statusClearTimer.Stop()
        FormStatusHelper.ResetTimedStatus(statusLabel)
    End Sub

    Private Sub FillLineGrid(detail As ReceiptSnapshot)
        dgvLines.Rows.Clear()
        If detail Is Nothing OrElse detail.Lines Is Nothing Then
            Return
        End If

        Dim sym As String = detail.CurrencySymbol
        For Each line As ReceiptLineRow In detail.Lines
            dgvLines.Rows.Add(
                line.ProductName,
                line.Quantity,
                sym & line.UnitPrice.ToString("N2", Globalization.CultureInfo.CurrentCulture),
                sym & line.LineTotal.ToString("N2", Globalization.CultureInfo.CurrentCulture))
        Next
    End Sub

    Private Sub ApplyReceiptContent(text As String, isPlaceholder As Boolean)
        If isPlaceholder Then
            picReceiptLogo.Visible = False
            rtbReceipt.Text = text
            rtbReceipt.ForeColor = UiTheme.TextSecondary
            ReceiptBranding.ApplyPreviewCenterAlignment(rtbReceipt)
            UpdatePreviewVisibility(True)
            ClearSaleMetadata()
            Return
        End If

        picReceiptLogo.Visible = True

        Dim previousLogo As Image = picReceiptLogo.Image
        Dim logo As Image = ReceiptBranding.TryGetReceiptLogo()
        If logo IsNot Nothing Then
            picReceiptLogo.Image = logo
            previousLogo?.Dispose()
        Else
            picReceiptLogo.Visible = False
            previousLogo?.Dispose()
            picReceiptLogo.Image = Nothing
        End If

        rtbReceipt.Text = ReceiptBranding.GetReceiptText(text)
        rtbReceipt.ForeColor = Color.Black
        ReceiptBranding.ApplyPreviewCenterAlignment(rtbReceipt)
        ApplyPreviewZoom()
        ResizeReceiptTextBox()
        UpdatePreviewVisibility(False)
        BeginInvoke(New Action(AddressOf LayoutReceiptPaper))
    End Sub

    Private Sub ResizeReceiptTextBox()
        rtbReceipt.Width = CInt(PreviewReceiptWidth * previewZoomScale)

        Dim normalized As String = rtbReceipt.Text.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf)
        Dim lines As String() = normalized.Split(ChrW(10))
        Dim lineCount As Integer = Math.Max(1, lines.Length)

        Using g As Graphics = rtbReceipt.CreateGraphics()
            Dim lineHeight As Single = g.MeasureString("Ag", rtbReceipt.Font, Integer.MaxValue, StringFormat.GenericTypographic).Height
            lineHeight *= ReceiptBranding.PreviewLineSpacingScale
            Dim desired As Integer = CInt(Math.Ceiling(lineCount * lineHeight)) + 28
            rtbReceipt.Height = Math.Max(ReceiptBranding.PreviewMinPaperHeight - 140, desired)
        End Using

        rtbReceipt.ScrollBars = RichTextBoxScrollBars.Both
        LayoutReceiptPaper()
    End Sub

    Private Sub UpdatePreviewVisibility(isEmpty As Boolean)
        If pnlEmptyPreview IsNot Nothing Then
            pnlEmptyPreview.Visible = isEmpty
            If isEmpty Then
                pnlEmptyPreview.BringToFront()
            End If
        End If
        If pnlReceiptScroll IsNot Nothing Then
            pnlReceiptScroll.Visible = Not isEmpty
            If Not isEmpty Then
                pnlReceiptScroll.BringToFront()
            End If
        End If
    End Sub

    Private Sub ClearSaleMetadata()
        pnlSaleChip.Visible = False
        lblChipSaleId.Text = String.Empty
        lblChipDate.Text = String.Empty
        lblChipTotal.Text = String.Empty
        lblChipCashier.Text = String.Empty
        lblSaleMeta.Text = String.Empty
    End Sub

    Private Sub UpdateSaleMetadataFromSnapshot(detail As ReceiptSnapshot, saleId As Integer)
        If detail Is Nothing Then
            ClearSaleMetadata()
            Return
        End If

        Dim sym As String = If(detail.CurrencySymbol, AppSettings.Current.CurrencySymbol)
        Dim cashier As String = If(String.IsNullOrWhiteSpace(detail.CashierName), "—", detail.CashierName.Trim())
        Dim saleLabel As String = If(saleId >= 0, "Sale #" & saleId.ToString(CultureInfo.InvariantCulture), "Current sale")
        Dim saleWhen As DateTime = ReceiptBranding.NormalizeStoredSaleDate(
            If(detail.SaleDateTime = DateTime.MinValue, DateTime.UtcNow, detail.SaleDateTime))
        ApplySaleMetadata(saleLabel, saleWhen, detail.GrandTotal, sym, cashier)
    End Sub

    Private Shared Function ReadStoredSaleDate(raw As Object) As DateTime
        If raw Is Nothing OrElse raw Is DBNull.Value Then
            Return DateTime.MinValue
        End If

        Return ReceiptBranding.NormalizeStoredSaleDate(Convert.ToDateTime(raw, CultureInfo.InvariantCulture))
    End Function

    Private Sub ApplyLoadedReceipt(saleId As Integer, storedSaleDate As Object, storedReceiptText As Object, totalAmount As Object)
        Dim sdt As DateTime = ReadStoredSaleDate(storedSaleDate)
        Dim total As Decimal = If(totalAmount Is Nothing OrElse totalAmount Is DBNull.Value,
            0D,
            Convert.ToDecimal(totalAmount, CultureInfo.InvariantCulture))
        receiptText = Convert.ToString(storedReceiptText)
        If sdt <> DateTime.MinValue Then
            receiptText = ReceiptBranding.AlignReceiptDateLine(receiptText, sdt)
        End If

        ApplyReceiptContent(receiptText, False)
        UpdateSaleMetadataFromText(saleId, sdt, receiptText, total)
    End Sub

    Private Sub UpdateSaleMetadataFromText(saleId As Integer, saleDate As DateTime, receiptText As String, totalAmount As Decimal?)
        Dim sym As String = AppSettings.Current.CurrencySymbol
        Dim total As Decimal = If(totalAmount.HasValue, totalAmount.Value, TryParseTotalFromReceipt(receiptText, sym))
        Dim cashier As String = TryParseCashierFromReceipt(receiptText)
        Dim saleLabel As String = If(saleId >= 0, "Sale #" & saleId.ToString(CultureInfo.InvariantCulture), "Current sale")
        ApplySaleMetadata(saleLabel, saleDate, total, sym, cashier)
    End Sub

    Private Sub ApplySaleMetadata(saleLabel As String, saleDate As DateTime, total As Decimal, currencySymbol As String, cashier As String)
        lblSaleMeta.Text = saleLabel

        lblChipSaleId.Text = saleLabel
        lblChipDate.Text = saleDate.ToString("MMMM d, yyyy  h:mm tt", CultureInfo.CurrentCulture)
        lblChipTotal.Text = "Total: " & currencySymbol & total.ToString("N2", CultureInfo.CurrentCulture)
        lblChipCashier.Text = "Cashier: " & cashier
        pnlSaleChip.Visible = True
    End Sub

    Private Shared Function TryParseCashierFromReceipt(receiptText As String) As String
        If String.IsNullOrWhiteSpace(receiptText) Then
            Return "—"
        End If

        For Each line As String In receiptText.Replace(vbCrLf, vbLf).Split(ChrW(10))
            Dim trimmed As String = line.Trim()
            If trimmed.StartsWith("Cashier:", StringComparison.OrdinalIgnoreCase) Then
                Dim name As String = trimmed.Substring("Cashier:".Length).Trim()
                Return If(String.IsNullOrWhiteSpace(name), "—", name)
            End If
        Next

        Return "—"
    End Function

    Private Shared Function TryParseTotalFromReceipt(receiptText As String, currencySymbol As String) As Decimal
        If String.IsNullOrWhiteSpace(receiptText) Then
            Return 0D
        End If

        Dim pattern As String = "TOTAL DUE:\s*" & Regex.Escape(currencySymbol) & "?\s*([\d,]+\.\d{2})"
        Dim match As Match = Regex.Match(receiptText, pattern, RegexOptions.IgnoreCase)
        If match.Success Then
            Dim raw As String = match.Groups(1).Value.Replace(",", String.Empty)
            Dim value As Decimal
            If Decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, value) Then
                Return value
            End If
        End If

        Return 0D
    End Function

    Private Function GetFullReceiptSource() As String
        If Not String.IsNullOrWhiteSpace(receiptText) Then
            Return receiptText
        End If

        Return rtbReceipt.Text
    End Function

    Private Sub LoadHistoryCombo(Optional reset As Boolean = True)
        If reset Then
            allHistoryItems.Clear()
            allHistoryItems.Add(New SaleListItem With {.SaleId = -1, .SaleDate = DateTime.MinValue, .TotalAmount = 0D})
            historySkip = 0
            historyHasMore = False
        End If

        Dim loadedThisPage As Integer = 0

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Dim query As String =
                    "SELECT sale_id, sale_date, total_amount, receipt_text, ISNULL(is_voided, 0) AS is_voided " &
                    "FROM sales WHERE receipt_text IS NOT NULL AND receipt_text <> '' " &
                    "ORDER BY sale_date DESC, sale_id DESC " &
                    "OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;"

                Using command As New SqlCommand(query, connection)
                    command.Parameters.AddWithValue("@skip", historySkip)
                    command.Parameters.AddWithValue("@take", HistoryPageSize)
                    Using reader As SqlDataReader = command.ExecuteReader()
                        While reader.Read()
                            Dim receiptSnippet As String = reader("receipt_text").ToString()
                            allHistoryItems.Add(New SaleListItem With {
                                .SaleId = Convert.ToInt32(reader("sale_id")),
                                .SaleDate = ReadStoredSaleDate(reader("sale_date")),
                                .TotalAmount = Convert.ToDecimal(reader("total_amount")),
                                .CashierHint = TryParseCashierFromReceipt(receiptSnippet),
                                .IsVoided = Convert.ToBoolean(reader("is_voided"))
                            })
                            loadedThisPage += 1
                        End While
                    End Using
                End Using
            End Using
        Catch
        End Try

        historySkip += loadedThisPage
        historyHasMore = loadedThisPage >= HistoryPageSize
        UpdateLoadMoreButton()
        ApplyHistoryFilters()
    End Sub

    Private Sub UpdateLoadMoreButton()
        If btnLoadMore Is Nothing Then
            Return
        End If

        btnLoadMore.Enabled = historyHasMore
        btnLoadMore.Text = If(historyHasMore, "Load more", "All loaded")
    End Sub

    Private Sub UpdateVoidButtonState()
        If btnVoid Is Nothing Then
            Return
        End If

        btnVoid.Visible = AppSession.IsAdmin()
        btnVoid.Enabled = AppSession.IsAdmin() AndAlso currentSaleId > 0 AndAlso Not currentSaleVoided
    End Sub

    Private Sub ApplyHistoryFilters()
        Dim previousId As Integer = -1
        If lstHistory.SelectedItem IsNot Nothing Then
            Dim selected As SaleListItem = TryCast(lstHistory.SelectedItem, SaleListItem)
            If selected IsNot Nothing Then
                previousId = selected.SaleId
            End If
        End If

        Dim searchTerm As String = GetHistorySearchTerm()
        Dim filterKind As HistoryDateFilter = CType(Math.Max(0, cmbDateFilter.SelectedIndex), HistoryDateFilter)
        pnlCustomRange.Visible = filterKind = HistoryDateFilter.CustomRange

        Dim filtered = allHistoryItems.Where(Function(item) ItemPassesDateFilter(item, filterKind) AndAlso item.MatchesSearch(searchTerm)).ToList()
        Select Case CType(Math.Max(0, cmbSort.SelectedIndex), HistorySortOption)
            Case HistorySortOption.OldestFirst
                filtered = filtered.OrderBy(Function(i) i.SaleDate).ThenBy(Function(i) i.SaleId).ToList()
            Case HistorySortOption.AmountHigh
                filtered = filtered.OrderByDescending(Function(i) i.TotalAmount).ThenByDescending(Function(i) i.SaleDate).ToList()
            Case HistorySortOption.AmountLow
                filtered = filtered.OrderBy(Function(i) i.TotalAmount).ThenBy(Function(i) i.SaleDate).ToList()
            Case Else
                filtered = filtered.OrderByDescending(Function(i) i.SaleDate).ThenByDescending(Function(i) i.SaleId).ToList()
        End Select

        suppressHistoryEvent = True
        cmbHistory.Items.Clear()
        lstHistory.Items.Clear()
        For Each item As SaleListItem In filtered
            cmbHistory.Items.Add(item)
            lstHistory.Items.Add(item)
        Next

        Dim pickIndex As Integer = 0
        If previousId >= 0 Then
            For i As Integer = 0 To lstHistory.Items.Count - 1
                Dim row As SaleListItem = TryCast(lstHistory.Items(i), SaleListItem)
                If row IsNot Nothing AndAlso row.SaleId = previousId Then
                    pickIndex = i
                    Exit For
                End If
            Next
        End If

        If lstHistory.Items.Count > 0 Then
            lstHistory.SelectedIndex = pickIndex
            cmbHistory.SelectedIndex = pickIndex
        End If

        suppressHistoryEvent = False
        UpdateHistoryEmptyState()
    End Sub

    Private Sub UpdateHistoryEmptyState()
        If lstHistory Is Nothing OrElse lblHistoryEmpty Is Nothing Then
            Return
        End If

        Dim isEmpty As Boolean = lstHistory.Items.Count = 0
        lblHistoryEmpty.Visible = isEmpty
        lstHistory.Visible = Not isEmpty
    End Sub

    Private Function GetHistorySearchTerm() As String
        If txtHistorySearch Is Nothing Then
            Return String.Empty
        End If

        Return txtHistorySearch.Text.Trim()
    End Function

    Private Function ItemPassesDateFilter(item As SaleListItem, filterKind As HistoryDateFilter) As Boolean
        If item.SaleId < 0 Then
            Return True
        End If

        Select Case filterKind
            Case HistoryDateFilter.Today
                Return item.SaleDate.Date = DateTime.Today
            Case HistoryDateFilter.ThisWeek
                Dim startOfWeek As DateTime = DateTime.Today.AddDays(-(CInt(DateTime.Today.DayOfWeek) + 6) Mod 7)
                Return item.SaleDate.Date >= startOfWeek AndAlso item.SaleDate.Date <= DateTime.Today
            Case HistoryDateFilter.ThisMonth
                Return item.SaleDate.Year = DateTime.Today.Year AndAlso item.SaleDate.Month = DateTime.Today.Month
            Case HistoryDateFilter.CustomRange
                Return item.SaleDate.Date >= dtpFilterFrom.Value.Date AndAlso item.SaleDate.Date <= dtpFilterTo.Value.Date
            Case Else
                Return True
        End Select
    End Function

    Private Sub HistoryFilterChanged(sender As Object, e As EventArgs)
        ApplyHistoryFilters()
        If Not suppressHistoryEvent Then
            ProcessHistorySelection()
        End If
    End Sub

    Private Sub btnBack_Click(sender As Object, e As EventArgs) Handles btnBack.Click
        Me.Close()
    End Sub

    Private Sub btnLoadList_Click(sender As Object, e As EventArgs) Handles btnLoadList.Click
        suppressHistoryEvent = True
        LoadHistoryCombo(reset:=True)
        If lstHistory.Items.Count > 0 Then
            lstHistory.SelectedIndex = 0
            cmbHistory.SelectedIndex = 0
        End If

        suppressHistoryEvent = False
        ProcessHistorySelection()
        ShowStatus("Sales list refreshed.", False)
    End Sub

    Private Sub btnLoadMore_Click(sender As Object, e As EventArgs) Handles btnLoadMore.Click
        LoadHistoryCombo(reset:=False)
        ShowStatus("Loaded more receipts.", False)
    End Sub

    Private Sub btnVoid_Click(sender As Object, e As EventArgs) Handles btnVoid.Click
        If Not AppSession.IsAdmin() Then
            Return
        End If

        If currentSaleId <= 0 Then
            MessageBox.Show("Select a saved sale to void.", "Void sale", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        If currentSaleVoided Then
            MessageBox.Show("This sale is already voided.", "Void sale", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        If Not UiTheme.ConfirmAction(
            "Void sale #" & currentSaleId.ToString(CultureInfo.InvariantCulture) &
            "? Stock will be restored and the sale excluded from revenue reports.") Then
            Return
        End If

        Try
            VoidSale(currentSaleId)
            LoadHistoryCombo(reset:=True)
            LoadReceiptBySaleId(currentSaleId)
            ShowStatus("Sale voided. Stock restored.", False)
        Catch ex As Exception
            MessageBox.Show("Could not void sale: " & ex.Message, "Void sale", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ErrorLogger.Log(ex, NameOf(ReceiptForm) & "." & NameOf(btnVoid_Click))
        End Try
    End Sub

    Private Sub VoidSale(saleId As Integer)
        Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
            connection.Open()

            Using transaction As SqlTransaction = connection.BeginTransaction()
                Dim alreadyVoided As Boolean
                Using checkCmd As New SqlCommand("SELECT ISNULL(is_voided, 0) FROM sales WHERE sale_id = @id;", connection, transaction)
                    checkCmd.Parameters.AddWithValue("@id", saleId)
                    Dim result As Object = checkCmd.ExecuteScalar()
                    If result Is Nothing OrElse result Is DBNull.Value Then
                        Throw New InvalidOperationException("Sale not found.")
                    End If

                    alreadyVoided = Convert.ToBoolean(result)
                End Using

                If alreadyVoided Then
                    Throw New InvalidOperationException("Sale is already voided.")
                End If

                Dim items As New List(Of (Name As String, Qty As Integer))()
                Using itemsCmd As New SqlCommand(
                    "SELECT product_name, quantity FROM sale_items WHERE sale_id = @id;",
                    connection,
                    transaction)
                    itemsCmd.Parameters.AddWithValue("@id", saleId)
                    Using reader As SqlDataReader = itemsCmd.ExecuteReader()
                        While reader.Read()
                            items.Add((reader("product_name").ToString(), Convert.ToInt32(reader("quantity"))))
                        End While
                    End Using
                End Using

                For Each item In items
                    Using restoreCmd As New SqlCommand(
                        "UPDATE products SET stock_quantity = stock_quantity + @qty, updated_at = SYSUTCDATETIME() " &
                        "WHERE product_name = @name;",
                        connection,
                        transaction)
                        restoreCmd.Parameters.AddWithValue("@name", item.Name)
                        restoreCmd.Parameters.AddWithValue("@qty", item.Qty)
                        restoreCmd.ExecuteNonQuery()
                    End Using
                Next

                Using voidCmd As New SqlCommand(
                    "UPDATE sales SET is_voided = 1, receipt_text = receipt_text + @banner WHERE sale_id = @id;",
                    connection,
                    transaction)
                    voidCmd.Parameters.AddWithValue("@id", saleId)
                    voidCmd.Parameters.AddWithValue("@banner", Environment.NewLine & Environment.NewLine & "*** VOIDED ***")
                    voidCmd.ExecuteNonQuery()
                End Using

                AuditLogger.LogSale(connection, "VOID", saleId, "Sale voided from ReceiptForm")
                AuditLogger.LogAudit(
                    connection,
                    "SALE_VOIDED",
                    "Voided sale #" & saleId.ToString(CultureInfo.InvariantCulture),
                    AppSession.CurrentRole)

                transaction.Commit()
            End Using
        End Using
    End Sub

    Private Sub lstHistory_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lstHistory.SelectedIndexChanged
        If suppressHistoryEvent Then
            Return
        End If

        suppressHistoryEvent = True
        cmbHistory.SelectedIndex = lstHistory.SelectedIndex
        suppressHistoryEvent = False
        ProcessHistorySelection()
    End Sub

    Private Sub cmbHistory_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbHistory.SelectedIndexChanged
        If suppressHistoryEvent Then
            Return
        End If

        ProcessHistorySelection()
    End Sub

    Private Sub ProcessHistorySelection()
        If cmbHistory.SelectedItem Is Nothing Then
            Return
        End If

        Dim item As SaleListItem = TryCast(cmbHistory.SelectedItem, SaleListItem)
        If item Is Nothing Then
            Return
        End If

        currentSaleId = item.SaleId
        If item.SaleId < 0 Then
            LoadLatestReceiptFromDb()
        Else
            LoadReceiptBySaleId(item.SaleId)
        End If
    End Sub

    Private Sub LoadLatestReceiptFromDb()
        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Dim query As String =
                    "SELECT TOP 1 sale_id, sale_date, receipt_text, total_amount, ISNULL(is_voided, 0) AS is_voided " &
                    "FROM sales " &
                    "WHERE receipt_text IS NOT NULL AND receipt_text <> '' " &
                    "ORDER BY sale_date DESC, sale_id DESC;"

                Using command As New SqlCommand(query, connection)
                    Using reader As SqlDataReader = command.ExecuteReader()
                        If reader.Read() Then
                            Dim sid As Integer = Convert.ToInt32(reader("sale_id"))
                            currentSaleVoided = Convert.ToBoolean(reader("is_voided"))
                            ApplyLoadedReceipt(sid, reader("sale_date"), reader("receipt_text"), reader("total_amount"))
                            LoadSaleLinesIntoGrid(connection, sid)
                            UpdateVoidButtonState()
                        Else
                            dgvLines.Rows.Clear()
                            ApplyReceiptContent("No saved receipt found. Finalize a sale from the Sales / Cart screen first.", True)
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            ApplyReceiptContent("Could not load receipt. Check App.config and LocalDB." & Environment.NewLine & ex.Message, True)
            MessageBox.Show("Error loading receipt: " & ex.Message, "Database", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ErrorLogger.Log(ex, NameOf(ReceiptForm) & "." & NameOf(LoadLatestReceiptFromDb))
        End Try
    End Sub

    Private Sub LoadReceiptBySaleId(saleId As Integer)
        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Dim query As String =
                    "SELECT sale_id, sale_date, receipt_text, total_amount, ISNULL(is_voided, 0) AS is_voided FROM sales WHERE sale_id = @id;"

                Using command As New SqlCommand(query, connection)
                    command.Parameters.AddWithValue("@id", saleId)
                    Using reader As SqlDataReader = command.ExecuteReader()
                        If reader.Read() Then
                            currentSaleVoided = Convert.ToBoolean(reader("is_voided"))
                            ApplyLoadedReceipt(
                                Convert.ToInt32(reader("sale_id")),
                                reader("sale_date"),
                                reader("receipt_text"),
                                reader("total_amount"))
                            UpdateVoidButtonState()
                        Else
                            dgvLines.Rows.Clear()
                            ApplyReceiptContent("Receipt not found for this sale id.", True)
                            Return
                        End If
                    End Using
                End Using

                LoadSaleLinesIntoGrid(connection, saleId)
            End Using
        Catch ex As Exception
            ApplyReceiptContent(ex.Message, True)
            MessageBox.Show("Error loading receipt: " & ex.Message, "Database", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ErrorLogger.Log(ex, NameOf(ReceiptForm) & "." & NameOf(LoadReceiptBySaleId))
        End Try
    End Sub

    Private Sub LoadSaleLinesIntoGrid(connection As SqlConnection, saleId As Integer)
        dgvLines.Rows.Clear()
        Dim sym As String = AppSettings.Current.CurrencySymbol

        Dim sql As String =
            "SELECT product_name, price, quantity, subtotal FROM sale_items WHERE sale_id = @sid ORDER BY sale_item_id;"
        Using cmd As New SqlCommand(sql, connection)
            cmd.Parameters.AddWithValue("@sid", saleId)
            Using reader As SqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim pname As String = reader("product_name").ToString()
                    Dim qty As Integer = Convert.ToInt32(reader("quantity"))
                    Dim price As Decimal = Convert.ToDecimal(reader("price"))
                    Dim lineTotal As Decimal = Convert.ToDecimal(reader("subtotal"))
                    dgvLines.Rows.Add(pname, qty, sym & price.ToString("N2", Globalization.CultureInfo.CurrentCulture), sym & lineTotal.ToString("N2", Globalization.CultureInfo.CurrentCulture))
                End While
            End Using
        End Using
    End Sub

    Private Sub btnPrint_Click(sender As Object, e As EventArgs) Handles btnPrint.Click
        PrintReceipt(False)
    End Sub

    Private Sub btnPrintPreview_Click(sender As Object, e As EventArgs) Handles btnPrintPreview.Click
        PrintReceipt(True)
    End Sub

    Private Sub btnReprint_Click(sender As Object, e As EventArgs) Handles btnReprint.Click
        PrintReceipt(False)
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        If rtbReceipt.Text.Trim().Length = 0 Then
            MessageBox.Show("Nothing to save.", "Receipt", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim defaultName As String = "GroupC_Receipt_" & DateTime.Now.ToString("yyyyMMdd_HHmm") & ".txt"

        Using saveDialog As New SaveFileDialog()
            saveDialog.Filter = "Text Files (*.txt)|*.txt"
            saveDialog.FileName = defaultName

            If saveDialog.ShowDialog() = DialogResult.OK Then
                File.WriteAllText(saveDialog.FileName, ReceiptBranding.GetReceiptText(GetFullReceiptSource()))
                ShowStatus("Receipt saved as text file.", False)
            End If
        End Using
    End Sub

    Private Sub btnSavePdf_Click(sender As Object, e As EventArgs) Handles btnSavePdf.Click
        If rtbReceipt.Text.Trim().Length = 0 Then
            MessageBox.Show("Nothing to export.", "Receipt", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim defaultName As String = "GroupC_Receipt_" & DateTime.Now.ToString("yyyyMMdd_HHmm") & ".pdf"
        Using saveDialog As New SaveFileDialog()
            saveDialog.Filter = "PDF Files (*.pdf)|*.pdf"
            saveDialog.FileName = defaultName
            If saveDialog.ShowDialog() <> DialogResult.OK Then
                Return
            End If

            Try
                PdfReceiptExporter.ExportTextToPdf(saveDialog.FileName, If(receiptText, rtbReceipt.Text))
                ShowStatus("PDF exported.", False)
            Catch ex As Exception
                ShowStatus("PDF export failed.", True)
                MessageBox.Show("Could not save PDF: " & ex.Message, "Receipt", MessageBoxButtons.OK, MessageBoxIcon.Error)
                ErrorLogger.Log(ex, NameOf(ReceiptForm) & "." & NameOf(btnSavePdf_Click))
            End Try
        End Using
    End Sub

    Private Sub btnCopy_Click(sender As Object, e As EventArgs) Handles btnCopy.Click
        If rtbReceipt.Text.Trim().Length = 0 Then
            Return
        End If

        Clipboard.SetText(ReceiptBranding.GetReceiptText(GetFullReceiptSource()))
        ShowStatus("Copied to clipboard.", False)
    End Sub

    Private Sub printDocument_BeginPrint(sender As Object, e As PrintEventArgs) Handles printDocument.BeginPrint
        printHelper = New ReceiptPrintHelper(GetFullReceiptSource())
        printHelper.BeginPrint()
    End Sub

    Private Sub printDocument_PrintPage(sender As Object, e As PrintPageEventArgs) Handles printDocument.PrintPage
        If printHelper Is Nothing Then
            e.Cancel = True
            Return
        End If

        Using receiptFont As New Font("Courier New", 10.0F)
            printHelper.PrintPage(e, receiptFont)
        End Using
    End Sub

    Private Sub btnEmail_Click(sender As Object, e As EventArgs) Handles btnEmail.Click
        If Not EnsureReceiptReady() Then
            Return
        End If

        Dim subject As String = Uri.EscapeDataString(If(currentSaleId > 0, "Receipt #" & currentSaleId.ToString(CultureInfo.InvariantCulture), "Receipt"))
        Dim bodyText As String = ReceiptBranding.GetReceiptText(GetFullReceiptSource())
        Try
            Clipboard.SetText(bodyText)
            Process.Start(New ProcessStartInfo("mailto:?subject=" & subject) With {.UseShellExecute = True})
            ShowStatus("Receipt copied to clipboard. Paste it into your email message.", False)
        Catch ex As Exception
            MessageBox.Show("Could not open email: " & ex.Message, "Receipt", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub btnDetails_Click(sender As Object, e As EventArgs) Handles btnDetails.Click
        If currentSaleId <= 0 AndAlso dgvLines.Rows.Count = 0 Then
            MessageBox.Show("Select a saved sale to view details.", "Receipt", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using details As New Form()
            details.Text = AppBranding.WindowTitle("Sale details")
            details.StartPosition = FormStartPosition.CenterParent
            details.Size = New Size(720, 480)
            details.MinimumSize = New Size(640, 400)
            Dim grid As New DataGridView() With {.Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False}
            grid.Columns.Add("ProductName", "Product")
            grid.Columns.Add("Qty", "Qty")
            grid.Columns.Add("UnitPrice", "Unit price")
            grid.Columns.Add("LineTotal", "Line total")
            UiTheme.ApplyDataGridViewChrome(grid)

            For Each row As DataGridViewRow In dgvLines.Rows
                If row.IsNewRow Then
                    Continue For
                End If
                grid.Rows.Add(row.Cells(0).Value, row.Cells(1).Value, row.Cells(2).Value, row.Cells(3).Value)
            Next

            Dim meta As New Label() With {
                .Dock = DockStyle.Top,
                .Height = 72,
                .Padding = New Padding(12, 8, 12, 8),
                .Text = lblChipSaleId.Text & Environment.NewLine & lblChipDate.Text & Environment.NewLine & lblChipTotal.Text & Environment.NewLine & lblChipCashier.Text,
                .Font = New Font("Segoe UI", 10.0F)
            }
            details.Controls.Add(grid)
            details.Controls.Add(meta)
            details.ShowDialog(Me)
        End Using
    End Sub

    Private Sub btnDuplicate_Click(sender As Object, e As EventArgs) Handles btnDuplicate.Click
        If currentSaleId <= 0 Then
            MessageBox.Show("Select a saved sale to duplicate.", "Receipt", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using sales As New SalesForm()
            sales.LoadCartFromSaleId(currentSaleId)
            sales.ShowDialog(Me)
        End Using
    End Sub

    Private Sub btnExportBatch_Click(sender As Object, e As EventArgs) Handles btnExportBatch.Click
        If lstHistory.Items.Count = 0 Then
            MessageBox.Show("No receipts in the current list.", "Export batch", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using folderDialog As New FolderBrowserDialog() With {.Description = "Choose folder for receipt text files"}
            If folderDialog.ShowDialog() <> DialogResult.OK Then
                Return
            End If

            Dim exported As Integer = 0
            For Each obj As Object In lstHistory.Items
                Dim item As SaleListItem = TryCast(obj, SaleListItem)
                If item Is Nothing OrElse item.SaleId < 0 Then
                    Continue For
                End If

                Try
                    Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                        connection.Open()
                        Using command As New SqlCommand("SELECT receipt_text FROM sales WHERE sale_id = @id;", connection)
                            command.Parameters.AddWithValue("@id", item.SaleId)
                            Dim text As String = Convert.ToString(command.ExecuteScalar())
                            If String.IsNullOrWhiteSpace(text) Then
                                Continue For
                            End If

                            Dim path As String = IO.Path.Combine(
                                folderDialog.SelectedPath,
                                "Receipt_" & item.SaleId.ToString("D6", CultureInfo.InvariantCulture) & ".txt")
                            File.WriteAllText(path, ReceiptBranding.GetReceiptText(text))
                            exported += 1
                        End Using
                    End Using
                Catch
                End Try
            Next

            ShowStatus(exported.ToString(CultureInfo.InvariantCulture) & " receipt(s) exported.", False)
            MessageBox.Show(exported.ToString(CultureInfo.InvariantCulture) & " file(s) saved.", "Export batch", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Using
    End Sub

    Private Sub btnZoomIn_Click(sender As Object, e As EventArgs)
        previewZoomScale = Math.Min(MaxPreviewZoom, previewZoomScale + PreviewZoomStep)
        ApplyPreviewZoom()
    End Sub

    Private Sub btnZoomOut_Click(sender As Object, e As EventArgs)
        previewZoomScale = Math.Max(MinPreviewZoom, previewZoomScale - PreviewZoomStep)
        ApplyPreviewZoom()
    End Sub

    Private Sub chkSimulatePage_CheckedChanged(sender As Object, e As EventArgs)
        LayoutReceiptPaper()
    End Sub

    Private Sub ApplyPreviewZoom()
        If rtbReceipt Is Nothing Then
            Return
        End If

        Dim fontSize As Single = 10.0F * previewZoomScale
        rtbReceipt.Font = New Font("Courier New", fontSize)
        If picReceiptLogo IsNot Nothing Then
            picReceiptLogo.Height = CInt(ReceiptBranding.ReceiptLogoHeight * previewZoomScale)
            picReceiptLogo.Width = CInt(PreviewReceiptWidth * previewZoomScale)
        End If

        If lblZoomPct IsNot Nothing Then
            lblZoomPct.Text = CInt(previewZoomScale * 100.0F).ToString(CultureInfo.InvariantCulture) & "%"
        End If

        If rtbReceipt.Text.Trim().Length > 0 AndAlso rtbReceipt.ForeColor <> UiTheme.TextSecondary Then
            rtbReceipt.ForeColor = Color.Black
            ReceiptBranding.ApplyPreviewCenterAlignment(rtbReceipt)
        End If

        ResizeReceiptTextBox()
    End Sub

    Private Function CreateToolbarButton(caption As String, primary As Boolean) As Button
        Dim btn As New Button() With {
            .Text = caption,
            .AutoSize = True,
            .MinimumSize = New Size(88, UiTheme.ButtonHeight),
            .Padding = New Padding(UiTheme.PadControl, UiTheme.PadTight, UiTheme.PadControl, UiTheme.PadTight),
            .Margin = New Padding(0, 0, UiTheme.PadControl, UiTheme.PadControl),
            .Cursor = Cursors.Hand
        }
        If primary Then
            UiTheme.ApplyPrimaryButton(btn)
        Else
            UiTheme.ApplySecondaryButton(btn)
        End If
        Return btn
    End Function

    Private Function BuildReceiptContextMenu() As ContextMenuStrip
        Dim menu As New ContextMenuStrip()
        menu.Items.Add(New ToolStripMenuItem("Print", Nothing, Sub() PrintReceipt(False)))
        menu.Items.Add(New ToolStripMenuItem("Print preview", Nothing, Sub() PrintReceipt(True)))
        menu.Items.Add(New ToolStripMenuItem("Save as PDF", Nothing, Sub() btnSavePdf.PerformClick()))
        menu.Items.Add(New ToolStripMenuItem("Save as text", Nothing, Sub() btnSave.PerformClick()))
        menu.Items.Add(New ToolStripMenuItem("Copy", Nothing, Sub() btnCopy.PerformClick()))
        menu.Items.Add(New ToolStripSeparator())
        menu.Items.Add(New ToolStripMenuItem("Email receipt", Nothing, Sub() btnEmail.PerformClick()))
        menu.Items.Add(New ToolStripMenuItem("View details", Nothing, Sub() btnDetails.PerformClick()))
        menu.Items.Add(New ToolStripMenuItem("Duplicate sale", Nothing, Sub() btnDuplicate.PerformClick()))
        Return menu
    End Function

    Private Function EnsureReceiptReady() As Boolean
        If rtbReceipt.Text.Trim().Length = 0 OrElse rtbReceipt.ForeColor.ToArgb() = UiTheme.TextSecondary.ToArgb() Then
            MessageBox.Show("Nothing to use for this action.", "Receipt", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End If
        Return True
    End Function

    Private Sub PrintReceipt(usePreview As Boolean)
        If Not EnsureReceiptReady() Then
            Return
        End If

        If usePreview Then
            Using preview As New PrintPreviewDialog()
                preview.Document = printDocument
                preview.WindowState = FormWindowState.Maximized
                preview.ShowDialog(Me)
                ShowStatus("Print preview closed.", False)
            End Using
            Return
        End If

        Using printDialog As New PrintDialog()
            printDialog.Document = printDocument
            If printDialog.ShowDialog() = DialogResult.OK Then
                printDocument.Print()
                ShowStatus("Print job sent.", False)
            End If
        End Using
    End Sub

    Private Sub pnlPageCanvas_Paint(sender As Object, e As PaintEventArgs)
        If chkSimulatePage Is Nothing OrElse Not chkSimulatePage.Checked Then
            Return
        End If

        Dim g As Graphics = e.Graphics
        Using marginPen As New Pen(Color.FromArgb(190, 198, 210), 1.0F)
            marginPen.DashStyle = DashStyle.Dot
            Dim r As Rectangle = pnlReceiptPaper.Bounds
            r.Inflate(12, 12)
            g.DrawRectangle(marginPen, r)
        End Using

        Using hintFont As New Font("Segoe UI", 8.0F, FontStyle.Italic)
            Using hintBrush As New SolidBrush(Color.FromArgb(120, 130, 145))
                g.DrawString("Simulated page margins", hintFont, hintBrush, 8, 6)
            End Using
        End Using
    End Sub

End Class
