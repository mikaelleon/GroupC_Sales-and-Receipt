Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Role selection and password/PIN entry before the main menu appears.
''' </summary>
Public Class LoginForm
    Inherits Form

    Private WithEvents radAdmin As RadioButton
    Private WithEvents radCashier As RadioButton
    Private txtSecret As TextBox
    Private lblHint As Label
    Private WithEvents btnOk As Button
    Private WithEvents btnCancel As Button

    Private Sub LoginForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "Group C — Sign in"
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MinimizeBox = False
        Me.MaximizeBox = False
        Me.Size = New Size(440, 320)
        UiTheme.ApplyStandardWindowChrome(Me)

        Dim root As New TableLayoutPanel()
        root.Dock = DockStyle.Fill
        root.Padding = New Padding(16)
        root.ColumnCount = 1
        root.RowCount = 7
        For i As Integer = 0 To 6
            root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Next

        Dim title As New Label() With {
            .Text = "SIGN IN",
            .Font = New Font("Segoe UI", 14.0F, FontStyle.Bold),
            .ForeColor = UiTheme.TextPrimary,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 8)
        }

        radAdmin = New RadioButton() With {.Text = "Administrator (full access)", .AutoSize = True, .Margin = New Padding(0, 4, 0, 4), .ForeColor = UiTheme.TextPrimary}
        radCashier = New RadioButton() With {.Text = "Cashier (sales and receipts only)", .AutoSize = True, .Checked = True, .Margin = New Padding(0, 4, 0, 8), .ForeColor = UiTheme.TextPrimary}

        Dim lblSecret As New Label() With {.Text = "Password / PIN", .AutoSize = True, .Margin = New Padding(0, 4, 0, 4), .ForeColor = UiTheme.TextSecondary}

        txtSecret = New TextBox() With {.Dock = DockStyle.Fill, .UseSystemPasswordChar = True}

        lblHint = New Label() With {
            .Text = "Administrators must enter the configured password. Cashiers: leave blank unless a PIN is configured in DatabaseConfig.",
            .AutoSize = True,
            .MaximumSize = New Size(400, 0),
            .ForeColor = UiTheme.TextSecondary,
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic),
            .Margin = New Padding(0, 6, 0, 12)
        }

        Dim buttons As New FlowLayoutPanel() With {.AutoSize = True, .FlowDirection = FlowDirection.RightToLeft, .Dock = DockStyle.Fill, .Padding = New Padding(0, 4, 0, 0)}
        btnOk = New Button() With {.Text = "OK", .AutoSize = True, .MinimumSize = New Size(100, 32), .DialogResult = DialogResult.None}
        btnCancel = New Button() With {.Text = "Cancel", .AutoSize = True, .MinimumSize = New Size(100, 32), .DialogResult = DialogResult.Cancel}
        UiTheme.ApplyPrimaryButton(btnOk)
        UiTheme.ApplySecondaryButton(btnCancel)
        buttons.Controls.Add(btnCancel)
        buttons.Controls.Add(btnOk)

        root.Controls.Add(title, 0, 0)
        root.Controls.Add(radAdmin, 0, 1)
        root.Controls.Add(radCashier, 0, 2)
        root.Controls.Add(lblSecret, 0, 3)
        root.Controls.Add(txtSecret, 0, 4)
        root.Controls.Add(lblHint, 0, 5)
        root.Controls.Add(buttons, 0, 6)

        Me.Controls.Add(root)
        Me.AcceptButton = btnOk
        Me.CancelButton = btnCancel
    End Sub

    Private Sub btnOk_Click(sender As Object, e As EventArgs) Handles btnOk.Click
        Dim secret As String = txtSecret.Text.Trim()

        Try
            DatabaseInitializer.EnsureDatabase()
        Catch
        End Try

        If radAdmin.Checked Then
            If Not String.Equals(secret, DatabaseConfig.HardcodedAdminPassword, StringComparison.Ordinal) Then
                AuditLogger.LogAudit("LOGIN_FAILED", "Invalid administrator password.", "Admin sign-in attempt")
                MessageBox.Show("Invalid administrator password.", "Sign in", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtSecret.Focus()
                Return
            End If

            AppSession.CurrentRole = AppSession.RoleAdmin
        Else
            Dim pinRequired As String = DatabaseConfig.HardcodedCashierPin
            If pinRequired.Length > 0 AndAlso Not String.Equals(secret, pinRequired, StringComparison.Ordinal) Then
                AuditLogger.LogAudit("LOGIN_FAILED", "Invalid cashier PIN.", "Cashier sign-in attempt")
                MessageBox.Show("Invalid cashier PIN.", "Sign in", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtSecret.Focus()
                Return
            End If

            AppSession.CurrentRole = AppSession.RoleCashier
        End If

        AuditLogger.LogAudit("LOGIN_SUCCESS", "Signed in to Group C POS.", AppSession.CurrentRole)

        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

End Class
