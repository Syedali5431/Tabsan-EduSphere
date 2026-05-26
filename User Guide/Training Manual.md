# Tabsan EduSphere – Training Manual

**Version:** 1.6  
**Date:** 26 May 2026  
**Aligned With PRD:** v1.8 | Modules v1.3  
**Completion Status:** Phase 38 complete (final separation baseline)  
**Audience:** Trainers, IT Staff, Finance, Super Admins, Department Coordinators

---

## 0. Training Updates (May 2026)

### Runtime Stability and Compatibility (26 May 2026)
- Include trainer scenario for announcement posting with invalid offering selection and expected controlled validation error behavior.
- Include trainer scenario for LMS content-module creation offering integrity failures and expected safe error handling.
- Include trainer scenario for graduation approval/reject concurrency conflicts and recommended retry workflow.
- Include trainer note for FYP panel-role backward compatibility (`Internal`/`External`) when validating legacy seeded data.
- Include settings governance walkthrough covering report/sidebar/dashboard/license/policy/module/tenant/campus/admin-user menus.

### Institution-Aware Certificate Workflow (26 May 2026)
- Include trainer walkthrough for Generate Certificates behavior by institution mode.
- University scenario: degree and period-scoped transcript generation for graduation-ready students.
- School/College scenario: additional certificate upload/list/download lifecycle and student access verification.
- Include validation step for policy/license-controlled visibility (degree/transcript hidden when university mode is disabled).
- Include terminology check for period label behavior (`Class` in university mode, `Semester` otherwise).

### Final Documentation Synchronization (Post Phase 38 Execute)
- Trainer-facing baseline now references final execute-mode publish-separation behavior.
- Training delivery should include where to find final package evidence and separated deliverables:
	- `Artifacts/Phase37/Publish-Separation-20260515.md`
	- `Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md`
- Training assets are distributed through the non-runtime packaging workflow and should not be bundled into runtime app artifacts.

### Final Release Packaging Baseline (Phase 37/38)
- Include release packaging walkthrough: runtime app publish is separate from Tabsan.Lic publish.
- Include operator guidance for non-runtime asset bundle distribution for training and operations materials.
- Refer to evidence reports in `Artifacts/Phase37/Publish-Separation-20260515.md` and `Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md`.

### Institution-Aware Vocabulary (Phase 23.2)
- Include module-governance demonstration: backend enforcement plus sidebar visibility alignment.
- Include institution-aware label and filter behavior checks for School/College/University contexts.
- Demonstrate dynamic vocabulary rendering on dashboard and lifecycle screens based on active institution policy.

### School-Specific Features (Phase 25.2-25.3)
- Include stream assignment and stream-aware subject filtering for Grade 9-12 students.
- Demonstrate promotion logic enforcement for School institutions based on pass-threshold eligibility.
- Include grade-based lifecycle navigation and label rendering (Grade/Promotion/Percentage terminology).

### Advanced Analytics (Phase 31.2-31.3)
- Include demonstration of top performers ranking and performance trend analysis in admin/faculty analytics views.
- Include comparative department metrics overview for cross-institutional analysis.
- Demonstrate standardized PDF/Excel export for all advanced analytics reports.
- Show queued export job tracking and deterministic naming conventions (analytics-{report-key}-{utcstamp}.{ext}).

### Module & License Governance (Phase 24.1-24.3)
- Demonstrate backend enforcement blocking disabled module APIs with 403 responses.
- Demonstrate UI navigation filtering hiding disabled modules from sidebar.
- Verify license institution-scope flags and their impact on available data/reports.

### Portal Governance Updates
- Include parent portal read-only and notification behavior walkthrough where parent module is enabled.
- Include tenant operations settings behavior overview for SaaS/multi-tenant readiness context.
- Verify role and institute filters work together for data scoping and authorization.

---

## 2. Pre-Training Setup Checklist (Updated)

- [ ] Training environment (separate from production) is available and accessible
- [ ] A valid license (with correct institution scope and expiry) has been uploaded and is active on the training environment
- [ ] At least one test account per role exists (Student, Faculty, Admin, Super Admin)
- [ ] Sample data is seeded: at least two departments, two programs, five courses, three semesters, and ten students
- [ ] At least one assignment, one quiz, one result set, and one FYP meeting are pre-created
- [ ] All trainers have reviewed the User Guide before delivery
- [ ] Projector or screen share is ready for live demonstration
- [ ] UAT, SAT, and Output docs in UAT-SAT docs/ are reviewed
- [ ] Import/export and index maintenance scripts are available for IT staff

## 2.1 License Tool and Acceptance Docs

The Tabsan.Lic tool now supports institution scope (School/College/University) and multiple expiry options. See UAT-SAT docs/UAT.md and UAT-SAT docs/SAT.md for acceptance criteria.

---

## 3. Training Programme Overview

| Session | Audience | Duration | Focus |
|---|---|---|---|
| Session 1 | All users | 30 min | Platform orientation and navigation |
| Session 2 | Students | 45 min | Registration, academic history, assignments, results |
| Session 3 | Students | 30 min | Quizzes, attendance, FYP, AI chatbot |
| Session 4 | Faculty | 60 min | Assignment and quiz management, attendance, grading |
| Session 5 | Faculty | 45 min | FYP scheduling, notifications, results publication |
| Session 6 | Admins | 45 min | Reporting, student data access, broadcast notifications |
| Session 7 | Super Admin | 60 min | User management, license, modules, themes, audit |
| Session 8 | Finance | 60 min | Payments, payment reports, payment analytics, access boundaries |
| Session 9 | Admin / Faculty | 60 min | Advanced analytics: top performers, trends, comparative metrics, exports |
| Session 10 | Admins | 45 min | Institution-aware report sections and institution-specific analytics |
| Session 11 | IT Staff / Super Admin | 30 min | Analytics export pipeline and queued job tracking |

---

## 4. Session 1 – Platform Orientation (All Users)

### Learning Objectives
- Understand the purpose and scope of EduSphere
- Navigate the portal confidently
- Log in and log out securely
- Personalise the UI theme

### Agenda
1. Welcome and system overview (5 min)
2. Logging in live demonstration (5 min)
3. Dashboard tour by role (10 min)
4. Navigating menus, notifications, and profile settings (5 min)
5. Personalising themes (5 min)

### Exercise 1A – First Login
Each participant should:
1. Navigate to the training portal URL
2. Log in using provided test credentials
3. Identify three items visible on their dashboard
4. Change the UI theme and confirm the change persists after logout and re-login

### Trainer Notes
- Emphasise that each role sees a different dashboard
- Confirm all participants can log in before proceeding
- Highlight the notification bell and profile avatar as the most-used nav items

---

## 5. Session 2 – Student: Registration, History, Assignments, Results

### Learning Objectives
- Complete student registration using a registration number
- Navigate academic history across semesters
- Submit an assignment
- Download a transcript

### Agenda
1. Student signup walkthrough (10 min)
2. Academic history structure explained (10 min)
3. Viewing and submitting assignments (15 min)
4. Viewing results and downloading transcripts (10 min)

### Exercise 2A – Student Signup
1. Use a provided unused registration number
2. Complete signup
3. Confirm the account is linked to the correct department and program

### Exercise 2B – Assignment Submission
1. Log in as a test student
2. Navigate to Assignments
3. Open an assignment and submit a sample file
4. Confirm the submission timestamp is recorded

### Exercise 2C – Transcript Download
1. Navigate to Results
2. View grades for the seeded semester
3. Download the transcript in both PDF and Excel format

### Trainer Notes
- Students can only sign up with a registration number that exists in the whitelist
- Once submitted, assignments cannot be re-submitted – demonstrate this clearly
- Emphasise that all semester records remain permanently accessible

---

## 6. Session 3 – Student: Quizzes, Attendance, FYP, Chatbot

### Learning Objectives
- Attempt a quiz within time and attempt limits
- Understand attendance tracking and thresholds
- View FYP meeting details
- Use the AI chatbot for academic queries

---

## 7. Session 8 – Finance: Payments, Reports, Analytics

### Learning Objectives
- Understand Finance role boundaries and allowed access
- Create, update, and confirm payment records
- Generate and export payment reports
- Review payment analytics with filters

### Agenda
1. Finance role overview and access boundaries (10 min)
2. Payment workflow walkthrough (20 min)
3. Payment report generation and export (15 min)
4. Payment analytics review and filtering (15 min)

### Exercise 8A – Finance Access Check
1. Log in as a Finance user.
2. Confirm the sidebar shows Payments, Payment Reports, Analytics, and Theme Settings.
3. Confirm academic modules are not visible or accessible.

### Exercise 8B – Payment Update Flow
1. Open Payments.
2. Locate an unpaid receipt in the assigned campus scope.
3. Update the receipt if required.
4. Mark the payment as paid and confirm the update trail changes.

### Exercise 8C – Report and Analytics Review
1. Open Payment Reports and apply campus, department, course, and semester/class filters.
2. Export the report to PDF and Excel.
3. Open Analytics and verify the Paid vs Unpaid chart updates with the same scope.

### Trainer Notes
- Stress that Finance users must not access academic modules.
- Demonstrate that multi-campus assignment affects which payment records are visible.
- Confirm report filters and chart filters produce matching scoped totals.

---

(Continue with remaining sessions as in previous version)

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.

## Phase 40.3 Scoped Governance Update (2026-05-25)

- Add training walkthrough for tenant and campus scoped Program Management, including activate and deactivate operations.
- Add training walkthrough for scoped Report Center activation and deactivation in governance settings.
- Include troubleshooting drill for sidebar visibility mismatches using role, module, and tenant/campus scope validation.
- Include governance awareness module for Admin Users, Tenant Management, and Campus Management settings surfaces.
- Reinforce user-import training to cover claim-aware tenant/campus boundaries in bulk onboarding.

