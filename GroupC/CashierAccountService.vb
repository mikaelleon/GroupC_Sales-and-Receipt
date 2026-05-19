Imports System.Data
Imports System.Globalization
Imports System.Text.RegularExpressions
Imports Microsoft.Data.SqlClient

''' <summary>
''' Database-backed cashier account registration, validation, and maintenance.
''' </summary>
Public NotInheritable Class CashierAccountService

    Public Const MinUsernameLength As Integer = 3
    Public Const MaxUsernameLength As Integer = 50
    Public Const MinPasswordLength As Integer = 6
    Public Const MaxDisplayNameLength As Integer = 100

    Private Shared ReadOnly UsernamePattern As New Regex("^[a-zA-Z0-9_]+$", RegexOptions.CultureInvariant)

    Private Sub New()
    End Sub

    Public Class CashierLoginResult
        Public Property Success As Boolean
        Public Property CashierId As Integer?
        Public Property Username As String = String.Empty
        Public Property DisplayName As String = String.Empty
        Public Property ErrorMessage As String = String.Empty
    End Class

    Public Shared Function ValidateUsername(username As String, ByRef errorMessage As String) As Boolean
        errorMessage = String.Empty
        Dim value As String = If(username, String.Empty).Trim()

        If value.Length < MinUsernameLength OrElse value.Length > MaxUsernameLength Then
            errorMessage = "Username must be " & MinUsernameLength.ToString(CultureInfo.InvariantCulture) &
                "–" & MaxUsernameLength.ToString(CultureInfo.InvariantCulture) & " characters."
            Return False
        End If

        If Not UsernamePattern.IsMatch(value) Then
            errorMessage = "Username may only contain letters, numbers, and underscores."
            Return False
        End If

        Return True
    End Function

    Public Shared Function ValidatePassword(password As String, ByRef errorMessage As String) As Boolean
        errorMessage = String.Empty
        Dim value As String = If(password, String.Empty)

        If value.Length < MinPasswordLength Then
            errorMessage = "Password must be at least " & MinPasswordLength.ToString(CultureInfo.InvariantCulture) & " characters."
            Return False
        End If

        Return True
    End Function

    Public Shared Function TryAuthenticate(username As String, password As String) As CashierLoginResult
        Dim result As New CashierLoginResult()
        Dim userError As String = String.Empty

        If Not ValidateUsername(username, userError) Then
            result.ErrorMessage = userError
            Return result
        End If

        If Not ValidatePassword(password, userError) Then
            result.ErrorMessage = "Enter your account password."
            Return result
        End If

        DatabaseInitializer.EnsureDatabase()

        Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
            connection.Open()

            Dim sql As String =
                "SELECT cashier_id, username, display_name, password_hash, password_salt, is_active " &
                "FROM dbo.cashier_accounts WHERE username = @username;"

            Using cmd As New SqlCommand(sql, connection)
                cmd.Parameters.AddWithValue("@username", username.Trim())

                Using reader As SqlDataReader = cmd.ExecuteReader()
                    If Not reader.Read() Then
                        result.ErrorMessage = "Invalid username or password."
                        Return result
                    End If

                    Dim isActive As Boolean = Convert.ToBoolean(reader("is_active"))
                    If Not isActive Then
                        result.ErrorMessage = "This cashier account is deactivated. Contact an administrator."
                        Return result
                    End If

                    Dim salt As String = reader("password_salt").ToString()
                    Dim hash As String = reader("password_hash").ToString()

                    If Not PasswordHasher.Verify(password, salt, hash) Then
                        result.ErrorMessage = "Invalid username or password."
                        Return result
                    End If

                    result.CashierId = Convert.ToInt32(reader("cashier_id"))
                    result.Username = reader("username").ToString()
                    Dim displayObj As Object = reader("display_name")
                    result.DisplayName = If(displayObj Is Nothing OrElse displayObj Is DBNull.Value, String.Empty, displayObj.ToString())
                End Using
            End Using

            Dim updateSql As String = "UPDATE dbo.cashier_accounts SET last_login_at = SYSUTCDATETIME() WHERE cashier_id = @id;"
            Using updateCmd As New SqlCommand(updateSql, connection)
                updateCmd.Parameters.AddWithValue("@id", result.CashierId.Value)
                updateCmd.ExecuteNonQuery()
            End Using
        End Using

        result.Success = True
        Return result
    End Function

    Public Shared Function LoadAccountsTable() As DataTable
        DatabaseInitializer.EnsureDatabase()

        Dim table As New DataTable()
        Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
            connection.Open()
            Dim sql As String =
                "SELECT cashier_id, username, display_name, is_active, last_login_at, created_at " &
                "FROM dbo.cashier_accounts ORDER BY username;"
            Using adapter As New SqlDataAdapter(sql, connection)
                adapter.Fill(table)
            End Using
        End Using

        Return table
    End Function

    Public Shared Sub RegisterAccount(username As String, password As String, displayName As String)
        Dim userError As String = String.Empty
        Dim passError As String = String.Empty

        If Not ValidateUsername(username, userError) Then
            Throw New InvalidOperationException(userError)
        End If

        If Not ValidatePassword(password, passError) Then
            Throw New InvalidOperationException(passError)
        End If

        Dim trimmedDisplay As String = If(displayName, String.Empty).Trim()
        If trimmedDisplay.Length > MaxDisplayNameLength Then
            Throw New InvalidOperationException("Display name is too long.")
        End If

        Dim salt As String = PasswordHasher.GenerateSalt()
        Dim hash As String = PasswordHasher.HashPassword(password, salt)

        DatabaseInitializer.EnsureDatabase()

        Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
            connection.Open()
            Dim sql As String =
                "INSERT INTO dbo.cashier_accounts (username, password_hash, password_salt, display_name, is_active) " &
                "VALUES (@username, @hash, @salt, @display, 1);"
            Using cmd As New SqlCommand(sql, connection)
                cmd.Parameters.AddWithValue("@username", username.Trim())
                cmd.Parameters.AddWithValue("@hash", hash)
                cmd.Parameters.AddWithValue("@salt", salt)
                If trimmedDisplay.Length = 0 Then
                    cmd.Parameters.AddWithValue("@display", DBNull.Value)
                Else
                    cmd.Parameters.AddWithValue("@display", trimmedDisplay)
                End If
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Public Shared Sub UpdateDisplayName(cashierId As Integer, displayName As String)
        Dim trimmedDisplay As String = If(displayName, String.Empty).Trim()
        If trimmedDisplay.Length > MaxDisplayNameLength Then
            Throw New InvalidOperationException("Display name is too long.")
        End If

        Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
            connection.Open()
            Dim sql As String = "UPDATE dbo.cashier_accounts SET display_name = @display WHERE cashier_id = @id;"
            Using cmd As New SqlCommand(sql, connection)
                cmd.Parameters.AddWithValue("@id", cashierId)
                If trimmedDisplay.Length = 0 Then
                    cmd.Parameters.AddWithValue("@display", DBNull.Value)
                Else
                    cmd.Parameters.AddWithValue("@display", trimmedDisplay)
                End If
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Public Shared Sub ResetPassword(cashierId As Integer, newPassword As String)
        Dim passError As String = String.Empty
        If Not ValidatePassword(newPassword, passError) Then
            Throw New InvalidOperationException(passError)
        End If

        Dim salt As String = PasswordHasher.GenerateSalt()
        Dim hash As String = PasswordHasher.HashPassword(newPassword, salt)

        Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
            connection.Open()
            Dim sql As String =
                "UPDATE dbo.cashier_accounts SET password_hash = @hash, password_salt = @salt WHERE cashier_id = @id;"
            Using cmd As New SqlCommand(sql, connection)
                cmd.Parameters.AddWithValue("@hash", hash)
                cmd.Parameters.AddWithValue("@salt", salt)
                cmd.Parameters.AddWithValue("@id", cashierId)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Public Shared Sub SetActive(cashierId As Integer, isActive As Boolean)
        Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
            connection.Open()
            Dim sql As String = "UPDATE dbo.cashier_accounts SET is_active = @active WHERE cashier_id = @id;"
            Using cmd As New SqlCommand(sql, connection)
                cmd.Parameters.AddWithValue("@active", isActive)
                cmd.Parameters.AddWithValue("@id", cashierId)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

End Class
