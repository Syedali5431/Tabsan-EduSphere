# College Scripts

This folder contains college-focused wrappers and seed scripts that follow the same ordering model as the main Scripts folder.

## Script Order

1. 01-Schema-Current.sql
2. 02-Seed-Core.sql
3. 03-FullDummyData.sql
4. 04-Maintenance-Indexes-And-Views.sql
5. 05-PostDeployment-Checks.sql

## Notes

- Scripts 01, 02, and 04 delegate to the shared root scripts to avoid duplication.
- Script 03 adds college class coverage (Class 11 and Class 12) with results and marks.
- Script 05 validates college class/result coverage.
