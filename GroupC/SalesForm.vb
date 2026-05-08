Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
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

    Private WithEvents numDiscountPercent As NumericUpDown
    Private WithEvents chkApplyTax As CheckBox
    Private WithEvents numTaxPercent As NumericUpDown
    Private WithEvents txtAmountTendered As TextBox

    Private lblSubtotalValue As Label
    Private lblDiscountValue As Label
    Private lblTaxValue As Label
    Private lblChangeValue As Label

    Private ReadOnly productPrices As New Dictionary(Of String, Decimal)()

    Private Sub SalesForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        SetupForm()
        CreateControls()
        Try
            DatabaseInitializer.EnsureDatabase()
        Catch
        End Try
        LoadProducts()
        UpdateSummaryLabels()
    End Sub

    Private Sub SetupForm()
        Me.Text = "Group C - Sales / Cart"
        Me.MinimumSize = New Size(720, 620)
        Me.Size = New Size(820, 640)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Font = New Font("Segoe UI", 10)
        Me.BackColor = Color.White
    End Sub

    Private Sub CreateControls()
        Dim root As New TableLayoutPanel()
        root.Dock = DockStyle.Fill
        root.Padding = New Padding(12)
        root.ColumnCount = 1
        root.RowCount = 4
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim group As New GroupBox()
        group.Text = "Sales / Cart"
        group.Dock = DockStyle.Fill
        group.Padding = New Padding(12)
        group.BackColor = Color.White

        Dim inner As New TableLayoutPanel()
        inner.Dock = DockStyle.Fill
        inner.ColumnCount = 6
        inner.RowCount = 7
        For c As Integer = 0 To 5
            If c = 1 OrElse c = 3 Then
                inner.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 18.0F))
            Else
                inner.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            End If
        Next
        inner.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        inner.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        inner.RowStyles.Add(New RowStyle(SizeType.AutoSize))
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
        lblPrice.Text = "Price"
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

        Dim lblDisc As New Label()
        lblDisc.Text = "Discount %"
        lblDisc.AutoSize = True
        lblDisc.Margin = New Padding(0, 8, 8, 8)

        numDiscountPercent = New NumericUpDown()
        numDiscountPercent.DecimalPlaces = 2
        numDiscountPercent.Minimum = 0D
        numDiscountPercent.Maximum = 100D
        numDiscountPercent.Increment = 0.5D
        numDiscountPercent.Dock = DockStyle.Fill
        numDiscountPercent.Margin = New Padding(0, 4, 12, 4)
        numDiscountPercent.TabIndex = 6

        lblDiscountValue = New Label()
        lblDiscountValue.Text = AppSettings.Current.CurrencySymbol & "0.00"
        lblDiscountValue.AutoSize = True
        lblDiscountValue.Margin = New Padding(0, 8, 8, 8)

        chkApplyTax = New CheckBox()
        chkApplyTax.Text = "VAT/Tax %"
        chkApplyTax.AutoSize = True
        chkApplyTax.Margin = New Padding(0, 8, 8, 8)
        chkApplyTax.TabIndex = 7

        numTaxPercent = New NumericUpDown()
        numTaxPercent.DecimalPlaces = 2
        numTaxPercent.Minimum = 0D
        numTaxPercent.Maximum = 100D
        numTaxPercent.Increment = 0.5D
        numTaxPercent.Enabled = False
        numTaxPercent.Dock = DockStyle.Fill
        numTaxPercent.Margin = New Padding(0, 4, 12, 4)
        numTaxPercent.TabIndex = 8

        lblTaxValue = New Label()
        lblTaxValue.Text = AppSettings.Current.CurrencySymbol & "0.00"
        lblTaxValue.AutoSize = True
        lblTaxValue.Margin = New Padding(0, 8, 8, 8)

        Dim lblSub As New Label()
        lblSub.Text = "Lines subtotal"
        lblSub.AutoSize = True
        lblSub.Margin = New Padding(0, 8, 8, 8)

        lblSubtotalValue = New Label()
        lblSubtotalValue.Text = AppSettings.Current.CurrencySymbol & "0.00"
        lblSubtotalValue.AutoSize = True
        lblSubtotalValue.Margin = New Padding(0, 8, 8, 8)

        Dim lblPay As New Label()
        lblPay.Text = "Cash tendered"
        lblPay.AutoSize = True
        lblPay.Margin = New Padding(0, 8, 8, 8)

        txtAmountTendered = New TextBox()
        txtAmountTendered.Dock = DockStyle.Fill
        txtAmountTendered.Margin = New Padding(0, 4, 12, 4)
        txtAmountTendered.TabIndex = 9
        txtAmountTendered.TextAlign = HorizontalAlignment.Right

        Dim lblCh As New Label()
        lblCh.Text = "Change"
        lblCh.AutoSize = True
        lblCh.Margin = New Padding(0, 8, 8, 8)

        lblChangeValue = New Label()
        lblChangeValue.Text = AppSettings.Current.CurrencySymbol & "0.00"
        lblChangeValue.ForeColor = Color.DarkGreen
        lblChangeValue.AutoSize = True
        lblChangeValue.Margin = New Padding(0, 8, 8, 8)

        Dim gridPanel As New Panel()
        gridPanel.Dock = DockStyle.Fill
        gridPanel.Margin = New Padding(0, 8, 0, 8)

        dgvProducts = New DataGridView()
        dgvProducts.Dock = DockStyle.Fill
        dgvProducts.AllowUserToAddRows = False
        dgvProducts.AllowUserToDeleteRows = False
        dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvProducts.MultiSelect = False
        dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dgvProducts.BackgroundColor = Color.White
        dgvProducts.BorderStyle = BorderStyle.FixedSingle
        dgvProducts.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue
        dgvProducts.TabIndex = 10

        dgvProducts.Columns.Add("Index", "#")
        dgvProducts.Columns.Add("ProductName", "Product")
        dgvProducts.Columns.Add("Price", "Price")
        dgvProducts.Columns.Add("Quantity", "Qty")
        dgvProducts.Columns.Add("Subtotal", "Subtotal")
        dgvProducts.Columns("Index").Width = 40
        dgvProducts.Columns("Index").ReadOnly = True
        dgvProducts.Columns("ProductName").ReadOnly = True
        dgvProducts.Columns("Price").ReadOnly = True
        dgvProducts.Columns("Quantity").ReadOnly = False
        dgvProducts.Columns("Subtotal").ReadOnly = True
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
        totalLbl.Text = "AMOUNT DUE:"
        totalLbl.Font = New Font("Segoe UI", 14.0F, FontStyle.Bold)
        totalLbl.AutoSize = True
        totalLbl.Margin = New Padding(0, 10, 12, 10)

        lblTotal = New Label()
        lblTotal.Text = FormatMoney(0D)
        lblTotal.Font = New Font("Segoe UI", 14.0F, FontStyle.Bold)
        lblTotal.ForeColor = Color.DarkGreen
        lblTotal.TextAlign = ContentAlignment.MiddleRight
        lblTotal.Dock = DockStyle.Fill
        lblTotal.Margin = New Padding(0, 10, 12, 10)

        btnFinalize = New Button()
        btnFinalize.Text = "Finalize and save sale"
        btnFinalize.AutoSize = True
        btnFinalize.MinimumSize = New Size(180, 40)
        btnFinalize.TabIndex = 11
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

        inner.Controls.Add(lblSub, 0, 2)
        inner.Controls.Add(lblSubtotalValue, 1, 2)
        inner.Controls.Add(lblDisc, 2, 2)
        inner.Controls.Add(numDiscountPercent, 3, 2)
        inner.Controls.Add(lblDiscountValue, 4, 2)
        inner.SetColumnSpan(lblDiscountValue, 2)

        inner.Controls.Add(chkApplyTax, 0, 3)
        inner.Controls.Add(numTaxPercent, 1, 3)
        inner.Controls.Add(lblTaxValue, 2, 3)
        inner.Controls.Add(lblPay, 3, 3)
        inner.Controls.Add(txtAmountTendered, 4, 3)
        inner.Controls.Add(lblCh, 0, 4)
        inner.Controls.Add(lblChangeValue, 1, 4)
        inner.SetColumnSpan(lblChangeValue, 3)

        inner.Controls.Add(gridPanel, 0, 5)
        inner.SetColumnSpan(gridPanel, 6)

        inner.Controls.Add(bottom, 0, 6)
        inner.SetColumnSpan(bottom, 6)

        group.Controls.Add(inner)
        root.Controls.Add(group, 0, 1)

        Dim topBar As New FlowLayoutPanel()
        topBar.AutoSize = True
        topBar.Dock = DockStyle.Fill
        topBar.FlowDirection = FlowDirection.LeftToRight
        topBar.WrapContents = False
        Dim hdr As New Label()
        hdr.Text = "Build the sale, apply discount/tax, enter cash tendered, then finalize."
        hdr.AutoSize = True
        hdr.Font = New Font("Segoe UI", 9.5F, FontStyle.Italic)
        hdr.ForeColor = Color.FromArgb(80, 80, 80)
        topBar.Controls.Add(hdr)

        root.Controls.Add(topBar, 0, 0)

        Me.Controls.Clear()
        Me.Controls.Add(root)
    End Sub

    Private Shared Function FormatMoney(amount As Decimal) As String
        Return AppSettings.Current.CurrencySymbol & amount.ToString("N2", CultureInfo.CurrentCulture)
    End Function

    Private Sub chkApplyTax_CheckedChanged(sender As Object, e As EventArgs) Handles chkApplyTax.CheckedChanged
        numTaxPercent.Enabled = chkApplyTax.Checked
        UpdateSummaryLabels()
    End Sub

    Private Sub DiscountOrTax_Changed(sender As Object, e As EventArgs) Handles numDiscountPercent.ValueChanged, numTaxPercent.ValueChanged
        UpdateSummaryLabels()
    End Sub

    Private Sub txtAmountTendered_TextChanged(sender As Object, e As EventArgs) Handles txtAmountTendered.TextChanged
        UpdateSummaryLabels()
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
            txtPrice.Text = productPrices(selectedProduct).ToString("N2", CultureInfo.CurrentCulture)
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

        If Not Decimal.TryParse(txtPrice.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, price) OrElse price <= 0D Then
            MessageBox.Show("Selected product has no valid price.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            cmbProductName.Focus()
            Return
        End If

        If quantity < 1 Then
            MessageBox.Show("Quantity must be at least 1.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim line As New CartLineItem(productName, price, quantity)
        Dim rowNumber As Integer = dgvProducts.Rows.Count + 1

        Dim idx As Integer = dgvProducts.Rows.Add(
            rowNumber,
            productName,
            line.UnitPrice.ToString("N2", CultureInfo.CurrentCulture),
            quantity,
            line.LineSubtotal.ToString("N2", CultureInfo.CurrentCulture))

        dgvProducts.Rows(idx).Tag = line

        cmbProductName.SelectedIndex = -1
        txtPrice.Clear()
        numQuantity.Value = 1D
        cmbProductName.Focus()
        UpdateSummaryLabels()
    End Sub

    Private Sub btnRemove_Click(sender As Object, e As EventArgs) Handles btnRemove.Click
        If dgvProducts.SelectedRows.Count = 0 Then
            MessageBox.Show("Select a row to remove.", "Cart", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        dgvProducts.Rows.Remove(dgvProducts.SelectedRows(0))
        ReindexRows()
        UpdateSummaryLabels()
    End Sub

    Private Sub btnClear_Click(sender As Object, e As EventArgs) Handles btnClear.Click
        dgvProducts.Rows.Clear()
        txtAmountTendered.Clear()
        numDiscountPercent.Value = 0D
        chkApplyTax.Checked = False
        numTaxPercent.Value = 0D
        cmbProductName.SelectedIndex = -1
        txtPrice.Clear()
        numQuantity.Value = 1D
        cmbProductName.Focus()
        UpdateSummaryLabels()
    End Sub

    Private Sub dgvProducts_CellValidating(sender As Object, e As DataGridViewCellValidatingEventArgs) Handles dgvProducts.CellValidating
        If dgvProducts.Columns(e.ColumnIndex).Name <> "Quantity" Then
            Return
        End If

        Dim qty As Integer
        If Not Integer.TryParse(Convert.ToString(e.FormattedValue), qty) OrElse qty < 1 Then
            MessageBox.Show("Quantity must be a whole number of at least 1.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            e.Cancel = True
        End If
    End Sub

    Private Sub dgvProducts_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs) Handles dgvProducts.CellEndEdit
        If dgvProducts.Columns(e.ColumnIndex).Name <> "Quantity" Then
            Return
        End If

        Dim row As DataGridViewRow = dgvProducts.Rows(e.RowIndex)
        Dim line As CartLineItem = TryCast(row.Tag, CartLineItem)
        If line Is Nothing Then
            Return
        End If

        Dim qty As Integer = Convert.ToInt32(row.Cells("Quantity").Value)
        line.Quantity = qty
        row.Cells("Price").Value = line.UnitPrice.ToString("N2", CultureInfo.CurrentCulture)
        row.Cells("Subtotal").Value = line.LineSubtotal.ToString("N2", CultureInfo.CurrentCulture)
        UpdateSummaryLabels()
    End Sub

    Private Sub btnFinalize_Click(sender As Object, e As EventArgs) Handles btnFinalize.Click
        If dgvProducts.Rows.Count = 0 Then
            MessageBox.Show("Add at least one line item before finalizing.", "Cart", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim grandTotal As Decimal = GetGrandTotal()
        Dim tendered As Decimal
        If Not Decimal.TryParse(txtAmountTendered.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, tendered) Then
            MessageBox.Show("Enter a valid cash tendered amount.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtAmountTendered.Focus()
            Return
        End If

        If tendered < grandTotal Then
            MessageBox.Show("Cash tendered is less than the amount due.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtAmountTendered.Focus()
            Return
        End If

        Dim snapshot As ReceiptSnapshot = BuildReceiptSnapshot()
        snapshot.ReceiptText = BuildReceiptText(snapshot)
        Dim newSaleId As Integer = -1
        If Not SaveSale(snapshot, newSaleId) Then
            Return
        End If

        Using receiptForm As New ReceiptForm(snapshot, newSaleId)
            receiptForm.ShowDialog()
        End Using
    End Sub

    Private Function BuildReceiptSnapshot() As ReceiptSnapshot
        Dim snap As New ReceiptSnapshot With {
            .StoreName = AppSettings.Current.StoreName,
            .FooterText = AppSettings.Current.ReceiptFooter,
            .CurrencySymbol = AppSettings.Current.CurrencySymbol,
            .Lines = New List(Of ReceiptLineRow)(),
            .DiscountPercent = numDiscountPercent.Value,
            .TaxApplied = chkApplyTax.Checked,
            .TaxPercent = numTaxPercent.Value,
            .SubtotalBeforeDiscount = GetCartSubtotalSum(),
            .DiscountAmount = GetDiscountAmount(),
            .AmountBeforeTax = GetAmountBeforeTax(),
            .TaxAmount = GetTaxAmount(),
            .GrandTotal = GetGrandTotal(),
            .AmountTendered = Decimal.Parse(txtAmountTendered.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture),
            .ChangeGiven = GetChangeDue()
        }

        For Each row As DataGridViewRow In dgvProducts.Rows
            Dim line As CartLineItem = TryCast(row.Tag, CartLineItem)
            If line Is Nothing Then
                Continue For
            End If

            snap.Lines.Add(New ReceiptLineRow With {
                .ProductName = line.ProductName,
                .Quantity = line.Quantity,
                .UnitPrice = line.UnitPrice,
                .LineTotal = line.LineSubtotal
            })
        Next

        Return snap
    End Function

    Private Function BuildReceiptText(snapshot As ReceiptSnapshot) As String
        Dim receipt As New StringBuilder()
        Dim sym As String = snapshot.CurrencySymbol

        receipt.AppendLine("========================================")
        receipt.AppendLine("         " & snapshot.StoreName)
        receipt.AppendLine("========================================")
        receipt.AppendLine("Date: " & DateTime.Now.ToString("MMMM dd, yyyy hh:mm tt", CultureInfo.CurrentCulture))
        receipt.AppendLine("----------------------------------------")
        receipt.AppendLine("Item              Qty   Price    Subtotal")
        receipt.AppendLine("----------------------------------------")

        For Each lineRow As ReceiptLineRow In snapshot.Lines
            Dim itemName As String = lineRow.ProductName
            If itemName.Length > 15 Then
                itemName = itemName.Substring(0, 15)
            End If

            Dim qtyStr As String = lineRow.Quantity.ToString(CultureInfo.CurrentCulture)
            Dim priceStr As String = sym & lineRow.UnitPrice.ToString("N2", CultureInfo.CurrentCulture)
            Dim subStr As String = sym & lineRow.LineTotal.ToString("N2", CultureInfo.CurrentCulture)

            receipt.AppendLine(itemName.PadRight(18) &
                               qtyStr.PadLeft(3) & " " &
                               priceStr.PadLeft(8) & " " &
                               subStr.PadLeft(9))
        Next

        receipt.AppendLine("----------------------------------------")
        receipt.AppendLine("Subtotal:".PadRight(22) & (sym & snapshot.SubtotalBeforeDiscount.ToString("N2", CultureInfo.CurrentCulture)).PadLeft(18))

        If snapshot.DiscountAmount > 0D Then
            receipt.AppendLine(("Discount (" & snapshot.DiscountPercent.ToString("N2", CultureInfo.CurrentCulture) & "%):").PadRight(22) &
                               ("-" & sym & snapshot.DiscountAmount.ToString("N2", CultureInfo.CurrentCulture)).PadLeft(17))
        End If

        If snapshot.TaxApplied AndAlso snapshot.TaxAmount > 0D Then
            receipt.AppendLine(("Tax (" & snapshot.TaxPercent.ToString("N2", CultureInfo.CurrentCulture) & "%):").PadRight(22) &
                               (sym & snapshot.TaxAmount.ToString("N2", CultureInfo.CurrentCulture)).PadLeft(18))
        End If

        receipt.AppendLine("TOTAL DUE:".PadRight(22) & (sym & snapshot.GrandTotal.ToString("N2", CultureInfo.CurrentCulture)).PadLeft(18))
        receipt.AppendLine("TENDERED:".PadRight(22) & (sym & snapshot.AmountTendered.ToString("N2", CultureInfo.CurrentCulture)).PadLeft(18))
        receipt.AppendLine("CHANGE:".PadRight(22) & (sym & snapshot.ChangeGiven.ToString("N2", CultureInfo.CurrentCulture)).PadLeft(18))
        receipt.AppendLine("========================================")
        receipt.AppendLine("       " & snapshot.FooterText)
        receipt.AppendLine("========================================")

        Return receipt.ToString()
    End Function

    Private Function SaveSale(snapshot As ReceiptSnapshot, ByRef newSaleId As Integer) As Boolean
        newSaleId = -1
        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Using transaction As SqlTransaction = connection.BeginTransaction()
                    Dim saleQuery As String =
                        "INSERT INTO sales (" &
                        "total_amount, receipt_text, subtotal_before_discount, discount_percent, discount_amount, " &
                        "amount_before_tax, tax_percent, tax_amount, amount_tendered, change_given) " &
                        "VALUES (" &
                        "@total_amount, @receipt_text, @subtotal_before_discount, @discount_percent, @discount_amount, " &
                        "@amount_before_tax, @tax_percent, @tax_amount, @amount_tendered, @change_given); " &
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);"

                    Dim saleId As Integer

                    Using saleCommand As New SqlCommand(saleQuery, connection, transaction)
                        saleCommand.Parameters.AddWithValue("@total_amount", snapshot.GrandTotal)
                        saleCommand.Parameters.AddWithValue("@receipt_text", snapshot.ReceiptText)
                        saleCommand.Parameters.AddWithValue("@subtotal_before_discount", snapshot.SubtotalBeforeDiscount)
                        saleCommand.Parameters.AddWithValue("@discount_percent", snapshot.DiscountPercent)
                        saleCommand.Parameters.AddWithValue("@discount_amount", snapshot.DiscountAmount)
                        saleCommand.Parameters.AddWithValue("@amount_before_tax", snapshot.AmountBeforeTax)
                        saleCommand.Parameters.AddWithValue("@tax_percent", If(snapshot.TaxApplied, CObj(snapshot.TaxPercent), DBNull.Value))
                        saleCommand.Parameters.AddWithValue("@tax_amount", snapshot.TaxAmount)
                        saleCommand.Parameters.AddWithValue("@amount_tendered", snapshot.AmountTendered)
                        saleCommand.Parameters.AddWithValue("@change_given", snapshot.ChangeGiven)
                        saleId = Convert.ToInt32(saleCommand.ExecuteScalar())
                        newSaleId = saleId
                    End Using

                    For Each row As DataGridViewRow In dgvProducts.Rows
                        Dim line As CartLineItem = TryCast(row.Tag, CartLineItem)
                        If line Is Nothing Then
                            Continue For
                        End If

                        Dim itemQuery As String =
                            "INSERT INTO sale_items " &
                            "(sale_id, product_name, price, quantity, subtotal) " &
                            "VALUES (@sale_id, @product_name, @price, @quantity, @subtotal);"

                        Using itemCommand As New SqlCommand(itemQuery, connection, transaction)
                            itemCommand.Parameters.AddWithValue("@sale_id", saleId)
                            itemCommand.Parameters.AddWithValue("@product_name", line.ProductName)
                            itemCommand.Parameters.AddWithValue("@price", line.UnitPrice)
                            itemCommand.Parameters.AddWithValue("@quantity", line.Quantity)
                            itemCommand.Parameters.AddWithValue("@subtotal", line.LineSubtotal)
                            itemCommand.ExecuteNonQuery()
                        End Using
                    Next

                    transaction.Commit()
                    AuditLogger.LogSale(connection, "FINALIZE", saleId, "Sale saved from SalesForm")
                End Using
            End Using

            MessageBox.Show("Sale saved. Receipt window opened.", "Database", MessageBoxButtons.OK, MessageBoxIcon.Information)
            dgvProducts.Rows.Clear()
            txtAmountTendered.Clear()
            numDiscountPercent.Value = 0D
            chkApplyTax.Checked = False
            numTaxPercent.Value = 0D
            UpdateSummaryLabels()
            Return True
        Catch ex As Exception
            ShowDatabaseError("Error saving sale", ex)
            ErrorLogger.Log(ex, NameOf(SalesForm) & "." & NameOf(SaveSale))
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
            ErrorLogger.Log(ex, NameOf(SalesForm) & "." & NameOf(LoadProducts))
            btnAdd.Enabled = False
            cmbProductName.Enabled = False
            numQuantity.Enabled = False
            lblEmptyHint.Visible = True
            lblEmptyHint.Text = "Could not load products. Check database and App.config."
        End Try
    End Sub

    Private Function GetCartSubtotalSum() As Decimal
        Dim total As Decimal = 0D

        For Each row As DataGridViewRow In dgvProducts.Rows
            Dim line As CartLineItem = TryCast(row.Tag, CartLineItem)
            If line IsNot Nothing Then
                total += line.LineSubtotal
            End If
        Next

        Return total
    End Function

    Private Function GetDiscountAmount() As Decimal
        Dim cartSum As Decimal = GetCartSubtotalSum()
        Dim pct As Decimal = numDiscountPercent.Value
        Return Math.Round(cartSum * (pct / 100D), 2, MidpointRounding.AwayFromZero)
    End Function

    Private Function GetAmountBeforeTax() As Decimal
        Return Math.Max(0D, GetCartSubtotalSum() - GetDiscountAmount())
    End Function

    Private Function GetTaxAmount() As Decimal
        If Not chkApplyTax.Checked Then
            Return 0D
        End If

        Dim baseAmt As Decimal = GetAmountBeforeTax()
        Dim rate As Decimal = numTaxPercent.Value
        Return Math.Round(baseAmt * (rate / 100D), 2, MidpointRounding.AwayFromZero)
    End Function

    Private Function GetGrandTotal() As Decimal
        Return GetAmountBeforeTax() + GetTaxAmount()
    End Function

    Private Function GetChangeDue() As Decimal
        Dim grandTotal As Decimal = GetGrandTotal()
        Dim tendered As Decimal
        If Not Decimal.TryParse(txtAmountTendered.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, tendered) Then
            Return 0D
        End If

        Return Math.Max(0D, tendered - grandTotal)
    End Function

    Private Sub UpdateSummaryLabels()
        Dim cartSum As Decimal = GetCartSubtotalSum()
        lblSubtotalValue.Text = FormatMoney(cartSum)
        lblDiscountValue.Text = FormatMoney(GetDiscountAmount())
        lblTaxValue.Text = FormatMoney(GetTaxAmount())
        lblTotal.Text = FormatMoney(GetGrandTotal())
        lblChangeValue.Text = FormatMoney(GetChangeDue())
    End Sub

    Private Sub ReindexRows()
        For i As Integer = 0 To dgvProducts.Rows.Count - 1
            dgvProducts.Rows(i).Cells("Index").Value = i + 1
        Next
    End Sub

End Class
