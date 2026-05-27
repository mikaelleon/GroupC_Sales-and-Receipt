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
    Private Const SettingsDialogWidth As Integer = 580
    Private Const SettingsDialogHeight As Integer = 520

    Private WithEvents txtStoreName As TextBox
    Private WithEvents txtFooter As TextBox
    Private WithEvents txtCurrency As TextBox
    Private lblSettingsError As Label
    Private WithEvents btnOk As Button
    Private WithEvents btnCancel As Button

    Private Sub SettingsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = AppBranding.WindowTitle("Settings")
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MinimizeBox = False
        Me.MaximizeBox = False
        Me.MinimumSize = New Size(520, 460)
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

        txtStoreName = New TextBox() With {
            .Text = settings.StoreName,
            .MaxLength = MaxStoreNameLength,
            .Dock = DockStyle.Fill
        }
        UiTheme.ApplyInputStyle(txtStoreName)

        txtFooter = New TextBox() With {
            .Text = settings.ReceiptFooter,
            .MaxLength = MaxFooterLength,
            .Multiline = True,
            .ScrollBars = ScrollBars.Vertical,
            .Height = 72,
            .Dock = DockStyle.Fill
        }
        UiTheme.ApplyInputStyle(txtFooter)

        txtCurrency = New TextBox() With {
            .Text = settings.CurrencySymbol,
            .MaxLength = MaxCurrencySymbolLength,
            .Dock = DockStyle.Fill
        }
        UiTheme.ApplyInputStyle(txtCurrency)

        btnOk = New Button() With {
            .Text = "&Save settings",
            .DialogResult = DialogResult.None,
            .AutoSize = True,
            .MinimumSize = New Size(120, UiTheme.ButtonHeight),
            .Cursor = Cursors.Hand
        }
        btnCancel = New Button() With {
            .Text = "Cancel",
            .DialogResult = DialogResult.Cancel,
            .AutoSize = True,
            .MinimumSize = New Size(100, UiTheme.ButtonHeight),
            .Cursor = Cursors.Hand
        }
        UiTheme.ApplyPrimaryButton(btnOk)
        UiTheme.ApplySecondaryButton(btnCancel)

        lblSettingsError = New Label() With {
            .AutoSize = True,
            .ForeColor = UiTheme.ColDanger,
            .Font = UiTheme.FontBodySmall,
            .Visible = False,
            .Margin = New Padding(0, UiTheme.PadControl, 0, 0),
            .MaximumSize = New Size(SettingsDialogWidth - (UiTheme.PadPage * 2) - (UiTheme.PadCard * 2), 0)
        }

        Dim buttonRow As FlowLayoutPanel = UiTheme.CreateButtonRow(FlowDirection.RightToLeft)
        buttonRow.Dock = DockStyle.Fill
        buttonRow.Controls.Add(btnCancel)
        buttonRow.Controls.Add(btnOk)

        Dim fields As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 9,
            .BackColor = Color.Transparent
        }
        fields.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        fields.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        fields.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        fields.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        fields.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        fields.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        fields.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        fields.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        fields.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim headerPanel As New Panel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .Dock = DockStyle.Top,
            .Margin = New Padding(0, 0, 0, UiTheme.PadSection),
            .BackColor = Color.Transparent
        }

        Dim lblTitle As New Label() With {
            .Text = "Store settings",
            .Font = UiTheme.FontHeading,
            .ForeColor = UiTheme.ColPrimary,
            .AutoSize = True,
            .Dock = DockStyle.Top,
            .Margin = New Padding(0, 0, 0, UiTheme.PadTight)
        }
        Dim lblSubtitle As New Label() With {
            .Text = "Update branding shown on receipts and across the application.",
            .Font = UiTheme.FontBodySmall,
            .ForeColor = UiTheme.ColTextSecondary,
            .AutoSize = True,
            .MaximumSize = New Size(SettingsDialogWidth - (UiTheme.PadPage * 2) - (UiTheme.PadCard * 2), 0),
            .Dock = DockStyle.Top
        }
        headerPanel.Controls.Add(lblSubtitle)
        headerPanel.Controls.Add(lblTitle)

        Dim sectionHeader As Panel = UiTheme.CreateSectionHeader("Receipt branding")

        fields.Controls.Add(headerPanel, 0, 0)
        fields.Controls.Add(sectionHeader, 0, 1)
        fields.Controls.Add(CreateFieldBlock(
            "Store name",
            txtStoreName,
            "Displayed on receipts, reports, and the sidebar."), 0, 2)
        fields.Controls.Add(CreateFieldBlock(
            "Receipt footer",
            txtFooter,
            "Closing message printed at the bottom of each receipt."), 0, 3)
        fields.Controls.Add(CreateFieldBlock(
            "Currency symbol",
            txtCurrency,
            "Prefix for prices and totals (for example ₱ or $)."), 0, 4)
        fields.Controls.Add(lblSettingsError, 0, 5)
        fields.Controls.Add(buttonRow, 0, 6)

        Dim cardOuter As Panel = UiTheme.CreateCardPanel(New Padding(UiTheme.PadCard))
        cardOuter.Dock = DockStyle.Fill
        Dim cardInner As Panel = UiTheme.GetCardContentHost(cardOuter)
        If cardInner IsNot Nothing Then
            cardInner.Controls.Add(fields)
        Else
            cardOuter.Controls.Add(fields)
        End If

        Dim root As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(UiTheme.PadPage),
            .ColumnCount = 1,
            .RowCount = 1,
            .BackColor = UiTheme.ColBackground
        }
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        root.Controls.Add(cardOuter, 0, 0)

        Me.Controls.Add(root)
        Me.AcceptButton = btnOk
        Me.CancelButton = btnCancel
        Me.ResumeLayout(True)
    End Sub

    Private Shared Function CreateFieldBlock(caption As String, input As Control, hint As String) As Panel
        Dim block As New TableLayoutPanel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .Dock = DockStyle.Top,
            .ColumnCount = 1,
            .RowCount = 3,
            .Margin = New Padding(0, 0, 0, UiTheme.PadSection),
            .BackColor = Color.Transparent
        }
        block.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        block.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        block.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim lblCaption As New Label() With {
            .Text = caption,
            .AutoSize = True,
            .Font = UiTheme.FontBodyBold,
            .ForeColor = UiTheme.ColTextPrimary,
            .Margin = New Padding(0, 0, 0, UiTheme.PadTight)
        }

        input.Margin = New Padding(0)

        Dim lblHint As New Label() With {
            .Text = hint,
            .AutoSize = True,
            .Font = UiTheme.FontBodySmall,
            .ForeColor = UiTheme.ColTextSecondary,
            .Margin = New Padding(0, UiTheme.PadTight, 0, 0),
            .MaximumSize = New Size(SettingsDialogWidth - (UiTheme.PadPage * 2) - (UiTheme.PadCard * 2), 0)
        }

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
