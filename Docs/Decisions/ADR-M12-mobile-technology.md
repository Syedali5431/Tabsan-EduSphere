# ADR M12 — Mobile app technology and API readiness

| | |
|---|---|
| Status | Accepted (2026-10-23) |
| Plan | M — Phase M12 *Native Mobile Apps*, Stage M12.1 |
| Deciders | EduSphere engineering |

## Context

The PWA (Plan M4) proved that students, teachers and parents want EduSphere on their phones, but institutions ask for
store apps (Google Play / App Store), native push notifications, the camera for handing in work and sign-in through their
identity provider (Plan M1). The team is a C#/.NET team; the server is .NET 8.

## Options considered

| Option | For | Against |
|---|---|---|
| **.NET MAUI** | Same language, tooling and team as the API and portal; one code base for Android and iOS; the app's logic can live in a plain .NET library that the main test suite already runs; WebAuthenticator (system browser + custom scheme) and SecureStorage built in | Rendering slower than Flutter on low-end Android; iOS builds need a Mac; MAUI releases are tied to .NET SDK versions |
| Flutter | Fast, consistent UI; large widget ecosystem | Dart — a second language and test stack; no code sharing with the server; a separate team skill |
| React Native | Large ecosystem | JavaScript/TypeScript stack beside .NET; bridging native modules |
| PWA only (M4) | Already shipped | No store presence (procurement checklists ask for it), weaker push on iOS, no system-browser SSO hand-off |

## Decision

1. **.NET MAUI**, with all behaviour in **`src/Tabsan.EduSphere.Mobile.Core`** (net8.0: API client, sign-in incl. 2FA and
   SSO hand-off, token refresh, version policy, crash reports, texts in English/Urdu/Arabic, view models). It is part of
   `Tabsan.EduSphere.sln` and unit-tested with the rest of the system. The MAUI project
   (`mobile/Tabsan.EduSphere.Mobile`) only draws screens (C#, no XAML pages) and wires platform services.
2. **One app with role tabs**, not three apps: students (home, timetable, courses, results), teachers (home, timetable,
   courses with attendance/grading/announcements), parents (children). The signed-in role decides the tabs. One store
   listing per institution is simpler to publish and to white-label.
3. **Quizzes stay in the portal** (opened in the browser): timers, access codes, lockdown and review rules already live
   there and must not be re-implemented.
4. **Single sign-on through the system browser with PKCE**: the app opens `/Login/Sso/{provider}?appChallenge=…`, the portal
   completes OIDC/SAML (and 2FA), asks the API for a one-time code bound to the challenge, ends its own session and redirects
   to `{scheme}://sso?code=…`; the app redeems the code with its verifier. Passwords for SSO accounts never touch the app.
5. **Native push through Firebase Cloud Messaging HTTP v1** (Android; iOS through Firebase's APNs relay), sent by the same
   dispatcher as Web Push and honouring the same per-type notification preferences.
6. **Target framework net9.0** for the MAUI head (the MAUI 9 workload is what the build machines have; `mobile/global.json`
   pins SDK 9.0.313 so the SDK 10 preview does not take over). The core library stays on net8.0 like the server.
7. **Crash reports are self-hosted** (`api/v1/mobile/crash-reports`, table `mobile_crash_reports`): no third-party SDK, no
   personal data beyond the user id, kept on the device when offline and sent at the next start.

## API readiness review

| Area | Finding | Action taken |
|---|---|---|
| Round trips | Phone screens would need 5–8 calls each (enrolments, offerings, timetable, assignments, quizzes, announcements, fees) | New read models under `api/v1/mobile/*`: home in one call per role, timetable, courses, course detail, results, attendance, report cards, children |
| Payload size / pagination | Notifications inbox already paged; other lists unbounded | Mobile lists are bounded: due work 14 days / 20 items, announcements 5 on home, report cards 20, timetable = one week; response compression already on |
| Mobile-friendly DTOs | Portal DTOs carry ids the phone must resolve | Mobile records carry names (course code/title, teacher, semester) and pre-computed percentages; nulls omitted |
| Refresh tokens | Rotation on refresh exists; sessions keep device info | App refreshes once behind a lock and signs out on failure; the hand-off creates its own session (`MobileApp`) |
| Access rules | Parent endpoints required the Parent role but results/attendance were split across controllers | One rule for mobile reads: a student sees their own profile, a parent only actively linked children, otherwise 403 |
| Versioning | No way to stop outdated apps | `X-App-Version` header; below `Mobile:MinimumVersion` → **426 Update required**; `api/v1/mobile/config` tells the app the latest version and store links |
| Push | Web Push only | Device registry (`mobile_devices`), FCM v1 sender with service-account JWT, dead tokens removed, devices switched off after 5 failures |
| Localisation | API messages follow Accept-Language (M11) | The app sends its language; mobile messages added to the Urdu/Arabic API catalogue |
| Rate limiting | Policies exist in Program.cs but are not attached to endpoints | Recorded for Plan M14 (scale hardening) |

## Consequences

- Android builds on Windows; **iOS builds and App Store submission need a Mac with Xcode** and an Apple developer account.
- Each institution's white-label build needs its own Firebase project files for push (`google-services.json`,
  `GoogleService-Info.plist`) and the matching `Mobile:AppRedirectUri` in its API settings.
- Moving the MAUI head to net10 later is a project-file change; the core library and server are unaffected.
- No offline data cache in this version: the session survives restarts, screens need a connection (the PWA keeps its
  offline pages for that case).
