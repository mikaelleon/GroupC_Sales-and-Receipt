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
    Private Const DiscountPwdPercent As Decimal = 20D
    Private Const DiscountSeniorPercent As Decimal = 20D
    Private Const DiscountMembershipPercent As Decimal = 10D

    Private Enum PosDiscountType
        None = 0
        Pwd = 1
        Senior = 2
        Membership = 3
    End Enum

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
    Private lblCustomerDiscount As Label
    Private WithEvents btnDiscPwd As Button
    Private WithEvents btnDiscSenior As Button
    Private WithEvents btnDiscMembership As Button
    Private WithEvents btnTaxToggle As Button
    Private WithEvents numTaxPercent As NumericUpDown
    Private selectedPosDiscount As PosDiscountType = PosDiscountType.None
    Private taxToggleOn As Boolean
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

    ''' <summary>
    ''' True while <see cref="CreateControls"/> runs — event handlers must not touch the cart grid yet.
    ''' </summary>
    Private suppressSalesSummary As Boolean

    Private Sub SalesForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' 1. FORM SETUP (Full Screen & Responsive)
        SetupForm()

        Try
            UiTheme.ApplyStandardWindowChrome(Me)
        Catch
        End Try

        CreateControls()

        Try
            DatabaseInitializer.EnsureDatabase()
        Catch
        End Try

        LoadProducts()
        UpdateSummaryLabels()
    End Sub

    Private Sub SetupForm()
        Me.Text = AppBranding.WindowTitle("Point of Sale")
        UiTheme.ApplyMaximizedWorkspaceDefaults(Me)
    End Sub

    Private Sub CreateControls()
        suppressSalesSummary = True
        Me.SuspendLayout()
        Me.Controls.Clear()
        Me.BackColor = UiTheme.FormBackground

        ' -----------------------------------------------------------
        ' 1. INITIALIZE CONTROLS
        ' -----------------------------------------------------------
        cmbSalesCategory = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Font = New Font("Segoe UI", 11), .Dock = DockStyle.Fill}
        cmbProductName = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Font = New Font("Segoe UI", 11), .Dock = DockStyle.Fill}
        txtPrice = New TextBox() With {.ReadOnly = True, .BackColor = UiTheme.CardSurface, .TextAlign = HorizontalAlignment.Right, .Font = New Font("Segoe UI", 11), .Dock = DockStyle.Fill}
        numQuantity = New NumericUpDown() With {.Minimum = MinLineQty, .Maximum = MaxLineQty, .TextAlign = HorizontalAlignment.Right, .Font = New Font("Segoe UI", 11), .Dock = DockStyle.Fill}

        Try
            UiTheme.ApplyTableLayoutDropDown(cmbSalesCategory)
            UiTheme.ApplyTableLayoutDropDown(cmbProductName)
            UiTheme.ApplyTableLayoutSingleLineTextBox(txtPrice)
        Catch
        End Try

        btnAdd = New Button() With {.Text = "&Add to cart", .Size = New Size(120, 38), .Cursor = Cursors.Hand}
        btnRemove = New Button() With {.Text = "&Remove item", .Size = New Size(120, 38), .Cursor = Cursors.Hand}
        btnClear = New Button() With {.Text = "C&lear cart", .Size = New Size(100, 38), .Cursor = Cursors.Hand}
        btnOpenProducts = New Button() With {.Text = "Open &Products…", .Size = New Size(140, 36), .Cursor = Cursors.Hand}

        ' Dynamic Back Button logic directly injected
        Dim btnBack As New Button() With {.Text = "← Back to Menu", .Size = New Size(140, 36), .Cursor = Cursors.Hand}
        AddHandler btnBack.Click, Sub(s, ev) Me.Close()

        lblDiscountHeading = New Label() With {.Text = "Discount", .AutoSize = True, .ForeColor = UiTheme.TextSecondary, .Anchor = AnchorStyles.Right Or AnchorStyles.Top, .Margin = New Padding(0, 5, 5, 0)}
        lblCustomerDiscount = New Label() With {.Text = "Customer discount", .AutoSize = True, .ForeColor = UiTheme.TextSecondary, .Font = New Font("Segoe UI", 9, FontStyle.Bold), .Margin = New Padding(0, 0, 0, 4)}

        btnDiscPwd = CreatePosDiscountToggle("♿ PWD (20%)", PosDiscountType.Pwd)
        btnDiscSenior = CreatePosDiscountToggle("👴 Senior (20%)", PosDiscountType.Senior)
        btnDiscMembership = CreatePosDiscountToggle("🎫 Member (10%)", PosDiscountType.Membership)

        btnTaxToggle = New Button() With {
            .Text = "🧾 VAT / Tax %",
            .AutoSize = True,
            .MinimumSize = New Size(130, 32),
            .Font = New Font("Segoe UI", 10),
            .Cursor = Cursors.Hand,
            .FlatStyle = FlatStyle.Flat,
            .Margin = New Padding(0, 8, 0, 4)
        }
        btnTaxToggle.FlatAppearance.BorderColor = UiTheme.CardBorder
        numTaxPercent = New NumericUpDown() With {.DecimalPlaces = 2, .Minimum = 0D, .Maximum = 100D, .Increment = 0.5D, .Enabled = False, .Font = New Font("Segoe UI", 11), .Width = 100}

        txtAmountTendered = New TextBox() With {.TextAlign = HorizontalAlignment.Right, .Font = New Font("Segoe UI", 12, FontStyle.Bold), .Dock = DockStyle.Fill}

        lblSubtotalValue = New Label() With {.Text = AppSettings.Current.CurrencySymbol & "0.00", .AutoSize = True, .Font = New Font("Segoe UI", 11)}
        lblDiscountValue = New Label() With {.Text = AppSettings.Current.CurrencySymbol & "0.00", .AutoSize = True, .Font = New Font("Segoe UI", 11)}
        lblTaxValue = New Label() With {.Text = AppSettings.Current.CurrencySymbol & "0.00", .AutoSize = True, .Font = New Font("Segoe UI", 11)}
        lblChangeValue = New Label() With {.Text = AppSettings.Current.CurrencySymbol & "0.00", .AutoSize = True, .Font = New Font("Segoe UI", 12, FontStyle.Bold), .ForeColor = UiTheme.Success}
        lblTotal = New Label() With {.Text = AppSettings.Current.CurrencySymbol & "0.00", .AutoSize = True, .Font = New Font("Segoe UI", 18, FontStyle.Bold), .ForeColor = UiTheme.PrimaryAccent}

        btnFinalize = New Button() With {.Text = "FINALIZE SALE", .Size = New Size(220, 50), .Font = New Font("Segoe UI", 12, FontStyle.Bold), .Cursor = Cursors.Hand}

        lblSalesInputError = New Label() With {.AutoSize = True, .ForeColor = UiTheme.Danger, .Visible = False, .Padding = New Padding(0, 5, 0, 10)}
        lblEmptyHint = New Label() With {.Text = "No products in catalog. Open Manage Products.", .AutoSize = True, .ForeColor = UiTheme.TextSecondary, .Visible = False}

        dgvProducts = New DataGridView() With {
            .Dock = DockStyle.Fill,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None
        }

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

        Try
            UiTheme.ApplyPrimaryButton(btnAdd)
            UiTheme.ApplySecondaryAccentButton(btnRemove)
            UiTheme.ApplyWarningButton(btnClear)
            UiTheme.ApplySecondaryButton(btnOpenProducts)
            UiTheme.ApplySecondaryButton(btnBack)
            UiTheme.ApplySuccessButton(btnFinalize)
            UiTheme.ApplyDataGridViewChrome(dgvProducts)
        Catch
        End Try

        statusStrip = New StatusStrip()
        statusLabel = New ToolStripStatusLabel("Ready") With {.Spring = True, .TextAlign = ContentAlignment.MiddleLeft}
        statusStrip.Items.Add(statusLabel)
        Try
            UiTheme.ApplyStatusStripTheme(statusStrip)
        Catch
        End Try

        ' -----------------------------------------------------------
        ' 2. BUILD THE RESPONSIVE LAYOUT (Side-by-Side POS)
        ' -----------------------------------------------------------
        Dim rootTable As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .Margin = New Padding(0),
            .BackColor = UiTheme.FormBackground
        }
        rootTable.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 380.0F))
        rootTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

        ' --- LEFT SIDEBAR (Product Selection) ---
        Dim leftSidebar As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(25, 30, 25, 30)}

        Dim leftLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 4
        }
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))        ' Title
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))        ' Inputs
        leftLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F)) ' Dynamic Spacer
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))        ' Bottom Utilities

        Dim lblTitleLeft As New Label() With {
            .Text = "Point of Sale",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = UiTheme.PrimaryAccent,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 20)
        }
        leftLayout.Controls.Add(lblTitleLeft, 0, 0)

        Dim inputLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .ColumnCount = 1,
            .RowCount = 10,
            .Margin = New Padding(0)
        }
        Dim CreateLabel = Function(text As String) New Label() With {.Text = text, .AutoSize = True, .ForeColor = UiTheme.TextSecondary, .Margin = New Padding(0, 15, 0, 5)}

        inputLayout.Controls.Add(CreateLabel("Category Filter"), 0, 0)
        inputLayout.Controls.Add(cmbSalesCategory, 0, 1)
        inputLayout.Controls.Add(CreateLabel("Select Product"), 0, 2)
        inputLayout.Controls.Add(cmbProductName, 0, 3)
        inputLayout.Controls.Add(CreateLabel("Unit Price (" & AppSettings.Current.CurrencySymbol & ")"), 0, 4)
        inputLayout.Controls.Add(txtPrice, 0, 5)
        inputLayout.Controls.Add(CreateLabel("Quantity"), 0, 6)
        inputLayout.Controls.Add(numQuantity, 0, 7)

        Dim pnlAdd As New FlowLayoutPanel() With {.AutoSize = True, .Margin = New Padding(0, 20, 0, 0)}
        pnlAdd.Controls.Add(btnAdd)
        inputLayout.Controls.Add(pnlAdd, 0, 8)

        leftLayout.Controls.Add(inputLayout, 0, 1)

        Dim pnlUtility As New FlowLayoutPanel() With {
            .Dock = DockStyle.Bottom,
            .AutoSize = True,
            .FlowDirection = FlowDirection.TopDown
        }
        pnlUtility.Controls.Add(btnOpenProducts)
        btnBack.Margin = New Padding(0, 15, 0, 0)
        pnlUtility.Controls.Add(btnBack)

        leftLayout.Controls.Add(pnlUtility, 0, 3)
        leftSidebar.Controls.Add(leftLayout)


        ' --- RIGHT CARD (Cart & Checkout) ---
        Dim rightCard As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .Padding = New Padding(30, 30, 30, 20)
        }
        rightCard.RowStyles.Add(New RowStyle(SizeType.AutoSize))        ' Header
        rightCard.RowStyles.Add(New RowStyle(SizeType.Percent, 58.0F))  ' Cart grid (bounded — do not star 100%)
        rightCard.RowStyles.Add(New RowStyle(SizeType.Percent, 42.0F))  ' Checkout always reserved

        Dim headerPanel As New Panel() With {.AutoSize = True, .Dock = DockStyle.Top}
        Dim lblTitleRight As New Label() With {
            .Text = "Shopping Cart",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = UiTheme.PrimaryAccent,
            .AutoSize = True,
            .Dock = DockStyle.Left
        }

        Dim pnlCartActions As New FlowLayoutPanel() With {
            .AutoSize = True,
            .Dock = DockStyle.Right,
            .FlowDirection = FlowDirection.RightToLeft, ' Right aligns the buttons perfectly
            .WrapContents = False
        }
        pnlCartActions.Controls.Add(btnClear)
        pnlCartActions.Controls.Add(btnRemove)

        lblSalesInputError.Dock = DockStyle.Bottom

        headerPanel.Controls.Add(pnlCartActions)
        headerPanel.Controls.Add(lblTitleRight)
        headerPanel.Controls.Add(lblSalesInputError)
        rightCard.Controls.Add(headerPanel, 0, 0)

        Dim gridContainer As New Panel() With {.Dock = DockStyle.Fill, .Margin = New Padding(0, 12, 0, 8), .MinimumSize = New Size(0, 120)}
        lblEmptyHint.Dock = DockStyle.Top
        gridContainer.Controls.Add(lblEmptyHint)
        gridContainer.Controls.Add(dgvProducts)
        rightCard.Controls.Add(gridContainer, 0, 1)

        ' --- CHECKOUT PANEL (Bottom Right Dashboard) ---
        Dim checkoutPanel As New TableLayoutPanel() With {
            .ColumnCount = 3,
            .RowCount = 1,
            .Dock = DockStyle.Fill,
            .BackColor = UiTheme.CardSurface,
            .Margin = New Padding(0),
            .MinimumSize = New Size(0, 200)
        }
        checkoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 35.0F)) ' Discount & Tax Settings
        checkoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 35.0F)) ' Subtotal, Tendered, Change Details
        checkoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 30.0F)) ' Finalize Button

        ' 1. Settings Panel
        Dim settingsLayout As New TableLayoutPanel() With {.AutoSize = True, .Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 4, .Padding = New Padding(15)}
        settingsLayout.Controls.Add(lblCustomerDiscount, 0, 0)

        Dim discountToggleRow As New FlowLayoutPanel() With {
            .AutoSize = True,
            .Dock = DockStyle.Fill,
            .WrapContents = True,
            .Margin = New Padding(0, 0, 0, 6)
        }
        discountToggleRow.Controls.Add(btnDiscPwd)
        discountToggleRow.Controls.Add(btnDiscSenior)
        discountToggleRow.Controls.Add(btnDiscMembership)
        settingsLayout.Controls.Add(discountToggleRow, 0, 1)

        settingsLayout.Controls.Add(btnTaxToggle, 0, 2)
        settingsLayout.Controls.Add(numTaxPercent, 0, 3)

        RefreshPosDiscountToggleUi()
        RefreshTaxToggleUi()

        ' 2. Details Panel
        Dim detailsLayout As New TableLayoutPanel() With {.AutoSize = True, .Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 5, .Padding = New Padding(15)}
        Dim CreateSummaryLabel = Function(text As String) New Label() With {.Text = text, .AutoSize = True, .ForeColor = UiTheme.TextSecondary, .Anchor = AnchorStyles.Right Or AnchorStyles.Top, .Margin = New Padding(0, 5, 5, 0)}

        detailsLayout.Controls.Add(CreateSummaryLabel("Subtotal:"), 0, 0)
        detailsLayout.Controls.Add(lblSubtotalValue, 1, 0)
        detailsLayout.Controls.Add(lblDiscountHeading, 0, 1)
        detailsLayout.Controls.Add(lblDiscountValue, 1, 1)
        detailsLayout.Controls.Add(CreateSummaryLabel("Tax:"), 0, 2)
        detailsLayout.Controls.Add(lblTaxValue, 1, 2)

        detailsLayout.Controls.Add(CreateSummaryLabel("Tendered:"), 0, 3)
        detailsLayout.Controls.Add(txtAmountTendered, 1, 3)
        detailsLayout.Controls.Add(CreateSummaryLabel("Change:"), 0, 4)
        detailsLayout.Controls.Add(lblChangeValue, 1, 4)

        ' 3. Finalize Panel
        Dim finalizeLayout As New TableLayoutPanel() With {.AutoSize = True, .Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 3, .Padding = New Padding(15)}
        finalizeLayout.Controls.Add(New Label() With {.Text = "AMOUNT DUE", .AutoSize = True, .ForeColor = UiTheme.TextSecondary, .Font = New Font("Segoe UI", 10, FontStyle.Bold)}, 0, 0)
        finalizeLayout.Controls.Add(lblTotal, 0, 1)
        finalizeLayout.Controls.Add(btnFinalize, 0, 2)

        checkoutPanel.Controls.Add(settingsLayout, 0, 0)
        checkoutPanel.Controls.Add(detailsLayout, 1, 0)
        checkoutPanel.Controls.Add(finalizeLayout, 2, 0)

        Dim checkoutCard As Panel = UiTheme.CreateCardPanel(New Padding(12))
        checkoutCard.Dock = DockStyle.Fill
        checkoutCard.Margin = New Padding(0, 4, 0, 0)
        checkoutCard.MinimumSize = New Size(0, 210)
        UiTheme.PopulateCardContent(checkoutCard, checkoutPanel)

        Dim checkoutHost As New Panel() With {.Dock = DockStyle.Fill, .AutoScroll = True, .Padding = Padding.Empty}
        checkoutHost.Controls.Add(checkoutCard)

        rightCard.Controls.Add(checkoutHost, 0, 2)

        ' 3. ASSEMBLE ALL
        rootTable.Controls.Add(leftSidebar, 0, 0)
        rootTable.Controls.Add(rightCard, 1, 0)

        Me.Controls.Add(rootTable)
        Me.Controls.Add(statusStrip)

        suppressSalesSummary = False
        Me.ResumeLayout(True)
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
        If lblSalesInputError Is Nothing Then
            Return
        End If

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

    Private Function CreatePosDiscountToggle(caption As String, discountType As PosDiscountType) As Button
        Dim btn As New Button() With {
            .Text = caption,
            .Tag = discountType,
            .AutoSize = True,
            .MinimumSize = New Size(118, 32),
            .Font = New Font("Segoe UI", 9.5F),
            .Cursor = Cursors.Hand,
            .FlatStyle = FlatStyle.Flat,
            .Margin = New Padding(0, 0, 6, 6)
        }
        btn.FlatAppearance.BorderColor = UiTheme.CardBorder
        Return btn
    End Function

    Private Sub btnTaxToggle_Click(sender As Object, e As EventArgs) Handles btnTaxToggle.Click
        If suppressSalesSummary Then
            Return
        End If

        taxToggleOn = Not taxToggleOn
        RefreshTaxToggleUi()
        UpdateSummaryLabels()
    End Sub

    Private Sub numTaxPercent_ValueChanged(sender As Object, e As EventArgs) Handles numTaxPercent.ValueChanged
        UpdateSummaryLabels()
    End Sub

    Private Sub PosDiscountToggle_Click(sender As Object, e As EventArgs) Handles btnDiscPwd.Click, btnDiscSenior.Click, btnDiscMembership.Click
        If suppressSalesSummary Then
            Return
        End If

        Dim clicked As Button = TryCast(sender, Button)
        If clicked Is Nothing OrElse clicked.Tag Is Nothing Then
            Return
        End If

        Dim clickedType As PosDiscountType = CType(clicked.Tag, PosDiscountType)
        If selectedPosDiscount = clickedType Then
            selectedPosDiscount = PosDiscountType.None
        Else
            selectedPosDiscount = clickedType
        End If

        RefreshPosDiscountToggleUi()
        UpdateSummaryLabels()
    End Sub

    Private Sub RefreshPosDiscountToggleUi()
        ApplyPosDiscountToggleState(btnDiscPwd, PosDiscountType.Pwd)
        ApplyPosDiscountToggleState(btnDiscSenior, PosDiscountType.Senior)
        ApplyPosDiscountToggleState(btnDiscMembership, PosDiscountType.Membership)
    End Sub

    Private Sub ApplyPosDiscountToggleState(btn As Button, discountType As PosDiscountType)
        If btn Is Nothing Then
            Return
        End If

        Dim isSelected As Boolean = selectedPosDiscount = discountType
        Dim othersLocked As Boolean = selectedPosDiscount <> PosDiscountType.None AndAlso Not isSelected

        btn.Enabled = Not othersLocked
        btn.BackColor = If(isSelected, UiTheme.PrimaryAccent, UiTheme.CardSurface)
        btn.ForeColor = If(isSelected, UiTheme.TextOnAccent, UiTheme.TextPrimary)
        btn.FlatAppearance.BorderColor = If(isSelected, UiTheme.PrimaryAccent, UiTheme.CardBorder)
    End Sub

    Private Sub RefreshTaxToggleUi()
        If btnTaxToggle Is Nothing OrElse numTaxPercent Is Nothing Then
            Return
        End If

        numTaxPercent.Enabled = taxToggleOn
        btnTaxToggle.BackColor = If(taxToggleOn, UiTheme.PrimaryAccent, UiTheme.CardSurface)
        btnTaxToggle.ForeColor = If(taxToggleOn, UiTheme.TextOnAccent, UiTheme.TextPrimary)
        btnTaxToggle.FlatAppearance.BorderColor = If(taxToggleOn, UiTheme.PrimaryAccent, UiTheme.CardBorder)
    End Sub

    Private Sub ResetPosCheckoutOptions()
        selectedPosDiscount = PosDiscountType.None
        taxToggleOn = False
        If numTaxPercent IsNot Nothing Then
            numTaxPercent.Value = 0D
        End If

        RefreshPosDiscountToggleUi()
        RefreshTaxToggleUi()
    End Sub

    Private Function GetSelectedDiscountPercent() As Decimal
        Select Case selectedPosDiscount
            Case PosDiscountType.Pwd
                Return DiscountPwdPercent
            Case PosDiscountType.Senior
                Return DiscountSeniorPercent
            Case PosDiscountType.Membership
                Return DiscountMembershipPercent
            Case Else
                Return 0D
        End Select
    End Function

    Private Function GetSelectedDiscountLabel() As String
        Select Case selectedPosDiscount
            Case PosDiscountType.Pwd
                Return "PWD " & DiscountPwdPercent.ToString("N0", CultureInfo.CurrentCulture) & "%"
            Case PosDiscountType.Senior
                Return "Senior " & DiscountSeniorPercent.ToString("N0", CultureInfo.CurrentCulture) & "%"
            Case PosDiscountType.Membership
                Return "Member " & DiscountMembershipPercent.ToString("N0", CultureInfo.CurrentCulture) & "%"
            Case Else
                Return String.Empty
        End Select
    End Function

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
            If Not IsSalesCartGridReady() Then
                Return
            End If

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
        If Not IsSalesCartGridReady() Then
            Return
        End If

        If dgvProducts.SelectedRows.Count = 0 Then
            MessageBox.Show("Select a row to remove.", "Cart", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        dgvProducts.Rows.Remove(dgvProducts.SelectedRows(0))
        ReindexRows()
        UpdateSummaryLabels()
    End Sub

    Private Sub btnClear_Click(sender As Object, e As EventArgs) Handles btnClear.Click
        If Not IsSalesCartGridReady() Then
            Return
        End If

        Dim hasLines As Boolean = dgvProducts.Rows.Count > 0
        Dim hasTenderOrDiscount As Boolean =
            txtAmountTendered.Text.Trim().Length > 0 OrElse
            selectedPosDiscount <> PosDiscountType.None OrElse
            taxToggleOn OrElse
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
        ResetPosCheckoutOptions()
        cmbProductName.SelectedIndex = -1
        txtPrice.Clear()
        numQuantity.Value = MinLineQty
        cmbProductName.Focus()
        ClearSalesInputError()
        UpdateSummaryLabels()
        ShowStatus("Cart cleared.", False)
    End Sub

    Private Sub dgvProducts_CellValidating(sender As Object, e As DataGridViewCellValidatingEventArgs) Handles dgvProducts.CellValidating
        If Not IsSalesCartGridReady() Then
            Return
        End If

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
        If Not IsSalesCartGridReady() Then
            Return
        End If

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
        If Not IsSalesCartGridReady() Then
            Return
        End If

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
            .CashierName = AppSession.GetReceiptOperatorName(),
            .Lines = New List(Of ReceiptLineRow)(),
            .DiscountPercent = GetSelectedDiscountPercent(),
            .DiscountIsPercent = True,
            .DiscountLabel = GetSelectedDiscountLabel(),
            .TaxApplied = taxToggleOn,
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
        If Not String.IsNullOrWhiteSpace(snapshot.CashierName) Then
            receipt.AppendLine("Cashier: " & snapshot.CashierName.Trim())
        End If
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
            If Not String.IsNullOrWhiteSpace(snapshot.DiscountLabel) Then
                discLabel = "Discount (" & snapshot.DiscountLabel & "):"
            ElseIf snapshot.DiscountIsPercent Then
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
        If Not IsSalesCartGridReady() Then
            Return False
        End If

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
            ResetPosCheckoutOptions()
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

    Private Function IsSalesCartGridReady() As Boolean
        Return dgvProducts IsNot Nothing AndAlso Not dgvProducts.IsDisposed
    End Function

    Private Function GetCartSubtotalSum() As Decimal
        ' --- THE SAFETY SHIELD ---
        If dgvProducts Is Nothing Then Return 0D
        ' -------------------------
        If Not IsSalesCartGridReady() Then
            Return 0D
        End If

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

        Dim pct As Decimal = GetSelectedDiscountPercent()
        If pct <= 0D Then
            Return 0D
        End If

        Return Math.Round(cartSum * (pct / 100D), 2, MidpointRounding.AwayFromZero)
    End Function

    Private Function GetAmountBeforeTax() As Decimal
        Return Math.Max(0D, GetCartSubtotalSum() - GetDiscountAmount())
    End Function

    Private Function GetTaxAmount() As Decimal
        If numTaxPercent Is Nothing Then
            Return 0D
        End If

        If Not taxToggleOn Then
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
        If txtAmountTendered Is Nothing Then
            Return 0D
        End If

        Dim grandTotal As Decimal = GetGrandTotal()
        Dim tendered As Decimal
        If Not Decimal.TryParse(txtAmountTendered.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, tendered) Then
            Return 0D
        End If

        Return Math.Max(0D, tendered - grandTotal)
    End Function

    Private Sub UpdateSummaryLabels()
        If suppressSalesSummary OrElse Not IsSalesCartGridReady() Then
            Return
        End If

        If lblSubtotalValue Is Nothing OrElse lblDiscountValue Is Nothing OrElse lblTaxValue Is Nothing OrElse lblTotal Is Nothing OrElse lblChangeValue Is Nothing OrElse dgvProducts Is Nothing Then
            Return
        End If

        Dim cartSum As Decimal = GetCartSubtotalSum()
        lblSubtotalValue.Text = FormatMoney(cartSum)

        If lblDiscountHeading IsNot Nothing Then
            Dim label As String = GetSelectedDiscountLabel()
            lblDiscountHeading.Text = If(label.Length > 0, "Discount (" & label & "):", "Discount:")
        End If

        lblDiscountValue.Text = FormatMoney(GetDiscountAmount())
        lblTaxValue.Text = FormatMoney(GetTaxAmount())
        lblTotal.Text = FormatMoney(GetGrandTotal())
        lblChangeValue.Text = FormatMoney(GetChangeDue())
    End Sub

    Private Sub ReindexRows()
        If Not IsSalesCartGridReady() Then
            Return
        End If

        For i As Integer = 0 To dgvProducts.Rows.Count - 1
            dgvProducts.Rows(i).Cells("Index").Value = i + 1
        Next
    End Sub

End Class
