# Faculty Enter Results Module Requirements

## Objective

Add a new **Enter Results** module under the **Faculty Related** section so authorized users can capture, validate, import, and publish student academic results without disrupting existing application logic, authorization flows, routing, or backward compatibility.

## Summary

The new module must allow faculty and authorized administrators to:

- Filter result-entry scope accurately.
- Enter results manually.
- Download a CSV template.
- Import results from CSV.
- Save results safely with duplicate and range validation.
- Publish results according to approval/governance rules.

The feature must follow current architecture, coding standards, tenant-aware boundaries, and existing menu/sidebar visibility rules.

## Menu Placement

- **Menu Group:** Faculty Related
- **Menu Display Name:** Enter Results
- **Suggested Menu Key:** `enter_results`
- **Authorized Roles:** SuperAdmin, Admin, Faculty
- **Role Access Rule:** Only SuperAdmin, Admin, and Faculty can access this menu.
- **Faculty Scope Rule:** Faculty can access only result-entry scope assigned to them.
- **Admin Scope Rule:** Admin and SuperAdmin can access this module according to tenant, campus, institution, and authorization boundaries.

## Phase 0. Access Control and Navigation

### Menu and Sidebar Requirements

- Add **Enter Results** under **Faculty Related**.
- Add the menu into **Sidebar Settings** so visibility, active status, and role mapping remain governed by the existing sidebar flow.
- The menu must be configurable like other governed sidebar entries.
- The module must not bypass existing route authorization or sidebar visibility rules.

### Access Requirements

- Only **SuperAdmin**, **Admin**, and **Faculty** may access the menu.
- Student and other non-authorized roles must not see or access this route unless future requirements explicitly change behavior.
- Direct route access must remain protected even when a URL is entered manually.

### Phase 0 Implementation Summary

- Added governed sidebar menu key `enter_results` under Faculty Related with display label **Enter Results**.
- Wired web route handling so `enter_results` opens dedicated portal action `EnterResults` while preserving existing `Results` surface and behavior.
- Enforced role defaults to Admin and Faculty with Student denied, while retaining SuperAdmin override through existing governance model.

### Phase 0 Validation Summary

- Sidebar integration matrix passed with `enter_results` visibility expectations for SuperAdmin/Admin/Faculty and deny expectation for Student.
- Solution build remained green after menu and route wiring.

## Phase 1. Result Entry Methods

The module must support both of these entry methods:

1. **Manual Result Entry**
   Faculty can select scope and enter marks/grades directly in the UI.
2. **CSV Result Import**
   Faculty can upload result data using a CSV file matching the system template.

Result entry must be scoped by:

- Tenant
- Institute / Campus
- Department
- Program / Course
- Subject
- Class/Semester
- Exam / Assessment Component

### Phase 1 Implementation Summary (Initial Slice)

- Added `PortalController.EnterResults(...)` as a dedicated entry route that reuses the existing result-entry workflow to avoid behavioral regressions.
- Kept existing manual result-entry behavior intact through the current Results page implementation.
- Preserved existing role-based and sidebar-governed authorization boundaries during entry-route expansion.

### Phase 1 Validation Summary (Initial Slice)

- Focused sidebar integration suite passed after route/menu changes.
- Full solution build passed.

### Phase 1 Implementation Summary (Runtime Completion)

- Implemented result CSV template download endpoint and CSV import endpoint under the Enter Results flow.
- Added strict-mode CSV validation and partial-success behavior with row-level skip/error feedback.
- Preserved tenant/campus scope and existing result write pathways while extending entry methods.

### Phase 1 Validation Summary (Runtime Completion)

- Web project build passed after result CSV template/import implementation.
- Sidebar/menu integration suite remained green for Enter Results access boundaries.

## Phase 2. Filter Criteria and Dynamic Selection

### Required Filters

- Tenant
- Campus
- Department
- Program/Course
- Subject
- Class (School/College) or Semester (University)
- Exam Type (Midterm/Final/Quiz/etc.)
- Assessment Component (Theory/Practical/Internal/etc.)

### Optional Filters

- Student
- Section
- Batch

### Filter Behavior

- All required filters must be selected before result write actions are enabled.
- Subject and assessment options must be limited by faculty assignment and timetable/exam-plan mapping.
- Downstream filters must refresh based on upstream selections while preserving tenant/campus boundaries.
- Student-level filter can narrow rows but must not bypass required filter rules.

### Phase 2 Implementation Summary

- Completed Enter Results filter scope wiring across Tenant/Campus, Department, Program/Course, Subject (course-offering scoped), and Class/Semester context.
- Added dependent filter refresh flow so downstream result-entry options re-evaluate when upstream scope changes.
- Added required-filter write guard behavior so result write actions remain disabled until required scope inputs are selected.
- Preserved tenant/campus and role-authorization boundaries while applying faculty/admin scoped filtering behavior.

### Phase 2 Validation Summary

- Manual verification completed for required-filter gating, dependent-refresh behavior, and scoped filtering continuity.
- Backward-compatibility check completed for existing Results route and role-based access behavior.
- No schema or migration change required for this phase.

### Phase 2 Validation Summary (Runtime Completion)

- Implemented required-filter write guards for Enter Results save/publish/import operations.
- Implemented dependent filter sequencing for Department, Program/Course, Subject, and Semester/Class context.
- Added server-side scope validation to prevent write operations outside selected filter context.

## Phase 3. Template Download

The module must provide a **Download Template** button.

### Template Rules

- Export format must be CSV.
- The template must contain required headers for results import.
- The template must include 2 valid example rows.
- Example rows are for format guidance only and must not be treated as production data.

### Suggested CSV Headers

- Tenant
- Campus
- Department
- Course
- Subject
- ClassOrSemester
- ExamType
- AssessmentComponent
- StudentId
- StudentName
- MarksObtained
- MaxMarks
- Grade
- Remarks

### Phase 3 Implementation Summary

- Added Enter Results **Download Template** runtime behavior in web flow.
- Template export remains CSV and now includes two explicit example rows for format guidance.
- Example rows are marked as guidance-only and are excluded from import write processing.

### Phase 3 Validation Summary

- Web build passed after template example-row update.
- Existing Enter Results route/menu governance remained unchanged.
- Template download behavior verified with CSV header and example-row output.

## Phase 4. Import Validation, Audit, and Reporting

### Core Validation Rules

- Uploaded structure must match template headers and order rules.
- Required fields must not be empty.
- Student IDs must exist and belong to selected scope.
- Marks must be numeric and within valid range: $0 \leq MarksObtained \leq MaxMarks$.
- Grade, when supplied, must match configured grading schema.
- Duplicate entries for the same Student + Subject + ExamType + AssessmentComponent must be blocked per policy.
- Import policy must support strict mode (fail-fast) and non-strict mode (partial-success with skip reasons).

### Stage 4.1 - Upload Audit Trail

- Every import attempt must produce an audit entry.
- Audit entry must include actor identity, UTC timestamp, strict-mode state, scope context, total/imported/skipped rows, and top reasons.

### Stage 4.2 - Import Result Report Download

- After import, users must be able to download a row-level result report.
- Report must include row outcome (`Imported` or `Skipped`) and reason.

### Stage 4.3 - Token Retention and Expiry Controls

- Report links must be one-time-use.
- Report links must expire after configured TTL.
- Expired and unavailable token paths must return distinct user messages.

### Stage 4.4 - UX Clarification

- UI must clearly state one-time and expiry behavior near report action controls.

### Stage 4.5 - Web Route Integration Coverage

- Add route-level integration tests to verify valid, invalid, and expired report-token behavior through actual web routing.

### Phase 4 Implementation Summary

- Added upload-level audit trail for Enter Results CSV import with actor identity, UTC timestamp, strict-mode flag, scope context, totals, imported/skipped counts, and top reasons.
- Added row-level import result report generation with `Imported`/`Skipped` outcomes and reason details.
- Added one-time report token retention with explicit TTL expiry behavior and distinct unavailable versus expired user messages.
- Added report-download UX guidance that explains one-time-use and 2-hour expiry behavior.
- Added web-route integration coverage for valid token download, one-time-use token behavior, and expired-token behavior.

### Phase 4 Validation Summary

- Web integration test suite for result import report route passed (`3/3`).
- Web build passed after phase 4 implementation.
- Sidebar integration suite remained green (`17/17`).

## Phase 5. UI Behavior Logic

### Case 1. All Required Filters Selected

Display editable result-entry table with columns such as:

- Student ID
- Student Name
- Marks Obtained
- Max Marks
- Grade
- Remarks

Enable save/import actions only when scope and validations are satisfied.

### Case 2. Filters Not Fully Selected

Display a disabled or guidance state with the same structure, but keep write actions disabled until required filters are complete.

### Phase 5 Implementation Summary

- Added Enter Results phase-5 UI behavior table state in the Results view for authorized roles.
- Added an editable result-entry grid when required filters are complete with columns: Student ID, Student Name, Marks Obtained, Max Marks, Grade, and Remarks.
- Added a guidance-state table with the same structure when required filters are incomplete; row inputs remain disabled.
- Kept save/import/publish action gating aligned with existing required-filter write guard logic.

### Phase 5 Validation Summary

- Web build passed after phase-5 UI behavior updates.
- Added focused unit coverage for `ResultsPageModel.CanWriteResults` required-filter gating behavior.

## Result Publishing Rules

- Draft results can be saved by authorized users.
- Final publish requires configured role/approval flow.
- Published results must be immutable unless correction workflow is used.
- Corrections must be fully auditable with reason and actor metadata.

## Non-Functional Requirements

- Preserve existing authorization and tenant isolation patterns.
- Ensure validation error messages are clear and row-specific.
- Support operational logging and troubleshooting through structured logs.
- Maintain backward compatibility for existing attendance and academic modules.

## Test Requirements

- Unit tests for validation matrix, strict/non-strict behavior, duplicate guards, and range checks.
- Integration tests for sidebar/menu access and filter enforcement.
- Web-route integration tests for report download token lifecycle.
- Regression validation for unaffected modules.

## Acceptance Criteria

1. Authorized roles can access **Enter Results** and unauthorized roles cannot.
2. Manual and CSV result entry both work under scoped filters.
3. Validation blocks invalid rows and returns actionable feedback.
4. Audit trail and import report are generated for every import attempt.
5. Report token one-time and expiry behavior works as designed.
6. Publishing and correction flows follow role and audit requirements.
