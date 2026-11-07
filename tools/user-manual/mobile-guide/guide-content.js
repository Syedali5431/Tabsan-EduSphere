// Text of the Tabsan EduSphere Mobile App Connection Guide (built by build-guide.js).
// Images: phone:<name> = shots/phone/<name>.png (taken from the APK with phone.ps1), portal:<name> = shots/portal,
// diagram:<name> = shots/diagram. **bold** and `code` work in all text.

const VERSION = '1.1';
const DATE = '7 November 2026';

module.exports = {
  VERSION, DATE,
  cover: ['phone:c02-server-filled', 'phone:s01-home'],

  about: [
    'This guide explains how the Tabsan EduSphere mobile app connects to your institute\'s EduSphere server, and walks through everything needed to put the app in the hands of students, teachers and parents: preparing the server (on the internet or on the institute\'s own network), getting the app to users, preparing accounts in the portal, connecting and signing in, and using the app day to day.',
    'Since version 1.1 of this guide there is **one app for every institute**. The app has no server built in: the first time it opens, each person types the server address their institute gave them, the app checks it, and from then on it opens straight on the sign-in screen. Institutions that want their own branded app can still have one with their server fixed in it (chapter 3).',
    'Every app screenshot in this guide was taken from the standard EduSphere Android package (APK, app version 1.0.0) on an Android 14 phone emulator, connected over the institute\'s Wi-Fi to a test server at `192.168.1.109:5181` with the School demonstration data (Tabsan Model School, Gulberg Campus). The screenshots use the light theme unless they say otherwise. Portal screenshots come from the same server at the same time, so the app and the portal show the same records.',
  ],
  aboutTable: [
    ['Product', 'Tabsan EduSphere mobile app (Android and iOS) and Campus Portal'],
    ['Document', 'Mobile App Connection Guide'],
    ['Version', `${VERSION} — ${DATE}`],
    ['App version', '1.0.0, standard build (any institute; server entered in the app)'],
    ['Audience', 'IT administrators (chapters 2–3), portal administrators (chapter 4), everyone who uses the app (chapter 5), students (6), teachers (7), parents (8), campus administrators (9)'],
    ['Related documents', 'Tabsan EduSphere User Manual · SSO Setup Guide · mobile/README.md (developers)'],
    ['Publisher', 'Tabsan Nexora'],
  ],
  aboutNote: 'Your institution\'s app may carry its own name, logo and colour (a branded build, section 3.4). Branded apps skip the server step because the server is built in; every other screen, menu and step is the same.',

  chapters: [
    // ── 1 ─────────────────────────────────────────────────────────────────────
    {
      chapter: 'How the App Connects to the Server',
      intro: ['The mobile app is not a separate system. It is a second way into the same EduSphere server that runs your web portal. Understanding this makes every later step obvious: the app needs to know the server\'s address, and users sign in with the accounts they already have.'],
      sections: [
        {
          title: 'One server, two ways in',
          body: [
            'The web portal and the mobile app both talk to the **EduSphere API** on your server. The API reads and writes one database. When a teacher takes attendance in the app, the portal shows it at once; when an administrator publishes results in the portal, students see them in the app the next time the screen refreshes. Nothing is copied or synchronised between the two, so they can never disagree.',
            { fig: 'diagram:connection', caption: 'The portal and the app use the same API, database and accounts' },
            { h3: 'What the phone keeps', bullets: [
              'The **server address** that was entered on the first start.',
              'The sign-in session (an access token and a refresh token) in the phone\'s secure storage (Android Keystore, iOS Keychain), so users stay signed in until they sign out.',
              'The chosen theme and class, and work that has not been sent yet (for example an unfinished answer on the hand-in screen while it is open).',
              'No copies of results, attendance or other records. Every screen is loaded from the server when it opens and when the user pulls down to refresh.',
            ] },
          ],
        },
        {
          title: 'One app for every institute',
          body: [
            'The standard EduSphere app works with any EduSphere server. Each institute runs its own server with its own database, and each person tells the app which server is theirs, once:',
            { steps: [
              'The institute\'s IT team gives everyone the **server address**, for example `edu.myschool.com` or `192.168.1.10:5181`.',
              'The person installs the app and types that address on the **Connect to your institute** screen.',
              'The app checks that the address really is an EduSphere server, keeps it, and shows the sign-in screen with the institute\'s name.',
            ] },
            { table: { head: ['Where the server runs', 'What people type', 'Connection'], widths: [2600, 3300, 3400], rows: [
              ['On the internet (hosted or cloud)', '`edu.myschool.com` or `https://api.myschool.edu`', 'HTTPS (encrypted). Works from anywhere: mobile data, home Wi-Fi, school Wi-Fi.'],
              ['On the institute\'s own network', '`192.168.1.10:5181`, `schoolserver:5181`, `server.school.local`', 'HTTP on the institute\'s network. Works only while the phone is on that Wi-Fi (or a VPN into it).'],
            ] } },
            { note: ['important', 'The app never sends a password over plain HTTP to an address on the internet. It accepts `http://` only for addresses on a local network (section 2.1) and marks those connections **not encrypted**.'] },
          ],
        },
        {
          title: 'What connects to what',
          body: [
            { table: { head: ['Connection', 'How', 'Set up in'], widths: [2600, 4200, 2500], rows: [
              ['App → API', 'Calls to `<server address>/api/v1/...` (sign-in, home, timetable, courses, results, notifications …)', 'The server address typed in the app (section 5.2), or built into a branded app (section 3.4)'],
              ['App → portal', 'The phone\'s browser opens the portal for single sign-on and for quizzes (exam settings and timing stay in the portal)', 'API setting `Mobile:PortalBaseUrl`, section 2.4'],
              ['Portal → app', 'After single sign-on the portal hands a one-time code back to the app through its address scheme (`edusphere://sso`)', '`Mobile:AppRedirectUri` on the API and the portal, sections 2.4–2.5'],
              ['API → phones', 'Push notifications through Firebase Cloud Messaging (optional)', 'Firebase project, app build and API, section 4.6'],
              ['Portal → app branding', 'The institution name and subtitle from the portal\'s Dashboard Settings appear on the app\'s sign-in screen', 'Portal, section 4.1'],
            ] } },
          ],
        },
        {
          title: 'Who does what',
          body: [
            { table: { head: ['Role', 'Tasks', 'Chapter'], widths: [2300, 5300, 1700], rows: [
              ['IT administrator', 'Makes the API reachable from phones (on the internet over HTTPS, or on the institute\'s network), fills in the `Mobile` settings, shares the app (store link or APK) and the server address, optionally builds a branded app and sets up push notifications', '2, 3, 4.6'],
              ['Portal administrator', 'Sets the institution name, creates parent accounts, links parents to their children, optionally sets up single sign-on', '4'],
              ['Students, teachers, parents', 'Install the app, enter the server address once, sign in with their portal account', '5–8'],
              ['Campus administrators', 'Follow their campuses in the app', '9'],
            ] } },
          ],
        },
        {
          title: 'Checklist',
          body: [
            'Work through these steps in order. Each one names the section that explains it.',
            { steps: [
              'Decide where the server is reached from: the internet, the institute\'s own network, or both (section 2.1).',
              'Phones can reach the API: over HTTPS with a trusted certificate (section 2.2), or on the institute\'s network with the API listening on the network and the firewall open (section 2.3).',
              'The `Mobile` settings are filled in on the API, including a portal address phones can open, and the portal uses the same return address (sections 2.4–2.5).',
              '`<server address>/api/v1/mobile/config` opens in a phone\'s browser and shows your institution name (section 2.6).',
              'The app is available to users: the standard app from the store or as an APK, or your own branded app (chapter 3).',
              'Users know the server address to type (section 3.3).',
              'The institution name and subtitle are right in the portal (section 4.1).',
              'Parent accounts are created and linked to their children, if parents will use the app (sections 4.3–4.4).',
              'A student, a teacher and a parent connect, sign in on a phone and see their own data (chapter 5).',
            ] },
          ],
        },
      ],
    },

    // ── 2 ─────────────────────────────────────────────────────────────────────
    {
      chapter: 'Prepare the Server',
      intro: ['The app talks only to the EduSphere API. This chapter makes sure phones can reach it, securely where it matters, and tells the API how the app signs in and where the portal is.'],
      sections: [
        {
          title: 'Choose where the server is reached from',
          who: 'IT administrator',
          body: [
            { table: { head: ['', 'On the internet', 'On the institute\'s own network'], widths: [2000, 3700, 3600], rows: [
              ['Who can use the app', 'Everyone, anywhere (mobile data, home, school)', 'Only phones on the institute\'s Wi-Fi (or on a VPN into it)'],
              ['Address users type', 'A name: `edu.myschool.com`', 'An IP address or computer name with the port: `192.168.1.10:5181`'],
              ['Connection', 'HTTPS with a certificate phones trust (required)', 'HTTP is accepted; HTTPS only with a certificate the phones trust'],
              ['You need', 'A public DNS name, a certificate (for example free from Let\'s Encrypt), a host or reverse proxy (IIS, nginx)', 'A fixed IP address for the server PC, the API listening on the network, a firewall rule'],
              ['Good for', 'Daily use by students and parents at home', 'Trying the app out, schools whose users are on site, servers without internet access'],
            ] } },
            'Both can be used together: phones on the school Wi-Fi may use the local address, everyone else the internet address. Each phone keeps the one its user typed.',
            { h3: 'Which addresses count as the institute\'s own network', bullets: [
              'Private IP addresses: `10.x.x.x`, `172.16.x.x`–`172.31.x.x`, `192.168.x.x`, and `100.64.x.x`–`100.127.x.x` (VPNs such as Tailscale).',
              'Computer names without a dot (`schoolserver`) and names ending in `.local`, `.lan`, `.internal` or `.home.arpa`.',
              'Anything else is treated as an internet address and must use HTTPS. The app shows "Servers on the internet must use https://." if someone types `http://` with such an address.',
            ] },
          ],
        },
        {
          title: 'A server on the internet',
          who: 'IT administrator',
          body: [
            { bullets: [
              'Give the **API** a public name, for example `edu.your-school.edu.pk` or `api.your-school.edu.pk`, and point it at the server in your DNS.',
              'Serve it over **HTTPS with a certificate trusted by phones** (Let\'s Encrypt or your certificate provider). Self-signed certificates do not work: the app reports that the phone does not trust the certificate.',
              'If the API runs behind IIS or nginx, forward all paths under `/api/` to it unchanged. An API published under a folder (for example `https://school.edu/edusphere`) also works: users type that address.',
              'The **portal** must be reachable from phones as well if you use single sign-on or quizzes, because the app opens it in the phone\'s browser.',
              'The server clock must be correct: sign-in tokens and single sign-on codes expire after a short time.',
            ] },
            'What users then type: `edu.your-school.edu.pk`. The app adds `https://` itself.',
            { note: ['warning', 'Do not open the API to the internet over plain HTTP. Passwords and tokens would travel unencrypted, and the app refuses such addresses anyway.'] },
          ],
        },
        {
          title: 'A server on the institute\'s own network',
          who: 'IT administrator (with administrator rights on the server PC)',
          body: [
            'A server on the school\'s own network needs no domain name or certificate. Phones on the same Wi-Fi reach it by its IP address. Out of the box the API only accepts connections from the server PC itself, so three things change.',
            { h3: '1. Give the server PC a fixed address' },
            { steps: [
              'Find the PC\'s address: open PowerShell and run `ipconfig`. Note the **IPv4 Address** of the Wi-Fi or Ethernet adapter, for example `192.168.1.109`.',
              'In your router\'s settings, reserve that address for the PC (often called **DHCP reservation** or **static lease**). Otherwise the router may give the PC another address later and phones stop connecting.',
            ] },
            { h3: '2. Make the API and the portal listen on the network' },
            'Start the API and the portal with network addresses instead of `localhost` (`0.0.0.0` means every network card of the PC):',
            { code: [
              '# from the published API folder',
              'dotnet Tabsan.EduSphere.API.dll --urls http://0.0.0.0:5181',
              '# from the published portal folder',
              'dotnet Tabsan.EduSphere.Web.dll --urls http://0.0.0.0:5063',
              '',
              '# or, for a Windows service / scheduled task, set the environment variable instead:',
              'ASPNETCORE_URLS=http://0.0.0.0:5181',
            ] },
            { h3: '3. Let phones through the Windows firewall' },
            'Open **PowerShell as administrator** on the server PC. If Windows treats the network as **Public** (check under Settings › Network & internet › Wi-Fi or Ethernet › Network profile type), set it to **Private** first, but only for the institute\'s own network. Then allow the two ports on private networks:',
            { code: [
              'Set-NetConnectionProfile -InterfaceAlias "Wi-Fi" -NetworkCategory Private',
              'New-NetFirewallRule -DisplayName "EduSphere (phones on Wi-Fi)" -Direction Inbound `',
              '    -Protocol TCP -LocalPort 5181,5063 -Action Allow -Profile Private',
            ] },
            { note: ['tip', 'Use the name of your adapter in `-InterfaceAlias` ("Wi-Fi", "Ethernet" …; `Get-NetConnectionProfile` lists them). If Windows shows a "Windows Security Alert" for `dotnet` when the API starts, allow it for **Private networks** only.'] },
            'What users then type: `192.168.1.109:5181` (the server\'s address and the API\'s port). The app tries HTTPS first and then HTTP, and shows the connection as **not encrypted**.',
            { note: ['important', 'Plain HTTP is not encrypted: anyone on the same Wi-Fi could read the traffic, passwords included. Use it only on a Wi-Fi network protected with a strong password (WPA2 or WPA3) that only the institute controls. For use from home, publish the server on the internet over HTTPS (section 2.2).'] },
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
              '  "AndroidStoreUrl": "https://play.google.com/store/apps/details?id=com.tabsan.edusphere",',
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
              ['PortalBaseUrl', 'The portal address the **phone** opens for single sign-on and for quizzes. It must work from a phone: not `localhost`.', '`https://portal.…` or `http://192.168.1.109:5063` on a local network'],
              ['AppRedirectUri', 'Where the portal returns the one-time sign-in code after single sign-on. Must match the app\'s address scheme (section 3.4).', '`edusphere://sso`'],
              ['HandoffSeconds', 'How long that one-time code stays valid (30–600 seconds).', '120'],
              ['MaxConsecutiveFailures', 'Failed push deliveries in a row after which a phone stops receiving push.', '5'],
              ['Fcm', 'Push notifications through Firebase (section 4.6).', 'Disabled until set up'],
            ] } },
            { note: ['tip', 'Settings can also be given when the API starts, without editing the file: `--Mobile:PortalBaseUrl=http://192.168.1.109:5063`, or the environment variable `Mobile__PortalBaseUrl`.'] },
          ],
        },
        {
          title: 'The matching setting on the portal',
          who: 'IT administrator',
          body: [
            'The portal finishes single sign-on for the app, so it needs the same return address. In the **portal\'s** `appsettings.json`:',
            { code: [
              '"EduApi": { "BaseUrl": "http://localhost:5181" },',
              '"Mobile": { "AppRedirectUri": "edusphere://sso" }',
            ] },
            '`EduApi:BaseUrl` is how the portal itself reaches the API. When both run on the same server it can stay `http://localhost:5181`; phones never use it.',
            { note: ['important', 'If `AppRedirectUri` differs between the API, the portal and the app build, single sign-on ends in the browser instead of returning to the app. Password sign-in is not affected.'] },
          ],
        },
        {
          title: 'Check the connection',
          who: 'IT administrator',
          body: [
            'The app reads its settings from the API before anyone signs in, and the server check in the app (section 5.2) uses the same address. Open it in the browser of a **phone**: on mobile data for an internet server, on the institute\'s Wi-Fi for a local one:',
            { code: [
              'https://edu.your-school.edu.pk/api/v1/mobile/config      (internet)',
              'http://192.168.1.109:5181/api/v1/mobile/config           (institute\'s network)',
            ] },
            'You should see your institution\'s settings. If the page does not open on the phone, the app cannot connect either: check the address, the firewall (section 2.3) or the DNS name and certificate (section 2.2) before going further.',
            { fig: 'portal:p06-mobile-config', caption: 'The configuration the app reads, from the test server on the institute\'s network used for this guide' },
            { table: { head: ['Field', 'Comes from'], widths: [2600, 6700], rows: [
              ['institutionName, subtitle', 'Portal › Dashboard Settings › Portal Branding (section 4.1). Shown on the app\'s sign-in screen.'],
              ['minimumVersion, latestVersion, store URLs', 'The `Mobile` settings (section 2.4).'],
              ['portalBaseUrl, appRedirectUri', 'The `Mobile` settings. Here `portalBaseUrl` is the portal on the institute\'s network.'],
              ['languages, defaultLanguage', 'The portal\'s languages (the app itself is in English).'],
              ['ssoProviders', 'Single sign-on set up in the portal (section 4.5). Each provider becomes a button on the sign-in screen.'],
              ['pushEnabled', '`true` when Firebase is set up on the API (section 4.6).'],
            ] } },
          ],
        },
      ],
    },

    // ── 3 ─────────────────────────────────────────────────────────────────────
    {
      chapter: 'Get the App to Users',
      intro: ['Most institutes use the standard EduSphere app and give their users the server address. Institutions that want the app under their own name, logo and colour can have a branded build with their server built in.'],
      sections: [
        {
          title: 'Standard app or branded app',
          who: 'IT administrator',
          body: [
            { table: { head: ['', 'Standard app', 'Branded app (white-label)'], widths: [2200, 3600, 3500], rows: [
              ['Name and look', 'EduSphere, Tabsan logo and colours', 'Your institution\'s name, logo, colour and store listing'],
              ['Server', 'Typed by each user on the first start; **Change server** later', 'Built in; users never see a server screen. Optionally changeable'],
              ['Who builds it', 'Nobody: use the published app or the standard APK', 'Your IT team or Tabsan, per institution (section 3.4)'],
              ['Updates', 'One app, updated for everyone', 'Each institution publishes its own updates'],
              ['Best for', 'Most schools, colleges and universities; trying the app out', 'Institutions with their own store presence (premium licence)'],
            ] } },
          ],
        },
        {
          title: 'The standard app',
          who: 'IT administrator',
          body: [
            'The standard Android app is a single file that works with every EduSphere server:',
            { table: { head: ['Item', 'Detail'], widths: [2600, 6700], rows: [
              ['File', '`com.tabsan.edusphere-Signed.apk`, about 28 MB'],
              ['App id', '`com.tabsan.edusphere`'],
              ['Phones', 'Android 6.0 or later; iPhone and iPad from the App Store (iOS 15 or later)'],
              ['Server', 'None built in: asked on the first start (section 5.2)'],
            ] } },
            'To build it yourself from the EduSphere source (a Windows PC with the .NET 9 SDK, the MAUI Android workload, the Android SDK and JDK 17 or later):',
            { code: [
              'cd mobile',
              'dotnet publish Tabsan.EduSphere.Mobile/Tabsan.EduSphere.Mobile.csproj `',
              '    -c Release -f net9.0-android -p:MobileTargets=net9.0-android `',
              '    -p:AndroidPackageFormat=apk -o artifacts/standard',
            ] },
            'The APK is written to `mobile/artifacts/standard/`. It is signed with a development key: it installs on any Android phone, but Google Play needs your own upload key (`-p:AndroidKeyStore=true -p:AndroidSigningKeyStore=… -p:AndroidSigningKeyAlias=…`, passwords from environment variables, and `-p:AndroidPackageFormat=aab`).',
          ],
        },
        {
          title: 'Give users the server address',
          who: 'IT administrator or portal administrator',
          body: [
            'Everyone at an institute types the same address. Put it wherever users will see it: the welcome letter, the portal\'s announcements, a notice board, the school\'s WhatsApp group. A message like this works well:',
            { code: [
              'EduSphere mobile app',
              '1. Install "EduSphere" from Google Play / the App Store (or open the APK we shared).',
              '2. Open it and type the server address:   edu.myschool.com',
              '3. Tap Connect, then sign in with your portal username and password.',
            ] },
            { bullets: [
              'Give the address **without** `https://` for an internet server; the app adds it. For a local server give it with the port: `192.168.1.109:5181`.',
              'If you use both, say which address works where ("at school: 192.168.1.109:5181; from home: edu.myschool.com").',
              'Users only do this once. They can check the address later in **Settings › About this app**.',
            ] },
          ],
        },
        {
          title: 'A branded app for your institution',
          who: 'IT administrator (on the build computer: .NET 9 SDK with the MAUI workload, Android SDK, JDK 17 or later; a Mac with Xcode for iOS)',
          body: [
            'Run the white-label build script from the `mobile` folder of the EduSphere source:',
            { code: [
              './build-whitelabel.ps1 -AppName "Tabsan Model School" -AppId pk.edu.tms.app `',
              '    -ApiBaseUrl https://api.your-school.edu.pk -Scheme edusphere `',
              '    -Version 1.0.0 -Build 1 -KeyStore ./tms/upload.keystore -KeyAlias upload',
            ] },
            { table: { head: ['Parameter', 'Required', 'Notes'], widths: [2300, 1200, 5800], rows: [
              ['-AppName', 'Yes', 'Name under the icon.'],
              ['-AppId', 'Yes', 'Store id (reverse domain name). Never change it after the first release.'],
              ['-ApiBaseUrl', 'Yes', 'Your server; must start with `https://`. The app connects only to this server.'],
              ['-AllowServerChange', 'No', 'Keeps **Change server** in the app, starting on `-ApiBaseUrl`. Without it the server is fixed.'],
              ['-Color', 'No', 'Hex colour, for example `#7A1F2B`. Default Tabsan navy.'],
              ['-Scheme', 'No', 'Lower-case letters; must match `Mobile:AppRedirectUri` on the API and portal (`<scheme>://sso`).'],
              ['-Version, -Build', 'Yes', 'For example `1.2.0` and `12`; the build number grows with every store upload.'],
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
            'The standard build carries the Tabsan logo: the TS emblem as the app icon on a navy background, the emblem on the splash screen, and the full Tabsan logo at the top of the server and sign-in screens. For a branded app, replace these files before building and keep their names:',
            { table: { head: ['File (under mobile/Tabsan.EduSphere.Mobile/Resources)', 'Used for'], widths: [5200, 4100], rows: [
              ['AppIcon/appicon.svg', 'Icon background'],
              ['AppIcon/appiconfg.png', 'Icon foreground: the emblem, inside the middle two thirds of a transparent square'],
              ['Splash/splash.png', 'Splash screen emblem (shown on the brand colour)'],
              ['Images/tabsan_wordmark.png', 'Logo at the top of the server and sign-in screens (light artwork for a dark background)'],
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
              '**APK without a store**: share the APK file through your institution\'s own channels (website, email, a shared drive). Users install it as described in section 5.1.',
              'After publishing in a store, put the store links into `AndroidStoreUrl` and `IosStoreUrl` (section 2.4) so the **Update** button works.',
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
              ['Server address changes (standard app)', 'Tell users the new address', 'They tap **Change server** (section 5.11) and sign in again'],
              ['Server address changes (branded app)', 'New build with the new `-ApiBaseUrl`', 'Users install the update and sign in again'],
            ] } },
            { note: ['tip', 'An internet name (`edu.myschool.com`) can move to a new server without users noticing: point the DNS name at the new server. That is one more reason to prefer a name over an IP address.'] },
          ],
        },
        {
          title: 'Test before going live',
          who: 'IT administrator or developer',
          body: [
            'Install the APK on one phone first, or on the Android emulator of Android Studio. Then go through chapter 5 with a student, a teacher and a parent account.',
            { bullets: [
              '**Phone on the institute\'s Wi-Fi**: type the server\'s local address, for example `192.168.1.109:5181`.',
              '**Android emulator on the server PC**: the emulator reaches the PC as `10.0.2.2`, so type `10.0.2.2:5181`.',
              '**Phone on mobile data** (internet server): this is how most parents will use the app; test it at least once.',
            ] },
            'Developers can preset a server in a debug build so the server screen is skipped while testing:',
            { code: [
              'dotnet build Tabsan.EduSphere.Mobile/Tabsan.EduSphere.Mobile.csproj -c Debug `',
              '    -p:MobileTargets=net9.0-android -p:RuntimeIdentifier=android-x64 `',
              '    -p:EmbedAssembliesIntoApk=true -p:AndroidPackageFormat=apk `',
              '    -p:ApiBaseUrl=http://10.0.2.2:5181',
              'adb install -r bin/Debug/net9.0-android/android-x64/com.tabsan.edusphere-Signed.apk',
            ] },
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
            'As soon as the server address is accepted, the app shows the name and subtitle from the portal\'s branding at the top of the sign-in screen, so users can see they are connected to the right institution.',
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
            { fig: 'portal:p01-portal-login', caption: 'The portal\'s sign-in page: the same username and password open the app' },
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
              'Give each parent their username and the server address. Their first password is the **username** itself; the app asks them to choose their own password at the first sign-in (section 5.6).',
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
              'Under **2. Link a student**, search by the student\'s registration number or name (for example `TMS-GUL-09-01` or `Sheikh`).',
              'In the student\'s row, choose the **Relationship** (Father, Mother, Guardian, Grandparent, Sibling or Other) and select **Link**. Repeat for each child.',
            ] },
            { fig: 'portal:p08-parent-links', caption: 'Portal › Parent Links: the parent Imran Sheikh with his two children, and the student search (both search boxes outlined)' },
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
            'Single sign-on in the app needs a `PortalBaseUrl` that phones can open, and the same `AppRedirectUri` on the API, the portal and the app build (sections 2.4, 2.5 and 3.4).',
          ],
        },
        {
          title: 'Push notifications (optional)',
          who: 'IT administrator',
          body: [
            'Without push the app works fully; users see new items in the **Notifications** tab when they open it. With push, phones show a notification as soon as something happens (new assignment, published result, attendance alert, announcement, live class), following each user\'s notification choices in the portal.',
            { steps: [
              'Create a Firebase project for the institution. Add an Android app with your app id (and an iOS app with your bundle id, uploading the APNs key in Firebase).',
              'Build the app with the downloaded `google-services.json` (`-GoogleServicesJson`; iOS: `-GoogleServiceInfoPlist`). Push needs a build made for your Firebase project, so it comes with a branded app (section 3.4).',
              'In Firebase › Project settings › Service accounts, generate a private key. On the API set `Mobile:Fcm:Enabled = true` and `Mobile:Fcm:ServiceAccountFile` to the key file (or `ServiceAccountJson` from a secret store). Never put the key in source control.',
              'Restart the API. `pushEnabled` in the configuration (section 2.6) becomes `true`; phones register after their next sign-in.',
            ] },
          ],
        },
      ],
    },

    // ── 5 ─────────────────────────────────────────────────────────────────────
    {
      chapter: 'Install, Connect and Sign In',
      intro: ['This chapter is for everyone who uses the app. You need two things from your institute: the **server address** and your **portal username and password**. It takes about two minutes.'],
      sections: [
        {
          title: 'Install the app',
          who: 'Students, teachers and parents',
          body: [
            { h3: 'Android from Google Play or iPhone from the App Store' },
            'Search for **EduSphere** (or your institution\'s app name if it has its own) and install it.',
            { h3: 'Android from an APK file your institute shared' },
            { steps: [
              'Open the link or file (for example `com.tabsan.edusphere-Signed.apk`) on the phone. The download finishes in a few seconds.',
              'Tap the downloaded file. If Android says installing apps from this source is not allowed, tap **Settings**, switch on **Allow from this source** for the app you opened the file with (browser, Files or WhatsApp), and go back.',
              'Tap **Install**, then **Open**. If Google Play Protect asks about an unknown app, choose **Install anyway**: the APK comes from your institute.',
            ] },
            { note: ['tip', 'Only install an APK that your institute shared through its own channels. After installing, you can switch **Allow from this source** off again.'] },
          ],
        },
        {
          title: 'Connect to your institute',
          who: 'Everyone, once (not needed in a branded app)',
          body: [
            'The first time the app opens it shows **Connect to your institute**.',
            { steps: [
              'Tap **Server address** and type the address your institute gave you, exactly as given, for example `edu.myschool.com` or `192.168.1.109:5181`. You do not need to type `https://`.',
              'Tap **Connect**. The app checks the address with the server; this takes a second or two.',
              'When the address is right, the sign-in screen opens with your institution\'s name at the top. The app remembers the server, so you never type it again.',
            ] },
            { phones: [['phone:c01-server', 'First start'], ['phone:c02-server-filled', 'Address typed'], ['phone:a03-signin', 'Connected']], caption: 'Connecting to the institute\'s server: the sign-in screen then shows the institution\'s name' },
            { table: { head: ['Your institute\'s server', 'Type for example', 'Note'], widths: [2700, 3000, 3600], rows: [
              ['On the internet', '`edu.myschool.com`', 'Works on mobile data and any Wi-Fi'],
              ['On the institute\'s network', '`192.168.1.109:5181`', 'Works only on the institute\'s Wi-Fi; shown as **not encrypted**'],
              ['Copied from a browser', '`https://edu.myschool.com/api/v1/…`', 'Fine: the app keeps only the server part'],
            ] } },
            'The bottom of the sign-in screen always shows which server the app uses. A connection on the institute\'s own network is marked "not encrypted — use only on the institute\'s network": use it only on that network\'s Wi-Fi.',
            { phones: [['phone:a03b-signin-server', 'Server and Change server']], caption: 'The server shown under the sign-in form, with the Change server link' },
          ],
        },
        {
          title: 'If the app cannot connect',
          who: 'Everyone',
          body: [
            'If the address is not right, the app says why and stays on the server screen. Correct the address and tap **Connect** again.',
            { phones: [['phone:c03-error-https', 'HTTP to the internet'], ['phone:c04-error-unreachable', 'No answer'], ['phone:c05-error-not-edusphere', 'Not an EduSphere server']], caption: 'What the app says when an address cannot be used' },
            { table: { head: ['The app says', 'What it means', 'What to do'], widths: [3200, 2900, 3200], rows: [
              ['Enter your institute\'s server address.', 'The box is empty.', 'Type the address from your institute.'],
              ['That is not a server address. Check it and try again.', 'A typing mistake (for example a space) or not a web address.', 'Type the address exactly as given, without spaces.'],
              ['Servers on the internet must use https://. Ask your institute for the https address.', '`http://` was typed with an internet address; the app does not send passwords unencrypted over the internet.', 'Leave out `http://` (the app then uses `https://`), or ask your institute.'],
              ['Could not reach … Check the address and your internet; for a server on the institute\'s own network, connect to its Wi-Fi.', 'Nothing answered at that address.', 'Check the address and your internet. For a local address (`192.168.…`), connect to the institute\'s Wi-Fi first. Still failing: ask IT (section 11.1).'],
              ['That address answers, but it is not an EduSphere server.', 'A website answered, but not the EduSphere API (often the portal\'s address or a mistyped port).', 'Ask your institute for the app\'s server address; it can differ from the portal\'s address.'],
              ['… uses a security certificate this phone does not trust.', 'The server\'s HTTPS certificate is self-signed, expired or for another name.', 'Tell your institute\'s IT team (section 2.2).'],
            ] } },
          ],
        },
        {
          title: 'Sign in',
          who: 'Students, teachers and parents',
          body: [
            'When the app starts it shows the Tabsan emblem while it connects, then the sign-in screen (or your Home screen if you are already signed in).',
            { steps: [
              'Enter your **Username**: the same as for the web portal.',
              'Enter your **Password**: the same as for the web portal.',
              'Tap **Sign in**.',
              'The first time, Android asks whether EduSphere may send you notifications. Tap **Allow** to get alerts about new homework, results and announcements.',
            ] },
            { phones: [['phone:a02-start', 'Starting'], ['phone:a04-signin-filled', 'Ready to sign in'], ['phone:a05-notifications', 'Allow notifications']], caption: 'Starting the app, signing in with the portal account, and allowing notifications' },
            { note: ['tip', 'You stay signed in. Next time the app opens straight on your Home screen until you sign out in Settings or your institution ends the session. If the phone offers to save the password (for example Google Password Manager or Samsung Pass), choose whatever you prefer; the app works either way.'] },
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
              'Sign in with your username and the temporary password (your username again).',
              'The app shows "Please choose your own password to continue." Enter a **New password** and type it again in **Repeat the new password**. It must follow your institution\'s password rules (by default 12 to 16 characters with a capital letter, a small letter, a number and a symbol); the app shows the rule if the password is too weak. If the two differ, the app says "The two new passwords are not the same."',
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
          title: 'Find your way around',
          who: 'Everyone',
          body: [
            'The tabs at the bottom depend on who you are. Android shows four tabs and puts the rest under **More**; tap **More** to reach Attendance, Results, Notifications and Settings.',
            { table: { head: ['You are', 'Tabs'], widths: [2200, 7100], rows: [
              ['Student', 'Home · Timetable · Courses · Homework · Attendance · Results · Notifications · Settings'],
              ['Teacher', 'Home · Timetable · Courses · Homework · Attendance · Notifications · Settings'],
              ['Parent', 'Home · Timetable · Courses · Homework · Attendance · Results · Notifications · Settings (for the chosen child)'],
              ['Admin, SuperAdmin', 'Home (campus at a glance) · Notifications · Settings'],
            ] } },
            { phones: [['phone:s19-more', 'The More menu']], caption: 'Tabs after the fourth are under More' },
            { bullets: [
              'Pull a screen down to refresh it.',
              'The arrow at the top left goes back; Android\'s own Back gesture does the same.',
            ] },
          ],
        },
        {
          title: 'Choose the child, class and campus',
          who: 'Everyone',
          body: [
            'The Timetable, Courses, Homework, Attendance and Results tabs start with drop-down lists that decide what they show. A choice made on one tab applies to all of them until you change it.',
            { table: { head: ['List', 'Shown to', 'What it does'], widths: [1900, 2400, 5000], rows: [
              ['Child', 'Parents', 'Picks the child whose timetable, courses, homework, attendance and results you see.'],
              ['Class', 'Students, parents and teachers', '**All classes**, or one class (for example Class 9). Earlier classes appear too when there are results or attendance from them. Teachers with many classes see one class at a time.'],
              ['Institute and campus', 'Teachers, Admins and SuperAdmins with more than one campus', 'Shows only that campus\'s classes and courses (on Home too). **All campuses** shows everything.'],
            ] } },
            { phones: [['phone:s15-class-picker', 'Choosing a class'], ['phone:g05-child-picker', 'Parents: choosing a child']], caption: 'The class list, and a parent choosing which child to look at' },
          ],
        },
        {
          title: 'Settings: theme, password and server',
          who: 'Everyone',
          body: [
            'Open **Settings** (under **More** for students, teachers and parents) to see your account, choose the theme, change your password, check which server the app is connected to, change the server and sign out.',
            { bullets: [
              '**Theme**: **Same as the phone** (default), **Light** or **Dark**. The app changes at once and remembers the choice.',
              '**Change password**: enter your **Current password**, then the **New password** and the same again in **Repeat the new password**, and tap **Change password**. The app says "Your password has been changed. Use the new one in the app and the portal." The new password must follow your institution\'s rules and cannot be one of your last five passwords.',
              '**About this app** shows the app version and the **Server** the app uses. Give both to your IT team when reporting a problem.',
            ] },
            { phones: [['phone:s09-settings', 'Settings'], ['phone:s18-settings-about', 'About this app'], ['phone:s16-change-password', 'Change password']], caption: 'Settings, the server in About this app, and changing the password' },
            { phones: [['phone:s01-home', 'Light theme'], ['phone:s17-home-dark', 'Dark theme']], caption: 'The Home screen in the light and the dark theme' },
            { note: ['note', 'The app is in English. The portal\'s other languages are not offered in the app.'] },
          ],
        },
        {
          title: 'Change to another institute\'s server',
          who: 'Everyone (not available in a branded app with a fixed server)',
          body: [
            'Use this when your institute moves to a new server address, when you move to another school, or when you typed a wrong address at the first start. Accounts belong to one server, so changing the server signs you out.',
            { h3: 'When you are signed in' },
            { steps: [
              'Open **Settings** and scroll to **About this app**.',
              'Tap **Change server**. The app asks "You will be signed out. Then enter the other institute\'s server address." Tap **Change server** again (or **Cancel** to stay).',
              'The server screen opens with the current address. Type the new address and tap **Connect**, then sign in with your account on that server.',
            ] },
            { phones: [['phone:c06-change-server-confirm', 'Confirm'], ['phone:c07-server-change', 'Enter the new address']], caption: 'Changing the server from Settings: the app signs out first; Cancel goes back to the sign-in screen' },
            { h3: 'On the sign-in screen' },
            'Tap **Change server** under the sign-in form (see the earlier figure "The server shown under the sign-in form"), type the address and tap **Connect**. **Cancel** keeps the current server.',
          ],
        },
        {
          title: 'Sign out',
          who: 'Everyone',
          body: [
            'In **Settings**, tap **Sign out**. The session is removed from the phone and the phone stops receiving notifications for your account. The server address stays, so the next person on a shared phone only needs to sign in.',
          ],
        },
      ],
    },

    // ── 6 ─────────────────────────────────────────────────────────────────────
    {
      chapter: 'Using the App — Students',
      intro: ['Students have Home, Timetable, Courses, Homework, Attendance, Results, Notifications and Settings. The screenshots show Fatima Sheikh, a Class 9 student at the Gulberg Campus of Tabsan Model School.'],
      sections: [
        {
          title: 'Home',
          who: 'Students',
          body: [
            'Home shows the day at a glance: a greeting with the date, unread notifications, your **attendance**, your **average** marks (CGPA for University students) and **fees due**; then today\'s classes, work that is due, and the latest announcements.',
            { phones: [['phone:s01-home', 'Home'], ['phone:s02-home-due', 'Due work']], caption: 'A Class 9 student\'s Home screen' },
            { bullets: [
              'Attendance is green from 75%, amber from 50% and red below.',
              'Each due item shows its date, or **Overdue**. Tap it to open the course.',
              'The figures are the same as in the portal\'s attendance, results and fee pages.',
            ] },
          ],
        },
        {
          title: 'Timetable',
          who: 'Students, parents and teachers',
          body: [
            'The week\'s classes for the class chosen at the top, day by day, with start and end times, subject, course code and room. Today is marked **Today**.',
            { phones: [['phone:s03-timetable', 'Timetable']], caption: 'The weekly timetable for Class 9' },
          ],
        },
        {
          title: 'Courses and course pages',
          who: 'Students',
          body: [
            'The **Courses** tab lists the courses of the chosen class with their code, class and teacher. Tap a course to see its assignments, materials, quizzes and announcements, and to open its **Discussions**.',
            { phones: [['phone:s04-courses', 'Courses'], ['phone:s05-course', 'A course'], ['phone:s05b-course-more', 'Quizzes and announcements']], caption: 'Courses and a course page' },
            { bullets: [
              'Tap **Open** next to a material to open it.',
              'Tap a quiz to take it in the portal: the phone\'s browser opens, where exam timing and settings are kept.',
            ] },
          ],
        },
        {
          title: 'Homework',
          who: 'Students',
          body: [
            'The **Homework** tab lists the homework of every course in the chosen class: still to do first (soonest due at the top), then work already handed in or past. Each shows its course, due date and state: **Hand in**, **Overdue**, **Not graded yet** (handed in) or your **marks**.',
            { steps: [
              'Tap a homework (here or on the course page).',
              'Type your answer, and/or tap **Take a photo** to photograph written work, or **Add a file** to attach a document.',
              'Tap **Hand in**. The homework now shows **Not graded yet**, and the teacher sees it in the portal and in the app ("1 to grade", section 7.2).',
            ] },
            { phones: [['phone:s12-homework', 'Homework'], ['phone:s06-handin', 'Hand in']], caption: 'The Homework tab and handing in' },
          ],
        },
        {
          title: 'Attendance',
          who: 'Students',
          body: [
            'The **Attendance** tab shows your **overall** attendance for the chosen class, each course (classes attended out of classes marked), and **day by day**: the date, the course and whether you were Present, Absent, Late or Excused.',
            { phones: [['phone:s13-attendance', 'Overall and per course'], ['phone:s14-attendance-days', 'Day by day']], caption: 'Attendance for Class 9' },
          ],
        },
        {
          title: 'Results',
          who: 'Students',
          body: [
            'The **Results** tab lists each published result of the chosen class with marks and percentage (for example "Class 9 · Midterm: 32/50 — 64%"), then report cards when your institution publishes them.',
            { phones: [['phone:s07-results', 'Results']], caption: 'Results' },
          ],
        },
        {
          title: 'Notifications',
          who: 'Everyone',
          body: [
            'The same notifications as the portal\'s bell: new assignments, results, attendance alerts, announcements, live classes and replies to helpdesk tickets, with the time each was sent (in the phone\'s time zone). Unread items have a dot; tap one to mark it read, or use **Mark all as read**.',
            { phones: [['phone:s08-notifications', 'Notifications']], caption: 'Notifications' },
          ],
        },
      ],
    },

    // ── 7 ─────────────────────────────────────────────────────────────────────
    {
      chapter: 'Using the App — Teachers',
      intro: ['Teachers have Home, Timetable, Courses, Homework, Attendance, Notifications and Settings. Choose a class at the top to work with one class at a time. The app covers the everyday classroom tasks; the portal remains the place for setting up courses and for the full grading workspace. The screenshots show Mr. Ali Siddiqui, who teaches Mathematics.'],
      sections: [
        {
          title: 'Home and courses',
          who: 'Teachers',
          body: [
            'Home shows your number of courses, the submissions waiting to be graded, today\'s classes with course codes and rooms, what is due (homework and online classes) and announcements. The **Courses** tab lists the courses you teach in the chosen class.',
            { phones: [['phone:t01-home', 'Teacher Home'], ['phone:t09-courses', 'Courses'], ['phone:t02-course', 'A course']], caption: 'A teacher\'s Home screen, courses and course page' },
            'A course page has **Take attendance**, **New announcement** and **Discussions** at the top, then the assignments with how much is left to grade.',
          ],
        },
        {
          title: 'Homework and grading',
          who: 'Teachers',
          body: [
            'The **Homework** tab lists the homework of your courses in the chosen class, with how many submissions wait for grading ("1 to grade" or "Nothing to grade").',
            { steps: [
              'Tap a homework. Each student\'s submission is shown with their typed answer.',
              'Enter the **Marks** (out of the maximum shown, for example "Marks / 20") and optional **Feedback**.',
              'Tap **Save grade**. The app confirms "Grade saved." and the student sees the marks in the app and the portal.',
            ] },
            { phones: [['phone:t07-homework', 'Homework'], ['phone:t04-grading', 'Grading'], ['phone:t04b-graded', 'Grade saved']], caption: 'Homework to grade, grading a submission, and the confirmation' },
            'Rubrics, annotations and similarity checks are in the portal\'s grading workspace.',
          ],
        },
        {
          title: 'Take attendance',
          who: 'Teachers',
          body: [
            { steps: [
              'Open the **Attendance** tab (under **More**), choose the class at the top, and tap **Take attendance** next to the course (or open the course and tap **Take attendance**).',
              'Check the date. Every student starts as **Present**.',
              'Tap a student\'s status to change it: Present → Absent → Late → Excused.',
              'Tap **Save** at the bottom. The app confirms "Attendance saved." The attendance appears in the portal and in the student\'s and parent\'s app at once.',
            ] },
            { phones: [['phone:t08-attendance', 'Attendance tab'], ['phone:t03-attendance', 'Marking'], ['phone:t03b-attendance-saved', 'Saved']], caption: 'Taking attendance: green present, red absent, amber late, blue excused' },
          ],
        },
        {
          title: 'Post an announcement',
          who: 'Teachers',
          body: [
            { steps: [
              'Open the course and tap **New announcement**.',
              'Enter a **Title** and the **Message**.',
              'Tap **Post**. The app confirms "Announcement posted." The announcement appears on the course in the portal and the app, students receive a notification, and parents see it on their Home screen.',
            ] },
            { phones: [['phone:t05-announcement', 'New announcement'], ['phone:t06-posted', 'Posted']], caption: 'Posting an announcement from the app' },
          ],
        },
      ],
    },

    // ── 8 ─────────────────────────────────────────────────────────────────────
    {
      chapter: 'Using the App — Parents',
      intro: ['Parents have Home, Timetable, Courses, Homework, Attendance, Results, Notifications and Settings, and see only the children linked to their account (section 4.4). Choose the child (and, if wanted, the class) at the top of each tab. The screenshots show Imran Sheikh, father of Fatima (Class 9) and Hassan (Class 4).'],
      sections: [
        {
          title: 'Home',
          who: 'Parents',
          body: [
            'One card per child with attendance, average marks and fees due, then the latest announcements of the children\'s courses. Tap a child to see their results, attendance, report cards and announcements on one page.',
            { phones: [['phone:g02-home', 'Parent Home'], ['phone:g08-home-announcements', 'Announcements'], ['phone:g03-child', 'A child']], caption: 'A parent\'s Home screen, the children\'s announcements, and a child\'s page' },
          ],
        },
        {
          title: 'Homework, attendance, timetable and results',
          who: 'Parents',
          body: [
            'The tabs show the chosen child\'s week, courses, homework (with **Not handed in**, **Not graded yet** or the marks), attendance (overall, per course and day by day) and results. Parents can open a course to see its homework and announcements, but cannot hand in work.',
            { phones: [['phone:g06-homework', 'Homework'], ['phone:g07-attendance', 'Attendance'], ['phone:g09-results', 'Results']], caption: 'Fatima\'s homework, attendance and results, as her father sees them' },
          ],
        },
      ],
    },

    // ── 9 ─────────────────────────────────────────────────────────────────────
    {
      chapter: 'Using the App — Administrators',
      intro: ['Admins and SuperAdmins sign in to the app with their portal account to follow their campuses. Everything else stays in the portal.'],
      sections: [
        {
          title: 'Campus at a glance',
          who: 'Admins and SuperAdmins',
          body: [
            'Home shows the chosen campus: its students, teachers and courses, and today\'s attendance (classes attended out of today\'s marks). Admins with more than one campus, and SuperAdmins, choose the **Institute and campus** at the top; **All campuses** adds them up. Admins see the campuses of the departments assigned to them, SuperAdmins every active campus.',
            { phones: [['phone:a05-admin-home', 'All campuses'], ['phone:a06-campus-picker', 'Choosing a campus'], ['phone:a07-admin-campus', 'One campus']], caption: 'A SuperAdmin\'s Home screen, the list of institutes and campuses, and the Gulberg Campus on its own' },
          ],
        },
      ],
    },

    // ── 10 ────────────────────────────────────────────────────────────────────
    {
      chapter: 'How the App and the Portal Stay in Step',
      intro: ['Because both use the same server, every change is visible in both at once. The table shows where the most common actions appear.'],
      sections: [
        {
          title: 'Where each action shows',
          body: [
            { table: { head: ['Done in the app', 'Shows in the portal', 'Shows in other apps'], widths: [3000, 3300, 3000], rows: [
              ['Teacher takes attendance', 'Attendance pages and reports', 'Students\' and parents\' attendance figures'],
              ['Teacher posts an announcement', 'Course announcements and the bell', 'Students\' Home, course page and Notifications; parents\' Home and child page'],
              ['Teacher saves a grade', 'Assignment submissions and gradebook', 'The student\'s homework state and marks'],
              ['Student hands in work', 'Assignment submissions and the grading workspace', 'The teacher\'s "to grade" count'],
              ['User chooses a new password at the first sign-in', 'The portal sign-in uses the new password', '—'],
              ['Done in the portal: results published, timetable changed, fees updated', '—', 'The app shows it the next time the screen loads'],
            ] } },
          ],
        },
        {
          title: 'Example: from the teacher\'s phone to everyone',
          body: [
            'For this guide the teacher of Class 9 Mathematics posted "Science fair on Friday" from his phone (section 7.4). Without any further step it appeared in the student\'s portal inbox, on the student\'s Home screen in the app, and on the Home screen of Fatima\'s father, Imran Sheikh.',
            { fig: 'portal:p07-announcement-in-portal', caption: 'The announcement posted from the phone, in the student\'s portal inbox (outlined)' },
            { phones: [['phone:s11-announcement-in-app', 'Student app'], ['phone:g08-home-announcements', 'Parent app']], caption: 'The same announcement in the student\'s app and the parent\'s app' },
            'Attendance behaves the same way: Fatima\'s own screen showed **93.6%** before the teacher saved today\'s attendance from his phone (section 7.3), and her father\'s Home screen, opened right after, shows **93.7%**.',
            { note: ['note', 'The portal shows times as the server keeps them, the app in the phone\'s own time zone, so the same announcement can show different clock times. It is the same moment.'] },
          ],
        },
      ],
    },

    // ── 11 ────────────────────────────────────────────────────────────────────
    {
      chapter: 'Troubleshooting',
      sections: [
        {
          title: 'Connecting to the server',
          body: [
            { table: { head: ['Problem', 'Likely cause', 'What to do'], widths: [3000, 3000, 3300], rows: [
              ['"Could not reach …" for a local address (`192.168.…`)', 'The phone is not on the institute\'s Wi-Fi, the API listens only on `localhost`, the firewall blocks the port, or the network is set to Public', 'Users: connect to the institute\'s Wi-Fi. IT: start the API with `--urls http://0.0.0.0:5181`, set the network to Private and add the firewall rule (section 2.3). Test `…/api/v1/mobile/config` in the phone\'s browser.'],
              ['It worked yesterday, now "Could not reach …"', 'The server PC got another IP address from the router', 'Reserve the address in the router (section 2.3); tell users the address if it changed (they tap **Change server**).'],
              ['Works at school, not at home', 'The server is only on the institute\'s network', 'Expected for a local server. For use from home, publish the API on the internet over HTTPS (section 2.2).'],
              ['"Could not reach …" for an internet address', 'DNS name wrong, server down, or a firewall/proxy in front', 'IT: open `https://<name>/api/v1/mobile/config` from a phone on mobile data; check DNS, the host and the proxy rules for `/api/`.'],
              ['"… uses a security certificate this phone does not trust."', 'Self-signed or expired certificate, or a certificate for another name', 'Install a certificate from a public authority (for example Let\'s Encrypt) for exactly the name users type.'],
              ['"That address answers, but it is not an EduSphere server."', 'The portal\'s or another website\'s address was typed, or the wrong port', 'Use the API\'s address (and port), not the portal\'s; check it with `…/api/v1/mobile/config`.'],
              ['"Servers on the internet must use https://."', '`http://` was typed with a public address', 'Type the address without `http://`. If the server has no HTTPS yet, set it up (section 2.2) or use it only on the institute\'s network.'],
              ['The server screen does not appear', 'A branded app has its server built in, or the app already has a server', 'Branded app: nothing to do. Standard app: use **Change server** (section 5.11).'],
            ] } },
          ],
        },
        {
          title: 'Messages after connecting',
          body: [
            { table: { head: ['The app says', 'Likely cause', 'What to do'], widths: [3000, 3000, 3300], rows: [
              ['No connection to EduSphere. Check your internet and try again.', 'The connection dropped, or the server did not answer in time', 'Check the phone\'s Wi-Fi or mobile data and try again. For a local server, check that you are still on the institute\'s Wi-Fi.'],
              ['The username or password is not correct.', 'Wrong details', 'Use the portal username and password. Reset the password in the portal if needed. Check the server under the sign-in form: an account exists only on its own institute\'s server.'],
              ['Please choose your own password to continue.', 'First sign-in with a temporary password', 'Choose a new password (section 5.6).'],
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
              ['Single sign-on ends in the browser and does not return to the app', '`AppRedirectUri` differs between API, portal and app build, or `PortalBaseUrl` is empty', 'Make all three use `<scheme>://sso` and set `PortalBaseUrl` (sections 2.4, 2.5, 3.4).'],
              ['Quizzes or single sign-on do not open', '`PortalBaseUrl` empty, or set to `localhost`', 'Set `Mobile:PortalBaseUrl` to a portal address phones can open (section 2.4).'],
              ['The sign-in screen shows the wrong institution name', 'Portal branding, or connected to the wrong server', 'Correct it in Dashboard Settings (section 4.1); check the server under the sign-in form.'],
              ['A parent sees no children', 'No active link (none made, or paused), or the student is not in a School department', 'Link the parent to each child on **Parent Links**, or select **Resume** (section 4.4).'],
              ['No push notifications', 'Firebase not set up (needs a branded build), notifications blocked on the phone, or the user switched that type off', 'Check `pushEnabled` (section 2.6), the phone\'s notification permission and the user\'s notification settings in the portal.'],
              ['A course or result is missing in the app', 'Not published, or the student is not enrolled', 'Check the same page in the portal: the app shows exactly what the portal shows.'],
              ['Times differ between the app and the portal', 'The app shows times in the phone\'s time zone; the portal shows them as the server keeps them', 'Nothing to fix: it is the same moment. If the app\'s times look wrong, check the phone\'s date and time settings.'],
              ['"Update" button does nothing', 'Store URL not set', 'Fill in `AndroidStoreUrl` / `IosStoreUrl`.'],
              ['Android will not install the APK', 'Installing from that source is not allowed, or Play Protect blocked it', 'Allow the source, or choose **Install anyway** (section 5.1). Use only the APK your institute shared.'],
            ] } },
            { note: ['tip', 'When reporting a problem, include the app version and the server from **Settings › About this app**, the exact message, and the time it happened. The API also receives crash reports from the app automatically (app version, phone model, the error and the user id; no other personal data).'] },
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
          ['Mobile:PortalBaseUrl', 'empty', 'Portal address phones open for single sign-on and quizzes'],
          ['Mobile:AppRedirectUri', 'edusphere://sso', 'Single sign-on return address'],
          ['Mobile:HandoffSeconds', '120', 'Life of the single sign-on code (30–600)'],
          ['Mobile:MaxConsecutiveFailures', '5', 'Failed pushes before a phone is switched off'],
          ['Mobile:Fcm:Enabled / ProjectId / ServiceAccountFile / ServiceAccountJson', 'off', 'Push notifications through Firebase'],
        ] } },
        { h3: 'Where the API and the portal listen' },
        { table: { head: ['Setting', 'Example', 'Purpose'], widths: [3300, 3200, 2800], rows: [
          ['--urls (command line)', '`--urls http://0.0.0.0:5181`', 'Listen on every network card (institute\'s network)'],
          ['ASPNETCORE_URLS (environment)', '`http://0.0.0.0:5181`', 'The same, for services and scheduled tasks'],
          ['Firewall rule (PowerShell, admin)', '`New-NetFirewallRule … -LocalPort 5181,5063 -Profile Private`', 'Let phones on the Wi-Fi in'],
        ] } },
        { h3: 'Portal (appsettings.json)' },
        { table: { head: ['Setting', 'Purpose'], widths: [3300, 6000], rows: [
          ['EduApi:BaseUrl', 'The API the portal itself uses (the same API as the app; may stay `localhost` on the same server)'],
          ['Mobile:AppRedirectUri', 'Where the portal returns single sign-on to the app; same value as on the API'],
        ] } },
        { h3: 'App build' },
        { table: { head: ['Parameter', 'Purpose'], widths: [3300, 6000], rows: [
          ['(none)', 'Standard app: no server built in; users enter it (section 3.2)'],
          ['-AppName, -AppId', 'Name and store id (branded app)'],
          ['-ApiBaseUrl', 'The server built into a branded app (HTTPS)'],
          ['-AllowServerChange', 'Branded app: keep **Change server** (otherwise the server is fixed)'],
          ['-Color, -Scheme', 'Brand colour; single sign-on address scheme'],
          ['-Version, -Build', 'Version shown to users; store build number'],
          ['-Platform, -Apk', 'Android or iOS; APK instead of a Google Play bundle'],
          ['-GoogleServicesJson, -GoogleServiceInfoPlist', 'Firebase files for push'],
          ['-KeyStore, -KeyAlias', 'Google Play upload key (passwords from environment variables)'],
          ['dotnet properties', '`-p:ApiBaseUrl=…` presets a server (people can change it); `-p:LockServer=true` fixes it'],
        ] } },
      ],
    },
    {
      title: 'Appendix B   API Endpoints Used by the App',
      body: [
        'For IT staff checking firewalls, proxies and logs. All are under the server address users type, for example `https://edu.myschool.com` or `http://192.168.1.109:5181`.',
        { table: { head: ['Endpoint', 'Used for'], widths: [4300, 5000], rows: [
          ['GET /api/v1/mobile/config', 'The server check on the Connect screen; institution name, versions, languages, sign-in options (before sign-in)'],
          ['POST /api/v1/auth/login, /auth/refresh, /auth/logout', 'Password sign-in, keeping the session, signing out'],
          ['POST /api/v1/auth/force-change-password', 'Choosing a password at the first sign-in'],
          ['POST /api/v1/mobile/handoff/redeem', 'Finishing single sign-on (one-time code)'],
          ['GET /api/v1/mobile/home, /timetable, /courses, /courses/{id}', 'Home, Timetable, Courses and course pages'],
          ['GET /api/v1/mobile/results, /attendance, /report-cards, /announcements, /children', 'Results, attendance, report cards, parents\' screens'],
          ['GET /api/v1/mobile/homework, /attendance/records', 'Homework tab; attendance day by day (parents add ?studentProfileId=)'],
          ['GET /api/v1/mobile/scopes, /campus-summary', 'Institute and campus list; admins\' campus at a glance'],
          ['PUT /api/v1/auth/change-password', 'Settings › Change password'],
          ['/api/v1/assignment/…, /api/v1/attendance/…, /api/v1/announcement, /api/v1/discussion/…', 'Hand-in, grading, taking attendance, announcements, discussions'],
          ['/api/v1/notification/…', 'Notifications and Mark all as read'],
          ['POST /api/v1/mobile/devices, /devices/unregister', 'Push notification registration'],
          ['POST /api/v1/mobile/crash-reports', 'Automatic crash reports'],
        ] } },
      ],
    },
    {
      title: 'Appendix C   Quick Start Card',
      body: [
        'Copy this page for students, teachers and parents. Fill in your institute\'s server address first.',
        { table: { head: ['Step', 'What to do'], widths: [1200, 8100], rows: [
          ['1', 'Install **EduSphere** from Google Play or the App Store, or open the APK your institute shared (allow installing from that source when Android asks).'],
          ['2', 'Open the app. On **Connect to your institute**, type the server address: ______________________ and tap **Connect**.'],
          ['3', 'Check that your institute\'s name appears at the top of the sign-in screen.'],
          ['4', 'Sign in with your **portal username and password**. New accounts: your first password is your username; the app then asks you to choose your own.'],
          ['5', 'Tap **Allow** when the app asks to send notifications.'],
          ['6', 'Use the tabs at the bottom; Attendance, Results, Notifications and Settings are under **More**. Pull a screen down to refresh it.'],
          ['Help', '"Could not reach …": check the address and your internet (for a local server, the institute\'s Wi-Fi). Anything else: send your IT team the message and **Settings › About this app**.'],
        ] } },
      ],
    },
    {
      title: 'Appendix D   Glossary',
      body: [
        { table: { head: ['Term', 'Meaning'], widths: [2600, 6700], rows: [
          ['API', 'The EduSphere server program that the portal and the app both use. It holds the business rules and talks to the database.'],
          ['APK / AAB', 'Android app files: an APK installs directly; an AAB is uploaded to Google Play.'],
          ['Branded (white-label) app', 'The app built with an institution\'s own name, logo, colour and a fixed server address.'],
          ['Firebase Cloud Messaging (FCM)', 'Google\'s service that delivers push notifications to Android phones and, through Apple, to iPhones.'],
          ['Firewall', 'The part of Windows that decides which connections from other devices may reach the server PC.'],
          ['Hand-off code', 'A one-time code with which the portal passes a single sign-on to the app.'],
          ['HTTP / HTTPS', 'Web connections: HTTPS is encrypted and required on the internet; HTTP is not encrypted and accepted only on the institute\'s own network.'],
          ['Institute\'s own network (local network)', 'The Wi-Fi or cabled network inside the institute, with private addresses such as 192.168.x.x.'],
          ['Scheme', 'The start of an app address (for example `edusphere://`) that the phone sends to the app.'],
          ['Server address', 'What users type on the Connect screen: the address of their institute\'s EduSphere API, for example `edu.myschool.com` or `192.168.1.109:5181`.'],
          ['Single sign-on (SSO)', 'Signing in with the institution\'s own account (Microsoft, Google, OpenID Connect or SAML).'],
          ['Standard app', 'The EduSphere app that works with any institute\'s server; the server is entered in the app.'],
          ['Two-factor authentication (2FA)', 'A second sign-in step with a 6-digit code from an authenticator app.'],
        ] } },
        { h3: 'Document history' },
        { table: { head: ['Version', 'Date', 'Changes'], widths: [1300, 2200, 5800], rows: [
          ['1.0', '30 October 2026', 'First edition, for app version 1.0.0 with the Tabsan look (logo, colours and redesigned screens).'],
          ['1.1', DATE, 'One app for every institute: the server address is entered in the app (Connect to your institute, Change server). New: servers on the institute\'s own network (listening on the network, firewall, HTTP rules), giving users the server address, installing the APK, connection troubleshooting, the Quick Start Card. All screenshots retaken from the standard APK.'],
        ] } },
      ],
    },
  ],
};
