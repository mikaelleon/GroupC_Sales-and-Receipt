Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' Salted SHA-256 hashing for cashier account passwords (demo / coursework use).
''' </summary>
Public NotInheritable Class PasswordHasher

    Private Sub New()
    End Sub

    Public Shared Function GenerateSalt() As String
        Dim bytes(31) As Byte
        Using rng As RandomNumberGenerator = RandomNumberGenerator.Create()
            rng.GetBytes(bytes)
        End Using
        Return Convert.ToBase64String(bytes)
    End Function

    Public Shared Function HashPassword(password As String, salt As String) As String
        Using sha As SHA256 = SHA256.Create()
            Dim combined As Byte() = Encoding.UTF8.GetBytes(salt & password)
            Return Convert.ToBase64String(sha.ComputeHash(combined))
        End Using
    End Function

    Public Shared Function Verify(password As String, salt As String, storedHash As String) As Boolean
        If String.IsNullOrEmpty(password) OrElse String.IsNullOrEmpty(salt) OrElse String.IsNullOrEmpty(storedHash) Then
            Return False
        End If

        Dim computed As String = HashPassword(password, salt)
        Return String.Equals(computed, storedHash, StringComparison.Ordinal)
    End Function

End Class
