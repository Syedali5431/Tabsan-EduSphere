# University Scripts

This folder contains university-focused wrappers and seed scripts that follow the same ordering model as the main Scripts folder.

## Script Order

1. 01-Schema-Current.sql
2. 02-Seed-Core.sql
3. 03-FullDummyData.sql
4. 04-Maintenance-Indexes-And-Views.sql
5. 05-PostDeployment-Checks.sql

## Notes

- Scripts 01, 02, and 04 delegate to the shared root scripts to avoid duplication.
- Script 03 adds university semester coverage (Semester 1 to Semester 8) with results and marks.
- Script 05 validates university semester/result coverage.
