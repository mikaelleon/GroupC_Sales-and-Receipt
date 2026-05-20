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

    Private Const LeftPanelWidth As Integer = 360
    Private Const PreviewReceiptWidth As Integer = 480
    Private Const HistoryListWidth As Integer = 312
    Private Const MinPreviewZoom As Single = 0.75F
    Private Const MaxPreviewZoom As Single = 1.5F
    Private Const PreviewZoomStep As Single = 0.1F
    Private Shared ReadOnly SurfaceGray As Color = Color.FromArgb(&HF5, &HF7, &HFA)
    Private Shared ReadOnly BorderLight As Color = Color.FromArgb(&HD0, &HDC, &HE8)
    Private Shared ReadOnly BrandBlueLight As Color = Color.FromArgb(&HE8, &HF4, &HFC)

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
            Return String.Format(
                CultureInfo.CurrentCulture,
                "#{0}  {1:MMM d, h:mm tt}  {2}{3:N2}",
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
    Private pnlEmptyPreview As Panel
    Private pnlReceiptScroll As Panel
    Private pnlReceiptPaper As Panel
    Private pnlLeft As Panel
    Private pnlRight As Panel
    Private pnlBottomBar As Panel
    Private lblStatus As Label
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
        UiTheme.ApplyMaximizedWorkspaceDefaults(Me)

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
            ApplyReceiptContent(receiptText, False)
            UpdateSaleMetadataFromSnapshot(snapshot, saleIdForMeta)
            suppressHistoryEvent = False

        ElseIf receiptText.Trim().Length > 0 Then
            ApplyReceiptContent(receiptText, False)
            UpdateSaleMetadataFromText(saleIdForMeta, DateTime.Now, receiptText, Nothing)
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
        Me.BackColor = SurfaceGray

        printDocument = New PrintDocument()

        cmbHistory = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Visible = False, .Size = New Size(0, 0)}
        lstHistory = New ListBox() With {
            .IntegralHeight = False,
            .Height = 180,
            .Width = HistoryListWidth,
            .BorderStyle = BorderStyle.FixedSingle,
            .Font = New Font("Segoe UI", 10.0F),
            .BackColor = Color.White,
            .HorizontalScrollbar = True
        }
        txtHistorySearch = New TextBox() With {
            .Width = HistoryListWidth,
            .Font = New Font("Segoe UI", 10.0F),
            .ForeColor = UiTheme.TextSecondary,
            .Text = "Search receipt #, amount, date…"
        }
        AddHandler txtHistorySearch.GotFocus, Sub()
                                                  If txtHistorySearch.ForeColor = UiTheme.TextSecondary Then
                                                      txtHistorySearch.Text = String.Empty
                                                      txtHistorySearch.ForeColor = UiTheme.TextPrimary
                                                  End If
                                              End Sub
        AddHandler txtHistorySearch.LostFocus, Sub()
                                                   If txtHistorySearch.Text.Trim().Length = 0 Then
                                                       txtHistorySearch.ForeColor = UiTheme.TextSecondary
                                                       txtHistorySearch.Text = "Search receipt #, amount, date…"
                                                   End If
                                               End Sub

        cmbDateFilter = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Width = HistoryListWidth,
            .Font = New Font("Segoe UI", 9.5F)
        }
        cmbDateFilter.Items.AddRange(New Object() {"All dates", "Today", "This week", "This month", "Custom range"})
        cmbDateFilter.SelectedIndex = 0

        cmbSort = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Width = HistoryListWidth,
            .Font = New Font("Segoe UI", 9.5F)
        }
        cmbSort.Items.AddRange(New Object() {"Newest first", "Oldest first", "Amount: high to low", "Amount: low to high"})
        cmbSort.SelectedIndex = 0

        dtpFilterFrom = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 148, .Value = DateTime.Today.AddDays(-7)}
        dtpFilterTo = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 148, .Value = DateTime.Today}

        pnlCustomRange = New Panel() With {.Width = HistoryListWidth, .Height = 32, .Visible = False}
        Dim lblFrom As New Label() With {.Text = "From", .AutoSize = True, .Location = New Point(0, 8), .Font = New Font("Segoe UI", 8.5F)}
        dtpFilterFrom.Location = New Point(36, 4)
        Dim lblTo As New Label() With {.Text = "To", .AutoSize = True, .Location = New Point(0, 8), .Font = New Font("Segoe UI", 8.5F)}
        dtpFilterTo.Location = New Point(168, 4)
        pnlCustomRange.Controls.AddRange(New Control() {lblFrom, dtpFilterFrom, lblTo, dtpFilterTo})

        btnLoadList = New Button() With {.Text = "↻ Refresh", .Size = New Size(100, 32), .Cursor = Cursors.Hand}
        btnExportBatch = New Button() With {.Text = "📦 Export batch", .Size = New Size(140, 32), .Cursor = Cursors.Hand}

        btnPrint = CreateToolbarButton("🖨 Print", True)
        btnPrintPreview = CreateToolbarButton("👁 Print preview", False)
        btnReprint = CreateToolbarButton("🔁 Reprint", False)
        btnSavePdf = CreateToolbarButton("📄 PDF", False)
        btnSave = CreateToolbarButton("💾 Text", False)
        btnCopy = CreateToolbarButton("📋 Copy", False)
        btnEmail = CreateToolbarButton("✉ Email", False)
        btnDetails = CreateToolbarButton("ℹ Details", False)
        btnDuplicate = CreateToolbarButton("⧉ Duplicate", False)

        Dim btnBack As New Button() With {
            .Text = "← Back to Menu",
            .Size = New Size(150, 36),
            .Cursor = Cursors.Hand,
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.White,
            .ForeColor = UiTheme.SecondaryAccent,
            .Font = New Font("Segoe UI", 10.0F)
        }
        btnBack.FlatAppearance.BorderSize = 1
        btnBack.FlatAppearance.BorderColor = BorderLight
        AddHandler btnBack.Click, Sub(s, ev) Me.Close()

        lblSaleMeta = New Label() With {.Visible = False, .AutoSize = True}

        BuildSaleChipPanel()

        picReceiptLogo = New PictureBox() With {
            .Size = New Size(PreviewReceiptWidth, ReceiptBranding.ReceiptLogoHeight),
            .SizeMode = PictureBoxSizeMode.Zoom,
            .BackColor = Color.White,
            .Margin = New Padding(0, 0, 0, 8)
        }

        rtbReceipt = New RichTextBox() With {
            .Width = PreviewReceiptWidth,
            .Font = New Font("Courier New", 10.0F),
            .ReadOnly = True,
            .BackColor = Color.White,
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

        btnLoadList.FlatStyle = FlatStyle.Flat
        btnExportBatch.FlatStyle = FlatStyle.Flat
        btnExportBatch.FlatAppearance.BorderSize = 1
        btnExportBatch.FlatAppearance.BorderColor = BorderLight
        btnExportBatch.BackColor = Color.White
        btnExportBatch.ForeColor = UiTheme.SecondaryAccent
        btnExportBatch.Font = New Font("Segoe UI", 9.5F)
        btnLoadList.FlatAppearance.BorderSize = 1
        btnLoadList.FlatAppearance.BorderColor = BorderLight
        btnLoadList.BackColor = Color.White
        btnLoadList.ForeColor = UiTheme.SecondaryAccent
        btnLoadList.Font = New Font("Segoe UI", 10.0F)

        statusClearTimer = New Timer() With {.Interval = FormStatusHelper.StatusShowMilliseconds}
        statusStrip = New StatusStrip()
        statusLabel = New ToolStripStatusLabel(FormStatusHelper.ReadyText) With {.Visible = False}
        statusStrip.Items.Add(statusLabel)

        lblStatus = New Label() With {
            .AutoSize = True,
            .Anchor = AnchorStyles.Right Or AnchorStyles.Top,
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic),
            .ForeColor = UiTheme.TextSecondary
        }

        pnlBottomBar = New Panel() With {.Height = 56, .BackColor = Color.White}
        AddHandler pnlBottomBar.Paint, Sub(s, e)
                                           Using pen As New Pen(BorderLight, 1.0F)
                                               e.Graphics.DrawLine(pen, 0, 0, pnlBottomBar.Width, 0)
                                           End Using
                                       End Sub
        pnlBottomBar.Controls.Add(lblStatus)
        AddHandler pnlBottomBar.Resize, Sub()
                                            lblStatus.Location = New Point(
                                                pnlBottomBar.Width - lblStatus.Width - 24,
                                                (pnlBottomBar.Height - lblStatus.Height) \ 2)
                                        End Sub

        pnlLeft = New Panel() With {
            .Width = LeftPanelWidth,
            .MinimumSize = New Size(LeftPanelWidth, 0),
            .MaximumSize = New Size(LeftPanelWidth, 9999),
            .BackColor = Color.White,
            .Padding = New Padding(24, 20, 24, 0)
        }
        AddHandler pnlLeft.Paint, Sub(s, e)
                                      Using pen As New Pen(BorderLight, 1.0F)
                                          e.Graphics.DrawLine(pen, pnlLeft.Width - 1, 0, pnlLeft.Width - 1, pnlLeft.Height)
                                      End Using
                                  End Sub

        Dim leftStack As New TableLayoutPanel() With {.Dock = DockStyle.Top, .AutoSize = True, .ColumnCount = 1, .Width = HistoryListWidth}
        leftStack.Controls.Add(New Label() With {
            .Text = "Receipts",
            .AutoSize = True,
            .Font = New Font("Segoe UI", 14.0F, FontStyle.Bold),
            .ForeColor = UiTheme.TextPrimary,
            .Margin = New Padding(0, 0, 0, 4)
        }, 0, 0)
        leftStack.Controls.Add(New Label() With {
            .Text = "Select a past sale to preview, print, or export.",
            .AutoSize = True,
            .MaximumSize = New Size(HistoryListWidth, 0),
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = UiTheme.TextSecondary,
            .Margin = New Padding(0, 0, 0, 12)
        }, 0, 1)
        leftStack.Controls.Add(New Label() With {
            .Text = "Search & filters",
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
            .ForeColor = UiTheme.SecondaryAccent,
            .Margin = New Padding(0, 0, 0, 4)
        }, 0, 2)
        txtHistorySearch.Margin = New Padding(0, 0, 0, 6)
        leftStack.Controls.Add(txtHistorySearch, 0, 3)
        cmbDateFilter.Margin = New Padding(0, 0, 0, 4)
        leftStack.Controls.Add(cmbDateFilter, 0, 4)
        leftStack.Controls.Add(pnlCustomRange, 0, 5)
        cmbSort.Margin = New Padding(0, 0, 0, 8)
        leftStack.Controls.Add(cmbSort, 0, 6)
        leftStack.Controls.Add(New Label() With {
            .Text = "Receipt history",
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
            .ForeColor = UiTheme.SecondaryAccent,
            .Margin = New Padding(0, 0, 0, 6)
        }, 0, 7)
        leftStack.Controls.Add(lstHistory, 0, 8)
        Dim pnlListActions As New FlowLayoutPanel() With {.AutoSize = True, .WrapContents = False, .Margin = New Padding(0, 6, 0, 0)}
        pnlListActions.Controls.AddRange(New Control() {btnLoadList, btnExportBatch})
        leftStack.Controls.Add(pnlListActions, 0, 9)
        pnlSaleChip.Margin = New Padding(0, 12, 0, 8)
        leftStack.Controls.Add(pnlSaleChip, 0, 10)

        Dim pnlLeftFooter As New FlowLayoutPanel() With {.Dock = DockStyle.Bottom, .AutoSize = True, .Padding = New Padding(0, 0, 0, 20)}
        btnBack.Margin = New Padding(0, 24, 0, 0)
        pnlLeftFooter.Controls.Add(btnBack)

        Dim pnlLeftBody As New Panel() With {.Dock = DockStyle.Fill, .AutoScroll = True}
        pnlLeftBody.Controls.Add(leftStack)
        pnlLeft.Controls.Add(pnlLeftBody)
        pnlLeft.Controls.Add(pnlLeftFooter)

        BuildEmptyPreviewPanel()
        BuildPreviewArea()

        pnlRight = New Panel() With {.BackColor = SurfaceGray, .Padding = New Padding(24)}
        Dim rightStack As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 4}
        rightStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        rightStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        rightStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        rightStack.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        rightStack.Controls.Add(New Label() With {
            .Text = "Receipt preview",
            .AutoSize = True,
            .Font = New Font("Segoe UI", 14.0F, FontStyle.Bold),
            .ForeColor = UiTheme.TextPrimary,
            .Margin = New Padding(0, 0, 0, 4)
        }, 0, 0)
        Dim pnlPreviewHeader As New FlowLayoutPanel() With {
            .AutoSize = True,
            .WrapContents = False,
            .Dock = DockStyle.Top,
            .Margin = New Padding(0, 0, 0, 8)
        }
        Dim lblPreviewHint As New Label() With {
            .Text = "Monospace receipt layout. Use zoom and print preview for page layout.",
            .AutoSize = True,
            .MaximumSize = New Size(520, 0),
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic),
            .ForeColor = UiTheme.TextSecondary,
            .Margin = New Padding(0, 6, 12, 0)
        }
        btnZoomOut = CreateToolbarButton("−", False)
        btnZoomOut.Size = New Size(36, 32)
        btnZoomIn = CreateToolbarButton("+", False)
        btnZoomIn.Size = New Size(36, 32)
        lblZoomPct = New Label() With {
            .Text = "100%",
            .AutoSize = True,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = UiTheme.TextPrimary,
            .Margin = New Padding(8, 8, 8, 0)
        }
        chkSimulatePage = New CheckBox() With {
            .Text = "Show page margins",
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = UiTheme.TextSecondary,
            .Margin = New Padding(16, 8, 0, 0)
        }
        pnlPreviewHeader.Controls.Add(lblPreviewHint)
        pnlPreviewHeader.Controls.Add(btnZoomOut)
        pnlPreviewHeader.Controls.Add(lblZoomPct)
        pnlPreviewHeader.Controls.Add(btnZoomIn)
        pnlPreviewHeader.Controls.Add(chkSimulatePage)
        rightStack.Controls.Add(pnlPreviewHeader, 0, 1)

        pnlActionToolbar = New FlowLayoutPanel() With {
            .AutoSize = True,
            .WrapContents = True,
            .Dock = DockStyle.Top,
            .Margin = New Padding(0, 0, 0, 8),
            .Padding = New Padding(0, 0, 0, 4)
        }
        pnlActionToolbar.Controls.AddRange(New Control() {
            btnPrint, btnPrintPreview, btnReprint, btnSavePdf, btnSave, btnCopy, btnEmail, btnDetails, btnDuplicate
        })
        rightStack.Controls.Add(pnlActionToolbar, 0, 2)

        Dim previewCard As Panel = UiTheme.CreateCardPanel(New Padding(12))
        previewCard.Dock = DockStyle.Fill
        previewCard.Margin = New Padding(0)
        Dim cardHost As Panel = UiTheme.GetCardContentHost(previewCard)
        cardHost.Padding = Padding.Empty
        pnlReceiptScroll.Dock = DockStyle.Fill
        pnlEmptyPreview.Dock = DockStyle.Fill
        cardHost.Controls.Add(pnlReceiptScroll)
        cardHost.Controls.Add(pnlEmptyPreview)
        rightStack.Controls.Add(previewCard, 0, 3)

        pnlRight.Controls.Add(rightStack)

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

        Me.Controls.Clear()
        Me.Controls.Add(pnlRight)
        Me.Controls.Add(pnlLeft)
        Me.Controls.Add(pnlBottomBar)

        pnlBottomBar.Dock = DockStyle.Bottom
        pnlLeft.Dock = DockStyle.Left
        pnlRight.Dock = DockStyle.Fill
        pnlBottomBar.BringToFront()

        AddHandler pnlReceiptScroll.Resize, AddressOf LayoutReceiptPaper

        Me.ResumeLayout(True)
        UpdatePreviewVisibility(True)
        ClearSaleMetadata()
    End Sub

    Private Sub BuildSaleChipPanel()
        pnlSaleChip = New Panel() With {
            .Height = 96,
            .Width = HistoryListWidth,
            .BackColor = BrandBlueLight,
            .Visible = False
        }
        AddHandler pnlSaleChip.Paint, Sub(s, e)
                                          Using pen As New Pen(BorderLight, 1.0F)
                                              e.Graphics.DrawRectangle(pen, 0, 0, pnlSaleChip.Width - 1, pnlSaleChip.Height - 1)
                                          End Using
                                      End Sub

        lblChipSaleId = New Label() With {
            .Location = New Point(12, 10),
            .Size = New Size(248, 22),
            .Font = New Font("Segoe UI", 11.0F, FontStyle.Bold),
            .ForeColor = UiTheme.TextPrimary
        }
        lblChipDate = New Label() With {
            .Location = New Point(12, 32),
            .Size = New Size(248, 18),
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = UiTheme.TextSecondary
        }
        lblChipTotal = New Label() With {
            .Location = New Point(12, 52),
            .Size = New Size(248, 20),
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = UiTheme.SecondaryAccent
        }
        lblChipCashier = New Label() With {
            .Location = New Point(12, 72),
            .Size = New Size(248, 18),
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = UiTheme.TextSecondary
        }
        pnlSaleChip.Controls.AddRange(New Control() {lblChipSaleId, lblChipDate, lblChipTotal, lblChipCashier})
    End Sub

    Private Sub BuildEmptyPreviewPanel()
        pnlEmptyPreview = New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = SurfaceGray,
            .Visible = True
        }

        Dim lblEmptyTitle As New Label() With {
            .Text = "No receipt loaded",
            .Width = 340,
            .Height = 30,
            .AutoSize = False,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI", 14.0F, FontStyle.Regular),
            .ForeColor = UiTheme.TextPrimary
        }
        Dim lblEmptySub As New Label() With {
            .Text = "Choose a sale from the history list on the left to preview its receipt.",
            .Width = 420,
            .Height = 40,
            .AutoSize = False,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Italic),
            .ForeColor = UiTheme.TextSecondary
        }

        pnlEmptyPreview.Controls.AddRange(New Control() {lblEmptyTitle, lblEmptySub})
        AddHandler pnlEmptyPreview.Resize, Sub()
                                               Dim cx As Integer = pnlEmptyPreview.Width \ 2
                                               Dim cy As Integer = pnlEmptyPreview.Height \ 2
                                               lblEmptyTitle.Location = New Point(cx - 170, cy - 36)
                                               lblEmptySub.Location = New Point(cx - 210, cy)
                                           End Sub
    End Sub

    Private Sub BuildPreviewArea()
        pnlReceiptScroll = New Panel() With {
            .AutoScroll = True,
            .BackColor = SurfaceGray,
            .Visible = False
        }

        pnlPageCanvas = New Panel() With {
            .AutoSize = True,
            .BackColor = SurfaceGray,
            .Padding = New Padding(24, 24, 24, 24)
        }
        AddHandler pnlPageCanvas.Paint, AddressOf pnlPageCanvas_Paint

        pnlReceiptPaper = New Panel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .BackColor = Color.White,
            .Padding = New Padding(20, 28, 20, 36),
            .BorderStyle = BorderStyle.FixedSingle,
            .MinimumSize = New Size(PreviewReceiptWidth + 40, ReceiptBranding.PreviewMinPaperHeight)
        }
        Dim receiptStack As New TableLayoutPanel() With {
            .AutoSize = True,
            .ColumnCount = 1,
            .Width = PreviewReceiptWidth,
            .BackColor = Color.White
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
        If lblStatus Is Nothing Then
            Return
        End If

        statusClearTimer.Stop()
        If String.IsNullOrWhiteSpace(message) Then
            lblStatus.Text = String.Empty
            lblStatus.ForeColor = UiTheme.TextSecondary
            Return
        End If

        lblStatus.Text = message
        lblStatus.ForeColor = If(isError, UiTheme.Danger, UiTheme.Success)
        If lblStatus.Parent IsNot Nothing Then
            lblStatus.Location = New Point(
                lblStatus.Parent.Width - lblStatus.Width - 24,
                (lblStatus.Parent.Height - lblStatus.Height) \ 2)
        End If
        statusClearTimer.Start()
    End Sub

    Private Sub statusClearTimer_Tick(sender As Object, e As EventArgs) Handles statusClearTimer.Tick
        statusClearTimer.Stop()
        If lblStatus IsNot Nothing Then
            lblStatus.Text = String.Empty
            lblStatus.ForeColor = UiTheme.TextSecondary
        End If
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
        ApplySaleMetadata(saleLabel, DateTime.Now, detail.GrandTotal, sym, cashier)
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

    Private Sub LoadHistoryCombo()
        allHistoryItems.Clear()
        allHistoryItems.Add(New SaleListItem With {.SaleId = -1, .SaleDate = DateTime.MinValue, .TotalAmount = 0D})

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Dim query As String =
                    "SELECT TOP 500 sale_id, sale_date, total_amount, receipt_text FROM sales " &
                    "WHERE receipt_text IS NOT NULL AND receipt_text <> '' " &
                    "ORDER BY sale_date DESC, sale_id DESC;"

                Using command As New SqlCommand(query, connection)
                    Using reader As SqlDataReader = command.ExecuteReader()
                        While reader.Read()
                            Dim receiptSnippet As String = reader("receipt_text").ToString()
                            allHistoryItems.Add(New SaleListItem With {
                                .SaleId = Convert.ToInt32(reader("sale_id")),
                                .SaleDate = Convert.ToDateTime(reader("sale_date")),
                                .TotalAmount = Convert.ToDecimal(reader("total_amount")),
                                .CashierHint = TryParseCashierFromReceipt(receiptSnippet)
                            })
                        End While
                    End Using
                End Using
            End Using
        Catch
        End Try

        ApplyHistoryFilters()
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
    End Sub

    Private Function GetHistorySearchTerm() As String
        If txtHistorySearch Is Nothing Then
            Return String.Empty
        End If

        Dim text As String = txtHistorySearch.Text.Trim()
        If txtHistorySearch.ForeColor = UiTheme.TextSecondary OrElse text = "Search receipt #, amount, date…" Then
            Return String.Empty
        End If

        Return text
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

    Private Sub btnLoadList_Click(sender As Object, e As EventArgs) Handles btnLoadList.Click
        suppressHistoryEvent = True
        LoadHistoryCombo()
        If lstHistory.Items.Count > 0 Then
            lstHistory.SelectedIndex = 0
            cmbHistory.SelectedIndex = 0
        End If

        suppressHistoryEvent = False
        ProcessHistorySelection()
        ShowStatus("Sales list refreshed.", False)
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
                    "SELECT TOP 1 sale_id, sale_date, receipt_text, total_amount " &
                    "FROM sales " &
                    "WHERE receipt_text IS NOT NULL AND receipt_text <> '' " &
                    "ORDER BY sale_date DESC, sale_id DESC;"

                Using command As New SqlCommand(query, connection)
                    Using reader As SqlDataReader = command.ExecuteReader()
                        If reader.Read() Then
                            Dim sid As Integer = Convert.ToInt32(reader("sale_id"))
                            Dim sdt As DateTime = Convert.ToDateTime(reader("sale_date"))
                            Dim total As Decimal = Convert.ToDecimal(reader("total_amount"))
                            receiptText = reader("receipt_text").ToString()
                            ApplyReceiptContent(receiptText, False)
                            UpdateSaleMetadataFromText(sid, sdt, receiptText, total)
                            LoadSaleLinesIntoGrid(connection, sid)
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
                    "SELECT sale_id, sale_date, receipt_text, total_amount FROM sales WHERE sale_id = @id;"

                Using command As New SqlCommand(query, connection)
                    command.Parameters.AddWithValue("@id", saleId)
                    Using reader As SqlDataReader = command.ExecuteReader()
                        If reader.Read() Then
                            Dim sdt As DateTime = Convert.ToDateTime(reader("sale_date"))
                            Dim total As Decimal = Convert.ToDecimal(reader("total_amount"))
                            receiptText = reader("receipt_text").ToString()
                            ApplyReceiptContent(receiptText, False)
                            UpdateSaleMetadataFromText(saleId, sdt, receiptText, total)
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
        Dim body As String = Uri.EscapeDataString(ReceiptBranding.GetReceiptText(GetFullReceiptSource()))
        Try
            Process.Start(New ProcessStartInfo("mailto:?subject=" & subject & "&body=" & body) With {.UseShellExecute = True})
            ShowStatus("Email client opened.", False)
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
            .MinimumSize = New Size(88, 34),
            .Padding = New Padding(8, 4, 8, 4),
            .Margin = New Padding(0, 0, 8, 8),
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
