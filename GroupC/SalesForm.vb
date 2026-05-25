Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
Imports System.Text
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient

Public Class SalesForm

    Private Class ProductCatalogEntry
        Public Property ProductId As Integer
        Public Property UnitPrice As Decimal
        Public Property CategoryId As Integer?
        Public Property StockQuantity As Integer
        Public Property ImagePath As String
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
    Private Const CheckoutSummaryLabelWidth As Single = 118.0F
    Private Const CheckoutSummaryRowHeight As Integer = 32
    Private Const CheckoutSummaryTenderedRowHeight As Integer = 40
    Private Const CheckoutDiscountColumnWidth As Single = 210.0F
    Private Const CheckoutFinalizeColumnWidth As Single = 196.0F
    Private Const TenderedFieldHeight As Integer = 40
    Private Const SalesInputShellHeight As Integer = 42
    Private Const DiscountPwdPercent As Decimal = 20D
    Private Const DiscountSeniorPercent As Decimal = 20D
    Private Const DiscountMembershipPercent As Decimal = 10D
    Private Const ProductCardWidth As Integer = 156
    Private Const ProductCardHeight As Integer = 218
    Private Const ProductCardImageHeight As Integer = 96
    Private Const CartRemoveColumnName As String = "Remove"
    Private Const CartColIndexWidth As Integer = 40
    Private Const CartColPriceWidth As Integer = 96
    Private Const CartColQtyWidth As Integer = 52
    Private Const CartColSubtotalWidth As Integer = 108
    Private Const CartColRemoveWidth As Integer = 84
    Private Const CartColProductMinWidth As Integer = 100

    Private Enum PosDiscountType
        None = 0
        Pwd = 1
        Senior = 2
        Membership = 3
    End Enum

    Private WithEvents cmbSalesCategory As ComboBox
    Private productCardHost As FlowLayoutPanel
    Private productCardScrollPanel As Panel
    Private lblSelectedProduct As Label
    Private lblNoProductCards As Label
    Private selectedProductName As String
    Private selectedProductCard As Panel
    Private ReadOnly productCardImages As New List(Of Image)()
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
    Private lblStockOnHand As Label

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
        cmbSalesCategory = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = UiTheme.FontBody,
            .Dock = DockStyle.Fill
        }
        productCardScrollPanel = New Panel() With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .BackColor = UiTheme.FormBackground
        }
        productCardHost = New FlowLayoutPanel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = True,
            .Dock = DockStyle.Top,
            .Padding = New Padding(UiTheme.SpaceXs),
            .BackColor = UiTheme.FormBackground
        }
        productCardScrollPanel.Controls.Add(productCardHost)
        AddHandler productCardScrollPanel.Resize, AddressOf ProductCardScrollPanel_Resize

        lblNoProductCards = New Label() With {
            .Text = "No products available for this category.",
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter,
            .ForeColor = UiTheme.TextSecondary,
            .Font = UiTheme.FontBody,
            .Visible = False
        }
        lblSelectedProduct = New Label() With {
            .AutoSize = True,
            .ForeColor = UiTheme.PrimaryAccent,
            .Font = UiTheme.FontBody,
            .Text = "Selected: none",
            .Margin = New Padding(0, 0, 0, UiTheme.SpaceSm)
        }
        numQuantity = New NumericUpDown() With {
            .Minimum = MinLineQty,
            .Maximum = MaxLineQty,
            .TextAlign = HorizontalAlignment.Right,
            .Font = UiTheme.FontBody,
            .Dock = DockStyle.Fill
        }

        Try
            UiTheme.ApplyTableLayoutDropDown(cmbSalesCategory)
        Catch
        End Try

        btnAdd = New Button() With {
            .Text = "&Add to cart",
            .Height = UiTheme.ButtonHeightMd,
            .AutoSize = True,
            .Cursor = Cursors.Hand
        }
        btnRemove = New Button() With {
            .Text = "&Remove item",
            .Height = UiTheme.ButtonHeightMd,
            .AutoSize = True,
            .Cursor = Cursors.Hand
        }
        btnClear = New Button() With {
            .Text = "C&lear cart",
            .Height = UiTheme.ButtonHeightMd,
            .AutoSize = True,
            .Cursor = Cursors.Hand
        }
        btnOpenProducts = New Button() With {
            .Text = "Open &Products…",
            .Height = UiTheme.ButtonHeightMd,
            .AutoSize = True,
            .Cursor = Cursors.Hand
        }

        ' Dynamic Back Button logic directly injected
        Dim btnBack As New Button() With {
            .Text = "← Back to Menu",
            .Height = UiTheme.ButtonHeightMd,
            .AutoSize = True,
            .Cursor = Cursors.Hand
        }
        AddHandler btnBack.Click, Sub(s, ev) Me.Close()

        lblDiscountHeading = CreateCheckoutSummaryCaption("Discount:")
        lblCustomerDiscount = New Label() With {
            .Text = "Customer discount",
            .AutoSize = True,
            .ForeColor = UiTheme.TextSecondary,
            .Font = UiTheme.FontBodySmall,
            .Margin = New Padding(0, 0, 0, UiTheme.SpaceXs)
        }

        btnDiscPwd = CreatePosDiscountToggle("PWD (20%)", PosDiscountType.Pwd)
        btnDiscSenior = CreatePosDiscountToggle("Senior (20%)", PosDiscountType.Senior)
        btnDiscMembership = CreatePosDiscountToggle("Member (10%)", PosDiscountType.Membership)

        btnTaxToggle = New Button() With {
            .Text = "VAT / Tax",
            .AutoSize = False,
            .Height = UiTheme.ButtonHeightSm,
            .Dock = DockStyle.Fill,
            .Font = UiTheme.FontBody,
            .Cursor = Cursors.Hand,
            .FlatStyle = FlatStyle.Flat,
            .Margin = Padding.Empty
        }
        btnTaxToggle.FlatAppearance.BorderColor = UiTheme.CardBorder
        numTaxPercent = New NumericUpDown() With {
            .DecimalPlaces = 2,
            .Minimum = 0D,
            .Maximum = 100D,
            .Increment = 0.5D,
            .Enabled = False,
            .Font = UiTheme.FontBody,
            .Dock = DockStyle.Fill,
            .TextAlign = HorizontalAlignment.Right
        }

        txtAmountTendered = New TextBox() With {
            .TextAlign = HorizontalAlignment.Right,
            .Font = UiTheme.FontHeading3,
            .BorderStyle = BorderStyle.None
        }
        Try
            UiTheme.ApplyTableLayoutSingleLineTextBox(txtAmountTendered)
        Catch
        End Try

        lblSubtotalValue = CreateCheckoutSummaryValueLabel()
        lblDiscountValue = CreateCheckoutSummaryValueLabel()
        lblTaxValue = CreateCheckoutSummaryValueLabel()
        lblChangeValue = CreateCheckoutSummaryValueLabel(bold:=True, successColor:=True)
        lblTotal = New Label() With {
            .Text = FormatMoney(0D),
            .Dock = DockStyle.Top,
            .TextAlign = ContentAlignment.MiddleCenter,
            .AutoSize = False,
            .Height = 52,
            .Font = UiTheme.FontHeading1,
            .ForeColor = UiTheme.PrimaryAccent,
            .Margin = New Padding(0, UiTheme.SpaceXs, 0, UiTheme.SpaceSm)
        }

        btnFinalize = New Button() With {
            .Text = "FINALIZE SALE",
            .Dock = DockStyle.Top,
            .AutoSize = False,
            .Height = UiTheme.ButtonHeightLg,
            .Font = UiTheme.FontHeading3,
            .Cursor = Cursors.Hand,
            .Margin = Padding.Empty
        }

        lblSalesInputError = New Label() With {.AutoSize = True, .ForeColor = UiTheme.Danger, .Visible = False, .Padding = New Padding(0, 5, 0, 10)}
        lblStockOnHand = New Label() With {
            .AutoSize = True,
            .ForeColor = UiTheme.TextSecondary,
            .Font = UiTheme.FontBodySmall,
            .Margin = New Padding(0, 0, 0, UiTheme.SpaceSm),
            .Text = "Available: —"
        }
        lblEmptyHint = New Label() With {.Text = "No products in catalog. Open Manage Products.", .AutoSize = True, .ForeColor = UiTheme.TextSecondary, .Visible = False}

        dgvProducts = New DataGridView() With {
            .Dock = DockStyle.Fill,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .AllowUserToResizeColumns = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            .AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .ScrollBars = ScrollBars.Vertical
        }

        dgvProducts.Columns.Add("Index", "#")
        dgvProducts.Columns.Add("ProductName", "Product")
        dgvProducts.Columns.Add("Price", "Price")
        dgvProducts.Columns.Add("Quantity", "Qty")
        dgvProducts.Columns.Add("Subtotal", "Subtotal")
        dgvProducts.Columns("Index").ReadOnly = True
        dgvProducts.Columns("ProductName").ReadOnly = True
        dgvProducts.Columns("Price").ReadOnly = True
        dgvProducts.Columns("Quantity").ReadOnly = False
        dgvProducts.Columns("Subtotal").ReadOnly = True

        Dim removeCol As New DataGridViewButtonColumn() With {
            .Name = CartRemoveColumnName,
            .HeaderText = "",
            .Text = "Remove",
            .UseColumnTextForButtonValue = True,
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            .Width = CartColRemoveWidth,
            .MinimumWidth = CartColRemoveWidth,
            .ReadOnly = True,
            .FlatStyle = FlatStyle.Flat,
            .DisplayIndex = 5
        }
        dgvProducts.Columns.Add(removeCol)

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

        ConfigureSalesCartGrid()

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
        rootTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 58.0F))
        rootTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 42.0F))

        ' --- LEFT: Product catalog cards ---
        Dim leftSidebar As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = UiTheme.CardSurface,
            .Padding = New Padding(UiTheme.SpaceXl, UiTheme.Space2xl, UiTheme.SpaceXl, UiTheme.Space2xl)
        }

        Dim leftLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 5
        }
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        leftLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim lblTitleLeft As New Label() With {
            .Text = "Point of Sale",
            .Font = UiTheme.FontHeading2,
            .ForeColor = UiTheme.PrimaryAccent,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, UiTheme.SpaceLg)
        }
        leftLayout.Controls.Add(lblTitleLeft, 0, 0)

        Dim filterPanel As New TableLayoutPanel() With {
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .ColumnCount = 1,
            .RowCount = 3
        }
        Dim lblCategoryFilter As New Label() With {
            .Text = "Category Filter",
            .AutoSize = True,
            .Font = UiTheme.FontBody,
            .ForeColor = UiTheme.TextSecondary,
            .Margin = New Padding(0, 0, 0, UiTheme.SpaceSm)
        }
        filterPanel.Controls.Add(lblCategoryFilter, 0, 0)
        filterPanel.Controls.Add(cmbSalesCategory, 0, 1)
        lblSalesInputError.Dock = DockStyle.Top
        filterPanel.Controls.Add(lblSalesInputError, 0, 2)
        leftLayout.Controls.Add(filterPanel, 0, 1)

        Dim catalogHost As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = UiTheme.FormBackground,
            .Padding = New Padding(0, UiTheme.SpaceSm, 0, UiTheme.SpaceSm)
        }
        Dim catalogCard As Panel = UiTheme.CreateCardPanel(New Padding(UiTheme.SpaceSm))
        catalogCard.Dock = DockStyle.Fill
        Dim catalogCardHost As Panel = UiTheme.GetCardContentHost(catalogCard)
        catalogCardHost.Controls.Add(lblNoProductCards)
        catalogCardHost.Controls.Add(productCardScrollPanel)
        catalogHost.Controls.Add(catalogCard)
        leftLayout.Controls.Add(catalogHost, 0, 2)

        Dim checkoutBar As New TableLayoutPanel() With {
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .ColumnCount = 1,
            .RowCount = 5,
            .Margin = New Padding(0, UiTheme.SpaceLg, 0, 0)
        }
        checkoutBar.Controls.Add(lblSelectedProduct, 0, 0)
        checkoutBar.Controls.Add(lblStockOnHand, 0, 1)
        Dim lblQty As New Label() With {
            .Text = "Quantity",
            .AutoSize = True,
            .Font = UiTheme.FontBody,
            .ForeColor = UiTheme.TextSecondary,
            .Margin = New Padding(0, UiTheme.SpaceMd, 0, UiTheme.SpaceSm)
        }
        checkoutBar.Controls.Add(lblQty, 0, 2)
        checkoutBar.Controls.Add(numQuantity, 0, 3)
        Dim pnlAdd As New FlowLayoutPanel() With {
            .AutoSize = True,
            .Margin = New Padding(0, UiTheme.SpaceMd, 0, 0)
        }
        pnlAdd.Controls.Add(btnAdd)
        checkoutBar.Controls.Add(pnlAdd, 0, 4)
        leftLayout.Controls.Add(checkoutBar, 0, 3)

        Dim pnlUtility As New FlowLayoutPanel() With {
            .Dock = DockStyle.Bottom,
            .AutoSize = True,
            .FlowDirection = FlowDirection.TopDown,
            .Margin = New Padding(0, UiTheme.SpaceLg, 0, 0)
        }
        pnlUtility.Controls.Add(btnOpenProducts)
        btnBack.Margin = New Padding(0, UiTheme.SpaceLg, 0, 0)
        pnlUtility.Controls.Add(btnBack)
        leftLayout.Controls.Add(pnlUtility, 0, 4)
        leftSidebar.Controls.Add(leftLayout)


        ' --- RIGHT CARD (Cart & Checkout) ---
        Dim rightCard As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .Padding = New Padding(UiTheme.Space2xl, UiTheme.Space2xl, UiTheme.Space2xl, UiTheme.SpaceLg)
        }
        rightCard.RowStyles.Add(New RowStyle(SizeType.AutoSize))        ' Header
        rightCard.RowStyles.Add(New RowStyle(SizeType.Percent, 62.0F))  ' Cart grid
        rightCard.RowStyles.Add(New RowStyle(SizeType.Percent, 38.0F))  ' Checkout

        Dim headerPanel As New Panel() With {.AutoSize = True, .Dock = DockStyle.Top}
        Dim lblTitleRight As New Label() With {
            .Text = "Shopping Cart",
            .Font = UiTheme.FontHeading2,
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

        headerPanel.Controls.Add(pnlCartActions)
        headerPanel.Controls.Add(lblTitleRight)
        rightCard.Controls.Add(headerPanel, 0, 0)

        Dim gridContainer As New Panel() With {
            .Dock = DockStyle.Fill,
            .Margin = New Padding(0, UiTheme.SpaceMd, 0, UiTheme.SpaceSm),
            .MinimumSize = New Size(0, 180)
        }
        lblEmptyHint.Dock = DockStyle.Top
        lblEmptyHint.Padding = New Padding(4, 0, 0, 6)
        Dim gridCard As Panel = UiTheme.CreateCardPanel(Padding.Empty)
        gridCard.Dock = DockStyle.Fill
        Dim gridHost As Panel = UiTheme.GetCardContentHost(gridCard)
        gridHost.Controls.Add(dgvProducts)
        gridContainer.Controls.Add(gridCard)
        gridContainer.Controls.Add(lblEmptyHint)
        rightCard.Controls.Add(gridContainer, 0, 1)

        ' --- CHECKOUT PANEL (Bottom Right Dashboard) ---
        Dim checkoutPanel As New TableLayoutPanel() With {
            .ColumnCount = 5,
            .RowCount = 1,
            .Dock = DockStyle.Fill,
            .BackColor = UiTheme.CardSurface,
            .Margin = Padding.Empty,
            .MinimumSize = New Size(0, 248)
        }
        checkoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, CheckoutDiscountColumnWidth))
        checkoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 1.0F))
        checkoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        checkoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 1.0F))
        checkoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, CheckoutFinalizeColumnWidth))

        Dim settingsLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .Padding = New Padding(UiTheme.SpaceMd, UiTheme.SpaceMd, UiTheme.SpaceSm, UiTheme.SpaceMd),
            .BackColor = Color.Transparent
        }
        settingsLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        settingsLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        settingsLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        settingsLayout.Controls.Add(lblCustomerDiscount, 0, 0)

        Dim discountLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .ColumnCount = 1,
            .RowCount = 3,
            .Margin = Padding.Empty
        }
        discountLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, UiTheme.ButtonHeightSm + UiTheme.SpaceXs))
        discountLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, UiTheme.ButtonHeightSm + UiTheme.SpaceXs))
        discountLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, UiTheme.ButtonHeightSm))
        discountLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        ConfigureCheckoutDiscountButton(btnDiscPwd)
        ConfigureCheckoutDiscountButton(btnDiscSenior)
        ConfigureCheckoutDiscountButton(btnDiscMembership)
        discountLayout.Controls.Add(btnDiscPwd, 0, 0)
        discountLayout.Controls.Add(btnDiscSenior, 0, 1)
        discountLayout.Controls.Add(btnDiscMembership, 0, 2)
        settingsLayout.Controls.Add(discountLayout, 0, 1)

        Dim taxLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .ColumnCount = 1,
            .RowCount = 2,
            .Margin = New Padding(0, UiTheme.SpaceSm, 0, 0)
        }
        taxLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, UiTheme.ButtonHeightSm))
        taxLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, UiTheme.InputHeight))
        taxLayout.Controls.Add(btnTaxToggle, 0, 0)
        taxLayout.Controls.Add(numTaxPercent, 0, 1)
        settingsLayout.Controls.Add(taxLayout, 0, 2)

        RefreshPosDiscountToggleUi()
        RefreshTaxToggleUi()

        Dim detailsLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 5,
            .Padding = New Padding(UiTheme.SpaceMd, UiTheme.SpaceMd, UiTheme.SpaceMd, UiTheme.SpaceMd),
            .BackColor = Color.Transparent
        }
        detailsLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, CheckoutSummaryLabelWidth))
        detailsLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        detailsLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, CheckoutSummaryRowHeight))
        detailsLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, CheckoutSummaryRowHeight))
        detailsLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, CheckoutSummaryRowHeight))
        detailsLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, CheckoutSummaryTenderedRowHeight))
        detailsLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, CheckoutSummaryRowHeight))

        detailsLayout.Controls.Add(CreateCheckoutSummaryCaption("Subtotal:"), 0, 0)
        detailsLayout.Controls.Add(lblSubtotalValue, 1, 0)
        detailsLayout.Controls.Add(lblDiscountHeading, 0, 1)
        detailsLayout.Controls.Add(lblDiscountValue, 1, 1)
        detailsLayout.Controls.Add(CreateCheckoutSummaryCaption("Tax:"), 0, 2)
        detailsLayout.Controls.Add(lblTaxValue, 1, 2)
        detailsLayout.Controls.Add(CreateCheckoutSummaryCaption("Tendered:"), 0, 3)
        detailsLayout.Controls.Add(CreateTenderedInputShell(), 1, 3)
        detailsLayout.Controls.Add(CreateCheckoutSummaryCaption("Change:"), 0, 4)
        detailsLayout.Controls.Add(lblChangeValue, 1, 4)

        Dim finalizeLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .Padding = New Padding(UiTheme.SpaceMd, UiTheme.SpaceMd, UiTheme.SpaceMd, UiTheme.SpaceMd),
            .BackColor = Color.Transparent
        }
        finalizeLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        finalizeLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        finalizeLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Dim lblAmountDueTitle As New Label() With {
            .Text = "AMOUNT DUE",
            .Dock = DockStyle.Top,
            .AutoSize = False,
            .Height = 22,
            .TextAlign = ContentAlignment.MiddleCenter,
            .ForeColor = UiTheme.TextSecondary,
            .Font = UiTheme.FontBodySmall,
            .Margin = Padding.Empty
        }
        finalizeLayout.Controls.Add(lblAmountDueTitle, 0, 0)
        finalizeLayout.Controls.Add(lblTotal, 0, 1)
        finalizeLayout.Controls.Add(btnFinalize, 0, 2)

        checkoutPanel.Controls.Add(settingsLayout, 0, 0)
        checkoutPanel.Controls.Add(CreateCheckoutColumnSeparator(), 1, 0)
        checkoutPanel.Controls.Add(detailsLayout, 2, 0)
        checkoutPanel.Controls.Add(CreateCheckoutColumnSeparator(), 3, 0)
        checkoutPanel.Controls.Add(finalizeLayout, 4, 0)

        Dim checkoutCard As Panel = UiTheme.CreateCardPanel(New Padding(UiTheme.SpaceSm))
        checkoutCard.Dock = DockStyle.Fill
        checkoutCard.Margin = New Padding(0, UiTheme.SpaceXs, 0, 0)
        checkoutCard.MinimumSize = New Size(0, 260)
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

    Private Sub ConfigureSalesCartGrid()
        If dgvProducts Is Nothing Then
            Return
        End If

        dgvProducts.RowTemplate.Height = 40
        dgvProducts.ColumnHeadersHeight = 40
        dgvProducts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        dgvProducts.DefaultCellStyle.Padding = New Padding(6, 4, 6, 4)
        dgvProducts.ColumnHeadersDefaultCellStyle.Padding = New Padding(4, 4, 4, 4)
        dgvProducts.AllowUserToResizeColumns = False

        Dim sym As String = AppSettings.Current.CurrencySymbol

        Dim indexCol As DataGridViewColumn = dgvProducts.Columns("Index")
        ConfigureFixedCartColumn(indexCol, CartColIndexWidth, DataGridViewContentAlignment.MiddleCenter, 0)

        Dim productCol As DataGridViewColumn = dgvProducts.Columns("ProductName")
        productCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        productCol.FillWeight = 100
        productCol.MinimumWidth = CartColProductMinWidth
        productCol.Resizable = DataGridViewTriState.False
        productCol.SortMode = DataGridViewColumnSortMode.NotSortable
        productCol.DisplayIndex = 1
        productCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
        productCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft

        Dim priceCol As DataGridViewColumn = dgvProducts.Columns("Price")
        priceCol.HeaderText = "Price (" & sym & ")"
        ConfigureFixedCartColumn(priceCol, CartColPriceWidth, DataGridViewContentAlignment.MiddleRight, 2)

        Dim qtyCol As DataGridViewColumn = dgvProducts.Columns("Quantity")
        ConfigureFixedCartColumn(qtyCol, CartColQtyWidth, DataGridViewContentAlignment.MiddleCenter, 3)

        Dim subtotalCol As DataGridViewColumn = dgvProducts.Columns("Subtotal")
        subtotalCol.HeaderText = "Subtotal (" & sym & ")"
        ConfigureFixedCartColumn(subtotalCol, CartColSubtotalWidth, DataGridViewContentAlignment.MiddleRight, 4)

        If dgvProducts.Columns.Contains(CartRemoveColumnName) Then
            Dim removeCol As DataGridViewColumn = dgvProducts.Columns(CartRemoveColumnName)
            ConfigureFixedCartColumn(removeCol, CartColRemoveWidth, DataGridViewContentAlignment.MiddleCenter, 5)
            removeCol.HeaderText = ""
            removeCol.DefaultCellStyle.Padding = New Padding(4, 6, 4, 6)
        End If
    End Sub

    Private Shared Sub ConfigureFixedCartColumn(
        column As DataGridViewColumn,
        width As Integer,
        alignment As DataGridViewContentAlignment,
        displayIndex As Integer)

        column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        column.Width = width
        column.MinimumWidth = width
        column.Resizable = DataGridViewTriState.False
        column.SortMode = DataGridViewColumnSortMode.NotSortable
        column.DisplayIndex = displayIndex
        column.DefaultCellStyle.Alignment = alignment
        column.HeaderCell.Style.Alignment = alignment
    End Sub

    Private Sub dgvProducts_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs) Handles dgvProducts.CellPainting
        If e.RowIndex < 0 OrElse Not IsCartRemoveColumn(e.ColumnIndex) Then
            Return
        End If

        e.PaintBackground(e.ClipBounds, True)

        Const inset As Integer = 5
        Dim buttonBounds As New Rectangle(
            e.CellBounds.X + inset,
            e.CellBounds.Y + inset,
            Math.Max(1, e.CellBounds.Width - (inset * 2)),
            Math.Max(1, e.CellBounds.Height - (inset * 2)))

        Dim backColor As Color = UiTheme.Danger
        If (e.State And DataGridViewElementStates.Selected) <> 0 Then
            backColor = UiTheme.DangerHover
        End If

        Using brush As New SolidBrush(backColor)
            e.Graphics.FillRectangle(brush, buttonBounds)
        End Using

        Dim caption As String = Convert.ToString(e.FormattedValue)
        If String.IsNullOrWhiteSpace(caption) Then
            caption = "Remove"
        End If

        TextRenderer.DrawText(
            e.Graphics,
            caption,
            UiTheme.FontBodySmall,
            buttonBounds,
            UiTheme.TextOnAccent,
            TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis)

        e.Handled = True
    End Sub

    Private Function IsCartRemoveColumn(columnIndex As Integer) As Boolean
        If columnIndex < 0 OrElse dgvProducts Is Nothing OrElse columnIndex >= dgvProducts.Columns.Count Then
            Return False
        End If

        Return String.Equals(dgvProducts.Columns(columnIndex).Name, CartRemoveColumnName, StringComparison.Ordinal)
    End Function

    Private Shared Function CreateCheckoutSummaryCaption(text As String) As Label
        Return New Label() With {
            .Text = text,
            .Dock = DockStyle.Fill,
            .AutoSize = False,
            .TextAlign = ContentAlignment.MiddleRight,
            .ForeColor = UiTheme.TextSecondary,
            .Font = New Font("Segoe UI", 10),
            .Margin = New Padding(0, 0, 10, 0)
        }
    End Function

    Private Shared Function CreateCheckoutSummaryValueLabel(Optional bold As Boolean = False, Optional successColor As Boolean = False) As Label
        Dim fontStyle As FontStyle = If(bold, FontStyle.Bold, FontStyle.Regular)
        Dim fore As Color = If(successColor, UiTheme.Success, UiTheme.TextPrimary)
        Return New Label() With {
            .Text = FormatMoney(0D),
            .Dock = DockStyle.Fill,
            .AutoSize = False,
            .TextAlign = ContentAlignment.MiddleRight,
            .ForeColor = fore,
            .Font = New Font("Segoe UI", If(bold, 12.0F, 11.0F), fontStyle),
            .Margin = Padding.Empty
        }
    End Function

    Private Function CreateTenderedInputShell() As Panel
        txtAmountTendered.Dock = DockStyle.Fill
        txtAmountTendered.Margin = Padding.Empty

        Dim inner As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = UiTheme.CardSurface,
            .Padding = New Padding(8, 4, 8, 4)
        }
        inner.Controls.Add(txtAmountTendered)

        Dim outer As New Panel() With {
            .Dock = DockStyle.Fill,
            .Height = TenderedFieldHeight,
            .MinimumSize = New Size(0, TenderedFieldHeight),
            .BackColor = UiTheme.CardBorder,
            .Padding = New Padding(1),
            .Margin = Padding.Empty
        }
        outer.Controls.Add(inner)
        Return outer
    End Function

    Private Shared Function CreateCheckoutColumnSeparator() As Panel
        Return New Panel() With {
            .Dock = DockStyle.Fill,
            .Width = 1,
            .Margin = New Padding(0, 12, 0, 12),
            .BackColor = UiTheme.CardBorder
        }
    End Function

    Private Function CreateSalesInputShell(inner As Control) As Panel
        inner.Dock = DockStyle.Fill
        inner.Margin = Padding.Empty

        Dim host As New Panel() With {
            .Dock = DockStyle.Top,
            .Height = SalesInputShellHeight,
            .MinimumSize = New Size(0, SalesInputShellHeight),
            .BackColor = UiTheme.CardBorder,
            .Padding = New Padding(1),
            .Margin = New Padding(0, 0, 0, 4)
        }

        Dim surface As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = UiTheme.CardSurface,
            .Padding = New Padding(10, 6, 10, 6)
        }
        surface.Controls.Add(inner)
        host.Controls.Add(surface)
        Return host
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

    Private Shared Sub ConfigureCheckoutDiscountButton(btn As Button)
        btn.AutoSize = False
        btn.Dock = DockStyle.Fill
        btn.Height = UiTheme.ButtonHeightSm
        btn.Margin = New Padding(0, 0, 0, UiTheme.SpaceXs)
        btn.TextAlign = ContentAlignment.MiddleCenter
    End Sub

    Private Function CreatePosDiscountToggle(caption As String, discountType As PosDiscountType) As Button
        Dim btn As New Button() With {
            .Text = caption,
            .Tag = discountType,
            .Font = UiTheme.FontBodySmall,
            .Cursor = Cursors.Hand,
            .FlatStyle = FlatStyle.Flat
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
        RefreshProductCards()
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

    Private Sub ProductCardScrollPanel_Resize(sender As Object, e As EventArgs)
        If productCardHost Is Nothing OrElse productCardScrollPanel Is Nothing Then
            Return
        End If

        productCardHost.Width = Math.Max(ProductCardWidth, productCardScrollPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - UiTheme.SpaceMd)
    End Sub

    Private Sub SelectProduct(productName As String)
        ClearSalesInputError()
        selectedProductName = productName

        Dim entry As ProductCatalogEntry = Nothing
        If Not productCatalog.TryGetValue(productName, entry) Then
            ClearProductSelection()
            Return
        End If

        Dim sym As String = AppSettings.Current.CurrencySymbol
        lblSelectedProduct.Text = String.Format(
            CultureInfo.CurrentCulture,
            "Selected: {0} — {1}{2:N2}",
            productName,
            sym,
            entry.UnitPrice)
        UpdateStockHintForProduct(entry.ProductId, productName)
        UpdateProductCardSelectionVisuals()
    End Sub

    Private Sub ClearProductSelection()
        selectedProductName = Nothing
        selectedProductCard = Nothing
        lblSelectedProduct.Text = "Selected: none"
        If lblStockOnHand IsNot Nothing Then
            lblStockOnHand.Text = "Available: —"
            lblStockOnHand.ForeColor = UiTheme.TextSecondary
        End If
        numQuantity.Maximum = MaxLineQty
        UpdateProductCardSelectionVisuals()
    End Sub

    Private Sub UpdateProductCardSelectionVisuals()
        If productCardHost Is Nothing Then
            Return
        End If

        selectedProductCard = Nothing
        For Each card As Control In productCardHost.Controls
            Dim panel As Panel = TryCast(card, Panel)
            If panel Is Nothing Then
                Continue For
            End If

            Dim productName As String = TryCast(panel.Tag, String)
            Dim isSelected As Boolean = Not String.IsNullOrWhiteSpace(selectedProductName) AndAlso
                String.Equals(productName, selectedProductName, StringComparison.OrdinalIgnoreCase)
            ApplyProductCardSelectionStyle(panel, isSelected)
            If isSelected Then
                selectedProductCard = panel
            End If
        Next
    End Sub

    Private Shared Sub ApplyProductCardSelectionStyle(card As Panel, selected As Boolean)
        card.BackColor = If(selected, Color.FromArgb(224, 228, 245), UiTheme.CardSurface)
    End Sub

    Private Sub ProductCard_Click(sender As Object, e As EventArgs)
        Dim card As Panel = FindProductCardPanel(TryCast(sender, Control))
        If card Is Nothing Then
            Return
        End If

        Dim productName As String = TryCast(card.Tag, String)
        If String.IsNullOrWhiteSpace(productName) Then
            Return
        End If

        SelectProduct(productName)
    End Sub

    Private Function FindProductCardPanel(control As Control) As Panel
        Dim current As Control = control
        While current IsNot Nothing
            Dim panel As Panel = TryCast(current, Panel)
            If panel IsNot Nothing AndAlso TypeOf panel.Tag Is String AndAlso productCatalog.ContainsKey(CStr(panel.Tag)) Then
                Return panel
            End If

            current = current.Parent
        End While

        Return Nothing
    End Function

    Private Sub WireProductCardClickEvents(root As Control)
        AddHandler root.Click, AddressOf ProductCard_Click
        For Each child As Control In root.Controls
            WireProductCardClickEvents(child)
        Next
    End Sub

    Private Sub DisposeProductCardImages()
        If productCardHost Is Nothing Then
            Return
        End If

        For Each card As Control In productCardHost.Controls
            Dim panel As Panel = TryCast(card, Panel)
            If panel Is Nothing Then
                Continue For
            End If

            For Each child As Control In panel.Controls
                Dim pic As PictureBox = TryCast(child, PictureBox)
                If pic IsNot Nothing Then
                    pic.Image = Nothing
                End If
            Next
        Next

        For Each image As Image In productCardImages
            image.Dispose()
        Next
        productCardImages.Clear()
    End Sub

    Private Function CreateProductCard(productName As String, entry As ProductCatalogEntry) As Panel
        Dim sym As String = AppSettings.Current.CurrencySymbol
        Dim available As Integer = GetAvailableStock(entry.ProductId, productName)

        Dim card As New Panel() With {
            .Width = ProductCardWidth,
            .Height = ProductCardHeight,
            .BackColor = UiTheme.CardSurface,
            .BorderStyle = BorderStyle.FixedSingle,
            .Margin = New Padding(UiTheme.SpaceSm),
            .Cursor = Cursors.Hand,
            .Tag = productName
        }

        Dim pic As New PictureBox() With {
            .Size = New Size(ProductCardWidth - 16, ProductCardImageHeight),
            .Location = New Point(8, 8),
            .SizeMode = PictureBoxSizeMode.Zoom,
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle
        }
        Dim cardImage As Image = ProductImageHelper.TryLoadProductImage(entry.ImagePath)
        If cardImage IsNot Nothing Then
            pic.Image = cardImage
            productCardImages.Add(cardImage)
        End If

        Dim lblName As New Label() With {
            .Text = productName,
            .Location = New Point(8, pic.Bottom + 6),
            .Size = New Size(ProductCardWidth - 16, 34),
            .Font = New Font(UiTheme.FontBody.FontFamily, 9.0F, FontStyle.Bold),
            .ForeColor = UiTheme.TextPrimary
        }

        Dim lblPrice As New Label() With {
            .Text = sym & entry.UnitPrice.ToString("N2", CultureInfo.CurrentCulture),
            .Location = New Point(8, lblName.Bottom + 2),
            .AutoSize = True,
            .Font = UiTheme.FontBodySmall,
            .ForeColor = UiTheme.PrimaryAccent
        }

        Dim lblStock As New Label() With {
            .Name = "cardStock",
            .Text = String.Format(CultureInfo.CurrentCulture, "Available: {0}", available),
            .Location = New Point(8, lblPrice.Bottom + 2),
            .AutoSize = True,
            .Font = UiTheme.FontBodySmall,
            .ForeColor = GetStockLabelColor(available)
        }

        card.Controls.Add(pic)
        card.Controls.Add(lblName)
        card.Controls.Add(lblPrice)
        card.Controls.Add(lblStock)
        WireProductCardClickEvents(card)
        Return card
    End Function

    Private Sub RefreshProductCardStockLabels()
        If productCardHost Is Nothing Then
            Return
        End If

        For Each card As Control In productCardHost.Controls
            Dim panel As Panel = TryCast(card, Panel)
            If panel Is Nothing Then
                Continue For
            End If

            Dim productName As String = TryCast(panel.Tag, String)
            If String.IsNullOrWhiteSpace(productName) Then
                Continue For
            End If

            Dim entry As ProductCatalogEntry = Nothing
            If Not productCatalog.TryGetValue(productName, entry) Then
                Continue For
            End If

            Dim stockLabel As Label = Nothing
            For Each child As Control In panel.Controls
                Dim lbl As Label = TryCast(child, Label)
                If lbl IsNot Nothing AndAlso String.Equals(lbl.Name, "cardStock", StringComparison.Ordinal) Then
                    stockLabel = lbl
                    Exit For
                End If
            Next

            If stockLabel Is Nothing Then
                Continue For
            End If

            Dim available As Integer = GetAvailableStock(entry.ProductId, productName)
            stockLabel.Text = String.Format(CultureInfo.CurrentCulture, "Available: {0}", available)
            stockLabel.ForeColor = GetStockLabelColor(available)
        Next

        If Not String.IsNullOrWhiteSpace(selectedProductName) Then
            Dim selectedEntry As ProductCatalogEntry = Nothing
            If productCatalog.TryGetValue(selectedProductName, selectedEntry) Then
                UpdateStockHintForProduct(selectedEntry.ProductId, selectedProductName)
            End If
        End If
    End Sub

    Private Sub UpdateStockHintForProduct(productId As Integer, productName As String)
        If lblStockOnHand Is Nothing Then
            Return
        End If

        Dim available As Integer = GetAvailableStock(productId, productName)
        lblStockOnHand.Text = String.Format(CultureInfo.CurrentCulture, "Available: {0}", available)
        lblStockOnHand.ForeColor = If(available > 0, UiTheme.TextSecondary, UiTheme.Danger)

        Dim maxQty As Decimal = Math.Max(MinLineQty, Math.Min(MaxLineQty, available))
        If maxQty < numQuantity.Minimum Then
            maxQty = numQuantity.Minimum
        End If
        numQuantity.Maximum = maxQty
        If numQuantity.Value > numQuantity.Maximum Then
            numQuantity.Value = numQuantity.Maximum
        End If
    End Sub

    Private Function GetCartQuantityForProduct(productId As Integer, productName As String, Optional excludeRowIndex As Integer = -1) As Integer
        If Not IsSalesCartGridReady() Then
            Return 0
        End If

        Dim total As Integer = 0
        For i As Integer = 0 To dgvProducts.Rows.Count - 1
            If i = excludeRowIndex Then
                Continue For
            End If

            Dim line As CartLineItem = TryCast(dgvProducts.Rows(i).Tag, CartLineItem)
            If line Is Nothing Then
                Continue For
            End If

            If ProductLinesMatch(line, productId, productName) Then
                total += line.Quantity
            End If
        Next

        Return total
    End Function

    Private Shared Function ProductLinesMatch(line As CartLineItem, productId As Integer, productName As String) As Boolean
        If line Is Nothing Then
            Return False
        End If

        If productId > 0 AndAlso line.ProductId > 0 Then
            Return line.ProductId = productId
        End If

        Return String.Equals(line.ProductName, productName, StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function GetOnHandStock(productId As Integer, productName As String) As Integer
        Dim entry As ProductCatalogEntry = Nothing
        If productCatalog.TryGetValue(productName, entry) Then
            Return entry.StockQuantity
        End If

        If productId > 0 Then
            For Each kvp As KeyValuePair(Of String, ProductCatalogEntry) In productCatalog
                If kvp.Value.ProductId = productId Then
                    Return kvp.Value.StockQuantity
                End If
            Next
        End If

        Return 0
    End Function

    Private Function GetAvailableStock(productId As Integer, productName As String, Optional excludeRowIndex As Integer = -1) As Integer
        Dim onHand As Integer = GetOnHandStock(productId, productName)
        Return Math.Max(0, onHand - GetCartQuantityForProduct(productId, productName, excludeRowIndex))
    End Function

    Private Function GetStockLabelColor(available As Integer) As Color
        If available <= AppSettings.Current.StockThreshold Then
            Return UiTheme.Danger
        End If

        If available = 0 Then
            Return UiTheme.Danger
        End If

        Return UiTheme.TextSecondary
    End Function

    Private Function TryValidateLineStock(productId As Integer, productName As String, lineQuantity As Integer, excludeRowIndex As Integer, ByRef message As String) As Boolean
        message = String.Empty
        Dim onHand As Integer = GetOnHandStock(productId, productName)
        Dim totalNeeded As Integer = GetCartQuantityForProduct(productId, productName, excludeRowIndex) + lineQuantity
        If totalNeeded <= onHand Then
            Return True
        End If

        Dim available As Integer = Math.Max(0, onHand - GetCartQuantityForProduct(productId, productName, excludeRowIndex))
        message = String.Format(
            CultureInfo.CurrentCulture,
            "Not enough stock for ""{0}"". Available: {1}, requested: {2}.",
            productName,
            available,
            lineQuantity)
        Return False
    End Function

    Private Function ValidateAllCartStock() As Boolean
        If Not IsSalesCartGridReady() Then
            Return True
        End If

        Dim totals As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
        Dim ids As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)

        For Each row As DataGridViewRow In dgvProducts.Rows
            Dim line As CartLineItem = TryCast(row.Tag, CartLineItem)
            If line Is Nothing Then
                Continue For
            End If

            Dim key As String = line.ProductName
            If totals.ContainsKey(key) Then
                totals(key) += line.Quantity
            Else
                totals(key) = line.Quantity
                ids(key) = line.ProductId
            End If
        Next

        For Each kvp As KeyValuePair(Of String, Integer) In totals
            Dim onHand As Integer = GetOnHandStock(ids(kvp.Key), kvp.Key)
            If kvp.Value > onHand Then
                Dim msg As String = String.Format(
                    CultureInfo.CurrentCulture,
                    "Not enough stock for ""{0}"". On hand: {1}, in cart: {2}.",
                    kvp.Key,
                    onHand,
                    kvp.Value)
                ShowSalesInputError(msg)
                MessageBox.Show(msg, "Insufficient stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End If
        Next

        Return True
    End Function

    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        Try
            ClearSalesInputError()
            If Not IsSalesCartGridReady() Then
                Return
            End If

            Dim productName As String = If(selectedProductName, String.Empty).Trim()
            If productName = String.Empty Then
                ShowSalesInputError("Select a product.")
                MessageBox.Show("Please select a product card.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim catalogEntry As ProductCatalogEntry = Nothing
            If Not productCatalog.TryGetValue(productName, catalogEntry) Then
                ShowSalesInputError("Product is not in the active catalog.")
                MessageBox.Show("Product is not in the active catalog.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim price As Decimal = catalogEntry.UnitPrice

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

            For i As Integer = 0 To dgvProducts.Rows.Count - 1
                Dim existingLine As CartLineItem = TryCast(dgvProducts.Rows(i).Tag, CartLineItem)
                If existingLine Is Nothing Then
                    Continue For
                End If

                If ProductLinesMatch(existingLine, catalogEntry.ProductId, productName) Then
                    Dim mergedQty As Integer = existingLine.Quantity + quantity
                    Dim stockMsg As String = String.Empty
                    If Not TryValidateLineStock(catalogEntry.ProductId, productName, mergedQty, i, stockMsg) Then
                        ShowSalesInputError(stockMsg)
                        MessageBox.Show(stockMsg, "Insufficient stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        numQuantity.Focus()
                        Return
                    End If

                    existingLine.Quantity = mergedQty
                    dgvProducts.Rows(i).Cells("Quantity").Value = mergedQty
                    dgvProducts.Rows(i).Cells("Subtotal").Value = FormatMoney(existingLine.LineSubtotal)
                    ClearProductSelection()
                    numQuantity.Value = MinLineQty
                    numQuantity.Maximum = MaxLineQty
                    ClearSalesInputError()
                    UpdateSummaryLabels()
                    RefreshProductCardStockLabels()
                    ShowStatus("Cart quantity updated.", False)
                    Return
                End If
            Next

            Dim stockMessage As String = String.Empty
            If Not TryValidateLineStock(catalogEntry.ProductId, productName, quantity, -1, stockMessage) Then
                ShowSalesInputError(stockMessage)
                MessageBox.Show(stockMessage, "Insufficient stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                numQuantity.Focus()
                Return
            End If

            Dim line As New CartLineItem(productName, price, quantity, catalogEntry.ProductId)
            Dim rowNumber As Integer = dgvProducts.Rows.Count + 1

            Dim idx As Integer = dgvProducts.Rows.Add(
            rowNumber,
            productName,
            FormatMoney(line.UnitPrice),
            quantity,
            FormatMoney(line.LineSubtotal))

            dgvProducts.Rows(idx).Tag = line

            ClearProductSelection()
            numQuantity.Value = MinLineQty
            numQuantity.Maximum = MaxLineQty
            ClearSalesInputError()

            ReindexRows()
            UpdateSummaryLabels()
            RefreshProductCardStockLabels()
            ShowStatus("Line added to cart.", False)

        Catch ex As Exception
            MessageBox.Show(
            "Crash Location: " & vbCrLf & ex.StackTrace,
            "Error Details",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnRemove_Click(sender As Object, e As EventArgs) Handles btnRemove.Click
        TryRemoveSelectedCartRow(showSelectMessage:=True)
    End Sub

    Private Sub dgvProducts_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvProducts.CellContentClick
        If e.RowIndex < 0 OrElse Not IsSalesCartGridReady() Then
            Return
        End If

        If Not String.Equals(dgvProducts.Columns(e.ColumnIndex).Name, CartRemoveColumnName, StringComparison.Ordinal) Then
            Return
        End If

        RemoveCartRowAt(e.RowIndex)
    End Sub

    Private Sub dgvProducts_KeyDown(sender As Object, e As KeyEventArgs) Handles dgvProducts.KeyDown
        If e.KeyCode <> Keys.Delete OrElse dgvProducts.IsCurrentCellInEditMode Then
            Return
        End If

        If TryRemoveSelectedCartRow(showSelectMessage:=False) Then
            e.Handled = True
            e.SuppressKeyPress = True
        End If
    End Sub

    Private Function TryRemoveSelectedCartRow(Optional showSelectMessage As Boolean = True) As Boolean
        If Not IsSalesCartGridReady() Then
            Return False
        End If

        If dgvProducts.SelectedRows.Count = 0 Then
            If showSelectMessage Then
                MessageBox.Show(
                    "Select a cart line to remove, or click Remove on the line.",
                    "Cart",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)
            End If

            Return False
        End If

        RemoveCartRowAt(dgvProducts.SelectedRows(0).Index)
        Return True
    End Function

    Private Sub RemoveCartRowAt(rowIndex As Integer)
        If Not IsSalesCartGridReady() OrElse rowIndex < 0 OrElse rowIndex >= dgvProducts.Rows.Count Then
            Return
        End If

        dgvProducts.Rows.RemoveAt(rowIndex)
        ReindexRows()
        UpdateSummaryLabels()
        RefreshProductCardStockLabels()
        ShowStatus("Item removed from cart.", False)
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
        ClearProductSelection()
        numQuantity.Value = MinLineQty
        ClearSalesInputError()
        UpdateSummaryLabels()
        RefreshProductCardStockLabels()
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
        Dim stockMessage As String = String.Empty
        If Not TryValidateLineStock(line.ProductId, line.ProductName, qty, e.RowIndex, stockMessage) Then
            ShowSalesInputError(stockMessage)
            MessageBox.Show(stockMessage, "Insufficient stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            row.Cells("Quantity").Value = line.Quantity
            Return
        End If

        line.Quantity = qty
        row.Cells("Price").Value = FormatMoney(line.UnitPrice)
        row.Cells("Subtotal").Value = FormatMoney(line.LineSubtotal)
        UpdateSummaryLabels()
        RefreshProductCardStockLabels()
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

        If Not ValidateAllCartStock() Then
            Return
        End If

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
        snapshot.SaleDateTime = DateTime.Now
        snapshot.PaymentMethod = "Cash"
        snapshot.ReceiptText = String.Empty
        Dim newSaleId As Integer = -1
        If Not SaveSale(snapshot, newSaleId) Then
            Return
        End If

        snapshot.SaleId = newSaleId
        snapshot.ReceiptNumber = ReceiptBranding.FormatReceiptNumber(newSaleId)
        snapshot.TransactionReference = ReceiptBranding.FormatTransactionReference(newSaleId, snapshot.SaleDateTime)
        snapshot.ReceiptText = ReceiptBranding.BuildReceiptText(snapshot)
        UpdateSaleReceiptText(newSaleId, snapshot.ReceiptText)

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

    Private Sub UpdateSaleReceiptText(saleId As Integer, receiptText As String)
        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()
                Using command As New SqlCommand("UPDATE sales SET receipt_text = @receipt_text WHERE sale_id = @sale_id;", connection)
                    command.Parameters.AddWithValue("@receipt_text", If(receiptText, String.Empty))
                    command.Parameters.AddWithValue("@sale_id", saleId)
                    command.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            ErrorLogger.Log(ex, NameOf(SalesForm) & "." & NameOf(UpdateSaleReceiptText))
        End Try
    End Sub

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

                    Dim deductionsById As New Dictionary(Of Integer, Integer)()
                    Dim deductionsByName As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)

                    For Each row As DataGridViewRow In dgvProducts.Rows
                        Dim line As CartLineItem = TryCast(row.Tag, CartLineItem)
                        If line Is Nothing Then
                            Continue For
                        End If

                        If line.ProductId > 0 Then
                            If deductionsById.ContainsKey(line.ProductId) Then
                                deductionsById(line.ProductId) += line.Quantity
                            Else
                                deductionsById(line.ProductId) = line.Quantity
                            End If
                        Else
                            If deductionsByName.ContainsKey(line.ProductName) Then
                                deductionsByName(line.ProductName) += line.Quantity
                            Else
                                deductionsByName(line.ProductName) = line.Quantity
                            End If
                        End If
                    Next

                    For Each kvp As KeyValuePair(Of Integer, Integer) In deductionsById
                        Dim deductSql As String =
                            "UPDATE products SET stock_quantity = stock_quantity - @qty, updated_at = SYSUTCDATETIME() " &
                            "WHERE id = @id AND is_active = 1 AND stock_quantity >= @qty;"
                        Using deductCmd As New SqlCommand(deductSql, connection, transaction)
                            deductCmd.Parameters.AddWithValue("@id", kvp.Key)
                            deductCmd.Parameters.AddWithValue("@qty", kvp.Value)
                            Dim affected As Integer = deductCmd.ExecuteNonQuery()
                            If affected <> 1 Then
                                Throw New InvalidOperationException(
                                    String.Format(CultureInfo.CurrentCulture, "Insufficient stock for product id {0}.", kvp.Key))
                            End If
                        End Using
                    Next

                    For Each kvp As KeyValuePair(Of String, Integer) In deductionsByName
                        Dim deductSql As String =
                            "UPDATE products SET stock_quantity = stock_quantity - @qty, updated_at = SYSUTCDATETIME() " &
                            "WHERE product_name = @name AND is_active = 1 AND stock_quantity >= @qty;"
                        Using deductCmd As New SqlCommand(deductSql, connection, transaction)
                            deductCmd.Parameters.AddWithValue("@name", kvp.Key)
                            deductCmd.Parameters.AddWithValue("@qty", kvp.Value)
                            Dim affected As Integer = deductCmd.ExecuteNonQuery()
                            If affected <> 1 Then
                                Throw New InvalidOperationException(
                                    String.Format(CultureInfo.CurrentCulture, "Insufficient stock for ""{0}"".", kvp.Key))
                            End If
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
            LoadProducts()
            Return True
        Catch ex As InvalidOperationException
            ShowStatus("Sale not saved — insufficient stock.", True)
            MessageBox.Show(ex.Message, "Insufficient stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            ErrorLogger.Log(ex, NameOf(SalesForm) & "." & NameOf(SaveSale))
            Return False
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
        ClearProductSelection()

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
                    "SELECT id, product_name, price, category_id, stock_quantity, image_path " &
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

                            Dim imagePath As String = String.Empty
                            Dim imageOrd As Integer = reader.GetOrdinal("image_path")
                            If Not reader.IsDBNull(imageOrd) Then
                                imagePath = reader.GetString(imageOrd)
                            End If

                            productCatalog(productName) = New ProductCatalogEntry With {
                                .ProductId = Convert.ToInt32(reader("id")),
                                .UnitPrice = price,
                                .CategoryId = catId,
                                .StockQuantity = Convert.ToInt32(reader("stock_quantity")),
                                .ImagePath = imagePath}
                        End While
                    End Using
                End Using
            End Using

            RefreshProductCards()

            Dim hasCatalogProducts As Boolean = productCatalog.Count > 0
            Dim hasVisibleCards As Boolean = productCardHost IsNot Nothing AndAlso productCardHost.Controls.Count > 0
            btnAdd.Enabled = hasVisibleCards
            numQuantity.Enabled = hasVisibleCards
            lblEmptyHint.Visible = Not hasCatalogProducts
            lblNoProductCards.Visible = hasCatalogProducts AndAlso Not hasVisibleCards
            productCardScrollPanel.Visible = hasVisibleCards
            If lblNoProductCards.Visible Then
                lblNoProductCards.BringToFront()
            Else
                productCardScrollPanel.BringToFront()
            End If
            btnOpenProducts.Visible = Not hasCatalogProducts AndAlso AppSession.IsAdmin()
        Catch ex As Exception
            suppressSalesCategoryEvent = False
            ShowDatabaseError("Error loading products", ex)
            ErrorLogger.Log(ex, NameOf(SalesForm) & "." & NameOf(LoadProducts))
            btnAdd.Enabled = False
            numQuantity.Enabled = False
            lblEmptyHint.Visible = True
            lblNoProductCards.Visible = False
            lblEmptyHint.Text = "Could not load products. Check database and App.config."
            btnOpenProducts.Visible = AppSession.IsAdmin()
        End Try
    End Sub

    Private Sub RefreshProductCards()
        If productCardHost Is Nothing OrElse cmbSalesCategory Is Nothing Then
            Return
        End If

        Dim previousSelection As String = selectedProductName
        DisposeProductCardImages()
        productCardHost.Controls.Clear()
        productCardHost.SuspendLayout()

        Dim sel As SalesCategoryFilterItem = TryCast(cmbSalesCategory.SelectedItem, SalesCategoryFilterItem)
        If sel Is Nothing Then
            sel = New SalesCategoryFilterItem With {.Kind = SalesCategoryFilterItem.FilterKindEnum.AllCategories, .Display = "All categories"}
        End If

        Dim names As New List(Of String)(productCatalog.Keys)
        names.Sort(StringComparer.OrdinalIgnoreCase)

        For Each productName As String In names
            Dim entry As ProductCatalogEntry = productCatalog(productName)
            If entry.StockQuantity <= 0 Then
                Continue For
            End If

            If Not CategoryFilterMatches(sel, entry.CategoryId) Then
                Continue For
            End If

            productCardHost.Controls.Add(CreateProductCard(productName, entry))
        Next

        productCardHost.ResumeLayout(True)
        ProductCardScrollPanel_Resize(productCardScrollPanel, EventArgs.Empty)

        If Not String.IsNullOrWhiteSpace(previousSelection) AndAlso productCatalog.ContainsKey(previousSelection) Then
            Dim entry As ProductCatalogEntry = productCatalog(previousSelection)
            If entry.StockQuantity > 0 AndAlso CategoryFilterMatches(sel, entry.CategoryId) Then
                SelectProduct(previousSelection)
                Return
            End If
        End If

        ClearProductSelection()
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

    ''' <summary>
    ''' Loads sale line items into the cart (duplicate sale workflow).
    ''' </summary>
    Public Sub LoadCartFromSaleId(saleId As Integer)
        If saleId <= 0 OrElse Not IsSalesCartGridReady() Then
            Return
        End If

        Try
            dgvProducts.Rows.Clear()
            txtAmountTendered.Clear()
            ResetPosCheckoutOptions()

            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()
                Dim sql As String =
                    "SELECT product_name, price, quantity FROM sale_items WHERE sale_id = @sid ORDER BY sale_item_id;"
                Using cmd As New SqlCommand(sql, connection)
                    cmd.Parameters.AddWithValue("@sid", saleId)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim productName As String = reader("product_name").ToString()
                            Dim price As Decimal = Convert.ToDecimal(reader("price"))
                            Dim quantity As Integer = Convert.ToInt32(reader("quantity"))
                            Dim productId As Integer = 0
                            Dim entry As ProductCatalogEntry = Nothing
                            If productCatalog.TryGetValue(productName, entry) Then
                                productId = entry.ProductId
                            End If

                            Dim line As New CartLineItem(productName, price, quantity, productId)
                            Dim idx As Integer = dgvProducts.Rows.Add(
                                dgvProducts.Rows.Count + 1,
                                productName,
                                FormatMoney(line.UnitPrice),
                                quantity,
                                FormatMoney(line.LineSubtotal))
                            dgvProducts.Rows(idx).Tag = line
                        End While
                    End Using
                End Using
            End Using

            ReindexRows()
            UpdateSummaryLabels()
            ShowStatus("Cart loaded from sale #" & saleId.ToString(CultureInfo.InvariantCulture) & ".", False)
        Catch ex As Exception
            MessageBox.Show("Could not duplicate sale: " & ex.Message, "Sales", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ErrorLogger.Log(ex, NameOf(SalesForm) & "." & NameOf(LoadCartFromSaleId))
        End Try
    End Sub

End Class
