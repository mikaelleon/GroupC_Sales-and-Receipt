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
        UiTheme.ApplyStandardWindowChrome(Me)

        Try
            DatabaseInitializer.EnsureDatabase()
        Catch
        End Try

        Me.Text = "Group C - Receipt Preview"
        Me.MinimumSize = New Size(620, 620)
        Me.Size = New Size(760, 760)
        Me.StartPosition = FormStartPosition.CenterScreen

        CreateControls()
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

    Private Sub CreateControls()
        Dim root As New TableLayoutPanel()
        root.Dock = DockStyle.Fill
        root.Padding = New Padding(12)
        root.BackColor = UiTheme.FormBackground
        root.ColumnCount = 1
        root.RowCount = 5
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim title As New Label()
        title.Text = "RECEIPT PREVIEW"
        title.Dock = DockStyle.Fill
        title.Font = New Font("Segoe UI", 15.0F, FontStyle.Bold)
        title.ForeColor = UiTheme.TextPrimary
        title.TextAlign = ContentAlignment.MiddleCenter
        title.AutoSize = True
        title.Margin = New Padding(0, 0, 0, 8)

        lblSaleMeta = New Label()
        lblSaleMeta.Dock = DockStyle.Fill
        lblSaleMeta.AutoSize = True
        lblSaleMeta.Font = New Font("Segoe UI", 9.0F, FontStyle.Italic)
        lblSaleMeta.ForeColor = UiTheme.TextSecondary
        lblSaleMeta.Text = "Select a saved sale or load latest."
        lblSaleMeta.Margin = New Padding(0, 0, 0, 8)

        Dim historyRow As New TableLayoutPanel()
        historyRow.AutoSize = True
        historyRow.ColumnCount = 3
        historyRow.RowCount = 1
        historyRow.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        historyRow.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        historyRow.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))

        Dim lblHist As New Label()
        lblHist.Text = "Saved sales:"
        lblHist.AutoSize = True
        lblHist.Margin = New Padding(0, 8, 8, 8)
        lblHist.ForeColor = UiTheme.TextSecondary

        cmbHistory = New ComboBox()
        cmbHistory.DropDownStyle = ComboBoxStyle.DropDownList
        cmbHistory.Margin = New Padding(0, 4, 8, 4)
        cmbHistory.TabIndex = 0

        btnLoadList = New Button()
        btnLoadList.Text = "&Refresh list"
        btnLoadList.AutoSize = True
        UiTheme.ApplySecondaryAccentButton(btnLoadList)

        historyRow.Controls.Add(lblHist, 0, 0)
        historyRow.Controls.Add(cmbHistory, 1, 0)
        historyRow.Controls.Add(btnLoadList, 2, 0)
        UiTheme.ApplyTableLayoutDropDown(cmbHistory)

        Dim historyCard As Panel = UiTheme.CreateCardPanel(New Padding(8))
        Dim historyCardInner As Panel = UiTheme.GetCardContentHost(historyCard)
        historyCardInner.Controls.Add(historyRow)
        historyRow.Dock = DockStyle.Top
        historyCard.AutoSize = True
        historyCard.AutoSizeMode = AutoSizeMode.GrowAndShrink

        Dim splitHost As New TableLayoutPanel()
        splitHost.Dock = DockStyle.Fill
        splitHost.ColumnCount = 1
        splitHost.RowCount = 2
        splitHost.RowStyles.Add(New RowStyle(SizeType.Percent, 42.0F))
        splitHost.RowStyles.Add(New RowStyle(SizeType.Percent, 58.0F))

        Dim lblGridTitle As New Label()
        lblGridTitle.Text = "Line items"
        lblGridTitle.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        lblGridTitle.Dock = DockStyle.Top
        lblGridTitle.Height = 22
        lblGridTitle.ForeColor = UiTheme.TextPrimary

        dgvLines = New DataGridView()
        dgvLines.Dock = DockStyle.Fill
        dgvLines.ReadOnly = True
        dgvLines.AllowUserToAddRows = False
        dgvLines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dgvLines.TabIndex = 1
        UiTheme.ApplyDataGridViewChrome(dgvLines)
        dgvLines.Columns.Add("ProductName", "Product")
        dgvLines.Columns.Add("Qty", "Qty")
        dgvLines.Columns.Add("UnitPrice", "Unit price")
        dgvLines.Columns.Add("LineTotal", "Line total")
        dgvLines.Columns("Qty").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvLines.Columns("UnitPrice").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvLines.Columns("LineTotal").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight

        Dim gridPanel As New Panel()
        gridPanel.Dock = DockStyle.Fill
        gridPanel.Controls.Add(dgvLines)
        gridPanel.Controls.Add(lblGridTitle)
        lblGridTitle.BringToFront()

        rtbReceipt = New RichTextBox()
        rtbReceipt.Dock = DockStyle.Fill
        rtbReceipt.ReadOnly = True
        rtbReceipt.Font = New Font("Courier New", 10.0F)
        rtbReceipt.BackColor = UiTheme.CardSurface
        rtbReceipt.ForeColor = UiTheme.TextPrimary
        rtbReceipt.BorderStyle = BorderStyle.FixedSingle
        rtbReceipt.TabIndex = 2

        splitHost.Controls.Add(gridPanel, 0, 0)
        splitHost.Controls.Add(rtbReceipt, 0, 1)

        Dim bodyCard As Panel = UiTheme.CreateCardPanel(New Padding(8))
        Dim bodyCardInner As Panel = UiTheme.GetCardContentHost(bodyCard)
        bodyCard.Dock = DockStyle.Fill
        bodyCardInner.Controls.Add(splitHost)
        splitHost.Dock = DockStyle.Fill

        Dim buttonFlow As New FlowLayoutPanel()
        buttonFlow.AutoSize = True
        buttonFlow.Dock = DockStyle.Fill
        buttonFlow.FlowDirection = FlowDirection.LeftToRight
        buttonFlow.WrapContents = False
        buttonFlow.Padding = New Padding(0, 8, 0, 0)

        btnPrint = New Button()
        btnPrint.Text = "&Print"
        btnPrint.AutoSize = True
        btnPrint.MinimumSize = New Size(110, 36)
        UiTheme.ApplyPrimaryButton(btnPrint)

        btnSave = New Button()
        btnSave.Text = "&Save TXT"
        btnSave.AutoSize = True
        btnSave.MinimumSize = New Size(110, 36)
        UiTheme.ApplySecondaryButton(btnSave)

        btnSavePdf = New Button()
        btnSavePdf.Text = "Save &PDF"
        btnSavePdf.AutoSize = True
        btnSavePdf.MinimumSize = New Size(110, 36)
        UiTheme.ApplySecondaryButton(btnSavePdf)

        btnCopy = New Button()
        btnCopy.Text = "&Copy"
        btnCopy.AutoSize = True
        btnCopy.MinimumSize = New Size(100, 36)
        UiTheme.ApplySecondaryButton(btnCopy)

        buttonFlow.Controls.AddRange(New Control() {btnPrint, btnSave, btnSavePdf, btnCopy})

        printDocument = New PrintDocument()

        root.Controls.Add(title, 0, 0)
        root.Controls.Add(lblSaleMeta, 0, 1)
        root.Controls.Add(historyCard, 0, 2)
        root.Controls.Add(bodyCard, 0, 3)
        root.Controls.Add(buttonFlow, 0, 4)

        statusClearTimer = New Timer() With {.Interval = FormStatusHelper.StatusShowMilliseconds}
        statusStrip = New StatusStrip()
        statusLabel = New ToolStripStatusLabel(FormStatusHelper.ReadyText)
        statusLabel.Spring = True
        statusStrip.Items.Add(statusLabel)
        UiTheme.ApplyStatusStripTheme(statusStrip)

        Me.Controls.Clear()
        Me.Controls.Add(statusStrip)
        Me.Controls.Add(root)
        statusStrip.Dock = DockStyle.Bottom
        root.Dock = DockStyle.Fill
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
