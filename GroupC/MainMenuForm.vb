Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient

Public Class MainMenuForm

    Private Class ChartDayPoint
        Public Property Day As DateTime
        Public Property Amount As Decimal
    End Class

    Private Enum DashboardChartSort
        DateAscending = 0
        DateDescending = 1
        AmountDescending = 2
        AmountAscending = 3
    End Enum

    Private Const MaxChartDays As Integer = 90
    Private Const ChartPresetLast7 As String = "Last 7 days"
    Private Const ChartPresetLast14 As String = "Last 14 days"
    Private Const ChartPresetLast30 As String = "Last 30 days"
    Private Const ChartPresetThisMonth As String = "This month"
    Private Const ChartPresetCustom As String = "Custom range"

    Private WithEvents btnProducts As Button
    Private WithEvents btnCategories As Button
    Private WithEvents btnCashierAccounts As Button
    Private WithEvents btnSales As Button
    Private WithEvents btnReceipt As Button
    Private WithEvents btnSettings As Button
    Private WithEvents btnReports As Button
    Private WithEvents btnBackup As Button
    Private WithEvents btnLogout As Button

    Private lblDbHealth As Label
    Private lblDashProducts As Label
    Private lblDashSalesToday As Label
    Private lblDashLastSale As Label
    Private lblDashSevenDay As Label
    Private WithEvents pnlSalesChart As Panel
    Private WithEvents dtpChartFrom As DateTimePicker
    Private WithEvents dtpChartTo As DateTimePicker
    Private WithEvents cmbChartPreset As ComboBox
    Private WithEvents cmbChartSort As ComboBox
    Private WithEvents btnApplyChart As Button
    Private lblChartFilterError As Label
    Private dbHealthTooltip As ToolTip
    Private lastSaleTooltip As ToolTip
    Private WithEvents tmrRefresh As Timer
    Private WithEvents tmrChartRedraw As Timer

    Private ReadOnly chartPoints As New List(Of ChartDayPoint)()
    Private chartCurrencySymbol As String = "₱"
    Private chartPeriodTotal As Decimal
    Private chartRangeStart As Date = Date.Today.AddDays(-6)
    Private chartRangeEnd As Date = Date.Today
    Private chartDataLoaded As Boolean
    Private chartLoadFailed As Boolean
    Private chartRangeTooWide As Boolean
    Private chartEmptyMessage As String
    Private suppressChartPresetEvents As Boolean

    Private statusStrip As StatusStrip
    Private statusLabel As ToolStripStatusLabel

    Private closeDueToLoginFail As Boolean

    Private Sub MainMenuForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' 1. FORM SETUP (Full Screen & Responsive)
        Me.SuspendLayout()
        Me.Text = AppBranding.WindowTitle("Dashboard")
        UiTheme.ApplyMaximizedWorkspaceDefaults(Me, 960, 600)

        ' THE FIX: Hide the Main Menu completely before Login
        Me.Opacity = 0
        Me.ShowInTaskbar = False

        Try
            UiTheme.ApplyStandardWindowChrome(Me)
        Catch
        End Try

        ' 2. LOGIN VERIFICATION
        Using loginForm As New LoginForm()
            ' Notice we removed "Me" from ShowDialog() so it doesn't inherit invisibility
            If loginForm.ShowDialog() <> DialogResult.OK Then
                closeDueToLoginFail = True
                Return
            End If
        End Using
        closeDueToLoginFail = False

        ' THE FIX: Bring the Main Menu back now that Login is successful!
        Me.Opacity = 1
        Me.ShowInTaskbar = True

        ' 3. DATABASE & PDF INIT
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

        If PdfSharp.Fonts.GlobalFontSettings.FontResolver Is Nothing Then
            PdfSharp.Fonts.GlobalFontSettings.FontResolver = New WindowsFontResolver()
        End If

        ' 4. INITIALIZE CONTROLS & BUILD LAYOUT
        InitializeControls()
        SetupResponsiveLayout()

        ' 5. FINAL WIRING
        tmrRefresh = New Timer() With {.Interval = 60000}
        tmrRefresh.Start()

        Me.CancelButton = btnLogout
        RefreshHealthAndDashboard()
        Me.ResumeLayout(True)
    End Sub

    Private Sub MainMenuForm_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        If closeDueToLoginFail Then
            Me.Close()
            Return
        End If
    End Sub

    ' -----------------------------------------------------------
    ' UI BUILDER METHODS
    ' -----------------------------------------------------------
    Private Sub InitializeControls()
        dbHealthTooltip = New ToolTip()

        ' Dashboard Labels
        lblDbHealth = New Label() With {.AutoSize = True, .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold), .Text = "System Status: Loading..."}
        lblDashProducts = CreateDashValueLabel("—")
        lblDashSalesToday = CreateDashValueLabel("—")
        lblDashLastSale = CreateDashValueLabel("—")
        lblDashSevenDay = CreateDashValueLabel("—")

        lastSaleTooltip = New ToolTip()

        ' Chart host — Paint handler keeps vectors sharp on resize (no stretched bitmap)
        pnlSalesChart = New Panel() With {
            .BackColor = UiTheme.CardSurface,
            .Dock = DockStyle.Fill,
            .MinimumSize = New Size(280, 220)
        }

        tmrChartRedraw = New Timer() With {.Interval = 150}

        dtpChartFrom = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 118, .Margin = New Padding(0, 4, 12, 4)}
        dtpChartTo = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Width = 118, .Margin = New Padding(0, 4, 12, 4)}
        dtpChartFrom.Value = DateTime.Today.AddDays(-6)
        dtpChartTo.Value = DateTime.Today

        cmbChartPreset = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 140, .Margin = New Padding(0, 4, 12, 4)}
        cmbChartPreset.Items.AddRange(New Object() {ChartPresetLast7, ChartPresetLast14, ChartPresetLast30, ChartPresetThisMonth, ChartPresetCustom})

        cmbChartSort = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 170, .Margin = New Padding(0, 4, 12, 4)}
        cmbChartSort.Items.AddRange(New Object() {"Date (oldest first)", "Date (newest first)", "Highest day", "Lowest day"})

        btnApplyChart = New Button() With {.Text = "Apply", .AutoSize = True, .MinimumSize = New Size(88, 36), .Margin = New Padding(8, 2, 0, 2)}
        UiTheme.ApplyPrimaryButton(btnApplyChart)

        lblChartFilterError = New Label() With {
            .AutoSize = True,
            .ForeColor = UiTheme.Danger,
            .Visible = False,
            .Margin = New Padding(0, 4, 0, 0)
        }

        suppressChartPresetEvents = True
        cmbChartPreset.SelectedIndex = 0
        cmbChartSort.SelectedIndex = 0
        suppressChartPresetEvents = False

        ' Buttons
        btnProducts = CreateNavButton("&Manage Products")
        btnCategories = CreateNavButton("Manage &Categories")
        btnCashierAccounts = CreateNavButton("Manage &Cashiers")
        btnSales = CreateNavButton("&Point of Sale")
        btnReceipt = CreateNavButton("&Receipt Preview")
        btnReports = CreateNavButton("&Reports")
        btnSettings = CreateNavButton("&Settings")
        btnBackup = CreateNavButton("&Backup / Restore")
        btnLogout = CreateNavButton("Log &out")

        ' THE FIX: Hierarchical theming for a professional look
        Try
            UiTheme.ApplyPrimaryButton(btnSales)
            UiTheme.ApplyPrimaryButton(btnProducts)
            UiTheme.ApplyPrimaryButton(btnCategories)
            UiTheme.ApplyPrimaryButton(btnCashierAccounts)
            UiTheme.ApplySecondaryAccentButton(btnReceipt)
            UiTheme.ApplySecondaryAccentButton(btnReports)
            UiTheme.ApplySecondaryButton(btnSettings)
            UiTheme.ApplySecondaryButton(btnBackup)
            UiTheme.ApplyDangerButton(btnLogout)
        Catch
        End Try

        ApplyRoleBasedNavigation()

        ' Status Strip
        statusStrip = New StatusStrip()
        statusLabel = New ToolStripStatusLabel("SQL Server LocalDB — connection string in App.config (GroupCSqlServer).") With {.Spring = True, .TextAlign = ContentAlignment.MiddleLeft}
        statusStrip.Items.Add(statusLabel)
        statusStrip.Items.Add(New ToolStripStatusLabel(AppBranding.ApplicationName))

        Try
            UiTheme.ApplyStatusStripTheme(statusStrip)
        Catch
        End Try
    End Sub

    Private Sub SetupResponsiveLayout()
        Me.Controls.Clear()

        ' 1. Root Layout: 2 Columns
        ' We use margin 0 to ensure it touches the absolute edges of the screen
        Dim rootTable As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .BackColor = UiTheme.FormBackground,
            .Margin = New Padding(0)
        }
        ' Make the sidebar a bit wider (260px) for a premium feel
        rootTable.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 260.0F))
        rootTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

        ' 2. Left Sidebar (Navigation)
        ' Give it a distinct White background to stand out from the gray dashboard
        Dim navContainer As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .Padding = New Padding(10, 20, 10, 20)
        }

        Dim navLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .BackColor = Color.White,
            .Margin = Padding.Empty
        }
        navLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        navLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        navLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim flowNavMain As New FlowLayoutPanel() With {
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .AutoScroll = True,
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .BackColor = Color.White,
            .Padding = New Padding(0)
        }
        flowNavMain.HorizontalScroll.Enabled = False
        flowNavMain.HorizontalScroll.Visible = False

        Dim flowNavBottom As New FlowLayoutPanel() With {
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .AutoScroll = False,
            .Dock = DockStyle.Bottom,
            .AutoSize = True,
            .BackColor = Color.White,
            .Padding = New Padding(0, 8, 0, 0)
        }
        flowNavBottom.HorizontalScroll.Enabled = False
        flowNavBottom.HorizontalScroll.Visible = False

        Dim mainNavButtons = {btnProducts, btnCategories, btnCashierAccounts, btnSales, btnReceipt, btnReports}
        For Each btn In mainNavButtons
            If btn IsNot Nothing Then
                btn.Width = 200
                btn.Margin = New Padding(10, 5, 10, 10)
                flowNavMain.Controls.Add(btn)
            End If
        Next

        Dim bottomNavButtons = {btnSettings, btnBackup, btnLogout}
        For Each btn In bottomNavButtons
            If btn IsNot Nothing Then
                btn.Width = 200
                btn.Margin = New Padding(10, 5, 10, 10)
                flowNavBottom.Controls.Add(btn)
            End If
        Next

        navLayout.Controls.Add(flowNavMain, 0, 0)
        navLayout.Controls.Add(New Panel() With {.Dock = DockStyle.Fill}, 0, 1)
        navLayout.Controls.Add(flowNavBottom, 0, 2)
        navContainer.Controls.Add(navLayout)

        ' 3. Right Dashboard Container
        ' Generous 30px padding creates modern "breathing room" around the edges
        Dim dashLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .Padding = New Padding(30, 30, 30, 20)
        }
        dashLayout.RowCount = 4
        dashLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize)) ' Header
        dashLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize)) ' Cards
        dashLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize)) ' Chart filters
        dashLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F)) ' Chart

        ' --- Header Section ---
        ' Add a professional title above the database health label
        Dim headerLayout As New TableLayoutPanel() With {
            .AutoSize = True,
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2,
            .Margin = New Padding(0, 0, 0, 25)
        }
        Dim lblTitle As New Label() With {
            .Text = AppBranding.ApplicationName,
            .Font = New Font("Segoe UI", 18.0F, FontStyle.Bold),
            .ForeColor = UiTheme.PrimaryAccent,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, 5)
        }
        lblDbHealth.Margin = New Padding(2, 0, 0, 0) ' Slight indent to align visually
        lblDbHealth.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold) ' Shrink DB health so it isn't distracting

        headerLayout.Controls.Add(lblTitle, 0, 0)
        headerLayout.Controls.Add(lblDbHealth, 0, 1)
        dashLayout.Controls.Add(headerLayout, 0, 0)

        ' --- Metric Cards Section (2×2 KPI grid) ---
        Dim cardsLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 2,
            .Margin = New Padding(0, 0, 0, 20),
            .AutoSize = True
        }
        cardsLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        cardsLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        cardsLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        cardsLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        cardsLayout.Controls.Add(CreateDashCard("Active products", lblDashProducts), 0, 0)
        cardsLayout.Controls.Add(CreateDashCard("Today's sales", lblDashSalesToday), 1, 0)
        cardsLayout.Controls.Add(CreateDashCard("Period sales", lblDashSevenDay), 0, 1)
        cardsLayout.Controls.Add(CreateDashCard("Last sale", lblDashLastSale), 1, 1)

        dashLayout.Controls.Add(cardsLayout, 0, 1)

        ' --- Chart filters ---
        Dim filterCard As Panel = UiTheme.CreateCardPanel(New Padding(12))
        filterCard.Dock = DockStyle.Fill
        filterCard.Margin = New Padding(0, 0, 0, 10)
        Dim filterInner As Panel = UiTheme.GetCardContentHost(filterCard)
        If filterInner IsNot Nothing Then
            Dim filterStack As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .AutoSize = True}
            filterStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            filterStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))

            Dim filterBar As New FlowLayoutPanel() With {
                .AutoSize = True,
                .WrapContents = True,
                .FlowDirection = FlowDirection.LeftToRight,
                .Margin = New Padding(0)
            }
            filterBar.Controls.Add(UiTheme.CreateSecondaryLabel("From"))
            filterBar.Controls.Add(dtpChartFrom)
            filterBar.Controls.Add(UiTheme.CreateSecondaryLabel("To"))
            filterBar.Controls.Add(dtpChartTo)
            filterBar.Controls.Add(UiTheme.CreateSecondaryLabel("Preset"))
            filterBar.Controls.Add(cmbChartPreset)
            filterBar.Controls.Add(UiTheme.CreateSecondaryLabel("Sort"))
            filterBar.Controls.Add(cmbChartSort)
            filterBar.Controls.Add(btnApplyChart)

            filterStack.Controls.Add(filterBar, 0, 0)
            filterStack.Controls.Add(lblChartFilterError, 0, 1)
            filterInner.Controls.Add(filterStack)
        End If

        dashLayout.Controls.Add(filterCard, 0, 2)

        ' --- Chart Section (card + paint panel) ---
        Dim chartCard As Panel = UiTheme.CreateCardPanel(New Padding(12))
        chartCard.Dock = DockStyle.Fill
        chartCard.Margin = New Padding(0)
        chartCard.MinimumSize = New Size(0, 240)
        Dim chartInner As Panel = UiTheme.GetCardContentHost(chartCard)
        If chartInner IsNot Nothing Then
            pnlSalesChart.Dock = DockStyle.Fill
            chartInner.Controls.Add(pnlSalesChart)
        End If

        dashLayout.Controls.Add(chartCard, 0, 3)

        ' 4. Final Assembly
        rootTable.Controls.Add(navContainer, 0, 0)
        rootTable.Controls.Add(dashLayout, 1, 0)

        Me.Controls.Add(rootTable)
        Me.Controls.Add(statusStrip)
    End Sub

    Private Function CreateNavButton(text As String) As Button
        ' THE FIX: Removed the automatic theme application here so we can customize them in InitializeControls
        Dim button As New Button() With {
            .Text = text,
            .Width = 200,
            .Height = 45,
            .Margin = New Padding(10, 5, 10, 10),
            .Cursor = Cursors.Hand
        }
        Return button
    End Function

    Private Function CreateDashCard(title As String, valueLabel As Label) As Panel
        Dim outer As Panel = UiTheme.CreateCardPanel(New Padding(10))
        outer.Margin = New Padding(6, 4, 6, 4)
        outer.MinimumSize = New Size(140, 88)
        outer.Dock = DockStyle.Fill

        Dim inner As Panel = UiTheme.GetCardContentHost(outer)
        If inner Is Nothing Then
            Return outer
        End If

        Dim lblTitle As New Label() With {
            .Text = title,
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
            .ForeColor = UiTheme.TextSecondary,
            .Dock = DockStyle.Top,
            .Height = 22
        }

        valueLabel.Dock = DockStyle.Fill
        valueLabel.TextAlign = ContentAlignment.MiddleLeft
        valueLabel.Font = New Font("Segoe UI", 18.0F, FontStyle.Bold)
        valueLabel.ForeColor = UiTheme.PrimaryAccent

        inner.Controls.Add(lblTitle)
        inner.Controls.Add(valueLabel)
        Return outer
    End Function

    Private Function CreateDashValueLabel(initial As String) As Label
        Return New Label() With {.Text = initial, .AutoSize = False}
    End Function

    ' -----------------------------------------------------------
    ' DATA & CHART METHODS
    ' -----------------------------------------------------------
    Private Sub RefreshHealthAndDashboard()
        Dim lastErr As String = Nothing
        ApplyChartFilterFromControls()

        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()

                lblDbHealth.Text = "System Status: Online"
                lblDbHealth.ForeColor = UiTheme.Success
                dbHealthTooltip.SetToolTip(lblDbHealth, "Connected to " & DatabaseConfig.DatabaseName)

                Dim sym As String = AppSettings.Current.CurrencySymbol

                Dim activeSql As String = "SELECT COUNT(*) FROM products WHERE is_active = 1;"
                Dim activeCount As Integer = 0
                Using cmd As New SqlCommand(activeSql, connection)
                    activeCount = Convert.ToInt32(cmd.ExecuteScalar())
                End Using
                lblDashProducts.Text = activeCount.ToString(CultureInfo.CurrentCulture)

                Dim todaySql As String = "SELECT ISNULL(SUM(total_amount), 0) FROM sales WHERE CAST(sale_date AS DATE) = CAST(GETDATE() AS DATE);"
                Dim todayTotal As Decimal = 0D
                Using cmd As New SqlCommand(todaySql, connection)
                    todayTotal = Convert.ToDecimal(cmd.ExecuteScalar())
                End Using
                lblDashSalesToday.Text = sym & todayTotal.ToString("N2", CultureInfo.CurrentCulture)

                Dim lastSaleSql As String =
                    "SELECT TOP 1 sale_id, sale_date, total_amount FROM sales ORDER BY sale_id DESC;"
                Using cmd As New SqlCommand(lastSaleSql, connection)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        If reader.Read() Then
                            Dim saleId As Integer = Convert.ToInt32(reader("sale_id"))
                            Dim saleWhen As DateTime = Convert.ToDateTime(reader("sale_date"), CultureInfo.InvariantCulture)
                            Dim lastAmt As Decimal = Convert.ToDecimal(reader("total_amount"))
                            Dim lastSaleWhen As String = saleWhen.ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture)
                            lblDashLastSale.Text = "#" & saleId.ToString(CultureInfo.CurrentCulture)
                            lastSaleTooltip.SetToolTip(
                                lblDashLastSale,
                                lastSaleWhen & " · " & sym & lastAmt.ToString("N2", CultureInfo.CurrentCulture))
                        Else
                            lblDashLastSale.Text = "—"
                            lastSaleTooltip.SetToolTip(lblDashLastSale, "No sales recorded yet.")
                        End If
                    End Using
                End Using

                LoadChartDataForRange(connection, sym, chartRangeStart, chartRangeEnd)
                chartLoadFailed = False
            End Using

            If chartRangeTooWide AndAlso Not String.IsNullOrEmpty(chartEmptyMessage) Then
                lblChartFilterError.Text = chartEmptyMessage
                lblChartFilterError.Visible = True
            End If

            chartDataLoaded = True
            InvalidateSalesChart()
        Catch ex As Exception
            lastErr = ex.Message
            lblDbHealth.Text = "System Status: Offline"
            lblDbHealth.ForeColor = UiTheme.Danger
            dbHealthTooltip.SetToolTip(lblDbHealth, lastErr)
            lblDashProducts.Text = "—"
            lblDashSalesToday.Text = "—"
            lblDashLastSale.Text = "—"
            lblDashSevenDay.Text = "—"
            chartDataLoaded = False
            chartLoadFailed = True
            chartPeriodTotal = 0D
            chartPoints.Clear()
            InvalidateSalesChart()
        End Try
    End Sub

    Private Sub btnApplyChart_Click(sender As Object, e As EventArgs) Handles btnApplyChart.Click
        ApplyChartFilterFromControls()
        RefreshHealthAndDashboard()
    End Sub

    Private Sub cmbChartPreset_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbChartPreset.SelectedIndexChanged
        If suppressChartPresetEvents OrElse cmbChartPreset.SelectedItem Is Nothing Then
            Return
        End If

        ApplyChartPresetDates(cmbChartPreset.SelectedItem.ToString())
        If cmbChartPreset.SelectedItem.ToString() <> ChartPresetCustom Then
            ApplyChartFilterFromControls()
            RefreshHealthAndDashboard()
        End If
    End Sub

    Private Sub cmbChartSort_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbChartSort.SelectedIndexChanged
        If Not chartDataLoaded OrElse chartPoints.Count = 0 Then
            Return
        End If

        ApplyChartSort()
        InvalidateSalesChart()
    End Sub

    Private Sub ApplyChartPresetDates(preset As String)
        Dim today As Date = Date.Today
        Select Case preset
            Case ChartPresetLast14
                dtpChartFrom.Value = today.AddDays(-13)
                dtpChartTo.Value = today
            Case ChartPresetLast30
                dtpChartFrom.Value = today.AddDays(-29)
                dtpChartTo.Value = today
            Case ChartPresetThisMonth
                dtpChartFrom.Value = New Date(today.Year, today.Month, 1)
                dtpChartTo.Value = today
            Case ChartPresetCustom
                ' Keep manual dates.
            Case Else
                dtpChartFrom.Value = today.AddDays(-6)
                dtpChartTo.Value = today
        End Select
    End Sub

    Private Sub ApplyChartFilterFromControls()
        chartRangeStart = dtpChartFrom.Value.Date
        chartRangeEnd = dtpChartTo.Value.Date
        If chartRangeEnd < chartRangeStart Then
            Dim swap As Date = chartRangeStart
            chartRangeStart = chartRangeEnd
            chartRangeEnd = swap
        End If

        Dim dayCount As Integer = CInt((chartRangeEnd - chartRangeStart).TotalDays) + 1
        If dayCount > MaxChartDays Then
            lblChartFilterError.Text = "Date range too wide. Select " & MaxChartDays.ToString(CultureInfo.InvariantCulture) & " days or fewer."
            lblChartFilterError.Visible = True
        Else
            lblChartFilterError.Visible = False
            lblChartFilterError.Text = String.Empty
        End If
    End Sub

    Private Sub dtpChartFrom_ValueChanged(sender As Object, e As EventArgs) Handles dtpChartFrom.ValueChanged
        MarkChartPresetCustom()
    End Sub

    Private Sub dtpChartTo_ValueChanged(sender As Object, e As EventArgs) Handles dtpChartTo.ValueChanged
        MarkChartPresetCustom()
    End Sub

    Private Sub MarkChartPresetCustom()
        If suppressChartPresetEvents OrElse cmbChartPreset Is Nothing Then
            Return
        End If

        For i As Integer = 0 To cmbChartPreset.Items.Count - 1
            If String.Equals(cmbChartPreset.Items(i).ToString(), ChartPresetCustom, StringComparison.Ordinal) Then
                If cmbChartPreset.SelectedIndex <> i Then
                    suppressChartPresetEvents = True
                    cmbChartPreset.SelectedIndex = i
                    suppressChartPresetEvents = False
                End If
                Exit For
            End If
        Next
    End Sub

    Private Function GetSelectedChartSort() As DashboardChartSort
        Select Case cmbChartSort.SelectedIndex
            Case 1
                Return DashboardChartSort.DateDescending
            Case 2
                Return DashboardChartSort.AmountDescending
            Case 3
                Return DashboardChartSort.AmountAscending
            Case Else
                Return DashboardChartSort.DateAscending
        End Select
    End Function

    Private Sub ApplyChartSort()
        Select Case GetSelectedChartSort()
            Case DashboardChartSort.DateDescending
                chartPoints.Sort(Function(a, b) b.Day.CompareTo(a.Day))
            Case DashboardChartSort.AmountDescending
                chartPoints.Sort(Function(a, b)
                                     Dim cmp = b.Amount.CompareTo(a.Amount)
                                     If cmp <> 0 Then
                                         Return cmp
                                     End If
                                     Return a.Day.CompareTo(b.Day)
                                 End Function)
            Case DashboardChartSort.AmountAscending
                chartPoints.Sort(Function(a, b)
                                     Dim cmp = a.Amount.CompareTo(b.Amount)
                                     If cmp <> 0 Then
                                         Return cmp
                                     End If
                                     Return a.Day.CompareTo(b.Day)
                                 End Function)
            Case Else
                chartPoints.Sort(Function(a, b) a.Day.CompareTo(b.Day))
        End Select
    End Sub

    Private Sub LoadChartDataForRange(connection As SqlConnection, currencySym As String, rangeStart As Date, rangeEnd As Date)
        chartCurrencySymbol = currencySym
        chartPeriodTotal = 0D
        chartPoints.Clear()
        chartRangeTooWide = False
        chartEmptyMessage = Nothing

        Dim start As Date = rangeStart.Date
        Dim [end] As Date = rangeEnd.Date
        If [end] < start Then
            Dim swap As Date = start
            start = [end]
            [end] = swap
        End If

        Dim dayCount As Integer = CInt(([end] - start).TotalDays) + 1
        If dayCount > MaxChartDays Then
            chartRangeTooWide = True
            chartEmptyMessage = "Date range too wide. Select " & MaxChartDays.ToString(CultureInfo.InvariantCulture) & " days or fewer."
            lblDashSevenDay.Text = "—"
            chartPoints.Clear()
            Return
        End If

        For offset As Integer = 0 To dayCount - 1
            chartPoints.Add(New ChartDayPoint With {.Day = start.AddDays(offset), .Amount = 0D})
        Next

        Dim rangeEndExclusive As Date = [end].AddDays(1)
        Dim aggSql As String =
            "SELECT CAST(sale_date AS DATE) AS sale_day, SUM(total_amount) AS day_total " &
            "FROM sales WHERE sale_date >= @start AND sale_date < @end_ex " &
            "GROUP BY CAST(sale_date AS DATE) ORDER BY sale_day;"

        Using cmd As New SqlCommand(aggSql, connection)
            cmd.Parameters.AddWithValue("@start", start)
            cmd.Parameters.AddWithValue("@end_ex", rangeEndExclusive)
            Using reader As SqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim d As DateTime = Convert.ToDateTime(reader("sale_day"), CultureInfo.InvariantCulture).Date
                    Dim total As Decimal = Convert.ToDecimal(reader("day_total"))
                    For Each pt As ChartDayPoint In chartPoints
                        If pt.Day.Date = d Then
                            pt.Amount = total
                            chartPeriodTotal += total
                            Exit For
                        End If
                    Next
                End While
            End Using
        End Using

        ApplyChartSort()
        lblDashSevenDay.Text = currencySym & chartPeriodTotal.ToString("N2", CultureInfo.CurrentCulture)
    End Sub

    Private Sub InvalidateSalesChart()
        If pnlSalesChart IsNot Nothing AndAlso Not pnlSalesChart.IsDisposed Then
            pnlSalesChart.Invalidate()
        End If
    End Sub

    Private Sub pnlSalesChart_Paint(sender As Object, e As PaintEventArgs) Handles pnlSalesChart.Paint
        PaintSalesChart(e.Graphics, pnlSalesChart.ClientRectangle)
    End Sub

    Private Sub PaintSalesChart(g As Graphics, bounds As Rectangle)
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit
        g.Clear(UiTheme.CardSurface)

        If chartLoadFailed Then
            DrawChartMessage(g, bounds, "Database unavailable", "Reconnect to view sales analytics.", UiTheme.Danger)
            Return
        End If

        If Not chartDataLoaded Then
            DrawChartMessage(g, bounds, "Loading analytics…", Nothing, UiTheme.TextSecondary)
            Return
        End If

        If chartRangeTooWide Then
            Dim detail As String = If(String.IsNullOrEmpty(chartEmptyMessage), "Narrow the date range and click Apply.", chartEmptyMessage)
            DrawChartMessage(g, bounds, "Date range too wide", detail, UiTheme.Danger)
            Return
        End If

        Dim pad As Single = 14.0F
        Dim marginLeft As Single = 58.0F
        Dim marginRight As Single = 16.0F
        Dim headerH As Single = 44.0F
        Dim footerH As Single = 56.0F

        Dim chartRect As New RectangleF(
            marginLeft,
            pad + headerH,
            Math.Max(40.0F, bounds.Width - marginLeft - marginRight),
            Math.Max(40.0F, bounds.Height - pad - headerH - footerH - pad))

        Dim displayStart As Date = chartRangeStart
        Dim displayEnd As Date = chartRangeEnd
        If displayEnd < displayStart Then
            Dim swap As Date = displayStart
            displayStart = displayEnd
            displayEnd = swap
        End If

        Dim subtitle As String =
            displayStart.ToString("MMM d", CultureInfo.CurrentCulture) & " – " &
            displayEnd.ToString("MMM d, yyyy", CultureInfo.CurrentCulture) &
            " · Total " & chartCurrencySymbol & chartPeriodTotal.ToString("N2", CultureInfo.CurrentCulture)

        Using titleFont As New Font("Segoe UI", 11.0F, FontStyle.Bold)
            Using subFont As New Font("Segoe UI", 9.0F, FontStyle.Regular)
                Using titleBrush As New SolidBrush(UiTheme.TextPrimary)
                    Using subBrush As New SolidBrush(UiTheme.TextSecondary)
                        g.DrawString("Daily sales", titleFont, titleBrush, pad, pad)
                        g.DrawString(subtitle, subFont, subBrush, pad, pad + 20.0F)
                    End Using
                End Using
            End Using
        End Using

        If chartPoints.Count = 0 Then
            DrawChartMessage(
                g,
                Rectangle.Round(chartRect),
                "No days in range",
                "Pick a valid from/to date and click Apply.",
                UiTheme.TextSecondary)
            Return
        End If

        If chartPeriodTotal <= 0D Then
            DrawChartMessage(
                g,
                Rectangle.Round(chartRect),
                "No sales in selected period",
                "Totals will appear here after you finalize sales.",
                UiTheme.TextSecondary)
            Return
        End If

        Dim maxVal As Decimal = 0D
        For Each pt As ChartDayPoint In chartPoints
            If pt.Amount > maxVal Then
                maxVal = pt.Amount
            End If
        Next
        If maxVal <= 0D Then
            maxVal = 1D
        End If

        Dim gridColor As Color = Color.FromArgb(40, UiTheme.CardBorder)
        Using gridPen As New Pen(gridColor, 1.0F)
            For tick As Integer = 0 To 4
                Dim y As Single = chartRect.Top + chartRect.Height * (tick / 4.0F)
                g.DrawLine(gridPen, chartRect.Left, y, chartRect.Right, y)
            Next
        End Using

        Using axisFont As New Font("Segoe UI", 8.0F)
            Using axisBrush As New SolidBrush(UiTheme.TextSecondary)
                For tick As Integer = 0 To 4
                    Dim frac As Decimal = 1D - (tick / 4D)
                    Dim tickVal As Decimal = maxVal * frac
                    Dim label As String = FormatCompactMoney(chartCurrencySymbol, tickVal)
                    Dim y As Single = chartRect.Top + chartRect.Height * (tick / 4.0F)
                    g.DrawString(label, axisFont, axisBrush, 4.0F, y - 7.0F)
                Next
            End Using
        End Using

        Dim slotCount As Integer = chartPoints.Count
        Dim slotW As Single = chartRect.Width / Math.Max(1, slotCount)
        Dim barW As Single = Math.Max(2.0F, Math.Min(28.0F, slotW * 0.72F))
        Dim gap As Single = (slotW - barW) / 2.0F
        Dim labelStep As Integer = 1
        If slotCount > 21 Then
            labelStep = CInt(Math.Ceiling(slotCount / 14.0))
        ElseIf slotCount > 14 Then
            labelStep = 2
        End If
        Dim showValueLabels As Boolean = slotCount <= 14

        Using dayFont As New Font("Segoe UI", 8.0F, FontStyle.Regular)
            Using valueFont As New Font("Segoe UI", 7.5F, FontStyle.Regular)
                Using labelBrush As New SolidBrush(UiTheme.TextSecondary)
                    Using todayBrush As New SolidBrush(UiTheme.SecondaryAccent)
                        Using defaultBrush As New SolidBrush(UiTheme.PrimaryAccent)
                            For i As Integer = 0 To slotCount - 1
                                Dim pt As ChartDayPoint = chartPoints(i)
                                Dim day As DateTime = pt.Day
                                Dim amt As Decimal = pt.Amount
                                Dim frac As Single = CSng(amt / maxVal)
                                If frac < 0F Then frac = 0F
                                If frac > 1.0F Then frac = 1.0F

                                Dim barH As Single = frac * chartRect.Height
                                Dim x As Single = chartRect.Left + i * slotW + gap
                                Dim y As Single = chartRect.Bottom - barH
                                Dim isToday As Boolean = day.Date = DateTime.Today

                                Dim barBrush As Brush = If(isToday, todayBrush, defaultBrush)
                                If barH >= 1.0F Then
                                    g.FillRectangle(barBrush, x, y, barW, barH)
                                ElseIf isToday AndAlso amt > 0D Then
                                    g.FillEllipse(barBrush, x + barW / 2.0F - 3.0F, chartRect.Bottom - 6.0F, 6.0F, 6.0F)
                                End If

                                If i Mod labelStep = 0 OrElse i = slotCount - 1 Then
                                    Dim dayLbl As String
                                    If slotCount <= 7 Then
                                        dayLbl = day.ToString("ddd", CultureInfo.CurrentCulture)
                                    ElseIf slotCount <= 31 Then
                                        dayLbl = day.ToString("M/d", CultureInfo.CurrentCulture)
                                    Else
                                        dayLbl = day.ToString("d", CultureInfo.CurrentCulture)
                                    End If
                                    Dim daySize As SizeF = g.MeasureString(dayLbl, dayFont)
                                    g.DrawString(
                                        dayLbl,
                                        dayFont,
                                        If(isToday, todayBrush, labelBrush),
                                        x + (barW - daySize.Width) / 2.0F,
                                        chartRect.Bottom + 8.0F)
                                End If

                                If showValueLabels AndAlso amt > 0D Then
                                    Dim moneyLbl As String = chartCurrencySymbol & amt.ToString("N0", CultureInfo.CurrentCulture)
                                    Dim moneySize As SizeF = g.MeasureString(moneyLbl, valueFont)
                                    Dim labelX As Single = x + (barW - moneySize.Width) / 2.0F
                                    Dim labelYAbove As Single = y - moneySize.Height - 4.0F
                                    If barH > 22.0F AndAlso labelYAbove >= chartRect.Top + 2.0F Then
                                        g.DrawString(moneyLbl, valueFont, labelBrush, labelX, labelYAbove)
                                    Else
                                        g.DrawString(moneyLbl, valueFont, labelBrush, labelX, chartRect.Bottom + 26.0F)
                                    End If
                                End If
                            Next
                        End Using
                    End Using
                End Using
            End Using
        End Using
    End Sub

    Private Shared Sub DrawChartMessage(g As Graphics, bounds As Rectangle, title As String, detail As String, accent As Color)
        Using titleFont As New Font("Segoe UI", 11.0F, FontStyle.Bold)
            Using detailFont As New Font("Segoe UI", 9.5F, FontStyle.Regular)
                Using titleBrush As New SolidBrush(accent)
                    Using detailBrush As New SolidBrush(UiTheme.TextSecondary)
                        Using format As New StringFormat() With {
                            .Alignment = StringAlignment.Center,
                            .LineAlignment = StringAlignment.Near,
                            .Trimming = StringTrimming.EllipsisCharacter
                        }
                            Dim titleSize As SizeF = g.MeasureString(title, titleFont, bounds.Width)
                            Dim detailSize As SizeF = SizeF.Empty
                            If Not String.IsNullOrEmpty(detail) Then
                                detailSize = g.MeasureString(detail, detailFont, bounds.Width)
                            End If

                            Dim blockH As Single = titleSize.Height + If(detailSize.IsEmpty, 0.0F, 6.0F + detailSize.Height)
                            Dim y As Single = bounds.Top + (bounds.Height - blockH) / 2.0F
                            Dim titleRect As New RectangleF(bounds.Left, y, bounds.Width, titleSize.Height + 2.0F)
                            g.DrawString(title, titleFont, titleBrush, titleRect, format)
                            If Not String.IsNullOrEmpty(detail) Then
                                Dim detailRect As New RectangleF(bounds.Left, y + titleSize.Height + 6.0F, bounds.Width, detailSize.Height + 4.0F)
                                g.DrawString(detail, detailFont, detailBrush, detailRect, format)
                            End If
                        End Using
                    End Using
                End Using
            End Using
        End Using
    End Sub

    Private Shared Function FormatCompactMoney(currencySym As String, amount As Decimal) As String
        Dim abs As Decimal = Math.Abs(amount)
        If abs >= 1000000D Then
            Return currencySym & (amount / 1000000D).ToString("0.#", CultureInfo.CurrentCulture) & "M"
        End If
        If abs >= 1000D Then
            Return currencySym & (amount / 1000D).ToString("0.#", CultureInfo.CurrentCulture) & "k"
        End If
        Return currencySym & amount.ToString("N0", CultureInfo.CurrentCulture)
    End Function

    Private Sub tmrRefresh_Tick(sender As Object, e As EventArgs) Handles tmrRefresh.Tick
        RefreshHealthAndDashboard()
    End Sub

    Private Sub tmrChartRedraw_Tick(sender As Object, e As EventArgs) Handles tmrChartRedraw.Tick
        tmrChartRedraw.Stop()
        InvalidateSalesChart()
    End Sub

    Private Sub MainMenuForm_ClientSizeChanged(sender As Object, e As EventArgs) Handles MyBase.ClientSizeChanged
        If pnlSalesChart Is Nothing OrElse tmrChartRedraw Is Nothing Then
            Return
        End If

        tmrChartRedraw.Stop()
        tmrChartRedraw.Start()
    End Sub

    ' -----------------------------------------------------------
    ' BUTTON CLICK HANDLERS
    ' -----------------------------------------------------------
    Private Sub btnProducts_Click(sender As Object, e As EventArgs) Handles btnProducts.Click
        If Not AppSession.RequireAdmin(Me) Then Return

        Me.Hide() ' <-- Hides the Main Menu

        Using form As New ProductsForm()
            form.ShowDialog() ' <-- Opens the Products Form
        End Using

        Me.Show() ' <-- Instantly brings the Main Menu back when Products closes
        RefreshHealthAndDashboard()
    End Sub

    Private Sub btnSales_Click(sender As Object, e As EventArgs) Handles btnSales.Click
        ' 1. Hide the Main Menu so ONLY the Sales Form is visible
        Me.Hide()

        ' 2. Open the Sales Form
        Using form As New SalesForm()
            form.ShowDialog()
        End Using

        ' 3. The Sales Form has closed (user clicked "← Back to Menu").
        ' Show the Main Menu again!
        Me.Show()

        ' Refresh dashboard stats in case they made a sale
        RefreshHealthAndDashboard()
    End Sub

    Private Sub btnReceipt_Click(sender As Object, e As EventArgs) Handles btnReceipt.Click
        Me.Hide()

        Using form As New ReceiptForm()
            form.ShowDialog()
        End Using

        ' Safely show the menu only if it hasn't been destroyed
        If Not Me.IsDisposed Then
            Me.Show()
        End If
    End Sub

    Private Sub btnSettings_Click(sender As Object, e As EventArgs) Handles btnSettings.Click
        If Not AppSession.RequireAdmin(Me) Then Return
        Using form As New SettingsForm()
            form.ShowDialog()
        End Using
        AppSettings.Reload()
        RefreshHealthAndDashboard()
    End Sub

    Private Sub btnReports_Click(sender As Object, e As EventArgs) Handles btnReports.Click
        If Not AppSession.RequireAdmin(Me) Then Return

        ' 1. Hide Main Menu
        Me.Hide()

        ' 2. Open Reports Full Screen
        Using form As New ReportsForm()
            form.ShowDialog()
        End Using

        ' 3. Safely Show Main Menu again when back is clicked
        If Not Me.IsDisposed Then
            Me.Show()
        End If
    End Sub

    Private Sub btnBackup_Click(sender As Object, e As EventArgs) Handles btnBackup.Click
        If Not AppSession.RequireAdmin(Me) Then Return
        Using form As New BackupRestoreForm()
            form.ShowDialog()
        End Using
    End Sub

    Private Sub ApplyRoleBasedNavigation()
        Dim showAdminNav As Boolean = AppSession.IsAdmin()
        If btnProducts IsNot Nothing Then btnProducts.Visible = showAdminNav
        If btnCategories IsNot Nothing Then btnCategories.Visible = showAdminNav
        If btnCashierAccounts IsNot Nothing Then btnCashierAccounts.Visible = showAdminNav
        If btnReports IsNot Nothing Then btnReports.Visible = showAdminNav
        If btnSettings IsNot Nothing Then btnSettings.Visible = showAdminNav
        If btnBackup IsNot Nothing Then btnBackup.Visible = showAdminNav
    End Sub

    Private Sub btnCategories_Click(sender As Object, e As EventArgs) Handles btnCategories.Click
        If Not AppSession.RequireAdmin(Me) Then
            Return
        End If

        Me.Hide()

        Using form As New CategoriesForm()
            form.ShowDialog()
        End Using

        Me.Show()
        RefreshHealthAndDashboard()
    End Sub

    Private Sub btnCashierAccounts_Click(sender As Object, e As EventArgs) Handles btnCashierAccounts.Click
        If Not AppSession.RequireAdmin(Me) Then
            Return
        End If

        Me.Hide()

        Using form As New CashierAccountsForm()
            form.ShowDialog()
        End Using

        Me.Show()
        RefreshHealthAndDashboard()
    End Sub

    Private Sub btnLogout_Click(sender As Object, e As EventArgs) Handles btnLogout.Click
        Try
            AuditLogger.LogAudit("LOGOUT", "Signed out from main menu.", AppSession.GetAuditIdentity())
        Catch
        End Try

        Me.Opacity = 0
        Me.ShowInTaskbar = False

        Using loginForm As New LoginForm()
            If loginForm.ShowDialog() <> DialogResult.OK Then
                Me.Opacity = 1
                Me.ShowInTaskbar = True
                Return
            End If
        End Using

        Me.Opacity = 1
        Me.ShowInTaskbar = True
        ApplyRoleBasedNavigation()
        RefreshHealthAndDashboard()
    End Sub

End Class