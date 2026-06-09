# Tabsan EduSphere — Database Scripts v1.0

## Execution Order

| # | Script | Purpose |
|---|--------|---------|
| 0 | `00-Cleanup-Master-Mistake.sql` | Drops existing database for fresh start |
| 1 | `01-Schema-Current.sql` | Creates all tables (EF Migrations generated) |
| 2 | `02-Seed-Core.sql` | Seeds roles, tenants, departments, programs, courses, semesters, core users |
| 3 | `03-FullDummyData.sql` | Full demo data: 210+ students, attendance, results, assignments, quizzes, FYP |
| 4 | `04-Maintenance-Indexes-And-Views.sql` | Performance indexes and summary views |
| 5 | `05-PostDeployment-Checks.sql` | Validates data integrity |
| 6 | `06-Create-SuperAdmin-User.sql` | Creates additional SuperAdmin (superadmin2) |
| 9 | `09-Restructure-Sidebar-Menu.sql` | Sidebar navigation with role-based visibility |

## Database Summary

| Item | Count |
|------|-------|
| **Institutes** | 3 (University, College, School) |
| **Tenants** | 3 |
| **Campuses** | 3 |
| **Departments** | 4 |
| **Academic Programs** | 6 |
| **Courses** | 122 |
| **Semesters** | 20 |
| **Demo Students** | 210+ |
| **FYP Projects** | 10 |
| **Database Version** | 1.0 |

## Programs

| Institute | Program | Duration | Students |
|-----------|---------|----------|----------|
| University | BSCS | 8 Semesters | 80 |
| University | BBA | 8 Semesters | 80 |
| University | MSE | 4 Semesters | 40 |
| University | Spanish Language | 1 Year | 10 |
| College | ICS | 2 Years | 20 |
| School | Science | 10 Years (Class 1-10) | 100 |

## Login Credentials

**All passwords:** `EduSphere147`

| Username | Role | Scope |
|----------|------|-------|
| `superadmin` | SuperAdmin | Global |
| `superadmin2` | SuperAdmin | Global |
| `admin.uni` | Admin | University |
| `admin.col` | Admin | College |
| `admin.sch` | Admin | School |
| `faculty.uni` | Faculty | University |
| `faculty.col` | Faculty | College |
| `faculty.sch` | Faculty | School |
| `student.uni` | Student | University |
| `student.col` | Student | College |
| `student.sch` | Student | School |
| `finance.uni` | Finance | University |
| `finance.col` | Finance | College |
| `finance.sch` | Finance | School |

Plus 210+ demo students (e.g. `bscs1s1`, `bba3s5`, `col11s3`, `sch5s7`, `mse2s4`, `spanish3`).

## Departments

| Institute | Department | Code |
|-----------|-----------|------|
| University | Information Technology | IT |
| University | Business Administration | BUS |
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
sqlcmd -S $server -d "Tabsan-EduSphere" -i "Scripts/09-Restructure-Sidebar-Menu.sql"
```


## Recommended Execution

Demo/full path:

```powershell
sqlcmd -S "localhost" -E -d "master" -i "Scripts\01-Schema-Current.sql"
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\02-Seed-Core.sql"
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\03-FullDummyData.sql"
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\04-Maintenance-Indexes-And-Views.sql"
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\05-PostDeployment-Checks.sql"
```

Clean baseline path:

```powershell
sqlcmd -S "localhost" -E -d "master" -i "Scripts\01-Schema-Current.sql"
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\Seed-Core-Clean.sql"
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\04-Maintenance-Indexes-And-Views.sql"
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\05-PostDeployment-Checks-Clean.sql"
```

The maintenance step is optional for strict clean-seed validation, but recommended to keep index/view state aligned with production deployments.

Utility and recovery scripts (run when needed):

```powershell
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\06-Create-SuperAdmin-User.sql"
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\07-Fix-Course-Institution-Scope.sql"
```

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.
