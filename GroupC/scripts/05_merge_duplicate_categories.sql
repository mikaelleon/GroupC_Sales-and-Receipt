-- ============================================================
-- Merge overlapping categories (03 + 04 seeds)
-- Run on GroupC_DB after seeding.
--
-- Keeps National Book Store-style names; moves products from
-- International Bookstore duplicates; deactivates empty duplicates.
-- Safe to re-run (no-op if already merged).
-- ============================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @merges TABLE (
    keep_name NVARCHAR(100) NOT NULL,
    drop_name NVARCHAR(100) NOT NULL,
    PRIMARY KEY (keep_name, drop_name)
);

-- keep_name  <--  drop_name (products reassigned, drop deactivated)
INSERT INTO @merges (keep_name, drop_name) VALUES
    (N'Writing Supplies',              N'Writing Instruments'),
    (N'Paper Supplies',                N'Notebooks & Paper'),
    (N'Arts & Crafts',                 N'Art Supplies'),
    (N'School & Office Essentials',    N'School Supplies'),
    (N'School & Office Essentials',    N'Office Supplies'),
    (N'Books & Media',                 N'Books'),
    (N'Planners & Journals',           N'Planners & Organizers'),
    (N'Tech & Calculators',            N'Tech Accessories'),
    (N'Bags & Travel',                 N'Bags & Cases');

-- Optional: legacy/orphan rows (uncomment if present in your DB)
-- INSERT INTO @merges (keep_name, drop_name) VALUES
--     (N'Paper Supplies', N'Stationery'),
--     (N'Paper Supplies', N'Paper'),
--     (N'School & Office Essentials', N'Supplies');

DECLARE @moved INT = 0;
DECLARE @deactivated INT = 0;

-- Reassign products from duplicate category -> keeper
UPDATE p
SET
    category_id = k.category_id,
    updated_at  = SYSUTCDATETIME()
FROM dbo.products p
INNER JOIN dbo.categories d ON d.category_id = p.category_id
INNER JOIN @merges m ON m.drop_name = d.category_name
INNER JOIN dbo.categories k ON k.category_name = m.keep_name
WHERE p.category_id <> k.category_id;

SET @moved = @@ROWCOUNT;

-- Deactivate duplicate categories (no delete — preserves FK / history)
UPDATE c
SET is_active = 0
FROM dbo.categories c
INNER JOIN @merges m ON m.drop_name = c.category_name
WHERE c.is_active = 1;

SET @deactivated = @@ROWCOUNT;

COMMIT TRANSACTION;

-- Report
SELECT
    m.keep_name,
    m.drop_name,
    k.category_id AS keep_id,
    d.category_id AS drop_id,
    (SELECT COUNT(*) FROM dbo.products p WHERE p.category_id = k.category_id) AS products_on_keeper,
    d.is_active AS drop_still_active
FROM @merges m
LEFT JOIN dbo.categories k ON k.category_name = m.keep_name
LEFT JOIN dbo.categories d ON d.category_name = m.drop_name
ORDER BY m.keep_name, m.drop_name;

DECLARE @active_cats INT = (SELECT COUNT(*) FROM dbo.categories WHERE is_active = 1);
DECLARE @active_prods INT = (SELECT COUNT(*) FROM dbo.products WHERE is_active = 1);

PRINT N'Merge complete.';
PRINT N'  Products reassigned: ' + CAST(@moved AS NVARCHAR(10));
PRINT N'  Categories deactivated: ' + CAST(@deactivated AS NVARCHAR(10));
PRINT N'  Active categories now: ' + CAST(@active_cats AS NVARCHAR(10));
PRINT N'  Active products: ' + CAST(@active_prods AS NVARCHAR(10));
