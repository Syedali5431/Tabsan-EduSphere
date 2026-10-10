// Chapters 1-4: introduction, concepts, getting started, quick start by role.
const ALL = 'SuperAdmin, Admin, Faculty, Student, Finance';

module.exports = [
  {
    chapter: 'Introduction',
    intro: [
      'Tabsan EduSphere is a web-based campus management portal for universities, colleges and schools. It brings the everyday work of an institution into one secure place: departments, programs, courses, enrolments, timetables, attendance, assessments, results, graduation, certificates, fee receipts, reports and support tickets.',
      'Everyone uses the same portal. What each person sees depends on their role: an administrator sees setup and management pages, a teacher sees their classes, a student sees their own records, and the finance office sees fee receipts.',
      'This manual explains every screen in the portal step by step, with screenshots taken from a running installation. You do not need to read it from start to finish. Use the Contents page or the Index at the back to jump to the task you need.',
    ],
    sections: [
      {
        title: 'Who should read this manual',
        intro: ['The manual is written for everyone who signs in to the portal. Each section begins with an "Available to" line, so you can quickly see whether it applies to you.'],
        bullets: [
          '**SuperAdmin** – the platform owner. Manages the product license, tenants and campuses, which modules are switched on, which menus each role sees, portal branding, the institution policy and the Admin accounts.',
          '**Admin** – institution administrators. Set up departments, programs, courses, students and timetables, and manage results, graduation, certificates, user accounts, fee receipts and reports for their departments.',
          '**Faculty** – teachers. Work with their own classes: timetable, assignments, attendance, marks, gradebook, rubrics, quizzes, online course content, final year project supervision and announcements.',
          '**Student** – learners. View their timetable, assignments, quizzes, course material, attendance and results, check their degree progress and apply for graduation.',
          '**Finance** – finance office staff. Issue and confirm fee receipts and run the payment report.',
        ],
      },
      {
        title: 'How this manual is organised',
        intro: ['Chapters follow the order in which an institution normally works: first the concepts and sign-in, then academic setup, then teaching and assessment, then progression and graduation, and finally reports, finance, accounts and SuperAdmin administration.'],
        bullets: [
          'Chapter 2 explains the terms used throughout the portal. Read it once if you are new.',
          'Chapter 3 covers signing in, finding your way around and signing out.',
          'Chapter 4 is a quick start for each role: the handful of pages you will use most.',
          'Chapters 5 to 13 describe each screen in detail: what it is for, what you need before you start, the steps, the fields, what happens afterwards and useful tips.',
          'Chapter 14 answers common questions. Appendix A shows which menus each role can see, and Appendix B is a glossary.',
        ],
      },
      {
        title: 'User roles at a glance',
        index: ['Roles'],
        intro: ['Each user account has exactly one role. The role decides which menus appear in the sidebar and which actions the portal allows. If you need something your role cannot do, ask an Admin or SuperAdmin; they can either do it for you or, where appropriate, change your access.'],
        table: {
          head: ['Role', 'Scope', 'Typical work'],
          widths: [1700, 2600, 5000],
          rows: [
            ['SuperAdmin', 'Whole installation, all tenants', 'License, tenants and campuses, module composition, sidebar visibility, branding, institution policy, Admin accounts, audit logs'],
            ['Admin', 'One tenant; only the departments assigned to them', 'Departments, programs, courses, enrolments, students, timetables, attendance, results, graduation, certificates, user import, payments, reports'],
            ['Faculty', 'The departments and classes they teach', 'Teacher timetable, assignments, attendance, results, gradebook, rubrics, quizzes, LMS, course material, FYP, announcements'],
            ['Student', 'Their own records', 'Timetable, assignments, quizzes, course material, attendance, results, degree audit, graduation application'],
            ['Finance', 'Fee receipts', 'Payments, payment summary report, analytics'],
          ],
        },
      },
      {
        title: 'Accounts used in this manual',
        index: ['Test accounts', 'testadmin'],
        intro: ['The screenshots were captured with the demonstration accounts below. All demo accounts use the password EduSphere147. These accounts exist only in the demonstration data; change or remove them before the portal is used with real students.'],
        table: {
          head: ['Username', 'Role', 'Notes'],
          widths: [2000, 1500, 5800],
          rows: [
            ['superadmin', 'SuperAdmin', 'Full platform access, including license and tenant management.'],
            ['testadmin', 'Admin', 'Testing account with every right except SuperAdmin. Works in the University tenant and is assigned to all of its departments. Created by Scripts/08-Create-Test-Admin-User.sql.'],
            ['faculty.it1', 'Faculty', 'A teacher in the Information Technology department.'],
            ['bscs8s1', 'Student', 'A Computer Science student in semester 8.'],
            ['finance1', 'Finance', 'A finance officer.'],
          ],
        },
        notes: [['note', 'Every account other than SuperAdmin belongs to one tenant (for example University, College or School). Only SuperAdmin can see data across tenants.']],
      },
      {
        title: 'Conventions used in this manual',
        bullets: [
          'Menu names, buttons and field labels are shown in **bold**, for example **Departments** > **Add Department**.',
          'Numbered lists are procedures: follow the steps in order.',
          '"Fields and options" tables explain each box or list on a form.',
          '"What happens next" explains the result of an action, and "Good to know" lists tips and limits.',
          'NOTE boxes add context, TIP boxes suggest a faster way, and IMPORTANT boxes warn about actions that change data or access.',
          'Fields marked with a red asterisk (*) in the portal are required; the portal will not save the form until they are filled in.',
        ],
      },
    ],
  },

  {
    chapter: 'Key Concepts',
    intro: ['The portal uses a few terms consistently on every page. Understanding them makes the filters and forms much easier to follow.'],
    sections: [
      {
        title: 'How the academic structure fits together',
        index: ['Academic structure'],
        intro: [
          'Information in the portal is organised from the broadest level to the most specific. Most pages ask you to choose these levels in order, from left to right, before they show any data. Each choice narrows the next list.',
        ],
        table: {
          head: ['Level', 'What it means', 'Example'],
          widths: [1800, 4900, 2600],
          rows: [
            ['Tenant', 'A separate institution in the installation, with its own users and data.', 'University, College, School'],
            ['Campus', 'A physical site of a tenant.', 'UNI-1 (main campus)'],
            ['Department', 'An academic unit. Belongs to one institution type.', 'Information Technology'],
            ['Program', 'A degree or course of study offered by a department.', 'BS Computer Science'],
            ['Semester / Class', 'A level within a program: semesters for University, classes for School and College.', 'Semester 4, Class 11'],
            ['Course (subject)', 'A subject with a code, title and credit hours.', 'CS101 Introduction to Programming'],
            ['Offering', 'A course taught in a particular semester or class, usually by one teacher. Students enrol in offerings.', 'CS101, BSCS Semester 1'],
          ],
        },
        tips: [
          'If a list looks empty, check the filters at the top of the page first. Most pages show nothing until a department and an offering are chosen.',
          'Admins only see the departments assigned to them. Faculty only see the departments and classes they teach.',
        ],
      },
      {
        title: 'Institution types',
        index: ['Institution types'],
        intro: [
          'Each department belongs to one institution type, and the type changes how progression and results work.',
        ],
        table: {
          head: ['Type', 'Levels', 'Results'],
          widths: [1800, 3500, 4000],
          rows: [
            ['University', 'Semesters 1 to 8 (depending on the program); final year project in the last semester', 'Grades and GPA / CGPA'],
            ['College', 'Class 11 and Class 12', 'Percentage based'],
            ['School', 'Class 1 to Class 10', 'Percentage based'],
          ],
        },
        notes: [['note', 'Degree audit, degree rules, graduation applications, FYP and the GPA & CGPA report apply to University departments only.']],
      },
      {
        title: 'Statuses you will see',
        intro: ['Many lists show a coloured status badge. The most common ones are:'],
        bullets: [
          '**Active / Inactive** – inactive records are kept for history but no longer used. Most lists hide them unless you tick a "Show inactive" option.',
          '**Published** – results and assignments become visible to students only once published.',
          '**Pending / Approved / Rejected** – used for requests that need someone else to act, such as graduation applications, study plans and result changes.',
        ],
      },
      {
        title: 'Security and your session',
        index: ['Session timeout', 'Password'],
        bullets: [
          'Your session ends automatically after a period of inactivity set by your administrator. Sign in again to continue; unsaved changes on the open page are lost.',
          'Passwords must be at least 8 characters and different from your previous password. Passwords expire after 90 days by default, and the portal asks you to choose a new one.',
          'New accounts start with a temporary password (the username) and must choose a new password at first sign-in.',
          'Two-factor authentication adds a 6-digit code from an authenticator app to sign-in (see Two-Factor Authentication).',
          'Every sign-in and every important change is recorded in the audit log.',
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
        intro: ['Open the portal address supplied by your administrator in a current browser such as Microsoft Edge, Google Chrome, Mozilla Firefox or Safari. The portal works on desktop computers, tablets and phones.'],
        before: [
          'You need a username and password from your administrator. Students and staff usually receive them by email.',
          'If two-factor authentication is switched on for your account, have your authenticator app ready.',
        ],
        steps: [
          'Open the portal address. The **Sign in to your account** page appears.',
          'Enter your **Username** and **Password**. Click the eye icon to show or hide the password while you type.',
          'Click **Sign In**.',
          'If two-factor authentication is enabled on your account, enter the 6-digit code from your authenticator app when prompted.',
          'If this is your first sign-in, or your password has expired, the portal asks you to choose a new password before continuing.',
          'The portal opens. SuperAdmin users land on the Dashboard; all other users land on the Helpdesk ticket queue, and the sidebar shows their menus.',
        ],
        figs: [['testadmin:01-login-page', 'The sign-in page'], ['testadmin:02-login-filled', 'Sign-in page with credentials entered'], ['flow:login/1', 'Landing page after signing in (Admin)']],
        tips: [
          'After several failed attempts the account may be blocked for security reasons. Ask your administrator to check the account rather than trying repeatedly.',
          'Do not let your browser save your password on a shared or public computer.',
        ],
      },
      {
        title: 'The portal layout',
        index: ['Sidebar', 'Search', 'Notifications', 'AI Assistant'],
        intro: ['Every page shares the same layout, so once you know one page you know them all.'],
        bullets: [
          '**Sidebar** (left) – the menus your role can use, grouped into sections: Overview, Setup Flow, Faculty Related, Student Related, Academic Related, Settings Related and Financial Related. The highlighted item is the current page. Click the menu button (three lines) to collapse the sidebar to icons and give the page more room.',
          '**Page header** – the page title and the workspace name.',
          '**Search** – type part of a page or feature name, such as "attendance", and click **Go** to jump straight to it.',
          '**Notifications** – opens your notification inbox. A green dot means you have unread notifications.',
          '**Profile chip** (top right) – shows your username and role; click it to open the profile menu.',
          '**AI Assistant** (bottom right) – a chat assistant that answers questions about using the portal, when the AI module is switched on.',
          '**Messages** – after you save something, a short message appears at the top right confirming success or explaining a problem. It closes by itself, or click × to close it.',
        ],
        figs: [['testadmin:Departments', 'Portal layout: sidebar, header, search, notifications and profile']],
      },
      {
        title: 'Using filters and lists',
        index: ['Filters'],
        intro: [
          'Most pages start with a row of filters, such as Campus, Department, Course and Offering. Choose them from left to right; each choice refreshes the next list and the data below.',
          'Tables can be long. Use the browser\'s find feature (Ctrl+F, or Cmd+F on a Mac) to locate a name quickly, or narrow the filters.',
        ],
        tips: [
          'Your last choices are kept in the address bar, so you can bookmark a filtered page or send the link to a colleague with the same access.',
          'Buttons that change data, such as Deactivate or Delete, ask you to confirm first.',
        ],
      },
      {
        title: 'Profile menu and signing out',
        index: ['Sign out', 'Profile menu'],
        steps: [
          'Click your name in the top-right corner.',
          'Choose **View Notifications**, **User Settings** or **Two-Factor Authentication** to open those pages.',
          'Click **Sign Out** to end your session.',
        ],
        figs: [['flow:login/2', 'The profile menu']],
        notes: [['important', 'Always sign out on a shared computer. Closing the browser tab alone may leave your session open until it times out.']],
      },
      {
        title: 'Access messages and read-only mode',
        index: ['Read-only mode', 'License'],
        intro: [
          'If you open a page your role cannot use (for example from an old bookmark), the portal returns you to your landing page with a short message explaining why. Nothing is changed. Ask your administrator if you believe you need access.',
        ],
        notes: [['important', 'If the product license is missing, expired or invalid, the portal switches to read-only mode: you can still view information, but saving, creating and deleting are blocked until a SuperAdmin uploads a valid license (see License Update).']],
      },
    ],
  },

  {
    chapter: 'Quick Start by Role',
    intro: ['This chapter lists the pages each role uses most, in the order they are usually needed. Each item points to the chapter that explains it in detail.'],
    sections: [
      {
        title: 'Admin: setting up a new term',
        roles: 'Admin, SuperAdmin',
        intro: ['Follow this order when preparing a new semester or school year. Each step depends on the one before it.'],
        steps: [
          'Check that your **Departments** and **Programs** exist (Chapter 5).',
          'Add any new **Courses**, then create an **offering** for each course you will teach this term and choose the teacher (Chapter 5).',
          'Create **Buildings** and **Rooms** if they are new, then build the class **timetables** in Timetable Admin (Chapter 6).',
          'Create accounts for new students and staff in **User Import** (Chapter 12).',
          'Enrol students in their offerings in **Enrollments** (Chapter 5).',
          'Check **Result Calculation** and **Grading Config** so marks become grades correctly (Chapter 8).',
          'At the end of the term, publish results, then promote students in **Student Lifecycle** (Chapter 8).',
        ],
      },
      {
        title: 'Faculty: a typical week',
        roles: 'Faculty',
        bullets: [
          'Check your classes in **Teacher Timetable** (Chapter 6).',
          'Take attendance after each class in **Enter Attendance** (Chapter 7).',
          'Post notices in **Announcements** and upload notes in **Course Material** or **LMS Manage** (Chapter 7).',
          'Create and grade **Assignments** and **Quizzes** (Chapter 7).',
          'Enter marks in **Enter Results** and review them in the **Gradebook** (Chapter 7).',
          'Answer questions in **Discussion** and, for final-year students, supervise projects in **FYP** (Chapters 7 and 8).',
        ],
      },
      {
        title: 'Student: finding your information',
        roles: 'Student',
        bullets: [
          '**Student Timetable** – when and where your classes are (Chapter 6).',
          '**Assignments** and **View Quizzes** – what is due and how to submit (Chapter 7).',
          '**Course Material** and **Announcements** – notes, files and notices from your teachers (Chapter 7).',
          '**Attendance** – your attendance percentage for each course (Chapter 7).',
          '**Results** – your published marks and grades; request a re-check if needed (Chapter 8).',
          '**Degree Audit** and **Graduation Apply** – your progress towards your degree (Chapter 9).',
          '**Helpdesk** – ask for help from the institution (Chapter 12).',
        ],
      },
      {
        title: 'Finance: managing fees',
        roles: 'Finance',
        bullets: [
          'Create a fee receipt for each student in **Payments**, or import many at once from a CSV file (Chapter 11).',
          'Confirm receipts when payment arrives, and cancel any issued in error (Chapter 11).',
          'Run **Report Center** > **Payment Summary** for totals by status (Chapter 10).',
        ],
      },
    ],
  },
];
