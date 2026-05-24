Imports System.Collections.Generic
Imports System.Data
Imports System.Globalization
Imports System.IO
Imports System.Drawing
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

    Private WithEvents cmbFilter As ComboBox
    Private WithEvents btnReactivate As Button

    Private WithEvents btnAdd As Button
    Private WithEvents btnUpdate As Button
    Private WithEvents btnDelete As Button
    Private WithEvents btnRefresh As Button
    Private WithEvents btnImportCsv As Button
    Private WithEvents btnBack As Button
    Private WithEvents btnManageCategories As Button

    Private WithEvents cmbGridCategoryFilter As ComboBox

    Private statusStrip As StatusStrip
    Private statusLabel As ToolStripStatusLabel
    Private WithEvents statusClearTimer As Timer

    Private lblProductsInputError As Label

    Private productsTable As DataTable
    Private productsView As DataView

    ''' <summary>
    ''' Blocks filter ComboBox events while controls still building — parenting can fire SelectedIndexChanged before grid exists.
    ''' </summary>
    Private suppressProductFilterEvents As Boolean

    Private suppressGridCategoryFilterEvents As Boolean

    ''' <summary>Prevents stacking multiple BeginInvoke layout passes for the product grid.</summary>
    Private productGridLayoutPending As Boolean

    Private Sub ProductsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' 1. FORM SETUP (Full Screen & Responsive)
        Me.Text = AppBranding.WindowTitle("Manage Products")
        UiTheme.ApplyMaximizedWorkspaceDefaults(Me, 960, 600)

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
        Me.BackColor = UiTheme.FormBackground

        ' -----------------------------------------------------------
        ' 1. INITIALIZE CONTROLS
        ' -----------------------------------------------------------
        txtProductName = New TextBox() With {
            .Dock = DockStyle.Fill,
            .MaxLength = 100,
            .Font = UiTheme.FontBody
        }
        numPrice = New NumericUpDown() With {
            .Dock = DockStyle.Fill,
            .DecimalPlaces = 2,
            .Minimum = 0.01D,
            .Maximum = 999999.99D,
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

        ' Apply UI Theme fixes to prevent TextBoxes from awkwardly stretching vertically in grids
        Try
            UiTheme.ApplyTableLayoutSingleLineTextBox(txtProductName)
            UiTheme.ApplyTableLayoutDropDown(cmbCategory)
        Catch
        End Try

        txtSearch = New TextBox() With {
            .Width = 260,
            .PlaceholderText = "Search products...",
            .Font = UiTheme.FontBody
        }
        cmbFilter = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Width = 160,
            .Font = UiTheme.FontBody
        }
        cmbFilter.Items.AddRange(New Object() {"Active products only", "All products", "Inactive only"})
        cmbGridCategoryFilter = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Width = 180,
            .Font = UiTheme.FontBody
        }
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
        btnBack = New Button() With {
            .Text = "← Back to Menu",
            .AutoSize = True,
            .MinimumSize = New Size(140, UiTheme.ButtonHeightMd),
            .Cursor = Cursors.Hand
        }
        btnManageCategories = New Button() With {
            .Text = "Manage &categories…",
            .AutoSize = True,
            .MinimumSize = New Size(140, UiTheme.ButtonHeightSm),
            .Cursor = Cursors.Hand
        }

        ' Apply Themes
        Try
            UiTheme.ApplyPrimaryButton(btnAdd)
            UiTheme.ApplyPrimaryButton(btnUpdate)
            UiTheme.ApplyWarningButton(btnDelete)
            UiTheme.ApplySuccessButton(btnReactivate)
            UiTheme.ApplySecondaryButton(btnRefresh)
            UiTheme.ApplyPrimaryButton(btnImportCsv)
            UiTheme.ApplySecondaryButton(btnBack)
            UiTheme.ApplySecondaryAccentButton(btnManageCategories)
        Catch
        End Try

        dgvProducts = New DataGridView() With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .ScrollBars = ScrollBars.Vertical,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None
        }
        Try
            UiTheme.ApplyDataGridViewChrome(dgvProducts)
        Catch
        End Try

        lblGridMessage = New Label() With {.Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleCenter, .ForeColor = Color.Gray, .Visible = False}
        lblProductsInputError = New Label() With {.AutoSize = True, .ForeColor = UiTheme.Danger, .Visible = False, .Padding = New Padding(0, 10, 0, 10)}

        statusStrip = New StatusStrip()
        statusLabel = New ToolStripStatusLabel(FormStatusHelper.ReadyText) With {.Spring = True, .TextAlign = ContentAlignment.MiddleLeft}
        statusStrip.Items.Add(statusLabel)
        Try
            UiTheme.ApplyStatusStripTheme(statusStrip)
        Catch
        End Try

        ' -----------------------------------------------------------
        ' 2. BUILD THE RESPONSIVE LAYOUT (Professional Hierarchy)
        ' -----------------------------------------------------------
        ' Root Container: Zero margins allow the Left Panel to touch the window edge
        Dim rootTable As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .Margin = New Padding(0),
            .BackColor = UiTheme.FormBackground
        }
        rootTable.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 380.0F))
        rootTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

        ' --- LEFT SIDEBAR: DATA ENTRY ---
        Dim leftSidebar As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .Padding = New Padding(25, 30, 25, 30) ' Clean, consistent padding
        }

        ' A 4-Row Grid manages the Left Sidebar effortlessly
        Dim leftLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 4
        }
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))        ' Row 0: Header & Errors
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))        ' Row 1: Inputs
        leftLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F)) ' Row 2: Dynamic Spacer!
        leftLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))        ' Row 3: Footer / Utilities

        ' Row 0: Header Section
        Dim headerPanel As New Panel() With {.AutoSize = True, .Dock = DockStyle.Top}
        Dim lblTitleLeft As New Label() With {
            .Text = "Product Details",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = UiTheme.PrimaryAccent,
            .AutoSize = True,
            .Dock = DockStyle.Top
        }
        lblProductsInputError.Dock = DockStyle.Top

        headerPanel.Controls.Add(lblProductsInputError)
        headerPanel.Controls.Add(lblTitleLeft)
        leftLayout.Controls.Add(headerPanel, 0, 0)

        ' Row 1: Input Section
        Dim inputLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .ColumnCount = 1,
            .RowCount = 10,
            .Margin = New Padding(0, 20, 0, 0)
        }

        Dim CreateLabel = Function(text As String) New Label() With {.Text = text, .AutoSize = True, .ForeColor = UiTheme.TextSecondary, .Margin = New Padding(0, 15, 0, 5)}

        inputLayout.Controls.Add(CreateLabel("Product Name"), 0, 0)
        inputLayout.Controls.Add(txtProductName, 0, 1)
        inputLayout.Controls.Add(CreateLabel("Price (" & AppSettings.Current.CurrencySymbol & ")"), 0, 2)
        inputLayout.Controls.Add(numPrice, 0, 3)
        inputLayout.Controls.Add(CreateLabel("Stock quantity"), 0, 4)
        inputLayout.Controls.Add(numStock, 0, 5)
        inputLayout.Controls.Add(CreateLabel("Category"), 0, 6)
        inputLayout.Controls.Add(cmbCategory, 0, 7)

        Dim pnlPrimaryActions As New FlowLayoutPanel() With {.AutoSize = True, .Margin = New Padding(0, 30, 0, 0)}
        pnlPrimaryActions.Controls.Add(btnAdd)
        pnlPrimaryActions.Controls.Add(btnUpdate)
        inputLayout.Controls.Add(pnlPrimaryActions, 0, 8)

        Dim pnlStatusActions As New FlowLayoutPanel() With {.AutoSize = True, .Margin = New Padding(0, 10, 0, 0)}
        pnlStatusActions.Controls.Add(btnDelete)
        pnlStatusActions.Controls.Add(btnReactivate)
        inputLayout.Controls.Add(pnlStatusActions, 0, 9)

        leftLayout.Controls.Add(inputLayout, 0, 1)

        ' Row 3: Footer / Utilities
        Dim pnlUtility As New FlowLayoutPanel() With {
            .Dock = DockStyle.Bottom,
            .AutoSize = True,
            .FlowDirection = FlowDirection.TopDown
        }
        pnlUtility.Controls.Add(New Label() With {.Text = "Database Utilities", .AutoSize = True, .ForeColor = UiTheme.TextSecondary, .Margin = New Padding(0, 0, 0, 10)})

        Dim pnlUtilBtns As New FlowLayoutPanel() With {.AutoSize = True}
        pnlUtilBtns.Controls.Add(btnManageCategories)
        pnlUtilBtns.Controls.Add(btnImportCsv)
        pnlUtility.Controls.Add(pnlUtilBtns)

        btnBack.Margin = New Padding(0, 30, 0, 0)
        pnlUtility.Controls.Add(btnBack)

        leftLayout.Controls.Add(pnlUtility, 0, 3)
        leftSidebar.Controls.Add(leftLayout)

        ' --- RIGHT CARD: DATA GRID ---
        ' Using a TableLayoutPanel manages the Toolbar vs Grid elegantly without overlapping
        Dim rightCard As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .Padding = New Padding(30, 30, 30, 20) ' Excellent breathing room
        }
        rightCard.RowStyles.Add(New RowStyle(SizeType.AutoSize))       ' Title
        rightCard.RowStyles.Add(New RowStyle(SizeType.AutoSize))       ' Toolbar
        rightCard.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F)) ' Grid

        Dim lblTitleRight As New Label() With {
            .Text = "Inventory Overview",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = UiTheme.PrimaryAccent,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 20)
        }
        rightCard.Controls.Add(lblTitleRight, 0, 0)

        Dim toolbar As New FlowLayoutPanel() With {
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .WrapContents = False,
            .Margin = New Padding(0, 0, 0, 15)
        }
        toolbar.Controls.Add(txtSearch)
        toolbar.Controls.Add(cmbGridCategoryFilter)
        toolbar.Controls.Add(cmbFilter)
        toolbar.Controls.Add(btnRefresh)

        rightCard.Controls.Add(toolbar, 0, 1)

        Dim gridContainer As New Panel() With {.Dock = DockStyle.Fill}
        gridContainer.Controls.Add(dgvProducts)
        gridContainer.Controls.Add(lblGridMessage) ' Errors hide securely behind the grid

        rightCard.Controls.Add(gridContainer, 0, 2)

        ' 3. ASSEMBLE
        rootTable.Controls.Add(leftSidebar, 0, 0)
        rootTable.Controls.Add(rightCard, 1, 0)

        Me.Controls.Add(rootTable)
        Me.Controls.Add(statusStrip)

        cmbFilter.SelectedIndex = 0
        suppressProductFilterEvents = False
        Me.ResumeLayout(True)
    End Sub

    Private Sub ProductsForm_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        ScheduleProductGridColumnLayout()
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
        btnReactivate.Enabled = False
        If dgvProducts.SelectedRows.Count = 0 Then
            Return
        End If

        If Not dgvProducts.Columns.Contains("is_active") Then
            Return
        End If

        Dim activeVal As Object = dgvProducts.SelectedRows(0).Cells("is_active").Value
        If activeVal Is Nothing OrElse activeVal Is DBNull.Value Then
            Return
        End If

        Dim isActive As Boolean = Convert.ToBoolean(activeVal)
        btnReactivate.Enabled = Not isActive
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
        ScheduleProductGridColumnLayout()
        UpdateReactivateEnabled()
    End Sub

    Private Sub FormatProductColumns()
        If dgvProducts.Columns.Count = 0 Then
            Return
        End If

        GridDisplayHelper.ApplyStandardBoundGridDisplay(dgvProducts)

        If dgvProducts.Columns.Contains("is_active") Then
            Dim activeCol As DataGridViewColumn = dgvProducts.Columns("is_active")
            activeCol.HeaderText = "Active"
            activeCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            activeCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter
        End If

        If dgvProducts.Columns.Contains("product_name") Then
            dgvProducts.Columns("product_name").HeaderText = "Product"
        End If

        If dgvProducts.Columns.Contains("price") Then
            Dim priceCol As DataGridViewColumn = dgvProducts.Columns("price")
            priceCol.HeaderText = "Price (₱)"
            priceCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            priceCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight
            priceCol.DefaultCellStyle.Format = "N2"
        End If

        If dgvProducts.Columns.Contains("stock_quantity") Then
            Dim stockCol As DataGridViewColumn = dgvProducts.Columns("stock_quantity")
            stockCol.HeaderText = "Stock"
            stockCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            stockCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter
        End If

        If dgvProducts.Columns.Contains("category_name") Then
            dgvProducts.Columns("category_name").HeaderText = "Category"
        End If

        GridDisplayHelper.MoveActiveStatusColumnToLeft(dgvProducts)
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

        productGridLayoutPending = True
        BeginInvoke(New MethodInvoker(
            Sub()
                productGridLayoutPending = False
                ApplyProductGridColumnLayout()
            End Sub))
    End Sub

    ''' <summary>
    ''' Keeps Active, Price, Stock, and Category at readable fixed widths; Product fills
    ''' the rest so the grid never overflows horizontally (which clips header text).
    ''' </summary>
    Private Sub ApplyProductGridColumnLayout()
        If dgvProducts Is Nothing OrElse dgvProducts.IsDisposed OrElse dgvProducts.Columns.Count = 0 Then
            Return
        End If

        If dgvProducts.ClientSize.Width <= 0 Then
            Return
        End If

        dgvProducts.SuspendLayout()
        Try
            dgvProducts.ScrollBars = ScrollBars.Vertical
            dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            dgvProducts.HorizontalScrollingOffset = 0

            ' Tighter header padding so labels like "Active" and "Category" fit narrow columns.
            dgvProducts.ColumnHeadersDefaultCellStyle.Padding = New Padding(6, 0, 6, 0)

            If dgvProducts.Columns.Contains("is_active") Then
                Dim activeCol As DataGridViewColumn = dgvProducts.Columns("is_active")
                activeCol.DisplayIndex = 0
                activeCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                activeCol.Width = 76
                activeCol.MinimumWidth = 68
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
                priceCol.Width = 100
                priceCol.MinimumWidth = 92
            End If

            If dgvProducts.Columns.Contains("stock_quantity") Then
                Dim stockCol As DataGridViewColumn = dgvProducts.Columns("stock_quantity")
                stockCol.DisplayIndex = 3
                stockCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                stockCol.Width = 72
                stockCol.MinimumWidth = 64
            End If

            If dgvProducts.Columns.Contains("category_name") Then
                Dim categoryCol As DataGridViewColumn = dgvProducts.Columns("category_name")
                categoryCol.DisplayIndex = 4
                categoryCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                categoryCol.Width = 120
                categoryCol.MinimumWidth = 96
            End If
        Finally
            dgvProducts.ResumeLayout(True)
            dgvProducts.HorizontalScrollingOffset = 0
        End Try
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
                    "INSERT INTO products (product_name, price, category_id, stock_quantity) VALUES (@product_name, @price, @category_id, @stock_quantity);"

                Dim newCatId As Integer? = Nothing
                TryGetCategoryIdForSave(newCatId)

                Using insertCmd As New SqlCommand(insertSql, connection)
                    insertCmd.Parameters.AddWithValue("@product_name", productName)
                    insertCmd.Parameters.AddWithValue("@price", price)
                    If newCatId.HasValue Then
                        insertCmd.Parameters.AddWithValue("@category_id", newCatId.Value)
                    Else
                        insertCmd.Parameters.AddWithValue("@category_id", DBNull.Value)
                    End If
                    insertCmd.Parameters.AddWithValue("@stock_quantity", stockQty)

                    insertCmd.ExecuteNonQuery()
                End Using

                AuditLogger.LogProduct(connection, "INSERT", Nothing, productName, "Added product")
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
            "Deactivate this product? It will be hidden from active lists.",
            "Confirm deactivate",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) <> DialogResult.OK Then
            Return
        End If

        Dim productId As Integer = Convert.ToInt32(dgvProducts.SelectedRows(0).Cells("id").Value)

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Dim query As String =
                    "UPDATE products SET is_active = 0, updated_at = SYSUTCDATETIME() WHERE id = @id;"

                Using command As New SqlCommand(query, connection)
                    command.Parameters.AddWithValue("@id", productId)
                    command.ExecuteNonQuery()
                End Using

                AuditLogger.LogProduct(connection, "DEACTIVATE", productId, Nothing, "Deactivated product")
                AuditLogger.LogAudit(
                    connection,
                    "PRODUCT_DELETE",
                    "Deactivated product #" & productId.ToString(CultureInfo.InvariantCulture),
                    AppSession.CurrentRole)
            End Using

            ClearInputs()
            LoadProducts()
            ShowStatus("Product deactivated.", False)
        Catch ex As Exception
            MessageBox.Show("Error deleting product: " & ex.Message, "Database", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ErrorLogger.Log(ex, NameOf(ProductsForm) & "." & NameOf(btnDelete_Click))
        End Try
    End Sub

    Private Sub btnReactivate_Click(sender As Object, e As EventArgs) Handles btnReactivate.Click
        If dgvProducts.SelectedRows.Count = 0 Then
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
        txtProductName.Text = row.Cells("product_name").Value.ToString()
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
                    "SELECT p.id, p.product_name, p.price, p.stock_quantity, p.is_active, p.category_id, c.category_name AS category_name " &
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
        ClearProductsInputError()
        txtProductName.Focus()
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

End Class
