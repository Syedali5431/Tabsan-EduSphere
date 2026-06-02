# School Scripts

This folder contains school-focused wrappers and seed scripts that follow the same ordering model as the main Scripts folder.

## Script Order

1. 01-Schema-Current.sql
2. 02-Seed-Core.sql
3. 03-FullDummyData.sql
4. 04-Maintenance-Indexes-And-Views.sql
5. 05-PostDeployment-Checks.sql
6. 06-Create-SuperAdmin-User.sql (optional utility)
7. 07-Fix-Course-Institution-Scope.sql (optional recovery/compatibility)

## Notes

- Scripts are pure T-SQL (no SQLCMD `:r` dependency) and can run in standard SSMS/Azure Data Studio query mode.
- Script 01 validates school prerequisites and self-heals missing LMS discussion columns used by the current app model.
- Script 02 ensures deterministic school baseline entities (department/program/faculty/student profile) for Class 1-10 flows.
- Script 03 seeds school class coverage (Class 1 to Class 10) with report cards/results and applies completion markers after Class 10 where schema supports it.
- Script 04 adds school-focused lookup indexes for Class 1-10 report-card and result access.
- Script 05 performs post-deployment coverage checks and prints warnings/info for any missing coverage.
- Script 06 creates/updates a SuperAdmin account when an additional privileged login is needed.
- Script 07 patches missing `courses` / `course_offerings` scope columns (`TenantId`, `CampusId`, `InstitutionType`) for API compatibility.
