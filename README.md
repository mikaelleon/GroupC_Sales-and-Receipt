# Group C Sales & Receipt System

**Application name:** International Bookstore  
**Platform:** Windows desktop (VB.NET, `net10.0-windows`)

## In plain English

**What is this?**  
A **desktop program for Windows** that helps a small store **sell items**, **track what was sold**, and **give customers a receipt**. Think of it as a simple electronic cash register with a product list on screen instead of paper sheets.

**Who is it for?**  
Anyone running a small retail counter—bookstore, school supplies, a booth, or a classroom demo—who wants prices and totals calculated automatically and sales remembered after closing the app.

**What can you do with it?**

- **Sign in** as **Administrator** (full access) or **Cashier** (sell and receipts only).
- **Keep a product list** with names, prices, and **categories** (for example “Fiction”, “Stationery”).
- **Ring up a sale**: pick products, set quantities, apply **one customer discount** at a time (**PWD**, **Senior**, or **Membership**), optionally add **VAT/tax**, enter cash received, and **finalize** the sale.
- **Preview, print, or save receipts as PDF** after a sale.
- **Glance at the main screen** for today’s totals, database health, and a **sales chart** (presets from 7 days up to 90 days, or a custom date range).
- **Administrators** can manage **categories**, **cashier accounts**, **store settings** (name, receipt footer, currency symbol), **run reports**, review an **audit trail**, open **backup/restore guidance** (sample SQL commands), and **import products from CSV**.

**Where is my data saved?**  
On your computer, in a **local database** (SQL Server LocalDB, database name `GroupC_DB`). You do not need your own server; the app creates the database and tables on first run when LocalDB is available.

Store branding (name, footer, currency) is saved separately in a **settings file** under your Windows user profile (see [Application settings](#application-settings)).

**Do I need to be technical to use the app?**  
No for daily use: open the program, sign in, sell, print or save receipts.  
Yes if something breaks: you may need someone comfortable installing **.NET** and **SQL Server LocalDB**, or follow the **Technical setup** section below.

---

## Overview (technical)

Windows Forms point-of-sale app with SQL Server LocalDB persistence.

Capabilities include:

- Product lifecycle (`Add`, `Update`, deactivate / “soft delete” via `is_active`)
- **Categories** on products; category filters on sales and product screens
- **Cashier accounts** (hashed passwords in `cashier_accounts`; admin-managed)
- Role-based UI (**Admin** vs **Cashier**)
- Cart totals with **discount** (PWD / Senior / Membership toggles) and **tax**
- Receipt preview, print, and **PDF export** (PDFsharp)
- **Audit logging** (`AuditLogs` plus product/sale audit tables)
- Dashboard chart with configurable date range (up to **90** days)
- **CSV product import** (merge by product name)
- **Backup/restore guidance** form with copy-to-clipboard SQL samples

## Tech stack

| Component | Details |
|-----------|---------|
| Language / UI | VB.NET, Windows Forms |
| Target framework | `net10.0-windows` |
| Database | SQL Server LocalDB `(localdb)\MSSQLLocalDB`, database `GroupC_DB` |
| Data access | `Microsoft.Data.SqlClient` 7.x |
| PDF receipts | `PDFsharp` 6.x |
| Settings file | JSON under `%LocalAppData%\GroupC\settings.json` |

## Repository structure

```text
GroupC_Sales-and-Receipt/
├── README.md
├── GroupC.slnx
└── GroupC/
    ├── GroupC.vbproj
    ├── App.config
    ├── Assets/                    AppIcon.ico, ReceiptLogo.png (receipt branding)
    ├── scripts/
    │   ├── 01_create_database.sql
    │   ├── 02_create_tables.sql
    │   └── 03_seed_data.sql
    ├── MainMenuForm.vb            Dashboard, navigation, sales chart
    ├── LoginForm.vb
    ├── SalesForm.vb
    ├── ProductsForm.vb
    ├── CategoriesForm.vb
    ├── CashierAccountsForm.vb
    ├── ReceiptForm.vb
    ├── ReportsForm.vb
    ├── SettingsForm.vb
    ├── BackupRestoreForm.vb
    ├── DatabaseConfig.vb
    ├── DatabaseInitializer.vb
    ├── CashierAccountService.vb
    ├── AppSettings.vb
    ├── PdfReceiptExporter.vb
    ├── GridDisplayHelper.vb
    └── UiTheme.vb
```

Runtime schema creation and upgrades are handled by `DatabaseInitializer.vb`. The `scripts/` folder is a **reference** for manual setup and documentation; keep it aligned when you change the schema in code.

## Prerequisites

- **Visual Studio 2022+** with the .NET desktop development workload (for editing and debugging), **or**
- **.NET SDK** that supports `net10.0-windows` (for `dotnet build` / `dotnet run` only)
- **SQL Server LocalDB** installed (`MSSQLLocalDB` instance)

Verify LocalDB (optional):

```powershell
sqllocaldb info MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

## Run the app

From the repository root:

```powershell
dotnet build GroupC.slnx
dotnet run --project .\GroupC\GroupC.vbproj
```

In Visual Studio, open `GroupC.slnx` and press **F5**.

On first successful startup, the app ensures `GroupC_DB` exists, applies schema, and seeds sample categories/products when the product table is empty.

## Sign-in and roles

| Role | How to sign in | Access |
|------|----------------|--------|
| **Administrator** | Select **Administrator**, enter the demo password (see below) | All menus: products, categories, cashiers, reports, settings, backup guidance |
| **Cashier** | Select **Cashier**, enter **username** and **password** | Sales, receipts; no admin screens |

**Demo administrator password** (change before any real deployment): `admin123`  
Defined in `GroupC/DatabaseConfig.vb` as `HardcodedAdminPassword`.

**Cashier accounts** are not pre-seeded. An administrator must create them under **Manage Cashiers** (`CashierAccountsForm`) before a cashier can sign in. Passwords are stored as salted hashes (`PasswordHasher` / `cashier_accounts` table).

Failed and successful sign-in attempts are written to `AuditLogs` when possible.

## Application settings

Receipt branding fields are **not** stored in SQL. They live in:

`%LocalAppData%\GroupC\settings.json`

| Field | Purpose | Default |
|-------|---------|---------|
| `StoreName` | Title on receipts | `International Bookstore` |
| `ReceiptFooter` | Footer line | `Thank you for your purchase!` |
| `CurrencySymbol` | Amount prefix | `₱` |

Administrators edit these in **Settings**. The dashboard chart and POS screens read the currency symbol from this file.

## Connection configuration

| Source | Detail |
|--------|--------|
| `GroupC/App.config` | Connection string name `GroupCSqlServer` |
| `GroupC/DatabaseConfig.vb` | Fallback connection string and `DatabaseName` constant |

Default connection (LocalDB):

```text
Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Database=GroupC_DB;TrustServerCertificate=true;
```

## Database setup (manual)

Normally you do **not** need manual scripts. Use this when LocalDB is installed but you want to inspect or recreate schema outside the app.

1. Connect to `(localdb)\MSSQLLocalDB` (SSMS, Azure Data Studio, or `sqlcmd`).
2. Run scripts in order from `GroupC/scripts/`:
   1. `01_create_database.sql` — creates `GroupC_DB` if missing
   2. `02_create_tables.sql` — categories and products (partial; see file header)
   3. Start the app once so `DatabaseInitializer` creates remaining tables (`sales`, `sale_items`, audit tables, `cashier_accounts`, etc.), **or** mirror the DDL in `DatabaseInitializer.vb` in your own script.

Sample data: the app seeds bookstore-style categories and products when `products` is empty (`DatabaseInitializer.SeedSampleProducts`). `03_seed_data.sql` documents that seed; it is not executed automatically from disk.

### Tables (runtime)

| Table | Purpose |
|-------|---------|
| `categories` | Product categories |
| `products` | Catalog (optional `category_id`) |
| `sales` | Sale header, totals, discount/tax/tender fields, `receipt_text` |
| `sale_items` | Line items per sale |
| `cashier_accounts` | Cashier login accounts |
| `audit_products`, `audit_sales` | Structured product/sale audit |
| `AuditLogs` | General audit trail (login, settings, etc.) |
| `error_log` | Logged exceptions |

## Forms and modules

| Form / module | Role |
|---------------|------|
| `MainMenuForm` | Entry after login; dashboard metrics and sales chart |
| `LoginForm` | Administrator vs cashier sign-in |
| `SalesForm` | POS cart, discounts, tax, checkout |
| `ReceiptForm` | Receipt history, preview, print, PDF |
| `ProductsForm` | Product CRUD, CSV import (admin) |
| `CategoriesForm` | Category CRUD (admin) |
| `CashierAccountsForm` | Register / deactivate cashiers (admin) |
| `ReportsForm` | Sales summaries; audit log tab (admin) |
| `SettingsForm` | Store name, footer, currency (admin) |
| `BackupRestoreForm` | Backup/restore instructions and sample T-SQL (admin) |
| `GridDisplayHelper` | Shared DataGridView display rules |
| `UiTheme` | Shared colors, buttons, window chrome |
| `AuditLogger` / `ErrorLogger` | Audit and error persistence |

## Point of Sale — discounts and tax

On **SalesForm**, checkout uses **toggle buttons** (not manual percent/fixed entry):

| Toggle | Default rate | Notes |
|--------|----------------|-------|
| ♿ **PWD** | 20% | Person with disability discount |
| 👴 **Senior** | 20% | Senior citizen discount |
| 🎫 **Member** | 10% | Store membership discount |

**Rules:**

- Only **one** customer discount can be active at a time. When one is on, the other discount toggles are **disabled**.
- Click the active toggle again to turn the discount **off**.
- The applied rate is stored on the sale as `discount_percent` / `discount_amount` (percent-based).
- Receipt text includes a label such as `Discount (PWD 20%)` when a discount is applied.

**Tax:** use the **🧾 VAT / Tax %** toggle to enable the tax rate field; tax is calculated on the amount **after** discount.

Rates are constants in `SalesForm.vb` (`DiscountPwdPercent`, `DiscountSeniorPercent`, `DiscountMembershipPercent`). Change those values if your store policy differs.

## Receipts

- Each finalized sale stores formatted text in `sales.receipt_text`.
- **Receipt** screen: select a sale, preview, **Print Receipt**, or **Save as PDF**.
- PDF generation: `PdfReceiptExporter` (PDFsharp); Windows font resolver in `WindowsFontResolver.vb`.
- Optional logo: `GroupC/Assets/ReceiptLogo.png` (copied to output with the build).

## Reports and audit

**Reports** (admin): daily totals and top products for a date range; separate **Audit** tab filtered by date (`AuditLogs`).

Product changes also write to `audit_products`; sales events to `audit_sales` where applicable.

## CSV product import

Administrators: **Products** → import CSV.

- Expected columns: `name`, `price` (header row optional; detected if both words appear in the first line).
- One product per row: `Product Name,12.50`
- Rows are **merged** by `product_name` (update price and reactivate, or insert). Invalid rows are skipped and counted in the status message.

## Backup and restore

**Backup / Restore** (admin) does not run backups inside the app. It shows step-by-step guidance and sample `BACKUP DATABASE` / `RESTORE DATABASE` commands for `GroupC_DB`, with a **Copy commands** button. Stop the app before restore.

## Troubleshooting

### App changes not visible in the database

- Confirm you are connected to `GroupC_DB`:

  ```sql
  SELECT DB_NAME() AS current_db;
  ```

- Restart the app after schema changes so `DatabaseInitializer` can apply migrations.

### LocalDB not running

```powershell
sqllocaldb start MSSQLLocalDB
```

### Cashier cannot sign in

- An admin must create the account under **Manage Cashiers**.
- Usernames: letters, numbers, underscore; 3–50 characters. Passwords: at least 6 characters.

### Build fails because GroupC.exe is locked

- Close the running app before `dotnet build`.

### Chart shows no data

- Check the date range (maximum span **90** days).
- Confirm sales exist for the selected period.

### PDF export fails

- Ensure the output path is writable and PDFsharp dependencies restored (`dotnet restore`).

## Security notes (demo / coursework)

- Replace `HardcodedAdminPassword` before production use.
- Restrict physical access to the machine; LocalDB uses Windows integrated security.
- Treat `settings.json` and database backups as sensitive if they contain business data.

## Team practices

- Prefer script-based or `DatabaseInitializer` schema changes over ad hoc designer edits on live tables.
- Run `dotnet build GroupC.slnx` before pushing.
- When schema changes, update `DatabaseInitializer.vb` and `GroupC/scripts/` reference scripts together.
- Keep `README.md` in sync when adding forms, tables, or user-visible features.
