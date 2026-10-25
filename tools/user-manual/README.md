# User manual generator

Rebuilds `User Guide/Tabsan-EduSphere-User-Manual.docx` and `.pdf` from live screenshots.

Requirements: Node.js, Google Chrome, Microsoft Word (for the contents page, index and PDF export), and the API (`http://localhost:5181`) and Web (`http://localhost:5063`) running with the demo data and the `testadmin` account (`Scripts/08-Create-Test-Admin-User.sql`).

The online-learning chapters (7–9) and the integration pages need extra demo data in the CS702 Machine Learning course:

- Upload three SCORM packages to CS702 on **LMS Manage** as `faculty.it1` (the manual used Rustici's free *Golf Explained* samples from scorm.com: "course in parts" SCORM 1.2, "lessons and quizzes" SCORM 2004 and "run-time basics" SCORM 1.2), and play some of them as `bscs8s1` so the report and the contents list have data.
- Run `node demo-data.mjs` (below). It creates, through the real API, a question bank and a quiz with attempts and graded essays, assignments with a late policy, an extension, groups, peer reviews, similarity checks and a graded submission, modules with requirements and release rules, program and course outcomes with alignments, live classes, an external-tool link, xAPI credentials and an API token. It is safe to re-run.
- The `sso-login` and `sso-settings` flows need single sign-on set up for tenant `TABSAN-UNI` with a test identity provider (the manual used a local Keycloak; see `User Guide/SSO-Setup-Guide.md`). Without it those two flows record an error and the rest still run.

```bash
cd tools/user-manual
npm install
# 0. Demo data for the online-learning screenshots (CS702; see above)
node demo-data.mjs
# 1. Page screenshots for each role (written to out/<role>/)
node capture.mjs superadmin superadmin EduSphere147
node capture.mjs testadmin  testadmin  EduSphere147
node capture.mjs faculty    faculty.it1 EduSphere147
node capture.mjs student    bscs8s1    EduSphere147
node capture.mjs finance    finance1   EduSphere147
# 2. Step-by-step workflow screenshots (written to flows/); nothing is submitted.
#    The language flow switches bscs8s1 to Arabic and Urdu and back to English.
node flows.mjs all
# 2b. Enrollment workspace screenshots (written to flows/enrollment-bulk/, signs in as admin.sch; nothing is submitted).
#     Needs Scripts/09-Institution-Dummy-Data.sql for the Commerce/Arts Class 9 data.
node enrollment-shots.mjs
# 3. Build the Word document (manual text lives in content-part1.js … content-part5.js; content.js combines them in chapter order)
node build.js manual.docx
```

Then let Word add the index, refresh the contents page and export the PDF (use a fresh source file name each time — if Word was force-closed while opening a file, it silently asks about that file name on the next open):

```powershell
$g = "..\..\User Guide"; Copy-Item manual.docx "$g\manual-source.docx"
.\finalize.ps1 -In (Resolve-Path "$g\manual-source.docx") -Terms (Resolve-Path index-terms.json) `
  -OutDocx "$((Resolve-Path $g).Path)\Tabsan-EduSphere-User-Manual.docx" -OutPdf "$((Resolve-Path $g).Path)\Tabsan-EduSphere-User-Manual.pdf"
Remove-Item "$g\manual-source.docx"
```
