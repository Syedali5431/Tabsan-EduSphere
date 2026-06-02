# College Scripts

This folder contains college-focused wrappers and seed scripts that follow the same ordering model as the main Scripts folder.

## Script Order

1. 01-Schema-Current.sql
2. 02-Seed-Core.sql
3. 03-FullDummyData.sql
4. 04-Maintenance-Indexes-And-Views.sql
5. 05-PostDeployment-Checks.sql
6. 06-Create-SuperAdmin-User.sql (optional utility)
7. 07-Fix-Course-Institution-Scope.sql (optional recovery/compatibility)

## Notes

- Script 01 delegates to the shared schema script and then validates required college schema tables.
- Script 02 delegates to the shared core seed script and then bootstraps deterministic college baseline entities.
- Script 03 seeds Class 11 and Class 12 report cards/results and applies completion behavior after Class 12.
- Script 04 delegates to shared maintenance and adds college-focused indexes for Class 11-12 report/result lookups.
- Script 05 validates Class 11/12 coverage and verifies completion-level progression after Class 12.
- Script 06 creates/updates a SuperAdmin account when an additional privileged login is needed.
- Script 07 patches missing `courses` / `course_offerings` scope columns (`TenantId`, `CampusId`, `InstitutionType`) for API compatibility.
