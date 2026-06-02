# User Acceptance Testing (UAT)

## UAT Synchronization Update (2026-06-02)

- Validate domain-specific script-pack path readiness for demo/UAT runs:
	- Scripts/School Scripts,
	- Scripts/College Scripts,
	- Scripts/University Scripts.
- Validate deterministic all-student data coverage in UAT evidence:
	- School: Class 1-10,
	- College: Class 11-12,
	- University: Semester 1-8.
- Add explicit UAT verification that University seeded lifecycle data includes:
	- enrollments,
	- attendance,
	- FYP projects.
- Include check that University strict post-deployment checks pass for attendance/FYP coverage.

## Purpose
User acceptance testing confirms the product behaves correctly from an operator perspective before deployment handoff.

## Role Coverage

- This UAT scope covers: Admin, Faculty, Student, and Finance.
- SuperAdmin flow testing is excluded from this cycle.
- Tabsan.Lic (license app) testing is excluded from this cycle.

## EduSphere UAT Steps (Admin/Faculty/Student)

1. Start the API and web application.
2. Sign in as an Admin user.
3. Confirm Admin navigation and role-based module access are correct.
4. Test user/timetable/report workflows available to Admin and confirm imports/exports (CSV, PDF, Excel) work.
5. Sign in as a Faculty user.
6. Validate Faculty flows for attendance, course access, assignments/quizzes, and results entry screens.
7. Sign in as a Student user.
8. Validate Student flows for dashboard, attendance view, courses/materials, and report-card/results visibility.
9. Validate user import templates in User Import Sheets:
	- faculty-admin-import-template.csv
	- students-import-template.csv
10. Run Scripts/01-Schema-Current.sql through Scripts/05-PostDeployment-Checks.sql in order for full deployment validation.

## Finance UAT Steps

1. Sign in as a Finance user.
2. Confirm Finance navigation shows Payments, Payment Reports, Analytics, and Theme Settings only.
3. Verify academic modules are blocked from the Finance account.
4. Open Payments and update an existing receipt in the correct campus scope.
5. Mark a payment as paid and confirm the paid/update trail fields change.
6. Open Payment Reports and apply campus, department, course, and class/semester filters.
7. Export the filtered payment report to PDF and Excel.
8. Open Finance Analytics and confirm the Paid vs Unpaid chart matches the filtered report totals.
9. Verify a Finance user with multi-campus assignment can only see receipts for assigned campuses.

## Acceptance Notes

- Import/export and index maintenance features are validated.
- User import templates are role-specific and aligned to current onboarding workflow.
- Finance payment workflows, report filters, and analytics scope are validated.
- Admin, Faculty, Student, and Finance role workflows are validated end-to-end.
- The unit-test build for the main application passed during this session.

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.
