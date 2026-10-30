# EduSphere mobile app (Plan M12)

One app for **students, teachers and parents** (Android and iOS, .NET MAUI). The signed-in role decides the tabs:

| Role | Tabs and screens |
|---|---|
| Student | Home (today's classes, due work, announcements, attendance %, average marks — CGPA for University students, fees due) · Timetable · Courses (materials, assignments with hand-in by camera or file, quizzes — opened in the portal, announcements, discussions) · Results (results, attendance per course, report cards) · Notifications · Settings |
| Teacher | Home (classes today, courses, work to grade) · Timetable · Courses (take attendance, grade submissions, post announcements, discussions with the class) · Notifications · Settings |
| Parent | Home (children at a glance) · My children (results, attendance, report cards, fees, average marks (CGPA for University students), announcements per child) · Notifications · Settings |

Sign-in: username + password (+ authenticator code when 2FA is on), or the institution's single sign-on in the system
browser. Language: English, Urdu or Arabic (right to left), chosen in Settings; the API answers in the same language.

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

```bash
cd mobile
dotnet build Tabsan.EduSphere.Mobile/Tabsan.EduSphere.Mobile.csproj -c Release -p:MobileTargets=net9.0-android
```

On Windows the Android SDK and JDK paths may have to be given:
`-p:AndroidSdkDirectory="C:\Program Files (x86)\Android\android-sdk" -p:JavaSdkDirectory="C:\Program Files (x86)\Android\openjdk\jdk-17.0.14"`.
`-p:MobileTargets=net9.0-windows10.0.19041.0` builds a Windows desktop version for quick UI checks during development.

Development against a local API: build with `-p:ApiBaseUrl=https://10.0.2.2:7061` (Android emulator → host) and trust the
development certificate on the device, or use the public test link. Debug builds also accept plain http, e.g. the API on
`http://localhost:5181` from the emulator:

```bash
dotnet build Tabsan.EduSphere.Mobile/Tabsan.EduSphere.Mobile.csproj -c Debug -p:MobileTargets=net9.0-android -p:RuntimeIdentifier=android-x64 -p:EmbedAssembliesIntoApk=true -p:AndroidPackageFormat=apk -p:ApiBaseUrl=http://10.0.2.2:5181
```

Release builds stay https only. The Android emulator needs hardware acceleration on Windows (the Android Emulator Hypervisor
Driver, or Windows Hypervisor Platform); without it the image does not boot. `-p:MobileTargets=net9.0-windows10.0.19041.0
-p:ApiBaseUrl=http://localhost:5181` runs the same screens as a Windows window for a quick check.

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
- The properties end up in the app as `Resources/Raw/whitelabel.json` (name, API address, colour, scheme, version), the
  application id, the icon/splash colour and the Android sign-in intent filter.
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
  a test build on a real Android phone and iPhone (sign-in with password and SSO, push, camera hand-in, Urdu/Arabic),
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
sign-on; notifications arrive as soon as something changes. Available in English, Urdu and Arabic.

**Privacy notes for the stores**: data collected — name, email/username, academic records shown in the app, device push
token, crash diagnostics; used only to provide the service; not sold or shared for advertising; data stays on the
institution's EduSphere servers. Camera: only to photograph work you choose to hand in.
