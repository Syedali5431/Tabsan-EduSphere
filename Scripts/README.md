# Tabsan EduSphere Database Scripts

This folder is intentionally kept DB-focused and now contains only database setup, seed, maintenance, and validation scripts.

## Script Set

| Order | File | Purpose |
| --- | --- | --- |
| 00 | 00-Cleanup-Master-Mistake.sql | One-time cleanup if legacy app tables were accidentally created in master. |
| 01C | 01-Schema.sql | Canonical compatibility alias that delegates to 01-Schema-Current.sql. |
| 01 | 01-Schema-Current.sql | Creates/updates current database schema. |
| 02C | 02-CoreSeed.sql | Canonical compatibility alias that delegates to 02-Seed-Core.sql. |
| 02 | 02-Seed-Core.sql | Seeds core roles, modules, departments, baseline users, and access matrices. |
| 02A | Seed-Core-Clean.sql | Seeds clean baseline (no dummy/demo rows). |
| 03 | 03-FullDummyData.sql | Seeds very high-volume dummy/demo data (v7) across School/College/University institutes with tenant/campus-aware program coverage, including all-semester offerings/enrollments, school/college class timetables, and institute-wide role users. |
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

Canonical alias path (equivalent):

```powershell
sqlcmd -S "localhost" -E -d "master" -i "Scripts\01-Schema.sql"
sqlcmd -S "localhost" -E -d "Tabsan-EduSphere" -i "Scripts\02-CoreSeed.sql"
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

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.
