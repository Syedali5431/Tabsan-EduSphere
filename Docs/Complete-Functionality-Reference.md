# Tabsan-EduSphere - Complete Functionality Reference

**Document Version:** 2.1 (Final Phase 38 Baseline)  
**Date:** May 15, 2026  
**Project Status:** Phase 38 complete (final publish-separation baseline)  
**Last Updated:** Based on PRD v1.75 and execute-mode closure evidence  

## Institute Parity Documentation Governance (2026-05-13)

For every completed stage under `Docs/Institute-Parity-Issue-Fix-Phases.md`, the stage entry must include:
- `Implementation Summary`
- `Validation Summary`

After each completed stage, this document must be updated to reflect any net functionality behavior change for School/College/University parity, including role/institute filters and report behavior.

## 2026-05-18 Update - Documentation Synchronization Request (Execution Snapshot)

- Recent request issue:
    - mandatory execution/planning tracker updates for the latest request were pending synchronized closure across six governance documents.
- Implementation Summary:
    - added synchronized 2026-05-18 execution snapshot coverage in PRD, consolidated issues, function list, functionality reference, development plan, and database schema documentation,
    - aligned issue wording and closure language so all trackers carry the same implementation and validation intent.
- Validation Summary:
    - verified each updated tracker contains a dated closure entry with both implementation and validation blocks,
    - verified the update is documentation-only and does not change runtime functionality, API contracts, or deployment behavior.
- Behavior impact:
    - no runtime functionality change,
    - documentation traceability for this request is complete and audit-consistent.

## 2026-05-18 Update - DeepScan Gap Phase/Stage Synchronization Request (Execution Snapshot)

- Recent request issue:
    - DeepScan findings identified missing/partial capability areas that were not yet captured as executable phase/stage entries in governance trackers.
- Implementation Summary:
    - added consolidated staged remediation planning (Phase 39/40) for waitlist, transactional import strict mode, MFA hardening, and EF warning cleanup,
    - synchronized this request closure snapshot across PRD, consolidated tracker, function list, development plan, and database schema docs.
- Validation Summary:
    - verified all six mandatory docs contain matching 2026-05-18 DeepScan-gap synchronization entries,
    - verified this request is documentation-only and does not alter runtime functionality, API contracts, or deployment behavior.
- Behavior impact:
    - no runtime functionality change,
    - functionality governance now explicitly tracks the DeepScan remediation roadmap.

## 2026-05-15 Update - Final Phase 37/38 Execute Closure Snapshot

- Completed execute-mode closure for final separation phases.
- Functional behavior confirmed:
    - runtime app publish output is separated from license app publish output,
    - non-runtime assets (docs/guides/scripts/training/import templates) are packaged independently from runtime app artifacts.
- Validation evidence:
    - `Artifacts/Phase37/Publish-Separation-20260515.md` shows execute mode `True` and phase status `PASS` (`4/4`),
    - `Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md` shows execute mode `True` and phase status `PASS` (`7/7`).
- Behavior impact:
    - production app packaging no longer bundles license tooling,
    - operational/documentation assets are distributed through dedicated non-runtime packaging workflow.

## 2026-05-13 Update - Institute Parity Stage 0.1 Audit Snapshot

- Completed baseline functional inventory for parity-scope modules (timetable, courses, buildings/rooms, departments, assignments, enrollments, reports, results, quizzes, lifecycle, payments, settings surfaces).
- Confirmed broad endpoint/service/repository coverage already exists for all target modules, with role guards primarily centered on SuperAdmin/Admin/Faculty/Student authorization and department/offering scoping.
- Identified remaining University-default behavior hotspots in branding/onboarding/prompt/template text and default policy assumptions, now queued into subsequent parity implementation stages.
- No user-visible functionality was changed in this stage; this is a validated baseline mapping update.

## 2026-05-14 Update - Phase 23 Stage 23.2 Dynamic Academic Labels and Context Snapshot

- Completed dynamic academic vocabulary parity validation for School/College/University policy modes.
- Functional behavior confirmed:
    - `GET /api/v1/labels` returns policy-based vocabulary for current tenant configuration,
    - University-enabled policy resolves University vocabulary,
    - School-only policy resolves School vocabulary,
    - College-only policy resolves College vocabulary,
    - School+College policy resolves School vocabulary,
    - mixed policy including University resolves University vocabulary.
- Web behavior confirmed:
    - portal API client consumes vocabulary contract via `GetVocabularyAsync`,
    - module composition surface renders returned label set for context-aware wording without screen duplication.
- Validation evidence:
    - focused integration suite passed (`8/8`) for `DynamicLabelIntegrationTests`,
    - unit label-service suite remains green (`4/4`) in `Phase24Tests` label tests.
- Behavior impact:
    - tenant-wide label terminology is now explicitly validated end-to-end for API and portal consumption,
    - no schema/migration change introduced in Stage 23.2.

## 2026-05-13 Update - Institute Parity Stage 0.2 Access Matrix Snapshot

- Completed role/institute access matrix baseline across core modules and operations (view/create/edit/deactivate/export).
- Confirmed effective enforcement pattern is currently mixed:
    - explicit institution policy flags at platform level,
    - operational scoping mainly through department assignment, course-offering ownership, and student self-ownership.
- Identified parity risk areas:
    - institute-specific checks not explicit on all mutation paths,
    - School/College filter coverage still incomplete on selected analytics/report surfaces.
- No runtime behavior changes were introduced in Stage 0.2; this is baseline authorization/scope mapping evidence for upcoming remediation stages.

## 2026-05-13 Update - Institute Parity Stage 0.3 Report Failure Snapshot

- Completed baseline inventory of report failures and report-adjacent operational failure modes.
- Confirmed previously observed critical failures are resolved:
    - Result Summary runtime exception,
    - Report Center visibility gaps for privileged roles.
- Classified remaining report parity risks:
    - incomplete explicit School/College/University filter propagation,
    - expected scope-guard failures that require UX/API clarity,
    - sparse dummy data causing empty outputs in some report scenarios.
- This stage introduced no runtime code change; it establishes a tagged failure backlog for Phase 4 and Phase 5 execution.

## 2026-05-13 Update - Institute Parity Stage 0.4 Exit Snapshot

- Completed Phase 0 sign-off by consolidating module audit, role/institute matrix, and report failure inventory into a prioritized execution backlog.
- Confirmed functional reference readiness for parity implementation phases:
    - authorization and scope hardening (Phase 2),
    - module workflow parity (Phase 3),
    - report/analytics parity fixes (Phase 4),
    - data-completeness hardening (Phase 5).
- No runtime functionality changed in Stage 0.4; this is governance and execution-readiness closure for Phase 0.

## 2026-05-13 Update - Institute Parity Stage 1.1 Normalization Snapshot

- Added canonical institute dimension normalization for departments by introducing persisted `InstitutionType` (School/College/University) with default compatibility set to University.
- Department create/update API flows now enforce license policy constraints for institution type selection, preventing assignment to disabled modes.
- Department read payloads now expose institution type to support downstream parity-aware UI/API behavior.
- Existing department CRUD behavior remains backward-compatible for current University-first flows because create defaults and update optionals preserve prior request shapes.
- Stage 1.1 establishes the schema and contract base for follow-on institute-scoped referential and indexing hardening in Stage 1.2.

## 2026-05-13 Update - Institute Parity Stage 1.2 Referential Integrity + Indexing Snapshot

- Added referential guards on academic write paths:
    - course create now requires valid department,
    - offering create now requires valid course/semester and faculty-department assignment when faculty is provided,
    - student profile creation now enforces program-department alignment.
- Normalized academic program uniqueness to department scope (`Code + DepartmentId`) for parity-safe multi-department operation.
- Added institute/report-heavy index coverage on programs, courses, offerings, student profiles, enrollments, and role-assignment lookup paths to reduce filter/join overhead.
- Updated enrollment status storage to bounded string length for SQL index support on active-status query paths.
- Behavior impact: invalid cross-scope academic links now fail early with explicit validation errors, while existing valid workflows remain backward-compatible.

## 2026-05-13 Update - Institute Parity Stage 1.3 Script Hardening Snapshot

- Hardened deployment SQL scripts to mirror Stage 1.1/1.2 runtime schema behavior for script-first rollout paths.
- `01-Schema-Current.sql` now includes idempotent parity migration-aligned blocks for department institution dimension and Stage 1.2 index/column updates.
- `04-Maintenance-Indexes-And-Views.sql` now enforces parity-critical index presence with safe existence guards.
- `05-PostDeployment-Checks.sql` now emits explicit parity verification signals for migration IDs, column presence/shape, and critical index existence.
- User-visible runtime behavior is unchanged by Stage 1.3 itself; this stage improves deployment safety and consistency across environments.

## 2026-05-13 Update - Institute Parity Stage 1.4 Exit Criteria Snapshot

- Completed Phase 1 exit verification enablement for institute parity schema/data integrity checks.
- Post-deployment verification now explicitly reports:
    - department institution-type validity and representation coverage,
    - orphan counts across institute-linked academic relations and assignment links.
- Runtime behavior impact:
    - no API contract or user-flow mutation in Stage 1.4,
    - improved operational confidence by making Phase 1 exit criteria auditable with deterministic SQL check outputs.

## 2026-05-13 Update - Institute Parity Stage 2.1 SuperAdmin Capability Snapshot

- Expanded SuperAdmin management capabilities for user-to-department assignment workflows by adding direct Faculty department assignment APIs (assign/remove/list).
- Strengthened institute-scope correctness for assignment operations by enforcing institution-type compatibility between users and target departments when explicit user institution type is set.
- Added assignment-management response visibility for user institution type in SuperAdmin user candidate listings.
- Behavior impact:
    - SuperAdmin assignment flows now cover both Admin and Faculty department assignment operations,
    - cross-institute assignment mismatches now fail early with explicit validation responses,
    - existing Admin-user create/update/assignment behavior remains backward-compatible.

## 2026-05-13 Update - Institute Parity Stage 2.2 Role-Scoped Institute Enforcement Snapshot

- Added `institutionType` JWT claim propagation for explicitly assigned users to support runtime institute-scope checks.
- Extended report endpoint authorization flow for Admin/Faculty to enforce institute compatibility in addition to existing role/department/offering scope checks.
- Added integration proof that an Admin with valid department assignment is still denied when institution scope mismatches target department.
- Behavior impact:
    - Admin/Faculty report access is now constrained by both role scope and institute scope where explicit institution assignment exists,
    - SuperAdmin behavior remains global and unchanged,
    - existing report export contracts and metadata remain stable.

## 2026-05-13 Update - Institute Parity Stage 2.3 Menu/Action Guard Consistency Snapshot

- Added portal-level guard consistency enforcement so direct portal URL navigation must match visible sidebar menu permissions for non-SuperAdmin users.
- Introduced a centralized action-to-menu key map in `PortalController` to keep menu visibility and invokable portal section routes aligned.
- Preserved SuperAdmin unrestricted behavior while applying deterministic redirect-on-deny behavior for hidden sections.
- Added integration proof that hidden menu state and endpoint authorization remain consistent (`Admin` hidden settings path -> forbidden, `SuperAdmin` visible settings path -> allowed).
- Behavior impact:
    - hidden menu sections can no longer be accessed by direct URL path in the portal flow,
    - visible menu sections retain expected accessibility for authorized roles,
    - existing role matrix behavior remains backward-compatible.

## 2026-05-13 Update - Institute Parity Stage 2.4 Exit Criteria Snapshot

- Completed Phase 2 authorization and permission correction closeout.
- Confirmed role + institute access matrix pass state across integrated Stage 2 behavior surfaces:
    - SuperAdmin assignment management with institute-compatibility enforcement,
    - Admin/Faculty report scope checks including institution mismatch denial,
    - sidebar visibility and direct-route guard consistency for non-SuperAdmin users.
- Validation evidence executed as a combined integration matrix (`AdminUserManagementIntegrationTests`, `ReportExportsIntegrationTests`, `SidebarMenuIntegrationTests`) with all tests passing.
- Behavior impact:
    - no new runtime behavior introduced in Stage 2.4 itself,
    - Phase 2 authorization outcomes are now formally validated and documented as complete,
    - subsequent parity work can proceed to Phase 3 CRUD/workflow parity scope.

## 2026-05-13 Update - Institute Parity Stage 3.1 Core Academic/Admin Modules Snapshot

- Completed first Phase 3 module parity hardening slice focused on department and course operational parity across institution types.
- Portal department management behavior was aligned for School/College/University:
    - department list now shows institution type,
    - create/edit forms now include institution-type selection,
    - edit flow preloads institution-type value for deterministic updates.
- Web API client and portal controller contract behavior was corrected to avoid silent University-default writes:
    - department create now requires explicit institution type,
    - department update now supports explicit institution-type mutation.
- Added cross-institution integration validation:
    - temporarily enables all institution policy flags,
    - verifies department/course CRUD operations across School/College/University,
    - restores original institution policy state after test execution.
- Hardened legacy admin assignment round-trip integration path to choose/create institution-compatible departments in mixed datasets.
- Behavior impact:
    - School/College/University department CRUD paths are now parity-safe from portal create/edit surfaces,
    - mixed-institution datasets no longer cause false assignment-path regressions in integration coverage,
    - no database schema or migration mutation was required for Stage 3.1.

## 2026-05-13 Update - Institute Parity Stage 3.2 Student Lifecycle Snapshot

- Completed lifecycle institute-parity hardening for student lifecycle workflows currently implemented in platform scope (graduation candidates, promote, graduate, deactivate, reactivate).
- API lifecycle access behavior now enforces:
    - Admin department-assignment scope checks,
    - institution-type claim compatibility with target department/student department,
    - SuperAdmin bypass for expected global governance behavior.
- Portal lifecycle behavior now enforces institute-aware filtering:
    - session JWT identity includes optional `institutionType`,
    - lifecycle department selector is constrained by institute type for non-SuperAdmin sessions,
    - out-of-scope department selections are blocked before lifecycle calls.
- Lifecycle UI operation consistency improved:
    - per-row graduation now posts correctly,
    - promote/graduate actions preserve selected department and semester filter context.
- Added focused integration validation proving Admin institute-mismatch deny behavior on lifecycle read/mutation paths while keeping Stage 2 report/menu/assignment parity suites green.
- Behavior impact:
    - student lifecycle operations now follow the same institute-boundary enforcement model already adopted by report authorization paths,
    - lifecycle transitions cannot be invoked by Admin users outside assigned department + institution scope,
    - no schema/model migration changes were required for Stage 3.2.

## 2026-05-13 Update - Institute Parity Stage 3.3 Student Submenu Snapshot

- Completed student submenu parity hardening on core student-data read surface used by Students/Enrollments/Payments submenu workflows.
- Student list endpoint behavior now enforces:
    - Admin assignment-constrained department scope,
    - institution-claim compatibility for explicit department filter requests,
    - institute-compatible result shaping for unfiltered student list queries.
- Preserved existing SuperAdmin full-scope behavior and faculty scoped behavior while extending institute parity consistency.
- Updated student submenu table terminology to institute-neutral `Level` wording on student/enrollment list surfaces to reduce University-only language assumptions.
- Added dedicated integration evidence covering:
    - forbidden response on institute-mismatched student list department request,
    - institute-compatible list output enforcement for admin callers.
- Behavior impact:
    - student submenu backing data now follows parity boundaries consistently with Phase 2/Stage 3.2 institute guard model,
    - mixed-assignment/mixed-institute datasets no longer risk cross-institute student list leakage in Admin submenu reads,
    - no schema or migration update required for Stage 3.3.

## 2026-05-13 Update - Institute Parity Stage 3.4 Exit Snapshot

- Completed Phase 3 exit-criteria validation and closure for module CRUD/workflow parity.
- Consolidated verified behavior from Stage 3.1-3.3:
    - core department/course parity across School/College/University,
    - lifecycle institute-scope enforcement,
    - student submenu institute-scope enforcement and institute-neutral terminology.
- Added web contract alignment fix by extending shared portal `LookupItem` with optional `InstitutionType` required by institute-aware department filtering paths.
- Validation evidence:
    - solution build pass,
    - full integration suite pass (`115/115`) with no failures.
- Behavior impact:
    - Phase 3 module parity exit criteria met for implemented scope,
    - no schema/model migration change required for Stage 3.4,
    - next execution focus moves to Phase 4 analytics/report parity and reliability stages.

## 2026-05-13 Update - Institute Parity Stage 4.1 Analytics Filter Expansion Snapshot

- Completed analytics filter expansion across API queries and portal analytics dashboard controls.
- Analytics API behavior now enforces:
    - optional institute filter support across analytics read/export surfaces,
    - role-aware institute defaults for constrained roles via JWT `institutionType` claim,
    - forbidden responses on explicit institute mismatch requests,
    - department + institute compatibility checks before analytics query execution.
- Analytics service behavior now supports institution-aware filtering for:
    - performance analytics,
    - attendance analytics,
    - assignment analytics,
    - quiz analytics,
    while preserving distributed cache behavior with scope-aware cache keys.
- Portal analytics behavior now includes:
    - institution + department filter controls,
    - claim-driven default institute filtering for constrained roles,
    - safe clearing of out-of-scope department selections.
- Validation evidence:
    - solution build pass,
    - focused parity integration suites pass (`41/41`) including new analytics institute parity tests.
- Behavior impact:
    - analytics dashboards and analytics API queries are now institute-aware for School/College/University contexts,
    - constrained-role analytics requests are auto-scoped and protected against cross-institute filter drift,
    - no schema or migration update required for Stage 4.1.

## 2026-05-13 Update - Institute Parity Stage 4.2 Reports Filter Expansion Snapshot

- Completed report filter expansion across report APIs, report exports, and report portal controls.
- Report API behavior now enforces:
    - optional `institutionType` filter support across report generation and export endpoints,
    - role-aware default institution scope for constrained roles using JWT `institutionType`,
    - forbidden responses on explicit report institution mismatch requests,
    - report request scope propagation to queued result-summary export jobs.
- Report query behavior now supports institution-aware filtering for:
    - attendance summary,
    - result summary,
    - assignment summary,
    - quiz summary,
    - GPA,
    - enrollment summary,
    - semester results,
    - low attendance warning,
    - FYP status report.
- Portal report behavior now includes:
    - institution filter controls on report forms,
    - institution-aware department dropdown narrowing,
    - report export links that preserve institution filter state.
- Validation evidence:
    - solution build pass,
    - focused parity integration suites pass (`43/43`) including expanded report institute parity tests.
- Behavior impact:
    - report dashboards and report API queries/exports are institute-aware for School/College/University contexts,
    - constrained-role report requests are auto-scoped and protected against cross-institute filter drift,
    - no schema or migration update required for Stage 4.2.

## 2026-05-13 Update - Institute Parity Stage 4.3 Broken Report Fixes Snapshot

- Completed report reliability fixes for faculty report scope guardrails.
- Report API behavior now enforces:
    - faculty department-scoped reports require explicit department (or department/offering where applicable),
    - faculty requests against unassigned departments are denied consistently,
    - faculty offering-scoped report checks validate department assignment scope instead of strict offering owner match.
- Reliability and deterministic validation additions:
    - added integration checks proving `400` responses for missing required faculty filters,
    - added integration checks proving `403` responses for faculty unassigned department filters,
    - included repaired report endpoints in focused parity regression run.
- Validation evidence:
    - solution build pass,
    - focused parity integration suites pass (`42/42`) including expanded report reliability tests.
- Behavior impact:
    - broken faculty report access behavior is corrected without changing report UI contracts,
    - report endpoints now apply role + institute + department scope consistently for repaired routes,
    - no schema or migration update required for Stage 4.3.

## 2026-05-13 Update - Institute Parity Stage 4.4 Exit Criteria Snapshot

- Completed Phase 4 exit gate after the report and analytics reliability fixes.
- Exit validation now confirms:
    - full integration-suite regression stability across report, analytics, role, and institute guard paths,
    - no remaining Phase 4 functional regressions in School/College/University parity flows,
    - no schema or migration update required for the phase-exit checkpoint.
- Validation evidence:
    - solution build pass,
    - full integration suite pass (`124/124`).
- Behavior impact:
    - Phase 4 is closed with error-free report and analytics parity behavior,
    - the execution pointer advances to Phase 5 without additional code changes.

## 2026-05-13 Update - Institute Parity Stage 5.1 Core Seed Coverage Snapshot

- Completed core seed coverage hardening for institute-aware foundational data.
- Core script behavior now enforces:
    - default institution policy flags in `portal_settings` for School/College/University enablement,
    - deterministic baseline departments mapped to all three institution types,
    - normalized legacy report keys to current canonical report-key format,
    - parity-aligned report role assignment defaults (operational reports for SuperAdmin/Admin/Faculty, transcript also for Student),
    - explicit SuperAdmin baseline sidebar role-access seed rows.
- Validation evidence:
    - user-import institution-assignment regression suite passed (`3/3`),
    - script content verification confirms policy keys, institution-typed baseline departments, and updated report key matrix are present.
- Behavior impact:
    - fresh and replayed core seed runs now produce institute-aware foundational policy and access baselines required for Phase 5 data completion,
    - no schema/migration mutation introduced in this stage.

## 2026-05-13 Update - Institute Parity Stage 5.2 Full Dummy Coverage Snapshot

- Completed full dummy parity dataset expansion for School/College/University representative execution paths.
- Full dummy script behavior now enforces:
    - explicit institution-type assignment for parity users and deterministic department institution mapping,
    - assignment-junction coverage for Admin and Faculty department scopes,
    - buildings/rooms/timetable/timetable-entry parity data across representative institute departments,
    - payment receipt and transcript export artifact coverage,
    - lifecycle/report artifacts including bulk promotions, graduation approvals, school stream assignment, and student report cards.
- Validation evidence:
    - targeted user-import institution-assignment regression suite passed (`3/3`),
    - script verification confirms new parity entity blocks and institution-type alignment rows are present.
- Behavior impact:
    - one-run full dummy seeding now provides broader parity validation data for reports, lifecycle, timetable, payments, and role/institute scope checks,
    - no schema/migration mutation introduced in this stage.

## 2026-05-13 Update - Institute Parity Stage 5.3 Data Quality and Replay Safety Snapshot

- Completed full dummy replay-safety hardening and post-deployment data-quality verification expansion.
- Script behavior now enforces:
    - deterministic replay alignment for seeded user/department identity and institution mapping fields,
    - institute-level parity aggregate checks for School/College/University datasets,
    - critical workflow coverage checks for timetable, payments, report artifacts, and lifecycle entities,
    - duplicate and dataset-version integrity checks for replay safety.
- Validation evidence:
    - targeted user-import institution-assignment regression suite passed (`3/3`),
    - script verification confirms institute-level and replay-safety check outputs are present in post-deployment script.
- Behavior impact:
    - replayed full dummy seeding now preserves deterministic parity shape more strictly,
    - post-deployment verification now provides direct parity-count and duplicate-safety signals for Stage 5 quality gates.

## 2026-05-13 Update - Institute Parity Stage 5.4 Exit Criteria Snapshot

- Completed Phase 5 exit-criteria validation for script-chain readiness.
- Exit validation behavior now confirms:
    - full script deployment order (`01` -> `02` -> `03` -> `05`) succeeds in one run,
    - dummy seed replay avoids superadmin duplicate-email collisions by reusing preexisting superadmin identity when available,
    - post-deployment checks emit non-zero institute-distributed parity coverage and zero duplicate key-identifier signals.
- Validation evidence:
    - targeted user-import institution-assignment regression suite passed (`3/3`),
    - one-run SQL execution reported successful full dummy completion and post-check parity metrics across School/College/University.
- Behavior impact:
    - Phase 5 database script parity scope is now closed with executable one-run readiness evidence,
    - execution can proceed to Phase 6 QA/UAT/regression protection expansion.

## 2026-05-13 Update - Institute Parity Stage 6.1 Automated Test Expansion Snapshot

- Completed automated parity regression expansion for institute-scope lifecycle, student submenu, and report access paths.
- Added integration coverage for matched-scope success paths that complement existing mismatch-deny checks:
    - lifecycle graduation-candidates access now has explicit Admin matched-institute allow-path evidence,
    - student submenu explicit department filter now has institute-compatible scope-shaping allow-path evidence,
    - enrollment-summary report now has Admin matched-institution query allow-path evidence.
- Validation evidence:
    - focused Stage 6.1 parity integration run passed (`28/28`) across `StudentLifecycleIntegrationTests`, `StudentSubmenuParityIntegrationTests`, and `ReportExportsIntegrationTests`.
- Behavior impact:
    - institute parity guard model now has both deny-path and allow-path automated coverage on key Phase 5-expanded surfaces,
    - no runtime feature contract changes or schema mutations introduced by Stage 6.1.

## 2026-05-13 Update - Institute Parity Stage 6.2 Cross-Role UAT Matrix Snapshot

- Completed cross-role UAT matrix automation across School/College/University institution contexts.
- Added matrix validation coverage for SuperAdmin/Admin/Faculty/Student on stable parity-sensitive endpoints:
    - report catalog role visibility,
    - account-security locked-account access boundaries,
    - attendance-by-offering authorization boundaries.
- Validation evidence:
    - focused Stage 6.2 UAT integration run passed (`100/100`) across `CrossRoleUatMatrixIntegrationTests`, `ReportCatalogIntegrationTests`, `AccountSecurityIntegrationTests`, and `AuthorizationRegressionTests`.
- Behavior impact:
    - role-boundary expectations are now explicitly regression-protected across all three institution claim contexts,
    - no runtime feature contract changes or schema mutations introduced by Stage 6.2.

## 2026-05-13 Update - Institute Parity Stage 6.3 Performance and Query Validation Snapshot

- Completed performance and query validation for parity-sensitive institute-filtered report/dashboard paths.
- Added Stage 6.3 validation coverage for:
    - parity index read-usage confirmation on institute-filtered query patterns (`academic_programs`, `courses`, `course_offerings`),
    - no-major-regression latency budgets on common Admin dashboard/report paths.
- Validation evidence:
    - focused Stage 6.3 integration run passed (`2/2`) across `PerformanceQueryValidationIntegrationTests`.
- Behavior impact:
    - index-backed institute-filtered query paths now have explicit automated validation evidence,
    - no runtime feature contract changes or schema mutations introduced by Stage 6.3.

## 2026-05-13 Update - Institute Parity Stage 6.4 Exit Criteria Snapshot

- Completed Phase 6 exit-criteria consolidation for institute parity.
- Executed consolidated parity certification run across Stage 6 suites covering:
    - lifecycle and student submenu parity success + scope checks,
    - cross-role UAT matrix and authorization/report boundary checks,
    - performance/query validation checks for parity-sensitive paths.
- Validation evidence:
    - consolidated Stage 6 phase-exit run passed (`132/132`) with zero failures.
- Behavior impact:
    - Phase 6 parity scenarios are fully regression-certified with no critical/blocker defects,
    - no runtime feature contract changes or schema mutations introduced by Stage 6.4.

## 2026-05-13 Update - Institute Parity Stage 7.1 Deployment Runbook Snapshot

- Completed deployment runbook finalization for institute parity operational readiness.
- Added explicit rollout guidance for SQL execution order (`01 -> 02 -> 03 -> 04 -> 05`) and environment prerequisites.
- Added rollback + verification checklist covering backup, fail-fast verification via post-deployment checks, cleanup fallback, and evidence archiving.
- Validation evidence:
    - required deployment scripts existence verified,
    - schema/create-context and post-check fail-fast guards verified from script content.
- Behavior impact:
    - no runtime feature contract changes or schema mutations introduced by Stage 7.1,
    - operational deployment guidance is now explicit and reproducible for Phase 7 rollout.

## 2026-05-13 Update - Institute Parity Stage 7.2 Functional Documentation Snapshot

- Completed functional documentation alignment for institute parity behavior across shared functionality and user-facing role guides.
- Added explicit role/institute scope guidance for:
    - Admin department/institute scope boundaries,
    - Faculty assignment + institute scoped access,
    - Student safe-surface access and restricted operational endpoints.
- Validation evidence:
    - Stage 7.2 documentation updates synchronized across required tracking docs and user guides,
    - command execution pointer advanced to Stage 7.3.
- Behavior impact:
    - no runtime feature contract changes or schema mutations introduced by Stage 7.2,
    - institute parity behavior is now explicitly documented for operational users.

## 2026-05-13 Update - Institute Parity Stage 7.3 Monitoring and Support Handover Snapshot

- Completed monitoring and support handover documentation for institute parity operational readiness.
- Added monitoring guidance for report/analytics failure signals, expected authorization-denial triage, and institute-filtered analytics health checks.
- Added support handover checklist for SuperAdmin parity incidents covering role, institution type, department/offering scope, menu visibility, and correlation evidence.
- Validation evidence:
    - Stage 7.3 monitoring/support guidance synchronized across required docs,
    - support handover checklist explicitly references the report/analytics and role/institute scenarios most likely to generate parity incidents.
- Behavior impact:
    - no runtime feature contract changes or schema mutations introduced by Stage 7.3,
    - operational support handling is now explicit for parity-scope defects.

## 2026-05-13 Update - Institute Parity Stage 7.4 Release Exit Criteria Snapshot

- Completed Phase 7 release exit criteria for institute parity readiness.
- Validation evidence:
    - Stage 7.4 completion evidence is recorded in the phase tracker and mandatory handoff docs,
    - command execution pointer is advanced beyond the institute parity closeout.
- Behavior impact:
    - no runtime feature contract changes or schema mutations introduced by Stage 7.4,
    - Phase 7 parity work is fully closed and ready for the next roadmap stage.

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Core System Architecture](#core-system-architecture)
3. [Complete Functionality Index](#complete-functionality-index)
4. [Authentication & Authorization](#authentication--authorization)
5. [User & Access Management](#user--access-management)
6. [Academic Management](#academic-management)
7. [Assessment & Grading](#assessment--grading)
8. [Learning Management System](#learning-management-system)
9. [Communication & Notifications](#communication--notifications)
10. [Administration & Configuration](#administration--configuration)
11. [Advanced Features](#advanced-features)
12. [Integration & Support](#integration--support)
13. [Service & Controller Matrix](#service--controller-matrix)
14. [Architecture & Design Patterns](#architecture--design-patterns)
15. [API Patterns & Data Flows](#api-patterns--data-flows)
16. [Performance Considerations](#performance-considerations)

---

## Executive Summary

**Tabsan-EduSphere** is a comprehensive, enterprise-grade educational management system built on ASP.NET Core, designed to support modern academic institutions with multi-role access control, flexible academic management, comprehensive assessment tools, and advanced features.

### Key Statistics
- **63+ API Controllers** providing 200+ endpoints
- **50+ Application Services** implementing business logic
- **50+ Service Interfaces** for dependency injection and abstraction
- **14 Application Layer Modules** organized by domain
- **45+ Major Feature Areas** across academic, assessment, communication, and administration domains
- **Supported Roles:** SuperAdmin, Admin, Faculty, Student, Parent, Staff
- **Database Entities:** 50+ domain entities
- **Multi-tenancy Support** with license-based feature control

### Core Capabilities
✅ Multi-role RBAC with department scoping  
✅ License enforcement with concurrent user limits  
✅ Comprehensive audit logging  
✅ Flexible grading strategies (GPA, Percentage)  
✅ Enrollment with timetable clash detection  
✅ Gradebook with weighted calculations  
✅ Notifications with fan-out dispatch  
✅ Feature flags with emergency rollback  
✅ Advanced reporting & analytics  
✅ AI-powered learning assistance  
✅ FYP (Final Year Project) management  
✅ Full-text search across platform  

---

## Core System Architecture

### Architectural Layers

```
┌─────────────────────────────────────────┐
│      Web Presentation Layer              │
│   (Razor Pages, API Responses)           │
├─────────────────────────────────────────┤
│   API Controller Layer (63+ Controllers)│
│   Input Validation, Auth Checks         │
├─────────────────────────────────────────┤
│  Application Service Layer (50+ Services)│
│  Business Logic, Orchestration          │
├─────────────────────────────────────────┤
│   Domain Layer (Entities & Interfaces)  │
│   Business Rules, Invariants            │
├─────────────────────────────────────────┤
│ Infrastructure Layer (Repositories)    │
│  Data Access Abstraction                │
├─────────────────────────────────────────┤
│  Database Layer (SQL Server)            │
│  50+ Domain Entities                    │
└─────────────────────────────────────────┘
```

### Technology Stack
- **Backend:** ASP.NET Core Web API
- **Frontend:** Web-based user interface
- **Database:** MSSQL Server with indexed queries
- **Authentication:** JWT Tokens with refresh rotation
- **Messaging:** In-app, Email, SMS notification system
- **AI Integration:** OpenAI ChatGPT integration
- **Design Pattern:** Repository Pattern, Service Pattern, DTO Pattern

### Core Modules

| Module | Location | Purpose |
|--------|----------|---------|
| **Academic** | `Academic/` | Programs, courses, enrollment, degrees |
| **Assignments** | `Assignments/` | Assignments, submissions, gradebook |
| **Attendance** | `Attendance/` | Attendance tracking and reporting |
| **Auth** | `Auth/` | Authentication, authorization, sessions |
| **Quizzes** | `Quizzes/` | Quiz creation, attempts, grading |
| **LMS** | `Lms/` | Learning content, modules, videos |
| **Notifications** | `Notifications/` | Event-driven notifications, inbox |
| **FYP** | `Fyp/` | Final year project management |
| **Helpdesk** | `Helpdesk/` | Support tickets and knowledge base |
| **AI Chat** | `AiChat/` | AI-powered learning assistant |
| **Search** | `Search/` | Full-text search across platform |
| **Study Planner** | `StudyPlanner/` | Personal study planning |
| **Modules** | `Modules/` | Feature/module registry, licenses |
| **Services** | `Services/` | Cross-cutting concerns |

---

## Complete Functionality Index

### Core System (5 features)
1. Authentication & Authorization
2. User & Role Management
3. Department & Institution Management
4. License & Module Management
5. Account Security & Settings

### Academic (12 features)
6. Program & Course Management
7. Enrollment Management
8. Semester & Academic Calendar
9. Student Registration & Whitelist
10. Student Profiles & Lifecycle
11. Results & Grading
12. Degree Audit & Progression
13. Parent Portal
14. Timetable Management
15. Bulk Promotion
16. Course Prerequisites
17. Student Status Tracking

### Assessment & Grading (5 features)
18. Assignments & Submissions
19. Quizzes & Assessments
20. Gradebook & Assessment Grid
21. Report Card Generation
22. Rubric-Based Evaluation

### Learning Management (5 features)
23. Learning Management System (LMS)
24. Study Planner
25. AI Chat Assistant
26. Attendance Management
27. Course Content Organization

### Communication & Notifications (4 features)
28. Notifications System
29. Announcements
30. Discussions & Forums
31. Communication Integrations

### Administration (8 features)
32. Portal Settings & Configuration
33. Theme & Personalization
34. Sidebar Menu & Navigation
35. Dashboard & Portal Composition
36. Reporting & Analytics
37. Audit & Logging
38. User Import & Bulk Operations
39. Building & Room Management

### Advanced Features (7 features)
40. Final Year Project (FYP)
41. Helpdesk & Support
42. Search & Discovery
43. Library Management
44. Labels & Tagging
45. Accreditation & Compliance
46. Feature Flags & Rollback

### Integration & Support (5 features)
47. Campus Infrastructure Integration
48. Portal Capabilities Matrix
49. Schema Streaming & Exports
50. External System Integration
51. Media & Storage Management

**Total: 51 Major Functionalities**

---

## Authentication & Authorization

### 1. User Authentication
**Module Location:** `Auth/`  
**API Controller:** `AuthController.cs`

**Features:**
- Multi-tenant login with email/password
- JWT-based access token generation (15-minute expiry)
- Refresh token rotation (7-day expiry)
- Forced password change for imported users
- Failed login lockout (5 attempts × 15 minutes)
- Multi-Factor Authentication (MFA) support
- Session risk detection and blocking
- Concurrent user limit enforcement per license
- SuperAdmin exemption from concurrency limits
- Device info tracking and IP address logging
- Risk-based session blocking

**Security Features:**
- Password history tracking (prevent reuse)
- License concurrent session enforcement
- Session risk-based blocking
- MFA code validation
- Lockout mechanism with time-based release
- Secure password hashing with salt

**Key Components:**
- `AuthService` - Authentication orchestration
- `ITokenService` - JWT token generation and validation
- `IPasswordHasher` - Secure password verification
- `IUserSessionRepository` - Session management
- DTOs: `LoginRequest`, `LoginResponse`, `RefreshRequest`, `ChangePasswordRequest`

---

### 2. Role-Based Access Control (RBAC)
**Features:**
- Multi-role support: SuperAdmin, Admin, Faculty, Student, Parent, Staff
- Department-based access control
- Role-based permission matrix
- Fine-grained permission assignment per role
- Dynamic authorization at runtime
- Access audit trail for all operations

**Role Hierarchy:**
```
SuperAdmin (System-wide)
├── System administration
├── License management
├── Feature flags
├── All data access
└── Concurrent limit exempt

Admin (Department-scoped)
├── User management
├── Course management
├── Student administration
└── Department data access

Faculty (Course-level)
├── Course teaching & management
├── Assignment/Quiz creation
├── Attendance marking
├── Grading
└── Course enrollment view

Student (Self-service)
├── Enrollment management
├── Assignment submission
├── Quiz participation
├── Result viewing
├── Attendance checking
└── Profile management

Parent (Read-only)
└── Student information viewing

Staff (Support)
├── Support ticket management
└── Limited administrative functions
```

**Key Components:**
- `AdminUserController` - User CRUD and import
- `StudentController` - Student profile management
- Role-based authorization middleware
- `IUserRepository` - User credential management

---

### 3. Department & Institution Management
**API Controllers:** `DepartmentController.cs`, `InstitutionPolicyController.cs`

**Features:**
- Department creation and hierarchical organization
- Faculty assignment to departments
- Admin assignment to departments
- Institution-wide policy configuration
- Institutional grading profiles
- Building and room management
- Department-specific settings

**Key Components:**
- `DepartmentController` - Department CRUD
- `InstitutionPolicyService` - Policy management
- `InstitutionGradingService` - Grading configuration
- `BuildingRoomService` - Facility management

---

### 4. License & Module Management
**API Controllers:** `LicenseController.cs`, `ModuleController.cs`, `ModuleRegistryController.cs`

**Features:**
- License key upload and validation
- Concurrent user limit management
- License expiry tracking and enforcement
- Module activation/deactivation per institution
- Feature flag control for rollout and emergency rollback
- Module entitlement resolution
- Portal capability matrix enforcement
- Usage tracking and reporting

**Key Components:**
- `ILicenseRepository` - License persistence and validation
- `ModuleRegistryService` - Module availability tracking
- `ModuleService` - Module CRUD operations
- `IModuleEntitlementResolver` - Permission matrix enforcement
- `IPortalCapabilityMatrixService` - Feature availability
- Feature Flag Control Plane - SuperAdmin kill switches

---

## User & Access Management

### 5. User Profiles & Account Management
**API Controllers:** `AdminUserController.cs`, `StudentController.cs`

**Features:**
- User account creation (individual and bulk)
- Profile management (personal information, contact details)
- Department assignment
- Role management
- Status management (Active/Inactive)
- Last login tracking
- Account security settings
- Password management and reset

**Key Components:**
- `AdminUserController` - User CRUD and operations
- `StudentController` - Student profile management
- `IUserRepository` - User data persistence
- DTOs: `CreateUserRequest`, `UpdateUserRequest`, `UserResponse`

---

### 6. Batch User Import
**API Controller:** `UserImportController.cs`, `RegistrationImportController.cs`

**Features:**
- CSV user import with validation
- Auto-generated hashed passwords
- Bulk student registration import
- CSV whitelist import
- Data validation before import
- Error reporting per row with detailed feedback
- Import history tracking
- Rollback capability
- Duplicate detection and handling

**Key Components:**
- `UserImportService` - User bulk import orchestration
- `CsvRegistrationImportService` - Registration import
- `IUserImportService` - Import interface
- DTOs: `BulkUserImportRequest`, `ImportResult`

---

### 7. Account Security & Settings
**API Controller:** `AccountSecurityController.cs`

**Features:**
- Password strength enforcement
- Password history (prevent reuse)
- Password change log
- Two-factor authentication setup
- Security questions
- Account recovery options
- Login activity viewing
- Active sessions management
- Device management
- Permission audit

**Key Components:**
- `AccountSecurityService` - Security management
- `IPasswordHasher` - Password verification
- `IAccountSecurityService` - Security interface

---

## Academic Management

### 8. Program & Course Management
**Module Location:** `Academic/`  
**API Controllers:** `ProgramController.cs`, `CourseController.cs`

**Features:**
- Program (degree) creation and configuration
- Course catalog management with credit hours
- Course categorization by department
- Semester-based course mapping
- Auto-generation of semester rows from course configuration
- Course offering creation with max enrollment
- Faculty assignment to course offerings
- Course prerequisite management
- Grading type specification (GPA, Percentage)
- Course sections/divisions
- Course code standardization

**Business Rules:**
- Each course maps to specific program
- Credit hours enforce academic standards
- Prerequisites prevent invalid enrollments
- Max enrollment enforced at offering level
- Faculty must be assigned to teach

**Key Components:**
- `CourseService` - Course CRUD and orchestration
- `ICourseRepository` - Course persistence
- `ISemesterRepository` - Semester lifecycle
- `IPrerequisiteRepository` - Prerequisite tracking
- DTOs: `CreateCourseRequest`, `CreateOfferingRequest`, `AssignFacultyRequest`

---

### 9. Enrollment Management
**API Controller:** `EnrollmentController.cs`

**Features:**
- Student self-enrollment in course offerings
- Admin enrollment of any student
- Seat availability enforcement
- Duplicate enrollment prevention
- Timetable clash detection and admin override
- Drop/withdrawal with audit trail
- Full enrollment history preservation
- Status tracking (Active, Dropped, Completed)
- Retroactive enrollment adjustments
- Enrollment verification and locking
- Waitlist management

**Business Rules Enforced:**
- Course offering must be open
- Semester must not be closed
- Available seats must exist
- Student cannot enroll twice in same offering
- Admin can override timetable clashes with reason
- Enrollment audit trail maintains full history

**Key Components:**
- `EnrollmentService` - Enrollment orchestration
- `IEnrollmentRepository` - Enrollment data
- `ITimetableRepository` - Schedule conflict detection
- DTOs: `EnrollRequest`, `AdminEnrollRequest`, `EnrollmentResponse`

---

### 10. Semester & Academic Calendar
**API Controllers:** `SemesterController.cs`, `CalendarController.cs`

**Features:**
- Semester creation and lifecycle management
- Semester open/closed status enforcement
- Named academic deadlines (assignment deadlines, exam dates)
- Automated reminder notifications based on deadlines
- Calendar-based scheduling
- Deadline visibility by role
- Semester-based course organization
- Academic calendar management

**Key Components:**
- `AcademicCalendarService` - Deadline management and reminders
- `IAcademicDeadlineRepository` - Deadline persistence
- `INotificationService` - Reminder dispatch
- DTOs: `CreateDeadlineRequest`, `UpdateDeadlineRequest`, `SemesterResponse`

---

### 11. Student Registration & Whitelist
**API Controller:** `RegistrationImportController.cs`

**Features:**
- Student self-registration via registration number
- Email-based student registration
- Whitelist management (approval before registration)
- Bulk whitelist import (CSV)
- Single entry whitelist add/update/delete
- Registration number/email dual-mode support
- Automated welcome emails
- Registration status tracking

**Key Components:**
- `StudentRegistrationService` - Registration orchestration
- `IStudentRegistrationService` - Registration business logic
- Student whitelist repositories
- DTOs: `StudentSelfRegisterRequest`, `WhitelistEntryRequest`

---

### 12. Student Profiles & Lifecycle
**Module Location:** `Academic/`  
**API Controllers:** `StudentController.cs`, `StudentLifecycleController.cs`

**Features:**
- Student profile creation (linked to user)
- Registration number tracking
- Program and department assignment
- Admission date recording
- Current semester tracking
- Academic history across all semesters
- Graduation management
- Student status changes (active, suspended, graduated)
- Student change requests (program change, leave of absence)
- Student modification workflow approvals
- Payment receipt management
- Promotion to next semester (bulk)
- Student lifecycle workflows

**Key Components:**
- `StudentRegistrationService` - Profile creation
- `StudentLifecycleService` - Lifecycle orchestration
- `BulkPromotionService` - Semester promotion
- `IStudentLifecycleRepository` - Student state
- DTOs: `CreateStudentProfileRequest`, `GraduationSummaryDto`, `StudentStatusChangeRequest`

---

### 13. Results & Grading
**Module Location:** `Academic/`  
**API Controllers:** `ResultController.cs`, `ResultCalculationController.cs`

**Features:**
- Component-based result entry (Midterm, Final, Project)
- GPA calculation (4.0 scale)
- CGPA (Cumulative GPA) tracking across semesters
- Result publishing with student notifications
- Transcript generation
- Multiple grading strategies (GPA, Percentage)
- Rubric-based evaluation
- Result correction tracking
- Max marks enforcement
- Weighted result calculation
- Grade locks to prevent accidental changes

**Grading Strategies:**
- **GPA Strategy:** 4.0 scale with letter grades (A, B, C, D, F)
- **Percentage Strategy:** 0-100 percentage marks
- Configurable per institution
- Custom grading rules per course

**Key Components:**
- `IResultService` - Result CRUD operations
- `IResultCalculationService` - GPA/CGPA computation
- `ICourseGradingService` - Course-level grading config
- `IInstitutionGradingService` - Institution grading profiles
- `IResultStrategyResolver` - Strategy selection
- DTOs: `CreateResultRequest`, `ResultResponse`, `RubricDto`

---

### 14. Degree Audit & Progression
**API Controllers:** `DegreeAuditController.cs`, `ProgressionController.cs`

**Features:**
- Degree audit trail showing course completion status
- GPA/CGPA tracking
- Remaining course requirements
- Academic standing assessment
- Student progression status (on-track, at-risk, behind)
- Academic alerts and warnings
- Course requirement fulfillment tracking
- Degree progress visualization
- Early warning system

**Key Components:**
- `DegreeAuditService` - Audit generation
- `ProgressionService` - Progression analysis
- `IProgressionRepository` - Progression data
- DTOs: `DegreeAuditResponse`, `ProgressionStatusResponse`

---

### 15. Parent Portal
**API Controller:** `ParentPortalController.cs`

**Features:**
- Parent/Guardian access to student information
- Course enrollment viewing
- Attendance summary
- Result/grade viewing
- Notifications and alerts
- Role-based access control
- Read-only access model

**Key Components:**
- `ParentPortalService` - Parent access logic
- `IParentPortalService` - Portal interface

---

### 16. Timetable Management
**API Controller:** `TimetableController.cs`

**Features:**
- Timetable creation per department/semester
- Timetable entry management (course, day, time, room)
- Publishing/unpublishing timetables
- Timetable clash detection
- PDF export (department-specific)
- Excel export (department-specific)
- Timetable download for all department users
- Auto-title generation from program code + semester
- Scheduling conflict prevention
- Room and instructor assignment

**Key Components:**
- `TimetableService` - Timetable orchestration
- `ITimetableRepository` - Timetable data
- `ITimetableExcelExporter` - Excel export
- `ITimetablePdfExporter` - PDF export
- DTOs: `TimetableEntryRequest`, `TimetableExportRequest`

---

## Assessment & Grading

### 17. Assignments & Submissions
**Module Location:** `Assignments/`  
**API Controller:** `AssignmentController.cs`

**Features:**
- Faculty assignment creation (unpublished draft)
- Assignment editing (only while unpublished)
- Assignment publishing (makes visible to students)
- Assignment retraction (unpublishes if no submissions)
- Assignment deletion (soft-delete with submission protection)
- Due date enforcement
- Max marks specification
- Student assignment submission
- File and text-based submissions
- Duplicate submission prevention
- Faculty grading with marks and feedback
- Submission rejection and re-submission
- Submission status tracking (Pending, Graded, Rejected)
- Assignment listing with submission counts
- Multiple attempt support

**Business Rules:**
- Only unpublished assignments can be edited
- Submissions require published assignment
- Submissions must be within due date
- Max marks awarded cannot exceed assignment max marks
- Duplicate submissions rejected
- Assignments with submissions cannot be retracted

**Key Components:**
- `AssignmentService` - Lifecycle orchestration
- `IAssignmentRepository` - Assignment persistence
- DTOs: `CreateAssignmentRequest`, `SubmitAssignmentRequest`, `GradeSubmissionRequest`

---

### 18. Quizzes & Assessments
**Module Location:** `Quizzes/`  
**API Controller:** `QuizController.cs`

**Features:**
- Quiz creation with configuration
- Timed quiz support
- Attempt limits per student
- Auto-grading for objective questions
- Manual grading for subjective questions
- Question pool management
- Question shuffling
- Student quiz attempts
- Attempt history
- Score and feedback
- Quiz publishing/unpublishing
- Result analytics
- Question randomization

**Key Components:**
- `QuizService` - Quiz management
- `IQuizService` - Quiz interface
- DTOs: `CreateQuizRequest`, `QuizAttemptRequest`, `QuizResultDto`

---

### 19. Gradebook & Assessment Grid
**API Controller:** `GradebookController.cs`

**Features:**
- Gradebook grid view by course offering
- Inline cell entry (marks for specific component)
- CSV import for bulk grade entry
- CSV export for external processing
- Weighted total calculation
- Component-based grading (Midterm, Final, Project)
- Publication status tracking per cell
- Student list with registration numbers
- Component column headers with weightage
- Publish all grades for offering
- Unpublish grades
- Grade visibility control

**Grid Calculation:**
- Weighted Total = Sum(Component Weight × (Marks/Max)) for complete records

**Key Components:**
- `GradebookService` - Grid orchestration
- `IGradebookRepository` - Gradebook data
- DTOs: `GradebookGridResponse`, `UpsertGradebookEntryRequest`, `GradebookExportRequest`

---

### 20. Report Card Generation
**API Controller:** `ReportCardController.cs`

**Features:**
- Semester report card generation
- Course listing with grades and credits
- GPA calculation display
- Semester performance summary
- PDF export capability
- Transcript generation (multi-semester)
- Official record format
- Print-ready layout

**Key Components:**
- `ReportCardService` - Report generation
- `IReportCardService` - Report interface
- DTOs: `ReportCardRequest`, `ReportCardResponse`

---

### 21. Rubric-Based Evaluation
**API Controller:** `RubricController.cs`

**Features:**
- Rubric creation with criteria
- Rubric scoring scales
- Assignment rubric linking
- Rubric application in grading
- Rubric template library
- Rubric sharing between faculty
- Detailed feedback framework

**Key Components:**
- `IRubricService` - Rubric management
- DTOs: `RubricDto`, `RubricCriterionDto`, `RubricScaleDto`

---

## Learning Management System

### 22. Learning Management System (LMS)
**Module Location:** `Lms/`  
**API Controller:** `LmsController.cs`

**Features:**
- Course content module organization (by week)
- Module publishing/unpublishing
- Course materials storage
- Content video embedding
- Video storage URL and embed URL support
- Video duration tracking
- Module-level content management
- Video deletion (soft-delete)
- Ordered module display
- Student access control to published content
- Course announcements
- Discussion forums
- Content versioning

**Key Components:**
- `LmsService` - Content management
- `ILmsRepository` - LMS data persistence
- DTOs: `CourseContentModuleDto`, `ContentVideoDto`, `ModuleCreateRequest`

---

### 23. Study Planner
**API Controller:** `StudyPlanController.cs`

**Features:**
- Personal study plan creation
- Task/milestone management
- Progress tracking
- Deadline management
- Study goal setting
- Calendar integration
- Study schedule visualization
- Task organization by subject/course
- Progress reports

**Key Components:**
- `StudyPlanService` - Study planning
- `IStudyPlanService` - Study planner interface
- DTOs: `StudyPlanDto`, `TaskDto`, `MilestoneDto`

---

### 24. AI Chat Assistant
**Module Location:** `AiChat/`  
**API Controller:** `AiChatController.cs`

**Features:**
- Role-aware chatbot responses
- Department context awareness
- Query topics: Assignments, Results, Attendance, FYP schedules, General help
- Natural language understanding
- Answer generation based on student context
- Theme inheritance from active user theme
- Conversation history (optional)
- LLM backend integration (configurable)
- Multi-turn conversations
- Context-aware responses

**Supported Topics:**
- Assignment and submission guidance
- Result inquiries and GPA calculations
- Attendance policies and records
- FYP schedule and requirements
- General academic guidance
- Course information

**Key Components:**
- `AiChatService` - Chat orchestration
- `IAiChatService` - Chat interface
- `ILlmClient` - LLM backend abstraction
- DTOs: `ChatMessageRequest`, `ChatMessageResponse`

---

### 25. Attendance Management
**Module Location:** `Attendance/`  
**API Controller:** `AttendanceController.cs`

**Features:**
- Single-student attendance marking
- Bulk class attendance marking
- Attendance correction (with audit trail)
- Date normalization (UTC)
- Attendance status: Present, Absent, Late, Excused
- Duplicate prevention per (student, offering, date)
- Remarks/notes field
- Attendance querying by offering
- Student attendance history
- Low attendance threshold checks
- Automatic notifications for low attendance
- Attendance reports and analytics
- Attendance trends analysis

**Business Rules:**
- One record per (student, offering, date)
- Duplicates silently skipped on bulk insert
- Corrections recorded with correcting user ID

**Key Components:**
- `AttendanceService` - Attendance orchestration
- `IAttendanceRepository` - Attendance data
- DTOs: `MarkAttendanceRequest`, `BulkMarkAttendanceRequest`, `AttendanceReportRequest`

---

## Communication & Notifications

### 26. Notifications System
**Module Location:** `Notifications/`  
**API Controller:** `NotificationController.cs`

**Features:**
- Dashboard notification display
- Notification types: Assignment, Quiz, Result, System, Announcement
- Fan-out dispatch to multiple recipients
- Notification read/unread tracking
- Inbox management with pagination
- Unread count badge
- Notification deactivation
- System-generated notifications (background jobs)
- Notification retention
- Role-specific notification delivery
- Notification preferences
- Email notifications
- SMS notifications (optional)
- In-app notifications

**Key Components:**
- `NotificationService` - Notification orchestration
- `INotificationRepository` - Notification persistence
- `INotificationFanoutQueue` - Asynchronous dispatch
- DTOs: `SendNotificationRequest`, `NotificationResponse`, `InboxResponse`

---

### 27. Announcements
**API Controller:** `AnnouncementController.cs`

**Features:**
- Faculty/Admin announcement creation
- Announcement scheduling
- Department-specific announcements
- Role-based visibility
- Announcement archival
- PIN/highlight important announcements
- Announcement expiration
- Rich text formatting
- Attachment support

**Key Components:**
- `IAnnouncementService` - Announcement interface
- DTOs: `CreateAnnouncementRequest`, `AnnouncementResponse`

---

### 28. Discussions & Forums
**API Controller:** `DiscussionController.cs`

**Features:**
- Course-level discussions
- Thread management
- Reply threading
- Moderation capabilities
- User mention support
- Rich text content
- Discussion archival
- Participant list
- Discussion search
- Notification on replies

**Key Components:**
- `IDiscussionService` - Discussion interface
- DTOs: `CreateDiscussionRequest`, `ThreadReplyRequest`, `DiscussionResponse`

---

### 29. Communication Integrations
**API Controller:** `CommunicationIntegrationsController.cs`

**Features:**
- Email integration
- SMS notification support (configurable)
- Chat integration hooks
- External system webhooks
- Integration audit logging
- SMTP configuration
- Email template management
- SMS gateway integration

**Key Components:**
- `CommunicationIntegrationService` - Integration management
- `IEmailSender` - Email abstraction
- `IOutboundIntegrationGateway` - External system communication

---

## Administration & Configuration

### 30. Portal Settings & Configuration
**API Controller:** `PortalSettingsController.cs`

**Features:**
- Institution name and branding
- Logo/favicon management
- System-wide email configurations
- SMS provider configuration
- Default theme selection
- LMS toggle
- Finance module configuration
- Payment gateway integration settings
- Session timeout configuration
- Feature flags management
- Super Admin controls
- System-wide parameters

**Key Components:**
- `SettingsService` - Settings management
- `ISettingsServices` - Settings interface
- DTOs: `PortalSettingsRequest`, `PortalSettingsResponse`

---

### 31. Theme & Personalization
**API Controller:** `ThemeController.cs`

**Features:**
- 15+ pre-built themes
- Light mode support
- Dark mode support
- High-contrast accessibility themes
- Per-user theme selection
- Persistent theme preferences
- Admin system default theme
- Theme customization (font, colors)
- AI chatbot theme inheritance
- Custom color schemes

**Key Components:**
- `ThemeService` - Theme management
- DTOs: `ThemeResponse`, `UserThemePreferenceRequest`

---

### 32. Sidebar Menu & Navigation
**API Controller:** `SidebarMenuController.cs`

**Features:**
- Role-based menu visibility
- Menu item ordering
- Sub-menu management
- Dynamic menu based on active modules
- Menu customization per role
- Menu state persistence
- Sidebar collapse/expand state
- Module-aware menu items

**Key Components:**
- `SidebarMenuController` - Menu management
- DTOs: `MenuResponse`, `MenuItemDto`

---

### 33. Dashboard & Portal Composition
**API Controller:** `DashboardCompositionController.cs`

**Features:**
- Customizable dashboard layout
- Widget support (cards, charts, lists)
- Role-specific dashboard defaults
- Student dashboard with courses, assignments, grades
- Faculty dashboard with class lists, grading, attendance
- Admin dashboard with analytics, reports
- Drag-and-drop composition (optional)
- Save/load dashboard configurations
- Dashboard state persistence

**Key Components:**
- `DashboardCompositionService` - Dashboard management
- `IDashboardCompositionService` - Dashboard interface

---

### 34. Reporting & Analytics
**API Controllers:** `ReportController.cs`, `AnalyticsController.cs`

**Features:**
- Student performance reports
- Department analytics
- Assignment/Quiz statistics
- Attendance reports
- Result distribution analysis
- Course popularity metrics
- Faculty performance metrics
- Export to PDF, Excel, CSV
- Scheduled report generation
- Report caching
- Custom report builder
- Data visualization
- Trend analysis

**Report Types:**
- Academic Performance Report
- Attendance Report
- Assessment Statistics
- Enrollment Report
- Financial Report (if Finance module active)
- Department Report
- Institution Report
- Course Analytics
- Student Analytics

**Key Components:**
- `IReportService` - Report generation
- `IAnalyticsService` - Analytics interface
- DTOs: `ReportRequest`, `ReportResponse`

---

### 35. Audit & Logging
**Module Location:** (Infrastructure)

**Features:**
- All create/update/delete operations logged
- IP address tracking
- User activity logging
- Failed authentication attempts
- Module access logging
- Data change tracking
- Audit trail export
- Log retention policies
- Compliance reporting
- Audit report generation

**Key Components:**
- `IAuditService` - Audit logging
- `AuditLog` - Domain entity
- DTOs: `AuditLogResponse`

---

### 36. User Import & Bulk Operations
**API Controller:** `UserImportController.cs`

**Features:**
- CSV user import with validation
- Bulk student registration import
- Auto-generated hashed passwords
- CSV whitelist import
- Data validation before import
- Error reporting per row
- Import history tracking
- Rollback capability
- Duplicate detection

**Key Components:**
- `UserImportService` - User bulk import
- `CsvRegistrationImportService` - Registration import
- `IUserImportService` - Import interface

---

### 37. Building & Room Management
**API Controller:** `BuildingController.cs`, `RoomController.cs`

**Features:**
- Building directory
- Room management
- Room capacity tracking
- Facility booking (for FYP, meetings)
- Room availability checking
- Room assignment for classes
- Facility reservation

**Key Components:**
- `BuildingRoomService` - Facility management
- `IBuildingRoomService` - Building interface

---

## Advanced Features

### 38. Final Year Project (FYP) Management
**Module Location:** `Fyp/`  
**API Controller:** `FypController.cs`

**Features:**
- Student FYP project allocation
- FYP proposal management
- Meeting scheduling
- Meeting location entry (Department, Room number)
- Panel member selection and assignment
- Supervisor assignment
- Student and panel notifications
- FYP status tracking (Proposal, Development, Review, Final, Completed)
- FYP progress reporting
- Defense scheduling
- Evaluation form management
- Grade assignment

**Key Components:**
- `FypService` - FYP orchestration
- `IFypService` - FYP interface
- DTOs: `FypAllocationDto`, `MeetingScheduleDto`, `ProposalSubmissionDto`

---

### 39. Helpdesk & Support
**Module Location:** `Helpdesk/`  
**API Controller:** `HelpdeskController.cs`

**Features:**
- Support ticket creation
- Ticket categorization
- Priority levels (Low, Medium, High, Urgent)
- Status tracking (Open, In Progress, Resolved, Closed)
- Ticket assignment to support staff
- Response management
- Attachment support
- Ticket history
- SLA tracking (optional)
- FAQ management
- Knowledge base
- Ticket search and filtering

**Key Components:**
- `HelpdeskService` - Ticket orchestration
- `IHelpdeskService` - Helpdesk interface
- DTOs: `CreateTicketRequest`, `TicketResponse`

---

### 40. Search & Discovery
**Module Location:** `Search/`  
**API Controller:** `SearchController.cs`

**Features:**
- Global full-text search
- Cross-entity search (courses, students, faculty, etc.)
- Search result ranking
- Faceted search by entity type
- Advanced search filters
- Search history (optional)
- Trending searches
- Search analytics
- Autocomplete suggestions

**Key Components:**
- `SearchService` - Search orchestration
- `ISearchService` - Search interface
- DTOs: `SearchRequest`, `SearchResponse`

---

### 41. Library Management
**API Controller:** `LibraryController.cs`

**Features:**
- Book catalog management
- Book checkout/return
- Availability tracking
- Due date management
- Late fee calculation
- Reservation system
- Student library account
- Book recommendations
- Digital resource access
- Book search

**Key Components:**
- `LibraryService` - Library management
- `ILibraryService` - Library interface

---

### 42. Labels & Tagging
**API Controller:** `LabelController.cs`

**Features:**
- Custom label creation
- Entity tagging (courses, students, etc.)
- Color-coded labels
- Bulk label operations
- Label search and filtering
- Label management

**Key Components:**
- `LabelService` - Label management
- `ILabelService` - Label interface

---

### 43. Accreditation & Compliance
**API Controller:** `AccreditationController.cs`

**Features:**
- Accreditation requirement tracking
- Compliance monitoring
- Report generation
- Audit trail
- Quality metrics
- Standards mapping
- Compliance documentation

**Key Components:**
- `AccreditationService` - Accreditation management
- `IAccreditationService` - Accreditation interface

---

### 44. Feature Flags & Rollback
**Module Location:** `Modules/`  
**API Controller:** `FeatureFlagsController.cs`

**Features:**
- Feature flag creation and management
- Rollout control (percentage-based, role-based)
- Emergency rollback capability
- A/B testing support
- Feature monitoring
- Flag audit trail
- SuperAdmin-only control
- Feature adoption tracking

**Key Components:**
- Feature Flag Control Plane (Infrastructure)
- `FeatureFlagsController` - Flag management
- DTOs: `FeatureFlagRequest`, `FeatureFlagResponse`

---

### 45. Labels & Classification
**Features:**
- Custom label/tag creation
- Entity classification
- Bulk operations
- Search by labels
- Color coding
- Hierarchical labeling

---

## Integration & Support

### 46. Campus Infrastructure Integration
**Module Location:** `Services/`  
**API Controller:** `BuildingController.cs`, `RoomController.cs`

**Features:**
- Student Information System (SIS) integration
- HR system integration
- Financial system integration
- Building Management integration
- Transportation system integration
- Hostel Management integration
- Data synchronization

**Key Components:**
- `BuildingRoomService` - Facility management
- `IBuildingRoomService` - Building interface

---

### 47. Portal Capabilities Matrix
**Module Location:** `Services/`  
**API Controller:** `PortalCapabilitiesController.cs`

**Features:**
- Capability discovery by client
- License-based capability filtering
- Module entitlement resolution
- API endpoint availability matrix
- Feature access control
- Capability export

**Key Components:**
- `IPortalCapabilityMatrixService` - Capability matrix
- `IModuleEntitlementResolver` - Entitlement logic

---

### 48. External System Integration
**Module Location:** (Infrastructure)

**Features:**
- REST API for external systems
- Webhook support for event notification
- OAuth2 integration (planned)
- SSO integration
- LMS platform integration (Canvas, Blackboard)
- Data exchange protocols

**Key Components:**
- `IOutboundIntegrationGateway` - External communication
- `ICommunicationIntegrationContracts` - Integration contracts

---

### 49. Media & Storage Management
**Module Location:** (Infrastructure)

**Features:**
- File upload/download management
- Document storage
- Video content storage
- File size limits
- Malware scanning (if configured)
- CDN integration support
- File expiry management
- Storage optimization

**Key Components:**
- `IMediaStorageService` - Storage abstraction

---

### 50. Schema Streaming & Exports
**Module Location:** (Implicit)

**Features:**
- Streaming data exports
- Large dataset handling
- Memory-efficient processing
- Export format options

---

---

## Service & Controller Matrix

### Academic Module Services

| Service | Location | Interface | Controller | Key Methods |
|---------|----------|-----------|------------|------------|
| `AcademicCalendarService` | Academic/ | `IAcademicCalendarService` | CalendarController | Deadlines, reminders |
| `CourseService` | Academic/ | `ICourseService` | CourseController | CRUD, auto-semester generation |
| `EnrollmentService` | Academic/ | `IEnrollmentService` | EnrollmentController | Enroll, drop, list |
| `BulkPromotionService` | Academic/ | `IBulkPromotionService` | BulkPromotionController | Promote students |
| `DegreeAuditService` | Academic/ | `IDegreeAuditService` | DegreeAuditController | Audit trail, requirements |
| `GraduationService` | Academic/ | `IGraduationService` | GraduationController | Graduate, list candidates |
| `InstitutionGradingService` | Academic/ | `IInstitutionGradingService` | InstitutionGradingProfileController | Profiles, rules |
| `ReportCardService` | Academic/ | `IReportCardService` | ReportCardController | Transcript, report card |
| `SchoolStreamService` | Academic/ | `ISchoolStreamService` | SchoolStreamController | Stream management |
| `StudentRegistrationService` | Academic/ | `IStudentRegistrationService` | RegistrationImportController | Register, whitelist, import |
| `ParentPortalService` | Academic/ | `IParentPortalService` | ParentPortalController | Parent info access |
| `ProgressionService` | Academic/ | `IProgressionService` | ProgressionController | Progression status |
| `CourseGradingService` | Academic/ | `ICourseGradingService` | GradingConfigController | Grading config |

### Assessment Services

| Service | Location | Interface | Controller | Key Methods |
|---------|----------|-----------|------------|------------|
| `AssignmentService` | Assignments/ | `IAssignmentService` | AssignmentController | Create, publish, submit, grade |
| `GradebookService` | Assignments/ | `IGradebookService` | GradebookController | Grid, entry, export |
| `QuizService` | Quizzes/ | `IQuizService` | QuizController | Create, attempt, grade |
| `RubricService` | Services/ | `IRubricService` | RubricController | Create, edit, apply |
| `ResultService` | Services/ | `IResultService` | ResultController | Enter, correct, publish |
| `ResultCalculationService` | Services/ | `IResultCalculationService` | ResultCalculationController | Calculate GPA, publish |

### Communication Services

| Service | Location | Interface | Controller | Key Methods |
|---------|----------|-----------|------------|------------|
| `NotificationService` | Notifications/ | `INotificationService` | NotificationController | Send, inbox, mark read |
| `AnnouncementService` | Services/ | `IAnnouncementService` | AnnouncementController | Create, list |
| `DiscussionService` | Services/ | `IDiscussionService` | DiscussionController | Create, reply, moderate |

### Learning Management Services

| Service | Location | Interface | Controller | Key Methods |
|---------|----------|-----------|------------|------------|
| `LmsService` | Lms/ | `ILmsService` | LmsController | Modules, videos CRUD |
| `TimetableService` | Services/ | `ITimetableService` | TimetableController | CRUD, export |
| `AttendanceService` | Attendance/ | `IAttendanceService` | AttendanceController | Mark, correct, query |
| `StudyPlanService` | StudyPlanner/ | `IStudyPlanService` | StudyPlanController | Create, update |

### Complete API Controller List (63 Controllers)

| # | Controller | Module | Primary Methods |
|---|-----------|--------|-----------------|
| 1 | AccountSecurityController | Services | Security, 2FA, password |
| 2 | AccreditationController | Services | Accreditation tracking |
| 3 | AdminChangeRequestController | Services | Admin workflows |
| 4 | AdminUserController | Auth | User CRUD, import |
| 5 | AiChatController | AiChat | Chat messages |
| 6 | AnalyticsController | Reports | Analytics queries |
| 7 | AnnouncementController | Services | Announcement CRUD |
| 8 | AssignmentController | Assignments | Assignment lifecycle |
| 9 | AttendanceController | Attendance | Mark, correct, query |
| 10 | AuthController | Auth | Login, refresh, logout |
| 11 | BuildingController | Services | Building CRUD |
| 12 | BulkPromotionController | Academic | Student promotion |
| 13 | CalendarController | Academic | Deadlines & calendar |
| 14 | CommunicationIntegrationsController | Services | Integration setup |
| 15 | CourseController | Academic | Course management |
| 16 | DashboardCompositionController | Services | Dashboard customization |
| 17 | DegreeAuditController | Academic | Degree audit |
| 18 | DepartmentController | Academic | Departments |
| 19 | DiscussionController | Services | Discussions |
| 20 | EnrollmentController | Academic | Enrollment |
| 21 | FeatureFlagsController | Modules | Feature flags |
| 22 | FypController | Fyp | FYP management |
| 23 | GradebookController | Assignments | Gradebook grid |
| 24 | GradingConfigController | Academic | Grading config |
| 25 | GraduationController | Academic | Graduation |
| 26 | HelpdeskController | Helpdesk | Support tickets |
| 27 | InstitutionGradingProfileController | Academic | Grading profiles |
| 28 | InstitutionPolicyController | Services | Policies |
| 29 | LabelController | Services | Tags/labels |
| 30 | LibraryController | Services | Library |
| 31 | LicenseController | Modules | License management |
| 32 | LmsController | Lms | Course content |
| 33 | ModuleController | Modules | Module management |
| 34 | ModuleRegistryController | Modules | Module registry |
| 35 | NotificationController | Notifications | Notifications |
| 36 | ParentPortalController | Academic | Parent access |
| 37 | PaymentReceiptController | Services | Payment receipts |
| 38 | PortalCapabilitiesController | Services | Capability matrix |
| 39 | PortalSettingsController | Services | Portal settings |
| 40 | PrerequisiteController | Academic | Prerequisites |
| 41 | ProgramController | Academic | Programs/degrees |
| 42 | ProgressionController | Academic | Student progression |
| 43 | QuizController | Quizzes | Quizzes |
| 44 | RegistrationImportController | Academic | Registration import |
| 45 | ReportCardController | Academic | Report cards |
| 46 | ReportController | Reports | Reports |
| 47 | ReportSettingsController | Reports | Report config |
| 48 | ResultCalculationController | Academic | Result calculation |
| 49 | ResultController | Academic | Results |
| 50 | RoomController | Services | Rooms |
| 51 | RubricController | Assignments | Rubrics |
| 52 | SchoolStreamController | Academic | Academic streams |
| 53 | SearchController | Search | Global search |
| 54 | SemesterController | Academic | Semesters |
| 55 | SidebarMenuController | Services | Menu config |
| 56 | StudentController | Academic | Student profiles |
| 57 | StudentLifecycleController | Services | Graduation, status |
| 58 | StudyPlanController | StudyPlanner | Study plans |
| 59 | TeacherModificationController | Academic | Faculty changes |
| 60 | TenantOperationsController | Modules | Multi-tenancy |
| 61 | ThemeController | Services | Theme selection |
| 62 | TimetableController | Services | Timetables |
| 63 | UserImportController | Services | User import |

---

## Architecture & Design Patterns

### Layered Architecture

```
┌────────────────────────────────────────────────┐
│         API Controller Layer (63+)             │
│    Input validation, Authentication checks    │
├────────────────────────────────────────────────┤
│    Application Service Layer (50+)            │
│    Business logic, orchestration, workflows   │
├────────────────────────────────────────────────┤
│      Domain Layer (Entities & Rules)          │
│    Business rule enforcement, validation      │
├────────────────────────────────────────────────┤
│  Repository & Infrastructure Layer            │
│    Data access abstraction, persistence       │
├────────────────────────────────────────────────┤
│         Database Layer (SQL Server)           │
│         50+ domain entities                   │
└────────────────────────────────────────────────┘
```

### Design Patterns Used

#### 1. Repository Pattern
- Abstraction over data persistence
- 40+ repository interfaces
- LINQ-based queries with Entity Framework Core
- Generic repository base class

```csharp
public interface IStudentRepository
{
    Task<Student> GetByIdAsync(int id);
    Task<IEnumerable<Student>> GetByDepartmentAsync(int deptId);
    Task AddAsync(Student student);
    Task UpdateAsync(Student student);
}
```

#### 2. Service Pattern
- Business logic encapsulation
- 50+ application services
- Dependency injection via constructor
- Service orchestration for complex operations

```csharp
public interface IEnrollmentService
{
    Task<EnrollmentResponse> EnrollAsync(int studentId, EnrollRequest request);
    Task DropAsync(int enrollmentId);
    Task<IEnumerable<EnrollmentResponse>> GetForStudentAsync(int studentId);
}
```

#### 3. DTO Pattern
- Request/Response DTOs for API contracts
- Type-safe data transfer
- Input validation via FluentValidation
- Mapping from/to domain entities

```csharp
public class EnrollRequest
{
    public int CourseOfferingId { get; set; }
    [Required]
    public bool OverrideTimetableClash { get; set; }
}
```

#### 4. Dependency Injection
- Constructor-based DI
- Service registration in startup
- Interface-based abstractions
- Loose coupling between components

#### 5. Specification Pattern (Optional)
- Complex query specifications
- Reusable query logic
- Linq-based predicates
- Domain query logic encapsulation

### Module Organization

```
Application Layer (14 Modules):
├── Academic/           → Program, course, enrollment, results
├── Assignments/        → Assignments, submissions, gradebook
├── Attendance/         → Attendance tracking
├── Auth/              → Authentication, authorization
├── Quizzes/           → Quiz management
├── Lms/               → Learning content
├── Notifications/     → Notification dispatch
├── Fyp/               → Final year projects
├── Helpdesk/          → Support tickets
├── AiChat/            → AI assistant
├── Search/            → Full-text search
├── StudyPlanner/      → Study planning
├── Modules/           → Feature registry
└── Services/          → Cross-cutting services

Each Module Contains:
├── Services/          → Business logic
├── Interfaces/        → Service contracts
└── DTOs/              → Request/Response models
```

---

## API Patterns & Data Flows

### Standard CRUD Pattern
```
GET    /api/v1/{resource}              List all
GET    /api/v1/{resource}/{id}         Get single
POST   /api/v1/{resource}              Create
PUT    /api/v1/{resource}/{id}         Update
DELETE /api/v1/{resource}/{id}         Delete
```

### Domain-Specific Patterns
```
POST   /api/v1/enrollments/enroll              Enroll student
POST   /api/v1/enrollments/{id}/drop           Drop enrollment
POST   /api/v1/assignments/{id}/publish        Publish assignment
POST   /api/v1/attendance/bulk-mark            Bulk mark attendance
GET    /api/v1/gradebook/{offeringId}         Get gradebook grid
POST   /api/v1/results/publish-all             Publish all results
GET    /api/v1/timetable/export/{id}/pdf      Export timetable
```

### Request → Response Flow
```
Client Request
    ↓
API Controller (validation, auth check)
    ↓
Middleware (logging, error handling)
    ↓
Application Service (business logic)
    ↓
Domain Model (invariant enforcement)
    ↓
Repository Interface (abstraction)
    ↓
Infrastructure (EF Core, SQL Server)
    ↓
Database Query Execution
    ↓
Response DTO → JSON → Client
```

### Notification Fan-Out Pattern
```
SendAsync() → Create Notification
    ↓
FanOutRecipientsAsync() → Create NotificationRecipient rows
    ↓
Queue for async dispatch (if > 250 recipients)
    ↓
User inbox fetch with Read tracking
    ↓
Notification history maintenance
```

### Error Handling Pattern
```
Controller try-catch
    ↓
Service exception handling
    ↓
Custom exception mapping
    ↓
HTTP status code response
    ↓
Error detail JSON response
```

---

## Performance Considerations

### Optimization Strategies

#### Database Level
- Indexed searches on commonly queried fields
- Composite indexes on (StudentId, CourseOfferingId, Date)
- Foreign key indexes
- Archive policies for old data
- Query optimization

#### Application Level
- Pagination for large datasets
- Lazy loading of navigation properties
- Batch operations for bulk imports
- Async operations throughout
- Caching strategies

#### Caching Strategy
- Role caching (30-minute TTL)
- Menu/sidebar caching (1-hour TTL)
- Settings caching (1-hour TTL)
- Result caching (varies by operation)
- Semester/program caching (daily TTL)

#### Query Optimization
```csharp
// Inefficient: N+1 queries
var students = _repository.GetAll();  // Query 1
foreach(var student in students)      // Query N
    var enrollments = student.Enrollments;

// Optimized: Single query with Include
var students = _repository.GetAll()
    .Include(s => s.Enrollments)
    .ToList();  // Query 1
```

### Common Performance Hotspots

1. **Gradebook Grid Loading** - Use pagination/lazy loading
2. **Full-Text Search** - Implement indexed search
3. **Attendance Bulk Mark** - Use batch insert operations
4. **Report Generation** - Run asynchronously with queuing
5. **Notification Dispatch** - Use fan-out queue for large audiences

---

## Key Validations & Business Rules

### Enrollment
```
✓ Semester NOT closed
✓ Offering is OPEN
✓ Student NOT already enrolled
✓ Seat available
✓ Timetable clash check (admin override allowed)
✓ Prerequisites satisfied
```

### Assignment Lifecycle
```
✓ Can edit only if UNPUBLISHED
✓ Can submit only if PUBLISHED
✓ Can submit only within DUE DATE
✓ Cannot submit if already submitted
✓ Cannot retract if submissions exist
✓ Max marks awarded ≤ Assignment.MaxMarks
```

### Attendance
```
✓ One record per (Student, Offering, Date)
✓ Duplicates silently skipped on bulk insert
✓ Corrections tracked with correcting user ID
✓ Date must be valid for offering
```

### Authentication
```
✓ 5 failed attempts → 15 min lockout
✓ MFA check if enabled
✓ Session risk assessment
✓ License concurrent limit enforcement
✓ SuperAdmin exempt from concurrency
```

### Grade Entry
```
✓ Component marks ≤ Component max marks
✓ Weighted total calculated correctly
✓ Publication status tracked
✓ Grade visible to student after publication
✓ Grade change requires re-entry
```

---

## Security Features Matrix

| Feature | Implementation | Details |
|---------|---------------|---------| 
| **Authentication** | JWT Tokens | 15-min access, 7-day refresh |
| **Authorization** | RBAC | Role-based, department-scoped |
| **Password Security** | Hashing/Salting | Bcrypt or PBKDF2 |
| **Password History** | Enforcement | Prevent reuse of N recent passwords |
| **Sessions** | Risk Detection | IP tracking, device info |
| **Concurrent Limits** | Enforcement | Per-license user limits |
| **Audit Trail** | Comprehensive | All CRUD operations logged |
| **Encryption** | Data at Rest | Database encryption (optional) |
| **SSL/TLS** | Transmission | HTTPS only |
| **Rate Limiting** | API Protection | Per-endpoint limits |

---

## Database Entity Groups

### Identity & Security
- User, Role, UserSession, PasswordHistory, AuditLog

### Academic Structure
- Program, Course, CourseOffering, Semester, Enrollment, Student

### Assessment
- Assignment, AssignmentSubmission, Quiz, QuizAttempt, Result, Rubric

### Attendance
- AttendanceRecord, AttendanceStatus

### Notifications
- Notification, NotificationRecipient, NotificationType

### Configuration
- PortalSettings, InstitutionPolicy, ThemeProfile, ModuleRegistry

### Student Lifecycle
- StudentProfile, StudentStatus, ChangeRequest, GraduationRecord

### FYP & Projects
- FypAllocation, FypProposal, FypMeeting, FypPanel

### Support & Library
- HelpDeskTicket, LibraryBook, LibraryCheckout, Reservation

---

## Integration Points

### Email
- User welcome emails
- Password reset links
- Announcements
- Notifications
- Reports
- Grade notifications

### SMS (Optional)
- Low attendance alerts
- Result announcements
- Event reminders
- Critical notifications

### LLM (AI Chat)
- OpenAI, Anthropic, or local LLM
- Role-aware prompt engineering
- Department context injection
- Query classification

### Payment Gateway (Optional)
- Online fee payment
- Receipt generation
- Payment verification
- Refund processing

### Export Formats
- **Excel** - Gradebook, timetable, reports, attendance
- **PDF** - Transcripts, timetables, assignments, reports
- **CSV** - Bulk import, export data feeds

---

## Common Task Flows

### Student Enrollment Flow
```
1. View available courses
2. Check prerequisites
3. Check timetable for clashes
4. Enroll (with admin override capability)
5. Confirmation notification
6. Access to course materials
7. Enrollment audit log created
```

### Assignment Submission Flow
```
1. Faculty publishes assignment
2. Student views assignment
3. Student submits work (file or text)
4. Duplicate submission check
5. Faculty grades submission
6. Student receives feedback
7. Grade published to gradebook
8. Automatic notification to student
```

### Grade Entry Flow
```
1. Faculty views gradebook grid
2. Inline edit marks for each component
3. Or bulk import via CSV
4. System calculates weighted totals
5. Faculty reviews calculations
6. Publish all grades
7. Automatic student notification
8. Audit log entry created
```

### Attendance Flow
```
1. Faculty marks attendance (single or bulk)
2. System checks for duplicates
3. Attendance recorded with timestamp
4. Automated low attendance check
5. Notifications sent to at-risk students
6. Attendance history maintained
7. Reports generated automatically
```

### FYP Workflow
```
1. Student submits FYP proposal
2. Supervisor assignment
3. Panel member selection
4. Meeting scheduling
5. Progress tracking
6. Final submission
7. Defense evaluation
8. Graduation recommendation
```

---

## Statistics & Metrics

### Codebase Metrics
| Metric | Count |
|--------|-------|
| API Controllers | 63+ |
| Application Services | 50+ |
| Service Interfaces | 50+ |
| Application Modules | 14 |
| API Endpoints | 200+ |
| Domain Entities | 50+ |
| Database Tables | 50+ |
| DTOs | 100+ |
| Repository Interfaces | 40+ |
| Major Features | 51 |
| Supported Roles | 6 |

### System Capability Matrix
| Capability | Status | Module |
|-----------|--------|--------|
| Multi-Role RBAC | ✅ Implemented | Auth |
| License Management | ✅ Implemented | Modules |
| Enrollment Management | ✅ Implemented | Academic |
| Gradebook with Weighted Calculations | ✅ Implemented | Assignments |
| Timetable Clash Detection | ✅ Implemented | Academic |
| AI Chat Assistant | ✅ Implemented | AiChat |
| Full-Text Search | ✅ Implemented | Search |
| FYP Management | ✅ Implemented | Fyp |
| Feature Flags & Rollback | ✅ Implemented | Modules |
| Audit & Compliance | ✅ Implemented | Infrastructure |
| Advanced Reporting | ✅ Implemented | Reports |
| Multi-Format Export | ✅ Implemented | Reports |

---

## Related Documentation

For more detailed information, see:
- [Function-List.md](Function-List.md) - Detailed function reference
- [Advance-Enhancements.md](Advance-Enhancements.md) - Planned enhancements
- [Phase31-Stage31.2-Security-Hardening.md](Phase31-Stage31.2-Security-Hardening.md) - Security details
- [PRD.md](../Project%20startup%20Docs/PRD.md) - Product requirements

---

## Revision History

| Version | Date | Status | Notes |
|---------|------|--------|-------|
| 1.0 | May 11, 2026 | Initial | Comprehensive overview created |
| 2.0 | May 11, 2026 | Merged | Combined 4 reference documents |

---

**Last Updated:** May 11, 2026  
**Status:** Phase 33 - Security Hardening Active  
**Next Phase:** Phase 34 - Performance Optimization & Scaling

