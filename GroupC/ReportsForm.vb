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
        UiTheme.ApplyStandardWindowChrome(Me)

        Me.Text = "Group C - Reports"
        Me.MinimumSize = New Size(760, 560)
        Me.Size = New Size(880, 640)
        Me.StartPosition = FormStartPosition.CenterParent

        statusClearTimer = New Timer() With {.Interval = FormStatusHelper.StatusShowMilliseconds}

        Try
            DatabaseInitializer.EnsureDatabase()
        Catch
        End Try

        tabReports = New TabControl() With {.Dock = DockStyle.Fill}

        Dim tabSales As New TabPage("Sales reports")
        tabSales.BackColor = UiTheme.FormBackground
        tabSales.Padding = New Padding(8)

        Dim root As New TableLayoutPanel()
        root.Dock = DockStyle.Fill
        root.Padding = New Padding(4)
        root.BackColor = UiTheme.FormBackground
        root.ColumnCount = 1
        root.RowCount = 4
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim title As New Label() With {
            .Text = "SALES REPORTS",
            .Font = New Font("Segoe UI", 14.0F, FontStyle.Bold),
            .ForeColor = UiTheme.TextPrimary,
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
        UiTheme.ApplySecondaryAccentButton(btnRun)

        filter.Controls.Add(New Label() With {.Text = "From", .AutoSize = True, .Margin = New Padding(0, 6, 8, 6), .ForeColor = UiTheme.TextSecondary}, 0, 0)
        filter.Controls.Add(dtpFrom, 1, 0)
        filter.Controls.Add(New Label() With {.Text = "To", .AutoSize = True, .Margin = New Padding(12, 6, 8, 6), .ForeColor = UiTheme.TextSecondary}, 2, 0)
        filter.Controls.Add(dtpTo, 3, 0)
        filter.Controls.Add(btnRun, 4, 0)

        lblSummary = New Label() With {.AutoSize = True, .ForeColor = UiTheme.TextSecondary, .Margin = New Padding(0, 8, 0, 4), .Dock = DockStyle.Top}

        Dim filterStack As New TableLayoutPanel() With {.AutoSize = True, .ColumnCount = 1, .RowCount = 2}
        filterStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        filterStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        filterStack.Controls.Add(filter, 0, 0)
        filterStack.Controls.Add(lblSummary, 0, 1)

        Dim filterCard As Panel = UiTheme.CreateCardPanel(New Padding(12))
        Dim filterCardInner As Panel = UiTheme.GetCardContentHost(filterCard)
        filterCard.Dock = DockStyle.Top
        filterCard.AutoSize = True
        filterCardInner.Controls.Add(filterStack)
        filterStack.Dock = DockStyle.Top

        dgvDaily = New DataGridView() With {.Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False}
        dgvTop = New DataGridView() With {.Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False}
        UiTheme.ApplyDataGridViewChrome(dgvDaily)
        UiTheme.ApplyDataGridViewChrome(dgvTop)

        Dim split As New SplitContainer() With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal, .SplitterDistance = 220}
        split.BackColor = UiTheme.FormBackground
        split.Panel1.BackColor = UiTheme.FormBackground
        split.Panel2.BackColor = UiTheme.FormBackground
        Dim l1 As New Label() With {
            .Text = "Sales by day",
            .Dock = DockStyle.Top,
            .Height = 26,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = UiTheme.TextPrimary
        }
        Dim l2 As New Label() With {
            .Text = "Top products (qty) in range",
            .Dock = DockStyle.Top,
            .Height = 26,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .ForeColor = UiTheme.TextPrimary
        }

        Dim dailyCard As Panel = UiTheme.CreateCardPanel(New Padding(8))
        Dim dailyInner As Panel = UiTheme.GetCardContentHost(dailyCard)
        dailyCard.Dock = DockStyle.Fill
        dailyInner.Controls.Add(dgvDaily)
        dailyInner.Controls.Add(l1)
        l1.BringToFront()

        Dim topCard As Panel = UiTheme.CreateCardPanel(New Padding(8))
        Dim topInner As Panel = UiTheme.GetCardContentHost(topCard)
        topCard.Dock = DockStyle.Fill
        topInner.Controls.Add(dgvTop)
        topInner.Controls.Add(l2)
        l2.BringToFront()

        split.Panel1.Controls.Add(dailyCard)
        split.Panel2.Controls.Add(topCard)

        root.Controls.Add(title, 0, 0)
        root.Controls.Add(filterCard, 0, 1)
        root.Controls.Add(split, 0, 2)

        tabSales.Controls.Add(root)
        tabReports.TabPages.Add(tabSales)

        If AppSession.IsAdmin() Then
            tabAuditPage = New TabPage("Audit log")
            tabAuditPage.BackColor = UiTheme.FormBackground
            tabAuditPage.Padding = New Padding(10)

            Dim auditRoot As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 3,
                .BackColor = UiTheme.FormBackground
            }
            auditRoot.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            auditRoot.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            auditRoot.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

            Dim auditFilter As New FlowLayoutPanel() With {.AutoSize = True, .WrapContents = False}
            auditFilter.Controls.Add(New Label() With {.Text = "From", .AutoSize = True, .Margin = New Padding(0, 10, 8, 8), .ForeColor = UiTheme.TextSecondary})
            dtpAuditFrom = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 120, .Value = DateTime.Today.AddDays(-7)}
            auditFilter.Controls.Add(dtpAuditFrom)
            auditFilter.Controls.Add(New Label() With {.Text = "To", .AutoSize = True, .Margin = New Padding(16, 10, 8, 8), .ForeColor = UiTheme.TextSecondary})
            dtpAuditTo = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 120, .Value = DateTime.Today}
            auditFilter.Controls.Add(dtpAuditTo)
            btnAuditRefresh = New Button() With {.Text = "&Load log", .AutoSize = True, .Margin = New Padding(16, 6, 0, 0)}
            UiTheme.ApplySecondaryAccentButton(btnAuditRefresh)
            auditFilter.Controls.Add(btnAuditRefresh)

            Dim auditHint As New Label() With {
                .Text = "Unified audit trail (product changes, sales, sign-in, settings).",
                .AutoSize = True,
                .ForeColor = UiTheme.TextSecondary,
                .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic),
                .Margin = New Padding(0, 4, 0, 8)
            }

            dgvAudit = New DataGridView() With {.Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False}
            UiTheme.ApplyDataGridViewChrome(dgvAudit)

            Dim auditCard As Panel = UiTheme.CreateCardPanel(New Padding(8))
            Dim auditInner As Panel = UiTheme.GetCardContentHost(auditCard)
            auditCard.Dock = DockStyle.Fill
            auditInner.Controls.Add(dgvAudit)

            auditRoot.Controls.Add(auditFilter, 0, 0)
            auditRoot.Controls.Add(auditHint, 0, 1)
            auditRoot.Controls.Add(auditCard, 0, 2)

            tabAuditPage.Controls.Add(auditRoot)
            tabReports.TabPages.Add(tabAuditPage)
        End If

        statusStrip = New StatusStrip()
        statusLabel = New ToolStripStatusLabel(FormStatusHelper.ReadyText)
        statusLabel.Spring = True
        statusStrip.Items.Add(statusLabel)
        UiTheme.ApplyStatusStripTheme(statusStrip)

        Dim shell As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .RowCount = 2, .ColumnCount = 1}
        shell.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        shell.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        shell.Controls.Add(tabReports, 0, 0)
        shell.Controls.Add(statusStrip, 0, 1)

        Me.Controls.Add(shell)
        RunReport()
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

End Class
