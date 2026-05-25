# Form Documentation

Detailed structure, controls, and behavior for every major form in the International Bookstore POS application.

---

## LoginForm

**Purpose:** Authenticates administrators and cashiers before the application dashboard is shown.

**Window behavior:** Resizable and maximizable via `UiTheme.ApplyMaximizedWorkspaceDefaults(Me, 500, 450)`; minimizable (`MinimizeBox = True`, `MaximizeBox = True`). Starts maximized with minimum size 500×450. No fixed dialog border.

**Layout structure:** Full-window `TableLayoutPanel` (3×3) centers a white login card. Card is a vertical `FlowLayoutPanel` with logo/title block, role radios, username block (cashier only), password block, hint, and button row.

**Controls:**
- `picLoginLogo` — PictureBox (ReceiptLogo.png via `LogoBranding`, hidden if no image)
- `lblTitle` — application name (Heading1, navy), shown when no logo
- `lblSubtitle` — "Sign in to continue"
- `lblRole` — "Select Role:"
- `radAdmin`, `radCashier` — RadioButton role selection (Administrator default)
- `lblUsername`, `txtUsername` — cashier username (hidden in admin mode)
- `lblSecretCaption`, `txtSecret` — password field inside bordered shell
- `pnlToggleSecret` — custom `PasswordTogglePanel` (eye icon show/hide)
- `lblHint` — role-specific sign-in instructions
- `btnOk` — "Sign In" (primary)
- `btnCancel` — "Cancel" (secondary)

**Visual appearance:** `UiTheme.FormBackground` page; centered `CardSurface` card (400px content width, 32px padding). Primary accent navy titles; secondary gray hints. Password/username fields use 1px `InputBorder` shell, 44px height. Primary/secondary button theming from `UiTheme`.

**User interactions:** Select Administrator or Cashier; enter password (and username for cashier); toggle password visibility; Sign In validates and sets `AppSession`; Cancel closes with `DialogResult.Cancel`. Enter submits (`AcceptButton = btnOk`); Escape cancels.

**Data displayed:** Branding logo or app title; no database list data on this form.

**Navigation:** Shown modally from `MainMenuForm` on startup and on Logout. On success closes with `DialogResult.OK` → main menu continues. On cancel at startup → app exits.

**Role access:** Both (entry point for Admin and Cashier).

**Known issues:** Administrator password is hardcoded (`DatabaseConfig.HardcodedAdminPassword`, default `admin123`). No "forgot password" or lockout UI.

---

## MainMenuForm

**Purpose:** Post-login dashboard and navigation hub for all application modules.

**Window behavior:** Resizable, maximized workspace (`ApplyMaximizedWorkspaceDefaults(Me, 960, 600)`); minimizable/maximizable per default Form settings. Hidden (`Opacity = 0`, `ShowInTaskbar = False`) until login succeeds.

**Layout structure:** Root 2-column `TableLayoutPanel`: fixed 260px left sidebar + fluid right dashboard. Right side: header, 2×2 KPI cards, chart filter card, daily sales chart card. Bottom `StatusStrip` spans full width.

**Controls:**
- **Sidebar:** `btnProducts`, `btnCategories`, `btnCashierAccounts`, `btnSales`, `btnReceipt`, `btnReports`, `btnSettings`, `btnBackup`, `btnLogout`
- **Dashboard header:** app title label (`FontHeading2`), `lblDbHealth` system status
- **KPI cards:** `lblDashProducts`, `lblDashSalesToday`, `lblDashSevenDay`, `lblDashLastSale`
- **Chart filters:** `dtpChartFrom`, `dtpChartTo`, `cmbChartPreset` (Last 7/14/30 days, This month, Custom), `cmbChartSort`, `btnApplyChart`, `lblChartFilterError`
- **Chart:** `pnlSalesChart` — custom GDI+ bar chart (Paint handler)
- **Status:** `statusStrip` / `statusLabel`; `tmrRefresh` (60s auto-refresh)

**Visual appearance:** Sidebar `CardSurface` white; dashboard `FormBackground` gray. Nav buttons: primary navy for Products/Categories/Cashiers/Sales; secondary accent for Receipt/Reports; secondary for Settings/Backup; danger red for Logout. KPI values use `FontHeading2` navy. Chart drawn on white card with navy bars, blue highlight for today.

**User interactions:** Click nav buttons to open child forms (modal, main menu hidden). Adjust chart date range via pickers, presets, or Apply; change sort order. Dashboard refreshes on load, after child forms close, every 60 seconds, and on chart Apply. Logout re-shows `LoginForm`.

**Data displayed:** Active product count, today's sales total, period sales total (chart range), last sale ID (tooltip shows datetime and amount), daily sales bar chart — all from `products` and `sales` tables via SQL. Currency symbol from `AppSettings`.

**Navigation:** Startup form (`Application.myapp`). Opens: `LoginForm`, `ProductsForm`, `CategoriesForm`, `CashierAccountsForm`, `SalesForm`, `ReceiptForm`, `ReportsForm`, `SettingsForm`, `BackupRestoreForm` (referenced). Child forms return here on close.

**Role access:** Both after login; admin-only buttons hidden for cashiers via `ApplyRoleBasedNavigation()` (Products, Categories, Cashiers, Reports, Settings, Backup).

**Known issues:** `BackupRestoreForm` is referenced in `btnBackup_Click` but the class file is **missing from the repository** — clicking Backup / Restore will fail at compile time or runtime until implemented. Chart range capped at 90 days.

---

## SalesForm

**Purpose:** Point-of-sale screen for building a cart, applying discounts/tax, tendering payment, and finalizing sales.

**Window behavior:** Resizable, maximized workspace (`ApplyMaximizedWorkspaceDefaults(Me)`); default minimizable/maximizable.

**Layout structure:** 58% / 42% two-column root. **Left:** category filter, scrollable product card catalog, selected product + stock + quantity + Add to cart, utility buttons. **Right:** Shopping Cart header with Remove/Clear actions, cart `DataGridView` (~62% height), checkout card (~38%) split into discount/tax column, summary/tender column, and finalize column.

**Controls:**
- `cmbSalesCategory` — category filter dropdown
- `productCardScrollPanel` / `productCardHost` — clickable product cards (image, name, price)
- `lblNoProductCards`, `lblEmptyHint` — empty catalog messages
- `lblSelectedProduct`, `lblStockOnHand`, `numQuantity`, `btnAdd`
- `btnOpenProducts`, dynamic `btnBack` ("← Back to Menu")
- `dgvProducts` — cart grid: #, Product, Price, Qty (editable), Subtotal, Remove button column
- `btnRemove`, `btnClear`
- Discount: `lblCustomerDiscount`, `btnDiscPwd`, `btnDiscSenior`, `btnDiscMembership`
- Tax: `btnTaxToggle`, `numTaxPercent`
- Checkout summary: `lblSubtotalValue`, `lblDiscountValue`, `lblTaxValue`, `txtAmountTendered`, `lblChangeValue`, `lblTotal`, `btnFinalize`
- `lblSalesInputError`, `statusStrip` / `statusLabel`, `statusClearTimer`

**Visual appearance:** `FormBackground` with white left sidebar and padded right panel. UiTheme primary/success/warning/secondary buttons. Cart grid via `ApplyDataGridViewChrome`. Checkout card with vertical dividers; Amount Due in `FontHeading1` navy; Finalize in green success style. Product cards ~156×218px with optional product images.

**User interactions:** Filter by category; click product card to select; set quantity (capped to available stock); Add to cart (merges duplicate lines); edit cart quantity inline; Remove selected row or per-row Remove button; Clear cart (confirmation); toggle one discount (PWD/Senior 20%, Member 10%); toggle VAT and set tax %; enter amount tendered; Finalize validates stock and tender, saves sale, opens `ReceiptForm`. Open Products (admin only). Back closes form.

**Data displayed:** Active in-stock products from `products` (with `stock_quantity`, optional `image_path`); categories from `categories`; cart is in-memory `CartLineItem` list; totals computed live with currency from `AppSettings`.

**Navigation:** Opened from `MainMenuForm` → Point of Sale. Opens `ProductsForm` (admin), `ReceiptForm` after finalize. Back → `MainMenuForm`.

**Role access:** Both (cashiers and admins). `btnOpenProducts` visible to admin only (or when catalog empty for admin).

**Known issues:** Out-of-stock products hidden from catalog — cannot sell without adjusting stock in Products first. `Open Products` requires admin session even if cashier needs stock help.

---

## ReceiptForm

**Purpose:** Browse, search, preview, print, and export past sale receipts.

**Window behavior:** Resizable, maximized workspace (`ApplyMaximizedWorkspaceDefaults(Me)`). Designer default 560×650 overridden at runtime.

**Layout structure:** Docked layout: fixed 360px left panel (filters + history list + sale chip + Back), fill right panel (preview title, zoom toolbar, action toolbar, receipt preview card), 60px bottom status bar.

**Controls:**
- **Left:** `txtHistorySearch`, `cmbDateFilter`, `pnlCustomRange` (`dtpFilterFrom`, `dtpFilterTo`), `cmbSort`, `lstHistory`, `btnLoadList`, `btnExportBatch`, `pnlSaleChip` (`lblChipSaleId`, `lblChipDate`, `lblChipTotal`, `lblChipCashier`), `btnBack`
- **Right:** preview title/hint, `btnZoomOut`, `lblZoomPct`, `btnZoomIn`, `chkSimulatePage`
- **Toolbar:** `btnPrint`, `btnPrintPreview`, `btnReprint`, `btnSavePdf`, `btnSave`, `btnCopy`, `btnEmail`, `btnDetails`, `btnDuplicate`
- **Preview:** `picReceiptLogo`, `rtbReceipt` (Courier New 10pt), `pnlEmptyPreview`, hidden `dgvLines` for line-item details
- `lblStatus`, hidden `cmbHistory`, `printDocument`, `ctxReceipt` context menu

**Visual appearance:** Left panel white with right border (`BorderLight`); right area `SurfaceGray` (#F5F7FA). Sale chip light blue (`BrandBlueLight`). Receipt paper white panel with border; monospace receipt text. Toolbar flat white buttons with emoji labels.

**User interactions:** Search/filter/sort history list; select sale to load receipt; zoom 75%–150%; toggle page margin simulation; print, print preview, reprint, save PDF/text, copy to clipboard, email via `mailto:`, view line-item details dialog, duplicate receipt workflow; batch export; Refresh reloads history; Back closes.

**Data displayed:** Up to 500 recent sales from `sales` (ID, date, total, cashier); receipt text from `sales.receipt_text` or rebuilt via `ReceiptBranding`; line items in hidden grid when snapshot available. Logo from branding assets.

**Navigation:** Opened from `MainMenuForm` → Receipt Preview; also opened from `SalesForm` after finalize (with snapshot). Back → `MainMenuForm`.

**Role access:** Both.

**Known issues:** History limited to 500 sales — performance note in code. `mailto:` body may exceed URL length for long receipts on some systems. Email opens default client only (no SMTP integration).

---

## ProductsForm

**Purpose:** Administrator CRUD for the product catalog, stock quantities, images, and CSV import.

**Window behavior:** Resizable, maximized workspace (`ApplyMaximizedWorkspaceDefaults(Me, 960, 600)`).

**Layout structure:** 420px left sidebar (scrollable product editor) + right "Inventory Overview" (toolbar + grid card). Status strip at bottom.

**Controls:**
- **Editor:** `txtProductName`, `numPrice`, `numStock`, `cmbCategory`, `picProductImage`, `btnChooseImage`, `btnRemoveImage`
- **CRUD:** `btnAdd`, `btnUpdate`, `btnDelete`, `btnDeactivate`, `btnReactivate`
- **Utilities:** `btnManageCategories`, `btnImportCsv`, `btnImportPdf`, `btnImportTxt`, `btnPrintCopy`, `btnRefresh`, `btnBack`
- **Grid toolbar:** `txtSearch`, `cmbGridCategoryFilter`, `cmbFilter` (Active / All / Inactive), `btnRefresh`
- `dgvProducts` — Active, Product, Price, Stock, Category (hidden: id, category_id, image_path)
- `lblProductsInputError`, `lblGridMessage`, `statusStrip`

**Visual appearance:** Standard UiTheme form background; white sidebar card; grid in bordered card. Primary actions navy; delete danger red; deactivate warning orange; reactivate green. Product image placeholder `SurfaceVariant`.

**User interactions:** Add/update products with name, price, stock, category, optional image; hard delete or soft deactivate/reactivate; search and filter grid; select row to load editor; CSV import (name, price, optional category, optional stock); export product list to PDF or TXT; print copy; open Categories; Back closes.

**Data displayed:** Products from `products` joined to `categories`; filters applied client-side on `DataView`. Categories loaded into dropdowns from DB.

**Navigation:** Opened from `MainMenuForm` (admin). Opens `CategoriesForm` modally. Back → `MainMenuForm`. Also reachable from `SalesForm` → Open Products (admin).

**Role access:** Admin only (`AppSession.RequireAdmin` on menu; form itself assumes admin use).

**Known issues:** `btnImportPdf` / `btnImportTxt` labels say "Import" but behavior is **export** product list to PDF/TXT. Hard delete (`btnDelete`) exists alongside deactivate — may conflict with sales history referencing product names. Sidebar title says "Product Details" while menu says "Manage Products".

---

## CategoriesForm

**Purpose:** Administrator CRUD for product categories used when assigning products.

**Window behavior:** Resizable, maximized workspace (`ApplyMaximizedWorkspaceDefaults(Me, 880, 560)`).

**Layout structure:** 360px left sidebar (title, hint, name input, action buttons, Back) + right grid area (filter toolbar + card-wrapped grid). Status strip at bottom.

**Controls:**
- `txtCategoryName` — category name input (max 100 chars)
- `btnAdd`, `btnUpdate`, `btnDeactivate`, `btnReactivate`, `btnRefresh`, `btnBack`
- `cmbFilter` — Active categories / All / Inactive only
- `dgvCategories` — Category, Active, Active products (count), hidden category_id
- `lblInputError`, `statusStrip`

**Visual appearance:** UiTheme form background; white sidebar; `ApplyFilledTextInputVisual` on name field; standard primary/warning/success buttons; read-only grid with `ApplyDataGridViewChrome`.

**User interactions:** Add category; select row to edit name; update, deactivate, reactivate; filter grid; refresh; Back closes. Grid selection loads name into text box and toggles reactivate/deactivate enabled state.

**Data displayed:** Categories from `categories` with subquery count of active products per category.

**Navigation:** Opened from `MainMenuForm` (admin) or `ProductsForm` → Manage categories. Back → calling form / menu.

**Role access:** Admin only.

**Known issues:** Deactivating a category does not bulk-reassign products — hint directs user to Products screen. No inline merge of duplicate categories (separate SQL script exists in `scripts/`).

---

## CashierAccountsForm

**Purpose:** Administrator registration and maintenance of database-backed cashier login accounts.

**Window behavior:** Resizable, maximized workspace (`ApplyMaximizedWorkspaceDefaults(Me, 900, 600)`). Custom `SurfaceGray` background on load.

**Layout structure:** Fixed 340px left panel (registration fields, account actions, Back) + right panel (toolbar + grid + empty state). Bottom status bar (56px).

**Controls:**
- **Registration:** `txtUsername`, `txtDisplayName`, `txtPassword`, `btnShowPass`, `txtConfirmPassword`, `lblPassHint`, `lblPassMatch`, `btnRegister`
- **Selection:** `pnlSelectedChip` (avatar, `lblChipUsername`, `btnClearSelection`), `lblNoSelection`
- **Account actions:** `btnUpdateDisplay`, `btnResetPassword`, `btnDeactivate`, `btnReactivate` (shown when row selected)
- **Grid area:** `cmbFilter` (Active / Inactive / All), `btnRefresh`, `dgvCashiers` (#, Status, Username, Display Name, Last sign-in, Registered)
- `pnlEmptyState` — empty grid illustration
- `lblInputError`, `lblStatus`, `btnBack`

**Visual appearance:** Mix of UiTheme and custom palette (`SurfaceGray`, `BrandBlueLight` chip, `BorderLight` dividers). White left/right toolbar panels. Grid white with horizontal rules. Status column color-coded via `CellFormatting`. Password fields in 42px bordered shells.

**User interactions:** Register new cashier (username, optional display name, password + confirm); select grid row to chip-select account; update display name; reset password; deactivate/reactivate; show/hide password; filter and refresh list; clear selection returns to new-account mode; Back closes.

**Data displayed:** Rows from `cashier_accounts` (id, username, display_name, is_active, last_login_at, created_at). Password hashes never shown.

**Navigation:** Opened from `MainMenuForm` → Manage Cashiers (admin). Back → `MainMenuForm`.

**Role access:** Admin only.

**Known issues:** Form title hardcoded "International Bookstore — Manage Cashiers" instead of `AppBranding.WindowTitle`. Some colors use hex literals rather than UiTheme constants. Last sign-in depends on successful cashier login updating `last_login_at`.

---

## ReportsForm

**Purpose:** Administrator sales summaries by date range and system audit log viewer.

**Window behavior:** Resizable, maximized workspace; `StartPosition = CenterParent` when shown modally from menu.

**Layout structure:** Top header bar (Back + title) over full-width `TabControl`. Tab 1 "Sales & Revenue": filter card, summary strip, side-by-side Daily Revenue and Top Products grids. Tab 2 "System Audit Logs" (admin only): date filter + audit grid. Status strip at bottom.

**Controls:**
- `btnBack` (inline in header), `tabReports`
- **Sales tab:** `dtpFrom`, `dtpTo`, preset chip buttons (7 / 30 / 90 days), `btnRun`, `btnExport`, `lblSummary`, `dgvDaily`, `dgvTop`, `lblDailyEmpty`, `lblTopEmpty`
- **Audit tab:** `dtpAuditFrom`, `dtpAuditTo`, `btnAuditRefresh`, `dgvAudit`, `lblAuditEmpty`
- `statusStrip` / `statusLabel`

**Visual appearance:** UiTheme form background; white header card; filter bar in card with flat chip preset buttons; summary on `GridAltRow` background; grids in titled card panels; primary Run/Load buttons.

**User interactions:** Set date range or click preset chips (auto-runs report); Run report; Export CSV (daily + top product sheets); switch to Audit tab loads log; Load log for audit date range; Back closes.

**Data displayed:** **Daily grid:** sale_day, sale_count, revenue from `sales`. **Top products:** top 20 by quantity from `sale_items`/`sales`. **Summary label:** range, total revenue, day count. **Audit grid:** LogID, Action, Detail, PerformedBy, LoggedAt from `AuditLogs`.

**Navigation:** Opened from `MainMenuForm` → Reports (admin). Back → `MainMenuForm`.

**Role access:** Admin only (menu hidden for cashiers; audit tab not created unless `AppSession.IsAdmin()`).

**Known issues:** CSV export covers sales tab data only, not audit log. Top products limited to TOP 20. Audit tab omitted entirely if opened before admin check in edge cases — normal path is admin-only menu.

---

## SettingsForm

**Purpose:** Edits store branding settings persisted to JSON in the user profile.

**Window behavior:** **Fixed dialog** (`FormBorderStyle.FixedDialog`); not resizable (`MinimizeBox = False`, `MaximizeBox = False`); centered on parent; size ~560×440; minimum 520×400.

**Layout structure:** Single padded root `TableLayoutPanel` containing one centered card with 2-column field layout and right-aligned OK/Cancel row.

**Controls:**
- `txtStoreName` — store name (max 120)
- `txtFooter` — receipt footer text (max 500)
- `txtCurrency` — currency symbol (1–6 chars)
- `lblSettingsError` — inline validation message
- `btnOk`, `btnCancel`

**Visual appearance:** `UiTheme.FormBackground`; white card via `CreateCardPanel`; secondary gray field labels; primary OK and secondary Cancel buttons. Standard window chrome font.

**User interactions:** Edit three settings fields; OK validates, saves via `AppSettings.Save`, logs audit event, closes with `DialogResult.OK`; Cancel discards. Enter/Escape wired to OK/Cancel.

**Data displayed:** Current values loaded from `AppSettings.Current` (JSON file under LocalApplicationData).

**Navigation:** Opened modally from `MainMenuForm` → Settings (admin). Does not hide main menu. On OK, caller reloads settings and refreshes dashboard.

**Role access:** Admin only (enforced in `MainMenuForm` via `RequireAdmin`).

**Known issues:** UI pattern differs from other forms (small fixed dialog vs maximized workspace). No live receipt preview of footer changes. No settings for tax default % or receipt logo path (those use other mechanisms/assets).
