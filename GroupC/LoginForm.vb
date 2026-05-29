Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' Role selection and password/PIN entry before the main menu appears.
''' </summary>
Public Class LoginForm
    Inherits Form

    Private Const SecretMaskChar As Char = "*"c
    Private Const SecretPlaceholder As String = "Please enter your password"
    Private Const LoginCardContentWidth As Integer = 380
    Private Const LoginLogoMaxHeight As Integer = 60
    Private Const SecretFieldHeight As Integer = UiTheme.InputHeight + 2
    Private Const SecretToggleWidth As Integer = 36
    Private Const SecretTextPaddingLeft As Integer = UiTheme.PadControl

    Private WithEvents radAdmin As RadioButton
    Private WithEvents radCashier As RadioButton
    Private txtUsername As TextBox
    Private pnlUsername As FlowLayoutPanel
    Private lblUsername As Label
    Private txtSecret As TextBox
    Private pnlSecret As Panel
    Private pnlSecretBorder As Panel
    Private lblSecretCaption As Label
    Private WithEvents pnlToggleSecret As PasswordTogglePanel
    Private lblHint As Label
    Private lblError As Label
    Private WithEvents btnOk As Button
    Private WithEvents btnCancel As Button
    Private picLoginLogo As PictureBox

    Private passwordVisible As Boolean

    Private Sub LoginForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SuspendLayout()
        Me.Text = AppBranding.WindowTitle("Sign in")
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MinimumSize = New Size(420, 480)
        Me.ClientSize = New Size(480, 640)
        Me.MinimizeBox = True
        Me.MaximizeBox = True
        UiTheme.ApplyStandardWindowChrome(Me)

        picLoginLogo = New PictureBox() With {
            .Width = LoginCardContentWidth,
            .Height = LoginLogoMaxHeight,
            .SizeMode = PictureBoxSizeMode.Zoom,
            .BackColor = Color.Transparent,
            .Margin = New Padding(0, 0, 0, UiTheme.PadCard)
        }

        Dim loginLogo As Image = TryLoadLoginLogo()
        Dim hasLogo As Boolean = loginLogo IsNot Nothing
        If hasLogo Then
            picLoginLogo.Image = loginLogo
        Else
            picLoginLogo.Visible = False
        End If

        Dim lblTitle As New Label() With {
            .Text = AppBranding.ApplicationName,
            .Font = UiTheme.FontHeading,
            .ForeColor = UiTheme.ColPrimary,
            .AutoSize = False,
            .Width = LoginCardContentWidth,
            .Height = 28,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl),
            .Visible = Not hasLogo
        }

        Dim cardDivider As New Panel() With {
            .Height = 1,
            .Width = LoginCardContentWidth,
            .BackColor = UiTheme.ColBorder,
            .Margin = New Padding(0, UiTheme.PadCard, 0, UiTheme.PadCard)
        }

        Dim lblSignInHeading As New Label() With {
            .Text = "Sign in to continue",
            .Font = UiTheme.FontSubheading,
            .ForeColor = UiTheme.ColTextSecondary,
            .AutoSize = False,
            .Width = LoginCardContentWidth,
            .Height = 22,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        }

        Dim lblRole As New Label() With {
            .Text = "Select role:",
            .AutoSize = True,
            .Font = UiTheme.FontBodyBold,
            .ForeColor = UiTheme.ColTextPrimary,
            .Margin = New Padding(0, UiTheme.PadTight, 0, UiTheme.PadTight)
        }

        radAdmin = New RadioButton() With {
            .Text = "Administrator",
            .AutoSize = True,
            .Checked = True,
            .ForeColor = UiTheme.ColTextPrimary,
            .Font = UiTheme.FontBody,
            .Margin = New Padding(0, 0, UiTheme.PadSection, 0),
            .TabStop = True
        }
        radCashier = New RadioButton() With {
            .Text = "Cashier",
            .AutoSize = True,
            .ForeColor = UiTheme.ColTextPrimary,
            .Font = UiTheme.FontBody,
            .Margin = New Padding(0),
            .TabStop = True
        }

        Dim pnlRoles As New FlowLayoutPanel() With {
            .AutoSize = True,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = False,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl),
            .BackColor = Color.Transparent,
            .Width = LoginCardContentWidth
        }
        pnlRoles.Controls.Add(radAdmin)
        pnlRoles.Controls.Add(radCashier)

        lblUsername = New Label() With {
            .Text = "Username",
            .AutoSize = True,
            .Margin = New Padding(0, UiTheme.PadTight, 0, UiTheme.PadTight),
            .ForeColor = UiTheme.ColTextPrimary,
            .Font = UiTheme.FontBodyBold,
            .Visible = False
        }

        txtUsername = New TextBox() With {
            .PlaceholderText = "Cashier username",
            .BorderStyle = BorderStyle.None,
            .Dock = DockStyle.Fill,
            .Margin = New Padding(SecretTextPaddingLeft, 0, UiTheme.PadControl, 0),
            .TabStop = True
        }
        UiTheme.ApplyInputStyle(txtUsername)

        Dim usernameInner As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = UiTheme.ColSurface,
            .MinimumSize = New Size(0, UiTheme.InputHeight),
            .Padding = New Padding(0, 1, 0, 1)
        }
        usernameInner.Controls.Add(txtUsername)

        Dim usernameOuter As New Panel() With {
            .Width = LoginCardContentWidth,
            .Height = SecretFieldHeight,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl),
            .BackColor = UiTheme.ColBorder,
            .Padding = New Padding(1)
        }
        usernameOuter.Controls.Add(usernameInner)

        pnlUsername = New FlowLayoutPanel() With {
            .FlowDirection = FlowDirection.TopDown,
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .WrapContents = False,
            .Visible = False,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl),
            .Width = LoginCardContentWidth,
            .BackColor = Color.Transparent
        }
        pnlUsername.Controls.Add(lblUsername)
        pnlUsername.Controls.Add(usernameOuter)

        lblSecretCaption = New Label() With {
            .Text = "Password",
            .AutoSize = True,
            .Margin = New Padding(0, UiTheme.PadTight, 0, UiTheme.PadTight),
            .ForeColor = UiTheme.ColTextPrimary,
            .Font = UiTheme.FontBodyBold
        }

        pnlSecret = BuildPasswordField()

        lblError = New Label() With {
            .Text = String.Empty,
            .ForeColor = UiTheme.ColDanger,
            .Font = UiTheme.FontCaption,
            .AutoSize = True,
            .Visible = False,
            .Margin = New Padding(0, UiTheme.PadControl, 0, 0),
            .Width = LoginCardContentWidth
        }

        lblHint = New Label() With {
            .Text = "Administrators use the admin password. Cashiers sign in with a registered username and password.",
            .ForeColor = UiTheme.ColTextSecondary,
            .Font = UiTheme.FontCaption,
            .AutoSize = True,
            .MaximumSize = New Size(LoginCardContentWidth, 0),
            .Margin = New Padding(0, UiTheme.PadControl, 0, UiTheme.PadControl)
        }

        btnOk = New Button() With {
            .Text = "Sign In",
            .DialogResult = DialogResult.None,
            .Margin = New Padding(0, UiTheme.PadSection, 0, UiTheme.PadControl),
            .TabStop = True,
            .Width = LoginCardContentWidth,
            .Height = UiTheme.ButtonHeight
        }
        UiTheme.ApplyPrimaryButton(btnOk)

        btnCancel = New Button() With {
            .Text = "Exit",
            .DialogResult = DialogResult.Cancel,
            .TabStop = True,
            .Width = LoginCardContentWidth,
            .Height = UiTheme.ButtonHeight
        }
        UiTheme.ApplyGhostButton(btnCancel)

        Dim cardStack As New FlowLayoutPanel() With {
            .FlowDirection = FlowDirection.TopDown,
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .WrapContents = False,
            .Width = LoginCardContentWidth,
            .Padding = Padding.Empty,
            .BackColor = Color.Transparent
        }

        cardStack.Controls.Add(picLoginLogo)
        cardStack.Controls.Add(lblTitle)
        cardStack.Controls.Add(cardDivider)
        cardStack.Controls.Add(lblSignInHeading)
        cardStack.Controls.Add(lblRole)
        cardStack.Controls.Add(pnlRoles)
        cardStack.Controls.Add(pnlUsername)
        cardStack.Controls.Add(lblSecretCaption)
        cardStack.Controls.Add(pnlSecret)
        cardStack.Controls.Add(lblError)
        cardStack.Controls.Add(lblHint)
        cardStack.Controls.Add(btnOk)
        cardStack.Controls.Add(btnCancel)

        Dim loginCardOuter As New Panel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .BackColor = UiTheme.ColBorder,
            .Padding = New Padding(1)
        }

        Dim loginCardInner As New Panel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .BackColor = UiTheme.ColSurface,
            .Padding = New Padding(UiTheme.PadCard)
        }
        loginCardInner.Controls.Add(cardStack)
        loginCardOuter.Controls.Add(loginCardInner)

        Dim centerGrid As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 3,
            .RowCount = 3,
            .BackColor = UiTheme.ColBackground
        }
        centerGrid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        centerGrid.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        centerGrid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        centerGrid.RowStyles.Add(New RowStyle(SizeType.Percent, 50.0F))
        centerGrid.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        centerGrid.RowStyles.Add(New RowStyle(SizeType.Percent, 50.0F))
        centerGrid.Controls.Add(loginCardOuter, 1, 1)

        Me.Controls.Clear()
        Me.Controls.Add(centerGrid)

        Me.AcceptButton = btnOk
        Me.CancelButton = btnCancel

        radAdmin.TabIndex = 0
        radCashier.TabIndex = 1
        txtUsername.TabIndex = 2
        txtSecret.TabIndex = 3
        btnOk.TabIndex = 4
        btnCancel.TabIndex = 5

        AddHandler txtSecret.Enter, Sub(s, ev) ClearLoginError()
        AddHandler txtUsername.Enter, Sub(s, ev) ClearLoginError()

        Me.ResumeLayout(True)
        UpdateRoleFields()
        UpdateHintWrapWidth()
    End Sub

    Private Shared Function TryLoadLoginLogo() As Image
        Dim appLogoPath As String = Path.Combine(AppContext.BaseDirectory, "Assets", "AppLogo.png")
        If File.Exists(appLogoPath) Then
            Try
                Using stream As New FileStream(appLogoPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                    Using temp As Image = Image.FromStream(stream)
                        Return New Bitmap(temp)
                    End Using
                End Using
            Catch
            End Try
        End If

        Return LogoBranding.TryGetBrandingLogo()
    End Function

    Private Sub ClearLoginError()
        If lblError Is Nothing Then
            Return
        End If

        lblError.Visible = False
        lblError.Text = String.Empty
        SetPasswordFieldError(False)
    End Sub

    Private Sub SetPasswordFieldError(hasError As Boolean)
        If pnlSecretBorder Is Nothing Then
            Return
        End If

        pnlSecretBorder.BackColor = If(hasError, UiTheme.ColDanger, UiTheme.ColBorder)
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

        ClearLoginError()

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
            UpdateHintWrapWidth()
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

        lblHint.MaximumSize = New Size(LoginCardContentWidth, 0)
    End Sub

    Private Sub ShowLoginError(message As String)
        lblError.Text = message
        lblError.Visible = True
        txtSecret.Clear()
        SetPasswordFieldError(True)
        txtSecret.Focus()
    End Sub

    Private Sub btnOk_Click(sender As Object, e As EventArgs) Handles btnOk.Click
        ClearLoginError()
        AppSession.ClearSession()
        Dim secret As String = txtSecret.Text.Trim()

        Try
            DatabaseInitializer.EnsureDatabase()
        Catch
        End Try

        If radAdmin.Checked Then
            AppSettings.Reload()
            If Not AdminAuth.ValidatePassword(secret) Then
                AuditLogger.LogAudit("LOGIN_FAILED", "Invalid administrator password.", "Admin sign-in attempt")
                ShowLoginError("Invalid administrator password.")
                MessageBox.Show("Invalid administrator password.", "Sign in", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            AppSession.BeginAdminSession()
        Else
            Dim username As String = txtUsername.Text.Trim()
            If username.Length = 0 Then
                MessageBox.Show("Enter your cashier username.", "Sign in", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtUsername.Focus()
                Return
            End If

            If secret.Length = 0 Then
                ShowLoginError("Enter your account password.")
                MessageBox.Show("Enter your account password.", "Sign in", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim auth As CashierAccountService.CashierLoginResult = CashierAccountService.TryAuthenticate(username, secret)
            If Not auth.Success Then
                AuditLogger.LogAudit("LOGIN_FAILED", auth.ErrorMessage, "Cashier: " & username)
                ShowLoginError(auth.ErrorMessage)
                MessageBox.Show(auth.ErrorMessage, "Sign in", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            AppSession.BeginCashierSession(
                auth.CashierId,
                auth.Username,
                If(String.IsNullOrWhiteSpace(auth.DisplayName), auth.Username, auth.DisplayName.Trim()))
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
            .BorderStyle = BorderStyle.None,
            .Dock = DockStyle.Fill,
            .Margin = New Padding(SecretTextPaddingLeft, 0, UiTheme.PadControl, 0),
            .TabStop = True
        }
        UiTheme.ApplyInputStyle(txtSecret)

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
            .BackColor = UiTheme.ColSurface,
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
            .BackColor = UiTheme.ColSurface,
            .MinimumSize = New Size(0, UiTheme.InputHeight),
            .Padding = New Padding(0, 1, 0, 1)
        }
        inner.Controls.Add(secretLayout)

        pnlSecretBorder = New Panel() With {
            .Width = LoginCardContentWidth,
            .Height = SecretFieldHeight,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl),
            .BackColor = UiTheme.ColBorder,
            .Padding = New Padding(1)
        }
        pnlSecretBorder.Controls.Add(inner)

        Dim host As New Panel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .Width = LoginCardContentWidth
        }
        host.Controls.Add(pnlSecretBorder)
        Return host
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
            BackColor = UiTheme.ColSurface
            Size = New Size(SecretToggleWidth, UiTheme.InputHeight)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)

            Dim g As Graphics = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.PixelOffsetMode = PixelOffsetMode.HighQuality

            Dim iconColor As Color = UiTheme.ColTextSecondary
            Dim fillColor As Color = Color.Transparent

            If _pressed Then
                fillColor = Color.FromArgb(56, UiTheme.ColPrimary)
                iconColor = UiTheme.ColPrimaryLight
            ElseIf _hover Then
                fillColor = Color.FromArgb(38, UiTheme.ColPrimary)
                iconColor = UiTheme.ColPrimary
            End If

            Dim bounds As Rectangle = ClientRectangle
            Dim hitSize As Integer = Math.Min(bounds.Width, bounds.Height) - 8
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
