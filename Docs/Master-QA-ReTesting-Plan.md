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

### Stage D.1 — Profile Picture Upload
- **Issue #8**: Profile picture upload works (upload, preview, replace).

#### Test Steps
1. Navigate to **User Settings**.
2. In the Profile Picture section, click **Choose File** and select a JPG/PNG under 2MB.
3. Click **Upload**.
4. Verify the preview shows the uploaded image.
5. Navigate to another page → verify the navbar avatar shows the new picture.

#### Expected Result
- Upload succeeds with success message.
- Preview displays the uploaded image.
- Navbar avatar updates to the new picture.
- Invalid file types (non-JPG/PNG) or files >2MB are rejected.
- Fallback initial-letter avatar appears when no picture is set.

---

## Phase E — Navigation & Sidebar

### Stage E.1 — Sidebar Item Count
- **Issue #10**: Sidebar shows the correct number of items for University admin.

#### Test Steps
1. Log in as Admin (university scope).
2. Count visible sidebar menu items.
3. Compare against expected count from `Sidebar-Menu-Purpose.csv`.

#### Expected Result
- Admin sees all menus specified in the CSV with non-"No Access" entries.
- SuperAdmin sees all menus including System Settings, ISO Compliance, Backup & DR, Document Management.

---

### Stage E.2 — Degree Rules Page
- **Issue #5**: Degree Rules loads correctly (no 302 redirect).

#### Test Steps
1. Log in as SuperAdmin → navigate to **Degree Rules**.
2. Log in as Admin → navigate to **Degree Rules**.

#### Expected Result
- SuperAdmin: Page renders with rule creation form and existing rules table.
- Admin: Page renders with appropriate message ("Only SuperAdmin can manage degree rules") — **NO redirect to Dashboard**.
- URL stays at `/Portal/DegreeRules` for both roles.

---

### Stage E.3 — ISO/Backup/Document Modules
- **Issue #6**: ISO Compliance, Backup & DR, Document Management modules exist.

#### Test Steps
1. Log in as SuperAdmin.
2. Scroll the sidebar to Settings section.
3. Confirm ISO Compliance, Backup & DR, and Document Management links are visible.
4. Click each link → confirm the page loads (or shows appropriate placeholder).

#### Expected Result
- Three modules visible in sidebar for SuperAdmin.
- Pages load without errors (may show "coming soon" or data-dependent message).
- Modules NOT visible for Admin, Faculty, Student roles.

---

## Phase F — Student Lifecycle

### Stage F.1 — Semester Dropdown Range
- **Issue #2**: University Semester dropdown shows 1–8, not 82,029 options.

#### Test Steps
1. Log in as Admin (university scope).
2. Navigate to **Student Lifecycle**.
3. Select a university program (e.g., BSCS).
4. Check the semester dropdown options.

#### Expected Result
- Semester dropdown shows only 1–8 (or the program's configured total semesters).
- Not showing thousands of invalid options.
- School/College shows Class 1–10 or 11–12 as appropriate.

---

### Stage F.2 — Student Management
- Student Add/Edit/Delete works via the Students page.

#### Expected Result
- Student list loads with correct filters.
- Student details viewable.
- Student status changes (Active/Inactive/Graduated) work.

---

## Phase G — Results, Attendance, Assignments

### Stage G.1 — Results Entry
- Percentage for School/College.
- GPA for University.
- GradingType is per-course.

#### Test Steps
1. Navigate to **Enter Results**.
2. Select a School department → enter percentage marks (e.g., 85/100).
3. Select a University department → enter GPA-based marks.
4. Publish results and verify they appear in the Results view.

#### Expected Result
- School/College: Marks entered as percentage with letter grades (A+, A, B, etc.).
- University: Marks entered with GPA calculation.
- Published results visible to students.

---

### Stage G.2 — Attendance
- Add attendance manually.
- Import CSV for bulk attendance.
- Labels correct (Present/Absent).

#### Expected Result
- Manual attendance entry works with date and status selection.
- CSV import processes correctly with validation feedback.
- Attendance records visible in the Attendance view.

---

### Stage G.3 — Assignments
- Create assignment.
- Grade submission.
- LMS quiz creation (if applicable).

#### Expected Result
- Assignment created with title, description, due date, max marks.
- Submissions can be graded.
- Quiz creation and attempts work.

---

## Phase H — Payments

### Stage H.1 — Demo Receipts
- **Issue #9**: Demo receipts appear.

#### Test Steps
1. Log in as Admin or SuperAdmin.
2. Navigate to **Payments**.
3. Check for demo payment receipts.

#### Expected Result
- 15 demo payment receipts visible with mixed statuses (Paid, Pending, Overdue).
- Receipt details (amount, description, due date) populated.
- Real payment data unaffected.

---

### Stage H.2 — CSV Import
- CSV import works for dates and amounts.

#### Expected Result
- Payment CSV import processes correctly.
- Date and amount fields parsed accurately.

---

## Phase I — Certificates

### Stage I.1 — Generate Certificates
Generate DOCX certificates:
- Degree
- Transcript
- Completion
- Report Card

#### Test Steps
1. Navigate to **Generate Certificates**.
2. Select a graduated student with complete marks.
3. Generate each certificate type.
4. Verify DOCX output and PDF export.

#### Expected Result
- Degree certificate: University only, GPA-based.
- Transcript: Full academic record with all courses.
- Completion: School (Class 10 completed) or College (Class 11+12 completed).
- Report Card: Single semester/class results.
- Navy/gold theme, double borders, signature blocks.
- PDF export works.

---

## Phase J — Study Plan

### Stage J.1 — Study Plan Management
1. Create / Edit / View Study Plans.
2. Advisor status correct (Draft/Submitted/Approved).
3. Course-type normalization.
4. Context preserved across pages.

#### Test Steps
1. Log in as Admin.
2. Navigate to **Study Plan**.
3. Select a BSCS or BBA student.
4. Create a new study plan with courses.
5. Change advisor status.
6. View the plan detail.

#### Expected Result
- 5 demo study plans visible (from seed data).
- New plans can be created with course assignments.
- Advisor status cycles through Draft → Submitted → Approved.
- Study plan detail page shows all assigned courses.

---

## Phase K — Degree Audit & Graduation

### Stage K.1 — Degree Audit
1. Degree Audit rows render.
2. Earned-credit totals correct.
3. Graduation eligibility works.
4. Degree Rules page functional (Issue #5).

#### Test Steps
1. Navigate to **Degree Audit** → select a student.
2. View audit rows showing course progress.
3. Navigate to **Graduation Eligibility** → select department and program.
4. Check eligibility status for students.

#### Expected Result
- Degree audit displays course-by-course progress.
- Credit totals (earned vs required) calculated correctly.
- Graduation eligibility shows students ready for graduation.
- Degree Rules page loads without redirect (confirmed in Phase E).

---

## Phase L — Reports

### Stage L.1 — Report Center
1. All report types open.
2. Data visible.
3. Filters optional.
4. User validation correct.

#### Test Steps
1. Navigate to **Report Center**.
2. Open each available report type:
   - Attendance Report
   - Results Report
   - Semester Results
   - Degree Certificate Report
   - Payment Report
3. Apply filters and verify data.
4. Export to CSV/PDF.

#### Expected Result
- All report types accessible.
- Data populates correctly with applied filters.
- Export functions produce valid CSV/PDF files.
- Different roles see appropriate report subsets.

---

## Phase M — General UI/UX

### Stage M.1 — Cross-Cutting Checks
1. No broken links on any page.
2. No unexpected redirects.
3. No misaligned elements.
4. No console errors in browser DevTools.

#### Test Steps
1. Navigate through every sidebar menu item.
2. Check browser console (F12) for JavaScript errors on each page.
3. Verify all forms submit without errors.
4. Test responsive layout at different window sizes.

#### Expected Result
- Zero broken links (no 404 errors).
- Zero unexpected redirects (all pages load at their expected URLs).
- No JavaScript console errors.
- UI elements properly aligned and styled.

---

## Final Output

Provide a **detailed pass/fail report** for each phase above, including:
- Steps performed
- Expected vs actual results
- Screenshots (if applicable)
- Any remaining defects
- Any new regressions discovered
- Confirmation that all issues from the Issue Tracker (#1–#10) have been resolved
