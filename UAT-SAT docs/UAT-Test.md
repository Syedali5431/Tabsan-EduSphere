# UAT Test Plan

## UAT Test Plan Synchronization Update (2026-06-04)

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
- **ISO 27001 + ISO 9001 Compliance Test Coverage (Phases 1-10)**:
	- Phase 1 Audit Logging: Verify audit_logs schema includes ActorRole, UserAgent, DeviceInfo, CorrelationId (NVARCHAR 64), Severity (NVARCHAR 20 DEFAULT 'Info'), EventCategory (NVARCHAR 50); confirm 4 filtered indexes present; verify audit immutability blocks UPDATE/DELETE.
	- Phase 2 Security: Verify users.LastPasswordChangedAt (DATETIME2), password_history.ExpiresAt (DATETIME2), user_sessions.LastActivityAt (DATETIME2); confirm IX_user_sessions_active filtered index; verify password ageing (MaxPasswordAgeDays=90) and session idle timeout (IdleTimeoutMinutes=30).
	- Phase 3 Login Activity: Verify login_activity_logs table schema (UserId, Username, AttemptedAt, IpAddress, UserAgent, DeviceInfo, IsSuccess, FailureReason, RiskLevel, UserIsLockedOut); confirm 4 indexes; verify sample data includes success/failure/concurrency/MFA scenarios.
	- Phase 4 Backup & DR: Verify backup_logs table schema (BackupType, FileName, FilePath, FileSizeBytes, DurationSeconds, Status, StartedAt, CompletedAt, ErrorMessage, Checksum, InitiatedBy); confirm 2 indexes; verify 5 sample records (Full/Diff/Log).
	- Phase 5 Data Protection: Verify users.ConsentToMonitoring (BIT DEFAULT 0) and DataRetentionDate (DATETIME2); confirm data_classification_entries schema (EntityName, EntityId, ClassificationLevel, ClassifiedBy, ClassifiedAt, Justification); verify 6 classification records (Confidential/Restricted/Internal/Public).
	- Phase 6 Incident Management: Verify incident_logs schema (Title, Description, Severity, Category, Status, ReportedBy, ReportedAt, AssignedTo, ResolvedAt, Resolution); confirm 3 indexes; verify 5 sample incidents across Open/Investigating/Resolved/Closed states.
	- Phase 7 Document Management: Verify policy_documents (Title, Content, Version, Status, Category, AccessLevel) and policy_document_versions (DocumentId, VersionNumber, Content, ChangedBy, ChangeNotes) schemas; confirm unique index on (DocumentId, VersionNumber); verify 4 documents with version histories.
	- Phase 8 Backup Validation: Verify backup_verification_logs schema (BackupLogId FK, VerificationType, VerifiedAt, VerifiedBy, IsSuccessful, DurationSeconds, Issues, VerifiedChecksum); confirm 2 indexes; verify FK cascade from backup_logs.
	- Phase 9 Data Integrity: Verify GET /api/v1/data-integrity/check endpoint; confirm report includes: audit coverage check, orphaned active users, student profiles, course/semester integrity, unpublished results, dropped enrollments, pending modification requests.
	- Phase 10 Compliance Dashboard: Verify GET /api/v1/compliance/dashboard endpoint; confirm 7 dashboard sections (Audit, Security, Backup, Incidents, Activity, Data Protection, Documents); verify aggregated data matches source tables.

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
| ISO Compliance | Verify all Phase 1-10 migration blocks in 01-Schema-Current.sql | 8 migration blocks executed; all new tables (login_activity_logs, backup_logs, incident_logs, policy_documents, policy_document_versions, backup_verification_logs, data_classification_entries) created; all new columns added to audit_logs, users, password_history, user_sessions |
| ISO Compliance | Verify 03-FullDummyData.sql ISO data | 10 login_activity records, 5 backup_log records, 6 data_classification records, 5 incident_log records, 4 policy_document records, 5 version records, 5 backup_verification records present |
| ISO Compliance | Verify ISO indexes | 20+ ISO indexes created across all tables (filtered, composite, and covering indexes) |
| ISO Compliance | Run data integrity check API | GET /api/v1/data-integrity/check returns OK/Warning/Error findings with descriptive messages |
| ISO Compliance | Run compliance dashboard API | GET /api/v1/compliance/dashboard returns 7-section aggregated posture with data from all ISO tables |

## Validation Results

- `dotnet test tests/Tabsan.EduSphere.UnitTests/Tabsan.EduSphere.UnitTests.csproj -v minimal` succeeded with 130/130 tests passing.
- **ISO 27001 + ISO 9001 validation**: All Phase 1-10 schema objects verified; all dummy data records present; all indexes operational; API endpoints returning expected compliance data.

## Exit Criteria

- Role-specific CSV import templates validate successfully.
- Admin, Faculty, Student, and Finance role workflows pass without blocking defects.
- The app runs without build or test failures.
- **ISO Compliance Exit**: All 10 ISO phases validated — schema changes present, dummy data populated, indexes active, compliance APIs functional, audit trail complete.

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.
