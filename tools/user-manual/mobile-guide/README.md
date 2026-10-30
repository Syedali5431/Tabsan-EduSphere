# Mobile App Connection Guide generator

Builds `User Guide/Tabsan-EduSphere-Mobile-App-Connection-Guide.docx` and `.pdf` in the house style of the user manual
(the `docx`, `sharp` and `playwright-core` packages from `../node_modules`; Word for the contents page and the PDF).

| File | What |
|---|---|
| `guide-content.js` | The text (chapters, steps, tables, notes, figures). `**bold**` and `` `code` `` work everywhere. |
| `build-guide.js` | Lays it out: cover, contents, numbered figures (wide pictures and side-by-side phone screens), callouts, tables. `GUIDE_DRAFT=1` uses grey stand-ins for missing screenshots (layout checks only). |
| `diagram.js` | Draws `shots/diagram/connection.png`. |
| `capture-portal.mjs` | Portal screenshots `p01`–`p04` and `p06` (sign-in, branding, user import, the app's configuration); signs in as superadmin, saves nothing. |
| `create-parent.mjs` | Demo data: creates the parent `parent.imran` through User Import › Create Single User. |
| `capture-sync.mjs` | `p07`: the announcement posted from the phone, in the student's portal inbox. |
| `capture-parent-links.mjs` | `p08`: the portal's Parent Links screen (section 4.4); signs in as a campus admin, saves nothing. |
| `phone.ps1` | Drives the app on an Android phone over adb (`Tap`, `Tab`, `Type-Text`, `Scroll-Down`, `Shot` …); screenshots go to `shots/phone` without the status and navigation bars. |
| `shots/` | The screenshots the published guide uses. |

## Rebuilding

1. Run a test API and portal on a copy of the School demo data (never the live site):
   API with `EDUSPHERE_DB_CONNECTION` → the copy database, portal with `EduApi__BaseUrl` → that API.
2. Link the parent `parent.imran` (Imran Sheikh) to `TMS-GUL-09-01` and `TMS-GUL-04-02` (section 4.4 of the guide).
3. Build a Debug APK for the phone (`-p:ApiBaseUrl=http://localhost:5182 -p:RuntimeIdentifier=android-arm64`, see
   `mobile/README.md`), connect the phone with USB debugging, `adb reverse tcp:5182 tcp:<API port>`, `adb install -r …`.
4. Take the phone screenshots with `phone.ps1` (names as in `guide-content.js`: `a02-start` … `g04-child-more`), then:

```bash
cd tools/user-manual/mobile-guide
node diagram.js
node capture-portal.mjs http://localhost:5064 http://localhost:5182 <demo password>
node capture-sync.mjs http://localhost:5064 <demo password> "Science fair on Friday"
node capture-parent-links.mjs http://localhost:5064 <demo password> admin.tmsgul parent.imran Sheikh
node build-guide.js guide.docx
```

5. Word refreshes the contents page and exports the PDF (copy the source to a fresh file name first, as for the manual):

```powershell
$g = "..\..\..\User Guide"; Copy-Item guide.docx "$env:TEMP\mobile-guide-source.docx"
'[]' | Set-Content no-terms.json
..\finalize.ps1 -In "$env:TEMP\mobile-guide-source.docx" -Terms (Resolve-Path no-terms.json) `
  -OutDocx "$((Resolve-Path $g).Path)\Tabsan-EduSphere-Mobile-App-Connection-Guide.docx" `
  -OutPdf "$((Resolve-Path $g).Path)\Tabsan-EduSphere-Mobile-App-Connection-Guide.pdf"
```

Samsung phones: the app's username field has keyboard suggestions off, so `input text` types usernames as they are; if
Samsung Pass offers to save a password, `phone.ps1` callers tap **Cancel**.
