Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Collects ID or membership proof before a privileged POS discount is applied.
''' </summary>
Public Class DiscountVerificationDialog
    Inherits Form

    Private txtVerificationId As TextBox
    Private lblError As Label
    Private enteredIdValue As String = String.Empty

    ''' <summary>
    ''' Gets the trimmed ID or membership number entered by the cashier.
    ''' </summary>
    Public ReadOnly Property EnteredId As String
        Get
            Return enteredIdValue
        End Get
    End Property

    ''' <summary>
    ''' Initializes a verification dialog for the given discount kind.
    ''' </summary>
    ''' <param name="discountTitle">Dialog title (for example PWD discount).</param>
    ''' <param name="instruction">Guidance shown above the ID field.</param>
    ''' <param name="idFieldLabel">Label for the proof field.</param>
    Public Sub New(discountTitle As String, instruction As String, idFieldLabel As String)
        BuildUi(discountTitle, instruction, idFieldLabel)
    End Sub

    Private Sub BuildUi(discountTitle As String, instruction As String, idFieldLabel As String)
        Me.Text = discountTitle
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MinimizeBox = False
        Me.MaximizeBox = False
        Me.ShowInTaskbar = False
        Me.ClientSize = New Size(420, 220)
        Me.AcceptButton = Nothing
        Me.CancelButton = Nothing

        Dim lblInstruction As New Label() With {
            .Text = instruction,
            .AutoSize = False,
            .Width = 380,
            .Height = 48,
            .Location = New Point(UiTheme.PadPage, UiTheme.PadPage),
            .ForeColor = UiTheme.ColTextSecondary,
            .Font = UiTheme.FontBody
        }

        Dim lblId As New Label() With {
            .Text = idFieldLabel,
            .AutoSize = True,
            .Location = New Point(UiTheme.PadPage, lblInstruction.Bottom + UiTheme.PadControl),
            .Font = UiTheme.FontBodyBold,
            .ForeColor = UiTheme.ColTextPrimary
        }

        txtVerificationId = New TextBox() With {
            .Width = 380,
            .Location = New Point(UiTheme.PadPage, lblId.Bottom + UiTheme.PadTight),
            .MaxLength = 40
        }
        UiTheme.ApplyInputStyle(txtVerificationId)

        lblError = New Label() With {
            .Text = String.Empty,
            .AutoSize = True,
            .ForeColor = UiTheme.ColDanger,
            .Font = UiTheme.FontCaption,
            .Location = New Point(UiTheme.PadPage, txtVerificationId.Bottom + UiTheme.PadTight),
            .Visible = False
        }

        Dim btnOk As New Button() With {
            .Text = "Verify & apply",
            .Width = 120,
            .Location = New Point(UiTheme.PadPage, lblError.Bottom + UiTheme.PadSection)
        }
        UiTheme.ApplyPrimaryButton(btnOk)

        Dim btnCancel As New Button() With {
            .Text = "Cancel",
            .Width = 100,
            .Location = New Point(btnOk.Right + UiTheme.PadControl, btnOk.Top),
            .DialogResult = DialogResult.Cancel
        }
        UiTheme.ApplySecondaryButton(btnCancel)

        AddHandler btnOk.Click,
            Sub()
                Dim idValue As String = txtVerificationId.Text.Trim()
                If idValue.Length < 4 Then
                    lblError.Text = "Enter a valid ID or membership number (at least 4 characters)."
                    lblError.Visible = True
                    txtVerificationId.Focus()
                    Return
                End If

                enteredIdValue = idValue
                Me.DialogResult = DialogResult.OK
                Me.Close()
            End Sub

        Me.Controls.Add(lblInstruction)
        Me.Controls.Add(lblId)
        Me.Controls.Add(txtVerificationId)
        Me.Controls.Add(lblError)
        Me.Controls.Add(btnOk)
        Me.Controls.Add(btnCancel)
        Me.CancelButton = btnCancel
    End Sub

End Class
