Result Entry Prompt (Based on Attendance Module Requirements)
Prompt Title: Enter Attendance – Result Entry Workflow

Prompt Content:

Build a Result Entry interface for Attendance that follows the same rules, constraints, and behaviors defined in the Enter Attendance module.

The Result Entry screen must allow authorized users (SuperAdmin, Admin, Faculty) to enter or import attendance results for students using manual entry or CSV import.

Functional Requirements:

Enforce all required filters before enabling save or import actions:

Tenant

Campus

Department

Course

Subject (restricted to subjects assigned to logged‑in faculty)

Class or Semester (dynamic based on institution type)

Optional: Student

When all required filters are selected, display the attendance result entry table with columns:

Student ID

Student Name

Date (calendar picker)

Present (checkbox: checked = Present, unchecked = Absent)

When filters are incomplete, show a disabled table structure with the same columns and disable all write actions.

Provide two attendance entry methods:

Manual Entry: User marks Present/Absent per student with a date.

CSV Import: User uploads a CSV file matching the system template.

Provide a Download Template button that generates a CSV with headers:

Tenant, Campus, Department, Course, Subject, ClassOrSemester, StudentId, StudentName, Date, Present

Include 2 example rows.

CSV Import must validate:

Required fields

StudentId exists in roster

Subject belongs to logged‑in faculty

Valid date format

Present is valid (true/false or Present/Absent)

No duplicate Student + Subject + Date

On import, return row‑level success or error details.

Support strict mode (fail‑fast) and non‑strict mode (partial success).

Generate an Import Result Report with row‑level outcomes and allow one‑time download using a token that expires in 2 hours.

Enforce all tenant, campus, department, course, and offering boundaries.

Prevent duplicate attendance entries both in UI validation and database (unique Student + Offering + Date).

Maintain full compatibility with existing attendance logic, routing, authorization, and sidebar governance.

Output Expectations:

A fully structured Result Entry UI flow.

Validation logic for manual and CSV entry.

Filter‑driven dynamic behavior.

CSV template and import rules.

Audit trail and report download behavior.

Compliance with role‑based access and tenant/campus boundaries.

No regression to existing attendance features.