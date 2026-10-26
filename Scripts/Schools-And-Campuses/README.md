# Schools and Campuses

Scripts that add several schools and their campuses to the Tabsan EduSphere database, for an installation that starts with schools only.

In EduSphere a **school is a tenant**, each **campus** belongs to one school, and a campus's **School section** is a department of institution type School. The portal treats an installation as School-only while School departments are the only active departments.

## Run order

| # | Script | What it does |
|---|--------|--------------|
| 1 | `01-Add-Schools-And-Campuses.sql` | Adds the schools (tenants) and campuses listed at its top. |
| 2 | `02-Add-School-Sections.sql` | Gives every campus of those schools a School section (`<campus>-SCH`) with a Class 1 to 10 programme (`<campus>-CLS`), adds the classes *Class 1 … Class 10* if the database has none, and switches School on in the institution policy if it is off. |
| 3 | `03-Check-Institution-Types.sql` | Read-only: which institution types the portal shows and why, the departments that keep College or University visible, and the schools with their campuses. |

Run them against `Tabsan-EduSphere` after `02-Seed-Core.sql` (see `../README-SCRIPT-EXECUTION-ORDER.md`). All three are safe to re-run: existing schools, campuses, sections and programmes are left unchanged.

```powershell
cd Scripts\Schools-And-Campuses
.\Run-School-Scripts.ps1                                # LocalDB, database Tabsan-EduSphere
.\Run-School-Scripts.ps1 -Server "myserver" -Database "Tabsan-EduSphere"
.\Run-School-Scripts.ps1 -CheckOnly                     # only 03
```

or one at a time:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d "Tabsan-EduSphere" -b -f 65001 -i "Scripts\Schools-And-Campuses\01-Add-Schools-And-Campuses.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d "Tabsan-EduSphere" -b -f 65001 -i "Scripts\Schools-And-Campuses\02-Add-School-Sections.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d "Tabsan-EduSphere" -b -f 65001 -i "Scripts\Schools-And-Campuses\03-Check-Institution-Types.sql"
```

### Your own schools

Edit the lists at the top of the scripts:

- `01`: `@Schools` (code up to 64 characters, name) and `@Campuses` (school code, campus code up to 16 characters, name).
- `02`: `@Schools` — the same school codes as in `01`.

The examples add Tabsan Model School (Gulberg, Johar Town and DHA campuses), Crescent Public School (Main and North) and Green Valley Grammar School (City and Model Town).

## What the portal shows

An institution type is shown when it is switched on in the **Institution Policy** *and* has at least one active department. If none of the enabled types has a department yet (a brand-new installation), the policy applies as it is.

With School departments only:

- **Shown:** School menus, modules, filters and reports; labels use the school vocabulary (*Grade*, *Subject*, *Percentage*).
- **Hidden (display only):** the University menus (Degree Audit, Degree Rules, Graduation Eligibility, Graduation Apply / Applications, Final Year Project, Study Plan), the University-only Final Year Project module, the College and University dashboard cards, College and University choices in the Institute filters (Analytics, reports, certificates, payments, Grading Configuration, Result Calculation, Student Lifecycle, User Import, Admin Users) and the University-only reports (GPA & CGPA, FYP Status, Degree Certificate).
- **Unchanged:** no code, controller, module or table is removed; API rules are the same and direct links still open; **Add Department** still offers College and University, because that is how they are added later. The Institution Policy page says which enabled types are hidden because they have no departments yet.

### Adding College or University later

Add a department of that type, on the Departments page or with a script. Everything for that type reappears by itself: at once when the department is added in the portal, within a minute when it is added directly in the database. Deactivating the last department of a type hides it again.

### Databases built from 02-Seed-Core.sql

`02-Seed-Core.sql` (and `03-FullDummyData.sql`) seed University and College departments as well. While they are active, University and College stay visible; `03-Check-Institution-Types.sql` lists them. To run School-only on such a database, deactivate those departments on the Departments page (they can be activated again at any time). The seeded tenants named *University* and *College* still appear by name in the SuperAdmin's tenant pickers; rename or deactivate them in Tenant Management if they are not wanted.
