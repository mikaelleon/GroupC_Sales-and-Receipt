Imports System.Drawing
Imports System.Drawing.Printing

Public NotInheritable Class ReceiptPrintHelper

    Private ReadOnly _lines As String()
    Private _lineIndex As Integer

    Public Sub New(receiptText As String)
        If receiptText Is Nothing Then
            receiptText = String.Empty
        End If

        _lines = receiptText.Replace(vbCrLf, vbLf).Split(ChrW(10))
    End Sub

    Public Sub BeginPrint()
        _lineIndex = 0
    End Sub

    Public Sub PrintPage(e As PrintPageEventArgs, font As Font)
        Dim margin As RectangleF = e.MarginBounds
        Dim y As Single = margin.Top
        Dim format As New StringFormat(StringFormatFlags.LineLimit)

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

End Class
