Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms

''' <summary>
''' Edits store name, receipt footer, and currency symbol persisted to LocalApplicationData.
''' </summary>
Public Class SettingsForm
    Inherits Form

    Private Const MaxStoreNameLength As Integer = 120
    Private Const MaxFooterLength As Integer = 500
    Private Const MinCurrencySymbolLength As Integer = 1
    Private Const MaxCurrencySymbolLength As Integer = 6

    Private WithEvents txtStoreName As TextBox
    Private WithEvents txtFooter As TextBox
    Private WithEvents txtCurrency As TextBox
    Private lblSettingsError As Label
    Private WithEvents btnOk As Button
    Private WithEvents btnCancel As Button

    Private Sub SettingsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "Group C - Settings"
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MinimizeBox = False
        Me.MaximizeBox = False
        Me.MinimumSize = New Size(520, 400)
        Me.Size = New Size(560, 440)
        UiTheme.ApplyStandardWindowChrome(Me)

        AppSettings.Reload()
        Dim s As AppSettingsData = AppSettings.Current

        Dim root As New TableLayoutPanel()
        root.Dock = DockStyle.Fill
        root.Padding = New Padding(12)
        root.ColumnCount = 1
        root.RowCount = 1
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

        Dim cardOuter As Panel = UiTheme.CreateCardPanel(New Padding(16))
        cardOuter.Dock = DockStyle.Fill
        Dim cardInner As Panel = UiTheme.GetCardContentHost(cardOuter)

        Dim fields As New TableLayoutPanel()
        fields.Dock = DockStyle.Fill
        fields.ColumnCount = 2
        fields.RowCount = 5
        fields.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        fields.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        For r As Integer = 0 To 4
            fields.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Next

        Dim lblStore As New Label() With {.Text = "Store name", .AutoSize = True, .Margin = New Padding(0, 6, 8, 6), .ForeColor = UiTheme.TextSecondary}
        txtStoreName = New TextBox() With {.Dock = DockStyle.Fill, .Text = s.StoreName, .MaxLength = MaxStoreNameLength}
        Dim lblFoot As New Label() With {.Text = "Receipt footer", .AutoSize = True, .Margin = New Padding(0, 6, 8, 6), .ForeColor = UiTheme.TextSecondary}
        txtFooter = New TextBox() With {.Dock = DockStyle.Fill, .Text = s.ReceiptFooter, .MaxLength = MaxFooterLength}
        Dim lblCur As New Label() With {.Text = "Currency symbol", .AutoSize = True, .Margin = New Padding(0, 6, 8, 6), .ForeColor = UiTheme.TextSecondary}
        txtCurrency = New TextBox() With {.Dock = DockStyle.Fill, .Text = s.CurrencySymbol, .MaxLength = MaxCurrencySymbolLength}

        btnOk = New Button() With {.Text = "OK", .DialogResult = DialogResult.None, .AutoSize = True, .MinimumSize = New Size(100, 32)}
        btnCancel = New Button() With {.Text = "Cancel", .DialogResult = DialogResult.Cancel, .AutoSize = True, .MinimumSize = New Size(100, 32)}
        UiTheme.ApplyPrimaryButton(btnOk)
        UiTheme.ApplySecondaryButton(btnCancel)

        Dim buttonRow As New FlowLayoutPanel() With {.AutoSize = True, .FlowDirection = FlowDirection.RightToLeft, .Dock = DockStyle.Fill, .Padding = New Padding(0, 12, 0, 0)}
        buttonRow.Controls.Add(btnCancel)
        buttonRow.Controls.Add(btnOk)

        fields.Controls.Add(lblStore, 0, 0)
        fields.Controls.Add(txtStoreName, 1, 0)
        fields.Controls.Add(lblFoot, 0, 1)
        fields.Controls.Add(txtFooter, 1, 1)
        fields.Controls.Add(lblCur, 0, 2)
        fields.Controls.Add(txtCurrency, 1, 2)

        lblSettingsError = New Label() With {
            .AutoSize = True,
            .ForeColor = UiTheme.Danger,
            .Visible = False,
            .Margin = New Padding(0, 4, 0, 8),
            .MaximumSize = New Size(420, 0)
        }
        fields.Controls.Add(lblSettingsError, 0, 3)
        fields.SetColumnSpan(lblSettingsError, 2)

        fields.SetColumnSpan(buttonRow, 2)
        fields.Controls.Add(buttonRow, 0, 4)

        cardInner.Controls.Add(fields)
        root.Controls.Add(cardOuter, 0, 0)

        Me.Controls.Add(root)
        Me.AcceptButton = btnOk
        Me.CancelButton = btnCancel
    End Sub

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

    Private Sub txtStoreName_TextChanged(sender As Object, e As EventArgs) Handles txtStoreName.TextChanged
        ClearSettingsInputError()
    End Sub

    Private Sub txtFooter_TextChanged(sender As Object, e As EventArgs) Handles txtFooter.TextChanged
        ClearSettingsInputError()
    End Sub

    Private Sub txtCurrency_TextChanged(sender As Object, e As EventArgs) Handles txtCurrency.TextChanged
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

        If storeName.Length > MaxStoreNameLength Then
            Dim msg As String = String.Format(CultureInfo.CurrentCulture, "Store name cannot exceed {0} characters.", MaxStoreNameLength)
            ShowSettingsInputError(msg)
            MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtStoreName.Focus()
            Return
        End If

        Dim footer As String = txtFooter.Text.Trim()
        If footer.Length > MaxFooterLength Then
            Dim msg As String = String.Format(CultureInfo.CurrentCulture, "Receipt footer cannot exceed {0} characters.", MaxFooterLength)
            ShowSettingsInputError(msg)
            MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtFooter.Focus()
            Return
        End If

        Dim currencySym As String = txtCurrency.Text.Trim()
        If currencySym.Length < MinCurrencySymbolLength OrElse currencySym.Length > MaxCurrencySymbolLength Then
            ShowSettingsInputError(
                String.Format(CultureInfo.CurrentCulture, "Currency symbol must be {0}–{1} characters.", MinCurrencySymbolLength, MaxCurrencySymbolLength))
            MessageBox.Show(
                String.Format(CultureInfo.CurrentCulture, "Enter a currency symbol ({0}–{1} characters), for example $ or ₱.", MinCurrencySymbolLength, MaxCurrencySymbolLength),
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)
            txtCurrency.Focus()
            Return
        End If

        Dim data As New AppSettingsData With {
            .StoreName = storeName,
            .ReceiptFooter = footer,
            .CurrencySymbol = currencySym
        }
        AppSettings.Save(data)
        AuditLogger.LogAudit(
            "SETTINGS_CHANGED",
            "Store name / receipt footer / currency symbol updated.",
            AppSession.CurrentRole)

        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

End Class
