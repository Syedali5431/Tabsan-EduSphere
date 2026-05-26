
(Moved to User Guide/Training Manual.md. This file is deprecated.)

Training synchronization update (26 May 2026):
- Add trainer walkthrough for runtime-safe announcement posting (invalid offering returns controlled validation feedback).
- Add trainer walkthrough for LMS content-module creation validation and save-stage offering integrity guard.
- Add trainer scenario for graduation approval/reject concurrency conflict handling and safe operator retry flow.
- Add trainer note on FYP legacy panel-role compatibility (`Internal`/`External`) during data migration demos.
- Add trainer verification for complete settings/governance menu catalog including report/sidebar/dashboard/license/policy/module/tenant/campus/admin-user.
- Add trainer workflow for institution-aware Generate Certificates behavior:
	- University: degree generation for graduation-ready students, transcript generation with class/semester filter.
	- School/College: upload multiple additional student certificates and verify student download visibility.
- Add trainer check for license-governed visibility: degree/transcript actions hidden when university mode is disabled.
- Add trainer check for period-label switch: `Class` in university context and `Semester` in non-university context.

Training synchronization update (25 May 2026):
- Add trainer walkthrough for Program Management in scoped mode (tenant/campus aware for SuperAdmin).
- Add trainer workflow for program activate/deactivate lifecycle controls.
- Add trainer workflow for report-center scope activation/deactivation from settings.
- Add trainer checklist to verify sidebar/settings visibility is role- and module-aligned.
- Add tenant/campus governance awareness for admin-user, tenant-management, and campus-management settings menus.

Final baseline update (15 May 2026):
- Phase 38 complete (final separation baseline).
- Canonical training guide: User Guide/Training Manual.md (Version 1.5).
- Packaging references:
	- Artifacts/Phase37/Publish-Separation-20260515.md
	- Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md

Documentation synchronization update (15 May 2026):
- Canonical training guide content is aligned with final execute-mode closure and packaging workflow notes.
# Tabsan EduSphere – Training Manual

**Version:** 1.1  
**Date:** 11 May 2026  
**Aligned With PRD:** v1.7 | Modules v1.2  
**Audience:** Trainers, IT Staff, Super Admins, Department Coordinators

---

## 1. Introduction

This manual is designed for trainers, IT staff, and Super Admins responsible for onboarding university personnel and students onto the Tabsan EduSphere platform. It provides structured sessions, exercises, and operational checklists to ensure a smooth rollout.

Each session is designed to stand alone. Trainers may deliver sessions in sequence or select relevant modules for targeted audiences.

---

## 2. Pre-Training Setup Checklist (Updated)

Before delivering any training session, ensure the following are in place:

- [ ] Training environment (separate from production) is available and accessible
- [ ] A valid license (with correct institution scope and expiry) has been uploaded and is active on the training environment
- [ ] At least one test account per role exists (Student, Faculty, Admin, Super Admin)
- [ ] Sample data is seeded: at least two departments, two programs, five courses, three semesters, and ten students
- [ ] At least one assignment, one quiz, one result set, and one FYP meeting are pre-created
- [ ] All trainers have reviewed the User Guide before delivery
- [ ] Projector or screen share is ready for live demonstration
- [ ] UAT, SAT, and Output docs in Docs/ are reviewed
- [ ] Import/export and index maintenance scripts are available for IT staff
## 2.1 License Tool and Acceptance Docs

The Tabsan.Lic tool now supports institution scope (School/College/University) and multiple expiry options. See Docs/UAT.md and Docs/SAT.md for acceptance criteria.

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
| Session 8 | IT Staff / Super Admin | 45 min | System health, operations, troubleshooting |

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

### Agenda
1. Attempting a quiz (10 min)
2. Attendance view and warnings (10 min)
3. FYP dashboard walkthrough (5 min)
4. AI chatbot demonstration (5 min)

### Exercise 3A – Quiz Attempt
1. Log in as a test student
2. Navigate to a published test quiz
3. Attempt the quiz and submit answers
4. View the score and review the answers

### Exercise 3B – Attendance Check
1. Navigate to the Attendance section
2. Identify any courses below threshold in the seeded data
3. Check the corresponding notification in the notification panel

### Exercise 3C – AI Chatbot
1. Open the chatbot
2. Ask: "When is my next FYP meeting?"
3. Ask: "What is my current attendance in [course name]?"
4. Note how answers reflect personal academic context

### Trainer Notes
- Attempt limits are enforced; use seeded quizzes with 2+ attempts for safe practice
- If the AI chatbot module is not licensed, note this and skip Exercise 3C
- Encourage students to explore the chatbot's contextual awareness

---

## 7. Session 4 – Faculty: Assignments, Quizzes, Attendance, Grading

### Learning Objectives
- Create and publish an assignment and quiz
- Record daily attendance
- Grade student submissions

### Agenda
1. Creating and publishing an assignment (15 min)
2. Adding quiz questions and publishing (15 min)
3. Recording attendance (10 min)
4. Grading a submission and entering feedback (20 min)

### Exercise 4A – Create and Publish an Assignment
1. Log in as a test faculty account
2. Navigate to Assignments → Create New
3. Select a course and semester, enter a title and due date
4. Save as draft, preview, then publish
5. Confirm enrolled students see the assignment

### Exercise 4B – Create a Quiz
1. Navigate to Quizzes → Create New
2. Add one MCQ question and one Short Answer question
3. Set start and end times to the next 10 minutes (for immediate testing)
4. Publish and switch to a student account to attempt it

### Exercise 4C – Record Attendance
1. Navigate to Attendance → Record
2. Select a course and today's date
3. Mark half the students present and half absent
4. Save and confirm the record is visible from a student account

### Exercise 4D – Grade a Submission
1. Open a test assignment with a pre-seeded submission
2. Open the submission, enter a grade and feedback
3. Save and switch to the student account to confirm visibility

### Trainer Notes
- Publishing an assignment triggers student notifications – demonstrate this
- Grading is audited; faculty cannot edit grades after publication without Super Admin intervention
- Short answer questions require manual grading; MCQ is auto-scored

---

## 8. Session 5 – Faculty: FYP, Notifications, Results Publication

### Learning Objectives
- Schedule and manage FYP meetings
- Send targeted notifications to students
- Publish semester results

### Agenda
1. FYP project view and meeting scheduling (15 min)
2. Sending notifications to targeted groups (10 min)
3. Entering marks and publishing results (20 min)

### Exercise 5A – Schedule a FYP Meeting
1. Navigate to FYP → Schedule Meeting
2. Select a student project
3. Set a date, time, department, room number
4. Add two panel members
5. Save and confirm the student receives a notification

### Exercise 5B – Send a Notification
1. Navigate to Notifications → Compose
2. Address it to students in a specific course
3. Send and switch to a student account to verify receipt

### Exercise 5C – Publish Results
1. Navigate to Results → Enter Marks
2. Enter marks for at least three students
3. Click Calculate GPA to preview
4. Click Publish – confirm students can now see their results

### Trainer Notes
- Once results are published they are visible to students immediately
- FYP notifications are auto-generated; manual ones are additional
- Warn faculty that result publication is irreversible without Super Admin action

---

## 9. Session 6 – Admin: Reporting, Student Data, Notifications

### Learning Objectives
- Access and search full student academic records
- Generate and export university-wide reports
- Send broadcast notifications

### Agenda
1. Navigating student profiles (10 min)
2. Generating reports (20 min)
3. Sending broadcast notifications (15 min)

### Exercise 6A – Student Academic History Search
1. Log in as a test Admin account
2. Navigate to Students
3. Search by registration number for a seeded student
4. View their full academic history across multiple semesters

### Exercise 6B – Generate a Department Report
1. Navigate to Reports
2. Select Student Performance
3. Scope to one department and the most recent semester
4. Generate and download in PDF format

### Exercise 6C – Broadcast Notification
1. Navigate to Notifications → Compose
2. Set recipients to All Students
3. Write a sample announcement
4. Send and verify at least one student account shows the notification

### Trainer Notes
- Admins see all departments but cannot change roles or system settings
- Reports contain personal academic data and should be treated as confidential
- Demonstrate the difference between targeted (faculty) and broadcast (admin) notifications

---

## 10. Session 7 – Super Admin: Users, License, Modules, Audit

### Learning Objectives
- Create and manage user accounts
- Upload and validate a license
- Activate and deactivate modules
- Navigate and export audit logs

### Agenda
1. User creation and role assignment (15 min)
2. License upload and validation (15 min)
3. Module management (15 min)
4. Audit log navigation and export (15 min)

### Exercise 7A – Create a Faculty Account
1. Log in as Super Admin
2. Navigate to Users → Create User
3. Create a new faculty account assigned to a specific department
4. Log in as that user and confirm access is department-scoped

### Exercise 7B – Upload a License
1. Navigate to Settings → License
2. Upload the provided training license file
3. Confirm status shows as Active with correct type and expiry date
4. Simulate expiry by uploading an expired test license file (if provided)
5. Confirm read-only degraded mode activates

### Exercise 7C – Activate a Module
1. Navigate to Settings → Modules
2. Identify a currently inactive optional module (e.g. Quizzes)
3. Toggle it to Active and confirm
4. Switch to a faculty account and verify the Quizzes menu item appears

### Exercise 7D – Export Audit Logs
1. Navigate to Audit
2. Filter by action type: Login, for the past 7 days
3. Verify all test logins from this session appear
4. Export the filtered log as a file

### Trainer Notes
- Mandatory modules (Authentication, Departments, SIS) have no toggle – highlight this
- License upload immediately affects all active sessions system-wide
- Audit log access is restricted to Super Admin; reinforce this during training

---

## 11. Session 8 – IT Staff / Super Admin: Operations and Troubleshooting

### Learning Objectives
- Monitor system health and background job status
- Identify and respond to common operational issues
- Follow the backup and restore runbook

### Agenda
1. Health check endpoints and dashboards (10 min)
2. Background job monitoring (10 min)
3. Common issues and resolution steps (15 min)
4. Backup and restore runbook overview (10 min)

### Common Issues Reference

| Issue | Likely Cause | Resolution |
|---|---|---|
| User cannot log in | Account inactive or wrong credentials | Check account status in Users panel; reset password |
| Assignment not visible to student | Not published or module inactive | Toggle published flag; check module status |
| License shows expired | License file outdated | Upload renewed license file from vendor |
| Notification not received | Notification module inactive | Activate Notifications module in Settings |
| Quiz attempt button disabled | Attempt limit reached | Check quiz settings; Super Admin can reset if needed |
| Report export fails | No data in selected range | Verify scope and date range match seeded data |
| Background job not running | Host service stopped | Restart BackgroundJobs service; check operational logs |

### Health Check Endpoints
- `/health` – overall system status
- `/health/db` – database connectivity
- `/health/license` – current license validation state
- `/health/jobs` – background worker status

### Backup Runbook Summary
1. Confirm automated daily backup job completed via operations log
2. Verify backup file exists in designated backup storage location
3. To restore: stop application, restore database from backup, restart application
4. Test restore on staging before performing on production

### Trainer Notes
- IT staff do not need portal accounts for health monitoring – health endpoints are accessible without authentication on internal networks only
- Always test backups by performing a restore to staging on a regular schedule
- Escalate unresolved license issues directly to the vendor

---

## 12. Post-Training Assessment Checklist

Use this checklist to confirm successful completion of training for each role.

### Student
- [ ] Successfully signed up using a registration number
- [ ] Located academic history for two semesters
- [ ] Submitted an assignment
- [ ] Attempted a quiz
- [ ] Downloaded a transcript
- [ ] Identified a notification and marked it read

### Faculty
- [ ] Created and published an assignment
- [ ] Graded at least one submission
- [ ] Recorded attendance for a full course
- [ ] Created and published a quiz
- [ ] Scheduled an FYP meeting
- [ ] Published results

### Admin
- [ ] Searched and opened a student academic profile
- [ ] Generated and exported a report
- [ ] Sent a broadcast notification

### Super Admin
- [ ] Created a user and assigned a role
- [ ] Uploaded and validated a license file
- [ ] Toggled a module on and off
- [ ] Exported an audit log

---

## 13. Trainer Delivery Tips

- Use a training environment that mirrors production data structure but contains no real student data
- Run all exercises yourself before the session to verify data is in place
- Allow time for questions after each exercise block
- If a module is not licensed in the training environment, explain it conceptually and skip the exercise with a note
- Record sessions where possible for self-paced follow-up

---

## 14. Glossary

| Term | Definition |
|---|---|
| Registration Number | Unique identifier issued to each student by the university |
| Module | A functional feature group that can be enabled or disabled |
| License | Cryptographically signed file that controls feature access and expiry |
| GPA | Grade Point Average for a single semester |
| CGPA | Cumulative GPA across all completed semesters |
| FYP | Final Year Project |
| RBAC | Role-Based Access Control |
| Degraded Mode | Read-only system state triggered when a license expires |
| Mandatory Module | A module that cannot be disabled (Authentication, Departments, SIS) |
| Audit Log | Immutable record of privileged actions performed in the system |

---

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.
