# Database Script Execution Order

Run scripts in this exact order.

## Full Demo Path

1. 01-Schema-Current.sql
2. 02-Seed-Core.sql
3. 03-FullDummyData.sql
4. 04-Maintenance-Indexes-And-Views.sql
5. 05-PostDeployment-Checks.sql

## Clean Baseline Path

1. 01-Schema-Current.sql
2. Seed-Core-Clean.sql
3. 04-Maintenance-Indexes-And-Views.sql (optional but recommended for index/view parity)
4. 05-PostDeployment-Checks-Clean.sql

## Notes

- `01-Schema-Current.sql` must run from `master` because it creates/ensures database context.
- Other scripts run against `Tabsan-EduSphere` database.
- The core seed step uses `02-Seed-Core.sql` as the maintained entry point.
- Seeded user password in both core and dummy seed scripts: `EduSphere147`.
- Full demo dataset marker after successful script 03 run: `DemoDatasetVersion = FullDummyData-v43`.
- Full demo now includes mixed attendance timeline rows and results lifecycle rows (`Midterm`, published `Final`, and draft `FinalReview`) for training and validation.
- Script 04 now adds results lifecycle indexes for publish/draft-heavy query paths.
- Script 05 now validates attendance/results institution coverage and lifecycle status distribution checks.
- Script 05A now explicitly validates that clean baseline contains no attendance or results rows.
- Script 03 includes expanded tenant/campus-aware high-volume saturation across departments/programs, all seeded classes/semesters, enrollments, and semester-cycle payment receipts.
- Domain script packs are available for institution-focused execution:
  - `Scripts/School Scripts/` (Class 1-10 focused dummy path)
  - `Scripts/College Scripts/` (Class 11-12 focused dummy path)
  - `Scripts/University Scripts/` (Semester 1-8 focused dummy path)
- If legacy objects were accidentally created in `master`, run `00-Cleanup-Master-Mistake.sql` once before step 1.

## Example Commands

```powershell
$server = "YOUR_SERVER"

sqlcmd -S $server -d master -i Scripts/01-Schema-Current.sql
sqlcmd -S $server -d Tabsan-EduSphere -i Scripts/02-Seed-Core.sql
sqlcmd -S $server -d Tabsan-EduSphere -i Scripts/03-FullDummyData.sql
sqlcmd -S $server -d Tabsan-EduSphere -i Scripts/04-Maintenance-Indexes-And-Views.sql
sqlcmd -S $server -d Tabsan-EduSphere -i Scripts/05-PostDeployment-Checks.sql
```

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.
