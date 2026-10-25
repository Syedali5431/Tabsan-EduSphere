// Chapters 5-7: academic setup, timetables and facilities, teaching and learning.
module.exports = [
  {
    chapter: 'Academic Setup',
    intro: [
      'Academic setup follows the order shown in the Setup Flow menu: departments first, then programs, then courses and their offerings, and finally enrolments. Each level depends on the one before it: a program needs a department, an offering needs a course, and an enrolment needs an offering.',
      'Students are created in User Import (Chapter 14) and then appear in the Students list.',
    ],
    sections: [
      {
        title: 'Departments', roles: 'SuperAdmin, Admin', index: ['Departments'],
        intro: [
          'Departments are the top level of the academic structure, such as Information Technology or Business Administration. Every program, course, teacher and student belongs to a department, and Admins manage only the departments assigned to them.',
          'The list shows each department\'s code, name, institution type and status.',
        ],
        steps: [
          'Open **Departments** from the Setup Flow menu.',
          'Click **Add Department**. A form opens on top of the page.',
          'Enter the **Department Code** and **Department Name**, and choose the **Institution Type**.',
          'Click **Create**. A confirmation message appears and the department is added to the list.',
          'To rename a department later, click **Edit**, change the name and save.',
          'To stop using a department without losing its history, click **Deactivate** and confirm. Click **Activate** to bring it back.',
        ],
        fields: [
          ['Department Code', 'A short, unique code, for example CS or BUS. It appears in lists and reports and cannot be reused within the same campus.'],
          ['Department Name', 'The full name, for example Computer Science.'],
          ['Institution Type', 'University, College or School. It decides how levels and results work for everything in the department and cannot be changed later.'],
        ],
        figs: [['flow:department-add/1', 'Departments list'], ['flow:department-add/3', 'Add Department form completed']],
        outcome: ['The new department is immediately available in the Department filters across the portal. If you are an Admin, it is assigned to you automatically so you can manage it straight away.'],
        notes: [['note', 'SuperAdmin also sees tenant and campus filters and the Admin Department Assignments panel, which controls which Admin users manage each department.']],
      },
      {
        title: 'Programs', roles: 'SuperAdmin, Admin, Faculty (view)', index: ['Programs'],
        intro: [
          'A program is a degree or course of study offered by a department, for example Bachelor of Science in Computer Science. Students are admitted to a program, and the program decides how many levels (semesters or classes) they study.',
        ],
        before: ['The department the program belongs to must already exist.'],
        steps: [
          'Open **Programs**.',
          'In **Create Program**, enter the **Code** and **Name**, choose the **Department**, and set **Total Levels**.',
          'Click **Create**. The program appears in the Program List.',
          'Use **Filter by Department** to narrow the list. Use **Rename** to correct a name and **Deactivate** to retire a program.',
        ],
        fields: [
          ['Code', 'A short code, for example BSCS.'],
          ['Name', 'The full program name as it should appear on certificates and transcripts.'],
          ['Department', 'The department that runs the program.'],
          ['Total Levels', 'The number of semesters (University) or classes (School and College). Students graduate after completing the last level.'],
        ],
        figs: [['testadmin:Programs', 'Programs page']],
        tips: ['A University program usually needs a degree rule before students can be audited for graduation (see Degree Rules).'],
      },
      {
        title: 'Courses and offerings', roles: 'SuperAdmin, Admin, Faculty (view)', index: ['Courses', 'Course offerings'],
        intro: [
          'A course is a subject, such as CS101 Introduction to Programming, with its credit hours and grading type. An offering is that course being taught in a particular semester or class, usually by one teacher. Students enrol in offerings, attendance is taken per offering, and marks are entered per offering.',
          'The page has two parts: the course catalogue on the left and the active offerings on the right.',
        ],
        before: ['The department exists, and for offerings the teacher has a Faculty account.'],
        steps: [
          'Open **Courses**, choose the **Department** in the scope filter and click **Apply Scope**.',
          'To add a course, click **Add Course**, complete the form and save.',
          'To teach a course this term, create an offering: choose the **Course**, optionally the **Faculty** member and the **Max Enrollment**, and save.',
          'When teaching finishes, switch the offering **Off** so it no longer accepts enrolments.',
        ],
        fields: [
          ['Course Code / Course Title', 'The subject code and full title, for example CS101 / Introduction to Programming.'],
          ['Credit Hours', 'The credit value used for GPA and degree audits.'],
          ['Grading Type', 'How the course is graded (for example by marks and grades).'],
          ['This course is semester-based', 'Tick for normal semester courses. Untick for courses that run over several semesters, then set the Number of Semesters or a Duration.'],
          ['Faculty (offering)', 'The teacher responsible for the offering. They will see it in their classes. Optional; it can be added later.'],
          ['Max Enrollment (offering)', 'The largest number of students who can enrol. Further students are placed on a waiting list.'],
        ],
        figs: [['testadmin:Courses', 'Courses and active offerings']],
        tips: ['Mark a course as core or elective where the option is shown; degree audits count core and elective credits separately.'],
      },
      {
        title: 'Enrollments', roles: 'SuperAdmin, Admin, Faculty (own courses)', index: ['Enrollments', 'Bulk enrollment', 'Roster'],
        intro: [
          'Enrollments connect students to the course offerings they are taking. A student must be enrolled in an offering to appear in its attendance sheet, mark sheet and gradebook.',
          'Staff enroll students in bulk: choose a department and a class or semester, tick the courses, tick the students, and enroll them all with one click. Each student is shown with their roll number and name, so you always know who you are selecting. Admins can manage every course in their departments; teachers can manage the courses they teach.',
        ],
        before: [
          'The course offerings exist for the class or semester (see Courses) and are open for enrollment.',
          'The students have student profiles (created in User Import) in the department.',
        ],
        steps: [
          'Open **Enrollments**. SuperAdmin users can first choose an **Institute**.',
          'Choose the **Department** and then the **Class** (School and College) or **Semester** (University). The list shows how many courses each class or semester has.',
          'Under **1. Courses**, tick the courses to enroll the students in. All open courses are ticked to start with. Each row shows the teacher, the seats used (for example 20 / 40), any waitlist, and whether the course is Open, Closed or Full.',
          'Under **2. Students**, the students of that department who are currently in that class or semester are listed with their **Roll No.**, **Name**, **Program** and level. Use **Search** (roll number or name) and **Program** to narrow the list, or **Show all department students** to include other levels. Students already in every ticked course are hidden.',
          'Tick the students, or click **Select all shown**. The bar at the bottom counts your selection, for example “5 students × 4 courses selected”.',
          'Click **Enroll N students in M courses**. A summary shows how many were enrolled, waitlisted, already enrolled or not enrolled, with the reason for each student who could not be enrolled.',
          'To see who is in a course, click **Roster** next to it. Tick students and click **Drop selected** to remove them, or **Download CSV** to save the list.',
        ],
        figs: [['flow:enrollment-bulk/2', 'Students of Commerce, Class 9 selected for enrollment in 4 courses'], ['flow:enrollment-bulk/3', 'Course roster with roll numbers, names and Drop selected']],
        outcome: [
          'Enrolled students immediately appear in the course’s attendance sheet, mark sheet and gradebook, and in their own My Enrolled Courses list. When a course is full, extra students are placed on its waitlist and move in automatically when a seat frees up.',
        ],
        tables: [
          {
            title: 'Why a student may not be enrolled', head: ['Reason shown', 'What to do'], widths: [3000, 6300], rows: [
              ['Unmet prerequisites', 'The student has not passed the course listed in brackets. Enroll them in that course first (see Prerequisites).'],
              ['Timetable clash detected', 'The course meets at the same time as another of the student’s courses. Admins can tick **Allow timetable clashes** and give a reason.'],
              ['Enrollment is closed for this offering', 'The course is closed. Admins can click **Open** next to it.'],
              ['Semester is closed', 'The class or semester has been closed and cannot take new students.'],
              ['Student belongs to a different institute', 'School, College and University students can only join their own institute’s courses.'],
              ['Already enrolled in this offering', 'Nothing to do: the student is already in the course.'],
            ],
          },
          {
            title: 'Enrollment statuses', head: ['Status', 'Meaning'], widths: [2200, 7100], rows: [
              ['Active', 'The student is taking the offering.'],
              ['Waitlisted', 'The offering was full; the student moves in automatically when a place becomes free.'],
              ['Dropped', 'The student left the offering. History is kept.'],
              ['Cancelled', 'The enrollment (or waitlist place) was cancelled by the institution.'],
            ],
          },
        ],
        tips: [
          'Admins can stop all enrollment changes for a campus with **Deactivate Enrollment**, and turn it back on the same way.',
          'Students can also enroll themselves in open courses from their own Enrollments page.',
        ],
        notes: [['note', 'Courses with prerequisites can only be enrolled in once the student has passed the prerequisite course (see Prerequisites).']],
      },
      {
        title: 'Students', roles: 'SuperAdmin, Admin, Faculty', index: ['Students'],
        intro: [
          'The Students page is a searchable directory of student profiles. Each row shows the registration number, full name, department, program, current level and status.',
          'Faculty see the students of the departments they teach; Admins see the students of their assigned departments.',
        ],
        steps: ['Open **Students**.', 'Choose a **Department** to filter the list.', 'Scan or search the list. The number beside Student List shows how many students match.'],
        figs: [['flow:students-filter/1', 'Student list filtered by department']],
        tables: [{
          title: 'Student statuses', head: ['Status', 'Meaning'], widths: [2200, 7100], rows: [
            ['Active', 'Currently studying.'],
            ['Inactive', 'Studies paused or the account was deactivated.'],
            ['Graduated', 'Completed the program. Graduated students keep read access to their records.'],
          ],
        }],
        notes: [['tip', 'New students are added through User Import (single user or CSV). Promotion between semesters is handled in Student Lifecycle.']],
      },
    ],
  },

  {
    chapter: 'Timetables and Facilities',
    intro: ['Timetables tell teachers and students when and where each class happens. Buildings and rooms are set up first so they can be chosen when timetable entries are added.'],
    sections: [
      {
        title: 'Buildings and rooms', roles: 'SuperAdmin, Admin', index: ['Buildings', 'Rooms'],
        intro: ['Buildings and rooms describe where classes take place. They are used when adding timetable entries, so rooms show correctly on teacher and student timetables.'],
        steps: [
          'Open **Buildings**. Under **Add Building**, enter the **Name** and **Code** and click **Create**.',
          'To rename a building or switch it off, click its name in the list; an edit panel opens with **Save** and **Deactivate**.',
          'Open **Rooms** (or click **Rooms** next to a building). Choose the building, enter the **Room Number** and optional **Capacity**, and click **Create**.',
        ],
        fields: [
          ['Building Name / Code', 'For example Faculty Block A / FBA. The code is shown on timetables, so keep it short.'],
          ['Room Number', 'The room label, for example 101 or Lab 2.'],
          ['Capacity', 'Optional. The number of seats, useful when planning large classes.'],
        ],
        figs: [['testadmin:Buildings', 'Buildings'], ['testadmin:Rooms', 'Rooms']],
      },
      {
        title: 'Timetable Admin', roles: 'SuperAdmin, Admin', index: ['Timetable'],
        intro: [
          'Timetable Admin builds the weekly class timetable for each program level, for example "7th Semester of BBA". A timetable has an effective date and a list of entries; each entry is one class on one day.',
        ],
        before: ['The program, its courses, the teachers and the rooms already exist.'],
        steps: [
          'Open **Timetable Admin** and set the **Scope Selection** (campus and department), then click **Apply Scope**.',
          'Under **Create Timetable**, choose the **Degree Program**, **Semester** and **Semester Number**, set the **Effective Date**, and click **Create Timetable**.',
          'Select the new timetable in **Available Timetables**.',
          'Under **Add Entry**, choose the **Day**, **Start** and **End** time, **Subject**, **Teacher**, **Building** and **Room**, then click **Add Entry**.',
          'Repeat for every class in the week. The entries appear in a table ordered by day and time.',
          'Publish the timetable so that students can see it in Student Timetable.',
        ],
        fields: [
          ['Effective Date', 'The date from which the timetable applies. Create a new timetable when the schedule changes, rather than editing an old one, to keep history.'],
          ['Subject (Fallback) / Room (Fallback)', 'Free-text alternatives for a subject or room that is not in the lists, for example "Sports Ground".'],
        ],
        figs: [['testadmin:TimetableAdmin', 'Timetable Admin']],
        notes: [['note', 'SuperAdmin must select both a tenant and a campus before creating or editing entries.']],
      },
      {
        title: 'Teacher Timetable', roles: 'SuperAdmin, Admin, Faculty', index: ['Teacher Timetable'],
        intro: [
          'Teacher Timetable shows every class assigned to one teacher across all published timetables, so a teacher can see their whole week in one place.',
          'Faculty see their own timetable straight away. Admins choose a **Faculty** member and click **Apply**. Tick **Show deactivated** to include timetables that are no longer in use.',
        ],
        figs: [['faculty:TimetableTeacher', 'Teacher Timetable (Faculty view)']],
      },
      {
        title: 'Student Timetable', roles: 'Student', index: ['Student Timetable'],
        intro: [
          'Students see the published timetables for their own department. Choose a **Timetable** and, if you only want one day, a **Day Filter**, then click **Apply**. Each row shows the day, time, subject, teacher, building and room.',
        ],
        figs: [['student:TimetableStudent', 'Student Timetable']],
        tips: ['If no timetable appears, it may not be published yet. Check with your department office.'],
      },
    ],
  },

  {
    chapter: 'Teaching and Learning',
    intro: ['This chapter covers the pages teachers use every week and what students see on the other side: assignments, attendance, marks, online course content (modules, live classes, external tools and SCORM packages), discussions and announcements. Quizzes and the detailed marking of assignments are in Chapter 8, and learning outcomes in Chapter 9.'],
    sections: [
      {
        title: 'Assignments', roles: 'SuperAdmin, Admin, Faculty, Student', index: ['Assignments'],
        intro: [
          'Assignments are pieces of coursework set for an offering. Teachers create and publish them, students hand in text and/or files before the due date, and teachers mark each submission in the grading workspace with marks, a rubric, annotations and comments.',
        ],
        steps: [
          'Open **Assignments** and choose the department and offering.',
          'Faculty: create an assignment with a **Title**, **Description**, **Due Date** and **Max Marks**, then publish it so students can see it.',
          'Faculty: click **Options** next to the assignment to set when work can be handed in, give individual students their own dates, make it group work, add peer review or switch on similarity checking (Chapter 8).',
          'Students: open the assignment, type your answer and/or attach files, and hand in before the due date. If your teacher allows more than one attempt, each new hand-in is kept as a separate attempt.',
          'Faculty: click **Grade** to open the grading workspace and mark each submission (Chapter 8).',
          'Tick **Show inactive assignments** to see assignments that have been retired.',
        ],
        fields: [
          ['Title / Description', 'What the students must do. Include any length, format or referencing requirements in the description.'],
          ['Due Date', 'The deadline shown to students. Students given their own dates see those instead.'],
          ['Max Marks', 'The highest mark available; used to calculate percentages and grades.'],
          ['Submission settings', 'Under **Grade** > **Settings**: attempts allowed (0 = unlimited), accepted file types, maximum file size, when students see grades (as soon as they are saved, or only when you post them), anonymous grading and moderated grading.'],
        ],
        figs: [['flow:faculty-assignments/1', 'Assignments (Faculty)'], ['student:Assignments', 'Assignments (Student view)']],
        tables: [{
          title: 'Submission statuses', head: ['Status', 'Meaning'], widths: [2200, 7100], rows: [
            ['Submitted', 'The student has handed in work; waiting for grading.'],
            ['Graded', 'Marks and feedback are available to the student.'],
            ['Rejected', 'The teacher returned the submission; the student should follow the feedback.'],
          ],
        }],
      },
      {
        title: 'Entering attendance', roles: 'SuperAdmin, Admin, Faculty', index: ['Attendance', 'Enter Attendance'],
        intro: [
          'Attendance is recorded for each offering and each class date. Teachers usually record it at the end of every class. The page also shows a running summary for the offering.',
        ],
        before: ['Students must be enrolled in the offering to appear in the attendance table.'],
        steps: [
          'Open **Enter Attendance**.',
          'Choose the **Department**.',
          'Choose the **Course** (program).',
          'Choose the offering in **Select Course Offering**. The Attendance Summary and the Attendance Entry Table load.',
          'In the entry table, tick **Present** for each student who attended (untick for absent) and click **Save Attendance**.',
          'To load a whole class at once, click **Get Template**, fill in the CSV in a spreadsheet, and use **Import Attendance**. Tick **Strict mode** to reject the whole file if any row is invalid.',
        ],
        figs: [['flow:attendance-entry/1', 'Enter Attendance – scope filters'], ['flow:attendance-entry/2', 'Step 1: department selected'], ['flow:attendance-entry/3', 'Step 2: course selected'], ['flow:attendance-entry/4', 'Step 3: offering selected – attendance summary per student']],
        tables: [{
          title: 'Attendance statuses', head: ['Status', 'Meaning'], widths: [2200, 7100], rows: [
            ['Present', 'Attended the class.'],
            ['Late', 'Attended but arrived late. Counts as present in percentages.'],
            ['Absent', 'Did not attend.'],
            ['Excused', 'Absent with permission.'],
          ],
        }],
        tips: [
          'Click **View Low Attendance (<75%)** to list students below the attendance threshold so you can follow up early.',
          'Mistakes can be corrected later; corrections are recorded in the audit log.',
        ],
      },
      {
        title: 'Viewing attendance', roles: 'Faculty, Student', index: ['Attendance'],
        intro: [
          'The Attendance page shows totals and percentages. Students see one row per course with the number of classes held, the number attended and the percentage, coloured green (75% or more), amber (50–75%) or red (below 50%).',
          'A separate **Low Attendance (<75%)** list shows any courses where the student is below the threshold.',
        ],
        figs: [['student:Attendance', 'Attendance (Student view)']],
        notes: [['important', 'Many institutions require at least 75% attendance to sit final exams. Students should contact their teacher early if their percentage is low.']],
      },
      {
        title: 'Entering results', roles: 'SuperAdmin, Admin, Faculty', index: ['Results', 'Enter Results'],
        intro: [
          'Marks are entered per subject and exam type, for example the Midterm of CS101. The page requires the department, program, subject and exam type to be chosen first, so that marks are always stored against the right assessment.',
          'Results stay private until they are published. After publishing, students can see them immediately.',
        ],
        before: ['Students are enrolled in the offering, and Result Calculation is set up so marks can be turned into grades.'],
        steps: [
          'Open **Enter Results** and choose the **Department**.',
          'Choose the **Program/Course**.',
          'Choose the **Subject**.',
          'Choose the **Exam Type** and click **Apply**.',
          'Click **+ Enter Result**, choose the **Student**, enter **Marks Obtained** and **Max Marks**, and save. Repeat for each student, or download the CSV with **Get Template**, fill it in and upload it with **Import Results**.',
          'Review the Result Entry Grid. When it is correct, click **Publish All** to release the results to students.',
          'Use **Export CSV** or **Export PDF** to keep a copy.',
        ],
        fields: [
          ['Exam Type', 'The assessment: Midterm, Final, Quiz, Assignment, Practical, Theory or Internal.'],
          ['Marks Obtained / Max Marks', 'The student\'s mark and the highest possible mark. The grade is calculated automatically.'],
          ['strict (import)', 'When ticked, the whole file is rejected if any row has a problem, so you never get a half-imported mark sheet.'],
        ],
        figs: [['flow:results-entry/1', 'Step 1: department'], ['flow:results-entry/4', 'Steps 2–4: program, subject and exam type applied; the Result Entry Grid']],
        outcome: [
          'Published marks appear on the student\'s Results page, in the Gradebook and in result reports. The student\'s semester GPA and CGPA (University) or percentage (School and College) are recalculated.',
          'To change a published mark, a teacher submits a modification request with the proposed marks and a reason; an Admin approves or rejects it. Every change is recorded in the audit log.',
        ],
        notes: [['important', 'Check marks carefully before clicking Publish All. Students are notified and can see published results straight away.']],
      },
      {
        title: 'Gradebook', roles: 'SuperAdmin, Admin, Faculty', index: ['Gradebook'],
        intro: [
          'The Gradebook shows every student\'s marks for every assessment of one offering in a single grid, so a teacher can see the whole class at a glance and spot missing marks.',
          'Choose a **Course Offering** to load the grid. You can also upload marks in bulk from a CSV file, review the preview, confirm it, and publish all marks for the offering.',
        ],
        figs: [['flow:faculty-gradebook/1', 'Gradebook for a course offering']],
      },
      {
        title: 'Rubric Management', roles: 'SuperAdmin, Admin, Faculty', index: ['Rubrics'],
        intro: [
          'A rubric is a marking guide: a list of criteria (for example Content, Structure, Referencing) and the performance levels for each, with points. Rubrics make grading fair and consistent and help students understand how they will be assessed.',
          'Choose a **Course Offering**, enter a **Rubric Title**, then add the criteria and levels with their points. Rubrics are used when grading assignments.',
        ],
        figs: [['testadmin:RubricManage', 'Rubric Management']],
      },
      {
        title: 'Course modules (LMS Manage)', roles: 'SuperAdmin, Admin, Faculty', index: ['LMS', 'Modules', 'Module items', 'Release conditions'],
        intro: [
          'LMS Manage builds a course\'s online content as **modules**, usually one per week or topic. A module has a title, a week number and an introduction, and holds **items** in the order students should follow them: text pages, web links, course materials, videos, assignments, quizzes, discussions, external tools, SCORM packages and headings.',
          'Each item can have a **requirement** that completes it, and each module can have **release rules**: an opening date, prerequisites and "in order" progression. Students then always know what to do next, and teachers can see how far each student has got.',
        ],
        before: ['Create the assignments, quizzes and materials you want to add first; they can then be picked from lists.'],
        steps: [
          'Open **LMS Manage** and choose the offering.',
          'Under **Add New Module**, enter the **Title** and **Week #**, write the introduction, and click **Add Module**. Click **Publish** to make it visible to students (**Unpublish** hides it again while you edit).',
          'Open the module\'s **Items** and use **Add an item** to add items in order. The form only asks for what each item type needs: **Text** for a page, a **Web address** for a link, or the assignment, quiz, discussion, tool or package to use. Click **Add item**.',
          'For each item choose **To complete it, students must**: view it, mark it as done, submit it, score at least a percentage (**Minimum score (%)**, from a posted grade or the best quiz attempt) or post in it (discussions).',
          'Under **Release and completion**, optionally set **Opens on (UTC, optional)**, tick **Students must work through items in order**, choose whether the module is complete when students finish **every item with a requirement** or **any one item with a requirement**, and tick the modules under **Open only after these modules are complete**. Click **Save release settings**.',
          'Use the arrows to reorder items, **Edit** to change one (untick **Visible to students** to hide it) and **Remove** to delete it.',
        ],
        fields: [
          ['Item', 'Header, Page, Web link, Course material, Video, Assignment, Quiz, Discussion, External tool or SCORM package.'],
          ['Title', 'Leave empty to use the name of the assignment, quiz or other item you picked.'],
          ['To complete it, students must', 'Nothing, view it, mark it as done, submit it, score at least a minimum percentage, or contribute (discussions). Headers cannot have a requirement.'],
          ['Indent', 'Indents the item under the header above it, to show structure.'],
          ['Opens on (UTC, optional)', 'The module stays locked until this date and time.'],
          ['Open only after these modules are complete', 'Prerequisite modules. A module cannot depend on itself in a loop.'],
          ['Students must work through items in order', 'Each item unlocks only when the required item before it is complete.'],
        ],
        figs: [['flow:modules-teacher/1', 'LMS Manage: modules, items, requirements and release rules'], ['flow:modules-teacher/2', 'External tools and SCORM packages on the same page']],
        outcome: [
          'Students see the module on their **Course Content** page with each item\'s requirement and status. Locked quizzes, assignments and tools cannot be started or handed in until the module opens.',
          'Changing an item\'s requirement re-checks it for everyone. Once a requirement is met it stays met, so a later regrade never locks a student out.',
        ],
      },
      {
        title: 'Module progress', roles: 'SuperAdmin, Admin, Faculty', index: ['Module progress'],
        intro: ['**Module Progress** (the **Student progress** link at the top of LMS Manage) lists every student in the course with their overall percentage and how many required items they have completed in each module. Click a student\'s name to see their item-by-item detail, including what is still to do and why an item is locked.'],
        figs: [['flow:modules-teacher/3', 'Module Progress for a course']],
        tips: ['Use it before a deadline to see who has not opened the week\'s material, and send them a reminder through Announcements or Discussion.'],
      },
      {
        title: 'Course Content (students)', roles: 'Student', index: ['Course Content', 'Progress'],
        intro: [
          '**Course Content** shows a student\'s courses as modules, with their overall progress at the top. Choose the course in the **Course** box and click **Open**.',
          'Each item shows a symbol: ✓ done, ○ still to do (with what is needed, for example "Submit" or "Score at least 60%"), and 🔒 locked, with the reason, such as "Complete Week 1 first" or an opening date.',
        ],
        steps: [
          'Open **Course Content**, choose the course and click **Open**.',
          'Work through the items in order. Click an item to open it; pages, links, files and videos open inside the portal, while assignments, quizzes, tools and packages open their own pages.',
          'At the top of an item use **‹ Previous** and **Next ›** to move through the module, and **Mark as done** where the item asks for it.',
        ],
        figs: [['flow:modules-student/1', 'Course Content: progress, modules and item requirements'], ['flow:modules-student/2', 'An item open, with Previous and Next']],
        tips: ['The course page also links to the course\'s Discussion, Announcements, Live classes and My groups.'],
      },
      {
        title: 'Live classes', roles: 'SuperAdmin, Admin, Faculty, Student', index: ['Live classes', 'Online classes'],
        intro: [
          'Live Classes schedules online classes for a course. A class can use a meeting link the teacher pastes (Google Meet, Jitsi, any Zoom or Teams link), or the institution\'s connected service (BigBlueButton, Zoom or Microsoft Teams) creates the meeting automatically. Students are notified when a class is scheduled, moved or cancelled, and reminded 15 minutes before.',
          'Joining through the portal can mark attendance: about 10 minutes after the class ends, students who joined (and stayed the minimum time, where the service reports it) are marked Present and the others Absent. Students who already have an attendance mark for that day keep it.',
        ],
        steps: [
          'Faculty: open **Live Classes** and choose the course.',
          'Under **Schedule a class**, enter the **Title**, the start date and time (UTC), the length in **Minutes**, an optional description and, if your institution has no meeting service, the **Meeting link**.',
          'Tick **Mark attendance from who joined** and set the **Minimum minutes** if attendance should be recorded automatically, then click **Schedule**.',
          'To schedule a whole term at once, use **From the timetable**: pick the course\'s timetable slot and the number of weeks; weeks already scheduled are skipped.',
          'At class time click **Start / join** (you join as the host). **Attendance** shows who joined, and **Mark attendance now** records it immediately. **Edit** and **Cancel** notify the students; **Add to calendar** downloads a calendar file.',
          'Students: open **Live Classes** (or the course page) and click **Join** from 15 minutes before the start. While a class is running, a **Live now** banner with **Join** appears on every page.',
        ],
        figs: [['flow:live-classes/1', 'Live Classes (Faculty): scheduled classes and the scheduling form'], ['flow:live-classes-student/1', 'Live Classes (Student)']],
        tips: ['Recordings appear on the class when the meeting service provides one.', 'An Admin or SuperAdmin connects the meeting service in Live Class Settings (Chapter 16).'],
      },
      {
        title: 'External tools (LTI) in a course', roles: 'SuperAdmin, Admin, Faculty, Student', index: ['External tools', 'LTI'],
        intro: [
          'External tools are other learning applications registered by your institution (Chapter 16), such as interactive content, video, coding or plagiarism tools. They open from the course without a second sign-in, using the LTI 1.3 standard, and graded tools send scores back to EduSphere.',
        ],
        steps: [
          'Faculty: open **LMS Manage** for the course and find **External tools**.',
          'Click **Add an external tool**, choose the tool, enter a **Title**, choose where it opens (**New tab** or **Inside EduSphere**) and, for a graded activity, the **Points**. A graded item creates an assignment that receives the tool\'s scores automatically.',
          'Or click **Choose content from** the tool to pick content inside the tool itself (for example one activity); it is added to the course, with an assignment for anything the tool grades.',
          'Use **Hide** / **Show** to control visibility, and **Grades** to open the grading workspace, where you can still adjust or post the grades.',
          'Students: click **Open** on the item on the course page or in a module. You are signed in to the tool automatically.',
        ],
        figs: [['flow:modules-teacher/2', 'External tools and SCORM packages in LMS Manage']],
        tips: ['If a tool shows a blank frame inside EduSphere, set it to open in a new tab.', 'If a tool says the launch has expired, go back to the course and click Open again.'],
      },
      {
        title: 'SCORM packages', roles: 'SuperAdmin, Admin, Faculty, Student', index: ['SCORM', 'Interactive lessons'],
        intro: [
          'SCORM packages are interactive lessons exported from authoring tools such as Articulate Storyline or Rise, iSpring, Adobe Captivate or Lectora, as a SCORM 1.2 or SCORM 2004 zip file. EduSphere plays them inside the portal and records each learner\'s progress, status, score and time, and can send the best score to the gradebook.',
          'A package may have several parts (for example one lesson per topic, then a quiz). Students then see a contents list beside the lesson showing each part\'s status, and move between parts with it or with **Previous** and **Next**. The package is complete when every part is; its score is the average of the parts\' scores.',
        ],
        steps: [
          'Faculty: open **LMS Manage** for the course and, under **SCORM packages**, click **Upload a SCORM package**.',
          'Choose the zip file, and set the allowed **attempts** (0 = unlimited), whether it is **Graded** (the best score goes to the gradebook, scaled to the marks you set) and whether students can see it. Click **Upload**.',
          'Click **Preview** to try it yourself (nothing is recorded) and **Report** to see each student\'s attempts, status, best and last score and time. Add the package to a module with a requirement such as "Score at least 80%".',
          'Students: open the package from the course page or its module and click **Start**. Your place is saved: **Continue** takes you back where you stopped. Use **Exit** on the player page to leave (your progress is saved first).',
          'When you finish, the package opens again for **Review**; if more attempts are allowed, **Start a new attempt** – your best score counts.',
        ],
        figs: [['flow:scorm-student/1', 'A student\'s SCORM package page'], ['flow:scorm-student/2', 'A package in parts: contents list with each part\'s status'], ['flow:scorm-teacher/1', 'SCORM package report (Faculty)']],
        tips: [
          'Packages up to 500 MB are accepted by default. A package that links to content on other websites, or whose files are missing, is refused with an explanation.',
          'Packages run in an isolated area so their scripts cannot read portal pages or cookies.',
          'Pages in a package that do not report to the portal (assets) count as complete once opened. A package of such pages only has no score.',
        ],
      },
      {
        title: 'Course Material', roles: 'SuperAdmin, Admin, Faculty, Student', index: ['Course Material'],
        intro: [
          'Course Material is a library of files and links for each subject: lecture slides, notes, past papers or useful websites. Teachers add material; students browse and download it.',
        ],
        steps: [
          'Open **Course Material** and set the filters (department, program, semester, subject/course), then click **Apply Filters**.',
          'Click **Add Material**. Choose the **Type**, enter a **Title** and **Description**, and either upload a file or enter an **External URL**.',
          'Save. The material appears in the list for that subject.',
          'Untick **Active only** to see material that has been withdrawn, and use the row actions to edit or withdraw an item.',
        ],
        figs: [['testadmin:CourseMaterial', 'Manage Course Materials'], ['student:CourseMaterial', 'Browse Course Materials (Student)']],
        tips: ['Students see the material published for the subjects of their department; inactive material is hidden from them.'],
      },
      {
        title: 'Discussion', roles: 'SuperAdmin, Admin, Faculty, Student', index: ['Discussion'],
        intro: [
          'Discussion is a question-and-answer board for each offering. Students can ask questions, classmates and teachers can reply, and a teacher can mark a thread as resolved once it has been answered, which helps others find the answer later.',
        ],
        steps: [
          'Open **Discussion** and choose the offering.',
          'Under **Start a New Thread**, type your question and click **Post**.',
          'Open a thread to read the replies, add your own reply, or (teachers) mark it solved or unresolved.',
        ],
        figs: [['testadmin:Discussion', 'Discussion']],
      },
      {
        title: 'Announcements', roles: 'SuperAdmin, Admin, Faculty, Student', index: ['Announcements'],
        intro: [
          'Announcements publish notices, such as a room change or an exam reminder, to a whole department or to a single course offering. Students see announcements for the courses they take.',
        ],
        steps: [
          'Open **Announcements**.',
          'Choose a **Department** and, optionally, a **Course Offering**, then click **Apply** to see announcements in that scope.',
          'Staff: under **Post Announcement**, enter the title and message and click **Post**. With only a department chosen, the announcement goes to every offering in that department.',
          'Use **Deactivate** to hide an announcement without deleting it, or **Delete** to remove it permanently. Tick **Show inactive** to see hidden announcements.',
        ],
        figs: [['flow:announcement/1', 'Announcements for a department']],
        notes: [['note', 'An announcement posted to a whole department reaches every course offering in it. It is listed once with an "N offerings" label, and Deactivate or Delete applies to all of those offerings together.']],
      },
    ],
  },
];
