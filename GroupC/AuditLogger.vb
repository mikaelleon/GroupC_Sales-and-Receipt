Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>
''' Writes audit rows for product lifecycle actions.
''' </summary>
Public NotInheritable Class AuditLogger

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Logs a product-related audit entry when the audit table exists.
    ''' </summary>
    ''' <param name="connection">Open SQL connection.</param>
    ''' <param name="actionCode">Short action label.</param>
    ''' <param name="productId">Product id if applicable.</param>
    ''' <param name="productName">Product name if applicable.</param>
    ''' <param name="detail">Extra detail text.</param>
    Public Shared Sub LogProduct(connection As SqlConnection, actionCode As String, productId As Integer?, productName As String, detail As String)
        Try
            If connection Is Nothing OrElse connection.State <> ConnectionState.Open Then
                Return
            End If

            If ObjectExists(connection, "audit_products") = False Then
                Return
            End If

            Dim sql As String =
                "INSERT INTO audit_products (action_code, product_id, product_name, detail) " &
                "VALUES (@action_code, @product_id, @product_name, @detail);"

            Using cmd As New SqlCommand(sql, connection)
                cmd.Parameters.AddWithValue("@action_code", actionCode)
                If productId.HasValue Then
                    cmd.Parameters.AddWithValue("@product_id", productId.Value)
                Else
                    cmd.Parameters.AddWithValue("@product_id", DBNull.Value)
                End If

                If productName Is Nothing Then
                    cmd.Parameters.AddWithValue("@product_name", DBNull.Value)
                Else
                    cmd.Parameters.AddWithValue("@product_name", productName)
                End If

                cmd.Parameters.AddWithValue("@detail", If(detail, String.Empty))
                cmd.ExecuteNonQuery()
            End Using
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Logs a sale-related audit entry when the audit table exists.
    ''' </summary>
    ''' <param name="connection">Open SQL connection.</param>
    ''' <param name="actionCode">Short action label.</param>
    ''' <param name="saleId">Sale id if applicable.</param>
    ''' <param name="detail">Extra detail text.</param>
    Public Shared Sub LogSale(connection As SqlConnection, actionCode As String, saleId As Integer?, detail As String)
        Try
            If connection Is Nothing OrElse connection.State <> ConnectionState.Open Then
                Return
            End If

            If ObjectExists(connection, "audit_sales") = False Then
                Return
            End If

            Dim sql As String =
                "INSERT INTO audit_sales (action_code, sale_id, detail) " &
                "VALUES (@action_code, @sale_id, @detail);"

            Using cmd As New SqlCommand(sql, connection)
                cmd.Parameters.AddWithValue("@action_code", actionCode)
                If saleId.HasValue Then
                    cmd.Parameters.AddWithValue("@sale_id", saleId.Value)
                Else
                    cmd.Parameters.AddWithValue("@sale_id", DBNull.Value)
                End If

                cmd.Parameters.AddWithValue("@detail", If(detail, String.Empty))
                cmd.ExecuteNonQuery()
            End Using
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Writes a unified audit row to <c>AuditLogs</c> when the table exists.
    ''' </summary>
    ''' <param name="action">Short action label.</param>
    ''' <param name="detail">Detail text.</param>
    ''' <param name="performedBy">User or role name.</param>
    Public Shared Sub LogAudit(action As String, detail As String, performedBy As String)
        Try
            Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
                connection.Open()
                LogAudit(connection, action, detail, performedBy)
            End Using
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Writes a unified audit row using an open connection.
    ''' </summary>
    ''' <param name="connection">Open SQL connection.</param>
    ''' <param name="action">Short action label.</param>
    ''' <param name="detail">Detail text.</param>
    ''' <param name="performedBy">User or role name.</param>
    Public Shared Sub LogAudit(connection As SqlConnection, action As String, detail As String, performedBy As String)
        Try
            If connection Is Nothing OrElse connection.State <> ConnectionState.Open Then
                Return
            End If

            If ObjectExists(connection, "AuditLogs") = False Then
                Return
            End If

            Dim sql As String =
                "INSERT INTO AuditLogs (Action, Detail, PerformedBy) VALUES (@action, @detail, @performed_by);"

            Using cmd As New SqlCommand(sql, connection)
                cmd.Parameters.AddWithValue("@action", If(action, String.Empty))
                cmd.Parameters.AddWithValue("@detail", If(detail, String.Empty))
                cmd.Parameters.AddWithValue("@performed_by", If(performedBy, String.Empty))
                cmd.ExecuteNonQuery()
            End Using
        Catch
        End Try
    End Sub

    Private Shared Function ObjectExists(connection As SqlConnection, tableName As String) As Boolean
        Dim sql As String =
            "SELECT 1 FROM sys.tables WHERE name = @name AND schema_id = SCHEMA_ID('dbo');"
        Using cmd As New SqlCommand(sql, connection)
            cmd.Parameters.AddWithValue("@name", tableName)
            Dim o As Object = cmd.ExecuteScalar()
            Return o IsNot Nothing
        End Using
    End Function

End Class
