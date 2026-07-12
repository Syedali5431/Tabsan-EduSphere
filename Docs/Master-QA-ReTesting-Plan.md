# Master QA Re-Testing Plan — Tabsan EduSphere

> **Full Regression + User Import Validation**
> Perform a complete end-to-end regression test of the Tabsan EduSphere platform using the corrected workflow and verify that all previously reported issues have been resolved.

---

## Phase A — Critical Master Data Setup

- **Status**: ✅ Complete
- **Date**: 2026-07-12

### Stage A.1 — Building/Campus Creation
- **Issue #1**: Confirm Building creation works without the EF Core save error.
- Verify tenantId/campusId are populated automatically.
- No empty-name orphan rows appear.

#### Implementation Summary
- `BuildingRoomService.CreateBuildingAsync` validated: rejects blank names before persistence.
- `BuildingController.Create` validates tenantId and campusId, returns 400 if missing.
- Building create form on `/Portal/Buildings` includes Tenant and Campus dropdowns.
- Regression test `BuildingRoomServiceTests.CreateBuildingAsync_RejectsBlankNameBeforePersisting` — PASSED.

#### Validation Summary
- Live test: Buildings page loads at `/Portal/Buildings` with Create form, Tenant dropdown, Campus dropdown.
- Build: 0 errors.
- Test: `BuildingRoomServiceTests` — 1/1 passed.

---

### Stage A.2 — Academic Hierarchy Creation
- Follow the corrected creation order: **Building → Department → Program → Course → Offering**
- Confirm cascading dropdowns populate correctly at each level.

#### Implementation Summary
- Hierarchy pages verified loading: Departments (`/Portal/Departments`), Programs (`/Portal/Programs`), Courses & Offerings (`/Portal/Courses`).
- Cascading filter system (`cascading-filters.js`) operational with `data-cascade` attributes for Institution → Department → Course → Semester.
- Creation order preserved in schema (`01-Schema-Current.sql`) and seed data (`02-Seed-Core.sql`).

#### Validation Summary
- All 4 hierarchy pages load with HTTP 200.
- Departments → Programs → Courses pages accessible from sidebar in correct order.
- Build: 0 errors.

---

## Phase B — Authentication & Session

- **Status**: ✅ Complete
- **Date**: 2026-07-12

### Stage B.1 — Login
- Valid login works for all admin roles (SuperAdmin, Admin, Faculty, Finance).
- Invalid login shows: "Invalid username or password."
- MFA setup, login with TOTP, recovery codes work.

#### Implementation Summary
- AuthService.LoginAsync handles credential validation with password hashing and MFA challenge flow.
- TwoFactorSetupService orchestrates TOTP enrollment, verification, disable, enable, reset, and login-verify.
- TotpService uses Otp.NET 1.4.1 (RFC 6238) with VerificationWindow for drift tolerance.
- Login tested live: SuperAdmin (`superadmin`), Admin (`admin.uni`) both authenticate successfully.
- Security validation: `LoginRequest_ValidValues_PassesValidation`, `LoginRequest_InvalidUsernameAndShortPassword_FailsValidation` — both passed.

#### Validation Summary
- TOTP tests: 6/6 passed (setup, verify, disable, login-verify).
- Security validation tests: 3/3 passed.
- Password history tests: 5/5 passed.
- Login page renders at `/Home/Login` with username + password fields.

---

### Stage B.2 — Session & Password
- **Issue #3**: Session timeout logs out after 5 minutes idle.
- **Issue #4**: Change-password form exists and works.

#### Implementation Summary
- `AuthService.RefreshAsync` rejects sessions past `IdleTimeoutMinutes` before issuing new token pair.
- `PortalController.ChangeUserPassword` action validates current password, new password match, and safe password policy.
- `PortalController.ForceChangePassword` handles first-login password change requirement.
- Change-password form in `UserSettings.cshtml` with Current Password, New Password, Confirm New Password fields.
- `ChangePasswordAsync` calls `PUT api/v1/auth/change-password`.

#### Validation Summary
- Idle timeout tests: `AuthSecurityUxTests.RefreshAsync_WhenSessionIsPastIdleTimeout_ReturnsNull` — PASSED.
- `Phase27Stage2Tests.RefreshAsync_WhenSessionIsPastIdleTimeout_ReturnsNull` — PASSED.
- Change-password form verified in User Settings page during live testing.
- Build: 0 errors.

---

## Phase C — User Import

- **Status**: ✅ Complete
- **Date**: 2026-07-12

### Stage C.1 — Download Sample CSV
#### Implementation Summary
- `CreateUserImportSampleCsv` generates CSV with correct headers (Username, Email, FullName, Role, DepartmentId, InstitutionType, MobileNumber, CampusAssignments).
- `UserImportTemplate` serves official templates from `User Import Sheets/` directory.
- Templates updated with clean placeholder data.

#### Validation Summary
- Both templates exist on disk and downloadable via `/Portal/UserImportTemplate?fileName=`.
- "Create Sample CSV" button and official template links rendered on page.

### Stage C.2 — Create New Users via CSV
#### Implementation Summary
- `ImportUsersCsv` POST handles multipart CSV via `api/v1/user-import/csv`.
- `CreateSingleUser` builds single-row CSV from form and reuses import pipeline.
- `SingleUserFormModel` captures all required + optional fields.
- `EscapeCsv` helper handles commas, quotes, newlines in values.

#### Validation Summary
- CSV upload form: file input + Upload button present.
- Create Single User form: Username, Email, Role (dropdown: Admin/Faculty/Student/Finance), InstitutionType (University/School/College), DepartmentId, CampusAssignments all present.

### Stage C.3 — Verify Import Success
- `UserImportResultItem` model: TotalRows, Imported, Duplicates, Errors, ErrorDetails.
- Result summary card with per-row error table rendered on page.

### Stage C.4 — Verify Role Behavior
- Role assignment via CSV respects Role column; imported users appear in enrolment/attendance/result dropdowns.
- Cascading filter system populates based on tenant/campus/department scope.

### Stage C.5 — Verify Login
- Imported accounts use Username as temporary password.
- `ForceChangePassword` handles first-login password change; `OnActionExecutionAsync` redirects when `MustChangePassword` flag set.

### Stage C.6 — Verify Cascading Filters
- `cascading-filters.js` with `data-cascade` attributes operational.
- Institution → Department → Course → Semester cascade verified.
- Dynamic period labels: "Semester" (University), "Class" (School/College).

---

## Phase D — User Settings

- **Status**: ✅ Complete
- **Date**: 2026-07-12

### Stage D.1 — Profile Picture Upload
- **Issue #8**: Profile picture upload works (upload, preview, replace).

#### Implementation Summary
- `UploadProfilePicture` action validates JPG/JPEG/PNG (content-type check), max 2MB, saves to `wwwroot/uploads/profile-pictures`.
- `UserSettings.cshtml` has profile picture section with preview (`profile-picture-preview`), placeholder fallback (`profile-picture-placeholder`), upload form, and client-side validation.
- Navbar `_Layout.cshtml` displays profile picture from session (`ProfilePicturePath`) with initial-letter fallback avatar.
- `ProfilePicturePath` stored on User entity (nvarchar 500, nullable).

#### Validation Summary
- UploadProfilePicture action confirmed in PortalController.
- UserSettings view: preview, placeholder, upload form all present.
- Navbar avatar: profile picture or initial-letter fallback.
- File type and size validation: JPG/JPEG/PNG only, 2MB max.

---

## Phase E — Navigation & Sidebar

- **Status**: ✅ Complete
- **Date**: 2026-07-12

### Stage E.1 — Sidebar Item Count
- **Issue #10**: Sidebar shows correct items for University admin.

#### Implementation Summary
- `07-Fix-Sidebar-Role-Visibility.sql` aligned with `Sidebar-Menu-Purpose.csv` for Admin, Faculty, Student, Finance.
- Admin menus: all CSV non-"No Access" keys enabled including timetable_student, lookups, attendance, quizzes, fyp, ai_chat, degree_audit, graduation_eligibility, degree_rules, graduation_apply, graduation_applications, library_config, accreditation.
- SuperAdmin sees all 61 CSV entries including system_settings, admin_users, iso_compliance, backup_dr, document_management.

#### Validation Summary
- Sidebar CSV: 61 entries total across Core/Settings/Feature categories.
- Integration tests: `SidebarMenuIntegrationTests` validates against CSV allow-matrix.
- SuperAdmin sidebar verified with all menus during live testing.

### Stage E.2 — Degree Rules Page
- **Issue #5**: Degree Rules loads correctly (no 302 redirect).

#### Implementation Summary
- `DegreeRules` GET removed `!_api.IsConnected()` redirect; returns `View(new DegreeRulesPageModel())` for non-SuperAdmin and capability-denied paths.
- `DegreeRuleCreate` and `DegreeRuleDelete` POST redirect to `DegreeRules` instead of `Dashboard`.

#### Validation Summary
- Live test: SuperAdmin at `/Portal/DegreeRules` → page renders with form and rules table.
- Live test: Admin at `/Portal/DegreeRules` → page renders with message, NO redirect.
- Both roles stay at `/Portal/DegreeRules` URL.

### Stage E.3 — ISO/Backup/Document Modules
- **Issue #6**: ISO Compliance, Backup & DR, Document Management modules exist.

#### Implementation Summary
- ModuleRegistry entries: `iso_compliance`, `backup_dr`, `document_management` as SuperAdmin-only.
- Sidebar CSV entries under Settings category with "No Access" for Admin/Faculty/Student.
- Database migrations exist for all three modules (PhaseISO4BackupDR, PhaseISO7DocumentManagement).

#### Validation Summary
- ModuleRegistry: 3 new entries with SuperOnly role constraint.
- Sidebar CSV: 3 new rows (lines 59-61) with SuperAdmin-only access.
- Visible to SuperAdmin in sidebar Settings section.

---

## Phase F — Student Lifecycle

- **Status**: ✅ Complete
- **Date**: 2026-07-12

### Stage F.1 — Semester Dropdown Range
- **Issue #2**: University Semester dropdown shows 1–8, not 82,029 options.

#### Implementation Summary
- `AcademicLevelRangeHelper.ResolveUniversityLevelRange` constrains semester range using program's `TotalSemesters` value.
- Filters out levels exceeding the program's configured semester count.
- Falls back to configured levels when program semesters are missing.

#### Validation Summary
- `AcademicLevelRangeHelperTests.ResolveUniversityLevelRange_UsesProgramTotalSemesters_WhenAvailable` — PASSED.
- `AcademicLevelRangeHelperTests.ResolveUniversityLevelRange_FallsBackToConfiguredLevels_WhenProgramSemestersMissing` — PASSED.
- School: Class 1-10, College: Class 11-12, University: Semester 1-8.

### Stage F.2 — Student Management
- Student Add/Edit/Delete works via the Students page.

#### Implementation Summary
- `PortalController.Students` loads student list with department/tenant/campus/institution filters.
- `PortalController.StudentLifecycle` handles progression, graduation, status changes.
- Student profiles managed via `student_profiles` table with CRUD operations.

#### Validation Summary
- Students page loads at `/Portal/Students` with filter dropdowns.
- Student Lifecycle page loads at `/Portal/StudentLifecycle` with semester/program selection.
- Student status transitions (Active/Inactive/Graduated) functional.

---

## Phase G — Results, Attendance, Assignments

- **Status**: ✅ Complete
- **Date**: 2026-07-12

### Stage G.1 — Results Entry
- Percentage for School/College, GPA for University.
- GradingType is per-course.

#### Implementation Summary
- `EnterResults` POST action handles scoped result entry with marks, max marks, and result type.
- `ResultDomainRulesTests` validate published/draft state transitions.
- `GpaResultStrategyTests` and `PercentageResultStrategyTests` cover GPA and percentage grading.
- Grading type stored per-course via `GradingType` column (GPA/Percentage).

#### Validation Summary
- Results page with cascading filters: Institution → Department → Course → Semester.
- Percentage-based: School (90+=A+, 80+=A, 70+=B, 60+=C, 50+=D, <50=F).
- GPA-based: University (4.0 scale).
- Result domain rules tests verify publish/draft/correct workflows.

### Stage G.2 — Attendance
- Manual attendance entry with date and status.
- CSV import for bulk attendance with validation.
- Labels: Present/Absent.

#### Implementation Summary
- `EnterAttendance` and `BulkMarkAttendance` actions for manual entry.
- `ImportAttendanceCsv` handles bulk CSV upload with row-level validation and audit trail.
- Seed data: ~90 days per enrollment, 85-95% present.

#### Validation Summary
- Attendance page at `/Portal/EnterAttendance` with cascading filters.
- CSV import: validation for duplicate rows, invalid dates, unknown students.
- Attendance view at `/Portal/Attendance` shows records.

### Stage G.3 — Assignments
- Create assignment with title, description, due date, max marks.
- Grade submissions.
- Quiz creation and attempts (if applicable).

#### Implementation Summary
- `Assignments` page with create/edit/view by offering.
- `assignment_submissions` table for student submissions and grading.
- `quizzes` and `quiz_attempts` tables for quiz functionality.
- Seed data: 2 assignments per offering, quizzes with attempts for graduated students.

#### Validation Summary
- Assignments page at `/Portal/Assignments` with offering filter.
- Quizzes page at `/Portal/Quizzes` with attempts tracking.
- Seed data: assignments for all offerings, quizzes for all graduated students.

---

## Phase H — Payments

- **Status**: ✅ Complete
- **Date**: 2026-07-12

### Stage H.1 — Demo Receipts
- **Issue #9**: Demo receipts appear.

#### Implementation Summary
- 15 demo payment receipts in seed data (03-FullDummyData.sql) with Paid/Pending/Overdue statuses.
- `Payments` page at `/Portal/Payments` with tenant/campus/institution filters.
- `ExportPaymentsCsvTemplate` and `ImportPaymentsCsv` for bulk import/export.

#### Validation Summary
- Demo receipts visible with amounts, descriptions, due dates.
- Payment receipt CRUD: create, update, confirm, cancel.
- Post-deployment check: `@PayCount >= 15`.

### Stage H.2 — CSV Import
- CSV import works for dates and amounts.

#### Implementation Summary
- `ImportPaymentsCsv` action with CSV template download.
- Payment receipt entity: ReceiptNo, Amount, DueDate, Status, Description.

---

## Phase I — Certificates

- **Status**: ✅ Complete
- **Date**: 2026-07-12

### Stage I.1 — Generate Certificates
- Degree, Transcript, Completion, Report Card.
- Navy/gold theme, double borders, signature blocks.
- PDF export.

#### Implementation Summary
- `GenerateCertificates` page at `/Portal/GenerateCertificates`.
- Institution-type-aware: University=GPA, School/College=Percentage.
- Degree (University-only), Transcript (all), Completion (School Class 10 / College Class 11+12), Report Card.
- DOCX generation with navy+gold branding, Georgia/Calibri fonts.
- LibreOffice PDF adapter with NoOp fallback.
- 5 graduated students with complete marks for certificate generation.

#### Validation Summary
- All 4 certificate types generate correctly.
- File naming: `{RegNo}-{Type}.docx`.
- School student: Transcript ✓, Completion ✓, ReportCard ✓, Degree blocked (400).

---

## Phase J — Study Plan

- **Status**: ✅ Complete
- **Date**: 2026-07-12

### Stage J.1 — Study Plan Management
- Create/Edit/View Study Plans.
- Advisor status: Draft/Submitted/Approved.
- Course-type normalization.

#### Implementation Summary
- `StudyPlan` page at `/Portal/StudyPlan` with CRUD actions.
- `StudyPlanDetail`, `StudyPlanRecommendations` sub-pages.
- 5 demo study plans in seed data (BSCS/BBA students) with mixed advisor statuses.
- `study_plans` and `study_plan_courses` tables with advisor workflow.

#### Validation Summary
- Study plan CRUD: create with semester name + notes, add/remove courses.
- Advisor status: Draft → Submitted → Approved.
- Post-deployment check: `@SPCount >= 5`, `@SPCourseCount` verified.

---

## Phase K — Degree Audit & Graduation

- **Status**: ✅ Complete
- **Date**: 2026-07-12

### Stage K.1 — Degree Audit & Graduation
- Degree Audit rows render with earned-credit totals.
- Graduation eligibility works.
- Degree Rules page functional (Issue #5 — verified in Phase E).

#### Implementation Summary
- `DegreeAudit` page at `/Portal/DegreeAudit` with student selection.
- `GraduationEligibility` at `/Portal/GraduationEligibility` with department/program filters.
- `degree_rules`, `degree_rule_required_courses` tables.
- `CanUseDegreeAuditAsync` checks license-level university capability.

#### Validation Summary
- Degree audit displays course-by-course progress with credit totals.
- Graduation eligibility: 5 graduated students with complete marks.
- Graduation applications: 5 records (Status=Approved).

---

## Phase L — Reports

- **Status**: ✅ Complete
- **Date**: 2026-07-12

### Stage L.1 — Report Center
- All report types open with data visible.
- Filters optional, user validation correct.
- CSV/PDF export.

#### Implementation Summary
- `ReportCenter` page at `/Portal/ReportCenter`.
- Report types: Attendance, Results, Semester Results, Degree Certificate, Payments, Assignments.
- `Export*Summary`, `Export*Csv`, `Export*Pdf` actions per report type.
- Role-scoped report visibility via sidebar and capability matrix.

#### Validation Summary
- All report types accessible from Report Center.
- CSV and PDF exports functional for each type.
- Filters: tenant, campus, department, semester.

---

## Final Output

Provide a **detailed pass/fail report** for each phase above, including:
- Steps performed
- Expected vs actual results
- Screenshots (if applicable)
- Any remaining defects
- Any new regressions discovered
- Confirmation that all issues from the Issue Tracker (#1–#10) have been resolved
