# Part IV — Project Submission Checklist

## 1. Project documentation (print or PDF)

Combine into one document for submission:

| Section | Source in repo | Done? |
|---------|----------------|-------|
| Title page | [08-title-page-and-reflection-template.md](08-title-page-and-reflection-template.md) | ☐ |
| Introduction / background | [01-system-description.md](01-system-description.md) — Purpose + Importance | ☐ |
| System description | [01-system-description.md](01-system-description.md) | ☐ |
| Features list (10 checked) | [05-features-checklist.md](05-features-checklist.md) | ☐ |
| System requirements | [02-system-requirements.md](02-system-requirements.md) | ☐ |
| Interface design | [03-interface-design-and-navigation.md](03-interface-design-and-navigation.md) + screenshots | ☐ |
| Database structure | [04-database-design.md](04-database-design.md) | ☐ |
| Screenshots (labeled) | [screenshots/](screenshots/) | ☐ |
| Reflection (per member) | [08-title-page-and-reflection-template.md](08-title-page-and-reflection-template.md) | ☐ |

**Export tip:** Open each `.md` in VS Code / Word / Pandoc → export combined PDF:

```powershell
# Optional — if pandoc installed:
pandoc docs/01-system-description.md docs/02-system-requirements.md docs/03-interface-design-and-navigation.md docs/04-database-design.md docs/05-features-checklist.md -o GroupC_Documentation.pdf
```

Or copy sections into Google Docs / Word manually.

---

## 2. Source code zip

| Item | Path | Include? |
|------|------|----------|
| Solution | `GroupC.slnx` | ✅ |
| Project source | `GroupC/*.vb`, `GroupC/Assets/` | ✅ |
| SQL scripts | `GroupC/scripts/*.sql` | ✅ |
| Documentation | `docs/`, `README.md` | ✅ |
| Build output | `GroupC/bin/`, `GroupC/obj/` | ❌ exclude |
| IDE state | `.vs/` | ❌ exclude |

**Suggested zip name:** `GroupC_SourceCode.zip`

```powershell
# From repo root — example (adjust paths):
Compress-Archive -Path GroupC.slnx, GroupC, docs, README.md, FORMS.md `
  -DestinationPath GroupC_SourceCode.zip -Force
```

Verify zip opens and `dotnet build GroupC.slnx` succeeds on a clean machine.

---

## 3. Presentation slides

Minimum slides — see [06-demo-and-presentation-guide.md](06-demo-and-presentation-guide.md):

- [ ] System overview  
- [ ] 10 features mapped to screens  
- [ ] Database tables + relationships  
- [ ] Screenshots (one per major form)  
- [ ] Demo flow outline  
- [ ] Members and roles  

---

## 4. Live demo readiness

- [ ] Rehearsed 5–10 min script ([06-demo-and-presentation-guide.md](06-demo-and-presentation-guide.md))  
- [ ] All items on [05-features-checklist.md](05-features-checklist.md) verified  
- [ ] Cashier test account created  
- [ ] At least 5 products in catalog  
- [ ] LocalDB running  
- [ ] Laptop charged / projector tested  

---

## 5. Files in this `docs/` folder

| File | Part |
|------|------|
| [README.md](README.md) | Index |
| [01-system-description.md](01-system-description.md) | I-A |
| [02-system-requirements.md](02-system-requirements.md) | I-B |
| [03-interface-design-and-navigation.md](03-interface-design-and-navigation.md) | I-C |
| [04-database-design.md](04-database-design.md) | I-D |
| [05-features-checklist.md](05-features-checklist.md) | IV — features |
| [06-demo-and-presentation-guide.md](06-demo-and-presentation-guide.md) | IV — demo & slides |
| [07-project-submission-checklist.md](07-project-submission-checklist.md) | IV — this file |
| [08-title-page-and-reflection-template.md](08-title-page-and-reflection-template.md) | IV — title & reflection |
| [screenshots/README.md](screenshots/README.md) | IV — screenshot guide |

---

## Instructor handout alignment notes

Your course materials use simplified table/column names. Our docs include a **mapping table** in [04-database-design.md](04-database-design.md) so you can explain equivalents during defense (e.g. `products.id` = `product_id`).

The handout lists `AuditLogs`; this project uses **`AuditLogs`** plus `audit_products` / `audit_sales` / `error_log` for extended logging.
