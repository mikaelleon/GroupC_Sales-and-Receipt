Imports System.IO
Imports PdfSharp.Fonts

Public Class WindowsFontResolver
    Implements IFontResolver

    ' Step 1: Map the requested font to a face name
    Public Function ResolveTypeface(familyName As String, isBold As Boolean, isItalic As Boolean) As FontResolverInfo Implements IFontResolver.ResolveTypeface
        If familyName.Equals("Courier New", StringComparison.OrdinalIgnoreCase) Then
            If isBold AndAlso isItalic Then
                Return New FontResolverInfo("CourierNew-BoldItalic")
            ElseIf isBold Then
                Return New FontResolverInfo("CourierNew-Bold")
            ElseIf isItalic Then
                Return New FontResolverInfo("CourierNew-Italic")
            Else
                Return New FontResolverInfo("CourierNew-Regular")
            End If
        End If

        ' Fallback to regular if font isn't found
        Return New FontResolverInfo("CourierNew-Regular")
    End Function

    ' Step 2: Grab the actual .ttf file from the Windows Font folder
    Public Function GetFont(faceName As String) As Byte() Implements IFontResolver.GetFont
        Dim fontPath As String = Environment.GetFolderPath(Environment.SpecialFolder.Fonts)

        Select Case faceName
            Case "CourierNew-Bold"
                fontPath = Path.Combine(fontPath, "courbd.ttf")
            Case "CourierNew-Italic"
                fontPath = Path.Combine(fontPath, "couri.ttf")
            Case "CourierNew-BoldItalic"
                fontPath = Path.Combine(fontPath, "courbi.ttf")
            Case Else ' Regular
                fontPath = Path.Combine(fontPath, "cour.ttf")
        End Select

        If File.Exists(fontPath) Then
            Return File.ReadAllBytes(fontPath)
        End If

        Return Nothing
    End Function
End Class