Imports System.Drawing
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
    Private dbHealthTooltip As ToolTip
    Private WithEvents tmrRefresh As Timer

    Private statusStrip As StatusStrip
    Private statusLabel As ToolStripStatusLabel
    Private WithEvents flowNav As FlowLayoutPanel

    Private Sub MainMenuForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
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
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 10)

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
        title.ForeColor = UiTheme.Navy
        title.TextAlign = ContentAlignment.MiddleCenter
        title.Height = 44

        Dim helper As New Label()
        helper.Text = "Manage products → run sales → preview receipt"
        helper.AutoSize = False
        helper.Dock = DockStyle.Fill
        helper.Font = New Font("Segoe UI", 9.5F, FontStyle.Italic)
        helper.ForeColor = Color.FromArgb(80, 80, 80)
        helper.TextAlign = ContentAlignment.MiddleCenter
        helper.Height = 28

        Dim dashBoard As New TableLayoutPanel()
        dashBoard.AutoSize = True
        dashBoard.ColumnCount = 1
        dashBoard.RowCount = 2
        dashBoard.Margin = New Padding(0, 0, 0, 8)

        lblDbHealth = New Label()
        lblDbHealth.AutoSize = True
        lblDbHealth.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        lblDbHealth.Text = "Database: checking…"
        lblDbHealth.ForeColor = Color.DimGray

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

        dashBoard.Controls.Add(lblDbHealth, 0, 0)
        dashBoard.Controls.Add(cards, 0, 1)

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
        UiTheme.ApplySecondaryButton(btnExit)
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

        tmrRefresh = New Timer() With {.Interval = 60000}
        tmrRefresh.Start()

        Me.Controls.Clear()
        Me.Controls.Add(root)
        Me.Controls.Add(statusStrip)

        Me.CancelButton = btnExit
        LayoutNavButtons()
        RefreshHealthAndDashboard()
    End Sub

    Private Function CreateDashCard(title As String, valueLabel As Label) As Panel
        Dim panel As New Panel()
        panel.Margin = New Padding(6, 4, 6, 4)
        panel.BorderStyle = BorderStyle.FixedSingle
        panel.BackColor = Color.FromArgb(248, 250, 252)
        panel.Padding = New Padding(10)
        panel.Height = 78
        panel.Dock = DockStyle.Fill

        Dim lblTitle As New Label()
        lblTitle.Text = title
        lblTitle.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        lblTitle.ForeColor = Color.FromArgb(60, 60, 60)
        lblTitle.Dock = DockStyle.Top
        lblTitle.Height = 22

        valueLabel.Dock = DockStyle.Fill
        valueLabel.TextAlign = ContentAlignment.MiddleLeft
        valueLabel.Font = New Font("Segoe UI", 14.0F, FontStyle.Bold)
        valueLabel.ForeColor = UiTheme.Navy

        panel.Controls.Add(lblTitle)
        panel.Controls.Add(valueLabel)
        Return panel
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
                lblDbHealth.ForeColor = Color.ForestGreen
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
            End Using
        Catch ex As Exception
            lastErr = ex.Message
            lblDbHealth.Text = "Database: offline"
            lblDbHealth.ForeColor = Color.Firebrick
            dbHealthTooltip.SetToolTip(lblDbHealth, lastErr)
            lblDashProducts.Text = "—"
            lblDashSalesToday.Text = "—"
            lblDashLastSale.Text = "—"
        End Try
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
        Using form As New SettingsForm()
            form.ShowDialog()
        End Using

        AppSettings.Reload()
        RefreshHealthAndDashboard()
    End Sub

    Private Sub btnReports_Click(sender As Object, e As EventArgs) Handles btnReports.Click
        Using form As New ReportsForm()
            form.ShowDialog()
        End Using
    End Sub

    Private Sub btnBackup_Click(sender As Object, e As EventArgs) Handles btnBackup.Click
        Using form As New BackupRestoreForm()
            form.ShowDialog()
        End Using
    End Sub

    Private Sub btnExit_Click(sender As Object, e As EventArgs) Handles btnExit.Click
        Me.Close()
    End Sub

End Class
