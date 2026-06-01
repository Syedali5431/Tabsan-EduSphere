
(Moved to User Guide/User Guide.md. This file is deprecated.)

User guide synchronization update (02 June 2026):
- Added domain script-pack execution references for environment walkthroughs:
   - Scripts/School Scripts,
   - Scripts/College Scripts,
   - Scripts/University Scripts.
- Clarified deterministic full dummy-data coverage by institution mode:
   - School => Class 1-10,
   - College => Class 11-12,
   - University => Semester 1-8.
- Clarified University lifecycle demo now includes enrollment, attendance, and FYP seeded data and strict post-deployment validation expectations.

User guide synchronization update (28 May 2026):
- Enter Results governance now documents stricter write-scope checks, controlled publish actions, and mandatory correction reason capture.
- Training/demo baseline now includes expanded attendance timeline rows and mixed result lifecycle states (`Midterm`, published `Final`, draft `FinalReview`).
- Full demo dataset marker reference updated to `DemoDatasetVersion = FullDummyData-v9`.

User guide synchronization update (27 May 2026):
- Startup profile warnings for missing environment profiles are resolved through layered profile-loading reliability updates.
- Development startup guidance now assumes LocalDB-compatible defaults for local validation.
- User-facing menu/role/functionality coverage remains unchanged and fully aligned with settings/governance + institution-aware certificate workflows.

User guide synchronization update (26 May 2026 - validation snapshot):
- Build/runtime validation remains healthy.
- Current test snapshot is `438 passed / 5 failed`; remaining failures are sidebar role/menu expected-count drift.
- Functional guidance remains aligned to institution-aware certificates, institution-aware result-calculation governance, complete settings menus, and operational portal 2FA flows.

User guide synchronization update (26 May 2026):
- Announcement create flow now shows controlled validation feedback when offering context is invalid or missing.
- LMS module create flow now returns user-safe validation feedback for offering integrity failures.
- Graduation workflows now show deterministic conflict feedback during concurrent approval/reject operations.
- FYP views remain compatible with legacy panel-role values (`Internal`/`External`) in existing data sets.
- Settings and governance navigation checklist now explicitly includes report/sidebar/dashboard/license/policy/module/tenant/campus/admin-user menus.
- Certificate workflow is institution-aware:
   - University users can generate degree (graduation-ready) and period-scoped transcript documents.
   - School/College users can manage additional student certificates (upload/list/download) under authorized roles.
- Degree/transcript actions are hidden when university mode is disabled in active license/policy.
- Period filter wording is now dynamic: `Class` in university context, `Semester` in non-university context.

User guide synchronization update (25 May 2026):
- Programs page now supports scope-aware filtering for tenant/campus and scoped activation/deactivation workflows.
- Report Center availability can now be enabled/disabled per tenant/campus scope by authorized governance users.
- Sidebar visibility now reflects synchronized menu status, role access, and module entitlement rules.
- Settings/governance navigation now includes complete privileged controls (sidebar/report/institution/module/tenant/campus/admin-user surfaces).
- Bulk user import guidance updated to emphasize scoped onboarding boundaries.

Final baseline update (15 May 2026):
- Phase 38 complete (final separation baseline).
- Canonical user guide: User Guide/User Guide.md (Version 1.5).
- Packaging references:
   - Artifacts/Phase37/Publish-Separation-20260515.md
   - Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md

Documentation synchronization update (15 May 2026):
- Canonical user guide content is aligned with final execute-mode closure and packaging workflow notes.
# Tabsan EduSphere – User Guide

**Version:** 1.1  
**Date:** 11 May 2026  
**Aligned With PRD:** v1.7 | Modules v1.2  
**Audience:** Students, Faculty, Admins, Super Admins

---

## 1. Introduction

Tabsan EduSphere is a secure, license-based university management portal. It is accessible via any modern web browser. Features available to you depend on your assigned role, the modules your institution has activated, and the current license scope (School, College, University).
---

## 0. What’s New (May 2026)

- License tool now supports 1 month, 1/2/3 year, and permanent expiry, and explicit School/College/University scope.
- UAT, SAT, and Output documentation are available in the Docs folder for validation and acceptance.
- Import/export (CSV, PDF, Excel) and index maintenance scripts are included for admins and IT staff.
### 2.6 Import/Export and Reports

Admins and SuperAdmins can:
- Import users from CSV (see Admin Guide)
- Export timetables and results as PDF/Excel
- Use the Reports section for operational exports
### 2.7 Database Index Maintenance

DBAs/IT staff: See Scripts/04-Maintenance-Indexes-And-Views.sql for index and view maintenance. Run after major upgrades or bulk imports.
### 2.8 UAT/SAT/Output Docs

Acceptance and validation checklists are in Docs/UAT.md, Docs/SAT.md, Docs/Output.md.

This guide is organised by role. Navigate to your section to get started.

---

## 2. Getting Started

**Grading Submissions**
1. Open an assignment and click **View Submissions**.
2. Each student submission lists the uploaded file and timestamp.
3. Enter a **Grade** and optional **Feedback**.
4. Click **Save Grade**. The student is notified.

---

### 4.3 Managing Quizzes

**Creating a Quiz**
1. Go to **Quizzes** → **Create New**.
2. Select Course and Semester.
3. Set Title, Start Time, End Time, and Maximum Attempts.
4. Add questions:
   - Click **Add Question**.
   - Select type: MCQ, Short Answer, or True/False.
   - Enter question text, marks, and options (for MCQ mark the correct answer).
5. Click **Publish** to make the quiz active.

**Reviewing Attempts**
1. Open a quiz and click **View Attempts**.
2. See each student's score and attempted time.
3. For short-answer questions, award marks manually and save.

---

### 4.4 Recording Attendance

1. Go to **Attendance** → **Record Attendance**.
2. Select Course, Semester, and Date.
3. The enrolled student list loads automatically.
4. Mark each student **Present** or **Absent**.
5. Click **Save**. Students below the threshold are flagged automatically.

---

### 4.5 Results and Grading

1. Go to **Results** → **Enter Marks**.
2. Select Course and Semester.
3. Enter marks for each student.
4. Click **Calculate GPA** to preview computed grades.
5. Click **Publish Results** to release them to students. This action is audited and cannot be undone.

---

### 4.6 FYP Management

**Scheduling a Meeting**
1. Go to **FYP** → **Schedule Meeting**.
2. Select the student project.
3. Enter:
   - Date and Time
   - Department
   - Room Number
4. Add panel members from the faculty list.
5. Click **Save**. All panel members and the student receive a notification.

**Viewing Your FYP Schedule**
- Go to **FYP** → **My Meetings** to see all scheduled meetings in a calendar and list view.

---

### 4.7 Sending Notifications

1. Go to **Notifications** → **Compose**.
2. Select recipients: specific students, entire course, or all department students.
3. Enter title and message.
4. Click **Send**. Delivery is tracked per recipient.

---

## 5. Admin Guide

### 5.1 Dashboard

The Admin dashboard shows university-wide statistics:
- Total enrolled students
- Active courses this semester
- Recent system notifications
- Pending report exports

---

### 5.2 Viewing All Departments

1. Go to **Departments** in the sidebar.
2. Click any department to see: faculty list, active programs, and enrolled student count.

---

### 5.3 Student Academic History

1. Go to **Students** in the sidebar.
2. Search by registration number, name, or department.
3. Open a student profile to view the full academic history across all semesters.

---

### 5.4 Generating Reports

1. Go to **Reports** in the sidebar.
2. Select a report type:
   - Student Performance
   - Department Analytics
   - Assignment and Quiz Statistics
   - Attendance Summary
3. Set the date range and scope (department, semester, or university-wide).
4. Click **Generate**.
5. Download as PDF or Excel.

---

### 5.5 Sending Notifications

Same as Faculty. Admins can also broadcast to all users across all departments.

---

## 6. Super Admin Guide

### 6.1 Dashboard

The Super Admin dashboard shows:
- License status and expiry date
- Active module summary
- System health indicators
- Recent audit log entries

---

### 6.2 User Management

**Creating a User**
1. Go to **Users** → **Create User**.
2. Enter Username, Email (optional), and temporary Password.
3. Assign a Role and Department.
4. Click **Create**.

**Deactivating a User**
1. Find the user via **Users** search.
2. Click **Deactivate**. The user cannot log in but their data is preserved.

---

### 6.3 Department Management

1. Go to **Departments** → **Manage**.
2. Create, edit, or deactivate departments.
3. Assign faculty to departments from this screen.

---

### 6.4 License Management

**Uploading a License**
1. Go to **Settings** → **License**.
2. Click **Upload License File**.
3. Select the signed license file provided by the vendor.
4. Click **Validate and Activate**.
5. The system verifies the cryptographic signature and displays license type and expiry.

**License Status Indicators**
- Active: full access to licensed features
- Expiring Soon: warning displayed (grace period active)
- Expired: read-only mode enforced across the system
- Invalid: portal locked except for data export

---

### 6.5 Module Management

1. Go to **Settings** → **Modules**.
2. Each module shows current status (active/inactive) and whether it is mandatory.
3. To activate a paid module, toggle it to **Active** and confirm.
4. To deactivate an optional module, toggle it off. All existing data is preserved.

> Mandatory modules (Authentication, Departments, SIS) cannot be toggled.

---

### 6.6 Theme Management

1. Go to **Settings** → **Themes**.
2. Activate or deactivate themes available to users.
3. Set a university-wide default theme.
4. Users may override the default from their personal settings.

---

### 6.7 Audit Logs

1. Go to **Audit** in the sidebar.
2. Filter by action type, user, date range, or entity.
3. Each entry shows: actor, action, affected entity, timestamp, and IP address.
4. Click **Export** to download the audit trail as a file for compliance reporting.

---

## 7. Common Tasks Quick Reference

| Task | Role | Where to Find |
|---|---|---|
| Sign up | Student | Login page → Sign Up |
| Submit assignment | Student | Assignments → Open → Submit |
| Download transcript | Student | Results → Download Transcript |
| Create assignment | Faculty | Assignments → Create New |
| Grade submission | Faculty | Assignments → Submissions |
| Record attendance | Faculty | Attendance → Record |
| Publish results | Faculty | Results → Publish |
| Schedule FYP meeting | Faculty | FYP → Schedule Meeting |
| Generate report | Admin | Reports |
| Upload license | Super Admin | Settings → License |
| Toggle module | Super Admin | Settings → Modules |
| View audit logs | Super Admin | Audit |

---

## 8. Accessibility

- The portal is WCAG 2.1 compliant.
- High-contrast themes are available under Appearance settings.
- All pages support keyboard navigation.
- Screen reader compatible markup is used throughout.

---

## 9. Support

For technical issues, contact your institution's IT support team. Super Admins should contact the vendor directly for licensing issues.

---

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.
