Imports System.Collections.Generic
Imports System.Drawing
Imports System.Text
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient

Public Class SalesForm

    Private WithEvents cmbProductName As ComboBox
    Private txtPrice As TextBox
    Private numQuantity As NumericUpDown
    Private WithEvents dgvProducts As DataGridView
    Private lblTotal As Label
    Private lblEmptyHint As Label
    Private WithEvents btnOpenProducts As Button

    Private WithEvents btnAdd As Button
    Private WithEvents btnRemove As Button
    Private WithEvents btnClear As Button
    Private WithEvents btnFinalize As Button

    Private ReadOnly productPrices As New Dictionary(Of String, Decimal)()

    Private Sub SalesForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        SetupForm()
        CreateControls()
        Try
            DatabaseInitializer.EnsureDatabase()
        Catch
        End Try
        LoadProducts()
        UpdateTotal()
    End Sub

    Private Sub SetupForm()
        Me.Text = "Group C - Sales / Cart"
        Me.MinimumSize = New Size(640, 480)
        Me.Size = New Size(780, 560)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Font = New Font("Segoe UI", 10)
        Me.BackColor = Color.White
    End Sub

    Private Sub CreateControls()
        Dim root As New TableLayoutPanel()
        root.Dock = DockStyle.Fill
        root.Padding = New Padding(12)
        root.ColumnCount = 1
        root.RowCount = 3
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim group As New GroupBox()
        group.Text = "Sales / Cart"
        group.Dock = DockStyle.Fill
        group.Padding = New Padding(12)
        group.BackColor = Color.White

        Dim inner As New TableLayoutPanel()
        inner.Dock = DockStyle.Fill
        inner.ColumnCount = 6
        inner.RowCount = 4
        For c As Integer = 0 To 5
            If c = 1 OrElse c = 3 Then
                inner.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20.0F))
            Else
                inner.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            End If
        Next
        inner.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        inner.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        inner.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        inner.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim lblProduct As New Label()
        lblProduct.Text = "Product"
        lblProduct.AutoSize = True
        lblProduct.Margin = New Padding(0, 8, 8, 8)

        cmbProductName = New ComboBox()
        cmbProductName.DropDownStyle = ComboBoxStyle.DropDownList
        cmbProductName.Dock = DockStyle.Fill
        cmbProductName.Margin = New Padding(0, 4, 12, 4)
        cmbProductName.TabIndex = 0

        Dim lblPrice As New Label()
        lblPrice.Text = "Price (₱)"
        lblPrice.AutoSize = True
        lblPrice.Margin = New Padding(0, 8, 8, 8)

        txtPrice = New TextBox()
        txtPrice.ReadOnly = True
        txtPrice.BackColor = Color.White
        txtPrice.Dock = DockStyle.Fill
        txtPrice.Margin = New Padding(0, 4, 12, 4)
        txtPrice.TabIndex = 1
        txtPrice.TextAlign = HorizontalAlignment.Right

        Dim lblQty As New Label()
        lblQty.Text = "Quantity"
        lblQty.AutoSize = True
        lblQty.Margin = New Padding(0, 8, 8, 8)

        numQuantity = New NumericUpDown()
        numQuantity.Minimum = 1D
        numQuantity.Maximum = 99999D
        numQuantity.Dock = DockStyle.Fill
        numQuantity.Margin = New Padding(0, 4, 12, 4)
        numQuantity.TabIndex = 2
        numQuantity.TextAlign = HorizontalAlignment.Right

        btnAdd = New Button()
        btnAdd.Text = "&Add to cart"
        btnAdd.AutoSize = True
        btnAdd.MinimumSize = New Size(110, 32)
        btnAdd.TabIndex = 3
        UiTheme.ApplyPrimaryButton(btnAdd)

        btnRemove = New Button()
        btnRemove.Text = "&Remove"
        btnRemove.AutoSize = True
        btnRemove.MinimumSize = New Size(100, 32)
        btnRemove.TabIndex = 4
        UiTheme.ApplyPrimaryButton(btnRemove)

        btnClear = New Button()
        btnClear.Text = "C&lear all"
        btnClear.AutoSize = True
        btnClear.MinimumSize = New Size(100, 32)
        btnClear.TabIndex = 5
        UiTheme.ApplyPrimaryButton(btnClear)

        lblEmptyHint = New Label()
        lblEmptyHint.Text = "No products in catalog. Open Manage Products to add items."
        lblEmptyHint.AutoSize = True
        lblEmptyHint.Dock = DockStyle.Top
        lblEmptyHint.ForeColor = Color.DimGray
        lblEmptyHint.Visible = False
        lblEmptyHint.Margin = New Padding(0, 4, 0, 8)

        btnOpenProducts = New Button()
        btnOpenProducts.Text = "Open &Products…"
        btnOpenProducts.AutoSize = True
        btnOpenProducts.Visible = False
        UiTheme.ApplySecondaryButton(btnOpenProducts)

        Dim gridPanel As New Panel()
        gridPanel.Dock = DockStyle.Fill
        gridPanel.Margin = New Padding(0, 8, 0, 8)

        dgvProducts = New DataGridView()
        dgvProducts.Dock = DockStyle.Fill
        dgvProducts.ReadOnly = True
        dgvProducts.AllowUserToAddRows = False
        dgvProducts.AllowUserToDeleteRows = False
        dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvProducts.MultiSelect = False
        dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dgvProducts.BackgroundColor = Color.White
        dgvProducts.BorderStyle = BorderStyle.FixedSingle
        dgvProducts.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue
        dgvProducts.TabIndex = 6

        dgvProducts.Columns.Add("Index", "#")
        dgvProducts.Columns.Add("ProductName", "Product")
        dgvProducts.Columns.Add("Price", "Price (₱)")
        dgvProducts.Columns.Add("Quantity", "Qty")
        dgvProducts.Columns.Add("Subtotal", "Subtotal (₱)")
        dgvProducts.Columns("Index").Width = 40
        dgvProducts.Columns("Price").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvProducts.Columns("Quantity").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgvProducts.Columns("Subtotal").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight

        gridPanel.Controls.Add(lblEmptyHint)
        gridPanel.Controls.Add(dgvProducts)

        Dim bottom As New TableLayoutPanel()
        bottom.Dock = DockStyle.Fill
        bottom.ColumnCount = 3
        bottom.RowCount = 1
        bottom.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        bottom.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        bottom.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))

        Dim totalLbl As New Label()
        totalLbl.Text = "TOTAL:"
        totalLbl.Font = New Font("Segoe UI", 14.0F, FontStyle.Bold)
        totalLbl.AutoSize = True
        totalLbl.Margin = New Padding(0, 10, 12, 10)

        lblTotal = New Label()
        lblTotal.Text = "₱0.00"
        lblTotal.Font = New Font("Segoe UI", 14.0F, FontStyle.Bold)
        lblTotal.ForeColor = Color.DarkGreen
        lblTotal.TextAlign = ContentAlignment.MiddleRight
        lblTotal.Dock = DockStyle.Fill
        lblTotal.Margin = New Padding(0, 10, 12, 10)

        btnFinalize = New Button()
        btnFinalize.Text = "Finalize and save sale"
        btnFinalize.AutoSize = True
        btnFinalize.MinimumSize = New Size(180, 40)
        btnFinalize.TabIndex = 7
        UiTheme.ApplyPrimaryButton(btnFinalize)

        bottom.Controls.Add(totalLbl, 0, 0)
        bottom.Controls.Add(lblTotal, 1, 0)
        bottom.Controls.Add(btnFinalize, 2, 0)

        inner.Controls.Add(lblProduct, 0, 0)
        inner.Controls.Add(cmbProductName, 1, 0)
        inner.SetColumnSpan(cmbProductName, 2)
        inner.Controls.Add(lblPrice, 3, 0)
        inner.Controls.Add(txtPrice, 4, 0)
        inner.Controls.Add(btnAdd, 5, 0)

        inner.Controls.Add(lblQty, 0, 1)
        inner.Controls.Add(numQuantity, 1, 1)
        inner.Controls.Add(btnRemove, 2, 1)
        inner.Controls.Add(btnClear, 3, 1)
        inner.SetColumnSpan(btnClear, 2)
        inner.Controls.Add(btnOpenProducts, 5, 1)

        inner.Controls.Add(gridPanel, 0, 2)
        inner.SetRowSpan(gridPanel, 1)
        inner.SetColumnSpan(gridPanel, 6)

        inner.Controls.Add(bottom, 0, 3)
        inner.SetColumnSpan(bottom, 6)

        group.Controls.Add(inner)
        root.Controls.Add(group, 0, 1)

        Dim topBar As New FlowLayoutPanel()
        topBar.AutoSize = True
        topBar.Dock = DockStyle.Fill
        topBar.FlowDirection = FlowDirection.LeftToRight
        topBar.WrapContents = False
        Dim hdr As New Label()
        hdr.Text = "Build the sale, then finalize to save to the database and open the receipt."
        hdr.AutoSize = True
        hdr.Font = New Font("Segoe UI", 9.5F, FontStyle.Italic)
        hdr.ForeColor = Color.FromArgb(80, 80, 80)
        topBar.Controls.Add(hdr)

        root.Controls.Add(topBar, 0, 0)

        Me.Controls.Clear()
        Me.Controls.Add(root)
    End Sub

    Private Sub btnOpenProducts_Click(sender As Object, e As EventArgs) Handles btnOpenProducts.Click
        Using form As New ProductsForm()
            form.ShowDialog()
        End Using
        LoadProducts()
    End Sub

    Private Sub cmbProductName_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbProductName.SelectedIndexChanged
        Dim selectedProduct As String = cmbProductName.Text

        If productPrices.ContainsKey(selectedProduct) Then
            txtPrice.Text = productPrices(selectedProduct).ToString("N2")
        Else
            txtPrice.Clear()
        End If
    End Sub

    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        Dim productName As String = cmbProductName.Text.Trim()
        Dim price As Decimal
        Dim quantity As Integer = CInt(numQuantity.Value)

        If productName = String.Empty Then
            MessageBox.Show("Please select a product.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            cmbProductName.Focus()
            Return
        End If

        If Not Decimal.TryParse(txtPrice.Text.Trim(), price) OrElse price <= 0D Then
            MessageBox.Show("Selected product has no valid price.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            cmbProductName.Focus()
            Return
        End If

        If quantity < 1 Then
            MessageBox.Show("Quantity must be at least 1.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim subtotal As Decimal = price * quantity
        Dim rowNumber As Integer = dgvProducts.Rows.Count + 1

        dgvProducts.Rows.Add(rowNumber, productName, price.ToString("N2"), quantity, subtotal.ToString("N2"))
        cmbProductName.SelectedIndex = -1
        txtPrice.Clear()
        numQuantity.Value = 1D
        cmbProductName.Focus()
        UpdateTotal()
    End Sub

    Private Sub btnRemove_Click(sender As Object, e As EventArgs) Handles btnRemove.Click
        If dgvProducts.SelectedRows.Count = 0 Then
            MessageBox.Show("Select a row to remove.", "Cart", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        dgvProducts.Rows.Remove(dgvProducts.SelectedRows(0))
        ReindexRows()
        UpdateTotal()
    End Sub

    Private Sub btnClear_Click(sender As Object, e As EventArgs) Handles btnClear.Click
        dgvProducts.Rows.Clear()
        lblTotal.Text = "₱0.00"
        cmbProductName.SelectedIndex = -1
        txtPrice.Clear()
        numQuantity.Value = 1D
        cmbProductName.Focus()
    End Sub

    Private Sub btnFinalize_Click(sender As Object, e As EventArgs) Handles btnFinalize.Click
        If dgvProducts.Rows.Count = 0 Then
            MessageBox.Show("Add at least one line item before finalizing.", "Cart", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim receiptText As String = BuildReceiptText()
        Dim newSaleId As Integer = -1
        If Not SaveSale(receiptText, newSaleId) Then
            Return
        End If

        Using receiptForm As New ReceiptForm(receiptText, newSaleId)
            receiptForm.ShowDialog()
        End Using
    End Sub

    Private Function BuildReceiptText() As String
        Dim receipt As New StringBuilder()

        receipt.AppendLine("========================================")
        receipt.AppendLine("         GROUP C SALES RECEIPT")
        receipt.AppendLine("========================================")
        receipt.AppendLine("Date: " & DateTime.Now.ToString("MMMM dd, yyyy hh:mm tt"))
        receipt.AppendLine("----------------------------------------")
        receipt.AppendLine("Item              Qty   Price    Subtotal")
        receipt.AppendLine("----------------------------------------")

        For Each row As DataGridViewRow In dgvProducts.Rows
            Dim itemName As String = row.Cells("ProductName").Value.ToString()
            Dim quantity As String = row.Cells("Quantity").Value.ToString()
            Dim price As String = "₱" & row.Cells("Price").Value.ToString()
            Dim subtotal As String = "₱" & row.Cells("Subtotal").Value.ToString()

            If itemName.Length > 15 Then
                itemName = itemName.Substring(0, 15)
            End If

            receipt.AppendLine(itemName.PadRight(18) &
                               quantity.PadLeft(3) & " " &
                               price.PadLeft(8) & " " &
                               subtotal.PadLeft(9))
        Next

        receipt.AppendLine("----------------------------------------")
        receipt.AppendLine("TOTAL:".PadRight(30) & ("₱" & GetTotal().ToString("N2")).PadLeft(10))
        receipt.AppendLine("========================================")
        receipt.AppendLine("       Thank you for your purchase!")
        receipt.AppendLine("========================================")

        Return receipt.ToString()
    End Function

    Private Function SaveSale(receiptText As String, ByRef newSaleId As Integer) As Boolean
        newSaleId = -1
        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Using transaction As SqlTransaction = connection.BeginTransaction()
                    Dim saleQuery As String =
                        "INSERT INTO sales (total_amount, receipt_text) " &
                        "VALUES (@total_amount, @receipt_text); " &
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);"

                    Dim saleId As Integer

                    Using saleCommand As New SqlCommand(saleQuery, connection, transaction)
                        saleCommand.Parameters.AddWithValue("@total_amount", GetTotal())
                        saleCommand.Parameters.AddWithValue("@receipt_text", receiptText)
                        saleId = Convert.ToInt32(saleCommand.ExecuteScalar())
                        newSaleId = saleId
                    End Using

                    For Each row As DataGridViewRow In dgvProducts.Rows
                        Dim itemQuery As String =
                            "INSERT INTO sale_items " &
                            "(sale_id, product_name, price, quantity, subtotal) " &
                            "VALUES (@sale_id, @product_name, @price, @quantity, @subtotal);"

                        Using itemCommand As New SqlCommand(itemQuery, connection, transaction)
                            itemCommand.Parameters.AddWithValue("@sale_id", saleId)
                            itemCommand.Parameters.AddWithValue("@product_name", row.Cells("ProductName").Value.ToString())
                            itemCommand.Parameters.AddWithValue("@price", Convert.ToDecimal(row.Cells("Price").Value))
                            itemCommand.Parameters.AddWithValue("@quantity", Convert.ToInt32(row.Cells("Quantity").Value))
                            itemCommand.Parameters.AddWithValue("@subtotal", Convert.ToDecimal(row.Cells("Subtotal").Value))
                            itemCommand.ExecuteNonQuery()
                        End Using
                    Next

                    transaction.Commit()
                End Using
            End Using

            MessageBox.Show("Sale saved. Receipt window opened.", "Database", MessageBoxButtons.OK, MessageBoxIcon.Information)
            dgvProducts.Rows.Clear()
            UpdateTotal()
            Return True
        Catch ex As Exception
            ShowDatabaseError("Error saving sale", ex)
            Return False
        End Try
    End Function

    Private Sub ShowDatabaseError(title As String, ex As Exception)
        Dim message As String =
            title & ":" & Environment.NewLine &
            ex.Message & Environment.NewLine & Environment.NewLine &
            "Fix:" & Environment.NewLine &
            "1. Confirm SQL Server LocalDB is installed (sqllocaldb info MSSQLLocalDB)." & Environment.NewLine &
            "2. Restart the app so DatabaseInitializer can recreate tables." & Environment.NewLine &
            "3. Check App.config connection name GroupCSqlServer."

        MessageBox.Show(message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub

    Private Sub LoadProducts()
        productPrices.Clear()
        cmbProductName.Items.Clear()
        txtPrice.Clear()

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Dim query As String =
                    "SELECT product_name, price " &
                    "FROM products " &
                    "WHERE is_active = 1 " &
                    "ORDER BY product_name;"

                Using command As New SqlCommand(query, connection)
                    Using reader As SqlDataReader = command.ExecuteReader()
                        While reader.Read()
                            Dim productName As String = reader("product_name").ToString()
                            Dim price As Decimal = Convert.ToDecimal(reader("price"))

                            productPrices(productName) = price
                            cmbProductName.Items.Add(productName)
                        End While
                    End Using
                End Using
            End Using

            Dim hasProducts As Boolean = cmbProductName.Items.Count > 0
            btnAdd.Enabled = hasProducts
            cmbProductName.Enabled = hasProducts
            numQuantity.Enabled = hasProducts
            lblEmptyHint.Visible = Not hasProducts
            btnOpenProducts.Visible = Not hasProducts
        Catch ex As Exception
            ShowDatabaseError("Error loading products", ex)
            btnAdd.Enabled = False
            cmbProductName.Enabled = False
            numQuantity.Enabled = False
            lblEmptyHint.Visible = True
            lblEmptyHint.Text = "Could not load products. Check database and App.config."
        End Try
    End Sub

    Private Sub UpdateTotal()
        lblTotal.Text = "₱" & GetTotal().ToString("N2")
    End Sub

    Private Function GetTotal() As Decimal
        Dim total As Decimal = 0D

        For Each row As DataGridViewRow In dgvProducts.Rows
            total += Convert.ToDecimal(row.Cells("Subtotal").Value)
        Next

        Return total
    End Function

    Private Sub ReindexRows()
        For i As Integer = 0 To dgvProducts.Rows.Count - 1
            dgvProducts.Rows(i).Cells("Index").Value = i + 1
        Next
    End Sub

End Class
