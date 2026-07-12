<!-- markdownlint-disable MD007 MD010 MD012 MD022 MD024 MD026 MD032 MD041 MD060 -->

## 2026-07-12 Update — Phase 3 Medium Severity Fixes
### Implementation sync
- Change-password feature verified: fully functional with current/new/confirm password flow under User Settings, backed by PUT api/v1/auth/change-password.
- Degree Rules page no longer redirects to Dashboard; renders normally with HTTP 200 for all authorized access paths, including when role or capability checks prevent data loading.
- ISO Compliance, Backup & DR, and Document Management modules registered in ModuleRegistry as SuperAdmin-only with corresponding sidebar menu entries.
- PRD terminology corrected: all "subjects" replaced with "courses", "Create User" references removed, CSV import kept as the only user creation method.
### Validation sync
- Build: 0 errors. DegreeRules GET returns View instead of RedirectToAction. All 3 compliance modules visible in ModuleRegistry.All().

## 2026-07-12 Update — Phase 4 Low Severity Fixes
### Implementation sync
- Profile-picture upload verified as fully functional: JPG/PNG/JPEG validation, 2MB limit, preview, replace, fallback initial-letter avatar in navbar.
- 15 demo payment receipts added to seed data with mixed statuses (Paid, Pending, Overdue) across graduated and regular students.
- Sidebar role visibility SQL script (07-Fix-Sidebar-Role-Visibility.sql) aligned with Sidebar-Menu-Purpose.csv for Admin, Faculty, Student, and Finance roles.
- Admin menu now includes missing: timetable_student, lookups, attendance, quizzes, fyp, ai_chat, degree_audit, graduation_eligibility, degree_rules, graduation_apply, graduation_applications, library_config, accreditation.
- Faculty menu expanded to match CSV: added dashboard, students, courses, assignments, attendance, quizzes, fyp, ai_chat, payments, enrollments, prerequisites, degree_audit, graduation_eligibility, degree_rules, library_config, accreditation, user_import, programs.
- Student menu expanded: added dashboard, timetable_student, assignments, attendance, quizzes, fyp, ai_chat, enrollments, gradebook, degree_audit, announcements, accreditation, generate_certificates, course_material, discussion; removed incorrect student_lifecycle.
### Validation sync
- Build: 0 errors. All PaymentReceipt + ModuleRegistry unit tests pass (8/8).
- Phase 4 is complete and ready for Phase 5.

## 2026-07-12 Update — Phase 0 Guardrails Established
### Implementation sync
- Phase 0 was completed as a documentation-only safeguard for the issue-resolution rollout.
- The existing creation order, POST endpoint boundaries, and protected working modules were explicitly locked to prevent scope creep.
- No production routes, modules, or working workflows were changed during this phase.
### Validation sync
- Scope review completed successfully.
- Phase 0 is stable and ready for implementation work in later phases.

## 2026-07-12 Update — Phase 1 Building Creation Flow Hardened
### Implementation sync
- Building creation now validates required name/code input before persistence.
- Tenant and campus scope are propagated from the portal form into the create request and API service layer.
- The flow now avoids creating invalid orphaned building rows during the first step of the academic lifecycle.
### Validation sync
- Focused regression test passed for blank-name rejection.
- Phase 1 is complete and scoped to the building create issue only.

## 2026-07-12 Update — Phase 2 Semester Range & Idle-Timeout Fixes
### Implementation sync
- The university semester dropdown now uses the selected program’s configured total-semester range instead of an oversized fallback list.
- Session refresh now rejects sessions that exceed the configured idle-timeout window before issuing a new token pair.
### Validation sync
- Targeted regression tests passed for both the academic-level range helper and the refresh-timeout path.
- Phase 2 is complete and ready for the next implementation phase.

## 2026-06-22 Update — Profile Picture Upload & Graduated Demo Students
### Implementation sync
- Users can upload a profile picture (JPG, JPEG, PNG, max 2MB) from the User Settings page.
- ProfilePicturePath (nvarchar 500, nullable) added to users table via EF migration.
- Files stored in wwwroot/uploads/profile-pictures with unique GUID-based names.
- Navbar header avatar displays profile picture as circular image (30-40px); falls back to initial letter.
- User Settings dropdown link added to profile menu in navbar.
- Five graduated demo students added to 03-FullDummyData.sql (BSCS, BBA, Spanish, School, College) with:
  - Mid-term and final exam results (ResultType = 'Mid' and 'Final')
  - Quiz attempts with scores
  - FYP projects with marks and GPA (BSCS/BBA only)
  - Graduation applications (Status = Approved)
- Grading rules: School/College use percentage (90+=A+, 80+=A, 70+=B, 60+=C, 50+=D, <50=F); University semester-based uses GPA.
- Post-deployment checks updated: graduated student count, graduation applications, ProfilePicturePath column existence.
### Validation sync
- Build: 0 errors. All 5 graduated students have complete marks for certificate generation.

## 2026-06-18 Update — MFA TOTP Implementation (Otp.NET 1.4.1)
### Implementation sync
- Replaced manual HMACSHA1 TOTP with Otp.NET 1.4.1. TotpService uses OtpNet.Totp.VerifyTotp() with VerificationWindow.
- GenerateSecret uses Base32Encoding.ToString(), BuildProvisioningUri generates otpauth:// URIs.
- TwoFactorController: setup, status, verify, disable, enable, reset-setup, login-verify endpoints.
- Login flow validates MFA codes + recovery codes via ITwoFactorStateStore in AuthService.LoginAsync.
- QRCodeService generates PNG data URLs for authenticator enrollment. Web UI: TwoFactorSettings.cshtml.
- MfaSettings: TotpDigits=6, StepSeconds=30, DriftWindows=1, RecoveryCodeCount=8.
### Validation sync
- Build: 0 errors. Wrong code rejected (400). Valid code accepted. 2FA status correct.

## 2026-06-15 Update — Professional Certificate Templates & Institution-Type Scoring\n### Implementation sync\n- Certificate templates fully redesigned with professional DOCX formatting: double borders, navy+gold brand colors, Georgia/Calibri fonts.\n- All 4 certificate types now professional: Degree (University), Transcript, Completion Certificate (School/College), Report Card.\n- File naming convention: {RegNo}-{Type}.docx (e.g., COL-REG-11-06-Transcript.docx) with sanitized file names.\n- Institution-type scoring fixed: University uses GPA/CGPA (4.0), School and College use Percentage (0-100%).\n- Completion eligibility: School requires CurrentSemesterNumber≥10, College requires published results in BOTH Class 11 and Class 12.\n- Degree restricted to University only with published results requirement.\n- LibreOffice PDF adapter with auto-detection and NoOp fallback.\n- TemplateProcessorService course tables enhanced with alternating row colors, dark navy headers, semester grouping.\n\n### Validation sync\n- Build: 0 errors. All certificate types generated and verified.\n- School student col11s6: Transcript ✓, Completion ✓, ReportCard ✓, Degree correctly blocked (400).\n\n## 2026-06-14 Update — Phase 8 Cascading Filter System (Final Check)
### Implementation sync
- Created wwwroot/js/cascading-filters.js — shared AJAX cascade component with data-cascade attributes.
- Added institution type filter to Attendance.cshtml and Results.cshtml (before department in filter order).
- Added data-cascade attributes for Institute → Department → Course → Semester/Class cascade.
- Added dynamic period labels via data-period-label/data-period-placeholder (Semester vs Class).
- Added cascading-filters.js include to Assignments.cshtml.
- Added AvailableInstitutionTypes and SelectedInstitutionType to AttendancePageModel and ResultsPageModel.
- Populated institution type filter in PortalController.RenderAttendanceAsync and RenderResultsAsync from capability matrix.

### Validation sync
- Web build: 0 errors.
- Institution → Department → Course → Semester/Class cascade order enforced.
- Progressive disclosure: child filters disabled until parent selected.
- Dynamic labels: resolvePeriodLabel() returns "Semester" for University, "Class" for School/College.
- Report views already had institution filtering from Phase 5.

## 2026-06-14 Update — Phase 7 FYP Result Entry & Transcript Integration (Final Check)
### Implementation sync
- Added FypGradePoint (decimal 5,2), FypMarks (decimal 7,2), FypMaxMarks (decimal 7,2) nullable fields to FypProject entity.
- Updated SetFinalResult to accept optional gradePoint, marks, maxMarks parameters.
- Updated EnterFypResultRequest DTO with optional grade fields.
- Updated FypProjectSummaryResponse and FypProjectDetailResponse to include grade fields.
- Updated FypService.ToSummary/ToDetail mappers for new grade fields.
- Updated FypService.EnterResultAsync to pass grade parameters.
- Updated EF configuration (FypProjectConfiguration) with decimal column type mappings.
- Updated Web client (IEduApiClient + FypApiDto) with grade field support.
- Updated Web model (FypProjectItem) with grade properties.
- Integrated FYP results into transcript via ReportRepository.GetTranscriptDataAsync — FYP rows appended with "FYP" course code.
- Removed duplicate files: FypProjectStatus.cs (duplicate types) and ProposeProjectRequest.cs (duplicate DTOs).

### Validation sync
- API build: 0 errors. Web build: 0 errors, 0 warnings.
- FYP result entry now accepts numeric grades alongside result text.
- Transcript displays FYP results after course results with grade point column populated.
- University-only enforcement unchanged — ModuleDescriptor.AllowedTypes = [University] + student eligibility check.
- FYP grade fields available for GPA/CGPA recalculation.

## 2026-06-14 Update — Phase 6 Role-Based Sidebar Menu Restrictions (Final Check)
### Implementation sync
- Verified 5-layer filtering pipeline in SidebarMenuController: role-based DB filter → institution policy → module activation → permission annotation.
- Verified portal guard (OnActionExecutionAsync) with ActionMenuKeyMap (40+ entries) — fail-closed enforcement.
- Verified FinanceBlockedAcademicMenuKeys (25+ keys) — Finance users blocked from all academic actions.
- Verified ShouldRestrictToFacultyAdminAcademicRoles and ShouldRestrictToFinanceAdminPaymentsRoles guards.
- Verified IPermissionService: SuperAdmin gets PermissionFlags.All; other roles get PermissionFlags.None by default.
- Verified Layout dynamic menu rendering with group sections (Overview/Setup/Faculty/Student/Academic/Financial/Settings).
- Verified SidebarSettings.cshtml admin page for SuperAdmin runtime role assignment management.
- Verified InstitutionContextMiddleware per-request policy resolution.
- Verified ModuleLicenseEnforcementMiddleware for module-level 403 blocking.
- No code changes required — architecture fully implemented in prior phases.

### Validation sync
- DB seed (09-Restructure-Sidebar-Menu.sql): 58 menu items, 155 role access records. Verified in Phase 1 post-deployment checks.
- SuperAdmin: 58 menus (all). Admin: 46 (all except 12 SuperAdmin-only settings). Faculty: 25. Student: 20. Finance: 6.
- Sidebar guard: Any action not in visible menu set → redirected to Dashboard with access denied message.
- SuperAdmin bypasses all sidebar and permission checks.
- University-only menus (7 total) correctly hidden when institution policy excludes University.
- Module-gated menus (ai_chat) correctly hidden when module is inactive.
- Finance users: only 6 menus visible; portal guard enforces FinanceBlockedAcademicMenuKeys for direct URL access.
### Implementation sync
- Added 18 export method signatures to IReportService: Enrollment/SemesterResults/LowAttendance/FYP Excel/CSV/PDF + GPA CSV/PDF + Transcript CSV/PDF.
- Implemented all 18 export methods in ReportService using BuildExcelBytes (ClosedXML), BuildCsvBytes, BuildPdfBytes (QuestPDF).
- Added 24 new API export endpoints in ReportController covering all missing formats for 6 report types.
- Added 20+ Web client proxy methods in IEduApiClient for all export formats.
- Added 18 export proxy actions in PortalController for all new export formats.
- Added license-based catalog filtering: ReportCenter action filters out fyp_status and gpa_report when license excludes University.
- Added ResolveLicensedInstitutionTypesAsync helper using portal capability matrix.
- Added Excel/CSV/PDF export buttons to 6 report views: ReportGpa, ReportEnrollment, ReportSemesterResults, ReportTranscript, ReportLowAttendance, ReportFypStatus.

### Validation sync
- API build: 0 errors. Web build: 0 errors, 0 warnings.
- All 9 report types now have complete Excel + CSV + PDF export (API, Web client, UI).
- Institution-type aware catalog filtering: School/College users don't see University-only reports (FYP Status, GPA Report).
- Export buttons visible on all report views; consistent styling (green Excel, outline-green CSV, outline-red PDF).
- Existing integration tests cover catalog access, attendance/result/assignment/quiz/payment export content types and filenames.

## 2026-06-12 Update — Phase 4 Sidebar Menu Data Audit (Final Check)
### Implementation sync
- Audited all 28+ database tables for row counts against 43 sidebar menu keys.
- Verified 87 `.cshtml` views exist in `Views/Portal/` — all menus have pages.
- 58 sidebar menu items active with 155 role access records.
- 17 tables have data; 14 tables have 0 demo rows.

### Validation sync
- Data-rich menus: Students (330), Courses (124), Attendance (7300), Results (1070), FYP (20), Departments (4), Programs (6), Timetables (20) — fully functional.
- Settings menus: Sidebar Settings, Institution Policy, Tenant/Campus Management, Admin Users — fully functional.
- Empty menus (0 rows): Assignments, Quizzes, Buildings, Rooms, Enrollments, Notifications, Payments, Study Plans, Announcements, Discussions, Support Tickets, Rubrics, Prerequisites, Timetable Entries.
- Missing DB table: course_materials (API-driven file storage).
- Recommendation: Extend 03-FullDummyData.sql to seed the 14 empty tables.

## 2026-06-12 Update — Phase 3 MFA Verification (Final Check)
### Implementation sync
- No code changes required — MFA system fully implemented.
- TOTP: RFC 6238 compliant, HMAC-SHA1, 6 digits, 30s steps, ±1 drift (90s window). Compatible with Gmail/MS/Authy.
- Enrollment: TwoFactorSettings page → BeginTwoFactorSetup (Base32 secret + QR) → VerifyTwoFactorSetup (validate code).
- Login: AuthService.LoginAsync enforces MFA only when user has individually enabled it. 400 MFA_CODE_REQUIRED / 401 INVALID_MFA_CODE.
- Recovery codes: SHA-256 hashed, one-time use in AuthService.TryConsumeRecoveryCodeHash.
- Disable: soft-disable preserves secret for re-enable. Reset: HardDeleteAsync permanently removes all MFA data.
- Secret storage: raw Base32 in users.MfaTotpSecret; TwoFactorStateStore.TryUnprotect with Base32 fallback.

### Validation sync
- Full API build succeeded. All MFA endpoints verified: setup, verify, disable, reset, resend-recovery-codes.
- AuthService login paths verified: 400 (no code), 401 (invalid code), 200 (valid TOTP + recovery code).
- Base32 raw secret format survives Data Protection key rotation.
- No demo users have MFA enabled (expected — users enable individually).

## 2026-06-12 Update — Phase 2 License-Based Institute Enforcement (Final Check)
### Implementation sync
- Institution policy: seeded `institution_include_school`, `institution_include_college`, `institution_include_university` in `portal_settings` (all enabled for dev).
- 02-Seed-Core.sql: added institution policy seeding block with IF NOT EXISTS guards.
- SidebarMenuController.cs: added `study_plan` to `UniversityOnlyMenuKeys` (was missing — study plan is semester-based, university-only).
- Architecture verified: `InstitutionPolicyService` → `portal_settings` → `InstitutionPolicySnapshot` → sidebar filter + capability matrix → Web dropdown options.
- Module-level gating: `ModuleDescriptor.AllowedTypes` restricts `fyp` to University only.

### Validation sync
- DB: institution policy settings confirmed (all 3 types enabled).
- API build succeeded after `study_plan` addition.
- University-only menus (7 total): `degree_audit`, `graduation_eligibility`, `degree_rules`, `graduation_apply`, `graduation_applications`, `fyp`, `study_plan` hidden when `IncludeUniversity=false`.
- Certificate types: University → Degree + Transcript; School/College → Completion + Report Card.
- Period labels: "Semester" for University, "Class" for School/College.

## 2026-06-12 Update — Phase 1 DB Script Validation (Final Check)
### Implementation sync
- BBA department InstitutionType: fixed from 2 (College) to 0 (University) via direct DB UPDATE.
- sidebar_menu_items: populated from empty state via updated 09-Restructure-Sidebar-Menu.sql (58 menu items, 5 roles).
- 09-Restructure-Sidebar-Menu.sql: fixed `rubric_management` → `rubric_manage` key mismatch. Added 8 missing menus: `lookups`, `payments`, `report_center`, `helpdesk`, `ai_chat`, `analytics`, `system_settings`, `admin_users`. Updated role access matrix (SuperAdmin=58, Admin=46, Faculty=25, Student=20, Finance=6).
- 05-PostDeployment-Checks.sql: updated tenant count expectation from 3 → 4 (DEFAULT tenant is intentional).
- Docs/Final-check.md: created with 11-phase validation plan, documentation sync policy, and Phase 1 results.

### Validation sync
- Post-deployment checks: 0 failures (down from 3 initial failures).
- BBA InstitutionType: confirmed 0 (University). ✓
- Sidebar generate_certificates menu: 1 item confirmed. ✓
- Tenant count: 4 (DEFAULT + TABSAN-SCH/COL/UNI). ✓
- All 363 users (1 SuperAdmin, 3 Admin, 20 Faculty, 333 Student, 6 Finance) verified active.
- Core login users (13) all active with correct roles.
- Known gaps identified: `course_materials` table missing from schema; `study_plans` and `payment_receipts` have 0 demo rows; timetable/course-material demo GUID checks reference non-existent GUIDs.

## 2026-06-10 Update — MFA Login Fix, Report Columns, Active-Only Filters, Session Timeout
### Implementation sync
- MFA: single-step TOTP validation restored; MFA enforced only when user has individually enabled it. AuthService.LoginAsync returns 400 MFA_CODE_REQUIRED (missing code) or 401 INVALID_MFA_CODE (wrong code).
- TwoFactor: Base32 raw-secret storage in TwoFactorStateStore survives Data Protection key rotation.
- Tenants/Campuses: deactivated tenants hidden from dropdowns via .Where(t => t.IsActive); CampusController defaults to activeOnly=true.
- Session timeout: idle timeout reduced from 30min to 5min (AuthSecurityOptions.IdleTimeoutMinutes).
- Reports: ProgramName + DepartmentName columns added to Attendance, Result, Assignment, and Quiz summary DTOs via left-join on academic_programs.
- Reports: all report endpoints now run without requiring department/course filter (Faculty/Admin scope enforcement relaxed).
- Semester sorting: changed from descending to ascending by StartDate.
- BBA department: InstitutionType corrected from College(2) to University(0).
- Duplicate DTO files consolidated (ReportCatalogItemResponse.cs removed).

### Validation sync
- Build succeeded for API and Web projects.
- Login flow verified: 400 MFA_CODE_REQUIRED, 401 INVALID_MFA_CODE, 200 with correct TOTP.
- Report endpoints verified to return data without filter selection.
- Semester list verified ascending (Semester 1 before Semester 2).
### Implementation sync
- AuthService.LoginAsync: MFA enforcement temporarily disabled on password login to unblock access while the two-factor secret-compatibility issue is resolved.
- TwoFactorStateStore.TryUnprotect: Added backward-compatible Base32 fallback so raw TOTP secrets stored without Data Protection encryption are accepted during login verification.
- TwoFactorStateStore: Added HardDeleteAsync for permanent MFA removal and IsValidBase32Secret syntactic validation.
- ITwoFactorStateStore: Extended with HardDeleteAsync contract.
- TotpService: Base32Encode/Base32Decode exposed for TOTP secret provisioning and HMAC key derivation.
- TwoFactorSetupService: Added ResetAsync for recovery-code-based 2FA reset.
- TwoFactorController: Added Reset and ResendRecoveryCodes endpoints.
- LoginController (Web): Updated to forward MFA code to API and surface MFA-required error messaging.
- PortalController (Web): Updated with TwoFactorSettings, BeginTwoFactorSetup, VerifyTwoFactorSetup, DisableTwoFactor, TestTwoFactorLogin portal actions.
- IEduApiClient (Web): Added GetSecurityProfileAsync, BeginTwoFactorSetupAsync, VerifyTwoFactorSetupAsync, DisableTwoFactorAsync, TestTwoFactorLoginAsync client methods.
- ApiConnectionModel (Web): Extended with MfaEnabled and MfaCode fields for portal-side state.
- TwoFactorApiModels (Web): Added DTOs for 2FA setup, verify, disable, and login-verify API calls.
- Login/Index.cshtml: Updated to conditionally render MFA code input when deployment policy indicates MFA is enabled.
- TwoFactorSettings.cshtml: New portal page for 2FA enrollment, disable, and login-test workflows.
- appsettings.Development.json: MFA configuration set to Enabled=true, RequireForPasswordLogin=true, RequireForPrivilegedRolesOnly=true.

### Validation sync
- Build succeeded for API and Web projects.
- API endpoint verification confirmed security-profile and login endpoints respond correctly.
- Login flow confirmed working with password-only authentication after MFA bypass.
### Implementation sync: Compliance dashboard aggregating 7 sections (Audit, Security, Backup, Incidents, Activity, Data Protection, Documents) from all previous phases. Single read-only API endpoint. No schema changes.
### Validation sync: Build succeeded. All 10 phases complete — ISO 27001 + ISO 9001 instrumented.

## 2026-06-04 Update - ISO Phase 9 Completion (Data Integrity)
### Implementation sync: Data integrity verification service checking 7 areas (audit coverage, orphaned users, students without profiles, stale offerings, draft results, pending modifications). No schema changes.
### Validation sync: Build succeeded. Service-only implementation.

## 2026-06-04 Update - ISO Phase 8 Completion (Backup Validation)
### Implementation sync: Created backup_verification_logs table for integrity checks and restore tests (ISO 27001 A.17.1.3). 3 API endpoints.
### Validation sync: Build succeeded, migration generated.

## 2026-06-04 Update - ISO Phase 7 Completion (Document Management)
### Implementation sync: Created policy_documents + policy_document_versions tables with version tracking and Draft→Published→Archived lifecycle. Read-all access, write restricted to Admin. Version history via immutable entries.
### Validation sync: Build succeeded. EF migration (PhaseISO7DocumentManagement) — 2 new tables.

## 2026-06-04 Update - ISO Phase 6 Completion (Incident Management)

### Implementation sync
- Created incident_logs table with severity/category/status lifecycle (Open→Investigating→Resolved→Closed).
- IncidentLog entity with domain methods: StartInvestigation, Resolve, Close, Reopen.
- Admin API: CRUD incidents, status transitions, summary with counts by status.

### Validation sync
- Build succeeded. EF migration (PhaseISO6IncidentManagement) — new table + 3 indexes.

## 2026-06-04 Update - ISO Phase 5 Completion (Data Protection)

### Implementation sync
- Implemented AES-256-CBC encryption service with PBKDF2 key derivation (Encrypt/Decrypt).
- Implemented data masking service for PII in UI (MaskEmail, MaskPhone, MaskName).
- Created data_classification_entries table for ISO 27001 A.8.2.1 compliance (Public/Internal/Confidential/Restricted).
- Added GDPR fields to users: ConsentToMonitoring, DataRetentionDate with domain methods.
- Admin API: encrypt, decrypt, mask demo, and classification CRUD.

### Validation sync
- Full solution build succeeded. EF migration generated (PhaseISO5DataProtection). All additive.

## 2026-06-04 Update - ISO Phase 4 Completion (Backup & DR)

### Implementation sync
- Created backup_logs table tracking all backup operations with type, file, size, duration, status, checksum.
- BackupLog entity includes lifecycle methods: MarkCompleted, MarkFailed, MarkVerified.
- Admin API: GET /backup/logs (paged history), POST /backup/logs (record start), PUT /backup/logs/{id} (update status), GET /backup/status (latest summary per type).
- External backup scripts integrate via API — record start then update status to Completed/Failed/Verified.

### Validation sync
- Full solution build succeeded. EF migration generated (PhaseISO4BackupDR).

## 2026-06-04 Update - ISO Phase 3 Completion (User Activity Monitoring)

### Implementation sync
- Created dedicated login_activity_logs table with structured columns for every login attempt (UserId, Username, IP, UserAgent, DeviceInfo, IsSuccess, FailureReason, RiskLevel, UserIsLockedOut).
- Integrated activity recording into AuthService.LoginAsync — all 8 outcomes (success + 7 failure paths) fire-and-forget.
- ConcurrencyLimitReached path now has an audit trail (was previously missing).
- Added admin API: GET /login-activity (paged + filters) and GET /login-activity/summary (daily breakdown, top failure reasons, top IPs).

### Validation sync
- Full solution build succeeded.
- EF migration generated (PhaseISO3LoginActivity) for new table with 4 indexes.

## 2026-06-04 Update - ISO Phase 2 Completion (Security)

### Implementation sync
- Password ageing policy added — LastPasswordChangedAt tracked on users; login returns PasswordExpired flag when password exceeds max age (90 days default).
- Password history entries now include ExpiresAt (2x max age) for future archival/pruning.
- Session idle timeout enforcement — LastActivityAt tracked on user_sessions; refresh rejects idle sessions beyond configured timeout (30 min default).
- Admin session management screen — list all active sessions, force-revoke individual sessions, revoke all sessions for a user.
- Password change paths (self-service, forced, admin reset) now record expiry on history entries.

### Validation sync
- Full solution build succeeded after implementation.
- EF migration generated (PhaseISO2Security) for 3 new columns + 1 filtered index.
- No regression in existing auth flows, session management, or password change behavior.

## 2026-06-03 Update - Institute Dynamic Model Phase 4 Continuation (Attendance)

### Implementation sync
- Attendance write operations now support School/College flows without mandatory semester/class filter input.
- Attendance UI readiness logic and period labeling now align with dynamic class/year/semester behavior.

### Validation sync
- Diagnostics checks report no errors in touched attendance controller/model/view files.

## 2026-06-03 Update - Institute Dynamic Model Phase 4 Progress (Results and Course Material)

### Implementation sync
- Result write operations now support School/College flows without mandatory semester/class filter input.
- Results UI readiness logic no longer blocks writes solely due to empty semester/class filter.
- Results UI now uses runtime period labels (`Class`/`Year`/`Semester`) inferred from available period options.
- Course Material create flow now supports optional period selection with safe fallback period resolution.

### Validation sync
- Diagnostics checks report no errors in touched Portal controller/model/view files.

## 2026-06-02 Update - Institute Dynamic Model Phase 3 Completion (UI and UX Adaptation)

### Implementation sync
- Completed Phase 3 UI/UX adaptation for institute-aware period semantics.
- University user-facing period labels now adapt between `Semester` and `Year` by configured period naming metadata.
- School/College user-facing period labels remain `Class` in lifecycle and key filter pages.
- Course creation UX now suppresses semester-first controls for School/College department selections.
- Course Material and Results pages now render dynamic period labels/placeholders in filters/tables/prompts.

### Validation sync
- Diagnostics checks report no errors in touched lifecycle/course/course-material/result files.
- Tenant/campus scoped behavior remains unchanged while period semantics now reflect institute context.

## 2026-06-02 Update - Institute Dynamic Model Phase 2 Completion (Academic-Level Structure)

### Implementation sync
- Completed Phase 2 lifecycle academic-level structure adjustments for institute-specific behavior.
- Student Lifecycle now enforces:
	- School mode => Class 1 to Class 10,
	- College mode => Class 11 to Class 12,
	- University mode => dynamic upper level inferred from configured semester/year metadata.
- University lifecycle range no longer uses fixed max-level constants.
- Tenant/campus scope behavior remains unchanged.

### Validation sync
- Diagnostics checks report no errors in touched lifecycle controller code.
- Runtime range clamping and option rendering now align with institute-specific academic boundaries.

## 2026-06-02 Update - Institute Dynamic Model Phase 1 Completion (Runtime Stability and Compatibility)

### Implementation sync
- Completed Phase 1 critical runtime fixes for institute-dynamic rollout.
- Domain script packs are now executable as pure T-SQL without SQLCMD dependency for:
	- School Scripts,
	- College Scripts,
	- University Scripts.
- LMS crash path from discussion-thread schema mismatch is mitigated with self-healing column guards in domain schema scripts.
- Result modification approval flow now supports legacy/new payload key shapes and avoids key-lookup runtime exceptions.
- Student Lifecycle and LMS entry paths now include safer institute-aware/default offering behavior while preserving existing university flow.

### Validation sync
- SQLCMD dependency scan confirms no `:r` directives remain in domain script packs.
- Diagnostics checks report no errors in touched scripts and portal-controller updates.
- Runtime parsing path in result approval now uses guarded extraction and fallback keys.

## 2026-06-02 Update - Password Hardening and Module-Sidebar Settings Parity

### Implementation sync
- Force-change-password workflow is now hardened end-to-end:
	- current (old) password is mandatory,
	- new password must satisfy strict safe-password policy rules,
	- validation messaging is aligned across API and portal.
- Password policy baseline now enforces stronger composition and pattern safety checks for user-initiated password changes.
- Sidebar settings parity is synchronized with module composition for recently expanded keys:
	- timetable admin/teacher/student,
	- advanced audit,
	- degree audit companion keys,
	- course-material/discussion/announcements/study-plan governance keys.
- Database seeding path now includes missing sidebar/menu upserts and role-access defaults so settings screens consistently expose configured features.

### Validation sync
- Force-change-password runtime checks confirm old-password validation and policy-rule enforcement before update.
- Sidebar settings and module composition checks confirm expected key availability and role visibility alignment.
- Settings and sidebar integration smoke tests remain green after parity synchronization.

## 2026-06-01 Update - Payments Student Scope Filter Stabilization and Demo Seed v43

### Implementation sync
- Payments student option loading now respects selected institution type even when tenant/campus are not selected (superadmin all-scope path).
- Scripts/03-FullDummyData.sql advanced to `FullDummyData-v43` and now adds deterministic student-scope payment demo receipts:
	- `RCPT-DEMO-PAY-SCP-U-043`
	- `RCPT-DEMO-PAY-SCP-C-043`
	- `RCPT-DEMO-PAY-SCP-S-043`
- Scripts/05-PostDeployment-Checks.sql now includes synchronized v43 marker and scope-demo receipt institution-alignment checks.

### Validation sync
- Payments menu runtime checks confirm institution/student filter narrowing shows only scope-correct rows.
- Deterministic v43 scope-demo receipts appear in the correct institution and student slices only.

## 2026-06-01 Update - Payments Filter Demo Seed v42 and CSV Import Reliability

### Implementation sync
- Payments menu institution filter now uses payment-specific licensed option mapping so labels align with the underlying scoped payment dataset.
- Payments CSV import now accepts common spreadsheet due-date formats (`yyyy-MM-dd`, `dd/MM/yyyy`, `MM/dd/yyyy`, and dash variants).
- Scripts/03-FullDummyData.sql advanced to `FullDummyData-v42` and adds deterministic payment filter-demo receipts:
	- `RCPT-DEMO-PAY-FLT-U-001` (University)
	- `RCPT-DEMO-PAY-FLT-C-001` (College)
	- `RCPT-DEMO-PAY-FLT-S-001` (School)
- Scripts/05-PostDeployment-Checks.sql now includes synchronized v42 checks for payment demo marker and per-institution filter-demo receipt integrity.

### Validation sync
- Payments menu runtime checks confirmed institution-filter and student-filter narrowing now show the expected deterministic rows.
- Payments CSV import checks confirmed spreadsheet-style due-date values no longer fail date validation.
- SQL checks confirmed deterministic payment filter-demo receipt presence and institution alignment.

## 2026-06-01 Update - Study Plan Filter Demo Seed and Runtime Scope/Serialization Stabilization

### Implementation sync
- Study Plan page flow now preserves selected scope context (tenant, campus, department, student) across list/detail/create/add/remove/delete/advise actions.
- Study Plan filter loading now supports selected-student and department fallback behavior for stable page-load rendering in scoped demo sessions.
- Study Plan payload normalization now handles numeric enum serialization from API responses:
	- advisor status,
	- course type,
	so web rendering is resilient when API sends enum numbers instead of strings.
- Scripts/03-FullDummyData.sql now adds deterministic Study Plan demo rows for three institution slices (University CS, College Commerce, School Math) with idempotent insert/update behavior.
- Scripts/05-PostDeployment-Checks.sql now includes deterministic Study Plan demo validation checks.

### Validation sync
- Runtime checks confirm Study Plan page loads without unexpected error/not found/json conversion failures.
- Filter checks confirm department->student narrowing and seeded plan visibility for target demo profiles.
- SQL checks confirm deterministic Study Plan demo row and course coverage.

## 2026-06-01 Update - Generate Certificates School/College Filter Demo Stabilization and Seed Sync v41

### Implementation sync
- Scripts/03-FullDummyData.sql advanced to FullDummyData-v41 and now seeds additional deterministic school/college Generate Certificates filter-demo records for demo/testing:
	- DEMO-CERT-COL-FILTER-945
	- DEMO-CERT-SCH-FILTER-946
- The seed now resolves school/college department/program IDs dynamically from current data, reducing environment-specific ID mismatch issues.
- Scripts/05-PostDeployment-Checks.sql synchronized to v41 and expanded with school/college split checks plus expanded filter-demo username/profile assertions.
- Grading Config scope behavior updated to improve filter usability:
	- auto-selects single campus for selected tenant,
	- uses resilient campus loading fallback in web API client when tenant-filtered campus list is unexpectedly empty.

### Validation sync
- Deterministic seed/check flow remains additive and idempotent for repeated demo resets.
- Runtime checks confirm tenant-campus-department-course/class filter chains render with expected options and data visibility.

## 2026-06-01 Update - Generate Certificates License-Driven Institute Filter and Search Demo Seed v40

### Implementation sync
- Generate Certificates filter surface now retains a single institution filter and removes tenant/campus/department/course/semester selectors from the page.
- Institution selection is now license-driven:
	- if license enables only one institute type, it auto-selects and locks that option,
	- if license enables multiple institute types, user selects from the dropdown.
- Table search now indexes student/program/department plus non-university generated certificate title/type values.
- Scripts/03-FullDummyData.sql advanced to FullDummyData-v40 with deterministic search-demo rows for this flow.
- Scripts/05-PostDeployment-Checks.sql synchronized with v40 marker and expanded Generate Certificates checks.

### Validation sync
- Runtime verification confirmed the page shows only Institution filter and Search Students controls for this section.
- Search verification confirmed non-university certificate title/type keywords now narrow row results.

## 2026-06-01 Update - Generate Certificates Additional Certificate Demo Seed v39 and Runtime Validation

### Implementation sync
- Portal certificate flow now covers both university and non-university document paths without changing existing navigation shape.
- Additional certificate generation, upload, and default-template download support is now documented alongside the existing degree/transcript flow.
- Scripts/03-FullDummyData.sql advanced to FullDummyData-v39 and adds deterministic school/college demo rows for Generate Certificates testing:
	- college demo registrations DEMO-CERT-COL-901 and DEMO-CERT-COL-902
	- school demo registrations DEMO-CERT-SCH-911 and DEMO-CERT-SCH-912
- Scripts/05-PostDeployment-Checks.sql now mirrors the v39 marker and validates the additional demo cohort.

### Validation sync
- Runtime validation confirmed the Generate Certificates page loads for authenticated users and the seeded rows appear in the scoped student lists.
- Filter validation confirmed tenant, campus, department, course/class, and semester narrowing still returns deterministic rows for both institution branches.

## 2026-05-31 Update - Generate Certificates Guard/Sidebar Synchronization and Course Materials Demo Seed v38

### Implementation sync
- Portal guard/sidebar synchronization for Generate Certificates:
	- Route-guard map now explicitly resolves GenerateCertificates to sidebar key generate_certificates.
	- Sidebar hide logic no longer suppresses generate_certificates under Degree Audit-specific hidden-key branch.
	- Outcome: users with allowed sidebar key can access the page both from sidebar navigation and direct route entry.
- Scripts/03-FullDummyData.sql marker advanced to FullDummyData-v38.
- Deterministic Course Materials demo set includes cross-department rows for filtering walkthroughs:
	- CS rows: 27272727-2727-2727-2727-272727272701..705,
	- BUS row: 27272727-2727-2727-2727-272727272706,
	- ENG row: 27272727-2727-2727-2727-272727272707.
- Scripts/05-PostDeployment-Checks.sql synchronized with v38 dataset and deterministic course-material assertions.

### Validation sync
- Portal runtime checks confirmed Generate Certificates menu/path visibility after guard/sidebar synchronization.
- Filter checks confirmed deterministic data rendering and expected narrowing behavior across department/course selections.
- Post-deployment SQL checks now report deterministic v38 marker and cross-department course-material coverage.

## 2026-05-31 Update - Graduation Eligibility Filter Demo Seed v37 and Verification Automation

### Implementation sync
- Updated Scripts/03-FullDummyData.sql marker to FullDummyData-v37 and expanded deterministic Graduation Eligibility filter-demo cohort:
	- added 2026-MAT-ACC-002 in accelerated/filter-demo program,
	- added deterministic user/profile/enrollment/result rows for filter-stability demo coverage,
	- preserved additive idempotent reseed behavior.
- Updated Scripts/05-PostDeployment-Checks.sql with synchronized v37 check names and expanded deterministic ID coverage:
	- DummySeed_DemoDatasetVersionIsV37,
	- DummySeed_DegreeAuditEligibilityDemo_StudentCountV37,
	- DummySeed_DegreeAuditEligibilityDemo_BaseProgramCountV37,
	- DummySeed_DegreeAuditEligibilityDemo_AccProgramCountV37,
	- DummySeed_DegreeAuditEligibilityDemo_EnrollmentRowsCountV37,
	- DummySeed_DegreeAuditEligibilityDemo_ResultRowsCountV37.
- Added reusable verification automation for secure role/menu regression checks:
	- tools/ci/verify-degree-rules-access.ps1,
	- tools/ci/verify-degree-rules-access-local-bootstrap.ps1,
	- tools/ci/run-degree-rules-verifier-interactive.ps1,
	- .vscode task entries for prompted/interactive/local-auto-bootstrap execution.

### Validation sync
- Graduation Eligibility menu/runtime filter checks confirmed deterministic split:
	- base program filter => 4 registrations (AUD set),
	- accelerated/filter-demo program => 2 registrations (ACC set).
- DB checks confirmed v37 marker and expanded deterministic cohort/enrollment/result row counts.
- Degree Rules automated verification task completed with passing role/menu/API matrix assertions.

## 2026-05-31 Update - Graduation Eligibility Filter Demo Seed v36 and Server-Side Graduation Enforcement

### Implementation sync
- Updated Scripts/03-FullDummyData.sql marker to FullDummyData-v36 and added deterministic Graduation Eligibility filter-demo cohort expansion:
	- new registration rows: 2026-MAT-AUD-004 and 2026-MAT-ACC-001,
	- deterministic user/profile/enrollment/result coverage for program-filter validation,
	- additive, idempotent sample-data extension for demo/testing.
- Updated Scripts/05-PostDeployment-Checks.sql with v36 marker and deterministic Graduation Eligibility assertions:
	- DummySeed_DemoDatasetVersionIsV36,
	- DummySeed_DegreeAuditEligibilityDemo_StudentCountV36,
	- DummySeed_DegreeAuditEligibilityDemo_BaseProgramCountV36,
	- DummySeed_DegreeAuditEligibilityDemo_AccProgramCountV36,
	- DummySeed_DegreeAuditEligibilityDemo_EnrollmentRowsCountV36,
	- DummySeed_DegreeAuditEligibilityDemo_ResultRowsCountV36.
- Graduation endpoint behavior now enforces eligibility server-side in lifecycle flow; ineligible direct POST attempts return validation errors and do not graduate students.

### Validation sync
- Portal filter validation confirmed deterministic data rendering with expected split:
	- department-only filter: 5 rows,
	- base program filter: 4 rows,
	- accelerated/filter-demo program filter: 1 row.
- Graduation direct-post validation confirmed ineligible graduation requests are rejected and student status remains unchanged.

## 2026-05-31 Update - Degree Audit Demo Seed v35, Sidebar Completion, and Portal Filter Validation

### Implementation sync
- Updated Scripts/02-Seed-Core.sql with the missing Degree Audit companion sidebar/menu entries and role access rows:
	- `graduation_eligibility`
	- `degree_rules`
- Updated Scripts/03-FullDummyData.sql to `FullDummyData-v35` with deterministic university math demo coverage for menu/filter validation:
	- `2026-MAT-AUD-001`
	- `2026-MAT-AUD-002`
	- `2026-MAT-AUD-003`
	- deterministic enrollments across Algebra, Geometry, and bridge-course offerings
	- deterministic published result rows with grade-point-backed credit totals
- Updated Scripts/05-PostDeployment-Checks.sql with synchronized v35 checks for dataset marker, deterministic cohort presence, and Degree Audit sidebar/menu visibility prerequisites.
- Runtime credit aggregation remains aligned with the current repository fix in `DegreeAuditRepository.GetEarnedCreditsAsync`, which now tolerates legacy scope data stored on departments.

### Validation sync
- API verification confirmed filtered eligibility results for the mathematics university slice return all three seeded demo rows.
- API verification confirmed the deterministic audit totals resolve as `6`, `4`, and `2` earned credits with `3`, `2`, and `1` completed courses.
- Portal verification confirmed dashboard menu visibility, Degree Audit selector rendering, and Graduation Eligibility filtered table rendering without page-load errors.

## 2026-05-31 Update - Prerequisites Filter Demo Seed v34 and Runtime Payload Validation

### Implementation sync
- Updated Scripts/03-FullDummyData.sql with deterministic Prerequisites filter-demo additions and marker upgrade to `FullDummyData-v34`:
	- PRQ-101 (`58585858-5858-5858-5858-585858585801`),
	- PRQ-201 (`59595959-5959-5959-5959-595959595901`),
	- deterministic prerequisite link (`5A5A5A5A-5A5A-5A5A-5A5A-5A5A5A5A5A01`).
- Updated Scripts/05-PostDeployment-Checks.sql with synchronized v34 marker and prerequisites assertions:
	- DummySeed_DemoDatasetVersionIsV34,
	- DummySeed_PrerequisitesFilterDemo_CourseRowsByIdCount,
	- DummySeed_PrerequisitesFilterDemo_LinkCount,
	- DummySeed_PrerequisitesFilterDemo_DepartmentScopedCourseCount,
	- DummySeed_PrerequisitesFilterDemo_CourseScopeAlignedCount.
- Updated prerequisites repository payload shaping by loading both course sides in prerequisite query:
	- PrerequisiteRepository.GetByCourseIdAsync now includes `Course` and `PrerequisiteCourse` navigation.

### Validation sync
- SQL verification confirmed deterministic prerequisites demo rows, department scoping, and link integrity.
- API verification confirmed department-scoped course filtering returns seeded rows for the Prerequisites menu path.
- Prerequisites menu/filter runtime validation confirmed seeded rows appear and relationship rendering works in department-scoped route.

## 2026-05-31 Update - Generate Certificates Filter Demo Seed v33 and Departments Institution Filter Validation

### Implementation sync
- Updated Scripts/03-FullDummyData.sql with deterministic Generate Certificates filter demo additions and marker upgrade to `FullDummyData-v33`:
	- DEMO-CERT-FILTER-CS-911,
	- DEMO-CERT-FILTER-BUS-912,
	- DEMO-CERT-FILTER-ENG-913.
- Updated Scripts/05-PostDeployment-Checks.sql with matching v33 assertions:
	- DummySeed_DemoDatasetVersionIsV33,
	- DummySeed_GenerateCertificatesFilterDemo_ProfilesByIdCount,
	- DummySeed_GenerateCertificatesFilterDemo_GraduatedStatusCount,
	- DummySeed_GenerateCertificatesFilterDemo_QueryCount_CS.
- Updated Departments list API behavior by extending `GET /api/v1/department` with optional `institutionType` filtering while preserving existing tenant/campus/role scope logic.

### Validation sync
- SQL verification confirmed deterministic filter-demo rows exist with graduated status and expected department/tenant/campus alignment.
- Generate Certificates menu load and filter verification confirmed:
	- all three new rows visible in scoped university context,
	- CS department filter includes CS row and excludes BUS/ENG rows,
	- no page-load error after runtime schema alignment.
- Departments API verification confirmed institutionType-filtered results return correctly segmented datasets.

## 2026-05-31 Update - Result Calculation Course-Type Filter Dummy Seed and Runtime Validation

### Implementation sync
- Updated Scripts/03-FullDummyData.sql with deterministic Result Calculation course-type filter demo alignment:
	- set HasSemesters for deterministic demo rows (CRSFILENG=1, CRSFILBUS=0, CRSFILMAT=1),
	- set aligned TotalSemesters values for deterministic demo rows.
- Updated Scripts/05-PostDeployment-Checks.sql with Result Calculation course-type filter assertions:
	- DummySeed_ResultCalculationCourseTypeFilter_HasSemestersTrueCount,
	- DummySeed_ResultCalculationCourseTypeFilter_HasSemestersFalseCount.
- Added web runtime proxy action for Result Calculation filter data:
	- GET /Portal/ResultCalculationCourseFilterData?hasSemesters={true|false}
- Updated ResultCalculation.cshtml filter fetch path to consume the web proxy action.

### Validation sync
- Authenticated menu validation confirmed Result Calculation screen loads and filter controls are present.
- Runtime filter dataset validation confirmed expected deterministic split:
	- true => CRSFILENG + CRSFILMAT,
	- false => CRSFILBUS.
- SQL verification confirmed deterministic counts for the new HasSemesters assertions (true=2, false=1).

## 2026-05-31 Update - Generate Certificates Demo Seed v32 and Filter Validation

### Implementation sync
- Updated Scripts/03-FullDummyData.sql with deterministic Generate Certificates demo/filter data synchronization:
	- advanced dataset marker to FullDummyData-v32,
	- expanded deterministic graduated student cohort from 3 to 6 total rows,
	- added deterministic users, student_profiles, and enrollments for CS/Business/Engineering second demo pair.
- Updated Scripts/05-PostDeployment-Checks.sql with v32 Generate Certificates demo assertions:
	- DummySeed_DemoDatasetVersionIsV32,
	- DummySeed_GenerateCertificatesDemo_ProfilesByIdCount,
	- DummySeed_GenerateCertificatesDemo_GraduatedCount,
	- DummySeed_GenerateCertificatesDemo_ComputerScienceCount,
	- DummySeed_GenerateCertificatesDemo_BusinessCount,
	- DummySeed_GenerateCertificatesDemo_EngineeringCount,
	- DummySeed_GenerateCertificatesDemo_EnrollmentRowsByIdCount.

### Validation sync
- SQL verification confirmed deterministic v32 counts:
	- marker=1,
	- profiles=6,
	- graduated=6,
	- department slices (CS/Business/Engineering)=2 each,
	- enrollments=6.
- Runtime behavior remains additive and data-only for existing Generate Certificates filter flow.

## 2026-05-31 Update - Generate Certificates Demo Seed v31 and Filter Validation

### Implementation sync
- Updated Scripts/03-FullDummyData.sql with deterministic Generate Certificates demo/filter data synchronization:
	- advanced dataset marker to FullDummyData-v31,
	- added deterministic graduated student rows for CS/Business/Engineering university departments,
	- added deterministic enrollment rows for the new certificate-demo students.
- Updated Scripts/05-PostDeployment-Checks.sql with Generate Certificates demo assertions:
	- DummySeed_DemoDatasetVersionIsV31,
	- DummySeed_GenerateCertificatesDemo_ProfilesByIdCount,
	- DummySeed_GenerateCertificatesDemo_GraduatedCount,
	- DummySeed_GenerateCertificatesDemo_ComputerScienceCount,
	- DummySeed_GenerateCertificatesDemo_BusinessCount,
	- DummySeed_GenerateCertificatesDemo_EngineeringCount,
	- DummySeed_GenerateCertificatesDemo_EnrollmentRowsByIdCount.

### Validation sync
- SQL verification confirmed deterministic DEMO-CERT rows are present and graduated (3 total; 1 per target department).
- SQL filter verification confirmed expected filter-path visibility for target department/course scenarios.

## 2026-05-31 Update - Generate Certificates Template Import/Export and Runtime Validation

### Implementation sync
- Updated certificate template API surface to support transcript template lifecycle in addition to degree template:
	- Added GET api/v1/degree/template/transcript/default
	- Added POST api/v1/degree/template/transcript/upload
- Updated web API client with template lifecycle proxy methods:
	- DownloadCertificateTemplateAsync(templateType)
	- UploadCertificateTemplateAsync(templateType, file)
- Updated Generate Certificates portal flow with additive university-scope template actions:
	- DownloadCertificateTemplate action
	- UploadCertificateTemplate action with admin guard + university-scope guard
	- UI section for Degree/Transcript template download + import

### Validation sync
- Diagnostics check confirmed no compile errors in touched files:
	- DegreeController.cs
	- PortalController.cs
	- EduApiClient.cs
	- GenerateCertificates.cshtml
- Runtime verification confirmed template download routes return file-download responses for both template types:
	- /Portal/DownloadCertificateTemplate?templateType=degree
	- /Portal/DownloadCertificateTemplate?templateType=transcript

## 2026-05-31 Update - Courses Demo Seed v30 and Filter Validation

### Implementation sync
- Updated Scripts/03-FullDummyData.sql with deterministic Courses demo/filter data synchronization:
	- advanced dataset marker to FullDummyData-v30,
	- added deterministic course rows for Engineering, Business, and Mathematics department filter scenarios,
	- added deterministic offering rows for the above course demo IDs,
	- added idempotent normalization plus scope-alignment logic (tenant/campus/institution type) for course and offering demo rows.
- Updated Scripts/05-PostDeployment-Checks.sql with Courses demo assertions:
	- DummySeed_DemoDatasetVersionIsV30,
	- DummySeed_CoursesFilterDemo_CourseRowsByIdCount,
	- DummySeed_CoursesFilterDemo_ActiveCourseCount,
	- DummySeed_CoursesFilterDemo_OfferingsByIdCount,
	- DummySeed_CoursesFilterDemo_EngineeringCourseCount,
	- DummySeed_CoursesFilterDemo_BusinessCourseCount,
	- DummySeed_CoursesFilterDemo_MathCourseCount,
	- DummySeed_CoursesFilterDemo_CourseScopeAlignedCount,
	- DummySeed_CoursesFilterDemo_OfferingScopeAlignedCount,
	- DummySeed_DemoDatasetVersion_v30.

### Validation sync
- SQL verification confirmed deterministic Courses demo counts, scope alignment, and v30 marker checks.
- Runtime menu validation confirmed Courses page loads and department filters show expected deterministic demo codes (CRSFILENG, CRSFILBUS, CRSFILMAT) without blocking errors.

## 2026-05-31 Update - Programs Demo Seed v29 and Filter Validation

### Implementation sync
- Updated Scripts/03-FullDummyData.sql with deterministic Programs demo/filter data synchronization:
	- advanced dataset marker to FullDummyData-v29,
	- added deterministic Programs filter-demo rows for Engineering, Business, and Mathematics departments,
	- added idempotent normalization updates so demo rows remain active and consistent across reseed runs.
- Updated Scripts/05-PostDeployment-Checks.sql with Programs demo assertions:
	- DummySeed_DemoDatasetVersionIsV29,
	- DummySeed_ProgramsFilterDemo_ByIdCount,
	- DummySeed_ProgramsFilterDemo_ActiveCount,
	- DummySeed_ProgramsFilterDemo_DummyEngineeringCount,
	- DummySeed_ProgramsFilterDemo_BusinessCount,
	- DummySeed_ProgramsFilterDemo_MathCount,
	- DummySeed_DemoDatasetVersion_v29.

### Validation sync
- SQL verification confirmed Programs deterministic demo counts and v29 marker checks pass.
- Runtime menu validation confirmed Programs page loads successfully and department filters show the expected seeded demo rows without blocking errors.

## 2026-05-31 Update - Degree Audit Demo Seed v28 and Filter Validation

### Implementation sync
- Updated Scripts/03-FullDummyData.sql with deterministic Degree Audit demo/filter data synchronization:
	- advanced dataset marker to FullDummyData-v28,
	- added deterministic grade-point population coverage for Degree Audit filter-demo cohorts,
	- added deterministic university grade-point coverage for Degree Audit student-selection demos.
- Updated Scripts/05-PostDeployment-Checks.sql with Degree Audit demo assertions:
	- DummySeed_DemoDatasetVersionIsV28,
	- DummySeed_DegreeAuditFilterDemo_ResultRowsCount,
	- DummySeed_DegreeAuditFilterDemo_GradePointPopulatedCount,
	- DummySeed_DegreeAuditUniversityDemo_ResultRowsCount,
	- DummySeed_DegreeAuditUniversityDemo_StudentProfileCount,
	- DummySeed_DegreeAuditUniversityDemo_GradePointPopulatedCount.
- Synchronized Degree Audit runtime behavior for reliable menu/filter testing:
	- non-student users now select a student before loading audit details,
	- Degree Audit controller/service guards now handle missing institution navigation safely,
	- page-level initial state no longer surfaces misleading no-data messaging before selection.

### Validation sync
- SQL verification confirmed deterministic Degree Audit demo counts and v28 marker checks pass.
- Runtime menu validation confirmed selected student context changes successfully and Degree Audit seeded data is rendered without blocking errors.

## 2026-05-30 Update - Enrollments Demo Seed and Filter Validation

### Implementation sync
- Updated Scripts/03-FullDummyData.sql with deterministic Enrollments filter-demo expansion:
	- Added DEMO-ENG-704 and DEMO-ENG-705 profile/user rows.
	- Added deterministic active enrollments for DB-201 filter path so DS-101 and DB-201 produce distinct roster sizes.
	- Added reseed normalization so Enrollments demo rows are reset to Active and not dropped.
- Updated Scripts/05-PostDeployment-Checks.sql with Enrollments-specific assertions:
	- DummySeed_EnrollmentsFilterDemo_StudentProfilesCount
	- DummySeed_EnrollmentsFilterDemo_DS101_ActiveCount
	- DummySeed_EnrollmentsFilterDemo_DB201_ActiveCount
	- dataset marker checks upgraded to FullDummyData-v27.

### Validation sync
- SQL verification confirmed deterministic Enrollments demo counts:
	- DS-101 active demo enrollments = 3,
	- DB-201 active demo enrollments = 5,
	- Enrollments demo student profiles = 5.
- Enrollments menu verification confirmed page load stability and offering filter changes roster rows correctly between DS-101 and DB-201.

## 2026-05-30 Update - Student Lifecycle Demo Seed and Filter Validation

### Implementation sync
- Added deterministic Student Lifecycle filter demo rows in Scripts/03-FullDummyData.sql:
	- DEMO-LIFE-CS-801 (CS, semester 8, active graduation-candidate path)
	- DEMO-LIFE-CS-101 (CS, semester 1, active semester-tab path)
	- DEMO-LIFE-ENG-201 (Engineering, semester 2, active department filter path)
- Updated Scripts/05-PostDeployment-Checks.sql with Student Lifecycle-specific assertions:
	- DummySeed_StudentLifecycleFilterDemo_ProfileCount
	- DummySeed_StudentLifecycleFilterDemo_CSDeptCount
	- DummySeed_StudentLifecycleFilterDemo_EngineeringDeptCount
	- DummySeed_StudentLifecycleFilterDemo_StatusActiveCount
	- DummySeed_StudentLifecycleFilterDemo_GraduationCandidateCount
	- DummySeed_StudentLifecycleFilterDemo_Semester1Count
	- DummySeed_StudentLifecycleFilterDemo_Semester2Count
	- StudentLifecycle_LegacyStatus0Count
- Synchronized runtime lifecycle data handling:
	- legacy active status (`0`) is now accepted alongside canonical active (`1`) for lifecycle filtering,
	- graduation candidates now populate display names from linked user records,
	- graduation candidate selection now enforces final-semester eligibility.

### Validation sync
- SQL verification confirmed deterministic Student Lifecycle demo rows and department split are present.
- Student Lifecycle menu load verification confirmed no runtime screen-load errors.
- Department/semester filter verification confirmed result-set changes and seeded data visibility across CS/Engineering scenarios.

## 2026-05-30 Update - FYP Demo Seed and Menu Filter Validation

### Implementation sync
- Added deterministic FYP filter demo rows in Scripts/03-FullDummyData.sql:
	- 2 rows in School of Computer Science (Proposed + InProgress)
	- 1 row in School of Engineering (Completed)
- Added deterministic upcoming FYP meeting row in Scripts/03-FullDummyData.sql for menu-panel verification.
- Updated Scripts/05-PostDeployment-Checks.sql with FYP-specific assertions:
	- DummySeed_FypFilterDemoRows_ByIdCount
	- DummySeed_FypFilterDemo_CS_DepartmentCount
	- DummySeed_FypFilterDemo_Engineering_DepartmentCount
	- DummySeed_FypFilterDemo_UpcomingMeetingCount

### Validation sync
- SQL verification confirmed deterministic FYP demo projects and meeting row are present.
- FYP menu load verification confirmed no runtime screen-load errors.
- Department filter verification confirmed:
	- CS filter shows CS demo projects and excludes Engineering demo row.
	- Engineering filter shows Engineering demo project and excludes CS demo rows.

## 2026-05-30 Update - Quizzes Demo Seed and Filter Validation

### Implementation sync
- Added deterministic Quizzes filter demo rows in Scripts/03-FullDummyData.sql for offering 55555555-5555-5555-5555-555555555501:
	- 13131313-1313-1313-1313-131313131307 (active)
	- 13131313-1313-1313-1313-131313131308 (inactive)
- Updated Scripts/05-PostDeployment-Checks.sql with Quizzes-specific assertions:
	- DummySeed_QuizRows_FilterDemoByIdCount
	- DummySeed_QuizRows_Offering501_ActiveCount
	- DummySeed_QuizRows_Offering501_InactiveCount
- Synchronized Quizzes runtime behavior:
	- web quiz list mapping now consumes API `quizId` identity,
	- activate flow guards empty quiz id payload,
	- hidden bool filter/action payloads submit explicit true/false,
	- quiz listing includes IsActive state for stable action labels.

### Validation sync
- SQL verification confirmed deterministic quiz demo rows and active/inactive split for offering 501.
- Quizzes menu filter path verified:
	- includeInactive=false: inactive demo quiz excluded,
	- includeInactive=true: inactive demo quiz included with Inactive badge.
- Activate action no longer triggers zero-guid not-found error in validated flow.

## 2026-05-30 Update - Results Internal Demo Seed and Filter Validation

### Implementation sync
- Added deterministic Results demo rows in Scripts/03-FullDummyData.sql for Internal exam filtering on offering 55555555-5555-5555-5555-555555555501:
	- cccccccc-cccc-cccc-cccc-cccccccccc40
	- cccccccc-cccc-cccc-cccc-cccccccccc41
	- cccccccc-cccc-cccc-cccc-cccccccccc42
- Added offering-scope alignment in Scripts/03-FullDummyData.sql so offering 555...501 is consistently bound to University tenant/campus context for scoped Results UI filters.
- Updated Scripts/05-PostDeployment-Checks.sql with Results-specific assertions:
	- DummySeed_ResultRows_InternalDemoRowCount
	- DummySeed_ResultRows_Offering501_InternalCount
	- DummySeed_ResultOffering501_ScopeAligned

### Validation sync
- SQL verification confirmed Internal demo rows exist (count 3) and offering scope alignment is in place (count 1).
- Results menu filter path (Internal/Internal + scoped offering context) rendered deterministic rows with Results 3 and expected seeded registration numbers.
- No runtime load error was observed for the validated Results filter path.

## 2026-05-30 Update - Students Demo Seed v26 and Filter Validation

### Implementation sync
- Upgraded `Scripts/03-FullDummyData.sql` dataset marker to `FullDummyData-v26`.
- Added deterministic Students demo rows for menu/filter verification:
	- `STUFILT-CS-901`, `STUFILT-CS-902` in `School of Computer Science`.
	- `STUFILT-BUS-903` in `School of Business Administration`.
- Updated `Scripts/05-PostDeployment-Checks.sql` with v26 assertion and Students filter demo checks.
- Hardened Students menu runtime behavior so temporary API unavailability shows user-facing warning instead of unhandled exception.

### Validation sync
- SQL verification confirmed deterministic Students demo split (`2` CS + `1` Business).
- Students menu loaded and rendered seeded records; department filter URL and list behavior remained stable under scoped role context.
- Outage-mode runtime now returns graceful UI messaging for Students page load.

## 2026-05-30 Update - Student Timetable Demo Seed v25 Expansion

### Implementation sync
- Upgraded `Scripts/03-FullDummyData.sql` dataset marker to `FullDummyData-v25`.
- Expanded deterministic Student Timetable demo rows for Dummy Engineering (`departmentId=33333333-3333-3333-3333-333333333333`):
	- 3 published timetables (`2525...2901`, `2525...2902`, `2525...2903`),
	- 6 timetable entries (`2626...2901` to `2626...2906`) across Monday/Tuesday/Wednesday/Thursday/Friday/Saturday.
- Updated `Scripts/05-PostDeployment-Checks.sql` with v25 marker checks plus Student Timetable checks for the new Tuesday/Friday/Saturday rows.

### Validation sync
- Student Timetable page now has richer deterministic coverage for both match and sparse/no-row day-filter demos.

## 2026-05-30 Update - Student Timetable Demo Seed v24 and Filter QA

### Implementation sync
- Upgraded `Scripts/03-FullDummyData.sql` dataset marker to `FullDummyData-v24`.
- Added deterministic Student Timetable demo rows for Dummy Engineering (`departmentId=33333333-3333-3333-3333-333333333333`):
	- 2 published timetables (`2525...2901`, `2525...2902`),
	- 3 timetable entries (`2626...2901` to `2626...2903`) across Monday/Wednesday/Thursday for day-filter verification.
- Updated `Scripts/05-PostDeployment-Checks.sql` with v24 marker checks plus Student Timetable demo checks (count + day-of-week coverage).
- Improved API error rendering by extracting JSON error message/title in web client so portal warnings show plain text instead of raw JSON.

### Validation sync
- Student Timetable menu route loads and displays deterministic seeded rows.
- Day filter and timetable switch scenarios render expected subsets without runtime errors.

## 2026-05-30 Update - Attendance Filter Demo Seed v23 and Student Filtering Reliability

### Implementation sync
- Upgraded `Scripts/03-FullDummyData.sql` dataset marker to `FullDummyData-v23`.
- Added deterministic attendance filter demo cohort for DS-101/DB-201 (`2026-S2`) including:
	- 3 demo student profiles,
	- 6 enrollments (3 per offering),
	- 6 attendance rows (3 per offering).
- Updated `Scripts/05-PostDeployment-Checks.sql` with v23 assertion and attendance filter demo row checks.
- Fixed attendance student filter mapping so selected student filters records correctly (student profile id mapping path).

### Validation sync
- Attendance and Enter Attendance now show deterministic records for offering-level filter testing.
- Student dropdown filtering now narrows records correctly instead of resetting to all students.

## 2026-05-30 Update - Announcements Demo Seed v22 and Show Inactive Reliability

### Implementation sync
- Upgraded `Scripts/03-FullDummyData.sql` dataset marker to `FullDummyData-v22`.
- Added deterministic announcement demo rows (active/inactive across two offerings) so Show inactive and offering filters are consistently testable.
- Updated `Scripts/05-PostDeployment-Checks.sql` with v22 dataset assertion and deterministic announcement filter-demo checks.
- Updated Announcements web filter form binding to prevent false override when Show inactive is checked.

### Validation sync
- Show inactive now consistently returns inactive announcements when checked.
- Offering-specific filter behavior is deterministic with seeded demo rows and explicit post-deployment assertions.

## 2026-05-29 Update - Discussion Demo Seed v21 (Offering Filter Coverage)

### Implementation sync
- Expanded `Scripts/03-FullDummyData.sql` Discussion section with additional mixed-state rows for offering-level demo/testing.
- Upgraded demo dataset marker to `FullDummyData-v21`.
- Added post-check assertions for discussion coverage by offering and reply population.

### Validation sync
- Discussion screen expected states are now seeded for two offerings with distinct thread sets.
- Post-deployment checks now validate v21 dataset marker and offering-specific discussion thread counts.

## 2026-05-29 Update - Discussion Menu Data and Schema Reliability (v20)

### Implementation sync
- Added Phase 31 Discussion schema coverage in the canonical schema script so fresh/rebuilt environments include all runtime-required columns.
- Upgraded dummy dataset marker to `FullDummyData-v20`.
- Expanded Discussion demo seed with mixed thread states for stable menu/demo verification:
	- pinned open issue,
	- open FAQ,
	- resolved + closed technical issue,
	- populated ticket numbers and replies.

### Validation sync
- Post-deployment checks now validate:
	- dataset version `FullDummyData-v20`,
	- Discussion Phase 31 column presence,
	- Discussion ticket population,
	- Discussion resolved-thread count.

## 2026-05-29 Update - LMS Demo Seed v18 Coverage

### Implementation sync
- Upgraded demo dataset marker to `FullDummyData-v18`.
- Added LMS Manage demo scenarios for offering `55555555-5555-5555-5555-555555555513`:
	- week 5 draft module,
	- week 6 and week 7 published modules,
	- additional module video rows to verify multi-video rendering.

### Validation sync
- Added post-check assertions for LMS data visibility:
	- offering-513 module count,
	- offering-513 draft week-5 module count,
	- week-6 module video count.

## 2026-05-29 Update - Rubric/Gradebook Demo Data Expansion

### Implementation sync
- Added explicit rubric demo submissions for offering `55555555-5555-5555-5555-555555555513` so Rubric Management assignment data always appears for seeded demo runs.
- Added offering-513 `Practical` result rows to strengthen fallback-component gradebook demo/testing coverage.
- Bumped dummy seed metadata version to `FullDummyData-v17`.

### Validation sync
- Post-deployment checks now validate:
	- dataset version `FullDummyData-v17`,
	- rubric submissions for offering 513,
	- practical result rows for offering 513.

## 2026-05-28 Update - Enter Results Acceptance Criteria Closure

## 2026-05-29 Update - Institution-Aware Gradebook Metrics

### Implementation sync
- Gradebook output is now institution-aware:
	- University context shows GPA and CGPA.
	- School/College context shows Percentage.
- Active gradebook components are now resolved per institution type.
- Added resilience behavior for demo/testing: when no institution component rule is configured, components are inferred from existing result types in the selected offering.
- Seed data now includes school and college component-specific result rows (ClassTest and Sessional) to verify percentage behavior with optional component mixes.

### Validation sync
- Unit tests passed (`197/197`) after repository interface alignment.
- Runtime verification confirmed:
	- university sample offering renders GPA/CGPA,
	- school/college sample offerings render Percentage and load without component errors.

### Implementation sync
- Confirmed closure of AC1-AC6 across role access, scoped manual/CSV entry, validation/error messaging, import audit/report lifecycle, token one-time+expiry behavior, and publish/correction governance.
- No additional runtime contract was introduced in this closure slice; this phase consolidates evidence mapping and verification status.

### Validation sync
- Focused result-governance unit slice passed (`9/9`).
- Report-token web integration suite passed (`3/3`).
- Sidebar integration suite passed (`17/17`).
- Web build passed on closure baseline.

## 2026-05-28 Update - Enter Results Non-Functional Hardening

### Implementation sync
- Enter Results correction flow now enforces the same selected-scope validation pattern used by create/import/publish actions.
- Manual create/correction flows now return clearer server-side mark-range validation guidance.
- Structured logging now captures actor, offering scope, and block reason context for create/correct/publish operations.

### Validation sync
- Web build passed after non-functional hardening.
- Focused result-governance unit tests remained green (`9/9`).
- Report-token web integration (`3/3`) and sidebar regression (`17/17`) remained green.

## 2026-05-28 Update - Enter Results Test Requirements Expansion

### Implementation sync
- Added ResultService governance unit coverage for publish/correction workflow invariants.
- Added unit checks for correction-audit reason propagation and published-only correction enforcement.
- Added unit checks for publish-all behavior to ensure only draft rows are published.

### Validation sync
- Focused result-governance unit slices passed (`9/9`).
- Web build passed.
- Report-token web integration (`3/3`) and sidebar regression (`17/17`) remained green.

## 2026-05-28 Update - Enter Results Publishing Rules Governance Hardening

### Implementation sync
- Enter Results final publish is now approval-gated to Admin/SuperAdmin roles in the web flow.
- Faculty can continue creating draft results and imports but cannot execute final publish.
- Result correction now requires published state and includes correction reason in correction audit payload.

### Validation sync
- Web build passed after governance updates.
- Focused unit test slice passed (`6/6`) for publish eligibility and published-only correction rule.
- Sidebar integration suite remained green (`17/17`).

## 2026-05-28 Update - Enter Results Phase 5 UI Behavior Logic

### Implementation sync
- Enter Results now renders a two-state result-entry grid for authorized result-writing roles.
- When required filters are complete, the table is editable and shows Student ID, Student Name, Marks Obtained, Max Marks, Grade, and Remarks columns.
- When required filters are incomplete, the same table structure renders in guidance/disabled state and write actions remain blocked.

### Validation sync
- Web build passed after phase 5 UI updates.
- Focused unit coverage added for required-filter write gating on `ResultsPageModel`.

## 2026-05-28 Update - Enter Results Phase 4 Import Audit and Report Lifecycle

### Implementation sync
- Enter Results CSV import now writes upload-level audit metadata and structured summary details.
- Import now generates downloadable row-level CSV report with Imported/Skipped outcomes and reasons.
- Report tokens now enforce one-time and 2-hour expiry behavior with distinct unavailable/expired messages.
- Route-level integration coverage added for valid, one-time, and expired report token scenarios.

### Validation sync
- Result report route web integration tests passed (`3/3`).
- Web build passed.
- Sidebar integration suite remained green (`17/17`).

## 2026-05-28 Update - Enter Results Phase 3 Template Download Completion

### Implementation sync
- Enter Results template download now includes two explicit example rows in CSV output.
- Example rows are treated as guidance-only and are skipped during import processing.

### Validation sync
- Web build passed after template behavior update.
- Sidebar integration suite remained green (`17/17`).

## 2026-05-28 Update - Enter Results Phase 1 and Phase 2 Runtime Completion

### Implementation sync
- Enter Results now supports CSV template download and CSV import with strict-mode validation behavior.
- Enter Results now enforces required-filter completion for write actions and uses dependent filter refresh across scoped academic context.
- Result write endpoints now validate selected scope context server-side before accepting writes.

### Validation sync
- Web build passed after Enter Results runtime completion.
- Sidebar integration suite remained green (`17/17`).

## 2026-05-28 Update - Enter Results Phase 2 Filter Criteria and Dynamic Selection

### Implementation sync
- Enter Results scoped filtering is synchronized for Tenant/Campus, Department, Program/Course, Subject, Class/Semester, Exam Type, and Assessment Component checkpoints.
- Required-filter write-guard behavior is now documented as mandatory for result write operations.
- Dependent filter refresh behavior is now documented so downstream selection options remain scope-safe.

### Validation sync
- Requirement-to-flow validation completed for filter dependency and required-filter guard behavior.
- Existing role-governed menu access and route protection remain unchanged.
- No schema mutation required in this phase.

## 2026-05-28 Update - Enter Results Phase 0/1 Initial Slice

### Implementation sync
- Added governed sidebar key `enter_results` for Faculty Related navigation.
- Added dedicated portal entry action `EnterResults` and mapped sidebar routing to that action while preserving the existing Results workflow.
- Applied Admin/Faculty allow and Student deny role visibility defaults for `enter_results` under existing sidebar governance.

### Validation sync
- Sidebar integration matrix passed with `enter_results` visibility assertions.
- Full solution build passed.

## 2026-05-28 Update - Enter Attendance Phase 11 Integration Expectations Closure

### Implementation sync
- Confirmed Enter Attendance remains stable across sidebar governance, role authorization, and attendance/reporting integrations.
- Confirmed no regression in student-facing attendance pathways after attendance feature hardening.
- Confirmed tenant/campus scope behavior remains consistent in attendance-related flows.

### Validation sync
- Attendance-focused unit matrix passed (`20/20`).
- Web route integration suite passed (`3/3`).
- Integration regression batch (Sidebar, Authorization, Report Exports, Parent Portal) passed (`117/117`).
- Full solution build passed.

## 2026-05-28 Update - Enter Attendance Phase 9 Database and Script Safeguards

### Implementation sync
- Added idempotent attendance index creation safeguards in maintenance script for offering/date, student, and unique student/offering/date paths.
- Added post-deployment checks to verify attendance index existence in standard and clean validation scripts.
- Confirmed attendance duplicate prevention and subject-scope coverage through StudentProfileId + CourseOfferingId + Date persistence/index model.

### Validation sync
- Full solution build passed.
- Attendance-focused unit matrix passed (`20/20`).
- Sidebar integration regression suite passed (`17/17`).

## 2026-05-28 Update - Enter Attendance Phase 8 Integration Compliance

### Implementation sync
- Verified Enter Attendance behavior remains within existing architecture boundaries across Web/API/Application/Infrastructure layers.
- Confirmed role, sidebar, tenant/campus scope, and authorization behaviors continue to use existing governance patterns.
- Confirmed no cross-module behavioral regressions were introduced by attendance phase updates.

### Validation sync
- Attendance-focused unit matrix passed (`20/20`).
- Web route integration suite passed (`3/3`).
- Sidebar integration regression suite passed (`17/17`).
- Full solution build passed.

## 2026-05-28 Update - Enter Attendance Phase 7 Manual Validation Hardening

### Implementation sync
- Manual attendance submission now rejects empty row payloads before write operations.
- Added duplicate Student+Date detection in manual row submissions.
- Added required row-date enforcement before grouping and writing attendance entries.

### Validation sync
- Attendance-focused unit matrix passed (`20/20`) including new manual duplicate and empty-row guard tests.
- Web build and sidebar integration regression suites passed.

## 2026-05-28 Update - Enter Attendance Phase 6 Row Table Structure

### Implementation sync
- Enter Attendance row-entry table now renders Student ID, Student Name, Date, and Present columns.
- Added generic disabled row table state with matching columns for incomplete filter/offering context.
- Bulk attendance post flow now supports per-row dates and groups writes by date while preserving existing scope validation.

### Validation sync
- Attendance-focused unit matrix passed (`18/18`) including new per-row date bulk-mark tests.
- Web build and sidebar integration regression suites passed.

## 2026-05-28 Update - Enter Attendance Phase 5 Required Filter UI Save Guard

### Implementation sync
- Enter Attendance now disables manual save and CSV import actions when required filters are incomplete.
- Added shared attendance page state logic for required-filter save eligibility and guidance messaging.
- Added UI warning message to explain why write actions are disabled.

### Validation sync
- Attendance-focused unit matrix passed (`16/16`) including new page model save-guard tests.
- Web build and sidebar integration regression suites passed.

## 2026-05-28 Update - Enter Attendance Phase 4.5 Web Route Integration Coverage

### Implementation sync
- Added a dedicated web-host integration test project to validate `PortalController.DownloadAttendanceImportReport` through the real MVC route.
- Added a web entry-point marker type to support route-level `WebApplicationFactory` test bootstrapping.
- Added coverage for valid one-time report download plus invalid/expired token redirect outcomes.

### Validation sync
- Attendance import unit matrix passed (`14/14`).
- Web integration route suite passed (`3/3`).
- Web build and sidebar integration regression suites passed.

## 2026-05-28 Update - Enter Attendance Phase 4.4 Report Download UX Hint

### Implementation sync
- Enter Attendance import section now displays explicit one-time and 2-hour expiry guidance for report links.
- No behavior changes to token validation logic in this slice.

### Validation sync
- Attendance import unit matrix passed (`14/14`).
- Web build and sidebar integration regression suites passed.

## 2026-05-28 Update - Enter Attendance Phase 4.3 Report Retention Controls

### Implementation sync
- Attendance import report tokens now apply explicit retention controls with TTL at download time.
- Expired report tokens now produce a distinct user-facing expiry message.
- One-time token consumption behavior remains enforced.

### Validation sync
- Attendance import unit matrix passed with one-time and expiry coverage (`14/14`).
- Web build and sidebar integration regressions passed.

## 2026-05-28 Update - Enter Attendance Phase 4.2 Import Result Report Download

### Implementation sync
- Enter Attendance CSV import now returns a report token and generates row-level import result CSV output.
- Added report download endpoint and UI button for the latest import result report.
- Existing strict/non-strict behavior, filter enforcement, and audit trail behavior remain intact.

### Validation sync
- Attendance import unit matrix passed with report download coverage (`12/12`).
- Web build and sidebar integration regressions passed.

## 2026-05-28 Update - Enter Attendance Phase 4.1 Upload Audit Trail

### Implementation sync
- Enter Attendance CSV import now writes an upload-level audit trail entry for every outcome (blocked/failed/success/warnings).
- Audit entry includes uploader identity, import timestamp, strict-mode state, offering scope, row totals, imported/skipped counts, and top error reasons.
- Existing strict/non-strict import behavior and row-level feedback UX remain intact.

### Validation sync
- Focused controller unit suite passed (`14/14`).
- Web build and targeted sidebar integration validations passed.

## 2026-05-28 Update - Enter Attendance Phase 3 Import Feedback and Strict Mode

### Implementation sync
- Enter Attendance CSV import now supports strict-mode fail-fast validation and non-strict partial-success behavior.
- Row-level CSV validation details are surfaced back to the attendance page for user correction guidance.
- Import UI now includes strict-mode control while preserving existing role and filter-context boundaries.

### Validation sync
- Focused attendance unit matrix passed with strict-mode coverage (`9/9`).
- Targeted sidebar integration suite remained green (`17/17`).

## 2026-05-28 Update - Enter Attendance Phase 2 Dependent Filter Enforcement

### Implementation sync
- Attendance page now exposes dependent filters for Department, Course/Subject, and Class/Semester in addition to tenant/campus/offering.
- Attendance write endpoints now reject write attempts when selected offering does not match selected Department, Course, and Class/Semester under effective tenant/campus scope.
- Filter context is carried across Enter Attendance post/get actions so users remain on the same scoped selection after operations.

### Validation sync
- Focused attendance unit matrix passed with new filter-context enforcement coverage (`8/8`).
- Targeted sidebar integration suite remained green (`17/17`).

## 2026-05-28 Update - Enter Attendance Phase 1 Hard Scope Enforcement

### Implementation sync
- Manual attendance write operations now enforce selected-offering roster membership under effective tenant/campus scope.
- Manual attendance status input is normalized and validated to `Present`/`Absent` before API submission.
- Focused unit test matrix now covers CSV validation paths and manual roster-scope guard behavior.

### Validation sync
- Focused unit test matrix passed (`7/7`).
- Targeted sidebar integration suite remained green (`17/17`).

## 2026-05-28 Update - Enter Attendance Phase 1 Completion (CSV Template and Import)

### Implementation sync
- Enter Attendance now supports CSV template download and CSV import in the portal attendance page.
- Template uses predefined columns: `StudentId,StudentName,Date,Present`.
- Import validates header shape, required fields, roster membership, duplicate StudentId+Date rows, and date/present value parsing.
- Validated rows are grouped by date and processed through the existing attendance bulk-mark API behavior.

### Validation sync
- Web project build passed after CSV feature additions.
- Existing Enter Attendance role/menu boundaries remain unchanged.

## 2026-05-28 Update - Enter Attendance Phase 1 Start

### Implementation sync
- Implemented the first runtime slice for **Enter Attendance** by adding the new governed sidebar menu key `enter_attendance`.
- Wired the new menu to a dedicated `EnterAttendance` portal action that reuses the current attendance screen for manual attendance entry.
- Restricted visibility for the new menu to **SuperAdmin**, **Admin**, and **Faculty** while leaving the existing `attendance` surface unchanged for backward compatibility.
- Preserved the selected entry route during attendance filtering, bulk marking, and correction flows.
- CSV import and template download remain pending for later slices.

### Validation sync
- Focused sidebar/menu validation passed in the existing integration suite (`17/17`).
- No schema or script change was introduced in this slice.

## 2026-05-28 Update - Enter Attendance Phase 0 Governance and Navigation Requirements

### Implementation sync
- Added Phase 0 documentation for a new **Enter Attendance** menu under **Faculty Related**.
- Restricted planned menu access to **SuperAdmin**, **Admin**, and **Faculty** only.
- Required the menu to be governed through **Sidebar Settings** so visibility, active state, and role mapping follow the existing sidebar administration flow.
- Kept this checkpoint documentation-only; manual entry, CSV import, validation, and persistence phases remain subsequent implementation stages.

### Validation sync
- Documentation alignment completed across menu, PRD, module, schema, and plan references.
- No current runtime functionality changed in Phase 0.

# Tabsan EduSphere - Functionality List

**Project**: Tabsan EduSphere - Comprehensive Educational Management System  
**Version**: Phase 33 (Current)  
**Last Updated**: May 2026

## Startup Configuration Reliability Update (2026-05-27)

- Configuration bootstrap now reliably loads environment profiles from parent/root absolute-path JSON sources, preventing false "missing Environments" startup warnings.
- API and Web startup now consistently resolve `Development` profile values from `src/environments.json`.
- Local development database profile defaults are normalized to LocalDB-compatible values for deterministic local startup smoke checks.
- Operational note: local API smoke validation requires an available SQL target (for example LocalDB instance started) before startup seeding.

## Full Validation Snapshot (2026-05-26)

- Build validation: passed (Debug + Release checks).
- Runtime smoke validation: passed for API/Web startup.
- Test snapshot: `438 passed / 5 failed`.
- Open gap: remaining failures are sidebar role/menu expected-count drift and are tracked for matrix-based test hardening.

## Functionality Synchronization Summary (2026-05-26)

- Institution-aware result calculation settings are active for School/College/University with scoped GPA/component-rule persistence.
- Certificate behavior is institution-aware: university degree/transcript generation and school/college additional certificate management.
- Portal 2FA flow remains fully operational in settings (setup, verify, disable, and test login hand-off).
- Settings/governance coverage is synchronized across report/sidebar/dashboard/license/policy/module/tenant/campus/admin-user controls.

## Runtime Stability Update (2026-05-26)

- Announcement posting now performs strict offering validation and returns controlled bad-request responses for invalid/empty offering selections.
- LMS module creation now includes save-stage FK safety handling to prevent unhandled SQL constraint exceptions.
- Graduation rejection/approval flows now return deterministic conflict messaging for concurrent edits.
- FYP listing reliability improved by removing continuation-based query patterns that could trigger DbContext parallel-operation faults.
- FYP panel-role compatibility expanded to accept legacy database role values (`Internal`/`External`) through safe enum aliases.
- Program/report/menu scope governance remains tenant/campus aware, with role + scope consistency preserved across portal and API paths.

## Certificate Workflow Update (2026-05-26)

- University behavior:
	- Degree generation remains university-only and requires graduate/pass context.
	- Transcript generation supports period-based output (class/semester scoped by selected filter).
- School/College behavior:
	- Admin/SuperAdmin can upload multiple additional student certificates.
	- Students can view and download their own uploaded additional certificates.
- License and policy behavior:
	- Degree/transcript actions are hidden when University is disabled in active policy/license matrix.
- Filter behavior:
	- Period filter label switches by institution context (`Class` for University; `Semester` for non-university).
- Scope behavior:
	- Certificate listing/upload/download operations enforce tenant/campus and role-based scope checks.

---

## Table of Contents

1. [Authentication & Authorization](#authentication--authorization)
2. [User & Access Management](#user--access-management)
3. [Academic Management](#academic-management)
4. [Assessment & Grading](#assessment--grading)
5. [Learning Management](#learning-management)
6. [Communication & Notifications](#communication--notifications)
7. [Attendance & Timetabling](#attendance--timetabling)
8. [Administration & Configuration](#administration--configuration)
9. [Reporting & Analytics](#reporting--analytics)
10. [Advanced Features](#advanced-features)
11. [Controller-Level Feature Surface](#controller-level-feature-surface)
12. [Integration & Support](#integration--support)
13. [Performance & Scalability](#performance--scalability)

---

## Institute Parity Behavior (Stage 7.2)

- **Institution-aware scope**: School, College, and University behavior is governed by license policy and explicit user institution assignment when present.
- **Role + institute guardrails**: Role authorization is combined with institute and assignment scope checks on report, analytics, and lifecycle-sensitive operations.
- **Menu and route consistency**: Hidden sidebar sections are enforced at portal route level, so direct URL navigation cannot bypass role/institute visibility restrictions.
- **Admin/Faculty operational scope**: Department and offering access must align with assignment and institute compatibility for parity-sensitive data surfaces.
- **Student scope model**: Students keep access to student-safe surfaces (for example transcript and dashboard context) while operational admin/faculty report endpoints remain restricted.
- **SuperAdmin scope model**: SuperAdmin retains global visibility and management capability across all licensed institution contexts.

## Advance Enhancements Behavior (Phase 23 Stage 23.2)

- **Dynamic academic vocabulary**: Institution policy now drives shared academic wording for Period, Progression, Grading, Course, and Student Group terms.
- **Vocabulary mapping behavior**:
	- University policy resolves to Semester / Progression / GPA-CGPA / Course / Batch,
	- School policy resolves to Grade / Promotion / Percentage / Subject / Class,
	- College policy resolves to Year / Progression / Percentage / Subject / Year-Group,
	- mixed policy precedence remains deterministic: University, then School, then College.
- **API contract**: Authenticated callers use `GET /api/v1/labels` to retrieve current tenant vocabulary.
- **Portal context behavior**: Dashboard module composition uses this vocabulary to keep one workflow surface while presenting institution-appropriate labels.
**Validation status**: Stage 23.2 integration verification passed (`8/8`) in `DynamicLabelIntegrationTests`.

### Stage 23.3 - Dashboard Context Switching
- Dashboard widgets and metrics are filtered by both role and institution policy (School/College/University).
- No workflow duplication: one configurable core, no cloned modules.
- Integration tests in `DashboardContextSwitchingIntegrationTests` verify:
	- Dashboard widgets adapt for all roles (SuperAdmin/Admin/Faculty/Student) and institution types (School/College/University)
	- Vocabulary adapts in dashboard context for each institution type
	- All tests passing (13/13)
- Implementation: `DashboardCompositionService`, `DashboardCompositionController`, web client and view integration.
- Status: Stage 23.3 completed and validated as of 2026-05-14.

### Stage 24.1 - License Flags
- Institution mode flags (`IncludeSchool`, `IncludeCollege`, `IncludeUniversity`) remain centrally persisted and validated.
- Save guard rejects invalid all-false payloads to keep at least one active institution mode.
- API contract enforcement:
	- `GET /api/v1/institution-policy` is accessible to authenticated roles.
	- `PUT /api/v1/institution-policy` remains restricted to SuperAdmin.
- Integration tests in `InstitutionPolicyLicenseFlagsIntegrationTests` verify:
	- role-based GET accessibility,
	- non-SuperAdmin PUT rejection (`403 Forbidden`),
	- all-false payload rejection (`400 BadRequest`),
	- valid payload persistence and read-back.
- Status: Stage 24.1 completed and validated as of 2026-05-14.

### Stage 24.2 - Backend Enforcement
- Added centralized backend module-license enforcement middleware for API paths.
- Middleware maps API route prefixes to module keys and checks `IModuleEntitlementResolver` before controller execution.
- Disabled modules now return consistent `403 Forbidden` with a clear message.
- Integration tests in `ModuleBackendEnforcementIntegrationTests` verify blocking for representative modules/endpoints:
	- courses (`/api/v1/course`),
	- reports (`/api/v1/reports`),
	- ai_chat (`/api/ai/conversations`),
	- fyp (`/api/v1/fyp/{id}`).
- Validation status: Stage 24.2 integration verification passed (`4/4`).
- Status: Stage 24.2 completed and validated as of 2026-05-14.

### Stage 24.3 - UI/Navigation Filtering
- Sidebar visibility now respects module activation state in addition to role/menu status.
- `GET /api/v1/sidebar-menu/my-visible` applies module-aware filtering so disabled module entries are hidden from navigation output.
- Portal route protection remains aligned through existing action-to-menu key guards, preventing access attempts through hidden menu keys.
- Integration tests in `SidebarMenuIntegrationTests` verify:
	- disabling `courses` hides course navigation entries,
	- disabling `reports` hides report-center/analytics navigation entries,
	- disabling `themes` hides theme-settings navigation entries.
- Validation status: Stage 24.3 sidebar integration verification passed (`17/17` for the suite).
- Status: Stage 24.3 completed and validated as of 2026-05-14.

### Stage 25.1 - Grade/Class Structure
- Student lifecycle progression surfaces now support academic-level terminology instead of fixed semester-only wording.
- API contract:
	- Added `GET /api/v1/student-lifecycle/academic-level-students/{departmentId}/{levelNumber}` for academic-level retrieval,
	- Existing `semester-students` route remains available as backward-compatible alias.
- Service contract:
	- Added `GetStudentsByAcademicLevelAsync(...)` in lifecycle service interface and implementation.
- Portal behavior:
	- Student Lifecycle page loads institution vocabulary from labels API and uses dynamic `PeriodLabel` (Semester/Grade/Year) in controls, tabs, and table headers,
	- Promotion confirmation message updated to "next academic level" wording.
- Integration tests in `StudentLifecycleIntegrationTests` verify academic-level endpoint availability for scoped Admin flows.
- Validation status: Stage 25.1 lifecycle integration verification passed (`4/4` for suite).
- Status: Stage 25.1 completed and validated as of 2026-05-14.

### Stage 25.2 - Stream Support (Grades 9-12)
- Stream assignment enforcement now requires:
	- School institution department context,
	- Grade range 9-12,
	- active target stream.
- Student subject surfaces now apply stream-aware filtering for School Grade 9-12 learners:
	- `GET /api/v1/course/offerings`,
	- `GET /api/v1/course/offerings/my`.
- Supported stream keyword handling includes Science, Biology, Computer, Commerce, and Arts, with safe compatibility fallback for legacy course naming.
- Validation status: Stage 25.2 unit verification passed in `Phase26Tests` (stream constraint scenarios covered).
- Status: Stage 25.2 completed and validated as of 2026-05-14.

### Stage 25.3 - School Grading and Promotion
- School-mode lifecycle promotion now enforces pass-rule checks through progression evaluation before advancing grade level.
- School/College progression score handling now normalizes legacy GPA-scale values (0.0-4.0) into percentage-equivalent values for threshold comparison.
- Student self-progression endpoint now supports current token claim naming (`studentProfileId`) with fallback compatibility (`student_profile_id`).
- Integration tests in `StudentLifecycleIntegrationTests` verify school promotion denial when pass criteria are not met.
- Validation status: Stage 25.3 focused lifecycle integration verification passed.
- Status: Stage 25.3 completed and validated as of 2026-05-14.

### Stage 26.1 - Year-Based Academic Model (College)
- College academic-level retrieval now maps `Year N` to semester range `[2N-1, 2N]` while reusing existing `CurrentSemesterNumber` storage.
- Added lifecycle repository semester-range query support to avoid schema churn.
- College promotions in lifecycle flow now use progression guard checks and advance by two semesters (one academic year) when eligible.
- Unit coverage in `ProgressionServiceTests` verifies college year-step promotion and GPA-to-percentage normalization behavior.
- Integration coverage in `StudentLifecycleIntegrationTests` verifies year-one academic-level retrieval and two-semester advancement on promotion.
- Validation status: focused verification passed (9/9 unit, 7/7 integration).
- Status: Stage 26.1 completed and validated as of 2026-05-14.

### Stage 26.2 - College Result and Promotion (Advanced Track)
- Bulk promotion apply flow now runs progression eligibility checks per promote entry before mutating student level.
- College promote entries that fail progression are converted to `Hold` with supplementary-required reasoning.
- College promote entries that pass progression now advance by an academic year step through progression orchestration.
- Unit coverage in `BulkPromotionServiceTests` validates both supplementary-hold conversion and successful year-step promotion.
- Validation status: focused unit verification passed together with progression checks (14/14).
- Status: Stage 26.2 completed and validated as of 2026-05-14.

### Stage 27.1 - SuperAdmin Grading Setup Sections (Advanced Track)
- Portal grading configuration now exposes explicit institution-specific setup sections for SuperAdmin:
	- School Grading,
	- College Grading,
	- University Grading.
- Each section supports pass-threshold update, optional grade-ranges JSON input, and active/inactive profile state.
- Section saves are routed to institution-type grading profile upsert API endpoints.
- Validation status: web project build verification passed after UI/controller/api-client updates.
- Status: Stage 27.1 completed and validated as of 2026-05-14.

### Stage 27.2 - Rule Application Engine (Advanced Track)
- Enrollment prerequisite pass checks now use institution-type grading profile thresholds rather than a fixed pass percentage.
- Prerequisite repository pass checks now receive threshold as input, enabling policy-driven pass/fail decisions.
- University threshold values configured on GPA scale are normalized to percentage for prerequisite comparison logic.
- Validation status: solution build and full unit suite verification passed (136/136).
- Status: Stage 27.2 completed and validated as of 2026-05-14.

### Stage 28.1 - Parent-Student Mapping (Advanced Track)
- Added controlled parent-student link management operations for Admin users (upsert/list/deactivate).
- Parent link upsert now validates:
	- target account must be Parent role,
	- target student profile must exist,
	- student must belong to School institution scope.
- Parent self-view endpoint remains read-only and returns only active links.
- Validation status: focused `ParentPortalServiceTests` verification passed (5/5) and full solution build passed.
- Status: Stage 28.1 completed and validated as of 2026-05-14.

### Stage 28.2 - Parent Read-Only Views (Advanced Track)
- Added parent read-only linked-student views for results, attendance, announcements, and timetable.
- Enforced active-link authorization checks so parent reads are denied when no active parent-student link exists.
- Added integration coverage for parent-portal linked-student success and role/auth guard behavior.
- Validation status: focused integration tests passed (10/10) and unit tests passed.
- Status: Stage 28.2 completed and validated as of 2026-05-14.

### Stage 28.3 - Parent Notifications (Advanced Track)
- Added parent notification fan-out for result publication events (single and bulk publish paths).
- Added linked-parent attendance warning notifications in background attendance alert workflows.
- Added linked-parent recipients in announcement broadcast fan-out.
- Validation status: API and BackgroundJobs builds passed, parent-portal integration tests passed (10/10), and unit tests passed.
- Status: Stage 28.3 completed and validated as of 2026-05-14.

### Phase 29 - Performance Foundation (MSSQL + Query Discipline)
- Stage 29.1: Added baseline and hot-path composite indexes, including parent-link notification lookup indexing.
- Stage 29.2: Added server-side pagination contracts for high-volume list paths (helpdesk tickets, graduation applications, payment receipts).
- Stage 29.3: Added SQL operations scripts for archive policy, index maintenance, and capacity-growth observability dashboards.
- Validation status: build and targeted/focused test runs passed for stage deliveries.
- Status: Phase 29 completed and validated as of 2026-05-14.

### Phase 30 - Distributed Cache and Background Processing
- Stage 30.1: Added distributed cache coverage for dashboard context, report summary reads, and tenant operations profile reads with invalidation on writes.
- Stage 30.2: Added async analytics export queue pipeline with queue/status/download lifecycle endpoints.
- Stage 30.3: Added worker reliability controls (retry/backoff), consecutive-failure alert signaling, and runtime worker health endpoint (`/health/background-jobs`).
- Validation status: solution build and unit suites passed for stage deliveries.
- Status: Phase 30 completed and validated as of 2026-05-14.

### Phase 31 - Reporting and Analytics Expansion
- Stage 31.1: Added institution-aware report sectioning endpoint (`GET /api/v1/reports/sections`) with School/College/University section grouping.
- Stage 31.2: Added advanced analytics endpoints for top performers, performance trends, and comparative summary.
- Stage 31.3: Added standardized analytics export metadata and advanced analytics export routes (PDF/Excel), including queued export support.
- Validation status: focused report and analytics integration suites passed.
- Status: Phase 31 completed and validated as of 2026-05-14.

### Phase 32 - Communication Enhancements
- Stage 32.1: Added persistent floating AI chatbot launcher and removed primary chatbot access from sidebar/menu paths.
- Stage 32.2: Added optional email fan-out channel for in-app notifications using template-based HTML email dispatch and runtime config toggles.
- Stage 32.3: Added optional Twilio-based SMS fan-out channel with template truncation and environment-secret configuration support.
- Validation status: web/API builds passed with focused notification tests for delivery behavior.
- Status: Phase 32 completed and validated as of 2026-05-14.

### Phase 33 - SaaS and Multi-Tenant Readiness
- Stage 33.1: Added tenant-scope isolation model for tenant operations settings via scoped setting keys and scoped distributed-cache keys.
- Stage 33.2: Subscription plan reads/writes now operate with tenant-scoped boundaries to prevent cross-tenant state leakage.
- Stage 33.3: Onboarding template and tenant profile/branding settings now resolve and persist per tenant scope.
- Added claim/header-based tenant-scope resolution (`tenant_code`, `tenantCode`, `tenant`, `tid`, `X-Tenant-Code`) through API resolver wiring.
- Added backward compatibility fallback for default scope so legacy single-tenant unscoped keys continue to work.
- Validation status: focused unit isolation coverage passed for shared-repository multi-scope access.
- Status: Phase 33 completed and validated as of 2026-05-14.

### Phase 40 - Scoped Program and Menu Governance Alignment
- Scope-aware Program Management:
	- Program APIs and portal workflows now resolve effective tenant/campus scope from claims with superadmin request validation.
	- Program create/update/activate/deactivate paths enforce department-in-scope checks.
	- Program management UI supports tenant/campus selection for superadmin and scoped read/write for non-superadmin users.
- Report Scope Activation Control:
	- Report center activation status is now tenant/campus scoped with dedicated activate/deactivate/status endpoints.
	- Portal settings can toggle report scope state through scoped API operations.
- Sidebar and Module Governance Completion:
	- Sidebar seed synchronization now self-heals existing environments by updating menu role-access and status mappings.
	- Sidebar route guarding and module-key mapping were expanded to cover Programs, Rubric Management, Gradebook, Accreditation templates, and related settings paths.
- User Import Scope Alignment:
	- User import API resolves effective tenant/campus scope using claim-aware validation for consistent onboarding boundaries.
- Status: Phase 40 scoped governance updates implemented and documentation-synchronized.

## Authentication & Authorization

### User Authentication
- **Multi-tenant Login**: Support for multiple roles and institutions
- **Identity Management**: Secure user account creation and management
- **Session Management**: Session tracking and timeout handling
- **Password Management**: Password reset, change, and security policies
- **Token-based Access**: JWT token generation and validation
- **Multi-factor Authentication**: Enhanced security for sensitive operations

### Role-Based Access Control (RBAC)
- **Role Definitions**: SuperAdmin, Admin, Faculty, Student, Parent, Staff
- **Permission Mapping**: Fine-grained permission assignment per role
- **Department Scoping**: Role-based access restricted by department
- **Dynamic Authorization**: Runtime permission evaluation
- **Access Audit Trail**: Logging of all access attempts and changes

---

## User & Access Management

### User Profiles
- **User Account Creation**: Bulk import and individual creation
- **Profile Management**: Personal information, contact details, profile pictures
- **Department Assignment**: User assignment to academic departments
- **Role Management**: Multiple role assignment and management
- **Status Management**: Active/Inactive user status
- **Last Login Tracking**: Monitor user activity

### Batch User Import
- **CSV Import**: Bulk upload of user data
- **Validation**: Data validation during import
- **Error Handling**: Detailed error reporting for failed imports
- **Duplicate Detection**: Prevent duplicate user entries

### License & Module Management
- **License Activation**: License key validation and activation
- **Module Activation**: Enable/disable specific modules per institution
- **Concurrent User Limits**: Enforce maximum user limits
- **Feature Flags**: Enable/disable features with emergency rollback
- **Usage Tracking**: Monitor feature and module usage
- **License Renewal**: Manage license expiration and renewal
- **Institution Scope License Binding**: Enforce School, College, University, or mixed institution modes from imported license.
- **Mixed-Mode Function Union**: When two or three institution scopes are licensed, expose full combined modules, configuration, and workflows.
- **Institution-Aware Data Visibility**: Restrict charts, tables, menus, and reports by licensed institution scope plus user assignment.
- **Institution-Aware User Provisioning**: SuperAdmin can assign institution scope on manual user creation and CSV import.
- **Institution-Specific Lifecycle Routing**: Student lifecycle and grading paths resolve by assigned institution mode.

### Institution Validation Phases
- **Execution Plan**: `Docs/Institution-License-Validation-Phases.md` defines 7 phases covering license binding, lifecycle, mixed-mode behavior, UI/report correctness, user import assignment, access boundaries, and SuperAdmin permission coverage.
- **Per-Phase Evidence**: Each phase must include `Implementation Summary`, `Validation Summary`, and `Status of Checks Done`.
- **Phase-End Sync**: After each phase, update required documentation and run commit/pull/push workflow.
- **Current Execution Snapshot (Phase 1)**: Authentication, license activation, policy binding, and module restriction checks succeeded; Phase 1 is complete for University-only license validation.
- **Current Execution Snapshot (Phase 2)**: School, College, and University lifecycle checks are validated with mode-specific policy flags, labels, capability matrix rows, and progression outcomes captured in `Docs/Institution-License-Validation-Phases.md`.
- **Current Execution Snapshot (Phase 3)**: Mixed-mode license combinations are validated with union policy behavior across School+College, School+University, College+University, and School+College+University.
- **Phase 3 Evidence Highlights**: Capability matrix union counts and persisted `portal_settings` flags match licensed combinations; progression endpoint remains institution-type deterministic across all combinations.
- **Validation Caveat (Current Tooling)**: Sequential mode activation in one environment currently requires resetting consumed verification keys because generated licenses reuse the same verification key.
- **Current Execution Snapshot (Phase 4)**: Charts/tables/menus/reports behavior is validated for School, College, and University across SuperAdmin, Admin, Faculty, and Student using mode-role evidence sweep artifacts in `Artifacts/Phase4/ModeRole/20260512-142021`.
- **Phase 4 Evidence Highlights**: Role-scoped dashboard context, labels vocabulary, and capability matrix rows align with mode flags; report data/export endpoints succeed for valid SuperAdmin/Admin/Faculty scopes and remain blocked for Student operational access.
- **Current Execution Snapshot (Phase 5)**: Manual admin creation and CSV import now support explicit per-user `institutionType` assignment with policy enforcement and persistence (`Artifacts/Phase5/Api/*_20260512-144212.json`).
- **Phase 5 Evidence Highlights**: Disabled institution assignment is rejected during CSV import; enabled assignment is accepted; admin list/create responses now include institution assignment where present.
- **Current Execution Snapshot (Phase 6)**: Institution data-access boundaries are validated for Admin, Faculty, and Student using scoped report export checks and role-allowed read surfaces (`Artifacts/Phase6/Access/20260512-150824/RunSummary.json`).
- **Phase 6 Evidence Highlights**: Admin and Faculty get `200` only for assigned department/offering scope, non-assigned scope is denied (`403`), missing scope is rejected (`400`), and Student remains blocked from operational report exports while catalog/dashboard context remain accessible (`200`).
- **Current Execution Snapshot (Phase 7)**: SuperAdmin full-access matrix is validated for CRUD, activation/deactivation, and cross-institution privileged visibility across School/College/University mode switches (`Artifacts/Phase7/SuperAdmin/20260512-151302/RunSummary.json`).
- **Phase 7 Evidence Highlights**: All SuperAdmin matrix checks succeeded (`35/35`), including department lifecycle actions, admin-user lifecycle actions, policy mode switching, and privileged dashboard/report/license access in every institution mode.

---

## Academic Management

### Programs & Curriculum
- **Program Management**: Create and manage academic programs (Bachelor's, Master's, etc.)
- **Program Tracks**: Multiple specialization tracks within programs
- **Degree Audit**: Track student progress toward degree requirements
- **Graduation Management**: Process student graduation and transcript generation
- **Credit System**: Manage credit hours and course weights
- **Program Outcomes**: Define and track learning outcomes

### Courses & Curriculum
- **Course Creation**: Add courses with syllabus and learning outcomes
- **Course Sections**: Multiple sections of the same course
- **Prerequisite Management**: Define course prerequisites and co-requisites
- **Course Mapping**: Link courses to programs and tracks
- **Course Code Management**: Standardized course coding system
- **Course Catalog**: Searchable course directory

### Enrollment & Registration
- **Student Enrollment**: Register students in courses and programs
- **Enrollment Status**: Track enrollment state (Enrolled, Dropped, Completed)
- **Class Capacity Management**: Set and enforce section capacity limits
- **Enrollment Verification**: Confirm and lock enrollments
- **Retroactive Enrollment**: Historical enrollment adjustments
- **Timetable Clash Detection**: Prevent scheduling conflicts
- **Waitlist Management**: Manage course waitlists

### Semesters & Schedules
- **Semester Management**: Define academic semesters and terms
- **Semester Status**: Open, Active, Closed, Archived states
- **Semester Calendar**: Important dates and deadlines
- **Add/Drop Period**: Configurable enrollment modification window
- **Grading Periods**: Define when grades are due

---

## Assessment & Grading

### Assignments
- **Assignment Creation**: Create and publish assignments
- **Assignment Types**: Homework, Projects, Practical work
- **Submission Management**: Student submissions and deadline enforcement
- **Late Submission Handling**: Grace periods and penalty rules
- **File Upload**: Multiple file format support for submissions
- **Peer Review**: Peer assessment and feedback
- **Rubric-Based Grading**: Defined grading criteria and rubrics

### Quizzes & Exams
- **Quiz Creation**: Multiple choice, true/false, short answer questions
- **Question Bank**: Reusable question library
- **Randomized Questions**: Random question selection per student
- **Time Limits**: Timed quiz delivery
- **Attempt Limits**: Control number of attempts
- **Instant Feedback**: Real-time quiz result feedback
- **Exam Scheduling**: Schedule and manage exams

### Gradebook
- **Grade Entry**: Manual grade input by faculty
- **Weighted Calculations**: Weighted average calculations
- **Grading Strategies**: GPA, Percentage, Letter grade conversion
- **Grade Components**: Assignments, quizzes, exams, participation
- **Grade Visibility**: Student-accessible gradebook
- **Grade Locks**: Prevent accidental grade changes
- **Bulk Grade Import**: Import grades from external sources

### Report Cards & Transcripts
- **Report Card Generation**: Semester-based performance reports
- **GPA Calculation**: Cumulative and semester GPA
- **Transcript Generation**: Official academic transcripts
- **Grade Distribution**: Class grade statistics and curves
- **Performance Analytics**: Student performance trends
- **Degree Audit Report**: Progress toward degree completion

---

## Learning Management

### Learning Management System (LMS)
- **Course Content**: Upload and organize learning materials
- **Modules & Lessons**: Structured content organization
- **Content Types**: Support for multiple media formats (PDF, Video, Images, Documents)
- **Course Announcements**: Faculty to student communications
- **Discussion Forums**: Collaborative learning discussions
- **Discussion Moderation**: Monitor and moderate discussions

### Study Planner
- **Study Schedule**: Personal study planning tools
- **Task Management**: Organize learning tasks and deadlines
- **Progress Tracking**: Monitor learning progress
- **Resource Organization**: Central repository for learning materials

### AI Chat Integration
- **AI Assistant**: ChatGPT-based educational assistant
- **Query Support**: Answer academic questions and clarifications
- **Learning Support**: Provide study help and explanations
- **Multi-turn Conversations**: Contextual conversation support
- **Conversation History**: Save and retrieve past conversations

### Timetable Management
- **Timetable Creation**: Schedule classes and sessions
- **Room Assignment**: Assign rooms and facilities
- **Instructor Assignment**: Assign faculty to sessions
- **Timetable View**: Calendar and list views
- **Conflict Detection**: Prevent scheduling conflicts
- **Student Schedule**: Personal class schedule access

### Attendance Management
- **Attendance Tracking**: Record attendance per session
- **Attendance Methods**: In-class marking, QR code, Biometric support
- **Attendance Reports**: Generate attendance reports
- **Absence Notifications**: Alert on high absenteeism
- **Attendance Policies**: Configurable attendance requirements
- **Attendance Verification**: Faculty and student verification

---

## Communication & Notifications

### Notifications System
- **Notification Types**: Email, SMS, In-app notifications
- **Event-Driven Notifications**: Automatic notifications for system events
- **Notification Templates**: Customizable notification messages
- **Notification Preferences**: User control over notification settings
- **Notification History**: Searchable notification archive
- **Fan-out Dispatch**: Send to multiple recipients efficiently
- **Notification Scheduling**: Schedule notifications for specific times

### Announcements
- **Create Announcements**: Faculty and admin announcements
- **Target Audience**: Send to specific groups or individuals
- **Announcement Scheduling**: Schedule future announcements
- **Pin Important**: Mark important announcements
- **Expiration Dates**: Archive old announcements

### Discussions
- **Create Discussion Threads**: Start discussions in courses
- **Thread Replies**: Nested discussion responses
- **Thread Status**: Open, Closed, Archived discussions
- **Discussion Moderation**: Review and approve posts
- **Participant List**: View discussion participants
- **Discussion Search**: Full-text search in discussions

---

## Attendance & Timetabling

### Class Timetable
- **Class Scheduling**: Schedule classes with day/time/location
- **Multi-Instructor Classes**: Support for co-taught classes
- **Recurring Classes**: Weekly recurring schedule patterns
- **Room & Equipment**: Facility and resource allocation
- **Timetable Optimization**: Minimize conflicts and maximize efficiency
- **Timetable Visualization**: Multiple view options

### Attendance Tracking
- **Session-Based Attendance**: Mark attendance per class session
- **Bulk Attendance**: Mark multiple students at once
- **Attendance Status**: Present, Absent, Late, Excused
- **Late Submissions**: Grace period for late attendance entry
- **Attendance Analysis**: Identify attendance patterns
- **Absent Notifications**: Alert relevant parties

---

## Administration & Configuration

### System Settings
- **Institution Configuration**: Configure institution name, logo, branding
- **Theme Management**: Dark/light themes, color schemes
- **Email Configuration**: SMTP settings for notifications
- **SMS Configuration**: SMS gateway integration
- **System Parameters**: Global system configuration
- **API Keys**: Manage API keys for integrations

### Department Management
- **Department Creation**: Create academic departments
- **Department Hierarchy**: Parent-child department relationships
- **Department Heads**: Assign department leadership
- **Department Budgets**: Allocate departmental budgets
- **Department Policies**: Configure department-specific settings

### Dashboard & Analytics
- **Admin Dashboard**: Key metrics and system status
- **Faculty Dashboard**: Teaching load, student progress
- **Student Dashboard**: Grades, announcements, upcoming deadlines
- **Custom Reports**: Configurable reporting
- **Data Visualization**: Charts and graphs
- **Export Functionality**: Export data in multiple formats

### Audit & Compliance
- **Audit Logging**: Track all system changes
- **User Activity Logs**: Monitor user actions
- **Access Logs**: Record access attempts
- **Data Change Audit**: Track data modifications
- **Report Generation**: Generate compliance reports
- **Data Retention**: Manage data retention policies

### User Import & Management
- **Bulk User Import**: CSV-based user import
- **User Roles**: Assign roles during import
- **Validation Rules**: Define import validation
- **Error Reports**: Detailed error logging
- **Duplicate Handling**: Prevent duplicate entries
- **Import History**: Track import operations

---

## Reporting & Analytics

### Academic Reports
- **Class Reports**: Student performance by class
- **Program Reports**: Program-wide performance analytics
- **Transcript Reports**: Official student transcripts
- **Grade Distribution**: Class grade statistics
- **Performance Trends**: Historical performance analysis
- **Predictive Analytics**: Early warning for struggling students

### Administrative Reports
- **Enrollment Reports**: Enrollment statistics and trends
- **Attendance Reports**: Attendance summary and analysis
- **Faculty Workload**: Teaching load and resource allocation
- **User Reports**: Active users, role distribution
- **System Usage**: Feature usage statistics
- **License Usage**: License utilization and compliance

### Custom Reports
- **Report Builder**: Create custom reports
- **Data Filtering**: Filter by department, semester, role
- **Scheduling**: Schedule automated report generation
- **Email Distribution**: Automatic email delivery
- **Export Formats**: PDF, Excel, CSV export
- **Report Library**: Save and reuse reports

---

## Advanced Features

### Final Year Project (FYP)
- **Project Creation**: Students propose FYP topics
- **Project Proposal**: Structured proposal submission
- **Supervisor Assignment**: Assign faculty supervisors
- **Co-supervisor Support**: Multiple supervisor support
- **Progress Tracking**: Track project milestones
- **Final Submission**: Project submission and evaluation
- **Defense Scheduling**: Schedule project defense dates
- **Evaluation**: Project assessment and grading

### Helpdesk & Support
- **Ticket Creation**: Create support requests
- **Ticket Categorization**: Assign ticket categories
- **Priority Levels**: Set ticket priority
- **Ticket Assignment**: Assign to support staff
- **Ticket Status**: Open, In Progress, Resolved, Closed
- **Knowledge Base**: FAQ and help articles
- **SLA Management**: Service level agreements

### Search Functionality
- **Full-Text Search**: Search across courses and content
- **Advanced Search**: Filter by multiple criteria
- **Search Indexing**: Fast indexed search
- **Search Analytics**: Popular search queries
- **Auto-complete**: Search suggestions

### Library System
- **Book Management**: Catalog library books
- **Book Checkout**: Borrow and return books
- **Reservation System**: Reserve unavailable books
- **Fine Management**: Track overdue books and fines
- **Digital Resources**: E-books and journals
- **Library Catalog**: Searchable library database

---

## Controller-Level Feature Surface

This section captures implementation-level functionality, including smaller operational features exposed through API controllers and supporting services.

### Security, Governance, and Platform Controls
- **Account Security Operations**: Password reset and account-unlock workflows, locked-account review paths, and account security activity handling.
- **Feature Flag Control Plane**: Runtime feature toggles for controlled rollout, emergency disable, and policy-safe activation boundaries.
- **Portal Capability Matrix**: Role and module capability exposure for portal composition and UI contract alignment.
- **Institution Policy and Licensing**: Institution mode policy management, module entitlement enforcement, and license activation/validation contracts.
- **Module Registry and Activation**: Module registration, activation-state management, and visibility governance.
- **Theme and Branding Operations**: Theme management, branding settings, logo/media support, and tenant profile branding settings.
- **Sidebar Governance**: Role + module-aware sidebar visibility payloads and route/menu consistency support.
- **Configuration and Reporting Governance**: Report settings, portal settings, and communication integration settings APIs.

### Academic Operations and Rule Engines
- **Accreditation Management**: Accreditation records and governance actions for academic quality tracking.
- **Program and Course Governance**: Program, course, and offering lifecycle management with prerequisite and grading-rule support.
- **Department and Facility Ops**: Department assignment flows, buildings, and room operations.
- **School Stream Operations**: Stream assignment and stream-aware subject filtering for School Grade 9-12 scenarios.
- **Prerequisite and Enrollment Rules**: Threshold-aware prerequisite checks and enrollment guard enforcement.
- **Result and Gradebook Engine**: Result entry/publish paths, result calculation endpoints, grading configurations, and rubric support.
- **Bulk Promotion Operations**: Promotion batch operations with progression-aware hold/promote decisions.

### Student Journey and Lifecycle Features
- **Student Lifecycle Controls**: Academic-level progression views, promote/hold/graduate paths, and institution-aware lifecycle scoping.
- **Progression Tracking**: Student progression summaries and institution-aware threshold interpretation.
- **Degree Audit and Report Cards**: Degree completion analysis and structured report-card generation.
- **Graduation Workflows**: Graduation application, candidate management, and certificate lifecycle support.
- **Student Profile and Registration Tools**: Student profile operations and registration import support.

### Learning, Engagement, and Support Features
- **LMS Operations**: Learning module/content endpoints and delivery metadata support.
- **Study Planner**: Study plan and planning workflow support for students and faculty scope.
- **Calendar and Deadline Features**: Academic calendar and deadline/reminder management support.
- **Discussion and Announcement Features**: Discussion threads, announcements, and role-aware communication publishing.
- **Helpdesk and Ticketing**: Ticket creation, assignment, responses, and lifecycle status operations.
- **Search Services**: Cross-entity search and typed result discovery.
- **AI Chat Operations**: AI chat conversation endpoints plus floating-launcher portal experience.
- **Parent Portal Features**: Parent-student link management, linked-student read-only views, and parent notification fan-out support.

### Communication, Notifications, and Integrations
- **Notification Channels**: In-app notification fan-out with optional email and SMS channel dispatch support.
- **Communication Integration APIs**: Configurable communication integration endpoints for runtime channel setup.
- **Attendance-Driven Alerts**: Attendance warning and related notification pathways.
- **Payment and Receipt Operations**: Payment receipt management and supporting upload/media paths.
- **Library Integration Paths**: Catalog and circulation-related API support for library workflows.

### Reporting, Analytics, Exports, and Jobs
- **Report Catalog and Generation**: Role/institute scoped report discovery and report generation endpoints.
- **Report Export Jobs**: Queued report export creation, status tracking, and download workflows.
- **Analytics Summaries**: Attendance/performance/assignment analytics plus top-performers, trends, and comparative summaries.
- **Analytics Export Jobs**: Async analytics export job queue, status, and result retrieval endpoints.
- **Institution-Aware Report Sections**: School/College/University section partition APIs for report UIs.

### Operations and Runtime Reliability
- **Health Surfaces**: Background jobs, scaling, and observability health endpoints for runtime diagnostics.
- **Queue and Worker Infrastructure**: Background processing support for notifications, exports, and heavy workloads.
- **Distributed Cache Behavior**: Cached dashboard/report/tenant-setting reads with scoped invalidation behavior.
- **Media Storage Reliability**: Provider-backed media persistence and signed-read URL compatibility workflows.

---

## Integration & Support

### Campus Infrastructure
- **Student Information**: Integration with SIS
- **HR System**: Faculty and staff data
- **Financial System**: Fee and payment integration
- **Building Management**: Room and facility data
- **Transportation**: Bus and transport integration
- **Hostel Management**: Accommodation tracking

### Feature Flags & Configuration
- **Feature Toggle**: Enable/disable features dynamically
- **Gradual Rollout**: Phase feature releases
- **Emergency Rollback**: Quickly disable problematic features
- **Feature Analytics**: Track feature adoption
- **A/B Testing**: Test feature variations
- **Capabilities Matrix**: Advertise available features

### System Monitoring
- **Performance Monitoring**: Track system performance
- **Error Tracking**: Monitor application errors
- **User Analytics**: Track user behavior
- **System Health**: Overall system status
- **Backup & Recovery**: Data backup and restore
- **Disaster Recovery**: Business continuity planning

### Institute Parity Monitoring and Support (Stage 7.3)
- **Report Failure Monitoring**: Watch for spikes in report endpoint `400/403/500` responses on analytics, report catalog, and export flows.
- **Analytics Health Checks**: Validate that institute-filtered analytics queries return data for the selected School, College, or University context.
- **Scope Mismatch Triage**: Distinguish expected authorization denials from true defects by checking role assignment, department assignment, and institution claim alignment first.
- **Support Escalation Signal**: Escalate only after confirming the target user role, department/offering scope, active module status, and selected institute filter.
- **Operational Logging**: Preserve request identifiers, endpoint name, role, institution type, and filter values when opening parity-scope incidents.

### Institute Parity Release Exit Criteria (Stage 7.4)
- **Release Readiness**: Phase 7 parity documentation, monitoring guidance, and support handoff are synchronized and complete.
- **Regression Boundary**: Stage 7.4 performs closeout only and does not change runtime behavior, authorization rules, or schema shape.
- **Roadmap Handoff**: Phase 7 closeout hands off to the next roadmap stage without reopening parity remediation work.

### Data Management
- **Data Import/Export**: Multiple data formats
- **ETL Pipelines**: Data transformation workflows
- **Database Maintenance**: Optimization and cleanup
- **Data Archiving**: Archive old data
- **Data Privacy**: GDPR compliance and data protection
- **Data Security**: Encryption and access control

---

## Performance & Scalability

### High-Load Optimization (Phase 1)
- **Connection Pool Hardening**: Tuned SQL connection pools and timeouts across environment profiles for higher concurrency stability.
- **Hot Query Optimization**: No-tracking and split-query optimizations on inbox/sidebar read-heavy paths.
- **Short-TTL Data Caching**: Added short-lifetime cache windows for dashboard composition, sidebar visibility, and notification inbox/badge reads.
- **Safe Cache Invalidation**: Version-based invalidation on write/mutation flows to reduce stale data risk while preserving read performance.
- **Load-Test Validation Workflow**: Stage-by-stage validation with unit/integration gates and k6 progressive scale checks.

### Horizontal API Scaling (Phase 2)
- **Multi-Instance Node Identity**: Each API node can expose a unique instance id via configuration for scale-out deployments.
- **Instance Distribution Traceability**: Optional response header (`X-EduSphere-Instance`) allows request distribution validation behind load balancers.
- **Per-Node Health Visibility**: Dedicated instance health endpoint (`/health/instance`) reports node identity and uptime.
- **Scale-Out Operations Script**: Local multi-instance API launcher script supports fast Stage 2.1 baseline verification.
- **Least-Connections Load Balancing**: Stage 2.2 baseline includes Nginx least-connections policy template for active-connection-aware API request routing.
- **Load-Balancer Distribution Validation**: Automated request sampling script summarizes per-instance load share to verify balancing behavior.
- **Stateless Runtime Enforcement**: Production requires shared distributed cache and shared data-protection keys so API/Web nodes behave identically across scale-out instances.

### Endpoint Aggregation (Phase 3)
- **Dashboard Context Aggregation**: The ModuleComposition screen now receives visible modules, vocabulary, and dashboard widgets in one API response.
- **Reduced Portal Round-Trips**: One aggregated request replaces the prior three-call composition flow for the dashboard composer.

### Async and Non-Blocking IO (Phase 3)
- **Repository Async Cleanup**: Hot timetable, settings, quiz, and building/room repository reads now use direct awaited EF calls instead of task continuation bridging.
- **Non-Blocking Data Access**: Remaining high-traffic reads stay fully asynchronous so request threads are not tied up unnecessarily.

### Transport Optimization (Phase 3)
- **Compression**: Brotli and Gzip response compression remains enabled for HTTPS responses.
- **Connection Tuning**: API and Web hosts use Kestrel keep-alive and request-header timeout tuning for cleaner transport behavior.
- **Header Reduction**: Server headers are suppressed to reduce wire chatter on high-volume responses.

### Caching Strategy (Phase 4)
- **API Distributed Cache Policy**: High-cost analytics report reads now use short-TTL distributed cache entries so repeated dashboard/report requests avoid repeated heavy DB aggregation.
- **Edge and Static Caching**: Web static assets now emit configurable `Cache-Control` headers suitable for CDN/edge caching.
- **Cache Scope Control**: Shared cache keys are scoped by report type and department, and dynamic/authenticated MVC responses remain outside static cache policy.

### Load Testing Improvements (Phase 5)
- **Realistic Throughput Model**: High-scale k6 profiles now use request-rate ramps (`ramping-arrival-rate`) instead of only virtual-user ramps.
- **User Think-Time Simulation**: Randomized think-time windows now approximate user pacing across dashboard/sidebar/notification flows.
- **Distributed Generator Sharding**: Scale runs support generator-aware load splitting (`GENERATOR_TOTAL`, `GENERATOR_INDEX`) for multi-machine execution.
- **Output Discipline**: Summary-first output is now the default for routine runs, while heavy raw JSON output remains explicit diagnostics-only mode.

### Dependency Optimization (Phase 6)
- **External Call Caching**: Safe external library loan reads now use short-TTL distributed cache to reduce repeated outbound calls.
- **Resilience Hardening**: Outbound integration channels now include configurable circuit-breaker controls alongside retry and timeout behavior.
- **Blocking Risk Reduction**: Gradebook request composition removed sync-over-async result reads to keep request processing fully asynchronous.

### Background Processing (Phase 7)
- **Queue Offloading Expansion**: Account-security unlock/reset email sends now enqueue background work items to keep request latency predictable.
- **Queue Platform Selection**: Queue processing supports startup-configurable in-memory channel mode and RabbitMQ mode based on deployment model.
- **Worker-Based Delivery**: Dedicated workers process queued account-security email work items outside request handlers.

### Infrastructure Tuning (Phase 8)
- **Auto-Scaling Policy Baseline**: API, Web, and BackgroundJobs now expose startup-validated `InfrastructureTuning:AutoScaling` policy controls for deployment-safe replica scaling bounds.
- **Host Concurrency Controls**: `InfrastructureTuning:HostLimits` now drives thread-pool minimums and API/Web Kestrel concurrent-connection limits.
- **Network Throughput Tuning**: `InfrastructureTuning:NetworkStack` now controls HTTP/2 stream/keep-alive limits and outbound connection pooling limits for high-volume traffic.
- **Scaling Diagnostics**: API now exposes `/health/scaling` to verify effective infrastructure tuning baseline at runtime.

### Monitoring and Observability (Phase 9)
- **Prometheus Metrics Stack**: API now publishes OpenTelemetry metrics with Prometheus scraping support at `/metrics`.
- **Latency SLO Snapshots**: Rolling request timing snapshots expose p50, p95, and p99 latency summaries at `/health/observability`.
- **Runtime Health Coverage**: API health checks now cover database connectivity, CPU pressure, memory pressure, network resolution, and rolling error-rate thresholds.
- **Continuous Monitoring Surface**: The observability stack is designed to feed dashboards, alerts, and SLO checks without requiring schema changes.

### Progressive Load Strategy (Phase 10)
- **Incremental Scale Gates**: Phase 10 now runs stepwise gate plans across 10k, 20k, 50k, 80k, 100k, and higher tiers using a reusable orchestrator.
- **Bottleneck Isolation**: Each gate emits a bottleneck class so operators can identify whether the first limiter is API, database/dependency, infra, rate-limiting, or contract/authz related.
- **Fix-and-Retest Cycle**: The gate runner supports repeated retest attempts so a targeted fix can be revalidated against the same stage before promotion.
- **Reusable Scenario**: A single parameterized k6 scenario now supports the progressive and extended gate plans without duplicating test logic.

---

## Architecture & Technical Details

### Technology Stack
- **Backend**: ASP.NET Core Web API
- **Frontend**: Web-based user interface
- **Database**: MSSQL Server
- **Authentication**: JWT Tokens
- **Messaging**: Notification System
- **AI Integration**: OpenAI ChatGPT

### Design Patterns
- **Repository Pattern**: Data access abstraction
- **Service Pattern**: Business logic layer
- **DTO Pattern**: Request/response models
- **Dependency Injection**: IoC container management
- **Clean Architecture**: Layered application design

### Key Entities
- **Users**: Students, Faculty, Admin, Parents
- **Courses**: Academic course definitions
- **Enrollments**: Student course registrations
- **Grades**: Academic performance records
- **Assignments**: Coursework and projects
- **Departments**: Academic organizational units
- **Programs**: Degree programs and qualifications

---

## Summary Statistics

| Metric | Count |
|--------|-------|
| **API Controllers** | 63+ |
| **Core Services** | 50+ |
| **Application Modules** | 14 |
| **Major Features** | 45+ |
| **Supported Roles** | 6 |
| **Database Entities** | 50+ |
| **API Endpoints** | 200+ |

---

## Related Documentation

For more detailed information, see:
- [Function-List.md](Function-List.md) - Detailed function reference
- [Advance-Enhancements.md](Advance-Enhancements.md) - Planned enhancements
- [Phase31-Stage31.2-Security-Hardening.md](Phase31-Stage31.2-Security-Hardening.md) - Security details
- [PRD.md](../Project%20startup%20Docs/PRD.md) - Product requirements

---

**Last Updated**: May 14, 2026  
**Status**: Phase 33 complete, Phase 34 next
