// Chapters 14-17: users and communication, SuperAdmin administration, integrations and platform settings, troubleshooting.
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
        title: 'Linked sign-in accounts', roles: ALL, index: ['Linked accounts', 'Single sign-on'],
        intro: [
          'When your institution uses single sign-on, the **Two-Factor Authentication** page also shows **Linked sign-in accounts**: the institution accounts (for example Microsoft, Google or SAML) linked to your EduSphere account, with the email and the date each was linked.',
          'Click **Unlink** to remove a link, for example after changing institution accounts. You can link it again by signing in with the provider\'s button. Administrators can see and remove the links of the users they manage.',
        ],
        figs: [['flow:linked-accounts/1', 'Linked sign-in accounts']],
        notes: [['note', 'If your institution requires single sign-on, unlinking your only account may stop you signing in. Ask your administrator first.']],
      },
      {
        title: 'Notifications', roles: ALL, index: ['Notifications'],
        intro: [
          'The notification inbox collects messages from the portal, such as new announcements, published results, new assignments and quizzes, live classes being scheduled or moved, graduation decisions and replies to your helpdesk tickets. Each shows when it was sent and its type. A green dot on the **Notifications** button means you have unread items.',
          'Click **Mark read** on a notification, or **Mark All Read** to clear the indicator.',
          'At the top, **Notifications on this device** turns on notifications for the phone or computer you are using and lets you choose which kinds you receive there (see Chapter 3).',
        ],
        figs: [['flow:push/1', 'Notifications: this device and the inbox']],
      },
      {
        title: 'API Tokens', roles: 'SuperAdmin, Admin', index: ['API tokens', 'Integrations'],
        intro: [
          'Other systems, such as a student information system, a reporting tool or a script, can call the EduSphere REST API with a personal access token. A token acts as you, with your role and access, so give each integration its own token.',
        ],
        steps: [
          'Open **API Tokens**.',
          'Under **New token**, enter a **Name** (for example "Nightly SIS sync"), how many days it is **Valid for** (up to 365) and whether it is **Read only**, and click **Create token**.',
          'Copy the token straight away and store it in the other system. It is shown only once; EduSphere keeps only a fingerprint of it.',
          'Click **Revoke** when a token is no longer used, or if it may have been seen by someone else.',
        ],
        figs: [['flow:admin-integrations/4', 'API Tokens: your tokens and, for administrators, everyone\'s']],
        tips: ['Administrators see every user\'s tokens under **All tokens** and can revoke any of them.', 'Developers find the API reference on the API\'s /swagger page and in Docs/Public-API.md.'],
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
        intro: [
          'Theme Settings changes the colours of the portal for your own account, for example a dark theme for evening use. Click a theme to preview it on the page, then click **Save Theme**. **Reset Preview** returns to your saved theme, and you can go back to **Default** at any time.',
          'Every theme keeps text readable. The dark themes (Midnight Dark, Deep Navy, Graphite Pro and Slate Grey) switch pages, tables, forms, menus and pop-ups to dark backgrounds with light text, and the coloured themes keep dark text on light backgrounds.',
        ],
        figs: [
          ['flow:theme/1', 'Theme Settings with the Ocean Blue theme previewed'],
          ['flow:theme-dark/1', 'Theme Settings with the Midnight Dark theme previewed'],
          ['flow:theme-dark/2', 'The Departments page in the Midnight Dark theme'],
        ],
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
      { title: 'Module Composition', roles: 'SuperAdmin', index: ['Modules'], intro: ['Module Composition shows the module registry (22 modules, such as Attendance, Results, Reports, FYP, Single Sign-On, Learning Outcomes & Mastery, Live Classes and Similarity Checking), whether each is active, whether your role can use it, and the dashboard widgets for your role. Deactivate a module to hide its features for everyone. Modules allowed by the license are activated automatically when the license is uploaded.'], figs: [['superadmin:ModuleComposition', 'Module Composition']] },
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
    chapter: 'Integrations and Platform Settings',
    intro: [
      'The pages in this chapter connect EduSphere to other systems and set platform-wide options. They are under **Settings Related** in the sidebar and are available to SuperAdmin by default; a SuperAdmin can give Admins access to some of them in Sidebar Settings. Most need the matching module to be licensed and active in Module Composition.',
      'Secrets entered on these pages (client secrets, API keys, shared secrets) are stored encrypted and never shown again. Every change is recorded in the audit log.',
    ],
    sections: [
      {
        title: 'Single Sign-On', roles: 'SuperAdmin', index: ['Single sign-on', 'SSO', 'SAML', 'OpenID Connect'],
        intro: [
          'Single Sign-On lets people sign in with their institution account through Microsoft Entra ID, Google Workspace, any other OpenID Connect identity provider, or a SAML 2.0 identity provider (for example ADFS, Shibboleth, Okta or Keycloak). Users are matched to existing EduSphere accounts; no new accounts are created.',
          'Providers can be installation-wide (shown on the normal sign-in page) or for one tenant (shown on the sign-in address ending in ?tenant= and the tenant code). Use **Tenant code** and **Switch** at the top of the page to choose which one you are configuring.',
        ],
        before: [
          'Register EduSphere at the identity provider with the values shown under **Values to register at your identity provider**: the OpenID Connect redirect URI, or for SAML the entity ID (audience), the ACS URL and the SP metadata (**Download SP metadata**).',
          'Note the client ID and client secret (OpenID Connect) or the IdP metadata URL (SAML).',
        ],
        steps: [
          'Open **Single Sign-On** and expand the provider: **Microsoft Entra ID**, **Google Workspace**, **Other OpenID Connect IdP** or **SAML 2.0 IdP**.',
          'Enter the **Button label**, then for OpenID Connect the **Client ID**, the client secret and the authority (issuer) address, or for SAML the **IdP metadata URL**.',
          'Choose how first-time sign-ins are linked: by **verified email**, and/or by **institution ID** (the claim or attribute carrying a student\'s registration number or a staff member\'s username).',
          'Optionally enter the claim or attribute carrying roles or affiliation and the **role mappings** (for example student=Student; faculty=Faculty; staff=Admin|Finance), and tick **Refuse sign-in when the identity provider\'s role does not match**.',
          'For SAML, optionally tick **Sign authentication requests** and **Require encrypted assertions** (then download the SP metadata again and update it at the identity provider).',
          'Optionally limit the **allowed email domains**, trust the provider\'s multi-factor sign-in, or **Require single sign-on** (password sign-in off for that scope; SuperAdmins always keep password sign-in).',
          'Tick **Show this provider on the login page**, click **Save**, then **Test connection**. Finally, sign in once with a test account to confirm the details.',
        ],
        figs: [['flow:sso-settings/1', 'Single Sign-On: values to register and the providers'], ['flow:sso-settings/2', 'A SAML 2.0 provider with attribute mapping, role check, signing and encryption']],
        outcome: ['The sign-in page shows the provider\'s button (Chapter 3). Sign-ins, links and refusals are recorded in the audit log with the reason, which helps when a user reports a problem.'],
        notes: [['note', 'The SSO Setup Guide in the User Guide folder has step-by-step instructions for each provider, a roll-out checklist and troubleshooting.']],
      },
      {
        title: 'External Tools (LTI 1.3)', roles: 'SuperAdmin (Admins when granted)', index: ['External tools', 'LTI', 'Dynamic registration'],
        intro: [
          'External Tools registers learning tools that support LTI 1.3 / LTI Advantage, so teachers can add them to courses (Chapter 7). Tools can receive the class list (Names and Role Provisioning) and send grades back (Assignment and Grade Services).',
        ],
        steps: [
          'Open **External Tools**. The top of the page shows the values to give the tool vendor: issuer, client ID, deployment ID, the platform\'s keys (JWKS), authorization and token addresses.',
          'If the tool offers a registration URL, paste it under **Register with the tool\'s registration URL** and click **Start registration**; the tool registers itself and starts switched off for your review.',
          'Otherwise, under **Register a tool**, enter its name, login initiation URL, target link URI, redirect URIs, deep-linking URL and keys (JWKS URL or public key), and choose what personal data it may receive and whether it may use grades and the class list. Click **Save**.',
          'Add **deployments** to limit a tool to a department or a course, and use **Turn on** / **Turn off** to make it available.',
        ],
        figs: [['flow:lti-tools/1', 'External Tools: platform details and registered tools']],
        tips: ['Choose the least personal data a tool needs (Nothing, Name, Email, or Name and email).', 'Only a SuperAdmin can rotate the platform\'s signing key; the previous key stays published for a while so tools keep working.', 'Step-by-step instructions are in the LTI Tool Setup Guide.'],
      },
      {
        title: 'Similarity Checking', roles: 'SuperAdmin (Admins when granted)', index: ['Similarity settings', 'Copyleaks'],
        intro: [
          'Similarity Checking chooses the checker used when teachers switch on similarity checks for an assignment (Chapter 8). The **built-in comparison** needs no account and compares work with the same assignment, every assignment of the course, or the whole institution. **Copyleaks** also checks web pages and publications.',
        ],
        steps: [
          'Open **Similarity Checking**, choose the provider and, for the built-in checker, what work to compare with.',
          'For Copyleaks, enter the account email and API key, optionally tick **Sandbox**, and click **Test connection**.',
          'Click **Save**. A SuperAdmin can also set the **installation default** for institutions without their own settings.',
        ],
        figs: [['flow:admin-integrations/2', 'Similarity Checking settings']],
      },
      {
        title: 'Live Class Settings', roles: 'SuperAdmin (Admins when granted)', index: ['Live class settings', 'BigBlueButton', 'Zoom', 'Teams'],
        intro: ['Live Class Settings chooses how live classes (Chapter 7) are held.'],
        bullets: [
          '**Meeting links only** – teachers paste a link for each class; nothing to connect.',
          '**BigBlueButton** – the API address and the shared secret of your BigBlueButton server.',
          '**Zoom** – a Server-to-Server OAuth app: account ID, client ID and client secret. Zoom reports how long each participant stayed, for attendance.',
          '**Microsoft Teams** – an Entra app (tenant ID, client ID and secret) with permission to create online meetings, and the organiser\'s user ID.',
          'Set how many minutes before the start students may join, click **Save**, then **Test connection**.',
        ],
        figs: [['flow:admin-integrations/1', 'Live Class Settings']],
      },
      {
        title: 'Language & Translations', roles: 'SuperAdmin', index: ['Translations', 'Default language'],
        intro: [
          'Language & Translations sets the default language (English, Urdu, Arabic, or the browser\'s language) used for people who have not chosen one, on the sign-in page and for emails to them. Everyone can still switch languages from the top bar.',
          'It also shows how many texts are translated for each language, and lets your institution improve the translations.',
        ],
        steps: [
          'Choose the **Default language** and click **Save**.',
          'Click **Export to Excel**. The workbook has one row per English text with an Urdu and an Arabic column (and where each text is used).',
          'A translator fills in or corrects the language columns, keeping placeholders such as {0} and {1} unchanged.',
          'Choose the workbook under **Translated workbook (.xlsx)** and click **Import**. Only changed texts are stored, and the portal uses them at once.',
        ],
        figs: [['flow:language-settings/1', 'Language & Translations']],
      },
      {
        title: 'Learning Record Store (xAPI)', roles: 'SuperAdmin (Admins when granted)', index: ['xAPI', 'Learning Record Store'],
        intro: [
          'EduSphere includes a Learning Record Store for xAPI (Experience API). Activities and authoring tools that report xAPI statements, for example courses published from Articulate Rise or Storyline with xAPI tracking, can send their records to EduSphere.',
        ],
        steps: [
          'Open **Learning Record Store**.',
          'Create credentials for each activity provider: enter a name and, optionally, the course its statements belong to. The key and secret are shown once; enter them, with the endpoint shown on the page, in the activity.',
          'Switch credentials off to stop an activity from sending statements. The latest statements received are listed at the bottom of the page.',
        ],
        figs: [['flow:admin-integrations/3', 'Learning Record Store: credentials and recent statements']],
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
            ['"This account is not linked to an EduSphere user" after single sign-on.', 'EduSphere could not match the institution account by verified email or institution ID. Sign in once with your username and password, or ask an administrator to check your email or registration number, or the provider\'s linking options.'],
            ['"Your identity provider reports a role that does not match this EduSphere account".', 'The provider\'s role or affiliation for you does not map to your EduSphere role. An administrator can check the role mappings in Single Sign-On; the audit log shows the values the provider sent.'],
            ['Single sign-on fails with a general message.', 'An administrator should check the audit log (SsoLoginFailed) for the reason, for example an expired certificate, an unencrypted assertion when encryption is required, or a wrong client secret, and use Test connection on the Single Sign-On page.'],
            ['The SSO button does not appear on the sign-in page.', 'Tenant providers appear only on the sign-in address ending in ?tenant= and your tenant code. Check also that the provider is switched on.'],
          ],
        },
      },
      {
        title: 'Online learning',
        table: {
          head: ['Problem', 'What to do'], widths: [3300, 6000],
          rows: [
            ['A module or item shows a lock.', 'The lock shows why: an opening date, a module that must be completed first, or an earlier item in an "in order" module. Complete what it asks, or ask your teacher.'],
            ['I cannot find my course modules.', 'Open Course Content, choose the course in the Course box and click Open. Only published modules and items appear.'],
            ['A quiz says the access code is needed or the network is not allowed.', 'Ask your teacher for the access code. Quizzes limited to campus networks can only be started from those networks.'],
            ['My quiz score shows as pending.', 'Some answers (essays, short answers or files) are marked by the teacher. The total appears when they are marked.'],
            ['A SCORM package does not save my progress.', 'Leave the package with Exit on the player page, not by closing the tab. If the package still starts from the beginning, tell your teacher; some packages do not save a resume point.'],
            ['A part of a SCORM package is locked, or Next says it is not available yet.', 'The package sets its own order. Finish or pass the part that is open first; point at the locked part to see what it is waiting for.'],
            ['The SCORM player says "You have reached the end of this package".', 'The package has nothing more to open and your result is saved. Open the package page to see it, and start a new attempt if more are allowed.'],
            ['I cannot join a live class.', 'Join opens 15 minutes before the start. If the class uses a meeting link, the link opens the meeting service in a new tab; allow pop-ups for the portal.'],
            ['An external tool shows a blank frame or "launch expired".', 'Go back to the course and click Open again. If the frame stays blank, ask your teacher to set the tool to open in a new tab.'],
            ['Notifications do not reach my phone.', 'Turn them on under Notifications > Notifications on this device and allow them when the browser asks. On iPhone, open EduSphere from its Home Screen icon. Signing out stops notifications on that device.'],
            ['Similarity score is 0% for the first submissions.', 'There was nothing to compare with yet. Use Check again in the grading workspace after other students have handed in.'],
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
            ['A student cannot enrol in a course.', 'The enrollment summary on the Enrollments page gives the reason for each student: an unmet prerequisite, a timetable clash, a closed course or semester, or a student from another institute. A full course puts the student on the waitlist instead.'],
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
