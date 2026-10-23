# Student User Guide

Version: 1.8  
Date: 10 June 2026  
Completion Status: Phase 38 complete + ISO 27001 & ISO 9001 Compliance (Phases 1-10) + 2026-06-10 Enhancements

## 1. Purpose

This guide helps students use Tabsan EduSphere for coursework, attendance, quizzes, results, transcript download, notifications, FYP updates, and AI chat support.

## 1.1 What's New (May 2026)

- Dashboard and lifecycle terminology now follows institution context (Semester/Grade/Year) based on your institution's policy (Phase 23.2).
- Stream assignment (School Grades 9-12): Your course offerings automatically filter based on assigned stream (Science, Biology, Computer, Commerce, Arts) (Phase 25.2).
- AI chat access uses a persistent floating launcher when chat module is licensed.
- Parent-linked notification experience can be active in school-focused deployments where parent module is enabled.
- Results, transcript, and data surfaces follow tighter role and institute scope boundaries for your privacy.
- Performance analytics and trends are now available in some institutions; view your individual performance compared to class averages (Phase 31.2).
- **Account Security (June 2026)**: Your account is now protected with enhanced security — passwords expire every 90 days, sessions timeout after 30 minutes of inactivity, and all login activity is monitored. Enable MFA (Multi-Factor Authentication) in your profile settings for extra protection.

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

### 2.1 Account Security and Privacy (ISO 27001)

Your account is protected by institutional security standards:
- **Strong Passwords**: Minimum 12 characters with uppercase, lowercase, digit, and special character. Change your password every 90 days when prompted. Your last 5 passwords cannot be reused.
- **Session Safety**: Always log out when using shared or public computers. Your session automatically expires after 30 minutes of inactivity.
- **Login Protection**: All login attempts are monitored. After 5 failed attempts, your account is temporarily locked. Contact your department admin if locked out.
- **MFA (Multi-Factor Authentication)**: Enable in your profile settings for stronger account security.
- **Data Privacy**: Your personal data is classified as Confidential and protected per GDPR. You have the right to access, correct, or request deletion of your data. Contact your institution's Data Protection Officer for privacy requests.

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
1. Open a course assignment (**Submit** or **Open** in the list).
2. Read instructions, due date, accepted file types and size limit.
3. Attach up to 10 files and/or type your answer, then **Submit**.
4. Your attempt appears under *Your submission* with its time; click a file to preview it.
5. If your teacher allows more than one attempt, use **Submit again** — earlier attempts are kept.

After grading:
- Your mark, feedback and rubric breakdown appear once your teacher posts the grade.
- Files your teacher annotated show their notes in the preview; **Annotated PDF** downloads a copy with the marks and a list of comments.
- Use *Comments* to ask a question or reply — type a message or press **Record** for a voice message.

Important notes:
- Submissions after the due date are not accepted.
- Web pages, scripts and programs cannot be uploaded.

## 5.1 Due Dates, Late Work, Groups and Peer Review (Plan M8)

- **Your dates:** an assignment may show *Your own dates* when your teacher gave you an extension. The page also shows when submissions open, until when late work is accepted and the course's late policy (for example "Late work loses 10% of the points per day late").
- **Late and missing work:** late work may lose points — your grade shows the mark and the deduction. Work never handed in may get an automatic grade after the due date; handing it in late (while still allowed) replaces it.
- **Groups:** Course Content or Assignments › **My groups** shows your groups. If students sign up themselves, join or leave a group there (not after your group has handed in work). For a group assignment one member hands in for everyone, and you must be in a group to hand in. Your group also has private discussion threads.
- **Peer review:** after the due date you may be asked to review classmates' work — open the assignment, choose **Review**, read the work, choose rubric levels or a score and write a helpful comment. Reviews of your own work appear on the assignment page (anonymous unless your teacher turned that off).

## 5.2 Similarity Score (Plan M9)

If your teacher turned it on, the assignment page shows *Similarity check: NN%* — how much of your work matches other sources. You never see other students' work. Quote and reference what you use; if the score seems wrong, ask your teacher, who can see the full report.

## 5.3 Live Classes (Plan M10)

Your teachers' online classes are listed under **Live Classes** and on the academic calendar. You are notified when a class is scheduled, moved or cancelled, and reminded 15 minutes before. From 15 minutes before the start, a *Live now* banner with **Join** appears on every page — joining through the portal records your attendance. Use **Add to calendar** to put a class in your phone's or computer's calendar.

## 5.4 Language (Plan M11)

Choose English, اردو (Urdu) or العربية (Arabic) from the language box in the top bar or on the login page. The whole portal — menus, pages, messages — switches at once, and right-to-left for Urdu and Arabic. Your choice is remembered on every device; emails from the portal also come in your language.

## 5.5 Mobile app (Plan M12)

Install the EduSphere app from your institution's store link and sign in with your usual username and password (and authenticator code), or tap your institution's sign-in button — it opens your phone's browser and comes back to the app. The app shows today's classes, what is due, your courses and materials, results, attendance and report cards. Open an assignment and **Hand in** with a photo from the camera or a file. Quizzes open in the portal. Turn on notifications to hear about new work, results and announcements; choose the language under Settings.

## 5.6 SCORM Packages (Plan M13)

Some courses contain interactive lessons (SCORM packages). Open one from the course page or its module and press **Start**. Your place is saved: **Continue** takes you back where you stopped. When you finish, the package can be opened again for **Review**; if your teacher allows more attempts, **Start a new attempt** — your best score counts. Use **Exit** on the player page to leave (your progress is saved first).

## 6. Quizzes

Path: Sidebar > Quizzes

Typical flow:
1. Open the quiz from *View Quizzes* (**Start**, **Resume** or **Open**).
2. Review attempts allowed, timer, and availability window; enter the access code if your teacher gave one.
3. Start the attempt. Answers save automatically as you go.
4. Answer the questions (some types: drag items into order with the arrows, choose matches from the lists, type into the blanks inside a sentence, upload a file).
5. Submit before time ends — when the timer reaches zero your saved answers are submitted for you.
6. **Review** shows what your teacher allows: your score, your answers and/or the correct answers.

Tips:
- Stable internet is recommended; if you lose connection, your last saved answers are kept and the timer keeps running on the server.
- If the quiz shows one question at a time with locking, you cannot go back after pressing Next.

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
- Download transcript in supported format (where available by institution mode and policy)

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

### 9.1 Notifications on your phone or computer
1. Open **Notifications** and, in *Notifications on this device*, press **Turn on**, then allow notifications when the browser asks.
2. Choose which kinds of notification reach this device (for example announcements and assignments).
3. **Send a test** checks it works; **Turn off** stops notifications on this device only.
- iPhone/iPad: first add EduSphere to the Home Screen (Share › Add to Home Screen) and open it from there.
- Signing out also stops notifications on that device.

### 9.2 Install EduSphere as an app
- On Android/desktop Chrome or Edge choose **Install the EduSphere app** in your profile menu (or the browser's install icon). On iPhone/iPad use Share › Add to Home Screen.
- Pages you opened recently — dashboard, timetable, announcements, course materials and notifications — and course files you downloaded can still be read when you are offline. You cannot submit work or change anything while offline.
- On a shared computer, always **Sign Out**: it removes the saved pages from that device.

### 9.3 External tools in your courses

- Your course page may list **External tools** (for example H5P activities, Zoom or Turnitin). Press **Open** — you are signed in to the tool automatically.
- Items marked **Graded** send your score back to EduSphere; it appears under the assignment once your teacher's posting policy releases it.
- If a tool says the launch has expired, go back to the course and press **Open** again.

### 9.4 My Outcomes

- **My Outcomes** shows, for each course that uses learning outcomes, how well your graded work shows you have mastered each outcome: *Mastered*, *Nearly there*, *Not yet* or *No evidence yet*.
- Open **Evidence** to see which assignments and quizzes it comes from. Only grades your teacher has released count.

### 9.5 Course modules and your progress

- **Course Content** shows your progress in the course and in each module. Open items from the list; ✓ means done, ○ means still to do (with what is needed), 🔒 means locked (with why — for example *Complete “Week 1” first* or an opening date).
- Use **Next ›** and **‹ Previous** at the top of an item to move through the module, and **Mark as done** where an item asks for it.

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

## Phase 40.7 Professional Certificates Update (2026-06-15)

- Certificates are now generated as professional DOCX documents with navy+gold branding and signature blocks.
- File naming: {RegNo}-{Type}.docx (e.g., COL-REG-11-06-Transcript.docx).
- University students see GPA/CGPA; School/College students see Percentage on certificates.
- Completion Certificate available after Class 10 (School) or Class 11+12 (College).
- Report Card shows all class percentages with professional formatting.
- Students can view and download their own certificates from the portal.

## Phase 40.3 Scoped Governance Update (2026-05-25)

- Student-facing access behavior remains role-safe, with stronger tenant and campus scoped governance in shared environments.
- Menu visibility consistency was improved by synchronized sidebar governance and module entitlement checks.
- If a student menu appears missing, support teams should verify role, active module state, and tenant/campus scope mapping before raising incidents.

## Phase 40.4 Institution-Aware Certificates Update (2026-05-26)

- Certificate surfaces are now institution-aware:
	- University context: transcript workflows follow university policy and visibility settings.
	- School/College context: students can view/download additional certificates uploaded by authorized admins.
- Degree/transcript options may be hidden when university mode is disabled by active policy/license.
- Period wording in related academic/certificate views is context-aware (`Class` in university mode; `Semester` in non-university modes).

## Phase 40.6 ISO 27001 + ISO 9001 Compliance Update (2026-06-04)

- Enhanced password policy: 12-character minimum, 90-day expiry, 5-password history.
- Idle session timeout: 30 minutes.
- Login activity monitoring for account protection.
- MFA available in profile settings.
- Data privacy rights and consent tracking per GDPR.
- All changes are additive and backward-compatible.

## Phase 40.5 Results and Attendance Governance Update (2026-05-28)

- Student-visible results continue to show published marks only; draft correction-stage records remain hidden.
- Result corrections now follow stricter governance and must include reason/audit context before republishing.
- Attendance data quality is improved through expanded multi-day records in the training/demo baseline.
- If a result is delayed, it may be under controlled `FinalReview` workflow pending authorized publication.

## 16. Detailed Student Semester Playbook

Follow this practical sequence each semester.

### 16.1 Week 1-2: Setup and Orientation

1. Confirm all enrolled courses are visible in dashboard and enrollments.
2. Verify timetable and class modality.
3. Review assignment and quiz policy for each course.
4. Enable notification monitoring habit (daily check).

### 16.2 Mid-Semester: Continuous Performance Control

1. Track assignment status twice weekly.
2. Review attendance per course at least once a week.
3. Check quiz attempts remaining before each assessment window.
4. Monitor performance trend and seek faculty guidance early.

### 16.3 End-Semester: Closure and Records

1. Verify all required submissions are complete.
2. Confirm published results by course.
3. Download transcript/report exports where needed.
4. Resolve discrepancies through official correction channels.

## 17. Student Data and Privacy Notes

Your access is intentionally restricted for security.

- You can see only your own academic and activity records.
- You cannot access department-level or admin-level reports.
- Some features are hidden by role, module, or institution policy.
- Sensitive operations are audited for compliance.

## 18. Student Self-Service Recovery Guide

1. Login trouble
- Recheck username/registration number format.
- Reset password through approved workflow.
- Contact admin if account is locked.

2. Missing assignment or quiz
- Confirm you are in correct course/period context.
- Confirm publication state and availability window.
- Raise issue with faculty including course and timestamp.

3. Attendance mismatch
- Capture date, class, and evidence.
- Request correction through faculty/admin process.

4. Result discrepancy
- Check if result is published or still under review.
- Submit correction/recheck request with evidence.

## 19. Student Productivity Tips

- Keep a weekly planner aligned with assignment due dates.
- Submit assignments at least 24 hours before deadline when possible.
- Avoid last-minute quiz attempts to reduce technical risk.
- Track low-attendance courses first and prioritize correction.

## 20. Student Weekly Checklist (Expanded)

- Dashboard reviewed daily.
- Assignments reviewed and pending items prioritized.
- Quiz windows checked before expiry.
- Attendance reviewed for all active courses.
- New notifications processed and archived.
- Performance concerns communicated to faculty early.

