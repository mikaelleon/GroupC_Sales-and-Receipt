# Recommendations and Improvements

Actionable improvements for the International Bookstore POS (VB.NET WinForms, SQL Server LocalDB). All items avoid third-party UI libraries, web rendering, database engine changes, and breaking changes to existing event handlers or service method signatures.

---

## 1. Features

### Low-stock alert on dashboard
- **What:** Show count of products at or below a threshold (e.g. stock ≤ 5) on `MainMenuForm` KPI cards or a warning strip.
- **Why:** Small retail counters need restock visibility without opening Products every shift.
- **Where:** `MainMenuForm.RefreshHealthAndDashboard` — add SQL `COUNT(*)` on `products WHERE is_active = 1 AND stock_quantity <= @threshold`; new label on dashboard card row.
- **WinForms note:** Simple label/badge on existing card panel — no custom control required.

### Backup and restore screen
- **What:** Implement the missing backup/restore form referenced by the main menu (copy `.mdf` / run scripted backup, show restore steps).
- **Why:** Menu already exposes "Backup / Restore" to admins; class file is absent — feature is broken/incomplete.
- **Where:** New `BackupRestoreForm.vb`; wire existing `MainMenuForm.btnBackup_Click`.
- **WinForms note:** Use `OpenFileDialog` / `SaveFileDialog` and `Process.Start` for `sqllocaldb` or file copy instructions — no embedded SQL Server Management UI.

### Stock adjustment log
- **What:** Record manual stock changes from Products (old qty → new qty, user, timestamp).
- **Why:** Auditing inventory changes separate from sales deductions helps dispute resolution.
- **Where:** `ProductsForm.btnUpdate_Click` after successful stock update; optional small `stock_adjustments` table in `DatabaseInitializer.vb`; optional read-only grid on `ReportsForm` audit tab.
- **WinForms note:** Extra grid on existing tab is straightforward; avoid modal chains deeper than one level.

### Sale void / same-day correction
- **What:** Allow admin to void a sale (restore stock, mark sale voided, keep audit trail) from Receipt or Reports.
- **Why:** Cashiers make mistakes; deleting rows breaks history integrity.
- **Where:** `ReceiptForm` toolbar — new admin-only button; `SalesForm`-like stock restore in transaction; `sales` flag column via migration.
- **WinForms note:** Confirmation `MessageBox` + single modal detail form is enough; no wizard required.

### Default tax rate setting
- **What:** Persist default VAT % in `AppSettings` instead of resetting on each POS session.
- **Why:** Store tax rate is stable; cashiers should not re-enter 12% daily.
- **Where:** `SettingsForm.vb` + `AppSettings.vb`; `SalesForm` load reads default into `numTaxPercent`.
- **WinForms note:** One extra `NumericUpDown` on existing settings dialog.

### Keyboard wedge barcode add-to-cart
- **What:** TextBox focused on POS accepts barcode + Enter to add product by SKU/barcode field.
- **Why:** Common retail hardware acts as keyboard input — no scanner SDK needed.
- **Where:** `SalesForm` — optional `barcode` column on products + hidden/fast-focus scan field.
- **WinForms note:** `KeyPreview` and `TextBox` with `AcceptButton` pattern works; scanner sends Enter after digits.

### Receipt quick-reprint from Reports
- **What:** Double-click a daily summary row or add "View receipts for day" to open `ReceiptForm` filtered to that date.
- **Why:** Managers reconcile day-end without searching receipt list manually.
- **Where:** `ReportsForm.dgvDaily` double-click handler; pass date filter into `ReceiptForm` constructor or public property.
- **WinForms note:** WinForms `DataGridView` `CellDoubleClick` is built-in; filter is query param only.

---

## 2. Functionality

### BackupRestoreForm missing
- **Current:** `MainMenuForm` instantiates `BackupRestoreForm`; no class in repo — project cannot fully compile or button crashes.
- **Should:** Add minimal form with backup path picker and documented restore steps, or remove menu item until ready.
- **File:** `MainMenuForm.vb` / new `BackupRestoreForm.vb`
- **Effort:** Medium (form + file ops + docs)

### Products export buttons mislabeled
- **Current:** "Import to PDF" and "Import to txt file" export the current product list (`btnImportPdf_Click`, `btnImportTxt_Click`).
- **Should:** Rename to "Export to PDF" / "Export to text" to match behavior.
- **File:** `ProductsForm.vb` button `.Text` only
- **Effort:** Low

### Hardcoded administrator password
- **Current:** Admin auth compares to `DatabaseConfig.HardcodedAdminPassword` (`admin123`).
- **Should:** Store bcrypt hash in JSON settings or DB; first-run prompt to set password; keep comparison logic in `LoginForm.btnOk_Click`.
- **File:** `LoginForm.vb`, `DatabaseConfig.vb`, `AppSettings.vb`
- **Effort:** Medium (migration path for existing installs)

### Receipt history cap (500 sales)
- **Current:** `ReceiptForm` loads a fixed maximum of recent sales; older receipts harder to reach.
- **Should:** Paginate list ("Load more") or date-scoped query only (already partially filtered client-side).
- **File:** `ReceiptForm.vb` — `LoadHistoryCombo` / list population SQL
- **Effort:** Medium

### mailto: length limit for Email receipt
- **Current:** `btnEmail_Click` puts full receipt in URL query string — fails for long receipts on some clients.
- **Should:** Copy body to clipboard and open `mailto:?subject=` only, or save temp `.txt` and attach via shell (Windows only, best-effort).
- **File:** `ReceiptForm.vb` — `btnEmail_Click`
- **Effort:** Low

### Cashier cannot open Products when catalog empty
- **Current:** `btnOpenProducts` admin-only; empty catalog shows hint but cashier cannot fix.
- **Should:** Either allow read-only Products for cashiers or show clearer "contact administrator" with stock/catalog status on POS.
- **File:** `SalesForm.vb` — visibility rules for `btnOpenProducts`
- **Effort:** Low

### Audit log only on tab switch
- **Current:** Audit grid loads when audit tab selected or Load clicked — not on first open if user stays on Sales tab.
- **Should:** Acceptable as-is; optional preload on form load for admin.
- **File:** `ReportsForm.vb` — `ReportsForm_Load`
- **Effort:** Low

### Product hard delete vs sales references
- **Current:** `btnDelete` permanently removes product row; historical `sale_items` keep name snapshot but FK/product id links may break if added later.
- **Should:** Prefer deactivate-only in UI or block delete when product appears in `sale_items`.
- **File:** `ProductsForm.vb` — `btnDelete_Click`
- **Effort:** Low

### Settings not applied to open POS until restart
- **Current:** Currency changes apply on next `AppSettings` reload; open `SalesForm` may show old symbol until reopened.
- **Should:** Reload `AppSettings` in `SalesForm_Load` or subscribe to settings-changed pattern.
- **File:** `SalesForm.vb`, `MainMenuForm.btnSettings_Click`
- **Effort:** Low

---

## 3. Aesthetics

### Settings dialog vs maximized workspace inconsistency
- **Issue:** All major forms maximize; `SettingsForm` is small fixed dialog — feels disconnected.
- **Fix:** Use same maximized shell with centered card (pattern from `LoginForm`) or `FormBorderStyle.Sizable` with min size 560×440.
- **Affected:** `SettingsForm.vb`
- **WinForms:** Yes — layout copy from existing `CreateCardPanel` pattern.

### Login card flat edges
- **Issue:** Login card is plain white rectangle without border radius used elsewhere on cards.
- **Fix:** Wrap card in `UiTheme.CreateCardPanel` or draw rounded border in Paint (same as dashboard cards).
- **Affected:** `LoginForm.vb`
- **WinForms:** Yes — `CreateCardPanel` already exists; no third-party graphics.

### CashierAccountsForm color drift
- **Issue:** Uses hardcoded `#1B7EC2`, `#5A6A7A`, custom grays instead of `UiTheme.SecondaryAccent` / `TextSecondary`.
- **Fix:** Replace literal colors with UiTheme constants for consistency with Products/Reports.
- **Affected:** `CashierAccountsForm.vb`
- **WinForms:** Yes — property changes only.

### ReceiptForm fixed left panel width
- **Issue:** Left panel locked at 360px; on narrow screens history controls truncate before right panel shrinks.
- **Fix:** Replace fixed width with `SplitContainer` (min 280px) so user can drag divider.
- **Affected:** `ReceiptForm.vb` — `pnlLeft` / `pnlRight` docking
- **WinForms:** Yes — native `SplitContainer` control.

### Product card grid spacing on small displays
- **Issue:** Product cards fixed 156px width; many cards wrap leaving uneven bottom whitespace in left POS column.
- **Fix:** On `ProductCardScrollPanel_Resize`, compute columns count and center `FlowLayoutPanel` or use `TableLayoutPanel` with percentage columns.
- **Affected:** `SalesForm.vb`
- **WinForms:** Yes — layout math in existing resize handler.

### Reports filter bar button alignment
- **Issue:** Preset chip buttons and Run/Export wrap unevenly when window narrowed — summary strip jumps height.
- **Fix:** Put presets on second row of `TableLayoutPanel` with fixed row heights (already partially structured).
- **Affected:** `ReportsForm.vb` — `BuildSalesFilterPanel`
- **WinForms:** Yes — `TableLayoutPanel` row styles.

### DataGridView focus rectangle on read-only grids
- **Issue:** Admin grids (Products, Categories, Cashiers) show heavy focus cues on row select — slightly noisy for read-only browse.
- **Fix:** Set `DefaultCellStyle.SelectionBackColor/ForeColor` via `UiTheme.ApplyReadOnlyGridTheme` where grid is read-only.
- **Affected:** `UiTheme.vb`, `ProductsForm.vb`, `CategoriesForm.vb`, `CashierAccountsForm.vb`
- **WinForms:** Yes — built-in DGV style properties.

### Status feedback inconsistency
- **Issue:** Some forms use bottom `StatusStrip`; `CashierAccountsForm` and `ReceiptForm` use custom bottom label bar — different timing and placement.
- **Fix:** Standardize on `FormStatusHelper` + `StatusStrip` via `UiTheme.ApplyStatusStripTheme` on Cashier/Receipt for one pattern.
- **Affected:** `CashierAccountsForm.vb`, `ReceiptForm.vb`
- **WinForms:** Yes — swap panel for existing status strip pattern from ProductsForm.
