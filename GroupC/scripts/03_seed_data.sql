-- ============================================================
-- International Bookstore — Seed Data
-- Run AFTER 02_create_tables.sql on GroupC_DB (or your app DB).
-- Schema: dbo.categories, dbo.products (see 02_create_tables.sql).
-- Idempotent: skips categories/products that already exist.
-- ============================================================

SET NOCOUNT ON;

-- Categories (insert only missing names)
INSERT INTO dbo.categories (category_name, is_active)
SELECT v.category_name, 1
FROM (VALUES
    (N'Writing Instruments'),
    (N'Notebooks & Paper'),
    (N'Art Supplies'),
    (N'School Supplies'),
    (N'Office Supplies'),
    (N'Books'),
    (N'Stationery & Cards'),
    (N'Planners & Organizers'),
    (N'Tech Accessories'),
    (N'Bags & Cases')
) AS v(category_name)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.categories c WHERE c.category_name = v.category_name
);

-- Resolve category_id by name (works regardless of IDENTITY gaps)
DECLARE @c_writing   INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Writing Instruments');
DECLARE @c_notebooks INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Notebooks & Paper');
DECLARE @c_art       INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Art Supplies');
DECLARE @c_school    INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'School Supplies');
DECLARE @c_office    INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Office Supplies');
DECLARE @c_books     INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Books');
DECLARE @c_stationery INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Stationery & Cards');
DECLARE @c_planners  INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Planners & Organizers');
DECLARE @c_tech      INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Tech Accessories');
DECLARE @c_bags      INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Bags & Cases');

IF @c_writing IS NULL OR @c_notebooks IS NULL OR @c_art IS NULL OR @c_school IS NULL
   OR @c_office IS NULL OR @c_books IS NULL OR @c_stationery IS NULL OR @c_planners IS NULL
   OR @c_tech IS NULL OR @c_bags IS NULL
BEGIN
    RAISERROR('Seed aborted: one or more bookstore categories missing. SELECT * FROM dbo.categories;', 16, 1);
    RETURN;
END;

-- Products: (product_name, price, category_id, is_active)
INSERT INTO dbo.products (product_name, price, category_id, is_active)
SELECT v.product_name, v.price, v.category_id, 1
FROM (VALUES
    -- Writing Instruments
    (N'BIC Round Stic Ballpen (Medium)', 15.00, @c_writing),
    (N'Pilot BP-S Ballpen', 35.00, @c_writing),
    (N'Pentel BK77 Ballpen', 45.00, @c_writing),
    (N'Uni Jetstream Ballpen', 95.00, @c_writing),
    (N'Pilot G-2 Gel Pen', 75.00, @c_writing),
    (N'Pentel EnerGel Gel Pen', 80.00, @c_writing),
    (N'Uni-ball Signo Gel Pen', 90.00, @c_writing),
    (N'Stabilo Point 88 Fineliner', 65.00, @c_writing),
    (N'Pentel P205 Mechanical Pencil', 175.00, @c_writing),
    (N'Staedtler 925 Mechanical Pencil', 225.00, @c_writing),
    (N'Pilot H-185 Mechanical Pencil', 150.00, @c_writing),
    (N'Stabilo Boss Highlighter', 75.00, @c_writing),
    (N'Pilot Frixion Light Highlighter', 165.00, @c_writing),
    (N'Zebra Mildliner Highlighter', 115.00, @c_writing),
    (N'Pentel N50 Permanent Marker', 65.00, @c_writing),
    (N'Sharpie Fine Permanent Marker', 95.00, @c_writing),

    -- Notebooks & Paper
    (N'Pee Wee Composition Notebook', 55.00, @c_notebooks),
    (N'NBS Composition Notebook', 60.00, @c_notebooks),
    (N'Classmate Spiral Notebook 80L', 75.00, @c_notebooks),
    (N'Pee Wee Spiral Notebook 80L', 70.00, @c_notebooks),
    (N'Viking Yellow Pad Legal', 55.00, @c_notebooks),
    (N'Pee Wee Yellow Pad', 50.00, @c_notebooks),
    (N'Navigator A4 Bond Paper (ream)', 350.00, @c_notebooks),
    (N'Multicopy A4 Bond Paper (ream)', 380.00, @c_notebooks),
    (N'HP Office A4 Bond Paper (ream)', 420.00, @c_notebooks),
    (N'Post-it 3x3 Sticky Notes', 120.00, @c_notebooks),
    (N'Post-it Super Sticky Notes', 185.00, @c_notebooks),
    (N'Stick''n Sticky Notes', 95.00, @c_notebooks),

    -- Art Supplies
    (N'Faber-Castell 12ct Colored Pencils', 185.00, @c_art),
    (N'Staedtler Noris 12ct Colored Pencils', 195.00, @c_art),
    (N'Stabilo Trio Thick 12ct Colored Pencils', 220.00, @c_art),
    (N'Prismacolor Premier 12ct Colored Pencils', 750.00, @c_art),
    (N'Pentel Watercolors 12 colors', 195.00, @c_art),
    (N'Sakura Koi 12 Watercolor Set', 650.00, @c_art),
    (N'Winsor & Newton Cotman 12 Watercolor', 895.00, @c_art),
    (N'Pee Wee Drawing Pad A4', 120.00, @c_art),
    (N'Canson 1557 Sketch Pad A4', 350.00, @c_art),
    (N'Fabriano Accademia Sketch Pad A4', 425.00, @c_art),
    (N'Pentel Fude Brush Pen', 155.00, @c_art),
    (N'Tombow Fudenosuke Brush Pen', 185.00, @c_art),
    (N'Pilot Parallel Pen Calligraphy Set', 355.00, @c_art),

    -- School Supplies
    (N'Maped Essentials Scissors', 95.00, @c_school),
    (N'Faber-Castell Scissors', 120.00, @c_school),
    (N'Stanley Scissors', 180.00, @c_school),
    (N'Elmer''s Glue Stick', 65.00, @c_school),
    (N'UHU Glue Stick', 80.00, @c_school),
    (N'Pritt Glue Stick', 85.00, @c_school),
    (N'NBS Clear Book A4 20 pockets', 95.00, @c_school),
    (N'Viking Clear Book A4 20 pockets', 85.00, @c_school),
    (N'Maruman A4 Clear Book', 140.00, @c_school),
    (N'Pentel ZL31 Correction Tape', 85.00, @c_school),
    (N'Tipp-Ex Pocket Mouse', 95.00, @c_school),
    (N'Maped 30cm Ruler', 55.00, @c_school),
    (N'Staedtler 30cm Ruler', 75.00, @c_school),

    -- Office Supplies
    (N'Max HD-10 Stapler', 250.00, @c_office),
    (N'Kangaro DS-45L Stapler', 285.00, @c_office),
    (N'Rapid F26 Stapler', 550.00, @c_office),
    (N'Deli Paper Clips 100pc', 45.00, @c_office),
    (N'Acco Paper Clips 100pc', 55.00, @c_office),
    (N'Scotch Magic Tape 3/4"', 125.00, @c_office),
    (N'Nichiban Tape', 95.00, @c_office),
    (N'Casio MX-12B Calculator', 450.00, @c_office),
    (N'Citizen SDC-444S Calculator', 385.00, @c_office),
    (N'Canon LS-100TS Calculator', 420.00, @c_office),
    (N'Casio FX-991ES Plus Scientific Calc', 1250.00, @c_office),
    (N'Casio FX-82ES Plus Scientific Calc', 750.00, @c_office),
    (N'Sharp EL-W506 Scientific Calculator', 1100.00, @c_office),
    (N'Colop E/20 Stamp Pad', 185.00, @c_office),
    (N'Trodat Printy Stamp Pad', 255.00, @c_office),

    -- Books
    (N'Atomic Habits — James Clear', 695.00, @c_books),
    (N'The Psychology of Money — Morgan Housel', 650.00, @c_books),
    (N'Ikigai — Garcia & Miralles', 550.00, @c_books),
    (N'Tomorrow and Tomorrow — Gabrielle Zevin', 799.00, @c_books),
    (N'Fourth Wing — Rebecca Yarros', 850.00, @c_books),
    (N'Intermezzo — Sally Rooney', 799.00, @c_books),
    (N'Diary of a Wimpy Kid — Jeff Kinney', 550.00, @c_books),
    (N'Dog Man — Dav Pilkey', 499.00, @c_books),
    (N'Big Nate — Lincoln Peirce', 450.00, @c_books),
    (N'One Piece Vol. 1 — Eiichiro Oda', 380.00, @c_books),
    (N'Demon Slayer Vol. 1 — Gotouge', 350.00, @c_books),
    (N'Solo Leveling Vol. 1 — Chugong', 455.00, @c_books),
    (N'Merriam-Webster Collegiate Dictionary', 895.00, @c_books),
    (N'Oxford Thesaurus of English', 750.00, @c_books),

    -- Stationery & Cards
    (N'Hallmark Birthday Card', 125.00, @c_stationery),
    (N'NBS Birthday Card', 75.00, @c_stationery),
    (N'American Greetings Birthday Card', 150.00, @c_stationery),
    (N'Hallmark Thank You Cards 10pc', 255.00, @c_stationery),
    (N'NBS Thank You Cards 10pc', 155.00, @c_stationery),
    (N'NBS Gift Wrap Roll', 85.00, @c_stationery),
    (N'Hallmark Gift Wrap Sheet', 125.00, @c_stationery),
    (N'NBS Long Envelopes 10pc', 55.00, @c_stationery),
    (N'Viking Long Envelopes 10pc', 50.00, @c_stationery),
    (N'Conqueror DL Envelopes 10pc', 185.00, @c_stationery),

    -- Planners & Organizers
    (N'NBS Academic Planner', 295.00, @c_planners),
    (N'Bright Planner', 450.00, @c_planners),
    (N'Passion Planner Compact', 850.00, @c_planners),
    (N'NBS Desk Calendar', 195.00, @c_planners),
    (N'BG Group Desk Calendar', 255.00, @c_planners),
    (N'NBS Wall Calendar', 150.00, @c_planners),
    (N'Post-it Index Flags 4-color', 95.00, @c_planners),
    (N'Pilot Index Tabs', 75.00, @c_planners),
    (N'Deli Index Tabs', 55.00, @c_planners),

    -- Tech Accessories
    (N'Kingston DataTraveler 16GB USB', 350.00, @c_tech),
    (N'SanDisk Cruzer Blade 16GB USB', 385.00, @c_tech),
    (N'Transcend JetFlash 16GB USB', 320.00, @c_tech),
    (N'Kingston DataTraveler 32GB USB', 450.00, @c_tech),
    (N'SanDisk Ultra 32GB USB', 485.00, @c_tech),
    (N'Transcend JetFlash 32GB USB', 420.00, @c_tech),
    (N'JBL C50HI Wired Earphones', 650.00, @c_tech),
    (N'Samsung EO-HS1303 Earphones', 455.00, @c_tech),
    (N'Havit HV-E301P Earphones', 380.00, @c_tech),

    -- Bags & Cases
    (N'NBS Pencil Case', 150.00, @c_bags),
    (N'Smiggle Pencil Case', 450.00, @c_bags),
    (N'Zipit Monsters Pencil Case', 355.00, @c_bags),
    (N'JanSport Superbreak Backpack', 2995.00, @c_bags),
    (N'Samsonite Classic Backpack', 3500.00, @c_bags),
    (N'NBS School Bag', 995.00, @c_bags),
    (N'NBS Document Sling Bag', 550.00, @c_bags),
    (N'Targus Document Bag', 1200.00, @c_bags),
    (N'Ecotak Sling Bag', 750.00, @c_bags)
) AS v(product_name, price, category_id)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.products p WHERE p.product_name = v.product_name
);

DECLARE @cat_count INT = (SELECT COUNT(*) FROM dbo.categories);
DECLARE @prod_count INT = (SELECT COUNT(*) FROM dbo.products);
PRINT N'Seed complete. Categories: ' + CAST(@cat_count AS NVARCHAR(10))
    + N'; Products: ' + CAST(@prod_count AS NVARCHAR(10)) + N'.';
