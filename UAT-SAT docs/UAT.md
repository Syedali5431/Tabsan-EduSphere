# User Acceptance Testing (UAT)

## UAT Synchronization Update (2026-06-10)

- Validate MFA-disabled login flow: sign in with username/password only and confirm access is granted without MFA prompt.
- Validate TwoFactor Settings page loads for authenticated admin/superadmin users.
- Validate TwoFactor Setup flow: begin setup, scan QR code, verify with TOTP code, confirm enabled status.
- Validate TwoFactor Disable flow: enter current TOTP code, confirm MFA is disabled.
- Validate TwoFactor Login Test: after setup, confirm login-verify hand-off works correctly.
- Validate TwoFactor Reset: use a recovery code to reset 2FA and confirm a fresh secret is provisioned.
- Validate security-profile endpoint returns correct MFA policy flags for the deployment.
- Validate login page renders MFA code input field when deployment MFA policy is active.
- Validate login page shows appropriate error messaging when MFA code is missing or invalid.

## UAT Synchronization Update (2026-06-04)

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
- **ISO 27001 + ISO 9001 Compliance UAT Coverage (Phases 1-10)**:
	- Phase 1 Audit Logging: Verify audit_logs has ActorRole, UserAgent, DeviceInfo, CorrelationId, Severity, EventCategory columns; audit immutability enforced.
	- Phase 2 Security: Verify users.LastPasswordChangedAt, password_history.ExpiresAt, user_sessions.LastActivityAt columns; password ageing policy active; session idle timeout enforced; IX_user_sessions_active filtered index present.
	- Phase 3 Login Activity Monitoring: Verify login_activity_logs table populated with sample success/failure attempts; 4 indexes present.
	- Phase 4 Backup & DR: Verify backup_logs table populated with Full/Differential/Log backup records; API endpoints for backup recording and status summary.
	- Phase 5 Data Protection: Verify users.ConsentToMonitoring, users.DataRetentionDate columns; data_classification_entries table with entity classification records; encryption/masking services available.
	- Phase 6 Incident Management: Verify incident_logs table with sample incidents (Open/Investigating/Resolved/Closed); status flow transitions correctly.
	- Phase 7 Document Management: Verify policy_documents + policy_document_versions tables; version tracking and access control functional.
	- Phase 8 Backup Validation: Verify backup_verification_logs table with IntegrityCheck and RestoreTest records linked to backup_logs.
	- Phase 9 Data Integrity: Verify GET /api/v1/data-integrity/check returns findings report covering audit coverage, orphaned users, enrollment consistency.
	- Phase 10 Compliance Dashboard: Verify GET /api/v1/compliance/dashboard returns 7-section aggregated compliance posture (Audit, Security, Backup, Incidents, Activity, Data Protection, Documents).

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
- **ISO 27001 + ISO 9001 Compliance**: All Phase 1-10 tables, columns, indexes verified; audit immutability confirmed; compliance dashboard operational; backup/DR logging active; incident management workflow validated; policy document versioning functional; data classification entries populated; login activity monitoring recording all login outcomes.

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.
