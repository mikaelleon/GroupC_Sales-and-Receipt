Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Shared timed status-line updates for forms using a <see cref="ToolStripStatusLabel"/> and <see cref="Timer"/>.
''' </summary>
Public Module FormStatusHelper

    Public Const ReadyText As String = "Ready."
    Public Const StatusShowMilliseconds As Integer = 3000

    ''' <summary>
    ''' Shows a status message and starts (or restarts) the auto-clear timer.
    ''' </summary>
    ''' <param name="label">Target status label.</param>
    ''' <param name="clearTimer">Single-shot style timer; interval is reset to <see cref="StatusShowMilliseconds"/>.</param>
    ''' <param name="message">Message text.</param>
    ''' <param name="isError">When true, uses danger color; otherwise success color.</param>
    Public Sub ShowTimedStatus(label As ToolStripStatusLabel, clearTimer As Timer, message As String, isError As Boolean)
        label.Text = message
        label.ForeColor = If(isError, UiTheme.Danger, UiTheme.Success)
        clearTimer.Stop()
        clearTimer.Interval = StatusShowMilliseconds
        clearTimer.Start()
    End Sub

    ''' <summary>
    ''' Restores the default ready appearance for the status label.
    ''' </summary>
    ''' <param name="label">Target status label.</param>
    Public Sub ResetTimedStatus(label As ToolStripStatusLabel)
        label.Text = ReadyText
        label.ForeColor = UiTheme.TextSecondary
    End Sub

End Module
