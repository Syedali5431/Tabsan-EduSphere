# EduSphere mobile app (Plan M12)

One app for **students, teachers, parents and administrators** (Android and iOS, .NET MAUI). The signed-in role decides the tabs:

| Role | Tabs and screens |
|---|---|
| Student | Home (today's classes, due work, announcements, attendance %, average marks — CGPA for University students, fees due) · Timetable · Courses (materials, assignments, quizzes — opened in the portal, announcements, discussions) · Homework (hand-in by camera or file) · Attendance (overall, per course, day by day) · Results (results, report cards) · Notifications · Settings |
| Teacher | Home (classes today, courses, work to grade) · Timetable · Courses (post announcements, discussions) · Homework (grade submissions) · Attendance (take attendance per course) · Notifications · Settings |
| Parent | Home (children at a glance, a page per child) · Timetable · Courses · Homework · Attendance · Results — each for the chosen child · Notifications · Settings |
| Admin, SuperAdmin | Home (the chosen campus: students, teachers, courses, today's attendance) · Notifications · Settings |

The Timetable, Courses, Homework, Attendance and Results tabs share one choice (`Core/Selection.cs`): the child (parents), the
class (all classes or one) and the institute and campus (teachers and admins with more than one). Settings: theme (same as
the phone, light, dark), change password (current, then the new one twice), version and server, sign out.

Sign-in: username + password (+ authenticator code when 2FA is on), or the institution's single sign-on in the system
browser. The app is in English (the core keeps Urdu/Arabic texts, but the app no longer offers a language choice).

## One app for every institute

The standard build has no server built in. On first start it asks for the **institute's server address**, checks it
(`GET api/v1/mobile/config` must answer like an EduSphere API) and keeps it on the phone; the sign-in page then shows the
institution's name. So one APK serves every school, college and university, each with its own server and database.

| The institute's server | What people type | Notes |
|---|---|---|
| On the internet (cloud, hosted) | `edu.myschool.com` or `https://api.myschool.edu` | https only, with a trusted certificate (for example Let's Encrypt). Plain `http://` to an internet address is refused: passwords and student records would cross the internet unencrypted. |
| On the institute's own network | `192.168.1.10:5181`, `schoolserver:5181`, `server.school.local` | https is tried first, then http. Phones must be on that network's Wi-Fi (or a VPN). The app marks http connections "not encrypted". |
| Under a folder | `https://school.edu/edusphere` | Anything copied from a browser after the API (`/api/v1/…`, `/swagger`) is dropped. |

"Local network" means private and loopback IPs (10.x, 172.16–31.x, 192.168.x, 127.x, 169.254.x, 100.64–127.x for VPNs such
as Tailscale, IPv6 ULA/link-local), names without a dot, and `.local`, `.lan`, `.internal`, `.home.arpa`, `localhost`
names (`Mobile.Core/ServerAddress.cs`).

People change the server on the sign-in page (**Change server**) or in Settings (**Change server** signs out first, since
accounts belong to one server). Switching servers forgets the session of the old one.

**Preparing a server for the app** (the institute's IT team):

- The API must be reachable from phones, not only from the portal. A local install listens on localhost by default: start it
  with `--urls http://0.0.0.0:5181` (or set `ASPNETCORE_URLS`) and allow the port through the Windows firewall
  (`New-NetFirewallRule -DisplayName "EduSphere API" -Direction Inbound -Protocol TCP -LocalPort 5181 -Action Allow`).
- Set `Mobile:PortalBaseUrl` in the API's `appsettings.json` to the portal address phones can open (quizzes and some
  materials open in the portal, and single sign-on runs there).
- Give people the address to type, the same one for everyone.

**Builds**

| Build | Server | How |
|---|---|---|
| Standard | asked on first start | `dotnet publish … -p:AndroidPackageFormat=apk` (no `ApiBaseUrl`) |
| Preset | starts on the given server; people can still change it | `-p:ApiBaseUrl=http://10.0.2.2:5181` (development) |
| Institution's own (white-label) | fixed to the institution's server, no Change server | `build-whitelabel.ps1` (sets `-p:LockServer=true`; `-AllowServerChange` keeps it open) |

Why MAUI, and how the API was prepared: [Docs/Decisions/ADR-M12-mobile-technology.md](../Docs/Decisions/ADR-M12-mobile-technology.md).

## Layout

| Path | What |
|---|---|
| `src/Tabsan.EduSphere.Mobile.Core` (in the main solution) | Everything that is not drawing: API client, sign-in/SSO/refresh, version policy, crash reports, app texts, view models. Unit tests: `tests/Tabsan.EduSphere.UnitTests/MobileAppTests.cs` |
| `mobile/Tabsan.EduSphere.Mobile` | The MAUI app: screens (`Pages/Pages.cs`), tabs per role (`AppShell.cs`), platform services, Android/iOS files |
| `mobile/global.json` | Pins the .NET 9 SDK (MAUI 9 workload) for this folder |
| `mobile/build-whitelabel.ps1` | Store build for one institution |
| Server | `api/v1/mobile/*` (`MobileController`), `MobileService`, `MobileQueries`, `FcmPushSender`; portal hand-off in `LoginController` |

## Building

Requirements: .NET SDK 9.0.3xx with the `maui-android` (and on a Mac `maui-ios`) workload, Android SDK and JDK 17.
iOS builds need a Mac with Xcode.

The standard APK (any institute; signed with the debug key, so it installs on any phone but cannot go to Google Play):

```bash
cd mobile
dotnet publish Tabsan.EduSphere.Mobile/Tabsan.EduSphere.Mobile.csproj -c Release -f net9.0-android -p:MobileTargets=net9.0-android -p:AndroidPackageFormat=apk -o artifacts/standard
```

The APK is `artifacts/standard/com.tabsan.edusphere-Signed.apk`. For Google Play, sign with the upload keystore
(`-p:AndroidKeyStore=true -p:AndroidSigningKeyStore=… -p:AndroidSigningKeyAlias=… -p:AndroidSigningStorePass=env:…`, as
`build-whitelabel.ps1` does) and use `-p:AndroidPackageFormat=aab`.

On Windows the Android SDK and JDK paths may have to be given:
`-p:AndroidSdkDirectory="C:\Program Files (x86)\Android\android-sdk" -p:JavaSdkDirectory="C:\Program Files (x86)\Android\openjdk\jdk-17.0.14"`.
`-p:MobileTargets=net9.0-windows10.0.19041.0` builds a Windows desktop version for quick UI checks during development.

Development against a local API: the Android emulator reaches the PC as `10.0.2.2`, so type `10.0.2.2:5181` on the server
page, or preset it in a debug build:

```bash
dotnet build Tabsan.EduSphere.Mobile/Tabsan.EduSphere.Mobile.csproj -c Debug -p:MobileTargets=net9.0-android -p:RuntimeIdentifier=android-x64 -p:EmbedAssembliesIntoApk=true -p:AndroidPackageFormat=apk -p:ApiBaseUrl=http://10.0.2.2:5181
```

The Android emulator needs hardware acceleration on Windows (the Android Emulator Hypervisor Driver, or Windows Hypervisor
Platform); without it the image does not boot. `-p:MobileTargets=net9.0-windows10.0.19041.0 -p:ApiBaseUrl=http://localhost:5181`
runs the same screens as a Windows window for a quick check.

Parents: the **Parent** role is seeded at API start-up. Create parent accounts with the user import (role `Parent`), link them
to School students with `PUT api/v1/parent-portal/links` (admin), and they sign in to the app; imported accounts choose their
own password at the first sign-in, as in the portal.

## White-label builds (one app per institution, premium licence)

```powershell
$env:EDUSPHERE_KEYSTORE_PASS = '…'   # upload keystore password (never in the command line or the repository)
./build-whitelabel.ps1 -AppName "City University" -AppId edu.cityuni.app -ApiBaseUrl https://api.cityuni.edu `
    -Color "#7A1F2B" -Scheme cityuni -Version 1.2.0 -Build 12 `
    -GoogleServicesJson ./cityuni/google-services.json -KeyStore ./cityuni/upload.keystore -KeyAlias upload
```

- Produces an Android App Bundle (`.aab`, for Google Play; `-Apk` for an installable `.apk`) in `mobile/artifacts/<AppId>/<Version>-<Build>/`.
  `-Platform ios` (on a Mac) archives an `.ipa` and writes the scheme into `Info.plist`.
- The properties end up in the app as `Resources/Raw/whitelabel.json` (name, API address, server lock, colour, scheme,
  version), the application id, the icon/splash colour and the Android sign-in intent filter.
- The app is locked to `-ApiBaseUrl` (no server page, no Change server); `-AllowServerChange` keeps the institution's server
  as the start but lets people switch.
- **The institution's servers must match the scheme**: `Mobile:AppRedirectUri = "cityuni://sso"` in the API's and the
  portal's `appsettings.json` (the API tells the app; the portal redirects to it).
- Logo: the standard build carries the Tabsan logo (from the portal's `wwwroot/images`): `Resources/AppIcon/appicon.svg`
  (icon background) and `appiconfg.png` (the emblem inside the middle two thirds), `Resources/Splash/splash.png`, and
  `Resources/Images/tabsan_wordmark.png` / `tabsan_mark.png` (sign-in screen, banners, Settings). Replace them with the
  institution's artwork before building (keep the file names).
- Look: navy and teal (the portal's colours); `-Color` sets the title bar, banners, icon background and splash colour.
  The shared screen parts (cards, banner, chips, framed fields) are in `Pages/Ui.cs`.
- Connecting the app to an institution's portal, step by step with screenshots: `User Guide/Tabsan-EduSphere-Mobile-App-Connection-Guide.pdf`
  (built by `tools/user-manual/mobile-guide`).

## Push notifications

The API sends the same notifications as Web Push (assignments, results, attendance alerts, announcements, live classes),
honouring each user's per-type choices, through **Firebase Cloud Messaging HTTP v1**.

1. Create a Firebase project for the institution; add an Android app with the white-label application id (and an iOS app
   with its bundle id, uploading the APNs key in Firebase).
2. App: pass the downloaded `google-services.json` to `build-whitelabel.ps1` (`-GoogleServicesJson`) or place it at
   `Tabsan.EduSphere.Mobile/Platforms/Android/google-services.json`. Only then is Firebase compiled in (`FIREBASE`);
   without it the app works without push. iOS: `-GoogleServiceInfoPlist`.
3. API: in Firebase › Project settings › Service accounts, generate a private key and set
   `Mobile:Fcm:Enabled = true`, `Mobile:Fcm:ServiceAccountFile = <path to the JSON>` (or `ServiceAccountJson` from a secret
   store). Never commit the key.

The app registers its token after sign-in and whenever Firebase hands out a new one; signing out removes it. Tokens that
Firebase reports as gone are deleted; a device is switched off after `Mobile:MaxConsecutiveFailures` failed sends.
Tapping a notification opens the matching tab (results, courses, notifications …).

## Versioning and release policy

- **Version** `AppVersion` (semantic, shown to users, e.g. 1.2.0) and **build** `AppBuild` (whole number that must grow with
  every store upload). Patch = fixes, minor = new screens/features, major = changes that need a newer API.
- The app sends `X-App-Version` with every call. The API's `Mobile:MinimumVersion` blocks older apps with
  **426 Update required** (the app shows an Update button to the store link); `Mobile:LatestVersion` makes the app show
  "A new version is available". Raise `MinimumVersion` only after the new version is live in both stores.
- API changes stay backward compatible for at least two app minor versions; the mobile endpoints are additive.
- Release checklist: unit tests (`MobileAppTests`), integration tests (`MobileIntegrationTests`, `MobileHandoffWebIntegrationTests`),
  a test build on a real Android phone and iPhone (sign-in with password and SSO, push, camera hand-in, light and dark theme),
  store listing updated, `LatestVersion` raised on the servers.

## Crash reports

Unhandled errors are written to the device (`pending-crash.json`) and sent after the next sign-in to
`api/v1/mobile/crash-reports` (table `mobile_crash_reports`: app version, OS, device model, exception type, message, stack
trace, screen, time; the user id only — no other personal data). No third-party crash SDK is used.

## Store listing (default texts — institutions replace the name)

**Short description** (80 characters)
- EN: Your classes, coursework, results and attendance — EduSphere in your pocket.
- UR: آپ کی کلاسیں، کورس ورک، نتائج اور حاضری — EduSphere آپ کی جیب میں۔
- AR: حصصك وواجباتك ونتائجك وحضورك — EduSphere في جيبك.

**Full description (EN)**
EduSphere brings your institution to your phone. Students see today's classes, what is due, course materials and
announcements, hand in assignments with a photo or a file, take quizzes, and follow their results, attendance and report
cards. Teachers take attendance, grade submissions, post announcements and talk with their classes. Parents follow each
child's results, attendance, fees and report cards. Sign in with your EduSphere account or your institution's single
sign-on; notifications arrive as soon as something changes.

**Privacy notes for the stores**: data collected — name, email/username, academic records shown in the app, device push
token, crash diagnostics; used only to provide the service; not sold or shared for advertising; data stays on the
institution's EduSphere servers. Camera: only to photograph work you choose to hand in.
