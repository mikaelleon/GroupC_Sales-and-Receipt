# CLAUDE.md

Context for AI assistants working in this repository.

## Project Overview

**International Bookstore** is a Windows desktop point-of-sale (POS) app for small retail counters (bookstore, school supplies, coursework demos). Cashiers ring up sales with discounts and tax, print or export receipts, and view sale history. Administrators manage products, categories, cashier accounts, reports, and store settings. Data persists locally in SQL Server LocalDB (`GroupC_DB`); branding settings live in a JSON file under the user profile.

## Tech Stack

- **Language / UI:** VB.NET, Windows Forms
- **Target framework:** `net10.0-windows` (`GroupC/GroupC.vbproj`)
- **Database:** SQL Server LocalDB `(localdb)\MSSQLLocalDB`, database `GroupC_DB`
- **NuGet packages:**
  - `Microsoft.Data.SqlClient` 7.0.1
  - `PDFsharp` 6.2.1
- **Settings:** JSON via `System.Text.Json` (`AppSettings.vb`)
- **IDE / build:** Visual Studio 2022+ or .NET SDK with `dotnet build` / `dotnet run`
- **Solution file:** `GroupC.slnx` (single project)

## Project Structure

- `README.md` — setup, features, POS/receipt behavior, troubleshooting
- `GroupC.slnx` — Visual Studio / `dotnet` solution entry
- `GroupC/` — main WinForms application source
- `GroupC/GroupC.vbproj` — project file, package references, asset embedding
- `GroupC/App.config` — SQL connection string (`GroupCSqlServer`)
- `GroupC/Assets/` — `AppIcon.ico`, `ReceiptLogo.png` (receipt branding); `AppLogo.png` also present
- `GroupC/scripts/` — manual SQL setup and demo catalog seeds (not auto-run by app)
- `GroupC/scripts/README.md` — script run order, catalog strategies, `sqlcmd` examples
- `GroupC/My Project/` — VB application host config (`Application.myapp` → startup form)
- `GroupC/bin/`, `GroupC/obj/`, `GroupC/.vs/` — build and IDE output (gitignored)

## Key Files

- `GroupC/My Project/Application.myapp` — sets `MainMenuForm` as startup form
- `GroupC/ApplicationEvents.vb` — app startup (icon load) and unhandled exception logging
- `GroupC/MainMenuForm.vb` — post-login dashboard, navigation, sales chart; shows `LoginForm` on load
- `GroupC/LoginForm.vb` — Administrator vs Cashier sign-in
- `GroupC/SalesForm.vb` — POS cart, discount/tax toggles, checkout, receipt snapshot on finalize
- `GroupC/ReceiptForm.vb` — receipt history, preview (zoom/margins), print/PDF/text export
- `GroupC/ProductsForm.vb` — product CRUD and CSV import (admin)
- `GroupC/CategoriesForm.vb` — category CRUD (admin)
- `GroupC/CashierAccountsForm.vb` — cashier account management (admin)
- `GroupC/ReportsForm.vb` — sales summaries and audit log tab (admin)
- `GroupC/SettingsForm.vb` — store name, receipt footer, currency symbol (admin)
- `GroupC/DatabaseConfig.vb` — connection string resolution, `DatabaseName`, demo admin password constant
- `GroupC/DatabaseInitializer.vb` — creates DB/schema at runtime, seeds sample products if empty
- `GroupC/AppSettings.vb` — loads/saves `%LocalAppData%\GroupC\settings.json`
- `GroupC/AppSession.vb` — in-memory signed-in role and cashier identity
- `GroupC/CashierAccountService.vb` / `PasswordHasher.vb` — cashier auth and password hashing
- `GroupC/ReceiptBranding.vb` — builds centered 40-column receipt text and preview formatting
- `GroupC/ReceiptSnapshot.vb` — structured sale data passed to receipt builder
- `GroupC/PdfReceiptExporter.vb` / `ReceiptPrintHelper.vb` — PDF and print output
- `GroupC/WindowsFontResolver.vb` — PDFsharp font registration on Windows
- `GroupC/UiTheme.vb` — shared colors, buttons, cards, grid chrome
- `GroupC/GridDisplayHelper.vb` — shared DataGridView display rules (IDs hidden, active status column)
- `GroupC/AuditLogger.vb` / `ErrorLogger.vb` — audit trail and exception persistence
- `GroupC/CartLineItem.vb` — in-memory cart line model for `SalesForm`
- `GroupC/scripts/01_create_database.sql` … `05_merge_duplicate_categories.sql` — manual DB/catalog scripts

## Common Commands

From repository root (per `README.md`):

```powershell
dotnet build GroupC.slnx
dotnet run --project .\GroupC\GroupC.vbproj
```

Optional LocalDB check:

```powershell
sqllocaldb info MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

Manual SQL seeds (example):

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -i "GroupC\scripts\01_create_database.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GroupC_DB -i "GroupC\scripts\02_create_tables.sql"
```

Visual Studio: open `GroupC.slnx`, press **F5**.

**Test:** None identified (no test project in solution).

**Deploy:** None identified (no publish/deploy scripts in repo).

## Environment Variables

None identified. Configuration uses:

- `GroupC/App.config` — connection string name `GroupCSqlServer`
- `%LocalAppData%\GroupC\settings.json` — store branding (not environment variables)

## Architecture Notes

On startup, `MainMenuForm` opens a modal `LoginForm`. After sign-in, `AppSession` holds role (`Admin` / `Cashier`) and cashier identity for the session. Forms call `DatabaseInitializer.EnsureDatabase()` to create `GroupC_DB`, apply DDL, and seed a small product set when `products` is empty. Sales flow: `SalesForm` builds a cart in memory, applies one discount toggle (PWD / Senior / Member) and optional VAT, saves `sales` + `sale_items`, then `ReceiptBranding.BuildReceiptText` writes `sales.receipt_text`. Store name, footer, and currency come from `AppSettings` JSON, not SQL. Receipt preview/print/PDF reads stored `receipt_text` or rebuilds from sale metadata. SQL scripts under `GroupC/scripts/` are reference/manual only; the app does not execute them from disk.

## Coding Conventions

- VB.NET **PascalCase** for classes, forms, and public members; forms named `*Form.vb`
- Shared utilities as `Public NotInheritable Class` with `Private Sub New()` (e.g. `UiTheme`, `DatabaseConfig`)
- `AppSession` is a **Module** for session-scoped globals
- Many forms build UI in code (`CreateControls`, `TableLayoutPanel`) rather than only the Designer
- Styling: call `UiTheme.ApplyStandardWindowChrome`, `ApplyDataGridViewChrome`, button helpers
- Grids: `GridDisplayHelper.ApplyStandardBoundGridDisplay` for bound admin grids
- Data access: inline `SqlConnection` / `SqlCommand` in form and service classes (no ORM)
- XML doc comments (`'''`) on key public APIs
- Schema changes belong in `DatabaseInitializer.vb` and matching `GroupC/scripts/` files
- **Lint/format:** None identified (no `.editorconfig`, StyleCop, or test runner in repo)

## Known Constraints or Notes

- **Demo admin password** is hardcoded in `DatabaseConfig.HardcodedAdminPassword` (`admin123`); change before real use
- **Cashier accounts** are not pre-seeded; admin must create them in `CashierAccountsForm`
- **`BackupRestoreForm.vb` is referenced** in `MainMenuForm.vb` and `README.md` but **not present** in the repo — build may fail if that menu path is used until the file is added
- Close running **GroupC.exe** before `dotnet build` (exe file lock is common on Windows)
- Ignore gitignored folders: `bin/`, `obj/`, `.vs/`
- `GroupC/scripts/02_create_tables.sql` is **partial**; full schema comes from `DatabaseInitializer.vb`
- SQL seed scripts **03–05** are idempotent but must be run manually; app only auto-seeds a small catalog when `products` is empty
- `SettingsForm` edits only `StoreName`, `ReceiptFooter`, `CurrencySymbol`; other `settings.json` fields (`StoreBranch`, `ReturnPolicyText`, etc.) require JSON edit or UI extension
- Legacy rows in `sales.receipt_text` keep old format; only new finalized sales get the full receipt template
- Receipt history loads up to **500** recent sales; dashboard chart span max **90** days
- Do not commit LocalDB `.mdf`/`.ldf` files or user-specific IDE state
