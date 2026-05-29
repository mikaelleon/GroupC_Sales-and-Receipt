# Part IV — Required Features Checklist (Group C)

Use this table during **rehearsal** and **live demo**. Tick each item after verifying on a clean run.

**Legend:** ✅ Implemented · 📍 Where to demo

| # | Required feature | Status | Where in app | Demo step (30 sec each) |
|---|------------------|--------|--------------|-------------------------|
| 1 | **Add products** | ✅ | Products → Add Product | Enter name, price, stock, category → Add → row appears in grid |
| 2 | **Compute total** | ✅ | Point of Sale → checkout panel | Add 2+ items → show Subtotal, Discount, Tax, **Amount Due** updating |
| 3 | **Receipt view** | ✅ | Receipt Preview | Select sale from history → receipt text + preview visible |
| 4 | **Quantity update** | ✅ | POS cart + Products stock | Change qty in cart OR update stock on Products form |
| 5 | **Discount computation** | ✅ | POS → PWD / Senior / Member | Toggle discount → Discount line and total change (20% or 10%) |
| 6 | **Tax/VAT computation** | ✅ | POS → VAT / Tax toggle | Enable tax, set % → Tax line and total update |
| 7 | **Receipt printing** | ✅ | Receipt Preview → Print | Print preview or Print → show 40-column receipt layout |
| 8 | **Daily sales report** | ✅ | Reports → Sales & Revenue | Set date range → Run report → Daily Revenue grid populated |
| 9 | **Product inventory deduction** | ✅ | POS finalize + Products grid | Note stock before sale → finalize → stock lower in Products |
| 10 | **Transaction history** | ✅ | Receipt Preview → history list | Search/filter by date; show multiple past sales |

---

## CRUD checklist (rubric: 20 pts)

| Operation | Entity | Demo action | Expected result |
|-----------|--------|-------------|-----------------|
| **Create** | Product | Add Product | New row in grid |
| **Read** | Product | Select row / search | Fields populate; grid filters work |
| **Update** | Product | Change price/stock → Update | DB + grid refresh |
| **Delete** | Product | Deactivate *(preferred)* or Delete | `is_active = 0` or row removed |
| **Create** | Category | Add category | Appears in dropdown on Products |
| **Update** | Category | Update name | Grid + product dropdown update |
| **Delete** | Category | Deactivate | Hidden from active filter |
| **Create** | Cashier | Register Cashier | New row in cashier grid |
| **Update** | Cashier | Update display name / reset password | Saved + audit log entry |
| **Delete** | Cashier | Deactivate account | Cannot login |

---

## Database integration checklist (rubric: 20 pts)

| Check | How to show evaluators |
|-------|------------------------|
| DB connects | Dashboard: **System Status: Online** (green dot) |
| Data persists | Close app, reopen — products/sales still there |
| Sale writes multiple tables | After finalize: `sales`, `sale_items`, stock updated |
| Reports read DB | Reports tab shows aggregated SQL results |
| Audit logged | Reports → System Audit Logs after admin change |
| LocalDB named correctly | Mention `(localdb)\MSSQLLocalDB`, database `GroupC_DB` |

---

## System functionality checklist (rubric: 20 pts)

| Flow | Pass criteria |
|------|---------------|
| Admin login | Full sidebar visible |
| Cashier login | Limited menu (POS + Receipt only) |
| Full sale | Cart → discount → tax → tender ≥ total → finalize → receipt opens |
| Receipt export | Save PDF or text succeeds |
| Settings save | Store name change appears on next receipt |
| Error handling | Empty cart finalize shows validation message (don't crash) |

---

## Known talking points (honest answers)

| Question | Answer |
|----------|--------|
| Admin default password? | Demo password `admin123` — change before real deployment |
| Backup automatic? | Dialog provides SQL commands; manual backup via sqlcmd |
| Receipt email? | Opens mail client; long receipts may need clipboard paste |
| Max history rows? | Last 500 sales in receipt list |
| UTC timestamps? | `sale_date` stored UTC; display normalized to local time |

See [RECOMMENDATIONS.md](../RECOMMENDATIONS.md) for improvement backlog.
