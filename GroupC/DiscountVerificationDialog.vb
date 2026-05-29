Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Collects ID or membership proof before a privileged POS discount is applied.
''' </summary>
Public Class DiscountVerificationDialog
    Inherits Form

    Private ReadOnly verificationKind As DiscountIdValidator.VerificationKind
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
    ''' <param name="kind">Validation rules to apply on submit.</param>
    ''' <param name="discountTitle">Dialog title (for example PWD discount).</param>
    ''' <param name="instruction">Guidance shown above the ID field.</param>
    ''' <param name="idFieldLabel">Label for the proof field.</param>
    Public Sub New(kind As DiscountIdValidator.VerificationKind, discountTitle As String, instruction As String, idFieldLabel As String)
        verificationKind = kind
        BuildUi(discountTitle, instruction, idFieldLabel)
    End Sub

    Private Sub BuildUi(discountTitle As String, instruction As String, idFieldLabel As String)
        Me.Text = discountTitle
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MinimizeBox = False
        Me.MaximizeBox = False
        Me.ShowInTaskbar = False
        Me.ClientSize = New Size(460, 250)
        Me.AcceptButton = Nothing
        Me.CancelButton = Nothing

        Dim instructionHeight As Integer = If(verificationKind = DiscountIdValidator.VerificationKind.Pwd, 72, 56)

        Dim lblInstruction As New Label() With {
            .Text = instruction,
            .AutoSize = False,
            .Width = 420,
            .Height = instructionHeight,
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
            .Width = 420,
            .Location = New Point(UiTheme.PadPage, lblId.Bottom + UiTheme.PadTight),
            .MaxLength = 40
        }
        UiTheme.ApplyInputStyle(txtVerificationId)

        Select Case verificationKind
            Case DiscountIdValidator.VerificationKind.Pwd
                txtVerificationId.PlaceholderText = "RR-PPMM-BBB-NNNNNNN (14 digits)"
            Case DiscountIdValidator.VerificationKind.Senior
                txtVerificationId.PlaceholderText = "Full LGU / OSCA ID number"
            Case DiscountIdValidator.VerificationKind.Membership
                txtVerificationId.PlaceholderText = "Membership card number"
        End Select

        lblError = New Label() With {
            .Text = String.Empty,
            .AutoSize = False,
            .Width = 420,
            .Height = 40,
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

        AddHandler btnOk.Click, AddressOf VerifyAndClose

        Me.Controls.Add(lblInstruction)
        Me.Controls.Add(lblId)
        Me.Controls.Add(txtVerificationId)
        Me.Controls.Add(lblError)
        Me.Controls.Add(btnOk)
        Me.Controls.Add(btnCancel)
        Me.CancelButton = btnCancel
    End Sub

    Private Sub VerifyAndClose(sender As Object, e As EventArgs)
        Dim normalized As String = String.Empty
        Dim validationError As String = String.Empty

        If Not DiscountIdValidator.TryValidate(verificationKind, txtVerificationId.Text, normalized, validationError) Then
            lblError.Text = validationError
            lblError.Visible = True
            txtVerificationId.Focus()
            Return
        End If

        enteredIdValue = normalized
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

End Class
