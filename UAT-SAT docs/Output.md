# Student Lifecycle Output

## Output Synchronization Update (2026-06-02)

- Domain script-pack execution outputs are now aligned by institution mode:
	- Scripts/School Scripts,
	- Scripts/College Scripts,
	- Scripts/University Scripts.
- Expected deterministic coverage in lifecycle output evidence:
	- School: Class 1-10 rows for all scoped school students,
	- College: Class 11-12 rows for all scoped college students,
	- University: Semester 1-8 rows for all scoped university students.
- University lifecycle output now explicitly includes seeded and verifiable:
	- enrollment records,
	- attendance records,
	- FYP project records.
- University post-deployment output validation now includes strict attendance/FYP coverage assertions.

## Reference Scenario
This document captures the expected lifecycle of a single student across the supported institution modes.

## University Flow

- Student enrolls in a semester-based program.
- Student registers for courses.
- Student submits quizzes and assignments.
- Results are calculated using the GPA / CGPA strategy.
- Promotion is determined by the progression service.
- Graduation is recorded by the student lifecycle service when degree requirements are met.

## School Flow

- Student enrolls in a grade-based stream.
- Student is assigned to the appropriate grade and stream.
- Quizzes, assignments, and term results are recorded.
- Results are evaluated using percentage-based grading.
- Promotion moves the student to the next grade.
- Final completion is captured when the student finishes the final grade.

## College Flow

- Student enrolls in a year-based program.
- Student submits coursework and exams.
- Percentage-based results are calculated.
- The progression service advances the student from one year to the next.
- Completion is recorded when the final year is passed.

## Expected Output Snapshot

- Enrollment record created.
- Academic activity recorded.
- Result summary produced.
- Promotion or graduation decision recorded.
- Notification emitted for lifecycle changes.

## Validation Basis

This output reflects the currently implemented lifecycle services, progression rules, institution policy support, and license-gated access paths in the codebase.

## Current Baseline Notes (15 May 2026)

- Institution parity is active for School, College, and University contexts.
- Standard DB deployment verification chain is Scripts/01 through Scripts/05.
- User onboarding templates are role-specific under User Import Sheets:
	- faculty-admin-import-template.csv
	- students-import-template.csv
- Cross-phase planning and enhancement history is consolidated in Docs/Consolidated-Execution-Enhancements-Issues.md.

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.
