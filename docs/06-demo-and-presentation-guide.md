# Part IV — Demo & Presentation Guide

## Presentation slides outline (8–10 slides)

| Slide | Title | Content |
|-------|-------|---------|
| 1 | Title | System name, Group C, members, course, section, date |
| 2 | Problem & purpose | Small bookstore needs POS + receipts + inventory |
| 3 | Target users | Admin vs Cashier roles (table) |
| 4 | **10 required features** | Checklist from [05-features-checklist.md](05-features-checklist.md) |
| 5 | System architecture | VB.NET WinForms + LocalDB diagram |
| 6 | **Database design** | ER diagram or table list from [04-database-design.md](04-database-design.md) |
| 7 | UI / navigation | Sidebar screenshot + flow diagram |
| 8 | Screenshots | 2×2 grid: Login, Dashboard, POS, Receipt |
| 9 | **Demo flow** | Numbered steps (below) |
| 10 | Members & roles | Name, role in project, one-line contribution |

---

## Live demo script (5–10 minutes)

**Before demo:** Close other apps; start LocalDB; run app; login ready; seed catalog if empty (`dotnet run` once or run seed script).

### Minute 0–1 — Introduce system

> “This is the **International Bookstore Sales & Receipt System** for Group C. It’s a Windows POS app using VB.NET and SQL Server LocalDB. Administrators manage the catalog and reports; cashiers ring up sales and print receipts.”

Show dashboard: KPI cards, chart, **System Status: Online**.

---

### Minute 1–2 — Admin: product CRUD

1. Open **Manage Products**
2. **Add** a product (e.g. “Demo Notebook”, ₱55, stock 20, category)
3. **Update** price or stock on selected row
4. Mention **Deactivate** vs Delete (soft delete preferred)

**Rubric:** CRUD + Add products feature

---

### Minute 2–3 — Categories (optional 20 sec)

1. **Manage Categories** → add or rename one category
2. Return to Products — category dropdown updated

**Rubric:** CRUD

---

### Minute 3–5 — Ring up a sale

1. **Point of Sale**
2. Add **2 products** to cart
3. Apply **Senior** or **PWD** discount (20%)
4. Toggle **VAT/Tax** (e.g. 12%)
5. Enter **tendered** amount ≥ total
6. **Finalize sale** → Receipt opens

**Rubric:** Compute total, discount, tax, inventory deduction (mention stock will drop)

---

### Minute 5–6 — Receipt

1. Show **receipt preview** (monospace layout, logo if present)
2. **Print preview** OR **Save PDF**
3. Point to **Date & Time**, line items, TOTAL, change

**Rubric:** Receipt view, receipt printing

---

### Minute 6–7 — Transaction history

1. **Receipt Preview** → history list
2. Search by sale # or use date filter
3. Select earlier sale — preview updates

**Rubric:** Transaction history

---

### Minute 7–8 — Daily sales report

1. **Reports** → Sales & Revenue
2. Set date range (Today or last 7 days)
3. **Run report** — Daily Revenue + Top Products
4. Switch to **System Audit Logs** — show one admin action

**Rubric:** Daily sales report + database + audit trail

---

### Minute 8–9 — Database & roles (talk track)

> “Data lives in **GroupC_DB** on LocalDB. Each sale inserts into `sales` and `sale_items` and reduces `products.stock_quantity` in one transaction.”

1. Log out → login as **Cashier** (pre-created account)
2. Show limited menu (POS + Receipt only)

**Rubric:** Database integration, role-based login

---

### Minute 9–10 — Close

> “All ten Group C features are implemented. Source code and documentation are in our submission folder.”

Offer Q&A.

---

## Demo disaster recovery

| Problem | Quick fix |
|---------|-----------|
| LocalDB offline | Run `sqllocaldb start MSSQLLocalDB` |
| Empty product list | Admin → Import CSV or use seeded data |
| Login fails | Admin: `admin123`; create cashier in Manage Cashiers |
| Build locked | Close `GroupC.exe` before rebuild |
| Chart empty | Run at least one sale in date range |

---

## What evaluators watch for (60 pts)

| Rubric area (20 pts each) | Show this clearly |
|---------------------------|-------------------|
| **Database Integration** | Online status, sale saves, report queries, mention `GroupC_DB` |
| **CRUD Operations** | Live add/update/deactivate product (+ category if time) |
| **System Functionality** | Full sale flow + receipt + report + role switch |

Use [05-features-checklist.md](05-features-checklist.md) as a printed cheat sheet during rehearsal.

---

## Suggested slide → demo mapping

| After slide | Live action |
|-------------|-------------|
| Features slide | “We’ll demonstrate each of these now.” |
| Database slide | Keep visible during Reports + audit log |
| Screenshots slide | Optional — or skip if live demo is smooth |
| Demo flow slide | Follow script above |
