Imports System.Windows.Forms

''' <summary>
''' Holds the signed-in role for the current application session (not persisted).
''' </summary>
Public Module AppSession

    Public Const RoleAdmin As String = "Admin"
    Public Const RoleCashier As String = "Cashier"

    ''' <summary>
    ''' Current operator role after login (<see cref="RoleAdmin"/> or <see cref="RoleCashier"/>).
    ''' </summary>
    Public CurrentRole As String = RoleCashier

    ''' <summary>
    ''' Signed-in cashier username (empty for administrator sessions).
    ''' </summary>
    Public CurrentUsername As String = String.Empty

    ''' <summary>
    ''' Friendly cashier name for receipts (display name or username).
    ''' </summary>
    Public CurrentCashierDisplayName As String = String.Empty

    ''' <summary>
    ''' Database cashier account id when signed in as cashier; otherwise Nothing.
    ''' </summary>
    Public CurrentCashierId As Integer? = Nothing

    ''' <summary>
    ''' Clears cashier identity fields (role unchanged).
    ''' </summary>
    Public Sub ClearCashierIdentity()
        CurrentUsername = String.Empty
        CurrentCashierDisplayName = String.Empty
        CurrentCashierId = Nothing
    End Sub

    ''' <summary>
    ''' Resets all session fields (call on sign-out and before a new sign-in).
    ''' </summary>
    Public Sub ClearSession()
        CurrentRole = RoleCashier
        ClearCashierIdentity()
    End Sub

    ''' <summary>
    ''' Starts an administrator session after successful sign-in.
    ''' </summary>
    Public Sub BeginAdminSession()
        ClearCashierIdentity()
        CurrentRole = RoleAdmin
    End Sub

    ''' <summary>
    ''' Starts a cashier session after successful sign-in.
    ''' </summary>
    Public Sub BeginCashierSession(cashierId As Integer, username As String, displayName As String)
        CurrentRole = RoleCashier
        CurrentCashierId = cashierId
        CurrentUsername = username
        CurrentCashierDisplayName = displayName
    End Sub

    ''' <summary>
    ''' Name printed on sales receipts for the current operator.
    ''' </summary>
    Public Function GetReceiptOperatorName() As String
        If IsAdmin() Then
            Return "Administrator"
        End If

        If Not String.IsNullOrWhiteSpace(CurrentCashierDisplayName) Then
            Return CurrentCashierDisplayName.Trim()
        End If

        If Not String.IsNullOrWhiteSpace(CurrentUsername) Then
            Return CurrentUsername.Trim()
        End If

        Return "Cashier"
    End Function

    ''' <summary>
    ''' Value stored in audit logs for the current operator.
    ''' </summary>
    Public Function GetAuditIdentity() As String
        If IsAdmin() Then
            Return RoleAdmin
        End If

        If Not String.IsNullOrEmpty(CurrentUsername) Then
            Return RoleCashier & " (" & CurrentUsername & ")"
        End If

        Return RoleCashier
    End Function

    ''' <summary>
    ''' Returns true when the current session is an administrator.
    ''' </summary>
    ''' <returns>True if <see cref="CurrentRole"/> is admin.</returns>
    Public Function IsAdmin() As Boolean
        Return String.Equals(CurrentRole, RoleAdmin, StringComparison.OrdinalIgnoreCase)
    End Function

    ''' <summary>
    ''' Returns true when a user has completed sign-in (admin or cashier).
    ''' </summary>
    Public Function HasActiveSession() As Boolean
        Return IsAdmin() OrElse CurrentCashierId.HasValue
    End Function

    ''' <summary>
    ''' Returns true when the current session is a signed-in cashier.
    ''' </summary>
    Public Function IsCashierSession() As Boolean
        Return Not IsAdmin() AndAlso CurrentCashierId.HasValue
    End Function

    ''' <summary>
    ''' Shows a message and returns false when the current user is not an administrator.
    ''' </summary>
    ''' <param name="owner">Optional window for the dialog.</param>
    ''' <returns>True if the user is an admin.</returns>
    Public Function RequireAdmin(Optional owner As IWin32Window = Nothing) As Boolean
        If IsAdmin() Then
            Return True
        End If

        MessageBox.Show(
            owner,
            "This action requires administrator access.",
            "Access denied",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information)
        Return False
    End Function

End Module
