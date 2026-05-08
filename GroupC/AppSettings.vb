Imports System.IO
Imports System.Text.Json

''' <summary>
''' Application settings persisted under LocalApplicationData.
''' </summary>
Public NotInheritable Class AppSettings

    Private Shared ReadOnly LockObj As New Object()
    Private Shared _current As AppSettingsData

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Gets the active settings instance, loading from disk when needed.
    ''' </summary>
    Public Shared ReadOnly Property Current As AppSettingsData
        Get
            SyncLock LockObj
                If _current Is Nothing Then
                    _current = LoadFromDisk()
                End If

                Return _current
            End SyncLock
        End Get
    End Property

    ''' <summary>
    ''' Reloads settings from disk.
    ''' </summary>
    Public Shared Sub Reload()
        SyncLock LockObj
            _current = LoadFromDisk()
        End SyncLock
    End Sub

    ''' <summary>
    ''' Saves settings to disk.
    ''' </summary>
    ''' <param name="data">Settings payload.</param>
    Public Shared Sub Save(data As AppSettingsData)
        SyncLock LockObj
            Dim folder As String = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GroupC")
            Directory.CreateDirectory(folder)
            Dim settingsPath As String = IO.Path.Combine(folder, "settings.json")
            Dim json As String = JsonSerializer.Serialize(data, New JsonSerializerOptions With {.WriteIndented = True})
            File.WriteAllText(settingsPath, json)
            _current = data
        End SyncLock
    End Sub

    Private Shared Function LoadFromDisk() As AppSettingsData
        Try
            Dim settingsPath As String = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GroupC", "settings.json")
            If File.Exists(settingsPath) Then
                Dim json As String = File.ReadAllText(settingsPath)
                Dim parsed As AppSettingsData = JsonSerializer.Deserialize(Of AppSettingsData)(json)
                If parsed IsNot Nothing Then
                    Return Normalize(parsed)
                End If
            End If
        Catch
        End Try

        Return Normalize(New AppSettingsData())
    End Function

    Private Shared Function Normalize(data As AppSettingsData) As AppSettingsData
        If String.IsNullOrWhiteSpace(data.StoreName) Then
            data.StoreName = "GROUP C SALES RECEIPT"
        End If

        If String.IsNullOrWhiteSpace(data.ReceiptFooter) Then
            data.ReceiptFooter = "Thank you for your purchase!"
        End If

        If String.IsNullOrWhiteSpace(data.CurrencySymbol) Then
            data.CurrencySymbol = "₱"
        End If

        Return data
    End Function

End Class

''' <summary>
''' Serializable settings payload.
''' </summary>
Public Class AppSettingsData

    ''' <summary>
    ''' Gets or sets the store name printed on receipts.
    ''' </summary>
    Public Property StoreName As String

    ''' <summary>
    ''' Gets or sets the footer line printed on receipts.
    ''' </summary>
    Public Property ReceiptFooter As String

    ''' <summary>
    ''' Gets or sets the currency symbol displayed with amounts.
    ''' </summary>
    Public Property CurrencySymbol As String

End Class
