# Part I-A — System Description

## System name

**International Bookstore — Sales & Receipt System** (Group C)

---

## Purpose

The system is a **desktop point-of-sale (POS) application** for a small retail bookstore. It lets staff **ring up sales**, **apply discounts and tax**, **print or export receipts**, and **manage products, categories, and cashier accounts**. All transaction data is stored locally in **SQL Server LocalDB** so the store can operate without a full database server.

The application replaces manual computation and paper receipt books with an automated, auditable sales workflow suitable for a school project demo and a real small counter (bookstore, school supplies, or similar).

---

## Target users

| Role | Description | Access |
|------|-------------|--------|
| **Store administrator** | Owner or manager who configures the store, catalog, staff accounts, and reports | Full menu: products, categories, cashiers, POS, receipts, reports, settings, backup guidance |
| **Cashier** | Front-desk staff who process daily sales | Point of Sale, Receipt Preview; no catalog admin or reports |

Authentication is **role-based**: administrators sign in with a system password; cashiers sign in with **username + password** stored in the database (hashed).

---

## Complete features list

### Required Group C features (10)

| # | Feature | What the system does |
|---|---------|----------------------|
| 1 | **Add products** | Create products with name, price, stock, category, optional image (`ProductsForm`) |
| 2 | **Compute total** | Cart subtotal, discount, tax, and grand total update live (`SalesForm`) |
| 3 | **Receipt view** | Preview past receipts with search, filters, and zoom (`ReceiptForm`) |
| 4 | **Quantity update** | Edit cart quantity; manage stock on products; validate against available stock |
| 5 | **Discount computation** | PWD 20%, Senior 20%, Member 10% (one at a time) |
| 6 | **Tax/VAT computation** | Optional VAT toggle with configurable percentage |
| 7 | **Receipt printing** | Print, print preview, save PDF, save text, copy to clipboard |
| 8 | **Daily sales report** | Daily revenue grid and top products by date range (`ReportsForm`) |
| 9 | **Product inventory deduction** | Stock reduced in same transaction as sale finalize |
| 10 | **Transaction history** | Receipt history list (500 recent sales), filters, export batch |

See [05-features-checklist.md](05-features-checklist.md) for demo verification steps.

### Additional features (beyond minimum)

- **Category management** — create, rename, deactivate categories  
- **Cashier account management** — register, reset password, deactivate/reactivate  
- **CSV product import** — bulk load/update catalog  
- **Dashboard** — KPI cards, daily sales chart (7–90 days), low-stock alert  
- **Audit log** — admin actions logged to `AuditLogs` (Reports → System Audit Logs)  
- **Store settings** — store name, receipt footer, currency symbol (JSON + Settings UI)  
- **Backup/restore guidance** — SQL commands dialog for LocalDB backup  
- **Cross-screen sidebar navigation** — move between modules without returning to dashboard first  
- **Role-based UI** — menu items hidden per role  

---

## Importance to the store

| Benefit | How the system helps |
|---------|----------------------|
| **Accurate pricing** | Totals, discounts, and tax computed automatically; less cashier error |
| **Professional receipts** | Printed/PDF receipts with store branding, line items, and payment breakdown |
| **Inventory control** | Stock decreases on each sale; low-stock alert on dashboard |
| **Accountability** | Each sale stored with timestamp and receipt text; audit log for admin changes |
| **Operational visibility** | Daily sales report and dashboard chart support end-of-day reconciliation |
| **Security** | Separate cashier accounts; admin-only catalog and settings |
| **Continuity** | Local database persists data after closing the app; backup guidance for recovery |

For a small bookstore, the system supports **daily operations** (sell, receipt, restock awareness) and **management tasks** (catalog, staff, reports) in one Windows application without cloud dependency.
