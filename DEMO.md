# Manual Smoke Test Checklist — Group C POS

Run before presentation or after code changes. Check each box when verified.

**Prerequisites:** LocalDB running, app builds, demo catalog loaded (Backup / Restore → Load demo catalog).

---

## 1. Authentication

| Step | Expected | OK |
|------|----------|-----|
| Login as **Admin** with default password `admin123` (or your changed password) | Main menu shows admin sidebar items | ☐ |
| Logout, login as **Cashier** (pre-created account) | Only POS + Receipt Preview visible | ☐ |
| Wrong admin password | Error message, no access | ☐ |

---

## 2. Product CRUD (Admin)

| Step | Expected | OK |
|------|----------|-----|
| **Manage Products** → Add product (name, price, stock, category) | Row appears in grid | ☐ |
| Select row → Update price/stock | Grid refreshes; stock change logged in **Reports → Audit Log** as `STOCK_ADJUSTED` | ☐ |
| Try **Delete** on product used in a past sale | Blocked with deactivate message | ☐ |
| **Deactivate** product | Hidden from POS catalog | ☐ |

---

## 3. Point of Sale

| Step | Expected | OK |
|------|----------|-----|
| Open **Point of Sale** after changing **Settings** (currency/footer) | Summary uses new currency without restart | ☐ |
| Empty catalog as **Cashier** | Message says contact administrator | ☐ |
| Add items via product cards | Cart subtotal updates | ☐ |
| **Scan barcode** field: enter product ID or barcode + Enter | Item added to cart | ☐ |
| Apply PWD/Senior discount + VAT | Discount and tax lines correct | ☐ |
| Finalize with sufficient tender | Receipt opens; stock decreases | ☐ |

---

## 4. Receipts

| Step | Expected | OK |
|------|----------|-----|
| **Receipt Preview** → search/filter history | Matching sales listed | ☐ |
| **Load more** when >500 receipts | Additional rows append | ☐ |
| Print preview or Save PDF | Output matches preview | ☐ |
| Admin: **Void sale** on selected receipt | Marked void; stock restored; excluded from reports | ☐ |

---

## 5. Reports & Dashboard

| Step | Expected | OK |
|------|----------|-----|
| **Reports** → Run report for last 7 days | Daily revenue + top products populate | ☐ |
| Double-click a **Daily Revenue** row | Receipt Preview opens filtered to that date | ☐ |
| **Audit Log** tab → Load | Shows admin actions + stock adjustments | ☐ |
| Dashboard **Low stock alert** click (admin) | Opens Manage Products filtered to low stock | ☐ |

---

## 6. Database & Backup

| Step | Expected | OK |
|------|----------|-----|
| Dashboard status | **Online** | ☐ |
| **Backup / Restore** → Run backup now | `.bak` file created | ☐ |
| **Load demo catalog** | Success message; products appear in POS | ☐ |

---

## Quick 10-minute demo path

1. Admin login → dashboard KPIs  
2. Low-stock alert → products grid  
3. POS sale with discount + tax  
4. Receipt print/PDF  
5. Reports daily row → receipt drill-down  
6. Audit log (stock adjustment if you edited stock)  
7. Logout → cashier login (limited menu)

See also: [docs/06-demo-and-presentation-guide.md](docs/06-demo-and-presentation-guide.md)
