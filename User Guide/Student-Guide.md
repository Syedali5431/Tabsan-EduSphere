# Student User Guide

Version: 1.5  
Date: 15 May 2026  
Completion Status: Phase 38 complete (final separation baseline)

## 1. Purpose

This guide helps students use Tabsan EduSphere for coursework, attendance, quizzes, results, transcript download, notifications, FYP updates, and AI chat support.

## 1.1 What's New (May 2026)

- Dashboard and lifecycle terminology now follows institution context (Semester/Grade/Year) based on your institution's policy (Phase 23.2).
- Stream assignment (School Grades 9-12): Your course offerings automatically filter based on assigned stream (Science, Biology, Computer, Commerce, Arts) (Phase 25.2).
- AI chat access uses a persistent floating launcher when chat module is licensed.
- Parent-linked notification experience can be active in school-focused deployments where parent module is enabled.
- Results, transcript, and data surfaces follow tighter role and institute scope boundaries for your privacy.
- Performance analytics and trends are now available in some institutions; view your individual performance compared to class averages (Phase 31.2).

## 1.2 Documentation Baseline (15 May 2026)

- Institution parity baseline remains active for School, College, and University contexts.
- Student terminology and lifecycle labels remain institution-aware by policy (Semester/Grade/Year).
- Standard DB deployment run path is Scripts/01 through Scripts/05.
- Consolidated enhancement and roadmap tracking is maintained in Docs/Consolidated-Execution-Enhancements-Issues.md.

## 1.3 Final Release Packaging Update (Phase 37/38)

- Runtime app publish and license app publish are separated for final delivery.
- Student-facing documentation is included in a separate non-runtime asset package.
- Package evidence references:
	- Artifacts/Phase37/Publish-Separation-20260515.md
	- Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md

## 2. Access and Login

1. Open the institutional EduSphere URL in a supported browser.
2. Enter your username and password.
3. Click Login.
4. If your institution allows student self-registration, use Sign Up with your registration number.

If login fails multiple times, contact your department admin for account unlock support.

## 3. Dashboard Overview

The student dashboard usually shows:
- Active semester snapshot
- Upcoming deadlines
- Recent notifications
- Attendance warnings
- Latest grades and FYP reminders

Use this page daily to avoid missing deadlines.

## 3.1 Role and Institute Filter Behavior

Student access is intentionally scoped for safety and parity:
- You can access student-safe surfaces like dashboard context, transcript/results views, assignments, quizzes, and notifications.
- Operational admin/faculty report endpoints are restricted for Student role.
- Visible terms and filter behavior can vary depending on institution context (School/College/University).

If you cannot access expected learning data, contact faculty/admin with course, semester, and current filter details.

## 4. Academic Record and Semester History

Path: Sidebar > My Academic Record

You can:
- View current and previous semester records
- Review enrolled courses
- Check GPA and CGPA
- Track published marks per course

Best practice: compare your course load and GPA trend at least once per week.

## 5. Assignments

Path: Sidebar > Assignments

Typical flow:
1. Open a course assignment.
2. Read instructions and due date.
3. Submit file or text response.
4. Confirm submission.
5. Reopen the assignment to verify timestamp and status.

Important notes:
- Treat submission confirmation as final unless your institution allows resubmission.
- Late submissions may be blocked based on faculty settings.

## 6. Quizzes

Path: Sidebar > Quizzes

Typical flow:
1. Open active quiz.
2. Review attempts allowed, timer, and availability window.
3. Start attempt.
4. Answer all required questions.
5. Submit before time ends.

Tips:
- Stable internet is recommended.
- Do not refresh during active timed attempts unless instructed.

## 7. Attendance Tracking

Path: Sidebar > Attendance

You can:
- Review attendance by course
- Identify low-attendance courses
- Monitor threshold warnings

If you believe attendance is wrong, contact the course faculty with date, course code, and evidence.

## 8. Results and Transcript

Path: Sidebar > Results

You can:
- View published midterm/final marks
- Track percentage per course
- Download transcript in supported format

Transcript export activity may be logged for audit.

## 9. Notifications

Path: Top bar bell icon or Sidebar > Notifications

Notification types can include:
- Assignment
- Quiz
- Result
- Attendance alert
- FYP meeting update
- Admin announcements

Action recommendation: open notifications daily and mark read after action.

## 10. FYP Module

Path: Sidebar > Final Year Project

You can:
- View project status
- Check supervisor/panel details
- Track meeting schedule and notes

For schedule conflicts, contact supervisor first, then department admin if needed.

## 11. AI Chat Assistant

Path: Chat icon (if module enabled)

Suggested usage:
- Ask for deadline reminders
- Clarify portal navigation
- Request summaries of your upcoming tasks

Do not share sensitive personal data in chat messages.

## 12. Enrollments

Path: Sidebar > Enrollments

You can:
- View all your currently enrolled courses with status (Active or Dropped)
- See enrollment date for each course
- Drop an active enrollment by clicking the Drop button on that row
- Enroll in a new course by clicking "Enroll in Course" and selecting from available open offerings

Notes:
- Only courses with open offerings appear in the enrollment list.
- Dropping an enrollment may affect attendance and result records — contact your admin if you do so in error.
- Enrollment windows may be limited by institutional policy.

## 13. Theme and Personalization

Path: Profile menu > Settings > Appearance

You can switch theme to improve readability and accessibility.

## 14. Common Issues

1. Cannot log in:
- Check username format
- Verify keyboard caps lock
- Request password reset

2. Assignment not visible:
- Confirm correct semester/course
- Confirm assignment is published

3. Result not visible:
- Results appear only after faculty publication

4. Attendance looks incorrect:
- Contact course faculty with exact lecture date

## 15. Student Checklist

Use this weekly:
- Review dashboard
- Check assignments and quizzes
- Verify attendance in all enrolled courses
- Review new notifications
- Track result publication updates

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.

## Phase 40.3 Scoped Governance Update (2026-05-25)

- Student-facing access behavior remains role-safe, with stronger tenant and campus scoped governance in shared environments.
- Menu visibility consistency was improved by synchronized sidebar governance and module entitlement checks.
- If a student menu appears missing, support teams should verify role, active module state, and tenant/campus scope mapping before raising incidents.

