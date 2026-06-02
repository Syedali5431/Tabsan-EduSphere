# University Scripts

This folder contains university-focused wrappers and seed scripts that follow the same ordering model as the main Scripts folder.

## Script Order

1. 01-Schema-Current.sql
2. 02-Seed-Core.sql
3. 03-FullDummyData.sql
4. 04-Maintenance-Indexes-And-Views.sql
5. 05-PostDeployment-Checks.sql

## Notes

- Scripts are pure T-SQL (no SQLCMD `:r` dependency) and can run in standard SSMS/Azure Data Studio query mode.
- Script 01 validates university prerequisites and self-heals missing LMS discussion columns used by the current app model.
- Script 02 ensures deterministic university baseline entities (department/program/faculty/student profile) for Semester 1-8 flows.
- Script 03 seeds university semester coverage (Semester 1 to Semester 8) with report cards/results and applies completion markers after Semester 8 where schema supports it; also seeds attendance/enrollment/FYP support data.
- Script 04 adds university-focused lookup indexes for semester report-card/result, attendance, and FYP access patterns.
- Script 05 performs post-deployment coverage checks and prints warnings/info for any missing coverage.
