// Chapters 8-9: quizzes and assessment (quiz engine, question banks, grading workspace, assignment options, groups,
// peer review, similarity) and learning outcomes.
module.exports = [
  {
    chapter: 'Quizzes and Assessment',
    intro: [
      'This chapter covers online quizzes, from building them and drawing questions from banks to marking and statistics, and the marking side of assignments: the grading workspace, late and missing work, personal due dates, group work, peer review and similarity checking.',
      'The screenshots use the CS702 Machine Learning course of the demonstration data, taught by faculty.it1.',
    ],
    sections: [
      {
        title: 'Quizzes', roles: 'SuperAdmin, Faculty; students take quizzes in View Quizzes', index: ['Quizzes'],
        intro: [
          'A quiz is an online test with questions marked automatically wherever possible. A quiz has instructions, an optional time limit, a number of attempts (the best counts) and an optional opening and closing date. Quiz scores go to the course gradebook.',
          'The **Quizzes** page lists the quizzes of a course with their number of questions and attempts, and links to **Questions & settings** (the quiz builder), **Attempts & grading** and **Statistics**.',
        ],
        steps: [
          'Open **Quizzes** and choose the course in **Select Course Offering**.',
          'Click **+ Create Quiz**, enter the **Title**, **Instructions**, **Time Limit (minutes)** (blank for no limit), **Max Attempts** and, optionally, when it opens and closes. Save.',
          'Click **Questions & settings** to add the questions and choose the delivery options (next section).',
          'Click **Publish** when the quiz is ready. Students see it in **View Quizzes**. Use **Deactivate** to close it early, or **Delete** to remove a quiz nobody has taken.',
        ],
        figs: [['flow:quiz-teacher/1', 'Quizzes of a course']],
        tips: ['Tick **Include Inactive** to see closed quizzes.', 'Quizzes created before the quiz engine was upgraded keep working and can be edited in the same builder.'],
      },
      {
        title: 'Building a quiz', roles: 'SuperAdmin, Faculty', index: ['Quiz builder', 'Question types'],
        intro: [
          'The quiz builder shows the questions on the left and the delivery settings on the right. Changes apply to attempts started after you save; to rescore attempts already finished, use **Regrade** (see Quiz statistics).',
        ],
        steps: [
          'Under **Add a question**, choose the **Question type**, the **Marks** and the **Position**, type the **Question**, and fill in what that type needs (answer options, pairs, blanks, the correct number and so on). Click **Add question**.',
          'To use a question bank, add a **group**: choose the bank, how many questions to pick and the marks for each. Every student gets a different random selection. Or copy chosen bank questions into the quiz.',
          'Under **Delivery & security**, choose whether to shuffle questions and answer options, show one question at a time, and lock answers once saved. Optionally set an **Access code** and **Allowed networks**.',
          'Under **After submitting, students see**, choose nothing, the score only, their answers, or their answers and the correct answers, and whether answers appear only after the quiz closes. Click **Save settings**.',
          'Under **Extra time & attempts**, give a student a time multiplier (for example 1.5 ×), extra minutes or extra attempts, for this quiz or every quiz in the course. Under **Different dates for one student**, give a student their own opening and closing dates.',
          'Click **Outcomes** on a question to align it with learning outcomes (Chapter 9).',
        ],
        figs: [['flow:quiz-teacher/2', 'The quiz builder: questions, a bank group, delivery and security settings, and accommodations']],
        tables: [{
          title: 'Question types', head: ['Type', 'How it is answered and marked'], widths: [2600, 6700], rows: [
            ['Multiple choice (one answer)', 'Pick one option. Marked automatically.'],
            ['Multiple answers', 'Tick every correct option; partial credit for each right choice, less for wrong ones.'],
            ['True / False', 'Choose True or False. Marked automatically.'],
            ['Matching', 'Match each prompt with an answer from a list, which can include extra wrong answers. Partial credit per pair.'],
            ['Ordering', 'Put items in the right order with the arrows. Partial credit for items in the right place.'],
            ['Fill in the blanks', 'Type the missing words; [[name]] in the question marks each blank. Several accepted answers per blank.'],
            ['Numeric answer', 'Type a number; exact with a tolerance, or within a range.'],
            ['Formula (calculated)', 'Variables in [brackets] get new values for every student; the answer is calculated from a formula.'],
            ['Short answer (manual)', 'A few words, marked by the teacher.'],
            ['Essay (manual)', 'A longer written answer, marked by the teacher.'],
            ['File upload (manual)', 'The student uploads a file, marked by the teacher.'],
          ],
        }],
        notes: [['tip', 'Check each question\'s correct answer before publishing; the builder shows it under the question.']],
      },
      {
        title: 'Question banks', roles: 'SuperAdmin, Faculty', index: ['Question banks'],
        intro: [
          'A question bank is a reusable pool of questions, tagged by topic and difficulty. A bank can belong to one course or be shared with other teachers. Quizzes draw random questions from it, so each student gets a different set and banks can be reused every term.',
        ],
        steps: [
          'From **Quizzes**, click **Question banks**.',
          'Under **New bank**, enter the **Title**, choose the **Course** (or leave it not tied to a course), tick **Share with other teachers** if you want, and click **Create bank**.',
          'Choose the bank and use **Add a question to this bank**. Questions use the same types as quizzes and can have tags and a difficulty.',
          'Use **Export to Excel** to save a bank, and **Import questions from Excel** to add many questions at once. Rows that cannot be read are listed so you can fix them.',
        ],
        figs: [['flow:quiz-teacher/3', 'A question bank with its questions']],
        tips: ['Export first: the file includes a Help sheet showing the format.', 'A bank used by a quiz cannot be deleted.'],
      },
      {
        title: 'Taking a quiz (students)', roles: 'Student', index: ['Taking a quiz', 'View Quizzes'],
        intro: ['Students find their quizzes in **View Quizzes**. The start page shows the number of questions, the time limit, the attempts allowed and used, when the quiz opens and closes, and the teacher\'s instructions.'],
        steps: [
          'Open **View Quizzes** and click the quiz.',
          'Read the instructions. Enter the **Access code** if your teacher gave you one, and click **Start quiz**.',
          'Answer the questions. Answers are saved as you go; drag items into order with the arrows, choose matches from the lists, type into the blanks inside a sentence, or upload a file where asked.',
          'Click **Submit** before the time runs out. When the timer reaches zero, your saved answers are submitted for you.',
          'Open **Review** to see what your teacher allows: your score, your answers and the correct answers, and any feedback.',
        ],
        figs: [['flow:quiz-student/1', 'Before starting a quiz'], ['flow:quiz-review/1', 'Reviewing a submitted attempt']],
        tips: [
          'The timer starts when you press Start and keeps running if you leave the page. If your connection drops, your last saved answers are kept; use **Resume attempt** to continue.',
          'If the quiz shows one question at a time with locked answers, you cannot go back after moving on.',
          'Answers that need a teacher (essays, short answers and files) show as pending until they are marked.',
        ],
      },
      {
        title: 'Marking quiz answers', roles: 'SuperAdmin, Faculty', index: ['Quiz grading'],
        intro: ['Objective questions are marked automatically when a student submits. Short answers, essays and uploaded files wait in the **Needs marking** queue on **Attempts & grading**, with the student, the question and the answer.'],
        steps: [
          'Open **Quizzes** and click **Attempts & grading** on the quiz.',
          'For each answer in **Needs marking**, enter the marks (up to the question\'s marks) and optional **Feedback (optional)**, and click **Save marks**.',
          'Under **All attempts**, click **Open** to see a whole attempt.',
        ],
        figs: [['flow:quiz-teacher/4', 'Attempts & grading: answers that need marking']],
        outcome: ['When the last answer of an attempt is marked, its total is calculated, the student can see it (as the review settings allow), and the gradebook is updated.'],
      },
      {
        title: 'Quiz statistics and regrading', roles: 'SuperAdmin, Faculty', index: ['Quiz statistics', 'Regrade'],
        intro: [
          '**Statistics** summarises the finished attempts (count, average, highest and lowest score) and, for each question, the average marks, the **Difficulty** (the share of marks students earned; 1.00 means everyone was right) and the **Discrimination** (how well the question separates strong from weak students: above 0.3 is good, near 0 means the question does not separate them, and below 0 suggests a wrong key). Option counts show how often each answer was chosen.',
        ],
        steps: [
          'Open **Statistics** from the quiz.',
          'If a question\'s answer key was wrong, click **Edit question**, correct it, then **Regrade** to recalculate every finished attempt, or **Full credit for all** to give everyone the marks.',
        ],
        figs: [['flow:quiz-teacher/5', 'Quiz statistics with difficulty and discrimination per question']],
        tips: ['Manually marked questions can only be regraded with full credit.', 'A question that has answers cannot be deleted, so the record of the attempts stays complete.'],
      },
      {
        title: 'Late and missing work, and student groups', roles: 'SuperAdmin, Admin, Faculty', index: ['Late policy', 'Missing work', 'Groups', 'Group sets'],
        intro: [
          '**Late policy & groups** (from Assignments, after choosing a course) holds two course-wide settings: how late and missing work is graded, and the course\'s student groups.',
        ],
        steps: [
          'Open **Assignments**, choose the course and click **Late policy & groups**.',
          'Under **Late & missing work**, tick **Deduct points for late work** and enter the **Deduction (%)** for every started hour or day late and the **Lowest grade (%)** it can fall to.',
          'Optionally tick **Grade work never handed in** and enter the **Missing grade (%)**. It is given automatically once each student\'s due date has passed; **Grade missing work now** applies it at once. Handing in late work later replaces it. Save.',
          'Under **Group sets**, create a set (for example "Project teams"), then add groups by name with **Add group**, split the class at random by the number of groups or students per group, or let students sign up themselves with a **Largest group** size.',
          'Add or remove members of each group as needed. A student belongs to at most one group in each set.',
        ],
        figs: [['flow:assignment-teacher/1', 'Late policy & groups for a course']],
        tips: ['Type the mark you would give; the deduction is applied on top and shown separately to the student.', 'Groups also get private discussion threads (Discussion > Post to).'],
      },
      {
        title: 'Assignment options: dates, group work, peer review and similarity', roles: 'SuperAdmin, Admin, Faculty', index: ['Assignment options', 'Extensions', 'Peer review', 'Similarity'],
        intro: ['Click **Options** next to an assignment (or **Options & dates** in the grading workspace) to change how it is handed in and reviewed.'],
        steps: [
          'Under **When students can hand in**, set **Opens (UTC, optional)** and **Late work accepted until (UTC)**. Work after the due date is late; after the cut-off it is refused. Leave the cut-off empty to close at the due date.',
          'Under **Group work**, choose **Individual work** or a **Group set**. With a group set one member hands in for the whole group, and one grade goes to every member unless you tick **Grade each member separately**.',
          'Under **Peer review**, set **Reviews per student** (0 turns it off, up to 5), **Reviews due (UTC)** and **Anonymous reviews**. Reviews are handed out automatically after the due date among students who handed in work, or click **Assign reviews now**.',
          'Under **Similarity check**, tick **Check every handed-in attempt** and, if you wish, **Show students their own score**. Click **Save options**.',
          'Under **Assign to — different dates**, choose a student (for example for an extension) or a group, enter their **Due (UTC)**, opening and late-work dates and a **Note**, and click **Add dates**. A student\'s own dates win over their group\'s.',
        ],
        figs: [['flow:assignment-teacher/2', 'Assignment options with peer reviews assigned and a personal due date']],
        notes: [['note', 'The grouping of an assignment cannot change once work has been handed in.']],
      },
      {
        title: 'The grading workspace', roles: 'SuperAdmin, Admin, Faculty', index: ['Grading workspace', 'Annotations', 'Rubric'],
        intro: [
          'The grading workspace (**Grade** next to an assignment) shows everything needed to mark one submission after another without leaving the page: the list of submissions, the work itself, and the marks, rubric and comments.',
        ],
        steps: [
          'The left column lists the submissions. Filter it with **All**, **Needs grading**, **Late** or **Graded, not posted**. Each shows its attempt, late status, similarity score and grade.',
          'The middle column shows the selected file. PDFs and images open in the viewer; Word, PowerPoint and Excel files appear as PDF once converted. Typed answers appear in full.',
          'Mark the work with the toolbar: highlight, strike-out, box, text, comment pin and freehand drawing. Type in an annotation\'s box to attach a comment.',
          'On the right, enter the marks and feedback; they save automatically (Ctrl+Enter saves at once). With a rubric, choose a level per criterion and click **Save rubric**; the total can set the marks.',
          'Add comments for the student, or tick **Graders only** for a private note. **Record** adds a voice comment.',
          'Move to the next student with **Alt+→** (previous: **Alt+←**) or the arrow buttons. With manual posting, click **Post grades** when you are ready; **Hide grades** takes them back.',
        ],
        figs: [['flow:assignment-teacher/3', 'The grading workspace']],
        tips: [
          'A late submission shows the deduction and the final grade; tick **Waive the late deduction** to give the full mark.',
          'With peer review, each submission shows its peer average, and **Use peer average** copies it into the marks.',
          '**Anonymous grading** shows "Student 1, Student 2 …" until grades are posted; **moderated grading** lets a moderator choose the final grade from the graders\' provisional marks.',
        ],
      },
      {
        title: 'Similarity checking', roles: 'SuperAdmin, Admin, Faculty, Student', index: ['Similarity', 'Plagiarism'],
        intro: [
          'With **Check every handed-in attempt** on, every attempt is compared within about a minute: with the other work of the assignment, the course or the institution (built-in checker), or also with web pages and publications (Copyleaks, when your institution uses it).',
          'The grading workspace shows **Similarity NN%** for each submission: green below 25%, amber below 50%, red from 50%. **Open report** shows the matching passages side by side with the work they match; **Check again** re-runs the check, which helps for the first students to hand in, who had nothing to compare with yet.',
        ],
        figs: [['flow:assignment-teacher/4', 'A similarity report with matching passages']],
        tips: [
          'Typed text and .txt, .docx, .pdf (with selectable text), .html and .rtf files are read; scanned images are not.',
          'A student\'s own earlier attempts and their group\'s work never count as matches.',
          'A high score is a prompt to look closer, not proof: quotations, references and templates match too.',
        ],
      },
      {
        title: 'Handing in work and peer review (students)', roles: 'Student', index: ['Hand in', 'Peer review', 'My groups'],
        intro: [
          'A student\'s assignment page shows the due date (or **Your own dates** after an extension), when hand-in opens and until when late work is accepted, the course\'s late rule, previous attempts, grades, feedback and teacher comments, the similarity score (if the teacher shows it), and peer reviews to do and received.',
        ],
        steps: [
          'Open **Assignments**, choose the assignment, type your answer and/or attach files, and click **Submit**. If more attempts are allowed, you can hand in again; every attempt is kept.',
          'For a group assignment, one member hands in for the group. You must be in a group to hand in; **My groups** shows your groups and lets you join or leave one when the teacher allows sign-up.',
          'When asked to review classmates\' work, click **Review** under **Peer reviews**, read the work, choose the rubric levels or a score, write a helpful comment and click **Submit review**.',
          'Reviews of your own work appear on the assignment page, anonymously unless your teacher turned that off.',
        ],
        figs: [['flow:assignment-student/1', 'A student\'s assignment page with a personal due date, the late rule, the similarity score and a review to do'], ['flow:assignment-student/2', 'Reviewing a classmate\'s work'], ['flow:groups-student/1', 'My groups']],
        tips: ['Late work may lose points: your grade shows the mark and the deduction.', 'You never see other students\' work in a similarity report; ask your teacher if your score seems wrong.'],
      },
    ],
  },

  {
    chapter: 'Learning Outcomes',
    intro: [
      'Learning outcomes describe what students should be able to do by the end of a program or course. EduSphere links graded work to outcomes, so each student\'s mastery of each outcome is calculated from evidence, and program attainment can be reported for accreditation (for example PEC, HEC, NCAAA or ABET-style reviews).',
    ],
    sections: [
      {
        title: 'Defining outcomes', roles: 'SuperAdmin, Admin, Faculty', index: ['Learning Outcomes', 'PLO', 'CLO'],
        intro: [
          'There are three levels: institution outcomes, **program learning outcomes (PLOs)** for each academic program (set by Admins), and **course learning outcomes (CLOs)** for each course (set by teachers). Each CLO can contribute to one or more PLOs.',
          'Each outcome has a rating scale (by default 0 to 4), the mastery points needed, and a calculation method: decaying average, highest, most recent, average, or "mastered n times".',
        ],
        steps: [
          'Open **Learning Outcomes**.',
          'To add an outcome, choose the **Level** (Program or Course), the program or course, a **Code** (for example PLO-3 or CLO1) and a **Title**. Keep the default scale or set your own levels, the mastery points and the calculation method.',
          'For a course outcome, tick the program outcomes it contributes to.',
          'Use **Edit** to change an outcome and **Turn off** to hide it from lists and reports (its evidence is kept). Tick **Show turned off** to see hidden outcomes.',
          'To add many outcomes at once, download the import template, fill it in and import it.',
        ],
        figs: [['flow:outcomes/1', 'Learning Outcomes: program and course outcomes with their mappings']],
      },
      {
        title: 'Aligning work with outcomes', roles: 'SuperAdmin, Admin, Faculty', index: ['Alignment'],
        intro: ['An outcome only gathers evidence from work aligned with it. Click **Outcomes** on a rubric criterion (Rubric Management), a quiz question (quiz builder), a bank question (Question Banks) or an assignment, and tick what it assesses. Work already graded is counted straight away.'],
        tips: ['Only grades students can see count: unposted grades and unmarked essay answers are left out until you post or mark them.', 'After bulk changes, click **Recalculate** on the course\'s mastery gradebook.'],
      },
      {
        title: 'Mastery gradebook', roles: 'SuperAdmin, Admin, Faculty', index: ['Mastery gradebook'],
        intro: ['**Learning Outcomes** > **Mastery gradebook** shows, for one course, each student against each outcome: green (mastered), amber (near) or red (below), with how many students were assessed and how many mastered each outcome. Export it to Excel for your records.'],
        figs: [['flow:outcomes-teacher/1', 'Mastery gradebook for a course']],
      },
      {
        title: 'Program attainment', roles: 'SuperAdmin, Admin', index: ['Program attainment', 'Accreditation'],
        intro: ['**Program attainment** reports, for each program outcome, how many students were assessed and how many attained it, the attainment percentage against your **Target (% of students)**, the average score and the courses the evidence came from.'],
        steps: ['Open **Learning Outcomes** > **Program attainment**.', 'Choose the **Program**, enter the target and click **Show report**.', 'Use **Download PDF** or **Download Excel** for accreditation files.'],
        figs: [['flow:outcomes/2', 'Program outcome attainment']],
      },
      {
        title: 'My Outcomes (students)', roles: 'Student', index: ['My Outcomes'],
        intro: ['**My Outcomes** shows, for each course that uses learning outcomes, how well your graded work shows you have mastered each outcome: Mastered, Nearly there, Not yet or No evidence yet. Open **Evidence** to see which assignments and quizzes it comes from. Only grades your teacher has released count.'],
        figs: [['flow:outcomes-student/1', 'My Outcomes']],
      },
    ],
  },
];
