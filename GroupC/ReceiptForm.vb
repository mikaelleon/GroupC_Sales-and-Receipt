Imports System.Drawing
Imports System.Drawing.Printing
Imports System.IO
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient

Public Class ReceiptForm

    Private Class SaleListItem
        Public SaleId As Integer
        Public SaleDate As DateTime

        Public Overrides Function ToString() As String
            If SaleId < 0 Then
                Return "Latest receipt"
            End If

            Return String.Format("Sale #{0} — {1:yyyy-MM-dd HH:mm}", SaleId, SaleDate)
        End Function
    End Class

    Private dgvLines As DataGridView
    Private rtbReceipt As RichTextBox
    Private lblSaleMeta As Label
    Private WithEvents cmbHistory As ComboBox
    Private WithEvents btnPrint As Button
    Private WithEvents btnSave As Button
    Private WithEvents btnSavePdf As Button
    Private WithEvents btnLoadList As Button
    Private WithEvents btnCopy As Button
    Private WithEvents printDocument As PrintDocument

    Private receiptText As String
    Private snapshot As ReceiptSnapshot
    Private saleIdForMeta As Integer = -1
    Private printHelper As ReceiptPrintHelper
    Private suppressHistoryEvent As Boolean

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
        Me.Text = "Group C - Receipt Preview"
        Me.MinimumSize = New Size(1024, 720) ' Increased so the side-by-side layout never crushes
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.WindowState = FormWindowState.Maximized ' Forces full screen
        Me.StartPosition = FormStartPosition.CenterScreen

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
            If saleIdForMeta >= 0 Then
                lblSaleMeta.Text = String.Format("Sale #{0} — saved", saleIdForMeta)
            Else
                lblSaleMeta.Text = "Current receipt (from sales screen)."
            End If
            suppressHistoryEvent = False

        ElseIf receiptText.Trim().Length > 0 Then
            ApplyReceiptContent(receiptText, False)
            If saleIdForMeta >= 0 Then
                lblSaleMeta.Text = String.Format("Sale #{0} — {1}", saleIdForMeta, DateTime.Now.ToString("yyyy-MM-dd HH:mm"))
            Else
                lblSaleMeta.Text = "Current receipt (from sales screen)."
            End If
            suppressHistoryEvent = False

        Else
            suppressHistoryEvent = False
            If cmbHistory.Items.Count > 0 Then
                cmbHistory.SelectedIndex = 0
            Else
                LoadLatestReceiptFromDb()
            End If
        End If
    End Sub

    Private Sub SetupForm()
        Me.Text = "Group C - Receipt Viewer"
        Me.MinimumSize = New Size(1024, 720)
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.WindowState = FormWindowState.Maximized ' Start in Full Screen
        Me.StartPosition = FormStartPosition.CenterScreen
    End Sub

    Private Sub CreateControls()
        Me.SuspendLayout()
        Me.Controls.Clear()
        Me.BackColor = UiTheme.FormBackground

        ' -----------------------------------------------------------
        ' 1. INITIALIZE CONTROLS
        ' -----------------------------------------------------------
        cmbHistory = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Font = New Font("Segoe UI", 11), .Dock = DockStyle.Fill}

        btnLoadList = New Button() With {.Text = "Refresh", .Size = New Size(100, 36), .Cursor = Cursors.Hand}
        btnPrint = New Button() With {.Text = "Print Receipt", .Size = New Size(180, 50), .Font = New Font("Segoe UI", 11, FontStyle.Bold), .Cursor = Cursors.Hand}
        btnSave = New Button() With {.Text = "Save as Text", .Size = New Size(180, 40), .Cursor = Cursors.Hand}
        btnSavePdf = New Button() With {.Text = "Save as PDF", .Size = New Size(180, 40), .Cursor = Cursors.Hand}
        btnCopy = New Button() With {.Text = "Copy to Clipboard", .Size = New Size(180, 40), .Cursor = Cursors.Hand}

        ' Dynamic Back Button logic directly injected
        Dim btnBack As New Button() With {.Text = "← Back to Menu", .Size = New Size(140, 36), .Cursor = Cursors.Hand}
        AddHandler btnBack.Click, Sub(s, ev) Me.Close()

        lblSaleMeta = New Label() With {.Text = "Sale Details will appear here...", .AutoSize = True, .ForeColor = UiTheme.TextSecondary, .Font = New Font("Segoe UI", 10)}

        ' The Receipt Paper
        rtbReceipt = New RichTextBox() With {
            .Dock = DockStyle.Fill,
            .Font = New Font("Courier New", 11),
            .ReadOnly = True,
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.None
        }

        dgvLines = New DataGridView() With {
            .Dock = DockStyle.Fill,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .Visible = False ' Hidden by default unless your code uses it
        }

        ' ---> ADD THESE LINES RIGHT HERE <---
        dgvLines.Columns.Add("ProductName", "Product")
        dgvLines.Columns.Add("Qty", "Qty")
        dgvLines.Columns.Add("UnitPrice", "Unit price")
        dgvLines.Columns.Add("LineTotal", "Line total")
        dgvLines.Columns("Qty").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvLines.Columns("UnitPrice").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvLines.Columns("LineTotal").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        ' ------------------------------------

        Try
            UiTheme.ApplyTableLayoutDropDown(cmbHistory)
            UiTheme.ApplySuccessButton(btnPrint)
            UiTheme.ApplySecondaryButton(btnSave)
            UiTheme.ApplySecondaryButton(btnSavePdf)
            UiTheme.ApplySecondaryButton(btnCopy)
            UiTheme.ApplySecondaryButton(btnLoadList)
            UiTheme.ApplySecondaryButton(btnBack)
            UiTheme.ApplyDataGridViewChrome(dgvLines)
        Catch
        End Try

        statusClearTimer = New Timer() With {.Interval = FormStatusHelper.StatusShowMilliseconds}

        statusStrip = New StatusStrip()
        statusLabel = New ToolStripStatusLabel("Ready") With {.Spring = True, .TextAlign = ContentAlignment.MiddleLeft}
        statusStrip.Items.Add(statusLabel)
        Try
            UiTheme.ApplyStatusStripTheme(statusStrip)
        Catch
        End Try

        ' -----------------------------------------------------------
        ' 2. BUILD THE RESPONSIVE LAYOUT (Side-by-Side)
        ' -----------------------------------------------------------
        Dim rootTable As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .Margin = New Padding(0),
            .BackColor = UiTheme.FormBackground
        }
        rootTable.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 350.0F))
        rootTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

        ' --- LEFT SIDEBAR (Actions & History) ---
        Dim leftSidebar As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(25, 30, 25, 30)}
        Dim leftLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 4
        }
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))        ' Title
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))        ' Inputs & Actions
        leftLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F)) ' Dynamic Spacer
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))        ' Back Button

        Dim lblTitleLeft As New Label() With {
            .Text = "Receipt Actions",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = UiTheme.PrimaryAccent,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 20)
        }
        leftLayout.Controls.Add(lblTitleLeft, 0, 0)

        Dim actionLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .ColumnCount = 1,
            .RowCount = 10,
            .Margin = New Padding(0)
        }

        Dim CreateLabel = Function(text As String) New Label() With {.Text = text, .AutoSize = True, .ForeColor = UiTheme.TextSecondary, .Margin = New Padding(0, 15, 0, 5)}

        actionLayout.Controls.Add(CreateLabel("Select Past Sale:"), 0, 0)
        actionLayout.Controls.Add(cmbHistory, 0, 1)

        Dim pnlRefresh As New FlowLayoutPanel() With {.AutoSize = True, .Margin = New Padding(0, 5, 0, 20)}
        pnlRefresh.Controls.Add(btnLoadList)
        actionLayout.Controls.Add(pnlRefresh, 0, 2)

        actionLayout.Controls.Add(lblSaleMeta, 0, 3)

        ' Action Buttons Stacked
        Dim pnlActions As New FlowLayoutPanel() With {
            .AutoSize = True,
            .FlowDirection = FlowDirection.TopDown,
            .Margin = New Padding(0, 30, 0, 0)
        }
        pnlActions.Controls.Add(btnPrint)
        btnSavePdf.Margin = New Padding(0, 15, 0, 0)
        pnlActions.Controls.Add(btnSavePdf)
        btnSave.Margin = New Padding(0, 10, 0, 0)
        pnlActions.Controls.Add(btnSave)
        btnCopy.Margin = New Padding(0, 10, 0, 0)
        pnlActions.Controls.Add(btnCopy)

        actionLayout.Controls.Add(pnlActions, 0, 4)
        leftLayout.Controls.Add(actionLayout, 0, 1)

        Dim pnlUtility As New FlowLayoutPanel() With {
            .Dock = DockStyle.Bottom,
            .AutoSize = True,
            .FlowDirection = FlowDirection.TopDown
        }
        pnlUtility.Controls.Add(btnBack)

        leftLayout.Controls.Add(pnlUtility, 0, 3)
        leftSidebar.Controls.Add(leftLayout)

        ' --- RIGHT CARD (Receipt Preview) ---
        Dim rightCard As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2,
            .Padding = New Padding(30, 30, 30, 30)
        }
        rightCard.RowStyles.Add(New RowStyle(SizeType.AutoSize))       ' Header
        rightCard.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F)) ' Preview Area

        Dim lblTitleRight As New Label() With {
            .Text = "Receipt Preview",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = UiTheme.PrimaryAccent,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 15)
        }
        rightCard.Controls.Add(lblTitleRight, 0, 0)

        ' Mocking a "piece of paper" for the receipt
        Dim paperPanel As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .Padding = New Padding(30),
            .BorderStyle = BorderStyle.FixedSingle ' Gives it a crisp paper edge!
        }

        paperPanel.Controls.Add(rtbReceipt)
        paperPanel.Controls.Add(dgvLines)

        ' THE FIX 1: Guarantee the text box is physically stacked on top of the grid
        rtbReceipt.BringToFront()

        ' THE FIX 2: Add the paper directly to the right card to avoid the custom Card Theme hiding it completely!
        rightCard.Controls.Add(paperPanel, 0, 1)

        ' 3. ASSEMBLE ALL
        rootTable.Controls.Add(leftSidebar, 0, 0)
        rootTable.Controls.Add(rightCard, 1, 0)

        Me.Controls.Add(rootTable)
        Me.Controls.Add(statusStrip)

        Me.ResumeLayout(True)
    End Sub

    Private Sub ShowStatus(message As String, isError As Boolean)
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
        rtbReceipt.Text = text
        If isPlaceholder Then
            rtbReceipt.ForeColor = UiTheme.TextSecondary
        Else
            rtbReceipt.ForeColor = UiTheme.TextPrimary
        End If
    End Sub

    Private Sub LoadHistoryCombo()
        cmbHistory.Items.Clear()
        cmbHistory.Items.Add(New SaleListItem With {.SaleId = -1, .SaleDate = DateTime.MinValue})

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Dim query As String =
                    "SELECT TOP 30 sale_id, sale_date FROM sales " &
                    "WHERE receipt_text IS NOT NULL AND receipt_text <> '' " &
                    "ORDER BY sale_date DESC, sale_id DESC;"

                Using command As New SqlCommand(query, connection)
                    Using reader As SqlDataReader = command.ExecuteReader()
                        While reader.Read()
                            Dim item As New SaleListItem With {
                                .SaleId = Convert.ToInt32(reader("sale_id")),
                                .SaleDate = Convert.ToDateTime(reader("sale_date"))
                            }
                            cmbHistory.Items.Add(item)
                        End While
                    End Using
                End Using
            End Using
        Catch
        End Try
    End Sub

    Private Sub btnLoadList_Click(sender As Object, e As EventArgs) Handles btnLoadList.Click
        suppressHistoryEvent = True
        LoadHistoryCombo()
        If cmbHistory.Items.Count > 0 Then
            cmbHistory.SelectedIndex = 0
        End If

        suppressHistoryEvent = False
        ShowStatus("Sales list refreshed.", False)
    End Sub

    Private Sub cmbHistory_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbHistory.SelectedIndexChanged
        If suppressHistoryEvent Then
            Return
        End If

        If cmbHistory.SelectedItem Is Nothing Then
            Return
        End If

        Dim item As SaleListItem = TryCast(cmbHistory.SelectedItem, SaleListItem)
        If item Is Nothing Then
            Return
        End If

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
                    "SELECT TOP 1 sale_id, sale_date, receipt_text " &
                    "FROM sales " &
                    "WHERE receipt_text IS NOT NULL AND receipt_text <> '' " &
                    "ORDER BY sale_date DESC, sale_id DESC;"

                Using command As New SqlCommand(query, connection)
                    Using reader As SqlDataReader = command.ExecuteReader()
                        If reader.Read() Then
                            Dim sid As Integer = Convert.ToInt32(reader("sale_id"))
                            Dim sdt As DateTime = Convert.ToDateTime(reader("sale_date"))
                            lblSaleMeta.Text = String.Format("Loaded sale #{0} — {1:yyyy-MM-dd HH:mm}", sid, sdt)
                            ApplyReceiptContent(reader("receipt_text").ToString(), False)
                            LoadSaleLinesIntoGrid(connection, sid)
                        Else
                            lblSaleMeta.Text = "No saved receipt in database."
                            dgvLines.Rows.Clear()
                            ApplyReceiptContent("No saved receipt found. Finalize a sale from the Sales / Cart screen first.", True)
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            lblSaleMeta.Text = "Database error."
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
                    "SELECT sale_id, sale_date, receipt_text FROM sales WHERE sale_id = @id;"

                Using command As New SqlCommand(query, connection)
                    command.Parameters.AddWithValue("@id", saleId)
                    Using reader As SqlDataReader = command.ExecuteReader()
                        If reader.Read() Then
                            Dim sdt As DateTime = Convert.ToDateTime(reader("sale_date"))
                            lblSaleMeta.Text = String.Format("Loaded sale #{0} — {1:yyyy-MM-dd HH:mm}", saleId, sdt)
                            ApplyReceiptContent(reader("receipt_text").ToString(), False)
                        Else
                            lblSaleMeta.Text = "Sale not found."
                            dgvLines.Rows.Clear()
                            ApplyReceiptContent("Receipt not found for this sale id.", True)
                            Return
                        End If
                    End Using
                End Using

                LoadSaleLinesIntoGrid(connection, saleId)
            End Using
        Catch ex As Exception
            lblSaleMeta.Text = "Database error."
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
        If rtbReceipt.Text.Trim().Length = 0 OrElse rtbReceipt.ForeColor.ToArgb() = UiTheme.TextSecondary.ToArgb() Then
            MessageBox.Show("Nothing to print.", "Receipt", MessageBoxButtons.OK, MessageBoxIcon.Information)
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
                File.WriteAllText(saveDialog.FileName, rtbReceipt.Text)
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
                PdfReceiptExporter.ExportTextToPdf(saveDialog.FileName, rtbReceipt.Text)
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

        Clipboard.SetText(rtbReceipt.Text)
        ShowStatus("Copied to clipboard.", False)
    End Sub

    Private Sub printDocument_BeginPrint(sender As Object, e As PrintEventArgs) Handles printDocument.BeginPrint
        printHelper = New ReceiptPrintHelper(rtbReceipt.Text)
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

End Class
