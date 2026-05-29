# Part I-B — System Requirements

## Functional requirements

| ID | Requirement | Implementation | Status |
|----|-------------|----------------|--------|
| FR-01 | Add, update, delete/deactivate **products** | `ProductsForm` — Add, Update, Deactivate, Reactivate, Delete (hard delete with confirmation) | ✅ |
| FR-02 | Add, update, delete/deactivate **categories** | `CategoriesForm` — Add, Update Name, Deactivate, Reactivate | ✅ |
| FR-03 | Compute **subtotal** from cart line items | `SalesForm` — sum of quantity × unit price | ✅ |
| FR-04 | Apply **discount** (PWD / Senior / Member) | Mutually exclusive toggles; 20% / 20% / 10% | ✅ |
| FR-05 | Apply **VAT/tax** on discounted subtotal | Tax toggle + percentage input | ✅ |
| FR-06 | Compute **grand total** and **change** | Tendered amount validated ≥ total | ✅ |
| FR-07 | **Generate** formatted receipt text | `ReceiptBranding.BuildReceiptText` → stored in `sales.receipt_text` | ✅ |
| FR-08 | **Print** receipts | Windows print dialog + `ReceiptPrintHelper` | ✅ |
| FR-09 | **Export** receipt (PDF, text) | PDFsharp + save dialog | ✅ |
| FR-10 | **View transaction history** | `ReceiptForm` — list, search, date filter, sort | ✅ |
| FR-11 | **Daily sales report** | `ReportsForm` — daily revenue + top products | ✅ |
| FR-12 | **Deduct inventory** per sale | `SalesForm.SaveSale` updates `products.stock_quantity` in transaction | ✅ |
| FR-13 | **Role-based login** (Admin / Cashier) | `LoginForm` + `AppSession` | ✅ |
| FR-14 | Manage **cashier accounts** | `CashierAccountsForm` (admin) | ✅ |
| FR-15 | **Audit trail** for admin actions | `AuditLogger` → `AuditLogs` table; viewed in Reports | ✅ |
| FR-16 | **Import products** from CSV | `ProductsForm` — Import CSV | ✅ |
| FR-17 | **Dashboard** metrics and chart | `MainMenuForm` | ✅ |

---

## Non-functional requirements

| ID | Requirement | How met |
|----|-------------|---------|
| NFR-01 | **User-friendly interface** with consistent theme | Shared `UiTheme.vb` design system; sidebar shell on all workspace forms; 8px spacing grid |
| NFR-02 | **Fast local database** response | SQL Server LocalDB `(localdb)\MSSQLLocalDB`; inline SQL; indexes on `sale_items.sale_id` |
| NFR-03 | **Secure login** | Cashier passwords: salted hash via `PasswordHasher` / `CashierAccountService`; admin password in config (demo: change before production) |
| NFR-04 | **Organized audit trail** | `AuditLogs` for settings, products, categories, cashiers; `audit_products` / `audit_sales` for domain events |
| NFR-05 | **Reliability** | Sale finalize uses SQL transaction (sale + line items + stock); errors logged to `error_log` |
| NFR-06 | **Recoverability** | Backup/restore SQL guidance dialog; scripts in `GroupC/scripts/` |
| NFR-07 | **Usability** | Tooltips, tab order, empty states, confirmation dialogs on destructive actions |
| NFR-08 | **Deployability** | Single-machine Windows app; no separate server install beyond LocalDB |
| NFR-09 | **Maintainability** | Service classes (`DatabaseInitializer`, `AppSettings`, `AuditLogger`); centralized UI helpers |

---

## Hardware & software requirements

| Component | Minimum |
|-----------|---------|
| OS | Windows 10/11 (64-bit) |
| Runtime | .NET 10 Desktop Runtime |
| Database | SQL Server LocalDB (installed with Visual Studio or SQL Express LocalDB) |
| Display | 1366×768 or higher (app maximizes; optimized for 1920×1080) |
| Printer | Optional — any Windows-compatible printer for receipt print |
| Input | Keyboard and mouse; barcode scanner optional (keyboard wedge compatible via search field) |

---

## Constraints & assumptions

- **Single store, single PC** — no multi-user concurrency design  
- **Cash payment** implied — tendered/change fields; no card gateway  
- **English UI** — currency symbol configurable (default ₱)  
- **Administrator password** is hardcoded for demo (`admin123`) — must be changed for real use  
- **Receipt history** loads up to 500 most recent sales in the list  
- **Settings UI** exposes store name, footer, currency; other receipt fields editable via `%LocalAppData%\GroupC\settings.json`  
