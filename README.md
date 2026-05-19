# Group C Sales & Receipt System

## In plain English

**What is this?**  
A **desktop program for Windows** that helps a small store **sell items**, **track what was sold**, and **give customers a receipt**. Think of it as a simple electronic cash register with a product list on screen instead of paper sheets.

**Who is it for?**  
Anyone running a small retail counter—school supplies, a booth, a classroom demo—who wants prices and totals calculated automatically and sales remembered after closing the app.

**What can you do with it?**

- **Sign in** as either a **cashier** (sell and receipts) or an **administrator** (full access).
- **Keep a product list** with names, prices, and **categories** (for example “Paper”, “Stationery”).
- **Ring up a sale**: pick products, set quantities, apply **one customer discount** at a time (**PWD**, **Senior**, or **Membership**), optionally add **VAT/tax**, enter cash received, and **finalize** the sale.
- **See and print a receipt** after a sale.
- **Glance at the main screen** for today’s totals and a simple **last-seven-days sales chart**.
- **Administrators** can also change store settings (store name on receipts, currency symbol), **run reports**, review an **audit trail** of important actions, get **backup/restore** guidance, and **manage products** (including CSV import).

**Where is my data saved?**  
On your computer, in a **local database** (SQL Server LocalDB). You don’t need your own server; the app creates or updates the database the first time it runs when possible.

**Do I need to be technical to use the app?**  
No for daily use: open the program, sign in, sell, print or save receipts.  
Yes if something breaks: you may need someone comfortable installing **.NET** and **SQL Server LocalDB**, or follow the **Technical setup** section below.

---

## Overview (technical)

Windows Forms point-of-sale style app (VB.NET, `net10.0-windows`) with SQL Server LocalDB.

Capabilities include:

- Product lifecycle (`Add`, `Update`, deactivate / “soft delete” via `is_active`)
- **Categories** on products; category filters on sales and product screens
- Role-based UI (**Admin** vs **Cashier**)
- Cart totals with **discount** (percent or fixed) and **tax**
- Receipt preview / persistence
- **Audit logging** (admin-facing list in Reports)
- Dashboard chart (**last 7 days** sales by day)

## Tech stack

- VB.NET (`net10.0-windows`)
- Windows Forms
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`)
- `Microsoft.Data.SqlClient`

## Repository structure

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
   ├─ ReportsForm.vb
   ├─ GridDisplayHelper.vb
   ├─ UiTheme.vb
   ├─ DatabaseConfig.vb
   └─ DatabaseInitializer.vb
```

Database scripts (reference / manual setup):

- `../db/01_create_database.sql`
- `../db/02_create_tables.sql`
- `../db/03_seed_data.sql`
- `../db/00_fix_products_identity_conflict.sql` (recovery script)

The app normally creates or upgrades schema on startup via `DatabaseInitializer`.

## Prerequisites

- Visual Studio 2022+ with .NET desktop workload (for development)
- .NET SDK compatible with `net10.0-windows`
- SQL Server LocalDB installed (`MSSQLLocalDB`)

## Database setup (GroupC_DB)

For **manual** setup:

1. Connect to `(localdb)\MSSQLLocalDB`.
2. Create database `GroupC_DB` if it does not exist.
3. Run scripts in order:
   1. `../db/01_create_database.sql`
   2. `../db/02_create_tables.sql`
   3. `../db/03_seed_data.sql`

If you previously edited `dbo.products` in the designer and hit identity/PK conflicts:

- Run `../db/00_fix_products_identity_conflict.sql`

## Run the app

From the `GroupC/` directory:

```powershell
dotnet build GroupC.slnx
dotnet run --project .\GroupC\GroupC.vbproj
```

## Connection configuration

- Config key: `GroupCSqlServer` in `GroupC/App.config`
- Default database: `GroupC_DB`
- Fallback / constants: `GroupC/DatabaseConfig.vb`

## Forms and flow (technical)

- `MainMenuForm` — entry, login, dashboard, navigation
- `LoginForm` — role + password/PIN
- `ProductsForm` — products and categories (admin)
- `SalesForm` — cart, customer discounts, tax, finalize sale
- `ReceiptForm` — receipt preview / history
- `ReportsForm` — sales summaries; audit log tab (admin)
- `SettingsForm` — store name, footer, currency symbol (admin)
- `CategoriesForm` — category CRUD (admin)
- `GridDisplayHelper` — shared DataGridView rules (see below)

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

Rates are defined as constants in `SalesForm.vb` (`DiscountPwdPercent`, `DiscountSeniorPercent`, `DiscountMembershipPercent`). Change those values if your store policy differs.

## Tables and grids

`GridDisplayHelper` standardizes how bound grids look:

- **Hidden columns:** `id`, `LogID`, and any column whose name ends with `_id` (for example `category_id`). IDs remain in the data source for code-behind but are not shown to users.
- **Active status:** boolean `is_active` columns render as **✅** (active) or **❌** (inactive) instead of checkbox cells.

Used on **Products**, **Categories**, and **Reports** (audit log). The POS cart grid uses friendly column names only (no product database IDs in the cart).

## Troubleshooting

### App changes not visible in “View Data”

- Refresh or reopen the window.
- Confirm database context:

  ```sql
  SELECT DB_NAME() AS current_db;
  ```

  Expected: `GroupC_DB`.

### SQL70001 in table designer

- Run full scripts in a **New Query** window, not the designer script pane.
- Designer rejects batches with `USE`, `GO`, conditional DDL.

### Duplicate identity/PK errors on `dbo.products`

- Run `../db/00_fix_products_identity_conflict.sql`.
- Prefer `id` as single `IDENTITY` PK on `products`.

### Build fails because GroupC.exe is locked

- Close the running app before `dotnet build`.

## Team practices

- Prefer script-based schema changes over designer-only edits.
- Run `dotnet build GroupC.slnx` before pushing.
- Update SQL scripts and `DatabaseInitializer` when schema changes.
