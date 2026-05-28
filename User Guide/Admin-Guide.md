<!-- markdownlint-disable MD007 MD010 MD012 MD022 MD029 MD032 MD060 -->

# Admin User Guide

Version: 1.6  
Date: 26 May 2026  
Completion Status: Phase 38 complete (final separation baseline)

## 1. Purpose

This guide covers departmental administration in Tabsan EduSphere: user operations, academic structure, course offerings, timetables, reporting, and operational oversight.

## 1.1 What's New (May 2026)

- Module-aware sidebar filtering is now enforced with backend module licensing checks for protected routes.
- Advanced analytics now include top performers ranking, performance trend analysis, and comparative department metrics (Phase 31.2).
- Institution-specific report sections are available for School, College, and University contexts (Phase 31.1).
- Analytics exports to PDF/Excel are standardized with deterministic naming and support queued export jobs (Phase 31.3).
- Analytics and report flows include stronger institute-aware scope checks for Admin requests.
- Student lifecycle views support institution-driven academic-level wording (Semester/Grade/Year).
- School stream support for Grades 9-12 with stream-aware subject filtering is available where School mode is active.
- Parent-facing read-only features and notifications are available when parent portal module is enabled.

## 1.2 Documentation Baseline (15 May 2026)

- Institution parity baseline remains active for School, College, and University contexts.
- User import templates are role-specific in User Import Sheets: faculty-admin-import-template.csv and students-import-template.csv.
- Standard DB deployment run path is Scripts/01 through Scripts/05.
- Cross-phase planning and enhancement tracking is consolidated in Docs/Consolidated-Execution-Enhancements-Issues.md.

## 1.3 Final Release Packaging Update (Phase 37/38)

- Runtime app artifact and license app artifact are now published separately.
- User guides and import templates are delivered through the non-runtime package workflow.
- Packaging evidence:
	- Artifacts/Phase37/Publish-Separation-20260515.md
	- Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md

## 2. Admin Scope

Admin role is department-focused. Typical responsibilities:
- Maintain department users
- Configure semester operations
- Coordinate course offerings
- Monitor attendance and results lifecycle
- Communicate policy notices

System-wide controls such as licensing and global module switches remain with SuperAdmin.

## 3. Dashboard and Operational Monitoring

Dashboard can include:
- Enrollment overview
- Attendance risk indicators
- Result publication status
- Pending requests and alerts

Daily routine recommendation:
- Check unresolved alerts
- Confirm key academic workflows are progressing

## 3.1 Role and Institute Filter Behavior

Admin visibility and actions are controlled by both role and institute scope:
- Menus and data listings can differ by active institution context (School/College/University).
- Department-scoped operations remain limited to assigned and institute-compatible departments.
- Report and analytics requests may be denied when institution context mismatches requested scope.

Operational recommendation:
- If data appears missing, verify selected department/institution filters and assignment scope before raising incident tickets.

## 4. User Management (Department)

Path: Sidebar > Users or Department Management

Typical tasks:
- Create/update faculty and student-linked accounts per policy
- Reset passwords when required
- Activate/deactivate accounts
- Escalate lockout patterns to security process

Security notes:
- Do not share temporary passwords in plain channels.
- Enforce role correctness before enabling account access.

## 5. Academic Structure and Scheduling

Path: Sidebar > Academic / Courses / Timetable

Core actions:
1. Verify department and program setup.
2. Open/close semester windows.
3. Create or verify course offerings.
4. Assign faculty to offerings.
5. Publish timetable after conflict check.

Before publishing timetable:
- Check room capacity
- Check faculty overlap
- Check class time collisions

## 6. Enrollment and SIS Oversight

Path: Sidebar > Enrollments

Admin enrollment workflows:
- **View roster**: Select a course offering from the dropdown to see all enrolled students (registration number, program, semester).
- **Enroll a student**: When an offering is selected, click "Enroll Student", choose the student profile from the dropdown, and confirm.
- **Drop an enrollment**: Click the Drop button next to any active enrollment row in the roster.

SIS oversight tasks:
- Verify student profiles and program mapping
- Monitor active enrollments
- Address registration exceptions

If a student cannot access course content, verify enrollment status first.

## 8. Analytics and Advanced Reporting

Path: Sidebar > Analytics, Reports

### Standard Analytics
Admin analytics coverage includes:
- Enrollment metrics by course, program, and department
- Attendance trends and at-risk student identification
- Result publication status and grade distributions
- Assignment and quiz completion rates

### Advanced Analytics (Phase 31.2)

#### Top Performers
1. Navigate to **Analytics > Top Performers**.
2. Select **Academic Level** (Semester/Grade/Year based on institution mode) and **Department**.
3. View ranked students by performance metric (GPA/Percentage).
4. Export to **PDF** or **Excel** for reporting or distribution.

#### Performance Trends
1. Navigate to **Analytics > Performance Trends**.
2. Select **Time Period** and **Academic Level**.
3. Review trend charts showing performance changes over selected period.
4. Identify improvement or decline patterns by course or department.
5. Export trend analysis to PDF/Excel.

#### Comparative Department Metrics
1. Navigate to **Analytics > Comparative Metrics**.
2. Select **Metric Type** (average GPA, enrollment, attendance).
3. Review cross-department comparison charts and tables.
4. Identify high and low-performing departments.
5. Export for executive briefing or board reporting.

### Institution-Specific Report Sections (Phase 31.1)

Reports are partitioned by institution type:
- **School Mode**: school_outcomes section includes grade-promotion metrics and class-level analytics.
- **College Mode**: college_progression section includes progression rates and year-wise analytics.
- **University Mode**: university_academics section includes semester progression, GPA analysis, and degree-completion metrics.

When accessing Reports:
1. Navigate to **Reports**.
2. Available report sections are automatically filtered by institution context.
3. Select a report from available sections.
4. Apply filters (department, academic level, date range) as needed.
5. Review and export results.

### Export Enhancements (Phase 31.3)

Exports are now standardized across all analytics and report surfaces:
- **Synchronous Export**: PDF/Excel downloads complete in seconds for typical report sizes (< 50MB).
- **Queued Export**: Large reports (> 50MB) are processed asynchronously. You receive a job ID and email notification when complete.
- **Filename Convention**: All exports follow pattern `{report-key}-{timestamp}.{extension}` (e.g., `analytics-top-performers-2026-05-14-143022.pdf`).
- **Access Queued Export**: Navigate to **Admin Dashboard > Export History** to view and download completed jobs.

Export workflow:
1. Select a report or analytics view.
2. Click **Export as PDF** or **Export as Excel**.
3. If synchronous, download begins immediately.
4. If queued, you receive a reference ID. Check Export History for status.
5. Once complete, download from Export History or via email link.

## 9. Lifecycle and Stream Management (School Mode)

Path: Sidebar > Student Lifecycle, Stream Management (where applicable)

### Grade-Based Lifecycle (School/College)
For institutions in School or College mode:
- Academic levels are labeled **Grade** (School) or **Year** (College) instead of Semester.
- Promotion workflows enforce institution-specific pass-threshold rules.
- Stream assignment (School, Grades 9-12) controls subject visibility for students.

### Stream Assignment (School Grades 9-12)
1. Navigate to **Student Lifecycle > Stream Management** (if visible).
2. Select **Grade 9, 10, 11, or 12**.
3. Choose **Stream**: Science, Biology, Computer, Commerce, or Arts.
4. Click **Assign** to apply to student group or individual students.
5. Verify students can now see stream-aligned subjects in their course listings.

Stream subject alignment:
- **Science**: Physics, Chemistry, Biology, Math, English (core subjects always visible).
- **Commerce**: Accounting, Economics, Business Studies, Math, English.
- **Arts**: History, Geography, Literature, Philosophy, English.
- **Computer**: Computer Science, Mathematics, Physics, English.
- **Biology**: Biology, Chemistry, Botany, Zoology, English.

## 10. Institution-Aware Academic Terminology

Vocabulary adapts by institution policy (Phase 23.2):
- **Semester/Grade/Year**: Period label changes based on institution type.
- **Progression/Promotion**: Advancement label changes based on context.
- **GPA/Percentage/Marks**: Grading interpretation changes based on institution type.

When managing students or viewing reports, terminology will automatically adjust. No configuration is needed; it is driven by the active institution license policy.
- Monitor attendance compliance and outliers
- Review correction requests where policy requires approval
- Track publication status of results
- Ensure governance timelines are met

Audit guidance:
- Keep clear review notes when approving or rejecting changes.

## 8. Notifications and Communication

Path: Sidebar > Notifications

Use notifications for:
- Registration windows
- Deadline reminders
- Policy updates
- Emergency announcements

Message template recommendation:
- What changed
- Who is affected
- Required action
- Deadline

## 9. Reports

Path: Sidebar > Reports

Useful report categories:
- Attendance summary
- Results summary
- Enrollment summary
- Department KPI snapshots

For executive review, export periodic summaries and archive according to institutional policy.

## 10. Payment and Request Workflows

If enabled in deployment:
- Review payment receipt statuses
- Track submitted and pending receipts
- Coordinate verification with finance policy

For change requests:
- Review pending requests with evidence
- Approve/reject with concise justification

## 11. Common Issues

1. Faculty cannot see assigned course:
- Check course offering assignment and semester state

2. Students missing from attendance sheet:
- Check enrollment status and offering linkage

3. Results visible for wrong audience:
- Verify publication status and role access settings

4. Timetable conflicts:
- Re-run conflict checks before publish

## 12. Admin Weekly Checklist

- Verify active semester health
- Review pending user/account actions
- Validate attendance anomalies
- Confirm results publication progress
- Publish necessary departmental notices
- Escalate system-wide issues to SuperAdmin

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.

## Phase 40.3 Scoped Governance Update (2026-05-25)

- Department-level Program operations now run under tenant and campus scoped rules with explicit activation and deactivation support.
- Program create and update actions now require department ownership inside effective scope.
- Report Center availability may now vary by tenant and campus because report activation is governed per scope.
- Sidebar and module governance synchronization now updates existing environments so admin-visible menus better match configured role and module policy.
- New governance surfaces may appear only for privileged roles: Admin Users, Tenant Management, and Campus Management.

## Phase 40.4 Institution-Aware Certificates Update (2026-05-26)

- Generate Certificates now uses institution-aware behavior:
	- University mode: generate degree and transcript for scoped, graduation-ready students.
	- School/College mode: upload and manage additional student certificates.
- Additional certificate uploads are admin-managed and remain restricted by tenant/campus/department scope.
- Degree/transcript actions are hidden automatically when university mode is disabled in policy/license.
- Period filter wording is now context-aware (`Class` in university mode; `Semester` in non-university modes).

## Phase 40.5 Results and Attendance Governance Update (2026-05-28)

- Results governance now separates write vs publish responsibilities with stricter role checks.
- Correction requests for marks require explicit reason capture and published-state precondition.
- Attendance and result audits should include both bulk seeded rows and non-bulk timeline rows.
- Post-deployment checks now include published/draft result counts and attendance status distribution checks.
- Full demo validation marker is now `DemoDatasetVersion = FullDummyData-v9`.

