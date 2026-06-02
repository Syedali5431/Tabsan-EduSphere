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
This checklist covers user-acceptance flow for the main EduSphere app before deployment.

- Included roles: Admin, Faculty, Student, Finance.
- Excluded: SuperAdmin role tests and standalone Tabsan.Lic tests.

## Pre-Deployment Checks

| Area | Step | Expected Result |
|---|---|---|
| EduSphere app | Start the solution and open the login page | App starts without startup errors |
| EduSphere app | Sign in as Admin and verify navigation/permissions | Admin modules are available and restricted correctly |
| EduSphere app | Execute core Admin flows | User/timetable/report workflows complete successfully |
| EduSphere app | Sign in as Faculty and verify navigation/permissions | Faculty modules are available and restricted correctly |
| EduSphere app | Execute core Faculty flows | Attendance/course/assignment-result workflows complete successfully |
| EduSphere app | Sign in as Student and verify navigation/permissions | Student modules are available and restricted correctly |
| EduSphere app | Execute core Student flows | Dashboard/attendance/course/report-card flows complete successfully |
| EduSphere app | Sign in as Finance and verify navigation/permissions | Finance-only modules are visible and academic modules are blocked |
| EduSphere app | Execute core Finance flows | Payments/reports/analytics workflows complete successfully |
| EduSphere app | Validate `User Import Sheets/faculty-admin-import-template.csv` | File is accepted and fields map correctly for Admin/Faculty onboarding |
| EduSphere app | Validate `User Import Sheets/students-import-template.csv` | File is accepted and student onboarding fields are validated as expected |
| EduSphere app | Execute Scripts/01 through Scripts/05 in order | Schema, seed, dummy data, maintenance, and post-deployment checks complete successfully |

## Validation Results

- `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` succeeded with 130/130 tests passing.

## Exit Criteria

- Role-specific CSV import templates validate successfully.
- Admin, Faculty, Student, and Finance role workflows pass without blocking defects.
- The app runs without build or test failures.

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.
