// Chapters 5-7: academic setup, timetables and facilities, teaching and learning.
module.exports = [
  {
    chapter: 'Academic Setup',
    intro: [
      'Academic setup follows the order shown in the Setup Flow menu: departments first, then programs, then courses and their offerings, and finally enrolments. Each level depends on the one before it: a program needs a department, an offering needs a course, and an enrolment needs an offering.',
      'Students are created in User Import (Chapter 12) and then appear in the Students list.',
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
        title: 'Enrollments', roles: 'SuperAdmin, Admin, Faculty (view)', index: ['Enrollments'],
        intro: [
          'Enrollments connect students to the offerings they are taking. A student must be enrolled in an offering to appear in its attendance sheet, mark sheet and gradebook.',
        ],
        before: ['The offering exists and the student has a student profile (created in User Import).'],
        steps: [
          'Open **Enrollments**.',
          'Choose an offering in **Select Course Offering**. The Enrollment Roster lists the students already enrolled.',
          'Click **Enroll Student**, choose the student and click **Enroll**.',
          'To remove a student from the offering, click **Drop** and confirm.',
          'Admins can open or close enrolment for the whole tenant/campus with **Deactivate Enrollment** (and re-open it the same way).',
        ],
        figs: [['testadmin:Enrollments', 'Enrollments page']],
        tables: [{
          title: 'Enrollment statuses', head: ['Status', 'Meaning'], widths: [2200, 7100], rows: [
            ['Active', 'The student is taking the offering.'],
            ['Waitlisted', 'The offering was full; the student moves in automatically when a place becomes free.'],
            ['Dropped', 'The student left the offering. History is kept.'],
            ['Cancelled', 'The enrolment was cancelled by the institution.'],
          ],
        }],
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
    intro: ['This chapter covers the pages teachers use every week and what students see on the other side: assignments, attendance, marks, quizzes, course content, discussions and announcements.'],
    sections: [
      {
        title: 'Assignments', roles: 'SuperAdmin, Admin, Faculty, Student', index: ['Assignments'],
        intro: [
          'Assignments are pieces of coursework set for an offering. Teachers create and publish them, students submit text and/or a file before the due date, and teachers grade each submission with marks and feedback.',
        ],
        steps: [
          'Open **Assignments** and choose the department and offering.',
          'Faculty: create an assignment with a **Title**, **Description**, **Due Date** and **Max Marks**, then publish it so students can see it.',
          'Students: open the assignment, type notes in **Text Submission** and/or choose a file in **Upload File**, and submit before the due date.',
          'Faculty: open the assignment to review submissions. For each student enter **Marks Awarded** and **Feedback**, and save.',
          'Tick **Show inactive assignments** to see assignments that have been retired.',
        ],
        fields: [
          ['Title / Description', 'What the students must do. Include any length, format or referencing requirements in the description.'],
          ['Due Date', 'The deadline shown to students.'],
          ['Max Marks', 'The highest mark available; used to calculate percentages and grades.'],
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
        title: 'Quizzes', roles: 'SuperAdmin, Faculty; Students take quizzes in View Quizzes', index: ['Quizzes'],
        intro: [
          'Quizzes are short assessments for an offering. Teachers create them with instructions, an optional time limit and a maximum number of attempts, then publish them. Students see published quizzes in **View Quizzes**, prepare their answers and upload them as a file before the last date.',
        ],
        steps: [
          'Faculty: open **Quizzes** and choose the offering in **Select Course Offering**.',
          'Create a quiz with a **Title**, **Instructions** (the questions or what to answer), **Time Limit (minutes)** and **Max Attempts**.',
          'Click **Publish** when it is ready. Use **Deactivate** to close it early, or **Delete** to remove a quiz that was created by mistake.',
          'Students: open **View Quizzes**, read the instructions, attach your answer file and click **Submit** before the last date.',
        ],
        figs: [['flow:faculty-quizzes/1', 'Quizzes (Faculty)'], ['student:ViewQuizzes', 'My Quizzes (Student)']],
        tips: ['Tick **Include Inactive** to see closed quizzes. Students see their marks once the quiz is graded.'],
      },
      {
        title: 'LMS Manage', roles: 'SuperAdmin, Admin, Faculty', index: ['LMS'],
        intro: [
          'LMS Manage builds online learning content for an offering, organised by week. Each module has a title, a week number and a body that can contain formatted text, links and embedded videos.',
        ],
        steps: [
          'Open **LMS Manage** and choose the offering.',
          'Under **Add New Module**, enter the **Title** and **Week #**, and write the **Body** (HTML is allowed for headings, lists and links).',
          'Click **Add Module**. Add videos to a module where needed.',
          'Click **Publish** to make a module visible to students, or **Unpublish** to hide it again while you edit.',
        ],
        figs: [['testadmin:LmsManage', 'LMS Manage']],
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
