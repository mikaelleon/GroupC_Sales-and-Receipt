Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient

Public Class MainMenuForm

    Private WithEvents btnProducts As Button
    Private WithEvents btnSales As Button
    Private WithEvents btnReceipt As Button
    Private WithEvents btnSettings As Button
    Private WithEvents btnReports As Button
    Private WithEvents btnBackup As Button
    Private WithEvents btnExit As Button

    Private lblDbHealth As Label
    Private lblDashProducts As Label
    Private lblDashSalesToday As Label
    Private lblDashLastSale As Label
    Private picSalesChart As PictureBox
    Private dbHealthTooltip As ToolTip
    Private WithEvents tmrRefresh As Timer

    Private statusStrip As StatusStrip
    Private statusLabel As ToolStripStatusLabel
    Private WithEvents flowNav As FlowLayoutPanel

    Private closeDueToLoginFail As Boolean

    Private Sub MainMenuForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        UiTheme.ApplyStandardWindowChrome(Me)

        Using loginForm As New LoginForm()
            If loginForm.ShowDialog(Me) <> DialogResult.OK Then
                closeDueToLoginFail = True
                Return
            End If
        End Using

        closeDueToLoginFail = False

        Try
            DatabaseInitializer.EnsureDatabase()
        Catch ex As Exception
            MessageBox.Show(
                "Could not initialize the local database." & Environment.NewLine &
                ex.Message & Environment.NewLine & Environment.NewLine &
                "Make sure SQL Server LocalDB is installed (sqllocaldb info MSSQLLocalDB).",
                "Database",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        End Try

        Me.Text = "Group C - Sales & Receipt System"
        Me.MinimumSize = New Size(520, 560)
        Me.Size = New Size(560, 620)
        Me.StartPosition = FormStartPosition.CenterScreen

        dbHealthTooltip = New ToolTip()

        Dim root As New TableLayoutPanel()
        root.Dock = DockStyle.Fill
        root.ColumnCount = 1
        root.RowCount = 5
        root.Padding = New Padding(16)
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim title As New Label()
        title.Text = "GROUP C SALES & RECEIPT SYSTEM"
        title.AutoSize = False
        title.Dock = DockStyle.Fill
        title.Font = New Font("Segoe UI", 16.0F, FontStyle.Bold)
        title.ForeColor = UiTheme.TextPrimary
        title.TextAlign = ContentAlignment.MiddleCenter
        title.Height = 44

        Dim helper As New Label()
        helper.Text = "Manage products → run sales → preview receipt"
        helper.AutoSize = False
        helper.Dock = DockStyle.Fill
        helper.Font = New Font("Segoe UI", 9.5F, FontStyle.Italic)
        helper.ForeColor = UiTheme.TextSecondary
        helper.TextAlign = ContentAlignment.MiddleCenter
        helper.Height = 28

        Dim dashBoard As New TableLayoutPanel()
        dashBoard.AutoSize = True
        dashBoard.ColumnCount = 1
        dashBoard.RowCount = 3
        dashBoard.Margin = New Padding(0, 0, 0, 8)

        lblDbHealth = New Label()
        lblDbHealth.AutoSize = True
        lblDbHealth.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        lblDbHealth.Text = "Database: checking…"
        lblDbHealth.ForeColor = UiTheme.TextSecondary

        Dim cards As New TableLayoutPanel()
        cards.AutoSize = True
        cards.ColumnCount = 3
        cards.RowCount = 1
        cards.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.33F))
        cards.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.34F))
        cards.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.33F))

        lblDashProducts = CreateDashValueLabel("—")
        lblDashSalesToday = CreateDashValueLabel("—")
        lblDashLastSale = CreateDashValueLabel("—")

        cards.Controls.Add(CreateDashCard("Active products", lblDashProducts), 0, 0)
        cards.Controls.Add(CreateDashCard("Today's sales total", lblDashSalesToday), 1, 0)
        cards.Controls.Add(CreateDashCard("Latest sale #", lblDashLastSale), 2, 0)

        dashBoard.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        dashBoard.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        dashBoard.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        picSalesChart = New PictureBox()
        picSalesChart.Dock = DockStyle.Top
        picSalesChart.Height = 188
        picSalesChart.MinimumSize = New Size(280, 160)
        picSalesChart.Margin = New Padding(0, 10, 0, 0)
        picSalesChart.BackColor = UiTheme.FormBackground

        dashBoard.Controls.Add(lblDbHealth, 0, 0)
        dashBoard.Controls.Add(cards, 0, 1)
        dashBoard.Controls.Add(picSalesChart, 0, 2)

        flowNav = New FlowLayoutPanel()
        flowNav.Dock = DockStyle.Fill
        flowNav.FlowDirection = FlowDirection.TopDown
        flowNav.WrapContents = False
        flowNav.AutoScroll = True
        flowNav.Padding = New Padding(0, 12, 0, 8)

        btnProducts = CreateNavButton("&Add / Manage Products")
        btnSales = CreateNavButton("&Sales / Compute Total")
        btnReceipt = CreateNavButton("&Receipt Preview")
        btnSettings = CreateNavButton("&Settings")
        btnReports = CreateNavButton("&Reports")
        btnBackup = CreateNavButton("&Backup / Restore help")
        flowNav.Controls.Add(btnProducts)
        flowNav.Controls.Add(btnSales)
        flowNav.Controls.Add(btnReceipt)
        flowNav.Controls.Add(btnSettings)
        flowNav.Controls.Add(btnReports)
        flowNav.Controls.Add(btnBackup)

        Dim showAdminNav As Boolean = AppSession.IsAdmin()
        btnProducts.Visible = showAdminNav
        btnReports.Visible = showAdminNav
        btnSettings.Visible = showAdminNav
        btnBackup.Visible = showAdminNav

        Dim exitPanel As New TableLayoutPanel()
        exitPanel.Dock = DockStyle.Fill
        exitPanel.ColumnCount = 3
        exitPanel.RowCount = 1
        exitPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        exitPanel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        exitPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        exitPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        exitPanel.Padding = New Padding(0, 8, 0, 0)

        btnExit = New Button()
        btnExit.Text = "E&xit"
        btnExit.AutoSize = True
        btnExit.MinimumSize = New Size(120, 36)
        btnExit.Margin = New Padding(3)
        UiTheme.ApplyDangerButton(btnExit)
        exitPanel.Controls.Add(btnExit, 1, 0)

        root.Controls.Add(title, 0, 0)
        root.Controls.Add(helper, 0, 1)
        root.Controls.Add(dashBoard, 0, 2)
        root.Controls.Add(flowNav, 0, 3)
        root.Controls.Add(exitPanel, 0, 4)

        statusStrip = New StatusStrip()
        statusStrip.Dock = DockStyle.Bottom
        statusLabel = New ToolStripStatusLabel("SQL Server LocalDB — connection string in App.config (GroupCSqlServer).")
        statusLabel.Spring = True
        statusLabel.TextAlign = ContentAlignment.MiddleLeft
        Dim groupLabel As New ToolStripStatusLabel("Group C")
        statusStrip.Items.Add(statusLabel)
        statusStrip.Items.Add(groupLabel)
        UiTheme.ApplyStatusStripTheme(statusStrip)

        tmrRefresh = New Timer() With {.Interval = 60000}
        tmrRefresh.Start()

        Me.Controls.Clear()
        Me.Controls.Add(root)
        Me.Controls.Add(statusStrip)

        Me.CancelButton = btnExit
        LayoutNavButtons()
        RefreshHealthAndDashboard()
    End Sub

    Private Sub MainMenuForm_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        If closeDueToLoginFail Then
            Me.Close()
        End If
    End Sub

    Private Function CreateDashCard(title As String, valueLabel As Label) As Panel
        Dim outer As Panel = UiTheme.CreateCardPanel(New Padding(10))
        outer.Margin = New Padding(6, 4, 6, 4)
        outer.MinimumSize = New Size(40, 78)
        outer.Dock = DockStyle.Fill

        Dim inner As Panel = UiTheme.GetCardContentHost(outer)

        Dim lblTitle As New Label()
        lblTitle.Text = title
        lblTitle.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        lblTitle.ForeColor = UiTheme.TextSecondary
        lblTitle.Dock = DockStyle.Top
        lblTitle.Height = 22

        valueLabel.Dock = DockStyle.Fill
        valueLabel.TextAlign = ContentAlignment.MiddleLeft
        valueLabel.Font = New Font("Segoe UI", 14.0F, FontStyle.Bold)
        valueLabel.ForeColor = UiTheme.PrimaryAccent

        inner.Controls.Add(lblTitle)
        inner.Controls.Add(valueLabel)
        Return outer
    End Function

    Private Function CreateDashValueLabel(initial As String) As Label
        Dim label As New Label()
        label.Text = initial
        label.AutoSize = False
        Return label
    End Function

    Private Sub RefreshHealthAndDashboard()
        Dim lastErr As String = Nothing

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()
                lblDbHealth.Text = "Database: OK"
                lblDbHealth.ForeColor = UiTheme.Success
                dbHealthTooltip.SetToolTip(lblDbHealth, "Connected to " & DatabaseConfig.DatabaseName)

                Dim sym As String = AppSettings.Current.CurrencySymbol

                Dim activeSql As String = "SELECT COUNT(*) FROM products WHERE is_active = 1;"
                Dim activeCount As Integer = 0
                Using cmd As New SqlCommand(activeSql, connection)
                    activeCount = Convert.ToInt32(cmd.ExecuteScalar())
                End Using
                lblDashProducts.Text = activeCount.ToString(CultureInfo.CurrentCulture)

                Dim todaySql As String =
                    "SELECT ISNULL(SUM(total_amount), 0) FROM sales WHERE CAST(sale_date AS DATE) = CAST(GETDATE() AS DATE);"
                Dim todayTotal As Decimal = 0D
                Using cmd As New SqlCommand(todaySql, connection)
                    todayTotal = Convert.ToDecimal(cmd.ExecuteScalar())
                End Using
                lblDashSalesToday.Text = sym & todayTotal.ToString("N2", CultureInfo.CurrentCulture)

                Dim lastIdSql As String = "SELECT ISNULL(MAX(sale_id), 0) FROM sales;"
                Dim lastId As Integer = 0
                Using cmd As New SqlCommand(lastIdSql, connection)
                    lastId = Convert.ToInt32(cmd.ExecuteScalar())
                End Using
                lblDashLastSale.Text = If(lastId = 0, "—", "#" & lastId.ToString(CultureInfo.CurrentCulture))

                PaintSevenDaySalesChart(connection, sym)
            End Using
        Catch ex As Exception
            lastErr = ex.Message
            lblDbHealth.Text = "Database: offline"
            lblDbHealth.ForeColor = UiTheme.Danger
            dbHealthTooltip.SetToolTip(lblDbHealth, lastErr)
            lblDashProducts.Text = "—"
            lblDashSalesToday.Text = "—"
            lblDashLastSale.Text = "—"
            ClearSalesChart()
        End Try
    End Sub

    Private Sub ClearSalesChart()
        If picSalesChart Is Nothing Then
            Return
        End If

        If picSalesChart.Image IsNot Nothing Then
            picSalesChart.Image.Dispose()
            picSalesChart.Image = Nothing
        End If
    End Sub

    Private Sub PaintSevenDaySalesChart(connection As SqlConnection, currencySym As String)
        If picSalesChart Is Nothing Then
            Return
        End If

        Dim amounts As New Dictionary(Of DateTime, Decimal)()
        For i As Integer = 0 To 6
            amounts(DateTime.Today.AddDays(-6 + i)) = 0D
        Next

        Dim rangeStart As DateTime = DateTime.Today.AddDays(-6)
        Dim rangeEndExclusive As DateTime = DateTime.Today.AddDays(1)
        Dim aggSql As String =
            "SELECT CAST(sale_date AS DATE) AS sale_day, SUM(total_amount) AS day_total " &
            "FROM sales WHERE sale_date >= @start AND sale_date < @end_ex " &
            "GROUP BY CAST(sale_date AS DATE);"

        Using cmd As New SqlCommand(aggSql, connection)
            cmd.Parameters.AddWithValue("@start", rangeStart)
            cmd.Parameters.AddWithValue("@end_ex", rangeEndExclusive)
            Using reader As SqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim d As DateTime = Convert.ToDateTime(reader("sale_day"), CultureInfo.InvariantCulture).Date
                    Dim total As Decimal = Convert.ToDecimal(reader("day_total"))
                    If amounts.ContainsKey(d) Then
                        amounts(d) = total
                    End If
                End While
            End Using
        End Using

        Dim maxVal As Decimal = 1D
        For Each kv As KeyValuePair(Of DateTime, Decimal) In amounts
            If kv.Value > maxVal Then
                maxVal = kv.Value
            End If
        Next

        Dim w As Integer = Math.Max(320, picSalesChart.ClientSize.Width)
        If w < 320 Then
            w = 320
        End If

        Dim h As Integer = Math.Max(160, picSalesChart.Height)
        Dim bmp As New Bitmap(w, h)

        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(UiTheme.FormBackground)

            Dim marginLeft As Single = 54.0F
            Dim marginBottom As Single = 50.0F
            Dim marginTop As Single = 26.0F
            Dim chartRect As New RectangleF(marginLeft, marginTop, w - marginLeft - 12.0F, h - marginBottom - marginTop)

            Using outline As New Pen(Color.FromArgb(140, 148, 158))
                g.DrawRectangle(outline, Rectangle.Round(chartRect))
            End Using

            Using headerBrush As New SolidBrush(UiTheme.TextPrimary)
                Using hf As New Font("Segoe UI", 9.5F, FontStyle.Bold)
                    g.DrawString("Sales — last 7 days", hf, headerBrush, marginLeft, 2.0F)
                End Using
            End Using

            Dim slots As Single = 7.0F
            Dim slotW As Single = chartRect.Width / slots
            Dim barW As Single = slotW * 0.64F
            Dim gap As Single = (slotW - barW) / 2.0F

            Using dayFont As New Font("Segoe UI", 7.5F)
                Using amtFont As New Font("Segoe UI", 7.0F)
                    Using labelBrush As New SolidBrush(UiTheme.TextSecondary)
                        For i As Integer = 0 To 6
                            Dim day As DateTime = DateTime.Today.AddDays(-6 + i)
                            Dim amt As Decimal = amounts(day)
                            Dim frac As Single = CSng(amt / maxVal)
                            If frac > 1.0F Then
                                frac = 1.0F
                            End If

                            Dim barH As Single = frac * chartRect.Height
                            Dim x As Single = chartRect.Left + i * slotW + gap
                            Dim y As Single = chartRect.Bottom - barH

                            Using barBrush As New SolidBrush(UiTheme.PrimaryAccent)
                                If barH > 0.0F Then
                                    g.FillRectangle(barBrush, x, y, barW, barH)
                                End If
                            End Using

                            Dim dayLbl As String = day.ToString("ddd", CultureInfo.CurrentCulture)
                            g.DrawString(dayLbl, dayFont, labelBrush, x - 4.0F, chartRect.Bottom + 6.0F)

                            Dim moneyLbl As String = currencySym & amt.ToString("N0", CultureInfo.CurrentCulture)
                            If barH > 18.0F Then
                                g.DrawString(moneyLbl, amtFont, labelBrush, x, y - 16.0F)
                            Else
                                g.DrawString(moneyLbl, amtFont, labelBrush, x, chartRect.Bottom + 22.0F)
                            End If
                        Next
                    End Using
                End Using
            End Using
        End Using

        If picSalesChart.Image IsNot Nothing Then
            picSalesChart.Image.Dispose()
        End If

        picSalesChart.Image = bmp
    End Sub

    Private Sub tmrRefresh_Tick(sender As Object, e As EventArgs) Handles tmrRefresh.Tick
        RefreshHealthAndDashboard()
    End Sub

    Private Sub FlowNav_Resize(sender As Object, e As EventArgs) Handles flowNav.Resize
        LayoutNavButtons()
    End Sub

    Private Sub LayoutNavButtons()
        If flowNav Is Nothing Then
            Return
        End If

        Dim innerWidth As Integer = flowNav.ClientSize.Width - flowNav.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth
        If innerWidth < 200 Then
            innerWidth = 200
        End If

        For Each ctrl As Control In flowNav.Controls
            Dim button As Button = TryCast(ctrl, Button)
            If button IsNot Nothing Then
                button.Width = innerWidth
            End If
        Next
    End Sub

    Private Function CreateNavButton(text As String) As Button
        Dim button As New Button()
        button.Text = text
        button.Height = 40
        button.Margin = New Padding(0, 0, 0, 10)
        UiTheme.ApplyPrimaryButton(button)
        Return button
    End Function

    Private Sub btnProducts_Click(sender As Object, e As EventArgs) Handles btnProducts.Click
        If Not AppSession.RequireAdmin(Me) Then
            Return
        End If

        Using form As New ProductsForm()
            form.ShowDialog()
        End Using

        RefreshHealthAndDashboard()
    End Sub

    Private Sub btnSales_Click(sender As Object, e As EventArgs) Handles btnSales.Click
        Using form As New SalesForm()
            form.ShowDialog()
        End Using

        RefreshHealthAndDashboard()
    End Sub

    Private Sub btnReceipt_Click(sender As Object, e As EventArgs) Handles btnReceipt.Click
        Using form As New ReceiptForm()
            form.ShowDialog()
        End Using
    End Sub

    Private Sub btnSettings_Click(sender As Object, e As EventArgs) Handles btnSettings.Click
        If Not AppSession.RequireAdmin(Me) Then
            Return
        End If

        Using form As New SettingsForm()
            form.ShowDialog()
        End Using

        AppSettings.Reload()
        RefreshHealthAndDashboard()
    End Sub

    Private Sub btnReports_Click(sender As Object, e As EventArgs) Handles btnReports.Click
        If Not AppSession.RequireAdmin(Me) Then
            Return
        End If

        Using form As New ReportsForm()
            form.ShowDialog()
        End Using
    End Sub

    Private Sub btnBackup_Click(sender As Object, e As EventArgs) Handles btnBackup.Click
        If Not AppSession.RequireAdmin(Me) Then
            Return
        End If

        Using form As New BackupRestoreForm()
            form.ShowDialog()
        End Using
    End Sub

    Private Sub btnExit_Click(sender As Object, e As EventArgs) Handles btnExit.Click
        Me.Close()
    End Sub

End Class
