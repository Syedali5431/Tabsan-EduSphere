# Tabsan EduSphere — Database Scripts v1.1

## Execution Order

| # | Script | Purpose |
|---|--------|---------|
| 0 | `00-Cleanup-Master-Mistake.sql` | Drops existing database for fresh start |
| 1 | `01-Schema-Current.sql` | Creates all tables (EF Migrations generated) |
| 2 | `02-Seed-Core.sql` | Seeds roles, tenants, departments, programs, courses, semesters, core users |
| 3 | `03-FullDummyData.sql` | Full demo data: 295 students, attendance, results, assignments, quizzes, FYP, payments, rubrics, notifications |
| 4 | `04-Maintenance-Indexes-And-Views.sql` | Performance indexes and summary views |
| 5 | `05-PostDeployment-Checks.sql` | Validates data integrity after deployment |
| 6 *(optional)* | `06-Create-SuperAdmin-User.sql` | Creates additional SuperAdmin (`superadmin2`) |
| 7 *(optional)* | `07-Fix-Sidebar-Role-Visibility.sql` | Resets sidebar menu visibility per role |
| 8 *(optional)* | `student-journey-class1-10.sql` | Certificate-demo student lifecycle: Class 1-10 with results, attendance, assignments for `col11s6`, under its own isolated department |

There is no `09-Restructure-Sidebar-Menu.sql` or `Seed-Core-Clean.sql` in this folder — earlier revisions of this README referenced scripts that were since renamed or removed; `07-Fix-Sidebar-Role-Visibility.sql` is the script that actually ships.

## Database Summary

Counts below are for the core pipeline (02 → 03 → 04 → 05); running the optional `student-journey-class1-10.sql` adds one extra department and 50 extra courses on top of these.

| Item | Count |
|------|-------|
| **Institutes** | 3 (University, College, School) |
| **Tenants** | 4 (Default + University + College + School) |
| **Campuses** | 4 |
| **Departments** | 5 (IT, BUS, IT-COL, SCI, SPA) |
| **Academic Programs** | 6 |
| **Courses** | ~124 |
| **Semesters** | 29 (BSCS 8 + BBA 8 + School classes 10 + College classes 2 + Spanish 1) |
| **Demo Students** | 295 |
| **FYP Projects** | 22 |
| **Database Version** | 2.4 |

## Programs

| Institute | Program | Duration | Demo Students |
|-----------|---------|----------|----------|
| University | BSCS | 8 Semesters | 80 |
| University | BBA | 8 Semesters | 80 |
| University | MSE | 4 Semesters | 0 — courses/offerings exist but no demo students are enrolled |
| University | Spanish Language | 1 Year | 10 |
| College | ICS | 2 Years | 20 |
| School | Science | 10 Years (Class 1-10) | 100 |

Plus 5 graduated demo students (one per program except MSE) and 5 core login-only users (SuperAdmin + 3 Admins), for 295 total student profiles.

## Login Credentials

**All passwords:** `EduSphere147`

| Username | Role | Scope |
|----------|------|-------|
| `superadmin` | SuperAdmin | Global |
| `superadmin2` | SuperAdmin | Global |
| `admin.uni` | Admin | University |
| `admin.col` | Admin | College |
| `admin.sch` | Admin | School |

Faculty, Student, and Finance users are created by `03-FullDummyData.sql` (e.g. `faculty.it1`, `bscs1s1`, etc.).

Plus 210+ demo students (e.g. `bscs1s1`, `bba3s5`, `col11s3`, `sch5s7`, `mse2s4`, `spanish3`).

## Departments

| Institute | Department | Code |
|-----------|-----------|------|
| University | Information Technology | IT |
| University | Business Administration | BUS |
| University | Spanish Language | SPA |
| College | Information Technology | IT-COL |
| School | Science Department | SCI |

## Example Commands

```powershell
$server = "(localdb)\MSSQLLocalDB"

sqlcmd -S $server -d master -i "Scripts/00-Cleanup-Master-Mistake.sql"
sqlcmd -S $server -d master -i "Scripts/01-Schema-Current.sql"
sqlcmd -S $server -d "Tabsan-EduSphere" -i "Scripts/02-Seed-Core.sql"
sqlcmd -S $server -d "Tabsan-EduSphere" -i "Scripts/03-FullDummyData.sql"
sqlcmd -S $server -d "Tabsan-EduSphere" -i "Scripts/04-Maintenance-Indexes-And-Views.sql"
sqlcmd -S $server -d "Tabsan-EduSphere" -i "Scripts/05-PostDeployment-Checks.sql"
```

Optional utilities:

```powershell
sqlcmd -S $server -d "Tabsan-EduSphere" -i "Scripts/06-Create-SuperAdmin-User.sql"
sqlcmd -S $server -d "Tabsan-EduSphere" -i "Scripts/07-Fix-Sidebar-Role-Visibility.sql"
sqlcmd -S $server -d "Tabsan-EduSphere" -i "Scripts/student-journey-class1-10.sql"
```


## Recommended Execution

Demo/full path:

```powershell
sqlcmd -S "localhost" -E -d "master" -i "Scripts\00-Cleanup-Master-Mistake.sql"
sqlcmd -S "localhost" -E -d "master" -i "Scripts\01-Schema-Current.sql"
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\02-Seed-Core.sql"
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\03-FullDummyData.sql"
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\04-Maintenance-Indexes-And-Views.sql"
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\05-PostDeployment-Checks.sql"
```

Clean baseline path (core seed only, skip the full 295-student dummy dataset):

```powershell
sqlcmd -S "localhost" -E -d "master" -i "Scripts\00-Cleanup-Master-Mistake.sql"
sqlcmd -S "localhost" -E -d "master" -i "Scripts\01-Schema-Current.sql"
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\02-Seed-Core.sql"
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\04-Maintenance-Indexes-And-Views.sql"
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\05-PostDeployment-Checks-Clean.sql"
```

The maintenance step is optional for strict clean-seed validation, but recommended to keep index/view state aligned with production deployments.

Utility and recovery scripts (run when needed):

```powershell
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\06-Create-SuperAdmin-User.sql"
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\07-Fix-Sidebar-Role-Visibility.sql"
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\student-journey-class1-10.sql"
```

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.
