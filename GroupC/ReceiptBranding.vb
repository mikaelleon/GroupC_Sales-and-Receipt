Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports System.Text

''' <summary>
''' Receipt header logo and text layout helpers.
''' </summary>
Public NotInheritable Class ReceiptBranding

    Private Shared ReadOnly SyncRoot As New Object()
    Private Shared cachedLogo As Image
    Private Shared loadAttempted As Boolean

    Private Const LogoRelativePath As String = "Assets\ReceiptLogo.png"
    Private Const EmbeddedLogoSuffix As String = "ReceiptLogo.png"
    Private Const ReceiptLineWidth As Integer = 40

    ''' <summary>Fixed width of the receipt column in the preview (pixels).</summary>
    Public Const ReceiptContentWidth As Integer = 400

    ''' <summary>Height reserved for the logo above receipt text in the preview.</summary>
    Public Const ReceiptLogoHeight As Integer = 96

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Store name as printed on the receipt (e.g. International Bookstore).
    ''' </summary>
    Public Shared Function FormatStoreTitle(Optional storeName As String = Nothing) As String
        Dim value As String = If(storeName, String.Empty).Trim()
        If value.Length = 0 Then
            value = AppSettings.Current.StoreName
        End If

        If value.Length = 0 Then
            Return AppBranding.ApplicationName
        End If

        If String.Equals(value, value.ToUpperInvariant(), StringComparison.Ordinal) AndAlso
           Not String.Equals(value, value.ToLowerInvariant(), StringComparison.Ordinal) Then
            Return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLowerInvariant())
        End If

        Return value
    End Function

    ''' <summary>
    ''' Parses the store title from saved receipt text.
    ''' </summary>
    Public Shared Function GetStoreTitle(Optional receiptText As String = Nothing) As String
        Dim fromText As String = TryParseStoreTitleFromReceipt(receiptText)
        If Not String.IsNullOrWhiteSpace(fromText) Then
            Return FormatStoreTitle(fromText)
        End If

        Return FormatStoreTitle()
    End Function

    ''' <summary>
    ''' Standard receipt header: separator, centered store name, separator.
    ''' </summary>
    Public Shared Function FormatReceiptHeader(Optional storeName As String = Nothing) As String
        Dim title As String = FormatStoreTitle(storeName).ToUpperInvariant()
        Return New String("="c, ReceiptLineWidth) & vbLf &
               CenterText(title) & vbLf &
               New String("="c, ReceiptLineWidth)
    End Function

    ''' <summary>
    ''' Centers text within the standard receipt width.
    ''' </summary>
    Public Shared Function CenterText(text As String) As String
        Dim value As String = If(text, String.Empty).Trim()
        If value.Length = 0 Then
            Return String.Empty
        End If

        If value.Length >= ReceiptLineWidth Then
            Return value.Substring(0, ReceiptLineWidth)
        End If

        Dim pad As Integer = (ReceiptLineWidth - value.Length) \ 2
        Return New String(" "c, pad) & value
    End Function

    ''' <summary>
    ''' Returns the receipt logo for display, print, and PDF export.
    ''' </summary>
    Public Shared Function TryGetReceiptLogo() As Image
        SyncLock SyncRoot
            If cachedLogo IsNot Nothing Then
                Return DirectCast(cachedLogo.Clone(), Image)
            End If

            If loadAttempted Then
                Return Nothing
            End If

            loadAttempted = True
            cachedLogo = LoadLogoFromEmbeddedResource()
            If cachedLogo Is Nothing Then
                cachedLogo = LoadLogoFromFile()
            End If

            If cachedLogo Is Nothing Then
                Return Nothing
            End If

            Return DirectCast(cachedLogo.Clone(), Image)
        End SyncLock
    End Function

    ''' <summary>
    ''' Full normalized receipt text (includes store name line for text export).
    ''' </summary>
    Public Shared Function GetReceiptText(receiptText As String) As String
        If String.IsNullOrEmpty(receiptText) Then
            Return String.Empty
        End If

        Dim normalized As String = receiptText.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf)
        Return NormalizeLegacyReceiptHeader(normalized)
    End Function

    ''' <summary>
    ''' Receipt body for logo layouts (same as full text; header is kept for column alignment).
    ''' </summary>
    Public Shared Function GetReceiptTextBelowLogo(receiptText As String) As String
        Return GetReceiptText(receiptText)
    End Function

    Private Shared Function NormalizeLegacyReceiptHeader(text As String) As String
        Dim lines As String() = text.Split(ChrW(10))
        If lines.Length < 3 Then
            Return text
        End If

        Dim bodyStart As Integer = FindReceiptBodyStart(lines)
        If bodyStart < 0 Then
            Return text
        End If

        Dim storeName As String = TryParseStoreTitleFromReceipt(text)
        If String.IsNullOrWhiteSpace(storeName) Then
            Return text
        End If

        If bodyStart >= 3 AndAlso
           IsSeparatorLine(lines(0)) AndAlso
           Not IsSeparatorLine(lines(1)) AndAlso
           IsSeparatorLine(lines(2)) AndAlso
           String.Equals(lines(1).Trim(), CenterText(FormatStoreTitle(storeName).ToUpperInvariant()), StringComparison.Ordinal) Then
            Return text.TrimEnd(ChrW(10), ChrW(13))
        End If

        Dim rebuilt As New StringBuilder()
        rebuilt.AppendLine(FormatReceiptHeader(storeName))

        For i As Integer = bodyStart To lines.Length - 1
            rebuilt.AppendLine(lines(i))
        Next

        Return rebuilt.ToString().TrimEnd(ChrW(10), ChrW(13))
    End Function

    Private Shared Function FindReceiptBodyStart(lines As String()) As Integer
        For i As Integer = 0 To lines.Length - 1
            If lines(i).Trim().StartsWith("Date:", StringComparison.OrdinalIgnoreCase) Then
                Return i
            End If
        Next

        Return -1
    End Function

    Private Shared Function TryParseStoreTitleFromReceipt(receiptText As String) As String
        If String.IsNullOrWhiteSpace(receiptText) Then
            Return String.Empty
        End If

        Dim lines As String() = receiptText.Replace(vbCrLf, vbLf).Split(ChrW(10))
        For Each line As String In lines
            Dim trimmed As String = line.Trim()
            If trimmed.Length = 0 Then
                Continue For
            End If

            If IsSeparatorLine(trimmed) Then
                Continue For
            End If

            If trimmed.StartsWith("Date:", StringComparison.OrdinalIgnoreCase) Then
                Return String.Empty
            End If

            Return trimmed
        Next

        Return String.Empty
    End Function

    Private Shared Function IsSeparatorLine(line As String) As Boolean
        Dim trimmed As String = If(line, String.Empty).Trim()
        Return trimmed.Length > 0 AndAlso trimmed.Replace("="c, String.Empty).Length = 0
    End Function

    Private Shared Function LoadLogoFromEmbeddedResource() As Image
        Dim assembly As Assembly = Assembly.GetExecutingAssembly()
        For Each resourceName As String In assembly.GetManifestResourceNames()
            If resourceName.EndsWith(EmbeddedLogoSuffix, StringComparison.OrdinalIgnoreCase) Then
                Using stream As Stream = assembly.GetManifestResourceStream(resourceName)
                    If stream IsNot Nothing Then
                        Return Image.FromStream(stream)
                    End If
                End Using
            End If
        Next

        Return Nothing
    End Function

    Private Shared Function LoadLogoFromFile() As Image
        Dim path As String = System.IO.Path.Combine(AppContext.BaseDirectory, LogoRelativePath)
        If Not File.Exists(path) Then
            Return Nothing
        End If

        Using stream As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
            Return Image.FromStream(stream)
        End Using
    End Function

End Class
