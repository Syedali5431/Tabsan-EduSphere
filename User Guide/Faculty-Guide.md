# Faculty User Guide

Version: 1.9  
Date: 15 June 2026  
Completion Status: Phase 38 complete + ISO 27001 & ISO 9001 Compliance (Phases 1-10) + 2026-06-15 Certificate Enhancements

## 1.1 What's New (June 2026)

- **Professional Certificates** — Generate and view professional Degree, Transcript, Completion, and Report Card documents.
- **Scoring Rules** — University=GPA/CGPA; School/College=Percentage on all certificates.
- **Reports** — Attendance, Result, Assignment, and Quiz reports now include Program and Department columns; reports can run without mandatory filter selection.
- **Semester Order** — Semester lists now sorted ascending (Semester 1 first).
- **Dropdown Filters** — Deactivated tenants/campuses hidden from dropdowns.
- **Session Timeout** — Idle session logout at 5 minutes.

## 1. Purpose

This guide explains faculty workflows in Tabsan EduSphere: teaching setup, assignments, quizzes, attendance, results, FYP supervision, notifications, and reporting.

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

Submission settings (Grade > Settings, any time):
- **Attempts allowed** — 1 by default; 0 means unlimited. Every resubmission is kept as a separate attempt.
- **Accepted file types** — leave blank for documents, images, audio, video and zip; or list types such as `.pdf, .docx`. Web pages, scripts and programs are never accepted.
- **Max file size** — up to 50 MB per file (10 files and 100 MB per attempt at most).
- **Students see grades** — *as soon as they are saved* (default) or *only when I post them*.
- **Anonymous grading** — graders see "Student 1, Student 2…" until every grade is posted (or you press *Reveal names*).
- **Moderated grading** — other graders' marks become provisional; the moderator (whoever switched moderation on) picks the final grade.

Grade submissions in the grading workspace (Assignments > **Grade**):
1. The left column lists submissions (filter: All, Needs grading, Late, Graded not posted).
2. The middle column shows the selected file: PDFs and images open in the viewer; Word/PowerPoint/Excel files appear as PDF once the conversion service has prepared them (otherwise use Download).
3. Mark the work with the toolbar: highlight, strike-out, box, text, comment pin and freehand drawing. Click an annotation in the list below the page to jump to it; type in its box to attach a comment.
4. Enter marks and feedback on the right — they save automatically (Ctrl+Enter saves at once). If the assignment has a rubric, choose a level per criterion and *Save rubric*; the total can set the marks (scaled to the assignment's marks).
5. Add comments for the student, or tick *Graders only* for a private note. *Record* adds a voice comment.
6. Move to the next student with **Alt+→** (previous: **Alt+←**) or the arrow buttons — the page does not reload.
7. With manual posting, press **Post grades** when you are ready; students then see marks, feedback, comments and annotations. *Hide grades* takes them back.

Good practices:
- Attach a rubric to the assignment so students see how marks were reached.
- Keep feedback specific and actionable; use annotations for line-level comments.
- Posted grades also update the gradebook's "Assignment"/"Assignments" component when your institution has one configured (hand-entered cells are never overwritten).

## 6. Quiz Management

Path: Sidebar > Quizzes

Create quiz:
1. Select course offering.
2. Configure title, time limit, attempts, and availability window.
3. Open **Build** and add questions. Eleven types are available: multiple choice, multiple answers, true/false, matching, ordering, fill in the blanks (`[[name]]` marks each blank), numeric (exact ± tolerance or a range), formula (variables in `[brackets]` get new values for every student), short answer, essay and file upload.
4. Optionally draw random questions from a **question bank** ("pick 5 from bank X") or copy bank questions in. Banks can be imported/exported as Excel.
5. Set delivery and security: shuffle questions/options, one question at a time, lock answers, access code, allowed networks, and what students see afterwards (nothing, score, their answers, correct answers — optionally only after the quiz closes).
6. Give individual students extra time/attempts or different dates under *Extra time & attempts*.
7. Publish.

Review attempts:
1. Open **Attempts & grading**: the queue lists short answers, essays and uploaded files waiting for marks.
2. Objective questions are marked automatically (with partial credit where it makes sense).
3. To fix a wrong key after students have answered, edit the question and press **Regrade** (recalculate, or give everyone full credit).
4. **Statistics** shows difficulty and discrimination per question and how often each option was chosen.

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

## 10.1 App Install and Device Notifications (Plan M4)

- Install EduSphere from the profile menu (*Install the EduSphere app*) or the browser's install icon; on iPhone/iPad use Share › Add to Home Screen.
- Turn on notifications for this device under **Notifications › Notifications on this device** and choose which types you want. Notifications you send to students also reach their subscribed devices.
- Recently opened timetable, announcements, course material and notification pages stay readable offline; anything that changes data needs a connection. Sign out on shared computers — it clears the saved pages.

## 10.2 External Tools in Your Course (Plan M5)

Path: LMS Manage › External tools (shown when your institution has registered tools)

- **Add an external tool**: choose the tool, a title, where it opens (**New tab** or **Inside EduSphere**) and, for a graded activity, **Points**. A graded item creates an assignment that receives the tool's scores automatically.
- **Choose content from <tool>**: pick content inside the tool (for example an H5P activity); it is added to the course, with an assignment for anything the tool grades.
- **Hide / Show** an item; **Grades** opens the grading workspace, where you can still adjust or post grades. Tool scores follow the assignment's posting policy.
- Students open items from the course page without signing in again. If a tool shows a blank frame, set it to open in a new tab.

## 10.3 Learning Outcomes and Mastery (Plan M6)

Path: Learning Outcomes (menu) — when your institution uses the Learning Outcomes module

1. **Course outcomes (CLOs):** on **Learning Outcomes**, choose Level *Course*, your course, a code (e.g. CLO1) and a title. Keep the default 0–4 scale or set your own levels, the mastery points and how mastery is calculated (decaying average, highest, most recent, average, or "mastered n times"). Under *contributes to these program outcomes*, tick the PLOs this CLO supports.
2. **Align:** press **Outcomes** on a rubric criterion (Rubric Management), a quiz question (Quiz Builder), a bank question (Question Banks) or an assignment's settings, and tick what it assesses. Work you have already graded is counted straight away.
3. **Mastery gradebook:** **Learning Outcomes › Mastery gradebook** shows each student × outcome, green (mastered), amber (near) or red (below). Export it to Excel. Only grades students can see count — unposted grades and ungraded essay answers are left out until you post or grade them.
4. **Import:** download the template on the Learning Outcomes page to add many outcomes (and their PLO mapping) at once.

## 10.4 Module Items, Requirements and Progress (Plan M7)

Path: LMS Manage › a module › Items

1. **Add items** in the order students should follow: assignments, quizzes, discussions, external tools, videos, course materials, text pages, web links and headings. The form only shows what each type needs.
2. **Requirements:** choose what completes each item — *view it*, *mark it as done*, *submit it*, *score at least …%* (posted grade or best quiz attempt) or *post in it* (discussions).
3. **Release and completion:** set an opening date (UTC), require items *in order*, choose whether *every* or *any one* required item completes the module, and pick modules that must be complete first. Students see locked items with the reason; locked quizzes, assignments and tools cannot be started or submitted.
4. **Student progress** (top of LMS Manage) shows each student's progress per module; click a name for item-by-item detail.

Tips: changing an item's requirement re-checks it for everyone; once a requirement is met it stays met, so a later regrade does not lock a student out.

## 10.5 Late Policy, Groups, Assign-to Dates and Peer Review (Plan M8)

Path: Assignments › choose a course › **Late policy & groups**, and **Options** next to an assignment (also **Options & dates** in the grading workspace).

1. **Late & missing work (per course):** deduct a percentage of the points for every started hour or day late, never below a lowest grade; optionally grade work never handed in (given automatically once each student's due date passes — handing in late work replaces it). **Grade missing work now** applies it at once.
2. **When students can hand in (per assignment):** an opening date and a *late work accepted until* cut-off. Work after the due date is late; after the cut-off it is refused. Dates are entered in UTC.
3. **Assign to — different dates:** give one student (e.g. an extension) or one group their own due date, opening date and cut-off. A student's own dates win over their group's.
4. **Groups:** create a group set, then add groups by hand, **Split at random** (by number of groups or students per group) or let students sign up (with a size limit). Choose the group set on an assignment to make it group work: one member hands in for the group, and one grade goes to every member unless you choose to grade each member separately. Groups also get private discussion threads (Discussion › *Post to*).
5. **Peer review:** set reviews per student (0–5), a review due date and anonymity. Reviews are handed out automatically after the due date among students who handed in work (or press **Assign reviews now**). In the grading workspace you see each submission's peer average and can **Use peer average** as the mark.
6. **In the grading workspace:** a late submission shows the deduction and the final grade; tick **Waive the late deduction** to give the full mark (and untick to restore it). Missing work is marked *Missing*.

Tips: type the mark you would give — the deduction is applied on top and shown separately; the grouping of an assignment cannot change once work has been handed in.

## 10.6 Similarity (Plagiarism) Checking (Plan M9)

Path: Assignments › **Options** next to an assignment › *Similarity check*.

1. Turn on **Check every handed-in attempt** (and, if you want, **Show students their own score**). Each attempt is checked within about a minute.
2. In the grading workspace each submission shows *Similarity NN%* (green below 25%, amber below 50%, red from 50%). Select it and choose **Open report** to see the matching sources and passages side by side, or **Check again** — the first students to hand in have nothing to compare with until others hand in.
3. Typed text and .txt, .docx, .pdf (with selectable text), .html and .rtf files are read; scanned images are not. A student's own earlier attempts and their group's work never count as matches.

Tip: a high score is a prompt to look closer, not proof — quotations, references and templates match too.

## 10.7 Live Classes (Plan M10)

Path: **Live Classes** (sidebar) or the course page › *Live classes*.

1. **Schedule a class:** title, start (UTC), length, and optionally a meeting link (Google Meet, Jitsi, any Zoom/Teams link). Without a link the institution's service (BigBlueButton, Zoom or Teams) creates the meeting. Students are notified and reminded 15 minutes before.
2. **From the timetable:** pick the course's timetable slot and how many weeks — one class per week is created (weeks already scheduled are skipped).
3. **Join:** **Start / join** on the class (you join as moderator/host). While a class is on, a *Live now* banner with **Join** shows on every page.
4. **Attendance:** with *Mark attendance from who joined*, attendance is marked 10 minutes after the class ends — Present for students who joined (and stayed the minimum minutes, where Zoom/Teams report them), Absent for the others. Students who already have a mark for that day keep it. **Attendance** shows who joined; **Mark attendance now** does it at once.
5. **Edit / Cancel** tells students; **Add to calendar** downloads a calendar file; recordings appear on the class when the service has one.

## 10.8 Language (Plan M11)

The portal speaks English, Urdu (اردو) and Arabic (العربية). Pick a language from the box in the top bar (or on the login page); Urdu and Arabic pages run right to left. Your choice is saved to your account, so it follows you to other devices. Dates and numbers you type in forms work the same in every language.

## 10.9 Mobile app (Plan M12)

In the EduSphere app open a course to **Take attendance** (tap each student to switch present → absent → late → excused, then Save), grade hand-ins (marks and feedback), post a **New announcement** or talk with the class in **Discussions**. Home shows today's classes and how many submissions wait for grading. Sign in as in the portal (password or single sign-on).

## 10.10 SCORM Packages (Plan M13)

On a course's **Manage Course Content** page, open **SCORM packages › Upload a SCORM package** and choose the zip exported from your authoring tool (Articulate, iSpring, Captivate, …; SCORM 1.2 or 2004). Set the allowed attempts (0 = unlimited), whether it is **Graded** (the best score goes to the gradebook, scaled to the marks you set) and whether students can see it. Use **Preview** to try it (nothing is recorded), **Report** to see each student's attempts, status, best and last score and time, and add the package to a module with a requirement such as "Score at least 80%". A package in parts (several SCOs or pages, such as one lesson per topic and a quiz) shows students a contents list with each part's status and **Previous** / **Next**; each part keeps its own progress, the package is complete when every part is, and its score is the average of the parts' scores (packages uploaded before October 2026 start with the first part until uploaded again).

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

