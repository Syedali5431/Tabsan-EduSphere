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
| 7 | `07-Fix-Sidebar-Role-Visibility.sql` | Resets sidebar menu visibility per role from a known-good spec |
| 8 | `student-journey-class1-10.sql` | Attaches a full Class 1-10 certificate-eligible history to student `col11s6`, under its own isolated demo department so it never contaminates 03's course/offering selection |
| 9 | `08-Create-Test-Admin-User.sql` | Creates the `testadmin` testing account: Admin role (every right except SuperAdmin) in the University tenant, assigned to all of its departments. Password `EduSphere147` |

## Notes

- `00-Cleanup-Master-Mistake.sql` and `01-Schema-Current.sql` must run against `master` because they create and switch to the `Tabsan-EduSphere` database.
- All other scripts run directly against the `Tabsan-EduSphere` database.
- Default password for all seeded users: **`EduSphere147`**
- Database version marker: currently `db.version = 2.4` (stored in `[Tabsan-EduSphere]` metadata table, set at the end of `03-FullDummyData.sql`)
- The previous domain script packs (`School Scripts/`, `College Scripts/`, `University Scripts/`) have been consolidated into `03-FullDummyData.sql`.
- There is no `09-Restructure-Sidebar-Menu.sql` — that step was superseded by `07-Fix-Sidebar-Role-Visibility.sql`, which is what actually ships in this folder.
- `student-journey-class1-10.sql` demonstrates a full school lifecycle with certificate eligibility (class 1-10 completion + attendance ≥85%). It resolves the target student, faculty, and its own dedicated department dynamically at runtime — it does not depend on any fixed GUIDs, so it stays valid across `03-FullDummyData.sql` re-runs.

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
sqlcmd -S $server -d "Tabsan-EduSphere" -i "Scripts/07-Fix-Sidebar-Role-Visibility.sql"
sqlcmd -S $server -d "Tabsan-EduSphere" -i "Scripts/student-journey-class1-10.sql"
sqlcmd -S $server -d "Tabsan-EduSphere" -i "Scripts/08-Create-Test-Admin-User.sql"
```

