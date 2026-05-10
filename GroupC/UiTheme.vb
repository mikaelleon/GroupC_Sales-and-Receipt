Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Central design tokens and WinForms chrome helpers for Group C UI.
''' </summary>
Public NotInheritable Class UiTheme

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

    ''' <summary>
    ''' Default UI font applied with <see cref="ApplyStandardWindowChrome"/>.
    ''' </summary>
    Public Shared ReadOnly StandardUiFont As Font = New Font("Segoe UI", 10.0F)

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
    ''' Bordered white card: outer 1 px border color, inner content surface.
    ''' </summary>
    Public Shared Function CreateCardPanel(Optional innerPadding As Padding = Nothing) As Panel
        If innerPadding = Padding.Empty Then
            innerPadding = New Padding(12)
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

    Public Shared Sub ApplyReadOnlyGridTheme(dgv As DataGridView)
        dgv.BackgroundColor = CardSurface
        dgv.BorderStyle = BorderStyle.None
        dgv.EnableHeadersVisualStyles = False
        dgv.GridColor = CardBorder
        dgv.DefaultCellStyle.BackColor = CardSurface
        dgv.DefaultCellStyle.ForeColor = TextPrimary
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 235, 245)
        dgv.DefaultCellStyle.SelectionForeColor = TextPrimary
        dgv.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderBack
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary
        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = GridHeaderBack
        dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextPrimary
        dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single
        dgv.RowHeadersVisible = False
        dgv.AlternatingRowsDefaultCellStyle.BackColor = GridAltRow
        dgv.AlternatingRowsDefaultCellStyle.ForeColor = TextPrimary
    End Sub

    Public Shared Sub ApplyPrimaryButton(button As Button)
        StyleFlatButton(button, PrimaryAccent, PrimaryAccentHover, PrimaryAccentPressed, TextOnAccent, Nothing)
    End Sub

    Public Shared Sub ApplySecondaryButton(button As Button)
        button.BackColor = SecondaryBack
        button.ForeColor = SecondaryFore
        button.FlatStyle = FlatStyle.Flat
        button.FlatAppearance.BorderSize = 1
        button.FlatAppearance.BorderColor = SecondaryBorder
        button.FlatAppearance.MouseOverBackColor = GridAltRow
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(230, 232, 236)
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

        button.FlatAppearance.MouseOverBackColor = hover
        button.FlatAppearance.MouseDownBackColor = pressed
    End Sub

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

End Class
