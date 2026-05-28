Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms

''' <summary>
''' Edits store settings persisted to LocalApplicationData.
''' </summary>
Public Class SettingsForm
    Inherits Form

    Private Const MaxStoreNameLength As Integer = 120
    Private Const MaxFooterLength As Integer = 500
    Private Const MaxBranchLength As Integer = 120
    Private Const MaxPolicyLength As Integer = 500
    Private Const MinCurrencySymbolLength As Integer = 1
    Private Const MaxCurrencySymbolLength As Integer = 6
    Private Const MinAdminPasswordLength As Integer = 6
    Private Const SettingsDialogWidth As Integer = 620
    Private Const SettingsDialogHeight As Integer = 680

    Private WithEvents txtStoreName As TextBox
    Private WithEvents txtBranch As TextBox
    Private WithEvents txtFooter As TextBox
    Private WithEvents txtReturnPolicy As TextBox
    Private WithEvents txtTerms As TextBox
    Private WithEvents txtCurrency As TextBox
    Private WithEvents numDefaultTax As NumericUpDown
    Private WithEvents numStockThreshold As NumericUpDown
    Private WithEvents txtAdminPassword As TextBox
    Private WithEvents txtAdminPasswordConfirm As TextBox
    Private lblSettingsError As Label
    Private WithEvents btnOk As Button
    Private WithEvents btnCancel As Button

    Private Sub SettingsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = AppBranding.WindowTitle("Settings")
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MinimizeBox = False
        Me.MaximizeBox = False
        Me.MinimumSize = New Size(540, 520)
        Me.Size = New Size(SettingsDialogWidth, SettingsDialogHeight)
        Me.BackColor = UiTheme.ColBackground
        UiTheme.ApplyStandardWindowChrome(Me)

        AppSettings.Reload()
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        Me.SuspendLayout()
        Me.Controls.Clear()

        Dim settings As AppSettingsData = AppSettings.Current

        txtStoreName = CreateTextInput(settings.StoreName, MaxStoreNameLength)
        txtBranch = CreateTextInput(settings.StoreBranch, MaxBranchLength)
        txtFooter = CreateMultilineInput(settings.ReceiptFooter, MaxFooterLength)
        txtReturnPolicy = CreateMultilineInput(settings.ReturnPolicyText, MaxPolicyLength)
        txtTerms = CreateMultilineInput(settings.TermsText, MaxPolicyLength)
        txtCurrency = CreateTextInput(settings.CurrencySymbol, MaxCurrencySymbolLength)

        numDefaultTax = New NumericUpDown()
        numDefaultTax.DecimalPlaces = 2
        numDefaultTax.Minimum = 0D
        numDefaultTax.Maximum = 100D
        numDefaultTax.Increment = 0.5D
        numDefaultTax.Value = Math.Min(Math.Max(settings.DefaultTaxPercent, 0D), 100D)
        numDefaultTax.Width = 100
        numDefaultTax.TextAlign = HorizontalAlignment.Right
        UiTheme.ApplyInputStyle(numDefaultTax)

        numStockThreshold = New NumericUpDown()
        numStockThreshold.Minimum = 1
        numStockThreshold.Maximum = 99999
        numStockThreshold.Value = Math.Max(settings.StockThreshold, 1)
        numStockThreshold.Width = 100
        numStockThreshold.TextAlign = HorizontalAlignment.Right
        UiTheme.ApplyInputStyle(numStockThreshold)

        txtAdminPassword = CreateTextInput(String.Empty, 64)
        txtAdminPassword.UseSystemPasswordChar = True
        txtAdminPasswordConfirm = CreateTextInput(String.Empty, 64)
        txtAdminPasswordConfirm.UseSystemPasswordChar = True

        btnOk = New Button()
        btnOk.Text = "&Save settings"
        btnOk.DialogResult = DialogResult.None
        btnOk.AutoSize = True
        btnOk.MinimumSize = New Size(120, UiTheme.ButtonHeight)
        btnOk.Cursor = Cursors.Hand

        btnCancel = New Button()
        btnCancel.Text = "Cancel"
        btnCancel.DialogResult = DialogResult.Cancel
        btnCancel.AutoSize = True
        btnCancel.MinimumSize = New Size(100, UiTheme.ButtonHeight)
        btnCancel.Cursor = Cursors.Hand

        UiTheme.ApplyPrimaryButton(btnOk)
        UiTheme.ApplySecondaryButton(btnCancel)

        lblSettingsError = New Label()
        lblSettingsError.AutoSize = True
        lblSettingsError.ForeColor = UiTheme.ColDanger
        lblSettingsError.Font = UiTheme.FontBodySmall
        lblSettingsError.Visible = False
        lblSettingsError.Margin = New Padding(0, UiTheme.PadControl, 0, 0)
        lblSettingsError.MaximumSize = New Size(SettingsDialogWidth - (UiTheme.PadPage * 2) - (UiTheme.PadCard * 2), 0)

        Dim buttonRow As FlowLayoutPanel = UiTheme.CreateButtonRow(FlowDirection.RightToLeft)
        buttonRow.Dock = DockStyle.Fill
        buttonRow.Controls.Add(btnCancel)
        buttonRow.Controls.Add(btnOk)

        Dim fields As New TableLayoutPanel()
        fields.Dock = DockStyle.Top
        fields.AutoSize = True
        fields.AutoSizeMode = AutoSizeMode.GrowAndShrink
        fields.ColumnCount = 1
        fields.BackColor = Color.Transparent

        Dim rowIndex As Integer = 0
        AddHeader(fields, rowIndex, "Store settings", "Branding, tax defaults, and receipt text shown at checkout.")
        rowIndex += 1
        AddField(fields, rowIndex, "Store name", txtStoreName, "Displayed on receipts, reports, and the sidebar.")
        rowIndex += 1
        AddField(fields, rowIndex, "Store branch", txtBranch, "Branch line under the store name on receipts.")
        rowIndex += 1
        AddField(fields, rowIndex, "Receipt footer", txtFooter, "Closing message at the bottom of each receipt.")
        rowIndex += 1
        AddField(fields, rowIndex, "Return policy", txtReturnPolicy, "Return/exchange policy printed on receipts.")
        rowIndex += 1
        AddField(fields, rowIndex, "Terms text", txtTerms, "Terms and conditions line on receipts.")
        rowIndex += 1
        AddField(fields, rowIndex, "Currency symbol", txtCurrency, "Prefix for prices and totals (for example PHP symbol or $).")
        rowIndex += 1
        AddField(fields, rowIndex, "Default tax rate (%)", numDefaultTax, "Applied when Point of Sale opens (cashier can still toggle tax off).")
        rowIndex += 1
        AddField(fields, rowIndex, "Low-stock threshold", numStockThreshold, "Dashboard alert when active products reach this quantity or below.")
        rowIndex += 1
        AddSection(fields, rowIndex, "Administrator password")
        rowIndex += 1
        AddField(fields, rowIndex, "New admin password", txtAdminPassword, "Leave blank to keep current password. Minimum 6 characters.")
        rowIndex += 1
        AddField(fields, rowIndex, "Confirm password", txtAdminPasswordConfirm, "Must match when changing the administrator password.")
        rowIndex += 1
        fields.Controls.Add(lblSettingsError, 0, rowIndex)
        rowIndex += 1
        fields.Controls.Add(buttonRow, 0, rowIndex)

        Dim scrollHost As New Panel()
        scrollHost.Dock = DockStyle.Fill
        scrollHost.AutoScroll = True
        scrollHost.Padding = New Padding(0, 0, 4, 0)
        scrollHost.BackColor = Color.Transparent
        scrollHost.Controls.Add(fields)

        Dim cardOuter As Panel = UiTheme.CreateCardPanel(New Padding(UiTheme.PadCard))
        cardOuter.Dock = DockStyle.Fill
        Dim cardInner As Panel = UiTheme.GetCardContentHost(cardOuter)
        If cardInner IsNot Nothing Then
            cardInner.Controls.Add(scrollHost)
        Else
            cardOuter.Controls.Add(scrollHost)
        End If

        Dim root As New TableLayoutPanel()
        root.Dock = DockStyle.Fill
        root.Padding = New Padding(UiTheme.PadPage)
        root.ColumnCount = 1
        root.RowCount = 1
        root.BackColor = UiTheme.ColBackground
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        root.Controls.Add(cardOuter, 0, 0)

        Me.Controls.Add(root)
        Me.AcceptButton = btnOk
        Me.CancelButton = btnCancel

        UiTheme.AssignTabOrder(
            txtStoreName,
            txtBranch,
            txtFooter,
            txtReturnPolicy,
            txtTerms,
            txtCurrency,
            numDefaultTax,
            numStockThreshold,
            txtAdminPassword,
            txtAdminPasswordConfirm,
            btnOk,
            btnCancel)

        Me.ResumeLayout(True)
    End Sub

    Private Shared Function CreateTextInput(value As String, maxLength As Integer) As TextBox
        Dim box As New TextBox()
        box.Text = value
        box.MaxLength = maxLength
        box.Dock = DockStyle.Fill
        UiTheme.ApplyInputStyle(box)
        Return box
    End Function

    Private Shared Function CreateMultilineInput(value As String, maxLength As Integer) As TextBox
        Dim box As New TextBox()
        box.Text = value
        box.MaxLength = maxLength
        box.Multiline = True
        box.ScrollBars = ScrollBars.Vertical
        box.Height = 64
        box.Dock = DockStyle.Fill
        UiTheme.ApplyInputStyle(box)
        Return box
    End Function

    Private Shared Sub AddHeader(table As TableLayoutPanel, row As Integer, title As String, subtitle As String)
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Dim headerPanel As New Panel()
        headerPanel.AutoSize = True
        headerPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink
        headerPanel.Dock = DockStyle.Top
        headerPanel.Margin = New Padding(0, 0, 0, UiTheme.PadSection)
        headerPanel.BackColor = Color.Transparent

        Dim lblSubtitle As New Label()
        lblSubtitle.Text = subtitle
        lblSubtitle.Font = UiTheme.FontBodySmall
        lblSubtitle.ForeColor = UiTheme.ColTextSecondary
        lblSubtitle.AutoSize = True
        lblSubtitle.MaximumSize = New Size(SettingsDialogWidth - (UiTheme.PadPage * 2) - (UiTheme.PadCard * 2), 0)
        lblSubtitle.Dock = DockStyle.Top

        Dim lblTitle As New Label()
        lblTitle.Text = title
        lblTitle.Font = UiTheme.FontHeading
        lblTitle.ForeColor = UiTheme.ColPrimary
        lblTitle.AutoSize = True
        lblTitle.Dock = DockStyle.Top
        lblTitle.Margin = New Padding(0, 0, 0, UiTheme.PadTight)

        headerPanel.Controls.Add(lblSubtitle)
        headerPanel.Controls.Add(lblTitle)
        table.Controls.Add(headerPanel, 0, row)
    End Sub

    Private Shared Sub AddSection(table As TableLayoutPanel, row As Integer, title As String)
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Dim sectionHeader As Panel = UiTheme.CreateSectionHeader(title)
        sectionHeader.Margin = New Padding(0, UiTheme.PadSection, 0, UiTheme.PadControl)
        table.Controls.Add(sectionHeader, 0, row)
    End Sub

    Private Shared Sub AddField(table As TableLayoutPanel, row As Integer, caption As String, input As Control, hint As String)
        table.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        table.Controls.Add(CreateFieldBlock(caption, input, hint), 0, row)
    End Sub

    Private Shared Function CreateFieldBlock(caption As String, input As Control, hint As String) As Panel
        Dim block As New TableLayoutPanel()
        block.AutoSize = True
        block.AutoSizeMode = AutoSizeMode.GrowAndShrink
        block.Dock = DockStyle.Top
        block.ColumnCount = 1
        block.RowCount = 3
        block.Margin = New Padding(0, 0, 0, UiTheme.PadSection)
        block.BackColor = Color.Transparent
        block.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        block.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        block.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim lblCaption As New Label()
        lblCaption.Text = caption
        lblCaption.AutoSize = True
        lblCaption.Font = UiTheme.FontBodyBold
        lblCaption.ForeColor = UiTheme.ColTextPrimary
        lblCaption.Margin = New Padding(0, 0, 0, UiTheme.PadTight)

        input.Margin = New Padding(0)

        Dim lblHint As New Label()
        lblHint.Text = hint
        lblHint.AutoSize = True
        lblHint.Font = UiTheme.FontBodySmall
        lblHint.ForeColor = UiTheme.ColTextSecondary
        lblHint.Margin = New Padding(0, UiTheme.PadTight, 0, 0)
        lblHint.MaximumSize = New Size(SettingsDialogWidth - (UiTheme.PadPage * 2) - (UiTheme.PadCard * 2), 0)

        block.Controls.Add(lblCaption, 0, 0)
        block.Controls.Add(input, 0, 1)
        block.Controls.Add(lblHint, 0, 2)
        Return block
    End Function

    Private Sub ClearSettingsInputError()
        If lblSettingsError Is Nothing Then
            Return
        End If

        lblSettingsError.Text = String.Empty
        lblSettingsError.Visible = False
    End Sub

    Private Sub ShowSettingsInputError(message As String)
        lblSettingsError.Text = message
        lblSettingsError.Visible = True
    End Sub

    Private Sub InputChanged(sender As Object, e As EventArgs) Handles txtStoreName.TextChanged, txtBranch.TextChanged, txtFooter.TextChanged, txtReturnPolicy.TextChanged, txtTerms.TextChanged, txtCurrency.TextChanged, numDefaultTax.ValueChanged, numStockThreshold.ValueChanged, txtAdminPassword.TextChanged, txtAdminPasswordConfirm.TextChanged
        ClearSettingsInputError()
    End Sub

    Private Sub btnOk_Click(sender As Object, e As EventArgs) Handles btnOk.Click
        ClearSettingsInputError()

        Dim storeName As String = txtStoreName.Text.Trim()
        If storeName.Length = 0 Then
            ShowSettingsInputError("Store name cannot be empty.")
            MessageBox.Show("Store name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtStoreName.Focus()
            Return
        End If

        Dim currencySym As String = txtCurrency.Text.Trim()
        If currencySym.Length < MinCurrencySymbolLength OrElse currencySym.Length > MaxCurrencySymbolLength Then
            ShowSettingsInputError("Currency symbol must be 1-6 characters.")
            txtCurrency.Focus()
            Return
        End If

        Dim newPassword As String = txtAdminPassword.Text
        Dim confirmPassword As String = txtAdminPasswordConfirm.Text
        If newPassword.Length > 0 OrElse confirmPassword.Length > 0 Then
            If newPassword.Length < MinAdminPasswordLength Then
                ShowSettingsInputError(String.Format(CultureInfo.CurrentCulture, "Administrator password must be at least {0} characters.", MinAdminPasswordLength))
                txtAdminPassword.Focus()
                Return
            End If

            If Not String.Equals(newPassword, confirmPassword, StringComparison.Ordinal) Then
                ShowSettingsInputError("Administrator passwords do not match.")
                txtAdminPasswordConfirm.Focus()
                Return
            End If
        End If

        Dim data As AppSettingsData = AppSettings.Current
        data.StoreName = storeName
        data.StoreBranch = txtBranch.Text.Trim()
        data.ReceiptFooter = txtFooter.Text.Trim()
        data.ReturnPolicyText = txtReturnPolicy.Text.Trim()
        data.TermsText = txtTerms.Text.Trim()
        data.CurrencySymbol = currencySym
        data.DefaultTaxPercent = numDefaultTax.Value
        data.StockThreshold = CInt(numStockThreshold.Value)

        If newPassword.Length > 0 Then
            AdminAuth.ApplyPasswordChange(data, newPassword)
        End If

        AppSettings.Save(data)

        Dim auditDetail As String = "Store settings updated (name, branch, receipt text, tax default, stock threshold"
        If newPassword.Length > 0 Then
            auditDetail &= ", admin password"
        End If
        auditDetail &= ")."

        AuditLogger.LogAudit("SETTINGS_CHANGED", auditDetail, AppSession.CurrentRole)

        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

End Class
