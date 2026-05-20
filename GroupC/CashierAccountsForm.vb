Imports System.Data
Imports System.Globalization
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient

''' <summary>
''' Administrator-only registration and maintenance of database-backed cashier accounts.
''' </summary>
Public Class CashierAccountsForm
    Inherits Form

    Private Shared ReadOnly SurfaceGray As Color = Color.FromArgb(&HF5, &HF7, &HFA)
    Private Shared ReadOnly BrandBlue As Color = UiTheme.SecondaryAccent
    Private Shared ReadOnly BrandBlueLight As Color = Color.FromArgb(&HE8, &HF4, &HFC)
    Private Shared ReadOnly BorderLight As Color = Color.FromArgb(&HD0, &HDC, &HE8)
    Private Shared ReadOnly SuccessGreen As Color = UiTheme.Success
    Private Shared ReadOnly SuccessBg As Color = Color.FromArgb(&HE8, &HF5, &HEE)
    Private Shared ReadOnly MutedBg As Color = Color.FromArgb(&HF1, &HF5, &HF9)
    Private Shared ReadOnly DangerBg As Color = Color.FromArgb(&HFE, &HE2, &HE2)
    Private Const StatusClearMs As Integer = 4000
    Private Const FieldWidth As Integer = 252
    Private Const FieldHeight As Integer = 34

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
    Private lblStatus As Label
    Private lblNoSelection As Label
    Private lblPassHint As Label
    Private lblPassMatch As Label
    Private lblChipUsername As Label
    Private WithEvents btnShowPass As Button
    Private WithEvents btnClearSelection As Button
    Private pnlSelectedChip As Panel
    Private pnlEmptyState As Panel
    Private pnlLeft As Panel
    Private pnlRight As Panel
    Private pnlToolbar As Panel
    Private pnlBottomBar As Panel
    Private pnlAvatar As Panel
    Private picEmptyIcon As PictureBox
    Private lblEmptyTitle As Label
    Private lblEmptySub As Label
    Private toolTips As ToolTip
    Private WithEvents statusClearTimer As Timer
    Private WithEvents fieldHighlightTimer As Timer

    Private cashiersTable As DataTable
    Private suppressFilterEvents As Boolean
    Private editingExistingAccount As Boolean
    Private passwordVisible As Boolean
    Private selectedUsername As String
    Private selectedDisplayName As String
    Private highlightRestore As Dictionary(Of TextBox, Color)

    Private Sub CashierAccountsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "International Bookstore — Manage Cashiers"
        UiTheme.ApplyMaximizedWorkspaceDefaults(Me, 900, 600)
        Me.BackColor = SurfaceGray
        Me.Font = UiTheme.StandardUiFont
        Me.AcceptButton = Nothing
        Me.CancelButton = Nothing

        Try
            UiTheme.ApplyStandardWindowChrome(Me)
            Me.BackColor = SurfaceGray
        Catch
        End Try

        statusClearTimer = New Timer() With {.Interval = StatusClearMs}
        fieldHighlightTimer = New Timer() With {.Interval = 3000}
        highlightRestore = New Dictionary(Of TextBox, Color)()
        toolTips = New ToolTip() With {.AutoPopDelay = 8000, .InitialDelay = 400, .ReshowDelay = 200, .ShowAlways = True}

        Try
            DatabaseInitializer.EnsureDatabase()
        Catch
        End Try

        BuildLayout()
        WireTooltips()
        AddHandler txtPassword.TextChanged, AddressOf PasswordFields_TextChanged
        AddHandler txtConfirmPassword.TextChanged, AddressOf PasswordFields_TextChanged
        AddHandler dgvCashiers.CellFormatting, AddressOf dgvCashiers_CellFormatting
        AddHandler dgvCashiers.RowsAdded, AddressOf dgvCashiers_RowsChanged
        AddHandler dgvCashiers.RowsRemoved, AddressOf dgvCashiers_RowsChanged
        AddHandler fieldHighlightTimer.Tick, AddressOf fieldHighlightTimer_Tick
        For Each field As TextBox In New TextBox() {txtUsername, txtDisplayName, txtPassword, txtConfirmPassword}
            AddHandler field.TextChanged, AddressOf InputField_TextChanged
        Next

        LoadCashiers()
    End Sub

    Private Sub BuildLayout()
        Me.SuspendLayout()
        Me.Controls.Clear()
        Me.BackColor = SurfaceGray

        txtUsername = CreateStyledTextBox(CashierAccountService.MaxUsernameLength, "3–50 chars, letters/numbers/_")
        txtDisplayName = CreateStyledTextBox(CashierAccountService.MaxDisplayNameLength, "Shown on receipts")
        txtPassword = CreateStyledTextBox(0, String.Empty)
        txtPassword.UseSystemPasswordChar = True
        txtConfirmPassword = CreateStyledTextBox(0, String.Empty)
        txtConfirmPassword.UseSystemPasswordChar = True

        cmbFilter = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Width = 190,
            .Height = FieldHeight,
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.White,
            .Font = New Font("Segoe UI", 10.0F)
        }
        cmbFilter.Items.AddRange(New Object() {"Active cashiers", "Inactive cashiers", "All cashiers"})

        btnRegister = New Button() With {.Text = "+ Register Cashier", .Size = New Size(FieldWidth, 42), .Cursor = Cursors.Hand}
        btnUpdateDisplay = New Button() With {.Text = "✏ Update Display Name", .Size = New Size(FieldWidth, 38), .Visible = False, .Cursor = Cursors.Hand}
        btnResetPassword = New Button() With {.Text = "🔑 Reset Password", .Size = New Size(FieldWidth, 38), .Visible = False, .Cursor = Cursors.Hand}
        btnDeactivate = New Button() With {.Text = "⊘ Deactivate Account", .Size = New Size(FieldWidth, 38), .Visible = False, .Cursor = Cursors.Hand}
        btnReactivate = New Button() With {.Text = "✓ Reactivate Account", .Size = New Size(FieldWidth, 38), .Visible = False, .Cursor = Cursors.Hand}
        btnRefresh = New Button() With {
            .Text = "↻  Refresh",
            .Size = New Size(100, FieldHeight),
            .Cursor = Cursors.Hand,
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.White,
            .ForeColor = ColorTranslator.FromHtml("#1B7EC2"),
            .Font = New Font("Segoe UI", 10.0F)
        }
        btnRefresh.FlatAppearance.BorderSize = 1
        btnRefresh.FlatAppearance.BorderColor = BorderLight

        btnBack = New Button() With {
            .Text = "← Back to Menu",
            .Size = New Size(150, 36),
            .Location = New Point(24, 10),
            .Cursor = Cursors.Hand,
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.White,
            .ForeColor = ColorTranslator.FromHtml("#1B7EC2"),
            .Font = New Font("Segoe UI", 10.0F)
        }
        btnBack.FlatAppearance.BorderSize = 1
        btnBack.FlatAppearance.BorderColor = BorderLight
        btnShowPass = New Button() With {
            .Text = "👁",
            .Size = New Size(26, FieldHeight),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.White,
            .ForeColor = UiTheme.TextSecondary,
            .Cursor = Cursors.Hand,
            .TabStop = False
        }
        btnShowPass.FlatAppearance.BorderSize = 0

        UiTheme.ApplyPrimaryButton(btnRegister)
        UiTheme.ApplySecondaryButton(btnUpdateDisplay)
        UiTheme.ApplySecondaryButton(btnResetPassword)
        UiTheme.ApplyDangerButton(btnDeactivate)
        UiTheme.ApplySecondaryButton(btnReactivate)
        btnReactivate.ForeColor = SuccessGreen
        dgvCashiers = New DataGridView() With {.Dock = DockStyle.Fill}
        SetupDataGridView()
        AddHandler dgvCashiers.DataBindingComplete, AddressOf dgvCashiers_DataBindingComplete

        lblInputError = New Label() With {.AutoSize = True, .ForeColor = UiTheme.Danger, .Visible = False}
        lblPassHint = New Label() With {
            .Text = "Minimum 6 characters",
            .AutoSize = True,
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Italic),
            .ForeColor = UiTheme.TextSecondary,
            .Margin = New Padding(0, 4, 0, 0)
        }
        lblPassMatch = New Label() With {
            .AutoSize = True,
            .Visible = False,
            .Font = New Font("Segoe UI", 9.0F)
        }
        lblNoSelection = New Label() With {
            .Text = "Select a cashier from the list to manage their account.",
            .AutoSize = True,
            .MaximumSize = New Size(FieldWidth, 0),
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic),
            .ForeColor = UiTheme.TextSecondary
        }

        pnlSelectedChip = New Panel() With {
            .Height = 40,
            .Width = FieldWidth,
            .BackColor = BrandBlueLight,
            .Visible = False,
            .Margin = New Padding(0, 0, 0, 8)
        }
        AddHandler pnlSelectedChip.Paint, AddressOf SelectedChip_Paint

        pnlAvatar = New Panel() With {
            .Size = New Size(28, 28),
            .Location = New Point(6, 6),
            .BackColor = Color.Transparent
        }
        AddHandler pnlAvatar.Paint, AddressOf Avatar_Paint

        lblChipUsername = New Label() With {
            .AutoSize = False,
            .Location = New Point(40, 0),
            .Size = New Size(170, 40),
            .TextAlign = ContentAlignment.MiddleLeft,
            .Font = New Font("Segoe UI", 10.0F),
            .ForeColor = UiTheme.TextPrimary
        }

        btnClearSelection = New Button() With {
            .Text = "×",
            .Size = New Size(28, 28),
            .Location = New Point(FieldWidth - 34, 6),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.Transparent,
            .ForeColor = UiTheme.TextSecondary,
            .Cursor = Cursors.Hand,
            .TabStop = False
        }
        btnClearSelection.FlatAppearance.BorderSize = 0

        pnlSelectedChip.Controls.Add(pnlAvatar)
        pnlSelectedChip.Controls.Add(lblChipUsername)
        pnlSelectedChip.Controls.Add(btnClearSelection)

        BuildEmptyStatePanel()
        AnchorLeftPanelTextBoxes()

        pnlBottomBar = New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 56,
            .BackColor = Color.White
        }
        AddHandler pnlBottomBar.Paint, Sub(s, e)
                                           Using pen As New Pen(BorderLight, 1.0F)
                                               e.Graphics.DrawLine(pen, 0, 0, pnlBottomBar.Width, 0)
                                           End Using
                                       End Sub

        lblStatus = New Label() With {
            .AutoSize = True,
            .Anchor = AnchorStyles.Right Or AnchorStyles.Top,
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic),
            .ForeColor = UiTheme.TextSecondary,
            .Text = String.Empty
        }
        pnlBottomBar.Controls.Add(lblStatus)
        AddHandler pnlBottomBar.Resize, Sub()
                                            lblStatus.Location = New Point(
                                                pnlBottomBar.Width - lblStatus.Width - 24,
                                                (pnlBottomBar.Height - lblStatus.Height) \ 2)
                                        End Sub

        pnlLeft = New Panel() With {
            .Dock = DockStyle.Left,
            .Width = 300,
            .MinimumSize = New Size(300, 0),
            .MaximumSize = New Size(300, 9999),
            .BackColor = Color.White,
            .AutoScroll = True,
            .Padding = New Padding(24, 20, 24, 0)
        }
        AddHandler pnlLeft.Paint, Sub(s, e)
                                      Using pen As New Pen(BorderLight, 1.0F)
                                          e.Graphics.DrawLine(pen, pnlLeft.Width - 1, 0, pnlLeft.Width - 1, pnlLeft.Height)
                                      End Using
                                  End Sub

        Dim leftStack As New TableLayoutPanel() With {
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .ColumnCount = 1,
            .Width = FieldWidth,
            .MaximumSize = New Size(FieldWidth, 0)
        }

        leftStack.Controls.Add(CreateTitleLabel("Cashier Accounts"), 0, 0)
        leftStack.Controls.Add(CreateSubtitleLabel(
            "Register usernames and passwords for cashiers. Only administrators can manage accounts."), 0, 1)
        leftStack.Controls.Add(CreateDivider(), 0, 2)

        leftStack.Controls.Add(CreateSectionLabel("New Account"), 0, 3)
        leftStack.Controls.Add(CreateFieldGroup("Username", txtUsername), 0, 4)
        leftStack.Controls.Add(CreateFieldGroup("Display name (optional)", txtDisplayName), 0, 5)

        Dim passGroup As New TableLayoutPanel() With {.AutoSize = True, .ColumnCount = 1}
        passGroup.Controls.Add(CreateFieldCaption("Password"), 0, 0)
        passGroup.Controls.Add(CreatePasswordRow(txtPassword, btnShowPass), 0, 1)
        passGroup.Controls.Add(lblPassHint, 0, 2)
        leftStack.Controls.Add(passGroup, 0, 6)

        Dim confirmGroup As New TableLayoutPanel() With {.AutoSize = True, .ColumnCount = 1}
        confirmGroup.Controls.Add(CreateFieldCaption("Confirm password"), 0, 0)
        confirmGroup.Controls.Add(CreatePasswordRow(txtConfirmPassword, Nothing), 0, 1)
        confirmGroup.Controls.Add(lblPassMatch, 0, 2)
        leftStack.Controls.Add(confirmGroup, 0, 7)

        btnRegister.Margin = New Padding(0, 20, 0, 0)
        leftStack.Controls.Add(btnRegister, 0, 8)
        leftStack.Controls.Add(CreateDividerWithLabel("— Account actions —", New Padding(0, 16, 0, 12)), 0, 9)
        leftStack.Controls.Add(pnlSelectedChip, 0, 10)
        leftStack.Controls.Add(lblNoSelection, 0, 11)

        Dim actionStack As New FlowLayoutPanel() With {
            .FlowDirection = FlowDirection.TopDown,
            .AutoSize = True,
            .WrapContents = False,
            .Margin = Padding.Empty
        }
        btnUpdateDisplay.Margin = New Padding(0, 8, 0, 0)
        btnResetPassword.Margin = New Padding(0, 8, 0, 0)
        btnDeactivate.Margin = New Padding(0, 8, 0, 0)
        btnReactivate.Margin = New Padding(0, 8, 0, 0)
        actionStack.Controls.Add(btnUpdateDisplay)
        actionStack.Controls.Add(btnResetPassword)
        actionStack.Controls.Add(btnDeactivate)
        actionStack.Controls.Add(btnReactivate)
        leftStack.Controls.Add(actionStack, 0, 12)
        leftStack.Controls.Add(lblInputError, 0, 13)

        Dim pnlLeftFooter As New FlowLayoutPanel() With {
            .Dock = DockStyle.Bottom,
            .AutoSize = True,
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .Padding = New Padding(0, 0, 0, 20)
        }
        btnBack.Margin = New Padding(0, 30, 0, 0)
        pnlLeftFooter.Controls.Add(btnBack)

        Dim pnlLeftBody As New Panel() With {.Dock = DockStyle.Fill, .AutoScroll = True}
        pnlLeftBody.Controls.Add(leftStack)

        pnlLeft.Controls.Add(pnlLeftBody)
        pnlLeft.Controls.Add(pnlLeftFooter)

        pnlRight = New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = SurfaceGray,
            .Padding = Padding.Empty
        }

        pnlToolbar = New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 56,
            .BackColor = Color.White,
            .Padding = New Padding(24, 0, 24, 0)
        }
        AddHandler pnlToolbar.Paint, Sub(s, e)
                                         Using pen As New Pen(BorderLight, 1.0F)
                                             e.Graphics.DrawLine(pen, 0, pnlToolbar.Height - 1, pnlToolbar.Width, pnlToolbar.Height - 1)
                                         End Using
                                     End Sub

        Dim lblShow As New Label() With {
            .Text = "Show:",
            .AutoSize = True,
            .ForeColor = ColorTranslator.FromHtml("#5A6A7A"),
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Regular)
        }
        cmbFilter.Anchor = AnchorStyles.Left Or AnchorStyles.Top
        btnRefresh.Anchor = AnchorStyles.Left Or AnchorStyles.Top
        pnlToolbar.Controls.Add(lblShow)
        pnlToolbar.Controls.Add(cmbFilter)
        pnlToolbar.Controls.Add(btnRefresh)
        CenterToolbarControls(lblShow)

        dgvCashiers.Margin = New Padding(24, 0, 24, 0)

        pnlRight.Controls.Clear()
        pnlRight.Controls.Add(dgvCashiers)
        pnlRight.Controls.Add(pnlEmptyState)
        pnlRight.Controls.Add(pnlToolbar)

        pnlToolbar.Dock = DockStyle.Top
        dgvCashiers.Dock = DockStyle.Fill
        pnlEmptyState.Dock = DockStyle.Fill

        If pnlBottomBar.Parent IsNot Nothing Then
            pnlBottomBar.Parent.Controls.Remove(pnlBottomBar)
        End If

        Me.Controls.Clear()
        Me.Controls.Add(pnlRight)
        Me.Controls.Add(pnlLeft)
        Me.Controls.Add(pnlBottomBar)

        pnlBottomBar.Dock = DockStyle.Bottom
        pnlLeft.Dock = DockStyle.Left
        pnlRight.Dock = DockStyle.Fill
        pnlBottomBar.BringToFront()

        Me.AcceptButton = btnRegister

        suppressFilterEvents = True
        cmbFilter.SelectedIndex = 0
        suppressFilterEvents = False

        SetNewAccountMode()
        UpdateSelectionUi()
        Me.ResumeLayout(True)
        pnlBottomBar.PerformLayout()
    End Sub

    Private Sub SetupDataGridView()
        dgvCashiers.AutoGenerateColumns = False
        dgvCashiers.Columns.Clear()

        Dim colId As New DataGridViewTextBoxColumn() With {
            .Name = "colCashierId",
            .DataPropertyName = "cashier_id",
            .Visible = False
        }
        Dim colNum As New DataGridViewTextBoxColumn() With {
            .Name = "colNum",
            .HeaderText = "#",
            .Width = 48,
            .ReadOnly = True,
            .SortMode = DataGridViewColumnSortMode.NotSortable
        }
        colNum.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

        Dim colUser As New DataGridViewTextBoxColumn() With {
            .Name = "colUsername",
            .HeaderText = "Username",
            .DataPropertyName = "username",
            .Width = 160
        }
        colUser.DefaultCellStyle.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)

        Dim colDisplay As New DataGridViewTextBoxColumn() With {
            .Name = "colDisplayName",
            .HeaderText = "Display Name",
            .DataPropertyName = "display_name",
            .Width = 160
        }
        colDisplay.DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#5A6A7A")

        Dim colStatus As New DataGridViewTextBoxColumn() With {
            .Name = "colStatus",
            .HeaderText = "Status",
            .DataPropertyName = "is_active",
            .Width = 90,
            .ReadOnly = True,
            .SortMode = DataGridViewColumnSortMode.NotSortable
        }
        colStatus.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

        Dim colLastSign As New DataGridViewTextBoxColumn() With {
            .Name = "colLastSignIn",
            .HeaderText = "Last sign-in",
            .DataPropertyName = "last_login_at",
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        }

        Dim colReg As New DataGridViewTextBoxColumn() With {
            .Name = "colRegistered",
            .HeaderText = "Registered",
            .DataPropertyName = "created_at",
            .Width = 140
        }

        dgvCashiers.Columns.AddRange(colId, colStatus, colNum, colUser, colDisplay, colLastSign, colReg)
        colStatus.DisplayIndex = 0
        colNum.DisplayIndex = 1
        colUser.DisplayIndex = 2
        colDisplay.DisplayIndex = 3
        colLastSign.DisplayIndex = 4
        colReg.DisplayIndex = 5

        With dgvCashiers
            .BackgroundColor = Color.White
            .BorderStyle = BorderStyle.None
            .CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            .GridColor = BorderLight
            .RowHeadersVisible = False
            .ColumnHeadersVisible = True
            .ColumnHeadersHeight = 40
            .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            .AllowUserToAddRows = False
            .AllowUserToResizeRows = False
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
            .MultiSelect = False
            .ReadOnly = True
            .Font = New Font("Segoe UI", 10.0F)
            .RowTemplate.Height = 52
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            .ScrollBars = ScrollBars.Vertical
            .EnableHeadersVisualStyles = False
        End With

        dgvCashiers.ColumnHeadersDefaultCellStyle.BackColor = SurfaceGray
        dgvCashiers.ColumnHeadersDefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#5A6A7A")
        dgvCashiers.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        dgvCashiers.ColumnHeadersDefaultCellStyle.Padding = New Padding(12, 0, 0, 0)

        dgvCashiers.DefaultCellStyle.BackColor = Color.White
        dgvCashiers.DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#1A2332")
        dgvCashiers.DefaultCellStyle.SelectionBackColor = BrandBlueLight
        dgvCashiers.DefaultCellStyle.SelectionForeColor = ColorTranslator.FromHtml("#1A2332")
        dgvCashiers.DefaultCellStyle.Padding = New Padding(12, 0, 12, 0)

        dgvCashiers.AlternatingRowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#FAFBFD")

        AddHandler dgvCashiers.CellPainting, AddressOf dgvCashiers_StatusCellPainting
    End Sub

    Private Sub AnchorLeftPanelTextBoxes()
        For Each field As TextBox In New TextBox() {txtUsername, txtDisplayName, txtPassword, txtConfirmPassword}
            field.Width = FieldWidth
            field.Anchor = AnchorStyles.Left Or AnchorStyles.Right
        Next
    End Sub

    Private Function CreateStyledTextBox(maxLength As Integer, placeholder As String) As TextBox
        Dim tb As New TextBox() With {
            .Width = FieldWidth,
            .Height = FieldHeight,
            .Font = UiTheme.StandardUiFont,
            .BorderStyle = BorderStyle.FixedSingle,
            .Anchor = AnchorStyles.Left Or AnchorStyles.Right
        }
        If maxLength > 0 Then
            tb.MaxLength = maxLength
        End If
        If Not String.IsNullOrEmpty(placeholder) Then
            tb.PlaceholderText = placeholder
        End If
        UiTheme.ApplyFilledTextInputVisual(tb)
        Return tb
    End Function

    Private Shared Function CreateTitleLabel(text As String) As Label
        Return New Label() With {
            .Text = text,
            .AutoSize = True,
            .Font = New Font("Segoe UI", 14.0F, FontStyle.Bold),
            .ForeColor = UiTheme.TextPrimary,
            .Margin = New Padding(0, 0, 0, 4)
        }
    End Function

    Private Shared Function CreateSubtitleLabel(text As String) As Label
        Return New Label() With {
            .Text = text,
            .AutoSize = True,
            .MaximumSize = New Size(FieldWidth, 0),
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = UiTheme.TextSecondary
        }
    End Function

    Private Shared Function CreateSectionLabel(text As String) As Label
        Return New Label() With {
            .Text = text,
            .AutoSize = True,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = BrandBlue,
            .Margin = New Padding(0, 0, 0, 12)
        }
    End Function

    Private Shared Function CreateFieldCaption(text As String) As Label
        Return New Label() With {
            .Text = text,
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = UiTheme.TextSecondary,
            .Margin = New Padding(0, 0, 0, 4)
        }
    End Function

    Private Function CreateFieldGroup(caption As String, textBox As TextBox) As Control
        Dim panel As New TableLayoutPanel() With {
            .AutoSize = True,
            .ColumnCount = 1,
            .Margin = New Padding(0, 0, 0, 12)
        }
        panel.Controls.Add(CreateFieldCaption(caption), 0, 0)
        panel.Controls.Add(textBox, 0, 1)
        Return panel
    End Function

    Private Shared Function CreatePasswordRow(passwordBox As TextBox, toggleBtn As Button) As Panel
        Dim row As New Panel() With {.Size = New Size(FieldWidth, FieldHeight)}
        passwordBox.Location = New Point(0, 0)
        passwordBox.Width = If(toggleBtn Is Nothing, FieldWidth, 222)
        passwordBox.Height = FieldHeight
        row.Controls.Add(passwordBox)
        If toggleBtn IsNot Nothing Then
            toggleBtn.Location = New Point(226, 0)
            row.Controls.Add(toggleBtn)
        End If
        Return row
    End Function

    Private Shared Function CreateDivider() As Panel
        Return New Panel() With {
            .Height = 1,
            .Dock = DockStyle.Top,
            .BackColor = BorderLight,
            .Margin = New Padding(0, 16, 0, 16)
        }
    End Function

    Private Shared Function CreateDividerWithLabel(text As String, margin As Padding) As Panel
        Dim host As New Panel() With {
            .Height = 28,
            .Width = FieldWidth,
            .Margin = margin
        }
        Dim line As New Panel() With {
            .Height = 1,
            .BackColor = BorderLight,
            .Width = FieldWidth,
            .Location = New Point(0, 13)
        }
        Dim lbl As New Label() With {
            .Text = text,
            .AutoSize = False,
            .Width = FieldWidth,
            .Height = 28,
            .BackColor = Color.White,
            .ForeColor = ColorTranslator.FromHtml("#5A6A7A"),
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Regular),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        host.Controls.Add(line)
        host.Controls.Add(lbl)
        Return host
    End Function

    Private Sub BuildEmptyStatePanel()
        pnlEmptyState = New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = SurfaceGray,
            .Visible = True
        }

        picEmptyIcon = New PictureBox() With {
            .Width = 64,
            .Height = 56,
            .BackColor = Color.Transparent,
            .SizeMode = PictureBoxSizeMode.Normal
        }
        AddHandler picEmptyIcon.Paint, AddressOf picEmptyIcon_Paint

        lblEmptyTitle = New Label() With {
            .Text = "No cashier accounts yet",
            .Width = 340,
            .Height = 30,
            .AutoSize = False,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI", 14.0F, FontStyle.Regular),
            .ForeColor = ColorTranslator.FromHtml("#1A2332")
        }
        lblEmptySub = New Label() With {
            .Text = "Use the form on the left to register the first cashier.",
            .Width = 420,
            .Height = 40,
            .AutoSize = False,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Italic),
            .ForeColor = ColorTranslator.FromHtml("#5A6A7A")
        }

        pnlEmptyState.Controls.AddRange(New Control() {picEmptyIcon, lblEmptyTitle, lblEmptySub})
        AddHandler pnlEmptyState.Resize, AddressOf EmptyState_Resize
        EmptyState_Resize(pnlEmptyState, EventArgs.Empty)
    End Sub

    Private Sub picEmptyIcon_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim c As Color = ColorTranslator.FromHtml("#A8BED4")

        Using br As New SolidBrush(c)
            g.FillEllipse(br, 2, 0, 18, 18)
            g.FillEllipse(br, 0, 20, 22, 22)
            g.FillEllipse(br, 20, 0, 20, 20)
            g.FillEllipse(br, 18, 22, 24, 24)
        End Using
    End Sub

    Private Sub EmptyState_Resize(sender As Object, e As EventArgs)
        If pnlEmptyState Is Nothing OrElse picEmptyIcon Is Nothing Then
            Return
        End If

        Const totalH As Integer = 56 + 12 + 30 + 8 + 40
        Dim startY As Integer = (pnlEmptyState.Height - totalH) \ 2
        Dim cx As Integer = pnlEmptyState.Width \ 2

        picEmptyIcon.Location = New Point(cx - 32, startY)
        lblEmptyTitle.Location = New Point(cx - 170, startY + 68)
        lblEmptySub.Location = New Point(cx - 210, startY + 106)
    End Sub

    Private Sub CenterToolbarControls(lblShow As Label)
        If pnlToolbar Is Nothing Then
            Return
        End If

        lblShow.Location = New Point(0, (pnlToolbar.Height - lblShow.Height) \ 2)
        cmbFilter.Location = New Point(lblShow.Right + 10, (pnlToolbar.Height - cmbFilter.Height) \ 2)
        btnRefresh.Location = New Point(cmbFilter.Right + 10, (pnlToolbar.Height - btnRefresh.Height) \ 2)

        AddHandler pnlToolbar.Resize, Sub()
                                          lblShow.Location = New Point(0, (pnlToolbar.Height - lblShow.Height) \ 2)
                                          cmbFilter.Location = New Point(lblShow.Right + 10, (pnlToolbar.Height - cmbFilter.Height) \ 2)
                                          btnRefresh.Location = New Point(cmbFilter.Right + 10, (pnlToolbar.Height - btnRefresh.Height) \ 2)
                                      End Sub
    End Sub

    Private Sub WireTooltips()
        toolTips.SetToolTip(btnShowPass, "Show / hide password")
        toolTips.SetToolTip(btnRefresh, "Reload cashier list")
        toolTips.SetToolTip(btnDeactivate, "Prevent this cashier from signing in")
        toolTips.SetToolTip(btnReactivate, "Allow this cashier to sign in again")
        toolTips.SetToolTip(btnResetPassword, "Set a new password for this cashier")
    End Sub

    Private Sub SelectedChip_Paint(sender As Object, e As PaintEventArgs)
        Dim panel As Panel = DirectCast(sender, Panel)
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
        Using pen As New Pen(BorderLight)
            Using path As GraphicsPath = CreateRoundedRect(New Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 6)
                e.Graphics.DrawPath(pen, path)
            End Using
        End Using
    End Sub

    Private Sub Avatar_Paint(sender As Object, e As PaintEventArgs)
        Dim initials As String = GetInitials(selectedDisplayName, selectedUsername)
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
        Using brush As New SolidBrush(BrandBlue)
            e.Graphics.FillEllipse(brush, 0, 0, 27, 27)
        End Using
        TextRenderer.DrawText(
            e.Graphics,
            initials,
            New Font("Segoe UI", 9.0F, FontStyle.Bold),
            New Rectangle(0, 0, 28, 28),
            Color.White,
            TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
    End Sub

    Private Shared Function GetInitials(displayName As String, username As String) As String
        Dim source As String = If(String.IsNullOrWhiteSpace(displayName), username, displayName).Trim()
        If String.IsNullOrEmpty(source) Then
            Return "?"
        End If
        Dim parts() As String = source.Split({" "c, vbTab}, StringSplitOptions.RemoveEmptyEntries)
        If parts.Length >= 2 Then
            Return (parts(0)(0).ToString() & parts(parts.Length - 1)(0).ToString()).ToUpperInvariant()
        End If
        Return parts(0)(0).ToString().ToUpperInvariant()
    End Function

    Private Shared Function CreateRoundedRect(rect As Rectangle, radius As Integer) As GraphicsPath
        Dim path As New GraphicsPath()
        Dim d As Integer = radius * 2
        path.AddArc(rect.X, rect.Y, d, d, 180, 90)
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90)
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90)
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90)
        path.CloseFigure()
        Return path
    End Function

    Private Sub btnShowPass_Click(sender As Object, e As EventArgs) Handles btnShowPass.Click
        passwordVisible = Not passwordVisible
        txtPassword.UseSystemPasswordChar = Not passwordVisible
        txtConfirmPassword.UseSystemPasswordChar = Not passwordVisible
        btnShowPass.Text = If(passwordVisible, "🙈", "👁")
    End Sub

    Private Sub PasswordFields_TextChanged(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(txtPassword.Text) AndAlso String.IsNullOrEmpty(txtConfirmPassword.Text) Then
            lblPassMatch.Visible = False
            lblPassHint.ForeColor = UiTheme.TextSecondary
            Return
        End If

        lblPassMatch.Visible = True
        If PasswordsMatch() Then
            lblPassMatch.Text = "✓ Passwords match"
            lblPassMatch.ForeColor = SuccessGreen
        Else
            lblPassMatch.Text = "✗ Passwords do not match"
            lblPassMatch.ForeColor = UiTheme.Danger
        End If
    End Sub

    Private Sub btnClearSelection_Click(sender As Object, e As EventArgs) Handles btnClearSelection.Click
        dgvCashiers.ClearSelection()
        UpdateSelectionUi()
    End Sub

    Private Sub SetNewAccountMode()
        editingExistingAccount = False
        txtUsername.ReadOnly = False
        btnRegister.Enabled = True
    End Sub

    Private Sub UpdateSelectionUi()
        Dim hasSelection As Boolean = GetSelectedCashierId().HasValue

        pnlSelectedChip.Visible = hasSelection
        lblNoSelection.Visible = Not hasSelection
        btnUpdateDisplay.Visible = hasSelection
        btnResetPassword.Visible = hasSelection

        If hasSelection AndAlso dgvCashiers.CurrentRow IsNot Nothing Then
            Dim row As DataGridViewRow = dgvCashiers.CurrentRow
            selectedUsername = Convert.ToString(row.Cells("colUsername").Value)
            Dim displayObj As Object = row.Cells("colDisplayName").Value
            selectedDisplayName = If(displayObj Is Nothing OrElse displayObj Is DBNull.Value, String.Empty, displayObj.ToString())
            lblChipUsername.Text = selectedUsername

            Dim isActive As Boolean = True
            Dim statusVal As Object = row.Cells("colStatus").Value
            If statusVal IsNot Nothing AndAlso statusVal IsNot DBNull.Value Then
                If TypeOf statusVal Is Boolean Then
                    isActive = CBool(statusVal)
                Else
                    isActive = String.Equals(Convert.ToString(statusVal), "Active", StringComparison.OrdinalIgnoreCase)
                End If
            End If

            btnDeactivate.Visible = isActive
            btnReactivate.Visible = Not isActive
            pnlAvatar.Invalidate()
        Else
            selectedUsername = String.Empty
            selectedDisplayName = String.Empty
            btnDeactivate.Visible = False
            btnReactivate.Visible = False
        End If
    End Sub

    Private Sub UpdateEmptyState()
        Dim empty As Boolean = dgvCashiers.Rows.Count = 0
        pnlEmptyState.Visible = empty
        dgvCashiers.Visible = Not empty
    End Sub

    Private Sub dgvCashiers_RowsChanged(sender As Object, e As EventArgs)
        UpdateEmptyState()
    End Sub

    Private Sub LoadCashiers()
        cashiersTable = CashierAccountService.LoadAccountsTable()
        ApplyFilter()
        ConfigureGridColumns()
        ClearInputFields()
        SetNewAccountMode()
        UpdateSelectionUi()
        UpdateEmptyState()
        ShowStatus(String.Empty, False)
    End Sub

    Private Sub ApplyFilter()
        If cashiersTable Is Nothing Then
            Return
        End If

        Dim view As New DataView(cashiersTable)
        Select Case cmbFilter.SelectedIndex
            Case 1
                view.RowFilter = "is_active = 0"
            Case 2
                view.RowFilter = String.Empty
            Case Else
                view.RowFilter = "is_active = 1"
        End Select

        dgvCashiers.DataSource = view
        dgvCashiers.ClearSelection()
        ConfigureGridColumns()
        UpdateSelectionUi()
        UpdateEmptyState()
    End Sub

    Private Sub dgvCashiers_DataBindingComplete(sender As Object, e As DataGridViewBindingCompleteEventArgs)
        ConfigureGridColumns()
        UpdateEmptyState()
    End Sub

    Private Sub ConfigureGridColumns()
        GridDisplayHelper.MoveActiveStatusColumnToLeft(dgvCashiers)
        UpdateEmptyState()
    End Sub

    Private Sub dgvCashiers_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex < 0 Then
            Return
        End If

        Dim col As DataGridViewColumn = dgvCashiers.Columns(e.ColumnIndex)
        If col Is Nothing Then
            Return
        End If

        If col.Name = "colNum" Then
            e.Value = (e.RowIndex + 1).ToString(CultureInfo.InvariantCulture)
            e.FormattingApplied = True
            Return
        End If

        If col.Name = "colDisplayName" Then
            If e.Value Is Nothing OrElse e.Value Is DBNull.Value OrElse String.IsNullOrWhiteSpace(Convert.ToString(e.Value)) Then
                e.Value = "—"
                e.CellStyle.ForeColor = ColorTranslator.FromHtml("#5A6A7A")
                e.CellStyle.Font = New Font("Segoe UI", 10.0F, FontStyle.Italic)
            Else
                e.CellStyle.ForeColor = ColorTranslator.FromHtml("#1A2332")
                e.CellStyle.Font = New Font("Segoe UI", 10.0F)
            End If
            e.FormattingApplied = True
            Return
        End If

        If col.Name = "colStatus" Then
            Dim isActive As Boolean = False
            If e.Value IsNot Nothing AndAlso e.Value IsNot DBNull.Value Then
                If TypeOf e.Value Is Boolean Then
                    isActive = CBool(e.Value)
                Else
                    Boolean.TryParse(Convert.ToString(e.Value), isActive)
                End If
            End If
            e.Value = If(isActive, "Active", "Inactive")
            e.FormattingApplied = True
            Return
        End If

        If col.Name = "colLastSignIn" Then
            If e.Value Is Nothing OrElse e.Value Is DBNull.Value Then
                e.Value = "Never"
                e.CellStyle.ForeColor = ColorTranslator.FromHtml("#5A6A7A")
                e.CellStyle.Font = New Font("Segoe UI", 10.0F, FontStyle.Italic)
            Else
                Dim dt As DateTime
                If TypeOf e.Value Is DateTime Then
                    dt = CDate(e.Value)
                ElseIf DateTime.TryParse(Convert.ToString(e.Value), dt) Then
                    ' parsed
                Else
                    Return
                End If
                e.Value = dt.ToString("MMM d, yyyy  h:mm tt", CultureInfo.CurrentCulture)
                e.CellStyle.ForeColor = ColorTranslator.FromHtml("#1A2332")
            End If
            e.FormattingApplied = True
            Return
        End If

        If col.Name = "colRegistered" Then
            If e.Value IsNot Nothing AndAlso e.Value IsNot DBNull.Value Then
                Dim dt As DateTime
                If TypeOf e.Value Is DateTime Then
                    dt = CDate(e.Value)
                ElseIf DateTime.TryParse(Convert.ToString(e.Value), dt) Then
                    e.Value = dt.ToString("MMM d, yyyy", CultureInfo.CurrentCulture)
                    e.FormattingApplied = True
                End If
            End If
        End If
    End Sub

    Private Sub dgvCashiers_StatusCellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then
            Return
        End If

        If dgvCashiers.Columns(e.ColumnIndex).Name <> "colStatus" Then
            Return
        End If

        e.PaintBackground(e.ClipBounds, True)

        Dim statusRaw As Object = dgvCashiers.Rows(e.RowIndex).Cells("colStatus").Value
        Dim isActive As Boolean = False
        If statusRaw IsNot Nothing AndAlso statusRaw IsNot DBNull.Value Then
            If TypeOf statusRaw Is Boolean Then
                isActive = CBool(statusRaw)
            Else
                isActive = String.Equals(Convert.ToString(statusRaw), "True", StringComparison.OrdinalIgnoreCase) OrElse
                    String.Equals(Convert.ToString(e.FormattedValue), "Active", StringComparison.OrdinalIgnoreCase)
            End If
        End If
        Dim badgeBg As Color = If(isActive, SuccessBg, MutedBg)
        Dim badgeFg As Color = If(isActive, SuccessGreen, ColorTranslator.FromHtml("#6B7280"))
        Dim badgeText As String = If(isActive, "Active", "Inactive")

        Const badgeW As Integer = 70
        Const badgeH As Integer = 24
        Dim bx As Integer = e.CellBounds.X + (e.CellBounds.Width - badgeW) \ 2
        Dim by As Integer = e.CellBounds.Y + (e.CellBounds.Height - badgeH) \ 2
        Dim badgeRect As New Rectangle(bx, by, badgeW, badgeH)

        Using br As New SolidBrush(badgeBg)
            e.Graphics.FillRectangle(br, badgeRect)
        End Using
        Using pen As New Pen(badgeFg, 0.5F)
            e.Graphics.DrawRectangle(pen, badgeRect)
        End Using
        Using br As New SolidBrush(badgeFg)
            Using sf As New StringFormat()
                sf.Alignment = StringAlignment.Center
                sf.LineAlignment = StringAlignment.Center
                e.Graphics.DrawString(
                    badgeText,
                    New Font("Segoe UI", 9.0F, FontStyle.Bold),
                    br,
                    RectangleF.FromLTRB(bx, by, bx + badgeW, by + badgeH),
                    sf)
            End Using
        End Using

        e.Handled = True
    End Sub

    Private Function GetSelectedCashierId() As Integer?
        If dgvCashiers.CurrentRow Is Nothing Then
            Return Nothing
        End If

        Dim row As DataGridViewRow = dgvCashiers.CurrentRow
        If row.Cells("colCashierId").Value Is Nothing OrElse row.Cells("colCashierId").Value Is DBNull.Value Then
            Return Nothing
        End If

        Return Convert.ToInt32(row.Cells("colCashierId").Value)
    End Function

    Private Sub dgvCashiers_SelectionChanged(sender As Object, e As EventArgs) Handles dgvCashiers.SelectionChanged
        ClearInputError()
        UpdateSelectionUi()
    End Sub

    Private Function PasswordsMatch() As Boolean
        Return String.Equals(txtPassword.Text, txtConfirmPassword.Text, StringComparison.Ordinal)
    End Function

    Private Sub btnRegister_Click(sender As Object, e As EventArgs) Handles btnRegister.Click
        ClearInputError()

        Dim username As String = txtUsername.Text.Trim()
        Dim userError As String = String.Empty
        If Not CashierAccountService.ValidateUsername(username, userError) Then
            ShowInputError(userError, txtUsername)
            txtUsername.Focus()
            Return
        End If

        If Not PasswordsMatch() Then
            ShowInputError("Password and confirmation do not match.", txtConfirmPassword)
            txtConfirmPassword.Focus()
            Return
        End If

        Dim passError As String = String.Empty
        If Not CashierAccountService.ValidatePassword(txtPassword.Text, passError) Then
            ShowInputError(passError, txtPassword)
            lblPassHint.ForeColor = UiTheme.Danger
            txtPassword.Focus()
            Return
        End If

        Try
            CashierAccountService.RegisterAccount(username, txtPassword.Text, txtDisplayName.Text)
            AuditLogger.LogAudit("CASHIER_REGISTER", "Registered cashier '" & username & "'.", AppSession.GetAuditIdentity())
            ClearInputFields()
            LoadCashiers()
            ShowStatus("✓ Cashier '" & username & "' registered.", False)
            txtUsername.Focus()
        Catch ex As SqlException When ex.Number = 2627 OrElse ex.Number = 2601
            ShowInputError("That username is already registered.", txtUsername)
        Catch ex As Exception
            ShowStatus("✗ Registration failed: " & ex.Message, True)
        End Try
    End Sub

    Private Sub btnUpdateDisplay_Click(sender As Object, e As EventArgs) Handles btnUpdateDisplay.Click
        ClearInputError()
        Dim id As Integer? = GetSelectedCashierId()
        If Not id.HasValue Then
            ShowInputError("Select a cashier account to update.")
            Return
        End If

        Dim newName As String = PromptDisplayName(selectedDisplayName)
        If newName Is Nothing Then
            Return
        End If

        Try
            CashierAccountService.UpdateDisplayName(id.Value, newName)
            AuditLogger.LogAudit("CASHIER_UPDATE", "Updated display name for cashier #" & id.Value.ToString(CultureInfo.InvariantCulture), AppSession.GetAuditIdentity())
            LoadCashiers()
            ShowStatus("✓ Display name updated.", False)
        Catch ex As Exception
            ShowStatus("✗ Update failed: " & ex.Message, True)
        End Try
    End Sub

    Private Sub btnResetPassword_Click(sender As Object, e As EventArgs) Handles btnResetPassword.Click
        ClearInputError()
        Dim id As Integer? = GetSelectedCashierId()
        If Not id.HasValue Then
            ShowInputError("Select a cashier account to reset the password.")
            Return
        End If

        Dim newPassword As String = Nothing
        If Not TryPromptNewPassword(newPassword) Then
            Return
        End If

        Try
            CashierAccountService.ResetPassword(id.Value, newPassword)
            AuditLogger.LogAudit("CASHIER_PASSWORD_RESET", "Reset password for cashier #" & id.Value.ToString(CultureInfo.InvariantCulture), AppSession.GetAuditIdentity())
            ShowStatus("✓ Password reset.", False)
        Catch ex As Exception
            ShowStatus("✗ Password reset failed: " & ex.Message, True)
        End Try
    End Sub

    Private Sub btnDeactivate_Click(sender As Object, e As EventArgs) Handles btnDeactivate.Click
        ClearInputError()
        Dim id As Integer? = GetSelectedCashierId()
        If Not id.HasValue Then
            ShowInputError("Select a cashier account to deactivate.")
            Return
        End If

        Dim username As String = If(String.IsNullOrEmpty(selectedUsername), "this cashier", selectedUsername)
        Dim result As DialogResult = MessageBox.Show(
            "Deactivate '" & username & "'? They will no longer be able to sign in.",
            "Confirm deactivation",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning)
        If result <> DialogResult.OK Then
            Return
        End If

        Try
            CashierAccountService.SetActive(id.Value, False)
            AuditLogger.LogAudit("CASHIER_DEACTIVATE", "Deactivated cashier #" & id.Value.ToString(CultureInfo.InvariantCulture), AppSession.GetAuditIdentity())
            LoadCashiers()
            ShowStatus("✓ Cashier deactivated.", False)
        Catch ex As Exception
            ShowStatus("✗ Deactivate failed: " & ex.Message, True)
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
            ShowStatus("✓ Cashier reactivated.", False)
        Catch ex As Exception
            ShowStatus("✗ Reactivate failed: " & ex.Message, True)
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
        lblPassMatch.Visible = False
        lblPassHint.ForeColor = UiTheme.TextSecondary
    End Sub

    Private Sub ClearInputError()
        lblInputError.Visible = False
        lblInputError.Text = String.Empty
    End Sub

    Private Sub ShowInputError(message As String)
        ShowInputError(message, Nothing)
    End Sub

    Private Sub ShowInputError(message As String, field As TextBox)
        lblInputError.Text = message
        lblInputError.Visible = True
        ShowStatus("✗ " & message, True)
        If field IsNot Nothing Then
            HighlightField(field)
        End If
    End Sub

    Private Sub HighlightField(field As TextBox)
        If Not highlightRestore.ContainsKey(field) Then
            highlightRestore(field) = field.BackColor
        End If
        field.BackColor = DangerBg
        field.Invalidate()
        fieldHighlightTimer.Stop()
        fieldHighlightTimer.Start()
    End Sub

    Private Sub InputField_TextChanged(sender As Object, e As EventArgs)
        Dim field As TextBox = TryCast(sender, TextBox)
        If field Is Nothing OrElse Not highlightRestore.ContainsKey(field) Then
            Return
        End If
        field.BackColor = highlightRestore(field)
        highlightRestore.Remove(field)
        If highlightRestore.Count = 0 Then
            fieldHighlightTimer.Stop()
        End If
    End Sub

    Private Sub fieldHighlightTimer_Tick(sender As Object, e As EventArgs)
        fieldHighlightTimer.Stop()
        For Each pair In highlightRestore
            pair.Key.BackColor = pair.Value
        Next
        highlightRestore.Clear()
    End Sub

    Private Sub ShowStatus(message As String, isError As Boolean)
        statusClearTimer.Stop()
        If String.IsNullOrWhiteSpace(message) Then
            lblStatus.Text = String.Empty
            lblStatus.ForeColor = UiTheme.TextSecondary
            Return
        End If

        lblStatus.Text = message
        lblStatus.ForeColor = If(isError, UiTheme.Danger, SuccessGreen)
        If lblStatus.Parent IsNot Nothing Then
            lblStatus.Location = New Point(
                lblStatus.Parent.Width - lblStatus.Width - 24,
                (lblStatus.Parent.Height - lblStatus.Height) \ 2)
        End If
        statusClearTimer.Interval = StatusClearMs
        statusClearTimer.Start()
    End Sub

    Private Sub statusClearTimer_Tick(sender As Object, e As EventArgs) Handles statusClearTimer.Tick
        statusClearTimer.Stop()
        lblStatus.Text = String.Empty
        lblStatus.ForeColor = UiTheme.TextSecondary
    End Sub

    Private Shared Function PromptDisplayName(current As String) As String
        Using dlg As New Form()
            dlg.Text = "Update display name"
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog
            dlg.StartPosition = FormStartPosition.CenterParent
            dlg.MinimizeBox = False
            dlg.MaximizeBox = False
            dlg.ClientSize = New Size(360, 130)
            dlg.Font = UiTheme.StandardUiFont

            Dim lbl As New Label() With {.Text = "Display name:", .Location = New Point(12, 16), .AutoSize = True}
            Dim txt As New TextBox() With {
                .Location = New Point(12, 40),
                .Width = 336,
                .Text = current,
                .MaxLength = CashierAccountService.MaxDisplayNameLength
            }
            UiTheme.ApplyFilledTextInputVisual(txt)
            Dim ok As New Button() With {.Text = "OK", .DialogResult = DialogResult.OK, .Location = New Point(176, 82), .Size = New Size(80, 32)}
            Dim cancel As New Button() With {.Text = "Cancel", .DialogResult = DialogResult.Cancel, .Location = New Point(268, 82), .Size = New Size(80, 32)}
            UiTheme.ApplyPrimaryButton(ok)
            UiTheme.ApplySecondaryButton(cancel)
            dlg.Controls.AddRange(New Control() {lbl, txt, ok, cancel})
            dlg.AcceptButton = ok
            dlg.CancelButton = cancel
            If dlg.ShowDialog() <> DialogResult.OK Then
                Return Nothing
            End If
            Return txt.Text
        End Using
    End Function

    Private Shared Function TryPromptNewPassword(ByRef password As String) As Boolean
        password = Nothing
        Using dlg As New Form()
            dlg.Text = "Reset password"
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog
            dlg.StartPosition = FormStartPosition.CenterParent
            dlg.MinimizeBox = False
            dlg.MaximizeBox = False
            dlg.ClientSize = New Size(360, 200)
            dlg.Font = UiTheme.StandardUiFont

            Dim lbl1 As New Label() With {.Text = "New password:", .Location = New Point(12, 12), .AutoSize = True}
            Dim txt1 As New TextBox() With {.Location = New Point(12, 34), .Width = 336, .UseSystemPasswordChar = True}
            Dim lbl2 As New Label() With {.Text = "Confirm password:", .Location = New Point(12, 68), .AutoSize = True}
            Dim txt2 As New TextBox() With {.Location = New Point(12, 90), .Width = 336, .UseSystemPasswordChar = True}
            Dim hint As New Label() With {
                .Text = "Minimum 6 characters",
                .Location = New Point(12, 118),
                .AutoSize = True,
                .ForeColor = UiTheme.TextSecondary,
                .Font = New Font("Segoe UI", 8.5F, FontStyle.Italic)
            }
            UiTheme.ApplyFilledTextInputVisual(txt1)
            UiTheme.ApplyFilledTextInputVisual(txt2)

            Dim ok As New Button() With {.Text = "OK", .DialogResult = DialogResult.OK, .Location = New Point(176, 148), .Size = New Size(80, 32)}
            Dim cancel As New Button() With {.Text = "Cancel", .DialogResult = DialogResult.Cancel, .Location = New Point(268, 148), .Size = New Size(80, 32)}
            UiTheme.ApplyPrimaryButton(ok)
            UiTheme.ApplySecondaryButton(cancel)
            dlg.Controls.AddRange(New Control() {lbl1, txt1, lbl2, txt2, hint, ok, cancel})
            dlg.AcceptButton = ok
            dlg.CancelButton = cancel

            If dlg.ShowDialog() <> DialogResult.OK Then
                Return False
            End If

            If Not String.Equals(txt1.Text, txt2.Text, StringComparison.Ordinal) Then
                MessageBox.Show("Password and confirmation do not match.", dlg.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End If

            Dim passError As String = String.Empty
            If Not CashierAccountService.ValidatePassword(txt1.Text, passError) Then
                MessageBox.Show(passError, dlg.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End If

            password = txt1.Text
            Return True
        End Using
    End Function

End Class
