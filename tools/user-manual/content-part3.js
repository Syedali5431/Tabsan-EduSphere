// Chapters 8-11: results and progression, graduation, reports, finance.
module.exports = [
  {
    chapter: 'Results, Progression and Final Year Projects',
    intro: ['This chapter covers how marks become grades, how students move from one level to the next, and how final year projects are run.'],
    sections: [
      {
        title: 'Results', roles: 'SuperAdmin, Admin, Faculty, Student', index: ['Results', 'Re-check'],
        intro: [
          'The Results page shows published marks. Staff use the same filters as Enter Results to review a class. Students see only their own results, grouped by class or semester, with the mark, grade and publish date for each course.',
        ],
        steps: [
          'Open **Results**. Students can choose a **Class** to narrow the list.',
          'Click **Export CSV** or **Export PDF** to download a copy of the results shown.',
          'Students: if you believe a mark is wrong, click **Request Re-check** on that result, give a **Reason** and submit.',
        ],
        figs: [['flow:student-results/1', 'Results (Student view)']],
        outcome: ['The request is recorded as a change request for the administrators to review. If the mark is changed, the corrected result replaces the old one and the change is recorded in the audit log.'],
        tips: ['Results only appear after the teacher publishes them. An empty page usually means marks have not been published yet.'],
      },
      {
        title: 'Result Calculation', roles: 'SuperAdmin, Admin', index: ['Result Calculation', 'GPA'],
        intro: [
          'Result Calculation tells the portal how to turn marks into grades and grade points. It has two sections: the score-to-GPA scale, and the weighting of each assessment within a subject.',
        ],
        steps: [
          'In **Section 1: GPA and Score**, choose the **Institute** and add a row for each score band and its GPA value with **Add Row** (for example 85 and above = 4.0).',
          'In **Section 2: Subject Score Calculation**, choose the **Course Type** or a specific **Course**, and set the **Weightage** of each **Component**, for example Midterm 30%, Final 50%, Assignments 20%.',
          'Tick **Active** for the rows that should apply and click **Save**. Use **X** to remove a row.',
        ],
        figs: [['testadmin:ResultCalculation', 'Result Calculation']],
        tips: [
          'Component weightings for a subject should add up to 100%.',
          'Change the rules before results are published for a term; changing them afterwards recalculates grades the next time results are processed.',
        ],
        notes: [['note', 'University results use GPA scales. School and College results are percentage based and do not need GPA rows.']],
      },
      {
        title: 'Grading Config', roles: 'SuperAdmin (edit), Admin and Faculty (view)', index: ['Grading Config', 'Pass mark'],
        intro: [
          'Grading Config sets the pass threshold, and optionally the grade bands, separately for School, College and University. The pass threshold decides whether a student is eligible for promotion to the next level.',
        ],
        steps: [
          'Choose the scope (institute, department, semester, course or subject) the setting applies to.',
          'Enter the **Pass Threshold** and, optionally, the **Grade Ranges JSON** that maps score ranges to letter grades.',
          'Tick **Active** and click the matching button: **Save School Grading**, **Save College Grading** or **Save University Grading**.',
        ],
        figs: [['superadmin:GradingConfig', 'Grading Config (SuperAdmin)']],
      },
      {
        title: 'Prerequisites', roles: 'SuperAdmin, Admin', index: ['Prerequisites'],
        intro: [
          'A prerequisite is a course a student must pass before they can enrol in another, for example CS101 before CS201. The page lists every course with the number of prerequisites it has.',
        ],
        steps: ['Find the course in the list.', 'Click **Add Prerequisite** and choose the course that must be passed first.', 'To remove a requirement, click **Remove** next to it.'],
        figs: [['testadmin:Prerequisites', 'Course Prerequisites']],
        outcome: ['When a student, or an Admin on their behalf, tries to enrol in the course, the portal checks the student\'s published results and blocks the enrolment if a prerequisite has not been passed.'],
      },
      {
        title: 'Student Lifecycle', roles: 'SuperAdmin, Admin', index: ['Student Lifecycle', 'Promotion', 'Graduation'],
        intro: [
          'Student Lifecycle moves students forward at the end of each term. **Promote** moves an eligible student to the next semester or class; **Graduate** completes the program for a student in the final level.',
          'A student is eligible for promotion when their result for the level meets the pass threshold set in Grading Config.',
        ],
        before: ['Results for the level are published, so eligibility can be calculated.'],
        steps: [
          'Open **Student Lifecycle** and choose the **Department**.',
          'Choose the **Semester** (or **Class**). The students at that level are listed with their standing.',
          'Click **Promote** for each eligible student, or **Graduate** for a student in the final level.',
        ],
        figs: [['flow:lifecycle/1', 'Department selected'], ['flow:lifecycle/2', 'Students in the selected semester']],
        outcome: ['Promoted students move to the next level and appear in its lists. Graduated students change to the Graduated status and become eligible for certificates.'],
        notes: [['important', 'University students must have their FYP created before the final promotion, because promoting past the last semester graduates the student automatically.']],
      },
      {
        title: 'Study Plan', roles: 'SuperAdmin, Admin, Faculty', index: ['Study Plan'],
        intro: [
          'A study plan records the courses a student intends to take in future semesters. Plans are reviewed by an academic advisor, which helps students avoid missing prerequisites or required courses.',
        ],
        steps: [
          'Choose the **Department** and the **Student**, then click **Create** to start a plan.',
          'Open the plan and add the courses the student intends to take. The portal can also suggest courses through its recommendations.',
          'The advisor (Faculty) reviews the plan, adds advisor notes, and endorses or rejects it.',
        ],
        figs: [['testadmin:StudyPlan', 'Study Plan']],
        tables: [{
          title: 'Study plan statuses', head: ['Status', 'Meaning'], widths: [2200, 7100], rows: [
            ['Pending', 'Waiting for advisor review.'],
            ['Endorsed', 'Approved by the advisor.'],
            ['Rejected', 'Returned with advisor notes; update and resubmit.'],
          ],
        }],
      },
      {
        title: 'Final Year Projects (FYP)', roles: 'SuperAdmin, Faculty', index: ['FYP', 'Final Year Project'],
        intro: [
          'FYP tracks final year projects for University students in their last semester: the project title, the student, the supervisor, the department, the status and the final result. Supervisors also schedule meetings, which appear under **Upcoming Meetings**.',
        ],
        steps: [
          'A project is registered with **+ Create Project** (or proposed by the student), giving the **Title**, **Description**, **Department** and **Student**.',
          'The project is reviewed and approved or rejected. An Admin or SuperAdmin assigns the **Supervisor**.',
          'During the semester the supervisor holds meetings and tracks progress.',
          'When the work is finished, completion is requested and approved, then the supervisor clicks **Enter Result** to record the FYP result and marks the project **Complete**.',
        ],
        figs: [['faculty:Fyp', 'FYP (Faculty view)']],
        tables: [{
          title: 'FYP statuses', head: ['Status', 'Meaning'], widths: [2200, 7100], rows: [
            ['Proposed', 'Submitted for approval.'],
            ['Under Review', 'Being reviewed by the department.'],
            ['Approved', 'Accepted; work can start.'],
            ['In Progress', 'Work is under way with a supervisor.'],
            ['Completed', 'Finished and graded. Required before graduation.'],
            ['Rejected', 'Not accepted; a new proposal is needed.'],
          ],
        }],
      },
    ],
  },

  {
    chapter: 'Graduation and Certificates',
    intro: ['Graduation applies to University programs. The portal checks each student against the program\'s degree rule, takes the application through a three-stage approval, and then makes certificates available.'],
    sections: [
      {
        title: 'Degree Rules', roles: 'SuperAdmin (edit), Admin and Faculty (view)', index: ['Degree Rules'],
        intro: [
          'A degree rule sets what a University program requires before a student can graduate. Without a rule, the degree audit for that program reports "No degree rule has been configured".',
        ],
        steps: ['Choose the **Academic Program**.', 'Enter the minimum total, core and elective credits and the minimum GPA.', 'Click **Create Rule**. Existing rules are listed below, with **Delete** to remove one.'],
        fields: [
          ['Min Total Credits', 'The total credit hours the student must complete.'],
          ['Min Core Credits / Min Elective Credits', 'How many of those credits must come from core courses and from electives.'],
          ['Min GPA', 'The lowest CGPA allowed for graduation, for example 2.00.'],
        ],
        figs: [['superadmin:DegreeRules', 'Degree Rules']],
      },
      {
        title: 'Degree Audit', roles: 'SuperAdmin, Admin, Faculty, Student', index: ['Degree Audit'],
        intro: [
          'Degree Audit compares a student\'s completed courses with the program\'s degree rule and shows what is still missing. Staff choose a **Student** and click **Load Audit**. Students see their own audit straight away.',
          'The audit lists completed courses with their credits, type (core or elective) and grade points, and an **Unmet Requirements** box such as "Total credits: 84/90 required".',
        ],
        figs: [['student:DegreeAudit', 'Degree Audit (Student view)']],
        tips: ['Students should check their degree audit at the start of the final year, so any missing courses can still be taken.'],
      },
      {
        title: 'Graduation Eligibility', roles: 'SuperAdmin, Admin, Faculty', index: ['Graduation Eligibility'],
        intro: [
          'Graduation Eligibility lists students with their credits earned, CGPA, unmet requirements and eligibility status, so staff can see who is ready to graduate. Click **View Audit** for the detail behind a student\'s status. Faculty see the students of their own departments.',
        ],
        figs: [['testadmin:GraduationEligibility', 'Graduation Eligibility']],
      },
      {
        title: 'Applying for graduation (Students)', roles: 'Student', index: ['Graduation Apply'],
        intro: ['An active University student in the final semester with a completed FYP can apply for graduation from the portal.'],
        before: ['Your degree audit shows no unmet requirements, and your FYP status is Completed.'],
        steps: ['Open **Graduation Apply**.', 'Optionally add a **Personal Note** for the reviewers.', 'Click **Submit Application**. Your application and its current stage are shown on the same page.'],
        figs: [['student:GraduationApply', 'Graduation Application (Student)']],
      },
      {
        title: 'Graduation Applications', roles: 'SuperAdmin, Admin, Faculty', index: ['Graduation Applications', 'Approvals'],
        intro: [
          'Applications are approved in three stages: first by Faculty, then by an Admin, and finally by a SuperAdmin. Any of these approvers can reject an application at their stage.',
        ],
        steps: ['Open **Graduation Applications**.', 'Filter by **Status** and **Department** and click **Filter**.', 'Click **View** on an application, review the student\'s audit, and approve or reject it.'],
        figs: [['testadmin:GraduationApplications', 'Graduation Applications']],
        tables: [{
          title: 'Application statuses', head: ['Status', 'Meaning'], widths: [2600, 6700], rows: [
            ['Pending Faculty', 'Waiting for a Faculty approver.'],
            ['Pending Admin', 'Approved by Faculty; waiting for an Admin.'],
            ['Pending Final Approval', 'Approved by an Admin; waiting for the SuperAdmin.'],
            ['Approved', 'Graduation approved; the certificate becomes available.'],
            ['Rejected', 'Not approved. The student can see the outcome on their application and should speak to their department.'],
          ],
        }],
      },
      {
        title: 'Generate Certificates', roles: 'SuperAdmin, Admin, Faculty', index: ['Certificates'],
        intro: [
          'Generate Certificates produces official documents for students: degree certificates and transcripts for University graduates, and completion certificates and report cards for School and College students. Files are named with the registration number and document type.',
        ],
        steps: [
          'Open **Generate Certificates** and filter by institution, department, course and class; use **Search Students** to find a student, then click **Apply**.',
          'In the student\'s row, choose the document type and click **Generate**. The generated files are listed under Certificates for download.',
          'Under **Certificate Templates**, use **Download Default** to get the standard layout, or **Import Template** to upload your institution\'s own design.',
        ],
        figs: [['testadmin:GenerateCertificates', 'Generate Certificates']],
        tips: ['Degree certificates are available only for University graduates. School completion certificates require Class 10, and College ones require Class 11 and 12 results.'],
      },
    ],
  },

  {
    chapter: 'Reports and Analytics',
    intro: ['Reports produce lists and summaries you can view on screen and download, while Analytics shows trends as charts.'],
    sections: [
      {
        title: 'Report Center', roles: 'SuperAdmin, Admin, Faculty, Finance', index: ['Reports', 'Report Center'],
        intro: ['Report Center lists the reports your role may run, as cards. Finance users see only the Payment Summary; students do not have reports.'],
        table: {
          head: ['Report', 'What it shows'], widths: [2800, 6500],
          rows: [
            ['Attendance Summary', 'Attendance totals and percentages by offering and student.'],
            ['Enrollment Summary', 'Enrolment counts by department, program and semester.'],
            ['FYP Status Report', 'Final year projects with supervisor, status and result.'],
            ['GPA & CGPA Report', 'Semester GPA and CGPA per student (University only).'],
            ['Low Attendance Warning', 'Students below the attendance threshold (default 75%).'],
            ['Payment Summary', 'Fee receipts by status and amount.'],
            ['Result Summary', 'Marks and grades by subject and exam type.'],
            ['Semester Results', 'Results for a whole semester or class.'],
            ['Student Transcript', 'A student\'s full academic record.'],
          ],
        },
        steps: [
          'Open **Report Center** and click a report card.',
          'Choose the filters (for example **Department** and **Program**) and click **Filter**. Some reports need a particular filter: Semester Results needs a semester, and Student Transcript needs a student.',
          'Review the results on screen. A summary line shows the number of records and key totals.',
          'Click **Excel**, **CSV** or **PDF** to download the report in that format.',
        ],
        figs: [['flow:report-gpa/1', 'Report Center'], ['flow:report-gpa/2', 'Report filters'], ['flow:report-gpa/3', 'GPA & CGPA Report generated, with export buttons']],
        tips: [
          'Excel is best for further analysis, CSV for importing into other systems, and PDF for printing or sharing.',
          'Reports reflect published data only; unpublished marks are not included.',
        ],
        notes: [['note', 'Admins must choose a department before a report generates. SuperAdmin can switch reports off for a tenant or campus with Deactivate Reports for Scope.']],
      },
      {
        title: 'Analytics', roles: 'SuperAdmin, Admin, Faculty, Finance', index: ['Analytics'],
        intro: [
          'Analytics gives an overview of performance, attendance and assignments, with headline figures (for example average marks and overall attendance) and charts of trends over time, plus a student table you can sort and review.',
        ],
        steps: ['Open **Analytics**.', 'Choose the institute, campus, department, course and semester/class you are interested in, and click **Apply Filters**.', 'Read the headline cards and charts. Click **Reset** to clear the filters.'],
        figs: [['testadmin:Analytics', 'Analytics overview']],
      },
    ],
  },

  {
    chapter: 'Finance',
    intro: ['Finance covers fee receipts: issuing them to students, recording payment and keeping the totals accurate.'],
    sections: [
      {
        title: 'Fee receipts (Payments)', roles: 'SuperAdmin, Admin, Finance', index: ['Payments', 'Fee receipts'],
        intro: [
          'Payments manages fee receipts from creation to confirmation. Each receipt belongs to one student and has an amount, a receipt number, a description and a due date. The list shows all receipts with their status, and can be filtered by student and paged with **Previous** and **Next**.',
        ],
        steps: [
          'Open **Payments**.',
          'Under **Create Fee Receipt**, choose the **Student**, enter the **Amount**, **Receipt No**, **Description** and **Due Date**.',
          'Click **Create**. The receipt starts as Pending.',
          'When payment has been received, click **Confirm** on the receipt. It changes to Paid.',
          'If a receipt was issued in error, click **Cancel**.',
          'For many receipts at once, click **Export Template**, fill in the CSV and upload it with **Import CSV**.',
        ],
        fields: [
          ['Student', 'The student the fee is for.'],
          ['Amount', 'The amount due, in your institution\'s currency.'],
          ['Receipt No', 'Your reference number, for example RCP-2026-0101. Use a unique number for each receipt.'],
          ['Description', 'What the fee is for, for example "Semester 3 tuition fee".'],
          ['Due Date', 'The date by which the student should pay.'],
        ],
        figs: [['flow:payment-create/1', 'Payments – all receipts'], ['flow:payment-create/2', 'Create Fee Receipt form completed']],
        tables: [{
          title: 'Receipt statuses', head: ['Status', 'Meaning'], widths: [2200, 7100], rows: [
            ['Pending', 'Issued and waiting for payment.'],
            ['Submitted', 'The student has reported payment and supplied proof; waiting for Finance to confirm.'],
            ['Paid', 'Payment confirmed by Finance or an Admin. Cannot be cancelled.'],
            ['Cancelled', 'Withdrawn. Hidden from the student\'s own list but kept for audit.'],
          ],
        }],
        notes: [['tip', 'Use Filter by Student to see one student\'s payment history, and Report Center > Payment Summary for totals.']],
      },
    ],
  },
];
