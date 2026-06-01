# UAT Test Plan

## UAT Test Plan Synchronization Update (2026-06-02)

- Add deterministic domain script-pack checks:
	- run and verify School Scripts pack coverage (Class 1-10 for all scoped school students),
	- run and verify College Scripts pack coverage (Class 11-12 for all scoped college students),
	- run and verify University Scripts pack coverage (Semester 1-8 for all scoped university students).
- Add University lifecycle demo-data checks:
	- enrollment row presence for all scoped university students,
	- attendance row presence across semester coverage,
	- FYP row presence per scoped university student.
- Add University strict post-deployment verification:
	- semester report-card/results thresholds,
	- attendance threshold,
	- FYP per-student threshold.

## Scope
This checklist covers the user-acceptance flow for the main EduSphere app and the standalone Tabsan.Lic tool before deployment.

## Pre-Deployment Checks

| Area | Step | Expected Result |
|---|---|---|
| EduSphere app | Start the solution and open the login page | App starts without startup errors |
| EduSphere app | Verify institution policy endpoints are reachable | School, College, and University flags can be read and saved by SuperAdmin |
| EduSphere app | Upload a valid `.tablic` file | License activates successfully |
| EduSphere app | Upload a tampered `.tablic` file | Activation is rejected |
| EduSphere app | Test a license locked to a different domain | Activation is rejected on the wrong host |
| EduSphere app | Log in with a license that has `MaxUsers = 0` | Unlimited concurrent usage is allowed |
| EduSphere app | Validate `User Import Sheets/faculty-admin-import-template.csv` | File is accepted and fields map correctly for Admin/Faculty onboarding |
| EduSphere app | Validate `User Import Sheets/students-import-template.csv` | File is accepted and student onboarding fields are validated as expected |
| EduSphere app | Execute Scripts/01 through Scripts/05 in order | Schema, seed, dummy data, maintenance, and post-deployment checks complete successfully |
| Tabsan.Lic | Generate a single verification key | A raw key is shown once and the hash is stored |
| Tabsan.Lic | Generate a bulk batch of keys | Multiple keys are created with unique IDs |
| Tabsan.Lic | Build a `.tablic` file with School/College/University scope | Selected institution flags are embedded in the payload |
| Tabsan.Lic | Use 1 month / 1 year / 2 years / 3 years / Permanent expiry | The generated record reflects the selected expiry |
| Tabsan.Lic | Set max users and allowed domain | Values are persisted and written into the payload |

## Validation Results

- `dotnet build tools/Tabsan.Lic/Tabsan.Lic.sln` succeeded.
- `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` succeeded with 130/130 tests passing.

## Exit Criteria

- License generation works end-to-end.
- License activation updates the app policy and enforces the selected domain and user constraints.
- Role-specific CSV import templates validate successfully.
- The app and the license tool both run without build or test failures.

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.
