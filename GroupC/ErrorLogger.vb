Imports System.IO
Imports Microsoft.Data.SqlClient

''' <summary>
''' Logs exceptions to file and optionally to SQL Server error_log table.
''' </summary>
Public NotInheritable Class ErrorLogger

    Private Shared ReadOnly LockObj As New Object()

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Logs an exception with optional context label.
    ''' </summary>
    ''' <param name="ex">The exception instance.</param>
    ''' <param name="source">Caller context label.</param>
    Public Shared Sub Log(ex As Exception, Optional source As String = Nothing)
        If ex Is Nothing Then
            Return
        End If

        Dim message As String = ex.Message
        Dim stack As String = ex.ToString()
        Dim stamp As String = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")

        Dim line As String = stamp & " | " & If(source, "?") & " | " & message & Environment.NewLine & stack & Environment.NewLine

        SyncLock LockObj
            Try
                Dim baseFolder As String = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GroupC", "logs")
                Directory.CreateDirectory(baseFolder)
                Dim logFilePath As String = IO.Path.Combine(baseFolder, "app.log")
                File.AppendAllText(logFilePath, line & Environment.NewLine)
            Catch
            End Try
        End SyncLock

        TryLogSql(message, stack, source)
    End Sub

    Private Shared Sub TryLogSql(message As String, stack As String, source As String)
        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()
                Dim sql As String =
                    "IF OBJECT_ID('dbo.error_log','U') IS NOT NULL " &
                    "INSERT INTO error_log (source, message, stack_trace) VALUES (@source, @message, @stack);"
                Using cmd As New SqlCommand(sql, connection)
                    cmd.Parameters.AddWithValue("@source", If(source, String.Empty))
                    cmd.Parameters.AddWithValue("@message", If(message, String.Empty))
                    cmd.Parameters.AddWithValue("@stack", If(stack, String.Empty))
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch
        End Try
    End Sub

End Class
