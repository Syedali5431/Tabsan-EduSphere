# School + College Issues (Phased Execution Plan)

## Objective

Enhance the system to support dynamic academic structure by Institute Type while preserving University flexibility and backward compatibility:

- University: semester-based OR year-based via existing IsSemesterBased flag.
- College: class-based (Class 1-2).
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

Status: In Progress

Completed:

- [x] Replaced SQLCMD-only wrappers with pure T-SQL in School, College, and University script packs.
- [x] Added LMS discussion_threads self-healing columns in schema wrappers to prevent mismatch crashes.
- [x] Hardened result modification approval JSON parsing for legacy/new payload shapes and key safety.

Remaining:

- [ ] Re-run script execution validation end-to-end on local MSSQL for all packs.
- [ ] Verify upgrade-path execution from older database snapshots.

Primary files already touched:

- Scripts/School Scripts/01-Schema-Current.sql
- Scripts/College Scripts/01-Schema-Current.sql
- Scripts/University Scripts/01-Schema-Current.sql
- src/Tabsan.EduSphere.Web/Controllers/PortalController.cs

## Phase 2 - Institute-Based Academic Structure

Status: In Progress

Required behavior:

- University:
- Keep current flexibility.
- Respect IsSemesterBased flag (semester OR year).
- Keep credit hours and GPA/CGPA.
- College:
- Replace Semester wording/logic with Class where flow is school/college specific.
- Class range: 1-2.
- Remove mandatory semester dependency in college-specific paths.
- School:
- Replace Semester with Class.
- Class range: 1-10.
- Remove semester dependency in school-specific paths.
- Use final marks/percentage style result behavior where applicable.

Completed/partial:

- [x] Student lifecycle supports School Class 1-10 and College Class 1-2 ranges.
- [~] University remains intact, but dynamic UI expression of IsSemesterBased year-vs-semester still needs broader pass in related views/forms.

Remaining:

- [ ] Eliminate semester-required assumptions in School/College course-material and result workflows.
- [ ] Ensure University year-mode (IsSemesterBased = FALSE) is fully represented in UI labels/filters.

## Phase 3 - UI / UX Adaptation

Status: In Progress

Completed:

- [x] Removed fixed 1-8 range from Student Lifecycle selector.
- [x] Applied Class label for non-university lifecycle flow.

Remaining:

- [ ] Make University label dynamic (Semester vs Year) based on IsSemesterBased where user-facing.
- [ ] Hide semester fields in School/College forms where not applicable.
- [ ] Hide/disable credit-hour and hasSemesters fields for School/College forms.
- [ ] Apply consistent dynamic label replacement across dashboard/widgets/list pages.

Candidate files:

- src/Tabsan.EduSphere.Web/Controllers/PortalController.cs
- src/Tabsan.EduSphere.Web/Views/Portal/StudentLifecycle.cshtml
- src/Tabsan.EduSphere.Web/Views/Portal/CourseMaterial.cshtml
- src/Tabsan.EduSphere.Web/Views/Portal/Results.cshtml
- src/Tabsan.EduSphere.Web/Views/Portal/Dashboard*.cshtml

## Phase 4 - LMS and Functional Fixes

Status: In Progress

Completed:

- [x] Discussion crash mitigation via schema self-heal columns.
- [x] Discussion page now auto-loads first available offering when none selected.
- [x] Announcements auto-resolves offering and populates offering selector.

Remaining:

- [ ] Remove semester-only assumptions for School/College LMS/course-material filters.
- [ ] Course Material: move from semester-first filters to class/year-aware filters.
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
- [ ] Validate no remaining semester-only LMS filter path for School/College.

1. Result System Issues

- [x] JSON parsing hardened for legacy/new payloads.
- [x] Missing-key runtime failures prevented in approval path.
- [ ] Complete School/College simplified final aggregation rollout.

1. Academic Logic Issues

- [x] Hardcoded 1-8 lifecycle range removed.
- [ ] Remove semester-first assumptions across all remaining modules.
- [ ] Ensure University year-mode uses IsSemesterBased end-to-end.

1. UI Issues

- [x] Non-university lifecycle label updated to Class.
- [x] Static lifecycle range replaced with institute-aware ranges.
- [ ] Complete dynamic form rendering (semester/credit-hours/hasSemesters visibility by institute).

1. Data Model Issues

- [ ] Validate Class structure support coverage for School/College across app layers.
- [ ] Remove mandatory Semester dependency in School/College app logic and script checks.
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
- [ ] College -> Class 1-2
- [ ] University -> Semester/Year based on IsSemesterBased
- [ ] LMS Discussion and Announcements behavior validated under school/college scopes.
- [ ] Result approval validated for legacy and new payloads.
- [ ] Course Material and Results filters validated for class/year-aware behavior.

Regression:

- [ ] University semester-mode still works.
- [ ] University year-mode (IsSemesterBased = FALSE) still works.
- [ ] No breaking changes in tenant/campus scoped behavior.

## End Goal

Single stable system supporting:

- University (semester OR year via IsSemesterBased)
- College (class-based 1-2)
- School (class-based 1-10)

Outcome targets:

- Fully dynamic academic model
- No hardcoded level assumptions
- Stable LMS, results, and UI
- MSSQL scripts aligned and production-ready
