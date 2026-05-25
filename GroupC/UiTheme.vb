Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

''' <summary>
''' Central design tokens and WinForms chrome helpers for Group C UI.
''' Canonical names use Col*, Font*, and Pad* prefixes; legacy aliases remain for existing call sites.
''' </summary>
Public NotInheritable Class UiTheme

    ' ========== SPACING (canonical) ==========
    Public Const PadPage As Integer = 24
    Public Const PadSection As Integer = 20
    Public Const PadCard As Integer = 16
    Public Const PadControl As Integer = 8
    Public Const PadTight As Integer = 4
    Public Const SidebarWidth As Integer = 220

    ' Legacy spacing aliases (8px grid)
    Public Const SpaceXs As Integer = PadTight
    Public Const SpaceSm As Integer = PadControl
    Public Const SpaceMd As Integer = 12
    Public Const SpaceLg As Integer = PadCard
    Public Const SpaceXl As Integer = PadPage
    Public Const Space2xl As Integer = 32
    Public Const Space3xl As Integer = 48

    ' ========== BORDER RADIUS SCALE ==========
    Public Const RadiusSm As Integer = 4
    Public Const RadiusMd As Integer = 8
    Public Const RadiusLg As Integer = 12
    Public Const RadiusXl As Integer = 16

    ' ========== COMPONENT HEIGHTS ==========
    Public Const InputHeight As Integer = 30
    Public Const ButtonHeight As Integer = 34
    Public Const ButtonHeightSm As Integer = ButtonHeight
    Public Const ButtonHeightMd As Integer = ButtonHeight
    Public Const ButtonHeightLg As Integer = 44
    Public Const GridRowHeight As Integer = 32
    Public Const GridHeaderHeight As Integer = 38

    ' ========== COLOR PALETTE (canonical) ==========
    Public Shared ReadOnly ColPrimary As Color = ColorFromHex(&H1E3A6E)
    Public Shared ReadOnly ColPrimaryLight As Color = ColorFromHex(&H2B52A0)
    Public Shared ReadOnly ColPrimaryMuted As Color = ColorFromHex(&HE8EDF5)

    Public Shared ReadOnly ColAccent As Color = ColorFromHex(&H2E7D32)
    Public Shared ReadOnly ColAccentLight As Color = ColorFromHex(&H388E3C)
    Public Shared ReadOnly ColAccentMuted As Color = ColorFromHex(&HE8F5E9)

    Public Shared ReadOnly ColDanger As Color = ColorFromHex(&HC62828)
    Public Shared ReadOnly ColDangerLight As Color = ColorFromHex(&HD32F2F)
    Public Shared ReadOnly ColDangerMuted As Color = ColorFromHex(&HFFEBEE)

    Public Shared ReadOnly ColWarning As Color = ColorFromHex(&HE65100)
    Public Shared ReadOnly ColWarningMuted As Color = ColorFromHex(&HFFF3E0)

    Public Shared ReadOnly ColBackground As Color = ColorFromHex(&HF4F6F9)
    Public Shared ReadOnly ColSurface As Color = ColorFromHex(&HFFFFFF)
    Public Shared ReadOnly ColSurfaceAlt As Color = ColorFromHex(&HF9FAFB)
    Public Shared ReadOnly ColBorder As Color = ColorFromHex(&HDDE1E9)
    Public Shared ReadOnly ColBorderFocus As Color = ColorFromHex(&H1E3A6E)
    Public Shared ReadOnly ColShadow As Color = Color.FromArgb(&H14, 0, 0, 0)

    Public Shared ReadOnly ColTextPrimary As Color = ColorFromHex(&H1A1A2E)
    Public Shared ReadOnly ColTextSecondary As Color = ColorFromHex(&H5C6478)
    Public Shared ReadOnly ColTextDisabled As Color = ColorFromHex(&HA0A8B8)
    Public Shared ReadOnly ColTextOnDark As Color = ColorFromHex(&HFFFFFF)
    Public Shared ReadOnly ColTextLink As Color = ColorFromHex(&H2B52A0)

    ' ========== LEGACY COLOR ALIASES ==========
    Public Shared ReadOnly FormBackground As Color = ColBackground
    Public Shared ReadOnly PrimaryAccent As Color = ColPrimary
    Public Shared ReadOnly PrimaryAccentHover As Color = ColPrimaryLight
    Public Shared ReadOnly PrimaryAccentPressed As Color = ColorFromHex(&H152A52)
    Public Shared ReadOnly SecondaryAccent As Color = ColTextLink
    Public Shared ReadOnly SecondaryAccentHover As Color = ColPrimaryLight
    Public Shared ReadOnly SecondaryAccentPressed As Color = ColorFromHex(&H1E3A6E)
    Public Shared ReadOnly Success As Color = ColAccent
    Public Shared ReadOnly SuccessHover As Color = ColAccentLight
    Public Shared ReadOnly SuccessPressed As Color = ColorFromHex(&H1B5E20)
    Public Shared ReadOnly Warning As Color = ColWarning
    Public Shared ReadOnly WarningHover As Color = ColorFromHex(&HF57C00)
    Public Shared ReadOnly WarningPressed As Color = ColorFromHex(&HBF360C)
    Public Shared ReadOnly Danger As Color = ColDanger
    Public Shared ReadOnly DangerHover As Color = ColDangerLight
    Public Shared ReadOnly DangerPressed As Color = ColorFromHex(&HB71C1C)
    Public Shared ReadOnly CardSurface As Color = ColSurface
    Public Shared ReadOnly CardBorder As Color = ColBorder
    Public Shared ReadOnly TextPrimary As Color = ColTextPrimary
    Public Shared ReadOnly TextSecondary As Color = ColTextSecondary
    Public Shared ReadOnly TextOnAccent As Color = ColTextOnDark
    Public Shared ReadOnly GridHeaderBack As Color = ColBackground
    Public Shared ReadOnly GridAltRow As Color = ColSurfaceAlt
    Public Shared ReadOnly InactiveRowBack As Color = ColSurfaceAlt
    Public Shared ReadOnly InactiveRowFore As Color = ColTextSecondary
    Public Shared ReadOnly FocusRing As Color = ColBorderFocus
    Public Shared ReadOnly DisabledBackground As Color = ColBackground
    Public Shared ReadOnly DisabledText As Color = ColTextDisabled
    Public Shared ReadOnly InputBorder As Color = ColBorder
    Public Shared ReadOnly InputBorderFocus As Color = ColBorderFocus
    Public Shared ReadOnly DividerColor As Color = ColBorder
    Public Shared ReadOnly SurfaceVariant As Color = ColSurfaceAlt
    Public Shared ReadOnly SuccessLight As Color = ColAccentMuted
    Public Shared ReadOnly WarningLight As Color = ColWarningMuted
    Public Shared ReadOnly DangerLight As Color = ColDangerMuted
    Public Shared ReadOnly InfoBackground As Color = ColPrimaryMuted
    Public Shared ReadOnly InfoText As Color = ColTextLink
    Public Shared ReadOnly Navy As Color = ColPrimary
    Public Shared ReadOnly NavyHover As Color = ColPrimaryLight
    Public Shared ReadOnly SecondaryBack As Color = ColSurface
    Public Shared ReadOnly SecondaryFore As Color = ColPrimary
    Public Shared ReadOnly SecondaryBorder As Color = ColBorder

    ' ========== TYPOGRAPHY (canonical) ==========
    Public Shared ReadOnly FontDisplay As Font = New Font("Segoe UI", 18.0F, FontStyle.Bold)
    Public Shared ReadOnly FontHeading As Font = New Font("Segoe UI", 13.0F, FontStyle.Bold)
    Public Shared ReadOnly FontSubheading As Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
    Public Shared ReadOnly FontBody As Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)
    Public Shared ReadOnly FontBodyBold As Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
    Public Shared ReadOnly FontCaption As Font = New Font("Segoe UI", 8.0F, FontStyle.Regular)
    Public Shared ReadOnly FontMono As Font = New Font("Courier New", 9.0F, FontStyle.Regular)

    ' Legacy typography aliases
    Public Shared ReadOnly StandardUiFont As Font = FontBody
    Public Shared ReadOnly FontHeading1 As Font = FontDisplay
    Public Shared ReadOnly FontHeading2 As Font = FontHeading
    Public Shared ReadOnly FontHeading3 As Font = FontSubheading
    Public Shared ReadOnly FontBodySmall As Font = FontBody
    Public Shared ReadOnly FontButton As Font = FontBody

    Public Const DefaultButtonCornerRadius As Integer = 10
    Private Const InputWrapTagKey As String = "UiTheme.InputWrap"
    Private Const SidebarActiveTagKey As String = "UiTheme.SidebarActive"

    Private Sub New()
    End Sub

    Private Shared Function ColorFromHex(rgb As Integer) As Color
        Return Color.FromArgb(&HFF, (rgb >> 16) And &HFF, (rgb >> 8) And &HFF, rgb And &HFF)
    End Function

    Private Shared Function BlendWithWhite(color As Color, amount As Single) As Color
        Dim t As Single = Math.Max(0.0F, Math.Min(1.0F, amount))
        Dim r As Integer = CInt(color.R * t + 255.0F * (1.0F - t))
        Dim g As Integer = CInt(color.G * t + 255.0F * (1.0F - t))
        Dim b As Integer = CInt(color.B * t + 255.0F * (1.0F - t))
        Return Color.FromArgb(255, r, g, b)
    End Function

    ' ========== WINDOW CHROME ==========

    Public Shared Sub ApplyFormSurface(form As Form)
        form.BackColor = ColBackground
    End Sub

    Public Shared Sub ApplyStandardWindowChrome(form As Form)
        ApplyFormSurface(form)
        form.Font = FontBody
        AppIcons.ApplyToForm(form)
    End Sub

    Public Shared Sub ApplyMaximizedWorkspaceDefaults(form As Form, Optional minWidth As Integer = 1024, Optional minHeight As Integer = 720)
        form.FormBorderStyle = FormBorderStyle.Sizable
        form.WindowState = FormWindowState.Maximized
        form.StartPosition = FormStartPosition.CenterScreen
        form.MinimumSize = New Size(minWidth, minHeight)
    End Sub

    Public Shared Sub ApplyStatusStripTheme(strip As StatusStrip)
        strip.BackColor = ColSurface
        strip.ForeColor = ColTextSecondary
        strip.RenderMode = ToolStripRenderMode.System
        For Each item As ToolStripItem In strip.Items
            item.ForeColor = ColTextSecondary
        Next
    End Sub

    ' ========== LAYOUT SHELL HELPERS ==========

    ''' <summary>Shared left navigation panel (220px, ColPrimary).</summary>
    Public Shared Function BuildSidebar() As Panel
        Dim sidebar As New Panel() With {
            .Width = SidebarWidth,
            .Dock = DockStyle.Left,
            .BackColor = ColPrimary,
            .Padding = New Padding(0)
        }
        Return sidebar
    End Function

    ''' <summary>Transparent nav button for sidebars.</summary>
    Public Shared Function CreateSidebarNavButton(text As String) As Button
        Dim btn As New Button() With {
            .Text = text,
            .Dock = DockStyle.Top,
            .Height = 44,
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.Transparent,
            .ForeColor = ColTextOnDark,
            .Font = FontBody,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(PadCard, 0, PadControl, 0),
            .Cursor = Cursors.Hand,
            .UseCompatibleTextRendering = False
        }
        btn.FlatAppearance.BorderSize = 0
        btn.FlatAppearance.MouseOverBackColor = ColPrimaryLight
        AddHandler btn.MouseEnter, Sub(s, e)
                                       If Not Object.Equals(btn.Tag, SidebarActiveTagKey) Then
                                           btn.BackColor = ColPrimaryLight
                                       End If
                                   End Sub
        AddHandler btn.MouseLeave, Sub(s, e)
                                       If Not Object.Equals(btn.Tag, SidebarActiveTagKey) Then
                                           btn.BackColor = Color.Transparent
                                       End If
                                   End Sub
        Return btn
    End Function

    ''' <summary>Marks a sidebar button as the active screen (3px ColAccent left border).</summary>
    Public Shared Sub SetSidebarButtonActive(btn As Button, active As Boolean)
        If btn Is Nothing Then
            Return
        End If

        RemoveHandler btn.Paint, AddressOf SidebarActiveButton_Paint

        If active Then
            btn.Tag = SidebarActiveTagKey
            btn.BackColor = ColPrimaryLight
            btn.Padding = New Padding(PadCard - 3, 0, PadControl, 0)
            AddHandler btn.Paint, AddressOf SidebarActiveButton_Paint
        Else
            btn.Tag = Nothing
            btn.BackColor = Color.Transparent
            btn.Padding = New Padding(PadCard, 0, PadControl, 0)
        End If

        btn.Invalidate()
    End Sub

    Private Shared Sub SidebarActiveButton_Paint(sender As Object, e As PaintEventArgs)
        Dim btn As Button = TryCast(sender, Button)
        If btn Is Nothing OrElse Not Object.Equals(btn.Tag, SidebarActiveTagKey) Then
            Return
        End If

        Using accentBrush As New SolidBrush(ColAccent)
            e.Graphics.FillRectangle(accentBrush, 0, 0, 3, btn.Height)
        End Using
    End Sub

    ''' <summary>Semi-transparent white separator for sidebar sections.</summary>
    Public Shared Function CreateSidebarSeparator() As Panel
        Return New Panel() With {
            .Height = 1,
            .Dock = DockStyle.Top,
            .BackColor = Color.FromArgb(40, 255, 255, 255),
            .Margin = New Padding(PadCard, PadControl, PadCard, PadControl)
        }
    End Function

    Public Shared Function CreateSidebarSpacer() As Panel
        Return New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.Transparent
        }
    End Function

    ''' <summary>Top bar: 60px, ColSurface, bottom ColBorder border.</summary>
    Public Shared Function CreateTopBar(pageTitle As String, Optional subtitle As String = Nothing) As Panel
        Dim bar As New Panel() With {
            .Height = 60,
            .Dock = DockStyle.Top,
            .BackColor = ColSurface,
            .Padding = New Padding(PadPage, PadControl, PadPage, PadControl)
        }

        Dim title As New Label() With {
            .Text = pageTitle,
            .Font = FontDisplay,
            .ForeColor = ColTextPrimary,
            .AutoSize = True,
            .Location = New Point(PadPage, PadControl)
        }
        bar.Controls.Add(title)

        If Not String.IsNullOrEmpty(subtitle) Then
            Dim subLbl As New Label() With {
                .Text = subtitle,
                .Font = FontCaption,
                .ForeColor = ColTextSecondary,
                .AutoSize = True,
                .Location = New Point(PadPage, title.Bottom + PadTight)
            }
            bar.Controls.Add(subLbl)
        End If

        Dim bottomRule As New Panel() With {
            .Height = 1,
            .Dock = DockStyle.Bottom,
            .BackColor = ColBorder
        }
        bar.Controls.Add(bottomRule)
        bottomRule.SendToBack()
        Return bar
    End Function

    ''' <summary>Main content area below top bar.</summary>
    Public Shared Function CreateContentArea() As Panel
        Return New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = ColBackground,
            .Padding = New Padding(PadPage)
        }
    End Function

    ' ========== LABELS & DIVIDERS ==========

    Public Shared Function CreateHeadingLabel(text As String, Optional level As Integer = 2) As Label
        Dim lbl As New Label() With {
            .Text = text,
            .AutoSize = True,
            .ForeColor = ColTextPrimary,
            .Margin = New Padding(0, 0, 0, SpaceMd)
        }

        Select Case level
            Case 1
                lbl.Font = FontDisplay
            Case 2
                lbl.Font = FontHeading
            Case 3
                lbl.Font = FontSubheading
            Case Else
                lbl.Font = FontHeading
        End Select

        Return lbl
    End Function

    Public Shared Function CreateSecondaryLabel(text As String) As Label
        Return New Label() With {
            .Text = text,
            .AutoSize = True,
            .ForeColor = ColTextSecondary,
            .Margin = New Padding(0, PadControl, PadControl, PadControl),
            .Font = FontBody
        }
    End Function

    ''' <summary>Section header with ColBorder bottom rule.</summary>
    Public Shared Function CreateSectionHeader(text As String) As Panel
        Dim host As New Panel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .Margin = New Padding(0, 0, 0, PadControl),
            .BackColor = Color.Transparent
        }

        Dim lbl As New Label() With {
            .Text = text,
            .Font = FontSubheading,
            .ForeColor = ColTextSecondary,
            .AutoSize = True,
            .Dock = DockStyle.Top,
            .Margin = New Padding(0, 0, 0, PadControl)
        }
        host.Controls.Add(lbl)

        Dim rule As New Panel() With {
            .Height = 1,
            .Dock = DockStyle.Bottom,
            .BackColor = ColBorder
        }
        host.Controls.Add(rule)
        Return host
    End Function

    Public Shared Function CreateDivider() As Panel
        Return New Panel() With {
            .Height = 1,
            .Dock = DockStyle.Top,
            .BackColor = ColBorder,
            .Margin = New Padding(0, PadCard, 0, PadCard)
        }
    End Function

    Public Shared Function CreateEmptyStateLabel(text As String) As Label
        Return New Label() With {
            .Text = text,
            .AutoSize = False,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter,
            .ForeColor = ColTextSecondary,
            .Font = FontBody,
            .BackColor = ColSurface
        }
    End Function

    Public Shared Function CreateButtonRow(Optional alignment As FlowDirection = FlowDirection.RightToLeft) As FlowLayoutPanel
        Return New FlowLayoutPanel() With {
            .FlowDirection = alignment,
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .Dock = DockStyle.Bottom,
            .Padding = New Padding(0),
            .Margin = New Padding(0, PadCard, 0, 0),
            .WrapContents = False
        }
    End Function

    ' ========== CARDS & BADGES ==========

    ''' <summary>Single card surface with 1px ColBorder; optional ColPrimary left accent.</summary>
    Public Shared Function CreateCard(Optional leftAccent As Boolean = False) As Panel
        Dim card As New CardPanel(leftAccent) With {
            .BackColor = ColSurface,
            .Padding = New Padding(PadCard)
        }
        Return card
    End Function

    ''' <summary>Bordered card with inner content host (legacy two-panel structure).</summary>
    Public Shared Function CreateCardPanel(Optional innerPadding As Padding = Nothing) As Panel
        If innerPadding = Padding.Empty Then
            innerPadding = New Padding(PadCard)
        End If

        Dim outer As New Panel() With {
            .BackColor = ColBorder,
            .Padding = New Padding(1)
        }

        Dim inner As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = ColSurface,
            .Padding = innerPadding
        }
        outer.Controls.Add(inner)
        Return outer
    End Function

    Public Shared Function GetCardContentHost(card As Panel) As Panel
        If card Is Nothing Then
            Return Nothing
        End If

        If TypeOf card Is CardPanel Then
            Return card
        End If

        If card.Controls.Count = 0 Then
            Return Nothing
        End If

        Return TryCast(card.Controls(0), Panel)
    End Function

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

    Public Shared Function CreateFormSection(title As String) As Panel
        Dim card As Panel = CreateCardPanel(New Padding(PadCard))
        Dim content As Panel = GetCardContentHost(card)

        If content IsNot Nothing AndAlso Not String.IsNullOrEmpty(title) Then
            Dim heading As Label = CreateHeadingLabel(title, 3)
            heading.Dock = DockStyle.Top
            content.Controls.Add(heading)
        End If

        Return card
    End Function

    ''' <summary>Status badge with muted background and rounded border.</summary>
    Public Shared Function CreateBadge(text As String, badgeColor As Color) As Label
        Return New BadgeLabel(text, badgeColor)
    End Function

    ' ========== INPUTS ==========

    ''' <summary>Styles TextBox, ComboBox, NumericUpDown, and DateTimePicker consistently.</summary>
    Public Shared Sub ApplyInputStyle(ctrl As Control)
        If ctrl Is Nothing Then
            Return
        End If

        ctrl.BackColor = ColSurface
        ctrl.ForeColor = ColTextPrimary
        ctrl.Font = FontBody

        Dim tb As TextBox = TryCast(ctrl, TextBox)
        If tb IsNot Nothing Then
            tb.BorderStyle = BorderStyle.FixedSingle
            WireInputFocusHandlers(tb)
            Return
        End If

        Dim combo As ComboBox = TryCast(ctrl, ComboBox)
        If combo IsNot Nothing Then
            combo.FlatStyle = FlatStyle.Flat
            WireInputFocusHandlers(combo)
            Return
        End If

        Dim num As NumericUpDown = TryCast(ctrl, NumericUpDown)
        If num IsNot Nothing Then
            num.BorderStyle = BorderStyle.FixedSingle
            num.Height = InputHeight
            WireInputFocusHandlers(num)
            Return
        End If

        Dim dtp As DateTimePicker = TryCast(ctrl, DateTimePicker)
        If dtp IsNot Nothing Then
            WireInputFocusHandlers(dtp)
        End If
    End Sub

    Public Shared Sub ApplyInputFieldStyle(textBox As TextBox)
        ApplyInputStyle(textBox)
    End Sub

    Public Shared Sub ApplyComboBoxStyle(combo As ComboBox)
        ApplyInputStyle(combo)
    End Sub

    Public Shared Sub ApplyFilledTextInputVisual(textBox As TextBox)
        ApplyInputStyle(textBox)
    End Sub

    ''' <summary>Wraps a control in a 1px border panel that highlights on focus.</summary>
    Public Shared Function WrapInputWithBorder(ctrl As Control) As Panel
        If ctrl Is Nothing Then
            Return Nothing
        End If

        Dim existing As InputBorderPanel = TryCast(ctrl.Tag, InputBorderPanel)
        If existing IsNot Nothing AndAlso Not existing.IsDisposed Then
            Return existing
        End If

        ApplyInputStyle(ctrl)
        Dim wrap As New InputBorderPanel(ctrl)
        ctrl.Tag = wrap
        Return wrap
    End Function

    Private Shared Sub WireInputFocusHandlers(ctrl As Control)
        RemoveHandler ctrl.Enter, AddressOf InputControl_Enter
        RemoveHandler ctrl.Leave, AddressOf InputControl_Leave
        AddHandler ctrl.Enter, AddressOf InputControl_Enter
        AddHandler ctrl.Leave, AddressOf InputControl_Leave
    End Sub

    Private Shared Sub InputControl_Enter(sender As Object, e As EventArgs)
        Dim ctrl As Control = TryCast(sender, Control)
        If ctrl Is Nothing Then
            Return
        End If

        Dim wrap As InputBorderPanel = TryCast(ctrl.Tag, InputBorderPanel)
        If wrap IsNot Nothing Then
            wrap.SetFocused(True)
        Else
            ctrl.BackColor = ColPrimaryMuted
        End If
    End Sub

    Private Shared Sub InputControl_Leave(sender As Object, e As EventArgs)
        Dim ctrl As Control = TryCast(sender, Control)
        If ctrl Is Nothing Then
            Return
        End If

        Dim wrap As InputBorderPanel = TryCast(ctrl.Tag, InputBorderPanel)
        If wrap IsNot Nothing Then
            wrap.SetFocused(False)
        Else
            ctrl.BackColor = ColSurface
        End If
    End Sub

    Public Shared Sub ApplyGroupBoxStyle(groupBox As GroupBox)
        If groupBox Is Nothing Then
            Return
        End If

        groupBox.ForeColor = ColTextPrimary
        groupBox.Font = FontSubheading
        groupBox.Padding = New Padding(PadCard)
    End Sub

    Public Shared Sub ApplyTableLayoutDropDown(combo As ComboBox)
        If combo Is Nothing Then
            Return
        End If

        combo.IntegralHeight = False
        combo.Dock = DockStyle.None
        combo.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top
        Dim h As Integer = Math.Max(combo.PreferredHeight, InputHeight)
        combo.MinimumSize = New Size(0, h)
        combo.MaximumSize = New Size(0, h)
        ApplyInputStyle(combo)
    End Sub

    Public Shared Sub ApplyTableLayoutSingleLineTextBox(textBox As TextBox)
        If textBox Is Nothing Then
            Return
        End If

        textBox.Dock = DockStyle.None
        textBox.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top
        Dim h As Integer = Math.Max(textBox.PreferredHeight, InputHeight)
        textBox.MinimumSize = New Size(0, h)
        textBox.MaximumSize = New Size(0, h)
        ApplyInputStyle(textBox)
    End Sub

    ' ========== GRIDS ==========

    Public Shared Sub ApplyGridStyle(dgv As DataGridView)
        If dgv Is Nothing Then
            Return
        End If

        dgv.BackgroundColor = ColSurface
        dgv.BorderStyle = BorderStyle.None
        dgv.EnableHeadersVisualStyles = False
        dgv.GridColor = ColBorder
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        dgv.RowHeadersVisible = False
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing
        dgv.ColumnHeadersHeight = GridHeaderHeight
        dgv.RowTemplate.Height = GridRowHeight

        dgv.DefaultCellStyle.BackColor = ColSurface
        dgv.DefaultCellStyle.ForeColor = ColTextPrimary
        dgv.DefaultCellStyle.Font = FontBody
        dgv.DefaultCellStyle.SelectionBackColor = ColPrimaryMuted
        dgv.DefaultCellStyle.SelectionForeColor = ColTextPrimary
        dgv.DefaultCellStyle.Padding = New Padding(PadControl, 0, 0, 0)

        dgv.ColumnHeadersDefaultCellStyle.BackColor = ColBackground
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = ColTextPrimary
        dgv.ColumnHeadersDefaultCellStyle.Font = FontBodyBold
        dgv.ColumnHeadersDefaultCellStyle.Padding = New Padding(PadControl, 0, 0, 0)
        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = ColBackground
        dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = ColTextPrimary
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
        dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single

        dgv.AlternatingRowsDefaultCellStyle.BackColor = ColSurfaceAlt
        dgv.AlternatingRowsDefaultCellStyle.ForeColor = ColTextPrimary
    End Sub

    Public Shared Sub ApplyReadOnlyGridTheme(dgv As DataGridView)
        ApplyGridStyle(dgv)
        dgv.AllowUserToAddRows = False
        dgv.AllowUserToDeleteRows = False
        dgv.ReadOnly = True
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgv.MultiSelect = False
    End Sub

    Public Shared Sub ApplyDataGridViewChrome(dgv As DataGridView)
        ApplyReadOnlyGridTheme(dgv)
    End Sub

    ' ========== BUTTONS (spec flat style) ==========

    Public Shared Sub ApplyPrimaryButton(button As Button)
        ApplySpecFlatButton(button, ColPrimary, ColPrimaryLight, ColTextOnDark, Nothing, FontBodyBold)
    End Sub

    Public Shared Sub ApplyDangerButton(button As Button)
        ApplySpecFlatButton(button, ColDanger, ColDangerLight, ColTextOnDark, Nothing, FontBodyBold)
    End Sub

    Public Shared Sub ApplySuccessButton(button As Button)
        ApplySpecFlatButton(button, ColAccent, ColAccentLight, ColTextOnDark, Nothing, FontBodyBold)
    End Sub

    Public Shared Sub ApplySecondaryButton(button As Button)
        ApplySpecFlatButton(button, ColSurface, ColPrimaryMuted, ColPrimary, ColBorder, FontBody)
    End Sub

    Public Shared Sub ApplyGhostButton(button As Button)
        ApplySpecFlatButton(button, Color.Transparent, ColBackground, ColTextSecondary, Nothing, FontCaption)
    End Sub

    Public Shared Sub ApplyDisabledButton(button As Button)
        If button Is Nothing Then
            Return
        End If

        ClearSpecButtonState(button)
        button.Enabled = False
        button.FlatStyle = FlatStyle.Flat
        button.BackColor = ColBackground
        button.ForeColor = ColTextDisabled
        button.Font = FontBody
        button.Cursor = Cursors.Default
        button.Padding = New Padding(12, 0, 12, 0)
        button.MinimumSize = New Size(0, ButtonHeight)
        button.FlatAppearance.BorderSize = 1
        button.FlatAppearance.BorderColor = ColBorder
        button.UseCompatibleTextRendering = False
        button.TextAlign = ContentAlignment.MiddleCenter
    End Sub

    Public Shared Sub ApplySecondaryAccentButton(button As Button)
        ApplySpecFlatButton(button, ColTextLink, ColPrimaryLight, ColTextOnDark, Nothing, FontBodyBold)
    End Sub

    Public Shared Sub ApplyWarningButton(button As Button)
        ApplySpecFlatButton(button, ColWarning, WarningHover, ColTextOnDark, Nothing, FontBodyBold)
    End Sub

    Private Shared Sub ApplySpecFlatButton(
        button As Button,
        back As Color,
        hover As Color,
        fore As Color,
        border As Nullable(Of Color),
        font As Font)

        If button Is Nothing Then
            Return
        End If

        ClearSpecButtonState(button)
        button.Enabled = True
        button.FlatStyle = FlatStyle.Flat
        button.BackColor = back
        button.ForeColor = fore
        button.Font = font
        button.Cursor = Cursors.Hand
        button.Padding = New Padding(12, 0, 12, 0)
        button.MinimumSize = New Size(0, ButtonHeight)
        button.UseCompatibleTextRendering = False
        button.TextAlign = ContentAlignment.MiddleCenter
        button.FlatAppearance.BorderSize = If(border.HasValue, 1, 0)
        If border.HasValue Then
            button.FlatAppearance.BorderColor = border.Value
        End If
        button.FlatAppearance.MouseOverBackColor = hover
        button.FlatAppearance.MouseDownBackColor = hover
    End Sub

    Private Shared Sub ClearSpecButtonState(button As Button)
        If button Is Nothing Then
            Return
        End If

        Dim rounded As RoundedButtonState = TryCast(button.Tag, RoundedButtonState)
        If rounded IsNot Nothing Then
            button.Tag = Nothing
            RemoveHandler button.Paint, AddressOf RoundedButton_Paint
            RemoveHandler button.MouseEnter, AddressOf RoundedButton_Invalidate
            RemoveHandler button.MouseLeave, AddressOf RoundedButton_Invalidate
            RemoveHandler button.MouseDown, AddressOf RoundedButton_Invalidate
            RemoveHandler button.MouseUp, AddressOf RoundedButton_Invalidate
            RemoveHandler button.EnabledChanged, AddressOf RoundedButton_Invalidate
            RemoveHandler button.Resize, AddressOf RoundedButton_Invalidate
            RemoveHandler button.TextChanged, AddressOf RoundedButton_Invalidate
        End If
    End Sub

    ' ========== ROUNDED BUTTONS (legacy — kept for backward compatibility) ==========

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
        button.Padding = New Padding(PadCard, PadControl, PadCard, PadControl)
        button.TextAlign = ContentAlignment.MiddleCenter
        button.Font = FontBody
        button.MinimumSize = New Size(0, ButtonHeightMd)
        WireRoundedButtonPaint(button, normal, hover, pressed, fore, border)
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
        Dim clearColor As Color = ColSurface
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

        Dim textColor As Color = If(button.Enabled, state.ForeColor, ColTextSecondary)
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

        If minH < ButtonHeight Then
            minH = ButtonHeight
        End If

        If button.Height < minH Then
            button.Height = minH
        End If

        If button.MinimumSize.Height < minH OrElse button.MinimumSize.Width < minW Then
            button.MinimumSize = New Size(Math.Max(button.MinimumSize.Width, minW), Math.Max(button.MinimumSize.Height, minH))
        End If
    End Sub

    ''' <summary>Legacy recursive theming — prefer explicit helpers on each form.</summary>
    Public Shared Sub ApplyProfessionalGraphics(targetForm As Form)
        targetForm.BackColor = ColBackground
        StyleControlsRecursively(targetForm.Controls)
    End Sub

    Private Shared Sub StyleControlsRecursively(controls As Control.ControlCollection)
        For Each ctrl As Control In controls
            If TypeOf ctrl Is Button Then
                Dim btn As Button = DirectCast(ctrl, Button)
                Dim name As String = btn.Name.ToLowerInvariant()
                If name.Contains("delete") OrElse name.Contains("remove") OrElse name.Contains("cancel") Then
                    ApplyDangerButton(btn)
                ElseIf name.Contains("save") OrElse name.Contains("add") OrElse name.Contains("checkout") OrElse name.Contains("ok") Then
                    ApplySuccessButton(btn)
                ElseIf name.Contains("print") OrElse name.Contains("report") Then
                    ApplySecondaryAccentButton(btn)
                Else
                    ApplyPrimaryButton(btn)
                End If
            ElseIf TypeOf ctrl Is DataGridView Then
                ApplyGridStyle(DirectCast(ctrl, DataGridView))
            ElseIf TypeOf ctrl Is TextBox Then
                ApplyInputStyle(DirectCast(ctrl, TextBox))
            ElseIf TypeOf ctrl Is ComboBox Then
                ApplyInputStyle(DirectCast(ctrl, ComboBox))
            ElseIf TypeOf ctrl Is NumericUpDown Then
                ApplyInputStyle(DirectCast(ctrl, NumericUpDown))
            ElseIf TypeOf ctrl Is Label Then
                DirectCast(ctrl, Label).Font = FontBody
            End If

            If ctrl.HasChildren Then
                StyleControlsRecursively(ctrl.Controls)
            End If
        Next
    End Sub

    ' ========== PRIVATE SUPPORT TYPES ==========

    Private NotInheritable Class RoundedButtonState
        Public Property CornerRadius As Integer = DefaultButtonCornerRadius
        Public Property NormalBack As Color
        Public Property HoverBack As Color
        Public Property PressedBack As Color
        Public Property ForeColor As Color
        Public Property BorderColor As Nullable(Of Color)
        Public Property PaintWired As Boolean
    End Class

    Private NotInheritable Class CardPanel
        Inherits Panel

        Private ReadOnly _leftAccent As Boolean

        Public Sub New(Optional leftAccent As Boolean = False)
            _leftAccent = leftAccent
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            Dim rect As New Rectangle(0, 0, Width - 1, Height - 1)
            Using borderPen As New Pen(ColBorder, 1.0F)
                e.Graphics.DrawRectangle(borderPen, rect)
            End Using

            If _leftAccent Then
                Using accentBrush As New SolidBrush(ColPrimary)
                    e.Graphics.FillRectangle(accentBrush, 0, 0, 3, Height)
                End Using
            End If
        End Sub
    End Class

    Private NotInheritable Class BadgeLabel
        Inherits Label

        Private ReadOnly _badgeColor As Color

        Public Sub New(text As String, badgeColor As Color)
            _badgeColor = badgeColor
            Me.Text = text
            AutoSize = True
            Font = New Font(FontCaption.FontFamily, FontCaption.Size, FontStyle.Bold)
            ForeColor = badgeColor
            BackColor = BlendWithWhite(badgeColor, 0.15F)
            Padding = New Padding(6, 2, 6, 2)
            Margin = New Padding(0)
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw Or ControlStyles.UserPaint, True)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            Dim rect As New Rectangle(0, 0, Width - 1, Height - 1)
            Using path As GraphicsPath = CreateRoundedRectPath(rect, RadiusSm)
                Using fillBrush As New SolidBrush(BackColor)
                    e.Graphics.FillPath(fillBrush, path)
                End Using
                Using borderPen As New Pen(_badgeColor, 1.0F)
                    e.Graphics.DrawPath(borderPen, path)
                End Using
            End Using

            Dim textRect As Rectangle = ClientRectangle
            TextRenderer.DrawText(e.Graphics, Text, Font, textRect, ForeColor,
                TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.SingleLine)
        End Sub
    End Class

    Private NotInheritable Class InputBorderPanel
        Inherits Panel

        Private ReadOnly _inner As Control
        Private _focused As Boolean

        Public Sub New(inner As Control)
            _inner = inner
            BackColor = ColBorder
            Padding = New Padding(1)
            AutoSize = True
            AutoSizeMode = AutoSizeMode.GrowAndShrink

            _inner.Dock = DockStyle.Fill
            Controls.Add(_inner)
            inner.Tag = Me
        End Sub

        Public Sub SetFocused(focused As Boolean)
            _focused = focused
            BackColor = If(focused, ColBorderFocus, ColBorder)
            Invalidate()
        End Sub
    End Class

End Class
