Imports System.Drawing
Imports System.Windows.Forms

Public Class MainMenuForm

    Private WithEvents btnProducts As Button
    Private WithEvents btnSales As Button
    Private WithEvents btnReceipt As Button
    Private WithEvents btnExit As Button

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
        Me.MinimumSize = New Size(480, 400)
        Me.Size = New Size(520, 460)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 10)

        Dim root As New TableLayoutPanel()
        root.Dock = DockStyle.Fill
        root.ColumnCount = 1
        root.RowCount = 4
        root.Padding = New Padding(16)
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

        flowNav = New FlowLayoutPanel()
        flowNav.Dock = DockStyle.Fill
        flowNav.FlowDirection = FlowDirection.TopDown
        flowNav.WrapContents = False
        flowNav.AutoScroll = True
        flowNav.Padding = New Padding(0, 12, 0, 8)

        btnProducts = CreateNavButton("&Add / Manage Products")
        btnSales = CreateNavButton("&Sales / Compute Total")
        btnReceipt = CreateNavButton("&Receipt Preview")
        flowNav.Controls.Add(btnProducts)
        flowNav.Controls.Add(btnSales)
        flowNav.Controls.Add(btnReceipt)

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
        root.Controls.Add(flowNav, 0, 2)
        root.Controls.Add(exitPanel, 0, 3)

        statusStrip = New StatusStrip()
        statusStrip.Dock = DockStyle.Bottom
        statusLabel = New ToolStripStatusLabel("SQL Server LocalDB — connection string in App.config (GroupCSqlServer).")
        statusLabel.Spring = True
        statusLabel.TextAlign = ContentAlignment.MiddleLeft
        Dim groupLabel As New ToolStripStatusLabel("Group C")
        statusStrip.Items.Add(statusLabel)
        statusStrip.Items.Add(groupLabel)

        Me.Controls.Clear()
        Me.Controls.Add(root)
        Me.Controls.Add(statusStrip)

        Me.CancelButton = btnExit
        LayoutNavButtons()
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
    End Sub

    Private Sub btnSales_Click(sender As Object, e As EventArgs) Handles btnSales.Click
        Using form As New SalesForm()
            form.ShowDialog()
        End Using
    End Sub

    Private Sub btnReceipt_Click(sender As Object, e As EventArgs) Handles btnReceipt.Click
        Using form As New ReceiptForm()
            form.ShowDialog()
        End Using
    End Sub

    Private Sub btnExit_Click(sender As Object, e As EventArgs) Handles btnExit.Click
        Me.Close()
    End Sub

End Class
