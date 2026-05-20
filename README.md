# Group C Sales & Receipt System

**Application name:** International Bookstore  
**Platform:** Windows desktop (VB.NET, `net10.0-windows`)

| Document | Description |
|----------|-------------|
| This file | Setup, features, POS, receipts, troubleshooting |
| [GroupC/scripts/README.md](GroupC/scripts/README.md) | SQL seed scripts, run order, `sqlcmd` examples |

## In plain English

**What is this?**  
A **desktop program for Windows** that helps a small store **sell items**, **track what was sold**, and **give customers a receipt**. Think of it as a simple electronic cash register with a product list on screen instead of paper sheets.

**Who is it for?**  
Anyone running a small retail counter—bookstore, school supplies, a booth, or a classroom demo—who wants prices and totals calculated automatically and sales remembered after closing the app.

**What can you do with it?**

- **Sign in** as **Administrator** (full access) or **Cashier** (sell and receipts only).
- **Keep a product list** with names, prices, and **categories** (for example “Fiction”, “Stationery”).
- **Ring up a sale**: pick products, set quantities, apply **one customer discount** at a time (**PWD**, **Senior**, or **Membership**), optionally add **VAT/tax**, enter cash received, and **finalize** the sale.
- **Preview, print, or save receipts** after a sale (PDF, plain text, batch export, print preview with margins, zoom).
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
- Receipt preview (history search/filter, zoom, print preview), print, **PDF** and text export (PDFsharp)
- Structured receipt template (header, transaction, items, pricing, payment, footer) stored in `sales.receipt_text`
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
    │   ├── 03_seed_data.sql              International Bookstore sample catalog
    │   ├── 04_seed_national_bookstore.sql  NBS-style categories/products (optional)
    │   ├── 05_merge_duplicate_categories.sql Merge 03+04 overlapping categories
    │   └── README.md                   Script run order and catalog strategies
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
    ├── WindowsFontResolver.vb     PDFsharp font resolution
    ├── ReceiptBranding.vb         Receipt text layout, logo, preview centering
    ├── ReceiptPrintHelper.vb
    ├── ReceiptSnapshot.vb
    ├── GridDisplayHelper.vb
    └── UiTheme.vb
```

Runtime schema creation and upgrades are handled by `DatabaseInitializer.vb`. The `scripts/` folder is a **reference** for manual setup and documentation; see [GroupC/scripts/README.md](GroupC/scripts/README.md) for run order and catalog options. Keep scripts aligned when you change the schema in code.

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
| `ReceiptFooter` | Thank-you line on receipts | `Thank you for your purchase!` |
| `CurrencySymbol` | Amount prefix | `₱` |
| `StoreBranch` | Branch line on receipts | `Main Branch` |
| `StoreLocation` | Location line on receipts | `Metro Manila, Philippines` |
| `CustomerServiceInfo` | Footer contact line | `help@internationalbookstore.local \| (02) 8123-4567` |
| `ReturnPolicyText` | Returns line on receipts | 7-day return policy (see `AppSettings.vb`) |
| `TermsText` | Terms line on receipts | Tax/terms disclaimer (see `AppSettings.vb`) |

Administrators edit **Store name**, **receipt footer**, and **currency** in **Settings** (`SettingsForm`). Branch, location, customer service, returns, and terms lines are read from the same `settings.json` file but are **not** on the Settings screen yet—edit the JSON directly or extend `SettingsForm` if you need UI for them. All fields are applied when `ReceiptBranding.BuildReceiptText` runs at checkout.

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
   4. Optional sample catalogs (idempotent; safe to re-run):
      - `03_seed_data.sql` — International Bookstore categories and products
      - `04_seed_national_bookstore.sql` — National Book Store–style departments and SKUs
      - `05_merge_duplicate_categories.sql` — run after **both** 03 and 04 if you want one combined catalog (reassigns products, deactivates duplicate categories)

Sample data at runtime: if `products` is empty, the app also calls `DatabaseInitializer.SeedSampleProducts()` on first run. SQL scripts are **not** executed automatically from disk; run them manually when you want a larger demo catalog.

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
| `ReceiptForm` | History (search, date filters, sort), preview (zoom, page margins), toolbar actions, print/PDF/text |
| `ProductsForm` | Product CRUD, CSV import (admin) |
| `CategoriesForm` | Category CRUD (admin) |
| `CashierAccountsForm` | Register / deactivate cashiers (admin) |
| `ReportsForm` | Sales summaries; audit log tab (admin) |
| `SettingsForm` | Store name, footer, currency (admin) |
| `BackupRestoreForm` | Backup/restore instructions and sample T-SQL (admin) |
| `GridDisplayHelper` | Shared DataGridView rules (currency columns, active/status column left) |
| `UiTheme` | Shared colors, buttons, cards, maximized workspace chrome (Products, Categories, Cashiers, Receipt, Settings) |
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

### Storage and generation

- On **Finalize**, the app saves the sale, then builds monospace receipt text with `ReceiptBranding.BuildReceiptText` and stores it in `sales.receipt_text` (includes receipt number `RCP-######` and transaction reference after the sale id is known).
- Receipt body uses a fixed **40-character** column (Courier New), centered in the preview, print, and PDF output.

### Receipt template sections

| Section | Contents |
|---------|----------|
| **Header** | Store name, branch, location, receipt number, transaction reference |
| **Transaction** | Date and time, cashier |
| **Items** | Line items (description, qty, unit price, line total) |
| **Pricing** | Subtotal, discount (if any), tax (if any), total due |
| **Payment** | Method (default **Cash**), tendered, change |
| **Footer** | Customer service, returns, terms, thank-you message, barcode/QR placeholder |

Discount labels on the receipt match the POS toggles (for example `Discount (PWD 20%)`).

### Receipt Preview screen (`ReceiptForm`)

Opened after checkout or from the main menu. Layout: **history and filters** on the left, **preview and actions** on the right.

**History and navigation**

| Control | Behavior |
|---------|----------|
| Search | Receipt #, amount, date, cashier (from receipt text) |
| Date filter | All, Today, This week, This month, Custom range |
| Sort | Newest, oldest, amount high/low |
| Refresh | Reload up to 500 recent sales from the database |
| Export batch | Save filtered list as `.txt` files to a folder |

**Preview**

| Control | Behavior |
|---------|----------|
| Zoom **−** / **+** | 75%–150% (font and logo scale) |
| Show page margins | Dotted margin guide around the receipt paper |
| Preview text | Black monospace, center-aligned |

**Actions** (toolbar and right-click context menu)

| Action | Behavior |
|--------|----------|
| Print | System print dialog |
| Print preview | Full-page print preview with margins |
| Reprint | Same as Print |
| PDF | Save via `PdfReceiptExporter` (PDFsharp) |
| Text | Save `.txt` file |
| Copy | Clipboard |
| Email | Opens default mail client (`mailto:` with receipt body) |
| Details | Dialog with sale metadata and line items |
| Duplicate sale | Opens **Sales** with cart loaded from selected sale (`SalesForm.LoadCartFromSaleId`) |

### Files

| File | Role |
|------|------|
| `ReceiptBranding.vb` | Build receipt text, logo, center-aligned black preview text |
| `WindowsFontResolver.vb` | Registers fonts for PDFsharp PDF export |
| `ReceiptPrintHelper.vb` | Paginated printing with logo |
| `PdfReceiptExporter.vb` | PDF export (PDFsharp); `WindowsFontResolver.vb` for fonts |
| `ReceiptSnapshot.vb` | Structured sale data passed to receipt builder |
| `Assets/ReceiptLogo.png` | Optional header logo (embedded or under `Assets/`) |

Legacy sales saved before the template upgrade keep their older `receipt_text` until you finalize new sales.

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

### Receipt preview looks short or old

- Only **new** finalized sales use the full section template. Older rows in `sales.receipt_text` are unchanged.
- Use **Refresh list** after seeding or merging categories if the history list looks stale.

### Email receipt does nothing

- **Email** opens the system default mail client (`mailto:`). Configure a default mail app on Windows, or use **Copy** / **Save as PDF** instead.

## Security notes (demo / coursework)

- Replace `HardcodedAdminPassword` before production use.
- Restrict physical access to the machine; LocalDB uses Windows integrated security.
- Treat `settings.json` and database backups as sensitive if they contain business data.

## Team practices

- Prefer script-based or `DatabaseInitializer` schema changes over ad hoc designer edits on live tables.
- Run `dotnet build GroupC.slnx` before pushing (close the running app if `GroupC.exe` is locked).
- When schema changes, update `DatabaseInitializer.vb`, `GroupC/scripts/` (including [scripts/README.md](GroupC/scripts/README.md)), and this file.
- When adding forms, tables, seeds, or user-visible behavior, update **both** root `README.md` and any affected script headers.
