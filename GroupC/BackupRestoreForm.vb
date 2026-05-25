Imports System.Diagnostics
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient

Public Class BackupRestoreForm
    Inherits Form

    Private lblDbName As Label
    Private lblConnection As Label
    Private lblDbFiles As Label
    Private txtDbFiles As TextBox

    Private WithEvents btnRefreshInfo As Button
    Private WithEvents btnOpenDataFolder As Button
    Private WithEvents btnCopyCommands As Button
    Private WithEvents btnCopyMdfLdf As Button
    Private WithEvents btnOpenLocalDbCmd As Button

    Private WithEvents btnCreateBak As Button
    Private WithEvents btnRestoreBak As Button
    Private WithEvents btnOpenSqllocaldbDocs As Button
    Private WithEvents btnClose As Button

    Private txtCommands As TextBox
    Private statusStrip As StatusStrip
    Private statusLabel As ToolStripStatusLabel

    Private Sub BackupRestoreForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = AppBranding.WindowTitle("Backup / Restore")
        Me.MinimumSize = New Size(760, 560)
        Me.Size = New Size(920, 680)
        Me.StartPosition = FormStartPosition.CenterParent
        UiTheme.ApplyStandardWindowChrome(Me)

        Dim header As Panel = UiTheme.CreateCardPanel(New Padding(UiTheme.SpaceLg))
        header.Dock = DockStyle.Top

        Dim lblTitle As Label = UiTheme.CreateHeadingLabel("Backup / Restore", level:=2)
        lblTitle.Margin = New Padding(0, 0, 0, UiTheme.SpaceSm)

        lblDbName = New Label() With {.AutoSize = True, .Font = UiTheme.FontBody, .ForeColor = UiTheme.TextPrimary}
        lblConnection = New Label() With {.AutoSize = True, .Font = UiTheme.FontBodySmall, .ForeColor = UiTheme.TextSecondary, .MaximumSize = New Size(840, 0)}
        lblDbFiles = New Label() With {.AutoSize = True, .Font = UiTheme.FontBodySmall, .ForeColor = UiTheme.TextSecondary, .Text = "Database files (MDF/LDF) location:"}

        txtDbFiles = New TextBox() With {
            .Multiline = True,
            .ReadOnly = True,
            .ScrollBars = ScrollBars.Vertical,
            .Height = 68,
            .Dock = DockStyle.Top,
            .Font = New Font("Consolas", 9.0F),
            .BackColor = UiTheme.CardSurface,
            .ForeColor = UiTheme.TextPrimary,
            .BorderStyle = BorderStyle.FixedSingle
        }

        btnRefreshInfo = New Button() With {.Text = "Refresh info", .AutoSize = True}
        btnOpenDataFolder = New Button() With {.Text = "Open data folder", .AutoSize = True}

        UiTheme.ApplySecondaryButton(btnRefreshInfo)
        UiTheme.ApplySecondaryButton(btnOpenDataFolder)

        Dim headerButtons As FlowLayoutPanel = UiTheme.CreateButtonRow(FlowDirection.LeftToRight)
        headerButtons.Dock = DockStyle.Top
        headerButtons.Margin = New Padding(0, UiTheme.SpaceSm, 0, 0)
        headerButtons.Controls.Add(btnRefreshInfo)
        headerButtons.Controls.Add(btnOpenDataFolder)

        Dim headerHost As Panel = UiTheme.GetCardContentHost(header)
        headerHost.Controls.Add(headerButtons)
        headerHost.Controls.Add(txtDbFiles)
        headerHost.Controls.Add(lblDbFiles)
        headerHost.Controls.Add(lblConnection)
        headerHost.Controls.Add(lblDbName)
        headerHost.Controls.Add(lblTitle)

        Dim body As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .Padding = New Padding(UiTheme.SpaceLg),
            .BackColor = UiTheme.FormBackground
        }
        body.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        body.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))

        Dim backupCard As Panel = UiTheme.CreateCardPanel(New Padding(UiTheme.SpaceLg))
        Dim restoreCard As Panel = UiTheme.CreateCardPanel(New Padding(UiTheme.SpaceLg))
        backupCard.Dock = DockStyle.Fill
        restoreCard.Dock = DockStyle.Fill

        BuildBackupCard(backupCard)
        BuildRestoreCard(restoreCard)

        body.Controls.Add(backupCard, 0, 0)
        body.Controls.Add(restoreCard, 1, 0)

        statusStrip = New StatusStrip()
        statusLabel = New ToolStripStatusLabel(FormStatusHelper.ReadyText) With {.Spring = True, .TextAlign = ContentAlignment.MiddleLeft}
        statusStrip.Items.Add(statusLabel)
        Try
            UiTheme.ApplyStatusStripTheme(statusStrip)
        Catch
        End Try

        btnClose = New Button() With {.Text = "Close", .DialogResult = DialogResult.Cancel, .AutoSize = True}
        UiTheme.ApplySecondaryButton(btnClose)

        Dim footer As FlowLayoutPanel = UiTheme.CreateButtonRow()
        footer.Dock = DockStyle.Bottom
        footer.Padding = New Padding(UiTheme.SpaceLg, 0, UiTheme.SpaceLg, UiTheme.SpaceLg)
        footer.Controls.Add(btnClose)

        Me.Controls.Add(body)
        Me.Controls.Add(footer)
        Me.Controls.Add(statusStrip)
        Me.Controls.Add(header)

        LoadDatabaseInfo()
    End Sub

    Private Sub BuildBackupCard(card As Panel)
        Dim host As Panel = UiTheme.GetCardContentHost(card)

        Dim title As Label = UiTheme.CreateHeadingLabel("Backup (.bak)", level:=3)
        title.Margin = New Padding(0, 0, 0, UiTheme.SpaceSm)

        Dim lblInfo As New Label() With {
            .AutoSize = True,
            .Font = UiTheme.FontBody,
            .ForeColor = UiTheme.TextPrimary,
            .Text = "Creates a standard SQL Server .bak file using BACKUP DATABASE. Recommended."
        }

        btnCreateBak = New Button() With {.Text = "Create backup…", .AutoSize = True, .MinimumSize = New Size(150, UiTheme.ButtonHeightMd)}
        UiTheme.ApplyPrimaryButton(btnCreateBak)

        btnCopyMdfLdf = New Button() With {.Text = "Copy MDF/LDF…", .AutoSize = True}
        UiTheme.ApplySecondaryButton(btnCopyMdfLdf)

        Dim lblCmd As New Label() With {
            .AutoSize = True,
            .Font = UiTheme.FontBodySmall,
            .ForeColor = UiTheme.TextSecondary,
            .Margin = New Padding(0, UiTheme.SpaceMd, 0, UiTheme.SpaceXs),
            .Text = "Commands (for sqlcmd/SSMS):"
        }

        txtCommands = New TextBox() With {
            .Multiline = True,
            .ReadOnly = True,
            .ScrollBars = ScrollBars.Vertical,
            .Dock = DockStyle.Top,
            .Height = 190,
            .Font = New Font("Consolas", 9.0F),
            .BackColor = UiTheme.CardSurface,
            .ForeColor = UiTheme.TextPrimary,
            .BorderStyle = BorderStyle.FixedSingle
        }

        btnCopyCommands = New Button() With {.Text = "Copy commands", .AutoSize = True}
        UiTheme.ApplySecondaryButton(btnCopyCommands)

        Dim row As FlowLayoutPanel = UiTheme.CreateButtonRow(FlowDirection.LeftToRight)
        row.Dock = DockStyle.Top
        row.Margin = New Padding(0, UiTheme.SpaceSm, 0, 0)
        row.Controls.Add(btnCreateBak)
        row.Controls.Add(btnCopyMdfLdf)
        row.Controls.Add(btnCopyCommands)

        host.Controls.Add(row)
        host.Controls.Add(txtCommands)
        host.Controls.Add(lblCmd)
        host.Controls.Add(lblInfo)
        host.Controls.Add(title)
    End Sub

    Private Sub BuildRestoreCard(card As Panel)
        Dim host As Panel = UiTheme.GetCardContentHost(card)

        Dim title As Label = UiTheme.CreateHeadingLabel("Restore (.bak)", level:=3)
        title.Margin = New Padding(0, 0, 0, UiTheme.SpaceSm)

        Dim lblInfo As New Label() With {
            .AutoSize = True,
            .Font = UiTheme.FontBody,
            .ForeColor = UiTheme.TextPrimary,
            .Text = "Restores from a .bak file and overwrites the current database. Close other app instances first."
        }

        btnRestoreBak = New Button() With {.Text = "Restore from backup…", .AutoSize = True, .MinimumSize = New Size(170, UiTheme.ButtonHeightMd)}
        UiTheme.ApplyWarningButton(btnRestoreBak)

        btnOpenLocalDbCmd = New Button() With {.Text = "Open LocalDB CMD", .AutoSize = True}
        UiTheme.ApplySecondaryButton(btnOpenLocalDbCmd)

        btnOpenSqllocaldbDocs = New Button() With {.Text = "Open LocalDB help", .AutoSize = True}
        UiTheme.ApplySecondaryButton(btnOpenSqllocaldbDocs)

        Dim steps As New TextBox() With {
            .Multiline = True,
            .ReadOnly = True,
            .ScrollBars = ScrollBars.Vertical,
            .Dock = DockStyle.Top,
            .Height = 254,
            .Font = UiTheme.FontBodySmall,
            .BackColor = UiTheme.CardSurface,
            .ForeColor = UiTheme.TextPrimary,
            .BorderStyle = BorderStyle.FixedSingle,
            .Text =
                "Restore steps:" & Environment.NewLine &
                "1) Make sure all GroupC app windows are closed (other PCs/users too)." & Environment.NewLine &
                "2) Click 'Restore from backup…' and choose a .bak file." & Environment.NewLine &
                "3) Wait for restore to finish, then reopen the app." & Environment.NewLine &
                Environment.NewLine &
                "If restore fails due to active connections, stop LocalDB:" & Environment.NewLine &
                "  sqllocaldb stop MSSQLLocalDB" & Environment.NewLine &
                "  sqllocaldb start MSSQLLocalDB"
        }

        Dim row As FlowLayoutPanel = UiTheme.CreateButtonRow(FlowDirection.LeftToRight)
        row.Dock = DockStyle.Top
        row.Margin = New Padding(0, UiTheme.SpaceSm, 0, 0)
        row.Controls.Add(btnRestoreBak)
        row.Controls.Add(btnOpenLocalDbCmd)
        row.Controls.Add(btnOpenSqllocaldbDocs)

        host.Controls.Add(row)
        host.Controls.Add(steps)
        host.Controls.Add(lblInfo)
        host.Controls.Add(title)
    End Sub

    Private Sub LoadDatabaseInfo()
        lblDbName.Text = "Database: " & DatabaseConfig.DatabaseName
        lblConnection.Text = "Connection: " & DatabaseConfig.ConnectionString

        Dim pathsText As String = "(Not available yet.)"
        Try
            Dim filePaths As List(Of String) = GetDatabaseFilePaths()
            If filePaths.Count > 0 Then
                pathsText = String.Join(Environment.NewLine, filePaths)
            End If
        Catch ex As Exception
            pathsText = "Error reading DB file paths: " & ex.Message
        End Try
        txtDbFiles.Text = pathsText

        txtCommands.Text = BuildCommandHelpText()
        ShowStatus("Ready.", False)
    End Sub

    Private Function BuildCommandHelpText() As String
        Dim sb As New StringBuilder()

        Dim instance As String = GetLocalDbInstanceName()
        Dim backupPathExample As String = "C:\GroupCBackup\" & DatabaseConfig.DatabaseName & ".bak"

        sb.AppendLine("BACKUP DATABASE [" & DatabaseConfig.DatabaseName & "]")
        sb.AppendLine("TO DISK = N'" & backupPathExample & "'")
        sb.AppendLine("WITH INIT, COPY_ONLY, FORMAT;")
        sb.AppendLine()
        sb.AppendLine("USE [master];")
        sb.AppendLine("ALTER DATABASE [" & DatabaseConfig.DatabaseName & "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;")
        sb.AppendLine("RESTORE DATABASE [" & DatabaseConfig.DatabaseName & "] FROM DISK = N'" & backupPathExample & "' WITH REPLACE;")
        sb.AppendLine("ALTER DATABASE [" & DatabaseConfig.DatabaseName & "] SET MULTI_USER;")
        sb.AppendLine()
        sb.AppendLine("LocalDB:")
        sb.AppendLine("  sqllocaldb info " & instance)
        sb.AppendLine("  sqllocaldb start " & instance)
        sb.AppendLine("  sqllocaldb stop " & instance)

        Return sb.ToString()
    End Function

    Private Sub btnRefreshInfo_Click(sender As Object, e As EventArgs) Handles btnRefreshInfo.Click
        LoadDatabaseInfo()
    End Sub

    Private Sub btnOpenDataFolder_Click(sender As Object, e As EventArgs) Handles btnOpenDataFolder.Click
        Try
            Dim folder As String = GetDatabaseFolderPath()
            If String.IsNullOrWhiteSpace(folder) OrElse Not Directory.Exists(folder) Then
                MessageBox.Show("Database folder is not available. Make sure the database exists and LocalDB is installed.", "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Process.Start(New ProcessStartInfo("explorer.exe", folder) With {.UseShellExecute = True})
        Catch ex As Exception
            MessageBox.Show("Could not open folder: " & ex.Message, "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnCopyCommands_Click(sender As Object, e As EventArgs) Handles btnCopyCommands.Click
        Clipboard.SetText(txtCommands.Text)
        MessageBox.Show("Commands copied to clipboard.", "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub btnCopyMdfLdf_Click(sender As Object, e As EventArgs) Handles btnCopyMdfLdf.Click
        Dim entries As List(Of KeyValuePair(Of String, String))
        Try
            entries = GetDatabaseFileEntries()
        Catch ex As Exception
            MessageBox.Show("Could not read database file paths: " & ex.Message, "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try

        If entries.Count = 0 Then
            MessageBox.Show("Database file paths are not available.", "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using fbd As New FolderBrowserDialog()
            fbd.Description = "Select a folder to copy MDF/LDF into"
            If fbd.ShowDialog(Me) <> DialogResult.OK OrElse String.IsNullOrWhiteSpace(fbd.SelectedPath) Then
                Return
            End If

            Dim stamp As String = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)

            Try
                For Each entry As KeyValuePair(Of String, String) In entries
                    Dim src As String = entry.Value
                    If String.IsNullOrWhiteSpace(src) OrElse Not File.Exists(src) Then
                        Continue For
                    End If

                    Dim ext As String = Path.GetExtension(src)
                    Dim dest As String = Path.Combine(fbd.SelectedPath, DatabaseConfig.DatabaseName & "_" & stamp & ext)
                    File.Copy(src, dest, overwrite:=True)
                Next

                MessageBox.Show("Files copied. If any file failed, close the app and stop LocalDB first.", "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show(
                    "Copy failed: " & ex.Message & Environment.NewLine & Environment.NewLine &
                    "Tip: Close the app and run:" & Environment.NewLine &
                    "  sqllocaldb stop " & GetLocalDbInstanceName() & Environment.NewLine &
                    "Then try again.",
                    "Backup / Restore",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Sub btnOpenLocalDbCmd_Click(sender As Object, e As EventArgs) Handles btnOpenLocalDbCmd.Click
        Try
            Dim instance As String = GetLocalDbInstanceName()
            Dim cmd As String =
                "echo LocalDB instance: " & instance & " & " &
                "echo. & " &
                "echo sqllocaldb info " & instance & " & " &
                "sqllocaldb info " & instance & " & " &
                "echo. & " &
                "echo To stop/start: & " &
                "echo   sqllocaldb stop " & instance & " & " &
                "echo   sqllocaldb start " & instance

            Process.Start(New ProcessStartInfo("cmd.exe", "/k " & cmd) With {.UseShellExecute = True})
        Catch ex As Exception
            MessageBox.Show("Could not open command prompt: " & ex.Message, "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnCreateBak_Click(sender As Object, e As EventArgs) Handles btnCreateBak.Click
        Using sfd As New SaveFileDialog()
            sfd.Filter = "SQL Backup (*.bak)|*.bak|All files (*.*)|*.*"
            sfd.FileName = DatabaseConfig.DatabaseName & "_" & DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture) & ".bak"
            sfd.OverwritePrompt = True
            If sfd.ShowDialog(Me) <> DialogResult.OK Then
                Return
            End If

            Try
                ShowStatus("Creating backup...", True)
                BackupDatabaseToFile(sfd.FileName)
                ShowStatus("Backup created: " & sfd.FileName, False)
                MessageBox.Show("Backup created successfully.", "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                ShowStatus("Backup failed.", False)
                MessageBox.Show("Backup failed: " & ex.Message, "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Sub btnRestoreBak_Click(sender As Object, e As EventArgs) Handles btnRestoreBak.Click
        Using ofd As New OpenFileDialog()
            ofd.Filter = "SQL Backup (*.bak)|*.bak|All files (*.*)|*.*"
            If ofd.ShowDialog(Me) <> DialogResult.OK Then
                Return
            End If

            If MessageBox.Show(
                "Restore from this backup and overwrite the current database?" & Environment.NewLine & Environment.NewLine &
                ofd.FileName,
                "Confirm restore",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) <> DialogResult.OK Then
                Return
            End If

            Try
                ShowStatus("Restoring database...", True)
                RestoreDatabaseFromFile(ofd.FileName)
                ShowStatus("Restore complete.", False)
                MessageBox.Show("Restore complete. Close and reopen the app.", "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                ShowStatus("Restore failed.", False)
                MessageBox.Show("Restore failed: " & ex.Message, "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Sub btnOpenSqllocaldbDocs_Click(sender As Object, e As EventArgs) Handles btnOpenSqllocaldbDocs.Click
        Try
            Process.Start(New ProcessStartInfo("https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb") With {.UseShellExecute = True})
        Catch ex As Exception
            MessageBox.Show("Could not open help: " & ex.Message, "Backup / Restore", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BackupDatabaseToFile(filePath As String)
        Dim folder As String = Path.GetDirectoryName(filePath)
        If Not String.IsNullOrWhiteSpace(folder) AndAlso Not Directory.Exists(folder) Then
            Directory.CreateDirectory(folder)
        End If

        Using connection As New SqlConnection(DatabaseConfig.MasterConnectionString)
            connection.Open()
            Dim sql As String =
                "BACKUP DATABASE [" & DatabaseConfig.DatabaseName & "] " &
                "TO DISK = @path " &
                "WITH INIT, COPY_ONLY, FORMAT;"
            Using cmd As New SqlCommand(sql, connection)
                cmd.CommandTimeout = 0
                cmd.Parameters.AddWithValue("@path", filePath)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Private Sub RestoreDatabaseFromFile(filePath As String)
        Using connection As New SqlConnection(DatabaseConfig.MasterConnectionString)
            connection.Open()

            Dim sql As String =
                "IF DB_ID(@db) IS NULL " &
                "BEGIN " &
                "    CREATE DATABASE [" & DatabaseConfig.DatabaseName & "]; " &
                "END; " &
                "ALTER DATABASE [" & DatabaseConfig.DatabaseName & "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " &
                "RESTORE DATABASE [" & DatabaseConfig.DatabaseName & "] FROM DISK = @path WITH REPLACE; " &
                "ALTER DATABASE [" & DatabaseConfig.DatabaseName & "] SET MULTI_USER;"

            Using cmd As New SqlCommand(sql, connection)
                cmd.CommandTimeout = 0
                cmd.Parameters.AddWithValue("@db", DatabaseConfig.DatabaseName)
                cmd.Parameters.AddWithValue("@path", filePath)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Private Function GetDatabaseFilePaths() As List(Of String)
        Dim result As New List(Of String)()
        Dim entries As List(Of KeyValuePair(Of String, String)) = GetDatabaseFileEntries()
        For Each entry As KeyValuePair(Of String, String) In entries
            result.Add(entry.Key & ": " & entry.Value)
        Next
        Return result
    End Function

    Private Function GetDatabaseFolderPath() As String
        Dim entries As List(Of KeyValuePair(Of String, String)) = GetDatabaseFileEntries()
        For Each entry As KeyValuePair(Of String, String) In entries
            Dim folder As String = Path.GetDirectoryName(entry.Value)
            If Not String.IsNullOrWhiteSpace(folder) Then
                Return folder
            End If
        Next
        Return String.Empty
    End Function

    Private Function GetDatabaseFileEntries() As List(Of KeyValuePair(Of String, String))
        Dim result As New List(Of KeyValuePair(Of String, String))()
        Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
            connection.Open()
            Dim sql As String = "SELECT type_desc, physical_name FROM sys.database_files ORDER BY type_desc;"
            Using cmd As New SqlCommand(sql, connection)
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim typeDesc As String = reader.GetString(0)
                        Dim physicalName As String = reader.GetString(1)
                        result.Add(New KeyValuePair(Of String, String)(typeDesc, physicalName))
                    End While
                End Using
            End Using
        End Using
        Return result
    End Function

    Private Function GetLocalDbInstanceName() As String
        Try
            Dim builder As New SqlConnectionStringBuilder(DatabaseConfig.ConnectionString)
            Dim dataSource As String = builder.DataSource
            If String.IsNullOrWhiteSpace(dataSource) Then
                Return "MSSQLLocalDB"
            End If
            Dim slash As Integer = dataSource.LastIndexOf("\"c)
            If slash >= 0 AndAlso slash < dataSource.Length - 1 Then
                Return dataSource.Substring(slash + 1)
            End If
        Catch
        End Try
        Return "MSSQLLocalDB"
    End Function

    Private Sub ShowStatus(message As String, isBusy As Boolean)
        If statusLabel Is Nothing Then
            Return
        End If

        statusLabel.Text = message
        UseWaitCursor = isBusy
        btnCreateBak.Enabled = Not isBusy
        btnRestoreBak.Enabled = Not isBusy
        btnRefreshInfo.Enabled = Not isBusy
        btnOpenDataFolder.Enabled = Not isBusy
        btnCopyCommands.Enabled = Not isBusy
        btnCopyMdfLdf.Enabled = Not isBusy
        btnOpenLocalDbCmd.Enabled = Not isBusy
        btnOpenSqllocaldbDocs.Enabled = Not isBusy
        Application.DoEvents()
    End Sub

End Class
