Imports System.Drawing
Imports System.IO

''' <summary>
''' Stores and loads product images under the application ProductImages folder.
''' </summary>
Public NotInheritable Class ProductImageHelper

    Private Shared ReadOnly AllowedExtensions As String() = {".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp"}

    Public Const ImagesFolderName As String = "ProductImages"

    Private Sub New()
    End Sub

    Public Shared Sub EnsureImagesDirectory()
        Dim folder As String = GetImagesDirectory()
        If Not Directory.Exists(folder) Then
            Directory.CreateDirectory(folder)
        End If
    End Sub

    Public Shared Function GetImagesDirectory() As String
        Return Path.Combine(AppContext.BaseDirectory, ImagesFolderName)
    End Function

    Public Shared Function IsAllowedImageFile(filePath As String) As Boolean
        If String.IsNullOrWhiteSpace(filePath) Then
            Return False
        End If

        Dim ext As String = Path.GetExtension(filePath).ToLowerInvariant()
        For Each allowed As String In AllowedExtensions
            If String.Equals(ext, allowed, StringComparison.Ordinal) Then
                Return True
            End If
        Next

        Return False
    End Function

    Public Shared Function GetOpenFileDialogFilter() As String
        Return "Image files (*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp)|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|All files (*.*)|*.*"
    End Function

    ''' <summary>
    ''' Returns a clone of the product image, or Nothing when unavailable.
    ''' </summary>
    Public Shared Function TryLoadProductImage(relativePath As String) As Image
        Dim fullPath As String = ResolveFullPath(relativePath)
        If fullPath Is Nothing OrElse Not File.Exists(fullPath) Then
            Return Nothing
        End If

        Using loaded As Image = Image.FromFile(fullPath)
            Return DirectCast(loaded.Clone(), Image)
        End Using
    End Function

    ''' <summary>
    ''' Loads an image from any readable file path (e.g. before save).
    ''' </summary>
    Public Shared Function TryLoadImageFile(filePath As String) As Image
        If String.IsNullOrWhiteSpace(filePath) OrElse Not File.Exists(filePath) Then
            Return Nothing
        End If

        Using loaded As Image = Image.FromFile(filePath)
            Return DirectCast(loaded.Clone(), Image)
        End Using
    End Function

    Public Shared Function ResolveFullPath(relativePath As String) As String
        If String.IsNullOrWhiteSpace(relativePath) Then
            Return Nothing
        End If

        If Path.IsPathRooted(relativePath) Then
            Return relativePath
        End If

        Return Path.Combine(AppContext.BaseDirectory, relativePath)
    End Function

    ''' <summary>
    ''' Copies the source image into ProductImages and returns the relative path stored in the database.
    ''' </summary>
    Public Shared Function SaveProductImage(productId As Integer, sourceFilePath As String, Optional existingRelativePath As String = Nothing) As String
        If productId <= 0 Then
            Throw New ArgumentOutOfRangeException(NameOf(productId))
        End If

        If String.IsNullOrWhiteSpace(sourceFilePath) OrElse Not File.Exists(sourceFilePath) Then
            Throw New FileNotFoundException("Product image file was not found.", sourceFilePath)
        End If

        If Not IsAllowedImageFile(sourceFilePath) Then
            Throw New InvalidOperationException("Unsupported image type. Use JPG, PNG, BMP, GIF, or WEBP.")
        End If

        EnsureImagesDirectory()

        Dim ext As String = Path.GetExtension(sourceFilePath).ToLowerInvariant()
        Dim fileName As String = productId.ToString() & ext
        Dim relativePath As String = Path.Combine(ImagesFolderName, fileName)
        Dim destinationPath As String = Path.Combine(AppContext.BaseDirectory, relativePath)

        If Not String.IsNullOrWhiteSpace(existingRelativePath) AndAlso
           Not String.Equals(NormalizeRelativePath(existingRelativePath), NormalizeRelativePath(relativePath), StringComparison.OrdinalIgnoreCase) Then
            DeleteProductImage(existingRelativePath)
        End If

        File.Copy(sourceFilePath, destinationPath, overwrite:=True)
        Return NormalizeRelativePath(relativePath)
    End Function

    Public Shared Sub DeleteProductImage(relativePath As String)
        Dim fullPath As String = ResolveFullPath(relativePath)
        If fullPath Is Nothing OrElse Not File.Exists(fullPath) Then
            Return
        End If

        Try
            File.Delete(fullPath)
        Catch
        End Try
    End Sub

    Private Shared Function NormalizeRelativePath(relativePath As String) As String
        Return relativePath.Replace("/"c, "\"c)
    End Function

End Class
