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

    Private Shared ReadOnly SuccessGreen As Color = UiTheme.ColAccent
    Private Shared ReadOnly SuccessBg As Color = UiTheme.SuccessLight
    Private Shared ReadOnly MutedBg As Color = UiTheme.SurfaceVariant
    Private Shared ReadOnly DangerBg As Color = UiTheme.DangerLight
    Private Const StatusClearMs As Integer = 4000
    Private Const FieldWidth As Integer = 292
    Private Const FieldHeight As Integer = 40
    Private Const FieldShellHeight As Integer = 42

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
    Private lblNoSelection As Label
    Private lblPassHint As Label
    Private lblPassMatch As Label
    Private lblChipUsername As Label
    Private WithEvents btnShowPass As Button
    Private WithEvents btnClearSelection As Button
    Private pnlSelectedChip As Panel
    Private pnlEmptyState As Panel
    Private pnlLeftBody As Panel
    Private leftStack As TableLayoutPanel
    Private picEmptyIcon As PictureBox
    Private lblEmptyTitle As Label
    Private lblEmptySub As Label
    Private pnlAvatar As Panel
    Private statusStrip As StatusStrip
    Private statusLabel As ToolStripStatusLabel
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
        Me.Text = AppBranding.WindowTitle("Manage Cashiers")
        UiTheme.ApplyMaximizedWorkspaceDefaults(Me, 900, 600)
        Me.BackColor = UiTheme.ColBackground
        Me.Font = UiTheme.StandardUiFont
        Me.AcceptButton = Nothing
        Me.CancelButton = Nothing

        Try
            UiTheme.ApplyStandardWindowChrome(Me)
            Me.BackColor = UiTheme.ColBackground
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
        Me.BackColor = UiTheme.ColBackground

        txtUsername = CreateStyledTextBox(CashierAccountService.MaxUsernameLength, "3–50 chars, letters/numbers/_")
        txtDisplayName = CreateStyledTextBox(CashierAccountService.MaxDisplayNameLength, "Shown on receipts")
        txtPassword = CreateStyledTextBox(0, String.Empty)
        txtPassword.UseSystemPasswordChar = True
        txtConfirmPassword = CreateStyledTextBox(0, String.Empty)
        txtConfirmPassword.UseSystemPasswordChar = True

        cmbFilter = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Width = 190,
            .Font = UiTheme.FontBody
        }
        cmbFilter.Items.AddRange(New Object() {"Active cashiers", "Inactive cashiers", "All cashiers"})
        UiTheme.ApplyInputStyle(cmbFilter)

        btnRegister = New Button() With {.Text = "Register Cashier", .AutoSize = True, .MinimumSize = New Size(FieldWidth, UiTheme.ButtonHeightLg), .Cursor = Cursors.Hand, .Dock = DockStyle.Top, .Margin = New Padding(0, UiTheme.PadSection, 0, 0)}
        btnUpdateDisplay = New Button() With {.Text = "Update display name", .AutoSize = True, .MinimumSize = New Size(FieldWidth, UiTheme.ButtonHeight), .Visible = False, .Cursor = Cursors.Hand, .Dock = DockStyle.Top, .Margin = New Padding(0, UiTheme.PadControl, 0, 0)}
        btnResetPassword = New Button() With {.Text = "Reset password", .AutoSize = True, .MinimumSize = New Size(FieldWidth, UiTheme.ButtonHeight), .Visible = False, .Cursor = Cursors.Hand, .Dock = DockStyle.Top, .Margin = New Padding(0, UiTheme.PadControl, 0, 0)}
        btnDeactivate = New Button() With {.Text = "Deactivate account", .AutoSize = True, .MinimumSize = New Size(FieldWidth, UiTheme.ButtonHeight), .Visible = False, .Cursor = Cursors.Hand, .Dock = DockStyle.Top, .Margin = New Padding(0, UiTheme.PadControl, 0, 0)}
        btnReactivate = New Button() With {.Text = "Reactivate account", .AutoSize = True, .MinimumSize = New Size(FieldWidth, UiTheme.ButtonHeight), .Visible = False, .Cursor = Cursors.Hand, .Dock = DockStyle.Top, .Margin = Padding.Empty}
        btnRefresh = New Button() With {
            .Text = "Refresh",
            .AutoSize = True,
            .MinimumSize = New Size(100, UiTheme.ButtonHeight),
            .Cursor = Cursors.Hand
        }
        UiTheme.ApplySecondaryButton(btnRefresh)
        btnShowPass = New Button() With {
            .Text = "👁",
            .MinimumSize = New Size(36, UiTheme.InputHeight),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = UiTheme.ColSurface,
            .ForeColor = UiTheme.ColTextSecondary,
            .Cursor = Cursors.Hand,
            .TabStop = False
        }
        btnShowPass.FlatAppearance.BorderSize = 0
        UiTheme.ApplyGhostButton(btnShowPass)

        UiTheme.ApplyPrimaryButton(btnRegister)
        UiTheme.ApplySecondaryButton(btnUpdateDisplay)
        UiTheme.ApplySecondaryButton(btnResetPassword)
        UiTheme.ApplyDangerButton(btnDeactivate)
        UiTheme.ApplySecondaryButton(btnReactivate)
        btnReactivate.ForeColor = SuccessGreen
        dgvCashiers = New DataGridView() With {.Dock = DockStyle.Fill}
        SetupDataGridView()
        AddHandler dgvCashiers.DataBindingComplete, AddressOf dgvCashiers_DataBindingComplete

        lblInputError = New Label() With {.AutoSize = True, .ForeColor = UiTheme.ColDanger, .Visible = False, .Margin = New Padding(0, UiTheme.PadControl, 0, 0)}
        lblPassHint = New Label() With {
            .Text = "Minimum 6 characters",
            .AutoSize = True,
            .Font = UiTheme.FontBodySmall,
            .ForeColor = UiTheme.TextSecondary,
            .Margin = New Padding(0, UiTheme.SpaceXs, 0, 0)
        }
        lblPassMatch = New Label() With {
            .AutoSize = True,
            .Visible = False,
            .Font = UiTheme.FontBodySmall
        }
        lblNoSelection = New Label() With {
            .Text = "Select a cashier from the list to manage their account.",
            .AutoSize = True,
            .MaximumSize = New Size(FieldWidth, 0),
            .Font = New Font(UiTheme.FontBody.FontFamily, UiTheme.FontBody.Size, FontStyle.Italic),
            .ForeColor = UiTheme.ColTextSecondary
        }

        pnlSelectedChip = New Panel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .BackColor = UiTheme.InfoBackground,
            .Visible = False,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl),
            .Padding = New Padding(UiTheme.PadControl)
        }
        AddHandler pnlSelectedChip.Paint, AddressOf SelectedChip_Paint

        pnlAvatar = New Panel() With {
            .Size = New Size(28, 28),
            .BackColor = Color.Transparent,
            .Margin = New Padding(0, 0, UiTheme.PadControl, 0)
        }
        AddHandler pnlAvatar.Paint, AddressOf Avatar_Paint

        lblChipUsername = New Label() With {
            .AutoSize = True,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Font = UiTheme.FontBodyBold,
            .ForeColor = UiTheme.ColTextPrimary
        }

        btnClearSelection = New Button() With {
            .Text = "×",
            .MinimumSize = New Size(28, 28),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.Transparent,
            .ForeColor = UiTheme.ColTextSecondary,
            .Cursor = Cursors.Hand,
            .TabStop = False,
            .Margin = New Padding(UiTheme.PadTight, 0, 0, 0)
        }
        btnClearSelection.FlatAppearance.BorderSize = 0

        Dim chipLayout As New TableLayoutPanel() With {
            .AutoSize = True,
            .Dock = DockStyle.Top,
            .ColumnCount = 3,
            .RowCount = 1,
            .Margin = Padding.Empty
        }
        chipLayout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        chipLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        chipLayout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        chipLayout.Controls.Add(pnlAvatar, 0, 0)
        chipLayout.Controls.Add(lblChipUsername, 1, 0)
        chipLayout.Controls.Add(btnClearSelection, 2, 0)
        pnlSelectedChip.Controls.Add(chipLayout)

        BuildEmptyStatePanel()
        AnchorLeftPanelTextBoxes()

        statusStrip = New StatusStrip()
        statusLabel = New ToolStripStatusLabel(FormStatusHelper.ReadyText) With {.Spring = True, .TextAlign = ContentAlignment.MiddleLeft}
        statusStrip.Items.Add(statusLabel)
        UiTheme.ApplyStatusStripTheme(statusStrip)

        leftStack = New TableLayoutPanel() With {
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .ColumnCount = 1,
            .Width = FieldWidth,
            .MaximumSize = New Size(FieldWidth, 0),
            .Margin = Padding.Empty,
            .Padding = Padding.Empty
        }
        leftStack.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

        leftStack.Controls.Add(UiTheme.CreateSectionHeader("Cashier accounts"), 0, 0)
        leftStack.Controls.Add(CreateSubtitleLabel(
            "Register usernames and passwords for cashiers. Only administrators can manage accounts."), 0, 1)

        leftStack.Controls.Add(CreateSectionLabel("New account"), 0, 2)
        leftStack.Controls.Add(CreateFieldGroup("Username", txtUsername), 0, 3)
        leftStack.Controls.Add(CreateFieldGroup("Display name (optional)", txtDisplayName), 0, 4)

        Dim passGroup As New TableLayoutPanel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .ColumnCount = 1,
            .Width = FieldWidth,
            .MaximumSize = New Size(FieldWidth, 0),
            .Dock = DockStyle.Top,
            .Margin = New Padding(0, 0, 0, 0)
        }
        passGroup.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        passGroup.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        passGroup.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        passGroup.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, FieldWidth))
        passGroup.Controls.Add(CreateFieldCaption("Password"), 0, 0)
        passGroup.Controls.Add(CreatePasswordRow(txtPassword, btnShowPass), 0, 1)
        passGroup.Controls.Add(lblPassHint, 0, 2)
        leftStack.Controls.Add(passGroup, 0, 5)

        Dim confirmGroup As New TableLayoutPanel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .ColumnCount = 1,
            .Width = FieldWidth,
            .MaximumSize = New Size(FieldWidth, 0),
            .Dock = DockStyle.Top,
            .Margin = New Padding(0, 0, 0, 0)
        }
        confirmGroup.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        confirmGroup.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        confirmGroup.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        confirmGroup.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, FieldWidth))
        confirmGroup.Controls.Add(CreateFieldCaption("Confirm password"), 0, 0)
        confirmGroup.Controls.Add(CreatePasswordRow(txtConfirmPassword, Nothing), 0, 1)
        confirmGroup.Controls.Add(lblPassMatch, 0, 2)
        leftStack.Controls.Add(confirmGroup, 0, 6)

        leftStack.Controls.Add(btnRegister, 0, 7)
        leftStack.Controls.Add(UiTheme.CreateSectionHeader("Account actions"), 0, 8)
        leftStack.Controls.Add(pnlSelectedChip, 0, 9)
        leftStack.Controls.Add(lblNoSelection, 0, 10)

        Dim actionStack As New TableLayoutPanel() With {
            .AutoSize = True,
            .Dock = DockStyle.Top,
            .ColumnCount = 1,
            .RowCount = 4,
            .Margin = New Padding(0, UiTheme.PadControl, 0, 0)
        }
        actionStack.Controls.Add(btnUpdateDisplay, 0, 0)
        actionStack.Controls.Add(btnResetPassword, 0, 1)
        actionStack.Controls.Add(btnDeactivate, 0, 2)
        actionStack.Controls.Add(btnReactivate, 0, 3)
        leftStack.Controls.Add(actionStack, 0, 11)
        leftStack.Controls.Add(lblInputError, 0, 12)

        pnlLeftBody = New Panel() With {.Dock = DockStyle.Fill, .AutoScroll = True, .Padding = Padding.Empty}
        pnlLeftBody.Controls.Add(leftStack)

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

        Dim gridHost As New Panel() With {.Dock = DockStyle.Fill}
        Dim gridCard As Panel = UiTheme.CreateCard()
        gridCard.Dock = DockStyle.Fill
        Dim gridCardHost As Panel = gridCard
        Try
            gridCardHost = UiTheme.GetCardContentHost(gridCard)
        Catch
        End Try
        dgvCashiers.Dock = DockStyle.Fill
        dgvCashiers.Margin = Padding.Empty
        gridCardHost.Controls.Add(dgvCashiers)
        gridHost.Controls.Add(gridCard)
        pnlEmptyState.Dock = DockStyle.Fill
        gridHost.Controls.Add(pnlEmptyState)

        Dim listLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .Margin = Padding.Empty
        }
        listLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        listLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        listLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        listLayout.Controls.Add(UiTheme.CreateSectionHeader("Cashier roster"), 0, 0)
        listLayout.Controls.Add(toolbar, 0, 1)
        listLayout.Controls.Add(gridHost, 0, 2)

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
            ("Manage Cashiers", True),
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
        Dim topBar As Panel = UiTheme.CreateTopBar("Manage Cashiers", AppSession.GetAuditIdentity())
        Dim contentArea As Panel = UiTheme.CreateContentArea()

        Dim cashiersSplit As New SplitContainer() With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Vertical,
            .SplitterWidth = 6,
            .BackColor = UiTheme.ColBorder,
            .Panel1MinSize = 320,
            .Panel2MinSize = 420
        }

        Dim editorCard As Panel = UiTheme.CreateCard()
        editorCard.Dock = DockStyle.Fill
        Dim editorCardHost As Panel = editorCard
        Try
            editorCardHost = UiTheme.GetCardContentHost(editorCard)
        Catch
        End Try
        editorCardHost.Controls.Add(pnlLeftBody)
        cashiersSplit.Panel1.Controls.Add(editorCard)

        Dim listCard As Panel = UiTheme.CreateCard()
        listCard.Dock = DockStyle.Fill
        Dim listCardHost As Panel = listCard
        Try
            listCardHost = UiTheme.GetCardContentHost(listCard)
        Catch
        End Try
        listCardHost.Controls.Add(listLayout)
        cashiersSplit.Panel2.Controls.Add(listCard)

        contentArea.Controls.Add(cashiersSplit)
        rightColumn.Controls.Add(contentArea)
        rightColumn.Controls.Add(topBar)

        rootTable.Controls.Add(sidebar, 0, 0)
        rootTable.Controls.Add(rightColumn, 1, 0)

        Me.Controls.Add(rootTable)
        Me.Controls.Add(statusStrip)

        AddHandler cashiersSplit.SplitterMoved, Sub(s, ev) ConfigureCashiersSplit(cashiersSplit)
        AddHandler Me.Resize, Sub(s, ev) ConfigureCashiersSplit(cashiersSplit)

        Me.AcceptButton = btnRegister

        suppressFilterEvents = True
        cmbFilter.SelectedIndex = 0
        suppressFilterEvents = False

        SetNewAccountMode()
        UpdateSelectionUi()
        Me.ResumeLayout(True)
        ConfigureCashiersSplit(cashiersSplit)
        SyncLeftPanelLayout()
        AddHandler pnlLeftBody.Resize, AddressOf SyncLeftPanelLayout
    End Sub

    Private Sub ConfigureCashiersSplit(cashiersSplit As SplitContainer)
        If cashiersSplit Is Nothing OrElse cashiersSplit.Width <= 0 Then
            Return
        End If

        Dim target As Integer = Math.Max(cashiersSplit.Panel1MinSize, CInt(cashiersSplit.Width * 0.36R))
        If target <> cashiersSplit.SplitterDistance Then
            cashiersSplit.SplitterDistance = target
        End If
    End Sub

    Private Sub SyncLeftPanelLayout()
        If leftStack Is Nothing OrElse pnlLeftBody Is Nothing Then
            Return
        End If

        Dim contentWidth As Integer = Math.Max(FieldWidth, pnlLeftBody.ClientSize.Width)
        leftStack.Width = contentWidth
        leftStack.MaximumSize = New Size(contentWidth, 0)
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
        colUser.DefaultCellStyle.Font = UiTheme.FontBodyBold

        Dim colDisplay As New DataGridViewTextBoxColumn() With {
            .Name = "colDisplayName",
            .HeaderText = "Display Name",
            .DataPropertyName = "display_name",
            .Width = 160
        }
        colDisplay.DefaultCellStyle.ForeColor = UiTheme.ColTextSecondary

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
            .Width = 168,
            .MinimumWidth = 140
        }

        Dim colReg As New DataGridViewTextBoxColumn() With {
            .Name = "colRegistered",
            .HeaderText = "Registered",
            .DataPropertyName = "created_at",
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            .MinimumWidth = 120,
            .FillWeight = 100
        }

        dgvCashiers.Columns.AddRange(colId, colStatus, colNum, colUser, colDisplay, colLastSign, colReg)
        colStatus.DisplayIndex = 0
        colNum.DisplayIndex = 1
        colUser.DisplayIndex = 2
        colDisplay.DisplayIndex = 3
        colLastSign.DisplayIndex = 4
        colReg.DisplayIndex = 5

        With dgvCashiers
            .BackgroundColor = UiTheme.ColSurface
            .BorderStyle = BorderStyle.None
            .CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            .GridColor = UiTheme.ColBorder
            .RowHeadersVisible = False
            .ColumnHeadersVisible = True
            .ColumnHeadersHeight = UiTheme.GridHeaderHeight
            .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            .AllowUserToAddRows = False
            .AllowUserToResizeRows = False
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
            .MultiSelect = False
            .ReadOnly = True
            .Font = UiTheme.FontBody
            .RowTemplate.Height = UiTheme.GridRowHeight + 12
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            .ScrollBars = ScrollBars.Both
            .EnableHeadersVisualStyles = False
        End With

        UiTheme.ApplyGridStyle(dgvCashiers)
        GridDisplayHelper.ApplyStandardBoundGridDisplay(dgvCashiers)

        dgvCashiers.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.ColBackground
        dgvCashiers.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.ColTextSecondary
        dgvCashiers.ColumnHeadersDefaultCellStyle.Font = UiTheme.FontCaption
        dgvCashiers.ColumnHeadersDefaultCellStyle.Padding = New Padding(UiTheme.PadControl, 0, 0, 0)

        dgvCashiers.DefaultCellStyle.BackColor = UiTheme.ColSurface
        dgvCashiers.DefaultCellStyle.ForeColor = UiTheme.ColTextPrimary
        dgvCashiers.DefaultCellStyle.SelectionBackColor = UiTheme.InfoBackground
        dgvCashiers.DefaultCellStyle.SelectionForeColor = UiTheme.ColTextPrimary
        dgvCashiers.DefaultCellStyle.Padding = New Padding(UiTheme.PadControl, 0, UiTheme.PadControl, 0)

        dgvCashiers.AlternatingRowsDefaultCellStyle.BackColor = UiTheme.SurfaceVariant

        AddHandler dgvCashiers.CellPainting, AddressOf dgvCashiers_StatusCellPainting
    End Sub

    Private Sub AnchorLeftPanelTextBoxes()
        ' Field hosts use fixed width; inner text boxes dock inside shells.
    End Sub

    Private Function CreateStyledTextBox(maxLength As Integer, placeholder As String) As TextBox
        Dim tb As New TextBox() With {
            .Font = UiTheme.FontBody,
            .BorderStyle = BorderStyle.None,
            .Dock = DockStyle.Fill,
            .Margin = Padding.Empty,
            .Multiline = False
        }
        If maxLength > 0 Then
            tb.MaxLength = maxLength
        End If
        If Not String.IsNullOrEmpty(placeholder) Then
            tb.PlaceholderText = placeholder
        End If
        UiTheme.ApplyInputStyle(tb)
        Return tb
    End Function

    Private Function CreateTextFieldShell(textBox As TextBox, Optional toggleBtn As Button = Nothing) As Panel
        textBox.Dock = DockStyle.Fill
        textBox.Margin = Padding.Empty
        textBox.BorderStyle = BorderStyle.None

        Dim inner As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = If(toggleBtn Is Nothing, 1, 2),
            .RowCount = 1,
            .BackColor = UiTheme.ColSurface,
            .Margin = Padding.Empty,
            .Padding = New Padding(UiTheme.PadControl, UiTheme.PadTight, If(toggleBtn Is Nothing, UiTheme.PadControl, UiTheme.PadTight), UiTheme.PadTight)
        }
        inner.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        inner.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

        If toggleBtn IsNot Nothing Then
            inner.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 40.0F))
            toggleBtn.Dock = DockStyle.None
            toggleBtn.Anchor = AnchorStyles.None
            toggleBtn.Size = New Size(36, 28)
            toggleBtn.Margin = Padding.Empty
            toggleBtn.TextAlign = ContentAlignment.MiddleCenter
            inner.Controls.Add(textBox, 0, 0)
            inner.Controls.Add(toggleBtn, 1, 0)
        Else
            inner.Controls.Add(textBox, 0, 0)
        End If

        Dim outer As New Panel() With {
            .Dock = DockStyle.Top,
            .Height = FieldShellHeight,
            .MinimumSize = New Size(0, FieldShellHeight),
            .BackColor = UiTheme.ColBorder,
            .Padding = New Padding(1),
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        }
        outer.Controls.Add(inner)
        Return outer
    End Function

    Private Shared Function CreateSubtitleLabel(text As String) As Label
        Return New Label() With {
            .Text = text,
            .AutoSize = True,
            .MaximumSize = New Size(FieldWidth, 0),
            .Font = UiTheme.FontCaption,
            .ForeColor = UiTheme.ColTextSecondary,
            .Margin = New Padding(0, 0, 0, UiTheme.PadSection)
        }
    End Function

    Private Shared Function CreateSectionLabel(text As String) As Label
        Return New Label() With {
            .Text = text,
            .AutoSize = True,
            .Font = UiTheme.FontSubheading,
            .ForeColor = UiTheme.ColPrimary,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        }
    End Function

    Private Shared Function CreateFieldCaption(text As String) As Label
        Return New Label() With {
            .Text = text,
            .AutoSize = True,
            .Font = UiTheme.FontCaption,
            .ForeColor = UiTheme.ColTextSecondary,
            .Margin = New Padding(0, 0, 0, UiTheme.PadTight)
        }
    End Function

    Private Function CreateFieldGroup(caption As String, textBox As TextBox) As Control
        Dim panel As New TableLayoutPanel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .ColumnCount = 1,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl),
            .Dock = DockStyle.Top
        }
        panel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        panel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        panel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        panel.Controls.Add(CreateFieldCaption(caption), 0, 0)
        panel.Controls.Add(CreateTextFieldShell(textBox), 0, 1)
        Return panel
    End Function

    Private Function CreatePasswordRow(passwordBox As TextBox, toggleBtn As Button) As Panel
        Return CreateTextFieldShell(passwordBox, toggleBtn)
    End Function

    Private Shared Function CreateDivider() As Panel
        Return UiTheme.CreateDivider()
    End Function

    Private Sub BuildEmptyStatePanel()
        pnlEmptyState = New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = UiTheme.ColBackground,
            .Visible = True
        }

        picEmptyIcon = New PictureBox() With {
            .Width = 64,
            .Height = 56,
            .BackColor = Color.Transparent,
            .SizeMode = PictureBoxSizeMode.Normal
        }
        AddHandler picEmptyIcon.Paint, AddressOf picEmptyIcon_Paint

        lblEmptyTitle = UiTheme.CreateEmptyStateLabel("No cashier accounts yet")
        lblEmptyTitle.Font = UiTheme.FontHeading
        lblEmptySub = UiTheme.CreateEmptyStateLabel("Use the form on the left to register the first cashier.")
        lblEmptySub.Margin = New Padding(UiTheme.PadSection, UiTheme.PadControl, UiTheme.PadSection, 0)

        pnlEmptyState.Controls.AddRange(New Control() {picEmptyIcon, lblEmptyTitle, lblEmptySub})
        AddHandler pnlEmptyState.Resize, AddressOf EmptyState_Resize
        EmptyState_Resize(pnlEmptyState, EventArgs.Empty)
    End Sub

    Private Sub picEmptyIcon_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim c As Color = UiTheme.ColTextSecondary

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
        Using pen As New Pen(UiTheme.ColBorder)
            Using path As GraphicsPath = CreateRoundedRect(New Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 6)
                e.Graphics.DrawPath(pen, path)
            End Using
        End Using
    End Sub

    Private Sub Avatar_Paint(sender As Object, e As PaintEventArgs)
        Dim initials As String = GetInitials(selectedDisplayName, selectedUsername)
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
        Using brush As New SolidBrush(UiTheme.ColPrimary)
            e.Graphics.FillEllipse(brush, 0, 0, 27, 27)
        End Using
        TextRenderer.DrawText(
            e.Graphics,
            initials,
            UiTheme.FontBodyBold,
            New Rectangle(0, 0, 28, 28),
            UiTheme.ColTextOnDark,
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
        FormStatusHelper.ShowTimedStatus(statusLabel, statusClearTimer, message, isError)
    End Sub

    Private Sub statusClearTimer_Tick(sender As Object, e As EventArgs) Handles statusClearTimer.Tick
        statusClearTimer.Stop()
        FormStatusHelper.ResetTimedStatus(statusLabel)
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
