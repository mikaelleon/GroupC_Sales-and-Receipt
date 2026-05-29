Imports System.Collections.Generic
Imports System.Data
Imports System.Globalization
Imports System.Drawing
Imports System.IO
Imports System.Text
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
    Private WithEvents btnExport As Button
    Private WithEvents dgvDaily As DataGridView
    Private dgvTop As DataGridView
    Private lblSummary As Label
    Private lblDailyEmpty As Label
    Private lblTopEmpty As Label
    Private lblAuditEmpty As Label

    Private WithEvents tabReports As TabControl
    Private tabAuditPage As TabPage
    Private WithEvents dtpAuditFrom As DateTimePicker
    Private WithEvents dtpAuditTo As DateTimePicker
    Private WithEvents btnAuditRefresh As Button
    Private dgvAudit As DataGridView

    Private statusStrip As StatusStrip
    Private statusLabel As ToolStripStatusLabel
    Private WithEvents statusClearTimer As Timer
    Private WithEvents btnBack As Button
    Private formToolTips As ToolTip

    Private Sub ReportsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' 1. FORM SETUP
        Me.Text = AppBranding.WindowTitle("Reports")
        UiTheme.ApplyMaximizedWorkspaceDefaults(Me, 860, 580)
        Me.StartPosition = FormStartPosition.CenterParent

        statusClearTimer = New Timer() With {.Interval = FormStatusHelper.StatusShowMilliseconds}

        Try
            UiTheme.ApplyStandardWindowChrome(Me)
            DatabaseInitializer.EnsureDatabase()
        Catch
        End Try

        ' 2. INSTANTIATE AND RESTRUCTURE THE UI
        CreateControls()

        ' 3. EXPLICITLY SET INITIAL FILTER DATE VALUES
        ' This ensures the controls have valid values before RunReport reads them
        If dtpFrom IsNot Nothing Then dtpFrom.Value = DateTime.Today.AddDays(-30)
        If dtpTo IsNot Nothing Then dtpTo.Value = DateTime.Today

        ' 4. LOAD INITIAL DATA
        Try
            RunReport()
        Catch ex As Exception
            Try
                ShowStatus("Could not load initial report.", True)
                MessageBox.Show(ex.Message, "Reports", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Catch
            End Try

            ErrorLogger.Log(ex, NameOf(ReportsForm) & "." & NameOf(ReportsForm_Load))
        End Try
    End Sub

    Private Sub CreateControls()
        Me.SuspendLayout()
        Me.Controls.Clear()
        Me.BackColor = UiTheme.ColBackground

        ' -----------------------------------------------------------
        ' 1. INSTANTIATE ALL CONTROLS (The Missing Code!)
        ' -----------------------------------------------------------
        tabReports = New TabControl() With {
            .Dock = DockStyle.Fill,
            .Padding = New Point(UiTheme.SpaceMd, UiTheme.SpaceSm),
            .Font = UiTheme.FontBody
        }

        ' Sales Tab Controls
        dtpFrom = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 140, .Value = DateTime.Today.AddDays(-30)}
        dtpTo = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 140, .Value = DateTime.Today}
        btnRun = New Button() With {
            .Text = "&Run report",
            .AutoSize = True,
            .MinimumSize = New Size(120, UiTheme.ButtonHeight),
            .Cursor = Cursors.Hand
        }
        lblSummary = New Label() With {
            .Dock = DockStyle.Fill,
            .AutoSize = False,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Font = UiTheme.FontBody,
            .ForeColor = UiTheme.ColTextPrimary,
            .Margin = Padding.Empty
        }
        lblDailyEmpty = UiTheme.CreateEmptyStateLabel("No sales in this date range.")
        lblTopEmpty = UiTheme.CreateEmptyStateLabel("No product sales in this date range.")

        dgvDaily = CreateReportGrid()
        dgvTop = CreateReportGrid()

        ' Audit Tab Controls
        dtpAuditFrom = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 140, .Value = DateTime.Today.AddDays(-30)}
        dtpAuditTo = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 140, .Value = DateTime.Today}
        btnAuditRefresh = New Button() With {
            .Text = "&Load log",
            .AutoSize = True,
            .MinimumSize = New Size(120, UiTheme.ButtonHeight),
            .Cursor = Cursors.Hand
        }
        btnExport = New Button() With {
            .Text = "Export &CSV",
            .AutoSize = True,
            .MinimumSize = New Size(120, UiTheme.ButtonHeight),
            .Cursor = Cursors.Hand
        }
        dgvAudit = CreateReportGrid()

        ' Status Strip
        statusStrip = New StatusStrip()
        statusLabel = New ToolStripStatusLabel(FormStatusHelper.ReadyText) With {.Spring = True, .TextAlign = ContentAlignment.MiddleLeft}
        statusStrip.Items.Add(statusLabel)

        ' Apply Themes
        Try
            UiTheme.ApplyPrimaryButton(btnRun)
            UiTheme.ApplyPrimaryButton(btnAuditRefresh)
            UiTheme.ApplySecondaryButton(btnExport)
            UiTheme.ApplyGridStyle(dgvDaily)
            UiTheme.ApplyGridStyle(dgvTop)
            UiTheme.ApplyGridStyle(dgvAudit)
            UiTheme.ApplyStatusStripTheme(statusStrip)
        Catch
        End Try

        ' -----------------------------------------------------------
        ' 2. SHARED SHELL + TAB CONTENT
        ' -----------------------------------------------------------
        Dim tabSales As New TabPage("Sales & Revenue") With {.BackColor = UiTheme.ColBackground, .Padding = New Padding(UiTheme.PadPage, UiTheme.PadSection, UiTheme.PadPage, UiTheme.PadSection)}
        Dim salesLayout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .RowCount = 2, .ColumnCount = 1}
        salesLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        salesLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

        salesLayout.Controls.Add(BuildSalesFilterPanel(), 0, 0)

        Dim gridsLayout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .RowCount = 1, .ColumnCount = 2, .Margin = Padding.Empty}
        gridsLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 54.0F))
        gridsLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 46.0F))

        Dim pnlDaily As Panel = WrapInCard("Daily Revenue", dgvDaily, lblDailyEmpty)
        pnlDaily.Margin = New Padding(0, 0, UiTheme.SpaceMd, 0)
        gridsLayout.Controls.Add(pnlDaily, 0, 0)

        Dim pnlTop As Panel = WrapInCard("Top Products", dgvTop, lblTopEmpty)
        pnlTop.Margin = New Padding(UiTheme.SpaceMd, 0, 0, 0)
        gridsLayout.Controls.Add(pnlTop, 1, 0)

        salesLayout.Controls.Add(gridsLayout, 0, 1)
        tabSales.Controls.Add(salesLayout)
        tabReports.TabPages.Add(tabSales)

        ' Audit Tab Assembly
        If AppSession.IsAdmin() Then
            tabAuditPage = New TabPage("System Audit Logs") With {.BackColor = UiTheme.ColBackground, .Padding = New Padding(UiTheme.PadPage, UiTheme.PadSection, UiTheme.PadPage, UiTheme.PadSection)}
            Dim auditLayout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .RowCount = 2, .ColumnCount = 1}
            auditLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            auditLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

            auditLayout.Controls.Add(BuildDateFilterPanel(dtpAuditFrom, dtpAuditTo, btnAuditRefresh), 0, 0)

            lblAuditEmpty = UiTheme.CreateEmptyStateLabel("No audit entries in this date range.")
            Dim pnlAuditGrid As Panel = WrapInCard("Audit Logs", dgvAudit, lblAuditEmpty)
            auditLayout.Controls.Add(pnlAuditGrid, 0, 1)

            tabAuditPage.Controls.Add(auditLayout)
            tabReports.TabPages.Add(tabAuditPage)
        End If

        Dim rootTable As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .Margin = Padding.Empty,
            .BackColor = UiTheme.ColBackground
        }
        rootTable.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, UiTheme.SidebarWidth))
        rootTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

        Dim sidebar As Panel = UiTheme.BuildWorkspaceSidebarShell(WorkspaceNavigation.Target.Reports, Me, btnBack)

        Dim rightColumn As New Panel() With {.Dock = DockStyle.Fill, .BackColor = UiTheme.ColBackground}
        Dim topBar As Panel = UiTheme.CreateTopBar("Reports", AppSession.GetAuditIdentity())
        Dim contentArea As Panel = UiTheme.CreateContentArea()
        contentArea.Padding = New Padding(UiTheme.PadSection)
        tabReports.Dock = DockStyle.Fill
        contentArea.Controls.Add(tabReports)
        rightColumn.Controls.Add(contentArea)
        rightColumn.Controls.Add(topBar)

        rootTable.Controls.Add(sidebar, 0, 0)
        rootTable.Controls.Add(rightColumn, 1, 0)

        Me.Controls.Add(rootTable)
        Me.Controls.Add(statusStrip)

        formToolTips = UiTheme.CreateStandardToolTip()
        formToolTips.SetToolTip(btnRun, "Generate the sales report for the selected date range")
        formToolTips.SetToolTip(btnExport, "Export the current report data to CSV")
        formToolTips.SetToolTip(btnAuditRefresh, "Load audit log entries for the selected date range")

        UiTheme.AssignTabOrder(dtpFrom, dtpTo, btnRun, btnExport, dgvDaily, dtpAuditFrom, dtpAuditTo, btnAuditRefresh, dgvAudit, btnBack)

        Me.ResumeLayout(True)
    End Sub

    Private Sub btnBack_Click(sender As Object, e As EventArgs) Handles btnBack.Click
        Me.Close()
    End Sub

    Private Sub btnAuditRefresh_Click(sender As Object, e As EventArgs) Handles btnAuditRefresh.Click
        LoadAuditLog()
    End Sub

    Private Sub tabReports_SelectedIndexChanged(sender As Object, e As EventArgs) Handles tabReports.SelectedIndexChanged
        If tabReports.SelectedTab Is tabAuditPage Then
            LoadAuditLog()
        End If
    End Sub

    Private Sub LoadAuditLog()
        If dgvAudit Is Nothing Then
            Return
        End If

        Dim start As DateTime = dtpAuditFrom.Value.Date
        Dim [end] As DateTime = dtpAuditTo.Value.Date
        If [end] < start Then
            ShowStatus("Audit: end date must be on or after start date.", True)
            MessageBox.Show("End date must be on or after start date.", "Audit log", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim endExclusive As DateTime = [end].AddDays(1)

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Dim sql As String =
                    "SELECT audit_id, Action, Detail, PerformedBy, LoggedAt FROM (" &
                    "SELECT LogID AS audit_id, Action, Detail, PerformedBy, LoggedAt " &
                    "FROM dbo.AuditLogs WHERE LoggedAt >= @from AND LoggedAt < @to " &
                    "UNION ALL " &
                    "SELECT adjustment_id, 'STOCK_ADJUSTED', " &
                    "product_name + ': ' + CAST(old_quantity AS NVARCHAR(20)) + ' -> ' + CAST(new_quantity AS NVARCHAR(20)), " &
                    "adjusted_by, adjusted_at " &
                    "FROM dbo.stock_adjustments WHERE adjusted_at >= @from AND adjusted_at < @to" &
                    ") AS combined ORDER BY LoggedAt DESC;"

                Dim dt As New DataTable()
                Using cmd As New SqlCommand(sql, connection)
                    cmd.Parameters.AddWithValue("@from", start)
                    cmd.Parameters.AddWithValue("@to", endExclusive)
                    Using adapter As New SqlDataAdapter(cmd)
                        adapter.Fill(dt)
                    End Using
                End Using

                dgvAudit.DataSource = dt
                ApplyAuditGridColumns(dgvAudit)
                UpdateGridEmptyState(dgvAudit, lblAuditEmpty, dt.Rows.Count = 0)
            End Using

            ShowStatus("Audit log loaded.", False)
        Catch ex As Exception
            ShowStatus("Audit log failed.", True)
            MessageBox.Show(ex.Message, "Audit log", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ErrorLogger.Log(ex, NameOf(ReportsForm) & "." & NameOf(LoadAuditLog))
        End Try
    End Sub

    Private Sub ShowStatus(message As String, isError As Boolean)
        FormStatusHelper.ShowTimedStatus(statusLabel, statusClearTimer, message, isError)
    End Sub

    Private Sub statusClearTimer_Tick(sender As Object, e As EventArgs) Handles statusClearTimer.Tick
        statusClearTimer.Stop()
        FormStatusHelper.ResetTimedStatus(statusLabel)
    End Sub

    Private Sub btnRun_Click(sender As Object, e As EventArgs) Handles btnRun.Click
        RunReport()
    End Sub

    Private Sub dgvDaily_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvDaily.CellDoubleClick
        If e.RowIndex < 0 OrElse dgvDaily Is Nothing Then
            Return
        End If

        If Not dgvDaily.Columns.Contains("sale_day") Then
            Return
        End If

        Dim dayValue As Object = dgvDaily.Rows(e.RowIndex).Cells("sale_day").Value
        If dayValue Is Nothing OrElse dayValue Is DBNull.Value Then
            Return
        End If

        Dim day As Date = Convert.ToDateTime(dayValue, CultureInfo.CurrentCulture).Date
        Using receipt As New ReceiptForm()
            receipt.ShowReceiptsForDay(day)
            receipt.ShowDialog(Me)
        End Using
    End Sub

    Private Sub RunReport()
        If dtpFrom Is Nothing OrElse dtpTo Is Nothing OrElse dgvDaily Is Nothing OrElse dgvTop Is Nothing OrElse lblSummary Is Nothing Then
            Return
        End If

        Dim start As DateTime = dtpFrom.Value.Date
        Dim [end] As DateTime = dtpTo.Value.Date
        If [end] < start Then
            ShowStatus("End date must be on or after start date.", True)
            MessageBox.Show("End date must be on or after start date.", "Reports", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim utcRange = ReceiptBranding.GetUtcRangeForLocalDates(start, [end])

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                Dim rawSql As String =
                    "SELECT sale_date, total_amount FROM sales " &
                    "WHERE ISNULL(is_voided, 0) = 0 AND sale_date >= @from AND sale_date < @to;"

                Dim dailyMap As New Dictionary(Of Date, (SaleCount As Integer, Revenue As Decimal))()
                Using cmd As New SqlCommand(rawSql, connection)
                    cmd.Parameters.AddWithValue("@from", utcRange.UtcStart)
                    cmd.Parameters.AddWithValue("@to", utcRange.UtcEndExclusive)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim localDay As Date = ReceiptBranding.NormalizeStoredSaleDate(
                                Convert.ToDateTime(reader("sale_date"), CultureInfo.InvariantCulture)).Date
                            Dim amount As Decimal = Convert.ToDecimal(reader("total_amount"))
                            If dailyMap.ContainsKey(localDay) Then
                                Dim existing = dailyMap(localDay)
                                dailyMap(localDay) = (existing.SaleCount + 1, existing.Revenue + amount)
                            Else
                                dailyMap(localDay) = (1, amount)
                            End If
                        End While
                    End Using
                End Using

                Dim dt As New DataTable()
                dt.Columns.Add("sale_day", GetType(Date))
                dt.Columns.Add("sale_count", GetType(Integer))
                dt.Columns.Add("revenue", GetType(Decimal))
                For Each kvp In dailyMap.OrderBy(Function(x) x.Key)
                    dt.Rows.Add(kvp.Key, kvp.Value.SaleCount, kvp.Value.Revenue)
                Next

                dgvDaily.DataSource = dt
                ApplyDailyGridColumns(dgvDaily)
                UpdateGridEmptyState(dgvDaily, lblDailyEmpty, dt.Rows.Count = 0)

                Dim topSql As String =
                    "SELECT TOP 20 si.product_name, SUM(si.quantity) AS qty, SUM(si.subtotal) AS revenue " &
                    "FROM sale_items si INNER JOIN sales s ON s.sale_id = si.sale_id " &
                    "WHERE ISNULL(s.is_voided, 0) = 0 AND s.sale_date >= @from AND s.sale_date < @to " &
                    "GROUP BY si.product_name ORDER BY qty DESC;"

                Dim top As New DataTable()
                Using cmd2 As New SqlCommand(topSql, connection)
                    cmd2.Parameters.AddWithValue("@from", utcRange.UtcStart)
                    cmd2.Parameters.AddWithValue("@to", utcRange.UtcEndExclusive)
                    Using adapter As New SqlDataAdapter(cmd2)
                        adapter.Fill(top)
                    End Using
                End Using

                dgvTop.DataSource = top
                ApplyTopGridColumns(dgvTop)
                UpdateGridEmptyState(dgvTop, lblTopEmpty, top.Rows.Count = 0)

                Dim sumSql As String =
                    "SELECT ISNULL(SUM(total_amount),0) FROM sales " &
                    "WHERE ISNULL(is_voided, 0) = 0 AND sale_date >= @from AND sale_date < @to;"
                Dim total As Decimal = 0D
                Using cmd3 As New SqlCommand(sumSql, connection)
                    cmd3.Parameters.AddWithValue("@from", utcRange.UtcStart)
                    cmd3.Parameters.AddWithValue("@to", utcRange.UtcEndExclusive)
                    Dim o As Object = cmd3.ExecuteScalar()
                    If o IsNot Nothing AndAlso Not Convert.IsDBNull(o) Then
                        total = Convert.ToDecimal(o)
                    End If
                End Using

                lblSummary.Text = String.Format(
                    CultureInfo.CurrentCulture,
                    "Range {0:yyyy-MM-dd} to {1:yyyy-MM-dd}  ·  Total revenue {2}{3:N2}  ·  {4} day(s)",
                    start,
                    [end],
                    AppSettings.Current.CurrencySymbol,
                    total,
                    dt.Rows.Count)
            End Using

            ShowStatus("Report updated.", False)
        Catch ex As Exception
            ShowStatus("Report failed. See error dialog.", True)
            MessageBox.Show(ex.Message, "Reports", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ErrorLogger.Log(ex, NameOf(ReportsForm) & "." & NameOf(RunReport))
        End Try
    End Sub

    Private Sub btnExport_Click(sender As Object, e As EventArgs) Handles btnExport.Click
        ExportReportCsv()
    End Sub

    Private Sub ExportReportCsv()
        If dgvDaily Is Nothing OrElse dgvTop Is Nothing Then
            Return
        End If

        Using dialog As New SaveFileDialog()
            dialog.Filter = "CSV files (*.csv)|*.csv"
            dialog.FileName = String.Format(
                CultureInfo.InvariantCulture,
                "sales-report-{0:yyyyMMdd}.csv",
                DateTime.Today)

            If dialog.ShowDialog(Me) <> DialogResult.OK Then
                Return
            End If

            Try
                Dim sb As New StringBuilder()
                sb.AppendLine("Daily Revenue")
                AppendGridCsv(sb, dgvDaily)
                sb.AppendLine()
                sb.AppendLine("Top Products")
                AppendGridCsv(sb, dgvTop)
                File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8)
                ShowStatus("Report exported to CSV.", False)
            Catch ex As Exception
                ShowStatus("Export failed.", True)
                MessageBox.Show(ex.Message, "Export", MessageBoxButtons.OK, MessageBoxIcon.Error)
                ErrorLogger.Log(ex, NameOf(ReportsForm) & "." & NameOf(ExportReportCsv))
            End Try
        End Using
    End Sub

    Private Shared Sub AppendGridCsv(sb As StringBuilder, dgv As DataGridView)
        If dgv Is Nothing OrElse dgv.Columns.Count = 0 Then
            Return
        End If

        Dim headers As New List(Of String)()
        For Each col As DataGridViewColumn In dgv.Columns
            If col.Visible Then
                headers.Add(EscapeCsv(col.HeaderText))
            End If
        Next
        sb.AppendLine(String.Join(",", headers))

        For Each row As DataGridViewRow In dgv.Rows
            If row.IsNewRow Then
                Continue For
            End If

            Dim cells As New List(Of String)()
            For Each col As DataGridViewColumn In dgv.Columns
                If Not col.Visible Then
                    Continue For
                End If

                Dim val As Object = row.Cells(col.Index).FormattedValue
                cells.Add(EscapeCsv(If(val, String.Empty).ToString()))
            Next
            sb.AppendLine(String.Join(",", cells))
        Next
    End Sub

    Private Shared Function EscapeCsv(value As String) As String
        If value Is Nothing Then
            Return String.Empty
        End If

        Dim needsQuotes As Boolean = value.Contains(","c) OrElse value.Contains(""""c) OrElse value.Contains(vbCr) OrElse value.Contains(vbLf)
        Dim escaped As String = value.Replace("""", """""")
        Return If(needsQuotes, """" & escaped & """", escaped)
    End Function

    Private Shared Function CreateReportGrid() As DataGridView
        Return New DataGridView() With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .BackgroundColor = UiTheme.ColSurface,
            .BorderStyle = BorderStyle.None,
            .ScrollBars = ScrollBars.Both,
            .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            .ColumnHeadersHeight = UiTheme.GridHeaderHeight,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            .AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None
        }
    End Function

    Private Shared Sub EnsureGridHeaderLayout(dgv As DataGridView)
        If dgv Is Nothing Then
            Return
        End If

        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        dgv.ColumnHeadersHeight = UiTheme.GridHeaderHeight
        dgv.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False
        dgv.ColumnHeadersDefaultCellStyle.Font = UiTheme.FontBodyBold
    End Sub

    Private Shared Sub UpdateGridEmptyState(dgv As DataGridView, emptyLabel As Label, isEmpty As Boolean)
        If dgv Is Nothing OrElse emptyLabel Is Nothing Then
            Return
        End If

        dgv.Visible = Not isEmpty
        emptyLabel.Visible = isEmpty
    End Sub

    Private Shared Sub ApplyDailyGridColumns(dgv As DataGridView)
        If dgv.Columns.Count = 0 Then Return

        EnsureGridHeaderLayout(dgv)
        GridDisplayHelper.HideInternalIdColumns(dgv)

        If dgv.Columns.Contains("sale_day") Then
            dgv.Columns("sale_day").HeaderText = "Date"
            dgv.Columns("sale_day").AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            dgv.Columns("sale_day").Width = 118
            dgv.Columns("sale_day").DefaultCellStyle.Format = "yyyy-MM-dd"
            dgv.Columns("sale_day").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            dgv.Columns("sale_day").HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft
        End If

        If dgv.Columns.Contains("sale_count") Then
            dgv.Columns("sale_count").HeaderText = "Sales"
            dgv.Columns("sale_count").AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            dgv.Columns("sale_count").Width = 88
            dgv.Columns("sale_count").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            dgv.Columns("sale_count").HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter
        End If

        If dgv.Columns.Contains("revenue") Then
            dgv.Columns("revenue").HeaderText = "Revenue"
            dgv.Columns("revenue").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            dgv.Columns("revenue").MinimumWidth = 132
            dgv.Columns("revenue").FillWeight = 120
            dgv.Columns("revenue").DefaultCellStyle.Format = "N2"
            dgv.Columns("revenue").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            dgv.Columns("revenue").HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight
        End If
    End Sub

    Private Shared Sub ApplyTopGridColumns(dgv As DataGridView)
        If dgv.Columns.Count = 0 Then Return

        EnsureGridHeaderLayout(dgv)
        GridDisplayHelper.HideInternalIdColumns(dgv)

        If dgv.Columns.Contains("product_name") Then
            dgv.Columns("product_name").HeaderText = "Product"
            dgv.Columns("product_name").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            dgv.Columns("product_name").FillWeight = 160
            dgv.Columns("product_name").MinimumWidth = 120
            dgv.Columns("product_name").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            dgv.Columns("product_name").HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft
        End If

        If dgv.Columns.Contains("qty") Then
            dgv.Columns("qty").HeaderText = "Qty"
            dgv.Columns("qty").AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            dgv.Columns("qty").Width = 64
            dgv.Columns("qty").MinimumWidth = 64
            dgv.Columns("qty").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            dgv.Columns("qty").HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter
        End If

        If dgv.Columns.Contains("revenue") Then
            dgv.Columns("revenue").HeaderText = "Revenue"
            dgv.Columns("revenue").AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            dgv.Columns("revenue").Width = 132
            dgv.Columns("revenue").MinimumWidth = 132
            dgv.Columns("revenue").DefaultCellStyle.Format = "N2"
            dgv.Columns("revenue").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            dgv.Columns("revenue").HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight
        End If
    End Sub

    Private Shared Sub ApplyAuditGridColumns(dgv As DataGridView)
        If dgv.Columns.Count = 0 Then Return

        EnsureGridHeaderLayout(dgv)
        GridDisplayHelper.HideInternalIdColumns(dgv)

        If dgv.Columns.Contains("LoggedAt") Then
            dgv.Columns("LoggedAt").HeaderText = "Logged at"
            dgv.Columns("LoggedAt").AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            dgv.Columns("LoggedAt").Width = 150
            dgv.Columns("LoggedAt").DefaultCellStyle.Format = "yyyy-MM-dd HH:mm"
        End If

        If dgv.Columns.Contains("Action") Then
            dgv.Columns("Action").HeaderText = "Action"
            dgv.Columns("Action").AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            dgv.Columns("Action").Width = 140
        End If

        If dgv.Columns.Contains("PerformedBy") Then
            dgv.Columns("PerformedBy").HeaderText = "Performed by"
            dgv.Columns("PerformedBy").AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            dgv.Columns("PerformedBy").Width = 140
        End If

        If dgv.Columns.Contains("Detail") Then
            dgv.Columns("Detail").HeaderText = "Detail"
            dgv.Columns("Detail").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            dgv.Columns("Detail").MinimumWidth = 180
        End If
    End Sub

    Private Function BuildSalesFilterPanel() As Control
        Dim card As Panel = UiTheme.CreateCard()
        card.Dock = DockStyle.Top
        card.Margin = New Padding(0, 0, 0, UiTheme.PadSection)

        Dim layout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2,
            .BackColor = Color.Transparent
        }
        layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim dateRow As New FlowLayoutPanel() With {
            .AutoSize = True,
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = False,
            .Padding = Padding.Empty,
            .Margin = Padding.Empty
        }

        dtpFrom.Width = 136
        dtpFrom.Height = UiTheme.InputHeight
        dtpFrom.Margin = New Padding(0, UiTheme.SpaceXs, UiTheme.SpaceMd, UiTheme.SpaceXs)
        dtpTo.Width = 136
        dtpTo.Height = UiTheme.InputHeight
        dtpTo.Margin = New Padding(0, UiTheme.SpaceXs, UiTheme.SpaceMd, UiTheme.SpaceXs)

        dateRow.Controls.Add(CreateFilterCaption("From:"))
        dateRow.Controls.Add(dtpFrom)
        dateRow.Controls.Add(CreateFilterCaption("To:"))
        dateRow.Controls.Add(dtpTo)
        dateRow.Controls.Add(CreateRangePresetButton("7 days", 7))
        dateRow.Controls.Add(CreateRangePresetButton("30 days", 30))
        dateRow.Controls.Add(CreateRangePresetButton("90 days", 90))

        ConfigureFilterActionButton(btnRun)
        ConfigureFilterActionButton(btnExport)
        ApplyFilterChipButton(btnExport)

        Dim actionRow As New FlowLayoutPanel() With {
            .AutoSize = True,
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = False,
            .Padding = Padding.Empty,
            .Margin = New Padding(0, UiTheme.SpaceSm, 0, 0)
        }
        actionRow.Controls.Add(btnRun)
        actionRow.Controls.Add(btnExport)

        Dim summaryHost As New Panel() With {
            .Dock = DockStyle.Fill,
            .AutoSize = True,
            .BackColor = UiTheme.GridAltRow,
            .Padding = New Padding(UiTheme.SpaceMd, UiTheme.SpaceSm, UiTheme.SpaceMd, UiTheme.SpaceSm),
            .Margin = New Padding(0, UiTheme.SpaceSm, 0, 0)
        }
        lblSummary.MinimumSize = New Size(0, 28)
        lblSummary.Dock = DockStyle.Fill
        summaryHost.Controls.Add(lblSummary)
        actionRow.Controls.Add(summaryHost)

        layout.Controls.Add(dateRow, 0, 0)
        layout.Controls.Add(actionRow, 0, 1)

        UiTheme.PopulateCardContent(card, layout)
        Return card
    End Function

    Private Shared Sub ConfigureFilterActionButton(button As Button)
        If button Is Nothing Then
            Return
        End If

        button.AutoSize = True
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink
        button.Dock = DockStyle.None
        button.Anchor = AnchorStyles.None
        button.Margin = New Padding(0, UiTheme.SpaceXs, UiTheme.SpaceSm, UiTheme.SpaceXs)
        button.MinimumSize = New Size(112, UiTheme.ButtonHeight)
    End Sub

    Private Function CreateRangePresetButton(caption As String, daysBack As Integer) As Button
        Dim btn As New Button() With {
            .Text = caption,
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .MinimumSize = New Size(76, UiTheme.ButtonHeight),
            .Margin = New Padding(0, UiTheme.SpaceXs, UiTheme.SpaceSm, UiTheme.SpaceXs),
            .Cursor = Cursors.Hand,
            .Tag = daysBack,
            .TabStop = False
        }
        Try
            ApplyFilterChipButton(btn)
        Catch
        End Try
        AddHandler btn.Click, AddressOf RangePresetButton_Click
        Return btn
    End Function

    Private Shared Sub ApplyFilterChipButton(button As Button)
        If button Is Nothing Then
            Return
        End If

        button.FlatStyle = FlatStyle.Flat
        button.FlatAppearance.BorderSize = 1
        button.FlatAppearance.BorderColor = UiTheme.ColBorder
        button.BackColor = UiTheme.ColSurface
        button.ForeColor = UiTheme.ColTextPrimary
        button.Cursor = Cursors.Hand
        button.UseCompatibleTextRendering = False
        button.Font = UiTheme.FontBody
        button.Padding = New Padding(UiTheme.SpaceMd, UiTheme.SpaceXs, UiTheme.SpaceMd, UiTheme.SpaceXs)
        button.TextAlign = ContentAlignment.MiddleCenter
    End Sub

    Private Sub RangePresetButton_Click(sender As Object, e As EventArgs)
        Dim btn As Button = TryCast(sender, Button)
        If btn Is Nothing OrElse btn.Tag Is Nothing Then
            Return
        End If

        Dim daysBack As Integer = Convert.ToInt32(btn.Tag, CultureInfo.InvariantCulture)
        dtpFrom.Value = DateTime.Today.AddDays(-daysBack)
        dtpTo.Value = DateTime.Today
        RunReport()
    End Sub

    Private Shared Function CreateFilterCaption(text As String) As Label
        Dim lbl As Label = UiTheme.CreateSecondaryLabel(text)
        lbl.AutoSize = True
        lbl.Dock = DockStyle.None
        lbl.TextAlign = ContentAlignment.MiddleLeft
        lbl.Margin = New Padding(0, UiTheme.SpaceSm, UiTheme.SpaceSm, UiTheme.SpaceSm)
        Return lbl
    End Function

    Private Shared Function BuildDateFilterPanel(
        fromPicker As DateTimePicker,
        toPicker As DateTimePicker,
        actionButton As Button,
        Optional summaryLabel As Label = Nothing) As TableLayoutPanel

        Dim columnCount As Integer = If(summaryLabel Is Nothing, 5, 6)
        Dim panel As New TableLayoutPanel() With {
            .AutoSize = True,
            .Dock = DockStyle.Top,
            .ColumnCount = columnCount,
            .RowCount = 1,
            .Margin = New Padding(0, 0, 0, UiTheme.SpaceLg),
            .Height = 44
        }
        panel.RowStyles.Add(New RowStyle(SizeType.Absolute, 44.0F))

        If summaryLabel Is Nothing Then
            For c As Integer = 0 To columnCount - 1
                panel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            Next
        Else
            For c As Integer = 0 To columnCount - 2
                panel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            Next
            panel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        End If

        fromPicker.Width = 132
        fromPicker.Margin = New Padding(0, 0, UiTheme.SpaceMd, 0)
        fromPicker.Dock = DockStyle.Fill
        toPicker.Width = 132
        toPicker.Margin = New Padding(0, 0, UiTheme.SpaceMd, 0)
        toPicker.Dock = DockStyle.Fill
        actionButton.Margin = Padding.Empty
        actionButton.Dock = DockStyle.Fill

        Dim col As Integer = 0
        panel.Controls.Add(CreateFilterCaption("From:"), col, 0)
        col += 1
        panel.Controls.Add(fromPicker, col, 0)
        col += 1
        panel.Controls.Add(CreateFilterCaption("To:"), col, 0)
        col += 1
        panel.Controls.Add(toPicker, col, 0)
        col += 1
        panel.Controls.Add(actionButton, col, 0)
        col += 1

        If summaryLabel IsNot Nothing Then
            summaryLabel.Dock = DockStyle.Fill
            summaryLabel.TextAlign = ContentAlignment.MiddleLeft
            panel.Controls.Add(summaryLabel, col, 0)
        End If

        Return panel
    End Function

    Private Function WrapInCard(title As String, grid As DataGridView, emptyLabel As Label) As Panel
        grid.Dock = DockStyle.Fill
        emptyLabel.Dock = DockStyle.Fill
        emptyLabel.Visible = False

        Dim gridHost As New Panel() With {.Dock = DockStyle.Fill}
        gridHost.Controls.Add(grid)
        gridHost.Controls.Add(emptyLabel)
        emptyLabel.BringToFront()

        Dim titleHost As Panel = UiTheme.CreateSectionHeader(title)
        titleHost.Dock = DockStyle.Fill
        titleHost.Margin = New Padding(0, 0, 0, UiTheme.PadControl)

        Dim cardLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2,
            .Margin = Padding.Empty
        }
        cardLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        cardLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        cardLayout.Controls.Add(titleHost, 0, 0)
        cardLayout.Controls.Add(gridHost, 0, 1)

        Dim card As Panel = UiTheme.CreateCard()
        card.Dock = DockStyle.Fill
        card.MinimumSize = New Size(0, 180)
        Dim cardHost As Panel = card
        Try
            cardHost = UiTheme.GetCardContentHost(card)
        Catch
        End Try
        cardHost.Controls.Add(cardLayout)
        Return card
    End Function

End Class
