Imports System.Drawing
Imports System.Windows.Forms

Public NotInheritable Class UiTheme

    Public Shared ReadOnly Navy As Color = Color.FromArgb(0, 0, 128)
    Public Shared ReadOnly NavyHover As Color = Color.FromArgb(25, 25, 150)
    Public Shared ReadOnly NavyPressed As Color = Color.FromArgb(0, 0, 90)
    Public Shared ReadOnly SecondaryBack As Color = Color.FromArgb(230, 230, 230)
    Public Shared ReadOnly SecondaryFore As Color = Color.FromArgb(40, 40, 40)

    Private Sub New()
    End Sub

    Public Shared Sub ApplyPrimaryButton(button As Button)
        button.BackColor = Navy
        button.ForeColor = Color.White
        button.FlatStyle = FlatStyle.Flat
        button.FlatAppearance.BorderSize = 0
        button.FlatAppearance.MouseOverBackColor = NavyHover
        button.FlatAppearance.MouseDownBackColor = NavyPressed
    End Sub

    Public Shared Sub ApplySecondaryButton(button As Button)
        button.BackColor = SecondaryBack
        button.ForeColor = SecondaryFore
        button.FlatStyle = FlatStyle.Flat
        button.FlatAppearance.BorderSize = 1
        button.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180)
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(210, 210, 210)
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(190, 190, 190)
    End Sub

End Class
