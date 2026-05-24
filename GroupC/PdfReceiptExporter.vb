Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports PdfSharp
Imports PdfSharp.Drawing
Imports PdfSharp.Pdf

''' <summary>
''' Exports receipt text to PDF using PDFsharp, with optional branded header logo.
''' </summary>
Public NotInheritable Class PdfReceiptExporter

    Private Const MarginPt As Double = 36
    Private Const LineHeightPt As Double = 14
    Private Const BlankLineHeightPt As Double = 8
    Private Const LogoMaxWidthPt As Double = 220
    Private Const LogoMaxHeightPt As Double = 70

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Writes receipt text to a PDF file with the store logo when available.
    ''' </summary>
    Public Shared Sub ExportTextToPdf(filePath As String, text As String)
        If String.IsNullOrWhiteSpace(filePath) Then
            Throw New ArgumentException("Path is required.", NameOf(filePath))
        End If

        Dim body As String = ReceiptBranding.GetReceiptText(If(text, String.Empty))

        Using logo As Image = ReceiptBranding.TryGetReceiptLogo()
            ExportToPdf(filePath, body, logo)
        End Using
    End Sub

    Private Shared Sub ExportToPdf(filePath As String, body As String, logo As Image)
        Dim document As New PdfDocument()
        document.Info.Title = "Receipt"

        Dim page As PdfPage = document.AddPage()
        page.Size = PageSize.A4
        Dim gfx As XGraphics = XGraphics.FromPdfPage(page)
        Dim font As New XFont("Courier New", 10, XFontStyleEx.Regular)
        Dim brush As XBrush = XBrushes.Black
        Dim y As Double = MarginPt
        Dim maxY As Double = page.Height.Point - MarginPt
        Dim contentWidth As Double = page.Width.Point - (MarginPt * 2)

        y = DrawLogo(gfx, logo, MarginPt, y, contentWidth)
        If logo IsNot Nothing Then
            y += 4
        End If

        Dim normalized As String = body.Replace(vbCrLf, vbLf)
        For Each line As String In normalized.Split(ChrW(10))
            Dim stepPt As Double = If(String.IsNullOrWhiteSpace(line), BlankLineHeightPt, LineHeightPt)
            If y + stepPt > maxY Then
                gfx.Dispose()
                page = document.AddPage()
                page.Size = PageSize.A4
                gfx = XGraphics.FromPdfPage(page)
                y = MarginPt
                maxY = page.Height.Point - MarginPt
            End If

            If Not String.IsNullOrWhiteSpace(line) Then
                Dim lineSize As XSize = gfx.MeasureString(line, font)
                Dim x As Double = MarginPt + Math.Max(0.0, (contentWidth - lineSize.Width) / 2.0)
                gfx.DrawString(line, font, brush, x, y)
            End If

            y += stepPt
        Next

        gfx.Dispose()

        Dim folder As String = System.IO.Path.GetDirectoryName(filePath)
        If Not String.IsNullOrEmpty(folder) Then
            Directory.CreateDirectory(folder)
        End If

        document.Save(filePath)
        document.Close()
    End Sub

    Private Shared Function DrawLogo(gfx As XGraphics, logo As Image, left As Double, y As Double, contentWidth As Double) As Double
        If logo Is Nothing Then
            Return y
        End If

        Using stream As New MemoryStream()
            logo.Save(stream, ImageFormat.Png)
            stream.Position = 0
            Dim xImage As XImage = XImage.FromStream(stream)
            Dim scale As Double = Math.Min(LogoMaxWidthPt / xImage.PixelWidth, LogoMaxHeightPt / xImage.PixelHeight)
            Dim drawWidth As Double = xImage.PixelWidth * scale
            Dim drawHeight As Double = xImage.PixelHeight * scale
            Dim x As Double = left + (contentWidth - drawWidth) / 2.0
            gfx.DrawImage(xImage, x, y, drawWidth, drawHeight)
            y += drawHeight + 8
        End Using

        Return y
    End Function

End Class
