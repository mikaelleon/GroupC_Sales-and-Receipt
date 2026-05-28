Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.IO
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

    Private lblSidebarStoreName As Label
    Private lblStatusDot As Label
    Private lblStatusText As Label
    Private pnlSystemStatus As FlowLayoutPanel
    Private pnlLowStockAlert As Panel
    Private lblDashProducts As Label
    Private lblDashSalesToday As Label
    Private lblDashLastSale As Label
    Private lblDashSevenDay As Label
    Private lblDashLowStock As Label
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
        UiTheme.ApplyMaximizedWorkspaceDefaults(Me, 900, 600)

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

        lblSidebarStoreName = New Label() With {
            .Text = AppSettings.Current.StoreName,
            .Font = UiTheme.FontSubheading,
            .ForeColor = UiTheme.ColTextOnDark,
            .AutoSize = True,
            .Dock = DockStyle.Top,
            .Padding = New Padding(UiTheme.PadCard),
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        }

        lblStatusDot = New Label() With {
            .Text = "●",
            .AutoSize = True,
            .Font = UiTheme.FontCaption,
            .ForeColor = UiTheme.ColTextSecondary,
            .Margin = New Padding(0, 0, UiTheme.PadTight, 0)
        }
        lblStatusText = New Label() With {
            .AutoSize = True,
            .Font = UiTheme.FontCaption,
            .ForeColor = UiTheme.ColTextSecondary,
            .Text = "System Status: Loading..."
        }
        pnlSystemStatus = New FlowLayoutPanel() With {
            .AutoSize = True,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = False,
            .BackColor = Color.Transparent,
            .Margin = New Padding(0)
        }
        pnlSystemStatus.Controls.Add(New Label() With {
            .Text = "System Status:",
            .AutoSize = True,
            .Font = UiTheme.FontCaption,
            .ForeColor = UiTheme.ColTextSecondary,
            .Margin = New Padding(0, 0, UiTheme.PadTight, 0)
        })
        pnlSystemStatus.Controls.Add(lblStatusDot)
        pnlSystemStatus.Controls.Add(lblStatusText)

        lblDashProducts = CreateDashValueLabel("—")
        lblDashSalesToday = CreateDashValueLabel("—")
        lblDashLastSale = CreateDashValueLabel("—")
        lblDashSevenDay = CreateDashValueLabel("—")
        lblDashLowStock = CreateDashValueLabel("—")

        lastSaleTooltip = New ToolTip()

        ' Chart host — Paint handler keeps vectors sharp on resize (no stretched bitmap)
        pnlSalesChart = New Panel() With {
            .BackColor = UiTheme.ColSurface,
            .Dock = DockStyle.Fill,
            .MinimumSize = New Size(280, 220)
        }

        tmrChartRedraw = New Timer() With {.Interval = 150}

        dtpChartFrom = New DateTimePicker() With {
            .Format = DateTimePickerFormat.Short,
            .Width = 118,
            .Margin = New Padding(0, 0, UiTheme.PadControl, 0)
        }
        dtpChartTo = New DateTimePicker() With {
            .Format = DateTimePickerFormat.Short,
            .Width = 118,
            .Margin = New Padding(0, 0, UiTheme.PadControl, 0)
        }
        UiTheme.ApplyInputStyle(dtpChartFrom)
        UiTheme.ApplyInputStyle(dtpChartTo)
        dtpChartFrom.Value = DateTime.Today.AddDays(-6)
        dtpChartTo.Value = DateTime.Today

        cmbChartPreset = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Width = 140,
            .Margin = New Padding(0, 0, UiTheme.PadControl, 0)
        }
        UiTheme.ApplyInputStyle(cmbChartPreset)
        cmbChartPreset.Items.AddRange(New Object() {ChartPresetLast7, ChartPresetLast14, ChartPresetLast30, ChartPresetThisMonth, ChartPresetCustom})

        cmbChartSort = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Width = 170,
            .Margin = New Padding(0, 0, UiTheme.PadControl, 0)
        }
        UiTheme.ApplyInputStyle(cmbChartSort)
        cmbChartSort.Items.AddRange(New Object() {"Date (oldest first)", "Date (newest first)", "Highest day", "Lowest day"})

        btnApplyChart = New Button() With {
            .Text = "Apply",
            .AutoSize = True,
            .MinimumSize = New Size(88, UiTheme.ButtonHeight),
            .Margin = New Padding(UiTheme.PadControl, 0, 0, 0)
        }
        UiTheme.ApplyPrimaryButton(btnApplyChart)

        lblChartFilterError = New Label() With {
            .AutoSize = True,
            .ForeColor = UiTheme.ColDanger,
            .Font = UiTheme.FontCaption,
            .Visible = False,
            .Margin = New Padding(0, UiTheme.PadTight, 0, 0)
        }

        pnlLowStockAlert = New Panel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .Dock = DockStyle.None,
            .BackColor = UiTheme.ColWarningMuted,
            .Padding = New Padding(UiTheme.PadCard, UiTheme.PadControl, UiTheme.PadCard, UiTheme.PadControl),
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl),
            .Visible = False
        }
        Dim lowStockFlow As New FlowLayoutPanel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .Dock = DockStyle.Top,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = False,
            .BackColor = Color.Transparent,
            .Margin = Padding.Empty
        }
        Dim lblLowStockCaption As New Label() With {
            .Text = "Low stock alert:",
            .AutoSize = True,
            .Font = UiTheme.FontCaption,
            .ForeColor = UiTheme.ColTextSecondary,
            .Margin = New Padding(0, 0, UiTheme.PadControl, 0)
        }
        lblDashLowStock.AutoSize = True
        lblDashLowStock.Font = UiTheme.FontBodyBold
        lblDashLowStock.Margin = New Padding(0)
        lowStockFlow.Controls.Add(lblLowStockCaption)
        lowStockFlow.Controls.Add(lblDashLowStock)
        pnlLowStockAlert.Controls.Add(lowStockFlow)

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

        StyleSidebarNavButton(btnProducts)
        StyleSidebarNavButton(btnCategories)
        StyleSidebarNavButton(btnCashierAccounts)
        StyleSidebarNavButton(btnSales)
        StyleSidebarNavButton(btnReceipt)
        StyleSidebarNavButton(btnReports)
        StyleSidebarUtilityButton(btnSettings)
        StyleSidebarUtilityButton(btnBackup)
        StyleSidebarLogoutButton(btnLogout)

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

        Dim rootTable As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .BackColor = UiTheme.ColBackground,
            .Margin = Padding.Empty,
            .Padding = Padding.Empty
        }
        rootTable.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, UiTheme.SidebarWidth))
        rootTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

        Dim sidebar As Panel = UiTheme.BuildSidebar()
        Dim sidebarStack As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .BackColor = UiTheme.ColPrimary,
            .Padding = Padding.Empty
        }
        sidebarStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        sidebarStack.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        sidebarStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim sidebarTop As New Panel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .Dock = DockStyle.Top,
            .BackColor = Color.Transparent
        }

        Dim navMain As New Panel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .Dock = DockStyle.Top,
            .BackColor = Color.Transparent
        }
        Dim mainNavButtons = {btnReports, btnReceipt, btnSales, btnCashierAccounts, btnCategories, btnProducts}
        For Each btn In mainNavButtons
            If btn IsNot Nothing Then
                btn.Dock = DockStyle.Top
                navMain.Controls.Add(btn)
            End If
        Next

        lblSidebarStoreName.Dock = DockStyle.Top
        sidebarTop.Controls.Add(navMain)
        sidebarTop.Controls.Add(lblSidebarStoreName)

        Dim navBottom As New Panel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .Dock = DockStyle.Bottom,
            .BackColor = Color.Transparent,
            .Padding = New Padding(0, UiTheme.PadControl, 0, UiTheme.PadCard)
        }
        navBottom.Controls.Add(UiTheme.CreateSidebarSeparator())
        Dim bottomNavButtons = {btnSettings, btnBackup, btnLogout}
        For Each btn In bottomNavButtons
            If btn IsNot Nothing Then
                btn.Dock = DockStyle.Top
                navBottom.Controls.Add(btn)
            End If
        Next

        sidebarStack.Controls.Add(sidebarTop, 0, 0)
        sidebarStack.Controls.Add(UiTheme.CreateSidebarSpacer(), 0, 1)
        sidebarStack.Controls.Add(navBottom, 0, 2)
        sidebar.Controls.Add(sidebarStack)

        Dim rightColumn As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = UiTheme.ColBackground
        }

        Dim topBar As New Panel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .MinimumSize = New Size(0, UiTheme.TopBarMinHeight),
            .Dock = DockStyle.Top,
            .BackColor = UiTheme.ColSurface,
            .Padding = New Padding(UiTheme.PadPage, UiTheme.PadControl, UiTheme.PadPage, UiTheme.PadControl)
        }
        Dim topBarStack As New TableLayoutPanel() With {
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .ColumnCount = 1,
            .RowCount = 3,
            .Margin = Padding.Empty,
            .BackColor = Color.Transparent
        }
        topBarStack.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        topBarStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        topBarStack.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        topBarStack.RowStyles.Add(New RowStyle(SizeType.Absolute, 1))

        Dim lblPageTitle As New Label() With {
            .Text = "Dashboard",
            .Font = UiTheme.FontDisplay,
            .ForeColor = UiTheme.ColTextPrimary,
            .AutoSize = True,
            .Dock = DockStyle.Fill,
            .Margin = Padding.Empty
        }
        pnlSystemStatus.Margin = New Padding(0, UiTheme.PadTight, 0, 0)
        pnlSystemStatus.Dock = DockStyle.Fill

        topBarStack.Controls.Add(lblPageTitle, 0, 0)
        topBarStack.Controls.Add(pnlSystemStatus, 0, 1)
        topBarStack.Controls.Add(New Panel() With {.Height = 1, .Dock = DockStyle.Fill, .BackColor = UiTheme.ColBorder}, 0, 2)
        topBar.Controls.Add(topBarStack)

        Dim contentArea As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = UiTheme.ColBackground,
            .Padding = New Padding(UiTheme.PadPage)
        }

        Dim contentLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .BackColor = UiTheme.ColBackground
        }
        contentLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        contentLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        contentLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        Dim cardsLayout As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 4,
            .RowCount = 1,
            .Margin = New Padding(0, 0, 0, UiTheme.PadSection),
            .AutoSize = True
        }
        For i As Integer = 0 To 3
            cardsLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25.0F))
        Next
        cardsLayout.Controls.Add(CreateDashCard("Active products", lblDashProducts), 0, 0)
        cardsLayout.Controls.Add(CreateDashCard("Today's sales", lblDashSalesToday), 1, 0)
        cardsLayout.Controls.Add(CreateDashCard("Period sales", lblDashSevenDay), 2, 0)
        cardsLayout.Controls.Add(CreateDashCard("Last sale", lblDashLastSale), 3, 0)

        Dim statsSection As New TableLayoutPanel() With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .Dock = DockStyle.Top,
            .ColumnCount = 1,
            .RowCount = 2,
            .Margin = Padding.Empty
        }
        statsSection.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        statsSection.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        statsSection.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        cardsLayout.Dock = DockStyle.Fill
        cardsLayout.Margin = New Padding(0, 0, 0, UiTheme.PadSection)
        pnlLowStockAlert.Dock = DockStyle.Fill
        statsSection.Controls.Add(pnlLowStockAlert, 0, 0)
        statsSection.Controls.Add(cardsLayout, 0, 1)

        Dim salesCard As Panel = UiTheme.CreateCard(False)
        salesCard.Dock = DockStyle.Fill
        salesCard.Padding = New Padding(UiTheme.PadCard)

        Dim salesHeader As Label = UiTheme.CreateHeadingLabel("Sales Overview", 3)
        salesHeader.Dock = DockStyle.Top
        salesHeader.Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        salesHeader.Font = UiTheme.FontSubheading
        salesHeader.ForeColor = UiTheme.ColTextSecondary

        Dim filterBar As New FlowLayoutPanel() With {
            .AutoSize = True,
            .WrapContents = True,
            .FlowDirection = FlowDirection.LeftToRight,
            .Dock = DockStyle.Top,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl),
            .Padding = Padding.Empty
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

        Dim filterDivider As Panel = UiTheme.CreateDivider()
        filterDivider.Dock = DockStyle.Top
        filterDivider.Margin = New Padding(0, 0, 0, UiTheme.PadControl)

        Dim filterErrorHost As New Panel() With {
            .AutoSize = True,
            .Dock = DockStyle.Top,
            .Margin = New Padding(0, 0, 0, UiTheme.PadControl)
        }
        filterErrorHost.Controls.Add(lblChartFilterError)

        salesCard.Controls.Add(pnlSalesChart)
        salesCard.Controls.Add(filterErrorHost)
        salesCard.Controls.Add(filterDivider)
        salesCard.Controls.Add(filterBar)
        salesCard.Controls.Add(salesHeader)

        Dim futureSpacer As New Panel() With {
            .Dock = DockStyle.Fill,
            .Height = 1,
            .MinimumSize = New Size(0, 0),
            .BackColor = Color.Transparent
        }

        contentLayout.Controls.Add(statsSection, 0, 0)
        contentLayout.Controls.Add(salesCard, 0, 1)
        contentLayout.Controls.Add(futureSpacer, 0, 2)
        contentArea.Controls.Add(contentLayout)

        rightColumn.Controls.Add(contentArea)
        rightColumn.Controls.Add(topBar)

        rootTable.Controls.Add(sidebar, 0, 0)
        rootTable.Controls.Add(rightColumn, 1, 0)

        Me.Controls.Add(rootTable)
        Me.Controls.Add(statusStrip)

        dbHealthTooltip.SetToolTip(pnlSystemStatus, "Checking database connection…")
    End Sub

    Private Sub StyleSidebarNavButton(btn As Button)
        If btn Is Nothing Then
            Return
        End If

        Dim styled As Button = UiTheme.CreateSidebarNavButton(btn.Text)
        btn.FlatStyle = styled.FlatStyle
        btn.BackColor = styled.BackColor
        btn.ForeColor = styled.ForeColor
        btn.Font = styled.Font
        btn.TextAlign = styled.TextAlign
        btn.Padding = styled.Padding
        btn.Cursor = styled.Cursor
        btn.FlatAppearance.BorderSize = 0
        btn.FlatAppearance.MouseOverBackColor = UiTheme.ColPrimaryLight
        btn.UseCompatibleTextRendering = False
        btn.Width = UiTheme.SidebarWidth
        btn.Height = 44
    End Sub

    Private Sub StyleSidebarUtilityButton(btn As Button)
        StyleSidebarNavButton(btn)
        btn.ForeColor = UiTheme.ColTextOnDark
    End Sub

    Private Sub StyleSidebarLogoutButton(btn As Button)
        StyleSidebarNavButton(btn)
        btn.ForeColor = UiTheme.ColDangerLight
        AddHandler btn.MouseEnter, Sub(s, e) btn.BackColor = Color.FromArgb(80, UiTheme.ColDanger)
        AddHandler btn.MouseLeave, Sub(s, e)
                                       If Not Object.Equals(btn.Tag, "active") Then
                                           btn.BackColor = Color.Transparent
                                       End If
                                   End Sub
    End Sub

    Private Function CreateNavButton(text As String) As Button
        Return UiTheme.CreateSidebarNavButton(text)
    End Function

    Private Function CreateDashCard(title As String, valueLabel As Label) As Panel
        Dim card As Panel = UiTheme.CreateCard(True)
        card.Margin = New Padding(UiTheme.PadTight, 0, UiTheme.PadTight, UiTheme.PadControl)
        card.Dock = DockStyle.Fill
        card.MinimumSize = New Size(140, 96)

        Dim lblTitle As New Label() With {
            .Text = title,
            .Font = UiTheme.FontCaption,
            .ForeColor = UiTheme.ColTextSecondary,
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .Margin = New Padding(0, 0, 0, UiTheme.PadTight)
        }

        valueLabel.Dock = DockStyle.Fill
        valueLabel.TextAlign = ContentAlignment.MiddleLeft
        valueLabel.Font = UiTheme.FontHeading
        valueLabel.ForeColor = UiTheme.ColTextPrimary
        valueLabel.AutoSize = False
        valueLabel.MinimumSize = New Size(0, 28)

        card.Controls.Add(valueLabel)
        card.Controls.Add(lblTitle)
        lblTitle.BringToFront()
        Return card
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

                lblStatusText.Text = "Online"
                lblStatusDot.ForeColor = UiTheme.ColAccent
                dbHealthTooltip.SetToolTip(pnlSystemStatus, "Connected to " & DatabaseConfig.DatabaseName)

                If lblSidebarStoreName IsNot Nothing Then
                    lblSidebarStoreName.Text = AppSettings.Current.StoreName
                End If

                Dim sym As String = AppSettings.Current.CurrencySymbol

                Dim activeSql As String = "SELECT COUNT(*) FROM products WHERE is_active = 1;"
                Dim activeCount As Integer = 0
                Using cmd As New SqlCommand(activeSql, connection)
                    activeCount = Convert.ToInt32(cmd.ExecuteScalar())
                End Using
                lblDashProducts.Text = activeCount.ToString(CultureInfo.CurrentCulture)

                Dim todayRange = ReceiptBranding.GetUtcRangeForLocalDay(DateTime.Today)
                Dim todaySql As String =
                    "SELECT ISNULL(SUM(total_amount), 0) FROM sales " &
                    "WHERE ISNULL(is_voided, 0) = 0 AND sale_date >= @start AND sale_date < @end;"
                Dim todayTotal As Decimal = 0D
                Using cmd As New SqlCommand(todaySql, connection)
                    cmd.Parameters.AddWithValue("@start", todayRange.UtcStart)
                    cmd.Parameters.AddWithValue("@end", todayRange.UtcEndExclusive)
                    todayTotal = Convert.ToDecimal(cmd.ExecuteScalar())
                End Using
                lblDashSalesToday.Text = sym & todayTotal.ToString("N2", CultureInfo.CurrentCulture)

                Dim lastSaleSql As String =
                    "SELECT TOP 1 sale_id, sale_date, total_amount FROM sales " &
                    "WHERE ISNULL(is_voided, 0) = 0 ORDER BY sale_id DESC;"
                Using cmd As New SqlCommand(lastSaleSql, connection)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        If reader.Read() Then
                            Dim saleId As Integer = Convert.ToInt32(reader("sale_id"))
                            Dim saleWhen As DateTime = ReceiptBranding.NormalizeStoredSaleDate(
                                Convert.ToDateTime(reader("sale_date"), CultureInfo.InvariantCulture))
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

                Dim lowStockSql As String = "SELECT COUNT(*) FROM products WHERE is_active = 1 AND stock_quantity <= @threshold;"
                Dim lowStockCount As Integer = 0
                Using cmd As New SqlCommand(lowStockSql, connection)
                    cmd.Parameters.AddWithValue("@threshold", AppSettings.Current.StockThreshold)
                    lowStockCount = Convert.ToInt32(cmd.ExecuteScalar())
                End Using
                lblDashLowStock.Text = lowStockCount.ToString(CultureInfo.CurrentCulture)
                lblDashLowStock.ForeColor = If(lowStockCount > 0, UiTheme.ColDanger, UiTheme.ColAccent)
                If pnlLowStockAlert IsNot Nothing Then
                    pnlLowStockAlert.Visible = lowStockCount > 0
                End If

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
            lblStatusText.Text = "Offline"
            lblStatusDot.ForeColor = UiTheme.ColDanger
            dbHealthTooltip.SetToolTip(pnlSystemStatus, lastErr)
            lblDashProducts.Text = "—"
            lblDashSalesToday.Text = "—"
            lblDashLastSale.Text = "—"
            lblDashSevenDay.Text = "—"
            lblDashLowStock.Text = "—"
            lblDashLowStock.ForeColor = UiTheme.ColTextPrimary
            If pnlLowStockAlert IsNot Nothing Then
                pnlLowStockAlert.Visible = False
            End If
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

        Dim utcRange = ReceiptBranding.GetUtcRangeForLocalDates(start, [end])
        Dim aggSql As String =
            "SELECT sale_date, total_amount FROM sales " &
            "WHERE ISNULL(is_voided, 0) = 0 AND sale_date >= @start AND sale_date < @end_ex;"

        Using cmd As New SqlCommand(aggSql, connection)
            cmd.Parameters.AddWithValue("@start", utcRange.UtcStart)
            cmd.Parameters.AddWithValue("@end_ex", utcRange.UtcEndExclusive)
            Using reader As SqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim localDay As Date = ReceiptBranding.NormalizeStoredSaleDate(
                        Convert.ToDateTime(reader("sale_date"), CultureInfo.InvariantCulture)).Date
                    Dim total As Decimal = Convert.ToDecimal(reader("total_amount"))
                    For Each pt As ChartDayPoint In chartPoints
                        If pt.Day.Date = localDay Then
                            pt.Amount += total
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

        Using titleBrush As New SolidBrush(UiTheme.TextPrimary)
            Using subBrush As New SolidBrush(UiTheme.TextSecondary)
                g.DrawString("Daily sales", UiTheme.FontHeading3, titleBrush, pad, pad)
                g.DrawString(subtitle, UiTheme.FontBodySmall, subBrush, pad, pad + 20.0F)
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

        Using axisBrush As New SolidBrush(UiTheme.TextSecondary)
            For tick As Integer = 0 To 4
                Dim frac As Decimal = 1D - (tick / 4D)
                Dim tickVal As Decimal = maxVal * frac
                Dim label As String = FormatCompactMoney(chartCurrencySymbol, tickVal)
                Dim y As Single = chartRect.Top + chartRect.Height * (tick / 4.0F)
                g.DrawString(label, UiTheme.FontCaption, axisBrush, 4.0F, y - 7.0F)
            Next
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
                                    Dim daySize As SizeF = g.MeasureString(dayLbl, UiTheme.FontCaption)
                                    g.DrawString(
                                        dayLbl,
                                        UiTheme.FontCaption,
                                        If(isToday, todayBrush, labelBrush),
                                        x + (barW - daySize.Width) / 2.0F,
                                        chartRect.Bottom + 8.0F)
                                End If

                                If showValueLabels AndAlso amt > 0D Then
                                    Dim moneyLbl As String = chartCurrencySymbol & amt.ToString("N0", CultureInfo.CurrentCulture)
                                    Dim moneySize As SizeF = g.MeasureString(moneyLbl, UiTheme.FontCaption)
                                    Dim labelX As Single = x + (barW - moneySize.Width) / 2.0F
                                    Dim labelYAbove As Single = y - moneySize.Height - 4.0F
                                    If barH > 22.0F AndAlso labelYAbove >= chartRect.Top + 2.0F Then
                                        g.DrawString(moneyLbl, UiTheme.FontCaption, labelBrush, labelX, labelYAbove)
                                    Else
                                        g.DrawString(moneyLbl, UiTheme.FontCaption, labelBrush, labelX, chartRect.Bottom + 26.0F)
                                    End If
                                End If
                            Next
                        End Using
                    End Using
                End Using
    End Sub

    Private Shared Sub DrawChartMessage(g As Graphics, bounds As Rectangle, title As String, detail As String, accent As Color)
        Using titleBrush As New SolidBrush(accent)
            Using detailBrush As New SolidBrush(UiTheme.TextSecondary)
                Using format As New StringFormat() With {
                    .Alignment = StringAlignment.Center,
                    .LineAlignment = StringAlignment.Near,
                    .Trimming = StringTrimming.EllipsisCharacter
                }
                    Dim titleSize As SizeF = g.MeasureString(title, UiTheme.FontHeading3, bounds.Width)
                    Dim detailSize As SizeF = SizeF.Empty
                    If Not String.IsNullOrEmpty(detail) Then
                        detailSize = g.MeasureString(detail, UiTheme.FontBody, bounds.Width)
                    End If

                    Dim blockH As Single = titleSize.Height + If(detailSize.IsEmpty, 0.0F, 6.0F + detailSize.Height)
                    Dim y As Single = bounds.Top + (bounds.Height - blockH) / 2.0F
                    Dim titleRect As New RectangleF(bounds.Left, y, bounds.Width, titleSize.Height + 2.0F)
                    g.DrawString(title, UiTheme.FontHeading3, titleBrush, titleRect, format)
                    If Not String.IsNullOrEmpty(detail) Then
                        Dim detailRect As New RectangleF(bounds.Left, y + titleSize.Height + 6.0F, bounds.Width, detailSize.Height + 4.0F)
                        g.DrawString(detail, UiTheme.FontBody, detailBrush, detailRect, format)
                    End If
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

    Private Sub ShowWorkspaceDialog(factory As Func(Of Form), Optional refreshDashboard As Boolean = True)
        If Me.IsDisposed Then
            Return
        End If

        Me.Hide()
        Try
            Using form As Form = factory()
                form.ShowDialog()
            End Using
        Catch ex As Exception
            ErrorLogger.Log(ex, NameOf(MainMenuForm) & "." & NameOf(ShowWorkspaceDialog))
            MessageBox.Show(
                "Could not open this screen." & Environment.NewLine & ex.Message,
                AppBranding.ApplicationName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        Finally
            If Not Me.IsDisposed Then
                Me.Show()
                Me.ShowInTaskbar = True
            End If
        End Try

        If Me.IsDisposed Then
            Return
        End If

        Dim pending As WorkspaceNavigation.Target = WorkspaceNavigation.TryConsumePending()
        If pending <> WorkspaceNavigation.Target.None Then
            OpenWorkspaceTarget(pending)
            Return
        End If

        If refreshDashboard Then
            RefreshHealthAndDashboard()
        End If
    End Sub

    Private Sub OpenWorkspaceTarget(target As WorkspaceNavigation.Target)
        Select Case target
            Case WorkspaceNavigation.Target.Products
                If Not AppSession.RequireAdmin(Me) Then Return
                ShowWorkspaceDialog(Function() New ProductsForm())
            Case WorkspaceNavigation.Target.Categories
                If Not AppSession.RequireAdmin(Me) Then Return
                ShowWorkspaceDialog(Function() New CategoriesForm())
            Case WorkspaceNavigation.Target.Cashiers
                If Not AppSession.RequireAdmin(Me) Then Return
                ShowWorkspaceDialog(Function() New CashierAccountsForm())
            Case WorkspaceNavigation.Target.Sales
                ShowWorkspaceDialog(Function() New SalesForm())
            Case WorkspaceNavigation.Target.Receipt
                ShowWorkspaceDialog(Function() New ReceiptForm(), refreshDashboard:=False)
            Case WorkspaceNavigation.Target.Reports
                If Not AppSession.RequireAdmin(Me) Then Return
                ShowWorkspaceDialog(Function() New ReportsForm(), refreshDashboard:=False)
        End Select
    End Sub

    Private Sub btnProducts_Click(sender As Object, e As EventArgs) Handles btnProducts.Click
        If Not AppSession.RequireAdmin(Me) Then Return
        ShowWorkspaceDialog(Function() New ProductsForm())
    End Sub

    Private Sub btnSales_Click(sender As Object, e As EventArgs) Handles btnSales.Click
        ShowWorkspaceDialog(Function() New SalesForm())
    End Sub

    Private Sub btnReceipt_Click(sender As Object, e As EventArgs) Handles btnReceipt.Click
        ShowWorkspaceDialog(Function() New ReceiptForm(), refreshDashboard:=False)
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
        ShowWorkspaceDialog(Function() New ReportsForm(), refreshDashboard:=False)
    End Sub

    Private Sub btnBackup_Click(sender As Object, e As EventArgs) Handles btnBackup.Click
        If Not AppSession.RequireAdmin(Me) Then Return
        ShowBackupRestoreDialog()
    End Sub

    Private Sub ShowBackupRestoreDialog()
        Using dlg As New Form()
            dlg.Text = AppBranding.WindowTitle("Database Backup / Restore")
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog
            dlg.StartPosition = FormStartPosition.CenterParent
            dlg.MinimizeBox = False
            dlg.MaximizeBox = False
            dlg.ClientSize = New Size(640, 520)
            dlg.BackColor = UiTheme.ColBackground
            UiTheme.ApplyStandardWindowChrome(dlg)

            Dim titleBar As New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 48,
                .BackColor = UiTheme.ColPrimary,
                .Padding = New Padding(UiTheme.PadCard)
            }
            titleBar.Controls.Add(New Label() With {
                .Text = "Database Backup / Restore",
                .Font = UiTheme.FontSubheading,
                .ForeColor = UiTheme.ColTextOnDark,
                .AutoSize = True,
                .Dock = DockStyle.Left
            })

            Dim card As Panel = UiTheme.CreateCard(False)
            card.Dock = DockStyle.Fill
            card.Margin = New Padding(UiTheme.PadPage)
            card.Padding = New Padding(UiTheme.PadCard)

            Dim instructionsHeader As Label = UiTheme.CreateHeadingLabel("Instructions", 3)
            instructionsHeader.Font = UiTheme.FontSubheading
            instructionsHeader.ForeColor = UiTheme.ColTextSecondary
            instructionsHeader.Dock = DockStyle.Top

            Dim stepsPanel As New TableLayoutPanel() With {
                .Dock = DockStyle.Top,
                .AutoSize = True,
                .ColumnCount = 2,
                .Margin = New Padding(0, UiTheme.PadControl, 0, UiTheme.PadSection)
            }
            stepsPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 28.0F))
            stepsPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

            Dim steps As String() = {
                "Ensure SQL Server LocalDB is running (sqllocaldb start MSSQLLocalDB).",
                "Create the backup folder if it does not exist.",
                "Run the copied commands in an elevated Command Prompt or SQLCMD.",
                "Keep backup files in a safe location before attempting restore."
            }
            For i As Integer = 0 To steps.Length - 1
                stepsPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
                stepsPanel.Controls.Add(New Label() With {
                    .Text = (i + 1).ToString(CultureInfo.InvariantCulture) & ".",
                    .Font = UiTheme.FontBodyBold,
                    .ForeColor = UiTheme.ColPrimary,
                    .AutoSize = True,
                    .Margin = New Padding(0, 0, UiTheme.PadControl, UiTheme.PadControl)
                }, 0, i)
                stepsPanel.Controls.Add(New Label() With {
                    .Text = steps(i),
                    .Font = UiTheme.FontBody,
                    .ForeColor = UiTheme.ColTextPrimary,
                    .AutoSize = True,
                    .MaximumSize = New Size(520, 0),
                    .Margin = New Padding(0, 0, 0, UiTheme.PadControl)
                }, 1, i)
            Next

            Dim lblPath As New Label() With {
                .Text = "Backup folder path:",
                .Font = UiTheme.FontBody,
                .ForeColor = UiTheme.ColTextPrimary,
                .AutoSize = True,
                .Dock = DockStyle.Top,
                .Margin = New Padding(0, 0, 0, UiTheme.PadTight)
            }
            Dim txtBackupPath As New TextBox() With {
                .Text = "C:\GroupCBackup",
                .Dock = DockStyle.Top,
                .Margin = New Padding(0, 0, 0, UiTheme.PadSection)
            }
            UiTheme.ApplyInputStyle(txtBackupPath)

            Dim buttonRow As New FlowLayoutPanel() With {
                .Dock = DockStyle.Bottom,
                .FlowDirection = FlowDirection.RightToLeft,
                .AutoSize = True,
                .Padding = New Padding(UiTheme.PadPage),
                .BackColor = UiTheme.ColBackground
            }
            Dim btnRunBackup As New Button() With {.Text = "Run backup now", .AutoSize = True}
            Dim btnSeedDemo As New Button() With {.Text = "Load demo catalog", .AutoSize = True}
            Dim btnCopy As New Button() With {.Text = "Copy commands", .AutoSize = True}
            Dim btnClose As New Button() With {.Text = "Close", .DialogResult = DialogResult.Cancel, .AutoSize = True}
            UiTheme.ApplyPrimaryButton(btnRunBackup)
            UiTheme.ApplySecondaryAccentButton(btnSeedDemo)
            UiTheme.ApplySecondaryAccentButton(btnCopy)
            UiTheme.ApplySecondaryButton(btnClose)
            buttonRow.Controls.Add(btnClose)
            buttonRow.Controls.Add(btnCopy)
            buttonRow.Controls.Add(btnSeedDemo)
            buttonRow.Controls.Add(btnRunBackup)

            card.Controls.Add(txtBackupPath)
            card.Controls.Add(lblPath)
            card.Controls.Add(stepsPanel)
            card.Controls.Add(instructionsHeader)

            Dim contentHost As New Panel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(UiTheme.PadPage),
                .BackColor = UiTheme.ColBackground
            }
            contentHost.Controls.Add(card)

            dlg.Controls.Add(buttonRow)
            dlg.Controls.Add(contentHost)
            dlg.Controls.Add(titleBar)
            dlg.CancelButton = btnClose
            dlg.AcceptButton = btnRunBackup

            AddHandler btnRunBackup.Click,
                Sub(s, ev)
                    Dim backupPath As String = txtBackupPath.Text.Trim()
                    If backupPath.Length = 0 Then
                        backupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GroupCBackup")
                    End If

                    Try
                        Directory.CreateDirectory(backupPath)
                        Dim bakFile As String = Path.Combine(
                            backupPath,
                            DatabaseConfig.DatabaseName & "_" & DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) & ".bak")

                        Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                            connection.Open()
                            Dim backupSql As String =
                                "BACKUP DATABASE [" & DatabaseConfig.DatabaseName & "] TO DISK = @path " &
                                "WITH FORMAT, INIT, NAME = @name, SKIP, NOREWIND, NOUNLOAD, STATS = 10;"
                            Using cmd As New SqlCommand(backupSql, connection)
                                cmd.CommandTimeout = 120
                                cmd.Parameters.AddWithValue("@path", bakFile)
                                cmd.Parameters.AddWithValue("@name", "GroupC Full Backup")
                                cmd.ExecuteNonQuery()
                            End Using
                        End Using

                        AuditLogger.LogAudit("DATABASE_BACKUP", "Backup saved to " & bakFile, AppSession.CurrentRole)
                        MessageBox.Show(
                            "Backup completed successfully." & Environment.NewLine & Environment.NewLine & bakFile,
                            "Backup / Restore",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information)
                    Catch ex As Exception
                        MessageBox.Show(
                            "Backup failed: " & ex.Message & Environment.NewLine & Environment.NewLine &
                            "Try a folder your user account can write to, or copy the SQL commands and run them in sqlcmd.",
                            "Backup / Restore",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning)
                        ErrorLogger.Log(ex, NameOf(MainMenuForm) & ".RunBackup")
                    End Try
                End Sub

            AddHandler btnSeedDemo.Click,
                Sub(s, ev)
                    If Not UiTheme.ConfirmAction("Load demo categories and products? Existing rows are kept.") Then
                        Return
                    End If

                    Try
                        Dim message As String = DatabaseInitializer.SeedDemoCatalog()
                        AuditLogger.LogAudit("DEMO_CATALOG_LOADED", message, AppSession.CurrentRole)
                        MessageBox.Show(message, "Demo catalog", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        RefreshHealthAndDashboard()
                    Catch ex As Exception
                        MessageBox.Show("Could not load demo catalog: " & ex.Message, "Demo catalog", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        ErrorLogger.Log(ex, NameOf(MainMenuForm) & ".SeedDemoCatalog")
                    End Try
                End Sub

            AddHandler btnCopy.Click,
                Sub(s, ev)
                    Dim backupPath As String = txtBackupPath.Text.Trim()
                    If backupPath.Length = 0 Then
                        backupPath = "C:\GroupCBackup"
                    End If

                    Dim commands As String =
                        "-- Backup" & Environment.NewLine &
                        "BACKUP DATABASE [" & DatabaseConfig.DatabaseName & "]" & Environment.NewLine &
                        "TO DISK = N'" & backupPath.TrimEnd("\"c) & "\" & DatabaseConfig.DatabaseName & ".bak'" & Environment.NewLine &
                        "WITH FORMAT, INIT, NAME = N'GroupC Full Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10;" & Environment.NewLine & Environment.NewLine &
                        "-- Restore (replace existing database — use with caution)" & Environment.NewLine &
                        "RESTORE DATABASE [" & DatabaseConfig.DatabaseName & "]" & Environment.NewLine &
                        "FROM DISK = N'" & backupPath.TrimEnd("\"c) & "\" & DatabaseConfig.DatabaseName & ".bak'" & Environment.NewLine &
                        "WITH REPLACE, RECOVERY, STATS = 10;"

                    Try
                        Clipboard.SetText(commands)
                        MessageBox.Show("SQL commands copied to the clipboard.", "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Catch ex As Exception
                        MessageBox.Show("Could not copy to clipboard: " & ex.Message, "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End Try
                End Sub

            dlg.ShowDialog(Me)
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

        ShowWorkspaceDialog(Function() New CategoriesForm())
    End Sub

    Private Sub btnCashierAccounts_Click(sender As Object, e As EventArgs) Handles btnCashierAccounts.Click
        If Not AppSession.RequireAdmin(Me) Then
            Return
        End If

        ShowWorkspaceDialog(Function() New CashierAccountsForm())
    End Sub

    Private Sub btnLogout_Click(sender As Object, e As EventArgs) Handles btnLogout.Click
        If Not UiTheme.ConfirmAction("Sign out and return to the login screen?") Then
            Return
        End If

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