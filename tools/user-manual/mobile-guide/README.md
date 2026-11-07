# Mobile App Connection Guide generator

Builds `User Guide/Tabsan-EduSphere-Mobile-App-Connection-Guide.docx` and `.pdf` in the house style of the user manual
(the `docx`, `sharp` and `playwright-core` packages from `../node_modules`; Word or LibreOffice for the contents page and the PDF).

| File | What |
|---|---|
| `guide-content.js` | The text (chapters, steps, tables, notes, figures). `**bold**` and `` `code` `` work everywhere. |
| `build-guide.js` | Lays it out: cover, contents, numbered figures (wide pictures and side-by-side phone screens), callouts, tables. `GUIDE_DRAFT=1` uses grey stand-ins for missing screenshots (layout checks only). |
| `diagram.js` | Draws `shots/diagram/connection.png`. |
| `capture-portal.mjs` | Portal screenshots `p01`–`p04` and `p06` (sign-in, branding, user import, the app's configuration); signs in as superadmin, saves nothing. |
| `create-parent.mjs` | Demo data: creates the parent `parent.imran` through User Import › Create Single User. |
| `capture-sync.mjs` | `p07`: the announcement posted from the phone, in the student's portal inbox. |
| `capture-parent-links.mjs` | `p08`: the portal's Parent Links screen (section 4.4); signs in as a campus admin, saves nothing. |
| `phone.ps1` | Drives the app on an Android phone or emulator over adb (`Tap`, `Tab`, `Type-Into`, `Hide-Keyboard`, `Scroll-Down`, `Shot` …); screenshots go to `shots/phone` without the status and navigation bars. Finds adb through `$env:ADB`, `ANDROID_HOME` or the usual SDK folders. |
| `shots/` | The screenshots the published guide uses. |

## Rebuilding

1. Run the API and the portal on a database with the School demo data (`Scripts/Schools-And-Campuses`, then
   `Scripts/School-Demo-Data`), listening on the network so the phone reaches them like a phone on the school Wi-Fi
   (`--urls http://0.0.0.0:5181` / `:5063`, and `--Mobile:PortalBaseUrl=http://<PC address>:5063` for the API).
2. Create the parent `parent.imran` (Imran Sheikh) with `node create-parent.mjs <portal> <superadmin password>` and link
   him as Father to `TMS-GUL-09-01` (Fatima) and `TMS-GUL-04-02` (Hassan) on Parent Links (section 4.4) or with
   `PUT /api/v1/parent-portal/links`. His first sign-in in the app (screenshot `g01`) sets his own password; the 1.1
   screenshots used `Imran#Parent2026`.
3. Install the standard APK (`mobile/README.md`, "Building") on a phone or the emulator:
   `adb install -r mobile/artifacts/standard/com.tabsan.edusphere-Signed.apk`. The 1.1 screenshots come from an Android 14
   emulator (720 × 1280, light theme) connected to `192.168.1.109:5181`.
4. Take the phone screenshots with `phone.ps1` (names as in `guide-content.js`: `c01-server` … `a07-admin-campus`).
   `Start-EduApp -Fresh` clears the app, so it opens on the server page. Close the keyboard with `Hide-Keyboard`
   before tapping a button: an open keyboard covers the lower half of the screen. Then:

```bash
cd tools/user-manual/mobile-guide
node diagram.js
node capture-portal.mjs http://localhost:5063 http://<PC address>:5181 <demo password>
node capture-sync.mjs http://localhost:5063 <demo password> "Science fair on Friday"
node capture-parent-links.mjs http://localhost:5063 <demo password> admin.tmsgul parent.imran Sheikh
node build-guide.js guide.docx
```

5. Refresh the contents page and export the PDF.

   With **Word** (copy the source to a fresh file name first, as for the manual):

```powershell
$g = "..\..\..\User Guide"; Copy-Item guide.docx "$env:TEMP\mobile-guide-source.docx"
'[]' | Set-Content no-terms.json
..\finalize.ps1 -In "$env:TEMP\mobile-guide-source.docx" -Terms (Resolve-Path no-terms.json) `
  -OutDocx "$((Resolve-Path $g).Path)\Tabsan-EduSphere-Mobile-App-Connection-Guide.docx" `
  -OutPdf "$((Resolve-Path $g).Path)\Tabsan-EduSphere-Mobile-App-Connection-Guide.pdf"
```

   Without Word, with **LibreOffice** (its own Python runs the script):

```powershell
$g = (Resolve-Path "..\..\..\User Guide").Path
& "C:\Program Files\LibreOffice\program\python.exe" ..\finalize-libreoffice.py (Resolve-Path guide.docx) `
  "$g\Tabsan-EduSphere-Mobile-App-Connection-Guide.docx" "$g\Tabsan-EduSphere-Mobile-App-Connection-Guide.pdf"
```

Samsung phones: the app's username field has keyboard suggestions off, so `input text` types usernames as they are; if
Samsung Pass offers to save a password, `phone.ps1` callers tap **Cancel**.
