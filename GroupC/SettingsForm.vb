Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Edits store name, receipt footer, and currency symbol persisted to LocalApplicationData.
''' </summary>
Public Class SettingsForm
    Inherits Form

    Private txtStoreName As TextBox
    Private txtFooter As TextBox
    Private txtCurrency As TextBox
    Private WithEvents btnOk As Button
    Private WithEvents btnCancel As Button

    Private Sub SettingsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "Group C - Settings"
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MinimizeBox = False
        Me.MaximizeBox = False
        Me.Size = New Size(480, 320)
        Me.Font = New Font("Segoe UI", 10)
        Me.BackColor = Color.White

        AppSettings.Reload()
        Dim s As AppSettingsData = AppSettings.Current

        Dim root As New TableLayoutPanel()
        root.Dock = DockStyle.Fill
        root.Padding = New Padding(16)
        root.ColumnCount = 2
        root.RowCount = 5
        root.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        root.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        For r As Integer = 0 To 4
            root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Next

        Dim lblStore As New Label() With {.Text = "Store name", .AutoSize = True, .Margin = New Padding(0, 6, 8, 6)}
        txtStoreName = New TextBox() With {.Dock = DockStyle.Fill, .Text = s.StoreName}
        Dim lblFoot As New Label() With {.Text = "Receipt footer", .AutoSize = True, .Margin = New Padding(0, 6, 8, 6)}
        txtFooter = New TextBox() With {.Dock = DockStyle.Fill, .Text = s.ReceiptFooter}
        Dim lblCur As New Label() With {.Text = "Currency symbol", .AutoSize = True, .Margin = New Padding(0, 6, 8, 6)}
        txtCurrency = New TextBox() With {.Dock = DockStyle.Fill, .Text = s.CurrencySymbol, .MaxLength = 6}

        btnOk = New Button() With {.Text = "OK", .DialogResult = DialogResult.None, .AutoSize = True, .MinimumSize = New Size(100, 32)}
        btnCancel = New Button() With {.Text = "Cancel", .DialogResult = DialogResult.Cancel, .AutoSize = True, .MinimumSize = New Size(100, 32)}
        UiTheme.ApplyPrimaryButton(btnOk)
        UiTheme.ApplySecondaryButton(btnCancel)

        Dim buttonRow As New FlowLayoutPanel() With {.AutoSize = True, .FlowDirection = FlowDirection.RightToLeft, .Dock = DockStyle.Fill, .Padding = New Padding(0, 12, 0, 0)}
        buttonRow.Controls.Add(btnCancel)
        buttonRow.Controls.Add(btnOk)

        root.Controls.Add(lblStore, 0, 0)
        root.Controls.Add(txtStoreName, 1, 0)
        root.Controls.Add(lblFoot, 0, 1)
        root.Controls.Add(txtFooter, 1, 1)
        root.Controls.Add(lblCur, 0, 2)
        root.Controls.Add(txtCurrency, 1, 2)
        root.SetColumnSpan(buttonRow, 2)
        root.Controls.Add(buttonRow, 0, 4)

        Me.Controls.Add(root)
        Me.AcceptButton = btnOk
        Me.CancelButton = btnCancel
    End Sub

    Private Sub btnOk_Click(sender As Object, e As EventArgs) Handles btnOk.Click
        Dim data As New AppSettingsData With {
            .StoreName = txtStoreName.Text.Trim(),
            .ReceiptFooter = txtFooter.Text.Trim(),
            .CurrencySymbol = txtCurrency.Text.Trim()
        }
        AppSettings.Save(data)
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

End Class
