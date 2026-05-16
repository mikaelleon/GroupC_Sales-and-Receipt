Imports System.Data
Imports System.Globalization
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

    Private WithEvents tabReports As TabControl
    Private tabAuditPage As TabPage
    Private WithEvents dtpAuditFrom As DateTimePicker
    Private WithEvents dtpAuditTo As DateTimePicker
    Private WithEvents btnAuditRefresh As Button
    Private dgvAudit As DataGridView

    Private statusStrip As StatusStrip
    Private statusLabel As ToolStripStatusLabel
    Private WithEvents statusClearTimer As Timer

    Private Sub ReportsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' 1. FORM SETUP
        Me.Text = "Group C - Reports"
        Me.MinimumSize = New Size(1024, 720)
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.WindowState = FormWindowState.Maximized
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
        RunReport()
    End Sub

    Private Sub CreateControls()
        Me.SuspendLayout()
        Me.Controls.Clear()
        Me.BackColor = UiTheme.FormBackground

        ' -----------------------------------------------------------
        ' 1. INSTANTIATE ALL CONTROLS (The Missing Code!)
        ' -----------------------------------------------------------
        tabReports = New TabControl() With {.Dock = DockStyle.Fill, .Padding = New Point(15, 10), .Font = New Font("Segoe UI", 11)}

        ' Sales Tab Controls
        dtpFrom = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 140, .Value = DateTime.Today.AddDays(-30)}
        dtpTo = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 140, .Value = DateTime.Today}
        btnRun = New Button() With {.Text = "&Run report", .Size = New Size(120, 32), .Cursor = Cursors.Hand}
        lblSummary = New Label() With {.AutoSize = True, .Font = New Font("Segoe UI", 11, FontStyle.Bold), .Margin = New Padding(30, 6, 0, 0)}

        dgvDaily = New DataGridView() With {.Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False, .BackgroundColor = Color.White, .BorderStyle = BorderStyle.None}
        dgvTop = New DataGridView() With {.Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False, .BackgroundColor = Color.White, .BorderStyle = BorderStyle.None}

        ' Audit Tab Controls
        dtpAuditFrom = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 140, .Value = DateTime.Today.AddDays(-30)}
        dtpAuditTo = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 140, .Value = DateTime.Today}
        btnAuditRefresh = New Button() With {.Text = "&Load log", .Size = New Size(120, 32), .Cursor = Cursors.Hand}
        dgvAudit = New DataGridView() With {.Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False, .BackgroundColor = Color.White, .BorderStyle = BorderStyle.None}

        ' Status Strip
        statusStrip = New StatusStrip()
        statusLabel = New ToolStripStatusLabel(FormStatusHelper.ReadyText) With {.Spring = True, .TextAlign = ContentAlignment.MiddleLeft}
        statusStrip.Items.Add(statusLabel)

        ' Apply Themes
        Try
            UiTheme.ApplyPrimaryButton(btnRun)
            UiTheme.ApplyPrimaryButton(btnAuditRefresh)
            UiTheme.ApplyDataGridViewChrome(dgvDaily)
            UiTheme.ApplyDataGridViewChrome(dgvTop)
            UiTheme.ApplyDataGridViewChrome(dgvAudit)
            UiTheme.ApplyStatusStripTheme(statusStrip)
        Catch
        End Try

        ' -----------------------------------------------------------
        ' 2. BUILD THE RESPONSIVE LAYOUT
        ' -----------------------------------------------------------
        ' Top Header & Back Button
        Dim btnBack As New Button() With {.Text = "← Back to Menu", .Size = New Size(140, 36), .Cursor = Cursors.Hand, .Margin = New Padding(0, 0, 20, 0)}
        AddHandler btnBack.Click, Sub(s, ev) Me.Close()
        Try : UiTheme.ApplySecondaryButton(btnBack) : Catch : End Try

        Dim headerPanel As New TableLayoutPanel() With {
            .Dock = DockStyle.Top, .Height = 80, .ColumnCount = 2, .RowCount = 1,
            .Padding = New Padding(30, 20, 30, 20), .BackColor = Color.White
        }
        headerPanel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        headerPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

        Dim lblTitle As New Label() With {.Text = "System Reports & Audit", .Font = New Font("Segoe UI", 18, FontStyle.Bold), .ForeColor = UiTheme.PrimaryAccent, .AutoSize = True, .Anchor = AnchorStyles.Left Or AnchorStyles.Top Or AnchorStyles.Bottom, .TextAlign = ContentAlignment.MiddleLeft}
        headerPanel.Controls.Add(btnBack, 0, 0)
        headerPanel.Controls.Add(lblTitle, 1, 0)

        ' Sales Tab Assembly
        Dim tabSales As New TabPage("Sales & Revenue") With {.BackColor = UiTheme.FormBackground, .Padding = New Padding(20)}
        Dim salesLayout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .RowCount = 2, .ColumnCount = 1}
        salesLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        salesLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

        Dim pnlSalesFilters As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .AutoSize = True, .Padding = New Padding(0, 0, 0, 20), .WrapContents = False}
        pnlSalesFilters.Controls.Add(New Label() With {.Text = "From:", .AutoSize = True, .Margin = New Padding(0, 8, 5, 0)})
        pnlSalesFilters.Controls.Add(dtpFrom)
        pnlSalesFilters.Controls.Add(New Label() With {.Text = "To:", .AutoSize = True, .Margin = New Padding(15, 8, 5, 0)})
        pnlSalesFilters.Controls.Add(dtpTo)
        btnRun.Margin = New Padding(20, 0, 0, 0)
        pnlSalesFilters.Controls.Add(btnRun)
        pnlSalesFilters.Controls.Add(lblSummary)
        salesLayout.Controls.Add(pnlSalesFilters, 0, 0)

        Dim gridsLayout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .RowCount = 1, .ColumnCount = 2}
        gridsLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 60.0F))
        gridsLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 40.0F))

        Dim pnlDaily As Panel = WrapInCard("Daily Revenue", dgvDaily)
        pnlDaily.Margin = New Padding(0, 0, 10, 0)
        gridsLayout.Controls.Add(pnlDaily, 0, 0)

        Dim pnlTop As Panel = WrapInCard("Top Products", dgvTop)
        pnlTop.Margin = New Padding(10, 0, 0, 0)
        gridsLayout.Controls.Add(pnlTop, 1, 0)

        salesLayout.Controls.Add(gridsLayout, 0, 1)
        tabSales.Controls.Add(salesLayout)
        tabReports.TabPages.Add(tabSales)

        ' Audit Tab Assembly
        If AppSession.IsAdmin() Then
            tabAuditPage = New TabPage("System Audit Logs") With {.BackColor = UiTheme.FormBackground, .Padding = New Padding(20)}
            Dim auditLayout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .RowCount = 2, .ColumnCount = 1}
            auditLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            auditLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

            Dim pnlAuditFilters As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .AutoSize = True, .Padding = New Padding(0, 0, 0, 20), .WrapContents = False}
            pnlAuditFilters.Controls.Add(New Label() With {.Text = "From:", .AutoSize = True, .Margin = New Padding(0, 8, 5, 0)})
            pnlAuditFilters.Controls.Add(dtpAuditFrom)
            pnlAuditFilters.Controls.Add(New Label() With {.Text = "To:", .AutoSize = True, .Margin = New Padding(15, 8, 5, 0)})
            pnlAuditFilters.Controls.Add(dtpAuditTo)
            btnAuditRefresh.Margin = New Padding(20, 0, 0, 0)
            pnlAuditFilters.Controls.Add(btnAuditRefresh)
            auditLayout.Controls.Add(pnlAuditFilters, 0, 0)

            Dim pnlAuditGrid As Panel = WrapInCard("Audit Logs", dgvAudit)
            auditLayout.Controls.Add(pnlAuditGrid, 0, 1)

            tabAuditPage.Controls.Add(auditLayout)
            tabReports.TabPages.Add(tabAuditPage)
        End If

        ' Final App Assembly
        Dim mainContainer As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(0)}
        mainContainer.Controls.Add(tabReports)
        mainContainer.Controls.Add(headerPanel)

        Dim shell As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .RowCount = 2, .ColumnCount = 1}
        shell.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        shell.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        shell.Controls.Add(mainContainer, 0, 0)
        shell.Controls.Add(statusStrip, 0, 1)

        Me.Controls.Add(shell)

        Me.ResumeLayout(True)
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
                    "SELECT LogID, Action, Detail, PerformedBy, LoggedAt " &
                    "FROM dbo.AuditLogs WHERE LoggedAt >= @from AND LoggedAt < @to " &
                    "ORDER BY LoggedAt DESC;"

                Dim dt As New DataTable()
                Using cmd As New SqlCommand(sql, connection)
                    cmd.Parameters.AddWithValue("@from", start)
                    cmd.Parameters.AddWithValue("@to", endExclusive)
                    Using adapter As New SqlDataAdapter(cmd)
                        adapter.Fill(dt)
                    End Using
                End Using

                dgvAudit.DataSource = dt
                If dgvAudit.Columns.Count > 0 Then
                    If dgvAudit.Columns.Contains("LoggedAt") Then
                        dgvAudit.Columns("LoggedAt").DefaultCellStyle.Format = "yyyy-MM-dd HH:mm"
                    End If
                End If
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
                ApplyDailyGridColumns(dgvDaily)

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
                ApplyTopGridColumns(dgvTop)

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
                    CultureInfo.CurrentCulture,
                    "Range {0:yyyy-MM-dd} .. {1:yyyy-MM-dd} — total revenue {2}{3:N2}",
                    start,
                    [end],
                    AppSettings.Current.CurrencySymbol,
                    total)
            End Using

            ShowStatus("Report updated.", False)
        Catch ex As Exception
            ShowStatus("Report failed. See error dialog.", True)
            MessageBox.Show(ex.Message, "Reports", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ErrorLogger.Log(ex, NameOf(ReportsForm) & "." & NameOf(RunReport))
        End Try
    End Sub

    Private Shared Sub ApplyDailyGridColumns(dgv As DataGridView)
        If dgv.Columns.Count = 0 Then Return

        If dgv.Columns.Contains("sale_day") Then
            dgv.Columns("sale_day").HeaderText = "Date"
        End If

        If dgv.Columns.Contains("sale_count") Then
            dgv.Columns("sale_count").HeaderText = "Sales"
        End If

        If dgv.Columns.Contains("revenue") Then
            dgv.Columns("revenue").HeaderText = "Revenue"
            dgv.Columns("revenue").DefaultCellStyle.Format = "N2"
        End If
    End Sub

    Private Shared Sub ApplyTopGridColumns(dgv As DataGridView)
        If dgv.Columns.Count = 0 Then Return

        If dgv.Columns.Contains("product_name") Then
            dgv.Columns("product_name").HeaderText = "Product"
        End If

        If dgv.Columns.Contains("qty") Then
            dgv.Columns("qty").HeaderText = "Qty"
        End If

        If dgv.Columns.Contains("revenue") Then
            dgv.Columns("revenue").HeaderText = "Revenue"
            dgv.Columns("revenue").DefaultCellStyle.Format = "N2"
        End If
    End Sub

    Private Function WrapInCard(title As String, grid As DataGridView) As Panel
        grid.Dock = DockStyle.Fill

        Dim lbl As New Label() With {
            .Text = title,
            .Dock = DockStyle.Top,
            .Height = 28,
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .ForeColor = UiTheme.TextPrimary
        }

        Dim card As Panel = UiTheme.CreateCardPanel(New Padding(8))
        card.Dock = DockStyle.Fill
        Dim host As Panel = UiTheme.GetCardContentHost(card)
        host.Controls.Add(grid)
        host.Controls.Add(lbl)
        lbl.BringToFront()

        Return card
    End Function

End Class
