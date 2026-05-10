-- Group C schema reference (SQL Server / LocalDB).
-- Runtime DDL is applied by DatabaseInitializer.vb; keep this file in sync.

IF OBJECT_ID(N'dbo.categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.categories (
        category_id   INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
        category_name NVARCHAR(100) NOT NULL,
        is_active     BIT NOT NULL CONSTRAINT DF_categories_is_active DEFAULT (1),
        CONSTRAINT UQ_categories_name UNIQUE (category_name)
    );
END;

IF OBJECT_ID(N'dbo.products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.products (
        id            INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
        product_name  NVARCHAR(100) NOT NULL,
        price         DECIMAL(10, 2) NOT NULL,
        is_active     BIT NOT NULL CONSTRAINT DF_products_is_active DEFAULT (1),
        category_id   INT NULL,
        created_at    DATETIME2 NOT NULL CONSTRAINT DF_products_created_at DEFAULT (SYSUTCDATETIME()),
        updated_at    DATETIME2 NOT NULL CONSTRAINT DF_products_updated_at DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_products_name UNIQUE (product_name),
        CONSTRAINT FK_products_categories FOREIGN KEY (category_id) REFERENCES dbo.categories (category_id)
    );
END;

IF COL_LENGTH(N'dbo.products', N'category_id') IS NULL
BEGIN
    ALTER TABLE dbo.products ADD category_id INT NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_products_categories' AND parent_object_id = OBJECT_ID(N'dbo.products'))
BEGIN
    ALTER TABLE dbo.products
        ADD CONSTRAINT FK_products_categories FOREIGN KEY (category_id) REFERENCES dbo.categories (category_id);
END;

-- sales, sale_items, audit_products, audit_sales, error_log, AuditLogs: see DatabaseInitializer.vb
