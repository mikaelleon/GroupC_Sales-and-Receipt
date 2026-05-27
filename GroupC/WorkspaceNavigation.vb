Imports System.Windows.Forms

''' <summary>
''' Cross-screen navigation from workspace sidebars back through MainMenuForm.
''' </summary>
Public Module WorkspaceNavigation

    Public Enum Target
        None = 0
        Products
        Categories
        Cashiers
        Sales
        Receipt
        Reports
    End Enum

    Private pendingTarget As Target = Target.None

    Public Sub RequestNavigate(target As Target)
        pendingTarget = target
    End Sub

    Public Function TryConsumePending() As Target
        Dim nextTarget As Target = pendingTarget
        pendingTarget = Target.None
        Return nextTarget
    End Function

    Public Function TryMapNavText(text As String) As Target
        Select Case If(text, String.Empty).Trim()
            Case "Manage Products"
                Return Target.Products
            Case "Manage Categories"
                Return Target.Categories
            Case "Manage Cashiers"
                Return Target.Cashiers
            Case "Point of Sale"
                Return Target.Sales
            Case "Receipt Preview"
                Return Target.Receipt
            Case "Reports"
                Return Target.Reports
            Case Else
                Return Target.None
        End Select
    End Function

    Public Function CanAccess(target As Target) As Boolean
        Select Case target
            Case Target.Products, Target.Categories, Target.Cashiers, Target.Reports
                Return AppSession.IsAdmin()
            Case Target.Sales, Target.Receipt
                Return True
            Case Else
                Return False
        End Select
    End Function

    Public Sub NavigateFromSidebar(hostForm As Form, target As Target)
        If hostForm Is Nothing OrElse target = Target.None Then
            Return
        End If

        If Not CanAccess(target) Then
            MessageBox.Show(
                "Administrator access is required for that screen.",
                AppBranding.ApplicationName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)
            Return
        End If

        RequestNavigate(target)
        hostForm.Close()
    End Sub

End Module
