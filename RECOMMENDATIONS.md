# Functional Recommendations — International Bookstore POS

Prioritized improvements for **Group C – Sales & Receipt System** (VB.NET WinForms, SQL Server LocalDB). Sorted for rubric targets: **System Functionality**, **CRUD**, **Database Integration**, **Code Organization**, **Documentation**, and **Presentation & Demonstration**.

**Audit date:** May 2026 (post UI overhaul branch)

---

## Required features — compliance

### Low-stock alert on dashboard (Implemented)
- **What:** Show count of products at or below a threshold (e.g. stock ≤ 5) on `MainMenuForm` KPI cards or a warning strip.
- **Why:** Small retail counters need restock visibility without opening Products every shift.
- **Where:** `MainMenuForm.RefreshHealthAndDashboard` — add SQL `COUNT(*)` on `products WHERE is_active = 1 AND stock_quantity <= @threshold`; new label on dashboard card row.
- **WinForms note:** Simple label/badge on existing card panel — no custom control required.

### Backup and restore screen (Implemented)
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

## Rubric gap summary

### BackupRestoreForm missing (Implemented)
- **Current:** `MainMenuForm` instantiates `BackupRestoreForm`; no class in repo — project cannot fully compile or button crashes.
- **Should:** Add minimal form with backup path picker and documented restore steps, or remove menu item until ready.
- **File:** `MainMenuForm.vb` / new `BackupRestoreForm.vb`
- **Effort:** Medium (form + file ops + docs)

### Products export buttons mislabeled (Implemented)
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

## Priority 0 — Fix before demo / grading (critical)

### 1. Hardcoded admin password (`admin123`)
- **Issue:** Admin auth uses `DatabaseConfig.HardcodedAdminPassword`; not production-safe; easy deduction in demo Q&A.
- **Fix:** Bcrypt hash in settings JSON or DB; first-run change prompt; document default in README only for dev.
- **Files:** `LoginForm.vb`, `DatabaseConfig.vb`, `AppSettings.vb`
- **Rubric:** System Functionality, Presentation

### 2. Dashboard / reports date timezone consistency
- **Issue:** `sale_date` stored as UTC (`SYSUTCDATETIME`); receipt preview was fixed to normalize; dashboard KPI tooltip and history filters may still show raw UTC-as-local in some paths.
- **Fix:** Use `ReceiptBranding.NormalizeStoredSaleDate()` (or shared helper) everywhere `sale_date` is read for display.
- **Files:** `MainMenuForm.vb`, `ReportsForm.vb`, `ReceiptForm.vb` (verify all paths)
- **Rubric:** System Functionality, Database Integration

### 3. Backup / restore is guidance-only
- **Issue:** Menu opens dialog with SQL copy-to-clipboard—not one-click backup. Evaluators may expect working backup.
- **Fix:** Add “Run backup” that executes `BACKUP DATABASE` via `sqlcmd` or documents bundled `.bat`; show success/failure in UI.
- **Files:** `MainMenuForm.vb` (`ShowBackupRestoreDialog`)
- **Rubric:** Database Integration, System Functionality

### 4. Product hard delete without guard
- **Issue:** `ProductsForm.btnDelete_Click` permanently removes rows; breaks referential integrity if extended; risky in live demo.
- **Fix:** Block delete when product appears in `sale_items`; show message to deactivate instead.
- **Files:** `ProductsForm.vb`
- **Rubric:** CRUD Operations

---

## Priority 1 — High impact (strong “Excellent” signal)

### 5. Default VAT/tax rate not persisted in Settings UI
- **Issue:** Cashiers re-enter tax % each session; `AppSettings` has no exposed tax default (POS `numTaxPercent` starts at 0, toggle off).
- **Fix:** Add “Default tax rate (%)” to `SettingsForm`; load into `SalesForm` on open.
- **Files:** `SettingsForm.vb`, `AppSettings.vb`, `SalesForm.vb`
- **Rubric:** System Functionality, CRUD (settings)

### 6. Expose remaining settings in UI (or document JSON path clearly)
- **Issue:** Receipt uses `StoreBranch`, `ReturnPolicyText`, `TermsText`, `StockThreshold` from JSON but Settings UI only edits name, footer, currency.
- **Fix:** Add fields for branch, return policy, low-stock threshold—or single “Advanced settings” section with JSON edit + validation.
- **Files:** `SettingsForm.vb`, `AppSettings.vb`
- **Rubric:** System Functionality, Documentation

### 7. Receipt email via long `mailto:` body
- **Issue:** Full receipt in URL query fails for long sales on many clients.
- **Fix:** Copy body to clipboard; open `mailto:?subject=` only; status message tells user to paste.
- **Files:** `ReceiptForm.vb` — `btnEmail_Click`
- **Rubric:** System Functionality

### 8. Receipt history cap (500 rows)
- **Issue:** Older receipts unreachable without date filter narrowing.
- **Fix:** Server-side paging (“Load more”) or raise cap with date-scoped default query.
- **Files:** `ReceiptForm.vb` — `LoadHistoryCombo`
- **Rubric:** Transaction history (required feature depth)

### 9. Sale void / same-day correction (admin)
- **Issue:** No way to undo mistaken sale while keeping audit trail; stock not restored except manual product edit.
- **Fix:** Admin “Void sale” on `ReceiptForm`; `sales.is_voided` flag + stock restore in transaction + audit log.
- **Files:** `ReceiptForm.vb`, `DatabaseInitializer.vb` (migration), `SalesForm.vb` stock logic reuse
- **Rubric:** CRUD, System Functionality

### 10. Demo data seed script run before presentation
- **Issue:** Empty catalog = weak demo; large seed scripts exist but are manual.
- **Fix:** Document one command in README; optional “Load demo catalog” admin button calling existing seed path.
- **Files:** `README.md`, `DatabaseInitializer.vb` or scripts `03`/`04`
- **Rubric:** Presentation & Demonstration

---

## Priority 2 — Medium (nice-to-have, still on-rubric)

### 11. Stock adjustment audit log
- **What:** Log manual stock changes (old → new, user, time) separate from sales deduction.
- **Where:** `ProductsForm` update path; optional `stock_adjustments` table; Reports audit tab.
- **Rubric:** Database Integration, CRUD

### 12. Low-stock alert → drill-down
- **What:** Click dashboard alert opens `ProductsForm` filtered to low-stock rows.
- **Where:** `MainMenuForm` — `pnlLowStockAlert` click handler.
- **Note:** Alert count already implemented (`StockThreshold` in settings JSON).

### 13. Reports → receipt drill-down
- **What:** Double-click daily revenue row opens `ReceiptForm` filtered to that date.
- **Where:** `ReportsForm.dgvDaily` — `CellDoubleClick`.

### 14. Barcode / keyboard wedge add-to-cart
- **What:** Scan field + Enter adds product by SKU/barcode column (optional on `products`).
- **Where:** `SalesForm` — hidden scan `TextBox`, `KeyPreview`.

### 15. Settings reload on open POS
- **What:** Currency/footer changes apply without restarting app.
- **Where:** `SalesForm_Load` → `AppSettings.Reload()`; refresh labels.

### 16. Cashier catalog empty state
- **What:** Clearer “contact administrator” when no products; `btnOpenProducts` remains admin-only by design.
- **Where:** `SalesForm` empty catalog panel copy.

### 17. Automated smoke checklist (manual QA doc)
- **What:** One-page test script: login both roles, one sale, receipt print, report run, product CRUD.
- **Where:** New `DEMO.md` or README section.
- **Rubric:** Documentation, Presentation

---

## Priority 3 — Low / future (beyond rubric minimum)

| Item | Notes |
|------|--------|
| Unit test project | No tests in solution; acceptable for coursework but not “Excellent” code assurance |
| Multi-payment methods | Cash only implied; track card/e-wallet on `sales` |
| Excel export for all grids | Reports has CSV; extend to Products/Receipt batch |
| Customer loyalty database | Member discount exists; no customer records |
| Dark mode | Theme tokens exist; no toggle |
| ClickOnce / installer | Manual xcopy deploy only |
| Email SMTP | Placeholder button exists; SMTP not wired |
| Inventory purchase orders | Out of scope for Group C |

---

## Known issues already addressed (no action)

| Item | Resolution |
|------|------------|
| Sidebar navigation double-click | `WorkspaceNavigation.vb` + `BuildWorkspaceSidebarShell` |
| SplitContainer crash on load | `UiTheme.CreateVerticalSplit` deferred min sizes |
| Receipt timestamp mismatch | UTC normalize + align date line on load |
| Products export mislabel | Renamed to “Export to PDF/text” |
| Low stock alert layout stretch | `TableLayoutPanel` auto-size rows |
| Top bar subtitle clipped | `CreateTopBar` auto-height |
| `BackupRestoreForm.vb` missing | Inline dialog in `MainMenuForm` |
| `btnDetails` placeholder | Wired to line-item grid dialog |

---

## Suggested 10-minute demo script (Presentation)

1. **Login** as Admin — show role menu.
2. **Dashboard** — KPI cards, chart filter, low-stock alert (if seeded).
3. **Products** — add/edit product, show stock; optional CSV import mention.
4. **Categories** — quick add/rename.
5. **Point of Sale** — add items, PWD or Senior discount, tax on, tender, finalize.
6. **Receipt Preview** — print preview, PDF save, history search.
7. **Reports** — daily revenue + top products; audit log tab.
8. **Settings** — store name on receipt.
9. **Cashiers** — register cashier; logout; login as Cashier — limited menu.
10. **Backup dialog** — show SQL commands (note live backup as improvement).

---

## File reference map

| Area | Key files |
|------|-----------|
| POS / totals | `SalesForm.vb`, `CartLineItem.vb` |
| Receipts | `ReceiptForm.vb`, `ReceiptBranding.vb`, `PdfReceiptExporter.vb` |
| Catalog CRUD | `ProductsForm.vb`, `CategoriesForm.vb` |
| Users | `CashierAccountsForm.vb`, `CashierAccountService.vb`, `LoginForm.vb` |
| Reports / audit | `ReportsForm.vb`, `AuditLogger.vb` |
| Database | `DatabaseInitializer.vb`, `DatabaseConfig.vb` |
| Settings | `AppSettings.vb`, `SettingsForm.vb` |
| Shell / nav | `MainMenuForm.vb`, `WorkspaceNavigation.vb`, `UiTheme.vb` |
