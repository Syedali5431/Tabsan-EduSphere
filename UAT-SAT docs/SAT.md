# Site Acceptance Testing (SAT)

## SAT Synchronization Update (2026-06-10)

- Confirm MFA login bypass is active in deployed environment: username/password login succeeds without MFA challenge.
- Confirm TwoFactor Settings page loads and is accessible to authorized roles in the deployed portal.
- Confirm TwoFactor Setup, Verify, Disable, and Login Test flows function correctly in the deployed environment.
- Confirm TwoFactor Reset via recovery code works and provisions a new valid secret.
- Confirm security-profile endpoint returns correct MFA policy configuration for the deployed instance.
- Confirm login page renders MFA code input field and appropriate error messaging when MFA policy is active.
- Confirm all two-factor API endpoints (Setup, Verify, Disable, LoginVerify, Reset, ResendRecoveryCodes) respond correctly in the deployed environment.

## SAT Synchronization Update (2026-06-04)

- Include domain script-pack verification as part of SAT execution evidence:
	- Scripts/School Scripts,
	- Scripts/College Scripts,
	- Scripts/University Scripts.
- Confirm deterministic all-student demo coverage by institution mode:
	- School: Class 1-10,
	- College: Class 11-12,
	- University: Semester 1-8.
- For University SAT, additionally confirm seeded lifecycle rows exist for:
	- enrollments,
	- attendance_records,
	- fyp_projects.
- Confirm University post-deployment strict checks include attendance and FYP coverage assertions.
- **ISO 27001 + ISO 9001 Compliance SAT Verification (Phases 1-10)**:
	- Phase 1 Audit Logging: Confirm audit_logs table has all ISO columns (ActorRole, UserAgent, DeviceInfo, CorrelationId, Severity, EventCategory); confirm 4 ISO indexes exist; verify audit immutability enforcement active in deployed environment.
	- Phase 2 Security: Confirm users (LastPasswordChangedAt), password_history (ExpiresAt), user_sessions (LastActivityAt) columns present; verify password ageing policy configured; verify session idle timeout active; confirm IX_user_sessions_active filtered index deployed.
	- Phase 3 Login Activity: Confirm login_activity_logs table exists with sample data; verify 4 indexes (UserId, AttemptedAt, IsSuccess+AttemptedAt, IpAddress+AttemptedAt) deployed.
	- Phase 4 Backup & DR: Confirm backup_logs table exists with Full/Differential/Log records; verify IX_backup_logs_status_started and IX_backup_logs_type_started indexes; confirm backup API endpoints reachable.
	- Phase 5 Data Protection: Confirm users has ConsentToMonitoring and DataRetentionDate columns; verify data_classification_entries table populated; verify encryption and data masking services operational.
	- Phase 6 Incident Management: Confirm incident_logs table deployed with Open/Investigating/Resolved/Closed lifecycle; verify 3 indexes present; confirm incident API endpoints functional.
	- Phase 7 Document Management: Confirm policy_documents and policy_document_versions tables exist; verify version tracking and document status (Draft/Published/Archived) workflow; confirm unique index on (DocumentId, VersionNumber).
	- Phase 8 Backup Validation: Confirm backup_verification_logs table linked to backup_logs via FK; verify IntegrityCheck and RestoreTest records present.
	- Phase 9 Data Integrity: Confirm GET /api/v1/data-integrity/check endpoint returns findings for audit coverage, orphaned users, enrollment consistency, course/semester integrity.
	- Phase 10 Compliance Dashboard: Confirm GET /api/v1/compliance/dashboard returns 7-section aggregated compliance posture; verify dashboard data sourced from all Phase 1-8 tables.

## Purpose
Site acceptance testing verifies the deployed application behaves correctly in its target environment after deployment.

## Role Coverage

- This SAT scope covers: Admin, Faculty, Student, and Finance.
- SuperAdmin flow testing is excluded from this cycle.
- Tabsan.Lic (license app) testing is excluded from this cycle.

## EduSphere SAT Steps (Admin/Faculty/Student)

1. Deploy the API and web application to the target host.
2. Confirm the application starts cleanly and health checks are green.
3. Sign in as an Admin user and confirm role-based module visibility and access boundaries.
4. Validate core Admin operations (users, timetable, reports) and import/export paths.
5. Sign in as a Faculty user and validate attendance/course/assignment-result operational flows.
6. Sign in as a Student user and validate dashboard, attendance, courses/materials, and result/report-card access.
7. Confirm student lifecycle actions still function after deployment.
8. Validate user import templates in User Import Sheets:
	- faculty-admin-import-template.csv
	- students-import-template.csv
9. Test import/export (CSV, PDF, Excel) features for users and timetables.
10. Run Scripts/01-Schema-Current.sql through Scripts/05-PostDeployment-Checks.sql in order for site deployment verification.

## Finance SAT Steps

1. Sign in as a Finance user on the deployed environment.
2. Confirm Payments, Payment Reports, Analytics, and Theme Settings are available, and academic modules are hidden or blocked.
3. Open Payments and confirm the assigned-campus receipt list loads successfully.
4. Update a payment record and mark one receipt as paid.
5. Confirm the latest payment state and update trail are reflected after refresh.
6. Open Payment Reports and apply campus, department, course, and class/semester filters.
7. Export the filtered payment report to PDF and Excel.
8. Open Finance Analytics and confirm the Paid vs Unpaid chart matches the filtered payment scope.
9. Verify multi-campus Finance access remains restricted to assigned campuses only.

## Exit Criteria

- Deployed services respond successfully.
- Import/export and index maintenance features are validated.
- Role-specific user import templates are available and validated.
- Finance access boundaries, payment flows, and report filtering remain correct after deployment.
- Admin, Faculty, Student, and Finance role workflows pass in deployed environment.
- **ISO 27001 + ISO 9001 Compliance**: All Phase 1-10 tables, columns, and indexes confirmed deployed; ISO migration history entries verified; compliance dashboard accessible; backup/DR logging operational; incident management workflow functional; policy document version history intact; data classification scheme active; login activity monitoring recording all outcomes.

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.
