# Faculty User Guide

Version: 1.7  
Date: 04 June 2026  
Completion Status: Phase 38 complete + ISO 27001 & ISO 9001 Compliance (Phases 1-10)

## 1. Purpose

This guide explains faculty workflows in Tabsan EduSphere: teaching setup, assignments, quizzes, attendance, results, FYP supervision, and notifications.

## 1.1 What's New (May 2026)

- Institution-aware vocabulary now appears in key screens (for example Semester vs Grade vs Year) based on active policy.
- Advanced analytics now available for faculty: performance trends and class-level comparative metrics for assigned offerings (Phase 31.2).
- Analytics exports to PDF/Excel are standardized with deterministic naming; large exports are processed as queued jobs (Phase 31.3).
- Report and analytics requests now enforce assignment plus institute compatibility more strictly.
- Parent notification fan-out is supported for published results and selected academic alerts where parent module is active.
- Export behavior for analytics/reporting is standardized across synchronous and queued job paths.
- **Security Enhancements (June 2026)**: Password policy now enforces 90-day expiry and prevents reuse of last 5 passwords. Sessions automatically timeout after 30 minutes of inactivity. All login attempts are monitored for account protection. Enable MFA in your profile settings for additional security. Report any suspicious activity through the incident reporting system.

## 1.2 Documentation Baseline (15 May 2026)

- Institution parity baseline remains active for School, College, and University contexts.
- Student and faculty-facing labels remain institution-aware (Semester/Grade/Year) by policy.
- User import templates are now role-specific under User Import Sheets and aligned to admin onboarding workflows.
- Standard DB deployment run path is Scripts/01 through Scripts/05.
- **ISO Compliance (New)**: Enhanced security controls active — password ageing, session timeout, login monitoring, and MFA support. Review Section 2.1 for details.

## 1.3 Final Release Packaging Update (Phase 37/38)

- Runtime app publish outputs are separated from license app outputs.
- User-facing guides and import templates are distributed through the non-runtime asset package.
- Phase evidence references:
   - Artifacts/Phase37/Publish-Separation-20260515.md
   - Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md

## 2. Login and Role Scope

Faculty accounts are created by SuperAdmin or Admin. After login, your access depends on:
- Assigned role: Faculty
- Assigned department
- Assigned course offerings
- Active modules

If menus are missing, contact your admin to verify role and module assignment.

### 2.1 Security and Account Protection (ISO 27001)

Your account is protected by enhanced security controls:
- **Password Policy**: Passwords must be 12+ characters with uppercase, lowercase, digit, and special character. Passwords expire every 90 days and cannot match your last 5 passwords.
- **Session Timeout**: After 30 minutes of inactivity, your session is automatically revoked. You will need to log in again.
- **Login Monitoring**: All login attempts (successful and failed) are recorded for your protection. Unusual activity triggers security alerts.
- **MFA (Multi-Factor Authentication)**: Available in your profile settings. Strongly recommended for all faculty accounts.
- **Audit Logging**: Key actions (grading, attendance changes, result publication) are logged with full context for compliance and traceability.

## 3. Faculty Dashboard

Dashboard highlights typically include:
- Pending submissions to grade
- Upcoming class sessions
- Attendance alerts
- Recent student notifications
- FYP meeting reminders

Recommended routine: review dashboard at the start and end of each teaching day.

## 3.1 Role and Institute Filter Behavior

Faculty scope is filtered by role, offering assignment, and institution context:
- Visible operational menus and data are constrained to assigned offerings and institute-compatible departments.
- Report and analytics surfaces enforce assignment + institution compatibility.
- Requests outside authorized scope can return access-denied responses by design.

If expected data is missing, confirm offering assignment and selected filters with your Admin.

## 4. Course Offerings and Enrollment View

Path: Sidebar > Courses / Enrollments

Use Courses to:
- Open assigned offerings by semester
- Review enrolled students
- Confirm class capacity and open state

Use Enrollments to:
- Select an offering from the dropdown and view the full roster with registration number, program, and semester number

If a course is not visible, request course offering assignment from Admin.

## 5. Assignment Management

Path: Sidebar > Assignments

Create assignment:
1. Select semester and offering.
2. Enter title and instructions.
3. Set due date and max marks.
4. Publish when ready.

Grade submissions:
1. Open assignment.
2. Review each submission.
3. Enter marks and feedback.
4. Save grading.

Good practices:
- Use clear rubrics in assignment description.
- Keep feedback specific and actionable.

## 6. Quiz Management

Path: Sidebar > Quizzes

Create quiz:
1. Select course offering.
2. Configure title, time limit, attempts, and availability window.
3. Add questions and options.
4. Publish.

Review attempts:
1. Open quiz attempts list.
2. Verify objective questions.
3. Manually grade descriptive/short-answer responses if applicable.

Operational advice:
- Keep a short buffer between start time and class start.
- Validate question ordering and correctness before publishing.

## 7. Attendance Recording

Path: Sidebar > Attendance

Record attendance:
1. Select offering and date.
2. Mark Present, Absent, Late, or Excused based on policy.
3. Save.

After saving:
- Low-attendance students may trigger warnings.
- Data is used in attendance summary views and reports.

Correction flow:
- Submit modification request if policy requires approval.

## 8. Results and Publishing

Path: Sidebar > Results

Typical flow:
1. Select offering.
2. Enter marks for result type (for example Midterm, Final).
3. Verify mark limits.
4. Publish when approved.

Important:
- Published results become student-visible.
- Publishing actions are audit-sensitive.

## 9. FYP Supervision

Path: Sidebar > FYP

You can:
- View assigned student projects
- Update project guidance notes
- Schedule and conduct meetings
- Add panel context where allowed

Meeting quality checklist:
- Define agenda before session
- Record decisions and action items
- Publish notes promptly

## 11. Analytics and Class Performance

Path: Sidebar > Analytics

### Class-Level Performance Analytics (Phase 31.2)

#### Performance Overview
1. Navigate to **Analytics > Class Performance** (available for assigned offerings).
2. Select an **Offering** and **Academic Level** (Semester/Grade/Year per institution policy).
3. Review class performance summary:
   - Average marks/percentage
   - Grade distribution (A/B/C/D/F or percentage bands)
   - Class attendance rate
   - Assignment submission rate

#### Top Performers in Class
1. From **Analytics > Class Performance**, click **Top Performers**.
2. View ranked students by performance metric (GPA/Percentage).
3. Identify high achievers for recognition or mentoring opportunities.
4. Export to PDF for class records.

#### Performance Trends
1. Navigate to **Analytics > Performance Trends**.
2. Select **Academic Level** and **Offering** (if offering-specific view available).
3. Review performance changes over time.
4. Identify improvement areas or performance dips that require intervention.
5. Export trend analysis to PDF/Excel for faculty meeting or parent communication.

### Export Enhancements (Phase 31.3)

Faculty analytics exports are standardized:
- **Synchronous Export**: PDF/Excel downloads complete within seconds for typical class sizes.
- **Queued Export**: Large class exports or complex reports are processed asynchronously. You receive a reference ID and email notification when complete.
- **Filename Convention**: All exports follow pattern `{report-key}-{timestamp}.{extension}` (e.g., `analytics-class-performance-2026-05-14-143022.pdf`).
- **Access Queued Export**: Navigate to **Faculty Dashboard > Export History** to view and download completed jobs.

Export workflow:
1. Open any analytics or report view for your assigned offerings.
2. Click **Export as PDF** or **Export as Excel**.
3. If synchronous, download begins immediately.
4. If queued, you receive a job ID. Check Export History for status.
5. Once complete, download from Export History or via email link.

## 12. Institution-Aware Academic Terminology

Vocabulary adapts by institution policy (Phase 23.2):
- **Semester/Grade/Year**: Period label changes based on institution type.
- **Progression/Promotion**: Advancement label changes based on context.
- **GPA/Percentage/Marks**: Grading interpretation changes based on institution type.

When viewing student records or analytics, terminology will automatically adjust. No configuration is needed; it is driven by the active institution license policy.
- Full course offerings
- Relevant student groups by scope

Best practice:
- Keep titles clear and action-oriented
- Include deadline and expected next action

## 11. Theme and Accessibility

Path: Profile > Settings > Appearance

Use high-contrast or readable themes when reviewing long grading sessions.

## 12. Common Issues

1. Cannot publish assignment/quiz:
- Confirm you are linked to the offering
- Confirm module is active

2. Attendance save blocked:
- Check date and offering status
- Check lock policy configured by admin

3. Missing student in class list:
- Verify enrollment status in offering

4. Result publication unavailable:
- Confirm permission and workflow stage

## 13. Faculty Weekly Checklist

- Review teaching schedule
- Publish/close assignments and quizzes on time
- Record attendance after each session
- Grade pending work
- Send targeted notifications
- Review FYP meeting queue

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.

## Phase 40.3 Scoped Governance Update (2026-05-25)

- Program and report access boundaries are now more explicitly tenant and campus scoped in shared multi-campus deployments.
- Report Center visibility can now differ per tenant and campus activation state, even when role permissions are unchanged.
- Sidebar visibility consistency was improved through synchronized menu governance, reducing mismatches between visible menus and allowed routes.
- Faculty should report missing menu items with role, department, tenant, and campus context so governance troubleshooting is accurate.

## Phase 40.4 Institution-Aware Certificates Update (2026-05-26)

- Faculty visibility on certificate surfaces now follows institution mode and active policy/license.
- University mode supports degree/transcript generation flows for authorized, scoped operations.
- School/College mode exposes additional student certificate workflows managed by authorized staff, with student download visibility.
- Period selector wording in certificate workflow is now context-aware (`Class` for university, `Semester` otherwise).

## Phase 40.6 ISO 27001 + ISO 9001 Compliance Update (2026-06-04)

- Enhanced password policy (90-day expiry, 5-password history, 12-char minimum).
- Idle session timeout (30 minutes) now enforced.
- Login activity monitoring active — all authentication attempts logged.
- MFA available for all faculty accounts.
- All changes are additive and backward-compatible.

## Phase 40.5 Results and Attendance Governance Update (2026-05-28)

- Enter Results now enforces stricter write scope checks by assignment and institution compatibility.
- Import report download uses short-lived, one-time tokens for safer audit/report handling.
- Result correction requires a reason and applies only after the result is already published.
- Faculty should treat `FinalReview` rows as draft correction-stage records; students see only published states.
- Demo dataset now includes expanded attendance timeline and mixed result types (`Midterm`, `Final`, `FinalReview`) for realistic faculty testing.

## 14. Detailed Faculty Teaching Cycle

This section gives a practical weekly teaching cycle with expected outputs.

### 14.1 Pre-Class Preparation

1. Validate timetable slots and room/modality details.
2. Open upcoming offering page and verify enrollment roster.
3. Confirm assignment/quiz visibility windows align with session plan.
4. Prepare attendance-ready session metadata (topic, date, remarks).

### 14.2 In-Class Actions

1. Share agenda and expected outcomes.
2. Mark attendance after identity confirmation process.
3. Record in-session notes for exceptional student cases.
4. If quiz is live, verify timer and attempt controls are correct.

### 14.3 Post-Class Closure

1. Publish attendance if not auto-published by policy.
2. Upload or update lesson resources.
3. Send summary notification for follow-up tasks.
4. Log students requiring intervention.

## 15. Grading and Publication Governance

Use this sequence to avoid publication mistakes.

1. Enter Marks
- Ensure selected offering is correct.
- Confirm result type and maximum marks.
- Save draft entries first.

2. Validate Before Publish
- Spot-check at least 10 percent of entries or minimum 10 records.
- Confirm no out-of-range marks.
- Confirm absent/incomplete coding is consistent with policy.

3. Publish
- Publish only if your role has publish authority.
- Confirm expected student visibility after publish.
- Record publish timestamp in course log.

4. Corrections
- Apply corrections only through reasoned workflow.
- Include clear correction reason and source evidence.
- Verify corrected rows are republished where policy requires.

## 16. Faculty Communication Playbook

Use concise, action-driven communication templates.

1. Assignment Reminder
- Include title, due date/time, allowed format, and submission policy.

2. Quiz Notice
- Include start/end window, attempts allowed, and technical requirements.

3. Result Notice
- Include published components and official recheck/correction request window.

4. Attendance Warning
- Include current percentage, threshold, and required corrective action.

## 17. Faculty Troubleshooting Deep-Dive

1. Roster mismatch
- Check offering ID and period filter.
- Confirm student enrollment status.
- Ask admin to verify department/program mapping.

2. Quiz submission anomalies
- Confirm quiz was in active window.
- Confirm attempt quota not exceeded.
- Capture timestamps and student identifiers for audit.

3. Result publication not visible to students
- Confirm status is published, not draft.
- Confirm students belong to the published offering.
- Confirm no scope mismatch in institution context.

4. Missing analytics data
- Check selected date range and offering filter.
- Check whether module/report permissions are active.
- Escalate with role/scope/filter details.

## 18. Faculty Weekly Quality Checklist

- All planned sessions delivered and attendance recorded.
- All assignments/quizzes carry clear instructions and deadlines.
- Pending grading backlog below institutional SLA.
- Published results verified for correctness and visibility.
- Student intervention list updated for low performance/attendance.

