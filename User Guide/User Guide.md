# Tabsan EduSphere – User Guide

**Version:** 1.5  
**Date:** 15 May 2026  
**Aligned With PRD:** v1.8 | Modules v1.3  
**Completion Status:** Phase 38 complete (final separation baseline)  
**Audience:** Students, Faculty, Admins, Finance, Super Admins

---

## 0. What’s New (May 2026)

### Final Documentation Synchronization (Post Phase 38 Execute)
- User documentation baseline has been synchronized to the final execute-mode release closure.
- Runtime app, license app, and non-runtime assets now follow separated packaging and distribution workflows.
- For operational verification, refer to:
   - `Artifacts/Phase37/Publish-Separation-20260515.md`
   - `Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md`

### Final Release Packaging (Phase 37/38)
- Runtime app publish output is now separated from the license app publish output.
- Non-runtime documentation/training assets are packaged separately for distribution.
- Evidence references:
   - `Artifacts/Phase37/Publish-Separation-20260515.md`
   - `Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md`

### Core Enhancements (Phase 23-25)
- Institution-aware academic vocabulary: dynamically render Semester/Grade/Year, GPA/Percentage/Marks based on School/College/University policy (Phase 23.2).
- School stream support for Grades 9-12 with automatic subject filtering by stream selection (Science, Biology, Computer, Commerce, Arts) (Phase 25.2).
- License tool now supports 1 month, 1/2/3 year, and permanent expiry, and explicit School/College/University scope (Phase 24.1).
- Backend module enforcement middleware blocks disabled modules consistently across all API endpoints (Phase 24.2).
- UI/navigation filtering now hides disabled module menu items from sidebar (Phase 24.3).
- Role + institute scoping has been tightened across analytics, reports, and lifecycle-sensitive operations.
- Module-aware sidebar filtering now aligns with backend module enforcement middleware.

### Analytics & Reporting (Phase 31)
- Advanced analytics now include top performers ranking, performance trend analysis, and comparative department metrics (Phase 31.2).
- Institution-specific report section composition for School, College, and University contexts (Phase 31.1).
- Standardized analytics export metadata and extended PDF/Excel coverage to all advanced analytics reports (Phase 31.3).
- Queued export support for advanced analytics with deterministic naming conventions.

### Infrastructure & Governance (Phase 33)
- Tenant operations settings now support tenant-scope isolation (onboarding, subscription, tenant profile) for SaaS-ready deployments.
- Parent portal enhancements include linked-student read-only views and parent notification fan-out (where enabled).
- UAT, SAT, and Output documentation are available in UAT-SAT docs/ for validation and acceptance.
- Import/export (CSV, PDF, Excel) and index maintenance scripts are included for admins and IT staff.

---

## 1. Introduction

Tabsan EduSphere is a secure, license-based university management portal. It is accessible via any modern web browser. Features available to you depend on your assigned role, the modules your institution has activated, and the current license scope (School, College, University).

This guide is organised by role. Navigate to your section to get started.

---

## 2. Getting Started

### 2.1 Supported Browsers

- Google Chrome 110+
- Microsoft Edge 110+
- Mozilla Firefox 110+
- Safari 16+

### 2.2 Accessing the Portal

1. Open your browser and navigate to the URL provided by your institution.
2. You will land on the **Login** page.

### 2.3 First-Time Login

**Students**
1. Click **Sign Up**.
2. Enter your official **Registration Number** issued by the university.
3. Create a password following the displayed password policy.
4. Submit. Your account is immediately linked to your department, program, and current semester.

**Faculty / Admin / Super Admin**
Accounts are created by the Super Admin. You will receive login credentials directly. Change your password on first login when prompted.

### 2.4 Logging In

1. Enter your **Username** and **Password**.
2. Click **Login**.
3. You will be directed to your role-specific dashboard.

### 2.5 Logging Out

Click your profile avatar in the top-right corner and select **Logout**. Always log out when using a shared device.

### 2.6 Import/Export and Reports

Admins and SuperAdmins can:
- Import users from CSV (see Admin Guide)
- Export timetables and results as PDF/Excel
- Use the Reports section for operational exports

### 2.7 Database Index Maintenance

DBAs/IT staff: See Scripts/04-Maintenance-Indexes-And-Views.sql for index and view maintenance. Run after major upgrades or bulk imports.

### 2.8 UAT/SAT/Output Docs

Acceptance and validation checklists are in UAT-SAT docs/UAT.md, UAT-SAT docs/SAT.md, UAT-SAT docs/Output.md.

### 2.9 Current Baseline Notes (15 May 2026)

- User import templates are role-specific under User Import Sheets: faculty-admin-import-template.csv and students-import-template.csv.
- Standard DB deployment run path is Scripts/01 through Scripts/05.
- Consolidated planning and enhancement history is maintained in Docs/Consolidated-Execution-Enhancements-Issues.md.

---

## 3. Student Guide

### 3.1 Dashboard

Upon login you see a personalised dashboard showing:
- Upcoming assignment deadlines
- Recent notifications
- Current semester summary (GPA, enrolled courses)
- FYP meeting schedule (if FYP module is active)

---

### 3.2 Viewing Academic History

1. Navigate to **My Academic Record** in the sidebar.
2. Select a semester from the semester dropdown to view:
   - Enrolled courses
   - Grades and GPA
   - Attendance summary
   - CGPA across all semesters
3. Records from all semesters are always visible and are never deleted.

---

### 3.3 Assignments

**Viewing Assignments**
1. Go to **Assignments** in the sidebar.
2. Assignments are grouped by **Semester → Course → Assignment**.
3. Each card shows the title, due date, and submission status.

**Submitting an Assignment**
1. Click an assignment title to open the detail view.
2. Click **Submit**.
3. Upload your file or enter your response text.
4. Click **Confirm Submission**.
5. A confirmation message and timestamp will be displayed.

> Each assignment allows one submission. Review your work before confirming.

**Viewing Grades and Feedback**
- Once marked by your faculty member, the grade and feedback appear on the assignment detail page.

---

### 3.4 Quizzes

1. Go to **Quizzes** in the sidebar.
2. Active quizzes display a countdown timer and an **Attempt** button.
3. Click **Attempt** to start. Read all instructions before beginning.
4. Answer each question and click **Submit Quiz** when finished.
5. The number of allowed attempts is shown on each quiz card. Once exhausted, the attempt button is disabled.

---

### 3.5 Attendance

1. Go to **Attendance** in the sidebar.
2. View attendance per course for the current or any previous semester.
3. If your attendance falls below the institution threshold, you will receive a notification and the course card will display a warning indicator.

---

### 3.6 Results and Transcripts

1. Go to **Results** in the sidebar.
2. Download your transcript as PDF or Excel.

---

## 4. Faculty Guide

(See Faculty-Guide.md for full details)

---

## 5. Admin Guide

(See Admin-Guide.md for full details)

---

## 6. SuperAdmin Guide

(See SuperAdmin-Guide.md for full details)

---

## 7. Finance Guide

Finance users can work with payments, payment reports, and payment analytics without accessing academic modules.

### 7.1 Finance Access

Finance users can access:
- Payments for creating, updating, and confirming payment records
- Payment Reports for viewing and exporting payment summaries
- Analytics for payment-status insights only
- Theme Settings for personal UI preferences

Finance users cannot:
- Access academic modules such as courses, attendance, results, assignments, or quizzes
- Delete payment records; use cancellation or reversal workflows instead

### 7.2 Payment Workflow

1. Open **Payments** from the finance navigation area.
2. Review unpaid and paid records for your assigned campus scope.
3. Create or update a payment record as needed before finalization.
4. Mark the payment as paid when cash or confirmed settlement is received.
5. Use the timestamp and update trail fields to verify the latest payment state.

### 7.3 Payment Reports

1. Open **Payment Reports**.
2. Select the required filter combination, such as campus, department, course, class, semester, year, or month.
3. Review the report summary and confirm the displayed student, amount, and payment status data.
4. Export the report to PDF, Excel, or CSV when required.

### 7.4 Finance Analytics

1. Open **Analytics** from the finance navigation area.
2. Review the Paid vs Unpaid payment chart.
3. Apply campus, department, course, and semester/class filters to narrow the results.
4. Use the chart and legend to compare payment status totals for the selected scope.

---

## 8. Troubleshooting

- If you cannot log in, contact your department admin or SuperAdmin.
- For license or module issues, see SuperAdmin Guide and License-KeyGen-Guide.md.
- For import/export or index maintenance, see Scripts/ and Docs/ folders.

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.
