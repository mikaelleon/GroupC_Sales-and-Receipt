# Functional Recommendations — International Bookstore POS

Prioritized improvements for **Group C – Sales & Receipt System** (VB.NET WinForms, SQL Server LocalDB). Sorted for rubric targets: **System Functionality**, **CRUD**, **Database Integration**, **Code Organization**, **Documentation**, and **Presentation & Demonstration**.

**Audit date:** May 2026 (post UI overhaul branch)

---

## Required features — compliance

| Required feature | Status | Primary location |
|------------------|--------|------------------|
| Add products | ✅ Present | `ProductsForm.vb` — add/update, CSV import |
| Compute total | ✅ Present | `SalesForm.vb` — cart + checkout summary |
| Receipt view | ✅ Present | `ReceiptForm.vb` — preview, filters, zoom |
| Quantity update | ✅ Present | `ProductsForm.vb` stock; `SalesForm.vb` cart qty |
| Discount computation | ✅ Present | PWD / Senior / Member toggles |
| Tax/VAT computation | ✅ Present | Tax toggle + `numTaxPercent` |
| Receipt printing | ✅ Present | `ReceiptPrintHelper.vb`, print preview |
| Daily sales report | ✅ Present | `ReportsForm.vb` — daily revenue grid |
| Product inventory deduction | ✅ Present | `SalesForm.SaveSale` transaction |
| Transaction history | ✅ Present | `ReceiptForm.vb` — history list + search |

**Verdict:** All ten core features are implemented. Remaining work is polish, security, and demo readiness—not missing core scope.

---

## Rubric gap summary

| Criterion | Current | To reach “Excellent” |
|-----------|---------|----------------------|
| System Functionality | Core flows work | Fix edge cases below; demo script with real data |
| CRUD Operations | Products, categories, cashiers, sales | Block unsafe hard-delete; void/refund path |
| Database Integration | LocalDB auto-init, transactions on sale | Real backup action; UTC date consistency everywhere |
| Code Organization | Shared `UiTheme`, services, navigation module | Extract repeated SQL; add smoke-test checklist |
| Documentation | `README.md`, `CLAUDE.md`, `FORMS.md` | Add demo script + evaluator quick-start one-pager |
| Presentation & Demo | Feature-rich | Rehearse 10-minute path; seed catalog before demo |

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
