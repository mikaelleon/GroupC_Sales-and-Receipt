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

    Private rtbReceipt As RichTextBox
    Private lblSaleMeta As Label
    Private WithEvents cmbHistory As ComboBox
    Private WithEvents btnPrint As Button
    Private WithEvents btnSave As Button
    Private WithEvents btnLoadList As Button
    Private WithEvents btnCopy As Button
    Private WithEvents printDocument As PrintDocument

    Private receiptText As String
    Private saleIdForMeta As Integer = -1
    Private printHelper As ReceiptPrintHelper
    Private suppressHistoryEvent As Boolean

    Public Sub New()
        InitializeComponent()
        receiptText = String.Empty
    End Sub

    Public Sub New(text As String, Optional savedSaleId As Integer = -1)
        InitializeComponent()
        receiptText = If(text, String.Empty)
        saleIdForMeta = savedSaleId
    End Sub

    Private Sub ReceiptForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            DatabaseInitializer.EnsureDatabase()
        Catch
        End Try

        Me.Text = "Group C - Receipt Preview"
        Me.MinimumSize = New Size(520, 560)
        Me.Size = New Size(640, 680)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 10)

        CreateControls()
        suppressHistoryEvent = True
        LoadHistoryCombo()

        If receiptText.Trim().Length > 0 Then
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
        title.ForeColor = UiTheme.Navy
        title.TextAlign = ContentAlignment.MiddleCenter
        title.AutoSize = True
        title.Margin = New Padding(0, 0, 0, 8)

        lblSaleMeta = New Label()
        lblSaleMeta.Dock = DockStyle.Fill
        lblSaleMeta.AutoSize = True
        lblSaleMeta.Font = New Font("Segoe UI", 9.0F, FontStyle.Italic)
        lblSaleMeta.ForeColor = Color.DimGray
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

        cmbHistory = New ComboBox()
        cmbHistory.DropDownStyle = ComboBoxStyle.DropDownList
        cmbHistory.Dock = DockStyle.Fill
        cmbHistory.Margin = New Padding(0, 4, 8, 4)
        cmbHistory.TabIndex = 0

        btnLoadList = New Button()
        btnLoadList.Text = "&Refresh list"
        btnLoadList.AutoSize = True
        UiTheme.ApplyPrimaryButton(btnLoadList)

        historyRow.Controls.Add(lblHist, 0, 0)
        historyRow.Controls.Add(cmbHistory, 1, 0)
        historyRow.Controls.Add(btnLoadList, 2, 0)

        rtbReceipt = New RichTextBox()
        rtbReceipt.Dock = DockStyle.Fill
        rtbReceipt.ReadOnly = True
        rtbReceipt.Font = New Font("Courier New", 10.0F)
        rtbReceipt.BackColor = Color.White
        rtbReceipt.BorderStyle = BorderStyle.FixedSingle
        rtbReceipt.TabIndex = 1

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
        UiTheme.ApplyPrimaryButton(btnSave)

        btnCopy = New Button()
        btnCopy.Text = "&Copy"
        btnCopy.AutoSize = True
        btnCopy.MinimumSize = New Size(100, 36)
        UiTheme.ApplyPrimaryButton(btnCopy)

        buttonFlow.Controls.AddRange(New Control() {btnPrint, btnSave, btnCopy})

        printDocument = New PrintDocument()

        root.Controls.Add(title, 0, 0)
        root.Controls.Add(lblSaleMeta, 0, 1)
        root.Controls.Add(historyRow, 0, 2)
        root.Controls.Add(rtbReceipt, 0, 3)
        root.Controls.Add(buttonFlow, 0, 4)

        Me.Controls.Clear()
        Me.Controls.Add(root)
    End Sub

    Private Sub ApplyReceiptContent(text As String, isPlaceholder As Boolean)
        rtbReceipt.Text = text
        If isPlaceholder Then
            rtbReceipt.ForeColor = Color.Gray
        Else
            rtbReceipt.ForeColor = Color.Black
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
                        Else
                            lblSaleMeta.Text = "No saved receipt in database."
                            ApplyReceiptContent("No saved receipt found. Finalize a sale from the Sales / Cart screen first.", True)
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            lblSaleMeta.Text = "Database error."
            ApplyReceiptContent("Could not load receipt. Check App.config and LocalDB." & Environment.NewLine & ex.Message, True)
            MessageBox.Show("Error loading receipt: " & ex.Message, "Database", MessageBoxButtons.OK, MessageBoxIcon.Error)
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
                            ApplyReceiptContent("Receipt not found for this sale id.", True)
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            lblSaleMeta.Text = "Database error."
            ApplyReceiptContent(ex.Message, True)
            MessageBox.Show("Error loading receipt: " & ex.Message, "Database", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnPrint_Click(sender As Object, e As EventArgs) Handles btnPrint.Click
        If rtbReceipt.Text.Trim().Length = 0 OrElse rtbReceipt.ForeColor = Color.Gray Then
            MessageBox.Show("Nothing to print.", "Receipt", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using printDialog As New PrintDialog()
            printDialog.Document = printDocument
            If printDialog.ShowDialog() = DialogResult.OK Then
                printDocument.Print()
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
                MessageBox.Show("Receipt saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End Using
    End Sub

    Private Sub btnCopy_Click(sender As Object, e As EventArgs) Handles btnCopy.Click
        If rtbReceipt.Text.Trim().Length = 0 Then
            Return
        End If

        Clipboard.SetText(rtbReceipt.Text)
        lblSaleMeta.Text = "Copied to clipboard. " & lblSaleMeta.Text
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
