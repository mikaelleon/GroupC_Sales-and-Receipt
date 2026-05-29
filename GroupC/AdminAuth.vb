''' <summary>
''' Administrator password verification and updates (stored hashed in settings JSON).
''' </summary>
Public NotInheritable Class AdminAuth

    Private Sub New()
    End Sub

    Public Shared Function ValidatePassword(password As String) As Boolean
        If String.IsNullOrEmpty(password) Then
            Return False
        End If

        Dim settings As AppSettingsData = AppSettings.Current
        Return PasswordHasher.Verify(password, settings.AdminPasswordSalt, settings.AdminPasswordHash)
    End Function

    Public Shared Sub ApplyPasswordChange(data As AppSettingsData, newPassword As String)
        If String.IsNullOrEmpty(newPassword) Then
            Return
        End If

        data.AdminPasswordSalt = PasswordHasher.GenerateSalt()
        data.AdminPasswordHash = PasswordHasher.HashPassword(newPassword, data.AdminPasswordSalt)
    End Sub

    Public Shared Sub EnsureDefaultPasswordHash(data As AppSettingsData)
        If Not String.IsNullOrEmpty(data.AdminPasswordSalt) AndAlso Not String.IsNullOrEmpty(data.AdminPasswordHash) Then
            Return
        End If

        ApplyPasswordChange(data, DatabaseConfig.DefaultAdminPassword)
    End Sub

End Class
