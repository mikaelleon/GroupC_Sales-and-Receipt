Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

''' <summary>
''' Role selection and password/PIN entry before the main menu appears.
''' </summary>
Public Class LoginForm
    Inherits Form

    Private Const SecretMaskChar As Char = "*"c
    Private Const SecretPlaceholder As String = "Please enter your password"
    Private Const SecretFieldWidth As Integer = 320
    Private Const SecretFieldHeight As Integer = 44
    Private Const SecretToggleWidth As Integer = 44
    Private Const SecretTextPaddingLeft As Integer = 14

    Private WithEvents radAdmin As RadioButton
    Private WithEvents radCashier As RadioButton
    Private txtUsername As TextBox
    Private pnlUsername As FlowLayoutPanel
    Private lblUsername As Label
    Private txtSecret As TextBox
    Private pnlSecret As Panel
    Private lblSecretCaption As Label
    Private WithEvents pnlToggleSecret As PasswordTogglePanel
    Private lblHint As Label
    Private WithEvents btnOk As Button
    Private WithEvents btnCancel As Button
    Private picLoginLogo As PictureBox

    Private passwordVisible As Boolean
    Private Const LoginLogoHeight As Integer = 88

    Private Sub LoginForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' 1. FORM SETUP: Full Screen & Responsive
        Me.SuspendLayout()
        Me.Text = AppBranding.WindowTitle("Sign in")

        UiTheme.ApplyMaximizedWorkspaceDefaults(Me, 500, 450)
        Me.MinimizeBox = True
        Me.MaximizeBox = True

        UiTheme.ApplyStandardWindowChrome(Me)

        ' 2. INITIALIZE CONTROLS
        picLoginLogo = New PictureBox() With {
            .Width = SecretFieldWidth,
            .Height = LoginLogoHeight,
            .SizeMode = PictureBoxSizeMode.Zoom,
            .BackColor = Color.Transparent,
            .Margin = New Padding(0, 0, 0, 12)
        }

        Dim loginLogo As Image = ReceiptBranding.TryGetReceiptLogo()
        Dim hasLogo As Boolean = loginLogo IsNot Nothing
        If hasLogo Then
            picLoginLogo.Image = loginLogo
        Else
            picLoginLogo.Visible = False
        End If

        Dim lblTitle As New Label() With {
            .Text = AppBranding.ApplicationName,
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = UiTheme.PrimaryAccent,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 4),
            .Visible = Not hasLogo
        }

        Dim lblSubtitle As New Label() With {
            .Text = "Sign in to continue",
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Regular),
            .ForeColor = UiTheme.TextSecondary,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 12)
        }

        Dim lblRole As New Label() With {.Text = "Select Role:", .AutoSize = True, .Margin = New Padding(0, 10, 0, 5)}

        radAdmin = New RadioButton() With {.Text = "Administrator", .AutoSize = True, .Checked = True, .ForeColor = UiTheme.TextPrimary, .Font = New Font("Segoe UI", 10.5F)}
        radCashier = New RadioButton() With {.Text = "Cashier", .AutoSize = True, .ForeColor = UiTheme.TextPrimary, .Font = New Font("Segoe UI", 10.5F)}

        Dim pnlRoles As New FlowLayoutPanel() With {.AutoSize = True, .Margin = New Padding(0, 0, 0, 15)}
        pnlRoles.Controls.Add(radAdmin)
        pnlRoles.Controls.Add(radCashier)

        lblUsername = New Label() With {
            .Text = "Username:",
            .AutoSize = False,
            .Width = SecretFieldWidth,
            .Margin = New Padding(0, 5, 0, 6),
            .ForeColor = UiTheme.TextPrimary,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Regular),
            .Visible = False
        }

        txtUsername = New TextBox() With {
            .Font = New Font("Segoe UI", 11.0F),
            .Width = SecretFieldWidth,
            .Margin = New Padding(0, 0, 0, 8),
            .PlaceholderText = "Cashier username"
        }
        UiTheme.ApplyFilledTextInputVisual(txtUsername)

        pnlUsername = New FlowLayoutPanel() With {
            .FlowDirection = FlowDirection.TopDown,
            .AutoSize = True,
            .WrapContents = False,
            .Visible = False,
            .Width = SecretFieldWidth
        }
        pnlUsername.Controls.Add(lblUsername)
        pnlUsername.Controls.Add(txtUsername)

        lblSecretCaption = New Label() With {
            .Text = "Password:",
            .AutoSize = False,
            .Width = SecretFieldWidth,
            .Margin = New Padding(0, 5, 0, 6),
            .ForeColor = UiTheme.TextPrimary,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Regular)
        }

        pnlSecret = BuildPasswordField()

        lblHint = New Label() With {
            .Text = "Administrators use the admin password. Cashiers sign in with a registered username and password.",
            .ForeColor = UiTheme.TextSecondary,
            .AutoSize = True,
            .Margin = New Padding(0, 4, 0, 28)
        }

        btnOk = New Button() With {.Text = "Sign In", .AutoSize = True, .MinimumSize = New Size(120, 40), .Cursor = Cursors.Hand, .DialogResult = DialogResult.None}
        btnCancel = New Button() With {.Text = "Cancel", .AutoSize = True, .MinimumSize = New Size(100, 40), .Cursor = Cursors.Hand, .DialogResult = DialogResult.Cancel}

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
            .Padding = New Padding(0),
            .Width = SecretFieldWidth
        }

        loginCard.Controls.Add(picLoginLogo)
        loginCard.Controls.Add(lblTitle)
        loginCard.Controls.Add(lblSubtitle)
        loginCard.Controls.Add(lblRole)
        loginCard.Controls.Add(pnlRoles)
        loginCard.Controls.Add(pnlUsername)
        loginCard.Controls.Add(lblSecretCaption)
        loginCard.Controls.Add(pnlSecret)
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
        UpdateRoleFields()
    End Sub

    Private Sub radAdmin_CheckedChanged(sender As Object, e As EventArgs) Handles radAdmin.CheckedChanged
        UpdateRoleFields()
    End Sub

    Private Sub radCashier_CheckedChanged(sender As Object, e As EventArgs) Handles radCashier.CheckedChanged
        UpdateRoleFields()
    End Sub

    Private Sub UpdateRoleFields()
        If pnlUsername Is Nothing Then
            Return
        End If

        Dim cashierMode As Boolean = radCashier IsNot Nothing AndAlso radCashier.Checked
        pnlUsername.Visible = cashierMode
        lblUsername.Visible = cashierMode
        txtUsername.Visible = cashierMode


        If txtSecret IsNot Nothing Then
            txtSecret.PlaceholderText = If(cashierMode, "Account password", SecretPlaceholder)
        End If

        If lblHint IsNot Nothing Then
            lblHint.Text = If(
                cashierMode,
                "Use the username and password created by an administrator in Manage Cashiers.",
                "Enter the administrator password.")
        End If

        If cashierMode Then
            txtUsername?.Focus()
        Else
            txtUsername?.Clear()
            txtSecret?.Focus()
        End If
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        If picLoginLogo?.Image IsNot Nothing Then
            picLoginLogo.Image.Dispose()
            picLoginLogo.Image = Nothing
        End If

        MyBase.OnFormClosed(e)
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

            AppSession.ClearCashierIdentity()
            AppSession.CurrentRole = AppSession.RoleAdmin
        Else
            Dim username As String = txtUsername.Text.Trim()
            If username.Length = 0 Then
                MessageBox.Show("Enter your cashier username.", "Sign in", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtUsername.Focus()
                Return
            End If

            If secret.Length = 0 Then
                MessageBox.Show("Enter your account password.", "Sign in", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtSecret.Focus()
                Return
            End If

            Dim auth As CashierAccountService.CashierLoginResult = CashierAccountService.TryAuthenticate(username, secret)
            If Not auth.Success Then
                AuditLogger.LogAudit("LOGIN_FAILED", auth.ErrorMessage, "Cashier: " & username)
                MessageBox.Show(auth.ErrorMessage, "Sign in", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtSecret.Focus()
                Return
            End If

            AppSession.CurrentRole = AppSession.RoleCashier
            AppSession.CurrentCashierId = auth.CashierId
            AppSession.CurrentUsername = auth.Username
            AppSession.CurrentCashierDisplayName = If(
                String.IsNullOrWhiteSpace(auth.DisplayName),
                auth.Username,
                auth.DisplayName.Trim())
        End If

        AuditLogger.LogAudit("LOGIN_SUCCESS", "Signed in to " & AppBranding.ApplicationName & ".", AppSession.GetAuditIdentity())

        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        ' Empty password/PIN: dismiss sign-in (startup exits app; logout returns to menu).
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Function BuildPasswordField() As Panel
        txtSecret = New TextBox() With {
            .PasswordChar = SecretMaskChar,
            .PlaceholderText = SecretPlaceholder,
            .Font = New Font("Segoe UI", 11.0F),
            .BorderStyle = BorderStyle.None,
            .Dock = DockStyle.Fill,
            .Margin = New Padding(SecretTextPaddingLeft, 10, 4, 10),
            .BackColor = UiTheme.CardSurface
        }

        pnlToggleSecret = New PasswordTogglePanel() With {
            .Dock = DockStyle.Fill,
            .PasswordVisible = passwordVisible,
            .AccessibleName = "Show password",
            .AccessibleDescription = "Toggles visibility of the password or PIN."
        }

        Dim secretLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .BackColor = UiTheme.CardSurface,
            .Margin = New Padding(0),
            .Padding = New Padding(0)
        }
        secretLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        secretLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, SecretToggleWidth))
        secretLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        secretLayout.Controls.Add(txtSecret, 0, 0)
        secretLayout.Controls.Add(pnlToggleSecret, 1, 0)

        Dim inner As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = UiTheme.CardSurface,
            .MinimumSize = New Size(0, SecretFieldHeight - 2)
        }
        inner.Controls.Add(secretLayout)

        Dim outer As New Panel() With {
            .Width = SecretFieldWidth,
            .Height = SecretFieldHeight,
            .Margin = New Padding(0, 0, 0, 8),
            .BackColor = UiTheme.CardBorder,
            .Padding = New Padding(1)
        }
        outer.Controls.Add(inner)
        Return outer
    End Function

    Private Sub pnlToggleSecret_PasswordVisibilityChanged(sender As Object, visible As Boolean) Handles pnlToggleSecret.PasswordVisibilityChanged
        ApplyPasswordVisibility(visible)
    End Sub

    Private Sub ApplyPasswordVisibility(visible As Boolean)
        passwordVisible = visible
        txtSecret.PasswordChar = If(passwordVisible, ChrW(0), SecretMaskChar)
        pnlToggleSecret.PasswordVisible = passwordVisible
        pnlToggleSecret.AccessibleName = If(passwordVisible, "Hide password", "Show password")
        txtSecret.Focus()
        txtSecret.SelectionStart = txtSecret.Text.Length
    End Sub

    ''' <summary>Password visibility toggle drawn inside the secret field.</summary>
    Private Class PasswordTogglePanel
        Inherits Panel

        Public Event PasswordVisibilityChanged As EventHandler(Of Boolean)

        Private _passwordVisible As Boolean
        Private _hover As Boolean
        Private _pressed As Boolean

        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
        <Browsable(False)>
        Public Property PasswordVisible As Boolean
            Get
                Return _passwordVisible
            End Get
            Set(value As Boolean)
                If _passwordVisible = value Then
                    Return
                End If

                _passwordVisible = value
                Invalidate()
            End Set
        End Property

        Public Sub New()
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
            UpdateStyles()
            TabStop = False
            Cursor = Cursors.Hand
            BackColor = UiTheme.CardSurface
            Size = New Size(SecretToggleWidth, SecretFieldHeight)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)

            Dim g As Graphics = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.PixelOffsetMode = PixelOffsetMode.HighQuality

            Dim iconColor As Color = UiTheme.TextSecondary
            Dim fillColor As Color = Color.Transparent

            If _pressed Then
                fillColor = Color.FromArgb(56, UiTheme.PrimaryAccent)
                iconColor = UiTheme.PrimaryAccentPressed
            ElseIf _hover Then
                fillColor = Color.FromArgb(38, UiTheme.PrimaryAccent)
                iconColor = UiTheme.PrimaryAccent
            End If

            Dim bounds As Rectangle = ClientRectangle
            Dim hitSize As Integer = Math.Min(bounds.Width, bounds.Height) - 10
            Dim hit As New Rectangle(
                bounds.X + ((bounds.Width - hitSize) \ 2),
                bounds.Y + ((bounds.Height - hitSize) \ 2),
                hitSize,
                hitSize)

            If fillColor.A > 0 Then
                Dim radius As Integer = Math.Max(4, hitSize \ 4)
                Using path As GraphicsPath = CreateRoundedRect(hit, radius)
                    Using brush As New SolidBrush(fillColor)
                        g.FillPath(brush, path)
                    End Using
                End Using
            End If

            PasswordEyeIconRenderer.Draw(g, hit, iconColor, _passwordVisible)
        End Sub

        Protected Overrides Sub OnMouseEnter(e As EventArgs)
            MyBase.OnMouseEnter(e)
            _hover = True
            Invalidate()
        End Sub

        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            MyBase.OnMouseLeave(e)
            _hover = False
            _pressed = False
            Invalidate()
        End Sub

        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            MyBase.OnMouseDown(e)
            If e.Button <> MouseButtons.Left Then
                Return
            End If

            _pressed = True
            Invalidate()
        End Sub

        Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
            MyBase.OnMouseUp(e)
            If e.Button <> MouseButtons.Left Then
                Return
            End If

            Dim wasPressed As Boolean = _pressed
            _pressed = False
            Invalidate()

            If Not wasPressed OrElse Not ClientRectangle.Contains(PointToClient(Cursor.Position)) Then
                Return
            End If

            PasswordVisible = Not PasswordVisible
            RaiseEvent PasswordVisibilityChanged(Me, PasswordVisible)
        End Sub

        Private Shared Function CreateRoundedRect(bounds As Rectangle, radius As Integer) As GraphicsPath
            Dim path As New GraphicsPath()
            Dim d As Integer = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height))
            If d < 2 Then
                path.AddRectangle(bounds)
                Return path
            End If

            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90)
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90)
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90)
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90)
            path.CloseFigure()
            Return path
        End Function
    End Class

    Private NotInheritable Class PasswordEyeIconRenderer
        Private Sub New()
        End Sub

        ''' <param name="passwordVisible">When true, draws the "hidden" eye-off icon.</param>
        Public Shared Sub Draw(g As Graphics, bounds As Rectangle, color As Color, passwordVisible As Boolean)
            Dim cx As Single = bounds.X + (bounds.Width / 2.0F)
            Dim cy As Single = bounds.Y + (bounds.Height / 2.0F)
            Dim halfW As Single = bounds.Width * 0.38F
            Dim halfH As Single = bounds.Height * 0.24F

            Using path As New GraphicsPath()
                path.AddBezier(
                    cx - halfW, cy,
                    cx - halfW * 0.55F, cy - halfH,
                    cx + halfW * 0.55F, cy - halfH,
                    cx + halfW, cy)
                path.AddBezier(
                    cx + halfW, cy,
                    cx + halfW * 0.55F, cy + halfH,
                    cx - halfW * 0.55F, cy + halfH,
                    cx - halfW, cy)

                Using pen As New Pen(color, 1.75F)
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round
                    pen.LineJoin = LineJoin.Round
                    g.DrawPath(pen, path)
                End Using
            End Using

            Dim pupil As Single = Math.Min(bounds.Width, bounds.Height) * 0.16F
            Using brush As New SolidBrush(color)
                g.FillEllipse(brush, cx - pupil, cy - pupil, pupil * 2.0F, pupil * 2.0F)
            End Using

            If passwordVisible Then
                Using pen As New Pen(color, 1.75F)
                    pen.StartCap = LineCap.Round
                    pen.EndCap = LineCap.Round
                    Dim pad As Single = bounds.Width * 0.12F
                    g.DrawLine(pen, bounds.Left + pad, bounds.Bottom - pad, bounds.Right - pad, bounds.Top + pad)
                End Using
            End If
        End Sub
    End Class

End Class
