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
        ' 1. FORM SETUP: Full Screen & Responsive
        Me.SuspendLayout()
        Me.Text = "Group C — Sign in"

        ' Allow resizing and set default state to Maximized
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.WindowState = FormWindowState.Maximized
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MinimizeBox = True
        Me.MaximizeBox = True
        Me.MinimumSize = New Size(500, 450) ' Prevents user from making the window too tiny

        UiTheme.ApplyStandardWindowChrome(Me)

        ' 2. INITIALIZE CONTROLS
        Dim lblTitle As New Label() With {
            .Text = "Welcome Back",
            .Font = New Font("Segoe UI", 18, FontStyle.Bold),
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 20)
        }

        Dim lblRole As New Label() With {.Text = "Select Role:", .AutoSize = True, .Margin = New Padding(0, 10, 0, 5)}

        radAdmin = New RadioButton() With {.Text = "Administrator", .AutoSize = True, .Checked = True}
        radCashier = New RadioButton() With {.Text = "Cashier", .AutoSize = True}

        Dim pnlRoles As New FlowLayoutPanel() With {.AutoSize = True, .Margin = New Padding(0, 0, 0, 15)}
        pnlRoles.Controls.Add(radAdmin)
        pnlRoles.Controls.Add(radCashier)

        Dim lblSecret As New Label() With {.Text = "Password / PIN:", .AutoSize = True, .Margin = New Padding(0, 5, 0, 5)}

        txtSecret = New TextBox() With {
            .PasswordChar = "*"c,
            .Width = 280,
            .Font = New Font("Segoe UI", 12),
            .Margin = New Padding(0, 0, 0, 5)
        }

        lblHint = New Label() With {
            .Text = "Enter the admin password or cashier PIN.",
            .ForeColor = Color.Gray,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 25)
        }

        btnOk = New Button() With {.Text = "Sign In", .Size = New Size(130, 36), .Cursor = Cursors.Hand}
        btnCancel = New Button() With {.Text = "Cancel", .Size = New Size(100, 36), .Cursor = Cursors.Hand}

        Try
            UiTheme.ApplyPrimaryButton(btnOk)
            UiTheme.ApplySecondaryButton(btnCancel)
        Catch
        End Try

        Dim pnlButtons As New FlowLayoutPanel() With {
            .AutoSize = True,
            .FlowDirection = FlowDirection.LeftToRight,
            .Margin = New Padding(0)
        }
        pnlButtons.Controls.Add(btnOk)
        pnlButtons.Controls.Add(btnCancel)

        ' 3. ASSEMBLE THE "CARD"
        Dim loginCard As New FlowLayoutPanel() With {
            .FlowDirection = FlowDirection.TopDown,
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .WrapContents = False,
            .Padding = New Padding(0)
        }

        loginCard.Controls.Add(lblTitle)
        loginCard.Controls.Add(lblRole)
        loginCard.Controls.Add(pnlRoles)
        loginCard.Controls.Add(lblSecret)
        loginCard.Controls.Add(txtSecret)
        loginCard.Controls.Add(lblHint)
        loginCard.Controls.Add(pnlButtons)

        ' 4. THE RESPONSIVE CENTERING GRID
        ' Because the outer rows/columns are 50%, they act like fluid springs 
        ' that constantly adjust to window resizing!
        Dim centerGrid As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 3,
            .RowCount = 3
        }
        centerGrid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        centerGrid.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        centerGrid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))

        centerGrid.RowStyles.Add(New RowStyle(SizeType.Percent, 50.0F))
        centerGrid.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        centerGrid.RowStyles.Add(New RowStyle(SizeType.Percent, 50.0F))

        centerGrid.Controls.Add(loginCard, 1, 1)

        ' 5. FINAL WIRING
        Me.Controls.Clear()
        Me.Controls.Add(centerGrid)

        Me.AcceptButton = btnOk
        Me.CancelButton = btnCancel

        Me.ResumeLayout(True)
    End Sub

    Private Sub LoginForm_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        UpdateHintWrapWidth()
    End Sub

    Private Sub UpdateHintWrapWidth()
        If lblHint Is Nothing Then
            Return
        End If

        Dim w As Integer = Me.ClientSize.Width - 64
        If w < 200 Then
            w = 200
        End If

        lblHint.MaximumSize = New Size(w, 0)
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
