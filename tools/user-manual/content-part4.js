// Chapters 12-14: users and communication, SuperAdmin administration, troubleshooting.
const ALL = 'SuperAdmin, Admin, Faculty, Student, Finance';

module.exports = [
  {
    chapter: 'Users, Communication and Personal Settings',
    intro: ['This chapter covers creating accounts, keeping your own details and password up to date, notifications, the helpdesk and the look of the portal.'],
    sections: [
      {
        title: 'User Import and creating users', roles: 'SuperAdmin, Admin', index: ['User Import', 'Users'],
        intro: [
          'User Import creates accounts for students, faculty, admins and finance staff, either one at a time with a form or many at once from a CSV file. Every new account starts with the username as its temporary password and must choose a new password at first sign-in.',
          'For students, the import also creates the student\'s academic profile (registration number, program and department), so they can use the student portal straight away.',
        ],
        steps: [
          'Open **User Import**.',
          'Bulk: download **faculty-admin-import-template.csv** or **students-import-template.csv**, fill it in a spreadsheet (Username, Email and Role are required), save it as CSV, choose it in **CSV File** and click **Upload and Import**. Review the import report.',
          'Single user: in **Create Single User**, enter the **Username**, **Email**, **Full Name** and **Role**. For a Faculty member choose the **Department**; for a Student choose the **Department** and **Program** and, optionally, a **Registration No.** (it defaults to the username). Mobile number, address and profile photo are optional.',
          'Click **Create User**. A message confirms the account was created, or explains why it was not (for example, the username already exists).',
        ],
        fields: [
          ['Username', 'The sign-in name. Must be unique. Also the temporary password.'],
          ['Email', 'Used for notifications and password resets.'],
          ['Role', 'Admin, Faculty, Student or Finance. SuperAdmin accounts cannot be created here.'],
          ['Department (Faculty / Student)', 'The department the person belongs to.'],
          ['Program (Student)', 'The program the student is admitted to. Choose a program of the selected department.'],
          ['Registration No. (Student)', 'Optional. The student\'s registration number; if left blank, the username in capitals is used. Must be unique.'],
          ['Campus Assignments', 'Optional. For staff who work on more than one campus: campus IDs separated by a | character.'],
        ],
        figs: [['flow:user-create/1', 'User Import'], ['flow:user-create/2', 'Create Single User form for a student, with Department and Program']],
        outcome: ['The import report shows how many rows were imported, how many were skipped as duplicates, and the reason for any errors, row by row. Fix the rows with errors and import just those rows again.'],
        notes: [['tip', 'In the students CSV template, fill DepartmentId and ProgramId (and optionally RegistrationNumber) to create each student profile during a bulk import.']],
      },
      {
        title: 'User Settings', roles: ALL, index: ['User Settings', 'Password'],
        intro: [
          'Every user can keep their own details up to date: email, mobile number, address and profile photo. This is also where you change your password.',
          'Admins see an extra user directory on the left. They can search for a user in their scope, edit the user\'s name and contact details, and reset the user\'s password to the default when someone is locked out.',
        ],
        steps: [
          'Open **User Settings** from the sidebar or the profile menu.',
          'Update your **Email**, **Mobile** or **Address** and click **Save Details**.',
          'To change your profile picture, choose an image and click **Upload Photo** (JPG or PNG, up to 2 MB).',
          'To change your password, enter your **Current password**, a **New password** and **Confirm new password**, then click **Change Password**.',
        ],
        figs: [['flow:user-settings/1', 'User Settings (Student view)'], ['testadmin:UserSettings', 'User Settings with the user directory (Admin view)']],
        tips: [
          'A new password must be at least 8 characters and different from your current one.',
          'Admins: after **Reset Password**, tell the user their temporary password; they must change it at their next sign-in.',
        ],
      },
      {
        title: 'Two-Factor Authentication', roles: ALL, index: ['Two-factor authentication', 'MFA'],
        intro: [
          'Two-factor authentication (2FA) protects your account even if someone learns your password. After you enable it, signing in needs both your password and a 6-digit code from an authenticator app on your phone, such as Microsoft Authenticator, Google Authenticator or Authy. The code changes every 30 seconds.',
        ],
        before: ['Install an authenticator app on your phone.'],
        steps: [
          'Open **Two-Factor Authentication** from the profile menu.',
          'Click **Start 2FA Setup**. A QR code and a manual key appear.',
          'In your authenticator app, add an account and scan the QR code (or type the manual key).',
          'Enter the 6-digit code shown in the app to verify and enable 2FA.',
          'Write down the recovery codes shown and keep them somewhere safe. Each can be used once instead of a code if you lose your phone.',
          'To turn 2FA off later, return to this page, enter a current code and disable it. The old secret is deleted, so setting it up again needs a new QR code.',
        ],
        figs: [['testadmin:TwoFactorSettings', 'Two-Factor Authentication']],
        notes: [['important', 'If you lose your phone and your recovery codes, an administrator must reset two-factor authentication for you.']],
      },
      {
        title: 'Notifications', roles: ALL, index: ['Notifications'],
        intro: [
          'The notification inbox collects messages from the portal, such as new announcements, published results, graduation decisions and replies to your helpdesk tickets. A green dot on the **Notifications** button means you have unread items.',
          'Click a notification to mark it as read, or click **Mark All Read** to clear the indicator.',
        ],
        figs: [['testadmin:Notifications', 'Notifications inbox']],
      },
      {
        title: 'Helpdesk', roles: ALL, index: ['Helpdesk', 'Support tickets'],
        intro: [
          'Helpdesk is the place to ask the institution for help, for example about a timetable clash, a missing mark or a sign-in problem. Each request is a ticket that staff answer and track until it is resolved. All messages in a ticket are kept together.',
        ],
        steps: [
          'Open **Helpdesk**. Use the status tabs (All, Open, InProgress, Resolved, Closed) to filter your tickets.',
          'Click **New Ticket**.',
          'Choose the **Department** (optional) and **Category**, enter a short **Subject** and the **Details**, and click **Submit Ticket**.',
          'Open a ticket with **View** to read replies and respond.',
        ],
        fields: [
          ['Category', 'Academic (courses, marks, timetables), Technical (sign-in or portal problems) or Administrative (fees, documents, records).'],
          ['Subject', 'A one-line summary, for example "CS201 midterm mark missing".'],
          ['Details', 'Include what you were doing, what you expected and what happened. Screenshots help.'],
        ],
        figs: [['flow:student-helpdesk/1', 'Helpdesk ticket queue'], ['flow:student-helpdesk/2', 'New Support Ticket form']],
        tables: [{
          title: 'Ticket statuses', head: ['Status', 'Meaning'], widths: [2200, 7100], rows: [
            ['Open', 'New; waiting for staff.'],
            ['In Progress', 'A staff member is working on it.'],
            ['Resolved', 'Staff believe the issue is fixed. Reply if it is not.'],
            ['Closed', 'Finished. Open a new ticket for a new issue.'],
          ],
        }],
      },
      {
        title: 'Theme Settings', roles: ALL, index: ['Theme'],
        intro: ['Theme Settings changes the colours of the portal for your own account, for example a dark theme for evening use. Click a theme to preview it on the page, then save it. You can return to **Default** at any time.'],
        figs: [['flow:theme/1', 'Theme Settings with a theme previewed']],
      },
    ],
  },

  {
    chapter: 'SuperAdmin Administration',
    intro: ['The pages in this chapter are available only to SuperAdmin users. They control the whole installation, so changes here affect every user.'],
    sections: [
      { title: 'Dashboard', roles: 'SuperAdmin', index: ['Dashboard'], intro: ['The SuperAdmin Dashboard explains the workspace modes and holds the API connection settings (API Base URL, access token and default department). These settings connect the portal to its back-end service. Change them only on instruction from your technical team.'], figs: [['superadmin:Dashboard', 'SuperAdmin Dashboard']] },
      {
        title: 'Tenant and Campus Management', roles: 'SuperAdmin', index: ['Tenants', 'Campuses'],
        intro: ['A tenant is a separate institution in the installation, for example the University, the College and the School, each with its own users and data. Each tenant has one or more campuses.'],
        steps: [
          'In **Tenant Management**, enter a **Code** and **Name** and click **Create Tenant**.',
          'Click **Manage Campuses**, choose the tenant, enter the campus **Code** and **Name**, and click **Create**.',
          'Use **Save** to rename, and **Deactivate** for tenants or campuses no longer in use. Deactivated items are hidden from all filters.',
        ],
        figs: [['superadmin:TenantManagement', 'Tenant Management'], ['superadmin:CampusManagement', 'Campus Management']],
      },
      {
        title: 'Admin Users', roles: 'SuperAdmin', index: ['Admin Users'],
        intro: ['Admin Users creates Admin accounts and decides which departments each Admin manages. An Admin sees and manages only the departments assigned to them.'],
        steps: [
          'Enter the **Username**, **Email** and a **Temporary Password**, and optionally an **Institution Type**.',
          'Tick the **Initial Department Assignments** (use **Select All** or **Clear** to speed this up) and click **Create Admin**.',
          'To change an existing Admin, search for and select them, update their details and department assignments, and click **Save Changes**.',
        ],
        figs: [['superadmin:AdminUsers', 'Admin Users']],
      },
      { title: 'Module Composition', roles: 'SuperAdmin', index: ['Modules'], intro: ['Module Composition shows the module registry (17 modules, such as Attendance, Results, Reports and FYP), whether each is active, whether your role can use it, and the dashboard widgets for your role. Deactivate a module to hide its features for everyone. Modules allowed by the license are activated automatically when the license is uploaded.'], figs: [['superadmin:ModuleComposition', 'Module Composition']] },
      { title: 'Sidebar Settings', roles: 'SuperAdmin', index: ['Sidebar Settings'], intro: ['Sidebar Settings controls which menu items Admin, Faculty and Student users see. Each row is a menu with its purpose and a switch for each role. SuperAdmin always keeps full access, and menus marked System cannot be switched off. Appendix A shows the default settings.'], figs: [['superadmin:SidebarSettings', 'Sidebar Settings']] },
      { title: 'Report Settings', roles: 'SuperAdmin', index: ['Report Settings'], intro: ['Report Settings lists the report definitions (nine by default). Here you can create a new definition, switch a report on or off, and choose which roles may run it. Report Center then shows each user only the reports allowed for their role.'], figs: [['superadmin:ReportSettings', 'Report Settings']] },
      {
        title: 'Dashboard Settings (Branding)', roles: 'SuperAdmin', index: ['Branding'],
        intro: ['Dashboard Settings controls how the portal presents your institution.'],
        fields: [
          ['University / Institution Name', 'Shown in the sidebar and on generated documents.'],
          ['Portal Subtitle', 'The line under the name, for example "Campus Portal".'],
          ['Footer Text', 'Text shown at the bottom of every page.'],
          ['Portal Logo', 'Upload your institution\'s logo (shown in the sidebar). If none is uploaded, the Tabsan emblem is used.'],
          ['Privacy Policy URL / Text', 'A link to, or the text of, your privacy policy.'],
          ['Font Family / Font Size', 'The portal\'s main font and base text size.'],
        ],
        steps: ['Update the fields you want to change.', 'Click **Save Branding**. The changes apply to all users at their next page load.'],
        figs: [['superadmin:DashboardSettings', 'Dashboard Settings – portal branding']],
      },
      { title: 'Institution Policy', roles: 'SuperAdmin', index: ['Institution Policy'], intro: ['Institution Policy switches the University, School and College institution types on or off for the installation, within what the license allows. Switching a type off hides its departments, menus and reports. Tick the types in use and click **Save Policy**.'], figs: [['superadmin:InstitutionPolicy', 'Institution Policy']] },
      {
        title: 'License Update', roles: 'SuperAdmin', index: ['License'],
        intro: ['License Update shows the current product license (status, type, activation date and expiry date) and lets you upload a new or renewed license file supplied by Tabsan.'],
        steps: ['Open **License Update**.', 'Under **Upload License File**, choose the .tablic file supplied by Tabsan.', 'Click **Upload & Activate**. The Current License table updates and the license status shows Active.'],
        figs: [['flow:license/1', 'License Update']],
        tables: [{
          title: 'License statuses', head: ['Status', 'Meaning'], widths: [2200, 7100], rows: [
            ['Active', 'The license is valid; the portal works normally.'],
            ['Expired', 'The expiry date has passed; the portal is read-only until a new license is uploaded.'],
            ['Invalid', 'No license, or the stored license failed its security checks; the portal is read-only.'],
          ],
        }],
        notes: [['important', 'License files are encrypted and digitally signed. A file that has been edited in any way is rejected. The expiry date is fixed inside the file and is the same on every server it is activated on. Each license file can be activated only once.']],
      },
      { title: 'Library Config', roles: 'SuperAdmin', index: ['Library'], intro: ['Library Config connects the portal to an external library system so users can search the catalogue and see loans. Enter the **Catalogue URL**, **Loan API URL** and **API Token** supplied by the library system, then click **Save Configuration**.'], figs: [['superadmin:LibraryConfig', 'Library Config']] },
      { title: 'Accreditation', roles: 'SuperAdmin', index: ['Accreditation'], intro: ['Accreditation helps prepare documents for accreditation bodies. Download the simple template, complete it with your institution\'s information, and upload it back to keep it with your records.'], figs: [['superadmin:AccreditationTemplates', 'Accreditation templates']] },
      {
        title: 'Advanced Audit', roles: 'SuperAdmin', index: ['Audit logs'],
        intro: ['The audit log records who did what and when, including sign-ins, changes to records and settings, with the before and after values. Use it to investigate a problem or to answer compliance questions.'],
        steps: [
          'Filter by any combination of **Search** text, **Actor User Id**, **Action**, **Module (Entity)** and a **From**/**To** date range (UTC), choose how many **Rows** to show, and click **Apply Filters**.',
          'Use **Previous** and **Next** to page through the results. Your filters are kept.',
          'Click **Export CSV**, **Export Excel** or **Export PDF** to download the filtered log.',
        ],
        figs: [['superadmin:AuditLogs', 'Audit Logs']],
      },
    ],
  },

  {
    chapter: 'Troubleshooting and FAQ',
    sections: [
      {
        title: 'Signing in and accounts',
        table: {
          head: ['Problem', 'What to do'], widths: [3300, 6000],
          rows: [
            ['I cannot sign in.', 'Check the username and that Caps Lock is off. If you have forgotten your password, ask an Admin to reset it from User Settings.'],
            ['I am asked to change my password.', 'This happens at first sign-in and when your password expires (every 90 days by default). Choose a new password of at least 8 characters.'],
            ['I was signed out unexpectedly.', 'Sessions end after a period of inactivity. Sign in again; save your work regularly on long forms.'],
            ['I lost my authenticator device.', 'Use one of your recovery codes to sign in, or ask an administrator to reset two-factor authentication.'],
            ['A new student cannot see their courses.', 'Check that the student was created with a Program (so a student profile exists) and has been enrolled in the term\'s offerings.'],
          ],
        },
      },
      {
        title: 'Pages and data',
        table: {
          head: ['Problem', 'What to do'], widths: [3300, 6000],
          rows: [
            ['I was returned to the Helpdesk with a message.', 'The page is not available to your role. The message explains why. Ask your administrator if you need access.'],
            ['A list is empty.', 'Check the filters at the top of the page. Most pages need a department, course or offering selected first.'],
            ['Save buttons return "read-only mode".', 'The license is missing or expired. A SuperAdmin must upload a valid license in License Update.'],
            ['A student cannot enrol in a course.', 'The course may have a prerequisite the student has not passed, the offering may be full (the student is waitlisted), or enrolment may be closed for the campus.'],
            ['Results are not visible to students.', 'Results appear only after the teacher clicks Publish All in Enter Results.'],
            ['A student cannot apply for graduation.', 'Only active University students in their final semester with a completed FYP can apply. Check Degree Audit for unmet requirements.'],
            ['Degree Audit says no degree rule is configured.', 'A SuperAdmin must create a degree rule for the student\'s program in Degree Rules.'],
            ['A report has no export buttons.', 'Choose all required filters and click Filter first; some reports need a semester or a student.'],
            ['Exported files do not open.', 'Make sure the download finished. CSV files open in Excel; PDF files need a PDF reader.'],
          ],
        },
      },
      {
        title: 'Getting more help',
        intro: [
          'If your question is not answered here, raise a ticket in **Helpdesk** with the Technical category, describing what you were doing and what you saw. Include the page name and, if possible, a screenshot. Your administrator can also check the audit log to see exactly what happened.',
        ],
      },
    ],
  },
];
