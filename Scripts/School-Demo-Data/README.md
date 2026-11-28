# School Demo Data

Fills the schools added by `../Schools-And-Campuses` with a term's worth of demo data, so that every page of the portal has something to show for every role: SuperAdmin, campus admin, finance officer, teacher and student.

## Run order

| # | Script | How it runs | What it adds |
|---|--------|-------------|--------------|
| 1 | `01-School-Records.sql` | sqlcmd | **People:** an admin and a finance officer per campus, five teachers per campus, six students per class (Class 1 to 10) and four new admissions waiting for a class. **Teaching:** five subjects per class, each taught by its teacher, with enrolments and prerequisites (Mathematics and English follow on class by class). **Records:** 25 school days of attendance, published Quiz and Midterm results, three months of tuition fee receipts (paid, proof submitted, pending) and admission fee receipts. **Campus:** a building with ten classrooms and two labs, and a weekly timetable per class. **Messages:** a welcome announcement and a study link per subject, school notifications. |
| 2 | `02-Learning-Activity.mjs` | Node.js, through the API | **Every subject:** a published class test taken by five of the six students (scores vary), two homework assignments (the first handed in by five students, four of them marked with feedback; the second due in about twelve days), a *Term 1* module (page, study link, homework, class test) with students' progress, a discussion thread with teacher and student replies, and an upcoming online revision class. **English and Mathematics:** programme and course learning outcomes, aligned to the class test questions with mastery calculated, and a marking rubric on the first homework. **Mathematics:** an interactive *Mathematics practice* (a small SCORM 1.2 package in `scorm/maths-practice`, zipped by the script) that saves its score to the course. **Class 9 and 10:** the *Science* stream for every student (the portal shows Class 9 to 12 students only the subjects of their stream), and a *Practice lab (LTI)* link in Computer Science if the installation has an LTI tool registered. **Every campus:** helpdesk tickets from students, every teacher and the finance officer (open, in progress, resolved) and an API token for the campus admin. **School settings:** result components (Quiz 20%, Midterm 30%, Final 50%) and grade points for School, online classes by meeting link, a learning record store (xAPI) credential, the Science, Commerce and Arts streams, and a sample privacy notice for the Privacy page if none is set (replace it with your own on Dashboard Settings). |

Run them after the `Schools-And-Campuses` scripts. Script 2 signs in as the teachers, students and admins that script 1 creates, so the API must be running and script 1 must have run first.

```powershell
cd Scripts\School-Demo-Data
.\Run-School-Demo-Data.ps1                                  # localhost\SQLEXPRESS, database Tabsan-EduSphere, API on http://localhost:5181
.\Run-School-Demo-Data.ps1 -Server "myserver" -Database "Tabsan-EduSphere" -Api "https://myserver/api/v1"
.\Run-School-Demo-Data.ps1 -RecordsOnly                     # only 01 (no API needed)
.\Run-School-Demo-Data.ps1 -ActivityOnly                    # only 02
```

or one at a time:

```powershell
sqlcmd -S "localhost\SQLEXPRESS" -d "Tabsan-EduSphere" -C -b -f 65001 -i "Scripts\School-Demo-Data\01-School-Records.sql"
node Scripts\School-Demo-Data\02-Learning-Activity.mjs http://localhost:5181/api/v1
node Scripts\School-Demo-Data\02-Learning-Activity.mjs http://localhost:5181/api/v1 TMS-GUL    # one campus only
```

Both are safe to re-run. Script 1 skips rows that exist (keyed on usernames, codes, registration and receipt numbers); script 2 skips anything it finds by title, so a run that stopped part-way can simply be started again. Script 2 needs Node.js 18 or later and takes a few minutes; campuses run in parallel.

Restart the API and the portal afterwards (or wait a few minutes) so cached menus, dashboards and settings pick up the new data.

On a database built from scratch: run `02-Seed-Core.sql`, the `Schools-And-Campuses` scripts, then **start the API once before script 1**. The API adds the Teacher, Student, Finance and Parent roles at its first start, and script 1 fails without them ("Cannot insert the value NULL into column 'RoleId'"). Install a licence (Settings → License Update, or `POST /api/v1/license/upload` as SuperAdmin) before script 2: on an unlicensed database every module is off and script 2 stops with "Module 'courses' is disabled".

## Demo sign-in: one login, any role

`04-Demo-Login-Accounts.sql` (run after script 1 and one API start) adds the accounts for a single demo login. With the API's `DemoLogin` settings switched on, people sign in as **`demo`** / `EduSphere147`; the mobile app then shows **Sign in as** with each role, and the session belongs to that role's account:

| Sign in as | Account | Shows |
|---|---|---|
| SuperAdmin | `superadmin` | every school and campus |
| Admin | `demo.admin` | Tabsan Model School, all three campuses |
| Teacher (`Faculty`) | `tmsgul.maths` | Mathematics in Class 1 to 10, Gulberg |
| Student | `tmsgul.c9s1` | Class 9, Gulberg |
| Parent | `demo.parent` | two children: `tmsgul.c9s1` and `tmsjt.c4s2` |

The portal has no role choice: `demo` opens the first role (SuperAdmin). API settings (environment variables, or the `DemoLogin` section of the API's `appsettings.json`):

```
DemoLogin__Enabled=true
DemoLogin__Accounts__0__Role=SuperAdmin   DemoLogin__Accounts__0__Username=superadmin
DemoLogin__Accounts__1__Role=Admin        DemoLogin__Accounts__1__Username=demo.admin
DemoLogin__Accounts__2__Role=Faculty      DemoLogin__Accounts__2__Username=tmsgul.maths
DemoLogin__Accounts__3__Role=Student      DemoLogin__Accounts__3__Username=tmsgul.c9s1
DemoLogin__Accounts__4__Role=Parent       DemoLogin__Accounts__4__Username=demo.parent
```

`DemoLogin` is off by default. **Never switch it on for a real institution**: anyone with the demo password can open every listed account.

## Sign-in names

Every account's password is the demo password `EduSphere147`. **Change the passwords, or delete these accounts, before real use.**

`<campus>` is the campus code in lower case without the dash: `tmsgul`, `tmsjt`, `tmsdha` (Tabsan Model School), `cpsmain`, `cpsnth` (Crescent Public School), `gvgscty`, `gvgsmt` (Green Valley Grammar School).

| Who | Username | Example |
|-----|----------|---------|
| Campus admin | `admin.<campus>` | `admin.tmsgul` |
| Finance officer | `finance.<campus>` | `finance.tmsgul` |
| Teachers | `<campus>.english`, `.urdu`, `.maths`, `.science`, `.social` | `tmsgul.maths` |
| Students | `<campus>.c<class>s<1-6>` | `tmsgul.c9s1` (Class 9, roll number 1) |
| New admissions | `<campus>.new<1-4>` | `tmsgul.new1` |

Students 1 to 3 of each class are the strongest and the furthest through their modules; student 5 scores lowest on the class tests; student 6 has missed several school days and has not handed in homework yet.

Subjects per class: Class 1 to 5 English, Urdu, Mathematics, General Science, Islamiyat; Class 6 to 8 English, Urdu, Mathematics, Science, Social Studies; Class 9 and 10 English, Urdu, Mathematics, Physics, Computer Science. The *science* teacher teaches Physics in Class 9 and 10, the *social* teacher Islamiyat, Social Studies and Computer Science.

## Your own data

- **Other schools:** script 1 works on every campus that has a School section from `../Schools-And-Campuses`; add your schools there first.
- **Fees, subjects, names:** the tables declared in the first part of `01-School-Records.sql` (`@Class` holds the monthly fee per class, `@Subject` the subjects and study links, the name lists the students' and teachers' names).
- **Class tests, homework, outcomes, tickets:** the subject content near the top of `02-Learning-Activity.mjs`.
- **Another password:** set `DEMO_PASSWORD` before running script 2 if you changed the accounts' password.

## Not included

An LTI tool: registering one needs the tool provider's details, so the scripts do not add one. Register your tool on **External Tools** (SuperAdmin) and run script 2 again to add the *Practice lab (LTI)* links. Other subjects have no SCORM package; add your own on **LMS Manage** (see the user manual, *Learning content*).
