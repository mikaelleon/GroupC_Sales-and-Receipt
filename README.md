# Group C Sales & Receipt System

Windows Forms point-of-sale style app built with VB.NET and SQL Server LocalDB.

## Overview

This project lets users:

- manage products (`Add`, `Update`, `Deactivate`)
- build a sale cart and compute totals
- generate, print, and save receipt text
- persist products and sales data in SQL Server LocalDB

## Tech Stack

- VB.NET (`net10.0-windows`)
- Windows Forms
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`)
- `Microsoft.Data.SqlClient`

## Repository Structure

```text
GroupC/
├─ GroupC.slnx
├─ README.md
└─ GroupC/
   ├─ GroupC.vbproj
   ├─ MainMenuForm.vb
   ├─ ProductsForm.vb
   ├─ SalesForm.vb
   ├─ ReceiptForm.vb
   ├─ DatabaseConfig.vb
   └─ DatabaseInitializer.vb
```

Database scripts live in sibling folder:

- `../db/01_create_database.sql`
- `../db/02_create_tables.sql`
- `../db/03_seed_data.sql`
- `../db/00_fix_products_identity_conflict.sql` (recovery script)

## Prerequisites

- Visual Studio 2022+ with .NET desktop workload
- .NET SDK compatible with `net10.0-windows`
- SQL Server LocalDB installed (`MSSQLLocalDB`)

## Database Setup (GroupC_DB)

1. Open SQL Server Object Explorer in Visual Studio.
2. Connect to `(localdb)\MSSQLLocalDB`.
3. Create database named `GroupC_DB` (if not existing).
4. Run scripts in order:
   1. `../db/01_create_database.sql`
   2. `../db/02_create_tables.sql`
   3. `../db/03_seed_data.sql`

If you previously modified `dbo.products` in designer and got identity/PK conflicts, run:

- `../db/00_fix_products_identity_conflict.sql`

## Run the App

From `GroupC/` directory:

```powershell
dotnet build GroupC.slnx
dotnet run --project .\GroupC\GroupC.vbproj
```

## Connection Configuration

Connection string key in `GroupC/App.config`:

- `GroupCSqlServer`

Default database target:

- `GroupC_DB`

If needed, update connection string in:

- `GroupC/App.config`
- `GroupC/DatabaseConfig.vb` (fallback string)

## Forms and Flow

- `MainMenuForm` -> entry point and navigation
- `ProductsForm` -> product CRUD-like operations (soft delete via `is_active`)
- `SalesForm` -> cart lines, total calculation, sale persistence
- `ReceiptForm` -> receipt display, print, save text

## Troubleshooting

### App changes not visible in "View Data"

- Reopen "View Data" window (it does not auto-refresh reliably).
- Confirm query context:
  ```sql
  SELECT DB_NAME() AS current_db;
  ```
  Must be `GroupC_DB`.

### SQL70001 in table designer

- Run full scripts in **New Query** window, not designer script context.
- Designer parser rejects batch commands like `USE`, `GO`, conditional DDL.

### Duplicate identity/PK errors on `dbo.products`

- Use `../db/00_fix_products_identity_conflict.sql`.
- Ensure `products` table uses only:
  - `id` as `IDENTITY` primary key
  - no `product_id` column

## Best Practices for Team

- Prefer script-based schema changes over table designer edits.
- Keep `id` as single primary key for `products`.
- Run `dotnet build GroupC.slnx` before pushing changes.
- Update docs and SQL scripts together when schema changes.
