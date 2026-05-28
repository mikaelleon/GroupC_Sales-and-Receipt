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
                "        stock_quantity INT NOT NULL CONSTRAINT DF_products_stock_quantity DEFAULT (100), " &
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
            EnsureSalesVoidColumn(connection)
            EnsureCategoriesAndProductCategory(connection)
            EnsureProductStockQuantity(connection)
            EnsureProductImagePath(connection)
            EnsureAuditAndLogTables(connection)
            EnsureCashierAccountsTable(connection)
        End Using
    End Sub

    Private Shared Sub EnsureSalesVoidColumn(connection As SqlConnection)
        Dim addCol As String =
            "IF COL_LENGTH('dbo.sales','is_voided') IS NULL " &
            "ALTER TABLE dbo.sales ADD is_voided BIT NOT NULL CONSTRAINT DF_sales_is_voided DEFAULT (0);"

        Using cmd As New SqlCommand(addCol, connection)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>
    ''' Loads the expanded demo catalog (categories + products) from scripts/03_seed_data.sql logic.
    ''' Idempotent — skips rows that already exist.
    ''' </summary>
    Public Shared Function SeedDemoCatalog() As String
        EnsureDatabase()
        Dim categoriesAdded As Integer = 0
        Dim productsAdded As Integer = 0

        Using connection As New SqlConnection(DatabaseConfig.ConnectionString)
            connection.Open()

            Dim seedCat As String =
                "INSERT INTO dbo.categories (category_name, is_active) " &
                "SELECT v.category_name, 1 FROM (VALUES " &
                "(N'Writing Instruments'), (N'Notebooks & Paper'), (N'Art Supplies'), (N'School Supplies'), " &
                "(N'Office Supplies'), (N'Books'), (N'Stationery & Cards'), (N'Planners & Organizers'), " &
                "(N'Tech Accessories'), (N'Bags & Cases')) AS v(category_name) " &
                "WHERE NOT EXISTS (SELECT 1 FROM dbo.categories c WHERE c.category_name = v.category_name);"

            Using cmd As New SqlCommand(seedCat, connection)
                categoriesAdded = cmd.ExecuteNonQuery()
            End Using

            Dim seedProducts As String =
                "DECLARE @c_writing INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Writing Instruments'); " &
                "DECLARE @c_notebooks INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Notebooks & Paper'); " &
                "DECLARE @c_stationery INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Stationery & Cards'); " &
                "DECLARE @c_books INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Books'); " &
                "IF @c_writing IS NULL SET @c_writing = (SELECT TOP 1 category_id FROM dbo.categories ORDER BY category_id); " &
                "IF @c_notebooks IS NULL SET @c_notebooks = @c_writing; " &
                "IF @c_stationery IS NULL SET @c_stationery = @c_writing; " &
                "IF @c_books IS NULL SET @c_books = @c_writing; " &
                "INSERT INTO dbo.products (product_name, price, category_id, stock_quantity, is_active) " &
                "SELECT v.product_name, v.price, v.category_id, v.stock_qty, 1 FROM (VALUES " &
                "(N'BIC Round Stic Ballpen (Medium)', 15.00, @c_writing, 200), " &
                "(N'Pilot G-2 Gel Pen', 75.00, @c_writing, 150), " &
                "(N'Classmate Spiral Notebook 80L', 75.00, @c_notebooks, 120), " &
                "(N'Navigator A4 Bond Paper (ream)', 350.00, @c_notebooks, 40), " &
                "(N'Greeting Card Assorted', 45.00, @c_stationery, 80), " &
                "(N'The Great Gatsby (Pocket)', 299.00, @c_books, 60), " &
                "(N'Introduction to Programming', 650.00, @c_books, 35), " &
                "(N'Bookmark Set (3-pack)', 25.00, @c_stationery, 100)) AS v(product_name, price, category_id, stock_qty) " &
                "WHERE NOT EXISTS (SELECT 1 FROM dbo.products p WHERE p.product_name = v.product_name);"

            Using cmd As New SqlCommand(seedProducts, connection)
                productsAdded = cmd.ExecuteNonQuery()
            End Using
        End Using

        Return String.Format(
            Globalization.CultureInfo.CurrentCulture,
            "Demo catalog loaded. {0} new categor{1}, {2} new product{3}.",
            categoriesAdded,
            If(categoriesAdded = 1, "y", "ies"),
            productsAdded,
            If(productsAdded = 1, "", "s"))
    End Function

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

    Private Shared Sub EnsureProductStockQuantity(connection As SqlConnection)
        Dim addCol As String =
            "IF COL_LENGTH('dbo.products','stock_quantity') IS NULL " &
            "ALTER TABLE dbo.products ADD stock_quantity INT NOT NULL CONSTRAINT DF_products_stock_quantity DEFAULT (100);"

        Using cmd As New SqlCommand(addCol, connection)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    Private Shared Sub EnsureProductImagePath(connection As SqlConnection)
        Dim addCol As String =
            "IF COL_LENGTH('dbo.products','image_path') IS NULL " &
            "ALTER TABLE dbo.products ADD image_path NVARCHAR(260) NULL;"

        Using cmd As New SqlCommand(addCol, connection)
            cmd.ExecuteNonQuery()
        End Using
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

    Private Shared Sub EnsureCashierAccountsTable(connection As SqlConnection)
        Dim sql As String =
            "IF OBJECT_ID('dbo.cashier_accounts','U') IS NULL " &
            "BEGIN " &
            "CREATE TABLE dbo.cashier_accounts (" &
            " cashier_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY, " &
            " username NVARCHAR(50) NOT NULL, " &
            " password_hash NVARCHAR(256) NOT NULL, " &
            " password_salt NVARCHAR(64) NOT NULL, " &
            " display_name NVARCHAR(100) NULL, " &
            " is_active BIT NOT NULL CONSTRAINT DF_cashier_accounts_is_active DEFAULT (1), " &
            " created_at DATETIME2 NOT NULL CONSTRAINT DF_cashier_accounts_created DEFAULT (SYSUTCDATETIME()), " &
            " last_login_at DATETIME2 NULL, " &
            " CONSTRAINT UQ_cashier_accounts_username UNIQUE (username) " &
            "); END;"

        Using cmd As New SqlCommand(sql, connection)
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
                " INSERT INTO dbo.products (product_name, price, category_id, stock_quantity) VALUES " &
                "  (N'The Great Gatsby', 450.00, @fiction, 100), " &
                "  (N'Introduction to Algorithms', 1200.00, @textbooks, 50), " &
                "  (N'Notebook', 45.00, @stationery, 200), " &
                "  (N'Ballpen', 12.00, @stationery, 500), " &
                "  (N'Bookmark Set', 25.00, @stationery, 150); " &
                "END;"

            Using command As New SqlCommand(seedProducts, connection)
                command.ExecuteNonQuery()
            End Using
        End Using
    End Sub

End Class
