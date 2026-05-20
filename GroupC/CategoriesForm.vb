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
        Me.BackColor = UiTheme.FormBackground

        txtCategoryName = New TextBox() With {.MaxLength = MaxCategoryNameLength, .Font = New Font("Segoe UI", 11)}
        UiTheme.ApplyFilledTextInputVisual(txtCategoryName)

        cmbFilter = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 180}
        cmbFilter.Items.AddRange(New Object() {"Active categories", "All categories", "Inactive only"})
        UiTheme.ApplyTableLayoutDropDown(cmbFilter)

        btnAdd = New Button() With {.Text = "&Add category", .Size = New Size(130, 38), .Cursor = Cursors.Hand}
        btnUpdate = New Button() With {.Text = "&Update name", .Size = New Size(120, 38), .Cursor = Cursors.Hand}
        btnDeactivate = New Button() With {.Text = "&Deactivate", .Size = New Size(110, 38), .Cursor = Cursors.Hand}
        btnReactivate = New Button() With {.Text = "Reactivate", .Size = New Size(110, 38), .Enabled = False, .Cursor = Cursors.Hand}
        btnRefresh = New Button() With {.Text = "Refresh", .Size = New Size(90, 34), .Cursor = Cursors.Hand}
        btnBack = New Button() With {.Text = "← Back to Menu", .Size = New Size(140, 36), .Cursor = Cursors.Hand}

        UiTheme.ApplyPrimaryButton(btnAdd)
        UiTheme.ApplyPrimaryButton(btnUpdate)
        UiTheme.ApplyWarningButton(btnDeactivate)
        UiTheme.ApplySuccessButton(btnReactivate)
        UiTheme.ApplySecondaryButton(btnRefresh)
        UiTheme.ApplySecondaryButton(btnBack)

        dgvCategories = New DataGridView() With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        }
        UiTheme.ApplyDataGridViewChrome(dgvCategories)
        AddHandler dgvCategories.DataBindingComplete, AddressOf dgvCategories_DataBindingComplete

        lblInputError = New Label() With {.AutoSize = True, .ForeColor = UiTheme.Danger, .Visible = False}

        statusStrip = New StatusStrip()
        statusLabel = New ToolStripStatusLabel(FormStatusHelper.ReadyText) With {.Spring = True}
        statusStrip.Items.Add(statusLabel)
        UiTheme.ApplyStatusStripTheme(statusStrip)

        Dim root As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1, .Margin = Padding.Empty}
        root.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 360.0F))
        root.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

        Dim sidebar As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(25, 30, 25, 30)}
        Dim sideStack As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 4}
        sideStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        sideStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        sideStack.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        sideStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim hdr As New Label() With {
            .Text = "Book categories",
            .Font = New Font("Segoe UI", 16.0F, FontStyle.Bold),
            .ForeColor = UiTheme.PrimaryAccent,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 6)
        }
        Dim hint As New Label() With {
            .Text = "Assign categories to products on the Products screen.",
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic),
            .ForeColor = UiTheme.TextSecondary,
            .AutoSize = True,
            .MaximumSize = New Size(300, 0),
            .Margin = New Padding(0, 0, 0, 16)
        }

        Dim lblName As Label = UiTheme.CreateSecondaryLabel("Category name")
        txtCategoryName.Margin = New Padding(0, 0, 0, 12)
        txtCategoryName.Dock = DockStyle.Fill

        Dim actionFlow As New FlowLayoutPanel() With {.AutoSize = True, .FlowDirection = FlowDirection.TopDown, .WrapContents = False}
        actionFlow.Controls.Add(btnAdd)
        actionFlow.Controls.Add(btnUpdate)
        actionFlow.Controls.Add(btnDeactivate)
        actionFlow.Controls.Add(btnReactivate)

        Dim headerPanel As New TableLayoutPanel() With {.AutoSize = True, .ColumnCount = 1, .RowCount = 3}
        headerPanel.Controls.Add(hdr, 0, 0)
        headerPanel.Controls.Add(hint, 0, 1)
        headerPanel.Controls.Add(lblInputError, 0, 2)

        Dim inputLayout As New TableLayoutPanel() With {
            .AutoSize = True,
            .ColumnCount = 1,
            .RowCount = 3,
            .Margin = New Padding(0, 20, 0, 0)
        }
        inputLayout.Controls.Add(lblName, 0, 0)
        inputLayout.Controls.Add(txtCategoryName, 0, 1)
        inputLayout.Controls.Add(actionFlow, 0, 2)

        Dim pnlFooter As New FlowLayoutPanel() With {
            .Dock = DockStyle.Bottom,
            .AutoSize = True,
            .FlowDirection = FlowDirection.TopDown
        }
        btnBack.Margin = New Padding(0, 30, 0, 0)
        pnlFooter.Controls.Add(btnBack)

        sideStack.Controls.Add(headerPanel, 0, 0)
        sideStack.Controls.Add(inputLayout, 0, 1)
        sideStack.Controls.Add(pnlFooter, 0, 3)

        sidebar.Controls.Add(sideStack)

        Dim gridHost As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(20, 24, 24, 16), .BackColor = UiTheme.FormBackground}
        Dim gridStack As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .RowCount = 2, .ColumnCount = 1}
        gridStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        gridStack.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

        Dim toolbar As New FlowLayoutPanel() With {.AutoSize = True, .WrapContents = False, .Margin = New Padding(0, 0, 0, 10)}
        toolbar.Controls.Add(UiTheme.CreateSecondaryLabel("Show"))
        toolbar.Controls.Add(cmbFilter)
        toolbar.Controls.Add(btnRefresh)

        Dim gridCard As Panel = UiTheme.CreateCardPanel(New Padding(8))
        gridCard.Dock = DockStyle.Fill
        UiTheme.GetCardContentHost(gridCard).Controls.Add(dgvCategories)

        gridStack.Controls.Add(toolbar, 0, 0)
        gridStack.Controls.Add(gridCard, 0, 1)
        gridHost.Controls.Add(gridStack)

        root.Controls.Add(sidebar, 0, 0)
        root.Controls.Add(gridHost, 1, 0)

        Dim shell As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .RowCount = 2, .ColumnCount = 1}
        shell.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        shell.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        shell.Controls.Add(root, 0, 0)
        shell.Controls.Add(statusStrip, 0, 1)

        Me.Controls.Add(shell)
        Me.ResumeLayout(True)

        suppressFilterEvents = True
        cmbFilter.SelectedIndex = 0
        suppressFilterEvents = False
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

    Private Sub ConfigureCategoryGridColumns()
        If dgvCategories.Columns.Count = 0 Then
            Return
        End If

        GridDisplayHelper.ApplyStandardBoundGridDisplay(dgvCategories)

        If dgvCategories.Columns.Contains("category_name") Then
            dgvCategories.Columns("category_name").HeaderText = "Category"
        End If

        If dgvCategories.Columns.Contains("is_active") Then
            dgvCategories.Columns("is_active").HeaderText = "Active"
            dgvCategories.Columns("is_active").Width = 64
        End If

        If dgvCategories.Columns.Contains("active_products") Then
            dgvCategories.Columns("active_products").HeaderText = "Active products"
            dgvCategories.Columns("active_products").Width = 110
        End If

        GridDisplayHelper.MoveActiveStatusColumnToLeft(dgvCategories)
    End Sub

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
