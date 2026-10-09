// Manual content. Figures reference page captures as "role:Action" and workflow steps as "flow:<name>/<n>".
// Each section: { title, roles, index[], intro[], fields[[label,desc]], steps[], figs[[ref,caption]], notes[[kind,text]] }

const ALL = 'SuperAdmin, Admin, Faculty, Student, Finance';

module.exports = [
  {
    chapter: 'Introduction',
    intro: [
      'Tabsan EduSphere is a web-based campus management portal for universities, colleges and schools. It brings departments, programs, courses, enrolments, timetables, attendance, assessments, results, graduation, certificates, finance, reporting and support into one secure portal.',
      'This manual explains every screen in the portal, step by step, with screenshots taken from a running installation. It is organised by area of work, and each section states which roles can use it.',
    ],
    sections: [
      {
        title: 'Who should read this manual',
        intro: ['The manual is written for everyone who signs in to the portal:'],
        bullets: [
          'SuperAdmin – the platform owner who manages the license, tenants, campuses, modules and portal-wide settings.',
          'Admin – institution administrators who set up departments, programs, courses, students, timetables, finance and reports.',
          'Faculty – teachers who manage their classes, attendance, assessments, results, course content and FYP supervision.',
          'Student – learners who view their timetable, assignments, quizzes, attendance and results, and apply for graduation.',
          'Finance – staff who issue and confirm fee receipts and run the payment report.',
        ],
      },
      {
        title: 'User roles at a glance',
        index: ['Roles'],
        intro: ['Each user account has exactly one role. The role decides which menus appear in the sidebar and which actions are allowed. Appendix A lists every menu against every role.'],
        table: {
          head: ['Role', 'Scope', 'Typical work'],
          widths: [1700, 2600, 5000],
          rows: [
            ['SuperAdmin', 'Whole installation, all tenants', 'License, tenants and campuses, module composition, sidebar visibility, branding, institution policy, admin users, audit logs'],
            ['Admin', 'One tenant; assigned departments', 'Departments, programs, courses, enrolments, students, timetables, attendance, results, graduation, certificates, user import, payments, reports'],
            ['Faculty', 'Assigned departments and classes', 'Teacher timetable, assignments, attendance, results, gradebook, rubrics, quizzes, LMS, course material, FYP, announcements'],
            ['Student', 'Own records', 'Timetable, assignments, quizzes, course material, attendance, results, degree audit, graduation application'],
            ['Finance', 'Fee receipts', 'Payments, payment summary report, analytics'],
          ],
        },
      },
      {
        title: 'Accounts used in this manual',
        index: ['Test accounts', 'testadmin'],
        intro: ['The screenshots were captured with the demonstration accounts below. All demo accounts use the password EduSphere147. Change these passwords before any production use.'],
        table: {
          head: ['Username', 'Role', 'Notes'],
          widths: [2000, 1500, 5800],
          rows: [
            ['superadmin', 'SuperAdmin', 'Full platform access, including license and tenant management.'],
            ['testadmin', 'Admin', 'Testing account with every right except SuperAdmin. Works in the University tenant and is assigned to all of its departments. Created by Scripts/08-Create-Test-Admin-User.sql.'],
            ['faculty.it1', 'Faculty', 'Information Technology faculty member.'],
            ['bscs8s1', 'Student', 'BSCS student in semester 8.'],
            ['finance1', 'Finance', 'Finance officer.'],
          ],
        },
        notes: [['note', 'Every account other than SuperAdmin belongs to one tenant (for example University, College or School). Only SuperAdmin can see data across tenants.']],
      },
      {
        title: 'Conventions used in this manual',
        bullets: [
          'Menu names, buttons and field labels are shown in bold, for example Departments > Add Department.',
          'Numbered lists are procedures: follow the steps in order.',
          'Note boxes add context, Tip boxes suggest a faster way, and Important boxes warn about actions that change data or access.',
          'Fields marked with a red asterisk (*) in the portal are required.',
        ],
      },
    ],
  },

  {
    chapter: 'Getting Started',
    intro: ['This chapter covers signing in, finding your way around the portal, and signing out.'],
    sections: [
      {
        title: 'Opening the portal and signing in',
        index: ['Sign in', 'Login'],
        intro: ['Open the portal address supplied by your administrator in a current browser (Microsoft Edge, Google Chrome, Mozilla Firefox or Safari).'],
        steps: [
          'Open the portal address. The Sign in to your account page appears.',
          'Enter your **Username** and **Password**. Use the eye icon to show or hide the password while typing.',
          'Click **Sign In**.',
          'If two-factor authentication is enabled on your account, enter the 6-digit code from your authenticator app when prompted.',
          'The portal opens. SuperAdmin users land on the Dashboard; all other users land on the Helpdesk ticket queue.',
        ],
        figs: [['testadmin:01-login-page', 'The sign-in page'], ['testadmin:02-login-filled', 'Sign-in page with credentials entered'], ['flow:login/1', 'Landing page after signing in (Admin)']],
        notes: [['tip', 'Your session is protected and every sign-in is logged. If you see a message that your account is locked or your password has expired, contact your administrator.']],
      },
      {
        title: 'The portal layout',
        index: ['Sidebar', 'Search', 'Notifications', 'AI Assistant'],
        intro: ['Every page shares the same layout:'],
        bullets: [
          '**Sidebar** (left) – the menus your role can use, grouped into sections such as Setup Flow, Faculty Related, Student Related, Academic Related, Settings Related and Financial Related. The highlighted item is the current page. Use the menu button (three lines) to collapse or expand it.',
          '**Page header** – the page title and the workspace name.',
          '**Search** – type a page or feature name and click **Go** to jump straight to it.',
          '**Notifications** – opens your notification inbox. A green dot means you have unread notifications.',
          '**Profile chip** – shows your username and role; click it to open the profile menu.',
          '**AI Assistant** (bottom right) – a chat assistant that answers questions about the portal, when the AI module is enabled.',
        ],
        figs: [['testadmin:Departments', 'Portal layout: sidebar, header, search, notifications and profile']],
      },
      {
        title: 'Profile menu and signing out',
        index: ['Sign out', 'Profile menu'],
        steps: [
          'Click your name in the top-right corner.',
          'Choose **View Notifications**, **User Settings** or **Two-Factor Authentication** to open those pages.',
          'Click **Sign Out** to end your session. Always sign out on shared computers.',
        ],
        figs: [['flow:login/2', 'The profile menu']],
      },
      {
        title: 'Access messages and read-only mode',
        index: ['Read-only mode', 'License'],
        intro: ['If you open a page your role cannot use, the portal returns you to your landing page with a short message explaining why. Ask your administrator if you believe you need access.'],
        notes: [['important', 'If the product license is missing, expired or invalid, the portal switches to read-only mode: you can still view information, but saving, creating and deleting are blocked until a SuperAdmin uploads a valid license (see License Update).']],
      },
    ],
  },

  {
    chapter: 'Academic Setup',
    intro: ['Academic setup follows the order shown in the Setup Flow menu: departments, then programs, then courses and offerings, then enrolments. Students are created through User Import (Chapter 9).'],
    sections: [
      {
        title: 'Departments', roles: 'SuperAdmin, Admin', index: ['Departments'],
        intro: ['Departments are the top level of the academic structure. Each department belongs to one institution type (University, College or School).'],
        steps: [
          'Open **Departments** from the Setup Flow menu. The list shows each department\'s code, name, institution type and status.',
          'Click **Add Department**.',
          'Enter the **Department Code** (short and unique, for example CS) and the **Department Name**, then choose the **Institution Type**.',
          'Click **Create**. The department appears in the list.',
          'To change a department later, click **Edit**. To retire it without deleting history, click **Deactivate**.',
        ],
        figs: [['flow:department-add/1', 'Departments list'], ['flow:department-add/3', 'Add Department form completed']],
        notes: [['note', 'A department created by an Admin is assigned to that Admin automatically. SuperAdmin also sees tenant and campus filters and the Admin Department Assignments panel, which controls which Admin users manage each department.']],
      },
      {
        title: 'Programs', roles: 'SuperAdmin, Admin, Faculty (view)', index: ['Programs'],
        intro: ['Programs are the degrees or courses of study offered by a department, for example Bachelor of Science in Computer Science. Total Levels is the number of semesters (University) or classes (School/College).'],
        steps: [
          'Open **Programs**.',
          'In **Create Program**, enter the **Code** and **Name**, choose the **Department**, and set **Total Levels**.',
          'Click **Create**.',
          'Use **Filter by Department** to narrow the Program List, and **Rename** or **Deactivate** to maintain programs.',
        ],
        figs: [['testadmin:Programs', 'Programs page']],
      },
      {
        title: 'Courses and offerings', roles: 'SuperAdmin, Admin, Faculty (view)', index: ['Courses', 'Course offerings'],
        intro: ['A course is a subject (code, title, credit hours and type). An offering is a course taught in a particular semester or class by a faculty member; students enrol in offerings.'],
        steps: [
          'Open **Courses** and choose the **Department** in the scope filter, then click **Apply Scope**.',
          'Click **Add Course** to create a course: enter the code, title, credit hours and type (core or elective).',
          'In **Active Offerings**, create an offering by choosing the course, semester and faculty member.',
          'Use **Off** to close an offering when teaching ends.',
        ],
        figs: [['testadmin:Courses', 'Courses and active offerings']],
      },
      {
        title: 'Enrollments', roles: 'SuperAdmin, Admin, Faculty (view)', index: ['Enrollments'],
        intro: ['Enrollments manage the roster of each course offering.'],
        steps: [
          'Open **Enrollments**.',
          'Choose an offering in **Select Course Offering**. The Enrollment Roster lists the students enrolled.',
          'Click **Enroll Student**, choose the student and click **Enroll**. Use **Drop** to remove an enrolment.',
          'Admins can open or close enrolment for the whole tenant/campus with **Deactivate Enrollment** (and re-open it the same way).',
        ],
        figs: [['testadmin:Enrollments', 'Enrollments page']],
        notes: [['note', 'Courses with prerequisites can only be enrolled in once the student has passed the prerequisite course (see Prerequisites).']],
      },
      {
        title: 'Students', roles: 'SuperAdmin, Admin, Faculty', index: ['Students'],
        intro: ['The Students page is a searchable directory of student profiles: registration number, full name, department, program, level and status.'],
        steps: ['Open **Students**.', 'Choose a **Department** to filter the list.', 'Scan or search the list; the count beside Student List shows how many match.'],
        figs: [['flow:students-filter/1', 'Student list filtered by department']],
        notes: [['tip', 'New students are added through User Import (single user or CSV). Promotion between semesters is handled in Student Lifecycle.']],
      },
    ],
  },

  {
    chapter: 'Timetables and Facilities',
    sections: [
      {
        title: 'Buildings and rooms', roles: 'SuperAdmin, Admin', index: ['Buildings', 'Rooms'],
        intro: ['Buildings and rooms are used when scheduling timetable entries.'],
        steps: [
          'Open **Buildings**, enter the **Name** and **Code** under Add Building, and click **Create**.',
          'Open **Rooms** (or click **Rooms** on the Buildings page), choose the building, enter the **Room Number** and **Capacity**, and click **Create**.',
        ],
        figs: [['testadmin:Buildings', 'Buildings'], ['testadmin:Rooms', 'Rooms']],
      },
      {
        title: 'Timetable Admin', roles: 'SuperAdmin, Admin', index: ['Timetable'],
        intro: ['Timetable Admin creates class timetables and their weekly entries.'],
        steps: [
          'Open **Timetable Admin** and set the **Scope Selection** (campus and department), then click **Apply Scope**.',
          'Under **Create Timetable**, choose the **Institute**, **Degree Program**, **Semester**, **Semester Number** and **Effective Date**, then click **Create Timetable**.',
          'Select the timetable in **Available Timetables**.',
          'Under **Add Entry**, choose the **Day**, **Start** and **End** time, **Subject**, **Teacher**, **Building** and **Room**, then click **Add Entry**. Repeat for every class in the week.',
          'Publish the timetable so students can see it in Student Timetable.',
        ],
        figs: [['testadmin:TimetableAdmin', 'Timetable Admin']],
        notes: [['note', 'SuperAdmin must select both a tenant and a campus before creating or editing entries.']],
      },
      {
        title: 'Teacher Timetable', roles: 'SuperAdmin, Admin, Faculty', index: ['Teacher Timetable'],
        intro: ['Shows the classes assigned to a faculty member. Faculty see their own; Admins choose a **Faculty** member and click **Apply**. Tick **Show deactivated** to include retired timetables.'],
        figs: [['faculty:TimetableTeacher', 'Teacher Timetable (Faculty view)']],
      },
      {
        title: 'Student Timetable', roles: 'Student', index: ['Student Timetable'],
        intro: ['Students see the published timetables for their department. Choose a **Timetable** and, optionally, a **Day Filter**, then click **Apply**.'],
        figs: [['student:TimetableStudent', 'Student Timetable']],
      },
    ],
  },

  {
    chapter: 'Teaching and Learning',
    sections: [
      {
        title: 'Assignments', roles: 'SuperAdmin, Admin, Faculty, Student', index: ['Assignments'],
        intro: ['Faculty create assignments for an offering, collect submissions and grade them. Students see their assignments with due dates, marks and status.'],
        steps: [
          'Open **Assignments** and choose the department and offering.',
          'Faculty: create an assignment with a title, description, due date and maximum marks, then publish it.',
          'Open an assignment to review submissions and enter marks and feedback.',
          'Tick **Show inactive assignments** to see retired assignments.',
        ],
        figs: [['flow:faculty-assignments/1', 'Assignments (Faculty)'], ['student:Assignments', 'Assignments (Student view)']],
      },
      {
        title: 'Entering attendance', roles: 'SuperAdmin, Admin, Faculty', index: ['Attendance', 'Enter Attendance'],
        intro: ['Attendance is recorded per course offering and date.'],
        steps: [
          'Open **Enter Attendance**.',
          'Choose the **Department**.',
          'Choose the **Course** (program).',
          'Choose the offering in **Select Course Offering**. The attendance summary for the offering and the Attendance Entry Table load.',
          'In the entry table, tick **Present** for each student who attended (untick for absent) and click **Save Attendance**.',
          'To load a whole class at once, click **Get Template**, fill in the CSV, and use **Import Attendance**. Tick **Strict mode** to reject the whole file if any row is invalid.',
        ],
        figs: [['flow:attendance-entry/1', 'Enter Attendance – scope filters'], ['flow:attendance-entry/2', 'Step 1: department selected'], ['flow:attendance-entry/3', 'Step 2: course selected'], ['flow:attendance-entry/4', 'Step 3: offering selected – attendance summary per student']],
        notes: [['tip', 'Click View Low Attendance (<75%) to list students below the attendance threshold.']],
      },
      {
        title: 'Viewing attendance', roles: 'Faculty, Student', index: ['Attendance'],
        intro: ['Attendance shows totals and percentages per student and course. Students see only their own attendance and a Low Attendance (<75%) list of courses that need attention.'],
        figs: [['student:Attendance', 'Attendance (Student view)']],
      },
      {
        title: 'Entering results', roles: 'SuperAdmin, Admin, Faculty', index: ['Results', 'Enter Results'],
        intro: ['Results are entered per subject and exam type (for example Midterm, Final, Quiz or Assignment). Required filters must be selected before entry is enabled.'],
        steps: [
          'Open **Enter Results** and choose the **Department**.',
          'Choose the **Program/Course**.',
          'Choose the **Subject**.',
          'Choose the **Exam Type** and click **Apply**.',
          'Click **+ Enter Result** to add marks for a student, or download the CSV with **Get Template** and upload it with **Import Results**.',
          'Review the Result Entry Grid, then click **Publish All** to release the results to students. Use **Export CSV** or **Export PDF** for a copy.',
        ],
        figs: [['flow:results-entry/1', 'Step 1: department'], ['flow:results-entry/4', 'Steps 2–4: program, subject and exam type applied; the Result Entry Grid']],
        notes: [['important', 'Published results are visible to students immediately. Corrections to published results are recorded in the audit log.']],
      },
      {
        title: 'Gradebook', roles: 'SuperAdmin, Admin, Faculty', index: ['Gradebook'],
        intro: ['The Gradebook shows every student\'s marks for every assessment in one offering. Choose a **Course Offering** to load the grid.'],
        figs: [['flow:faculty-gradebook/1', 'Gradebook for a course offering']],
      },
      {
        title: 'Rubric Management', roles: 'SuperAdmin, Admin, Faculty', index: ['Rubrics'],
        intro: ['Rubrics define marking criteria and performance levels for an offering. Choose a **Course Offering**, then add criteria and levels with their points. Rubrics are used when grading assignments.'],
        figs: [['testadmin:RubricManage', 'Rubric Management']],
      },
      {
        title: 'Quizzes', roles: 'SuperAdmin, Faculty; Students take quizzes in View Quizzes', index: ['Quizzes'],
        intro: ['Faculty create quizzes with questions and options for an offering. Choose the offering in **Select Course Offering** to list its quizzes; tick **Include Inactive** to see closed quizzes.'],
        figs: [['flow:faculty-quizzes/1', 'Quizzes (Faculty)'], ['student:ViewQuizzes', 'My Quizzes (Student)']],
        notes: [['note', 'Students open View Quizzes, answer the questions before the last date, and click Submit. Marks appear once the quiz is graded.']],
      },
      {
        title: 'LMS Manage', roles: 'SuperAdmin, Admin, Faculty', index: ['LMS'],
        intro: ['LMS Manage builds weekly learning modules for an offering.'],
        steps: ['Open **LMS Manage** and choose the offering.', 'Under **Add New Module**, enter the **Title**, **Week #** and the **Body** (HTML is allowed).', 'Click **Add Module**.'],
        figs: [['testadmin:LmsManage', 'LMS Manage']],
      },
      {
        title: 'Course Material', roles: 'SuperAdmin, Admin, Faculty, Student', index: ['Course Material'],
        intro: ['Staff upload documents and links for a subject; students browse and download them.'],
        steps: ['Open **Course Material** and set the filters (department, program, semester, subject/course), then click **Apply Filters**.', 'Click **Add Material** to upload a file or link.', 'Tick **Active only** to hide withdrawn material.'],
        figs: [['testadmin:CourseMaterial', 'Manage Course Materials'], ['student:CourseMaterial', 'Browse Course Materials (Student)']],
      },
      {
        title: 'Discussion', roles: 'SuperAdmin, Admin, Faculty, Student', index: ['Discussion'],
        intro: ['Discussion threads let students and staff ask and answer questions for an offering. Use **Start a New Thread**, enter the question and click **Post**. Faculty can mark a thread as solved.'],
        figs: [['testadmin:Discussion', 'Discussion']],
      },
      {
        title: 'Announcements', roles: 'SuperAdmin, Admin, Faculty, Student', index: ['Announcements'],
        intro: ['Announcements publish notices to a department or a single course offering.'],
        steps: ['Open **Announcements**.', 'Choose a **Department** and, optionally, a **Course Offering**, then click **Apply** to view announcements in that scope.', 'Staff: under **Post Announcement**, enter the title and message and click **Post**.', 'Tick **Show inactive** to see expired announcements.'],
        figs: [['flow:announcement/1', 'Announcements for a department']],
        notes: [['note', 'An announcement posted to a whole department reaches every course offering in it. It is listed once with an "N offerings" label, and Deactivate or Delete applies to all of those offerings together.']],
      },
    ],
  },

  {
    chapter: 'Results, Progression and Final Year Projects',
    sections: [
      {
        title: 'Results', roles: 'SuperAdmin, Admin, Faculty, Student', index: ['Results'],
        intro: ['Staff use Results to review published marks with the same filters as Enter Results. Students see their own results by class, can export them to CSV or PDF, and can click **Request Re-check** on a result they want reviewed.'],
        figs: [['flow:student-results/1', 'Results (Student view)']],
      },
      {
        title: 'Result Calculation', roles: 'SuperAdmin, Admin', index: ['Result Calculation', 'GPA'],
        intro: ['Result Calculation defines how marks become grades and GPA.'],
        steps: ['In **Section 1: GPA and Score**, choose the **Institute** and map score ranges to GPA points with **Add Row**.', 'In **Section 2: Subject Score Calculation**, choose the **Course Type** or **Course** and set the weight of each **Component** (for example Midterm 30%, Final 50%, Assignments 20%).', 'Tick **Active** and click **Save**.'],
        figs: [['testadmin:ResultCalculation', 'Result Calculation']],
        notes: [['note', 'University results use GPA scales. School and College results are percentage based and do not need GPA rows.']],
      },
      {
        title: 'Grading Config', roles: 'SuperAdmin (edit), Admin and Faculty (view)', index: ['Grading Config'],
        intro: ['Grading Config sets the pass threshold and optional grade ranges for School, College and University separately. Choose the scope, enter the **Pass Threshold** and **Grade Ranges JSON**, tick **Active**, and click the matching Save button.'],
        figs: [['superadmin:GradingConfig', 'Grading Config (SuperAdmin)']],
      },
      {
        title: 'Prerequisites', roles: 'SuperAdmin, Admin', index: ['Prerequisites'],
        intro: ['Prerequisites stop students enrolling in a course until they have passed another. Each course card shows its prerequisites; click **Add Prerequisite** to choose the required course, or **Remove** to delete one.'],
        figs: [['testadmin:Prerequisites', 'Course Prerequisites']],
      },
      {
        title: 'Student Lifecycle', roles: 'SuperAdmin, Admin', index: ['Student Lifecycle', 'Promotion', 'Graduation'],
        intro: ['Student Lifecycle promotes students to the next semester or class and graduates students who complete their program.'],
        steps: ['Open **Student Lifecycle** and choose the **Department**.', 'Choose the **Semester** (or **Class**). The students at that level are listed with their standing.', 'Click **Promote** for an eligible student, or **Graduate** for a student in the final level.'],
        figs: [['flow:lifecycle/1', 'Department selected'], ['flow:lifecycle/2', 'Students in the selected semester']],
        notes: [['important', 'University students must have their FYP created before the final promotion, because promoting past the last semester graduates the student automatically.']],
      },
      {
        title: 'Study Plan', roles: 'SuperAdmin, Admin, Faculty', index: ['Study Plan'],
        intro: ['Study plans record the courses a student intends to take in future semesters, for advisor review. Choose the **Department** and **Student**, then click **Create**; add courses to the plan and submit it for advice. Faculty can approve or comment as advisors.'],
        figs: [['testadmin:StudyPlan', 'Study Plan']],
      },
      {
        title: 'Final Year Projects (FYP)', roles: 'SuperAdmin, Faculty', index: ['FYP', 'Final Year Project'],
        intro: ['FYP tracks university final-year projects: title, student, supervisor, department, status and result. Faculty supervisors schedule meetings (Upcoming Meetings), enter the FYP result, and mark the project **Complete**. Click **+ Create Project** to register a project for a student in the final semester.'],
        figs: [['faculty:Fyp', 'FYP (Faculty view)']],
      },
    ],
  },

  {
    chapter: 'Graduation and Certificates',
    sections: [
      {
        title: 'Degree Rules', roles: 'SuperAdmin (edit), Admin and Faculty (view)', index: ['Degree Rules'],
        intro: ['A degree rule sets what a University program requires: minimum total, core and elective credits, minimum GPA and any required courses. SuperAdmin chooses the **Academic Program**, enters the minimums and clicks **Create Rule**.'],
        figs: [['superadmin:DegreeRules', 'Degree Rules']],
      },
      {
        title: 'Degree Audit', roles: 'SuperAdmin, Admin, Faculty, Student', index: ['Degree Audit'],
        intro: ['Degree Audit compares a student\'s completed courses with the program\'s degree rule. Staff choose a **Student** and click **Load Audit**. Students see their own audit, with completed courses and any unmet requirements.'],
        figs: [['student:DegreeAudit', 'Degree Audit (Student view)']],
      },
      {
        title: 'Graduation Eligibility', roles: 'SuperAdmin, Admin, Faculty', index: ['Graduation Eligibility'],
        intro: ['Lists students with credits earned, CGPA, unmet requirements and status. Click **View Audit** for the detail behind a student\'s status. Faculty see the students of their own departments.'],
        figs: [['testadmin:GraduationEligibility', 'Graduation Eligibility']],
      },
      {
        title: 'Applying for graduation (Students)', roles: 'Student', index: ['Graduation Apply'],
        intro: ['An active University student in the final semester with a completed FYP can apply for graduation.'],
        steps: ['Open **Graduation Apply**.', 'Optionally add a **Personal Note**.', 'Click **Submit Application**. Track its status on the same page.'],
        figs: [['student:GraduationApply', 'Graduation Application (Student)']],
      },
      {
        title: 'Graduation Applications', roles: 'SuperAdmin, Admin, Faculty', index: ['Graduation Applications', 'Approvals'],
        intro: ['Applications are approved in stages: Faculty, then Admin, then SuperAdmin (final). Any approver can reject an application. Filter by **Status** and **Department**, click **Filter**, then **View** an application to approve or reject it. Once finally approved, the graduation certificate becomes available.'],
        figs: [['testadmin:GraduationApplications', 'Graduation Applications']],
      },
      {
        title: 'Generate Certificates', roles: 'SuperAdmin, Admin, Faculty', index: ['Certificates'],
        intro: ['Generates completion, degree and transcript certificates for students.'],
        steps: ['Open **Generate Certificates** and filter by institution, department, course and class; use **Search Students** to find a student, then click **Apply**.', 'Click **Generate** on a student\'s row to produce the certificate.', 'Under **Certificate Templates**, use **Download Default** to get the standard template or **Import Template** to upload your institution\'s design.'],
        figs: [['testadmin:GenerateCertificates', 'Generate Certificates']],
      },
    ],
  },

  {
    chapter: 'Reports and Analytics',
    sections: [
      {
        title: 'Report Center', roles: 'SuperAdmin, Admin, Faculty, Finance', index: ['Reports', 'Report Center'],
        intro: ['Report Center lists the reports your role may run. Finance users see only the Payment Summary; students do not have reports.'],
        table: {
          head: ['Report', 'What it shows'], widths: [2800, 6500],
          rows: [
            ['Attendance Summary', 'Attendance totals and percentages by offering and student.'],
            ['Enrollment Summary', 'Enrolment counts by department, program and semester.'],
            ['FYP Status Report', 'Final year projects with supervisor, status and result.'],
            ['GPA & CGPA Report', 'Semester GPA and CGPA per student (University only).'],
            ['Low Attendance Warning', 'Students below the attendance threshold (default 75%).'],
            ['Payment Summary', 'Fee receipts by status and amount.'],
            ['Result Summary', 'Marks and grades by subject and exam type.'],
            ['Semester Results', 'Results for a whole semester or class.'],
            ['Student Transcript', 'A student\'s full academic record.'],
          ],
        },
        steps: ['Open **Report Center** and click a report.', 'Choose the filters (for example **Department** and **Program**) and click **Filter**.', 'Review the results on screen, then click **Excel**, **CSV** or **PDF** to download.'],
        figs: [['flow:report-gpa/1', 'Report Center'], ['flow:report-gpa/2', 'Report filters'], ['flow:report-gpa/3', 'GPA & CGPA Report generated, with export buttons']],
        notes: [['note', 'Admins must choose a department before a report generates. SuperAdmin can switch reports off for a tenant or campus with Deactivate Reports for Scope.']],
      },
      {
        title: 'Analytics', roles: 'SuperAdmin, Admin, Faculty, Finance', index: ['Analytics'],
        intro: ['Analytics shows performance, attendance and assignment trends with charts and a student table. Choose the institute, campus, department, course and semester/class, then click **Apply Filters**. **Reset** clears the filters.'],
        figs: [['testadmin:Analytics', 'Analytics overview']],
      },
    ],
  },

  {
    chapter: 'Finance',
    sections: [
      {
        title: 'Fee receipts (Payments)', roles: 'SuperAdmin, Admin, Finance', index: ['Payments', 'Fee receipts'],
        intro: ['Payments manages fee receipts from creation to confirmation.'],
        steps: [
          'Open **Payments**. All Receipts lists each receipt with student, receipt number, amount, due date and status.',
          'Under **Create Fee Receipt**, choose the **Student**, enter the **Amount**, **Receipt No**, **Description** and **Due Date**.',
          'Click **Create**. The receipt starts as Pending.',
          'When the student has paid, open the receipt and **Confirm** it. Use **Cancel** for receipts issued in error.',
          'For many receipts at once, click **Export Template**, fill in the CSV and use **Import CSV**.',
        ],
        figs: [['flow:payment-create/1', 'Payments – all receipts'], ['flow:payment-create/2', 'Create Fee Receipt form completed']],
        notes: [['tip', 'Use Filter by Student to see the payment history of one student, and Report Center > Payment Summary for totals.']],
      },
    ],
  },

  {
    chapter: 'Users, Communication and Personal Settings',
    sections: [
      {
        title: 'User Import and creating users', roles: 'SuperAdmin, Admin', index: ['User Import', 'Users'],
        intro: ['User Import creates accounts for students, faculty, admins and finance staff, one at a time or in bulk from CSV.'],
        steps: [
          'Open **User Import**.',
          'Bulk: download **faculty-admin-import-template.csv** or **students-import-template.csv**, fill it in (Username, Email and Role are required), choose it in **CSV File** and click **Upload and Import**. Review the import report.',
          'Single user: in **Create Single User**, enter the **Username**, **Email**, **Full Name** and **Role**. For a Faculty member choose the **Department**; for a Student choose the **Department** and **Program** and, optionally, a **Registration No.** (it defaults to the username). Mobile number, address and profile photo are optional.',
          'Click **Create User**. The temporary password is the username, and new users must change it at first sign-in. For students the academic profile is created at the same time, so they can use the student portal immediately.',
        ],
        figs: [['flow:user-create/1', 'User Import'], ['flow:user-create/2', 'Create Single User form for a student, with Department and Program']],
        notes: [['tip', 'In the students CSV template, fill DepartmentId and ProgramId (and optionally RegistrationNumber) to create each student profile during a bulk import.']],
      },
      {
        title: 'User Settings', roles: ALL, index: ['User Settings', 'Password'],
        intro: ['Every user can update their own email, mobile number and address, upload a profile photo, and change their password (current password, new password, confirm). Admins also see a scoped user directory where they can edit other users\' details and reset a password to the default.'],
        figs: [['flow:user-settings/1', 'User Settings (Student view)'], ['testadmin:UserSettings', 'User Settings with the user directory (Admin view)']],
      },
      {
        title: 'Two-Factor Authentication', roles: ALL, index: ['Two-factor authentication', 'MFA'],
        intro: ['Two-factor authentication adds a 6-digit code from an authenticator app (Microsoft Authenticator, Google Authenticator or Authy) to your sign-in.'],
        steps: ['Open **Two-Factor Authentication** from the profile menu.', 'Click **Start 2FA Setup** and scan the QR code with your authenticator app (or type the manual key).', 'Enter the code shown in the app to verify and enable 2FA. Keep the recovery codes in a safe place.', 'To turn 2FA off, enter a current code and disable it; the old secret is deleted.'],
        figs: [['testadmin:TwoFactorSettings', 'Two-Factor Authentication']],
      },
      {
        title: 'Notifications', roles: ALL, index: ['Notifications'],
        intro: ['The Inbox lists notifications such as new announcements, published results and approvals. Click **Mark All Read** to clear the unread indicator.'],
        figs: [['testadmin:Notifications', 'Notifications inbox']],
      },
      {
        title: 'Helpdesk', roles: ALL, index: ['Helpdesk', 'Support tickets'],
        intro: ['Helpdesk is the support ticket system. Tickets move through Open, In Progress, Resolved and Closed.'],
        steps: ['Open **Helpdesk**. Use the status tabs (All, Open, InProgress, Resolved, Closed) to filter.', 'Click **New Ticket**.', 'Choose the **Department** (optional) and **Category**, enter a **Subject** and **Details**, and click **Submit Ticket**.', 'Open a ticket with **View** to read replies and respond.'],
        figs: [['flow:student-helpdesk/1', 'Helpdesk ticket queue'], ['flow:student-helpdesk/2', 'New Support Ticket form']],
      },
      {
        title: 'Theme Settings', roles: ALL, index: ['Theme'],
        intro: ['Choose a colour theme for the portal. Click a theme to preview it, then save.'],
        figs: [['flow:theme/1', 'Theme Settings with a theme previewed']],
      },
    ],
  },

  {
    chapter: 'SuperAdmin Administration',
    intro: ['The pages in this chapter are available only to SuperAdmin users.'],
    sections: [
      { title: 'Dashboard', roles: 'SuperAdmin', index: ['Dashboard'], intro: ['The SuperAdmin Dashboard explains the workspace modes and holds the API connection settings (API Base URL, access token and default department). Change these only on instruction from your technical team.'], figs: [['superadmin:Dashboard', 'SuperAdmin Dashboard']] },
      { title: 'Tenant and Campus Management', roles: 'SuperAdmin', index: ['Tenants', 'Campuses'], intro: ['Tenants separate institutions (for example University, College and School); each tenant has one or more campuses. Create a tenant with a **Code** and **Name**, then click **Manage Campuses** to add campuses. Deactivate tenants or campuses that are no longer used.'], figs: [['superadmin:TenantManagement', 'Tenant Management'], ['superadmin:CampusManagement', 'Campus Management']] },
      { title: 'Admin Users', roles: 'SuperAdmin', index: ['Admin Users'], intro: ['Create Admin accounts and assign the departments each Admin manages. Enter the **Username**, **Email** and **Temporary Password**, optionally an **Institution Type**, tick the **Initial Department Assignments**, and click **Create Admin**. Select an existing admin to update their details and assignments.'], figs: [['superadmin:AdminUsers', 'Admin Users']] },
      { title: 'Module Composition', roles: 'SuperAdmin', index: ['Modules'], intro: ['Module Composition shows the module registry (17 modules), which are active, and the dashboard widgets for your role. Deactivate a module to hide its features for everyone. Modules allowed by the license are activated automatically when the license is uploaded.'], figs: [['superadmin:ModuleComposition', 'Module Composition']] },
      { title: 'Sidebar Settings', roles: 'SuperAdmin', index: ['Sidebar Settings'], intro: ['Controls which menu items Admin, Faculty and Student roles see. SuperAdmin always keeps full access, and System menus cannot be deactivated.'], figs: [['superadmin:SidebarSettings', 'Sidebar Settings']] },
      { title: 'Report Settings', roles: 'SuperAdmin', index: ['Report Settings'], intro: ['Lists the report definitions (nine by default) and lets SuperAdmin create new definitions, activate or deactivate them, and choose which roles may run each report.'], figs: [['superadmin:ReportSettings', 'Report Settings']] },
      { title: 'Dashboard Settings (Branding)', roles: 'SuperAdmin', index: ['Branding'], intro: ['Sets the institution name, portal subtitle, footer text, logo, privacy policy link and text, and the portal font family and size. Click **Save Branding**.'], figs: [['superadmin:DashboardSettings', 'Dashboard Settings – portal branding']] },
      { title: 'Institution Policy', roles: 'SuperAdmin', index: ['Institution Policy'], intro: ['Turns the University, School and College institution types on or off for the installation, within what the license allows. Click **Save Policy**.'], figs: [['superadmin:InstitutionPolicy', 'Institution Policy']] },
      {
        title: 'License Update', roles: 'SuperAdmin', index: ['License'],
        intro: ['Shows the current license (status, type, activation date and expiry) and uploads a new license file.'],
        steps: ['Open **License Update**.', 'Under **Upload License File**, choose the .tablic file supplied by Tabsan.', 'Click **Upload & Activate**. The Current License table updates.'],
        figs: [['flow:license/1', 'License Update']],
        notes: [['important', 'License files are encrypted and digitally signed. A file that has been edited in any way is rejected. The expiry date is fixed inside the file and is the same on every server it is activated on. When a license expires, the portal becomes read-only until a new license is uploaded.']],
      },
      { title: 'Library Config', roles: 'SuperAdmin', index: ['Library'], intro: ['Connects the portal to an external library system: enter the **Catalogue URL**, **Loan API URL** and **API Token**, then click **Save Configuration**.'], figs: [['superadmin:LibraryConfig', 'Library Config']] },
      { title: 'Accreditation', roles: 'SuperAdmin', index: ['Accreditation'], intro: ['Download the accreditation report template, complete it, and upload it back for record keeping.'], figs: [['superadmin:AccreditationTemplates', 'Accreditation templates']] },
      { title: 'Advanced Audit', roles: 'SuperAdmin', index: ['Audit logs'], intro: ['The audit log records who did what and when, with before and after values. Filter by text, actor, action, module and date range, then click **Apply Filters**. Export with **Export CSV**, **Export Excel** or **Export PDF**.'], figs: [['superadmin:AuditLogs', 'Audit Logs']] },
    ],
  },

  {
    chapter: 'Troubleshooting and FAQ',
    sections: [
      {
        title: 'Common questions',
        table: {
          head: ['Problem', 'What to do'], widths: [3300, 6000],
          rows: [
            ['I cannot sign in.', 'Check the username and that Caps Lock is off. After several failed attempts the account locks temporarily; ask your administrator to reset it.'],
            ['I was returned to the Helpdesk with a message.', 'The page is not available to your role. The message explains why. Ask your administrator if you need access.'],
            ['A list is empty.', 'Check the filters at the top of the page. Most pages need a department, course or offering selected first.'],
            ['Save buttons return "read-only mode".', 'The license is missing or expired. A SuperAdmin must upload a valid license in License Update.'],
            ['A student cannot enrol in a course.', 'The course may have a prerequisite the student has not passed, or enrolment may be closed for the campus.'],
            ['A student cannot apply for graduation.', 'Only active University students in their final semester with a completed FYP can apply. Check Degree Audit for unmet requirements.'],
            ['I lost my authenticator device.', 'Use a recovery code to sign in, or ask an administrator to reset two-factor authentication.'],
            ['Exported files do not open.', 'Make sure the download finished. CSV files open in Excel; PDF files need a PDF reader.'],
          ],
        },
      },
    ],
  },
];
