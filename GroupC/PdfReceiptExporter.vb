Imports System.IO
Imports PdfSharp
Imports PdfSharp.Drawing
Imports PdfSharp.Pdf

''' <summary>
''' Exports plain receipt text to a single- or multi-page PDF using PDFsharp.
''' </summary>
Public NotInheritable Class PdfReceiptExporter

    Private Const MarginPt As Double = 36
    Private Const LineHeightPt As Double = 12

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Writes receipt monospace text to a PDF file.
    ''' </summary>
    ''' <param name="filePath">Destination path (.pdf).</param>
    ''' <param name="text">Receipt body.</param>
    Public Shared Sub ExportTextToPdf(filePath As String, text As String)
        If String.IsNullOrWhiteSpace(filePath) Then
            Throw New ArgumentException("Path is required.", NameOf(filePath))
        End If

        Dim body As String = If(text, String.Empty)
        Dim document As New PdfDocument()
        document.Info.Title = "Receipt"

        Dim page As PdfPage = document.AddPage()
        page.Size = PageSize.A4
        Dim gfx As XGraphics = XGraphics.FromPdfPage(page)
        Dim font As New XFont("Courier New", 10, XFontStyleEx.Regular)
        Dim brush As XBrush = XBrushes.Black
        Dim y As Double = MarginPt
        Dim maxY As Double = page.Height.Point - MarginPt

        Dim normalized As String = body.Replace(vbCrLf, vbLf)
        For Each line As String In normalized.Split(ChrW(10))
            If y + LineHeightPt > maxY Then
                gfx.Dispose()
                page = document.AddPage()
                page.Size = PageSize.A4
                gfx = XGraphics.FromPdfPage(page)
                y = MarginPt
                maxY = page.Height.Point - MarginPt
            End If

            gfx.DrawString(line, font, brush, MarginPt, y)
            y += LineHeightPt
        Next

        gfx.Dispose()

        Dim folder As String = System.IO.Path.GetDirectoryName(filePath)
        If Not String.IsNullOrEmpty(folder) Then
            Directory.CreateDirectory(folder)
        End If

        document.Save(filePath)
        document.Close()
    End Sub

End Class
