Imports System.Configuration
Imports Microsoft.Data.SqlClient

Public NotInheritable Class DatabaseConfig

    Private Const FallbackConnectionString As String =
        "Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Database=GroupC_DB;TrustServerCertificate=true;"

    Public Const DatabaseName As String = "GroupC_DB"

    Private Sub New()
    End Sub

    Public Shared ReadOnly Property ConnectionString As String
        Get
            Try
                Dim settings As ConnectionStringSettings = ConfigurationManager.ConnectionStrings("GroupCSqlServer")
                If settings IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(settings.ConnectionString) Then
                    Return settings.ConnectionString
                End If
            Catch
            End Try

            Return FallbackConnectionString
        End Get
    End Property

    Public Shared ReadOnly Property MasterConnectionString As String
        Get
            Dim builder As New SqlConnectionStringBuilder(ConnectionString)
            builder.InitialCatalog = "master"
            Return builder.ConnectionString
        End Get
    End Property

End Class
