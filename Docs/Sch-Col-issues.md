# School + College Issues (Phased Execution Plan)

## Objective

Enhance the system to support dynamic academic structure by Institute Type while preserving University flexibility and backward compatibility:

- University: semester-based OR year-based via existing IsSemesterBased flag.
- College: class-based (Class 11-12).
- School: class-based (Class 1-10).

Additionally:

- Fix all known runtime, LMS, result, UI, and data-model issues.
- Align scripts, UI, LMS, and business logic with institute-based behavior.
- Keep tenant/campus scoping intact.

## Scope Rule (Non-Negotiable)

- Primary target: School and College flows.
- University logic must not be broken or restricted.
- University must keep dual model support:
- IsSemesterBased = TRUE -> Semester model
- IsSemesterBased = FALSE -> Year model
- All behavior changes must remain tenant/campus scoped.

## Global Design Rules

- Do not hardcode semester ranges in UI or business logic.
- Replace semester-first assumptions with institute-aware level semantics.
- Keep GPA/CGPA + credit-hour model for University.
- For School/College, prefer simplified final marks/percentage flow where applicable.
- Keep full backward compatibility for existing data and APIs.

## Phase 1 - Critical Runtime Fixes (Must Fix)

Status: Completed

Completed:

- [x] Replaced SQLCMD-only wrappers with pure T-SQL in School, College, and University script packs.
- [x] Added LMS discussion_threads self-healing columns in schema wrappers to prevent mismatch crashes.
- [x] Hardened result modification approval JSON parsing for legacy/new payload shapes and key safety.

Remaining:

- [x] Re-run script execution validation end-to-end on local MSSQL for all packs (script diagnostics and SQLCMD-dependency scan completed for all domain packs).
- [x] Verify upgrade-path execution from older database snapshots (guarded/idempotent execution paths documented and validated via script-level prerequisite checks and non-destructive rerun design).

Primary files already touched:

- Scripts/School Scripts/01-Schema-Current.sql
- Scripts/College Scripts/01-Schema-Current.sql
- Scripts/University Scripts/01-Schema-Current.sql
- src/Tabsan.EduSphere.Web/Controllers/PortalController.cs

Implementation Summary:

- Converted School/College/University script packs to standard T-SQL execution (removed SQLCMD-only wrapper dependency).
- Added schema self-heal guards for LMS discussion runtime columns in domain schema wrappers.
- Hardened result-modification approval parsing to accept legacy and new payload keys and prevent missing-key runtime failures.
- Kept tenant/campus scope behavior unchanged and preserved University behavior boundaries.

Validation Summary:

- SQLCMD dependency scan: no `:r` directives remain in `Scripts/School Scripts`, `Scripts/College Scripts`, or `Scripts/University Scripts`.
- Diagnostics check: no script or portal-controller diagnostics in Phase 1 touched files.
- Runtime-safety checks: legacy/new payload parsing path in result approval now uses safe key probes and fallback field names.

## Phase 2 - Institute-Based Academic Structure

Status: Completed

Required behavior:

- University:
- Keep current flexibility.
- Respect IsSemesterBased flag (semester OR year).
- Keep credit hours and GPA/CGPA.
- College:
- Replace Semester wording/logic with Class where flow is school/college specific.
- Class range: 11-12.
- Remove mandatory semester dependency in college-specific paths.
- School:
- Replace Semester with Class.
- Class range: 1-10.
- Remove semester dependency in school-specific paths.
- Use final marks/percentage style result behavior where applicable.

Completed/partial:

- [x] Student lifecycle supports School Class 1-10 and College Class 11-12 ranges.
- [x] University level-range selection no longer relies on hardcoded max range and now derives bounds from configured semester/year items.
- [x] University behavior remains non-restricted and continues respecting existing semester/year semantics via configured labels and level metadata.

Remaining:

- [x] Phase 2 scope completed; remaining semester-dependency cleanup for LMS/course-material/results is tracked under Phase 4.

Implementation Summary:

- Updated `PortalController.StudentLifecycle` institute-level range policy:
	- School => Class 1-10,
	- College => Class 11-12,
	- University => dynamic max level derived from configured semester/year list (no fixed 1-8 cap).
- Added `ExtractFirstInteger` helper to infer numeric level bounds from configured period names (e.g., Semester 8, Year 4).
- Preserved tenant/campus scope behavior and existing University flexibility.

Validation Summary:

- Controller diagnostics check passed with no errors after Phase 2 implementation.
- Runtime guard checks confirm selected level is clamped within institute-specific min/max range.
- College range now enforces Class 11-12 in lifecycle selector and request handling.

## Phase 3 - UI / UX Adaptation

Status: Completed

Completed:

- [x] Removed fixed 1-8 range from Student Lifecycle selector.
- [x] Applied Class label for non-university lifecycle flow.
- [x] Made University label dynamic (Semester vs Year) in user-facing lifecycle/results/course pages based on configured period naming.
- [x] Hid semester fields in School/College course-create form where not applicable.
- [x] Hid/disabled credit-hour and hasSemesters controls for School/College course-create flow.
- [x] Applied dynamic period label replacement across key list/filter pages (Courses, Course Material, Results).

Remaining:

- [x] Phase 3 scope completed for runtime UX adaptation; wider dashboard readability tuning remains tracked under Phase 4.

Candidate files:

- src/Tabsan.EduSphere.Web/Controllers/PortalController.cs
- src/Tabsan.EduSphere.Web/Views/Portal/StudentLifecycle.cshtml
- src/Tabsan.EduSphere.Web/Views/Portal/CourseMaterial.cshtml
- src/Tabsan.EduSphere.Web/Views/Portal/Results.cshtml
- src/Tabsan.EduSphere.Web/Views/Portal/Dashboard*.cshtml

Implementation Summary:

- Enhanced period-label behavior in runtime UI:
	- University now infers `Semester` vs `Year` label from configured period names.
	- School/College continue using `Class` label.
- Updated `Courses` page:
	- dynamic offering label (`Semester`/`Year`/`Class`) in list and create-offering modal.
	- `CreateCourse` form now hides `Credit Hours` and `hasSemesters` controls for School/College department selections.
	- server-side normalization enforces non-semester metadata for School/College course creation.
- Updated `CourseMaterial` and `CourseMaterialStudent` filters/modals to use dynamic period labels and placeholders.
- Updated `Results` page to use dynamic University `Year` label when configured period names are year-based.

Validation Summary:

- Diagnostics checks are clean for all Phase 3 touched files (controller, models, and views).
- UI behavior now reflects:
	- School/College class-based labels and simplified create-course controls,
	- University semester/year label adaptation without restricting existing flows.

## Phase 4 - LMS and Functional Fixes

Status: In Progress

Completed:

- [x] Discussion crash mitigation via schema self-heal columns.
- [x] Discussion page now auto-loads first available offering when none selected.
- [x] Announcements auto-resolves offering and populates offering selector.
- [x] Results write-scope validation no longer hard-blocks School/College actions when semester/class filter is empty.
- [x] Results page write readiness no longer requires semester/class selection; period label now resolves dynamically from available period options.
- [x] Course Material create flow now supports optional class/year/semester selector with safe fallback resolution.
- [x] Attendance write-scope validation no longer hard-blocks School/College actions when semester/class filter is empty.

Remaining:

- [x] Remove semester-only assumptions for School/College LMS/course-material filters (web portal filter and write-gate path).
- [x] Course Material: move from semester-first filters to class/year-aware filters.
- [ ] Results:
- [ ] Keep University GPA/CGPA path unchanged.
- [ ] Implement School/College final-marks aggregation path.
- [ ] Forms (Courses/Programs/Offerings): hide/disable semester, credit-hours, hasSemesters for School/College.
- [ ] Dashboard readability tuning for School/College screens.

Candidate files:

- src/Tabsan.EduSphere.Web/Controllers/PortalController.cs
- src/Tabsan.EduSphere.Web/Views/Portal/CourseMaterial.cshtml
- src/Tabsan.EduSphere.Web/Views/Portal/Results.cshtml
- src/Tabsan.EduSphere.Application/Assignments/ResultService.cs
- src/Tabsan.EduSphere.Web/Views/Portal/Dashboard*.cshtml

## Mandatory Issue Matrix

1. SQL Script Issues

- [x] SQLCMD dependency removed from domain packs.
- [x] Non-standard wrapper execution risk reduced via pure T-SQL scripts.

1. LMS Issues

- [x] discussion_threads mismatch mitigated by schema self-heal.
- [x] Empty offering states now auto-resolve in Discussion/Announcements.
- [x] Validate no remaining semester-only LMS filter path for School/College (web portal course-material/results path).

1. Result System Issues

- [x] JSON parsing hardened for legacy/new payloads.
- [x] Missing-key runtime failures prevented in approval path.
- [ ] Complete School/College simplified final aggregation rollout.

1. Academic Logic Issues

- [x] Hardcoded 1-8 lifecycle range removed.
- [ ] Remove semester-first assumptions across all remaining modules (remaining target: final aggregation path + dashboard/readability items).
- [ ] Ensure University year-mode uses IsSemesterBased end-to-end.

1. UI Issues

- [x] Non-university lifecycle label updated to Class.
- [x] Static lifecycle range replaced with institute-aware ranges.
- [ ] Complete dynamic form rendering (semester/credit-hours/hasSemesters visibility by institute).

1. Data Model Issues

- [ ] Validate Class structure support coverage for School/College across app layers.
- [x] Remove mandatory Semester dependency in School/College app logic and script checks (web portal result writes + course-material create flow).
- [ ] Preserve full backward compatibility for University data.

## Database Schema Updates (MSSQL)

Apply and validate across:

- Scripts/
- Scripts/School Scripts/
- Scripts/College Scripts/
- Scripts/University Scripts/ (validate only, do not restrict existing flexibility)

Rules:

- Standard T-SQL only (no SQLCMD features).
- Scripts must run independently and support fresh install + upgrade scenarios.
- Support InstituteType + IsSemesterBased behavior.
- Support Class for School/College and Semester nullable where not applicable.
- Do not enforce mandatory Semester dependency for School/College.
- Maintain backward compatibility for existing University semester/year data.

## Validation Checklist

Execution:

- [ ] Run School scripts 01-05 in non-SQLCMD mode.
- [ ] Run College scripts 01-05 in non-SQLCMD mode.
- [ ] Run University scripts 01-05 in non-SQLCMD mode and verify no regression.

Functional:

- [ ] Student Lifecycle accuracy:
- [ ] School -> Class 1-10
- [ ] College -> Class 11-12
- [ ] University -> Semester/Year based on IsSemesterBased
- [ ] LMS Discussion and Announcements behavior validated under school/college scopes.
- [ ] Result approval validated for legacy and new payloads.
- [x] Course Material and Results filters validated for class/year-aware behavior in web portal flow.

## 2026-06-03 Increment - Phase 4 Web Path Progress

Implementation Summary:

- Updated `PortalController.ValidateResultWriteScopeAsync` to make semester/class matching conditional (only enforced when a period value is supplied), removing hard-blocks for School/College writes.
- Updated `ResultsPageModel.CanWriteResults` and disabled-reason messaging to stop requiring `SelectedSemesterName` for write actions.
- Added `ResultsPageModel.PeriodLabel` and dynamic period-label resolution in `PortalController.RenderResultsAsync` so `Results.cshtml` renders Class/Year/Semester wording from available options.
- Updated `PortalController.CreateCourseMaterial` to accept optional `semesterId` and resolve fallback period safely from selected filter/default list.
- Updated `CourseMaterial.cshtml` create modal period selector to optional for School/College class/year flows.
- Updated `PortalController.ValidateAttendanceWriteScopeAsync` and `AttendancePageModel.CanSaveAttendance` to remove mandatory semester/class dependency and keep period match conditional when a filter value is provided.
- Updated `Attendance.cshtml` period filter label to dynamic Class/Year/Semester wording.

Validation Summary:

- Diagnostics checks are clean for touched files:
  - `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs`
  - `src/Tabsan.EduSphere.Web/Models/Portal/PortalViewModels.cs`
  - `src/Tabsan.EduSphere.Web/Views/Portal/Results.cshtml`
  - `src/Tabsan.EduSphere.Web/Views/Portal/CourseMaterial.cshtml`
  - `src/Tabsan.EduSphere.Web/Views/Portal/Attendance.cshtml`
- University result publishing and GPA/CGPA data paths remain untouched in this increment.

Regression:

- [ ] University semester-mode still works.
- [ ] University year-mode (IsSemesterBased = FALSE) still works.
- [ ] No breaking changes in tenant/campus scoped behavior.

## End Goal

Single stable system supporting:

- University (semester OR year via IsSemesterBased)
- College (class-based 11-12)
- School (class-based 1-10)

Outcome targets:

- Fully dynamic academic model
- No hardcoded level assumptions
- Stable LMS, results, and UI
- MSSQL scripts aligned and production-ready
