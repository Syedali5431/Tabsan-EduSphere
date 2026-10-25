// Chapters 1-4: introduction, concepts, getting started, quick start by role.
const ALL = 'SuperAdmin, Admin, Faculty, Student, Finance';

module.exports = [
  {
    chapter: 'Introduction',
    intro: [
      'Tabsan EduSphere is a web-based campus management portal for universities, colleges and schools. It brings the everyday work of an institution into one secure place: departments, programs, courses, enrolments, timetables, attendance, assessments, results, graduation, certificates, fee receipts, reports and support tickets.',
      'It is also a complete online learning system: course modules with completion requirements, quizzes with question banks and many question types, a grading workspace, peer review and student groups, similarity checking, learning outcomes and mastery, live online classes, external learning tools (LTI 1.3) and SCORM packages. People can sign in with their institution account (single sign-on), use the portal in English, Urdu or Arabic, install it as an app and receive notifications on their phones.',
      'Everyone uses the same portal. What each person sees depends on their role: an administrator sees setup and management pages, a teacher sees their classes, a student sees their own records, and the finance office sees fee receipts.',
      'This manual explains every screen in the portal step by step, with screenshots taken from a running installation. You do not need to read it from start to finish. Use the Contents page or the Index at the back to jump to the task you need.',
    ],
    sections: [
      {
        title: 'Who should read this manual',
        intro: ['The manual is written for everyone who signs in to the portal. Each section begins with an "Available to" line, so you can quickly see whether it applies to you.'],
        bullets: [
          '**SuperAdmin** – the platform owner. Manages the product license, tenants and campuses, which modules are switched on, which menus each role sees, portal branding, the institution policy and the Admin accounts, and connects the portal to other systems (single sign-on, external tools, live class and similarity services, languages, the learning record store).',
          '**Admin** – institution administrators. Set up departments, programs, courses, students and timetables, and manage results, graduation, certificates, user accounts, fee receipts, learning outcomes and reports for their departments.',
          '**Faculty** – teachers. Work with their own classes: timetable, assignments, attendance, marks, gradebook, rubrics, quizzes and question banks, course modules, live classes, external tools, SCORM packages, outcomes, final year project supervision and announcements.',
          '**Student** – learners. Follow their course modules, hand in assignments, take quizzes, join live classes, review classmates\' work, see their attendance, results and outcomes, check their degree progress and apply for graduation.',
          '**Finance** – finance office staff. Issue and confirm fee receipts and run the payment report.',
        ],
      },
      {
        title: 'How this manual is organised',
        intro: ['Chapters follow the order in which an institution normally works: first the concepts and sign-in, then academic setup, then teaching and assessment, then progression and graduation, and finally reports, finance, accounts and SuperAdmin administration.'],
        bullets: [
          'Chapter 2 explains the terms used throughout the portal. Read it once if you are new.',
          'Chapter 3 covers signing in (including single sign-on), choosing your language, finding your way around, using the portal on a phone and signing out.',
          'Chapter 4 is a quick start for each role: the handful of pages you will use most.',
          'Chapters 5 to 16 describe each screen in detail: what it is for, what you need before you start, the steps, the fields, what happens afterwards and useful tips. Chapter 7 covers teaching and online course content, Chapter 8 quizzes and assignment marking, Chapter 9 learning outcomes, and Chapter 16 the integrations a SuperAdmin sets up.',
          'Chapter 17 answers common questions. Appendix A shows which menus each role can see, and Appendix B is a glossary.',
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
            ['SuperAdmin', 'Whole installation, all tenants', 'License, tenants and campuses, module composition, sidebar visibility, branding, institution policy, Admin accounts, audit logs, single sign-on, external tools, integrations and languages'],
            ['Admin', 'One tenant; only the departments assigned to them', 'Departments, programs, courses, enrolments, students, timetables, attendance, results, graduation, certificates, user import, payments, learning outcomes, API tokens, reports'],
            ['Faculty', 'The departments and classes they teach', 'Teacher timetable, assignments and grading, attendance, results, gradebook, rubrics, quizzes and question banks, course modules, live classes, external tools, SCORM, outcomes, FYP, announcements'],
            ['Student', 'Their own records', 'Course modules, assignments and peer review, quizzes, live classes, course material, attendance, results, outcomes, degree audit, graduation application'],
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
            ['bscs8s1 … bscs8s5', 'Student', 'Computer Science students in semester 8, also enrolled in CS702 Machine Learning, the course used for the online-learning screenshots.'],
            ['finance1', 'Finance', 'A finance officer.'],
            ['admin.sch / admin.col / admin.uni', 'Admin', 'The School, College and University administrators. Each sees only its own institute’s departments, so no Institute filter is shown to them.'],
            ['faculty.sch3 / faculty.col3 / faculty.eco1', 'Faculty', 'Teachers in the School Arts & Humanities, College Pre-Medical and University Economics departments.'],
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
          head: ['Type', 'Levels', 'Results', 'Demo departments'],
          widths: [1500, 2600, 1900, 3300],
          rows: [
            ['University', 'Semesters 1 to 8 (depending on the program); final year project in the last semester', 'Grades and GPA / CGPA', 'Information Technology, Business Administration, Economics, Spanish Language'],
            ['College', 'Class 11 and Class 12', 'Percentage based', 'Information Technology, Pre-Medical, Pre-Engineering, Commerce (College)'],
            ['School', 'Class 1 to Class 10', 'Percentage based', 'Science Department, Arts & Humanities, Commerce (Class 9-10)'],
          ],
        },
        notes: [['note', 'Degree audit, degree rules, graduation applications, FYP and the GPA & CGPA report apply to University departments only.']],
      },
      {
        title: 'Online learning terms',
        index: ['Module items', 'Question banks', 'Outcomes', 'External tools', 'SCORM'],
        intro: ['The online learning pages add a few more terms. Each is explained in detail in Chapters 7 to 9.'],
        table: {
          head: ['Term', 'What it means'],
          widths: [2300, 7000],
          rows: [
            ['Module', 'A unit of a course, usually one week or topic, that holds items in the order students should follow them.'],
            ['Item and requirement', 'Anything in a module (a page, link, file, video, assignment, quiz, discussion, external tool or SCORM package), with what a student must do to complete it: view it, mark it done, submit it, score at least a percentage or post in it.'],
            ['Question bank', 'A reusable pool of quiz questions. A quiz can draw a number of questions from a bank at random, so each student gets a different set.'],
            ['Grading workspace', 'The page where a teacher marks submissions one after another: the work, annotations, rubric, marks, comments, similarity score and peer reviews side by side.'],
            ['Group set', 'A way of splitting a class into groups (for example project teams). A group assignment is handed in once per group.'],
            ['Peer review', 'Students review a number of classmates\' work, usually anonymously, with the assignment\'s rubric or a score and a comment.'],
            ['Similarity score', 'How much of a submission matches other work, from 0% to 100%. A prompt to look closer, not proof of copying.'],
            ['Learning outcome', 'A statement of what students should be able to do: program outcomes (PLOs) and course outcomes (CLOs). Graded work aligned to an outcome shows each student\'s mastery of it.'],
            ['Live class', 'A scheduled online class with a join link; joining through the portal can record attendance automatically.'],
            ['External tool (LTI)', 'Another learning application (for example an interactive-content or video tool) opened from a course without a second sign-in, which can send grades back.'],
            ['SCORM package', 'An interactive lesson exported from an authoring tool as a zip file. It runs inside the portal and reports progress, scores and time.'],
          ],
        },
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
          'Your institution may let you sign in with its own account (Microsoft, Google or another single sign-on service) instead of an EduSphere password. It may also require it, in which case password sign-in is turned off for your account.',
          'Every sign-in and every important change is recorded in the audit log, including how you signed in (password or single sign-on).',
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
          'You need a username and password from your administrator. Students and staff usually receive them by email. If your institution uses single sign-on, you can use your institution account instead (see the next section).',
          'If two-factor authentication is switched on for your account, have your authenticator app ready.',
        ],
        steps: [
          'Open the portal address. The **Sign in to your account** page appears. To use the portal in Urdu or Arabic, choose the language in the box above the form first.',
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
        title: 'Signing in with your institution account (single sign-on)',
        index: ['Single sign-on', 'SSO'],
        intro: [
          'When your institution has connected its identity provider (for example Microsoft Entra ID, Google Workspace or another OpenID Connect or SAML 2.0 service), the sign-in page shows a button for it below the password form. You then sign in with the same account you use for your institution\'s email, and you do not need a separate EduSphere password.',
          'Institutions with several tenants give each one its own sign-in address, ending in **?tenant=** and the tenant code; the buttons appear on that address.',
        ],
        steps: [
          'Open the sign-in address your institution gave you and click the button for your institution, for example **Sign in with Microsoft**.',
          'Your institution\'s own sign-in page opens. Sign in there as usual (with its own multi-factor check, if it uses one).',
          'You return to EduSphere and the portal opens. The first time, EduSphere finds your existing account by your verified email address or your institution ID (registration number or staff username), depending on how your administrator set it up.',
        ],
        figs: [['flow:sso-login/1', 'Sign-in page with single sign-on buttons'], ['flow:sso-login/2', 'The institution\'s own sign-in page (a test identity provider in this example)']],
        outcome: ['EduSphere remembers the link between your institution account and your EduSphere account. You can see and remove links under **Two-Factor Authentication** > **Linked sign-in accounts** (Chapter 14).'],
        tips: [
          'Single sign-on never creates new EduSphere accounts. If you see "This account is not linked to an EduSphere user", sign in with your username and password once, or ask your administrator.',
          'If you see a message that your identity provider reports a role that does not match your account, your institution\'s directory lists you differently (for example as a student when your EduSphere account is staff). Contact your administrator.',
          'SuperAdmin accounts always sign in with a password and two-factor authentication.',
        ],
      },
      {
        title: 'Choosing your language',
        index: ['Language', 'Urdu', 'Arabic', 'Right to left'],
        intro: [
          'The portal is available in English, Urdu (اردو) and Arabic (العربية). Choose a language in the language box in the top bar of every page, or above the sign-in form. Menus, pages, buttons and messages switch at once, and for Urdu and Arabic the whole layout runs from right to left.',
          'Your choice is saved to your account, so it follows you to other computers and to the mobile app. Emails and PDF reports from the portal also use your language; official certificates keep the language of their template.',
        ],
        figs: [['flow:language/1', 'A course page in Arabic, laid out right to left'], ['flow:language/2', 'The same page in Urdu']],
        tips: ['Dates and numbers you type in forms work the same in every language.', 'A SuperAdmin chooses the default language for people who have not chosen one, and can improve the translations (Chapter 16).'],
      },
      {
        title: 'The portal layout',
        index: ['Sidebar', 'Search', 'Notifications', 'AI Assistant'],
        intro: ['Every page shares the same layout, so once you know one page you know them all.'],
        bullets: [
          '**Sidebar** (left) – the menus your role can use, grouped into sections: Overview, Setup Flow, Faculty Related, Student Related, Academic Related, Settings Related and Financial Related. The highlighted item is the current page. Click the menu button (three lines) to collapse the sidebar to icons and give the page more room.',
          '**Page header** – the page title and the workspace name.',
          '**Search** – type part of a page or feature name, such as "attendance", and click **Go** to jump straight to it.',
          '**Language** – switches the portal between English, Urdu and Arabic.',
          '**Notifications** – opens your notification inbox. A green dot means you have unread notifications.',
          '**Live now banner** – while one of your live classes is running, a banner with a **Join** button appears at the top of every page.',
          '**Profile chip** (top right) – shows your username and role; click it to open the profile menu.',
          '**AI Assistant** (bottom right) – a chat assistant that answers questions about using the portal, when the AI module is switched on.',
          '**Messages** – after you save something, a short message appears at the top right confirming success or explaining a problem. It closes by itself, or click × to close it.',
        ],
        figs: [['testadmin:Departments', 'Portal layout: sidebar, header, search, notifications and profile']],
      },
      {
        title: 'Using filters and lists',
        index: ['Filters', 'Institute filter'],
        intro: [
          'Most pages start with a row of filters, such as Institute, Campus, Department, Course and Offering. Choose them from left to right; each choice refreshes the next list and the data below.',
          'The **Institute** filter (University, School or College) appears for users who can see more than one type of institution, such as SuperAdmin. Choosing an institute limits every list and drop-down on the page to it: only that institute\'s departments, programs, courses, classes or semesters and students are offered. Switching institute clears the department, course and other choices made for the previous one. Users who belong to a single institute do not see this filter; they only ever see their own institute\'s data.',
          'Tables can be long. Use the browser\'s find feature (Ctrl+F, or Cmd+F on a Mac) to locate a name quickly, or narrow the filters.',
        ],
        figs: [
          ['flow:institute-filter/1', 'Programs with the Institute filter set to College'],
          ['flow:institute-filter/2', 'Courses with the Institute filter set to School'],
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
          'Choose **View Notifications**, **User Settings** or **Two-Factor Authentication** to open those pages, or **Install the EduSphere app** where your browser supports it.',
          'Click **Sign Out** to end your session.',
        ],
        figs: [['flow:login/2', 'The profile menu']],
        notes: [['important', 'Always sign out on a shared computer. Closing the browser tab alone may leave your session open until it times out. Signing out also removes pages saved for offline reading and stops notifications on that device.']],
      },
      {
        title: 'Installing EduSphere and getting notifications on your devices',
        index: ['Install app', 'Push notifications', 'Offline'],
        intro: [
          'The portal can be installed like an app on a phone, tablet or computer, and can send notifications (new work, results, announcements, live classes) to the device even when the portal is closed. The portal must be on a secure (https) address for both.',
        ],
        steps: [
          'Android, Windows or Mac (Chrome or Edge): open the profile menu and choose **Install the EduSphere app**, or use the install icon in the browser\'s address bar. An EduSphere icon appears with your other apps.',
          'iPhone or iPad (iOS 16.4 or later): open the portal in Safari, tap **Share** > **Add to Home Screen**, and from then on open EduSphere from its Home Screen icon (notifications only work there).',
          'Open **Notifications**. Under **Notifications on this device**, click **Turn on** and allow notifications when the browser asks.',
          'Tick the kinds of notification you want on this device, and click **Send a test** to check that it arrives.',
        ],
        figs: [['flow:push/1', 'Notifications on this device, and the notification inbox']],
        tips: [
          'Pages you opened recently, such as the dashboard, timetable, announcements, course material and notifications, can still be read while you are offline. Anything that changes data needs a connection.',
          '**Turn off** stops notifications on that device only. Notifications always remain in your inbox in the portal.',
        ],
      },
      {
        title: 'The EduSphere mobile app',
        index: ['Mobile app', 'Parent app'],
        intro: [
          'Your institution may also offer the EduSphere mobile app for Android and iPhone, from the store link it provides. Sign in with your usual username and password (and authenticator code), or with your institution\'s sign-in button, which opens the phone\'s browser and returns to the app.',
        ],
        bullets: [
          '**Students** see today\'s classes, what is due, courses and materials, results, attendance and report cards, and hand in assignments with a photo or a file. Quizzes open in the portal.',
          '**Teachers** take attendance (tap a student to switch present, absent, late or excused), grade hand-ins, post announcements and talk with the class in discussions. Home shows today\'s classes and how much work waits for grading.',
          '**Parents** see only the children linked to them: timetable, attendance, results and announcements.',
          'Choose the language and turn notifications on under **Settings** in the app. If the app asks you to update, install the newer version from the store.',
        ],
      },
      {
        title: 'Accessibility',
        index: ['Accessibility', 'Keyboard'],
        intro: ['The portal is designed to meet the WCAG 2.1 AA accessibility guidelines and passes automated accessibility checks.'],
        bullets: [
          'Every page works with the keyboard alone. Press **Tab** on any page to reveal the **Skip to main content** link, and use **Tab** / **Shift+Tab** to move between controls.',
          'Pages have proper headings, labelled form fields and descriptive buttons for screen readers, and messages are announced when they appear.',
          'Text and controls keep readable contrast in every built-in theme, and pages can be zoomed to 200% without losing content.',
          'If something is hard to use with your assistive technology, tell your institution through the Helpdesk so it can be fixed.',
        ],
        notes: [['note', 'Custom branding colours chosen by an institution are not covered by these checks. SuperAdmins should check contrast after changing them.']],
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
          'Create accounts for new students and staff in **User Import** (Chapter 14), or connect single sign-on so they use their institution accounts (Chapter 16).',
          'Enrol students in **Enrollments** (Chapter 5): choose the department and class or semester, tick the courses and students, and enroll them all at once.',
          'Check **Result Calculation** and **Grading Config** so marks become grades correctly (Chapter 10), and set up program outcomes in **Learning Outcomes** if you report on them (Chapter 9).',
          'At the end of the term, publish results, then promote students in **Student Lifecycle** (Chapter 10).',
        ],
      },
      {
        title: 'Faculty: a typical week',
        roles: 'Faculty',
        bullets: [
          'Check your classes in **Teacher Timetable** (Chapter 6) and schedule online sessions in **Live Classes** (Chapter 7).',
          'Take attendance after each class in **Enter Attendance** (Chapter 7).',
          'Build the week\'s module in **LMS Manage** – pages, links, files, assignments, quizzes, external tools and SCORM packages, with what students must do to complete each (Chapter 7) – and post notices in **Announcements**.',
          'Create **Quizzes** from your question banks, and mark **Assignments** in the grading workspace (Chapter 8).',
          'Check **Module Progress** and the outcome **Mastery gradebook** to see who needs help (Chapters 7 and 9).',
          'Enter marks in **Enter Results** and review them in the **Gradebook** (Chapter 7).',
          'Answer questions in **Discussion** and, for final-year students, supervise projects in **FYP** (Chapters 7 and 10).',
        ],
      },
      {
        title: 'Student: finding your information',
        roles: 'Student',
        bullets: [
          '**Student Timetable** and **Live Classes** – when and where your classes are, and how to join online ones (Chapters 6 and 7).',
          '**Course Content** – each course\'s modules, what you must do next, and your progress (Chapter 7).',
          '**Assignments** and **View Quizzes** – what is due, how to hand in, peer reviews to do and your feedback (Chapter 8).',
          '**Course Material** and **Announcements** – notes, files and notices from your teachers (Chapter 7).',
          '**Attendance** – your attendance percentage for each course (Chapter 7).',
          '**Results** and **My Outcomes** – your published marks and grades, and how well you have mastered each outcome (Chapters 9 and 10).',
          '**Degree Audit** and **Graduation Apply** – your progress towards your degree (Chapter 11).',
          '**Helpdesk** – ask for help from the institution (Chapter 14).',
        ],
      },
      {
        title: 'Finance: managing fees',
        roles: 'Finance',
        bullets: [
          'Create a fee receipt for each student in **Payments**, or import many at once from a CSV file (Chapter 13).',
          'Confirm receipts when payment arrives, and cancel any issued in error (Chapter 13).',
          'Run **Report Center** > **Payment Summary** for totals by status (Chapter 12).',
        ],
      },
    ],
  },
];
