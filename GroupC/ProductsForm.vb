Imports System.Collections.Generic
Imports System.Data
Imports System.Globalization
Imports System.IO
Imports System.Drawing
Imports System.Text
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient

Public Class ProductsForm

    Private Class CategoryEditOption
        Public Property CategoryId As Integer?
        Public Property Display As String = String.Empty

        Public Overrides Function ToString() As String
            Return Display
        End Function
    End Class

    Private Class GridCategoryFilterOption
        Public Enum KindEnum
            AllCategories = 0
            Uncategorized = 1
            SpecificCategory = 2
        End Enum

        Public Property Kind As KindEnum = KindEnum.AllCategories
        Public Property CategoryId As Integer?
        Public Property Display As String = String.Empty

        Public Overrides Function ToString() As String
            Return Display
        End Function
    End Class

    Private Const MinProductPrice As Decimal = 0.01D
    Private Const MaxProductPrice As Decimal = 999999.99D
    Private Const MaxProductNameLength As Integer = 100
    Private Const MaxSearchLength As Integer = 100
    Private Const MaxStockQuantity As Integer = 999999
    Private Const DefaultStockQuantity As Integer = 100
    Private Const SidebarButtonGap As Integer = 8
    Private Const ProductImageHeight As Integer = 180
    Private Const GridFilterComboWidth As Integer = 190
    Private Const GridStatusFilterWidth As Integer = 190
    Private Const GridActiveColumnWidth As Integer = 100
    Private Const GridPriceColumnWidth As Integer = 108
    Private Const GridStockColumnWidth As Integer = 80
    Private Const GridProductFillWeight As Integer = 230
    Private Const GridProductMinWidth As Integer = 150
    Private Const GridCategoryFixedWidth As Integer = 140

    Private Enum ProductFilterMode
        ActiveOnly = 0
        AllProducts = 1
        InactiveOnly = 2
    End Enum

    Private WithEvents txtProductName As TextBox
    Private WithEvents numPrice As NumericUpDown
    Private WithEvents numStock As NumericUpDown
    Private WithEvents cmbCategory As ComboBox
    Private WithEvents txtSearch As TextBox
    Private WithEvents dgvProducts As DataGridView
    Private lblGridMessage As Label
    Private formToolTips As ToolTip

    Private WithEvents cmbFilter As ComboBox
    Private WithEvents btnReactivate As Button

    Private WithEvents btnAdd As Button
    Private WithEvents btnUpdate As Button
    Private WithEvents btnDelete As Button
    Private WithEvents btnDeactivate As Button
    Private WithEvents btnRefresh As Button
    Private WithEvents btnImportCsv As Button
    Private WithEvents btnImportPdf As Button
    Private WithEvents btnImportTxt As Button
    Private WithEvents btnPrintCopy As Button
    Private WithEvents btnBack As Button
    Private WithEvents btnManageCategories As Button

    Private WithEvents cmbGridCategoryFilter As ComboBox

    Private picProductImage As PictureBox
    Private WithEvents btnChooseImage As Button
    Private WithEvents btnRemoveImage As Button

    Private pendingImageSourcePath As String
    Private clearImageOnSave As Boolean

    Private statusStrip As StatusStrip
    Private statusLabel As ToolStripStatusLabel
    Private WithEvents statusClearTimer As Timer

    Private lblProductsInputError As Label

    Private productsTable As DataTable
    Private productsView As DataView

    Private suppressProductFilterEvents As Boolean

    Private suppressGridCategoryFilterEvents As Boolean

    Private productGridLayoutPending As Boolean

    Private Sub ProductsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = AppBranding.WindowTitle("Manage Products")
        UiTheme.ApplyMaximizedWorkspaceDefaults(Me, 860, 580)

        Try
            UiTheme.ApplyStandardWindowChrome(Me)
        Catch
        End Try

        statusClearTimer = New Timer() With {.Interval = FormStatusHelper.StatusShowMilliseconds}

        Try
            DatabaseInitializer.EnsureDatabase()
        Catch
        End Try

        CreateControls()
        LoadProducts()
    End Sub

    Private Sub CreateControls()
        Me.SuspendLayout()
        Me.Controls.Clear()
        Me.BackColor = UiTheme.ColBackground

        txtProductName = New TextBox() With {
            .Dock = DockStyle.Fill,
            .MaxLength = MaxProductNameLength,
            .Font = UiTheme.FontBody
        }
        numPrice = New NumericUpDown() With {
            .Dock = DockStyle.Fill,
            .DecimalPlaces = 2,
            .Minimum = MinProductPrice,
            .Maximum = MaxProductPrice,
            .TextAlign = HorizontalAlignment.Right,
            .ThousandsSeparator = True,
            .Font = UiTheme.FontBody
        }
        numStock = New NumericUpDown() With {
            .Dock = DockStyle.Fill,
            .Minimum = 0D,
            .Maximum = MaxStockQuantity,
            .Value = DefaultStockQuantity,
            .TextAlign = HorizontalAlignment.Right,
            .ThousandsSeparator = True,
            .Font = UiTheme.FontBody
        }
        cmbCategory = New ComboBox() With {
            .Dock = DockStyle.Fill,
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = UiTheme.FontBody
        }

        Try
            UiTheme.ApplyTableLayoutSingleLineTextBox(txtProductName)
            UiTheme.ApplyTableLayoutDropDown(cmbCategory)
            ApplyTableLayoutNumeric(numPrice)
            ApplyTableLayoutNumeric(numStock)
        Catch
        End Try

        txtSearch = New TextBox() With {
            .PlaceholderText = "Search products...",
            .MaxLength = MaxSearchLength,
            .Font = UiTheme.FontBody
        }
        Try
            UiTheme.ApplyTableLayoutSingleLineTextBox(txtSearch)
            txtSearch.Dock = DockStyle.Fill
        Catch
        End Try
        cmbFilter = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Width = GridStatusFilterWidth,
            .Font = UiTheme.FontBody
        }
        cmbFilter.Items.AddRange(New Object() {"Active products only", "All products", "Inactive only"})
        cmbGridCategoryFilter = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Width = GridFilterComboWidth,
            .Font = UiTheme.FontBody
        }
        Try
            UiTheme.ApplyTableLayoutDropDown(cmbFilter)
            UiTheme.ApplyTableLayoutDropDown(cmbGridCategoryFilter)
        Catch
        End Try
        suppressProductFilterEvents = True

        btnAdd = New Button() With {
            .Text = "&Add Product",
            .AutoSize = True,
            .MinimumSize = New Size(120, UiTheme.ButtonHeightMd),
            .Cursor = Cursors.Hand
        }
        btnUpdate = New Button() With {
            .Text = "&Update",
            .AutoSize = True,
            .MinimumSize = New Size(100, UiTheme.ButtonHeightMd),
            .Cursor = Cursors.Hand
        }
        btnDelete = New Button() With {
            .Text = "&Delete",
            .AutoSize = True,
            .MinimumSize = New Size(100, UiTheme.ButtonHeightMd),
            .Cursor = Cursors.Hand
        }
        btnDeactivate = New Button() With {
            .Text = "&Deactivate",
            .AutoSize = True,
            .MinimumSize = New Size(100, UiTheme.ButtonHeightMd),
            .Cursor = Cursors.Hand
        }
        btnReactivate = New Button() With {
            .Text = "Reactivate",
            .AutoSize = True,
            .MinimumSize = New Size(100, UiTheme.ButtonHeightMd),
            .Enabled = False,
            .Cursor = Cursors.Hand
        }
        btnRefresh = New Button() With {
            .Text = "Refresh",
            .AutoSize = True,
            .MinimumSize = New Size(90, UiTheme.ButtonHeightSm),
            .Cursor = Cursors.Hand
        }
        btnImportCsv = New Button() With {
            .Text = "Import CSV",
            .AutoSize = True,
            .MinimumSize = New Size(100, UiTheme.ButtonHeightSm),
            .Cursor = Cursors.Hand
        }
        btnImportPdf = New Button() With {
            .Text = "Import to PDF",
            .AutoSize = True,
            .MinimumSize = New Size(100, UiTheme.ButtonHeightSm),
            .Cursor = Cursors.Hand
        }
        btnImportTxt = New Button() With {
            .Text = "Import to txt file",
            .AutoSize = True,
            .MinimumSize = New Size(100, UiTheme.ButtonHeightSm),
            .Cursor = Cursors.Hand
        }
        btnPrintCopy = New Button() With {
            .Text = "Print copy",
            .AutoSize = True,
            .MinimumSize = New Size(100, UiTheme.ButtonHeight),
            .Cursor = Cursors.Hand
        }
        btnManageCategories = New Button() With {
            .Text = "Manage &categories…",
            .AutoSize = True,
            .MinimumSize = New Size(140, UiTheme.ButtonHeightSm),
            .Cursor = Cursors.Hand
        }
        picProductImage = New PictureBox() With {
            .Dock = DockStyle.Fill,
            .Height = 130,
            .SizeMode = PictureBoxSizeMode.Zoom,
            .BackColor = UiTheme.SurfaceVariant,
            .BorderStyle = BorderStyle.FixedSingle
        }
        btnChooseImage = New Button() With {
            .Text = "Choose image…",
            .AutoSize = True,
            .MinimumSize = New Size(120, UiTheme.ButtonHeightSm),
            .Cursor = Cursors.Hand
        }
        btnRemoveImage = New Button() With {
            .Text = "Remove image",
            .AutoSize = True,
            .MinimumSize = New Size(120, UiTheme.ButtonHeightSm),
            .Cursor = Cursors.Hand
        }

        ConfigureSidebarButton(btnAdd)
        ConfigureSidebarButton(btnUpdate)
        ConfigureSidebarButton(btnDelete)
        ConfigureSidebarButton(btnDeactivate)
        ConfigureSidebarButton(btnReactivate)
        ConfigureSidebarButton(btnImportCsv)
        ConfigureSidebarButton(btnImportPdf)
        ConfigureSidebarButton(btnImportTxt)
        ConfigureSidebarButton(btnPrintCopy)
        ConfigureSidebarButton(btnManageCategories)
        ConfigureSidebarSmallButton(btnRefresh)
        ConfigureSidebarSmallButton(btnChooseImage)
        ConfigureSidebarSmallButton(btnRemoveImage)

        Try
            UiTheme.ApplyPrimaryButton(btnAdd)
            UiTheme.ApplyPrimaryButton(btnUpdate)
            UiTheme.ApplyDangerButton(btnDelete)
            UiTheme.ApplyWarningButton(btnDeactivate)
            UiTheme.ApplySuccessButton(btnReactivate)
            UiTheme.ApplySecondaryButton(btnRefresh)
            UiTheme.ApplyPrimaryButton(btnImportCsv)
            UiTheme.ApplySecondaryButton(btnImportPdf)
            UiTheme.ApplySecondaryButton(btnImportTxt)
            UiTheme.ApplySecondaryButton(btnPrintCopy)
            UiTheme.ApplySecondaryAccentButton(btnManageCategories)
            UiTheme.ApplySecondaryButton(btnChooseImage)
            UiTheme.ApplySecondaryButton(btnRemoveImage)
        Catch
        End Try

        dgvProducts = New DataGridView() With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AllowUserToResizeColumns = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            .BackgroundColor = UiTheme.ColSurface,
            .BorderStyle = BorderStyle.None,
            .ScrollBars = ScrollBars.Both,
            .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        }
        Try
            UiTheme.ApplyReadOnlyGridTheme(dgvProducts)
        Catch
        End Try

        lblGridMessage = UiTheme.CreateEmptyStateLabel("No products match the current filters.")
        lblGridMessage.Visible = False
        lblProductsInputError = New Label() With {.AutoSize = True, .ForeColor = UiTheme.Danger, .Visible = False, .Padding = New Padding(0, 10, 0, 10)}

        statusStrip = New StatusStrip()
        statusLabel = New ToolStripStatusLabel(FormStatusHelper.ReadyText) With {.Spring = True, .TextAlign = ContentAlignment.MiddleLeft}
        statusStrip.Items.Add(statusLabel)
        Try
            UiTheme.ApplyStatusStripTheme(statusStrip)
        Catch
        End Try

        Dim inputLayout As New TableLayoutPanel() With {
            .AutoSize = True,
            .ColumnCount = 1,
            .RowCount = 14,
            .Margin = New Padding(0, UiTheme.PadControl, 0, UiTheme.PadControl),
            .Dock = DockStyle.Top
        }
        inputLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        ConfigureProductInputRowStyles(inputLayout)

        inputLayout.Controls.Add(CreateFieldLabel("Product Name", isFirst:=True), 0, 0)
        inputLayout.Controls.Add(txtProductName, 0, 1)
        inputLayout.Controls.Add(CreateFieldLabel("Price (" & AppSettings.Current.CurrencySymbol & ")"), 0, 2)
        inputLayout.Controls.Add(numPrice, 0, 3)
        inputLayout.Controls.Add(CreateFieldLabel("Stock quantity"), 0, 4)
        inputLayout.Controls.Add(numStock, 0, 5)
        inputLayout.Controls.Add(CreateFieldLabel("Category"), 0, 6)
        inputLayout.Controls.Add(cmbCategory, 0, 7)
        inputLayout.Controls.Add(CreateFieldLabel("Product image"), 0, 8)

        Dim picHost As New Panel() With {
            .Dock = DockStyle.Top,
            .Height = ProductImageHeight,
            .Margin = New Padding(0, 0, 0, UiTheme.PadTight),
            .BackColor = UiTheme.SurfaceVariant
        }
        picHost.Controls.Add(picProductImage)
        inputLayout.Controls.Add(picHost, 0, 9)

        Dim pnlImageActions As New TableLayoutPanel() With {
            .AutoSize = True,
            .ColumnCount = 2,
            .RowCount = 1,
            .Margin = New Padding(0, UiTheme.PadTight, 0, UiTheme.PadSection),
            .Dock = DockStyle.Top
        }
        pnlImageActions.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        pnlImageActions.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        pnlImageActions.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        btnChooseImage.Dock = DockStyle.Fill
        btnRemoveImage.Dock = DockStyle.Fill

        btnChooseImage.Margin = New Padding(0, 0, UiTheme.PadTight, 0)
        btnRemoveImage.Margin = New Padding(UiTheme.PadTight, 0, 0, 0)

        pnlImageActions.Controls.Add(btnChooseImage, 0, 0)
        pnlImageActions.Controls.Add(btnRemoveImage, 1, 0)
        inputLayout.Controls.Add(pnlImageActions, 0, 10)

        Dim actionGrid As New TableLayoutPanel() With {
            .AutoSize = True,
            .ColumnCount = 2,
            .RowCount = 3,
            .Margin = New Padding(0, UiTheme.PadTight, 0, 0),
            .Dock = DockStyle.Top
        }
        actionGrid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        actionGrid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        actionGrid.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        actionGrid.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        actionGrid.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        btnAdd.Dock = DockStyle.Fill
        btnUpdate.Dock = DockStyle.Fill
        btnDelete.Dock = DockStyle.None
        btnDelete.Anchor = AnchorStyles.None
        btnDeactivate.Dock = DockStyle.Fill
        btnReactivate.Dock = DockStyle.Fill

        btnAdd.Margin = New Padding(0, 0, UiTheme.PadTight, UiTheme.PadControl)
        btnUpdate.Margin = New Padding(UiTheme.PadTight, 0, 0, UiTheme.PadControl)
        btnDelete.Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        btnDeactivate.Margin = New Padding(0, 0, UiTheme.PadTight, 0)
        btnReactivate.Margin = New Padding(UiTheme.PadTight, 0, 0, 0)

        actionGrid.Controls.Add(btnAdd, 0, 0)
        actionGrid.Controls.Add(btnUpdate, 1, 0)
        actionGrid.Controls.Add(btnDelete, 0, 1)
        actionGrid.SetColumnSpan(btnDelete, 2)
        actionGrid.Controls.Add(btnDeactivate, 0, 2)
        actionGrid.Controls.Add(btnReactivate, 1, 2)
        inputLayout.Controls.Add(actionGrid, 0, 11)

        inputLayout.Controls.Add(UiTheme.CreateSectionHeader("Utility tools"), 0, 12)

        Dim pnlUtilities As New TableLayoutPanel() With {
            .AutoSize = True,
            .ColumnCount = 2,
            .RowCount = 3,
            .Margin = New Padding(0, 0, 0, UiTheme.PadSection),
            .Dock = DockStyle.Top
        }
        pnlUtilities.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        pnlUtilities.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        pnlUtilities.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        pnlUtilities.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        pnlUtilities.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        btnManageCategories.Dock = DockStyle.Fill
        btnImportCsv.Dock = DockStyle.Fill
        btnImportPdf.Dock = DockStyle.Fill
        btnImportTxt.Dock = DockStyle.Fill
        btnPrintCopy.Dock = DockStyle.Fill

        btnManageCategories.Margin = New Padding(0, 0, UiTheme.PadTight, UiTheme.PadControl)
        btnImportCsv.Margin = New Padding(UiTheme.PadTight, 0, 0, UiTheme.PadControl)
        btnImportPdf.Margin = New Padding(0, 0, UiTheme.PadTight, UiTheme.PadControl)
        btnImportTxt.Margin = New Padding(UiTheme.PadTight, 0, 0, UiTheme.PadControl)
        btnPrintCopy.Margin = New Padding(0, 0, UiTheme.PadTight, 0)

        pnlUtilities.Controls.Add(btnManageCategories, 0, 0)
        pnlUtilities.Controls.Add(btnImportCsv, 1, 0)
        pnlUtilities.Controls.Add(btnImportPdf, 0, 1)
        pnlUtilities.Controls.Add(btnImportTxt, 1, 1)
        pnlUtilities.Controls.Add(btnPrintCopy, 0, 2)
        inputLayout.Controls.Add(pnlUtilities, 0, 13)

        Dim sidebarBody As New Panel() With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .Padding = Padding.Empty
        }
        sidebarBody.Controls.Add(inputLayout)
        AddHandler sidebarBody.Resize,
            Sub()
                inputLayout.Width = Math.Max(0, sidebarBody.ClientSize.Width - SystemInformation.VerticalScrollBarWidth)
            End Sub

        Dim toolbar As New TableLayoutPanel() With {
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .ColumnCount = 5,
            .RowCount = 1,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        }
        toolbar.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 35.0F))
        toolbar.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 22.0F))
        toolbar.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 22.0F))
        toolbar.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 21.0F))
        toolbar.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        toolbar.RowStyles.Add(New RowStyle(SizeType.Absolute, ToolbarRowHeight()))

        txtSearch.Dock = DockStyle.Fill
        cmbGridCategoryFilter.Dock = DockStyle.Fill
        cmbFilter.Dock = DockStyle.Fill
        btnRefresh.Dock = DockStyle.Fill

        txtSearch.Margin = New Padding(0, 0, UiTheme.PadControl, 0)
        cmbGridCategoryFilter.Margin = New Padding(0, 0, UiTheme.PadControl, 0)
        cmbFilter.Margin = New Padding(0, 0, UiTheme.PadControl, 0)
        btnRefresh.Margin = Padding.Empty

        toolbar.Controls.Add(txtSearch, 0, 0)
        toolbar.Controls.Add(cmbGridCategoryFilter, 1, 0)
        toolbar.Controls.Add(cmbFilter, 2, 0)
        toolbar.Controls.Add(New Panel(), 3, 0)
        toolbar.Controls.Add(btnRefresh, 4, 0)

        Dim gridContainer As New Panel() With {.Dock = DockStyle.Fill}
        Dim gridCard As Panel = UiTheme.CreateCard()
        gridCard.Dock = DockStyle.Fill
        Dim gridCardHost As Panel = gridCard
        Try
            gridCardHost = UiTheme.GetCardContentHost(gridCard)
        Catch
        End Try
        dgvProducts.Dock = DockStyle.Fill
        gridCardHost.Controls.Add(dgvProducts)
        gridContainer.Controls.Add(gridCard)
        lblGridMessage.Dock = DockStyle.Fill
        gridContainer.Controls.Add(lblGridMessage)

        Dim inventoryLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .Margin = Padding.Empty
        }
        inventoryLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        inventoryLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        inventoryLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        inventoryLayout.Controls.Add(UiTheme.CreateSectionHeader("Inventory overview"), 0, 0)
        inventoryLayout.Controls.Add(toolbar, 0, 1)
        inventoryLayout.Controls.Add(gridContainer, 0, 2)

        ' -----------------------------------------------------------
        ' SHARED SHELL + PRODUCTS SPLIT LAYOUT
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
            ("Manage Products", True),
            ("Manage Categories", False),
            ("Manage Cashiers", False),
            ("Point of Sale", False),
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
        btnBack = UiTheme.CreateSidebarNavButton("← Back to Menu")
        btnBack.Dock = DockStyle.Top
        navBottom.Controls.Add(btnBack)

        Dim sidebarTop As New Panel() With {.AutoSize = True, .Dock = DockStyle.Top, .BackColor = Color.Transparent}
        sidebarTop.Controls.Add(navMain)
        sidebarTop.Controls.Add(lblSidebarStore)

        sidebarStack.Controls.Add(sidebarTop, 0, 0)
        sidebarStack.Controls.Add(UiTheme.CreateSidebarSpacer(), 0, 1)
        sidebarStack.Controls.Add(navBottom, 0, 2)
        sidebar.Controls.Add(sidebarStack)

        Dim rightColumn As New Panel() With {.Dock = DockStyle.Fill, .BackColor = UiTheme.ColBackground}
        Dim topBar As Panel = UiTheme.CreateTopBar("Manage Products", AppSession.GetAuditIdentity())
        Dim contentArea As Panel = UiTheme.CreateContentArea()

        Dim productsSplit As SplitContainer = UiTheme.CreateVerticalSplit()

        Dim editorCard As Panel = UiTheme.CreateCard()
        editorCard.Dock = DockStyle.Fill
        Dim editorCardHost As Panel = editorCard
        Try
            editorCardHost = UiTheme.GetCardContentHost(editorCard)
        Catch
        End Try

        Dim editorLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .Margin = Padding.Empty
        }
        editorLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        editorLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        editorLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        editorLayout.Controls.Add(UiTheme.CreateSectionHeader("Product editor"), 0, 0)
        lblProductsInputError.Dock = DockStyle.Top
        lblProductsInputError.Margin = New Padding(0, UiTheme.PadControl, 0, 0)
        editorLayout.Controls.Add(lblProductsInputError, 0, 1)
        editorLayout.Controls.Add(sidebarBody, 0, 2)
        editorCardHost.Controls.Add(editorLayout)
        productsSplit.Panel1.Controls.Add(editorCard)

        Dim inventoryCard As Panel = UiTheme.CreateCard()
        inventoryCard.Dock = DockStyle.Fill
        Dim inventoryCardHost As Panel = inventoryCard
        Try
            inventoryCardHost = UiTheme.GetCardContentHost(inventoryCard)
        Catch
        End Try
        inventoryCardHost.Controls.Add(inventoryLayout)
        productsSplit.Panel2.Controls.Add(inventoryCard)

        contentArea.Controls.Add(productsSplit)
        rightColumn.Controls.Add(contentArea)
        rightColumn.Controls.Add(topBar)

        rootTable.Controls.Add(sidebar, 0, 0)
        rootTable.Controls.Add(rightColumn, 1, 0)

        Me.Controls.Add(rootTable)
        Me.Controls.Add(statusStrip)

        AddHandler productsSplit.SplitterMoved, Sub(s, ev) ConfigureProductsSplit(productsSplit)
        AddHandler Me.Resize, Sub(s, ev) ConfigureProductsSplit(productsSplit)

        cmbFilter.SelectedIndex = 0
        suppressProductFilterEvents = False
        RefreshReactivateButtonAppearance()

        formToolTips = UiTheme.CreateStandardToolTip()
        formToolTips.SetToolTip(btnUpdate, "Save changes to the selected product")
        formToolTips.SetToolTip(btnDeactivate, "Hide this product from active lists")
        formToolTips.SetToolTip(btnReactivate, "Show this product in active lists again")
        formToolTips.SetToolTip(txtSearch, "Filter the product list by name")
        formToolTips.SetToolTip(btnRefresh, "Reload products from the database")

        UiTheme.AssignTabOrder(
            txtProductName,
            cmbCategory,
            numPrice,
            numStock,
            btnAdd,
            btnUpdate,
            btnDeactivate,
            btnReactivate,
            txtSearch,
            cmbGridCategoryFilter,
            cmbFilter,
            btnRefresh,
            dgvProducts,
            btnBack)

        Me.ResumeLayout(True)
        AddHandler Me.Shown, Sub(s, ev) ConfigureProductsSplit(productsSplit)
        inputLayout.Width = Math.Max(0, sidebarBody.ClientSize.Width - SystemInformation.VerticalScrollBarWidth)
    End Sub

    Private Sub ConfigureProductsSplit(productsSplit As SplitContainer)
        UiTheme.ConfigureSplitDistance(productsSplit, 0.38R, 280, 320)
    End Sub

    Private Sub cmbFilter_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbFilter.SelectedIndexChanged
        If suppressProductFilterEvents Then
            Return
        End If

        If cmbFilter Is Nothing OrElse cmbFilter.SelectedIndex < 0 Then
            Return
        End If

        LoadProducts()
    End Sub

    Private Sub dgvProducts_SelectionChanged(sender As Object, e As EventArgs) Handles dgvProducts.SelectionChanged
        UpdateReactivateEnabled()
    End Sub

    Private Sub UpdateReactivateEnabled()
        Dim hasSelection As Boolean = dgvProducts IsNot Nothing AndAlso dgvProducts.SelectedRows.Count > 0
        Dim isActive As Boolean = True

        If hasSelection AndAlso dgvProducts.Columns.Contains("is_active") Then
            Dim activeVal As Object = dgvProducts.SelectedRows(0).Cells("is_active").Value
            If activeVal IsNot Nothing AndAlso activeVal IsNot DBNull.Value Then
                isActive = Convert.ToBoolean(activeVal)
            End If
        Else
            hasSelection = False
        End If

        UiTheme.SetSelectionButtonState(btnUpdate, hasSelection, AddressOf UiTheme.ApplyPrimaryButton)
        UiTheme.SetSelectionButtonState(btnDeactivate, hasSelection AndAlso isActive, AddressOf UiTheme.ApplyWarningButton)
        UiTheme.SetSelectionButtonState(btnReactivate, hasSelection AndAlso Not isActive, AddressOf UiTheme.ApplySuccessButton)
    End Sub

    Private Sub RefreshReactivateButtonAppearance()
        UpdateReactivateEnabled()
    End Sub

    Private Sub dgvProducts_RowPrePaint(sender As Object, e As DataGridViewRowPrePaintEventArgs) Handles dgvProducts.RowPrePaint
        If e.RowIndex < 0 Then
            Return
        End If

        If Not dgvProducts.Columns.Contains("is_active") Then
            Return
        End If

        Dim val As Object = dgvProducts.Rows(e.RowIndex).Cells("is_active").Value
        If val Is Nothing OrElse val Is DBNull.Value Then
            Return
        End If

        If Not Convert.ToBoolean(val) Then
            dgvProducts.Rows(e.RowIndex).DefaultCellStyle.BackColor = UiTheme.InactiveRowBack
            dgvProducts.Rows(e.RowIndex).DefaultCellStyle.ForeColor = UiTheme.InactiveRowFore
        Else
            dgvProducts.Rows(e.RowIndex).DefaultCellStyle.BackColor = Color.Empty
            dgvProducts.Rows(e.RowIndex).DefaultCellStyle.ForeColor = Color.Empty
        End If
    End Sub

    Private Sub statusClearTimer_Tick(sender As Object, e As EventArgs) Handles statusClearTimer.Tick
        statusClearTimer.Stop()
        FormStatusHelper.ResetTimedStatus(statusLabel)
    End Sub

    Private Sub ShowStatus(message As String, isError As Boolean)
        FormStatusHelper.ShowTimedStatus(statusLabel, statusClearTimer, message, isError)
    End Sub

    Private Sub txtSearch_TextChanged(sender As Object, e As EventArgs) Handles txtSearch.TextChanged
        ClearProductsInputError()
        ApplyCombinedFilter()
    End Sub

    Private Sub cmbGridCategoryFilter_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbGridCategoryFilter.SelectedIndexChanged
        If suppressGridCategoryFilterEvents Then
            Return
        End If

        ClearProductsInputError()
        ApplyCombinedFilter()
    End Sub

    Private Sub cmbCategory_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbCategory.SelectedIndexChanged
        ClearProductsInputError()
    End Sub

    Private Sub ApplyCombinedFilter()
        If productsView Is Nothing Then
            Return
        End If

        Dim parts As New List(Of String)()

        Dim term As String = txtSearch.Text.Trim()
        If term.Length > 0 Then
            Dim safe As String = term.Replace("'", "''").Replace("*", "[*]").Replace("%", "[%]")
            parts.Add("product_name LIKE '%" & safe & "%'")
        End If

        Dim gf As GridCategoryFilterOption = TryCast(cmbGridCategoryFilter.SelectedItem, GridCategoryFilterOption)
        If gf IsNot Nothing Then
            Select Case gf.Kind
                Case GridCategoryFilterOption.KindEnum.Uncategorized
                    parts.Add("(category_id IS NULL)")
                Case GridCategoryFilterOption.KindEnum.SpecificCategory
                    If gf.CategoryId.HasValue Then
                        parts.Add("category_id = " & gf.CategoryId.Value.ToString(CultureInfo.InvariantCulture))
                    End If
            End Select
        End If

        If parts.Count = 0 Then
            productsView.RowFilter = String.Empty
        Else
            productsView.RowFilter = String.Join(" AND ", parts)
        End If
    End Sub

    Private Sub dgvProducts_DataBindingComplete(sender As Object, e As DataGridViewBindingCompleteEventArgs) Handles dgvProducts.DataBindingComplete
        FormatProductColumns()
        UpdateReactivateEnabled()
    End Sub

    Private Sub dgvProducts_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs) Handles dgvProducts.CellFormatting
        If e.RowIndex < 0 Then
            Return
        End If

        Dim dgv As DataGridView = TryCast(sender, DataGridView)
        If dgv Is Nothing OrElse dgv.Columns.Count = 0 Then
            Return
        End If

        Dim col As DataGridViewColumn = dgv.Columns(e.ColumnIndex)
        If col Is Nothing OrElse Not String.Equals(col.Name, "stock_quantity", StringComparison.OrdinalIgnoreCase) Then
            Return
        End If

        If e.Value Is Nothing OrElse e.Value Is DBNull.Value Then
            Return
        End If

        Try
            Dim stock As Integer = Convert.ToInt32(e.Value)

            ' Check if stock is less than or equal to 5
            If stock <= 5 Then
                ' You can use Color.Red directly, or keep using UiTheme.Danger if it is already red
                e.CellStyle.ForeColor = Color.Red

                ' Optional: Keep it red even if the user clicks/selects the row
                e.CellStyle.SelectionForeColor = Color.Red
            Else
                ' CRITICAL: Reset the color for stocks > 5 so scrolling doesn't glitch the colors
                e.CellStyle.ForeColor = dgv.DefaultCellStyle.ForeColor
                e.CellStyle.SelectionForeColor = dgv.DefaultCellStyle.SelectionForeColor
            End If
        Catch
        End Try
    End Sub

    Private Sub FormatProductColumns()
        If dgvProducts.Columns.Count = 0 Then
            Return
        End If

        GridDisplayHelper.ApplyStandardBoundGridDisplay(dgvProducts)

        Dim sym As String = AppSettings.Current.CurrencySymbol
        Dim priceHeader As String = "Price (" & sym & ")"
        Dim priceColumnWidth As Integer = GetInventoryPriceColumnWidth(priceHeader)

        If dgvProducts.Columns.Contains("is_active") Then
            Dim activeCol As DataGridViewColumn = dgvProducts.Columns("is_active")
            activeCol.HeaderText = "Active"
            ConfigureInventoryGridFixedColumn(activeCol, GridActiveColumnWidth, DataGridViewContentAlignment.MiddleCenter, 0)
        End If

        If dgvProducts.Columns.Contains("product_name") Then
            Dim productCol As DataGridViewColumn = dgvProducts.Columns("product_name")
            productCol.HeaderText = "Product"
            ConfigureInventoryGridFillColumn(productCol, GridProductFillWeight, GridProductMinWidth, DataGridViewContentAlignment.MiddleLeft, 1)
        End If

        If dgvProducts.Columns.Contains("price") Then
            Dim priceCol As DataGridViewColumn = dgvProducts.Columns("price")
            priceCol.HeaderText = priceHeader
            priceCol.DefaultCellStyle.Format = "N2"
            ConfigureInventoryGridFixedColumn(priceCol, priceColumnWidth, DataGridViewContentAlignment.MiddleRight, 2)
        End If

        If dgvProducts.Columns.Contains("stock_quantity") Then
            Dim stockCol As DataGridViewColumn = dgvProducts.Columns("stock_quantity")
            stockCol.HeaderText = "Stock"
            ConfigureInventoryGridFixedColumn(stockCol, GridStockColumnWidth, DataGridViewContentAlignment.MiddleCenter, 3)
        End If

        If dgvProducts.Columns.Contains("category_name") Then
            Dim categoryCol As DataGridViewColumn = dgvProducts.Columns("category_name")
            categoryCol.HeaderText = "Category"
            ConfigureInventoryGridFixedColumn(categoryCol, GridCategoryFixedWidth, DataGridViewContentAlignment.MiddleLeft, 4)
        End If

        If dgvProducts.Columns.Contains("image_path") Then
            dgvProducts.Columns("image_path").Visible = False
        End If

        If dgvProducts.Columns.Contains("category_id") Then
            dgvProducts.Columns("category_id").Visible = False
        End If
    End Sub

    ''' <summary>
    ''' Defers width-sensitive Fill layout until after the form and grid have finished layout.
    ''' Coalesces duplicate requests from DataBindingComplete and Shown into one pass.
    ''' </summary>
    Private Sub ScheduleProductGridColumnLayout()
        If dgvProducts Is Nothing OrElse dgvProducts.IsDisposed OrElse dgvProducts.Columns.Count = 0 Then
            Return
        End If

        If Not Me.IsHandleCreated OrElse Not dgvProducts.IsHandleCreated Then
            Return
        End If

        If productGridLayoutPending Then
            Return
        End If

        Try
            productGridLayoutPending = True
            BeginInvoke(New MethodInvoker(
                Sub()
                    Try
                        ApplyProductGridColumnLayout()
                    Finally
                        productGridLayoutPending = False
                    End Try
                End Sub))
        Catch
            productGridLayoutPending = False
            Throw
        End Try
    End Sub

    ''' <summary>
    ''' Keeps Active, Price, Stock, and Category at readable fixed widths; Product fills
    ''' the rest so the grid never overflows horizontally (which clips header text).
    ''' </summary>
    Private Sub ApplyProductGridColumnLayout()
        If dgvProducts Is Nothing OrElse dgvProducts.IsDisposed OrElse dgvProducts.Columns.Count = 0 Then
            Return
        End If

        Dim available As Integer = dgvProducts.ClientSize.Width
        If available <= 0 Then
            Return
        End If

        If dgvProducts.DisplayedRowCount(False) < dgvProducts.RowCount Then
            available -= SystemInformation.VerticalScrollBarWidth
        End If

        dgvProducts.SuspendLayout()
        Try
            dgvProducts.ScrollBars = ScrollBars.Vertical
            dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            dgvProducts.HorizontalScrollingOffset = 0

            ' Tighter header padding so labels like "Active" and "Category" fit narrow columns.
            dgvProducts.ColumnHeadersDefaultCellStyle.Padding = New Padding(6, 0, 6, 0)

            Dim activeWidth As Integer = GetRequiredHeaderWidth("is_active", GridActiveColumnWidth)
            Dim priceWidth As Integer = GetRequiredHeaderWidth("price", GridPriceColumnWidth)
            Dim stockWidth As Integer = GetRequiredHeaderWidth("stock_quantity", GridStockColumnWidth)
            Dim categoryWidth As Integer = GetRequiredHeaderWidth("category_name", GridCategoryFixedWidth)

            If dgvProducts.Columns.Contains("is_active") Then
                Dim activeCol As DataGridViewColumn = dgvProducts.Columns("is_active")
                activeCol.DisplayIndex = 0
                activeCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                activeCol.Width = activeWidth
                activeCol.MinimumWidth = 68
                activeCol.SortMode = DataGridViewColumnSortMode.NotSortable
            End If

            If dgvProducts.Columns.Contains("product_name") Then
                Dim productCol As DataGridViewColumn = dgvProducts.Columns("product_name")
                productCol.DisplayIndex = 1
                productCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                productCol.FillWeight = 200
                productCol.MinimumWidth = 80
            End If

            If dgvProducts.Columns.Contains("price") Then
                Dim priceCol As DataGridViewColumn = dgvProducts.Columns("price")
                priceCol.DisplayIndex = 2
                priceCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                priceCol.Width = priceWidth
                priceCol.MinimumWidth = 92
                priceCol.SortMode = DataGridViewColumnSortMode.NotSortable
            End If

            If dgvProducts.Columns.Contains("stock_quantity") Then
                Dim stockCol As DataGridViewColumn = dgvProducts.Columns("stock_quantity")
                stockCol.DisplayIndex = 3
                stockCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                stockCol.Width = stockWidth
                stockCol.MinimumWidth = 64
                stockCol.SortMode = DataGridViewColumnSortMode.NotSortable
            End If

            If dgvProducts.Columns.Contains("category_name") Then
                Dim categoryCol As DataGridViewColumn = dgvProducts.Columns("category_name")
                categoryCol.DisplayIndex = 4
                categoryCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                categoryCol.Width = categoryWidth
                categoryCol.MinimumWidth = 96
                categoryCol.SortMode = DataGridViewColumnSortMode.NotSortable
            End If

            Dim usedWidth As Integer = 0
            For Each col As DataGridViewColumn In dgvProducts.Columns
                If col.Visible Then
                    usedWidth += col.Width
                End If
            Next

            If dgvProducts.Columns.Contains("product_name") AndAlso usedWidth < available Then
                dgvProducts.Columns("product_name").Width += available - usedWidth
            End If
        Finally
            dgvProducts.ResumeLayout(True)
            dgvProducts.HorizontalScrollingOffset = 0
        End Try
    End Sub

    ''' <summary>
    ''' Returns a safe width for a fixed column based on header text and padding.
    ''' This avoids clipped headers on different DPI/font scaling settings.
    ''' </summary>
    Private Function GetRequiredHeaderWidth(columnName As String, fallbackWidth As Integer) As Integer
        If dgvProducts Is Nothing OrElse Not dgvProducts.Columns.Contains(columnName) Then
            Return fallbackWidth
        End If

        Dim col As DataGridViewColumn = dgvProducts.Columns(columnName)
        Dim headerText As String = col.HeaderText
        If String.IsNullOrWhiteSpace(headerText) Then
            Return fallbackWidth
        End If

        Dim fontToUse As Font = dgvProducts.ColumnHeadersDefaultCellStyle.Font
        If fontToUse Is Nothing Then
            fontToUse = dgvProducts.Font
        End If

        Dim measured As Size = TextRenderer.MeasureText(headerText, fontToUse)
        Dim horizontalPadding As Integer = dgvProducts.ColumnHeadersDefaultCellStyle.Padding.Left + dgvProducts.ColumnHeadersDefaultCellStyle.Padding.Right

        Dim required As Integer = measured.Width + horizontalPadding + 20
        Return Math.Max(fallbackWidth, required)
    End Function

    Private Shared Function GetInventoryPriceColumnWidth(headerText As String) As Integer
        Dim measured As Integer = TextRenderer.MeasureText(
            headerText,
            UiTheme.FontHeading3,
            New Size(Integer.MaxValue, Integer.MaxValue),
            TextFormatFlags.SingleLine).Width + UiTheme.SpaceLg

        Return Math.Max(GridPriceColumnWidth, measured)
    End Function

    Private Shared Sub ConfigureInventoryGridFixedColumn(
        column As DataGridViewColumn,
        width As Integer,
        alignment As DataGridViewContentAlignment,
        displayIndex As Integer)

        column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        column.Width = width
        column.MinimumWidth = width
        column.DisplayIndex = displayIndex
        column.SortMode = DataGridViewColumnSortMode.Automatic
        column.DefaultCellStyle.Alignment = alignment
        column.HeaderCell.Style.Alignment = alignment
        column.HeaderCell.Style.WrapMode = DataGridViewTriState.False
    End Sub

    Private Shared Sub ConfigureInventoryGridFillColumn(
        column As DataGridViewColumn,
        fillWeight As Integer,
        minimumWidth As Integer,
        alignment As DataGridViewContentAlignment,
        displayIndex As Integer)

        column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        column.FillWeight = fillWeight
        column.MinimumWidth = minimumWidth
        column.DisplayIndex = displayIndex
        column.SortMode = DataGridViewColumnSortMode.Automatic
        column.DefaultCellStyle.Alignment = alignment
        column.HeaderCell.Style.Alignment = alignment
        column.HeaderCell.Style.WrapMode = DataGridViewTriState.False
    End Sub

    Private Shared Function CreateFieldLabel(text As String, Optional isFirst As Boolean = False) As Label
        Return New Label() With {
            .Text = text,
            .AutoSize = True,
            .ForeColor = UiTheme.TextSecondary,
            .Font = UiTheme.FontBodySmall,
            .Dock = DockStyle.Fill,
            .Margin = New Padding(0, If(isFirst, UiTheme.SpaceSm, UiTheme.SpaceLg), 0, UiTheme.SpaceXs)
        }
    End Function

    Private Shared Function SidebarButtonRowHeight() As Integer
        Return UiTheme.ButtonHeightMd + UiTheme.SpaceMd
    End Function

    Private Shared Function SidebarSmallButtonRowHeight() As Integer
        Return UiTheme.ButtonHeightSm + UiTheme.SpaceSm
    End Function

    Private Shared Function ToolbarRowHeight() As Integer
        Return Math.Max(UiTheme.InputHeight, SidebarSmallButtonRowHeight())
    End Function

    Private Shared Sub ConfigureProductInputRowStyles(layout As TableLayoutPanel)
        Dim inputRowHeight As Integer = Math.Max(UiTheme.InputHeight, 30)

        layout.RowStyles.Clear()
        For i As Integer = 0 To 16
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Next

        layout.RowStyles(1).SizeType = SizeType.Absolute
        layout.RowStyles(1).Height = inputRowHeight
        layout.RowStyles(3).SizeType = SizeType.Absolute
        layout.RowStyles(3).Height = inputRowHeight
        layout.RowStyles(5).SizeType = SizeType.Absolute
        layout.RowStyles(5).Height = inputRowHeight
        layout.RowStyles(7).SizeType = SizeType.Absolute
        layout.RowStyles(7).Height = inputRowHeight
    End Sub

    Private Shared Sub ApplyTableLayoutNumeric(nud As NumericUpDown)
        If nud Is Nothing Then
            Return
        End If

        nud.Dock = DockStyle.None
        nud.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top
        Dim h As Integer = Math.Max(UiTheme.InputHeight, 30)
        nud.MinimumSize = New Size(0, h)
        nud.MaximumSize = New Size(0, h)
    End Sub

    Private Shared Sub ApplyPairedSidebarButtonMargins(left As Button, right As Button)
        Dim halfGap As Integer = SidebarButtonGap \ 2
        left.Margin = New Padding(0, 0, halfGap, 0)
        right.Margin = New Padding(halfGap, 0, 0, 0)
    End Sub

    Private Shared Sub ConfigureSidebarButton(btn As Button)
        btn.AutoSize = False
        btn.Dock = DockStyle.Fill
        btn.Margin = Padding.Empty
        btn.TextAlign = ContentAlignment.MiddleCenter
        btn.MinimumSize = New Size(0, UiTheme.ButtonHeightMd)
    End Sub

    Private Shared Sub ConfigureSidebarSmallButton(btn As Button)
        btn.AutoSize = False
        btn.Dock = DockStyle.Fill
        btn.Margin = Padding.Empty
        btn.TextAlign = ContentAlignment.MiddleCenter
        btn.MinimumSize = New Size(0, UiTheme.ButtonHeightSm)
    End Sub

    Private Function GetFilterMode() As ProductFilterMode
        Return CType(cmbFilter.SelectedIndex, ProductFilterMode)
    End Function

    Private Sub ClearProductsInputError()
        If lblProductsInputError Is Nothing Then
            Return
        End If

        lblProductsInputError.Text = String.Empty
        lblProductsInputError.Visible = False
    End Sub

    Private Sub ResetProductImageEditorState()
        pendingImageSourcePath = Nothing
        clearImageOnSave = False
    End Sub

    Private Sub ClearProductImagePreview()
        If picProductImage?.Image IsNot Nothing Then
            picProductImage.Image.Dispose()
            picProductImage.Image = Nothing
        End If
    End Sub

    Private Sub ShowProductImageFromRelativePath(relativePath As String)
        ClearProductImagePreview()
        picProductImage.Image = ProductImageHelper.TryLoadProductImage(relativePath)
    End Sub

    Private Shared Function TryReadInsertedProductId(value As Object) As Integer?
        If value Is Nothing OrElse value Is DBNull.Value Then
            Return Nothing
        End If

        If TypeOf value Is Integer Then
            Return CInt(value)
        End If

        If TypeOf value Is Long Then
            Dim longValue As Long = CLng(value)
            If longValue >= Integer.MinValue AndAlso longValue <= Integer.MaxValue Then
                Return CInt(longValue)
            End If

            Return Nothing
        End If

        If TypeOf value Is Short Then
            Return CInt(value)
        End If

        If TypeOf value Is Decimal Then
            Dim decimalValue As Decimal = CDec(value)
            If decimalValue >= Integer.MinValue AndAlso decimalValue <= Integer.MaxValue AndAlso decimalValue = Decimal.Truncate(decimalValue) Then
                Return CInt(decimalValue)
            End If

            Return Nothing
        End If

        Dim parsed As Integer
        If Integer.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, parsed) Then
            Return parsed
        End If

        Return Nothing
    End Function

    Private Shared Function ReadImagePathValue(value As Object) As String
        If value Is Nothing OrElse value Is DBNull.Value Then
            Return Nothing
        End If

        Dim text As String = value.ToString().Trim()
        If text.Length = 0 Then
            Return Nothing
        End If

        Return text
    End Function

    Private Sub btnChooseImage_Click(sender As Object, e As EventArgs) Handles btnChooseImage.Click
        Using dialog As New OpenFileDialog()
            dialog.Filter = ProductImageHelper.GetOpenFileDialogFilter()
            dialog.Title = "Choose product image"
            If dialog.ShowDialog(Me) <> DialogResult.OK Then
                Return
            End If

            If Not ProductImageHelper.IsAllowedImageFile(dialog.FileName) Then
                MessageBox.Show("Unsupported image type. Use JPG, PNG, BMP, GIF, or WEBP.", "Product image", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            pendingImageSourcePath = dialog.FileName
            clearImageOnSave = False
            ShowProductImageFromFile(pendingImageSourcePath)
            ClearProductsInputError()
        End Using
    End Sub

    Private Sub btnRemoveImage_Click(sender As Object, e As EventArgs) Handles btnRemoveImage.Click
        pendingImageSourcePath = Nothing
        clearImageOnSave = True
        ClearProductImagePreview()
        ClearProductsInputError()
    End Sub

    Private Sub ShowProductImageFromFile(sourceFilePath As String)
        ClearProductImagePreview()
        picProductImage.Image = ProductImageHelper.TryLoadImageFile(sourceFilePath)
    End Sub

    Private Sub ShowProductsInputError(message As String)
        lblProductsInputError.Text = message
        lblProductsInputError.Visible = True
    End Sub

    Private Function ValidateProductNameInput(productName As String) As Boolean
        If productName.Length = 0 Then
            ShowProductsInputError("Product name cannot be empty.")
            MessageBox.Show("Please enter a product name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtProductName.Focus()
            Return False
        End If

        If productName.Length > MaxProductNameLength Then
            ShowProductsInputError(String.Format(CultureInfo.CurrentCulture, "Product name cannot exceed {0} characters.", MaxProductNameLength))
            MessageBox.Show(
                String.Format(CultureInfo.CurrentCulture, "Product name cannot exceed {0} characters.", MaxProductNameLength),
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)
            txtProductName.Focus()
            Return False
        End If

        Return True
    End Function

    Private Function TryGetValidatedStockQuantity(ByRef stockQty As Integer) As Boolean
        stockQty = 0
        Dim stockText As String = numStock.Text.Trim()
        If stockText.Length = 0 Then
            ShowProductsInputError("Stock quantity cannot be empty.")
            MessageBox.Show("Stock quantity cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            numStock.Focus()
            Return False
        End If

        Dim stockDecimal As Decimal
        If Not Decimal.TryParse(stockText, NumberStyles.Number, CultureInfo.CurrentCulture, stockDecimal) Then
            If Not Decimal.TryParse(stockText, NumberStyles.Number, CultureInfo.InvariantCulture, stockDecimal) Then
                ShowProductsInputError("Stock quantity must be a valid whole number.")
                MessageBox.Show("Stock quantity must be a valid whole number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                numStock.Focus()
                Return False
            End If
        End If

        If stockDecimal <> Decimal.Truncate(stockDecimal) Then
            ShowProductsInputError("Stock quantity must be a whole number.")
            MessageBox.Show("Stock quantity must be a whole number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            numStock.Focus()
            Return False
        End If

        stockQty = CInt(stockDecimal)
        If stockQty < 0 OrElse stockQty > MaxStockQuantity Then
            ShowProductsInputError(String.Format(CultureInfo.CurrentCulture, "Stock quantity must be from 0 to {0}.", MaxStockQuantity))
            MessageBox.Show(
                String.Format(CultureInfo.CurrentCulture, "Stock quantity must be between 0 and {0}.", MaxStockQuantity),
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)
            numStock.Focus()
            Return False
        End If

        Return True
    End Function

    Private Sub numStock_ValueChanged(sender As Object, e As EventArgs) Handles numStock.ValueChanged
        ClearProductsInputError()
    End Sub

    Private Function TryGetValidatedProductPrice(ByRef price As Decimal) As Boolean
        price = 0D
        Dim priceText As String = numPrice.Text.Trim()
        If priceText.Length = 0 Then
            ShowProductsInputError("Price cannot be empty.")
            MessageBox.Show("Price cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            numPrice.Focus()
            Return False
        End If

        If Not Decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.CurrentCulture, price) Then
            If Not Decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.InvariantCulture, price) Then
                ShowProductsInputError("Price must be a valid positive number.")
                MessageBox.Show("Price must be a valid number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                numPrice.Focus()
                Return False
            End If
        End If

        If price < MinProductPrice OrElse price > MaxProductPrice Then
            ShowProductsInputError(String.Format(CultureInfo.CurrentCulture, "Price must be from {0:N2} to {1:N2}.", MinProductPrice, MaxProductPrice))
            MessageBox.Show(
                String.Format(CultureInfo.CurrentCulture, "Price must be between {0:N2} and {1:N2}.", MinProductPrice, MaxProductPrice),
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)
            numPrice.Focus()
            Return False
        End If

        Return True
    End Function

    Private Sub txtProductName_TextChanged(sender As Object, e As EventArgs) Handles txtProductName.TextChanged
        ClearProductsInputError()
    End Sub

    Private Sub numPrice_ValueChanged(sender As Object, e As EventArgs) Handles numPrice.ValueChanged
        ClearProductsInputError()
    End Sub

    Private Sub numPrice_TextChanged(sender As Object, e As EventArgs) Handles numPrice.TextChanged
        ClearProductsInputError()
    End Sub

    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        ClearProductsInputError()
        Dim productName As String = txtProductName.Text.Trim()

        If Not ValidateProductNameInput(productName) Then
            Return
        End If

        Dim price As Decimal
        If Not TryGetValidatedProductPrice(price) Then
            Return
        End If

        Dim stockQty As Integer
        If Not TryGetValidatedStockQuantity(stockQty) Then
            Return
        End If

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Dim checkSql As String =
                    "SELECT id, is_active FROM products WHERE product_name = @product_name;"
                Dim existingId As Integer = -1
                Dim existingActive As Boolean = True

                Using checkCmd As New SqlCommand(checkSql, connection)
                    checkCmd.Parameters.AddWithValue("@product_name", productName)
                    Using reader As SqlDataReader = checkCmd.ExecuteReader()
                        If reader.Read() Then
                            existingId = Convert.ToInt32(reader("id"))
                            existingActive = Convert.ToBoolean(reader("is_active"))
                        End If
                    End Using
                End Using

                If existingId >= 0 Then
                    If existingActive Then
                        MessageBox.Show(
                            "A product with this name already exists. Use Update to change its price or name.",
                            "Duplicate product",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information)
                        Return
                    Else
                        MessageBox.Show(
                            "This product exists but is deactivated. Set Show to All or Inactive only, select it, then click Reactivate.",
                            "Inactive product",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information)
                        Return
                    End If
                End If

                Dim insertSql As String =
                    "INSERT INTO products (product_name, price, category_id, stock_quantity) " &
                    "OUTPUT INSERTED.id " &
                    "VALUES (@product_name, @price, @category_id, @stock_quantity);"

                Dim newCatId As Integer? = Nothing
                TryGetCategoryIdForSave(newCatId)

                Dim newProductId As Integer? = Nothing
                Using insertCmd As New SqlCommand(insertSql, connection)
                    insertCmd.Parameters.AddWithValue("@product_name", productName)
                    insertCmd.Parameters.AddWithValue("@price", price)
                    If newCatId.HasValue Then
                        insertCmd.Parameters.AddWithValue("@category_id", newCatId.Value)
                    Else
                        insertCmd.Parameters.AddWithValue("@category_id", DBNull.Value)
                    End If
                    insertCmd.Parameters.AddWithValue("@stock_quantity", stockQty)

                    newProductId = TryReadInsertedProductId(insertCmd.ExecuteScalar())
                End Using

                If Not newProductId.HasValue Then
                    Throw New InvalidOperationException("The product was saved but the database did not return a new product id.")
                End If

                PersistProductImage(connection, newProductId.Value, Nothing)

                AuditLogger.LogProduct(connection, "INSERT", newProductId.Value, productName, "Added product")
                AuditLogger.LogAudit(
                    connection,
                    "PRODUCT_ADD",
                    productName & ", price " & price.ToString("N2", CultureInfo.InvariantCulture),
                    AppSession.CurrentRole)
            End Using

            ClearInputs()
            ClearProductsInputError()
            LoadProducts()
            ShowStatus("Product added.", False)
        Catch ex As SqlException
            If ex.Number = 2627 OrElse ex.Number = 2601 Then
                MessageBox.Show("Duplicate product name is not allowed.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Else
                MessageBox.Show("Error saving product: " & ex.Message, "Database", MessageBoxButtons.OK, MessageBoxIcon.Error)
                ErrorLogger.Log(ex, NameOf(ProductsForm) & "." & NameOf(btnAdd_Click))
            End If
        Catch ex As Exception
            MessageBox.Show("Error saving product: " & ex.Message, "Database", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ErrorLogger.Log(ex, NameOf(ProductsForm) & "." & NameOf(btnAdd_Click))
        End Try
    End Sub

    Private Sub btnUpdate_Click(sender As Object, e As EventArgs) Handles btnUpdate.Click
        ClearProductsInputError()
        If dgvProducts.SelectedRows.Count = 0 Then
            MessageBox.Show("Select a product first.", "Products", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim productId As Integer = Convert.ToInt32(dgvProducts.SelectedRows(0).Cells("id").Value)
        Dim productName As String = txtProductName.Text.Trim()

        If Not ValidateProductNameInput(productName) Then
            Return
        End If

        Dim price As Decimal
        If Not TryGetValidatedProductPrice(price) Then
            Return
        End If

        Dim stockQty As Integer
        If Not TryGetValidatedStockQuantity(stockQty) Then
            Return
        End If

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Dim dupSql As String =
                    "SELECT COUNT(*) FROM products WHERE product_name = @product_name AND id <> @id;"
                Using dupCmd As New SqlCommand(dupSql, connection)
                    dupCmd.Parameters.AddWithValue("@product_name", productName)
                    dupCmd.Parameters.AddWithValue("@id", productId)
                    Dim cnt As Integer = Convert.ToInt32(dupCmd.ExecuteScalar())
                    If cnt > 0 Then
                        MessageBox.Show("Another product already uses this name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If
                End Using

                Dim updCatId As Integer? = Nothing
                TryGetCategoryIdForSave(updCatId)

                Dim query As String =
                    "UPDATE products " &
                    "SET product_name = @product_name, price = @price, category_id = @category_id, stock_quantity = @stock_quantity, is_active = 1, updated_at = SYSUTCDATETIME() " &
                    "WHERE id = @id;"

                Using command As New SqlCommand(query, connection)
                    command.Parameters.AddWithValue("@id", productId)
                    command.Parameters.AddWithValue("@product_name", productName)
                    command.Parameters.AddWithValue("@price", price)
                    If updCatId.HasValue Then
                        command.Parameters.AddWithValue("@category_id", updCatId.Value)
                    Else
                        command.Parameters.AddWithValue("@category_id", DBNull.Value)
                    End If
                    command.Parameters.AddWithValue("@stock_quantity", stockQty)

                    command.ExecuteNonQuery()
                End Using

                Dim existingImagePath As String = GetSelectedRowImagePath()
                PersistProductImage(connection, productId, existingImagePath)

                AuditLogger.LogProduct(connection, "UPDATE", productId, productName, "Updated product")
                AuditLogger.LogAudit(
                    connection,
                    "PRODUCT_EDIT",
                    "#" & productId.ToString(CultureInfo.InvariantCulture) & " " & productName,
                    AppSession.CurrentRole)
            End Using

            ClearInputs()
            ClearProductsInputError()
            LoadProducts()
            ShowStatus("Product updated.", False)
        Catch ex As SqlException
            If ex.Number = 2627 OrElse ex.Number = 2601 Then
                MessageBox.Show("Duplicate product name is not allowed.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Else
                MessageBox.Show("Error updating product: " & ex.Message, "Database", MessageBoxButtons.OK, MessageBoxIcon.Error)
                ErrorLogger.Log(ex, NameOf(ProductsForm) & "." & NameOf(btnUpdate_Click))
            End If
        Catch ex As Exception
            MessageBox.Show("Error updating product: " & ex.Message, "Database", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ErrorLogger.Log(ex, NameOf(ProductsForm) & "." & NameOf(btnUpdate_Click))
        End Try
    End Sub

    Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        If dgvProducts.SelectedRows.Count = 0 Then
            MessageBox.Show("Select a product first.", "Products", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        If MessageBox.Show(
            "Permanently delete this product? This cannot be undone.",
            "Confirm delete",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) <> DialogResult.OK Then
            Return
        End If

        Dim productId As Integer = Convert.ToInt32(dgvProducts.SelectedRows(0).Cells("id").Value)
        Dim productName As String = GetCellStringValue(dgvProducts.SelectedRows(0), "product_name")
        Dim imagePath As String = GetSelectedRowImagePath()

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Dim query As String = "DELETE FROM products WHERE id = @id;"

                Using command As New SqlCommand(query, connection)
                    command.Parameters.AddWithValue("@id", productId)
                    command.ExecuteNonQuery()
                End Using

                AuditLogger.LogProduct(connection, "HARD_DELETE", productId, productName, "Deleted product")
                AuditLogger.LogAudit(
                    connection,
                    "PRODUCT_HARD_DELETE",
                    "Deleted product #" & productId.ToString(CultureInfo.InvariantCulture) & " " & productName,
                    AppSession.CurrentRole)
            End Using

            ProductImageHelper.DeleteProductImage(imagePath)
            ClearInputs()
            LoadProducts()
            ShowStatus("Product deleted.", False)
        Catch ex As Exception
            MessageBox.Show("Error deleting product: " & ex.Message, "Database", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ErrorLogger.Log(ex, NameOf(ProductsForm) & "." & NameOf(btnDelete_Click))
        End Try
    End Sub

    Private Sub btnDeactivate_Click(sender As Object, e As EventArgs) Handles btnDeactivate.Click
        If dgvProducts.SelectedRows.Count = 0 Then
            MessageBox.Show("Select a product first.", "Products", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        If Not UiTheme.ConfirmAction("Deactivate this product? It will be hidden from active lists.") Then
            Return
        End If

        Dim productId As Integer = Convert.ToInt32(dgvProducts.SelectedRows(0).Cells("id").Value)
        Dim productName As String = GetCellStringValue(dgvProducts.SelectedRows(0), "product_name")

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Dim query As String =
                    "UPDATE products SET is_active = 0, updated_at = SYSUTCDATETIME() WHERE id = @id;"

                Using command As New SqlCommand(query, connection)
                    command.Parameters.AddWithValue("@id", productId)
                    command.ExecuteNonQuery()
                End Using

                AuditLogger.LogProduct(connection, "DEACTIVATE", productId, productName, "Deactivated product")
                AuditLogger.LogAudit(
                    connection,
                    "PRODUCT_DEACTIVATE",
                    "Deactivated product #" & productId.ToString(CultureInfo.InvariantCulture) & " " & productName,
                    AppSession.CurrentRole)
            End Using

            ClearInputs()
            LoadProducts()
            ShowStatus("Product deactivated.", False)
        Catch ex As Exception
            MessageBox.Show("Error deactivating product: " & ex.Message, "Database", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ErrorLogger.Log(ex, NameOf(ProductsForm) & "." & NameOf(btnDeactivate_Click))
        End Try
    End Sub

    Private Sub btnReactivate_Click(sender As Object, e As EventArgs) Handles btnReactivate.Click
        If dgvProducts.SelectedRows.Count = 0 Then
            Return
        End If

        If Not UiTheme.ConfirmAction("Reactivate this product? It will appear in active catalog lists again.") Then
            Return
        End If

        Dim productId As Integer = Convert.ToInt32(dgvProducts.SelectedRows(0).Cells("id").Value)

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Dim query As String =
                    "UPDATE products SET is_active = 1, updated_at = SYSUTCDATETIME() WHERE id = @id;"

                Using command As New SqlCommand(query, connection)
                    command.Parameters.AddWithValue("@id", productId)
                    command.ExecuteNonQuery()
                End Using

                AuditLogger.LogProduct(connection, "REACTIVATE", productId, Nothing, "Reactivated product")
            End Using

            LoadProducts()
            ShowStatus("Product reactivated.", False)
        Catch ex As Exception
            MessageBox.Show("Error reactivating product: " & ex.Message, "Database", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ErrorLogger.Log(ex, NameOf(ProductsForm) & "." & NameOf(btnReactivate_Click))
        End Try
    End Sub

    Private Sub btnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefresh.Click
        ClearInputs()
        LoadProducts()
        ShowStatus("List refreshed.", False)
    End Sub

    Private Sub btnImportCsv_Click(sender As Object, e As EventArgs) Handles btnImportCsv.Click
        Using ofd As New OpenFileDialog()
            ofd.Filter = "CSV files (*.csv)|*.csv|All files|*.*"
            If ofd.ShowDialog() <> DialogResult.OK Then
                Return
            End If

            ImportProductsFromCsv(ofd.FileName)
        End Using
    End Sub

    Private Sub ImportProductsFromCsv(path As String)
        Dim lines As String() = File.ReadAllLines(path)
        If lines.Length = 0 Then
            MessageBox.Show("The file is empty.", "CSV import", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim startIndex As Integer = 0
        Dim first As String = lines(0)
        If first.IndexOf("name", StringComparison.OrdinalIgnoreCase) >= 0 AndAlso first.IndexOf("price", StringComparison.OrdinalIgnoreCase) >= 0 Then
            startIndex = 1
        End If

        Dim mergedOk As Integer = 0
        Dim skipped As Integer = 0

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Using transaction As SqlTransaction = connection.BeginTransaction()
                    Try
                        Dim mergeSql As String =
                            "MERGE products AS t " &
                            "USING (SELECT @n AS product_name, @p AS price, @s AS stock_quantity) AS s " &
                            "ON t.product_name = s.product_name " &
                            "WHEN MATCHED THEN UPDATE SET t.price = s.price, t.is_active = 1, t.updated_at = SYSUTCDATETIME() " &
                            "WHEN NOT MATCHED THEN INSERT (product_name, price, category_id, stock_quantity) VALUES (s.product_name, s.price, NULL, s.stock_quantity);"

                        For i As Integer = startIndex To lines.Length - 1
                            Dim raw As String = lines(i).Trim()
                            If raw.Length = 0 Then
                                Continue For
                            End If

                            Dim parts As String() = raw.Split(","c)
                            If parts.Length < 2 Then
                                skipped += 1
                                Continue For
                            End If

                            Dim productName As String = parts(0).Trim().Trim(""""c)
                            Dim priceText As String = parts(1).Trim()
                            Dim unitPrice As Decimal

                            If Not Decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.CurrentCulture, unitPrice) Then
                                If Not Decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, unitPrice) Then
                                    skipped += 1
                                    Continue For
                                End If
                            End If

                            If productName.Length = 0 OrElse unitPrice < MinProductPrice OrElse unitPrice > MaxProductPrice Then
                                skipped += 1
                                Continue For
                            End If

                            Dim stockQty As Integer = DefaultStockQuantity
                            If parts.Length >= 3 Then
                                Dim stockText As String = parts(2).Trim()
                                Dim stockDecimal As Decimal
                                If Decimal.TryParse(stockText, NumberStyles.Number, CultureInfo.CurrentCulture, stockDecimal) OrElse
                                    Decimal.TryParse(stockText, NumberStyles.Number, CultureInfo.InvariantCulture, stockDecimal) Then
                                    If stockDecimal = Decimal.Truncate(stockDecimal) AndAlso stockDecimal >= 0D AndAlso stockDecimal <= MaxStockQuantity Then
                                        stockQty = CInt(stockDecimal)
                                    End If
                                End If
                            End If

                            Using cmd As New SqlCommand(mergeSql, connection, transaction)
                                cmd.Parameters.AddWithValue("@n", productName)
                                cmd.Parameters.AddWithValue("@p", unitPrice)
                                cmd.Parameters.AddWithValue("@s", stockQty)
                                cmd.ExecuteNonQuery()
                            End Using

                            mergedOk += 1
                        Next

                        transaction.Commit()
                        AuditLogger.LogProduct(connection, "BULK_IMPORT", Nothing, Nothing, "Merged rows: " & mergedOk.ToString(CultureInfo.CurrentCulture) & "; skipped: " & skipped.ToString(CultureInfo.CurrentCulture))
                    Catch
                        transaction.Rollback()
                        Throw
                    End Try
                End Using
            End Using

            LoadProducts()
            ShowStatus(String.Format(CultureInfo.CurrentCulture, "CSV import done. Merged: {0}. Skipped: {1}.", mergedOk, skipped), False)
        Catch ex As Exception
            MessageBox.Show("Import failed: " & ex.Message, "CSV import", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ErrorLogger.Log(ex, NameOf(ProductsForm) & "." & NameOf(ImportProductsFromCsv))
        End Try
    End Sub

    Private Sub dgvProducts_CellClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvProducts.CellClick
        If e.RowIndex < 0 Then
            Return
        End If

        Dim row As DataGridViewRow = dgvProducts.Rows(e.RowIndex)
        txtProductName.Text = Convert.ToString(row.Cells("product_name").Value, CultureInfo.CurrentCulture)
        Dim priceVal As Decimal = Convert.ToDecimal(row.Cells("price").Value)
        If priceVal < numPrice.Minimum Then
            numPrice.Value = numPrice.Minimum
        ElseIf priceVal > numPrice.Maximum Then
            numPrice.Value = numPrice.Maximum
        Else
            numPrice.Value = priceVal
        End If

        If dgvProducts.Columns.Contains("stock_quantity") Then
            Dim stockVal As Integer = Convert.ToInt32(row.Cells("stock_quantity").Value)
            If stockVal < CInt(numStock.Minimum) Then
                numStock.Value = numStock.Minimum
            ElseIf stockVal > CInt(numStock.Maximum) Then
                numStock.Value = numStock.Maximum
            Else
                numStock.Value = stockVal
            End If
        End If

        Dim catKey As Integer? = Nothing
        If dgvProducts.Columns.Contains("category_id") Then
            Dim cv As Object = row.Cells("category_id").Value
            If cv IsNot Nothing AndAlso cv IsNot DBNull.Value Then
                catKey = Convert.ToInt32(cv)
            End If
        End If

        SelectCategoryForEditor(catKey)

        ResetProductImageEditorState()
        Dim imagePath As String = Nothing
        If dgvProducts.Columns.Contains("image_path") Then
            imagePath = ReadImagePathValue(row.Cells("image_path").Value)
        End If

        ShowProductImageFromRelativePath(imagePath)

        UpdateReactivateEnabled()
    End Sub

    Private Sub SelectCategoryForEditor(categoryId As Integer?)
        If cmbCategory Is Nothing OrElse cmbCategory.Items.Count = 0 Then
            Return
        End If

        For i As Integer = 0 To cmbCategory.Items.Count - 1
            Dim opt As CategoryEditOption = TryCast(cmbCategory.Items(i), CategoryEditOption)
            If opt Is Nothing Then
                Continue For
            End If

            If Not categoryId.HasValue AndAlso Not opt.CategoryId.HasValue Then
                cmbCategory.SelectedIndex = i
                Return
            End If

            If categoryId.HasValue AndAlso opt.CategoryId.HasValue AndAlso categoryId.Value = opt.CategoryId.Value Then
                cmbCategory.SelectedIndex = i
                Return
            End If
        Next

        cmbCategory.SelectedIndex = 0
    End Sub

    Private Sub TryGetCategoryIdForSave(ByRef categoryId As Integer?)
        categoryId = Nothing
        Dim sel As CategoryEditOption = TryCast(cmbCategory.SelectedItem, CategoryEditOption)
        If sel Is Nothing Then
            Return
        End If

        categoryId = sel.CategoryId
    End Sub

    Private Sub FillCategoryLists(connection As SqlConnection)
        suppressGridCategoryFilterEvents = True
        Dim prevGridIdx As Integer = 0
        If cmbGridCategoryFilter.SelectedIndex >= 0 Then
            prevGridIdx = cmbGridCategoryFilter.SelectedIndex
        End If

        cmbGridCategoryFilter.Items.Clear()
        cmbGridCategoryFilter.Items.Add(
            New GridCategoryFilterOption With {.Kind = GridCategoryFilterOption.KindEnum.AllCategories, .Display = "All categories"})
        cmbGridCategoryFilter.Items.Add(
            New GridCategoryFilterOption With {.Kind = GridCategoryFilterOption.KindEnum.Uncategorized, .Display = "Uncategorized"})

        cmbCategory.Items.Clear()
        cmbCategory.Items.Add(New CategoryEditOption With {.CategoryId = Nothing, .Display = "(No category)"})

        Dim catSql As String = "SELECT category_id, category_name FROM dbo.categories WHERE is_active = 1 ORDER BY category_name;"
        Using catCmd As New SqlCommand(catSql, connection)
            Using reader As SqlDataReader = catCmd.ExecuteReader()
                While reader.Read()
                    Dim cid As Integer = Convert.ToInt32(reader("category_id"))
                    Dim nm As String = reader("category_name").ToString()
                    cmbGridCategoryFilter.Items.Add(
                        New GridCategoryFilterOption With {
                            .Kind = GridCategoryFilterOption.KindEnum.SpecificCategory,
                            .CategoryId = cid,
                            .Display = nm})
                    cmbCategory.Items.Add(New CategoryEditOption With {.CategoryId = cid, .Display = nm})
                End While
            End Using
        End Using

        If prevGridIdx < cmbGridCategoryFilter.Items.Count Then
            cmbGridCategoryFilter.SelectedIndex = prevGridIdx
        Else
            cmbGridCategoryFilter.SelectedIndex = 0
        End If

        suppressGridCategoryFilterEvents = False
    End Sub

    Private Sub LoadProducts()
        If dgvProducts Is Nothing OrElse lblGridMessage Is Nothing Then
            Return
        End If

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                FillCategoryLists(connection)

                Dim whereClause As String = String.Empty
                Select Case GetFilterMode()
                    Case ProductFilterMode.ActiveOnly
                        whereClause = "WHERE p.is_active = 1"
                    Case ProductFilterMode.InactiveOnly
                        whereClause = "WHERE p.is_active = 0"
                    Case Else
                        whereClause = String.Empty
                End Select

                Dim query As String =
                    "SELECT p.id, p.product_name, p.price, p.stock_quantity, p.image_path, p.is_active, p.category_id, c.category_name AS category_name " &
                    "FROM products p " &
                    "LEFT JOIN dbo.categories c ON c.category_id = p.category_id " &
                    whereClause &
                    " ORDER BY p.product_name;"

                Using adapter As New SqlDataAdapter(query, connection)
                    productsTable = New DataTable()
                    adapter.Fill(productsTable)
                End Using
            End Using

            productsView = New DataView(productsTable)
            dgvProducts.DataSource = productsView
            dgvProducts.Visible = True
            lblGridMessage.Visible = False
            ApplyCombinedFilter()
            FormatProductColumns()
        Catch ex As Exception
            productsTable = Nothing
            productsView = Nothing
            dgvProducts.DataSource = Nothing
            dgvProducts.Visible = False
            lblGridMessage.Visible = True
            lblGridMessage.Text = "Could not load products. Check LocalDB and App.config (GroupCSqlServer)." & Environment.NewLine & ex.Message
            MessageBox.Show("Error loading products: " & ex.Message, "Database", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ErrorLogger.Log(ex, NameOf(ProductsForm) & "." & NameOf(LoadProducts))
        End Try
    End Sub

    Private Sub ClearInputs()
        txtProductName.Clear()
        numPrice.Value = numPrice.Minimum
        numStock.Value = DefaultStockQuantity
        SelectCategoryForEditor(Nothing)
        ResetProductImageEditorState()
        ClearProductImagePreview()
        ClearProductsInputError()
        txtProductName.Focus()
    End Sub

    Private Function GetCellStringValue(row As DataGridViewRow, columnName As String) As String
        If row Is Nothing OrElse Not dgvProducts.Columns.Contains(columnName) Then
            Return String.Empty
        End If

        Dim value As Object = row.Cells(columnName).Value
        If value Is Nothing OrElse value Is DBNull.Value Then
            Return String.Empty
        End If

        Return value.ToString()
    End Function

    Private Function GetSelectedRowImagePath() As String
        If dgvProducts.SelectedRows.Count = 0 Then
            Return String.Empty
        End If

        Return GetCellStringValue(dgvProducts.SelectedRows(0), "image_path")
    End Function

    Private Sub PersistProductImage(connection As SqlConnection, productId As Integer, existingRelativePath As String)
        If clearImageOnSave Then
            ProductImageHelper.DeleteProductImage(existingRelativePath)
            UpdateProductImagePath(connection, productId, Nothing)
            Return
        End If

        If String.IsNullOrWhiteSpace(pendingImageSourcePath) Then
            Return
        End If

        Dim savedPath As String = ProductImageHelper.SaveProductImage(productId, pendingImageSourcePath, existingRelativePath)
        UpdateProductImagePath(connection, productId, savedPath)
    End Sub

    Private Shared Sub UpdateProductImagePath(connection As SqlConnection, productId As Integer, relativePath As String)
        Dim sql As String =
            "UPDATE products SET image_path = @image_path, updated_at = SYSUTCDATETIME() WHERE id = @id;"
        Using command As New SqlCommand(sql, connection)
            command.Parameters.AddWithValue("@id", productId)
            If String.IsNullOrWhiteSpace(relativePath) Then
                command.Parameters.AddWithValue("@image_path", DBNull.Value)
            Else
                command.Parameters.AddWithValue("@image_path", relativePath)
            End If

            command.ExecuteNonQuery()
        End Using
    End Sub

    Private Sub btnBack_Click(sender As Object, e As EventArgs) Handles btnBack.Click
        Me.Close()
    End Sub

    Private Sub btnManageCategories_Click(sender As Object, e As EventArgs) Handles btnManageCategories.Click
        If Not AppSession.RequireAdmin(Me) Then
            Return
        End If

        Using form As New CategoriesForm()
            form.ShowDialog(Me)
        End Using

        LoadProducts()
    End Sub

    Private Function GetProductListText() As String
        Dim sb As New StringBuilder()
        sb.AppendLine("Product Inventory List")
        sb.AppendLine("Generated on: " & DateTime.Now.ToString("g", CultureInfo.CurrentCulture))
        sb.AppendLine(New String("-"c, 60))
        If dgvProducts IsNot Nothing AndAlso dgvProducts.Rows.Count > 0 Then
            For Each row As DataGridViewRow In dgvProducts.Rows
                If row.IsNewRow Then Continue For
                Dim name As String = If(row.Cells("product_name")?.FormattedValue?.ToString(), String.Empty)
                Dim price As String = If(row.Cells("price")?.FormattedValue?.ToString(), String.Empty)
                Dim stock As String = If(row.Cells("stock_quantity")?.FormattedValue?.ToString(), String.Empty)
                sb.AppendLine(name.PadRight(30) & " | " & price.PadLeft(10) & " | Stock: " & stock)
            Next
        Else
            sb.AppendLine("No products found.")
        End If
        Return sb.ToString()
    End Function

    Private Sub btnImportPdf_Click(sender As Object, e As EventArgs) Handles btnImportPdf.Click
        Using saveDialog As New SaveFileDialog()
            saveDialog.Filter = "PDF Files (*.pdf)|*.pdf"
            saveDialog.FileName = "ProductsList_" & DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture) & ".pdf"
            If saveDialog.ShowDialog() = DialogResult.OK Then
                Try
                    PdfReceiptExporter.ExportTextToPdf(saveDialog.FileName, GetProductListText())
                    ShowStatus("PDF exported successfully.", False)
                Catch ex As Exception
                    MessageBox.Show("Error exporting to PDF: " & ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    ErrorLogger.Log(ex, NameOf(ProductsForm) & "." & NameOf(btnImportPdf_Click))
                End Try
            End If
        End Using
    End Sub

    Private Sub btnImportTxt_Click(sender As Object, e As EventArgs) Handles btnImportTxt.Click
        Using saveDialog As New SaveFileDialog()
            saveDialog.Filter = "Text Files (*.txt)|*.txt"
            saveDialog.FileName = "ProductsList_" & DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture) & ".txt"
            If saveDialog.ShowDialog() = DialogResult.OK Then
                Try
                    File.WriteAllText(saveDialog.FileName, GetProductListText())
                    ShowStatus("Text file exported successfully.", False)
                Catch ex As Exception
                    MessageBox.Show("Error exporting to TXT: " & ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    ErrorLogger.Log(ex, NameOf(ProductsForm) & "." & NameOf(btnImportTxt_Click))
                End Try
            End If
        End Using
    End Sub

    Private Sub btnPrintCopy_Click(sender As Object, e As EventArgs) Handles btnPrintCopy.Click
        MessageBox.Show("Print functionality for the products list is not fully implemented. Please export to PDF and print the file.", "Print Copy", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

End Class
