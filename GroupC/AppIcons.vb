Imports System.Drawing
Imports System.IO
Imports System.Reflection
Imports System.Windows.Forms

''' <summary>
''' Loads and applies the shared application icon to windows and the taskbar.
''' </summary>
Public NotInheritable Class AppIcons

    Private Shared ReadOnly SyncRoot As New Object()
    Private Shared cachedIcon As Icon
    Private Shared loadAttempted As Boolean

    Private Const IconRelativePath As String = "Assets\AppIcon.ico"
    Private Const EmbeddedIconSuffix As String = "AppIcon.ico"

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Preloads the icon during application startup.
    ''' </summary>
    Public Shared Sub EnsureLoaded()
        SyncLock SyncRoot
            EnsureCachedIconUnlocked()
        End SyncLock
    End Sub

    ''' <summary>
    ''' Returns a clone of the application icon, or Nothing when the icon cannot be loaded.
    ''' </summary>
    Public Shared Function TryGetApplicationIcon() As Icon
        SyncLock SyncRoot
            If Not EnsureCachedIconUnlocked() Then
                Return Nothing
            End If

            Return DirectCast(cachedIcon.Clone(), Icon)
        End SyncLock
    End Function

    ''' <summary>
    ''' Sets the form title-bar and taskbar icon. Replaces the default WinForms icon.
    ''' </summary>
    Public Shared Sub ApplyToForm(form As Form)
        If form Is Nothing Then
            Return
        End If

        Dim icon As Icon = TryGetApplicationIcon()
        If icon Is Nothing Then
            Return
        End If

        form.Icon = icon
    End Sub

    Private Shared Function EnsureCachedIconUnlocked() As Boolean
        If cachedIcon IsNot Nothing Then
            Return True
        End If

        If loadAttempted Then
            Return False
        End If

        loadAttempted = True
        cachedIcon = LoadIconFromEmbeddedResource()
        If cachedIcon Is Nothing Then
            cachedIcon = LoadIconFromFile()
        End If

        Return cachedIcon IsNot Nothing
    End Function

    Private Shared Function LoadIconFromEmbeddedResource() As Icon
        Dim assembly As Assembly = Assembly.GetExecutingAssembly()
        For Each resourceName As String In assembly.GetManifestResourceNames()
            If resourceName.EndsWith(EmbeddedIconSuffix, StringComparison.OrdinalIgnoreCase) Then
                Using stream As Stream = assembly.GetManifestResourceStream(resourceName)
                    If stream IsNot Nothing Then
                        Return New Icon(stream)
                    End If
                End Using
            End If
        Next

        Return Nothing
    End Function

    Private Shared Function LoadIconFromFile() As Icon
        Dim path As String = System.IO.Path.Combine(AppContext.BaseDirectory, IconRelativePath)
        If Not File.Exists(path) Then
            Return Nothing
        End If

        Using stream As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
            Return New Icon(stream)
        End Using
    End Function

End Class
