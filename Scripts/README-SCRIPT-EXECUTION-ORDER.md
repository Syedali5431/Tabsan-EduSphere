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
- Seeded user password in both core and dummy seed scripts: `EduSphere147`.
- Full demo dataset marker after successful script 03 run: `DemoDatasetVersion = FullDummyData-v6`.
- Script 03 includes high-volume semester saturation for enrollments and semester-cycle payment receipts across School/College/University data scopes.
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
