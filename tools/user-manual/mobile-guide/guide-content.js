// Text of the Tabsan EduSphere Mobile App Connection Guide (built by build-guide.js).
// Images: phone:<name> = shots/phone/<name>.png (taken from the APK on an Android phone), portal:<name> = shots/portal,
// diagram:<name> = shots/diagram. **bold** and `code` work in all text.

const VERSION = '1.0';
const DATE = '30 October 2026';

module.exports = {
  VERSION, DATE,
  cover: ['phone:a03-signin', 'phone:s01-home'],

  about: [
    'This guide explains how the Tabsan EduSphere mobile app connects to the EduSphere web portal, and walks through everything needed to put the app in the hands of students, teachers and parents: preparing the server, building and distributing the app, preparing accounts in the portal, signing in, and using the app day to day.',
    'Every app screenshot in this guide was taken from the Android app package (APK) running on a Samsung Galaxy Z Fold5 (cover screen, Android 16, dark mode), connected to an EduSphere test server with the School demonstration data (Tabsan Model School, Gulberg Campus). Portal screenshots come from the same server at the same time, so the app and the portal show the same records. In light mode the app shows white cards on a light grey background; everything else is the same.',
  ],
  aboutTable: [
    ['Product', 'Tabsan EduSphere mobile app (Android and iOS) and Campus Portal'],
    ['Document', 'Mobile App Connection Guide'],
    ['Version', `${VERSION} — ${DATE}`],
    ['App version', '1.0.0 (Tabsan branding)'],
    ['Audience', 'IT administrators (chapters 2–3), portal administrators (chapter 4), students, teachers and parents (chapters 5–8)'],
    ['Related documents', 'Tabsan EduSphere User Manual · SSO Setup Guide · mobile/README.md (developers)'],
    ['Publisher', 'Tabsan Nexora'],
  ],
  aboutNote: 'Your institution\'s app may carry its own name, logo and colour (a white-label build, chapter 3). The screens, menus and steps are the same.',

  chapters: [
    // ── 1 ─────────────────────────────────────────────────────────────────────
    {
      chapter: 'How the App Connects to the Portal',
      intro: ['The mobile app is not a separate system. It is a second way into the same EduSphere server that runs your web portal. Understanding this makes every later step obvious: the app needs to know the server\'s address, and users sign in with the accounts they already have.'],
      sections: [
        {
          title: 'One server, two ways in',
          body: [
            'The web portal and the mobile app both talk to the **EduSphere API** on your server. The API reads and writes one database. When a teacher takes attendance in the app, the portal shows it at once; when an administrator publishes results in the portal, students see them in the app the next time the screen refreshes. Nothing is copied or synchronised between the two, so they can never disagree.',
            { fig: 'diagram:connection', caption: 'The portal and the app use the same API, database and accounts' },
            { h3: 'What the phone keeps', bullets: [
              'The sign-in session (an access token and a refresh token) in the phone\'s secure storage (Android Keystore, iOS Keychain), so users stay signed in until they sign out.',
              'The chosen language, and work that has not been sent yet (for example an unfinished answer on the hand-in screen while it is open).',
              'No copies of results, attendance or other records. Every screen is loaded from the server when it opens and when the user pulls down to refresh.',
            ] },
          ],
        },
        {
          title: 'What connects to what',
          body: [
            { table: { head: ['Connection', 'How', 'Set up in'], widths: [2600, 4200, 2500], rows: [
              ['App → API', 'HTTPS calls to `https://<your API>/api/v1/...` (sign-in, home, timetable, courses, results, notifications …)', 'The app build (API address), chapter 3'],
              ['App → portal', 'The phone\'s browser opens the portal for single sign-on and for quizzes (exam settings and timing stay in the portal)', 'API setting `Mobile:PortalBaseUrl`, chapter 2'],
              ['Portal → app', 'After single sign-on the portal hands a one-time code back to the app through its address scheme (`edusphere://sso`)', '`Mobile:AppRedirectUri` on the API and the portal, chapter 2'],
              ['API → phones', 'Push notifications through Firebase Cloud Messaging (optional)', 'Firebase project, app build and API, section 4.6'],
              ['Portal → app branding', 'The institution name and subtitle from the portal\'s Dashboard Settings appear on the app\'s sign-in screen', 'Portal, section 4.1'],
            ] } },
          ],
        },
        {
          title: 'Who does what',
          body: [
            { table: { head: ['Role', 'Tasks', 'Chapter'], widths: [2300, 5300, 1700], rows: [
              ['IT administrator', 'Publishes the API over HTTPS, sets the `Mobile` settings, builds the app with the API address, publishes it to the stores or shares the APK, optionally sets up push notifications', '2, 3, 4.6'],
              ['Portal administrator', 'Sets the institution name, creates parent accounts, links parents to their children, optionally sets up single sign-on', '4'],
              ['Students, teachers, parents', 'Install the app and sign in with their portal account', '5–8'],
            ] } },
          ],
        },
        {
          title: 'Checklist',
          body: [
            'Work through these steps in order. Each one names the section that explains it.',
            { steps: [
              'The API is reachable from the internet over HTTPS with a trusted certificate (section 2.1).',
              'The `Mobile` settings are filled in on the API, and the portal uses the same return address (sections 2.2–2.3).',
              '`https://<your API>/api/v1/mobile/config` opens in a browser and shows your institution name (section 2.4).',
              'The app is built with your API address and published, or the APK is shared for a pilot (chapter 3).',
              'The institution name and subtitle are right in the portal (section 4.1).',
              'Parent accounts are created and linked to their children, if parents will use the app (sections 4.3–4.4).',
              'A student, a teacher and a parent sign in on a phone and see their own data (chapter 5).',
            ] },
          ],
        },
      ],
    },

    // ── 2 ─────────────────────────────────────────────────────────────────────
    {
      chapter: 'Prepare the Server',
      intro: ['The app talks only to the EduSphere API. This chapter makes sure phones can reach it securely and tells the API how the app signs in and where the portal is.'],
      sections: [
        {
          title: 'Requirements',
          who: 'IT administrator',
          body: [
            { bullets: [
              'The **API** must be reachable from phones on mobile data and on any Wi-Fi, by a public name such as `https://api.your-school.edu.pk`. Addresses like `localhost` or a private IP address only work inside your network.',
              'It must use **HTTPS with a certificate trusted by phones** (for example from Let\'s Encrypt or your certificate provider). Store builds of the app refuse plain `http://` addresses and untrusted certificates.',
              'The **portal** must also be reachable from phones if you use single sign-on or quizzes, because the app opens it in the phone\'s browser.',
              'The server clock must be correct: sign-in tokens and single sign-on codes expire after a short time.',
            ] },
            { note: ['important', 'Do not open the API to the internet over plain HTTP. Passwords and tokens would travel unencrypted. Test builds may use HTTP on a private network only (section 3.6).'] },
          ],
        },
        {
          title: 'The Mobile settings on the API',
          who: 'IT administrator',
          body: [
            'Open `appsettings.json` (or `appsettings.Production.json`) of the **API** and fill in the `Mobile` section. Restart the API after saving.',
            { code: [
              '"Mobile": {',
              '  "MinimumVersion": "1.0.0",',
              '  "LatestVersion": "1.0.0",',
              '  "AndroidStoreUrl": "https://play.google.com/store/apps/details?id=pk.edu.yourschool.app",',
              '  "IosStoreUrl": "https://apps.apple.com/app/id0000000000",',
              '  "PortalBaseUrl": "https://portal.your-school.edu.pk",',
              '  "AppRedirectUri": "edusphere://sso",',
              '  "HandoffSeconds": 120,',
              '  "MaxConsecutiveFailures": 5,',
              '  "Fcm": { "Enabled": false, "ProjectId": null, "ServiceAccountFile": null }',
              '}',
            ] },
            { table: { head: ['Setting', 'What it does', 'Typical value'], widths: [2300, 4900, 2100], rows: [
              ['MinimumVersion', 'Oldest app version allowed to sign in. Older apps show "This version of the app is no longer supported. Please update it." with an **Update** button.', 'The version all users have'],
              ['LatestVersion', 'Newest published version. Older apps show "A new version of the app is available."', 'Your latest store version'],
              ['AndroidStoreUrl, IosStoreUrl', 'Where the **Update** button sends users.', 'Your store listings'],
              ['PortalBaseUrl', 'The portal address the app opens for single sign-on and for quizzes.', '`https://portal.…`'],
              ['AppRedirectUri', 'Where the portal returns the one-time sign-in code after single sign-on. Must match the app\'s address scheme (section 3.2).', '`edusphere://sso`'],
              ['HandoffSeconds', 'How long that one-time code stays valid (30–600 seconds).', '120'],
              ['MaxConsecutiveFailures', 'Failed push deliveries in a row after which a phone stops receiving push.', '5'],
              ['Fcm', 'Push notifications through Firebase (section 4.6).', 'Disabled until set up'],
            ] } },
          ],
        },
        {
          title: 'The matching setting on the portal',
          who: 'IT administrator',
          body: [
            'The portal finishes single sign-on for the app, so it needs the same return address. In the **portal\'s** `appsettings.json`:',
            { code: [
              '"EduApi": { "BaseUrl": "https://api.your-school.edu.pk" },',
              '"Mobile": { "AppRedirectUri": "edusphere://sso" }',
            ] },
            { note: ['important', 'If `AppRedirectUri` differs between the API, the portal and the app build, single sign-on ends in the browser instead of returning to the app. Password sign-in is not affected.'] },
          ],
        },
        {
          title: 'Check the connection',
          who: 'IT administrator',
          body: [
            'The app reads its settings from the API before anyone signs in. Open this address in any browser, on a phone if possible, using mobile data rather than the school Wi-Fi:',
            { code: ['https://api.your-school.edu.pk/api/v1/mobile/config'] },
            'You should see your institution\'s settings. If the page does not open, phones cannot reach the API: check the DNS name, the firewall and the certificate before going further.',
            { fig: 'portal:p06-mobile-config', caption: 'The configuration the app reads (from the test server used for this guide)' },
            { table: { head: ['Field', 'Comes from'], widths: [2600, 6700], rows: [
              ['institutionName, subtitle', 'Portal › Dashboard Settings › Portal Branding (section 4.1). Shown on the app\'s sign-in screen.'],
              ['minimumVersion, latestVersion, store URLs', 'The `Mobile` settings (section 2.2).'],
              ['portalBaseUrl, appRedirectUri', 'The `Mobile` settings. On the test server `portalBaseUrl` is empty; on your server it must be the portal address.'],
              ['languages, defaultLanguage', 'The portal\'s languages: English, Urdu and Arabic.'],
              ['ssoProviders', 'Single sign-on set up in the portal (section 4.5). Each provider becomes a button on the sign-in screen.'],
              ['pushEnabled', '`true` when Firebase is set up on the API (section 4.6).'],
            ] } },
          ],
        },
      ],
    },

    // ── 3 ─────────────────────────────────────────────────────────────────────
    {
      chapter: 'Build and Distribute the App',
      intro: ['The API address is built into the app, so users never have to type a server address. Each institution gets its own build with its name, address and, if wanted, its own logo and colour.'],
      sections: [
        {
          title: 'What a build contains',
          who: 'IT administrator',
          body: [
            { table: { head: ['Property', 'Meaning', 'Default'], widths: [2000, 5100, 2200], rows: [
              ['App name', 'Name under the icon and in the stores.', 'EduSphere'],
              ['App id', 'Unique store id, for example `pk.edu.yourschool.app`. Never change it after the first release.', 'com.tabsan.edusphere'],
              ['API address', 'The HTTPS address of your API (chapter 2). The app connects only to this server.', '—'],
              ['Colour', 'Title bar, banners, icon background and splash screen.', '#0F2A4A (Tabsan navy)'],
              ['Scheme', 'The address scheme for the single sign-on return (`<scheme>://sso`).', 'edusphere'],
              ['Version, build', 'Version shown to users (1.2.0) and a build number that grows with every store upload.', '—'],
              ['Logo', 'App icon, splash screen and the logo on the sign-in screen.', 'Tabsan logo'],
            ] } },
          ],
        },
        {
          title: 'Build for your institution',
          who: 'IT administrator (on the build computer: .NET 9 SDK with the MAUI workload, Android SDK, JDK 17; a Mac with Xcode for iOS)',
          body: [
            'Run the white-label build script from the `mobile` folder of the EduSphere source:',
            { code: [
              './build-whitelabel.ps1 -AppName "Tabsan Model School" -AppId pk.edu.tms.app `',
              '    -ApiBaseUrl https://api.your-school.edu.pk -Scheme edusphere `',
              '    -Version 1.0.0 -Build 1 -KeyStore ./tms/upload.keystore -KeyAlias upload',
            ] },
            { table: { head: ['Parameter', 'Required', 'Notes'], widths: [2300, 1200, 5800], rows: [
              ['-AppName', 'Yes', 'Name under the icon.'],
              ['-AppId', 'Yes', 'Store id (reverse domain name).'],
              ['-ApiBaseUrl', 'Yes', 'Must start with `https://`.'],
              ['-Color', 'No', 'Hex colour, for example `#7A1F2B`. Default Tabsan navy.'],
              ['-Scheme', 'No', 'Lower-case letters; must match `Mobile:AppRedirectUri` on the API and portal (`<scheme>://sso`).'],
              ['-Version, -Build', 'Yes', 'For example `1.2.0` and `12`.'],
              ['-Platform', 'No', '`android` (default) or `ios` (on a Mac).'],
              ['-GoogleServicesJson, -GoogleServiceInfoPlist', 'No', 'Firebase files for push notifications (section 4.6). Without them the app works without push.'],
              ['-KeyStore, -KeyAlias', 'For Google Play', 'Upload key. Passwords are read from the `EDUSPHERE_KEYSTORE_PASS` and `EDUSPHERE_KEY_PASS` environment variables, never typed on the command line.'],
              ['-Apk', 'No', 'Produce an installable `.apk` instead of a Google Play bundle (`.aab`).'],
            ] } },
            'The result is written to `mobile/artifacts/<AppId>/<Version>-<Build>/`.',
          ],
        },
        {
          title: 'Logo, icon and splash screen',
          who: 'IT administrator',
          body: [
            'The standard build carries the Tabsan logo: the TS emblem as the app icon on a navy background, the emblem on the splash screen, and the full Tabsan logo at the top of the sign-in screen. To use your institution\'s artwork, replace these files before building and keep their names:',
            { table: { head: ['File (under mobile/Tabsan.EduSphere.Mobile/Resources)', 'Used for'], widths: [5200, 4100], rows: [
              ['AppIcon/appicon.svg', 'Icon background'],
              ['AppIcon/appiconfg.png', 'Icon foreground: the emblem, inside the middle two thirds of a transparent square'],
              ['Splash/splash.png', 'Splash screen emblem (shown on the brand colour)'],
              ['Images/tabsan_wordmark.png', 'Logo at the top of the sign-in screen (light artwork for a dark background)'],
              ['Images/tabsan_mark.png', 'Emblem on the banners, the start screen and in Settings'],
            ] } },
          ],
        },
        {
          title: 'Distribute the app',
          who: 'IT administrator',
          body: [
            { bullets: [
              '**Google Play**: upload the `.aab` to your Google Play Console listing. Publish it to an internal testing track first, then to production.',
              '**Apple App Store**: upload the `.ipa` from the Mac with Xcode or Transporter, test it with TestFlight, then submit it for review.',
              '**Pilot without a store**: build with `-Apk` and share the file through your institution\'s own channels. On Android, users allow "Install unknown apps" for the app they open the file with.',
              'After publishing, put the store links into `AndroidStoreUrl` and `IosStoreUrl` (section 2.2) so the **Update** button works.',
            ] },
            { note: ['tip', 'Raise `MinimumVersion` only after the new version is available in both stores; otherwise users are asked to update to a version they cannot get yet.'] },
          ],
        },
        {
          title: 'Updates and versions',
          who: 'IT administrator',
          body: [
            { table: { head: ['Change', 'Do this', 'Users see'], widths: [2600, 3700, 3000], rows: [
              ['New app version published', 'Raise `LatestVersion`', '"A new version of the app is available." with an **Update** button'],
              ['Old versions must stop working', 'Raise `MinimumVersion`', '"This version of the app is no longer supported. Please update it."'],
              ['Server address changes', 'New build with the new `-ApiBaseUrl`', 'Users install the update; their sign-in starts again'],
            ] } },
          ],
        },
        {
          title: 'Test before going live',
          who: 'IT administrator or developer',
          body: [
            'For testing, a debug build can use a test server over plain HTTP on your own network. This is how the screenshots in this guide were made: the APK was installed on an Android phone connected to the build computer, and the phone reached the test API through the debugging connection.',
            { code: [
              'dotnet build Tabsan.EduSphere.Mobile/Tabsan.EduSphere.Mobile.csproj -c Debug -p:MobileTargets=net9.0-android `',
              '    -p:RuntimeIdentifier=android-arm64 -p:EmbedAssembliesIntoApk=true -p:AndroidPackageFormat=apk `',
              '    -p:ApiBaseUrl=http://localhost:5182',
              'adb reverse tcp:5182 tcp:5182      # the phone\'s localhost:5182 is the build computer\'s API',
              'adb install -r bin/Debug/net9.0-android/android-arm64/com.tabsan.edusphere-Signed.apk',
            ] },
            { note: ['warning', 'Debug builds accept HTTP and are signed with a debug key. Never give them to users; use the white-label build (section 3.2) for anything real.'] },
          ],
        },
      ],
    },

    // ── 4 ─────────────────────────────────────────────────────────────────────
    {
      chapter: 'Prepare the Portal',
      intro: ['Students and teachers already have everything they need: the app uses their portal accounts. This chapter covers the institution name the app shows, parent accounts, and the optional single sign-on and push notifications.'],
      sections: [
        {
          title: 'Institution name on the app',
          who: 'SuperAdmin or Admin in the portal',
          body: [
            'The sign-in screen of the app shows the name and subtitle from the portal\'s branding, so users can see they are connecting to the right institution.',
            { steps: [
              'Sign in to the portal and open **Dashboard Settings** (in the **Settings Related** group of the menu).',
              'Under **Portal Branding**, enter the **University / Institution Name** and the **Portal Subtitle**.',
              'Select **Save**. The app shows the new name the next time its sign-in screen opens.',
            ] },
            { fig: 'portal:p02-branding', caption: 'Portal › Dashboard Settings: the name and subtitle the app shows (outlined)' },
          ],
        },
        {
          title: 'Students and teachers',
          who: 'No action needed',
          body: [
            'Students and teachers sign in to the app with **the same username and password as the portal**. There is nothing to create or enable.',
            { bullets: [
              'A password changed in the portal applies in the app straight away, and the other way round.',
              'An account that is locked or deactivated in the portal cannot sign in to the app either.',
              'If two-factor authentication is switched on for an account, the app asks for the authenticator code too.',
              'Students see the courses they are enrolled in and teachers the courses they teach, exactly as in the portal.',
            ] },
            { fig: 'portal:p01-portal-login', caption: 'The portal\'s sign-in page: the same username and password open the app (compare Figure 5.1)' },
          ],
        },
        {
          title: 'Create parent accounts',
          who: 'SuperAdmin or Admin in the portal',
          body: [
            'Parents use the app to follow their children\'s results, attendance, fees and announcements. Parent accounts are for **School** students and have the role **Parent**.',
            { steps: [
              'In the portal, open **User Import**.',
              'For one parent, use **Create Single User**: enter the **Username**, **Email** and **Full Name**, and choose the role **Parent**. Select **Create User**.',
              'For many parents, upload a CSV file under **Import Users From CSV** with `Parent` in the Role column.',
              'Give each parent their username. Their first password is the **username** itself; the app asks them to choose their own password at the first sign-in (section 5.4).',
            ] },
            { fig: 'portal:p04-single-user-parent', caption: 'Creating a parent account (role Parent outlined)' },
            { fig: 'portal:p03-user-import', caption: 'User Import, with the page tip listing the role values and the temporary password rule' },
          ],
        },
        {
          title: 'Link parents to their children',
          who: 'SuperAdmin or Admin in the portal',
          body: [
            'A parent sees only the children linked to their account. Links are made on the portal\'s **Parent Links** screen. One parent can have several children, and a child can have several parents (for example the father and the mother, each with their own account).',
            { steps: [
              'In the portal, open **Parent Links** (in the **Settings Related** group of the menu, next to **User Import**).',
              'Under **1. Find the parent**, type part of the parent\'s username, name, email or phone and select **Search**. Select the parent in the list; the number beside each name is how many children are linked already.',
              'Under **2. Link a student**, search by the student\'s registration number or name (for example `TMS-GUL-09-01` or `Fatima`).',
              'In the student\'s row, choose the **Relationship** (Father, Mother, Guardian, Grandparent, Sibling or Other) and select **Link**. Repeat for each child.',
            ] },
            { fig: 'portal:p08-parent-links', caption: 'Portal › Parent Links: the parent found by name (left), their children, and the student search (both search boxes outlined)' },
            'The children appear under **Children of …** with their class and relationship. The parent sees them in the app the next time its home screen loads (open the app again, or pull the screen down to refresh).',
            { table: { head: ['To…', 'On the parent\'s row for the child'], widths: [2800, 6500], rows: [
              ['Correct the relationship', 'Choose another value and select **Save**.'],
              ['Hide a child for a while', 'Select **Pause**. The link stays on the list as **Paused** and the parent no longer sees the child; **Resume** shows the child again.'],
              ['End the link', 'Select **Remove** and confirm. The parent stops seeing the child at once. Linking the same child again later is allowed.'],
            ] } },
            { note: ['important', 'Only **School** students can be linked, and only to accounts with the role **Parent** (section 4.3). An Admin sees and links only the students of the departments assigned to them, and only the parent accounts of their own school (any campus); a SuperAdmin sees all. If **Parent Links** is missing from the menu, the installation has no active School department yet, or the menu is switched off for Admins in **Sidebar Settings**.'] },
            { note: ['tip', 'A SuperAdmin who creates parent accounts for a school on **User Import** should first choose that school\'s **Tenant** and **Campus** at the top of the page. Otherwise the parents belong to the default tenant and the school\'s own Admins cannot find them on **Parent Links** (the SuperAdmin can still link them).'] },
            { h3: 'For integrations', text: ['A script (for example a nightly import from the admissions system) can make the same links through the API with an Admin token; the same rules apply.'] },
            { table: { head: ['Request', 'Purpose'], widths: [4900, 4400], rows: [
              ['GET `/api/v1/parent-portal/parents?search=`', 'Find Parent accounts by username, name, email or phone'],
              ['GET `/api/v1/parent-portal/students?search=`', 'Find School students by registration number or name'],
              ['GET `/api/v1/parent-portal/parents/{parentUserId}/links`', 'A parent\'s children, with names, classes and relationships'],
              ['PUT `/api/v1/parent-portal/links`', 'Create or update a link (`relationship`; `isActive = false` pauses it)'],
              ['DELETE `/api/v1/parent-portal/links/{parentUserId}/{studentProfileId}?remove=true`', 'Remove a link (without `remove=true`, the link is paused)'],
            ] } },
          ],
        },
        {
          title: 'Single sign-on (optional)',
          who: 'SuperAdmin',
          body: [
            'If your institution signs in with Microsoft, Google, OpenID Connect or SAML, set it up in the portal as described in the **SSO Setup Guide**. The app then shows a **Sign in with …** button for each provider, under the password fields.',
            { steps: [
              'The user taps **Sign in with …**. The phone\'s browser opens the portal\'s single sign-on page.',
              'The user signs in with the institution\'s account, as on a computer.',
              'The portal hands a one-time code back to the app (`edusphere://sso`); the app exchanges it with the API for its own session. The code works once and expires after `HandoffSeconds`.',
            ] },
            'Single sign-on in the app needs `PortalBaseUrl` and the same `AppRedirectUri` on the API, the portal and the app build (sections 2.2, 2.3 and 3.2).',
          ],
        },
        {
          title: 'Push notifications (optional)',
          who: 'IT administrator',
          body: [
            'Without push the app works fully; users see new items in the **Notifications** tab when they open it. With push, phones show a notification as soon as something happens (new assignment, published result, attendance alert, announcement, live class), following each user\'s notification choices in the portal.',
            { steps: [
              'Create a Firebase project for the institution. Add an Android app with your app id (and an iOS app with your bundle id, uploading the APNs key in Firebase).',
              'Build the app with the downloaded `google-services.json` (`-GoogleServicesJson`; iOS: `-GoogleServiceInfoPlist`).',
              'In Firebase › Project settings › Service accounts, generate a private key. On the API set `Mobile:Fcm:Enabled = true` and `Mobile:Fcm:ServiceAccountFile` to the key file (or `ServiceAccountJson` from a secret store). Never put the key in source control.',
              'Restart the API. `pushEnabled` in the configuration (section 2.4) becomes `true`; phones register after their next sign-in.',
            ] },
          ],
        },
      ],
    },

    // ── 5 ─────────────────────────────────────────────────────────────────────
    {
      chapter: 'Install the App and Sign In',
      intro: ['This chapter is for everyone who uses the app. It takes about a minute.'],
      sections: [
        {
          title: 'Install',
          who: 'Students, teachers and parents',
          body: [
            { bullets: [
              'Android: install the app from Google Play (search for your institution\'s app name), or open the APK file your institution shared.',
              'iPhone: install it from the App Store.',
              'Allow notifications when the app asks, to receive alerts on the phone.',
            ] },
          ],
        },
        {
          title: 'Sign in',
          who: 'Students, teachers and parents',
          body: [
            'When the app starts it shows the Tabsan emblem while it connects, then the sign-in screen. The name under the logo is your institution\'s name from the portal.',
            { steps: [
              'Enter your **Username**: the same as for the web portal.',
              'Enter your **Password**: the same as for the web portal.',
              'Tap **Sign in**.',
            ] },
            { phones: [['phone:a02-start', 'Starting'], ['phone:a03-signin', 'Sign-in screen'], ['phone:a04-signin-filled', 'Ready to sign in']], caption: 'Starting the app and signing in with the portal account' },
            { note: ['tip', 'You stay signed in. Next time the app opens straight on your Home screen until you sign out in Settings or your institution ends the session. If the phone offers to save the password (for example Samsung Pass or Google Password Manager), choose whatever you prefer; the app works either way.'] },
          ],
        },
        {
          title: 'If the app asks for an authenticator code',
          who: 'Users with two-factor authentication',
          body: [
            'When two-factor authentication is on for your account, an **Authenticator code** field appears after you tap **Sign in**. Enter the 6-digit code from your authenticator app and tap **Sign in** again. It is the same code you use for the portal.',
          ],
        },
        {
          title: 'First sign-in with a temporary password',
          who: 'New users created by import (often parents)',
          body: [
            'Accounts created through User Import start with a temporary password (the username). The first time such an account signs in, the app asks for a new password before it opens. Nothing is saved on the phone until the new password is set.',
            { steps: [
              'Sign in with your username and the temporary password.',
              'The app shows "Please choose your own password to continue." Enter a **New password** and type it again in **Repeat the new password**. It must follow your institution\'s password rules; the app shows the rule if the password is too weak. If the two differ, the app says "The two new passwords are not the same."',
              'Tap **Sign in**. The app signs you in with the new password. Use it for the portal as well.',
            ] },
            { phones: [['phone:g01-new-password', 'Choose your own password'], ['phone:g02-home', 'Signed in']], caption: 'A parent\'s first sign-in: the app asks for a new password, then opens the Home screen' },
          ],
        },
        {
          title: 'Sign in with your institution\'s account (SSO)',
          who: 'Institutions that use single sign-on',
          body: [
            'If the sign-in screen shows **Sign in with …** buttons, your institution uses single sign-on. Tap your institution\'s button, sign in on the page that opens in the browser, and the app opens again, signed in. If your institution uses only single sign-on, the app says "Your institution uses single sign-on. Use your organization\'s button below."',
          ],
        },
        {
          title: 'Language, server and signing out',
          who: 'Everyone',
          body: [
            'Open **Settings** to see your account, change the language, check which server the app is connected to, and sign out. Teachers and parents have a Settings tab; students find **Notifications** and **Settings** under **More** at the end of the tab bar on Android.',
            { bullets: [
              '**Language**: English, اردو (Urdu) or العربية (Arabic). Urdu and Arabic switch the whole app to right-to-left. Course names and messages from the server come in the same language.',
              '**About this app** shows the app version and the **Server** address the app was built for. Give both to your IT team when reporting a problem.',
              '**Sign out** removes the session from the phone and stops notifications to it.',
            ] },
            { phones: [['phone:s09-settings', 'Settings'], ['phone:s10-home-urdu', 'Home in Urdu']], caption: 'Settings, and the Home screen after switching to Urdu' },
          ],
        },
      ],
    },

    // ── 6 ─────────────────────────────────────────────────────────────────────
    {
      chapter: 'Using the App — Students',
      intro: ['Students have six screens: Home, Timetable, Courses, Results, Notifications and Settings. Android shows five tabs at most, so Notifications and Settings are under **More**. Pull down on Home or Notifications to refresh.'],
      sections: [
        {
          title: 'Home',
          who: 'Students',
          body: [
            'Home shows the day at a glance: a greeting with the date, unread notifications, your **attendance**, your **average** marks (CGPA for University students) and **fees due**; then today\'s classes, work that is due, and the latest announcements.',
            { phones: [['phone:s01-home', 'Home'], ['phone:s02-home-due', 'Due work and announcements']], caption: 'A Class 9 student\'s Home screen' },
            { bullets: [
              'Attendance is green from 75%, amber from 50% and red below.',
              'Each due item shows its date, or **Overdue**. Tap it to open the course.',
              'The figures are the same as in the portal\'s attendance, results and fee pages.',
            ] },
          ],
        },
        {
          title: 'Timetable',
          who: 'Students and teachers',
          body: [
            'The week\'s classes, day by day, with start and end times, subject, course code and room. Today is marked **Today**.',
            { phones: [['phone:s03-timetable', 'Timetable']], caption: 'The weekly timetable' },
          ],
        },
        {
          title: 'Courses and course pages',
          who: 'Students',
          body: [
            'The **Courses** tab lists your courses with their code, class and teacher. Tap a course to see its assignments, materials, quizzes and announcements, and to open its **Discussions**.',
            { phones: [['phone:s04-courses', 'Courses'], ['phone:s05-course', 'A course']], caption: 'Courses and a course page' },
            { bullets: [
              'Each assignment shows its due date and state: **Hand in**, **Not graded yet** (handed in) or your **marks**.',
              'Tap a material to open it. Tap a quiz to take it in the portal (the browser opens), where exam timing and settings are kept.',
            ] },
          ],
        },
        {
          title: 'Hand in an assignment',
          who: 'Students',
          body: [
            { steps: [
              'Open the course and tap the assignment.',
              'Type your answer, and/or tap **Take a photo** to photograph written work, or **Add a file** to attach a document.',
              'Tap **Hand in**. The app returns to the course, where the assignment now shows **Not graded yet**. The teacher sees the submission in the portal and in the app ("1 to grade", section 7.3).',
            ] },
            { phones: [['phone:s06-handin', 'Hand in']], caption: 'Handing in an assignment' },
          ],
        },
        {
          title: 'Results and attendance',
          who: 'Students',
          body: [
            'The **Results** tab lists each published result with marks and percentage, attendance per course, and report cards when your institution publishes them.',
            { phones: [['phone:s07-results', 'Results'], ['phone:s07b-attendance', 'Attendance per course']], caption: 'Results and attendance' },
          ],
        },
        {
          title: 'Notifications',
          who: 'Everyone',
          body: [
            'The same notifications as the portal\'s bell: new assignments, results, attendance alerts, announcements and more, with the time each was sent (in the phone\'s time zone). Unread items have a dot; tap one to mark it read, or use **Mark all as read**.',
            { phones: [['phone:s08-notifications', 'Notifications']], caption: 'Notifications' },
          ],
        },
      ],
    },

    // ── 7 ─────────────────────────────────────────────────────────────────────
    {
      chapter: 'Using the App — Teachers',
      intro: ['Teachers have Home, Timetable, Courses, Notifications and Settings. The app covers the everyday classroom tasks; the portal remains the place for setting up courses and for the full grading workspace.'],
      sections: [
        {
          title: 'Home',
          who: 'Teachers',
          body: [
            'Your number of courses, the submissions waiting to be graded, today\'s classes with course codes and rooms, and announcements.',
            { phones: [['phone:t01-home', 'Teacher Home'], ['phone:t02-course', 'A course']], caption: 'A teacher\'s Home screen and course page' },
          ],
        },
        {
          title: 'Take attendance',
          who: 'Teachers',
          body: [
            { steps: [
              'Open the course and tap **Take attendance**.',
              'Check the date. Every student starts as **Present**.',
              'Tap a student\'s status to change it: Present → Absent → Late → Excused.',
              'Tap **Save**. The app confirms "Attendance saved." The attendance appears in the portal and in the student\'s and parent\'s app at once.',
            ] },
            { phones: [['phone:t03-attendance', 'Marking'], ['phone:t03b-attendance-saved', 'Saved']], caption: 'Taking attendance: green present, red absent, amber late, blue excused' },
          ],
        },
        {
          title: 'Grade submissions',
          who: 'Teachers',
          body: [
            'Each assignment card on the course page shows how many submissions wait for grading ("1 to grade" or "Nothing to grade"). Tap it to see each student\'s submission with their typed answer, enter **Marks** and **Feedback**, and tap **Save grade**. The app confirms "Grade saved." and the student\'s assignment shows the marks. Rubrics, annotations and similarity checks are in the portal\'s grading workspace.',
            { phones: [['phone:t04-grading', 'Grading']], caption: 'Grading a submission' },
          ],
        },
        {
          title: 'Post an announcement',
          who: 'Teachers',
          body: [
            { steps: [
              'Open the course and tap **New announcement**.',
              'Enter a **Title** and the **Message**.',
              'Tap **Post**. The announcement appears on the course in the portal and the app, and students receive a notification.',
            ] },
            { phones: [['phone:t05-announcement', 'New announcement'], ['phone:t06-posted', 'Posted']], caption: 'Posting an announcement from the app' },
          ],
        },
      ],
    },

    // ── 8 ─────────────────────────────────────────────────────────────────────
    {
      chapter: 'Using the App — Parents',
      intro: ['Parents have Home, My children, Notifications and Settings, and see only the children linked to their account (section 4.4).'],
      sections: [
        {
          title: 'Home',
          who: 'Parents',
          body: [
            'One card per child with attendance, average marks and fees due. Tap a child to see more.',
            { phones: [['phone:g02-home', 'Parent Home'], ['phone:g03-child', 'A child']], caption: 'A parent\'s Home screen and a child\'s page' },
          ],
        },
        {
          title: 'A child\'s page',
          who: 'Parents',
          body: [
            'The child\'s attendance, average and fees, then published results, attendance per course, report cards and the announcements from the child\'s courses.',
            { phones: [['phone:g04-child-more', 'Results and announcements']], caption: 'Further down a child\'s page' },
          ],
        },
      ],
    },

    // ── 9 ─────────────────────────────────────────────────────────────────────
    {
      chapter: 'How the App and the Portal Stay in Step',
      intro: ['Because both use the same server, every change is visible in both at once. The table shows where the most common actions appear.'],
      sections: [
        {
          title: 'Where each action shows',
          body: [
            { table: { head: ['Done in the app', 'Shows in the portal', 'Shows in other apps'], widths: [3000, 3300, 3000], rows: [
              ['Teacher takes attendance', 'Attendance pages and reports', 'Students\' and parents\' attendance figures'],
              ['Teacher posts an announcement', 'Course announcements and the bell', 'Students\' Home, course page and Notifications; parents\' child page'],
              ['Teacher saves a grade', 'Assignment submissions and gradebook', 'The student\'s assignment state and marks'],
              ['Student hands in work', 'Assignment submissions and the grading workspace', 'The teacher\'s "to grade" count'],
              ['User chooses a new password at the first sign-in', 'The portal sign-in uses the new password', '—'],
              ['Done in the portal: results published, timetable changed, fees updated', '—', 'The app shows it the next time the screen loads'],
            ] } },
          ],
        },
        {
          title: 'Example: an announcement from the app',
          body: [
            'For this guide the teacher of Class 9 Mathematics posted "Science fair on Friday" from the phone (section 7.4). Without any further step it appeared at the top of the student\'s portal inbox, on the student\'s Home screen in the app, and on the Home screen of Fatima\'s father, Imran Sheikh. Attendance behaves the same way: the teacher saved attendance from the phone, and the parent\'s screen shows Fatima\'s attendance as 94.7%, up from the 94.6% on her own screen captured just before.',
            { fig: 'portal:p07-announcement-in-portal', caption: 'The announcement posted from the phone, in the student\'s portal' },
            { phones: [['phone:s11-announcement-in-app', 'Student app'], ['phone:g02-home', 'Parent app']], caption: 'The same announcement in the student\'s app and the parent\'s app' },
          ],
        },
      ],
    },

    // ── 10 ────────────────────────────────────────────────────────────────────
    {
      chapter: 'Troubleshooting',
      sections: [
        {
          title: 'Messages and what to do',
          body: [
            { table: { head: ['The app says', 'Likely cause', 'What to do'], widths: [3000, 3000, 3300], rows: [
              ['No connection to EduSphere. Check your internet and try again.', 'No internet, or the phone cannot reach the API', 'Try mobile data and Wi-Fi. IT: open `…/api/v1/mobile/config` from a phone (section 2.4); check DNS, firewall and certificate.'],
              ['The username or password is not correct.', 'Wrong details', 'Use the portal username and password. Reset the password in the portal if needed.'],
              ['Please choose your own password to continue.', 'First sign-in with a temporary password', 'Choose a new password (section 5.4).'],
              ['The two new passwords are not the same.', 'Typing mistake', 'Type the same new password in both fields.'],
              ['This version of the app is no longer supported. Please update it.', 'App older than `MinimumVersion`', 'Tap **Update** or update from the store.'],
              ['Your session has ended. Please sign in again.', 'The session expired or was ended in the portal', 'Sign in again.'],
              ['Your EduSphere account is inactive or locked. Contact your administrator.', 'Account locked or deactivated', 'Administrator: unlock or activate the account in the portal.'],
              ['Single sign-on was cancelled.', 'The browser was closed before signing in', 'Tap **Sign in with …** again.'],
            ] } },
          ],
        },
        {
          title: 'Other problems',
          body: [
            { table: { head: ['Problem', 'Cause', 'Solution'], widths: [3000, 3000, 3300], rows: [
              ['Single sign-on ends in the browser and does not return to the app', '`AppRedirectUri` differs between API, portal and app build, or `PortalBaseUrl` is empty', 'Make all three use `<scheme>://sso` and set `PortalBaseUrl` (sections 2.2, 2.3, 3.2).'],
              ['The sign-in screen shows the wrong institution name', 'Portal branding', 'Correct it in Dashboard Settings (section 4.1).'],
              ['A parent sees no children', 'No active link (none made, or paused), or the student is not in a School department', 'Link the parent to each child on **Parent Links**, or select **Resume** (section 4.4).'],
              ['No push notifications', 'Firebase not set up, notifications blocked on the phone, or the user switched that type off', 'Check `pushEnabled` (section 2.4), the phone\'s notification permission and the user\'s notification settings in the portal.'],
              ['A course or result is missing in the app', 'Not published, or the student is not enrolled', 'Check the same page in the portal: the app shows exactly what the portal shows.'],
              ['Quizzes do not open', '`PortalBaseUrl` not set', 'Set `Mobile:PortalBaseUrl` (section 2.2).'],
              ['Times differ between the app and the portal', 'The app shows times in the phone\'s time zone; the portal shows them as the server keeps them', 'Nothing to fix: it is the same moment. If the app\'s times look wrong, check the phone\'s date and time settings.'],
              ['A space appears in the username after a dot ("tmsgul. c9s1")', 'The keyboard\'s automatic spacing (seen on Samsung keyboards in builds before this guide)', 'Install the current build, which turns keyboard suggestions off in the username field, or delete the space before tapping **Sign in**.'],
              ['"Update" button does nothing', 'Store URL not set', 'Fill in `AndroidStoreUrl` / `IosStoreUrl`.'],
            ] } },
            { note: ['tip', 'When reporting a problem, include the app version and the server address from **Settings › About this app**, the exact message, and the time it happened. The API also receives crash reports from the app automatically (app version, phone model, the error and the user id; no other personal data).'] },
          ],
        },
      ],
    },
  ],

  appendices: [
    {
      title: 'Appendix A   Settings Reference',
      body: [
        { h3: 'API (appsettings.json)' },
        { table: { head: ['Setting', 'Default', 'Purpose'], widths: [3300, 1700, 4300], rows: [
          ['Mobile:MinimumVersion', '1.0.0', 'Oldest app version allowed'],
          ['Mobile:LatestVersion', '1.0.0', 'Newest published version'],
          ['Mobile:AndroidStoreUrl / IosStoreUrl', 'empty', 'Update button targets'],
          ['Mobile:PortalBaseUrl', 'empty', 'Portal address for single sign-on and quizzes'],
          ['Mobile:AppRedirectUri', 'edusphere://sso', 'Single sign-on return address'],
          ['Mobile:HandoffSeconds', '120', 'Life of the single sign-on code (30–600)'],
          ['Mobile:MaxConsecutiveFailures', '5', 'Failed pushes before a phone is switched off'],
          ['Mobile:Fcm:Enabled / ProjectId / ServiceAccountFile / ServiceAccountJson', 'off', 'Push notifications through Firebase'],
        ] } },
        { h3: 'Portal (appsettings.json)' },
        { table: { head: ['Setting', 'Purpose'], widths: [3300, 6000], rows: [
          ['EduApi:BaseUrl', 'The API the portal uses (the same API as the app)'],
          ['Mobile:AppRedirectUri', 'Where the portal returns single sign-on to the app; same value as on the API'],
        ] } },
        { h3: 'App build (build-whitelabel.ps1)' },
        { table: { head: ['Parameter', 'Purpose'], widths: [3300, 6000], rows: [
          ['-AppName, -AppId', 'Name and store id'],
          ['-ApiBaseUrl', 'The API address built into the app (HTTPS)'],
          ['-Color, -Scheme', 'Brand colour; single sign-on address scheme'],
          ['-Version, -Build', 'Version shown to users; store build number'],
          ['-Platform, -Apk', 'Android or iOS; APK instead of a Google Play bundle'],
          ['-GoogleServicesJson, -GoogleServiceInfoPlist', 'Firebase files for push'],
          ['-KeyStore, -KeyAlias', 'Google Play upload key (passwords from environment variables)'],
        ] } },
      ],
    },
    {
      title: 'Appendix B   API Endpoints Used by the App',
      body: [
        'For IT staff checking firewalls, proxies and logs. All are under `https://<your API>`.',
        { table: { head: ['Endpoint', 'Used for'], widths: [4300, 5000], rows: [
          ['GET /api/v1/mobile/config', 'Institution name, versions, languages, sign-in options (before sign-in)'],
          ['POST /api/v1/auth/login, /auth/refresh, /auth/logout', 'Password sign-in, keeping the session, signing out'],
          ['POST /api/v1/auth/force-change-password', 'Choosing a password at the first sign-in'],
          ['POST /api/v1/mobile/handoff/redeem', 'Finishing single sign-on (one-time code)'],
          ['GET /api/v1/mobile/home, /timetable, /courses, /courses/{id}', 'Home, Timetable, Courses and course pages'],
          ['GET /api/v1/mobile/results, /attendance, /report-cards, /announcements, /children', 'Results, attendance, report cards, parents\' screens'],
          ['/api/v1/assignment/…, /api/v1/attendance/…, /api/v1/discussion/…', 'Hand-in, grading, taking attendance, discussions'],
          ['/api/v1/notification/…', 'Notifications and Mark all as read'],
          ['POST /api/v1/mobile/devices, /devices/unregister', 'Push notification registration'],
          ['POST /api/v1/mobile/crash-reports', 'Automatic crash reports'],
        ] } },
      ],
    },
    {
      title: 'Appendix C   Glossary',
      body: [
        { table: { head: ['Term', 'Meaning'], widths: [2600, 6700], rows: [
          ['API', 'The EduSphere server program that the portal and the app both use. It holds the business rules and talks to the database.'],
          ['APK / AAB', 'Android app files: an APK installs directly; an AAB is uploaded to Google Play.'],
          ['Firebase Cloud Messaging (FCM)', 'Google\'s service that delivers push notifications to Android phones and, through Apple, to iPhones.'],
          ['Hand-off code', 'A one-time code with which the portal passes a single sign-on to the app.'],
          ['HTTPS', 'Encrypted web connection; required between the app and the API.'],
          ['Scheme', 'The start of an app address (for example `edusphere://`) that the phone sends to the app.'],
          ['Single sign-on (SSO)', 'Signing in with the institution\'s own account (Microsoft, Google, OpenID Connect or SAML).'],
          ['Two-factor authentication (2FA)', 'A second sign-in step with a 6-digit code from an authenticator app.'],
          ['White-label build', 'The app built with an institution\'s own name, logo, colour and server address.'],
        ] } },
        { h3: 'Document history' },
        { table: { head: ['Version', 'Date', 'Changes'], widths: [1300, 2200, 5800], rows: [
          ['1.0', DATE, 'First edition, for app version 1.0.0 with the Tabsan look (logo, colours and redesigned screens).'],
        ] } },
      ],
    },
  ],
};
