# Tabsan EduSphere Database Scripts

This folder is intentionally kept DB-focused and now contains only database setup, seed, maintenance, and validation scripts.

## Script Set

| Order | File | Purpose |
| --- | --- | --- |
| 00 | 00-Cleanup-Master-Mistake.sql | One-time cleanup if legacy app tables were accidentally created in master. |
| 01 | 01-Schema-Current.sql | Creates/updates current database schema. |
| 02 | 02-Seed-Core.sql | Seeds core roles, modules, departments, baseline users, and access matrices. |
| 02A | Seed-Core-Clean.sql | Seeds clean baseline (no dummy/demo rows). |
| 03 | 03-FullDummyData.sql | Seeds high-volume dummy/demo data across School/College/University institutes. |
| 04 | 04-Maintenance-Indexes-And-Views.sql | Adds maintenance indexes and reporting views. |
| 05 | 05-PostDeployment-Checks.sql | Post-deployment checks for demo/full path. |
| 05A | 05-PostDeployment-Checks-Clean.sql | Post-deployment checks for clean path. |

## Default Seeded Credentials

For scripts that seed users (`02-Seed-Core.sql`, `Seed-Core-Clean.sql`, `03-FullDummyData.sql`):

- Password: EduSphere147

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
