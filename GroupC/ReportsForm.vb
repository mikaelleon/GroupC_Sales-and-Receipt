Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient

''' <summary>
''' Simple sales summaries by date range (today stretch goals).
''' </summary>
Public Class ReportsForm
    Inherits Form

    Private WithEvents dtpFrom As DateTimePicker
    Private WithEvents dtpTo As DateTimePicker
    Private WithEvents btnRun As Button
    Private dgvDaily As DataGridView
    Private dgvTop As DataGridView
    Private lblSummary As Label

    Private Sub ReportsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "Group C - Reports"
        Me.MinimumSize = New Size(720, 520)
        Me.Size = New Size(800, 580)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Font = New Font("Segoe UI", 10)
        Me.BackColor = Color.White

        Try
            DatabaseInitializer.EnsureDatabase()
        Catch
        End Try

        Dim root As New TableLayoutPanel()
        root.Dock = DockStyle.Fill
        root.Padding = New Padding(12)
        root.ColumnCount = 1
        root.RowCount = 4
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 45.0F))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 55.0F))

        Dim title As New Label() With {
            .Text = "SALES REPORTS",
            .Font = New Font("Segoe UI", 14.0F, FontStyle.Bold),
            .ForeColor = UiTheme.Navy,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter,
            .AutoSize = True
        }

        Dim filter As New TableLayoutPanel() With {.AutoSize = True, .ColumnCount = 7, .RowCount = 1}
        filter.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        filter.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        filter.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        filter.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        filter.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        filter.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        filter.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))

        dtpFrom = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 120, .Value = DateTime.Today.AddDays(-7)}
        dtpTo = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 120, .Value = DateTime.Today}
        btnRun = New Button() With {.Text = "&Run report", .AutoSize = True}
        UiTheme.ApplyPrimaryButton(btnRun)

        filter.Controls.Add(New Label() With {.Text = "From", .AutoSize = True, .Margin = New Padding(0, 6, 8, 6)}, 0, 0)
        filter.Controls.Add(dtpFrom, 1, 0)
        filter.Controls.Add(New Label() With {.Text = "To", .AutoSize = True, .Margin = New Padding(12, 6, 8, 6)}, 2, 0)
        filter.Controls.Add(dtpTo, 3, 0)
        filter.Controls.Add(btnRun, 4, 0)

        lblSummary = New Label() With {.AutoSize = True, .ForeColor = Color.DimGray, .Margin = New Padding(0, 8, 0, 4)}

        dgvDaily = New DataGridView() With {.Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False, .RowHeadersVisible = False, .BackgroundColor = Color.White}
        dgvTop = New DataGridView() With {.Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False, .RowHeadersVisible = False, .BackgroundColor = Color.White}

        Dim split As New SplitContainer() With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal, .SplitterDistance = 220}
        Dim p1 As New Panel() With {.Dock = DockStyle.Fill}
        Dim p2 As New Panel() With {.Dock = DockStyle.Fill}
        Dim l1 As New Label() With {.Text = "Sales by day", .Dock = DockStyle.Top, .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)}
        Dim l2 As New Label() With {.Text = "Top products (qty) in range", .Dock = DockStyle.Top, .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)}
        p1.Controls.Add(dgvDaily)
        p1.Controls.Add(l1)
        l1.BringToFront()
        p2.Controls.Add(dgvTop)
        p2.Controls.Add(l2)
        l2.BringToFront()
        split.Panel1.Controls.Add(p1)
        split.Panel2.Controls.Add(p2)

        root.Controls.Add(title, 0, 0)
        root.Controls.Add(filter, 0, 1)
        root.Controls.Add(lblSummary, 0, 2)
        root.Controls.Add(split, 0, 3)

        Me.Controls.Add(root)
        RunReport()
    End Sub

    Private Sub btnRun_Click(sender As Object, e As EventArgs) Handles btnRun.Click
        RunReport()
    End Sub

    Private Sub RunReport()
        Dim start As DateTime = dtpFrom.Value.Date
        Dim [end] As DateTime = dtpTo.Value.Date
        If [end] < start Then
            MessageBox.Show("End date must be on or after start date.", "Reports", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim endExclusive As DateTime = [end].AddDays(1)

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Dim dailySql As String =
                    "SELECT CAST(s.sale_date AS DATE) AS sale_day, COUNT(*) AS sale_count, SUM(s.total_amount) AS revenue " &
                    "FROM sales s WHERE s.sale_date >= @from AND s.sale_date < @to " &
                    "GROUP BY CAST(s.sale_date AS DATE) ORDER BY sale_day;"

                Dim dt As New DataTable()
                Using cmd As New SqlCommand(dailySql, connection)
                    cmd.Parameters.AddWithValue("@from", start)
                    cmd.Parameters.AddWithValue("@to", endExclusive)
                    Using adapter As New SqlDataAdapter(cmd)
                        adapter.Fill(dt)
                    End Using
                End Using

                dgvDaily.DataSource = dt
                If dgvDaily.Columns.Count > 0 Then
                    If dgvDaily.Columns.Contains("sale_day") Then
                        dgvDaily.Columns("sale_day").HeaderText = "Date"
                    End If

                    If dgvDaily.Columns.Contains("sale_count") Then
                        dgvDaily.Columns("sale_count").HeaderText = "Sales"
                    End If

                    If dgvDaily.Columns.Contains("revenue") Then
                        dgvDaily.Columns("revenue").HeaderText = "Revenue"
                        dgvDaily.Columns("revenue").DefaultCellStyle.Format = "N2"
                    End If
                End If

                Dim topSql As String =
                    "SELECT TOP 20 si.product_name, SUM(si.quantity) AS qty, SUM(si.subtotal) AS revenue " &
                    "FROM sale_items si INNER JOIN sales s ON s.sale_id = si.sale_id " &
                    "WHERE s.sale_date >= @from AND s.sale_date < @to " &
                    "GROUP BY si.product_name ORDER BY qty DESC;"

                Dim top As New DataTable()
                Using cmd2 As New SqlCommand(topSql, connection)
                    cmd2.Parameters.AddWithValue("@from", start)
                    cmd2.Parameters.AddWithValue("@to", endExclusive)
                    Using adapter As New SqlDataAdapter(cmd2)
                        adapter.Fill(top)
                    End Using
                End Using

                dgvTop.DataSource = top
                If dgvTop.Columns.Count > 0 Then
                    If dgvTop.Columns.Contains("product_name") Then
                        dgvTop.Columns("product_name").HeaderText = "Product"
                    End If

                    If dgvTop.Columns.Contains("qty") Then
                        dgvTop.Columns("qty").HeaderText = "Qty"
                    End If

                    If dgvTop.Columns.Contains("revenue") Then
                        dgvTop.Columns("revenue").HeaderText = "Revenue"
                        dgvTop.Columns("revenue").DefaultCellStyle.Format = "N2"
                    End If
                End If

                Dim sumSql As String = "SELECT ISNULL(SUM(total_amount),0) FROM sales WHERE sale_date >= @from AND sale_date < @to;"
                Dim total As Decimal = 0D
                Using cmd3 As New SqlCommand(sumSql, connection)
                    cmd3.Parameters.AddWithValue("@from", start)
                    cmd3.Parameters.AddWithValue("@to", endExclusive)
                    Dim o As Object = cmd3.ExecuteScalar()
                    If o IsNot Nothing AndAlso Not Convert.IsDBNull(o) Then
                        total = Convert.ToDecimal(o)
                    End If
                End Using

                lblSummary.Text = String.Format(
                    "Range {0:yyyy-MM-dd} .. {1:yyyy-MM-dd} — total revenue {2}{3:N2}",
                    start,
                    [end],
                    AppSettings.Current.CurrencySymbol,
                    total)
            End Using
        Catch ex As Exception
            MessageBox.Show(ex.Message, "Reports", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ErrorLogger.Log(ex, NameOf(ReportsForm) & "." & NameOf(RunReport))
        End Try
    End Sub

End Class
