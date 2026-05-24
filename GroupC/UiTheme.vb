Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

''' <summary>
''' Central design tokens and WinForms chrome helpers for Group C UI.
''' </summary>
Public NotInheritable Class UiTheme

    ' ========== SPACING SCALE (8px base grid) ==========
    Public Const SpaceXs As Integer = 4
    Public Const SpaceSm As Integer = 8
    Public Const SpaceMd As Integer = 12
    Public Const SpaceLg As Integer = 16
    Public Const SpaceXl As Integer = 24
    Public Const Space2xl As Integer = 32
    Public Const Space3xl As Integer = 48

    ' ========== BORDER RADIUS SCALE ==========
    Public Const RadiusSm As Integer = 4
    Public Const RadiusMd As Integer = 8
    Public Const RadiusLg As Integer = 12
    Public Const RadiusXl As Integer = 16

    ' ========== COMPONENT HEIGHTS ==========
    Public Const InputHeight As Integer = 32
    Public Const ButtonHeightSm As Integer = 32
    Public Const ButtonHeightMd As Integer = 40
    Public Const ButtonHeightLg As Integer = 48
    Public Const GridRowHeight As Integer = 40
    Public Const GridHeaderHeight As Integer = 44

    ' ========== COLOR PALETTE ==========
    Public Shared ReadOnly FormBackground As Color = ColorFromHex(&HF0F2F5)
    Public Shared ReadOnly PrimaryAccent As Color = ColorFromHex(&H1A237E)
    Public Shared ReadOnly PrimaryAccentHover As Color = ColorFromHex(&H283593)
    Public Shared ReadOnly PrimaryAccentPressed As Color = ColorFromHex(&H121858)
    Public Shared ReadOnly SecondaryAccent As Color = ColorFromHex(&H1565C0)
    Public Shared ReadOnly SecondaryAccentHover As Color = ColorFromHex(&H1976D2)
    Public Shared ReadOnly SecondaryAccentPressed As Color = ColorFromHex(&H0D47A1)
    Public Shared ReadOnly Success As Color = ColorFromHex(&H2E7D32)
    Public Shared ReadOnly SuccessHover As Color = ColorFromHex(&H388E3C)
    Public Shared ReadOnly SuccessPressed As Color = ColorFromHex(&H1B5E20)
    Public Shared ReadOnly Warning As Color = ColorFromHex(&HF57F17)
    Public Shared ReadOnly WarningHover As Color = ColorFromHex(&HFB8C00)
    Public Shared ReadOnly WarningPressed As Color = ColorFromHex(&HE65100)
    Public Shared ReadOnly Danger As Color = ColorFromHex(&HC62828)
    Public Shared ReadOnly DangerHover As Color = ColorFromHex(&HD32F2F)
    Public Shared ReadOnly DangerPressed As Color = ColorFromHex(&HB71C1C)
    Public Shared ReadOnly CardSurface As Color = ColorFromHex(&HFFFFFF)
    Public Shared ReadOnly CardBorder As Color = ColorFromHex(&HE0E0E0)
    Public Shared ReadOnly TextPrimary As Color = ColorFromHex(&H212121)
    Public Shared ReadOnly TextSecondary As Color = ColorFromHex(&H757575)
    Public Shared ReadOnly TextOnAccent As Color = ColorFromHex(&HFFFFFF)
    Public Shared ReadOnly GridHeaderBack As Color = ColorFromHex(&HFAFAFA)
    Public Shared ReadOnly GridAltRow As Color = ColorFromHex(&HF5F7FA)
    Public Shared ReadOnly InactiveRowBack As Color = ColorFromHex(&HF5F5F5)
    Public Shared ReadOnly InactiveRowFore As Color = TextSecondary

    ' Extended palette for modern UI
    Public Shared ReadOnly FocusRing As Color = ColorFromHex(&H1976D2)
    Public Shared ReadOnly DisabledBackground As Color = ColorFromHex(&HE0E0E0)
    Public Shared ReadOnly DisabledText As Color = ColorFromHex(&H9E9E9E)
    Public Shared ReadOnly InputBorder As Color = ColorFromHex(&HBDBDBD)
    Public Shared ReadOnly InputBorderFocus As Color = FocusRing
    Public Shared ReadOnly DividerColor As Color = ColorFromHex(&HEEEEEE)
    Public Shared ReadOnly SurfaceVariant As Color = ColorFromHex(&HF5F5F5)
    Public Shared ReadOnly SuccessLight As Color = ColorFromHex(&HE8F5E9)
    Public Shared ReadOnly WarningLight As Color = ColorFromHex(&HFFF8E1)
    Public Shared ReadOnly DangerLight As Color = ColorFromHex(&HFFEBEE)
    Public Shared ReadOnly InfoBackground As Color = ColorFromHex(&HE3F2FD)
    Public Shared ReadOnly InfoText As Color = ColorFromHex(&H1565C0)

    ''' <summary>
    ''' Same as <see cref="PrimaryAccent"/>; kept for existing call sites.
    ''' </summary>
    Public Shared ReadOnly Navy As Color = PrimaryAccent

    ''' <summary>
    ''' Same as <see cref="PrimaryAccentHover"/>.
    ''' </summary>
    Public Shared ReadOnly NavyHover As Color = PrimaryAccentHover

    ''' <summary>
    ''' Neutral outline button (Cancel, Refresh).
    ''' </summary>
    Public Shared ReadOnly SecondaryBack As Color = CardSurface
    Public Shared ReadOnly SecondaryFore As Color = TextPrimary
    Public Shared ReadOnly SecondaryBorder As Color = CardBorder

    ' ========== TYPOGRAPHY SCALE ==========
    ''' <summary>
    ''' Default UI font applied with <see cref="ApplyStandardWindowChrome"/>.
    ''' </summary>
    Public Shared ReadOnly StandardUiFont As Font = New Font("Segoe UI", 10.0F)
    Public Shared ReadOnly FontHeading1 As Font = New Font("Segoe UI", 20.0F, FontStyle.Bold)
    Public Shared ReadOnly FontHeading2 As Font = New Font("Segoe UI", 16.0F, FontStyle.Bold)
    Public Shared ReadOnly FontHeading3 As Font = New Font("Segoe UI", 13.0F, FontStyle.Bold)
    Public Shared ReadOnly FontBody As Font = New Font("Segoe UI", 10.0F)
    Public Shared ReadOnly FontBodySmall As Font = New Font("Segoe UI", 9.0F)
    Public Shared ReadOnly FontCaption As Font = New Font("Segoe UI", 8.5F)
    Public Shared ReadOnly FontButton As Font = New Font("Segoe UI", 10.0F, FontStyle.Regular)

    ''' <summary>Default corner radius for <see cref="ApplyRoundedButton"/>.</summary>
    Public Const DefaultButtonCornerRadius As Integer = 10

    Private Sub New()
    End Sub

    Private Shared Function ColorFromHex(rgb As Integer) As Color
        Return Color.FromArgb(&HFF, (rgb >> 16) And &HFF, (rgb >> 8) And &HFF, rgb And &HFF)
    End Function

    Public Shared Sub ApplyFormSurface(form As Form)
        form.BackColor = FormBackground
    End Sub

    ''' <summary>
    ''' Applies shared background and default font to a top-level window without changing layout.
    ''' </summary>
    ''' <param name="form">Form to theme.</param>
    Public Shared Sub ApplyStandardWindowChrome(form As Form)
        ApplyFormSurface(form)
        form.Font = StandardUiFont
        AppIcons.ApplyToForm(form)
    End Sub

    ''' <summary>
    ''' Applies shared grid surface, header, alternating row, and selection colors.
    ''' </summary>
    ''' <param name="dgv">Grid to theme (read-only or editable).</param>
    Public Shared Sub ApplyDataGridViewChrome(dgv As DataGridView)
        ApplyReadOnlyGridTheme(dgv)
    End Sub

    Public Shared Sub ApplyStatusStripTheme(strip As StatusStrip)
        strip.BackColor = CardSurface
        strip.ForeColor = TextSecondary
        strip.RenderMode = ToolStripRenderMode.System
        For Each item As ToolStripItem In strip.Items
            item.ForeColor = TextSecondary
        Next
    End Sub

    ''' <summary>
    ''' Creates a heading label with appropriate size and weight.
    ''' </summary>
    ''' <param name="text">Heading text.</param>
    ''' <param name="level">1 = largest (H1), 2 = H2, 3 = H3.</param>
    Public Shared Function CreateHeadingLabel(text As String, Optional level As Integer = 2) As Label
        Dim lbl As New Label()
        lbl.Text = text
        lbl.AutoSize = True
        lbl.ForeColor = TextPrimary
        lbl.Margin = New Padding(0, 0, 0, SpaceMd)

        Select Case level
            Case 1
                lbl.Font = FontHeading1
            Case 2
                lbl.Font = FontHeading2
            Case 3
                lbl.Font = FontHeading3
            Case Else
                lbl.Font = FontHeading2
        End Select

        Return lbl
    End Function

    ''' <summary>
    ''' Creates a horizontal divider line for section separation.
    ''' </summary>
    Public Shared Function CreateDivider() As Panel
        Dim divider As New Panel()
        divider.Height = 1
        divider.Dock = DockStyle.Top
        divider.BackColor = DividerColor
        divider.Margin = New Padding(0, SpaceLg, 0, SpaceLg)
        Return divider
    End Function

    ''' <summary>
    ''' Creates a centered empty state label for grids with no data.
    ''' </summary>
    Public Shared Function CreateEmptyStateLabel(text As String) As Label
        Dim lbl As New Label()
        lbl.Text = text
        lbl.AutoSize = False
        lbl.Dock = DockStyle.Fill
        lbl.TextAlign = ContentAlignment.MiddleCenter
        lbl.ForeColor = TextSecondary
        lbl.Font = FontBody
        lbl.BackColor = CardSurface
        Return lbl
    End Function

    ''' <summary>
    ''' Creates a FlowLayoutPanel for action buttons with consistent spacing.
    ''' </summary>
    ''' <param name="alignment">Left, Right, or Center alignment.</param>
    Public Shared Function CreateButtonRow(Optional alignment As FlowDirection = FlowDirection.RightToLeft) As FlowLayoutPanel
        Dim flow As New FlowLayoutPanel()
        flow.FlowDirection = alignment
        flow.AutoSize = True
        flow.AutoSizeMode = AutoSizeMode.GrowAndShrink
        flow.Dock = DockStyle.Bottom
        flow.Padding = New Padding(0)
        flow.Margin = New Padding(0, SpaceLg, 0, 0)
        flow.WrapContents = False
        Return flow
    End Function

    ''' <summary>
    ''' Creates a titled section with card styling.
    ''' </summary>
    ''' <param name="title">Section title.</param>
    Public Shared Function CreateFormSection(title As String) As Panel
        Dim card As Panel = CreateCardPanel(New Padding(SpaceLg))
        Dim content As Panel = GetCardContentHost(card)

        If content IsNot Nothing AndAlso Not String.IsNullOrEmpty(title) Then
            Dim heading As Label = CreateHeadingLabel(title, 3)
            heading.Dock = DockStyle.Top
            content.Controls.Add(heading)
        End If

        Return card
    End Function

    ''' <summary>
    ''' Applies modern input field styling with focus indicator support.
    ''' </summary>
    Public Shared Sub ApplyInputFieldStyle(textBox As TextBox)
        If textBox Is Nothing Then
            Return
        End If

        textBox.BorderStyle = BorderStyle.FixedSingle
        textBox.BackColor = CardSurface
        textBox.ForeColor = TextPrimary
        textBox.Font = FontBody

        ' Focus ring handlers
        AddHandler textBox.Enter, Sub(s, e)
                                       textBox.BackColor = CardSurface
                                   End Sub
        AddHandler textBox.Leave, Sub(s, e)
                                      textBox.BackColor = CardSurface
                                  End Sub
    End Sub

    ''' <summary>
    ''' Applies consistent styling to ComboBox controls.
    ''' </summary>
    Public Shared Sub ApplyComboBoxStyle(combo As ComboBox)
        If combo Is Nothing Then
            Return
        End If

        combo.FlatStyle = FlatStyle.Flat
        combo.BackColor = CardSurface
        combo.ForeColor = TextPrimary
        combo.Font = FontBody
    End Sub

    ''' <summary>
    ''' Applies modern GroupBox styling with proper spacing.
    ''' </summary>
    Public Shared Sub ApplyGroupBoxStyle(groupBox As GroupBox)
        If groupBox Is Nothing Then
            Return
        End If

        groupBox.ForeColor = TextPrimary
        groupBox.Font = FontHeading3
        groupBox.Padding = New Padding(SpaceLg)
    End Sub

    ''' <summary>
    ''' Bordered white card: outer 1 px border color, inner content surface.
    ''' </summary>
    Public Shared Function CreateCardPanel(Optional innerPadding As Padding = Nothing) As Panel
        If innerPadding = Padding.Empty Then
            innerPadding = New Padding(SpaceLg)
        End If

        Dim outer As New Panel()
        outer.BackColor = CardBorder
        outer.Padding = New Padding(1)

        Dim inner As New Panel()
        inner.Dock = DockStyle.Fill
        inner.BackColor = CardSurface
        inner.Padding = innerPadding
        outer.Controls.Add(inner)
        Return outer
    End Function

    ''' <summary>
    ''' Returns the inner content panel of a card created with <see cref="CreateCardPanel"/>.
    ''' </summary>
    Public Shared Function GetCardContentHost(card As Panel) As Panel
        If card Is Nothing OrElse card.Controls.Count = 0 Then
            Return Nothing
        End If

        Return TryCast(card.Controls(0), Panel)
    End Function

    ''' <summary>
    ''' Adds controls to a card's inner host, or to <paramref name="card"/> when no host exists.
    ''' </summary>
    Public Shared Function PopulateCardContent(card As Panel, ParamArray contents() As Control) As Panel
        If card Is Nothing Then
            Return Nothing
        End If

        Dim host As Panel = GetCardContentHost(card)
        If host Is Nothing Then
            host = card
        End If

        If contents IsNot Nothing Then
            For Each item As Control In contents
                If item IsNot Nothing Then
                    host.Controls.Add(item)
                End If
            Next
        End If

        Return host
    End Function

    ''' <summary>
    ''' Shared sizing and window state for primary workspace forms shown maximized.
    ''' </summary>
    Public Shared Sub ApplyMaximizedWorkspaceDefaults(form As Form, Optional minWidth As Integer = 1024, Optional minHeight As Integer = 720)
        form.FormBorderStyle = FormBorderStyle.Sizable
        form.WindowState = FormWindowState.Maximized
        form.StartPosition = FormStartPosition.CenterScreen
        form.MinimumSize = New Size(minWidth, minHeight)
    End Sub

    ''' <summary>
    ''' Consistent caption styling for filters and form fields.
    ''' </summary>
    Public Shared Function CreateSecondaryLabel(text As String) As Label
        Return New Label() With {
            .Text = text,
            .AutoSize = True,
            .ForeColor = TextSecondary,
            .Margin = New Padding(0, SpaceSm, SpaceSm, SpaceSm),
            .Font = FontBody
        }
    End Function

    Public Shared Sub ApplyReadOnlyGridTheme(dgv As DataGridView)
        dgv.BackgroundColor = CardSurface
        dgv.BorderStyle = BorderStyle.None
        dgv.EnableHeadersVisualStyles = False
        dgv.GridColor = CardBorder
        dgv.DefaultCellStyle.BackColor = CardSurface
        dgv.DefaultCellStyle.ForeColor = TextPrimary
        dgv.DefaultCellStyle.Font = FontBody
        dgv.DefaultCellStyle.Padding = New Padding(SpaceSm)
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 235, 245)
        dgv.DefaultCellStyle.SelectionForeColor = TextPrimary
        dgv.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderBack
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary
        dgv.ColumnHeadersDefaultCellStyle.Font = FontHeading3
        dgv.ColumnHeadersDefaultCellStyle.Padding = New Padding(SpaceSm)
        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = GridHeaderBack
        dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextPrimary
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
        dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single
        dgv.ColumnHeadersHeight = GridHeaderHeight
        dgv.RowHeadersVisible = False
        dgv.RowTemplate.Height = GridRowHeight
        dgv.AlternatingRowsDefaultCellStyle.BackColor = GridAltRow
        dgv.AlternatingRowsDefaultCellStyle.ForeColor = TextPrimary
        dgv.AllowUserToAddRows = False
        dgv.AllowUserToDeleteRows = False
        dgv.ReadOnly = True
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgv.MultiSelect = False
    End Sub

    Public Shared Sub ApplyPrimaryButton(button As Button)
        StyleFlatButton(button, PrimaryAccent, PrimaryAccentHover, PrimaryAccentPressed, TextOnAccent, Nothing)
    End Sub

    Public Shared Sub ApplySecondaryButton(button As Button)
        button.Cursor = Cursors.Hand
        button.UseCompatibleTextRendering = False
        button.Padding = New Padding(SpaceLg, SpaceSm, SpaceLg, SpaceSm)
        button.TextAlign = ContentAlignment.MiddleCenter
        button.Font = FontButton
        button.MinimumSize = New Size(0, ButtonHeightMd)
        WireRoundedButtonPaint(button, SecondaryBack, GridAltRow, Color.FromArgb(230, 232, 236), SecondaryFore, SecondaryBorder)
    End Sub

    Public Shared Sub ApplySecondaryAccentButton(button As Button)
        StyleFlatButton(button, SecondaryAccent, SecondaryAccentHover, SecondaryAccentPressed, TextOnAccent, Nothing)
    End Sub

    Public Shared Sub ApplySuccessButton(button As Button)
        StyleFlatButton(button, Success, SuccessHover, SuccessPressed, TextOnAccent, Nothing)
    End Sub

    Public Shared Sub ApplyWarningButton(button As Button)
        StyleFlatButton(button, Warning, WarningHover, WarningPressed, TextOnAccent, Nothing)
    End Sub

    Public Shared Sub ApplyDangerButton(button As Button)
        StyleFlatButton(button, Danger, DangerHover, DangerPressed, TextOnAccent, Nothing)
    End Sub

    Private Shared Sub StyleFlatButton(
        button As Button,
        normal As Color,
        hover As Color,
        pressed As Color,
        fore As Color,
        border As Nullable(Of Color))

        button.BackColor = normal
        button.ForeColor = fore
        button.FlatStyle = FlatStyle.Flat
        button.FlatAppearance.BorderSize = If(border.HasValue, 1, 0)
        If border.HasValue Then
            button.FlatAppearance.BorderColor = border.Value
        End If

        button.Cursor = Cursors.Hand
        button.UseCompatibleTextRendering = False
        button.Padding = New Padding(SpaceLg, SpaceSm, SpaceLg, SpaceSm)
        button.TextAlign = ContentAlignment.MiddleCenter
        button.Font = FontButton
        button.MinimumSize = New Size(0, ButtonHeightMd)
        WireRoundedButtonPaint(button, normal, hover, pressed, fore, border)
    End Sub

    ''' <summary>
    ''' Rounded corners via custom paint (does not clip label text like <see cref="Control.Region"/>).
    ''' </summary>
    Public Shared Sub ApplyRoundedButton(button As Button, Optional cornerRadius As Integer = DefaultButtonCornerRadius)
        If button Is Nothing Then
            Return
        End If

        WireRoundedButtonPaint(
            button,
            button.BackColor,
            button.FlatAppearance.MouseOverBackColor,
            button.FlatAppearance.MouseDownBackColor,
            button.ForeColor,
            Nothing,
            cornerRadius)
    End Sub

    Private Shared Sub WireRoundedButtonPaint(
        button As Button,
        normal As Color,
        hover As Color,
        pressed As Color,
        fore As Color,
        border As Nullable(Of Color),
        Optional cornerRadius As Integer = DefaultButtonCornerRadius)

        If button Is Nothing Then
            Return
        End If

        Dim state As RoundedButtonState = GetOrCreateButtonState(button)
        state.CornerRadius = Math.Max(4, cornerRadius)
        state.NormalBack = normal
        state.HoverBack = hover
        state.PressedBack = pressed
        state.ForeColor = fore
        state.BorderColor = border

        button.FlatStyle = FlatStyle.Flat
        button.FlatAppearance.BorderSize = 0
        button.Region = Nothing
        button.BackColor = normal
        button.ForeColor = fore

        If Not state.PaintWired Then
            state.PaintWired = True
            AddHandler button.Paint, AddressOf RoundedButton_Paint
            AddHandler button.MouseEnter, AddressOf RoundedButton_Invalidate
            AddHandler button.MouseLeave, AddressOf RoundedButton_Invalidate
            AddHandler button.MouseDown, AddressOf RoundedButton_Invalidate
            AddHandler button.MouseUp, AddressOf RoundedButton_Invalidate
            AddHandler button.EnabledChanged, AddressOf RoundedButton_Invalidate
            AddHandler button.Resize, AddressOf RoundedButton_Invalidate
            AddHandler button.TextChanged, AddressOf RoundedButton_Invalidate
        End If

        EnsureButtonFitsText(button)
        button.Invalidate()
    End Sub

    Private Shared Function GetOrCreateButtonState(button As Button) As RoundedButtonState
        Dim state As RoundedButtonState = TryCast(button.Tag, RoundedButtonState)
        If state Is Nothing Then
            state = New RoundedButtonState()
            button.Tag = state
        End If

        Return state
    End Function

    Private Shared Sub RoundedButton_Invalidate(sender As Object, e As EventArgs)
        Dim button As Button = TryCast(sender, Button)
        If button IsNot Nothing AndAlso Not button.IsDisposed Then
            button.Invalidate()
        End If
    End Sub

    Private Shared Sub RoundedButton_Paint(sender As Object, e As PaintEventArgs)
        Dim button As Button = TryCast(sender, Button)
        Dim state As RoundedButtonState = If(button Is Nothing, Nothing, TryCast(button.Tag, RoundedButtonState))
        If button Is Nothing OrElse state Is Nothing Then
            Return
        End If

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
        Dim clearColor As Color = CardSurface
        If button.Parent IsNot Nothing AndAlso button.Parent.BackColor <> Color.Transparent Then
            clearColor = button.Parent.BackColor
        End If

        e.Graphics.Clear(clearColor)

        Dim back As Color = state.NormalBack
        If button.Enabled Then
            Dim pt As Point = button.PointToClient(Cursor.Position)
            If button.ClientRectangle.Contains(pt) Then
                If (Control.MouseButtons And MouseButtons.Left) = MouseButtons.Left Then
                    back = state.PressedBack
                Else
                    back = state.HoverBack
                End If
            End If
        Else
            back = Color.FromArgb(200, state.NormalBack)
        End If

        Dim rect As New Rectangle(0, 0, button.Width - 1, button.Height - 1)
        Using path As GraphicsPath = CreateRoundedRectPath(rect, state.CornerRadius)
            Using fillBrush As New SolidBrush(back)
                e.Graphics.FillPath(fillBrush, path)
            End Using

            If state.BorderColor.HasValue Then
                Using borderPen As New Pen(state.BorderColor.Value, 1.0F)
                    e.Graphics.DrawPath(borderPen, path)
                End Using
            End If
        End Using

        Dim textFlags As TextFormatFlags =
            TextFormatFlags.HorizontalCenter Or
            TextFormatFlags.VerticalCenter Or
            TextFormatFlags.SingleLine Or
            TextFormatFlags.EndEllipsis

        Dim textColor As Color = If(button.Enabled, state.ForeColor, TextSecondary)
        TextRenderer.DrawText(e.Graphics, button.Text, button.Font, rect, textColor, textFlags)
    End Sub

    Private Shared Function CreateRoundedRectPath(rect As Rectangle, cornerRadius As Integer) As GraphicsPath
        Dim path As New GraphicsPath()
        If rect.Width < 2 OrElse rect.Height < 2 Then
            path.AddRectangle(rect)
            Return path
        End If

        Dim radius As Integer = Math.Min(cornerRadius, Math.Min(rect.Width, rect.Height) \ 2)
        Dim d As Integer = radius * 2
        path.AddArc(rect.X, rect.Y, d, d, 180, 90)
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90)
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90)
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90)
        path.CloseFigure()
        Return path
    End Function

    Private Shared Sub EnsureButtonFitsText(button As Button)
        If button Is Nothing OrElse String.IsNullOrEmpty(button.Text) Then
            Return
        End If

        Dim flags As TextFormatFlags =
            TextFormatFlags.HorizontalCenter Or
            TextFormatFlags.VerticalCenter Or
            TextFormatFlags.SingleLine

        Dim textSize As Size = TextRenderer.MeasureText(button.Text, button.Font, New Size(Integer.MaxValue, Integer.MaxValue), flags)
        Dim minH As Integer = textSize.Height + button.Padding.Vertical + 8
        Dim minW As Integer = textSize.Width + button.Padding.Horizontal + 12

        If minH < 36 Then
            minH = 36
        End If

        If button.Height < minH Then
            button.Height = minH
        End If

        If button.MinimumSize.Height < minH OrElse button.MinimumSize.Width < minW Then
            button.MinimumSize = New Size(Math.Max(button.MinimumSize.Width, minW), Math.Max(button.MinimumSize.Height, minH))
        End If
    End Sub

    Private NotInheritable Class RoundedButtonState
        Public Property CornerRadius As Integer = DefaultButtonCornerRadius
        Public Property NormalBack As Color
        Public Property HoverBack As Color
        Public Property PressedBack As Color
        Public Property ForeColor As Color
        Public Property BorderColor As Nullable(Of Color)
        Public Property PaintWired As Boolean
    End Class

    Public Shared Sub ApplyProfessionalGraphics(targetForm As Form)
        ' 1. Apply smooth background color
        targetForm.BackColor = FormBackground

        ' 2. Scan all controls and style them
        StyleControlsRecursively(targetForm.Controls)
    End Sub

    Private Shared Sub StyleControlsRecursively(controls As Control.ControlCollection)
        For Each ctrl As Control In controls
            ' --- STYLE BUTTONS ---
            If TypeOf ctrl Is Button Then
                Dim btn As Button = DirectCast(ctrl, Button)
                btn.Cursor = Cursors.Hand
                btn.Font = New Font("Segoe UI", 10, FontStyle.Regular)

                ' Intelligently color buttons based on their names!
                Dim name As String = btn.Name.ToLower()
                If name.Contains("delete") Or name.Contains("remove") Or name.Contains("cancel") Then
                    ApplyDangerButton(btn)
                ElseIf name.Contains("save") Or name.Contains("add") Or name.Contains("checkout") Or name.Contains("ok") Then
                    ApplySuccessButton(btn)
                ElseIf name.Contains("print") Or name.Contains("report") Then
                    ApplySecondaryAccentButton(btn)
                Else
                    ApplyPrimaryButton(btn)
                End If

                ' --- STYLE TABLES (DataGridView) ---
            ElseIf TypeOf ctrl Is DataGridView Then
                Dim grid As DataGridView = DirectCast(ctrl, DataGridView)
                grid.BackgroundColor = Color.White
                grid.BorderStyle = BorderStyle.None
                grid.EnableHeadersVisualStyles = False ' Required to custom-color headers
                grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single

                ' Modern Header Design
                grid.ColumnHeadersDefaultCellStyle.BackColor = PrimaryAccent
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
                grid.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 10, FontStyle.Bold)
                grid.ColumnHeadersHeight = 40

                ' Modern Row Design
                grid.DefaultCellStyle.SelectionBackColor = SecondaryAccent
                grid.DefaultCellStyle.Font = New Font("Segoe UI", 9.5!)
                grid.RowTemplate.Height = 35
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
                grid.ReadOnly = True
                grid.AllowUserToAddRows = False

                ' --- STYLE INPUTS ---
            ElseIf TypeOf ctrl Is TextBox Or TypeOf ctrl Is ComboBox Or TypeOf ctrl Is NumericUpDown Then
                ctrl.Font = New Font("Segoe UI", 10)

                ' --- STYLE LABELS ---
            ElseIf TypeOf ctrl Is Label Then
                Dim lbl As Label = DirectCast(ctrl, Label)
                lbl.Font = New Font("Segoe UI", 9.5!)
            End If

            ' Recursively search inside Panels, GroupBoxes, and TableLayoutPanels
            If ctrl.HasChildren Then
                StyleControlsRecursively(ctrl.Controls)
            End If
        Next
    End Sub

    ''' <summary>
    ''' Drop-down combos in <see cref="TableLayoutPanel"/> must not use <see cref="DockStyle.Fill"/> vertically — stretched height makes WinForms paint them like an always-open list.
    ''' </summary>
    ''' <param name="combo">Configured combo (typically <see cref="ComboBoxStyle.DropDownList"/>).</param>
    Public Shared Sub ApplyTableLayoutDropDown(combo As ComboBox)
        If combo Is Nothing Then
            Return
        End If

        combo.IntegralHeight = False
        combo.Dock = DockStyle.None
        combo.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top
        Dim h As Integer = combo.PreferredHeight
        If h < 26 Then
            h = 28
        End If

        combo.MinimumSize = New Size(0, h)
        combo.MaximumSize = New Size(0, h)
    End Sub

    ''' <summary>
    ''' Single-line text boxes in table layouts should match combo fix — avoid vertical stretch in AutoSize rows.
    ''' </summary>
    ''' <param name="textBox">Text box to constrain.</param>
    Public Shared Sub ApplyTableLayoutSingleLineTextBox(textBox As TextBox)
        If textBox Is Nothing Then
            Return
        End If

        textBox.Dock = DockStyle.None
        textBox.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top
        Dim h As Integer = textBox.PreferredHeight
        If h < 22 Then
            h = 24
        End If

        textBox.MinimumSize = New Size(0, h)
        textBox.MaximumSize = New Size(0, h)
        ApplyFilledTextInputVisual(textBox)
    End Sub

    ''' <summary>
    ''' Single-line field surface colors (does not change layout).
    ''' </summary>
    Public Shared Sub ApplyFilledTextInputVisual(textBox As TextBox)
        If textBox Is Nothing Then
            Return
        End If

        textBox.BorderStyle = BorderStyle.FixedSingle
        textBox.BackColor = CardSurface
        textBox.ForeColor = TextPrimary
    End Sub

End Class
