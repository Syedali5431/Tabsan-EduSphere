# Tabsan EduSphere — Complete Pages Catalog

> **Generated:** 2026-07-28  
> **Purpose:** Comprehensive catalog of every page in the EduSphere web application, detailing what each page contains and what it does.

---

## Table of Contents

- [Public Pages (Non-Authenticated)](#public-pages-non-authenticated)
- [Authentication & Security](#authentication--security)
- [Dashboard & System Configuration](#dashboard--system-configuration)
- [Academic Hierarchy Management](#academic-hierarchy-management)
- [Facilities Management](#facilities-management)
- [Timetable Management](#timetable-management)
- [Student Management](#student-management)
- [Course & Enrollment Management](#course--enrollment-management)
- [Assignments & Quizzes](#assignments--quizzes)
- [Attendance Management](#attendance-management)
- [Results & Grading](#results--grading)
- [Gradebook & Rubrics](#gradebook--rubrics)
- [Final Year Projects (FYP)](#final-year-projects-fyp)
- [LMS & Course Materials](#lms--course-materials)
- [Discussion Forum](#discussion-forum)
- [Announcements](#announcements)
- [Academic Calendar & Deadlines](#academic-calendar--deadlines)
- [Study Plans](#study-plans)
- [Degree Audit & Rules](#degree-audit--rules)
- [Graduation](#graduation)
- [Student Lifecycle & Bulk Promotion](#student-lifecycle--bulk-promotion)
- [Payments](#payments)
- [Reports Center](#reports-center)
- [Analytics](#analytics)
- [AI Chat](#ai-chat)
- [Helpdesk (Support Tickets)](#helpdesk-support-tickets)
- [Notifications](#notifications)
- [Search](#search)
- [User Settings & Profile](#user-settings--profile)
- [User Import](#user-import)
- [Admin Users Management](#admin-users-management)
- [Portal Configuration](#portal-configuration)
- [License Management](#license-management)
- [Theme Settings](#theme-settings)
- [Audit Logs](#audit-logs)
- [Accreditation Templates](#accreditation-templates)
- [Certificate Generation](#certificate-generation)
- [Institution Policy](#institution-policy)
- [Library Integration](#library-integration)
- [Prerequisites](#prerequisites)
- [Error Page](#error-page)

---

## Public Pages (Non-Authenticated)

### Login
**Route:** `/Login` / `/Portal/Login`  
**View:** `Login/Index.cshtml`

The login page is the entry point for all users. It presents a username/password form with validation. After successful authentication, users are redirected to their role-appropriate dashboard. The page also displays the institution's branding (retrieved from the API) and security profile settings such as password policy requirements. If a user is already authenticated, they are automatically redirected to the portal dashboard.

**Key Features:**
- Username and password authentication form
- Anti-forgery token protection
- Return URL support for deep-linking after login
- Institution branding display
- Automatic redirect if already logged in

---

### Privacy Policy
**Route:** `/Home/Privacy`  
**View:** `Home/Privacy.cshtml`

Displays the institution's privacy policy. Content is dynamically loaded from the portal branding settings via the API. This page is accessible to all users, including unauthenticated visitors. If the API is unavailable, a fallback generic privacy statement is shown.

**Key Features:**
- Dynamic privacy policy content from branding configuration
- Fallback content when API is unreachable
- Accessible without authentication

---

## Authentication & Security

### Force Change Password
**Route:** `/Portal/ForceChangePassword`  
**View:** `Portal/ForceChangePassword.cshtml`

A mandatory password change page shown to users whose accounts require a password reset (e.g., first-time login, admin-forced reset). Users must provide their current password and a new password that meets the security policy (12–16 characters, uppercase, lowercase, digit, and special symbol). Users cannot navigate away until the password is successfully changed.

**Key Features:**
- Current password verification
- New password with strict policy enforcement (12–16 chars, upper, lower, digit, symbol)
- Confirm password matching
- Blocks navigation until password is changed
- Success redirects to the dashboard

---

### Two-Factor Authentication Settings
**Route:** `/Portal/TwoFactorSettings`  
**View:** `Portal/TwoFactorSettings.cshtml`

Allows users to manage their Two-Factor Authentication (2FA) settings. Users can set up TOTP-based 2FA by scanning a QR code with an authenticator app (Google Authenticator, Microsoft Authenticator, etc.), verify the setup code, disable 2FA, re-enable 2FA with an existing secret, fully reset the 2FA setup, and test the login verification flow.

**Key Features:**
- QR code generation for authenticator app enrollment
- Manual entry key for setup
- Verify setup with TOTP code
- Enable/disable 2FA toggle
- Full reset of 2FA (wipes stored secret)
- Test login verification flow
- Status display showing 2FA state

---

## Dashboard & System Configuration

### Dashboard
**Route:** `/Portal/Dashboard`  
**View:** `Portal/Dashboard.cshtml`  
**Access:** SuperAdmin only (non-SuperAdmin redirected to Helpdesk)

The main system dashboard and API connection management hub. Displays workspace status, deployment fit (School/College/University), and the active theme. SuperAdmins configure the API base URL and connection settings here. The dashboard serves as the command center with quick links to Timetable Admin and Portal Search.

**Key Features:**
- API connection configuration
- Workspace status monitoring
- Deployment type display (School/College/University)
- Active theme indicator
- Quick-launch buttons for Timetable and Search
- Hero section with institutional branding

---

### Module Composition
**Route:** `/Portal/ModuleComposition`  
**View:** `Portal/ModuleComposition.cshtml`  
**Access:** Sidebar-guarded

Displays all dashboard composition modules, their widgets, and vocabulary labels. Shows which modules are active and provides SuperAdmin controls for toggling modules on/off globally for all users. Acts as the central module orchestration page.

**Key Features:**
- List of all composition modules with activation status
- Widget definitions per module
- Vocabulary/label management
- Global module activation toggle (SuperAdmin only)

---

### Portal Capability Matrix
**Route:** `/Portal/PortalCapabilityMatrix`  
**View:** `Portal/PortalCapabilityMatrix.cshtml`

A comprehensive grid view showing the full role/permission/capability matrix for all portal features. Displays which roles (SuperAdmin, Admin, Faculty, Student, Finance) have access to each capability, with the ability to modify assignments. This is the central access control visualization.

**Key Features:**
- Full role × capability matrix grid
- Role assignment for each capability
- Visual indicators for granted/denied permissions
- Capability grouping by functional area

---

## Academic Hierarchy Management

### Departments
**Route:** `/Portal/Departments`  
**View:** `Portal/Departments.cshtml`  
**Access:** Faculty, Admin, SuperAdmin

Manages academic departments within the institution. Lists all departments with their codes, institution types (School/College/University), and active status. Admins and SuperAdmins can create new departments, update names and institution types, activate/deactivate departments, and assign admin users to departments.

**Key Features:**
- Department list with name, code, institution type, active status
- Create department form (name, code, institution type)
- Edit department details
- Activate/deactivate toggle
- Admin user assignment to departments

---

### Programs
**Route:** `/Portal/Programs`  
**View:** `Portal/Programs.cshtml`  
**Access:** Faculty, Admin, SuperAdmin

Manages academic programs (degrees, diplomas, certificates) offered by departments. Lists programs filtered by department, tenant, and campus. Each program has a name and an auto-normalized semester count based on institution type. Supports creating, updating, activating, and deactivating programs.

**Key Features:**
- Program list with cascade filters (tenant → campus → department)
- Create program with institution-type-aware semester count
- Edit program name
- Activate/deactivate program
- Semester count auto-normalization per institution type

---

### Courses
**Route:** `/Portal/Courses`  
**View:** `Portal/Courses.cshtml`  
**Access:** Faculty, Admin, SuperAdmin

Manages courses (subjects) and course offerings. Lists courses by department, tenant, and campus with their associated offerings. Faculty/Admins can create courses with institution-type-aware defaults, create course offerings (course + semester + max enrollment + assigned faculty), deactivate courses, and delete offerings.

**Key Features:**
- Course list with cascade filters
- Create course form with institution-type defaults
- Create course offering (assigns course to semester with enrollment cap and faculty)
- Deactivate course
- Delete offering
- Course offering details display

---

### Prerequisites
**Route:** `/Portal/Prerequisites`  
**View:** `Portal/Prerequisites.cshtml`

Defines and manages course prerequisite chains. Allows administrators to set which courses must be completed before a student can enroll in an advanced course. Supports creating prerequisite relationships, viewing prerequisite chains, and managing prerequisite rules.

**Key Features:**
- Prerequisite chain management
- Define course prerequisites
- View prerequisite dependencies
- Support for multi-level prerequisite chains

---

## Facilities Management

### Buildings
**Route:** `/Portal/Buildings`  
**View:** `Portal/Buildings.cshtml`  
**Access:** Sidebar-guarded

Manages physical buildings across campuses. Lists buildings with their codes and active status, filtered by tenant and campus. Supports creating new buildings, updating names/codes, and activating/deactivating buildings.

**Key Features:**
- Building list with tenant/campus filtering
- Create building form (name, code, tenant, campus)
- Edit building name and code
- Activate/deactivate toggle

---

### Rooms
**Route:** `/Portal/Rooms`  
**View:** `Portal/Rooms.cshtml`  
**Access:** Sidebar-guarded

Manages rooms within buildings. Lists rooms filtered by building, showing room numbers and capacities. Supports creating new rooms, updating room numbers/capacities, and activating/deactivating rooms. Used for timetable scheduling and space management.

**Key Features:**
- Room list filtered by building
- Create room form (number, capacity, building)
- Edit room number and capacity
- Activate/deactivate toggle

---

## Timetable Management

### Timetable Admin
**Route:** `/Portal/TimetableAdmin`  
**View:** `Portal/TimetableAdmin.cshtml`  
**Access:** Sidebar-guarded

The main timetable management interface for administrators. Displays departments, courses, faculty, buildings, and rooms in a consolidated view. Admins can create timetables, add entries (day, time slot, room, faculty assignment), and publish timetables to make them visible to students and teachers.

**Key Features:**
- Consolidated view of departments, courses, faculty, buildings, rooms
- Create/edit timetables
- Add timetable entries (day, time, room, faculty)
- Publish/unpublish timetable

---

### Timetable Student
**Route:** `/Portal/TimetableStudent`  
**View:** `Portal/TimetableStudent.cshtml`  
**Access:** Sidebar-guarded

Student-facing timetable view showing their weekly class schedule. Displays courses organized by day of the week with time slots, room numbers, and faculty names. Includes day-of-week filtering for easy navigation. Admins can activate or deactivate student timetables.

**Key Features:**
- Weekly schedule grid by day
- Day-of-week filtering
- Course, room, and faculty information per time slot
- Admin controls for activation/deactivation

---

### Timetable Teacher
**Route:** `/Portal/TimetableTeacher`  
**View:** `Portal/TimetableTeacher.cshtml`  
**Access:** Sidebar-guarded

Teacher-facing timetable view showing their teaching schedule. Displays classes assigned to a faculty member across the week. Includes faculty selection for administrators to view any teacher's schedule. Supports activation and deactivation of teacher timetables.

**Key Features:**
- Teacher schedule grid
- Faculty selection dropdown (for admins viewing others)
- Course and room details per time slot
- Admin controls for activation/deactivation

---

## Student Management

### Students
**Route:** `/Portal/Students`  
**View:** `Portal/Students.cshtml`  
**Access:** Sidebar-guarded

The student directory page. Lists all students filtered by department, tenant, campus, and institution type. Displays student profiles with key information. Provides a searchable, filterable view of the student body.

**Key Features:**
- Student list with cascade filters
- Department/tenant/campus/institution type filtering
- Student profile cards/list view
- Search functionality

---

### Enrollments
**Route:** `/Portal/Enrollments`  
**View:** `Portal/Enrollments.cshtml`  
**Access:** Faculty, Admin, SuperAdmin

Manages student enrollments in course offerings. Students can view their own enrollments, while admins and faculty see the full roster. Supports admin enrollment of students into offerings, student self-enrollment, admin dropping of enrollments, student dropping of own enrollments, and enrollment activation/deactivation by scope.

**Key Features:**
- Enrollment roster with cascade filters
- Admin: enroll student into offering
- Student: self-enroll in available offerings
- Admin: drop student enrollment
- Student: drop own enrollment
- Scope-level enrollment activation toggle

---

## Course & Enrollment Management

### Section
**Route:** `/Portal/Section`  
**View:** `Portal/Section.cshtml`

A generic scaffolded section page used as a placeholder for features under development. Displays a title and description set by the controller, with an info message indicating the page is ready for feature implementation.

**Key Features:**
- Scaffolded placeholder page
- Dynamic title and description
- Ready-for-implementation indicator

---

## Assignments & Quizzes

### Assignments
**Route:** `/Portal/Assignments`  
**View:** `Portal/Assignments.cshtml`  
**Access:** Sidebar-guarded

The assignments management page. Shows different views for students vs. faculty/admin. Students see their assignments with submission status, due dates, and the ability to submit work (file upload or text content). Faculty/admins can create assignments for course offerings, update details, publish to students, set active status, and grade student submissions.

**Key Features:**
- Student view: assignment list with due dates and submission status
- Faculty view: manage assignments by offering
- Create assignment (title, description, due date, offering)
- Update assignment details
- Publish assignment to students
- Activate/deactivate assignment
- Student submission (file upload or text)
- Faculty grading of submissions

---

### Quizzes
**Route:** `/Portal/Quizzes`  
**View:** `Portal/Quizzes.cshtml`  
**Access:** Faculty, Admin, SuperAdmin (no students)

Faculty/admin view for managing quizzes. Displays all quizzes for a course offering. Faculty can create quizzes, update details, publish quizzes to students, activate/deactivate, and delete quizzes. Students are redirected to the View Quizzes page instead.

**Key Features:**
- Quiz list by course offering
- Create quiz (title, questions, due date)
- Update quiz details
- Publish quiz to students
- Activate/deactivate quiz
- Delete quiz

---

### View Quizzes
**Route:** `/Portal/ViewQuizzes`  
**View:** `Portal/ViewQuizzes.cshtml`  
**Access:** Student only

Student-facing quiz page. Shows available quizzes with attempt status. Students can view quiz details and submit quiz attempts with file uploads. Displays due dates, submission status, and grades for completed quizzes.

**Key Features:**
- Available quizzes list
- Quiz attempt status (not started, submitted, graded)
- Submit quiz attempt with file upload
- View quiz details and due dates
- Graded quiz results

---

## Attendance Management

### Attendance
**Route:** `/Portal/Attendance`  
**View:** `Portal/Attendance.cshtml`  
**Access:** Sidebar-guarded

View-only attendance records page. Shows attendance data by course offering with summary statistics (present/absent/late counts). Students see their own attendance; faculty and admins see class-wide records with filtering options.

**Key Features:**
- Attendance records by offering
- Summary statistics (present, absent, late)
- Student view: own attendance
- Faculty/admin view: class-wide with filters

---

### Enter Attendance
**Route:** `/Portal/EnterAttendance`  
**View:** `Portal/EnterAttendance.cshtml`  
**Access:** Sidebar-guarded (write UI)

The attendance entry interface for faculty. Allows marking attendance for students in bulk (same date or per-row dates), CSV import for bulk attendance entry, downloading CSV templates, and correcting individual attendance records with remarks.

**Key Features:**
- Bulk mark attendance (same date or per-row)
- CSV template download for bulk entry
- CSV import with strict validation
- Import report download
- Individual attendance correction with remarks

---

## Results & Grading

### Results
**Route:** `/Portal/Results`  
**View:** `Portal/Results.cshtml`  
**Access:** Sidebar-guarded

View-only results page. Shows student results filtered by department, course, semester, and exam type. Students see their own results; faculty and admins see all results with comprehensive filtering. Supports viewing published and unpublished results based on role.

**Key Features:**
- Results list with cascade filters
- Department/course/semester/exam type filtering
- Student view: own results only
- Faculty/admin view: all results with filters

---

### Enter Results
**Route:** `/Portal/EnterResults`  
**View:** `Portal/EnterResults.cshtml`  
**Access:** Sidebar-guarded (write UI, no students)

The results entry interface for faculty and admins. Allows creating individual student results with optional promotion, CSV import for bulk result entry, downloading CSV templates with roster pre-population, correcting results with mandatory audit reason, publishing all results for an offering, and managing result modification requests.

**Key Features:**
- Create individual result with optional promotion
- CSV template download with roster pre-population
- CSV import with strict validation
- Import report download
- Correct result with audit reason
- Publish all results for an offering (Admin/SuperAdmin)
- Request result modification (faculty → admin workflow)
- Approve/reject modification requests (Admin/SuperAdmin)
- Student result re-check request

---

### Grading Config
**Route:** `/Portal/GradingConfig`  
**View:** `Portal/GradingConfig.cshtml`  
**Access:** Faculty, Admin, SuperAdmin

Configures grading parameters per course and at the institution level. Supports setting pass thresholds, grading types (letter grades, GPA scale, percentage), and grade ranges. SuperAdmins can save institution-level grading profiles for School, College, and University types.

**Key Features:**
- Course-level grading configuration
- Pass threshold setting
- Grading type selection (letter, GPA, percentage)
- Grade range definitions
- Institution-level grading profiles (SuperAdmin only)

---

### Result Calculation
**Route:** `/Portal/ResultCalculation`  
**View:** `Portal/ResultCalculation.cshtml`  
**Access:** Faculty, Admin, SuperAdmin

Defines GPA calculation rules, component weightage, and pass thresholds per institution type. Controls how final grades are computed from individual assessment components (assignments, quizzes, exams). Course filter data is dynamically loaded based on semester configuration.

**Key Features:**
- GPA rules per institution type
- Component weightage configuration
- Pass threshold definitions
- Dynamic course filtering by semester flag

---

## Gradebook & Rubrics

### Gradebook
**Route:** `/Portal/Gradebook`  
**View:** `Portal/Gradebook.cshtml`  
**Access:** Faculty, Admin, SuperAdmin

A comprehensive grid view for faculty to see all students' grades across assessment components for a selected course offering. Displays a matrix of student vs. assessment component with scores, providing a bird's-eye view of class performance.

**Key Features:**
- Student × component grade matrix
- Course offering selector with cascade filters
- Aggregated scores per component
- Overall grade summary

---

### Rubric Manage
**Route:** `/Portal/RubricManage`  
**View:** `Portal/RubricManage.cshtml`  
**Access:** Faculty, Admin, SuperAdmin

Allows faculty to create and manage grading rubrics for course offerings. Rubrics define assessment criteria, point allocations, and grading scales for assignments and other evaluative components. Select a course offering to view and edit its associated rubrics.

**Key Features:**
- Rubric list by course offering
- Create rubric with criteria and point allocations
- Edit rubric criteria
- Define grading scales per criterion

---

### Rubric View
**Route:** `/Portal/RubricView`  
**View:** `Portal/RubricView.cshtml`

Student-facing rubric viewer. Displays the grading rubric for a specific assignment or course component so students understand how their work will be evaluated.

**Key Features:**
- View grading rubric details
- Criteria breakdown
- Point allocations per criterion
- Read-only student view

---

## Final Year Projects (FYP)

### FYP
**Route:** `/Portal/Fyp`  
**View:** `Portal/Fyp.cshtml`  
**Access:** Sidebar-guarded

The Final Year Project management hub. Role-based views show different content: students see their FYP status, faculty see supervised projects, and admins see all projects. Supports the full FYP lifecycle: proposal, creation, updates, approval/rejection, supervisor assignment, completion, result entry, and completion request workflows.

**Key Features:**
- Role-based project views (student/faculty/admin)
- Propose FYP project
- Create FYP project for student
- Update project title/description
- Approve/reject project
- Assign faculty supervisor
- Mark project as complete
- Enter FYP result (grade/marks)
- Student completion request workflow
- Faculty completion approval

---

## LMS & Course Materials

### Course LMS (Student View)
**Route:** `/Portal/CourseLms`  
**View:** `Portal/CourseLms.cshtml`

Student-facing Learning Management System view for a specific course. Displays course content organized by week in an accordion layout. Each week module shows its title, description, and associated learning materials. Shows a message when no content has been published yet.

**Key Features:**
- Week-by-week course content display
- Accordion layout for module navigation
- Module title and description per week
- Empty state when no content published
- Connected to course material repository

---

### Course Material (Faculty/Admin View)
**Route:** `/Portal/CourseMaterial`  
**View:** `Portal/CourseMaterial.cshtml`

Faculty and admin interface for managing course materials. Upload and organize learning resources (documents, videos, links) by course offering and week. Supports file uploads, content organization, and publishing materials to the student LMS view.

**Key Features:**
- Upload course materials (documents, videos, links)
- Organize by course offering and week number
- Material metadata (title, type, description)
- Publish/unpublish materials to student view

---

### Course Material Student
**Route:** `/Portal/CourseMaterialStudent`  
**View:** `Portal/CourseMaterialStudent.cshtml`

Alternate student-facing view for course materials. Provides access to learning resources organized by course offering and week, similar to the LMS view but potentially with a different layout or access path.

**Key Features:**
- Student access to course materials
- Organized by week and course
- Download/view learning resources

---

### LMS Manage
**Route:** `/Portal/LmsManage`  
**View:** `Portal/LmsManage.cshtml`

Administrative interface for managing the LMS module configuration. Controls which courses have LMS features enabled, manages content publishing workflows, and configures LMS module settings.

**Key Features:**
- LMS module configuration per course
- Content publishing management
- LMS feature toggles

---

## Discussion Forum

### Discussion
**Route:** `/Portal/Discussion`  
**View:** `Portal/Discussion.cshtml`

The main discussion forum listing page. Displays discussion threads organized by course offering. Provides a course offering selector to filter threads. Users can create new discussion threads, view existing threads, and participate in academic discussions related to their courses.

**Key Features:**
- Discussion thread listing
- Course offering filter
- Create new discussion thread
- Thread metadata (author, date, replies count)

---

### Discussion Thread
**Route:** `/Portal/DiscussionThread`  
**View:** `Portal/DiscussionThread.cshtml`

Lists all discussion threads, possibly across multiple offerings or in a consolidated view. Provides an overview of active discussions with filtering and search capabilities.

**Key Features:**
- Consolidated thread list
- Multi-offering view
- Thread search and filter

---

### Discussion Thread Detail
**Route:** `/Portal/DiscussionThreadDetail`  
**View:** `Portal/DiscussionThreadDetail.cshtml`

Detailed view of a single discussion thread showing all replies in a threaded or chronological format. Users can post replies, view the full conversation, and see participant information.

**Key Features:**
- Full thread conversation view
- Post reply functionality
- Participant information
- Thread metadata (created date, reply count)

---

## Announcements

### Announcements
**Route:** `/Portal/Announcements`  
**View:** `Portal/Announcements.cshtml`

Course announcements page. Displays announcements posted by faculty for specific course offerings. Students see announcements relevant to their enrolled courses. Faculty and admins can create, edit, and publish announcements with cascade filters for targeting specific departments, courses, and offerings.

**Key Features:**
- Announcement list with cascade filters
- Create announcement (title, content, target offering)
- Edit announcement
- Publish/unpublish announcements
- Role-based visibility (student sees enrolled courses only)

---

## Academic Calendar & Deadlines

### Academic Calendar
**Route:** `/Portal/AcademicCalendar`  
**View:** `Portal/AcademicCalendar.cshtml`

Displays the academic calendar with important dates organized by semester. Shows events such as semester start/end dates, exam periods, holidays, and registration deadlines. Includes semester filtering and the ability for admins to add calendar events.

**Key Features:**
- Calendar view by semester
- Event list with dates and descriptions
- Semester filter dropdown
- Admin: add/edit calendar events
- Event type categorization

---

### Academic Deadlines
**Route:** `/Portal/AcademicDeadlines`  
**View:** `Portal/AcademicDeadlines.cshtml`

Tracks and displays critical academic deadlines such as assignment due dates, exam dates, registration deadlines, and fee payment deadlines. Provides a consolidated view of all upcoming and past deadlines with filtering options.

**Key Features:**
- Deadline list with date tracking
- Filter by type (assignment, exam, registration, fee)
- Upcoming/past deadline toggle
- Admin: create and manage deadlines

---

## Study Plans

### Study Plans
**Route:** `/Portal/StudyPlan`  
**View:** `Portal/StudyPlan.cshtml`

Lists all study plans for students. Study plans define the recommended course sequence across semesters. Faculty advisors can create study plans for students, and students can view their assigned plans. Plans have statuses such as Draft, Submitted, and Approved with advisor tracking.

**Key Features:**
- Study plan list with filters
- Plan status tracking (Draft, Submitted, Approved)
- Advisor assignment display
- Create new study plan
- View plan details

---

### Study Plan Detail
**Route:** `/Portal/StudyPlanDetail`  
**View:** `Portal/StudyPlanDetail.cshtml`

Detailed view of a single study plan showing all courses organized by semester. Displays the course sequence, credit hours, and prerequisites. Supports editing the plan (adding/removing courses), changing the plan status, and advisor notes.

**Key Features:**
- Semester-by-semester course breakdown
- Course details (credits, prerequisites)
- Add/remove courses from plan
- Plan status management
- Advisor notes and comments

---

### Study Plan Recommendations
**Route:** `/Portal/StudyPlanRecommendations`  
**View:** `Portal/StudyPlanRecommendations.cshtml`

Provides AI or rule-based course recommendations based on a student's study plan progress, completed prerequisites, academic performance, and degree requirements. Helps students and advisors make informed decisions about course selection.

**Key Features:**
- Course recommendations based on plan progress
- Prerequisite satisfaction checking
- Degree requirement alignment
- Performance-based suggestions

---

## Degree Audit & Rules

### Degree Audit
**Route:** `/Portal/DegreeAudit`  
**View:** `Portal/DegreeAudit.cshtml`

A comprehensive degree progress tracking page. Shows a credit breakdown by category (core, elective, general), completed vs. remaining requirements, and overall degree eligibility status. Staff can select any student to view their audit; students see their own. Displays progress bars and detailed requirement checklists.

**Key Features:**
- Credit breakdown by category
- Completed vs. remaining requirements
- Eligibility status indicator
- Staff: student selector for viewing any audit
- Progress visualization (progress bars)
- Detailed requirement checklists

---

### Degree Rules
**Route:** `/Portal/DegreeRules`  
**View:** `Portal/DegreeRules.cshtml`

Defines the rules that govern degree requirements. Administrators configure credit hour requirements per category, mandatory course lists, elective pools, minimum GPA thresholds, and other degree completion criteria. These rules feed into the Degree Audit system.

**Key Features:**
- Credit hour requirement configuration
- Mandatory course definitions
- Elective pool management
- Minimum GPA thresholds
- Degree rule templates per program

---

## Graduation

### Graduation Eligibility
**Route:** `/Portal/GraduationEligibility`  
**View:** `Portal/GraduationEligibility.cshtml`

Checks and displays a student's eligibility for graduation based on completed credits, GPA requirements, and degree rules. Shows a checklist of requirements with pass/fail status for each criterion. Students can check their own eligibility; staff can check any student.

**Key Features:**
- Graduation requirement checklist
- Credit completion status
- GPA threshold verification
- Eligibility determination (eligible/not eligible)
- Staff: student selector

---

### Graduation Apply
**Route:** `/Portal/GraduationApply`  
**View:** `Portal/GraduationApply.cshtml`

The graduation application form for eligible students. Students submit their formal application for graduation, which initiates the approval workflow. Captures application details, expected graduation term, and any special notes.

**Key Features:**
- Graduation application form
- Eligibility pre-check
- Application status tracking
- Submission to approval workflow

---

### Graduation Applications
**Route:** `/Portal/GraduationApplications`  
**View:** `Portal/GraduationApplications.cshtml`  
**Access:** Staff (Admin, SuperAdmin)

Staff-facing view listing all graduation applications with their current status. Supports filtering by status (Draft, PendingFaculty, PendingAdmin, PendingFinalApproval, Approved, Rejected). Staff can review applications and advance them through the approval workflow.

**Key Features:**
- Application list with status filter
- Multi-stage approval workflow tracking
- Department and program filters
- Bulk status view

---

### Graduation Application Detail
**Route:** `/Portal/GraduationApplicationDetail`  
**View:** `Portal/GraduationApplicationDetail.cshtml`

Detailed view of a single graduation application showing the student's academic record, eligibility checklist, application details, and approval history. Staff can approve or reject applications at each stage of the workflow.

**Key Features:**
- Full application details
- Student academic record summary
- Eligibility checklist
- Approval history and workflow actions
- Approve/reject at current stage

---

## Student Lifecycle & Bulk Promotion

### Student Lifecycle
**Route:** `/Portal/StudentLifecycle`  
**View:** `Portal/StudentLifecycle.cshtml`  
**Access:** Faculty, Admin, SuperAdmin

Manages student academic progression. Shows students organized by academic level/semester. Supports promoting individual students to the next academic level, graduating students (university only), and viewing promotion-eligible and graduation-eligible candidates. Displays special messaging for school completion (e.g., class 10 completion).

**Key Features:**
- Students grouped by academic level/semester
- Individual student promotion
- Student graduation (university only)
- Promotion-eligible candidate lists
- Graduation-eligible candidate lists
- Special school completion messaging

---

### Bulk Promotion
**Route:** Handled via API (`/Portal/BulkPromotion` redirects to `StudentLifecycle`)  

Bulk promotion functionality for advancing multiple students at once. Processes batch promotions based on academic performance criteria (e.g., all students who passed all courses). Handled primarily through the Student Lifecycle page and API endpoints.

**Key Features:**
- Batch student promotion
- Criteria-based promotion rules
- Promotion report generation

---

## Payments

### Payments
**Route:** `/Portal/Payments`  
**View:** `Portal/Payments.cshtml`  
**Access:** Finance, Admin, SuperAdmin (students see own receipts only)

Payment receipt management page. Finance staff and admins can view all payment receipts, create new payment receipts, update receipt details (amount, receipt number, description), confirm payments as received, cancel payments, and import payments via CSV. Students can only view their own payment receipts and submit proof of payment notes.

**Key Features:**
- Payment receipt list (scoped by role)
- Create payment receipt
- Update payment details
- Confirm payment as received
- Cancel payment
- CSV import for bulk payment entry
- CSV template download
- Student: submit proof of payment note
- Student: view own receipts only

---

## Reports Center

### Report Center
**Route:** `/Portal/ReportCenter`  
**View:** `Portal/ReportCenter.cshtml`  
**Access:** No students

Central hub for all reports. Displays available reports filtered by user role. Each report card provides a description and links to the full report view. Admins can activate/deactivate reports for their scope. Reports cover attendance, results, assignments, quizzes, GPA, enrollment, semester results, transcripts, degree certificates, low attendance warnings, FYP status, and payments.

**Key Features:**
- Report catalog with role-based filtering
- Report cards with descriptions
- Scope-level report activation (Admin/SuperAdmin)
- Links to all individual report pages

---

### Report Attendance
**Route:** `/Portal/ReportAttendance`  
**View:** `Portal/ReportAttendance.cshtml`  
**Access:** No students

Attendance summary report with filtering by department, semester, course offering, and individual student. Shows attendance statistics with present/absent/late counts and percentages. Supports export to Excel, CSV, and PDF formats.

**Key Features:**
- Attendance summary with multi-field filters
- Present/absent/late statistics
- Export: Excel (.xlsx)
- Export: CSV
- Export: PDF

---

### Report Results
**Route:** `/Portal/ReportResults`  
**View:** `Portal/ReportResults.cshtml`  
**Access:** No students

Results summary report with filtering by department, semester, and course offering. Displays aggregated result statistics including pass/fail rates, grade distributions, and individual student results. Exportable to Excel, CSV, and PDF.

**Key Features:**
- Result summary with filters
- Pass/fail statistics
- Grade distribution
- Export: Excel, CSV, PDF

---

### Report Assignments
**Route:** `/Portal/ReportAssignments`  
**View:** `Portal/ReportAssignments.cshtml`  
**Access:** No students

Assignment summary report showing submission rates, grading status, and score distributions across course offerings. Filters by department and offering. Exportable to Excel, CSV, and PDF.

**Key Features:**
- Assignment submission statistics
- Grading completion rates
- Score distributions
- Export: Excel, CSV, PDF

---

### Report Quizzes
**Route:** `/Portal/ReportQuizzes`  
**View:** `Portal/ReportQuizzes.cshtml`  
**Access:** No students

Quiz summary report showing quiz participation rates, average scores, and performance distributions. Exportable to Excel, CSV, and PDF.

**Key Features:**
- Quiz participation statistics
- Average scores per quiz
- Performance distribution
- Export: Excel, CSV, PDF

---

### Report GPA
**Route:** `/Portal/ReportGpa`  
**View:** `Portal/ReportGpa.cshtml`  
**Access:** No students

GPA and CGPA report organized by department and program. Shows individual student GPAs, semester GPAs, and cumulative GPAs. Includes grade point distributions and academic standing classifications. Exportable to Excel, CSV, and PDF.

**Key Features:**
- GPA/CGPA by department and program
- Student GPA listing
- Semester-wise GPA tracking
- Academic standing classification
- Export: Excel, CSV, PDF

---

### Report Enrollment
**Route:** `/Portal/ReportEnrollment`  
**View:** `Portal/ReportEnrollment.cshtml`  
**Access:** No students

Enrollment summary report showing enrollment counts by course offering, department, and semester. Displays capacity utilization and enrollment trends. Exportable to Excel, CSV, and PDF.

**Key Features:**
- Enrollment counts by offering
- Capacity utilization rates
- Department and semester filters
- Export: Excel, CSV, PDF

---

### Report Semester Results
**Route:** `/Portal/ReportSemesterResults`  
**View:** `Portal/ReportSemesterResults.cshtml`  
**Access:** No students

Comprehensive semester-wise results report. Shows all results for a selected semester organized by course and student. Includes aggregate statistics like semester GPA averages and pass rates. Exportable to Excel, CSV, and PDF.

**Key Features:**
- Semester results overview
- Course-wise breakdown
- Semester GPA statistics
- Export: Excel, CSV, PDF

---

### Report Transcript
**Route:** `/Portal/ReportTranscript`  
**View:** `Portal/ReportTranscript.cshtml`  
**Access:** No students

Official student transcript report. Shows a student's complete academic record including all courses taken, grades received, credits earned, and cumulative GPA. Can be generated for any student. Exportable to Excel, CSV, and PDF (official transcript format).

**Key Features:**
- Complete academic record per student
- All courses, grades, credits
- Cumulative GPA calculation
- Student selector
- Export: Excel, CSV, PDF

---

### Report Degree Certificate
**Route:** `/Portal/ReportDegreeCertificate`  
**View:** `Portal/ReportDegreeCertificate.cshtml`  
**Access:** No students

Degree certificate report for graduated students only. Shows the official degree details including degree name, conferral date, and program information. Exportable to PDF format for official certificate generation.

**Key Features:**
- Degree certificate for graduated students
- Degree name and conferral date
- Program information
- Export: PDF only

---

### Report Low Attendance
**Route:** `/Portal/ReportLowAttendance`  
**View:** `Portal/ReportLowAttendance.cshtml`  
**Access:** No students

Low attendance warning report flagging students with attendance below the 75% threshold. Helps identify at-risk students for intervention. Filterable by department and offering. Exportable to Excel, CSV, and PDF.

**Key Features:**
- Students below 75% attendance threshold
- At-risk student identification
- Department and offering filters
- Export: Excel, CSV, PDF

---

### Report FYP Status
**Route:** `/Portal/ReportFypStatus`  
**View:** `Portal/ReportFypStatus.cshtml`  
**Access:** No students

Final Year Project status report showing all FYP projects by department and status (proposed, in-progress, completed, approved). Tracks project progress, supervisor assignments, and completion rates. Exportable to Excel, CSV, and PDF.

**Key Features:**
- FYP project list by status
- Department filter
- Supervisor assignment tracking
- Progress status per project
- Export: Excel, CSV, PDF

---

### Report Payments
**Route:** `/Portal/ReportPayments`  
**View:** `Portal/ReportPayments.cshtml`  
**Access:** Admin, SuperAdmin, Finance only

Payment summary report with filters by year, month, semester, and department. Shows payment totals, status breakdowns (confirmed, pending, cancelled), and receipt summaries. Exportable to Excel, CSV, and PDF.

**Key Features:**
- Payment totals by year/month
- Status breakdown (confirmed, pending, cancelled)
- Department and semester filters
- Receipt summaries
- Export: Excel, CSV, PDF

---

### Report Settings
**Route:** `/Portal/ReportSettings`  
**View:** `Portal/ReportSettings.cshtml`  
**Access:** Sidebar-guarded

Configuration page for report definitions. Lists all available reports with their activation status. Admins can create new report definitions, toggle reports on/off, and configure which roles (Admin, Faculty, Student) can access each report.

**Key Features:**
- Report definitions list
- Create new report definition
- Activate/deactivate reports
- Role-based access configuration per report
- Report metadata management

---

## Analytics

### Analytics
**Route:** `/Portal/Analytics`  
**View:** `Portal/Analytics.cshtml`  
**Access:** Sidebar-guarded

The analytics dashboard providing visual insights into institutional data. Displays summary cards, performance trends, attendance analytics, assignment submission analytics, and payment analytics. Includes charts and data visualizations. Admins can activate/deactivate analytics for tenant/campus scopes. Data is also available as JSON for AJAX consumption.

**Key Features:**
- Summary statistics cards
- Performance trend charts
- Attendance analytics visualization
- Assignment submission analytics
- Payment analytics
- JSON endpoint for AJAX/SPA (`/Portal/AnalyticsSnapshot`)
- Scope-level activation toggle
- Interactive data visualizations

---

## AI Chat

### AI Chat
**Route:** `/Portal/AiChat`  
**View:** `Portal/AiChat.cshtml`  
**Access:** Sidebar-guarded

A full-page AI-powered assistant chat interface. Users can ask questions, get help with academic tasks, and interact with the EduSphere AI assistant. The chat supports conversation history, streaming responses, and context-aware answers. Also available as a floating sidebar widget (`ChatPanel.cshtml`, `FloatingChatButton.cshtml`).

**Key Features:**
- Full-page chat interface
- AI-powered academic assistant
- Conversation history
- Message sending and response display
- Sidebar widget version (collapsible chat panel)
- Floating chat button for quick access
- Widget state management (conversations, messages)

---

## Helpdesk (Support Tickets)

### Helpdesk
**Route:** `/Portal/Helpdesk`  
**View:** `Portal/Helpdesk.cshtml`

The support ticket listing page. Displays all helpdesk tickets with status filtering (Open, InProgress, Resolved, Closed). Staff (Admin/SuperAdmin) see all tickets; regular users see only their own. Includes ticket status badges, pagination, and links to ticket details.

**Key Features:**
- Ticket list with status filter tabs
- Staff: view all tickets
- Users: view own tickets only
- Pagination
- Status badge indicators
- Create ticket link

---

### Helpdesk Create
**Route:** `/Portal/HelpdeskCreate`  
**View:** `Portal/HelpdeskCreate.cshtml`

Ticket creation form. Users can submit new support tickets with a subject, description, category, and priority level. Supports attaching files or screenshots. Submitted tickets enter the Open status and are visible in the Helpdesk list.

**Key Features:**
- Ticket creation form
- Subject, description, category, priority fields
- File attachment support
- Automatic status assignment (Open)

---

### Helpdesk Detail
**Route:** `/Portal/HelpdeskDetail`  
**View:** `Portal/HelpdeskDetail.cshtml`

Detailed view of a single support ticket showing the full conversation thread. Displays original ticket details, all replies, status changes, and assignee information. Staff can reply, change status, and assign tickets. Users can add replies and close their own tickets.

**Key Features:**
- Full ticket conversation thread
- Ticket metadata (status, priority, category, dates)
- Reply functionality
- Staff: status changes and assignment
- User: reply and close own tickets

---

## Notifications

### Notifications
**Route:** `/Portal/Notifications`  
**View:** `Portal/Notifications.cshtml`  
**Access:** Sidebar-guarded

Displays all user notifications with read/unread status. Shows notification type, message, timestamp, and related entity links. Supports marking individual notifications as read and a "Mark All Read" bulk action. Unread count is displayed in the navigation bar.

**Key Features:**
- Notification list with read/unread status
- Notification type and message display
- Timestamp for each notification
- Mark individual notification as read
- Mark all as read bulk action
- Unread count badge in navbar

---

## Search

### Search
**Route:** `/Portal/Search`  
**View:** `Portal/Search.cshtml`

Global search page for finding entities across the system. Searches across Students, Courses, Course Offerings, Faculty, and Departments. Requires a minimum of 2 characters. Results are displayed in categorized groups with relevant details for each entity type.

**Key Features:**
- Global search across 5 entity types
- Minimum 2-character search query
- Categorized results (Students, Courses, Offerings, Faculty, Departments)
- Search-as-you-type or submit-based search
- Result cards with entity-specific details

---

## User Settings & Profile

### User Settings
**Route:** `/Portal/UserSettings`  
**View:** `Portal/UserSettings.cshtml`

Personal user settings page. Users can update their profile information, change their password (current/new/confirm with policy validation), and manage their account preferences. Displays a hero card with user information and panels for each setting category.

**Key Features:**
- Profile information display and edit
- Change password (current, new, confirm)
- Password policy enforcement
- Account preferences management
- Hero card with user summary
- Panel-based settings layout

---

### Dashboard Settings
**Route:** `/Portal/DashboardSettings`  
**View:** `Portal/DashboardSettings.cshtml`

Allows users to customize their personal dashboard view. Configure which widgets and modules appear on the dashboard, arrange layout preferences, and set personal display options.

**Key Features:**
- Dashboard widget customization
- Layout preferences
- Personal display settings
- Widget visibility toggles

---

## User Import

### User Import
**Route:** `/Portal/UserImport`  
**View:** `Portal/UserImport.cshtml`  
**Access:** Admin, SuperAdmin

Bulk user import page. Supports uploading CSV files for batch user creation, downloading CSV templates (faculty-admin-import and students-import), creating single users via a form (with optional profile photo), and generating sample CSV templates dynamically. Shows department and course lists for data mapping.

**Key Features:**
- CSV file upload for bulk import
- Static CSV template download (faculty-admin-import, students-import)
- Dynamic sample CSV generation
- Single user creation form with profile photo upload
- Department and course reference lists
- Import progress and error reporting

---

## Admin Users Management

### Admin Users
**Route:** `/Portal/AdminUsers`  
**View:** `Portal/AdminUsers.cshtml`  
**Access:** SuperAdmin only

Manages administrative user accounts. Lists all admin users with their roles, active status, and department assignments. SuperAdmins can create new admin users with department assignments, update admin user details (email, active status, password), and manage department assignments via add/remove diffs.

**Key Features:**
- Admin user list with role and status
- Create admin user with department assignments
- Update admin email, active status, password
- Department assignment management (add/remove diff)
- Role display

---

## Portal Configuration

### Module Settings
**Route:** `/Portal/ModuleSettings`  
**View:** `Portal/ModuleSettings.cshtml`  
**Access:** SuperAdmin only

Module-level configuration page. Shows all system modules with their activation status and role assignments. SuperAdmins can toggle modules on/off and configure which roles (Admin, Faculty, Student) have access to each module.

**Key Features:**
- Module list with activation status
- Module toggle (on/off)
- Role assignment per module (Admin, Faculty, Student)
- Global module configuration

---

### Sidebar Settings
**Route:** `/Portal/SidebarSettings`  
**View:** `Portal/SidebarSettings.cshtml`  
**Access:** Sidebar-guarded

Manages the sidebar navigation menu configuration. Lists all sidebar menus with their current status and role visibility settings. Admins can update which roles see each menu item and toggle menu items active/inactive.

**Key Features:**
- Sidebar menu list with status indicators
- Role visibility controls (Admin, Faculty, Student)
- Menu activation/deactivation toggle
- Menu ordering and hierarchy display

---

### Tenant Management
**Route:** `/Portal/TenantManagement`  
**View:** `Portal/TenantManagement.cshtml`  
**Access:** SuperAdmin only

Top-level organizational unit management. Lists all tenants (institutions/organizations) with their active status. SuperAdmins can create new tenants, update tenant names, and activate/deactivate tenants. Tenants are the highest level of the organizational hierarchy.

**Key Features:**
- Tenant list with active status
- Create tenant
- Update tenant name
- Activate/deactivate tenant

---

### Campus Management
**Route:** `/Portal/CampusManagement`  
**View:** `Portal/CampusManagement.cshtml`  
**Access:** SuperAdmin only

Manages campuses within tenants. Lists campuses for a selected tenant. SuperAdmins can create new campuses under a tenant, update campus names, and activate/deactivate campuses. Campuses sit below tenants in the organizational hierarchy.

**Key Features:**
- Campus list filtered by tenant
- Create campus under a tenant
- Update campus name
- Activate/deactivate campus

---

## License Management

### License Update
**Route:** `/Portal/LicenseUpdate`  
**View:** `/Portal/LicenseUpdate.cshtml`  
**Access:** Sidebar-guarded

Displays current license information including license type, status, effective dates, and remaining days. Supports uploading new `.tablic` license files to update or extend the system license. Shows license expiry warnings when approaching the end date.

**Key Features:**
- License status display (type, dates, remaining days)
- License upload (.tablic file format)
- Expiry warnings
- License details summary

---

## Theme Settings

### Theme Settings
**Route:** `/Portal/ThemeSettings`  
**View:** `/Portal/ThemeSettings.cshtml`  
**Access:** Sidebar-guarded

Theme customization page. Shows the currently active theme and provides a theme switcher interface. Users can preview and select from available themes to change the visual appearance of the portal.

**Key Features:**
- Current theme display
- Theme switcher with preview
- Available themes list
- Apply theme by key

---

## Audit Logs

### Audit Logs
**Route:** `/Portal/AuditLogs`  
**View:** `/Portal/AuditLogs.cshtml`  
**Access:** Sidebar-guarded

Comprehensive audit trail viewer. Displays system audit logs with multi-field filtering (user, action, entity, date range). Supports pagination through large log datasets. Exportable to CSV, Excel (.xlsx), and PDF formats for compliance and review purposes.

**Key Features:**
- Audit log list with multi-field search/filter
- Pagination
- Export to CSV
- Export to Excel (.xlsx)
- Export to PDF
- Date range filtering
- User, action, entity filters

---

## Accreditation Templates

### Accreditation Templates
**Route:** `/Portal/AccreditationTemplates`  
**View:** `Portal/AccreditationTemplates.cshtml`

Manages accreditation templates for institutional and program-level accreditation processes. Define criteria, evidence requirements, and evaluation frameworks used for accreditation submissions and reviews.

**Key Features:**
- Accreditation template list
- Create/edit templates
- Criteria and evidence requirement definitions
- Evaluation framework configuration

---

## Certificate Generation

### Generate Certificates
**Route:** `/Portal/GenerateCertificates`  
**View:** `Portal/GenerateCertificates.cshtml`

Certificate generation interface. Allows administrators to generate official certificates (degree certificates, transcripts, completion certificates) for students. Supports template-based certificate generation with institution branding, bulk generation, and download options.

**Key Features:**
- Certificate template selection
- Student selection for certificate generation
- Bulk certificate generation
- Institution branding on certificates
- Certificate download

---

## Institution Policy

### Institution Policy
**Route:** `/Portal/InstitutionPolicy`  
**View:** `Portal/InstitutionPolicy.cshtml`

Displays and manages institution-wide policies. Admins can configure academic policies, attendance policies, grading policies, code of conduct, and other institutional regulations. Policies are displayed to all users based on their role and scope.

**Key Features:**
- Policy listing by category
- Create/edit institution policies
- Academic, attendance, grading, conduct policies
- Role-scoped policy visibility

---

## Library Integration

### Library Config
**Route:** `/Portal/LibraryConfig`  
**View:** `Portal/LibraryConfig.cshtml`

Configuration page for library system integration. Manages connection settings and configuration for integrating with external library management systems. Allows configuring library API endpoints, authentication, and synchronization settings.

**Key Features:**
- Library integration configuration
- External library system connection settings
- API endpoint configuration
- Authentication setup for library systems
- Synchronization settings

---

## Error Page

### Error
**Route:** (Handled by exception middleware)  
**View:** `Shared/Error.cshtml`

The global error page displayed when an unhandled exception occurs. Shows a user-friendly error message with a request ID for support reference. Does not expose sensitive stack trace information in production. Uses response caching disabled headers to ensure fresh content.

**Key Features:**
- User-friendly error display
- Request ID for support reference
- No sensitive information exposure
- No-cache response headers
- Consistent error handling across the application

---

## Summary Statistics

| Category | Page Count |
|---|---|
| **Public Pages** | 2 |
| **Authentication & Security** | 2 |
| **Dashboard & System Config** | 3 |
| **Academic Hierarchy** | 5 |
| **Facilities** | 2 |
| **Timetable** | 3 |
| **Student Management** | 2 |
| **Assignments & Quizzes** | 3 |
| **Attendance** | 2 |
| **Results & Grading** | 3 |
| **Gradebook & Rubrics** | 3 |
| **FYP** | 1 |
| **LMS & Course Materials** | 4 |
| **Discussion Forum** | 3 |
| **Announcements** | 1 |
| **Academic Calendar & Deadlines** | 2 |
| **Study Plans** | 3 |
| **Degree Audit & Rules** | 2 |
| **Graduation** | 4 |
| **Student Lifecycle** | 2 |
| **Payments** | 1 |
| **Reports** | 14 |
| **Analytics** | 1 |
| **AI Chat** | 1 |
| **Helpdesk** | 3 |
| **Notifications** | 1 |
| **Search** | 1 |
| **User Settings** | 2 |
| **User Import** | 1 |
| **Admin Users** | 1 |
| **Portal Configuration** | 4 |
| **License** | 1 |
| **Theme** | 1 |
| **Audit Logs** | 1 |
| **Accreditation** | 1 |
| **Certificates** | 1 |
| **Institution Policy** | 1 |
| **Library Integration** | 1 |
| **Error** | 1 |
| **TOTAL** | **92 pages** |

---

> **Note:** This catalog documents the EduSphere web portal (`Tabsan.EduSphere.Web`). The backend API (`Tabsan.EduSphere.API`) exposes REST endpoints consumed by these pages and is documented separately. Partial views (prefixed with `_`) are internal components and not listed as standalone pages.
