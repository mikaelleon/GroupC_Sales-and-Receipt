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
                "        created_at DATETIME2 NOT NULL CONSTRAINT DF_sales_created_at DEFAULT (SYSUTCDATETIME()), " &
                "        subtotal_before_discount DECIMAL(10, 2) NULL, " &
                "        discount_percent DECIMAL(5, 2) NULL, " &
                "        discount_amount DECIMAL(10, 2) NULL, " &
                "        amount_before_tax DECIMAL(10, 2) NULL, " &
                "        tax_percent DECIMAL(5, 2) NULL, " &
                "        tax_amount DECIMAL(10, 2) NULL, " &
                "        amount_tendered DECIMAL(10, 2) NULL, " &
                "        change_given DECIMAL(10, 2) NULL " &
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

            EnsureSalesExtendedColumns(connection)
            EnsureCategoriesAndProductCategory(connection)
            EnsureAuditAndLogTables(connection)
        End Using
    End Sub

    Private Shared Sub EnsureSalesExtendedColumns(connection As SqlConnection)
        Dim alters As String() = {
            "IF COL_LENGTH('dbo.sales','subtotal_before_discount') IS NULL ALTER TABLE dbo.sales ADD subtotal_before_discount DECIMAL(10,2) NULL;",
            "IF COL_LENGTH('dbo.sales','discount_percent') IS NULL ALTER TABLE dbo.sales ADD discount_percent DECIMAL(5,2) NULL;",
            "IF COL_LENGTH('dbo.sales','discount_amount') IS NULL ALTER TABLE dbo.sales ADD discount_amount DECIMAL(10,2) NULL;",
            "IF COL_LENGTH('dbo.sales','amount_before_tax') IS NULL ALTER TABLE dbo.sales ADD amount_before_tax DECIMAL(10,2) NULL;",
            "IF COL_LENGTH('dbo.sales','tax_percent') IS NULL ALTER TABLE dbo.sales ADD tax_percent DECIMAL(5,2) NULL;",
            "IF COL_LENGTH('dbo.sales','tax_amount') IS NULL ALTER TABLE dbo.sales ADD tax_amount DECIMAL(10,2) NULL;",
            "IF COL_LENGTH('dbo.sales','amount_tendered') IS NULL ALTER TABLE dbo.sales ADD amount_tendered DECIMAL(10,2) NULL;",
            "IF COL_LENGTH('dbo.sales','change_given') IS NULL ALTER TABLE dbo.sales ADD change_given DECIMAL(10,2) NULL;"
        }

        For Each stmt As String In alters
            Using cmd As New SqlCommand(stmt, connection)
                cmd.ExecuteNonQuery()
            End Using
        Next
    End Sub

    Private Shared Sub EnsureCategoriesAndProductCategory(connection As SqlConnection)
        Dim catSql As String =
            "IF OBJECT_ID('dbo.categories','U') IS NULL " &
            "BEGIN " &
            "CREATE TABLE dbo.categories (" &
            " category_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY, " &
            " category_name NVARCHAR(100) NOT NULL, " &
            " is_active BIT NOT NULL CONSTRAINT DF_categories_is_active DEFAULT (1), " &
            " CONSTRAINT UQ_categories_name UNIQUE (category_name) " &
            "); END;"

        Using cmd As New SqlCommand(catSql, connection)
            cmd.ExecuteNonQuery()
        End Using

        Dim addCol As String = "IF COL_LENGTH('dbo.products','category_id') IS NULL ALTER TABLE dbo.products ADD category_id INT NULL;"
        Using cmd As New SqlCommand(addCol, connection)
            cmd.ExecuteNonQuery()
        End Using

        Dim fkSql As String =
            "IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_products_categories' AND parent_object_id = OBJECT_ID(N'dbo.products')) " &
            "AND OBJECT_ID(N'dbo.categories','U') IS NOT NULL " &
            "ALTER TABLE dbo.products ADD CONSTRAINT FK_products_categories FOREIGN KEY (category_id) REFERENCES dbo.categories(category_id);"

        Try
            Using cmd As New SqlCommand(fkSql, connection)
                cmd.ExecuteNonQuery()
            End Using
        Catch
        End Try
    End Sub

    Private Shared Sub EnsureAuditAndLogTables(connection As SqlConnection)
        Dim auditSql As String =
            "IF OBJECT_ID('dbo.audit_products','U') IS NULL " &
            "BEGIN " &
            "CREATE TABLE dbo.audit_products (" &
            " audit_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY, " &
            " occurred_at DATETIME2 NOT NULL CONSTRAINT DF_audit_products_occurred DEFAULT (SYSUTCDATETIME()), " &
            " action_code NVARCHAR(20) NOT NULL, " &
            " product_id INT NULL, " &
            " product_name NVARCHAR(100) NULL, " &
            " detail NVARCHAR(MAX) NULL " &
            "); END; " &
            "IF OBJECT_ID('dbo.audit_sales','U') IS NULL " &
            "BEGIN " &
            "CREATE TABLE dbo.audit_sales (" &
            " audit_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY, " &
            " occurred_at DATETIME2 NOT NULL CONSTRAINT DF_audit_sales_occurred DEFAULT (SYSUTCDATETIME()), " &
            " action_code NVARCHAR(20) NOT NULL, " &
            " sale_id INT NULL, " &
            " detail NVARCHAR(MAX) NULL " &
            "); END; " &
            "IF OBJECT_ID('dbo.error_log','U') IS NULL " &
            "BEGIN " &
            "CREATE TABLE dbo.error_log (" &
            " log_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY, " &
            " occurred_at DATETIME2 NOT NULL CONSTRAINT DF_error_log_occurred DEFAULT (SYSUTCDATETIME()), " &
            " source NVARCHAR(200) NULL, " &
            " message NVARCHAR(MAX) NOT NULL, " &
            " stack_trace NVARCHAR(MAX) NULL " &
            "); END; " &
            "IF OBJECT_ID('dbo.AuditLogs','U') IS NULL " &
            "BEGIN " &
            "CREATE TABLE dbo.AuditLogs (" &
            " LogID INT IDENTITY(1,1) NOT NULL PRIMARY KEY, " &
            " Action NVARCHAR(100) NOT NULL, " &
            " Detail NVARCHAR(MAX) NULL, " &
            " PerformedBy NVARCHAR(100) NULL, " &
            " LoggedAt DATETIME2 NOT NULL CONSTRAINT DF_AuditLogs_LoggedAt DEFAULT (SYSUTCDATETIME()) " &
            "); END;"

        Using cmd As New SqlCommand(auditSql, connection)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    Private Shared Sub SeedSampleProducts()
        Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
            connection.Open()

            Dim seedCat As String =
                "IF NOT EXISTS (SELECT 1 FROM dbo.categories) " &
                "BEGIN " &
                " INSERT INTO dbo.categories (category_name) VALUES (N'Fiction'), (N'Textbooks'), (N'Children''s Books'), (N'Stationery'), (N'Gifts'); " &
                "END;"

            Using cmd As New SqlCommand(seedCat, connection)
                cmd.ExecuteNonQuery()
            End Using

            Dim seedProducts As String =
                "IF NOT EXISTS (SELECT 1 FROM dbo.products) " &
                "BEGIN " &
                " DECLARE @fiction INT = (SELECT TOP 1 category_id FROM dbo.categories WHERE category_name = N'Fiction'); " &
                " DECLARE @textbooks INT = (SELECT TOP 1 category_id FROM dbo.categories WHERE category_name = N'Textbooks'); " &
                " DECLARE @stationery INT = (SELECT TOP 1 category_id FROM dbo.categories WHERE category_name = N'Stationery'); " &
                " IF @fiction IS NULL SET @fiction = (SELECT TOP 1 category_id FROM dbo.categories ORDER BY category_id); " &
                " IF @textbooks IS NULL SET @textbooks = @fiction; " &
                " IF @stationery IS NULL SET @stationery = @fiction; " &
                " INSERT INTO dbo.products (product_name, price, category_id) VALUES " &
                "  (N'The Great Gatsby', 450.00, @fiction), " &
                "  (N'Introduction to Algorithms', 1200.00, @textbooks), " &
                "  (N'Notebook', 45.00, @stationery), " &
                "  (N'Ballpen', 12.00, @stationery), " &
                "  (N'Bookmark Set', 25.00, @stationery); " &
                "END;"

            Using command As New SqlCommand(seedProducts, connection)
                command.ExecuteNonQuery()
            End Using
        End Using
    End Sub

End Class
