# Database Script Execution Order — Tabsan EduSphere v1.1

Run scripts in this exact order for a fresh deployment.

## Full Deployment Path

| Step | Script | Runs Against |
|------|--------|-------------|
| 0 | `00-Cleanup-Master-Mistake.sql` | `master` |
| 1 | `01-Schema-Current.sql` | `master` |
| 2 | `02-Seed-Core.sql` | `Tabsan-EduSphere` |
| 3 | `03-FullDummyData.sql` | `Tabsan-EduSphere` |
| 4 | `04-Maintenance-Indexes-And-Views.sql` | `Tabsan-EduSphere` |
| 5 | `05-PostDeployment-Checks.sql` | `Tabsan-EduSphere` |

## Optional Post-Deployment Scripts

| Step | Script | Purpose |
|------|--------|---------|
| 6 | `06-Create-SuperAdmin-User.sql` | Creates an additional SuperAdmin account (`superadmin2`) |
| 7 | `student-journey-class1-10.sql` | School student lifecycle: Class 1-10 results, attendance, assignments for col11s6 |
| 9 | `09-Restructure-Sidebar-Menu.sql` | Configures sidebar navigation with role-based menu visibility |

## Notes

- `00-Cleanup-Master-Mistake.sql` and `01-Schema-Current.sql` must run against `master` because they create and switch to the `Tabsan-EduSphere` database.
- All other scripts run directly against the `Tabsan-EduSphere` database.
- Default password for all seeded users: **`EduSphere147`**
- Database version marker: `db.version = 1.1` (stored in `[Tabsan-EduSphere]` metadata table)
- The previous domain script packs (`School Scripts/`, `College Scripts/`, `University Scripts/`) have been consolidated into `03-FullDummyData.sql`.
- Legacy utility scripts (07, 08, 09-old) have been removed; functionality is now part of the core seed scripts.
- `student-journey-class1-10.sql` demonstrates a full school lifecycle with certificate eligibility (class 1-10 completion + attendance ≥85%).

## Example Commands (LocalDB)

```powershell
$server = "(localdb)\MSSQLLocalDB"

# Step 0-1: Run against master
sqlcmd -S $server -d master -i "Scripts/00-Cleanup-Master-Mistake.sql"
sqlcmd -S $server -d master -i "Scripts/01-Schema-Current.sql"

# Steps 2-5: Run against Tabsan-EduSphere
sqlcmd -S $server -d "Tabsan-EduSphere" -i "Scripts/02-Seed-Core.sql"
sqlcmd -S $server -d "Tabsan-EduSphere" -i "Scripts/03-FullDummyData.sql"
sqlcmd -S $server -d "Tabsan-EduSphere" -i "Scripts/04-Maintenance-Indexes-And-Views.sql"
sqlcmd -S $server -d "Tabsan-EduSphere" -i "Scripts/05-PostDeployment-Checks.sql"

# Optional
sqlcmd -S $server -d "Tabsan-EduSphere" -i "Scripts/06-Create-SuperAdmin-User.sql"
sqlcmd -S $server -d "Tabsan-EduSphere" -i "Scripts/09-Restructure-Sidebar-Menu.sql"
```

