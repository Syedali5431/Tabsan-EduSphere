# User manual generator

Rebuilds `User Guide/Tabsan-EduSphere-User-Manual.docx` and `.pdf` from live screenshots.

Requirements: Node.js, Google Chrome, Microsoft Word (for the contents page, index and PDF export), and the API (`http://localhost:5181`) and Web (`http://localhost:5063`) running with the demo data and the `testadmin` account (`Scripts/08-Create-Test-Admin-User.sql`).

```bash
cd tools/user-manual
npm install
# 1. Page screenshots for each role (written to out/<role>/)
node capture.mjs superadmin superadmin EduSphere147
node capture.mjs testadmin  testadmin  EduSphere147
node capture.mjs faculty    faculty.it1 EduSphere147
node capture.mjs student    bscs8s1    EduSphere147
node capture.mjs finance    finance1   EduSphere147
# 2. Step-by-step workflow screenshots (written to flows/); nothing is submitted
node flows.mjs all
# 3. Build the Word document (manual text lives in content.js)
node build.js manual.docx
```

Then let Word add the index, refresh the contents page and export the PDF (use a fresh source file name each time — if Word was force-closed while opening a file, it silently asks about that file name on the next open):

```powershell
$g = "..\..\User Guide"; Copy-Item manual.docx "$g\manual-source.docx"
.\finalize.ps1 -In (Resolve-Path "$g\manual-source.docx") -Terms (Resolve-Path index-terms.json) `
  -OutDocx "$((Resolve-Path $g).Path)\Tabsan-EduSphere-User-Manual.docx" -OutPdf "$((Resolve-Path $g).Path)\Tabsan-EduSphere-User-Manual.pdf"
Remove-Item "$g\manual-source.docx"
```
