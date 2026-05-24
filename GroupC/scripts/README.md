# SQL scripts reference (`GroupC/scripts`)

Manual setup and **demo catalog** scripts for `GroupC_DB` on SQL Server LocalDB. The Windows app creates the database and most tables at startup via `DatabaseInitializer.vb`; these files are for SSMS, Azure Data Studio, or `sqlcmd` when you want control outside the app.

**Main project docs:** [README.md](../../README.md) at the repository root.

## Run order

| Step | File | Purpose |
|------|------|---------|
| 1 | `01_create_database.sql` | Creates `GroupC_DB` if missing |
| 2 | `02_create_tables.sql` | `categories`, `products` (partial schema reference) |
| 3 | — | Start the app **once** so `DatabaseInitializer` creates `sales`, `sale_items`, `cashier_accounts`, audit tables, etc. |
| 4a | `03_seed_data.sql` | International Bookstore sample categories and products (~100+ SKUs) |
| 4b | `04_seed_national_bookstore.sql` | National Book Store–style departments and products (optional; different category names) |
| 5 | `05_merge_duplicate_categories.sql` | Optional: merge overlapping categories after running **both** 03 and 04 |

Scripts **03**, **04**, and **05** are **idempotent** (safe to re-run; skips existing names or no-ops when already merged).

## Catalog strategies

### International Bookstore only

```text
01 → 02 → (app once) → 03
```

### NBS-style catalog only

```text
01 → 02 → (app once) → 04
```

### Combined demo (recommended for coursework demos)

```text
01 → 02 → (app once) → 03 → 04 → 05
```

`05_merge_duplicate_categories.sql` reassigns products from duplicate category names (for example `Writing Instruments` → `Writing Supplies`) and deactivates the empty duplicate categories.

## Example: `sqlcmd`

From a shell (adjust server if not LocalDB):

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -i "GroupC\scripts\01_create_database.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GroupC_DB -i "GroupC\scripts\02_create_tables.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GroupC_DB -i "GroupC\scripts\03_seed_data.sql"
# optional:
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GroupC_DB -i "GroupC\scripts\04_seed_national_bookstore.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d GroupC_DB -i "GroupC\scripts\05_merge_duplicate_categories.sql"
```

## Runtime seed vs SQL scripts

| Source | When | What |
|--------|------|------|
| `DatabaseInitializer.SeedSampleProducts()` | First app run if `products` is empty | Small generic Fiction / Textbooks / Stationery set |
| `03_seed_data.sql` | Manual | Full International Bookstore–style catalog |
| `04_seed_national_bookstore.sql` | Manual | NBS-aligned departments and PHP retail prices |

SQL files are **not** executed automatically from disk. Run them manually when you want the larger catalogs.

## Data notes

- **03** and **04** use different category names so both can coexist until **05** merges duplicates.
- Product prices are **PHP-style** demo values, not live National Book Store API data (no official public product CSV).
- Unique constraints: `categories.category_name`, `products.product_name`.

## Keep in sync

When you change product/category columns in `DatabaseInitializer.vb`, update `02_create_tables.sql` and re-test seeds **03** / **04**.
