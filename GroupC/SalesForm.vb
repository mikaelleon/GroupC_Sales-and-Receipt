Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
Imports System.Text
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient

Public Class SalesForm

    Private Class ProductCatalogEntry
        Public Property UnitPrice As Decimal
        Public Property CategoryId As Integer?
    End Class

    Private Class SalesCategoryFilterItem
        Public Enum FilterKindEnum
            AllCategories = 0
            Uncategorized = 1
            SpecificCategory = 2
        End Enum

        Public Property Kind As FilterKindEnum = FilterKindEnum.AllCategories
        Public Property CategoryId As Integer?
        Public Property Display As String = String.Empty

        Public Overrides Function ToString() As String
            Return Display
        End Function
    End Class

    Private Const MinLineUnitPrice As Decimal = 0.01D
    Private Const MaxLineUnitPrice As Decimal = 999999.99D
    Private Const MinLineQty As Integer = 1
    Private Const MaxLineQty As Integer = 99999
    Private Const MaxCashTendered As Decimal = 999999999.99D

    Private WithEvents cmbSalesCategory As ComboBox
    Private WithEvents cmbProductName As ComboBox
    Private txtPrice As TextBox
    Private WithEvents numQuantity As NumericUpDown
    Private WithEvents dgvProducts As DataGridView
    Private lblTotal As Label
    Private lblEmptyHint As Label
    Private WithEvents btnOpenProducts As Button

    Private WithEvents btnAdd As Button
    Private WithEvents btnRemove As Button
    Private WithEvents btnClear As Button
    Private WithEvents btnFinalize As Button

    Private lblDiscountHeading As Label
    Private WithEvents radDiscountPercent As RadioButton
    Private WithEvents radDiscountFixed As RadioButton
    Private WithEvents numDiscountPercent As NumericUpDown
    Private WithEvents chkApplyTax As CheckBox
    Private WithEvents numTaxPercent As NumericUpDown
    Private WithEvents txtAmountTendered As TextBox

    Private lblSalesInputError As Label

    Private lblSubtotalValue As Label
    Private lblDiscountValue As Label
    Private lblTaxValue As Label
    Private lblChangeValue As Label

    Private statusStrip As StatusStrip
    Private statusLabel As ToolStripStatusLabel
    Private WithEvents statusClearTimer As Timer

    Private ReadOnly productCatalog As New Dictionary(Of String, ProductCatalogEntry)(StringComparer.OrdinalIgnoreCase)
    Private suppressSalesCategoryEvent As Boolean

    Private Sub SalesForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        UiTheme.ApplyStandardWindowChrome(Me)
        SetupForm()
        CreateControls()
        Try
            DatabaseInitializer.EnsureDatabase()
        Catch
        End Try
        LoadProducts()
        ConfigureDiscountNumeric()
        UpdateSummaryLabels()
    End Sub

    Private Sub SetupForm()
        Me.Text = "Group C - Sales / Cart"
        Me.MinimumSize = New Size(720, 620)
        Me.Size = New Size(820, 640)
        Me.StartPosition = FormStartPosition.CenterScreen
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

        Dim cardOuter As Panel = UiTheme.CreateCardPanel(New Padding(12))
        cardOuter.Dock = DockStyle.Fill
        Dim cardInner As Panel = UiTheme.GetCardContentHost(cardOuter)

        Dim sectionTitle As New Label()
        sectionTitle.Text = "Sales / Cart"
        sectionTitle.Font = New Font("Segoe UI", 11.0F, FontStyle.Bold)
        sectionTitle.ForeColor = UiTheme.TextPrimary
        sectionTitle.Dock = DockStyle.Top
        sectionTitle.Height = 28

        Dim inner As New TableLayoutPanel()
        inner.Dock = DockStyle.Fill
        inner.ColumnCount = 6
        inner.RowCount = 10
        For c As Integer = 0 To 5
            If c = 1 OrElse c = 3 Then
                inner.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 18.0F))
            Else
                inner.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            End If
        Next
        For r As Integer = 0 To 9
            If r = 8 Then
                inner.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
            Else
                inner.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            End If
        Next

        Dim lblCatFilter As New Label()
        lblCatFilter.Text = "Category filter"
        lblCatFilter.AutoSize = True
        lblCatFilter.Margin = New Padding(0, 8, 8, 8)
        lblCatFilter.ForeColor = UiTheme.TextSecondary

        cmbSalesCategory = New ComboBox()
        cmbSalesCategory.DropDownStyle = ComboBoxStyle.DropDownList
        cmbSalesCategory.Dock = DockStyle.Fill
        cmbSalesCategory.Margin = New Padding(0, 4, 12, 4)
        cmbSalesCategory.TabIndex = 0

        Dim lblProduct As New Label()
        lblProduct.Text = "Product"
        lblProduct.AutoSize = True
        lblProduct.Margin = New Padding(0, 8, 8, 8)
        lblProduct.ForeColor = UiTheme.TextSecondary

        cmbProductName = New ComboBox()
        cmbProductName.DropDownStyle = ComboBoxStyle.DropDownList
        cmbProductName.Dock = DockStyle.Fill
        cmbProductName.Margin = New Padding(0, 4, 12, 4)
        cmbProductName.TabIndex = 1

        Dim lblPrice As New Label()
        lblPrice.Text = "Price"
        lblPrice.AutoSize = True
        lblPrice.Margin = New Padding(0, 8, 8, 8)
        lblPrice.ForeColor = UiTheme.TextSecondary

        txtPrice = New TextBox()
        txtPrice.ReadOnly = True
        txtPrice.BackColor = UiTheme.CardSurface
        txtPrice.Dock = DockStyle.Fill
        txtPrice.Margin = New Padding(0, 4, 12, 4)
        txtPrice.TabIndex = 1
        txtPrice.TextAlign = HorizontalAlignment.Right

        Dim lblQty As New Label()
        lblQty.Text = "Quantity"
        lblQty.AutoSize = True
        lblQty.Margin = New Padding(0, 8, 8, 8)
        lblQty.ForeColor = UiTheme.TextSecondary

        numQuantity = New NumericUpDown()
        numQuantity.Minimum = MinLineQty
        numQuantity.Maximum = MaxLineQty
        numQuantity.Dock = DockStyle.Fill
        numQuantity.Margin = New Padding(0, 4, 12, 4)
        numQuantity.TabIndex = 3
        numQuantity.TextAlign = HorizontalAlignment.Right

        btnAdd = New Button()
        btnAdd.Text = "&Add to cart"
        btnAdd.AutoSize = True
        btnAdd.MinimumSize = New Size(110, 32)
        btnAdd.TabIndex = 4
        UiTheme.ApplyPrimaryButton(btnAdd)

        btnRemove = New Button()
        btnRemove.Text = "&Remove"
        btnRemove.AutoSize = True
        btnRemove.MinimumSize = New Size(100, 32)
        btnRemove.TabIndex = 5
        UiTheme.ApplySecondaryAccentButton(btnRemove)

        btnClear = New Button()
        btnClear.Text = "C&lear all"
        btnClear.AutoSize = True
        btnClear.MinimumSize = New Size(100, 32)
        btnClear.TabIndex = 6
        UiTheme.ApplyWarningButton(btnClear)

        lblEmptyHint = New Label()
        lblEmptyHint.Text = "No products in catalog. Open Manage Products to add items."
        lblEmptyHint.AutoSize = True
        lblEmptyHint.Dock = DockStyle.Top
        lblEmptyHint.ForeColor = UiTheme.TextSecondary
        lblEmptyHint.Visible = False
        lblEmptyHint.Margin = New Padding(0, 4, 0, 8)

        btnOpenProducts = New Button()
        btnOpenProducts.Text = "Open &Products…"
        btnOpenProducts.AutoSize = True
        btnOpenProducts.Visible = False
        UiTheme.ApplySecondaryButton(btnOpenProducts)

        lblDiscountHeading = New Label()
        lblDiscountHeading.Text = "Discount (%)"
        lblDiscountHeading.AutoSize = True
        lblDiscountHeading.Margin = New Padding(0, 8, 8, 8)
        lblDiscountHeading.ForeColor = UiTheme.TextSecondary

        radDiscountPercent = New RadioButton()
        radDiscountPercent.Text = "Percent (%)"
        radDiscountPercent.AutoSize = True
        radDiscountPercent.Margin = New Padding(0, 6, 12, 6)
        radDiscountPercent.Checked = True
        radDiscountPercent.TabStop = True
        radDiscountPercent.ForeColor = UiTheme.TextPrimary

        Dim symDisc As String = AppSettings.Current.CurrencySymbol
        radDiscountFixed = New RadioButton()
        radDiscountFixed.Text = "Fixed amount (" & symDisc & ")"
        radDiscountFixed.AutoSize = True
        radDiscountFixed.Margin = New Padding(0, 6, 12, 6)
        radDiscountFixed.ForeColor = UiTheme.TextPrimary

        numDiscountPercent = New NumericUpDown()
        numDiscountPercent.DecimalPlaces = 2
        numDiscountPercent.Minimum = 0D
        numDiscountPercent.Maximum = 100D
        numDiscountPercent.Increment = 0.5D
        numDiscountPercent.Dock = DockStyle.Fill
        numDiscountPercent.Margin = New Padding(0, 4, 12, 4)
        numDiscountPercent.TabIndex = 7

        lblDiscountValue = New Label()
        lblDiscountValue.Text = AppSettings.Current.CurrencySymbol & "0.00"
        lblDiscountValue.AutoSize = True
        lblDiscountValue.Margin = New Padding(0, 8, 8, 8)

        chkApplyTax = New CheckBox()
        chkApplyTax.Text = "VAT/Tax %"
        chkApplyTax.AutoSize = True
        chkApplyTax.Margin = New Padding(0, 8, 8, 8)
        chkApplyTax.TabIndex = 8
        chkApplyTax.ForeColor = UiTheme.TextSecondary

        numTaxPercent = New NumericUpDown()
        numTaxPercent.DecimalPlaces = 2
        numTaxPercent.Minimum = 0D
        numTaxPercent.Maximum = 100D
        numTaxPercent.Increment = 0.5D
        numTaxPercent.Enabled = False
        numTaxPercent.Dock = DockStyle.Fill
        numTaxPercent.Margin = New Padding(0, 4, 12, 4)
        numTaxPercent.TabIndex = 9

        lblTaxValue = New Label()
        lblTaxValue.Text = AppSettings.Current.CurrencySymbol & "0.00"
        lblTaxValue.AutoSize = True
        lblTaxValue.Margin = New Padding(0, 8, 8, 8)

        Dim lblSub As New Label()
        lblSub.Text = "Lines subtotal"
        lblSub.AutoSize = True
        lblSub.Margin = New Padding(0, 8, 8, 8)
        lblSub.ForeColor = UiTheme.TextSecondary

        lblSubtotalValue = New Label()
        lblSubtotalValue.Text = AppSettings.Current.CurrencySymbol & "0.00"
        lblSubtotalValue.AutoSize = True
        lblSubtotalValue.Margin = New Padding(0, 8, 8, 8)

        Dim lblPay As New Label()
        lblPay.Text = "Cash tendered"
        lblPay.AutoSize = True
        lblPay.Margin = New Padding(0, 8, 8, 8)
        lblPay.ForeColor = UiTheme.TextSecondary

        txtAmountTendered = New TextBox()
        txtAmountTendered.Dock = DockStyle.Fill
        txtAmountTendered.Margin = New Padding(0, 4, 12, 4)
        txtAmountTendered.TabIndex = 10
        txtAmountTendered.TextAlign = HorizontalAlignment.Right

        Dim lblCh As New Label()
        lblCh.Text = "Change"
        lblCh.AutoSize = True
        lblCh.Margin = New Padding(0, 8, 8, 8)
        lblCh.ForeColor = UiTheme.TextSecondary

        lblChangeValue = New Label()
        lblChangeValue.Text = AppSettings.Current.CurrencySymbol & "0.00"
        lblChangeValue.ForeColor = UiTheme.Success
        lblChangeValue.AutoSize = True
        lblChangeValue.Margin = New Padding(0, 8, 8, 8)

        lblSalesInputError = New Label()
        lblSalesInputError.AutoSize = True
        lblSalesInputError.Margin = New Padding(0, 4, 0, 6)
        lblSalesInputError.ForeColor = UiTheme.Danger
        lblSalesInputError.Visible = False
        lblSalesInputError.Dock = DockStyle.Fill

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
        dgvProducts.TabIndex = 11
        UiTheme.ApplyDataGridViewChrome(dgvProducts)

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
        totalLbl.ForeColor = UiTheme.TextPrimary

        lblTotal = New Label()
        lblTotal.Text = FormatMoney(0D)
        lblTotal.Font = New Font("Segoe UI", 14.0F, FontStyle.Bold)
        lblTotal.ForeColor = UiTheme.Success
        lblTotal.TextAlign = ContentAlignment.MiddleRight
        lblTotal.Dock = DockStyle.Fill
        lblTotal.Margin = New Padding(0, 10, 12, 10)

        btnFinalize = New Button()
        btnFinalize.Text = "Finalize and save sale"
        btnFinalize.AutoSize = True
        btnFinalize.MinimumSize = New Size(180, 40)
        btnFinalize.TabIndex = 12
        UiTheme.ApplySuccessButton(btnFinalize)

        bottom.Controls.Add(totalLbl, 0, 0)
        bottom.Controls.Add(lblTotal, 1, 0)
        bottom.Controls.Add(btnFinalize, 2, 0)

        inner.Controls.Add(lblCatFilter, 0, 0)
        inner.Controls.Add(cmbSalesCategory, 1, 0)
        inner.SetColumnSpan(cmbSalesCategory, 5)

        inner.Controls.Add(lblProduct, 0, 1)
        inner.Controls.Add(cmbProductName, 1, 1)
        inner.SetColumnSpan(cmbProductName, 2)
        inner.Controls.Add(lblPrice, 3, 1)
        inner.Controls.Add(txtPrice, 4, 1)
        inner.Controls.Add(btnAdd, 5, 1)

        inner.Controls.Add(lblQty, 0, 2)
        inner.Controls.Add(numQuantity, 1, 2)
        inner.Controls.Add(btnRemove, 2, 2)
        inner.Controls.Add(btnClear, 3, 2)
        inner.SetColumnSpan(btnClear, 2)
        inner.Controls.Add(btnOpenProducts, 5, 2)

        inner.Controls.Add(lblSub, 0, 3)
        inner.Controls.Add(lblSubtotalValue, 1, 3)
        inner.Controls.Add(lblDiscountHeading, 2, 3)
        inner.Controls.Add(numDiscountPercent, 3, 3)
        inner.Controls.Add(lblDiscountValue, 4, 3)
        inner.SetColumnSpan(lblDiscountValue, 2)

        inner.Controls.Add(radDiscountPercent, 2, 4)
        inner.Controls.Add(radDiscountFixed, 3, 4)
        inner.SetColumnSpan(radDiscountFixed, 3)

        inner.Controls.Add(chkApplyTax, 0, 5)
        inner.Controls.Add(numTaxPercent, 1, 5)
        inner.Controls.Add(lblTaxValue, 2, 5)
        inner.Controls.Add(lblPay, 3, 5)
        inner.Controls.Add(txtAmountTendered, 4, 5)
        inner.Controls.Add(lblCh, 0, 6)
        inner.Controls.Add(lblChangeValue, 1, 6)
        inner.SetColumnSpan(lblChangeValue, 3)

        inner.Controls.Add(lblSalesInputError, 0, 7)
        inner.SetColumnSpan(lblSalesInputError, 6)

        inner.Controls.Add(gridPanel, 0, 8)
        inner.SetColumnSpan(gridPanel, 6)

        inner.Controls.Add(bottom, 0, 9)
        inner.SetColumnSpan(bottom, 6)

        cardInner.Controls.Add(sectionTitle)
        cardInner.Controls.Add(inner)
        inner.Dock = DockStyle.Fill
        root.Controls.Add(cardOuter, 0, 1)

        Dim topBar As New FlowLayoutPanel()
        topBar.AutoSize = True
        topBar.Dock = DockStyle.Fill
        topBar.FlowDirection = FlowDirection.LeftToRight
        topBar.WrapContents = False
        Dim hdr As New Label()
        hdr.Text = "Build the sale, apply discount/tax, enter cash tendered, then finalize."
        hdr.AutoSize = True
        hdr.Font = New Font("Segoe UI", 9.5F, FontStyle.Italic)
        hdr.ForeColor = UiTheme.TextSecondary
        topBar.Controls.Add(hdr)

        root.Controls.Add(topBar, 0, 0)

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

    Private Shared Function FormatMoney(amount As Decimal) As String
        Return AppSettings.Current.CurrencySymbol & amount.ToString("N2", CultureInfo.CurrentCulture)
    End Function

    Private Sub ClearSalesInputError()
        If lblSalesInputError Is Nothing Then
            Return
        End If

        lblSalesInputError.Text = String.Empty
        lblSalesInputError.Visible = False
    End Sub

    Private Sub ShowSalesInputError(message As String)
        lblSalesInputError.Text = message
        lblSalesInputError.Visible = True
    End Sub

    Private Function TryParsePositiveDecimal(text As String, minInclusive As Decimal, maxInclusive As Decimal, ByRef value As Decimal) As Boolean
        value = 0D
        Dim trimmed As String = text.Trim()
        If trimmed.Length = 0 Then
            Return False
        End If

        If Not Decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.CurrentCulture, value) Then
            If Not Decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, value) Then
                Return False
            End If
        End If

        Return value >= minInclusive AndAlso value <= maxInclusive
    End Function

    Private Sub chkApplyTax_CheckedChanged(sender As Object, e As EventArgs) Handles chkApplyTax.CheckedChanged
        numTaxPercent.Enabled = chkApplyTax.Checked
        UpdateSummaryLabels()
    End Sub

    Private Sub DiscountOrTax_Changed(sender As Object, e As EventArgs) Handles numDiscountPercent.ValueChanged, numTaxPercent.ValueChanged
        UpdateSummaryLabels()
    End Sub

    Private Sub radDiscountType_CheckedChanged(sender As Object, e As EventArgs) Handles radDiscountPercent.CheckedChanged, radDiscountFixed.CheckedChanged
        ConfigureDiscountNumeric()
        UpdateSummaryLabels()
    End Sub

    Private Sub ConfigureDiscountNumeric()
        If numDiscountPercent Is Nothing OrElse radDiscountPercent Is Nothing Then
            Return
        End If

        Dim cur As Decimal = numDiscountPercent.Value
        If radDiscountPercent.Checked Then
            numDiscountPercent.DecimalPlaces = 2
            numDiscountPercent.Minimum = 0D
            numDiscountPercent.Maximum = 100D
            numDiscountPercent.Increment = 0.5D
            If cur > 100D Then
                numDiscountPercent.Value = 100D
            End If
        Else
            numDiscountPercent.DecimalPlaces = 2
            numDiscountPercent.Minimum = 0D
            numDiscountPercent.Maximum = MaxCashTendered
            numDiscountPercent.Increment = 1D
        End If
    End Sub

    Private Sub cmbSalesCategory_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbSalesCategory.SelectedIndexChanged
        If suppressSalesCategoryEvent Then
            Return
        End If

        ClearSalesInputError()
        FilterProductCombo()
    End Sub

    Private Sub txtAmountTendered_TextChanged(sender As Object, e As EventArgs) Handles txtAmountTendered.TextChanged
        ClearSalesInputError()
        UpdateSummaryLabels()
    End Sub

    Private Sub numQuantity_ValueChanged(sender As Object, e As EventArgs) Handles numQuantity.ValueChanged
        ClearSalesInputError()
    End Sub

    Private Sub btnOpenProducts_Click(sender As Object, e As EventArgs) Handles btnOpenProducts.Click
        If Not AppSession.RequireAdmin(Me) Then
            Return
        End If

        Using form As New ProductsForm()
            form.ShowDialog()
        End Using
        LoadProducts()
    End Sub

    Private Sub cmbProductName_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbProductName.SelectedIndexChanged
        ClearSalesInputError()
        Dim selectedProduct As String = cmbProductName.Text

        Dim entry As ProductCatalogEntry = Nothing
        If productCatalog.TryGetValue(selectedProduct, entry) Then
            txtPrice.Text = entry.UnitPrice.ToString("N2", CultureInfo.CurrentCulture)
        Else
            txtPrice.Clear()
        End If
    End Sub

    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        Try
            ClearSalesInputError()
            Dim productName As String = cmbProductName.Text.Trim()
            Dim price As Decimal

            If productName = String.Empty Then
                ShowSalesInputError("Select a product.")
                MessageBox.Show("Please select a product.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                cmbProductName.Focus()
                Return
            End If

            Dim priceText As String = txtPrice.Text.Trim()
            If priceText.Length = 0 Then
                ShowSalesInputError("Price cannot be empty.")
                MessageBox.Show("Price cannot be empty. Select a product with a valid price.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                cmbProductName.Focus()
                Return
            End If

            If Not TryParsePositiveDecimal(priceText, MinLineUnitPrice, MaxLineUnitPrice, price) Then
                ShowSalesInputError(String.Format(CultureInfo.CurrentCulture, "Price must be a number from {0:N2} to {1:N2}.", MinLineUnitPrice, MaxLineUnitPrice))
                MessageBox.Show(
                String.Format(CultureInfo.CurrentCulture, "Enter a valid unit price between {0:N2} and {1:N2}.", MinLineUnitPrice, MaxLineUnitPrice),
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)
                Return
            End If

            Dim qtyText As String = numQuantity.Text.Trim()
            If qtyText.Length = 0 Then
                ShowSalesInputError("Quantity cannot be empty.")
                MessageBox.Show("Quantity cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                numQuantity.Focus()
                Return
            End If

            Dim qtyDecimal As Decimal
            If Not Decimal.TryParse(qtyText, NumberStyles.Number, CultureInfo.CurrentCulture, qtyDecimal) Then
                If Not Decimal.TryParse(qtyText, NumberStyles.Number, CultureInfo.InvariantCulture, qtyDecimal) Then
                    ShowSalesInputError("Quantity must be a valid whole number.")
                    MessageBox.Show("Quantity must be a valid whole number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    numQuantity.Focus()
                    Return
                End If
            End If

            If qtyDecimal <> Decimal.Truncate(qtyDecimal) Then
                ShowSalesInputError("Quantity must be a whole number.")
                MessageBox.Show("Quantity must be a whole number (no decimals).", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                numQuantity.Focus()
                Return
            End If

            Dim quantity As Integer = CInt(qtyDecimal)
            If quantity < MinLineQty OrElse quantity > MaxLineQty Then
                ShowSalesInputError(String.Format(CultureInfo.CurrentCulture, "Quantity must be from {0} to {1}.", MinLineQty, MaxLineQty))
                MessageBox.Show(
                String.Format(CultureInfo.CurrentCulture, "Quantity must be between {0} and {1}.", MinLineQty, MaxLineQty),
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)
                numQuantity.Focus()
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
            numQuantity.Value = MinLineQty
            cmbProductName.Focus()
            ClearSalesInputError()

            ReindexRows()
            UpdateSummaryLabels()
            ShowStatus("Line added to cart.", False)

        Catch ex As Exception
            ' THIS WILL CATCH THE CRASH AND TELL US EXACTLY WHERE IT HAPPENED!
            MessageBox.Show(
            "Crash Location: " & vbCrLf & ex.StackTrace,
            "Error Details",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error)
        End Try
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
        Dim hasLines As Boolean = dgvProducts.Rows.Count > 0
        Dim hasTenderOrDiscount As Boolean =
            txtAmountTendered.Text.Trim().Length > 0 OrElse
            numDiscountPercent.Value <> 0D OrElse
            chkApplyTax.Checked OrElse
            numTaxPercent.Value <> 0D

        If Not hasLines AndAlso Not hasTenderOrDiscount Then
            Return
        End If

        If MessageBox.Show(
            "Clear the entire cart and reset tendered amount, discount, and tax?",
            "Confirm clear cart",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) <> DialogResult.OK Then
            Return
        End If

        dgvProducts.Rows.Clear()
        txtAmountTendered.Clear()
        radDiscountPercent.Checked = True
        numDiscountPercent.Value = 0D
        ConfigureDiscountNumeric()
        chkApplyTax.Checked = False
        numTaxPercent.Value = 0D
        cmbProductName.SelectedIndex = -1
        txtPrice.Clear()
        numQuantity.Value = MinLineQty
        cmbProductName.Focus()
        ClearSalesInputError()
        UpdateSummaryLabels()
        ShowStatus("Cart cleared.", False)
    End Sub

    Private Sub dgvProducts_CellValidating(sender As Object, e As DataGridViewCellValidatingEventArgs) Handles dgvProducts.CellValidating
        If dgvProducts.Columns(e.ColumnIndex).Name <> "Quantity" Then
            Return
        End If

        Dim raw As String = Convert.ToString(e.FormattedValue).Trim()
        If raw.Length = 0 Then
            ShowSalesInputError("Line quantity cannot be empty.")
            MessageBox.Show("Quantity cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            e.Cancel = True
            Return
        End If

        Dim qtyDec As Decimal
        If Not Decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, qtyDec) Then
            If Not Decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, qtyDec) Then
                ShowSalesInputError("Quantity must be a valid whole number.")
                MessageBox.Show("Quantity must be a valid whole number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                e.Cancel = True
                Return
            End If
        End If

        If qtyDec <> Decimal.Truncate(qtyDec) Then
            ShowSalesInputError("Quantity must be a whole number.")
            MessageBox.Show("Quantity must be a whole number (no decimals).", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            e.Cancel = True
            Return
        End If

        Dim qty As Integer = CInt(qtyDec)
        If qty < MinLineQty OrElse qty > MaxLineQty Then
            ShowSalesInputError(String.Format(CultureInfo.CurrentCulture, "Quantity must be from {0} to {1}.", MinLineQty, MaxLineQty))
            MessageBox.Show(
                String.Format(CultureInfo.CurrentCulture, "Quantity must be between {0} and {1}.", MinLineQty, MaxLineQty),
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)
            e.Cancel = True
            Return
        End If
    End Sub

    Private Sub dgvProducts_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs) Handles dgvProducts.CellEndEdit
        If dgvProducts.Columns(e.ColumnIndex).Name <> "Quantity" Then
            Return
        End If

        ClearSalesInputError()

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
        ClearSalesInputError()
        If dgvProducts.Rows.Count = 0 Then
            MessageBox.Show("Add at least one line item before finalizing.", "Cart", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim cartSubtotalCheck As Decimal = GetCartSubtotalSum()
        Dim discountCheck As Decimal = GetDiscountAmount()
        If discountCheck > cartSubtotalCheck Then
            ShowSalesInputError("Discount cannot exceed the cart subtotal.")
            MessageBox.Show("Discount cannot exceed the cart subtotal.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        For Each row As DataGridViewRow In dgvProducts.Rows
            Dim line As CartLineItem = TryCast(row.Tag, CartLineItem)
            If line Is Nothing Then
                Continue For
            End If

            If line.UnitPrice < MinLineUnitPrice OrElse line.UnitPrice > MaxLineUnitPrice Then
                ShowSalesInputError("A cart line has an invalid unit price. Remove the line or fix the catalog.")
                MessageBox.Show("A cart line has an invalid unit price. Remove the line or fix the product catalog.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If line.Quantity < MinLineQty OrElse line.Quantity > MaxLineQty Then
                ShowSalesInputError(String.Format(CultureInfo.CurrentCulture, "Each line quantity must be from {0} to {1}.", MinLineQty, MaxLineQty))
                MessageBox.Show(
                    String.Format(CultureInfo.CurrentCulture, "Each line quantity must be between {0} and {1}.", MinLineQty, MaxLineQty),
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)
                Return
            End If
        Next

        Dim grandTotal As Decimal = GetGrandTotal()
        Dim tenderedText As String = txtAmountTendered.Text.Trim()
        If tenderedText.Length = 0 Then
            ShowSalesInputError("Cash tendered cannot be empty.")
            MessageBox.Show("Enter the cash tendered amount.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtAmountTendered.Focus()
            Return
        End If

        Dim tendered As Decimal
        If Not Decimal.TryParse(tenderedText, NumberStyles.Number, CultureInfo.CurrentCulture, tendered) Then
            If Not Decimal.TryParse(tenderedText, NumberStyles.Number, CultureInfo.InvariantCulture, tendered) Then
                ShowSalesInputError("Cash tendered must be a valid number.")
                MessageBox.Show("Enter a valid cash tendered amount.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtAmountTendered.Focus()
                Return
            End If
        End If

        If tendered < 0D Then
            ShowSalesInputError("Cash tendered cannot be negative.")
            MessageBox.Show("Cash tendered cannot be negative.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtAmountTendered.Focus()
            Return
        End If

        If tendered > MaxCashTendered Then
            ShowSalesInputError(String.Format(CultureInfo.CurrentCulture, "Cash tendered cannot exceed {0:N2}.", MaxCashTendered))
            MessageBox.Show(
                String.Format(CultureInfo.CurrentCulture, "Cash tendered is too large (maximum {0:N2}).", MaxCashTendered),
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)
            txtAmountTendered.Focus()
            Return
        End If

        If tendered < grandTotal Then
            ShowSalesInputError("Cash tendered is less than the amount due.")
            MessageBox.Show("Cash tendered is less than the amount due.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtAmountTendered.Focus()
            Return
        End If

        If MessageBox.Show(
            "Finalize and save this sale to the database? This cannot be undone from this screen.",
            "Confirm sale",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) <> DialogResult.OK Then
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
        Dim tenderedSnap As Decimal
        Dim tenderText As String = txtAmountTendered.Text.Trim()
        If Not Decimal.TryParse(tenderText, NumberStyles.Number, CultureInfo.CurrentCulture, tenderedSnap) Then
            Decimal.TryParse(tenderText, NumberStyles.Number, CultureInfo.InvariantCulture, tenderedSnap)
        End If

        Dim snap As New ReceiptSnapshot With {
            .StoreName = AppSettings.Current.StoreName,
            .FooterText = AppSettings.Current.ReceiptFooter,
            .CurrencySymbol = AppSettings.Current.CurrencySymbol,
            .Lines = New List(Of ReceiptLineRow)(),
            .DiscountPercent = numDiscountPercent.Value,
            .DiscountIsPercent = radDiscountPercent.Checked,
            .TaxApplied = chkApplyTax.Checked,
            .TaxPercent = numTaxPercent.Value,
            .SubtotalBeforeDiscount = GetCartSubtotalSum(),
            .DiscountAmount = GetDiscountAmount(),
            .AmountBeforeTax = GetAmountBeforeTax(),
            .TaxAmount = GetTaxAmount(),
            .GrandTotal = GetGrandTotal(),
            .AmountTendered = tenderedSnap,
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
            Dim discLabel As String
            If snapshot.DiscountIsPercent Then
                discLabel = "Discount (" & snapshot.DiscountPercent.ToString("N2", CultureInfo.CurrentCulture) & "%):"
            Else
                discLabel = "Discount (" & sym & snapshot.DiscountPercent.ToString("N2", CultureInfo.CurrentCulture) & " fixed):"
            End If

            receipt.AppendLine(discLabel.PadRight(22) &
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
                    AuditLogger.LogAudit(
                        connection,
                        "SALE_FINALIZED",
                        "Sale ID " & saleId.ToString(CultureInfo.InvariantCulture) & ", total " & snapshot.GrandTotal.ToString("N2", CultureInfo.InvariantCulture),
                        AppSession.CurrentRole)
                End Using
            End Using

            ShowStatus("Sale saved. Receipt preview opened.", False)
            dgvProducts.Rows.Clear()
            txtAmountTendered.Clear()
            radDiscountPercent.Checked = True
            numDiscountPercent.Value = 0D
            ConfigureDiscountNumeric()
            chkApplyTax.Checked = False
            numTaxPercent.Value = 0D
            numQuantity.Value = MinLineQty
            ClearSalesInputError()
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
        productCatalog.Clear()
        cmbProductName.Items.Clear()
        txtPrice.Clear()

        Dim prevCatIdx As Integer = 0
        If cmbSalesCategory IsNot Nothing AndAlso cmbSalesCategory.SelectedIndex >= 0 Then
            prevCatIdx = cmbSalesCategory.SelectedIndex
        End If

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                suppressSalesCategoryEvent = True
                cmbSalesCategory.Items.Clear()
                cmbSalesCategory.Items.Add(
                    New SalesCategoryFilterItem With {
                        .Kind = SalesCategoryFilterItem.FilterKindEnum.AllCategories,
                        .Display = "All categories"})
                cmbSalesCategory.Items.Add(
                    New SalesCategoryFilterItem With {
                        .Kind = SalesCategoryFilterItem.FilterKindEnum.Uncategorized,
                        .Display = "Uncategorized"})

                Dim catSql As String = "SELECT category_id, category_name FROM dbo.categories WHERE is_active = 1 ORDER BY category_name;"
                Using catCmd As New SqlCommand(catSql, connection)
                    Using r As SqlDataReader = catCmd.ExecuteReader()
                        While r.Read()
                            Dim cid As Integer = Convert.ToInt32(r("category_id"))
                            Dim cname As String = r("category_name").ToString()
                            cmbSalesCategory.Items.Add(
                                New SalesCategoryFilterItem With {
                                    .Kind = SalesCategoryFilterItem.FilterKindEnum.SpecificCategory,
                                    .CategoryId = cid,
                                    .Display = cname})
                        End While
                    End Using
                End Using

                If prevCatIdx < cmbSalesCategory.Items.Count Then
                    cmbSalesCategory.SelectedIndex = prevCatIdx
                Else
                    cmbSalesCategory.SelectedIndex = 0
                End If

                suppressSalesCategoryEvent = False

                Dim query As String =
                    "SELECT product_name, price, category_id " &
                    "FROM products " &
                    "WHERE is_active = 1 " &
                    "ORDER BY product_name;"

                Using command As New SqlCommand(query, connection)
                    Using reader As SqlDataReader = command.ExecuteReader()
                        While reader.Read()
                            Dim productName As String = reader("product_name").ToString()
                            Dim price As Decimal = Convert.ToDecimal(reader("price"))
                            Dim catId As Integer? = Nothing
                            Dim ord As Integer = reader.GetOrdinal("category_id")
                            If Not reader.IsDBNull(ord) Then
                                catId = Convert.ToInt32(reader.GetValue(ord))
                            End If

                            productCatalog(productName) = New ProductCatalogEntry With {
                                .UnitPrice = price,
                                .CategoryId = catId}
                        End While
                    End Using
                End Using
            End Using

            FilterProductCombo()

            Dim hasProducts As Boolean = cmbProductName.Items.Count > 0
            btnAdd.Enabled = hasProducts
            cmbProductName.Enabled = hasProducts
            numQuantity.Enabled = hasProducts
            lblEmptyHint.Visible = Not hasProducts
            btnOpenProducts.Visible = Not hasProducts AndAlso AppSession.IsAdmin()
        Catch ex As Exception
            suppressSalesCategoryEvent = False
            ShowDatabaseError("Error loading products", ex)
            ErrorLogger.Log(ex, NameOf(SalesForm) & "." & NameOf(LoadProducts))
            btnAdd.Enabled = False
            cmbProductName.Enabled = False
            numQuantity.Enabled = False
            lblEmptyHint.Visible = True
            lblEmptyHint.Text = "Could not load products. Check database and App.config."
            btnOpenProducts.Visible = AppSession.IsAdmin()
        End Try
    End Sub

    Private Sub FilterProductCombo()
        If cmbSalesCategory Is Nothing OrElse cmbProductName Is Nothing Then
            Return
        End If

        Dim prev As String = cmbProductName.Text.Trim()
        cmbProductName.Items.Clear()

        Dim sel As SalesCategoryFilterItem = TryCast(cmbSalesCategory.SelectedItem, SalesCategoryFilterItem)
        If sel Is Nothing Then
            sel = New SalesCategoryFilterItem With {.Kind = SalesCategoryFilterItem.FilterKindEnum.AllCategories, .Display = "All categories"}
        End If

        Dim names As New List(Of String)(productCatalog.Keys)
        names.Sort(StringComparer.OrdinalIgnoreCase)

        For Each productName As String In names
            Dim entry As ProductCatalogEntry = productCatalog(productName)
            If CategoryFilterMatches(sel, entry.CategoryId) Then
                cmbProductName.Items.Add(productName)
            End If
        Next

        Dim restored As Boolean = False
        If prev.Length > 0 Then
            For Each it As Object In cmbProductName.Items
                Dim s As String = TryCast(it, String)
                If s IsNot Nothing AndAlso String.Equals(s, prev, StringComparison.OrdinalIgnoreCase) Then
                    cmbProductName.Text = s
                    restored = True
                    Exit For
                End If
            Next
        End If

        If Not restored Then
            cmbProductName.SelectedIndex = -1
            txtPrice.Clear()
        End If
    End Sub

    Private Shared Function CategoryFilterMatches(sel As SalesCategoryFilterItem, productCategoryId As Integer?) As Boolean
        Select Case sel.Kind
            Case SalesCategoryFilterItem.FilterKindEnum.AllCategories
                Return True
            Case SalesCategoryFilterItem.FilterKindEnum.Uncategorized
                Return Not productCategoryId.HasValue
            Case Else
                Return productCategoryId.HasValue AndAlso sel.CategoryId.HasValue AndAlso productCategoryId.Value = sel.CategoryId.Value
        End Select
    End Function

    Private Function GetCartSubtotalSum() As Decimal
        ' --- THE SAFETY SHIELD ---
        If dgvProducts Is Nothing Then Return 0D
        ' -------------------------

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
        If cartSum <= 0D Then
            Return 0D
        End If

        If radDiscountPercent.Checked Then
            Dim pct As Decimal = numDiscountPercent.Value
            Return Math.Round(cartSum * (pct / 100D), 2, MidpointRounding.AwayFromZero)
        End If

        Dim fixedPart As Decimal = numDiscountPercent.Value
        fixedPart = Math.Round(fixedPart, 2, MidpointRounding.AwayFromZero)
        Return Math.Min(fixedPart, cartSum)
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
        ' --- THE SAFETY SHIELD ---
        If lblTotal Is Nothing OrElse dgvProducts Is Nothing Then Return
        ' -------------------------

        Dim cartSum As Decimal = GetCartSubtotalSum()
        lblSubtotalValue.Text = FormatMoney(cartSum)

        If lblDiscountHeading IsNot Nothing Then
            If radDiscountPercent.Checked Then
                lblDiscountHeading.Text = "Discount rate (%)"
            Else
                lblDiscountHeading.Text = "Discount (" & AppSettings.Current.CurrencySymbol & ")"
            End If
        End If

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
