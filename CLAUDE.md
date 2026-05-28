# CLAUDE.md

Context for AI assistants working in this repository.

## Project Overview

**International Bookstore** is a Windows desktop point-of-sale (POS) app for small retail counters (bookstore, school supplies, coursework demos). Cashiers ring up sales with discounts and tax, print or export receipts, and view sale history. Administrators manage products, categories, cashier accounts, reports, and store settings. Data persists locally in SQL Server LocalDB (`GroupC_DB`); branding settings live in a JSON file under the user profile.

**Current State:** All forms have been redesigned with modern, consistent UI/UX using a comprehensive design system. The application uses responsive layouts (TableLayoutPanel/FlowLayoutPanel), follows an 8px spacing grid, and implements Material Design-inspired visual language.

**Full name:** International Bookstore — Sales & Receipt System (Group C)

**Target users:**
- **Store administrator** — owner/manager; full menu (catalog, cashiers, POS, receipts, reports, settings, backup guidance)
- **Cashier** — front-desk staff; Point of Sale and Receipt Preview only

**Store benefits:** accurate pricing (auto totals/discount/tax), professional branded receipts, inventory control with low-stock awareness, sale accountability with audit trail, daily reconciliation via dashboard/reports, role-based security, local persistence without cloud dependency.

## Project Documentation Index

- docs/01-system-description.md — system overview and purpose
- docs/02-system-requirements.md — functional and non-functional requirements
- docs/03-interface-design-and-navigation.md — UI layout and navigation flow
- docs/04-database-design.md — schema, tables, and relationships
- docs/05-features-checklist.md — required features and completion status
- docs/06-demo-and-presentation-guide.md — walkthrough script for demo day
- docs/07-project-submission-checklist.md — submission requirements and status
- docs/08-title-page-and-reflection-template.md — group members and project reflection
- docs/screenshots/ — UI screenshots of all forms (suggested: `01-login.png` through `10-audit-log.png`; folder currently contains README only)

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

## Features

### Cashier Features
- **Point of Sale:** Add products to cart, adjust quantities, apply customer discounts (PWD 20%, Senior 20%, Member 10%), toggle VAT/tax, finalize sale
- **Receipt Viewing:** Browse receipt history, search by sale ID/amount/date, filter by date ranges, preview receipts with zoom controls
- **Receipt Export:** Print receipts, save as PDF or text file, copy receipt text to clipboard
- **Sales Dashboard:** View daily sales chart (last 7 days default), see total sales metrics

### Administrator Features
- **Product Management:** Add/update/deactivate products, set prices and categories, import products from CSV, search and filter
- **Category Management:** Create/rename/deactivate product categories, organize catalog
- **Cashier Account Management:** Register new cashier accounts, update display names, reset passwords, activate/deactivate accounts
- **Sales Reports:** View daily sales summaries, top-selling products, date range filtering
- **Audit Log:** Track all administrative actions (product changes, category changes, cashier account operations, settings changes)
- **Store Settings:** Configure store name, receipt footer text, currency symbol
- **All Cashier Features:** Admins have full access to POS and receipt viewing

**Additional features (beyond Group C minimum):** category management, cashier accounts, CSV import, dashboard KPIs + chart (7–90 days) + low-stock alert, audit log, store settings, backup/restore SQL guidance dialog, cross-screen sidebar navigation (`WorkspaceNavigation`), role-based menu hiding.

### Security & Authentication
- **Role-based Access:** Administrator vs Cashier roles with different menu access
- **Admin Authentication:** Hardcoded password (`admin123` - **change before production**)
- **Cashier Authentication:** Database-backed accounts with bcrypt password hashing
- **Session Management:** In-memory session tracking via `AppSession` module
- **Audit Trail:** All admin actions logged with timestamp, username, role

## Project Structure

```
GroupC_Sales-and-Receipt/
├── CLAUDE.md                          # This file - comprehensive project documentation
├── README.md                          # User-facing setup and usage guide
├── GroupC.slnx                        # Visual Studio solution file
└── GroupC/                            # Main application source
    ├── GroupC.vbproj                  # Project file with package references
    ├── App.config                     # SQL connection string config
    ├── My Project/
    │   ├── Application.myapp          # Startup form configuration (MainMenuForm)
    │   └── AssemblyInfo.vb            # Assembly metadata
    ├── Assets/
    │   ├── AppIcon.ico                # Application icon
    │   ├── AppLogo.png                # Branding logo
    │   └── ReceiptLogo.png            # Receipt header logo
    ├── scripts/                       # Manual SQL scripts (reference only)
    │   ├── README.md                  # Script usage guide
    │   ├── 01_create_database.sql     # Database creation
    │   ├── 02_create_tables.sql       # Partial schema (superseded by DatabaseInitializer.vb)
    │   ├── 03_seed_small_catalog.sql  # Sample products (books, stationery)
    │   ├── 04_seed_large_catalog.sql  # Expanded product catalog
    │   └── 05_merge_duplicate_categories.sql  # Cleanup script
    ├── Forms/                         # (Conceptual - files are in root GroupC/)
    │   ├── LoginForm.vb               # Authentication UI
    │   ├── MainMenuForm.vb            # Dashboard and navigation
    │   ├── SalesForm.vb               # Point of sale interface
    │   ├── ReceiptForm.vb             # Receipt history viewer
    │   ├── ProductsForm.vb            # Product CRUD
    │   ├── CategoriesForm.vb          # Category CRUD
    │   ├── CashierAccountsForm.vb     # Cashier account management
    │   ├── ReportsForm.vb             # Sales reports and audit log
    │   └── SettingsForm.vb            # Store configuration
    ├── Services/
    │   ├── DatabaseConfig.vb          # Connection string and constants
    │   ├── DatabaseInitializer.vb     # Schema creation and seeding
    │   ├── AppSettings.vb             # JSON settings persistence
    │   ├── AppSession.vb              # Session state (Module)
    │   ├── CashierAccountService.vb   # Cashier authentication
    │   ├── PasswordHasher.vb          # bcrypt password hashing
    │   ├── AuditLogger.vb             # Audit trail persistence
    │   └── ErrorLogger.vb             # Exception logging
    ├── Models/
    │   ├── CartLineItem.vb            # In-memory cart line
    │   └── ReceiptSnapshot.vb         # Structured receipt data
    ├── UI/
    │   ├── UiTheme.vb                 # Design system (colors, typography, spacing, helpers)
    │   ├── GridDisplayHelper.vb       # DataGridView utilities
    │   ├── FormStatusHelper.vb        # Status bar helpers
    │   └── AppBranding.vb             # Application branding utilities
    ├── Receipts/
    │   ├── ReceiptBranding.vb         # 40-column receipt text builder
    │   ├── PdfReceiptExporter.vb      # PDF export via PDFsharp
    │   ├── ReceiptPrintHelper.vb      # Windows print API integration
    │   └── WindowsFontResolver.vb     # PDFsharp font registration
    └── ApplicationEvents.vb           # App startup and exception handling
```

## Database Schema

**Database:** `GroupC_DB` (created automatically by `DatabaseInitializer.vb`)

### Tables

#### `products`
- `product_id` (INT, PK, IDENTITY) — Unique product identifier
- `product_name` (NVARCHAR(100), NOT NULL, UNIQUE) — Product display name
- `unit_price` (DECIMAL(10, 2), NOT NULL) — Price per unit
- `category_id` (INT, NULL, FK → categories) — Optional category assignment
- `is_active` (BIT, DEFAULT 1) — Soft delete flag (1 = active, 0 = deactivated)
- `created_at` (DATETIME2, DEFAULT GETDATE()) — Creation timestamp

**Indexes:** `product_name`, `category_id`, `is_active`

#### `categories`
- `category_id` (INT, PK, IDENTITY) — Unique category identifier
- `category_name` (NVARCHAR(100), NOT NULL, UNIQUE) — Category display name
- `is_active` (BIT, DEFAULT 1) — Soft delete flag
- `created_at` (DATETIME2, DEFAULT GETDATE()) — Creation timestamp

**Indexes:** `category_name`, `is_active`

#### `sales`
- `sale_id` (INT, PK, IDENTITY) — Unique sale identifier
- `sale_date` (DATETIME2, DEFAULT GETDATE()) — Transaction timestamp
- `subtotal` (DECIMAL(10, 2), NOT NULL) — Pre-discount, pre-tax total
- `discount_amount` (DECIMAL(10, 2), DEFAULT 0) — Total discount applied
- `tax_amount` (DECIMAL(10, 2), DEFAULT 0) — Total tax applied
- `total_amount` (DECIMAL(10, 2), NOT NULL) — Final amount due
- `discount_type` (NVARCHAR(50), NULL) — Discount name (PWD, Senior, Member)
- `tax_rate` (DECIMAL(5, 2), DEFAULT 0) — Tax percentage applied
- `cashier_username` (NVARCHAR(50), NULL) — Cashier who processed sale
- `receipt_text` (NVARCHAR(MAX), NULL) — Formatted 40-column receipt snapshot

**Indexes:** `sale_date`, `cashier_username`

#### `sale_items`
- `sale_item_id` (INT, PK, IDENTITY) — Unique line item identifier
- `sale_id` (INT, NOT NULL, FK → sales) — Parent sale
- `product_name` (NVARCHAR(100), NOT NULL) — Product name snapshot
- `quantity` (INT, NOT NULL) — Quantity sold
- `unit_price` (DECIMAL(10, 2), NOT NULL) — Price snapshot
- `line_total` (DECIMAL(10, 2), NOT NULL) — quantity × unit_price

**Indexes:** `sale_id`

#### `cashier_accounts`
- `cashier_id` (INT, PK, IDENTITY) — Unique account identifier
- `username` (NVARCHAR(50), NOT NULL, UNIQUE) — Login username (3-50 chars, alphanumeric + underscore)
- `display_name` (NVARCHAR(100), NOT NULL) — Friendly name shown on receipts
- `password_hash` (NVARCHAR(200), NOT NULL) — bcrypt hashed password
- `is_active` (BIT, DEFAULT 1) — Account status (1 = active, 0 = deactivated)
- `created_at` (DATETIME2, DEFAULT GETDATE()) — Registration timestamp

**Indexes:** `username`, `is_active`

#### `audit_log`
- `audit_id` (INT, PK, IDENTITY) — Unique audit entry identifier
- `event_time` (DATETIME2, DEFAULT GETDATE()) — Timestamp
- `event_type` (NVARCHAR(50), NOT NULL) — Action category (PRODUCT_ADDED, CASHIER_REGISTERED, etc.)
- `description` (NVARCHAR(500), NULL) — Human-readable event description
- `username` (NVARCHAR(50), NULL) — User who triggered the action
- `user_role` (NVARCHAR(20), NULL) — Role at time of action (Admin, Cashier)

**Indexes:** `event_time`, `event_type`, `username`

#### `error_log`
- `error_id` (INT, PK, IDENTITY) — Unique error identifier
- `error_time` (DATETIME2, DEFAULT GETDATE()) — Timestamp
- `error_message` (NVARCHAR(MAX), NULL) — Exception message
- `stack_trace` (NVARCHAR(MAX), NULL) — Exception stack trace
- `source` (NVARCHAR(200), NULL) — Exception source context

**Indexes:** `error_time`

### Foreign Key Relationships
- `products.category_id` → `categories.category_id` (ON DELETE SET NULL)
- `sale_items.sale_id` → `sales.sale_id` (ON DELETE CASCADE)

**Updated per docs/04-database-design.md:** Runtime schema in `DatabaseInitializer.vb` may use column names below. Map to instructor handout equivalents during defense.

| Instructor handout | This project |
|---|---|
| `products.product_id` | `products.id` |
| `products.name` | `products.product_name` |
| `products.unit_price` | `products.price` |
| `products.stock_qty` | `products.stock_quantity` |
| `sale_items.item_id` | `sale_items.sale_item_id` |
| `sale_items.unit_price` / `line_total` | `sale_items.price` / `subtotal` |
| `cashier_accounts.account_id` | `cashier_accounts.cashier_id` |
| `audit_log` | `AuditLogs` (+ `audit_products`, `audit_sales`) |
| `error_log.error_id` / `error_time` | `error_log.log_id` / `occurred_at` |

Additional runtime columns/tables not listed above in this file:
- **products:** `stock_quantity`, `image_path`, `updated_at`
- **sales:** `subtotal_before_discount`, `discount_percent`, `amount_before_tax`, `tax_percent`, `amount_tendered`, `change_given`, `created_at` (plus `sale_date` stored UTC)
- **cashier_accounts:** `password_salt`, `last_login_at`
- **AuditLogs:** `LogID`, `Action`, `Detail`, `PerformedBy`, `LoggedAt` (Reports → System Audit Logs)
- **audit_products** / **audit_sales:** domain-specific audit via `AuditLogger.LogProduct` / `LogSale`
- **sale_items:** no FK to `products` — name/price snapshot only

Sample verification queries: active product count, today's sales total, daily revenue GROUP BY, low stock (`stock_quantity <= 5`).

## UI/UX Design System (UiTheme.vb)

**Philosophy:** Material Design-inspired, 8px spacing grid, responsive layouts, consistent typography and color usage.

### Spacing Scale (8px base grid)
```vb
SpaceXs = 4px      ' Tight spacing, chip gaps
SpaceSm = 8px      ' Label-to-input, small gaps
SpaceMd = 12px     ' Button spacing, card internal padding
SpaceLg = 16px     ' Section padding, card default padding
SpaceXl = 24px     ' Panel padding, large section gaps
Space2xl = 32px    ' Form/page outer padding
Space3xl = 48px    ' Major section breaks
```

### Typography Scale
```vb
FontHeading1 = Segoe UI 20pt Bold    ' Page titles
FontHeading2 = Segoe UI 16pt Bold    ' Section titles
FontHeading3 = Segoe UI 13pt Bold    ' Subsection headers, grid column headers
FontBody = Segoe UI 10pt Regular     ' Default body text, inputs, buttons
FontBodySmall = Segoe UI 9pt Regular ' Secondary text, hints, captions
FontCaption = Segoe UI 8.5pt Regular ' Chart axis labels, tiny text
FontButton = Segoe UI 10pt Regular   ' Button text
```

### Color Palette
**Primary:**
- `PrimaryAccent` (#1A237E) — Navy blue for primary actions, titles
- `PrimaryAccentHover` (#283593)
- `PrimaryAccentPressed` (#121858)

**Secondary:**
- `SecondaryAccent` (#1565C0) — Lighter blue for secondary actions, links
- `SecondaryAccentHover` (#1976D2)
- `SecondaryAccentPressed` (#0D47A1)

**Semantic:**
- `Success` (#2E7D32) — Green for success states, reactivate buttons
- `Warning` (#F57F17) — Orange/yellow for warning states
- `Danger` (#C62828) — Red for delete/deactivate actions, errors

**Neutral:**
- `FormBackground` (#F0F2F5) — Light gray page background
- `CardSurface` (#FFFFFF) — White card/panel backgrounds
- `CardBorder` (#E0E0E0) — Light gray borders
- `TextPrimary` (#212121) — Primary text color
- `TextSecondary` (#757575) — Secondary text, labels, hints
- `TextOnAccent` (#FFFFFF) — White text on colored backgrounds

**Extended:**
- `FocusRing` (#1976D2) — Input focus indicator
- `DisabledBackground` (#E0E0E0)
- `DisabledText` (#9E9E9E)
- `InputBorder` (#BDBDBD)
- `InputBorderFocus` (#1976D2)
- `DividerColor` (#EEEEEE)
- `SurfaceVariant` (#F5F5F5) — Alternate surface
- `SuccessLight` (#E8F5E9) — Success background tint
- `WarningLight` (#FFF8E1) — Warning background tint
- `DangerLight` (#FFEBEE) — Danger background tint
- `InfoBackground` (#E3F2FD)
- `InfoText` (#1565C0)

### Component Heights
```vb
InputHeight = 32px         ' Text inputs, numeric up/downs
ButtonHeightSm = 32px      ' Small utility buttons
ButtonHeightMd = 40px      ' Standard action buttons
ButtonHeightLg = 48px      ' Primary large buttons
GridRowHeight = 40px       ' DataGridView row height
GridHeaderHeight = 44px    ' DataGridView header height
```

### Border Radius Scale
```vb
RadiusSm = 4px    ' Small elements
RadiusMd = 8px    ' Medium elements
RadiusLg = 12px   ' Large elements, default buttons
RadiusXl = 16px   ' Extra large elements
```

### Helper Methods
- `ApplyStandardWindowChrome(form)` — Set form background, font
- `ApplyPrimaryButton(btn)` — Navy blue filled button
- `ApplySecondaryButton(btn)` — White outlined button
- `ApplySecondaryAccentButton(btn)` — Blue outlined button
- `ApplySuccessButton(btn)` — Green filled button
- `ApplyWarningButton(btn)` — Orange filled button
- `ApplyDangerButton(btn)` — Red filled button
- `ApplyDataGridViewChrome(dgv)` — Consistent grid styling
- `ApplyReadOnlyGridTheme(dgv)` — Read-only grid variant
- `CreateCardPanel(padding)` — White card with border
- `CreateHeadingLabel(text, level)` — Consistent heading styles
- `CreateDivider()` — Horizontal rule separator
- `CreateEmptyStateLabel(text)` — Centered empty state message
- `CreateButtonRow(alignment)` — FlowLayoutPanel for action buttons
- `CreateFormSection(title)` — Titled card section
- `ApplyInputFieldStyle(textBox)` — Modern input styling
- `ApplyComboBoxStyle(combo)` — Consistent dropdown styling
- `ApplyGroupBoxStyle(groupBox)` — Modern section container

## Form Responsibilities

### LoginForm.vb
**Purpose:** Authentication entry point for admin and cashier users.

**Layout:** Centered card with responsive TableLayoutPanel (3×3 grid for centering), FlowLayoutPanel for form fields.

**Features:**
- Logo display (from ReceiptLogo.png) or fallback to application title
- Role selection (Administrator vs Cashier) via radio buttons
- Conditional username field (hidden for admin, shown for cashier)
- Password field with toggle visibility button (👁 icon)
- Custom PasswordTogglePanel control for show/hide password
- Keyboard support (Tab order, Enter to submit)

**Authentication Flow:**
- Admin: Password-only auth against `DatabaseConfig.HardcodedAdminPassword` (default: `admin123`)
- Cashier: Username + password auth against `cashier_accounts` table via `CashierAccountService`
- On success: Set `AppSession.CurrentRole` and `AppSession.CurrentCashierId`/`CurrentCashierUsername`
- On failure: Show error, clear password field, refocus

**Validation:**
- Admin: Password cannot be empty
- Cashier: Username and password required, minimum password length enforced
- Inactive cashier accounts rejected with specific error message

### MainMenuForm.vb
**Purpose:** Post-login dashboard and navigation hub.

**Layout:** SplitContainer with left sidebar (240px) and right dashboard area (responsive TableLayoutPanel).

**Updated per docs/03-interface-design-and-navigation.md:** Shared workspace shell uses dark navy sidebar (~220px), light gray workspace, top bar with page title; child forms highlight current nav item. Dashboard shows **low-stock alert** when any active product has `stock_quantity <= 5`. Cross-screen sidebar navigation via `WorkspaceNavigation` lets users switch modules without returning to dashboard first.

**Features:**
- **Top Header:** Application title (Heading2), system status label (database connection health)
- **Left Sidebar (Navigation):**
  - Role-based menu buttons (48px height for main actions, 40px for utilities)
  - Main section: Products, Categories, Cashier Accounts (admin only), Sales, Receipts, Reports (admin only)
  - Bottom section: Settings (admin only), Backup/Restore (referenced but not implemented), Logout
  - White card surface with proper spacing
- **Right Dashboard:**
  - **KPI Cards (2×2 grid):** Total products, sales today, 7-day sales, last sale amount
  - **Daily Sales Chart:** Bar chart showing last 7/14/30 days or custom range, zoom to 90 days max
  - **Chart Filters:** Date range pickers, preset buttons, sort options (newest/oldest/high/low)
  - **Chart Features:** Hover highlights today, value labels on bars, compact axis labels, responsive sizing
- **Status Bar:** Bottom status strip with ready/action feedback

**Dashboard Data:**
- KPI refresh on form load and after sales
- Chart data fetched from `sales` table grouped by date
- Auto-refresh chart when filters change
- Chart shows up to 90 days, defaults to last 7 days

**Navigation:**
- Button clicks open child forms modally (ShowDialog)
- Hide MainMenu while child form open, restore on close
- Logout returns to LoginForm, clears session

### SalesForm.vb
**Purpose:** Point of sale interface for ringing up sales.

**Layout:** Side-by-side layout with left sidebar (380px) and right main area (cart + checkout).

**Features:**
- **Left Sidebar (Product Selection):**
  - Title "Point of Sale" (Heading2)
  - Category filter dropdown
  - Product name dropdown (filtered by category)
  - Unit price display (read-only, auto-populated)
  - Quantity numeric up/down (1–99999)
  - "Add to cart" button (primary blue)
  - Utility buttons: "Open Products…", "Back to Menu"
- **Right Main Area:**
  - **Cart Grid (62% of height):**
    - Columns: # (index), Product, Price, Qty (editable), Subtotal
    - Inline quantity editing (double-click to edit)
    - Row selection for remove/clear operations
    - Empty state message when no items
  - **Checkout Panel (38% of height, 3-column layout):**
    - **Left Column (36%):** Customer discount toggles (PWD, Senior, Member - mutually exclusive), VAT/Tax toggle with percentage input
    - **Center Column (40%):** Summary details (Subtotal, Discount, Tax, Tendered amount input, Change display)
    - **Right Column (24%):** Amount Due (large Heading1), Finalize Sale button (green, 48px height)
- **Cart Operations:** Add item, remove selected item, clear cart (with confirmation)
- **Discount Logic:** Single discount type at a time (PWD/Senior 20%, Member 10%), recalculates totals
- **Tax Logic:** Optional VAT toggle, user-configurable percentage (default 12%), recalculates totals
- **Finalize Flow:**
  1. Validate cart not empty, amount tendered >= total
  2. Build `ReceiptSnapshot` with line items, totals, metadata
  3. Generate receipt text via `ReceiptBranding.BuildReceiptText`
  4. Insert `sales` row (subtotal, discount, tax, total, receipt_text)
  5. Insert `sale_items` rows (product name snapshot, qty, price)
  6. Show success status, open ReceiptForm with finalized receipt
  7. Clear cart for next sale

**Status Feedback:** Status bar shows success/error messages, auto-clears after 4 seconds.

### ReceiptForm.vb
**Purpose:** View, search, filter, and export past sale receipts.

**Layout:** Side-by-side with left panel (360px filters/history) and right preview area.

**Features:**
- **Left Panel:**
  - Title "Receipts" (Heading2), subtitle instructions
  - Search box (by sale ID, amount, date)
  - Date filter dropdown (All, Today, This week, This month, Custom range)
  - Custom date range pickers (From/To)
  - Sort dropdown (Newest/Oldest first, Amount high/low)
  - Receipt history list (last 500 sales, scrollable)
  - Refresh button, Export Batch button
  - Selected sale chip (ID, date, total, cashier)
  - Back to Menu button
- **Right Panel:**
  - Receipt preview (40-column monospaced text in RichTextBox)
  - Logo display above receipt (if configured)
  - Zoom controls (+/– buttons, percentage label)
  - Action toolbar: Print, Print Preview, Save PDF, Save Text, Copy, Email (placeholder), Details (placeholder)
  - Empty state when no receipt selected
- **Search/Filter:** Real-time filtering as user types or changes dropdowns
- **Receipt Display:** Monospace font (Courier New 10pt), white background, scrollable, zoomable (75%–150%)
- **Export:**
  - **Print:** Windows print dialog → receipt print helper
  - **PDF:** Save dialog → PDFsharp export with embedded fonts
  - **Text:** Save dialog → plain .txt export
  - **Copy:** Copy receipt text to clipboard
- **Limitations:** Loads up to 500 most recent sales, performance degrades with large datasets

### ProductsForm.vb
**Purpose:** Administrator CRUD interface for product catalog.

**Layout:** Side-by-side with left sidebar (360px) and right data grid.

**Features:**
- **Left Sidebar:**
  - Title "Manage Products" (Heading2)
  - Input fields: Product Name (text, max 100 chars), Unit Price (numeric, 0.01–999999.99), Category (dropdown)
  - Action buttons: Add Product (primary), Update (primary), Deactivate (warning), Reactivate (success, shown when inactive selected)
  - Utility buttons: Refresh, Import CSV, Manage Categories, Back
- **Right Grid:**
  - Columns: Product ID (hidden), Product Name, Unit Price, Category, Active Status
  - Search box (real-time filter by product name)
  - Status filter dropdown (Active only, All, Inactive only)
  - Category filter dropdown (All, Uncategorized, specific category)
  - Grid chrome: alternating row colors, row height 40px, header height 44px
  - Selection mode: full row select, single selection
- **Add Flow:** Enter name/price/category, click Add → insert product → refresh grid → clear inputs
- **Update Flow:** Select row, modify inputs, click Update → update product → refresh grid
- **Deactivate/Reactivate:** Select row, click button → soft delete (set is_active = 0/1) → refresh grid
- **CSV Import:** Open file dialog → parse CSV (product_name, unit_price, category_name) → bulk insert/update → refresh grid
- **Validation:** Product name required and unique, price in valid range, category exists

### CategoriesForm.vb
**Purpose:** Administrator CRUD interface for product categories.

**Layout:** Side-by-side with left sidebar (360px) and right data grid.

**Features:**
- **Left Sidebar:**
  - Title "Book categories" (Heading2), hint text
  - Input field: Category Name (text, max 100 chars)
  - Action buttons: Add Category, Update Name, Deactivate, Reactivate
  - Utility buttons: Refresh, Back
- **Right Grid:**
  - Columns: Category ID (hidden), Category Name, Active Status, Product Count (computed)
  - Status filter dropdown (Active, All, Inactive)
  - Grid chrome with consistent styling
- **Add/Update/Deactivate:** Same pattern as ProductsForm
- **Validation:** Category name required and unique
- **Notes:** Deactivating category sets product.category_id to NULL for affected products

### CashierAccountsForm.vb
**Purpose:** Administrator interface for cashier account lifecycle.

**Layout:** Side-by-side with left panel (340px) and right data grid.

**Features:**
- **Left Panel:**
  - Input fields: Username (3-50 chars, alphanumeric + underscore), Display Name (shown on receipts), Password (min 6 chars), Confirm Password
  - Password visibility toggle (👁 button)
  - Password match indicator (green "Passwords match" or red "Do not match")
  - Action buttons: Register Cashier (primary, 48px), Update Display Name (secondary), Reset Password (secondary), Deactivate/Reactivate
  - Selected cashier chip (username, display name, status badge)
- **Right Grid:**
  - Columns: Cashier ID (hidden), Username, Display Name, Active Status, Created At
  - Filter dropdown (Active, Inactive, All)
  - Refresh button
- **Register Flow:** Enter username/display/passwords, validate match + length + uniqueness, bcrypt hash password → insert → refresh grid
- **Update Display Flow:** Select cashier, modify display name → update → refresh grid
- **Reset Password Flow:** Select cashier, enter new password twice → validate → bcrypt hash → update → refresh grid
- **Deactivate/Reactivate:** Select cashier → soft delete (is_active = 0/1) → refresh grid
- **Validation:** Username 3-50 chars + unique, password min 6 chars, passwords match, display name not empty
- **Security:** All passwords bcrypt hashed (cost factor 12), plain text never stored

### ReportsForm.vb
**Purpose:** Sales analytics and audit trail viewing.

**Layout:** Tabbed interface with two tabs: "Sales & Revenue" and "Audit Log".

**Features:**
- **Sales Tab:**
  - Date range filter (From/To date pickers)
  - Run Report button
  - Summary label (total sales, date range)
  - Daily Sales grid (Date, Total Sales, Number of Sales)
  - Top Products grid (Product, Quantity Sold, Revenue)
- **Audit Tab:**
  - Date range filter (From/To)
  - Load Log button
  - Audit log grid (Time, Event Type, Description, Username, Role)
  - Event types: PRODUCT_ADDED, PRODUCT_UPDATED, PRODUCT_DEACTIVATED, CATEGORY_ADDED, CASHIER_REGISTERED, SETTINGS_CHANGED, etc.
- **Analytics:**
  - Aggregates sales by day, products by quantity
  - Default date range: last 30 days
  - No pagination (loads all matching rows)

### SettingsForm.vb
**Purpose:** Configure store-level settings persisted to JSON.

**Layout:** Modal dialog (560×440px) with card panel.

**Features:**
- Input fields: Store Name (max 120 chars), Receipt Footer (max 500 chars), Currency Symbol (1-6 chars)
- Buttons: OK (primary), Cancel (secondary)
- Validation: Store name required, currency symbol 1-6 chars
- Persistence: Saves to `%LocalAppData%\GroupC\settings.json` via `AppSettings.Save`
- Audit: Logs SETTINGS_CHANGED event
- Note: Other JSON fields (StoreBranch, ReturnPolicyText, etc.) not exposed in UI - require manual JSON edit

## Service Layer

### DatabaseInitializer.vb
**Responsibility:** Ensure database and schema exist, seed initial data.

**Key Methods:**
- `EnsureDatabase()` — Main entry point, checks existence, creates if missing, ensures schema
- `CreateDatabaseIfNotExists()` — CREATE DATABASE if not exists
- `EnsureTablesExist()` — CREATE TABLE for products, categories, sales, sale_items, cashier_accounts, audit_log, error_log
- `SeedSmallCatalog()` — Insert 10-20 sample products if products table empty

**Initialization Flow:**
1. Connect to LocalDB (creates instance if needed)
2. Check if GroupC_DB exists, create if not
3. Switch to GroupC_DB context
4. Create all tables with proper constraints, indexes, foreign keys
5. If products.Count = 0, seed small catalog
6. Return success/failure

**Schema Management:** This is the authoritative source for schema. SQL scripts in `GroupC/scripts/` are reference only.

### AppSettings.vb
**Responsibility:** JSON settings persistence for store branding.

**File Location:** `%LocalAppData%\GroupC\settings.json`

**Settings (AppSettingsData class):**
```vb
StoreName (String)           ' "International Bookstore" default
StoreBranch (String)         ' "Main Branch" default
CurrencySymbol (String)      ' "₱" default
ReceiptFooter (String)       ' "Thank you for your purchase!" default
ReturnPolicyText (String)    ' Return policy details
TaxRate (Decimal)            ' Default tax percentage
```

**Methods:**
- `AppSettings.Current` — Singleton property, lazy load on first access
- `AppSettings.Reload()` — Force reload from disk
- `AppSettings.Save(data)` — Serialize and write to JSON file
- `AppSettings.GetSettingsFilePath()` — Compute %LocalAppData% path

**Usage:** `SettingsForm` edits StoreName, ReceiptFooter, CurrencySymbol. Other fields require manual JSON editing.

### AppSession.vb (Module)
**Responsibility:** Track authenticated user session state in memory.

**Session Variables:**
```vb
Public CurrentRole As String = Nothing        ' "Admin" or "Cashier"
Public CurrentCashierId As Integer? = Nothing ' Cashier primary key
Public CurrentCashierUsername As String = Nothing
Public CurrentCashierDisplay As String = Nothing
```

**Lifecycle:**
- Set by `LoginForm` on successful authentication
- Read by all forms to determine role-based menu visibility, audit log username
- Cleared on logout (return to LoginForm)
- Not persisted (session ends when app closes)

### CashierAccountService.vb
**Responsibility:** Cashier account CRUD and authentication.

**Key Methods:**
- `ValidateCredentials(username, password)` — Returns (success: Boolean, cashierId, displayName, errorMessage)
- `CreateAccount(username, displayName, password)` — bcrypt hash, insert, audit log
- `UpdateDisplayName(cashierId, newDisplayName)` — Update display_name, audit log
- `ResetPassword(cashierId, newPassword)` — bcrypt hash, update password_hash, audit log
- `DeactivateAccount(cashierId)` — Set is_active = 0, audit log
- `ReactivateAccount(cashierId)` — Set is_active = 1, audit log
- `IsUsernameAvailable(username)` — Check uniqueness

**Validation:**
- Username: 3-50 chars, alphanumeric + underscore, unique
- Display name: 1-100 chars, not empty
- Password: min 6 chars (frontend enforces, backend double-checks)

**Security:** Uses `PasswordHasher.vb` for bcrypt hashing (cost factor 12).

### PasswordHasher.vb
**Responsibility:** bcrypt password hashing and verification.

**Methods:**
- `HashPassword(plaintext)` — Returns bcrypt hash string
- `VerifyPassword(plaintext, hash)` — Returns Boolean (match or not)

**Implementation:** Uses BCrypt.Net-Next NuGet package (or compatible), cost factor 12.

### AuditLogger.vb
**Responsibility:** Record admin/cashier actions for compliance and debugging.

**Key Method:**
- `LogAudit(eventType, description, username, userRole)` — Insert into audit_log table

**Event Types (examples):**
- `PRODUCT_ADDED`, `PRODUCT_UPDATED`, `PRODUCT_DEACTIVATED`, `PRODUCT_REACTIVATED`
- `CATEGORY_ADDED`, `CATEGORY_UPDATED`, `CATEGORY_DEACTIVATED`
- `CASHIER_REGISTERED`, `CASHIER_DISPLAY_UPDATED`, `CASHIER_PASSWORD_RESET`, `CASHIER_DEACTIVATED`
- `SETTINGS_CHANGED`
- `LOGIN_SUCCESS`, `LOGIN_FAILED`

**Usage:** Called after successful mutations in ProductsForm, CategoriesForm, CashierAccountsForm, SettingsForm, LoginForm.

**Viewing:** `ReportsForm` Audit Log tab shows all events with filtering by date range.

### ErrorLogger.vb
**Responsibility:** Persist unhandled exceptions to database for debugging.

**Key Method:**
- `Log(exception, source)` — Insert exception.Message, exception.StackTrace, source into error_log table

**Usage:**
- `ApplicationEvents.vb` logs unhandled exceptions on app crash
- Individual forms log exceptions in Try-Catch blocks
- Not exposed in UI (database query required to view)

## Business Logic Flows

### Sale Lifecycle
1. **Cart Building (SalesForm):**
   - User selects category → product → enters quantity → clicks Add
   - CartLineItem created in memory (product name, quantity, unit price, line total)
   - Added to BindingList backing DataGridView
   - Grid updates, shows running subtotal
2. **Discounts/Tax:**
   - User toggles discount button (PWD/Senior/Member) → recalculates discount_amount
   - User toggles tax, sets percentage → recalculates tax_amount
   - Summary panel updates: Subtotal, Discount, Tax, Total
3. **Tendered Amount:**
   - User enters amount in Tendered field
   - Change auto-calculated and displayed (green if sufficient, red if insufficient)
4. **Finalize:**
   - Validate: cart not empty, tendered >= total
   - Build `ReceiptSnapshot`: line items, subtotal, discount (type + amount), tax (rate + amount), total, tendered, change, metadata (date, cashier, store name)
   - Call `ReceiptBranding.BuildReceiptText(snapshot)` → 40-column formatted receipt
   - Insert `sales` row: subtotal, discount_amount, tax_amount, total_amount, discount_type, tax_rate, cashier_username, receipt_text
   - Get inserted sale_id (SCOPE_IDENTITY)
   - Insert `sale_items` rows: sale_id, product_name, quantity, unit_price, line_total
   - Commit transaction
   - Show success status
   - Open `ReceiptForm` with sale_id and receipt_text
   - Clear cart, reset UI

### Receipt Text Generation
**Function:** `ReceiptBranding.BuildReceiptText(ReceiptSnapshot)` → String

**Format:** 40-column centered text, monospaced font.

**Structure:**
```
[Logo text if no image]
STORE NAME
Store Branch
════════════════════════════════════════

Sale #1234
Date: MMM DD, YYYY HH:MM AM/PM
Cashier: Display Name
────────────────────────────────────────

Product Name               Qty      Total
Product Name 2               3     ₱99.99
────────────────────────────────────────

Subtotal                        ₱999.99
Discount (PWD 20%)             –₱200.00
Tax (12%)                       ₱120.00
────────────────────────────────────────
TOTAL                           ₱919.99

Tendered                        ₱1000.00
Change                           ₱80.01
════════════════════════════════════════

Receipt Footer Text Here
Return Policy: ...
Thank you for your purchase!
════════════════════════════════════════
```

**Customization:** Store name, branch, currency symbol, footer from `AppSettings.json`. Logo from `Assets/ReceiptLogo.png` (displayed above text in preview, not embedded in text).

## Common Commands

From repository root (per `README.md`):

```powershell
# Build
dotnet build GroupC.slnx

# Run
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
- **Responsive layouts:** All forms use TableLayoutPanel/FlowLayoutPanel with proper Dock/Anchor, no fixed pixel positions
- **Spacing:** Follow 8px spacing grid using UiTheme constants (SpaceXs–Space3xl)
- **Typography:** Use UiTheme font constants (FontHeading1/2/3, FontBody, etc.), no hardcoded Font() calls
- **Colors:** Use UiTheme color constants, no hardcoded hex values outside UiTheme.vb
- **Buttons:** Use UiTheme.Apply*Button methods, set MinimumSize with ButtonHeight constants, enable AutoSize
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
- **No unit tests** — manual testing required for all changes
- **No CI/CD** — build and run locally for development
- **No publish profile** — distribution requires manual setup (ClickOnce, installer, or xcopy deployment)
- **LocalDB dependency** — end users must have SQL Server LocalDB installed or connection string updated for full SQL Server
- **Single-user design** — no multi-user concurrency handling, optimistic locking, or transaction isolation beyond default
- **Receipt text column** — NVARCHAR(MAX) can grow large, no archival/compression strategy
- **CSV import** — basic parsing, no error recovery for malformed files, no progress indicator for large imports
- **Performance** — no pagination on grids, full table scans for some queries, acceptable for small datasets (<10,000 products, <50,000 sales)

**Updated per docs/02-system-requirements.md:**
- **Hardware:** Windows 10/11 64-bit; .NET 10 Desktop Runtime; LocalDB; display 1366×768+ (optimized 1920×1080); optional Windows printer; keyboard/mouse (barcode wedge via search field)
- **Assumptions:** single store/single PC; cash payment implied (tendered/change, no card gateway); English UI with configurable currency (default ₱)
- **ProductsForm** also supports hard delete with confirmation (per FR-01), in addition to deactivate/reactivate

**Updated per docs/03-interface-design-and-navigation.md:** Backup/Restore is an **inline SQL guidance dialog** in `MainMenuForm.vb`, not a separate `BackupRestoreForm.vb`.

**Updated per docs/05-features-checklist.md:** `sale_date` stored UTC; display normalized to local time. Receipt email opens mail client; long receipts may need clipboard paste.

## Future Enhancements (Not Implemented)

**Updated per docs/03-interface-design-and-navigation.md:** Backup/restore **SQL guidance dialog** exists inline in `MainMenuForm.vb`; automatic backup not implemented.

- Backup/Restore functionality (BackupRestoreForm.vb referenced but missing)
- Email receipt feature (button exists in ReceiptForm, not wired)
- Barcode scanner support for faster product lookup
- Multi-store support (branch management)
- Inventory tracking (stock levels, low stock alerts)
- Purchase orders and suppliers
- Advanced reporting (profit margins, cashier performance, hourly trends)
- Export to Excel/CSV for all grids
- Receipt email via SMTP
- Cloud sync for multi-location deployments
- Customer database for loyalty tracking
- Return/refund transactions
- Payment method tracking (cash, card, e-wallet)
- Receipt templates (customize layout beyond 40 columns)
- Dark mode theme toggle
- Localization/internationalization (currently English + currency symbol only)

## Troubleshooting

**Build fails with "process cannot access GroupC.exe":**
- Close running GroupC.exe before building
- Check Task Manager for lingering processes, kill if needed: `taskkill //F //PID <pid>`

**Database creation fails:**
- Ensure SQL Server LocalDB installed: `sqllocaldb info`
- Start LocalDB instance: `sqllocaldb start MSSQLLocalDB`
- Check connection string in App.config

**Login fails with admin password:**
- Default password is `admin123`, case-sensitive
- To change: edit `DatabaseConfig.HardcodedAdminPassword` constant

**Cashier cannot login:**
- Ensure cashier account exists (admin must create in CashierAccountsForm)
- Ensure account is active (is_active = 1)
- Check username/password (case-sensitive)

**Receipt logo not showing:**
- Ensure `Assets/ReceiptLogo.png` exists and is set to Copy to Output Directory in project properties
- Check file path in ReceiptBranding.vb

**Chart not loading:**
- Check sales table has data (run some sales through SalesForm)
- Check date range filter (default last 7 days)
- Check for exceptions in error_log table

**Grid displays no data:**
- Check filter dropdowns (may be filtered to "Inactive only" with no inactive records)
- Click Refresh button to reload from database
- Check database connection and table population

## Version History

**Current State (Latest):**
- Complete UI/UX redesign with modern design system
- All forms use responsive layouts (TableLayoutPanel/FlowLayoutPanel)
- Consistent 8px spacing grid throughout
- Material Design-inspired color palette and typography
- All hardcoded fonts/spacing replaced with UiTheme constants
- Button heights standardized (32/40/48px)
- Improved accessibility (focus indicators, contrast ratios, logical tab order)
- Card-based layouts with proper padding and borders
- Grid chrome consistent across all admin screens

**Previous Features:**
- Basic POS functionality (cart, checkout, receipt generation)
- Role-based authentication (admin/cashier)
- Product/category management with soft deletes
- Receipt history with print/PDF/text export
- Sales reports and audit logging
- Store settings persistence to JSON
- bcrypt password hashing for cashier accounts

---

**Document maintained by:** AI assistant (Claude)
**Last updated:** 2026-05-29 (merged docs/ course documentation index and submission guides)
**Repository:** GroupC_Sales-and-Receipt

## Form Documentation

See FORMS.md for a detailed breakdown of every form's structure, appearance, controls, and behavior.

## Feature Audit

| Feature | Status | Location |
|---|---|---|
| Add products | PRESENT | `ProductsForm.vb` — `btnAdd_Click` inserts into `products` |
| Compute total | PRESENT | `SalesForm.vb` — `GetGrandTotal`, `GetAmountBeforeTax`, `UpdateSummaryLabels` |
| Receipt view | PRESENT | `SalesForm.vb` — `btnFinalize_Click` opens `ReceiptForm`; `ReceiptForm.vb` — `LoadReceiptBySaleId`, preview panel |
| Quantity update | PRESENT | `ProductsForm.vb` — `numStock`, add/update stock; `SalesForm.vb` — cart qty edit + stock validation |
| Discount computation | PRESENT | `SalesForm.vb` — `GetDiscountAmount`, `GetSelectedDiscountPercent`, discount toggles; `SaveSale` |
| Tax/VAT computation | PRESENT | `SalesForm.vb` — `GetTaxAmount`, tax toggle + `numTaxPercent`; `SaveSale` |
| Receipt printing | PRESENT | `ReceiptForm.vb` — `PrintReceipt`, `ReceiptPrintHelper.vb` |
| Daily sales report | PRESENT | `ReportsForm.vb` — `RunReport` daily revenue query (`sale_day` GROUP BY) |
| Product inventory deduction | PRESENT | `DatabaseInitializer.vb` — `stock_quantity` column; `SalesForm.SaveSale` decrements stock in transaction |
| Transaction history | PRESENT | `ReceiptForm.vb` — `LoadHistoryCombo`, filters, `LoadReceiptBySaleId` |

### Partial / missing notes

None — all audited features are implemented. Restart the app once so `DatabaseInitializer.EnsureProductStockQuantity` adds `stock_quantity` to existing databases (default 100). Set stock in **Manage Products** or CSV import (optional third column).

**Demo verification:** See docs/05-features-checklist.md for 30-second demo steps per feature, CRUD rubric actions, database integration checks, and evaluator talking points.

## Project Documentation Files

- `FORMS.md` — detailed structure, appearance, controls, and behavior of every form
- `RECOMMENDATIONS.md` — possible improvements and recommendations covering features, functionality, and aesthetics
- `docs/` — course submission documentation (see Project Documentation Index above)

## UI/UX Overhaul Log

A complete design system overhaul was applied to the entire application. All forms now follow a unified design language defined in `UiTheme.vb`.

Changes summary:

- **UiTheme.vb:** Rebuilt with full color palette, typography scale, spacing constants, button variants, card helpers, badge helpers, grid chrome helpers, sidebar builder, and Phase 3 polish helpers (`ConfirmAction`, `CreateStandardToolTip`, `AssignTabOrder`, `SetSelectionButtonState`, italic empty-state labels)
- **All forms:** Shared sidebar layout shell applied; top bar with page title and subtitle; `ColBackground` content area with `PadPage` padding
- **LoginForm:** Centered card layout, dynamic role fields, error state, Enter key support, logical tab order
- **MainMenuForm:** Consistent sidebar, stat cards, grouped sales chart with filters, logout confirmation
- **SalesForm:** Product search, card grid with placeholders and name tooltips, cart empty state, discount toggle states, FINALIZE SALE guard, clear-cart confirmation, tab order and tooltips
- **ReceiptForm:** Consolidated action buttons, receipt history empty state, standardized preview, zoom tooltips, email tooltip
- **ProductsForm:** Full CRUD panel, search + filter, status badges, selection-based disabled buttons, empty grid state, confirmations for deactivate/reactivate
- **CategoriesForm:** Status badges, context-sensitive disabled buttons, grid empty state, deactivate/reactivate confirmations
- **CashierAccountsForm:** Conditional Account Actions panel, avatar chip, empty roster state, tooltips, deactivate/reactivate confirmations
- **ReportsForm:** Tabbed sales/audit layout, empty states on grids, preset filter chips, tooltips and tab order
- **SettingsForm:** Restyled modal dialog, Save button, stacked field hints, inline validation, tab order
- **Backup / Restore dialog:** Inline in `MainMenuForm` — structured steps, path customization, consistent buttons

Phase 3 global polish: standardized `Confirm action` Yes/No dialogs for logout, clear cart, finalize sale, deactivate, and reactivate; minimum sizes on workspace forms; tooltips on key controls; logical tab order on data-entry forms; `ApplyDisabledButton` enforcement for selection-dependent actions; italic empty-state labels on grids and lists.

No business logic, SQL queries, database calls, service methods, or protected files (`DatabaseConfig.vb`, `DatabaseInitializer.vb`, `AppSession.vb`, `AppSettings.vb`, audit/error/password/cashier services, `GroupC/scripts/`) were modified.

## System Requirements

Source: docs/02-system-requirements.md

### Functional requirements (FR-01–FR-17)

| ID | Requirement | Location |
|---|---|---|
| FR-01 | Product CRUD + deactivate/reactivate/delete | `ProductsForm` |
| FR-02 | Category CRUD + deactivate/reactivate | `CategoriesForm` |
| FR-03 | Cart subtotal | `SalesForm` |
| FR-04 | Discount (PWD/Senior/Member) | `SalesForm` toggles |
| FR-05 | VAT/tax on discounted subtotal | `SalesForm` tax toggle |
| FR-06 | Grand total + change | `SalesForm` tendered validation |
| FR-07 | Formatted receipt text | `ReceiptBranding.BuildReceiptText` → `sales.receipt_text` |
| FR-08 | Print receipts | `ReceiptPrintHelper` |
| FR-09 | Export PDF/text | PDFsharp + save dialog |
| FR-10 | Transaction history | `ReceiptForm` |
| FR-11 | Daily sales report | `ReportsForm` |
| FR-12 | Inventory deduction | `SalesForm.SaveSale` → `products.stock_quantity` |
| FR-13 | Role-based login | `LoginForm` + `AppSession` |
| FR-14 | Cashier account management | `CashierAccountsForm` |
| FR-15 | Audit trail | `AuditLogger` → `AuditLogs`; Reports audit tab |
| FR-16 | CSV product import | `ProductsForm` |
| FR-17 | Dashboard metrics/chart | `MainMenuForm` |

All FR items marked ✅ implemented in course docs.

### Non-functional requirements (NFR-01–NFR-09)

- **NFR-01 Usability:** `UiTheme.vb` design system; sidebar shell; 8px grid; tooltips, tab order, empty states, confirm dialogs
- **NFR-02 Performance:** LocalDB inline SQL; indexes on `sale_items.sale_id`
- **NFR-03 Security:** bcrypt/salted cashier passwords; demo admin password in config
- **NFR-04 Audit:** `AuditLogs` + `audit_products` / `audit_sales`
- **NFR-05 Reliability:** sale finalize in SQL transaction (sale + line items + stock); `error_log` + `%LocalAppData%\GroupC\logs\app.log`
- **NFR-06 Recoverability:** backup/restore SQL guidance dialog; `GroupC/scripts/`
- **NFR-07–NFR-09:** single-machine deploy; service-layer maintainability

## UI/UX Notes (Navigation & Workspace Shell)

Source: docs/03-interface-design-and-navigation.md

- **Shell:** left dark navy sidebar (220px) + light workspace + top bar (title, user/subtitle)
- **Forms:** Login, Dashboard, POS, Receipt, Products, Categories, Cashiers, Reports, Settings, Backup dialog (inline in MainMenu)
- **Navigation:** child forms modal; MainMenu hides while module open; sidebar links switch modules via `WorkspaceNavigation`; "← Back to Menu" returns to dashboard
- **Role menus:** admin sees full nav; cashier sees POS, Receipt, Log out only
- **Screenshots for submission:** capture at 1920×1080 with sidebar + top bar visible; place labeled PNGs in `docs/screenshots/` (`01-login.png` … `10-audit-log.png`)
- **Wireframes & flow:** mermaid navigation diagram in docs/03; detailed control lists in `FORMS.md`

## Database Design Notes

Source: docs/04-database-design.md

- **Connection:** `GroupC/App.config` → `GroupCSqlServer`; instance `(localdb)\MSSQLLocalDB`; database `GroupC_DB`
- **Authority:** `DatabaseInitializer.vb` at runtime; reference scripts in `GroupC/scripts/`
- **Design choice:** `sale_items` stores product name/price snapshot — no FK to `products.id` — preserves history after rename/deactivate
- **Settings (non-DB):** `%LocalAppData%\GroupC\settings.json` — store name, footer, currency, branch, policies, stock threshold
- **Oral defense:** use mapping table under Database Schema section for instructor handout name equivalents

## Presentation Guide

Source: docs/06-demo-and-presentation-guide.md

### Slide outline (8–10 slides)

Title → Problem/purpose → Target users → 10 required features → Architecture (VB.NET + LocalDB) → Database design → UI/navigation → Screenshots grid → Demo flow → Members/roles

### Live demo script (~5–10 min)

1. **Intro (0–1 min):** dashboard KPIs, chart, System Status Online
2. **Admin CRUD (1–2 min):** Products — add, update, mention deactivate vs delete
3. **Categories (optional 20 sec):** add/rename; verify dropdown on Products
4. **Sale (3–5 min):** POS — 2 items, Senior/PWD discount, VAT, tender ≥ total, finalize → receipt
5. **Receipt (5–6 min):** preview, print preview or Save PDF
6. **History (6–7 min):** Receipt Preview — search/filter past sales
7. **Reports (7–8 min):** daily revenue + top products; System Audit Logs tab
8. **Roles (8–9 min):** logout → cashier login → limited menu
9. **Close (9–10 min):** all 10 features implemented; Q&A

### Demo disaster recovery

| Problem | Fix |
|---|---|
| LocalDB offline | `sqllocaldb start MSSQLLocalDB` |
| Empty catalog | Import CSV or run seed |
| Login fails | Admin `admin123`; create cashier account |
| Build locked | Close `GroupC.exe` |
| Chart empty | Run sale in date range |

### Evaluator rubric (60 pts)

Three areas × 20 pts: **Database Integration** (Online status, sale saves, reports, `GroupC_DB`), **CRUD Operations** (live product add/update/deactivate), **System Functionality** (full sale + receipt + report + role switch). Cheat sheet: docs/05-features-checklist.md.

## Submission Checklist

Source: docs/07-project-submission-checklist.md

### Documentation bundle (print/PDF)

Combine: title page + introduction (docs/08) → system description (docs/01) → features checklist (docs/05) → requirements (docs/02) → interface design + screenshots (docs/03) → database (docs/04) → per-member reflection (docs/08)

Optional Pandoc merge from repo root:
```powershell
pandoc docs/01-system-description.md docs/02-system-requirements.md docs/03-interface-design-and-navigation.md docs/04-database-design.md docs/05-features-checklist.md -o GroupC_Documentation.pdf
```

### Source code zip

Include: `GroupC.slnx`, `GroupC/*.vb`, `GroupC/Assets/`, `GroupC/scripts/*.sql`, `docs/`, `README.md`, `FORMS.md`. Exclude: `bin/`, `obj/`, `.vs/`. Suggested name: `GroupC_SourceCode.zip`. Verify `dotnet build GroupC.slnx` on clean machine.

### Pre-demo checklist

- Rehearse 5–10 min script (docs/06)
- Verify all 10 features (docs/05)
- Create cashier test account; ≥5 products; LocalDB running

## Project Reflection

Source: docs/08-title-page-and-reflection-template.md

Submission title page placeholders: course, section, school year, date, group member names/IDs, instructor name.

Per-member reflection sections: role, what learned, challenges, contribution (specific files/features).

Optional group closing: all ten required features delivered; skills gained; future work (auto backup, barcode, cloud sync).

Member roles slide template: assign primary responsibilities (database/POS, UI/navigation, CRUD/import, reports/docs) — replace bracketed placeholders before printing.
