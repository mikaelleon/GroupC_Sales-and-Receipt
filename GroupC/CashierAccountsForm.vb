Imports System.Data
Imports System.Globalization
Imports System.Drawing
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient

''' <summary>
''' Administrator-only registration and maintenance of database-backed cashier accounts.
''' </summary>
Public Class CashierAccountsForm
    Inherits Form

    Private WithEvents txtUsername As TextBox
    Private WithEvents txtDisplayName As TextBox
    Private WithEvents txtPassword As TextBox
    Private WithEvents txtConfirmPassword As TextBox
    Private WithEvents cmbFilter As ComboBox
    Private WithEvents dgvCashiers As DataGridView
    Private WithEvents btnRegister As Button
    Private WithEvents btnUpdateDisplay As Button
    Private WithEvents btnResetPassword As Button
    Private WithEvents btnDeactivate As Button
    Private WithEvents btnReactivate As Button
    Private WithEvents btnRefresh As Button
    Private WithEvents btnBack As Button

    Private lblInputError As Label
    Private statusStrip As StatusStrip
    Private statusLabel As ToolStripStatusLabel
    Private WithEvents statusClearTimer As Timer

    Private cashiersTable As DataTable
    Private suppressFilterEvents As Boolean
    Private editingExistingAccount As Boolean

    Private Sub CashierAccountsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = AppBranding.WindowTitle("Manage Cashiers")
        UiTheme.ApplyMaximizedWorkspaceDefaults(Me, 960, 580)

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
        LoadCashiers()
    End Sub

    Private Sub BuildLayout()
        Me.SuspendLayout()
        Me.Controls.Clear()
        Me.BackColor = UiTheme.FormBackground

        txtUsername = New TextBox() With {
            .MaxLength = CashierAccountService.MaxUsernameLength,
            .Font = New Font("Segoe UI", 11)
        }
        txtDisplayName = New TextBox() With {
            .MaxLength = CashierAccountService.MaxDisplayNameLength,
            .Font = New Font("Segoe UI", 11)
        }
        txtPassword = New TextBox() With {.Font = New Font("Segoe UI", 11), .UseSystemPasswordChar = True}
        txtConfirmPassword = New TextBox() With {.Font = New Font("Segoe UI", 11), .UseSystemPasswordChar = True}

        UiTheme.ApplyFilledTextInputVisual(txtUsername)
        UiTheme.ApplyFilledTextInputVisual(txtDisplayName)
        UiTheme.ApplyFilledTextInputVisual(txtPassword)
        UiTheme.ApplyFilledTextInputVisual(txtConfirmPassword)

        cmbFilter = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 180}
        cmbFilter.Items.AddRange(New Object() {"Active cashiers", "All cashiers", "Inactive only"})
        UiTheme.ApplyTableLayoutDropDown(cmbFilter)

        btnRegister = New Button() With {.Text = "&Register cashier", .Size = New Size(150, 38), .Cursor = Cursors.Hand}
        btnUpdateDisplay = New Button() With {.Text = "Update &display name", .Size = New Size(160, 38), .Cursor = Cursors.Hand}
        btnResetPassword = New Button() With {.Text = "&Reset password", .Size = New Size(140, 38), .Cursor = Cursors.Hand}
        btnDeactivate = New Button() With {.Text = "&Deactivate", .Size = New Size(110, 38), .Cursor = Cursors.Hand}
        btnReactivate = New Button() With {.Text = "Reactivate", .Size = New Size(110, 38), .Enabled = False, .Cursor = Cursors.Hand}
        btnRefresh = New Button() With {.Text = "Refresh", .Size = New Size(90, 34), .Cursor = Cursors.Hand}
        btnBack = New Button() With {.Text = "← Back to Menu", .Size = New Size(140, 36), .Cursor = Cursors.Hand}

        UiTheme.ApplyPrimaryButton(btnRegister)
        UiTheme.ApplyPrimaryButton(btnUpdateDisplay)
        UiTheme.ApplyPrimaryButton(btnResetPassword)
        UiTheme.ApplyWarningButton(btnDeactivate)
        UiTheme.ApplySuccessButton(btnReactivate)
        UiTheme.ApplySecondaryButton(btnRefresh)
        UiTheme.ApplySecondaryButton(btnBack)

        dgvCashiers = New DataGridView() With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        }
        UiTheme.ApplyDataGridViewChrome(dgvCashiers)
        AddHandler dgvCashiers.DataBindingComplete, AddressOf dgvCashiers_DataBindingComplete

        lblInputError = New Label() With {.AutoSize = True, .ForeColor = UiTheme.Danger, .Visible = False}

        statusStrip = New StatusStrip()
        statusLabel = New ToolStripStatusLabel(FormStatusHelper.ReadyText) With {.Spring = True}
        statusStrip.Items.Add(statusLabel)
        UiTheme.ApplyStatusStripTheme(statusStrip)

        Dim root As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1, .Margin = Padding.Empty}
        root.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 380.0F))
        root.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

        Dim sidebar As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(25, 30, 25, 30)}
        Dim sideStack As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 4}
        sideStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        sideStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        sideStack.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        sideStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim hdr As New Label() With {
            .Text = "Cashier accounts",
            .Font = New Font("Segoe UI", 16.0F, FontStyle.Bold),
            .ForeColor = UiTheme.PrimaryAccent,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 6)
        }
        Dim hint As New Label() With {
            .Text = "Register usernames and passwords for cashiers. Only administrators can manage accounts.",
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic),
            .ForeColor = UiTheme.TextSecondary,
            .AutoSize = True,
            .MaximumSize = New Size(320, 0),
            .Margin = New Padding(0, 0, 0, 16)
        }

        Dim CreateFieldLabel = Function(text As String) UiTheme.CreateSecondaryLabel(text)

        Dim inputLayout As New TableLayoutPanel() With {
            .AutoSize = True,
            .ColumnCount = 1,
            .RowCount = 10,
            .Margin = New Padding(0, 20, 0, 0)
        }
        inputLayout.Controls.Add(CreateFieldLabel("Username"), 0, 0)
        inputLayout.Controls.Add(txtUsername, 0, 1)
        inputLayout.Controls.Add(CreateFieldLabel("Display name (optional)"), 0, 2)
        inputLayout.Controls.Add(txtDisplayName, 0, 3)
        inputLayout.Controls.Add(CreateFieldLabel("Password"), 0, 4)
        inputLayout.Controls.Add(txtPassword, 0, 5)
        inputLayout.Controls.Add(CreateFieldLabel("Confirm password"), 0, 6)
        inputLayout.Controls.Add(txtConfirmPassword, 0, 7)

        Dim actionFlow As New FlowLayoutPanel() With {.AutoSize = True, .FlowDirection = FlowDirection.TopDown, .WrapContents = False, .Margin = New Padding(0, 16, 0, 0)}
        actionFlow.Controls.Add(btnRegister)
        actionFlow.Controls.Add(btnUpdateDisplay)
        actionFlow.Controls.Add(btnResetPassword)
        actionFlow.Controls.Add(btnDeactivate)
        actionFlow.Controls.Add(btnReactivate)
        inputLayout.Controls.Add(actionFlow, 0, 8)

        Dim headerPanel As New TableLayoutPanel() With {.AutoSize = True, .ColumnCount = 1, .RowCount = 3}
        headerPanel.Controls.Add(hdr, 0, 0)
        headerPanel.Controls.Add(hint, 0, 1)
        headerPanel.Controls.Add(lblInputError, 0, 2)

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
        UiTheme.GetCardContentHost(gridCard).Controls.Add(dgvCashiers)

        gridStack.Controls.Add(toolbar, 0, 0)
        gridStack.Controls.Add(gridCard, 0, 1)
        gridHost.Controls.Add(gridStack)

        root.Controls.Add(sidebar, 0, 0)
        root.Controls.Add(gridHost, 1, 0)

        Me.Controls.Add(root)
        Me.Controls.Add(statusStrip)

        suppressFilterEvents = True
        cmbFilter.SelectedIndex = 0
        suppressFilterEvents = False

        SetNewAccountMode()
        Me.ResumeLayout(True)
    End Sub

    Private Sub SetNewAccountMode()
        editingExistingAccount = False
        txtUsername.ReadOnly = False
        btnRegister.Enabled = True
        btnUpdateDisplay.Enabled = False
        btnResetPassword.Enabled = False
        btnDeactivate.Enabled = False
        btnReactivate.Enabled = False
    End Sub

    Private Sub LoadCashiers()
        cashiersTable = CashierAccountService.LoadAccountsTable()
        ApplyFilter()
        ConfigureGridColumns()
        ClearInputFields()
        SetNewAccountMode()
        ShowStatus(FormStatusHelper.ReadyText, False)
    End Sub

    Private Sub ApplyFilter()
        If cashiersTable Is Nothing Then
            Return
        End If

        Dim view As New DataView(cashiersTable)
        Select Case cmbFilter.SelectedIndex
            Case 1
                view.RowFilter = String.Empty
            Case 2
                view.RowFilter = "is_active = 0"
            Case Else
                view.RowFilter = "is_active = 1"
        End Select

        dgvCashiers.DataSource = view
        ConfigureGridColumns()
    End Sub

    Private Sub dgvCashiers_DataBindingComplete(sender As Object, e As DataGridViewBindingCompleteEventArgs)
        ConfigureGridColumns()
    End Sub

    Private Sub ConfigureGridColumns()
        If dgvCashiers.Columns Is Nothing OrElse dgvCashiers.Columns.Count = 0 Then
            Return
        End If

        GridDisplayHelper.ApplyStandardBoundGridDisplay(dgvCashiers)

        If dgvCashiers.Columns.Contains("username") Then
            dgvCashiers.Columns("username").HeaderText = "Username"
        End If

        If dgvCashiers.Columns.Contains("display_name") Then
            dgvCashiers.Columns("display_name").HeaderText = "Display name"
        End If

        If dgvCashiers.Columns.Contains("is_active") Then
            dgvCashiers.Columns("is_active").HeaderText = "Active"
            dgvCashiers.Columns("is_active").Width = 56
        End If

        If dgvCashiers.Columns.Contains("last_login_at") Then
            dgvCashiers.Columns("last_login_at").HeaderText = "Last sign-in"
            dgvCashiers.Columns("last_login_at").DefaultCellStyle.Format = "g"
        End If

        If dgvCashiers.Columns.Contains("created_at") Then
            dgvCashiers.Columns("created_at").HeaderText = "Registered"
            dgvCashiers.Columns("created_at").DefaultCellStyle.Format = "g"
        End If
    End Sub

    Private Function GetSelectedCashierId() As Integer?
        If dgvCashiers.CurrentRow Is Nothing Then
            Return Nothing
        End If

        Dim row As DataGridViewRow = dgvCashiers.CurrentRow
        If row.Cells("cashier_id").Value Is Nothing OrElse row.Cells("cashier_id").Value Is DBNull.Value Then
            Return Nothing
        End If

        Return Convert.ToInt32(row.Cells("cashier_id").Value)
    End Function

    Private Sub dgvCashiers_SelectionChanged(sender As Object, e As EventArgs) Handles dgvCashiers.SelectionChanged
        ClearInputError()
        btnReactivate.Enabled = False

        Dim id As Integer? = GetSelectedCashierId()
        If Not id.HasValue Then
            SetNewAccountMode()
            Return
        End If

        editingExistingAccount = True
        Dim row As DataGridViewRow = dgvCashiers.CurrentRow
        txtUsername.Text = row.Cells("username").Value.ToString()
        txtUsername.ReadOnly = True

        Dim displayObj As Object = row.Cells("display_name").Value
        txtDisplayName.Text = If(displayObj Is Nothing OrElse displayObj Is DBNull.Value, String.Empty, displayObj.ToString())
        txtPassword.Clear()
        txtConfirmPassword.Clear()

        Dim isActive As Boolean = True
        If row.Cells("is_active").Value IsNot Nothing AndAlso row.Cells("is_active").Value IsNot DBNull.Value Then
            isActive = Convert.ToBoolean(row.Cells("is_active").Value)
        End If

        btnRegister.Enabled = False
        btnUpdateDisplay.Enabled = True
        btnResetPassword.Enabled = True
        btnDeactivate.Enabled = isActive
        btnReactivate.Enabled = Not isActive
    End Sub

    Private Function PasswordsMatch() As Boolean
        Return String.Equals(txtPassword.Text, txtConfirmPassword.Text, StringComparison.Ordinal)
    End Function

    Private Sub btnRegister_Click(sender As Object, e As EventArgs) Handles btnRegister.Click
        ClearInputError()

        Dim username As String = txtUsername.Text.Trim()
        Dim userError As String = String.Empty
        If Not CashierAccountService.ValidateUsername(username, userError) Then
            ShowInputError(userError)
            txtUsername.Focus()
            Return
        End If

        If Not PasswordsMatch() Then
            ShowInputError("Password and confirmation do not match.")
            txtConfirmPassword.Focus()
            Return
        End If

        Dim passError As String = String.Empty
        If Not CashierAccountService.ValidatePassword(txtPassword.Text, passError) Then
            ShowInputError(passError)
            txtPassword.Focus()
            Return
        End If

        Try
            CashierAccountService.RegisterAccount(username, txtPassword.Text, txtDisplayName.Text)
            AuditLogger.LogAudit("CASHIER_REGISTER", "Registered cashier '" & username & "'.", AppSession.GetAuditIdentity())
            LoadCashiers()
            ShowStatus("Cashier account registered.", False)
        Catch ex As SqlException When ex.Number = 2627 OrElse ex.Number = 2601
            ShowInputError("That username is already registered.")
        Catch ex As Exception
            ShowStatus("Registration failed: " & ex.Message, True)
        End Try
    End Sub

    Private Sub btnUpdateDisplay_Click(sender As Object, e As EventArgs) Handles btnUpdateDisplay.Click
        ClearInputError()
        Dim id As Integer? = GetSelectedCashierId()
        If Not id.HasValue Then
            ShowInputError("Select a cashier account to update.")
            Return
        End If

        Try
            CashierAccountService.UpdateDisplayName(id.Value, txtDisplayName.Text)
            AuditLogger.LogAudit("CASHIER_UPDATE", "Updated display name for cashier #" & id.Value.ToString(CultureInfo.InvariantCulture), AppSession.GetAuditIdentity())
            LoadCashiers()
            ShowStatus("Display name updated.", False)
        Catch ex As Exception
            ShowStatus("Update failed: " & ex.Message, True)
        End Try
    End Sub

    Private Sub btnResetPassword_Click(sender As Object, e As EventArgs) Handles btnResetPassword.Click
        ClearInputError()
        Dim id As Integer? = GetSelectedCashierId()
        If Not id.HasValue Then
            ShowInputError("Select a cashier account to reset the password.")
            Return
        End If

        If Not PasswordsMatch() Then
            ShowInputError("Password and confirmation do not match.")
            txtConfirmPassword.Focus()
            Return
        End If

        Dim passError As String = String.Empty
        If Not CashierAccountService.ValidatePassword(txtPassword.Text, passError) Then
            ShowInputError(passError)
            txtPassword.Focus()
            Return
        End If

        Dim result As DialogResult = MessageBox.Show(
            "Reset the password for this cashier account?",
            AppBranding.ApplicationName,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)
        If result <> DialogResult.Yes Then
            Return
        End If

        Try
            CashierAccountService.ResetPassword(id.Value, txtPassword.Text)
            AuditLogger.LogAudit("CASHIER_PASSWORD_RESET", "Reset password for cashier #" & id.Value.ToString(CultureInfo.InvariantCulture), AppSession.GetAuditIdentity())
            txtPassword.Clear()
            txtConfirmPassword.Clear()
            ShowStatus("Password reset.", False)
        Catch ex As Exception
            ShowStatus("Password reset failed: " & ex.Message, True)
        End Try
    End Sub

    Private Sub btnDeactivate_Click(sender As Object, e As EventArgs) Handles btnDeactivate.Click
        ClearInputError()
        Dim id As Integer? = GetSelectedCashierId()
        If Not id.HasValue Then
            ShowInputError("Select a cashier account to deactivate.")
            Return
        End If

        Dim result As DialogResult = MessageBox.Show(
            "Deactivate this cashier account? They will not be able to sign in until reactivated.",
            AppBranding.ApplicationName,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)
        If result <> DialogResult.Yes Then
            Return
        End If

        Try
            CashierAccountService.SetActive(id.Value, False)
            AuditLogger.LogAudit("CASHIER_DEACTIVATE", "Deactivated cashier #" & id.Value.ToString(CultureInfo.InvariantCulture), AppSession.GetAuditIdentity())
            LoadCashiers()
            ShowStatus("Cashier deactivated.", False)
        Catch ex As Exception
            ShowStatus("Deactivate failed: " & ex.Message, True)
        End Try
    End Sub

    Private Sub btnReactivate_Click(sender As Object, e As EventArgs) Handles btnReactivate.Click
        ClearInputError()
        Dim id As Integer? = GetSelectedCashierId()
        If Not id.HasValue Then
            ShowInputError("Select an inactive cashier to reactivate.")
            Return
        End If

        Try
            CashierAccountService.SetActive(id.Value, True)
            AuditLogger.LogAudit("CASHIER_REACTIVATE", "Reactivated cashier #" & id.Value.ToString(CultureInfo.InvariantCulture), AppSession.GetAuditIdentity())
            LoadCashiers()
            ShowStatus("Cashier reactivated.", False)
        Catch ex As Exception
            ShowStatus("Reactivate failed: " & ex.Message, True)
        End Try
    End Sub

    Private Sub btnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefresh.Click
        LoadCashiers()
    End Sub

    Private Sub btnBack_Click(sender As Object, e As EventArgs) Handles btnBack.Click
        Me.Close()
    End Sub

    Private Sub cmbFilter_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbFilter.SelectedIndexChanged
        If suppressFilterEvents Then
            Return
        End If

        ApplyFilter()
        ConfigureGridColumns()
    End Sub

    Private Sub ClearInputFields()
        txtUsername.Clear()
        txtDisplayName.Clear()
        txtPassword.Clear()
        txtConfirmPassword.Clear()
    End Sub

    Private Sub ClearInputError()
        lblInputError.Visible = False
        lblInputError.Text = String.Empty
    End Sub

    Private Sub ShowInputError(message As String)
        lblInputError.Text = message
        lblInputError.Visible = True
    End Sub

    Private Sub ShowStatus(message As String, isError As Boolean)
        FormStatusHelper.ShowTimedStatus(statusLabel, statusClearTimer, message, isError)
    End Sub

    Private Sub statusClearTimer_Tick(sender As Object, e As EventArgs) Handles statusClearTimer.Tick
        statusClearTimer.Stop()
        FormStatusHelper.ResetTimedStatus(statusLabel)
    End Sub

End Class
