Imports System.Drawing
Imports System.Text
Imports System.Windows.Forms

''' <summary>
''' Shows backup/restore guidance and copies sample SQL commands to the clipboard.
''' </summary>
Public Class BackupRestoreForm
    Inherits Form

    Private rtb As RichTextBox
    Private WithEvents btnCopy As Button
    Private WithEvents btnClose As Button

    Private Sub BackupRestoreForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "Group C - Backup / Restore"
        Me.MinimumSize = New Size(560, 420)
        Me.Size = New Size(640, 480)
        Me.StartPosition = FormStartPosition.CenterParent
        UiTheme.ApplyStandardWindowChrome(Me)

        Dim sb As New StringBuilder()
        sb.AppendLine("BACKUP / RESTORE (LocalDB / SQL Server)")
        sb.AppendLine()
        sb.AppendLine("1) Find your database files (MDF path) from SQL Server or use a folder you control, e.g. C:\GroupCBackup")
        sb.AppendLine()
        sb.AppendLine("2) Full backup (run in SSMS or sqlcmd while connected to database " & DatabaseConfig.DatabaseName & "):")
        sb.AppendLine("   BACKUP DATABASE [" & DatabaseConfig.DatabaseName & "] TO DISK = N'C:\GroupCBackup\GroupC_DB.bak' WITH INIT, FORMAT;")
        sb.AppendLine()
        sb.AppendLine("3) Restore (replace paths; requires exclusive access — stop the app first):")
        sb.AppendLine("   RESTORE DATABASE [" & DatabaseConfig.DatabaseName & "] FROM DISK = N'C:\GroupCBackup\GroupC_DB.bak' WITH REPLACE;")
        sb.AppendLine()
        sb.AppendLine("4) LocalDB: ensure the instance is running (sqllocaldb start MSSQLLocalDB). Use connection string from App.config.")
        sb.AppendLine()
        sb.AppendLine("5) For production, prefer maintenance plans or DBA-approved scripts; test restores regularly.")

        rtb = New RichTextBox() With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .Font = New Font("Consolas", 9.0F),
            .Text = sb.ToString(),
            .BorderStyle = BorderStyle.FixedSingle,
            .BackColor = UiTheme.CardSurface,
            .ForeColor = UiTheme.TextPrimary
        }

        Dim bottom As New FlowLayoutPanel() With {.Dock = DockStyle.Bottom, .AutoSize = True, .FlowDirection = FlowDirection.LeftToRight, .Padding = New Padding(8)}
        btnCopy = New Button() With {.Text = "&Copy commands", .AutoSize = True}
        btnClose = New Button() With {.Text = "Close", .DialogResult = DialogResult.Cancel}
        UiTheme.ApplyPrimaryButton(btnCopy)
        UiTheme.ApplySecondaryButton(btnClose)
        bottom.Controls.Add(btnCopy)
        bottom.Controls.Add(btnClose)

        Me.Controls.Add(bottom)
        Me.Controls.Add(rtb)
    End Sub

    Private Sub btnCopy_Click(sender As Object, e As EventArgs) Handles btnCopy.Click
        Clipboard.SetText(rtb.Text)
        MessageBox.Show("Copied to clipboard.", "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

End Class
