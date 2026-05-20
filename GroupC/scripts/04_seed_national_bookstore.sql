-- ============================================================
-- National Book Store (Philippines) — Reference Seed Data
-- Run AFTER 01_create_database.sql and 02_create_tables.sql.
--
-- Sources (manual curation, May 2026):
--   • nationalbookstore.com collection taxonomy (Writing, Paper,
--     School & Office, Arts & Crafts, Filing, etc.)
--   • Featured SKUs and PHP prices from NBS online storefront
--   • Brands commonly stocked at National Book Store PH (Pilot,
--     Pentel, Deli, Sakura, Stabilo, NBS, Limelight, Best Buy, etc.)
--
-- No official open product CSV from National Book Store exists;
-- this file is a demo dataset aligned with their retail mix.
--
-- Idempotent: skips existing category_name / product_name.
-- Safe to run alongside 03_seed_data.sql (different category names).
-- ============================================================

SET NOCOUNT ON;

-- ------------------------------------------------------------
-- Categories (NBS-style departments)
-- ------------------------------------------------------------
INSERT INTO dbo.categories (category_name, is_active)
SELECT v.category_name, 1
FROM (VALUES
    (N'Writing Supplies'),
    (N'Paper Supplies'),
    (N'School & Office Essentials'),
    (N'Arts & Crafts'),
    (N'Filing Supplies'),
    (N'Books & Media'),
    (N'Planners & Journals'),
    (N'Tech & Calculators'),
    (N'Bags & Travel'),
    (N'Gifts & Novelties')
) AS v(category_name)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.categories c WHERE c.category_name = v.category_name
);

DECLARE @c_writing  INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Writing Supplies');
DECLARE @c_paper    INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Paper Supplies');
DECLARE @c_school   INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'School & Office Essentials');
DECLARE @c_arts     INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Arts & Crafts');
DECLARE @c_filing   INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Filing Supplies');
DECLARE @c_books    INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Books & Media');
DECLARE @c_planners INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Planners & Journals');
DECLARE @c_tech     INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Tech & Calculators');
DECLARE @c_bags     INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Bags & Travel');
DECLARE @c_gifts    INT = (SELECT category_id FROM dbo.categories WHERE category_name = N'Gifts & Novelties');

IF @c_writing IS NULL OR @c_paper IS NULL OR @c_school IS NULL OR @c_arts IS NULL
   OR @c_filing IS NULL OR @c_books IS NULL OR @c_planners IS NULL OR @c_tech IS NULL
   OR @c_bags IS NULL OR @c_gifts IS NULL
BEGIN
    RAISERROR('NBS seed aborted: one or more categories missing. SELECT * FROM dbo.categories;', 16, 1);
    RETURN;
END;

-- ------------------------------------------------------------
-- Products (PHP retail-style prices)
-- ------------------------------------------------------------
INSERT INTO dbo.products (product_name, price, category_id, is_active)
SELECT v.product_name, v.price, v.category_id, 1
FROM (VALUES
    -- Writing Supplies (incl. NBS storefront samples)
    (N'Pilot Hi-Tecpoint RT Retractable 0.5mm Blue', 90.00, @c_writing),
    (N'Pilot G2 Gel Pen 0.5mm Black', 75.00, @c_writing),
    (N'Pilot Frixion Clicker Erasable 0.5mm', 165.00, @c_writing),
    (N'Pilot BP-S Ballpen Medium Blue', 35.00, @c_writing),
    (N'Pentel EnerGel BL77 0.5mm', 80.00, @c_writing),
    (N'Pentel RSVP Ballpen Medium', 45.00, @c_writing),
    (N'Uni Jetstream SXN-101 0.5mm', 95.00, @c_writing),
    (N'Uni-ball Signo UM-151 0.38mm', 90.00, @c_writing),
    (N'Zebra Sarasa Clip 0.5mm', 85.00, @c_writing),
    (N'Zebra Mildliner 5-color Set', 425.00, @c_writing),
    (N'Stabilo Boss Original Highlighter', 75.00, @c_writing),
    (N'Stabilo Point 88 Fineliner 0.4mm', 65.00, @c_writing),
    (N'Artline 500A Permanent Marker Black', 55.00, @c_writing),
    (N'Flex Office Ballpen 1.0mm 3pc', 35.00, @c_writing),
    (N'M&G Gel Pen 0.5mm', 45.00, @c_writing),
    (N'HBW Ballpen Medium 12pc Box', 120.00, @c_writing),
    (N'NBS Retractable Gel Pen 0.5mm', 55.00, @c_writing),
    (N'Best Buy Ballpen 1.0mm 3pc', 25.00, @c_writing),
    (N'Pilot V5 Hi-Tecpoint 0.5mm Black', 125.00, @c_writing),
    (N'Pilot Acroball 0.7mm Blue', 95.00, @c_writing),
    (N'Pilot Juice Up 0.4mm Gel Pen', 110.00, @c_writing),
    (N'Pilot Frixion Ball 0.5mm Erasable Black', 145.00, @c_writing),
    (N'Pilot Frixion Light Pastel Highlighter 6pc', 495.00, @c_writing),
    (N'Pilot Kakuno Fountain Pen Medium Nib', 595.00, @c_writing),
    (N'Pilot Parallel Pen 3.8mm Calligraphy', 355.00, @c_writing),
    (N'Pilot H-185 Mechanical Pencil 0.5mm', 150.00, @c_writing),
    (N'Pilot Color Eno Mechanical Pencil 0.7mm', 175.00, @c_writing),
    (N'Pilot Super Grip Mechanical Pencil 0.5mm', 135.00, @c_writing),
    (N'Pilot Wyteboard Whiteboard Marker Black', 85.00, @c_writing),
    (N'Pilot Vboard Master Whiteboard Marker 4pc', 295.00, @c_writing),
    (N'Pilot BeGreen B2P Ballpen 0.7mm', 65.00, @c_writing),
    (N'Deli Gel Pen 0.5mm Black', 35.00, @c_writing),
    (N'Deli Ballpen 1.0mm Blue 12pc Box', 95.00, @c_writing),
    (N'Deli Retractable Gel Pen 0.5mm Assorted 4pc', 125.00, @c_writing),
    (N'Deli Highlighter Chisel Tip Yellow', 45.00, @c_writing),
    (N'Deli Permanent Marker Twin Tip Black', 55.00, @c_writing),
    (N'Deli Whiteboard Marker Bullet Tip Black', 65.00, @c_writing),
    (N'Stabilo Boss Pastel Highlighter 6pc Set', 425.00, @c_writing),
    (N'Stabilo Boss Original Highlighter 4pc Set', 285.00, @c_writing),
    (N'Stabilo Swing Cool Highlighter Yellow', 85.00, @c_writing),
    (N'Stabilo Point 88 Fineliner 20-color Set', 895.00, @c_writing),
    (N'Stabilo pointMax Fineliner 0.4mm Black', 75.00, @c_writing),
    (N'Stabilo Pen 68 Felt Tip 12pc Set', 650.00, @c_writing),
    (N'Stabilo Liner 4-color Fineliner Set', 295.00, @c_writing),
    (N'Stabilo Exam Grade 4-color Pen Set', 225.00, @c_writing),
    (N'Stabilo GREENgraph Pencil HB', 55.00, @c_writing),
    (N'Stabilo EASYgraph Pencil for Kids 2B', 65.00, @c_writing),
    (N'Sakura Gelly Roll White 0.8mm', 75.00, @c_writing),
    (N'Sakura Gelly Roll Metallic 10pc Set', 550.00, @c_writing),
    (N'Sakura Pigma Micron 05 Black', 95.00, @c_writing),
    (N'Sakura Pigma Micron Set 6pc', 495.00, @c_writing),
    (N'Sakura Pigma Brush Pen Black', 185.00, @c_writing),
    (N'Sakura Koi Coloring Brush Pen 12 Set', 895.00, @c_writing),

    -- Paper Supplies
    (N'Limelight Spiral Notebook 8.5x11 80 Sheets', 93.00, @c_paper),
    (N'Orions Spiral Notebook College Ruled 70L', 85.00, @c_paper),
    (N'Premiere Notes Composition Notebook', 65.00, @c_paper),
    (N'Best Buy Composition Notebook', 45.00, @c_paper),
    (N'NBS Composition Notebook Wide Ruled', 55.00, @c_paper),
    (N'Victoria Yellow Pad Legal Size', 55.00, @c_paper),
    (N'Blueberry Yellow Pad A5', 45.00, @c_paper),
    (N'Navigator Presentation A4 Bond 80gsm Ream', 350.00, @c_paper),
    (N'Best Buy A4 Bond Paper 80gsm Ream', 295.00, @c_paper),
    (N'HP Office A4 Paper 80gsm Ream', 420.00, @c_paper),
    (N'Post-it Notes 3x3 Canary 100s', 120.00, @c_paper),
    (N'Post-it Super Sticky Notes 3x3', 185.00, @c_paper),
    (N'Scripti Sticky Notes 3x3 100s', 75.00, @c_paper),
    (N'NBS Sticky Notes 4-color Pack', 95.00, @c_paper),
    (N'Oxford Index Cards 3x5 100pc', 125.00, @c_paper),
    (N'Avanti Graph Paper Pad A4', 85.00, @c_paper),
    (N'Deli Sticky Notes 3x3 100s Canary', 65.00, @c_paper),
    (N'Deli Sticky Notes 3x3 5-color Pack', 95.00, @c_paper),
    (N'Pilot Index Tabs Writable 5-color', 95.00, @c_paper),
    (N'Stabilo Study Notes Sticky Flags 4-color', 125.00, @c_paper),

    -- School & Office Essentials
    (N'Maped Barbie Rubber Eraser with Refill', 110.00, @c_school),
    (N'Milan Kiddie Scissors Rounded Tip 5in', 149.00, @c_school),
    (N'Maped Essentials Scissors 17cm', 95.00, @c_school),
    (N'Faber-Castell School Scissors', 120.00, @c_school),
    (N'UHU Glue Stick 21g', 80.00, @c_school),
    (N'Pritt Glue Stick 22g', 85.00, @c_school),
    (N'Elmer''s Glue Stick 7g', 65.00, @c_school),
    (N'NBS Glue Stick 15g', 45.00, @c_school),
    (N'Pentel ZL31 Correction Tape 5mm', 85.00, @c_school),
    (N'Tipp-Ex Easy Correct Correction Tape', 95.00, @c_school),
    (N'Maped 30cm Plastic Ruler', 55.00, @c_school),
    (N'Staedtler Mars 30cm Ruler', 75.00, @c_school),
    (N'NBS x BINI Acrylic Paper Clip Blind Box', 39.00, @c_school),
    (N'Deli Paper Clips 28mm 100pc', 45.00, @c_school),
    (N'Acco Binder Clips Assorted 12pc', 95.00, @c_school),
    (N'Orions Clear Book A4 20 Pockets', 85.00, @c_school),
    (N'NBS Clear Book A4 20 Pockets', 95.00, @c_school),
    (N'Deli Stapler No.10', 185.00, @c_school),
    (N'Deli Stapler No.35 Heavy Duty', 395.00, @c_school),
    (N'Deli Hole Punch 2-hole 20 sheets', 225.00, @c_school),
    (N'Deli Cutter Knife 18mm with Blade', 95.00, @c_school),
    (N'Deli Office Scissors 7in', 125.00, @c_school),
    (N'Deli Correction Tape 5mm x 8m', 55.00, @c_school),
    (N'Deli Binder Clips 41mm 12pc', 85.00, @c_school),
    (N'Deli Ruler 30cm Stainless', 95.00, @c_school),
    (N'Deli Tape Dispenser Weighted Base', 195.00, @c_school),
    (N'Deli Desktop Organizer 3-tier', 325.00, @c_school),
    (N'Deli Index Tabs Writable 5-color', 75.00, @c_school),
    (N'Pilot Rexgrip Mechanical Pencil 0.5mm', 125.00, @c_school),
    (N'Sakura SumoGrip Eraser', 65.00, @c_school),
    (N'Stabilo Woody 3in1 Pencil 6pc Set', 395.00, @c_school),

    -- Arts & Crafts
    (N'DIY Activity Pack Sequins Crafts Mermaid', 41.00, @c_arts),
    (N'Crayola Super Tips Markers 20ct', 350.00, @c_arts),
    (N'Crayola Washable Markers 12ct', 285.00, @c_arts),
    (N'Faber-Castell Classic Colored Pencils 12ct', 185.00, @c_arts),
    (N'Staedtler Noris Colored Pencils 12ct', 195.00, @c_arts),
    (N'Prismacolor Scholar Colored Pencils 12ct', 550.00, @c_arts),
    (N'Pentel Water Colors 12-cake Set', 195.00, @c_arts),
    (N'Sakura Koi Water Colors 12 Set', 650.00, @c_arts),
    (N'Orions Drawing Pad A4 20 sheets', 95.00, @c_arts),
    (N'Canson XL Sketch Pad A4', 350.00, @c_arts),
    (N'Tombow Fudenosuke Brush Pen Soft', 185.00, @c_arts),
    (N'Pentel Sign Brush Pen Black', 155.00, @c_arts),
    (N'NBS Acrylic Paint Set 12 Colors', 225.00, @c_arts),
    (N'Best Buy Oil Pastels 12 Colors', 85.00, @c_arts),
    (N'Sakura Koi Water Colors 24 Set', 1150.00, @c_arts),
    (N'Sakura Koi Water Colors Field Sketch Box', 1450.00, @c_arts),
    (N'Sakura Solid Poster Paint 12 colors', 285.00, @c_arts),
    (N'Sakura Cray-Pas Junior Artist 12pc', 195.00, @c_arts),
    (N'Sakura Quickie Glue Pen', 95.00, @c_arts),
    (N'Sakura Foam Brush Set 3pc', 125.00, @c_arts),
    (N'Sakura Archival Ink Pad Black', 225.00, @c_arts),
    (N'Sakura Pigma Micron PN 0.45mm Black', 105.00, @c_arts),
    (N'Stabilo CarbOthello Pastel Pencil 12pc', 750.00, @c_arts),
    (N'Stabilo Woody 3in1 Watercolor Pencil 10pc', 550.00, @c_arts),
    (N'Pilot Color Eno Neox Erasable Colored Lead 6pc', 195.00, @c_arts),

    -- Filing Supplies
    (N'NBS Expanding File 13 Pockets A4', 195.00, @c_filing),
    (N'Orions Expanding File 12 Pockets', 175.00, @c_filing),
    (N'Flex Office Document Envelope A4', 25.00, @c_filing),
    (N'NBS Long Brown Envelope 10pc', 55.00, @c_filing),
    (N'NBS Document Folder A4 Assorted 5pc', 75.00, @c_filing),
    (N'Smead Manila Folder Letter Size 10pc', 125.00, @c_filing),
    (N'NBS Ring Binder A4 2-inch', 285.00, @c_filing),
    (N'Deli Ring Binder A4 1-inch', 195.00, @c_filing),
    (N'NBS Magazine File Box', 165.00, @c_filing),
    (N'Orions Magazine Holder', 145.00, @c_filing),
    (N'Deli Expanding File 13 Pockets A4', 185.00, @c_filing),
    (N'Deli Document Folder A4 Assorted 5pc', 65.00, @c_filing),
    (N'Deli Magazine File A4', 135.00, @c_filing),

    -- Books & Media (bestseller-style titles)
    (N'Tomorrow, and Tomorrow, and Tomorrow — Zevin', 799.00, @c_books),
    (N'Fourth Wing — Rebecca Yarros', 850.00, @c_books),
    (N'Atomic Habits — James Clear', 695.00, @c_books),
    (N'The Psychology of Money — Morgan Housel', 650.00, @c_books),
    (N'Ikigai — Garcia & Miralles', 550.00, @c_books),
    (N'Dog Man: Grime and Punishment — Pilkey', 499.00, @c_books),
    (N'Diary of a Wimpy Kid #1 — Kinney', 550.00, @c_books),
    (N'One Piece Vol. 1 — Oda', 380.00, @c_books),
    (N'Demon Slayer Vol. 1 — Gotouge', 350.00, @c_books),
    (N'Solo Leveling Vol. 1 — Chugong', 455.00, @c_books),
    (N'Merriam-Webster Pocket Dictionary', 395.00, @c_books),
    (N'Oxford Learner''s Thesaurus', 750.00, @c_books),
    (N'NBS Textbook Cover Clear', 35.00, @c_books),

    -- Planners & Journals
    (N'NBS Academic Planner 2026', 295.00, @c_planners),
    (N'Bright Planner Weekly 2026', 450.00, @c_planners),
    (N'Passion Planner Undated Compact', 850.00, @c_planners),
    (N'NBS Desk Calendar 2026', 195.00, @c_planners),
    (N'BG Group Desk Calendar 2026', 255.00, @c_planners),
    (N'Limelight Journal A5 Dotted', 185.00, @c_planners),
    (N'NBS Bullet Journal A5', 225.00, @c_planners),
    (N'Post-it Index Flags 4-color', 95.00, @c_planners),
    (N'Pilot Index Tabs Writable', 75.00, @c_planners),
    (N'NBS Weekly Planner Pad', 125.00, @c_planners),
    (N'Pilot Coleto 5-color Multi Pen 0.5mm', 395.00, @c_planners),
    (N'Pilot Frixion Stamp Erasable 3pc', 295.00, @c_planners),
    (N'Deli Planner Sticker Set Assorted', 85.00, @c_planners),
    (N'Stabilo Planner Highlighter Wallet 4pc', 325.00, @c_planners),

    -- Tech & Calculators
    (N'Casio MX-12B Desktop Calculator', 450.00, @c_tech),
    (N'Citizen SDC-444S Desktop Calculator', 385.00, @c_tech),
    (N'Casio FX-991ES Plus Scientific Calculator', 1250.00, @c_tech),
    (N'Casio FX-82ES Plus Scientific Calculator', 750.00, @c_tech),
    (N'Sharp EL-W506 Scientific Calculator', 1100.00, @c_tech),
    (N'Kingston DataTraveler 32GB USB', 450.00, @c_tech),
    (N'SanDisk Cruzer Blade 32GB USB', 485.00, @c_tech),
    (N'Transcend JetFlash 32GB USB', 420.00, @c_tech),
    (N'JBL C50HI Wired Earphones', 650.00, @c_tech),
    (N'Samsung EO-HS1303 Earphones', 455.00, @c_tech),
    (N'NBS USB Extension Cable 1m', 195.00, @c_tech),
    (N'Best Buy Mouse Pad', 85.00, @c_tech),

    -- Bags & Travel
    (N'JanSport SuperBreak Backpack', 2995.00, @c_bags),
    (N'Samsonite Classic Business Backpack', 3500.00, @c_bags),
    (N'NBS School Backpack', 995.00, @c_bags),
    (N'NBS Pencil Case Zipper', 150.00, @c_bags),
    (N'Smiggle Hardtop Pencil Case', 450.00, @c_bags),
    (N'Zipit Monsters Pencil Case', 355.00, @c_bags),
    (N'NBS Sling Bag Document', 550.00, @c_bags),
    (N'Targus Laptop Sleeve 14in', 850.00, @c_bags),
    (N'Best Buy Tote Bag Canvas', 195.00, @c_bags),
    (N'NBS Lunch Bag Insulated', 325.00, @c_bags),

    -- Gifts & Novelties
    (N'Hallmark Birthday Card', 125.00, @c_gifts),
    (N'NBS Birthday Card Assorted', 75.00, @c_gifts),
    (N'Hallmark Thank You Cards 10pc', 255.00, @c_gifts),
    (N'NBS Gift Wrap Roll Kraft', 85.00, @c_gifts),
    (N'Hallmark Gift Bag Medium', 125.00, @c_gifts),
    (N'NBS Sticker Sheet Assorted', 55.00, @c_gifts),
    (N'NBS Keychain Acrylic', 95.00, @c_gifts),
    (N'NBS Tumbler 500ml', 450.00, @c_gifts),
    (N'NBS Tote Bag Reusable', 125.00, @c_gifts),
    (N'Best Buy Gift Tag Set 20pc', 45.00, @c_gifts)
) AS v(product_name, price, category_id)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.products p WHERE p.product_name = v.product_name
);

DECLARE @nbs_cat INT = (
    SELECT COUNT(*) FROM dbo.categories
    WHERE category_name IN (
        N'Writing Supplies', N'Paper Supplies', N'School & Office Essentials',
        N'Arts & Crafts', N'Filing Supplies', N'Books & Media',
        N'Planners & Journals', N'Tech & Calculators', N'Bags & Travel', N'Gifts & Novelties'
    )
);
DECLARE @nbs_prod INT = (
    SELECT COUNT(*) FROM dbo.products p
    INNER JOIN dbo.categories c ON c.category_id = p.category_id
    WHERE c.category_name IN (
        N'Writing Supplies', N'Paper Supplies', N'School & Office Essentials',
        N'Arts & Crafts', N'Filing Supplies', N'Books & Media',
        N'Planners & Journals', N'Tech & Calculators', N'Bags & Travel', N'Gifts & Novelties'
    )
);

DECLARE @total_prod INT = (SELECT COUNT(*) FROM dbo.products);

PRINT N'National Book Store seed complete.';
PRINT N'  NBS categories present: ' + CAST(@nbs_cat AS NVARCHAR(10));
PRINT N'  NBS-tagged products: ' + CAST(@nbs_prod AS NVARCHAR(10));
PRINT N'  Total products in DB: ' + CAST(@total_prod AS NVARCHAR(10));
