Imports Microsoft.Data.SqlClient

Public NotInheritable Class DatabaseInitializer

    Private Shared initialized As Boolean

    Private Sub New()
    End Sub

    Public Shared Sub EnsureDatabase()
        If initialized Then
            Return
        End If

        EnsureDatabaseExists()
        EnsureSchema()
        SeedSampleProducts()
        initialized = True
    End Sub

    Private Shared Sub EnsureDatabaseExists()
        Using connection As New SqlConnection(DatabaseConfig.MasterConnectionString)
            connection.Open()

            Dim sql As String =
                "IF DB_ID(@name) IS NULL CREATE DATABASE [" & DatabaseConfig.DatabaseName & "];"

            Using command As New SqlCommand(sql, connection)
                command.Parameters.AddWithValue("@name", DatabaseConfig.DatabaseName)
                command.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Private Shared Sub EnsureSchema()
        Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
            connection.Open()

            Dim sql As String =
                "IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'products') " &
                "BEGIN " &
                "    CREATE TABLE products ( " &
                "        id INT IDENTITY(1,1) PRIMARY KEY, " &
                "        product_name NVARCHAR(100) NOT NULL UNIQUE, " &
                "        price DECIMAL(10, 2) NOT NULL CHECK (price > 0), " &
                "        is_active BIT NOT NULL CONSTRAINT DF_products_is_active DEFAULT (1), " &
                "        created_at DATETIME2 NOT NULL CONSTRAINT DF_products_created_at DEFAULT (SYSUTCDATETIME()), " &
                "        updated_at DATETIME2 NOT NULL CONSTRAINT DF_products_updated_at DEFAULT (SYSUTCDATETIME()) " &
                "    ); " &
                "END; " &
                "IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'sales') " &
                "BEGIN " &
                "    CREATE TABLE sales ( " &
                "        sale_id INT IDENTITY(1,1) PRIMARY KEY, " &
                "        sale_date DATETIME2 NOT NULL CONSTRAINT DF_sales_sale_date DEFAULT (SYSUTCDATETIME()), " &
                "        total_amount DECIMAL(10, 2) NOT NULL CHECK (total_amount >= 0), " &
                "        receipt_text NVARCHAR(MAX) NULL, " &
                "        created_at DATETIME2 NOT NULL CONSTRAINT DF_sales_created_at DEFAULT (SYSUTCDATETIME()) " &
                "    ); " &
                "END; " &
                "IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'sale_items') " &
                "BEGIN " &
                "    CREATE TABLE sale_items ( " &
                "        sale_item_id INT IDENTITY(1,1) PRIMARY KEY, " &
                "        sale_id INT NOT NULL, " &
                "        product_name NVARCHAR(100) NOT NULL, " &
                "        price DECIMAL(10, 2) NOT NULL CHECK (price > 0), " &
                "        quantity INT NOT NULL CHECK (quantity > 0), " &
                "        subtotal DECIMAL(10, 2) NOT NULL CHECK (subtotal >= 0), " &
                "        created_at DATETIME2 NOT NULL CONSTRAINT DF_sale_items_created_at DEFAULT (SYSUTCDATETIME()), " &
                "        CONSTRAINT FK_sale_items_sales FOREIGN KEY (sale_id) REFERENCES sales(sale_id) ON DELETE CASCADE " &
                "    ); " &
                "    CREATE INDEX IX_sale_items_sale_id ON sale_items(sale_id); " &
                "END;"

            Using command As New SqlCommand(sql, connection)
                command.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Private Shared Sub SeedSampleProducts()
        Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
            connection.Open()

            Dim sql As String =
                "IF NOT EXISTS (SELECT 1 FROM products) " &
                "BEGIN " &
                "    INSERT INTO products (product_name, price) VALUES " &
                "        ('Notebook', 45.00), " &
                "        ('Ballpen', 12.00), " &
                "        ('Pencil', 8.00), " &
                "        ('Eraser', 10.00), " &
                "        ('Bond Paper', 150.00); " &
                "END;"

            Using command As New SqlCommand(sql, connection)
                command.ExecuteNonQuery()
            End Using
        End Using
    End Sub

End Class
