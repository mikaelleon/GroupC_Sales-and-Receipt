Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports System.Text
Imports System.Windows.Forms

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

    ''' <summary>Vertical spacing multiplier for receipt preview line height.</summary>
    Public Const PreviewLineSpacingScale As Single = 1.45F

    ''' <summary>Minimum receipt preview paper height in pixels.</summary>
    Public Const PreviewMinPaperHeight As Integer = 560

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
    ''' Centers every line of receipt text within the standard receipt column width.
    ''' </summary>
    Public Shared Function CenterReceiptLines(receiptText As String) As String
        If String.IsNullOrEmpty(receiptText) Then
            Return String.Empty
        End If

        Dim normalized As String = receiptText.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf)
        Dim lines As String() = normalized.Split(ChrW(10))
        Dim centered As New StringBuilder()

        For i As Integer = 0 To lines.Length - 1
            Dim line As String = lines(i)
            If line.Length = 0 Then
                centered.AppendLine()
                Continue For
            End If

            centered.Append(CenterText(line))
            If i < lines.Length - 1 Then
                centered.AppendLine()
            End If
        Next

        Return centered.ToString()
    End Function

    ''' <summary>
    ''' StringFormat for print/PDF: each line centered in the content area.
    ''' </summary>
    Public Shared Function CreateReceiptDrawFormat() As StringFormat
        Return New StringFormat(StringFormatFlags.LineLimit) With {
            .Alignment = StringAlignment.Center,
            .Trimming = StringTrimming.None
        }
    End Function

    ''' <summary>
    ''' Builds the full monospace receipt from a sale snapshot.
    ''' </summary>
    Public Shared Function BuildReceiptText(snapshot As ReceiptSnapshot) As String
        If snapshot Is Nothing Then
            Return String.Empty
        End If

        Dim settings As AppSettingsData = AppSettings.Current
        Dim sym As String = If(snapshot.CurrencySymbol, settings.CurrencySymbol)
        Dim whenPrinted As DateTime = NormalizeStoredSaleDate(
            If(snapshot.SaleDateTime = DateTime.MinValue, DateTime.UtcNow, snapshot.SaleDateTime))
        Dim receipt As New StringBuilder()

        AppendBlank(receipt)
        receipt.AppendLine(FormatReceiptHeader(snapshot.StoreName))
        AppendBlank(receipt)
        AppendCentered(receipt, settings.StoreBranch)
        AppendCentered(receipt, settings.StoreLocation)
        AppendBlank(receipt)

        If Not String.IsNullOrWhiteSpace(snapshot.ReceiptNumber) Then
            AppendCentered(receipt, "Receipt No: " & snapshot.ReceiptNumber.Trim())
        End If

        If Not String.IsNullOrWhiteSpace(snapshot.TransactionReference) Then
            AppendCentered(receipt, "Transaction: " & snapshot.TransactionReference.Trim())
        ElseIf snapshot.SaleId > 0 Then
            AppendCentered(receipt, "Transaction: TXN" & whenPrinted.ToString("yyyyMMdd", CultureInfo.InvariantCulture) &
                           "-" & snapshot.SaleId.ToString("D6", CultureInfo.InvariantCulture))
        End If

        AppendBlank(receipt)
        AppendSectionRule(receipt, "TRANSACTION DETAILS")
        AppendBlank(receipt)
        AppendCentered(receipt, "Date & Time: " & FormatReceiptDateTime(whenPrinted))

        If Not String.IsNullOrWhiteSpace(snapshot.CashierName) Then
            AppendCentered(receipt, "Cashier: " & snapshot.CashierName.Trim())
        End If

        AppendBlank(receipt)
        AppendSectionRule(receipt, "ITEMS PURCHASED")
        receipt.AppendLine(CenterText("Item              Qty   Price    Subtotal"))
        receipt.AppendLine(New String("-"c, ReceiptLineWidth))

        If snapshot.Lines IsNot Nothing Then
            For Each lineRow As ReceiptLineRow In snapshot.Lines
                Dim itemName As String = lineRow.ProductName
                If itemName.Length > 18 Then
                    itemName = itemName.Substring(0, 18)
                End If

                Dim qtyStr As String = lineRow.Quantity.ToString(CultureInfo.CurrentCulture)
                Dim priceStr As String = sym & lineRow.UnitPrice.ToString("N2", CultureInfo.CurrentCulture)
                Dim subStr As String = sym & lineRow.LineTotal.ToString("N2", CultureInfo.CurrentCulture)
                Dim row As String = itemName.PadRight(18) &
                                    qtyStr.PadLeft(3) & " " &
                                    priceStr.PadLeft(8) & " " &
                                    subStr.PadLeft(9)
                receipt.AppendLine(CenterText(row))
            Next
        End If

        AppendBlank(receipt)
        AppendSectionRule(receipt, "PRICING BREAKDOWN")
        AppendBlank(receipt)
        AppendAmountLine(receipt, "Subtotal:", sym, snapshot.SubtotalBeforeDiscount)

        If snapshot.DiscountAmount > 0D Then
            Dim discLabel As String
            If Not String.IsNullOrWhiteSpace(snapshot.DiscountLabel) Then
                discLabel = "Discount (" & snapshot.DiscountLabel & "):"
            ElseIf snapshot.DiscountIsPercent Then
                discLabel = "Discount (" & snapshot.DiscountPercent.ToString("N2", CultureInfo.CurrentCulture) & "%):"
            Else
                discLabel = "Discount (fixed):"
            End If

            AppendAmountLine(receipt, discLabel, sym, -snapshot.DiscountAmount)
        End If

        If snapshot.TaxApplied AndAlso snapshot.TaxAmount > 0D Then
            AppendAmountLine(receipt,
                             "Tax (" & snapshot.TaxPercent.ToString("N2", CultureInfo.CurrentCulture) & "%):",
                             sym,
                             snapshot.TaxAmount)
        End If

        AppendBlank(receipt)
        AppendAmountLine(receipt, "TOTAL DUE:", sym, snapshot.GrandTotal)

        AppendBlank(receipt)
        AppendSectionRule(receipt, "PAYMENT")
        AppendBlank(receipt)

        Dim paymentMethod As String = If(snapshot.PaymentMethod, String.Empty).Trim()
        If paymentMethod.Length = 0 Then
            paymentMethod = "Cash"
        End If

        AppendCentered(receipt, "Method: " & paymentMethod)
        AppendAmountLine(receipt, "Tendered:", sym, snapshot.AmountTendered)
        AppendAmountLine(receipt, "Change:", sym, snapshot.ChangeGiven)

        AppendBlank(receipt)
        receipt.AppendLine(New String("="c, ReceiptLineWidth))
        AppendCentered(receipt, "FOOTER")
        receipt.AppendLine(New String("="c, ReceiptLineWidth))
        AppendBlank(receipt)
        AppendWrappedCentered(receipt, "Customer Service: " & settings.CustomerServiceInfo)
        AppendWrappedCentered(receipt, "Returns: " & settings.ReturnPolicyText)
        AppendWrappedCentered(receipt, "Terms: " & settings.TermsText)
        AppendBlank(receipt)
        AppendCentered(receipt, If(snapshot.FooterText, settings.ReceiptFooter))
        AppendBlank(receipt)

        Dim barcodeRef As String = If(snapshot.ReceiptNumber, String.Empty).Trim()
        If barcodeRef.Length = 0 AndAlso snapshot.SaleId > 0 Then
            barcodeRef = "RCP-" & snapshot.SaleId.ToString("D6", CultureInfo.InvariantCulture)
        End If

        If barcodeRef.Length > 0 Then
            AppendCentered(receipt, "|" & barcodeRef & "|")
            AppendCentered(receipt, "[QR / barcode for inventory]")
        End If

        AppendBlank(receipt)
        receipt.AppendLine(New String("="c, ReceiptLineWidth))

        Return CenterReceiptLines(receipt.ToString())
    End Function

    Public Shared Function FormatReceiptNumber(saleId As Integer) As String
        Return "RCP-" & saleId.ToString("D6", CultureInfo.InvariantCulture)
    End Function

    Public Shared Function FormatTransactionReference(saleId As Integer, whenUtc As DateTime) As String
        Return "TXN" & whenUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture) &
               "-" & saleId.ToString("D6", CultureInfo.InvariantCulture)
    End Function

    ''' <summary>
    ''' Centers receipt paragraphs in a read-only preview RichTextBox.
    ''' </summary>
    Public Shared Sub ApplyPreviewCenterAlignment(rtb As RichTextBox)
        If rtb Is Nothing OrElse rtb.TextLength = 0 Then
            Return
        End If

        rtb.ForeColor = Color.Black
        rtb.SelectAll()
        rtb.SelectionColor = Color.Black
        rtb.SelectionFont = New Font(rtb.Font, FontStyle.Regular)
        rtb.SelectionAlignment = HorizontalAlignment.Center
        rtb.Select(0, 0)
    End Sub

    ''' <summary>
    ''' Colors receipt preview lines by section for visual hierarchy.
    ''' </summary>
    Public Shared Sub ApplyPreviewSectionColors(rtb As RichTextBox)
        If rtb Is Nothing OrElse rtb.TextLength = 0 Then
            Return
        End If

        Dim headerColor As Color = Color.FromArgb(23, 74, 124)
        Dim sectionColor As Color = Color.FromArgb(0, 102, 153)
        Dim itemsColor As Color = Color.FromArgb(45, 55, 72)
        Dim pricingColor As Color = Color.FromArgb(180, 83, 9)
        Dim paymentColor As Color = Color.FromArgb(22, 128, 57)
        Dim footerColor As Color = Color.FromArgb(107, 114, 128)
        Dim separatorColor As Color = Color.FromArgb(156, 163, 175)

        Dim normalized As String = rtb.Text.Replace(vbCrLf, vbLf)
        Dim lines As String() = normalized.Split(ChrW(10))
        Dim index As Integer = 0
        Dim section As String = "header"

        For Each line As String In lines
            Dim trimmed As String = line.Trim()
            Dim lineColor As Color = itemsColor
            Dim boldSection As Boolean = False

            If trimmed.Length = 0 Then
                lineColor = separatorColor
            ElseIf IsSeparatorLine(trimmed) Then
                lineColor = separatorColor
            ElseIf trimmed.Contains("="c) AndAlso trimmed.Replace("="c, String.Empty).Length = 0 Then
                lineColor = separatorColor
            ElseIf trimmed.Contains("-"c) AndAlso trimmed.Replace("-"c, String.Empty).Length = 0 Then
                lineColor = separatorColor
            ElseIf String.Equals(trimmed, "TRANSACTION DETAILS", StringComparison.OrdinalIgnoreCase) OrElse
                   String.Equals(trimmed, "ITEMS PURCHASED", StringComparison.OrdinalIgnoreCase) OrElse
                   String.Equals(trimmed, "PRICING BREAKDOWN", StringComparison.OrdinalIgnoreCase) OrElse
                   String.Equals(trimmed, "PAYMENT", StringComparison.OrdinalIgnoreCase) OrElse
                   String.Equals(trimmed, "FOOTER", StringComparison.OrdinalIgnoreCase) Then
                lineColor = sectionColor
                boldSection = True
                section = trimmed.ToUpperInvariant()
            ElseIf trimmed.StartsWith("Receipt No:", StringComparison.OrdinalIgnoreCase) OrElse
                   trimmed.StartsWith("Transaction:", StringComparison.OrdinalIgnoreCase) Then
                lineColor = headerColor
            ElseIf section = "PRICING BREAKDOWN" OrElse
                   trimmed.StartsWith("Subtotal:", StringComparison.OrdinalIgnoreCase) OrElse
                   trimmed.StartsWith("Discount", StringComparison.OrdinalIgnoreCase) OrElse
                   trimmed.StartsWith("Tax (", StringComparison.OrdinalIgnoreCase) OrElse
                   trimmed.StartsWith("TOTAL DUE:", StringComparison.OrdinalIgnoreCase) Then
                lineColor = pricingColor
            ElseIf section = "PAYMENT" OrElse
                   trimmed.StartsWith("Method:", StringComparison.OrdinalIgnoreCase) OrElse
                   trimmed.StartsWith("Tendered:", StringComparison.OrdinalIgnoreCase) OrElse
                   trimmed.StartsWith("Change:", StringComparison.OrdinalIgnoreCase) Then
                lineColor = paymentColor
            ElseIf section = "FOOTER" OrElse
                   trimmed.StartsWith("Customer Service:", StringComparison.OrdinalIgnoreCase) OrElse
                   trimmed.StartsWith("Returns:", StringComparison.OrdinalIgnoreCase) OrElse
                   trimmed.StartsWith("Terms:", StringComparison.OrdinalIgnoreCase) OrElse
                   trimmed.StartsWith("Thank you", StringComparison.OrdinalIgnoreCase) OrElse
                   trimmed.StartsWith("[QR", StringComparison.OrdinalIgnoreCase) OrElse
                   (trimmed.StartsWith("|", StringComparison.Ordinal) AndAlso trimmed.EndsWith("|", StringComparison.Ordinal)) Then
                lineColor = footerColor
            ElseIf section = "TRANSACTION DETAILS" AndAlso
                   (trimmed.StartsWith("Date", StringComparison.OrdinalIgnoreCase) OrElse
                    trimmed.StartsWith("Cashier:", StringComparison.OrdinalIgnoreCase)) Then
                lineColor = headerColor
            ElseIf section = "ITEMS PURCHASED" Then
                lineColor = itemsColor
                If trimmed.StartsWith("Item", StringComparison.OrdinalIgnoreCase) Then
                    boldSection = True
                End If
            End If

            rtb.Select(index, line.Length)
            rtb.SelectionColor = lineColor
            If boldSection Then
                rtb.SelectionFont = New Font(rtb.Font, FontStyle.Bold)
            Else
                rtb.SelectionFont = New Font(rtb.Font, FontStyle.Regular)
            End If

            index += line.Length + 1
        Next

        rtb.Select(0, 0)
    End Sub

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
    ''' Converts a sale_date value from SQL Server (UTC via SYSUTCDATETIME) to local display time.
    ''' </summary>
    Public Shared Function NormalizeStoredSaleDate(stored As DateTime) As DateTime
        If stored = DateTime.MinValue Then
            Return stored
        End If

        Select Case stored.Kind
            Case DateTimeKind.Utc
                Return stored.ToLocalTime()
            Case DateTimeKind.Local
                Return stored
            Case Else
                Return DateTime.SpecifyKind(stored, DateTimeKind.Utc).ToLocalTime()
        End Select
    End Function

    Public Shared Function FormatReceiptDateTime(saleWhen As DateTime) As String
        Return NormalizeStoredSaleDate(saleWhen).ToString("MMMM dd, yyyy hh:mm tt", CultureInfo.CurrentCulture)
    End Function

    ''' <summary>
    ''' Replaces the receipt's date/time line so it matches the authoritative sale_date from the database.
    ''' </summary>
    Public Shared Function AlignReceiptDateLine(receiptText As String, saleDate As DateTime) As String
        If String.IsNullOrWhiteSpace(receiptText) OrElse saleDate = DateTime.MinValue Then
            Return receiptText
        End If

        Dim normalized As String = receiptText.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf)
        Dim lines As String() = normalized.Split(ChrW(10))
        Dim dateLine As String = CenterText("Date & Time: " & FormatReceiptDateTime(saleDate))

        For i As Integer = 0 To lines.Length - 1
            Dim trimmed As String = lines(i).Trim()
            If trimmed.StartsWith("Date & Time:", StringComparison.OrdinalIgnoreCase) OrElse
               trimmed.StartsWith("Date:", StringComparison.OrdinalIgnoreCase) Then
                lines(i) = dateLine
                Return String.Join(Environment.NewLine, lines)
            End If
        Next

        Return receiptText
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
            Dim trimmed As String = lines(i).Trim()
            If trimmed.StartsWith("Date:", StringComparison.OrdinalIgnoreCase) OrElse
               trimmed.StartsWith("Date & Time:", StringComparison.OrdinalIgnoreCase) OrElse
               trimmed.StartsWith("Receipt No:", StringComparison.OrdinalIgnoreCase) Then
                Return i
            End If
        Next

        Return -1
    End Function

    Private Shared Sub AppendBlank(receipt As StringBuilder)
        receipt.AppendLine()
    End Sub

    Private Shared Sub AppendCentered(receipt As StringBuilder, text As String)
        If String.IsNullOrWhiteSpace(text) Then
            Return
        End If

        receipt.AppendLine(CenterText(text.Trim()))
    End Sub

    Private Shared Sub AppendSectionRule(receipt As StringBuilder, title As String)
        receipt.AppendLine(CenterText(title))
        receipt.AppendLine(New String("-"c, ReceiptLineWidth))
    End Sub

    Private Shared Sub AppendAmountLine(receipt As StringBuilder, label As String, currencySymbol As String, amount As Decimal)
        Dim sym As String = If(currencySymbol, String.Empty)
        Dim valueText As String = sym & Math.Abs(amount).ToString("N2", CultureInfo.CurrentCulture)
        If amount < 0D Then
            valueText = "-" & valueText
        End If

        Dim row As String = label.PadRight(22) & valueText.PadLeft(18)
        receipt.AppendLine(CenterText(row))
    End Sub

    Private Shared Sub AppendWrappedCentered(receipt As StringBuilder, text As String)
        Dim value As String = If(text, String.Empty).Trim()
        If value.Length = 0 Then
            Return
        End If

        Dim start As Integer = 0
        While start < value.Length
            Dim take As Integer = Math.Min(ReceiptLineWidth, value.Length - start)
            If take < ReceiptLineWidth AndAlso start > 0 Then
                receipt.AppendLine(CenterText(value.Substring(start)))
                Exit While
            End If

            If take = ReceiptLineWidth AndAlso start + take < value.Length Then
                Dim breakAt As Integer = value.LastIndexOf(" "c, start + take - 1, take)
                If breakAt > start Then
                    take = breakAt - start
                End If
            End If

            receipt.AppendLine(CenterText(value.Substring(start, take).Trim()))
            start += take
            If start < value.Length AndAlso value(start) = " "c Then
                start += 1
            End If
        End While
    End Sub

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
