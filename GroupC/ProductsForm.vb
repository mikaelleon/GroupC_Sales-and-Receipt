Imports System.Data
Imports System.Globalization
Imports System.IO
Imports System.Drawing
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient

Public Class ProductsForm

    Private Enum ProductFilterMode
        ActiveOnly = 0
        AllProducts = 1
        InactiveOnly = 2
    End Enum

    Private txtProductName As TextBox
    Private numPrice As NumericUpDown
    Private WithEvents txtSearch As TextBox
    Private WithEvents dgvProducts As DataGridView
    Private lblGridMessage As Label

    Private WithEvents cmbFilter As ComboBox
    Private WithEvents btnReactivate As Button

    Private WithEvents btnAdd As Button
    Private WithEvents btnUpdate As Button
    Private WithEvents btnDelete As Button
    Private WithEvents btnRefresh As Button
    Private WithEvents btnTestDb As Button
    Private WithEvents btnImportCsv As Button

    Private statusStrip As StatusStrip
    Private statusLabel As ToolStripStatusLabel
    Private WithEvents statusClearTimer As Timer

    Private productsTable As DataTable
    Private productsView As DataView

    ''' <summary>
    ''' Blocks filter ComboBox events while controls still building — parenting can fire SelectedIndexChanged before grid exists.
    ''' </summary>
    Private suppressProductFilterEvents As Boolean

    Private Sub ProductsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "Group C - Manage Products"
        Me.MinimumSize = New Size(640, 480)
        Me.Size = New Size(780, 580)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 10)

        statusClearTimer = New Timer()
        statusClearTimer.Interval = 3000

        Try
            DatabaseInitializer.EnsureDatabase()
        Catch
        End Try

        CreateControls()
        LoadProducts()
    End Sub

    Private Sub CreateControls()
        suppressProductFilterEvents = True
        Dim root As New TableLayoutPanel()
        root.Dock = DockStyle.Fill
        root.ColumnCount = 1
        root.RowCount = 3
        root.Padding = New Padding(12)
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim header As New TableLayoutPanel()
        header.AutoSize = True
        header.ColumnCount = 1
        header.RowCount = 2
        header.Dock = DockStyle.Fill

        Dim title As New Label()
        title.Text = "ADD / MANAGE PRODUCTS"
        title.Font = New Font("Segoe UI", 15.0F, FontStyle.Bold)
        title.ForeColor = UiTheme.Navy
        title.TextAlign = ContentAlignment.MiddleCenter
        title.Dock = DockStyle.Fill
        title.AutoSize = True
        title.Margin = New Padding(0, 0, 0, 8)

        Dim inputGrid As New TableLayoutPanel()
        inputGrid.AutoSize = True
        inputGrid.ColumnCount = 6
        inputGrid.RowCount = 4
        inputGrid.Dock = DockStyle.Fill
        For i As Integer = 0 To 5
            If i = 1 OrElse i = 3 Then
                inputGrid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 22.0F))
            ElseIf i = 0 OrElse i = 2 Then
                inputGrid.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            Else
                inputGrid.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            End If
        Next
        inputGrid.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        inputGrid.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        inputGrid.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        inputGrid.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim lblName As New Label()
        lblName.Text = "Product name"
        lblName.AutoSize = True
        lblName.Anchor = AnchorStyles.Left
        lblName.Margin = New Padding(0, 6, 8, 6)

        txtProductName = New TextBox()
        txtProductName.MaxLength = 100
        txtProductName.Dock = DockStyle.Fill
        txtProductName.Margin = New Padding(0, 4, 12, 4)
        txtProductName.TabIndex = 0

        Dim lblPrice As New Label()
        lblPrice.Text = "Price (₱)"
        lblPrice.AutoSize = True
        lblPrice.Margin = New Padding(0, 6, 8, 6)

        numPrice = New NumericUpDown()
        numPrice.DecimalPlaces = 2
        numPrice.Minimum = 0.01D
        numPrice.Maximum = 999999.99D
        numPrice.Increment = 1D
        numPrice.ThousandsSeparator = True
        numPrice.Dock = DockStyle.Fill
        numPrice.Margin = New Padding(0, 4, 12, 4)
        numPrice.TabIndex = 1
        numPrice.TextAlign = HorizontalAlignment.Right

        btnAdd = New Button()
        btnAdd.Text = "&Add"
        btnAdd.AutoSize = True
        btnAdd.MinimumSize = New Size(100, 32)
        btnAdd.TabIndex = 2
        UiTheme.ApplyPrimaryButton(btnAdd)

        btnUpdate = New Button()
        btnUpdate.Text = "&Update"
        btnUpdate.AutoSize = True
        btnUpdate.MinimumSize = New Size(100, 32)
        btnUpdate.TabIndex = 3
        UiTheme.ApplyPrimaryButton(btnUpdate)

        btnDelete = New Button()
        btnDelete.Text = "&Deactivate"
        btnDelete.AutoSize = True
        btnDelete.MinimumSize = New Size(100, 32)
        btnDelete.TabIndex = 4
        UiTheme.ApplyPrimaryButton(btnDelete)

        btnRefresh = New Button()
        btnRefresh.Text = "&Refresh"
        btnRefresh.AutoSize = True
        btnRefresh.MinimumSize = New Size(100, 32)
        btnRefresh.TabIndex = 5
        UiTheme.ApplyPrimaryButton(btnRefresh)

        btnTestDb = New Button()
        btnTestDb.Text = "Te&st DB"
        btnTestDb.AutoSize = True
        btnTestDb.MinimumSize = New Size(100, 32)
        btnTestDb.TabIndex = 6
        UiTheme.ApplyPrimaryButton(btnTestDb)

        btnImportCsv = New Button()
        btnImportCsv.Text = "Import &CSV…"
        btnImportCsv.AutoSize = True
        btnImportCsv.MinimumSize = New Size(120, 32)
        btnImportCsv.TabIndex = 10
        UiTheme.ApplyPrimaryButton(btnImportCsv)

        Dim lblSearch As New Label()
        lblSearch.Text = "Search"
        lblSearch.AutoSize = True
        lblSearch.Margin = New Padding(0, 6, 8, 6)

        txtSearch = New TextBox()
        txtSearch.Dock = DockStyle.Fill
        txtSearch.Margin = New Padding(0, 4, 12, 4)
        txtSearch.TabIndex = 7
        txtSearch.PlaceholderText = "Filter by product name"

        Dim lblFilter As New Label()
        lblFilter.Text = "Show"
        lblFilter.AutoSize = True
        lblFilter.Margin = New Padding(0, 6, 8, 6)

        cmbFilter = New ComboBox()
        cmbFilter.DropDownStyle = ComboBoxStyle.DropDownList
        cmbFilter.Dock = DockStyle.Fill
        cmbFilter.Margin = New Padding(0, 4, 12, 4)
        cmbFilter.TabIndex = 8
        cmbFilter.Items.AddRange(New Object() {"Active products only", "All products", "Inactive only"})

        btnReactivate = New Button()
        btnReactivate.Text = "&Reactivate"
        btnReactivate.AutoSize = True
        btnReactivate.MinimumSize = New Size(110, 32)
        btnReactivate.TabIndex = 9
        btnReactivate.Enabled = False
        UiTheme.ApplySecondaryButton(btnReactivate)

        inputGrid.Controls.Add(lblName, 0, 0)
        inputGrid.Controls.Add(txtProductName, 1, 0)
        inputGrid.Controls.Add(lblPrice, 2, 0)
        inputGrid.Controls.Add(numPrice, 3, 0)
        inputGrid.Controls.Add(btnAdd, 4, 0)
        inputGrid.Controls.Add(btnUpdate, 5, 0)

        inputGrid.Controls.Add(lblSearch, 0, 1)
        inputGrid.Controls.Add(txtSearch, 1, 1)
        inputGrid.SetColumnSpan(txtSearch, 3)
        inputGrid.Controls.Add(btnDelete, 4, 1)
        inputGrid.Controls.Add(btnRefresh, 5, 1)

        inputGrid.Controls.Add(lblFilter, 0, 2)
        inputGrid.Controls.Add(cmbFilter, 1, 2)
        inputGrid.SetColumnSpan(cmbFilter, 2)
        inputGrid.Controls.Add(btnReactivate, 3, 2)
        inputGrid.Controls.Add(btnTestDb, 4, 2)
        inputGrid.SetColumnSpan(btnTestDb, 2)

        Dim lblCsv As New Label()
        lblCsv.Text = "Bulk CSV (name, price)"
        lblCsv.AutoSize = True
        lblCsv.Margin = New Padding(0, 6, 8, 6)

        inputGrid.Controls.Add(lblCsv, 0, 3)
        inputGrid.SetColumnSpan(lblCsv, 2)
        inputGrid.Controls.Add(btnImportCsv, 2, 3)
        inputGrid.SetColumnSpan(btnImportCsv, 4)

        header.Controls.Add(title, 0, 0)
        header.Controls.Add(inputGrid, 0, 1)

        Dim gridHost As New Panel()
        gridHost.Dock = DockStyle.Fill
        gridHost.Padding = New Padding(0, 8, 0, 0)

        lblGridMessage = New Label()
        lblGridMessage.Dock = DockStyle.Fill
        lblGridMessage.TextAlign = ContentAlignment.MiddleCenter
        lblGridMessage.Font = New Font("Segoe UI", 10.0F, FontStyle.Italic)
        lblGridMessage.ForeColor = Color.DimGray
        lblGridMessage.Visible = False
        lblGridMessage.Text = "Could not load products. Check LocalDB and App.config (GroupCSqlServer)."

        dgvProducts = New DataGridView()
        dgvProducts.Dock = DockStyle.Fill
        dgvProducts.ReadOnly = True
        dgvProducts.AllowUserToAddRows = False
        dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvProducts.MultiSelect = False
        dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dgvProducts.BackgroundColor = Color.White
        dgvProducts.BorderStyle = BorderStyle.FixedSingle
        dgvProducts.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue
        dgvProducts.TabIndex = 10

        gridHost.Controls.Add(dgvProducts)
        gridHost.Controls.Add(lblGridMessage)
        lblGridMessage.BringToFront()

        suppressProductFilterEvents = False

        statusStrip = New StatusStrip()
        statusLabel = New ToolStripStatusLabel("Ready.")
        statusLabel.Spring = True
        statusStrip.Items.Add(statusLabel)

        root.Controls.Add(header, 0, 0)
        root.Controls.Add(gridHost, 0, 1)
        root.Controls.Add(statusStrip, 0, 2)

        cmbFilter.SelectedIndex = 0

        Me.Controls.Clear()
        Me.Controls.Add(root)
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
            dgvProducts.Rows(e.RowIndex).DefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
            dgvProducts.Rows(e.RowIndex).DefaultCellStyle.ForeColor = Color.DimGray
        Else
            dgvProducts.Rows(e.RowIndex).DefaultCellStyle.BackColor = Color.Empty
            dgvProducts.Rows(e.RowIndex).DefaultCellStyle.ForeColor = Color.Empty
        End If
    End Sub

    Private Sub statusClearTimer_Tick(sender As Object, e As EventArgs) Handles statusClearTimer.Tick
        statusClearTimer.Stop()
        statusLabel.Text = "Ready."
    End Sub

    Private Sub ShowStatus(message As String)
        statusLabel.Text = message
        statusClearTimer.Stop()
        statusClearTimer.Start()
    End Sub

    Private Sub txtSearch_TextChanged(sender As Object, e As EventArgs) Handles txtSearch.TextChanged
        ApplySearchFilter()
    End Sub

    Private Sub ApplySearchFilter()
        If productsView Is Nothing Then
            Return
        End If

        Dim term As String = txtSearch.Text.Trim()
        If term.Length = 0 Then
            productsView.RowFilter = String.Empty
            Return
        End If

        Dim safe As String = term.Replace("'", "''").Replace("*", "[*]").Replace("%", "[%]")
        productsView.RowFilter = "product_name LIKE '%" & safe & "%'"
    End Sub

    Private Sub dgvProducts_DataBindingComplete(sender As Object, e As DataGridViewBindingCompleteEventArgs) Handles dgvProducts.DataBindingComplete
        FormatProductColumns()
        UpdateReactivateEnabled()
    End Sub

    Private Sub FormatProductColumns()
        If dgvProducts.Columns.Count = 0 Then
            Return
        End If

        If dgvProducts.Columns.Contains("id") Then
            dgvProducts.Columns("id").Visible = False
        End If

        If dgvProducts.Columns.Contains("product_name") Then
            dgvProducts.Columns("product_name").HeaderText = "Product"
            dgvProducts.Columns("product_name").MinimumWidth = 120
        End If

        If dgvProducts.Columns.Contains("price") Then
            dgvProducts.Columns("price").HeaderText = "Price (₱)"
            dgvProducts.Columns("price").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            dgvProducts.Columns("price").DefaultCellStyle.Format = "N2"
        End If

        If dgvProducts.Columns.Contains("is_active") Then
            dgvProducts.Columns("is_active").HeaderText = "Active"
            dgvProducts.Columns("is_active").Width = 70
        End If
    End Sub

    Private Function GetFilterMode() As ProductFilterMode
        Return CType(cmbFilter.SelectedIndex, ProductFilterMode)
    End Function

    Private Sub btnTestDb_Click(sender As Object, e As EventArgs) Handles btnTestDb.Click
        Try
            DatabaseInitializer.EnsureDatabase()
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()
                MessageBox.Show("LocalDB connection OK.", "SQL Server LocalDB", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Using
        Catch ex As Exception
            MessageBox.Show("Connection failed: " & ex.Message, "SQL Server LocalDB", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        Dim productName As String = txtProductName.Text.Trim()

        If productName = String.Empty Then
            MessageBox.Show("Please enter product name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim price As Decimal = numPrice.Value

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
                    "INSERT INTO products (product_name, price) VALUES (@product_name, @price);"

                Using insertCmd As New SqlCommand(insertSql, connection)
                    insertCmd.Parameters.AddWithValue("@product_name", productName)
                    insertCmd.Parameters.AddWithValue("@price", price)
                    insertCmd.ExecuteNonQuery()
                End Using

                AuditLogger.LogProduct(connection, "INSERT", Nothing, productName, "Added product")
            End Using

            ClearInputs()
            LoadProducts()
            ShowStatus("Product added.")
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
        If dgvProducts.SelectedRows.Count = 0 Then
            MessageBox.Show("Select a product first.", "Products", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim productId As Integer = Convert.ToInt32(dgvProducts.SelectedRows(0).Cells("id").Value)
        Dim productName As String = txtProductName.Text.Trim()
        Dim price As Decimal = numPrice.Value

        If productName = String.Empty Then
            MessageBox.Show("Enter a valid product name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
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

                Dim query As String =
                    "UPDATE products " &
                    "SET product_name = @product_name, price = @price, is_active = 1, updated_at = SYSUTCDATETIME() " &
                    "WHERE id = @id;"

                Using command As New SqlCommand(query, connection)
                    command.Parameters.AddWithValue("@id", productId)
                    command.Parameters.AddWithValue("@product_name", productName)
                    command.Parameters.AddWithValue("@price", price)
                    command.ExecuteNonQuery()
                End Using

                AuditLogger.LogProduct(connection, "UPDATE", productId, productName, "Updated product")
            End Using

            ClearInputs()
            LoadProducts()
            ShowStatus("Product updated.")
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

        Dim confirm As DialogResult = MessageBox.Show(
            "Deactivate this product? It will be hidden from active lists.",
            "Confirm",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)

        If confirm <> DialogResult.Yes Then
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
            End Using

            ClearInputs()
            LoadProducts()
            ShowStatus("Product deactivated.")
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
            ShowStatus("Product reactivated.")
        Catch ex As Exception
            MessageBox.Show("Error reactivating product: " & ex.Message, "Database", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ErrorLogger.Log(ex, NameOf(ProductsForm) & "." & NameOf(btnReactivate_Click))
        End Try
    End Sub

    Private Sub btnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefresh.Click
        ClearInputs()
        LoadProducts()
        ShowStatus("List refreshed.")
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
                            "USING (SELECT @n AS product_name, @p AS price) AS s " &
                            "ON t.product_name = s.product_name " &
                            "WHEN MATCHED THEN UPDATE SET t.price = s.price, t.is_active = 1, t.updated_at = SYSUTCDATETIME() " &
                            "WHEN NOT MATCHED THEN INSERT (product_name, price) VALUES (s.product_name, s.price);"

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

                            If productName.Length = 0 OrElse unitPrice <= 0D Then
                                skipped += 1
                                Continue For
                            End If

                            Using cmd As New SqlCommand(mergeSql, connection, transaction)
                                cmd.Parameters.AddWithValue("@n", productName)
                                cmd.Parameters.AddWithValue("@p", unitPrice)
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
            ShowStatus(String.Format(CultureInfo.CurrentCulture, "CSV import done. Merged: {0}. Skipped: {1}.", mergedOk, skipped))
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

        UpdateReactivateEnabled()
    End Sub

    Private Sub LoadProducts()
        If dgvProducts Is Nothing OrElse lblGridMessage Is Nothing Then
            Return
        End If

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Dim whereClause As String = String.Empty
                Select Case GetFilterMode()
                    Case ProductFilterMode.ActiveOnly
                        whereClause = "WHERE is_active = 1"
                    Case ProductFilterMode.InactiveOnly
                        whereClause = "WHERE is_active = 0"
                    Case Else
                        whereClause = String.Empty
                End Select

                Dim query As String =
                    "SELECT id, product_name, price, is_active " &
                    "FROM products " &
                    whereClause &
                    " ORDER BY product_name;"

                Using adapter As New SqlDataAdapter(query, connection)
                    productsTable = New DataTable()
                    adapter.Fill(productsTable)
                End Using
            End Using

            productsView = New DataView(productsTable)
            dgvProducts.DataSource = productsView
            dgvProducts.Visible = True
            lblGridMessage.Visible = False
            ApplySearchFilter()
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
        txtProductName.Focus()
    End Sub

End Class
