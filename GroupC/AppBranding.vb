''' <summary>
''' Application display name and window title helpers.
''' </summary>
Public NotInheritable Class AppBranding

    Public Const ApplicationName As String = "International Bookstore"

    Private Sub New()
    End Sub

    Public Shared Function WindowTitle(section As String) As String
        If String.IsNullOrWhiteSpace(section) Then
            Return ApplicationName
        End If

        Return ApplicationName & " — " & section.Trim()
    End Function

End Class
