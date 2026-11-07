# Installation and Connection Guide generator

Builds `User Guide/Tabsan-EduSphere-Installation-and-Connection-Guide.docx` and `.pdf`: how the database, API, portal and
mobile app connect, with a worked example of a second institute ("Northfield Model School") on the same PC. Same house
style and packages as `../mobile-guide` (`docx`, `sharp`, `playwright-core` from `../node_modules`).

| File | What |
|---|---|
| `guide-content.js` | The text (chapters, steps, tables, notes, figures). |
| `build-guide.js` | Lays it out (a copy of `../mobile-guide/build-guide.js` with this guide's titles; "…:" lead-ins keep with what follows). |
| `diagram.js` | Draws `shots/diagram/chain.png` and `two-institutes.png`. |
| `capture-portal.mjs` | `n01`–`n10`: the databases (sqlcmd), the API's and portal's first console lines (`shots/console/*.txt`, copied from the running programs), the API's Database Setup page (tested, not saved), the new portal, Dashboard Settings (**saves** the example's name), License Update, the app configuration. |
| `start-northfield.ps1` | Starts the example's API (`-Part api`, port 5281, database `EduSphere-Northfield`) or portal (`-Part portal`, port 5163) from a published folder. |
| `shots/phone` | `m01`–`m08`, taken with `../mobile-guide/phone.ps1` (dot-source it, then set `$script:ShotDir` to this folder's `shots\phone`). |

## Rebuilding

1. Publish the API and the portal into an empty folder, then start both with `start-northfield.ps1`. The demo SuperAdmin
   password of the example is the default in that script; a new database gets it on the API's first start.

```powershell
dotnet publish src/Tabsan.EduSphere.API/Tabsan.EduSphere.API.csproj -c Release -o <folder>\api
dotnet publish src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -c Release -o <folder>\portal
.\start-northfield.ps1 -Part api -Root <folder>
.\start-northfield.ps1 -Part portal -Root <folder>
```

2. Keep the main installation running too (ports 5181/5063), so the phone can move from it to Northfield.
3. Take the pictures, then build:

```bash
cd tools/user-manual/connection-guide
node diagram.js
node capture-portal.mjs http://localhost:5163 http://localhost:5281 http://<PC address>:5281 <Northfield superadmin password>
node build-guide.js guide.docx
```

   Phone screenshots (`m01`–`m08`): start on the main institute (signed in as superadmin), then Settings → Change server,
   type the portal's port (`:5163`, refused), then the API's (`:5281`), sign in, Home and Settings.

4. Contents page and PDF, as for the mobile guide (LibreOffice):

```powershell
$g = (Resolve-Path "..\..\..\User Guide").Path
& "C:\Program Files\LibreOffice\program\python.exe" ..\finalize-libreoffice.py (Resolve-Path guide.docx) `
  "$g\Tabsan-EduSphere-Installation-and-Connection-Guide.docx" "$g\Tabsan-EduSphere-Installation-and-Connection-Guide.pdf"
```
