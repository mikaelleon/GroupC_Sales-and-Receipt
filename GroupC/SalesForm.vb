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
    Private Const ProductCardWidth As Integer = 150
    Private Const ProductCardHeight As Integer = 180
    Private Const ProductCardImageHeight As Integer = 90
    Private Const CartRemoveColumnName As String = "Remove"
    Private Const CartColIndexWidth As Integer = 40
    Private Const CartColPriceWidth As Integer = 80
    Private Const CartColQtyWidth As Integer = 50
    Private Const CartColSubtotalWidth As Integer = 90
    Private Const CartColRemoveWidth As Integer = 40
    Private Const CartColProductMinWidth As Integer = 100

    Private Enum PosDiscountType
        None = 0
        Pwd = 1
        Senior = 2
        Membership = 3
    End Enum

    Private WithEvents cmbSalesCategory As ComboBox
    Private WithEvents txtProductSearch As TextBox
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
    Private lblCartEmpty As Label
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
        UiTheme.ApplyMaximizedWorkspaceDefaults(Me, 1000, 650)
    End Sub

    Private Sub CreateControls()
        suppressSalesSummary = True
        Me.SuspendLayout()
        Me.Controls.Clear()
        Me.BackColor = UiTheme.ColBackground

        ' -----------------------------------------------------------
        ' 1. INITIALIZE CONTROLS
        ' -----------------------------------------------------------
        txtProductSearch = New TextBox() With {
            .PlaceholderText = "Search products...",
            .Width = 180,
            .Margin = New Padding(0, 0, UiTheme.PadControl, 0)
        }
        UiTheme.ApplyInputStyle(txtProductSearch)

        cmbSalesCategory = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Width = 160,
            .Margin = New Padding(0)
        }
        UiTheme.ApplyInputStyle(cmbSalesCategory)
        productCardScrollPanel = New Panel() With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .BackColor = UiTheme.ColBackground
        }
        productCardHost = New FlowLayoutPanel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = True,
            .Dock = DockStyle.Top,
            .Padding = New Padding(UiTheme.PadTight),
            .BackColor = UiTheme.ColBackground
        }
        productCardScrollPanel.Controls.Add(productCardHost)
        AddHandler productCardScrollPanel.Resize, AddressOf ProductCardScrollPanel_Resize

        lblNoProductCards = New Label() With {
            .Text = "No products available for this category.",
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter,
            .ForeColor = UiTheme.ColTextSecondary,
            .Font = UiTheme.FontBody,
            .Visible = False
        }
        lblSelectedProduct = New Label() With {
            .AutoSize = True,
            .ForeColor = UiTheme.ColTextPrimary,
            .Font = UiTheme.FontBodyBold,
            .Text = "No product selected",
            .Margin = New Padding(0)
        }
        numQuantity = New NumericUpDown() With {
            .Minimum = MinLineQty,
            .Maximum = MaxLineQty,
            .TextAlign = HorizontalAlignment.Right,
            .Width = 60
        }
        UiTheme.ApplyInputStyle(numQuantity)

        btnAdd = New Button() With {
            .Text = "&Add to cart",
            .AutoSize = True,
            .MinimumSize = New Size(0, UiTheme.ButtonHeight),
            .Cursor = Cursors.Hand
        }
        UiTheme.ApplyDisabledButton(btnAdd)
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
            .Text = "Discount",
            .AutoSize = True,
            .ForeColor = UiTheme.ColTextSecondary,
            .Font = UiTheme.FontCaption,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        }

        btnDiscPwd = CreatePosDiscountToggle("PWD  20%", PosDiscountType.Pwd)
        btnDiscSenior = CreatePosDiscountToggle("Senior  20%", PosDiscountType.Senior)
        btnDiscMembership = CreatePosDiscountToggle("Member  10%", PosDiscountType.Membership)

        btnTaxToggle = New Button() With {
            .Text = "VAT / Tax",
            .AutoSize = False,
            .Height = UiTheme.ButtonHeight,
            .Dock = DockStyle.Top,
            .Font = UiTheme.FontBody,
            .Cursor = Cursors.Hand,
            .Margin = New Padding(0, UiTheme.PadControl, 0, UiTheme.PadTight)
        }
        UiTheme.ApplySecondaryButton(btnTaxToggle)
        numTaxPercent = New NumericUpDown() With {
            .DecimalPlaces = 2,
            .Minimum = 0D,
            .Maximum = 100D,
            .Increment = 0.5D,
            .Enabled = False,
            .Width = 70,
            .TextAlign = HorizontalAlignment.Right
        }
        UiTheme.ApplyInputStyle(numTaxPercent)

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
            .AutoSize = True,
            .Font = UiTheme.FontHeading,
            .ForeColor = UiTheme.ColPrimary,
            .TextAlign = ContentAlignment.MiddleRight,
            .Margin = Padding.Empty
        }

        btnFinalize = New Button() With {
            .Text = "FINALIZE SALE",
            .Dock = DockStyle.Top,
            .AutoSize = False,
            .Height = 44,
            .Font = UiTheme.FontSubheading,
            .Cursor = Cursors.Hand,
            .Margin = New Padding(0, UiTheme.PadControl, 0, 0)
        }
        UiTheme.ApplyDisabledButton(btnFinalize)

        lblSalesInputError = New Label() With {.AutoSize = True, .ForeColor = UiTheme.ColDanger, .Font = UiTheme.FontCaption, .Visible = False}
        lblStockOnHand = New Label() With {
            .AutoSize = True,
            .ForeColor = UiTheme.ColTextSecondary,
            .Font = UiTheme.FontCaption,
            .Margin = New Padding(0),
            .Text = "Available: —"
        }
        lblEmptyHint = New Label() With {.Text = "No products in catalog. Open Manage Products.", .AutoSize = True, .ForeColor = UiTheme.ColTextSecondary, .Visible = False}
        lblCartEmpty = New Label() With {
            .Text = "Cart is empty",
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter,
            .ForeColor = UiTheme.ColTextSecondary,
            .Font = New Font(UiTheme.FontBody, FontStyle.Italic),
            .BackColor = Color.Transparent,
            .Visible = True
        }

        dgvProducts = New DataGridView() With {
            .Dock = DockStyle.Fill,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .AllowUserToResizeColumns = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            .AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
            .BackgroundColor = UiTheme.ColSurface,
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
            .Text = "×",
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
            UiTheme.ApplySecondaryButton(btnRemove)
            UiTheme.ApplySecondaryButton(btnClear)
            UiTheme.ApplySecondaryButton(btnOpenProducts)
            UiTheme.ApplyGhostButton(btnBack)
            UiTheme.ApplyGridStyle(dgvProducts)
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
        ' 2. SHARED SHELL + POS SPLIT LAYOUT
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

        Dim sidebar As Panel = UiTheme.BuildSidebar()
        Dim sidebarStack As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .BackColor = UiTheme.ColPrimary
        }
        sidebarStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        sidebarStack.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        sidebarStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim lblSidebarStore As New Label() With {
            .Text = AppSettings.Current.StoreName,
            .Font = UiTheme.FontSubheading,
            .ForeColor = UiTheme.ColTextOnDark,
            .AutoSize = True,
            .Dock = DockStyle.Top,
            .Padding = New Padding(UiTheme.PadCard),
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        }

        Dim navMain As New Panel() With {.AutoSize = True, .Dock = DockStyle.Top, .BackColor = Color.Transparent}
        Dim navItems As (Text As String, Active As Boolean)() = {
            ("Manage Products", False),
            ("Manage Categories", False),
            ("Manage Cashiers", False),
            ("Point of Sale", True),
            ("Receipt Preview", False),
            ("Reports", False)
        }
        For i As Integer = navItems.Length - 1 To 0 Step -1
            Dim item = navItems(i)
            Dim navBtn As Button = UiTheme.CreateSidebarNavButton(item.Text)
            navBtn.Dock = DockStyle.Top
            If item.Active Then
                UiTheme.SetSidebarButtonActive(navBtn, True)
            Else
                AddHandler navBtn.Click, Sub(s, ev) Me.Close()
            End If
            navMain.Controls.Add(navBtn)
        Next

        Dim navBottom As New Panel() With {
            .AutoSize = True,
            .Dock = DockStyle.Top,
            .BackColor = Color.Transparent,
            .Padding = New Padding(0, UiTheme.PadControl, 0, UiTheme.PadCard)
        }
        navBottom.Controls.Add(UiTheme.CreateSidebarSeparator())
        Dim btnBackNav As Button = UiTheme.CreateSidebarNavButton("← Back to Menu")
        btnBackNav.Dock = DockStyle.Top
        AddHandler btnBackNav.Click, Sub(s, ev) Me.Close()
        navBottom.Controls.Add(btnBackNav)

        Dim sidebarTop As New Panel() With {.AutoSize = True, .Dock = DockStyle.Top, .BackColor = Color.Transparent}
        sidebarTop.Controls.Add(navMain)
        sidebarTop.Controls.Add(lblSidebarStore)

        sidebarStack.Controls.Add(sidebarTop, 0, 0)
        sidebarStack.Controls.Add(UiTheme.CreateSidebarSpacer(), 0, 1)
        sidebarStack.Controls.Add(navBottom, 0, 2)
        sidebar.Controls.Add(sidebarStack)

        Dim rightColumn As New Panel() With {.Dock = DockStyle.Fill, .BackColor = UiTheme.ColBackground}

        Dim topBar As New Panel() With {
            .Height = 60,
            .Dock = DockStyle.Top,
            .BackColor = UiTheme.ColSurface,
            .Padding = New Padding(UiTheme.PadPage, UiTheme.PadControl, UiTheme.PadPage, UiTheme.PadControl)
        }
        Dim lblPageTitle As New Label() With {
            .Text = "Point of Sale",
            .Font = UiTheme.FontDisplay,
            .ForeColor = UiTheme.ColTextPrimary,
            .AutoSize = True,
            .Location = New Point(UiTheme.PadPage, UiTheme.PadControl)
        }
        Dim subtitleText As String = AppSession.GetReceiptOperatorName()
        Dim lblPageSubtitle As New Label() With {
            .Text = subtitleText,
            .Font = UiTheme.FontCaption,
            .ForeColor = UiTheme.ColTextSecondary,
            .AutoSize = True,
            .Location = New Point(UiTheme.PadPage, lblPageTitle.Bottom + UiTheme.PadTight)
        }
        topBar.Controls.Add(lblPageTitle)
        topBar.Controls.Add(lblPageSubtitle)
        topBar.Controls.Add(New Panel() With {.Height = 1, .Dock = DockStyle.Bottom, .BackColor = UiTheme.ColBorder})

        Dim contentArea As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = UiTheme.ColBackground,
            .Padding = New Padding(UiTheme.PadPage)
        }

        Dim posSplit As New SplitContainer() With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Vertical,
            .SplitterWidth = 6,
            .BackColor = UiTheme.ColBorder,
            .Panel1MinSize = 320,
            .Panel2MinSize = 280
        }

        ' --- LEFT: product browser ---
        Dim leftPanel As New Panel() With {.Dock = DockStyle.Fill, .BackColor = UiTheme.ColBackground}
        Dim leftLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 4
        }
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        leftLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim productsHeaderHost As Panel = UiTheme.CreateSectionHeader("Products")
        productsHeaderHost.Dock = DockStyle.Top

        Dim filterRow As New FlowLayoutPanel() With {
            .AutoSize = True,
            .WrapContents = False,
            .FlowDirection = FlowDirection.LeftToRight,
            .Dock = DockStyle.Top,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl),
            .Width = 400
        }
        filterRow.Controls.Add(txtProductSearch)
        filterRow.Controls.Add(cmbSalesCategory)

        Dim catalogHost As New Panel() With {.Dock = DockStyle.Fill, .BackColor = UiTheme.ColBackground}
        catalogHost.Controls.Add(lblNoProductCards)
        catalogHost.Controls.Add(productCardScrollPanel)

        Dim selectionBar As New Panel() With {
            .Dock = DockStyle.Top,
            .BackColor = UiTheme.ColSurface,
            .Padding = New Padding(UiTheme.PadCard),
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink
        }
        Dim selectionLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .ColumnCount = 2,
            .Margin = Padding.Empty
        }
        selectionLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 55.0F))
        selectionLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 45.0F))

        Dim selectionLeft As New FlowLayoutPanel() With {
            .FlowDirection = FlowDirection.TopDown,
            .AutoSize = True,
            .WrapContents = False,
            .Dock = DockStyle.Fill
        }
        selectionLeft.Controls.Add(lblSelectedProduct)
        selectionLeft.Controls.Add(lblStockOnHand)

        Dim selectionRight As New FlowLayoutPanel() With {
            .FlowDirection = FlowDirection.LeftToRight,
            .AutoSize = True,
            .WrapContents = False,
            .Dock = DockStyle.Fill,
            .Anchor = AnchorStyles.Right
        }
        selectionRight.Controls.Add(New Label() With {
            .Text = "Qty",
            .AutoSize = True,
            .Font = UiTheme.FontCaption,
            .ForeColor = UiTheme.ColTextSecondary,
            .Margin = New Padding(0, 6, UiTheme.PadTight, 0)
        })
        selectionRight.Controls.Add(numQuantity)
        selectionRight.Controls.Add(btnAdd)

        selectionLayout.Controls.Add(selectionLeft, 0, 0)
        selectionLayout.Controls.Add(selectionRight, 1, 0)
        selectionBar.Controls.Add(selectionLayout)

        Dim utilityRow As New FlowLayoutPanel() With {
            .AutoSize = True,
            .FlowDirection = FlowDirection.LeftToRight,
            .Dock = DockStyle.Top,
            .Margin = New Padding(0, UiTheme.PadControl, 0, 0)
        }
        utilityRow.Controls.Add(btnOpenProducts)
        utilityRow.Controls.Add(btnBack)

        leftLayout.Controls.Add(productsHeaderHost, 0, 0)
        leftLayout.Controls.Add(filterRow, 0, 1)
        leftLayout.Controls.Add(catalogHost, 0, 2)
        leftLayout.Controls.Add(selectionBar, 0, 3)
        leftPanel.Controls.Add(leftLayout)
        lblSalesInputError.Dock = DockStyle.Bottom
        leftPanel.Controls.Add(lblSalesInputError)
        leftPanel.Controls.Add(utilityRow)

        ' --- RIGHT: cart + checkout ---
        Dim rightSplit As New SplitContainer() With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Horizontal,
            .SplitterWidth = 6,
            .BackColor = UiTheme.ColBorder,
            .Panel1MinSize = 120,
            .Panel2MinSize = 220
        }

        Dim cartPanel As New Panel() With {.Dock = DockStyle.Fill, .BackColor = UiTheme.ColBackground}
        Dim cartHeader As New FlowLayoutPanel() With {
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = False,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        }
        cartHeader.Controls.Add(New Label() With {
            .Text = "Shopping Cart",
            .Font = UiTheme.FontSubheading,
            .ForeColor = UiTheme.ColTextPrimary,
            .AutoSize = True,
            .Margin = New Padding(0, 0, UiTheme.PadSection, 0)
        })
        cartHeader.Controls.Add(btnRemove)
        cartHeader.Controls.Add(btnClear)

        Dim cartGridHost As New Panel() With {.Dock = DockStyle.Fill, .MinimumSize = New Size(0, 160)}
        cartGridHost.Controls.Add(dgvProducts)
        cartGridHost.Controls.Add(lblCartEmpty)
        cartGridHost.Controls.Add(lblEmptyHint)

        cartPanel.Controls.Add(cartGridHost)
        cartPanel.Controls.Add(cartHeader)

        RefreshPosDiscountToggleUi()
        RefreshTaxToggleUi()

        Dim totalsPanel As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = UiTheme.ColSurface,
            .Padding = New Padding(UiTheme.PadCard)
        }
        Dim totalsTopBorder As New Panel() With {
            .Height = 1,
            .Dock = DockStyle.Top,
            .BackColor = UiTheme.ColBorder
        }

        Dim checkoutSplit As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1
        }
        checkoutSplit.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 42.0F))
        checkoutSplit.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 58.0F))

        Dim discountColumn As New FlowLayoutPanel() With {
            .FlowDirection = FlowDirection.TopDown,
            .Dock = DockStyle.Fill,
            .WrapContents = False,
            .AutoScroll = True
        }
        discountColumn.Controls.Add(lblCustomerDiscount)
        ConfigureCheckoutDiscountButton(btnDiscPwd)
        ConfigureCheckoutDiscountButton(btnDiscSenior)
        ConfigureCheckoutDiscountButton(btnDiscMembership)
        discountColumn.Controls.Add(btnDiscPwd)
        discountColumn.Controls.Add(btnDiscSenior)
        discountColumn.Controls.Add(btnDiscMembership)
        discountColumn.Controls.Add(btnTaxToggle)
        discountColumn.Controls.Add(numTaxPercent)
        discountColumn.Controls.Add(New Label() With {
            .Text = "Custom tax rate",
            .Font = UiTheme.FontCaption,
            .ForeColor = UiTheme.ColTextSecondary,
            .AutoSize = True,
            .Margin = New Padding(0, UiTheme.PadTight, 0, 0)
        })

        Dim totalsColumn As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 9
        }
        totalsColumn.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 45.0F))
        totalsColumn.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 55.0F))
        For r As Integer = 0 To 8
            totalsColumn.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Next

        Dim lblAmountDueCaption As New Label() With {
            .Text = "Amount Due",
            .Font = UiTheme.FontSubheading,
            .ForeColor = UiTheme.ColTextPrimary,
            .AutoSize = True
        }

        totalsColumn.Controls.Add(CreateCheckoutSummaryCaption("Subtotal"), 0, 0)
        totalsColumn.Controls.Add(lblSubtotalValue, 1, 0)
        totalsColumn.Controls.Add(lblDiscountHeading, 0, 1)
        totalsColumn.Controls.Add(lblDiscountValue, 1, 1)
        totalsColumn.Controls.Add(CreateCheckoutSummaryCaption("Tax"), 0, 2)
        totalsColumn.Controls.Add(lblTaxValue, 1, 2)
        totalsColumn.Controls.Add(New Panel() With {.Height = 1, .Dock = DockStyle.Fill, .BackColor = UiTheme.ColBorder, .Margin = New Padding(0, UiTheme.PadControl, 0, UiTheme.PadControl)}, 0, 3)
        totalsColumn.SetColumnSpan(totalsColumn.Controls(totalsColumn.Controls.Count - 1), 2)
        totalsColumn.Controls.Add(lblAmountDueCaption, 0, 4)
        totalsColumn.Controls.Add(lblTotal, 1, 4)
        totalsColumn.Controls.Add(CreateCheckoutSummaryCaption("Tendered"), 0, 5)
        totalsColumn.Controls.Add(CreateTenderedInputShell(), 1, 5)
        totalsColumn.Controls.Add(CreateCheckoutSummaryCaption("Change"), 0, 6)
        totalsColumn.Controls.Add(lblChangeValue, 1, 6)
        totalsColumn.Controls.Add(New Panel() With {.Height = 1, .Dock = DockStyle.Fill, .BackColor = UiTheme.ColBorder, .Margin = New Padding(0, UiTheme.PadControl, 0, UiTheme.PadControl)}, 0, 7)
        totalsColumn.SetColumnSpan(totalsColumn.Controls(totalsColumn.Controls.Count - 1), 2)
        totalsColumn.Controls.Add(btnFinalize, 0, 8)
        totalsColumn.SetColumnSpan(btnFinalize, 2)

        checkoutSplit.Controls.Add(discountColumn, 0, 0)
        checkoutSplit.Controls.Add(totalsColumn, 1, 0)
        totalsPanel.Controls.Add(checkoutSplit)
        totalsPanel.Controls.Add(totalsTopBorder)

        rightSplit.Panel1.Controls.Add(cartPanel)
        rightSplit.Panel2.Controls.Add(totalsPanel)

        posSplit.Panel1.Controls.Add(leftPanel)
        posSplit.Panel2.Controls.Add(rightSplit)

        contentArea.Controls.Add(posSplit)
        rightColumn.Controls.Add(contentArea)
        rightColumn.Controls.Add(topBar)

        rootTable.Controls.Add(sidebar, 0, 0)
        rootTable.Controls.Add(rightColumn, 1, 0)

        Me.Controls.Add(rootTable)
        Me.Controls.Add(statusStrip)

        ConfigurePosSplitters(posSplit, rightSplit)

        suppressSalesSummary = False
        Me.ResumeLayout(True)
        UpdateAddButtonState()
        UpdateFinalizeButtonState()
        UpdateCartEmptyState()
    End Sub

    Private Sub ConfigurePosSplitters(horizontalSplit As SplitContainer, verticalSplit As SplitContainer)
        If horizontalSplit Is Nothing OrElse horizontalSplit.Width <= 0 Then
            Return
        End If

        horizontalSplit.SplitterDistance = CInt(Math.Max(horizontalSplit.Panel1MinSize, horizontalSplit.Width * 0.6R))
        If verticalSplit IsNot Nothing AndAlso verticalSplit.Height > 0 Then
            verticalSplit.SplitterDistance = CInt(Math.Max(verticalSplit.Panel1MinSize, verticalSplit.Height * 0.62R))
        End If
    End Sub

    Private Sub txtProductSearch_TextChanged(sender As Object, e As EventArgs) Handles txtProductSearch.TextChanged
        RefreshProductCards()
    End Sub

    Private Sub UpdateAddButtonState()
        If btnAdd Is Nothing Then
            Return
        End If

        If String.IsNullOrWhiteSpace(selectedProductName) Then
            UiTheme.ApplyDisabledButton(btnAdd)
        Else
            UiTheme.ApplyPrimaryButton(btnAdd)
        End If
    End Sub

    Private Sub UpdateFinalizeButtonState()
        If btnFinalize Is Nothing OrElse Not IsSalesCartGridReady() Then
            Return
        End If

        If dgvProducts.Rows.Count > 0 Then
            btnFinalize.Enabled = True
            UiTheme.ApplySuccessButton(btnFinalize)
            btnFinalize.Height = 44
            btnFinalize.Font = UiTheme.FontSubheading
        Else
            UiTheme.ApplyDisabledButton(btnFinalize)
        End If
    End Sub

    Private Sub UpdateCartEmptyState()
        If lblCartEmpty Is Nothing OrElse Not IsSalesCartGridReady() Then
            Return
        End If

        lblCartEmpty.Visible = dgvProducts.Rows.Count = 0
        lblCartEmpty.BringToFront()
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

        Dim caption As String = "×"

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
        If isSelected Then
            UiTheme.ApplyPrimaryButton(btn)
        ElseIf othersLocked Then
            UiTheme.ApplyDisabledButton(btn)
        Else
            UiTheme.ApplySecondaryButton(btn)
        End If
    End Sub

    Private Sub RefreshTaxToggleUi()
        If btnTaxToggle Is Nothing OrElse numTaxPercent Is Nothing Then
            Return
        End If

        numTaxPercent.Enabled = taxToggleOn
        If taxToggleOn Then
            UiTheme.ApplyPrimaryButton(btnTaxToggle)
        Else
            UiTheme.ApplySecondaryButton(btnTaxToggle)
        End If
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
        UpdateAddButtonState()
    End Sub

    Private Sub ClearProductSelection()
        selectedProductName = Nothing
        selectedProductCard = Nothing
        lblSelectedProduct.Text = "No product selected"
        If lblStockOnHand IsNot Nothing Then
            lblStockOnHand.Text = "Available: —"
            lblStockOnHand.ForeColor = UiTheme.TextSecondary
        End If
        numQuantity.Maximum = MaxLineQty
        UpdateProductCardSelectionVisuals()
        UpdateAddButtonState()
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
        card.BackColor = If(selected, UiTheme.InfoBackground, UiTheme.ColSurface)
    End Sub

    Private Sub ProductCard_MouseEnter(sender As Object, e As EventArgs)
        Dim card As Panel = FindProductCardPanel(TryCast(sender, Control))
        If card Is Nothing OrElse Object.ReferenceEquals(card, selectedProductCard) Then
            Return
        End If

        card.BackColor = UiTheme.SurfaceVariant
    End Sub

    Private Sub ProductCard_MouseLeave(sender As Object, e As EventArgs)
        Dim card As Panel = FindProductCardPanel(TryCast(sender, Control))
        If card Is Nothing Then
            Return
        End If

        Dim productName As String = TryCast(card.Tag, String)
        Dim isSelected As Boolean = Not String.IsNullOrWhiteSpace(selectedProductName) AndAlso
            String.Equals(productName, selectedProductName, StringComparison.OrdinalIgnoreCase)
        ApplyProductCardSelectionStyle(card, isSelected)
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
        AddHandler root.MouseEnter, AddressOf ProductCard_MouseEnter
        AddHandler root.MouseLeave, AddressOf ProductCard_MouseLeave
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
            .BackColor = UiTheme.ColSurface,
            .BorderStyle = BorderStyle.FixedSingle
        }
        Dim cardImage As Image = ProductImageHelper.TryLoadProductImage(entry.ImagePath)
        If cardImage IsNot Nothing Then
            pic.Image = cardImage
            productCardImages.Add(cardImage)
        Else
            Dim lblPlaceholder As New Label() With {
                .Text = "📦",
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Font = New Font(UiTheme.FontBody.FontFamily, 24.0F, FontStyle.Regular),
                .ForeColor = UiTheme.ColTextSecondary,
                .BackColor = UiTheme.SurfaceVariant
            }
            pic.Controls.Add(lblPlaceholder)
        End If

        Dim lblName As New Label() With {
            .Text = productName,
            .Location = New Point(8, pic.Bottom + 6),
            .Size = New Size(ProductCardWidth - 16, 34),
            .Font = UiTheme.FontBodyBold,
            .ForeColor = UiTheme.ColTextPrimary
        }

        Dim lblPrice As New Label() With {
            .Text = sym & entry.UnitPrice.ToString("N2", CultureInfo.CurrentCulture),
            .Location = New Point(8, lblName.Bottom + 2),
            .AutoSize = True,
            .Font = UiTheme.FontBodySmall,
            .ForeColor = UiTheme.ColPrimary
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
            UpdateAddButtonState()
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
            UpdateAddButtonState()
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

        Dim searchText As String = String.Empty
        If txtProductSearch IsNot Nothing Then
            searchText = txtProductSearch.Text.Trim()
        End If

        For Each productName As String In names
            Dim entry As ProductCatalogEntry = productCatalog(productName)
            If entry.StockQuantity <= 0 Then
                Continue For
            End If

            If Not CategoryFilterMatches(sel, entry.CategoryId) Then
                Continue For
            End If

            If searchText.Length > 0 AndAlso productName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0 Then
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
        Dim grandTotal As Decimal = GetGrandTotal()
        lblTotal.Text = FormatMoney(grandTotal)
        lblChangeValue.Text = FormatMoney(GetChangeDue())

        Dim tendered As Decimal
        If dgvProducts.Rows.Count > 0 AndAlso
            Decimal.TryParse(txtAmountTendered.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, tendered) AndAlso
            tendered < grandTotal Then
            lblChangeValue.ForeColor = UiTheme.ColDanger
        Else
            lblChangeValue.ForeColor = UiTheme.ColAccent
        End If

        UpdateCartEmptyState()
        UpdateFinalizeButtonState()
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
