# Faculty Enter Attendance Module Requirements

## Objective

Add a new **Enter Attendance** module under the **Faculty Related** section so authorized users can manage student attendance through manual entry and CSV import without disrupting existing application logic, authorization flows, routing, or backward compatibility.

## Summary

The new module must allow faculty to:

- Filter student attendance scope accurately.
- Enter attendance manually.
- Download a CSV template.
- Import attendance from CSV.
- Save attendance safely with duplicate prevention.

The feature must follow the current application architecture, coding standards, tenant-aware data boundaries, and existing menu/sidebar visibility rules.

## Menu Placement

- **Menu Group:** Faculty Related
- **Menu Display Name:** Enter Attendance
- **Suggested Menu Key:** `enter_attendance`
- **Authorized Roles:** SuperAdmin, Admin, Faculty
- **Role Access Rule:** Only SuperAdmin, Admin, and Faculty can access this menu.
- **Faculty Scope Rule:** Faculty can access only attendance data for subjects assigned to them.
- **Admin Scope Rule:** Admin and SuperAdmin can access the module according to existing tenant, campus, institution, and authorization boundaries.

## Phase 0. Access Control and Navigation

This phase defines how the menu is introduced into the application without affecting existing navigation behavior.

### Menu and Sidebar Requirements

- Add the **Enter Attendance** menu under the **Faculty Related** group.
- Add the menu into **Sidebar Settings** so its visibility, active status, and role mapping can be managed through the existing sidebar governance flow.
- The menu must be configurable in the same way as other governed sidebar entries.
- The new menu must not bypass existing route authorization or sidebar visibility rules.

### Access Requirements

- Only **SuperAdmin**, **Admin**, and **Faculty** may have access to this menu.
- Student and other non-authorized roles must not see or access the menu unless future requirements explicitly change that behavior.
- Sidebar role mapping for the new menu must default to **SuperAdmin**, **Admin**, and **Faculty** only.
- Direct route access must remain protected even if a user manually enters the URL.

### Phase 1 Implementation Summary

- Defined the new `enter_attendance` menu as a governed **Faculty Related** sidebar entry.
- Restricted default access to **SuperAdmin**, **Admin**, and **Faculty** only.
- Required the menu to be added to **Sidebar Settings** so visibility, active status, and role mapping remain governed by the existing sidebar administration workflow.
- Kept Phase 0 documentation-only so navigation and authorization requirements are captured before runtime changes begin.

### Phase 1 Validation Summary

- Verified Phase 0 scope remains documentation-only with no runtime, database, or API mutation in this step.
- Verified the Phase 0 requirements align with existing role-aware sidebar and route-guard patterns.
- Verified downstream documentation must reflect the same menu key, role access, and sidebar-governance expectations.

## Phase 1. Attendance Entry Methods

This phase introduces the core attendance entry capabilities.

The module must support both of the following entry methods:

1. **Manual Attendance Entry**
   Faculty can select the required academic scope and mark attendance for students directly in the UI.

2. **CSV Attendance Import**
   Faculty can upload attendance data using a CSV file that matches the system-provided template.

Attendance entry must be scoped by:

- Tenant
- Institute / Campus
- Department
- Course
- Subject assigned to the logged-in faculty

### Implementation Summary

- Added runtime menu wiring for `enter_attendance` as a distinct sidebar entry that points to the existing attendance page flow.
- Restricted the new menu to **SuperAdmin**, **Admin**, and **Faculty** through sidebar seeding and route-guard mappings.
- Added a dedicated `EnterAttendance` portal action so the page can be entered through the new menu without breaking existing `attendance` behavior.
- Preserved the active entry-point route during filter, mark, and correction operations so the new menu remains the controlling surface after user interaction.
- Reused the existing manual attendance entry UI for this phase-start slice.
- Added Enter Attendance CSV template download with required headers (`StudentId,StudentName,Date,Present`) and two example rows.
- Added Enter Attendance CSV import with file/header checks, required-field checks, roster-scoped StudentId validation, duplicate StudentId+Date file checks, and date/present parsing.
- Reused existing attendance bulk-mark APIs by grouping imported rows by date and posting validated entries.
- Hardened manual attendance write paths so submitted/corrected student IDs must belong to the selected offering roster for the effective tenant/campus scope.
- Normalized manual attendance status values to valid `Present` or `Absent` values before API submission.

### Validation Summary

- Verified editor diagnostics for the touched seed, controller, layout, view, and sidebar test files.
- Focused validation passed in `SidebarMenuIntegrationTests` (`17/17`).
- Verified the new menu is visible for **SuperAdmin**, **Admin**, and **Faculty**, and hidden for **Student** in the owning sidebar integration suite.
- Verified `Tabsan.EduSphere.Web` project build succeeds after adding CSV template/import actions and view wiring.
- Added and passed focused unit test matrix for attendance CSV import validation and roster hard-scope guards (`7/7`).

## Phase 2. Filter Criteria and Dynamic Selection

This phase defines the required filtering behavior before attendance is entered or imported.

The screen must provide the following filters.

### Required Filters

- Tenant
- Campus
- Department
- Course
- Subject
- Class for School/College institutions, or Semester for University institutions

### Optional Filter

- Student

## Filter Behavior

- All required filters must be selected before save actions are allowed.
- The Subject filter must only show subjects assigned to the logged-in faculty user.
- The Class/Semester filter must behave dynamically based on institution type.
- Downstream filters must refresh based on upstream selections, preserving current tenant-aware and campus-aware boundaries.
- Student filtering must narrow the visible result set but must not bypass the required filter selection rules.

### Phase 2 Implementation Summary

- Added dependent attendance filters in the portal UI for Tenant, Campus, Department, Course/Subject, and Class/Semester.
- Added server-side filter-context validation so attendance write actions require Department + Course + Class/Semester + Offering consistency under effective tenant/campus scope.
- Preserved filter context across Enter Attendance actions (template download, CSV import, manual mark, correction) through hidden route/form values.

### Phase 2 Validation Summary

- Added and passed focused unit tests for CSV import plus filter-context enforcement (`8/8`).
- Verified `Tabsan.EduSphere.Web` build passed after Phase 2 filter and controller changes.
- Revalidated sidebar governance behavior in `SidebarMenuIntegrationTests` (`17/17`).

## Phase 3. Template Download

This phase provides a system-generated CSV template for faculty and administrators.

The module must provide a **Download Template** button.

### Template Rules

- Export format must be CSV.
- The template must contain predefined headers required for import.
- The template must include 2 example rows.
- Example rows must demonstrate valid formatting only and must not be treated as production data.

### Suggested CSV Headers

- Tenant
- Campus
- Department
- Course
- Subject
- ClassOrSemester
- StudentId
- StudentName
- Date
- Present

### Phase 3 Implementation Summary

- Added strict-mode control to Enter Attendance CSV import so users can choose fail-fast validation or partial-success processing.
- Added row-level import feedback collection and surfaced it in the attendance UI as detailed warning items.
- Preserved selected filter context (tenant, campus, department, course, semester/class, offering) across template download/import operations.

### Phase 3 Validation Summary

- Focused attendance unit test matrix passed with strict-mode and partial-success coverage (`9/9`).
- Verified `Tabsan.EduSphere.Web` build succeeded after Phase 3 import-feedback enhancements.
- Revalidated sidebar/menu governance behavior in `SidebarMenuIntegrationTests` (`17/17`).

## Phase 4. Import Audit Trail (Stage 4.1)

### Goal

Ensure every attendance CSV import attempt creates an audit trail entry with actor, timestamp, strict mode, row counts, and top error reasons.

### Scope Delivered

- Added upload-level audit trail capture in `ImportAttendanceCsv` for blocked, failed, successful, and warning outcomes.
- Captured audit fields: uploader identity (`UserName`/`Email` fallback), UTC timestamp, strict-mode flag, offering, total/imported/skipped rows, and top validation reasons.
- Stored audit summary in portal temp state (`PortalImportAudit`) and emitted structured server logs.

### Phase 4.1 Implementation Summary

- Extended `PortalController` with logger-backed import audit writing across all return paths in the CSV import flow.
- Kept existing strict/non-strict import behavior unchanged while adding audit metadata.
- Updated controller unit-test construction sites for logger constructor dependency.

### Phase 4.1 Validation Summary

- Focused controller unit suite passed (`14/14`) covering attendance CSV and portal 2FA controller construction.
- Web build succeeded after audit instrumentation.
- Sidebar integration regression suite remained green (`17/17`).

## Phase 4.2. Import Result Report Download

### Goal

Allow faculty/admin users to download a CSV report after each import showing row-level outcomes (imported or skipped) with reasons.

### Scope Delivered

- Added per-import report token flow from CSV import action to Enter Attendance view.
- Added download endpoint for attendance import reports with one-time token retrieval.
- Added report generation with columns: row number, student fields, outcome, and reason.

### Phase 4.2 Implementation Summary

- `ImportAttendanceCsv` now generates/stores report payloads and redirects with `reportToken`.
- Enter Attendance UI now exposes `Download Last Import Report` when a token is present.
- Added unit coverage for report token propagation and report file download content.

### Phase 4.2 Validation Summary

- Attendance CSV import unit matrix passed (`12/12`) including report download test.
- Web build passed.
- Sidebar integration regression suite passed (`17/17`).

## Phase 4.3. Report Token Retention and Expiry Controls

### Goal

Harden report download flow with explicit retention and one-time-use token behavior so stale links fail safely with clear user feedback.

### Scope Delivered

- Enforced one-time token semantics (consume on first successful lookup).
- Added TTL enforcement (`2` hours) during report download path.
- Added explicit expired-token message separate from generic unavailable-token message.

### Phase 4.3 Implementation Summary

- Added retention controls via centralized report TTL and clock provider inside attendance import report flow.
- Updated download endpoint to return expired-message redirect when token age exceeds retention window.
- Added focused unit tests for one-time use and expiry behavior.

### Phase 4.3 Validation Summary

- Attendance import unit matrix passed (`14/14`) including one-time/expiry tests.
- Web build passed.
- Sidebar integration regression suite passed (`17/17`).

## Phase 4. CSV Import

This phase enables bulk attendance entry through template-based import.

The module must provide an **Import CSV** button.

### Import Validation Rules

The system must validate that:

- The uploaded file structure matches the template.
- Required fields are not empty.
- Student IDs exist in the system.
- The selected Subject is valid for the logged-in faculty.
- Date values are valid and parseable.
- Present values are valid boolean-style attendance values.
- Duplicate attendance records are not inserted for the same Student + Subject + Date combination.

### Import Result Behavior

- On validation failure, the system must return clear row-level error details.
- On success, the system must insert attendance records into the system.
- Partial success behavior must follow the application's existing import pattern. If no standard pattern exists, the preferred behavior is fail-fast with no partial insert.

## Phase 5. UI Behavior Logic

This phase defines how the attendance entry screen behaves as users select filters.

### Case 1. All Required Filters Selected

When all required filters are selected, display the student list and attendance entry table with the following columns:

- Student ID
- Student Name
- Date using a calendar picker
- Present using a checkbox

### Case 2. Filters Not Fully Selected

When required filters are not fully selected, display a generic table structure with the same columns:

- Student ID
- Student Name
- Date using a calendar picker
- Present using a checkbox

In this state, save actions must remain disabled until all required filters are selected.

## Phase 6. Attendance Table Structure

This phase defines the row-level attendance entry model displayed in the UI.

Each attendance row must contain:

- Student ID
- Student Name
- Date
- Present

### Field Meaning

- **Date:** Selectable using a calendar picker.
- **Present:** Checkbox where checked means Present and unchecked means Absent.

## Phase 7. Validation Rules

This phase defines validation behavior for manual entry and import.

The module must enforce the following validations before attendance is saved:

- All required filters are selected.
- Faculty can only submit attendance for their assigned subject scope.
- Required attendance fields are present.
- Duplicate attendance entries are prevented for the same Student, Subject, and Date.
- Attendance records must respect tenant, campus, department, and course boundaries.

## Phase 8. Integration and Technical Constraints

This phase ensures the feature fits the current application architecture and governed navigation model.

- Do not break or modify existing application logic outside the required feature surface.
- Maintain backward compatibility with current workflows.
- Follow existing architecture patterns across API, Application, Infrastructure, and Web layers.
- Reuse existing authorization, menu rendering, validation, and import patterns wherever possible.
- Keep the implementation tenant-aware, role-aware, and institution-type-aware.
- Ensure the new menu is available in Sidebar Settings for governed visibility management.
- Ensure role mapping remains restricted to SuperAdmin, Admin, and Faculty unless explicitly changed through approved governance flow.

## Phase 9. Database Requirements

This phase covers persistence and data integrity changes required for attendance entry.

Update or create attendance-related database structures as needed.

### Database Expectations

- Maintain proper foreign key relationships.
- Ensure indexing supports attendance entry and lookup performance.
- At minimum, ensure efficient indexing around:
  - StudentId
  - SubjectId
  - AttendanceDate
- Enforce duplicate prevention at the database level where practical, preferably through a unique constraint or equivalent safeguard covering Student + Subject + Date.

## Phase 10. Script Update Requirements

This phase ensures all database changes are reflected in deployment scripts.

After implementation, update the database scripts in the `Scripts` folder.

The implementation must include, where applicable:

- Table creation scripts
- Table alteration scripts
- Index creation scripts
- Stored procedures, if the current architecture uses them for attendance operations
- Optional seed or test data updates

## Phase 11. Integration Expectations

This phase defines the expected cross-module behavior after implementation.

- Sidebar/menu configuration must include the new menu under Faculty Related.
- Sidebar Settings must include the new menu so administrators can manage its visibility and active status under the existing governance model.
- Role access must be configured only for SuperAdmin, Admin, and Faculty unless a future requirement changes that scope.
- Existing attendance reporting, analytics, and student-facing attendance views must remain stable.
- Existing logic for tenant, campus, department, course, and subject resolution must not regress.

## Acceptance Criteria

The feature is complete when all of the following are true:

1. The **Enter Attendance** menu appears under the Faculty Related section.
2. The new menu is available in **Sidebar Settings** for governed role access and active/inactive management.
3. Only **SuperAdmin**, **Admin**, and **Faculty** can see or access the menu.
4. Faculty can filter by Tenant, Campus, Department, Course, Subject, and Class/Semester before saving attendance.
5. Subject options for faculty are restricted to subjects assigned to the logged-in faculty.
6. Faculty can manually mark attendance using a table with Student ID, Student Name, Date, and Present fields.
7. Authorized users can download a CSV template with predefined headers and 2 sample rows.
8. Authorized users can import attendance through CSV when the uploaded file matches the template.
9. The system rejects invalid CSV rows, missing required fields, unknown Student IDs, and duplicate Student + Subject + Date records.
10. Successful CSV import inserts valid attendance records correctly.
11. Existing application logic and workflows continue to function without regression.
12. Required database scripts under the `Scripts` folder are updated to reflect the implementation.

## Expected Outcome

After implementation:

- SuperAdmin, Admin, and Faculty can access the **Enter Attendance** menu according to sidebar and authorization rules.
- The new menu is manageable through **Sidebar Settings**.
- Faculty can filter students accurately.
- Faculty can enter attendance manually.
- Authorized users can upload attendance through CSV.
- Authorized users can download a template easily.
- The system remains stable, consistent, and backward compatible with existing functionality.
