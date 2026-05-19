Imports System.Collections.Generic
Imports System.Globalization
Imports System.Windows.Forms

''' <summary>
''' Shared DataGridView display rules: hide internal IDs and show status as emoji instead of checkboxes.
''' </summary>
Public NotInheritable Class GridDisplayHelper

    Private Const ActiveEmoji As String = "✅"
    Private Const InactiveEmoji As String = "❌"

    Private Shared ReadOnly WiredGrids As New HashSet(Of DataGridView)()

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Hides id / *_id / LogID columns while keeping them available for code-behind.
    ''' </summary>
    Public Shared Sub HideInternalIdColumns(dgv As DataGridView)
        If dgv Is Nothing OrElse dgv.Columns.Count = 0 Then
            Return
        End If

        For Each col As DataGridViewColumn In dgv.Columns
            Dim name As String = col.Name
            If String.IsNullOrEmpty(name) Then
                Continue For
            End If

            Dim lower As String = name.ToLowerInvariant()
            If lower = "id" OrElse lower = "logid" OrElse lower.EndsWith("_id", StringComparison.Ordinal) Then
                col.Visible = False
            End If
        Next
    End Sub

    ''' <summary>
    ''' Replaces a bound boolean status column with centered emoji text.
    ''' </summary>
    Public Shared Sub ConfigureActiveStatusDisplay(dgv As DataGridView, columnName As String)
        If dgv Is Nothing OrElse Not dgv.Columns.Contains(columnName) Then
            Return
        End If

        Dim existing As DataGridViewColumn = dgv.Columns(columnName)
        If TypeOf existing Is DataGridViewCheckBoxColumn Then
            Dim idx As Integer = existing.Index
            Dim header As String = existing.HeaderText
            Dim width As Integer = existing.Width
            Dim prop As String = existing.DataPropertyName
            dgv.Columns.Remove(existing)

            Dim textCol As New DataGridViewTextBoxColumn() With {
                .Name = columnName,
                .DataPropertyName = prop,
                .HeaderText = If(String.IsNullOrWhiteSpace(header), "Active", header),
                .ReadOnly = True,
                .Width = Math.Max(width, 56),
                .SortMode = DataGridViewColumnSortMode.NotSortable
            }
            textCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            dgv.Columns.Insert(idx, textCol)
        Else
            existing.ReadOnly = True
            existing.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        End If

        WireActiveStatusFormatting(dgv)
    End Sub

    ''' <summary>
    ''' Applies ID hiding and active-status emoji for standard admin grids.
    ''' </summary>
    Public Shared Sub ApplyStandardBoundGridDisplay(dgv As DataGridView)
        HideInternalIdColumns(dgv)
        ConfigureActiveStatusDisplay(dgv, "is_active")
    End Sub

    Private Shared Sub WireActiveStatusFormatting(dgv As DataGridView)
        If WiredGrids.Contains(dgv) Then
            Return
        End If

        WiredGrids.Add(dgv)
        AddHandler dgv.CellFormatting, AddressOf ActiveStatus_CellFormatting
    End Sub

    Private Shared Sub ActiveStatus_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        If e.RowIndex < 0 Then
            Return
        End If

        Dim dgv As DataGridView = TryCast(sender, DataGridView)
        If dgv Is Nothing Then
            Return
        End If

        Dim col As DataGridViewColumn = dgv.Columns(e.ColumnIndex)
        If col Is Nothing Then
            Return
        End If

        Dim isActiveColumn As Boolean =
            String.Equals(col.Name, "is_active", StringComparison.OrdinalIgnoreCase) OrElse
            String.Equals(col.DataPropertyName, "is_active", StringComparison.OrdinalIgnoreCase)

        If Not isActiveColumn Then
            Return
        End If

        If e.Value Is Nothing OrElse e.Value Is DBNull.Value Then
            e.Value = InactiveEmoji
        ElseIf TypeOf e.Value Is Boolean Then
            e.Value = If(CBool(e.Value), ActiveEmoji, InactiveEmoji)
        Else
            Dim text As String = Convert.ToString(e.Value, CultureInfo.CurrentCulture)
            Dim asBool As Boolean
            If Boolean.TryParse(text, asBool) Then
                e.Value = If(asBool, ActiveEmoji, InactiveEmoji)
            ElseIf text = ActiveEmoji OrElse text = InactiveEmoji Then
                Return
            Else
                e.Value = InactiveEmoji
            End If
        End If

        e.FormattingApplied = True
    End Sub

End Class
