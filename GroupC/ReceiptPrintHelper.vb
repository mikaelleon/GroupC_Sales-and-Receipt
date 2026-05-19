Imports System.Drawing
Imports System.Drawing.Printing

Public NotInheritable Class ReceiptPrintHelper

    Private ReadOnly _lines As String()
    Private ReadOnly _logo As Image
    Private _lineIndex As Integer
    Private _headerPrinted As Boolean

    Public Sub New(receiptText As String)
        Me.New(receiptText, ReceiptBranding.TryGetReceiptLogo())
    End Sub

    Public Sub New(receiptText As String, logo As Image)
        If receiptText Is Nothing Then
            receiptText = String.Empty
        End If

        _logo = logo
        Dim normalized As String = ReceiptBranding.GetReceiptText(receiptText).Replace(vbCrLf, vbLf).Replace(vbCr, vbLf)
        _lines = normalized.Split(ChrW(10))
    End Sub

    Public Sub BeginPrint()
        _lineIndex = 0
        _headerPrinted = False
    End Sub

    Public Sub PrintPage(e As PrintPageEventArgs, font As Font)
        Dim margin As RectangleF = e.MarginBounds
        Dim y As Single = margin.Top
        Dim format As New StringFormat(StringFormatFlags.LineLimit)

        If Not _headerPrinted Then
            y = DrawLogo(e.Graphics, margin, y)
            _headerPrinted = True
        End If

        While _lineIndex < _lines.Length
            Dim line As String = _lines(_lineIndex)
            Dim layout As New RectangleF(margin.Left, y, margin.Width, margin.Bottom - y)
            Dim measured As SizeF = e.Graphics.MeasureString(line, font, New SizeF(margin.Width, Single.MaxValue), format)
            Dim lineHeight As Single = measured.Height
            If lineHeight < font.GetHeight(e.Graphics) Then
                lineHeight = font.GetHeight(e.Graphics)
            End If

            If y + lineHeight > margin.Bottom Then
                e.HasMorePages = True
                Return
            End If

            e.Graphics.DrawString(line, font, Brushes.Black, New RectangleF(margin.Left, y, margin.Width, lineHeight), format)
            y += lineHeight
            _lineIndex += 1
        End While

        e.HasMorePages = False
    End Sub

    Private Function DrawLogo(g As Graphics, margin As RectangleF, y As Single) As Single
        If _logo Is Nothing Then
            Return y
        End If

        Dim maxLogoWidth As Single = margin.Width * 0.75F
        Dim maxLogoHeight As Single = 72.0F
        Dim scale As Single = Math.Min(maxLogoWidth / _logo.Width, maxLogoHeight / _logo.Height)
        Dim drawWidth As Single = _logo.Width * scale
        Dim drawHeight As Single = _logo.Height * scale
        Dim x As Single = margin.Left + (margin.Width - drawWidth) / 2.0F
        g.DrawImage(_logo, x, y, drawWidth, drawHeight)
        Return y + drawHeight + 8.0F
    End Function

End Class
