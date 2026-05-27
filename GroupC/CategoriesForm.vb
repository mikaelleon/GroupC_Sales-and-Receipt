Imports System.Data
Imports System.Globalization
Imports System.Drawing
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient

''' <summary>
''' Add, update, deactivate, and reactivate product categories for International Bookstore.
''' </summary>
Public Class CategoriesForm
    Inherits Form

    Private Const MaxCategoryNameLength As Integer = 100
    Private Const GridStatusColumnWidth As Integer = 68
    Private Const GridProductsColumnWidth As Integer = 88
    Private Const GridCategoryFillWeight As Integer = 200
    Private Const GridCategoryMinWidth As Integer = 120

    Private WithEvents txtCategoryName As TextBox
    Private WithEvents cmbFilter As ComboBox
    Private WithEvents dgvCategories As DataGridView
    Private WithEvents btnAdd As Button
    Private WithEvents btnUpdate As Button
    Private WithEvents btnDeactivate As Button
    Private WithEvents btnReactivate As Button
    Private WithEvents btnRefresh As Button
    Private WithEvents btnBack As Button

    Private lblInputError As Label
    Private statusStrip As StatusStrip
    Private statusLabel As ToolStripStatusLabel
    Private WithEvents statusClearTimer As Timer

    Private categoriesTable As DataTable
    Private suppressFilterEvents As Boolean
    Private categoryGridLayoutPending As Boolean

    Private Sub CategoriesForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = AppBranding.WindowTitle("Manage Categories")
        UiTheme.ApplyMaximizedWorkspaceDefaults(Me, 880, 560)

        Try
            UiTheme.ApplyStandardWindowChrome(Me)
        Catch
        End Try

        statusClearTimer = New Timer() With {.Interval = FormStatusHelper.StatusShowMilliseconds}

        Try
            DatabaseInitializer.EnsureDatabase()
        Catch
        End Try

        BuildLayout()
        LoadCategories()
    End Sub

    Private Sub BuildLayout()
        Me.SuspendLayout()
        Me.Controls.Clear()
        Me.BackColor = UiTheme.ColBackground

        txtCategoryName = New TextBox() With {
            .MaxLength = MaxCategoryNameLength,
            .Font = UiTheme.FontBody,
            .Dock = DockStyle.Fill
        }
        UiTheme.ApplyInputStyle(txtCategoryName)

        cmbFilter = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Width = 180,
            .Font = UiTheme.FontBody
        }
        cmbFilter.Items.AddRange(New Object() {"Active categories", "All categories", "Inactive only"})
        UiTheme.ApplyInputStyle(cmbFilter)

        btnAdd = New Button() With {.Text = "&Add category", .AutoSize = True, .MinimumSize = New Size(120, UiTheme.ButtonHeight), .Cursor = Cursors.Hand, .Dock = DockStyle.Top, .Margin = New Padding(0, 0, 0, UiTheme.PadControl)}
        btnUpdate = New Button() With {.Text = "&Update name", .AutoSize = True, .MinimumSize = New Size(110, UiTheme.ButtonHeight), .Cursor = Cursors.Hand, .Dock = DockStyle.Top, .Margin = New Padding(0, 0, 0, UiTheme.PadControl)}
        btnDeactivate = New Button() With {.Text = "&Deactivate", .AutoSize = True, .MinimumSize = New Size(100, UiTheme.ButtonHeight), .Cursor = Cursors.Hand, .Dock = DockStyle.Top, .Margin = New Padding(0, 0, 0, UiTheme.PadControl)}
        btnReactivate = New Button() With {.Text = "Reactivate", .AutoSize = True, .MinimumSize = New Size(100, UiTheme.ButtonHeight), .Enabled = False, .Cursor = Cursors.Hand, .Dock = DockStyle.Top, .Margin = Padding.Empty}

        UiTheme.ApplyPrimaryButton(btnAdd)
        UiTheme.ApplyPrimaryButton(btnUpdate)
        UiTheme.ApplyWarningButton(btnDeactivate)
        UiTheme.ApplySuccessButton(btnReactivate)

        btnRefresh = New Button() With {.Text = "Refresh", .AutoSize = True, .MinimumSize = New Size(90, UiTheme.ButtonHeight), .Cursor = Cursors.Hand}
        UiTheme.ApplySecondaryButton(btnRefresh)

        dgvCategories = New DataGridView() With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AllowUserToResizeColumns = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            .BackgroundColor = UiTheme.ColSurface,
            .BorderStyle = BorderStyle.None,
            .ScrollBars = ScrollBars.Both
        }
        UiTheme.ApplyGridStyle(dgvCategories)
        AddHandler dgvCategories.DataBindingComplete, AddressOf dgvCategories_DataBindingComplete
        AddHandler dgvCategories.Resize, AddressOf dgvCategories_Resize
        AddHandler Me.Shown, AddressOf CategoriesForm_Shown
        AddHandler Me.Resize, AddressOf CategoriesForm_Resize

        lblInputError = New Label() With {.AutoSize = True, .ForeColor = UiTheme.ColDanger, .Visible = False, .Margin = New Padding(0, UiTheme.PadControl, 0, 0)}

        statusStrip = New StatusStrip()
        statusLabel = New ToolStripStatusLabel(FormStatusHelper.ReadyText) With {.Spring = True, .TextAlign = ContentAlignment.MiddleLeft}
        statusStrip.Items.Add(statusLabel)
        UiTheme.ApplyStatusStripTheme(statusStrip)

        Dim actionStack As New TableLayoutPanel() With {
            .AutoSize = True,
            .Dock = DockStyle.Top,
            .ColumnCount = 1,
            .RowCount = 4,
            .Margin = New Padding(0, UiTheme.PadSection, 0, 0)
        }
        actionStack.Controls.Add(btnAdd, 0, 0)
        actionStack.Controls.Add(btnUpdate, 0, 1)
        actionStack.Controls.Add(btnDeactivate, 0, 2)
        actionStack.Controls.Add(btnReactivate, 0, 3)

        Dim editorLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 4,
            .Margin = Padding.Empty
        }
        editorLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        editorLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        editorLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        editorLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        editorLayout.Controls.Add(UiTheme.CreateSectionHeader("Category editor"), 0, 0)
        editorLayout.Controls.Add(UiTheme.CreateSecondaryLabel("Assign categories to products on the Products screen."), 0, 1)
        editorLayout.Controls.Add(lblInputError, 0, 2)

        Dim inputPanel As New TableLayoutPanel() With {
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .ColumnCount = 1,
            .RowCount = 2,
            .Margin = New Padding(0, UiTheme.PadControl, 0, 0)
        }
        inputPanel.Controls.Add(UiTheme.CreateSecondaryLabel("Category name"), 0, 0)
        txtCategoryName.Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        inputPanel.Controls.Add(txtCategoryName, 0, 1)

        Dim editorBody As New Panel() With {.Dock = DockStyle.Fill, .AutoScroll = True}
        Dim editorStack As New TableLayoutPanel() With {.AutoSize = True, .Dock = DockStyle.Top, .ColumnCount = 1, .RowCount = 2}
        editorStack.Controls.Add(inputPanel, 0, 0)
        editorStack.Controls.Add(actionStack, 0, 1)
        editorBody.Controls.Add(editorStack)
        editorLayout.Controls.Add(editorBody, 0, 3)

        Dim toolbar As New TableLayoutPanel() With {
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .ColumnCount = 4,
            .RowCount = 1,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        }
        toolbar.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        toolbar.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        toolbar.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        toolbar.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        toolbar.RowStyles.Add(New RowStyle(SizeType.Absolute, UiTheme.InputHeight + UiTheme.PadControl))

        Dim lblShow As Label = UiTheme.CreateSecondaryLabel("Show")
        lblShow.Margin = New Padding(0, UiTheme.PadTight, UiTheme.PadControl, 0)
        lblShow.Anchor = AnchorStyles.Left
        cmbFilter.Dock = DockStyle.Fill
        cmbFilter.Margin = New Padding(0, 0, UiTheme.PadControl, 0)
        btnRefresh.Dock = DockStyle.Fill
        toolbar.Controls.Add(lblShow, 0, 0)
        toolbar.Controls.Add(cmbFilter, 1, 0)
        toolbar.Controls.Add(New Panel(), 2, 0)
        toolbar.Controls.Add(btnRefresh, 3, 0)

        Dim gridLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .Margin = Padding.Empty
        }
        gridLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        gridLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        gridLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        gridLayout.Controls.Add(UiTheme.CreateSectionHeader("Categories"), 0, 0)
        gridLayout.Controls.Add(toolbar, 0, 1)

        Dim gridCard As Panel = UiTheme.CreateCard()
        gridCard.Dock = DockStyle.Fill
        Dim gridCardHost As Panel = gridCard
        Try
            gridCardHost = UiTheme.GetCardContentHost(gridCard)
        Catch
        End Try
        gridCardHost.Controls.Add(dgvCategories)

        Dim gridPanel As New Panel() With {.Dock = DockStyle.Fill}
        gridPanel.Controls.Add(gridCard)
        gridLayout.Controls.Add(gridPanel, 0, 2)

        ' -----------------------------------------------------------
        ' SHARED SHELL + CATEGORIES SPLIT LAYOUT
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
            ("Manage Categories", True),
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
        Dim topBar As Panel = UiTheme.CreateTopBar("Manage Categories", AppSession.GetAuditIdentity())
        Dim contentArea As Panel = UiTheme.CreateContentArea()

        Dim categoriesSplit As New SplitContainer() With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Vertical,
            .SplitterWidth = 6,
            .BackColor = UiTheme.ColBorder,
            .Panel1MinSize = 300,
            .Panel2MinSize = 360
        }

        Dim editorCard As Panel = UiTheme.CreateCard()
        editorCard.Dock = DockStyle.Fill
        Dim editorCardHost As Panel = editorCard
        Try
            editorCardHost = UiTheme.GetCardContentHost(editorCard)
        Catch
        End Try
        editorCardHost.Controls.Add(editorLayout)
        categoriesSplit.Panel1.Controls.Add(editorCard)

        Dim listCard As Panel = UiTheme.CreateCard()
        listCard.Dock = DockStyle.Fill
        Dim listCardHost As Panel = listCard
        Try
            listCardHost = UiTheme.GetCardContentHost(listCard)
        Catch
        End Try
        listCardHost.Controls.Add(gridLayout)
        categoriesSplit.Panel2.Controls.Add(listCard)

        contentArea.Controls.Add(categoriesSplit)
        rightColumn.Controls.Add(contentArea)
        rightColumn.Controls.Add(topBar)

        rootTable.Controls.Add(sidebar, 0, 0)
        rootTable.Controls.Add(rightColumn, 1, 0)

        Me.Controls.Add(rootTable)
        Me.Controls.Add(statusStrip)

        AddHandler categoriesSplit.SplitterMoved, Sub(s, ev) ConfigureCategoriesSplit(categoriesSplit)
        AddHandler Me.Resize, Sub(s, ev) ConfigureCategoriesSplit(categoriesSplit)

        Me.ResumeLayout(True)
        ConfigureCategoriesSplit(categoriesSplit)

        suppressFilterEvents = True
        cmbFilter.SelectedIndex = 0
        suppressFilterEvents = False
    End Sub

    Private Sub ConfigureCategoriesSplit(categoriesSplit As SplitContainer)
        If categoriesSplit Is Nothing OrElse categoriesSplit.Width <= 0 Then
            Return
        End If

        Dim target As Integer = Math.Max(categoriesSplit.Panel1MinSize, CInt(categoriesSplit.Width * 0.34R))
        If target <> categoriesSplit.SplitterDistance Then
            categoriesSplit.SplitterDistance = target
        End If
    End Sub

    Private Sub LoadCategories()
        Try
            categoriesTable = New DataTable()
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()
                Dim sql As String =
                    "SELECT c.category_id, c.category_name, c.is_active, " &
                    " (SELECT COUNT(*) FROM dbo.products p WHERE p.category_id = c.category_id AND p.is_active = 1) AS active_products " &
                    "FROM dbo.categories c ORDER BY c.category_name;"
                Using adapter As New SqlDataAdapter(sql, connection)
                    adapter.Fill(categoriesTable)
                End Using
            End Using

            ApplyCategoryFilter()
            ShowStatus("Categories loaded.", False)
        Catch ex As Exception
            ShowStatus("Could not load categories: " & ex.Message, True)
        End Try
    End Sub

    Private Sub ApplyCategoryFilter()
        If categoriesTable Is Nothing Then
            Return
        End If

        Dim view As New DataView(categoriesTable)
        Select Case cmbFilter.SelectedIndex
            Case 1
                view.RowFilter = String.Empty
            Case 2
                view.RowFilter = "is_active = 0"
            Case Else
                view.RowFilter = "is_active = 1"
        End Select

        dgvCategories.DataSource = view
        ConfigureCategoryGridColumns()
    End Sub

    Private Sub dgvCategories_DataBindingComplete(sender As Object, e As DataGridViewBindingCompleteEventArgs)
        ConfigureCategoryGridColumns()
    End Sub

    Private Sub CategoriesForm_Shown(sender As Object, e As EventArgs)
        ScheduleCategoryGridColumnLayout()
    End Sub

    Private Sub CategoriesForm_Resize(sender As Object, e As EventArgs)
        ScheduleCategoryGridColumnLayout()
    End Sub

    Private Sub dgvCategories_Resize(sender As Object, e As EventArgs)
        ScheduleCategoryGridColumnLayout()
    End Sub

    Private Sub ConfigureCategoryGridColumns()
        If dgvCategories.Columns.Count = 0 Then
            Return
        End If

        GridDisplayHelper.ApplyStandardBoundGridDisplay(dgvCategories)

        If dgvCategories.Columns.Contains("is_active") Then
            Dim statusCol As DataGridViewColumn = dgvCategories.Columns("is_active")
            statusCol.HeaderText = "Active"
            statusCol.SortMode = DataGridViewColumnSortMode.NotSortable
        End If

        If dgvCategories.Columns.Contains("category_name") Then
            dgvCategories.Columns("category_name").HeaderText = "Category"
        End If

        If dgvCategories.Columns.Contains("active_products") Then
            Dim productsCol As DataGridViewColumn = dgvCategories.Columns("active_products")
            productsCol.HeaderText = "Products"
            productsCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            productsCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter
            productsCol.SortMode = DataGridViewColumnSortMode.NotSortable
        End If

        ScheduleCategoryGridColumnLayout()
    End Sub

    Private Sub ScheduleCategoryGridColumnLayout()
        If dgvCategories Is Nothing OrElse dgvCategories.IsDisposed OrElse dgvCategories.Columns.Count = 0 Then
            Return
        End If

        If Not Me.IsHandleCreated OrElse Not dgvCategories.IsHandleCreated Then
            Return
        End If

        If categoryGridLayoutPending Then
            Return
        End If

        Try
            categoryGridLayoutPending = True
            BeginInvoke(New MethodInvoker(
                Sub()
                    Try
                        ApplyCategoryGridColumnLayout()
                    Finally
                        categoryGridLayoutPending = False
                    End Try
                End Sub))
        Catch
            categoryGridLayoutPending = False
        End Try
    End Sub

    ''' <summary>
    ''' Active status and product count stay at readable fixed widths; category name fills the rest.
    ''' </summary>
    Private Sub ApplyCategoryGridColumnLayout()
        If dgvCategories Is Nothing OrElse dgvCategories.IsDisposed OrElse dgvCategories.Columns.Count = 0 Then
            Return
        End If

        Dim available As Integer = dgvCategories.ClientSize.Width
        If available <= 0 Then
            Return
        End If

        If dgvCategories.DisplayedRowCount(False) < dgvCategories.RowCount Then
            available -= SystemInformation.VerticalScrollBarWidth
        End If

        dgvCategories.SuspendLayout()
        Try
            dgvCategories.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            dgvCategories.HorizontalScrollingOffset = 0
            dgvCategories.ColumnHeadersDefaultCellStyle.Padding = New Padding(6, 0, 6, 0)

            Dim statusWidth As Integer = GetCategoryGridHeaderWidth("is_active", GridStatusColumnWidth)
            Dim productsWidth As Integer = GetCategoryGridHeaderWidth("active_products", GridProductsColumnWidth)

            If dgvCategories.Columns.Contains("is_active") Then
                Dim statusCol As DataGridViewColumn = dgvCategories.Columns("is_active")
                statusCol.DisplayIndex = 0
                statusCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                statusCol.Width = statusWidth
                statusCol.MinimumWidth = statusWidth
                statusCol.SortMode = DataGridViewColumnSortMode.NotSortable
            End If

            If dgvCategories.Columns.Contains("category_name") Then
                Dim categoryCol As DataGridViewColumn = dgvCategories.Columns("category_name")
                categoryCol.DisplayIndex = 1
                categoryCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                categoryCol.FillWeight = GridCategoryFillWeight
                categoryCol.MinimumWidth = GridCategoryMinWidth
            End If

            If dgvCategories.Columns.Contains("active_products") Then
                Dim productsCol As DataGridViewColumn = dgvCategories.Columns("active_products")
                productsCol.DisplayIndex = 2
                productsCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                productsCol.Width = productsWidth
                productsCol.MinimumWidth = productsWidth
                productsCol.SortMode = DataGridViewColumnSortMode.NotSortable
            End If

            Dim usedWidth As Integer = 0
            For Each col As DataGridViewColumn In dgvCategories.Columns
                If col.Visible Then
                    usedWidth += col.Width
                End If
            Next

            If dgvCategories.Columns.Contains("category_name") AndAlso usedWidth < available Then
                dgvCategories.Columns("category_name").Width += available - usedWidth
            End If
        Finally
            dgvCategories.ResumeLayout(True)
            dgvCategories.HorizontalScrollingOffset = 0
        End Try
    End Sub

    Private Function GetCategoryGridHeaderWidth(columnName As String, fallbackWidth As Integer) As Integer
        If dgvCategories Is Nothing OrElse Not dgvCategories.Columns.Contains(columnName) Then
            Return fallbackWidth
        End If

        Dim col As DataGridViewColumn = dgvCategories.Columns(columnName)
        Dim headerText As String = col.HeaderText
        If String.IsNullOrWhiteSpace(headerText) Then
            Return fallbackWidth
        End If

        Dim fontToUse As Font = dgvCategories.ColumnHeadersDefaultCellStyle.Font
        If fontToUse Is Nothing Then
            fontToUse = dgvCategories.Font
        End If

        Dim measured As Size = TextRenderer.MeasureText(headerText, fontToUse)
        Dim horizontalPadding As Integer =
            dgvCategories.ColumnHeadersDefaultCellStyle.Padding.Left +
            dgvCategories.ColumnHeadersDefaultCellStyle.Padding.Right

        Return Math.Max(fallbackWidth, measured.Width + horizontalPadding + 20)
    End Function

    Private Function GetSelectedCategoryId() As Integer?
        If dgvCategories.CurrentRow Is Nothing Then
            Return Nothing
        End If

        Dim row As DataGridViewRow = dgvCategories.CurrentRow
        If row.Cells("category_id").Value Is Nothing OrElse row.Cells("category_id").Value Is DBNull.Value Then
            Return Nothing
        End If

        Return Convert.ToInt32(row.Cells("category_id").Value)
    End Function

    Private Sub dgvCategories_SelectionChanged(sender As Object, e As EventArgs) Handles dgvCategories.SelectionChanged
        ClearInputError()
        btnReactivate.Enabled = False

        If dgvCategories.CurrentRow Is Nothing Then
            Return
        End If

        Dim row As DataGridViewRow = dgvCategories.CurrentRow
        txtCategoryName.Text = row.Cells("category_name").Value.ToString()

        Dim isActive As Boolean = True
        If row.Cells("is_active").Value IsNot Nothing AndAlso row.Cells("is_active").Value IsNot DBNull.Value Then
            isActive = Convert.ToBoolean(row.Cells("is_active").Value)
        End If

        btnDeactivate.Enabled = isActive
        btnUpdate.Enabled = True
        btnReactivate.Enabled = Not isActive
    End Sub

    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        ClearInputError()
        Dim name As String = txtCategoryName.Text.Trim()
        If name.Length = 0 Then
            ShowInputError("Enter a category name.")
            txtCategoryName.Focus()
            Return
        End If

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()
                Dim sql As String = "INSERT INTO dbo.categories (category_name) VALUES (@name);"
                Using cmd As New SqlCommand(sql, connection)
                    cmd.Parameters.AddWithValue("@name", name)
                    cmd.ExecuteNonQuery()
                End Using
            End Using

            AuditLogger.LogAudit("CATEGORY_ADD", "Added category '" & name & "'.", AppSession.CurrentRole)
            txtCategoryName.Clear()
            LoadCategories()
            ShowStatus("Category added.", False)
        Catch ex As SqlException When ex.Number = 2627 OrElse ex.Number = 2601
            ShowInputError("A category with that name already exists.")
        Catch ex As Exception
            ShowStatus("Add failed: " & ex.Message, True)
        End Try
    End Sub

    Private Sub btnUpdate_Click(sender As Object, e As EventArgs) Handles btnUpdate.Click
        ClearInputError()
        Dim id As Integer? = GetSelectedCategoryId()
        If Not id.HasValue Then
            ShowInputError("Select a category to update.")
            Return
        End If

        Dim name As String = txtCategoryName.Text.Trim()
        If name.Length = 0 Then
            ShowInputError("Enter a category name.")
            txtCategoryName.Focus()
            Return
        End If

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()
                Dim sql As String =
                    "UPDATE dbo.categories SET category_name = @name WHERE category_id = @id;"
                Using cmd As New SqlCommand(sql, connection)
                    cmd.Parameters.AddWithValue("@name", name)
                    cmd.Parameters.AddWithValue("@id", id.Value)
                    cmd.ExecuteNonQuery()
                End Using
            End Using

            AuditLogger.LogAudit("CATEGORY_UPDATE", "Updated category #" & id.Value.ToString(CultureInfo.InvariantCulture) & " to '" & name & "'.", AppSession.CurrentRole)
            LoadCategories()
            ShowStatus("Category updated.", False)
        Catch ex As SqlException When ex.Number = 2627 OrElse ex.Number = 2601
            ShowInputError("A category with that name already exists.")
        Catch ex As Exception
            ShowStatus("Update failed: " & ex.Message, True)
        End Try
    End Sub

    Private Sub btnDeactivate_Click(sender As Object, e As EventArgs) Handles btnDeactivate.Click
        ClearInputError()
        Dim id As Integer? = GetSelectedCategoryId()
        If Not id.HasValue Then
            ShowInputError("Select a category to deactivate.")
            Return
        End If

        Dim result As DialogResult = MessageBox.Show(
            "Deactivate this category? Products will keep the link but the category will be hidden from pick lists.",
            AppBranding.ApplicationName,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)
        If result <> DialogResult.Yes Then
            Return
        End If

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()
                Dim sql As String = "UPDATE dbo.categories SET is_active = 0 WHERE category_id = @id;"
                Using cmd As New SqlCommand(sql, connection)
                    cmd.Parameters.AddWithValue("@id", id.Value)
                    cmd.ExecuteNonQuery()
                End Using
            End Using

            AuditLogger.LogAudit("CATEGORY_DEACTIVATE", "Deactivated category #" & id.Value.ToString(CultureInfo.InvariantCulture), AppSession.CurrentRole)
            LoadCategories()
            ShowStatus("Category deactivated.", False)
        Catch ex As Exception
            ShowStatus("Deactivate failed: " & ex.Message, True)
        End Try
    End Sub

    Private Sub btnReactivate_Click(sender As Object, e As EventArgs) Handles btnReactivate.Click
        ClearInputError()
        Dim id As Integer? = GetSelectedCategoryId()
        If Not id.HasValue Then
            ShowInputError("Select an inactive category to reactivate.")
            Return
        End If

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()
                Dim sql As String = "UPDATE dbo.categories SET is_active = 1 WHERE category_id = @id;"
                Using cmd As New SqlCommand(sql, connection)
                    cmd.Parameters.AddWithValue("@id", id.Value)
                    cmd.ExecuteNonQuery()
                End Using
            End Using

            AuditLogger.LogAudit("CATEGORY_REACTIVATE", "Reactivated category #" & id.Value.ToString(CultureInfo.InvariantCulture), AppSession.CurrentRole)
            LoadCategories()
            ShowStatus("Category reactivated.", False)
        Catch ex As Exception
            ShowStatus("Reactivate failed: " & ex.Message, True)
        End Try
    End Sub

    Private Sub btnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefresh.Click
        LoadCategories()
    End Sub

    Private Sub btnBack_Click(sender As Object, e As EventArgs) Handles btnBack.Click
        Me.Close()
    End Sub

    Private Sub cmbFilter_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbFilter.SelectedIndexChanged
        If suppressFilterEvents Then
            Return
        End If

        ApplyCategoryFilter()
    End Sub

    Private Sub ClearInputError()
        lblInputError.Text = String.Empty
        lblInputError.Visible = False
    End Sub

    Private Sub ShowInputError(message As String)
        lblInputError.Text = message
        lblInputError.Visible = True
    End Sub

    Private Sub ShowStatus(message As String, isError As Boolean)
        FormStatusHelper.ShowTimedStatus(statusLabel, statusClearTimer, message, isError)
    End Sub

End Class
