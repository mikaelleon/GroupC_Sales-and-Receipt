Imports System.Drawing
Imports System.IO

''' <summary>
''' Stores product images under LocalApplicationData and resolves paths for the UI.
''' </summary>
Public NotInheritable Class ProductImageStorage

    Private Const MaxFileBytes As Long = 5L * 1024L * 1024L

    Private Shared ReadOnly AllowedExtensions As String() = {".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp"}

    Private Sub New()
    End Sub

    Public Shared Function GetImagesRootFolder() As String
        Dim folder As String = IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GroupC",
            "ProductImages")
        Directory.CreateDirectory(folder)
        Return folder
    End Function

    Public Shared Function TryLoadImage(relativePath As String) As Image
        If String.IsNullOrWhiteSpace(relativePath) Then
            Return Nothing
        End If

        Dim fullPath As String = ResolveFullPath(relativePath)
        If fullPath Is Nothing OrElse Not File.Exists(fullPath) Then
            Return Nothing
        End If

        Try
            Using stream As New FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                Using source As Image = Image.FromStream(stream)
                    Return New Bitmap(source)
                End Using
            End Using
        Catch
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Copies the chosen file into the product images folder and returns a relative path for the database.
    ''' </summary>
    Public Shared Function SaveProductImage(productId As Integer, sourceFilePath As String) As String
        If productId <= 0 Then
            Throw New ArgumentOutOfRangeException(NameOf(productId))
        End If

        If String.IsNullOrWhiteSpace(sourceFilePath) OrElse Not File.Exists(sourceFilePath) Then
            Throw New FileNotFoundException("Product image file was not found.", sourceFilePath)
        End If

        Dim info As New FileInfo(sourceFilePath)
        If info.Length > MaxFileBytes Then
            Throw New InvalidOperationException("Image must be 5 MB or smaller.")
        End If

        Dim extension As String = info.Extension.ToLowerInvariant()
        If Not IsAllowedExtension(extension) Then
            Throw New InvalidOperationException("Supported image types: PNG, JPG, BMP, GIF, WEBP.")
        End If

        Dim root As String = GetImagesRootFolder()
        Dim fileName As String = "product_" & productId.ToString() & extension
        Dim destination As String = IO.Path.Combine(root, fileName)

        File.Copy(sourceFilePath, destination, overwrite:=True)
        Return fileName
    End Function

    Public Shared Sub DeleteImageIfExists(relativePath As String)
        Dim fullPath As String = ResolveFullPath(relativePath)
        If fullPath Is Nothing OrElse Not File.Exists(fullPath) Then
            Return
        End If

        Try
            File.Delete(fullPath)
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Resolves a stored image reference to a file under the product images folder only.
    ''' Absolute paths and path traversal sequences are rejected.
    ''' </summary>
    Public Shared Function ResolveFullPath(relativePath As String) As String
        If String.IsNullOrWhiteSpace(relativePath) Then
            Return Nothing
        End If

        Dim trimmed As String = relativePath.Trim()

        ' Database values must be bare filenames (e.g. product_12.png), never full paths.
        If IO.Path.IsPathRooted(trimmed) Then
            Return Nothing
        End If

        Dim fileName As String = IO.Path.GetFileName(trimmed)
        If String.IsNullOrEmpty(fileName) OrElse fileName.IndexOf("..", StringComparison.Ordinal) >= 0 Then
            Return Nothing
        End If

        If Not fileName.StartsWith("product_", StringComparison.OrdinalIgnoreCase) Then
            Return Nothing
        End If

        Dim rootFull As String = IO.Path.GetFullPath(GetImagesRootFolder())
        Dim candidateFull As String = IO.Path.GetFullPath(IO.Path.Combine(rootFull, fileName))
        If Not IsPathUnderImagesRoot(candidateFull, rootFull) Then
            Return Nothing
        End If

        Return candidateFull
    End Function

    Private Shared Function IsPathUnderImagesRoot(fullPath As String, rootFull As String) As Boolean
        Dim normalizedRoot As String = IO.Path.TrimEndingDirectorySeparator(IO.Path.GetFullPath(rootFull))
        normalizedRoot &= IO.Path.DirectorySeparatorChar

        Dim normalizedFile As String = IO.Path.GetFullPath(fullPath)
        Return normalizedFile.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
    End Function

    Public Shared Function IsAllowedImageFile(path As String) As Boolean
        If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
            Return False
        End If

        Return IsAllowedExtension(IO.Path.GetExtension(path))
    End Function

    Private Shared Function IsAllowedExtension(extension As String) As Boolean
        If String.IsNullOrWhiteSpace(extension) Then
            Return False
        End If

        Dim normalized As String = extension.ToLowerInvariant()
        For Each allowed As String In AllowedExtensions
            If normalized = allowed Then
                Return True
            End If
        Next

        Return False
    End Function

End Class
