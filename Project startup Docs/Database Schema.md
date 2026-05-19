# Database Schema Documentation
## University Portal & License Creation Tool

**Version:** 1.2  
**Aligned With PRD:** v1.33  
**Purpose:** Define database schemas for the University Portal Application and the License Creation Tool  

## Institute Parity Stage Documentation Rule (2026-05-13)

After every completed stage in `Docs/Institute-Parity-Issue-Fix-Phases.md`:
- Add `Implementation Summary` and `Validation Summary` in the stage record.
- Update this schema document with explicit statement of schema impact:
	- `No schema mutation` or
	- `Schema updated` with table/column/index/migration details.

## 2026-05-19 Update - Plan A Phase 5 Implementation (Tenant/Campus UI Management Interfaces)

- Recent request issue:
	- proceed to Plan A Phase 5 and add tenant/campus management screens linked to existing menu patterns.
- Implementation Summary:
	- added SuperAdmin API endpoints for tenant/campus lifecycle operations,
	- added web UI and API client wiring for tenant/campus management.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- focused unit tests passed (`9/9`),
	- focused integration tests passed (`52/52`).
- Testing and result summary:
	- total focused tests passed: `61/61`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Plan A Phase 4 Implementation (Access Control and Filtering)

- Recent request issue:
	- proceed to Plan A Phase 4 and enforce tenant/campus read scoping with SuperAdmin bypass.
- Implementation Summary:
	- added request claims-based access-scope resolver,
	- added tenant/campus JWT claims used by repository filtering,
	- added tenant/campus scoped read filters in user/department repositories.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- focused unit tests passed (`9/9`),
	- focused integration tests passed (`52/52`).
- Testing and result summary:
	- total focused tests passed: `61/61`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Plan A Phase 3 Implementation (Compatibility and Safety Hardening)

- Recent request issue:
	- proceed to Plan A Phase 3 and ensure tenant/campus integration cannot enter invalid state.
- Implementation Summary:
	- added migration `20260519034517_Phase43_TenantCampusCompatibilitySafety`,
	- migration adds tenant/campus pair check constraints on `users` and `departments`,
	- migration adds composite tenant-bound campus FK integrity for `users` and `departments`.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- focused unit tests passed (`9/9`),
	- focused integration tests passed (`52/52`).
- Testing and result summary:
	- total focused tests passed: `61/61`.
- Schema impact: `Schema updated`.
- EF migration impact:
	- added migration `20260519034517_Phase43_TenantCampusCompatibilitySafety`.
	- adds constraints/indexes/FK integrity hardening (no breaking table replacement).

## 2026-05-19 Update - Plan A Phase 2 Implementation (Default Tenant/Campus Data Integration)

- Recent request issue:
	- proceed to Plan A Phase 2 and ensure existing records are assigned to default tenant/campus safely.
- Implementation Summary:
	- added migration `20260519032844_Phase42_DefaultTenantCampusBackfill` to perform safe data backfill,
	- migration inserts default tenant/campus baseline records if missing,
	- migration updates null tenant/campus on `users` and `departments`.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- focused unit tests passed (`9/9`),
	- focused integration tests passed (`52/52`).
- Testing and result summary:
	- total focused tests passed: `61/61`.
- Schema impact: `Schema updated`.
- EF migration impact:
	- added migration `20260519032844_Phase42_DefaultTenantCampusBackfill`.
	- data backfill only (no new table/column/index structures in this phase migration).

## 2026-05-19 Update - Plan A Phase 1 Implementation (Tenant + Campus Domain Foundation)

- Recent request issue:
	- proceed with Plan A Phase 1 implementation and synchronize mandatory tracker documents.
- Implementation Summary:
	- added tenancy foundation entities and mappings,
	- applied migration `Phase41_TenantCampusFoundation` for additive schema expansion.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- focused unit tests passed (`9/9`) confirming no baseline regression in sampled security/enrollment paths.
- Testing and result summary:
	- build passed,
	- focused unit test filter passed (`9/9`).
- Schema impact: `Schema updated`.
- EF migration impact:
	- added migration `20260519031636_Phase41_TenantCampusFoundation`.
	- added table `tenants`.
	- added table `campuses` with FK to `tenants`.
	- added nullable `TenantId` and `CampusId` to `users`.
	- added nullable `TenantId` and `CampusId` to `departments`.
	- added supporting indexes/FKs for tenant/campus lookup and integrity.

## 2026-05-19 Update - Plan A Phase 1 Kickoff (App Configuration: Tenant + Campus)

- Recent request issue:
	- start Plan A Phase 1 and synchronize mandatory governance/planning docs with phase-end implementation and validation summaries.
- Implementation Summary:
	- initiated Plan A Phase 1 (Domain Layer Extension) execution baseline,
	- updated phase document formatting so completion evidence is recorded at phase end,
	- synchronized schema tracker with required planning and product documentation updates.
- Validation Summary:
	- cross-document consistency review completed,
	- verified this kickoff wave is documentation-only and introduces no persistence or migration change.
- Testing and result summary:
	- documentation consistency checks completed,
	- no runtime/schema test execution required for this documentation-only kickoff update.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - Deep System Audit + Validation + AI Chatbot Modernization (Phase 1-7)

- Recent request issue:
	- execute phase-by-phase full-system audit/validation and chatbot modernization with governance synchronization after each phase.

### Phase 1 - System Understanding
- Implementation Summary:
	- completed architecture and data-flow review for academic, enrollment, notifications, reports, auth/MFA, and AI chat modules.
- Validation Summary:
	- controller/service/repository and DTO binding checks completed.
- Testing and result summary:
	- phase 1 traceability audit completed.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Phase 2 - API and Backend Validation
- Implementation Summary:
	- added AI chat API versioned route compatibility and aligned client route usage,
	- updated middleware enforcement mapping for versioned AI route.
- Validation Summary:
	- build + targeted tests passed after backend updates.
- Testing and result summary:
	- validated by `dotnet build` plus focused unit/integration suites (`13/13` passed).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Phase 3 - Database and Data Flow Validation
- Implementation Summary:
	- reviewed enrollment/waitlist EF data access and includes in affected flows,
	- confirmed no additional index/table/constraint changes are required.
- Validation Summary:
	- no migration delta introduced by this request.
- Testing and result summary:
	- phase 3 data-integrity check completed without new schema risk.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Phase 4 - UI and Frontend Validation
- Implementation Summary:
	- validated frontend bindings and role-aware navigation behavior for chat-enabled shell.
- Validation Summary:
	- preserved API/data contracts in all touched UI paths.
- Testing and result summary:
	- phase 4 UI binding validation completed.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Phase 5 - Performance and Stability Check
- Implementation Summary:
	- completed async/stability sanity review for touched request paths.
- Validation Summary:
	- focused regression tests and build stayed green.
- Testing and result summary:
	- phase 5 stability check completed with no schema-related impact.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Phase 6 - AI Chatbot Redesign (Critical)
- Implementation Summary:
	- modularized chatbot UI into shared components (floating button + chat panel) while preserving existing backend integration.
- Validation Summary:
	- build/tests passed after UI componentization.
- Testing and result summary:
	- phase 6 chatbot modernization validated with unchanged persistence behavior.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

### Phase 7 - Final Consolidated Reporting
- Implementation Summary:
	- synchronized mandatory governance docs with per-phase implementation and validation/test summaries.
- Validation Summary:
	- all required trackers updated for this execution cycle.
- Testing and result summary:
	- phase 7 reporting completed.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-19 Update - UI/UX Redesign Phase 9 (Final UI Polish)

- Recent request issue:
	- proceed with the final redesign phase and keep docs plus repository synchronization mandatory.
- Implementation Summary:
	- refined shared presentation styles for final polish,
	- preserved all existing data contracts and backend behaviors.
- Validation Summary:
	- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
	- workspace diagnostics reported no errors in touched frontend CSS.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - UI/UX Redesign Phase 8 (Responsive Hardening)

- Recent request issue:
	- proceed with responsive-design phase and keep docs plus repository synchronization mandatory.
- Implementation Summary:
	- updated shared CSS and targeted view hooks for responsive behavior improvements,
	- preserved all existing data contracts, actions, and route behavior.
- Validation Summary:
	- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
	- workspace diagnostics reported no errors in touched responsive frontend files.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - UI/UX Redesign Phase 7 (AI Chatbot UI)

- Recent request issue:
	- proceed with AI chatbot phase and keep docs plus repository synchronization mandatory.
- Implementation Summary:
	- updated shared layout, CSS, and frontend JS for chatbot presentation and interaction polish,
	- preserved existing backend endpoint usage and request/response contracts.
- Validation Summary:
	- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
	- workspace diagnostics reported no errors in touched chatbot frontend files.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - UI/UX Redesign Phase 6 (Branding Pass)

- Recent request issue:
	- proceed with branding phase and keep docs plus repository synchronization mandatory.
- Implementation Summary:
	- updated shared layout and CSS for branding-focused shell/header improvements,
	- improved profile dropdown and notification icon UI treatment without changing data contracts.
- Validation Summary:
	- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
	- workspace diagnostics reported no errors in touched frontend files.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - UI/UX Redesign Continuation (Enrollments/Results/Payments + Phase-Level Summary Formatting)

- Recent request issue:
	- continuation requested the next portal-page visual pass and required implementation/validation summaries at each completed redesign phase.
- Implementation Summary:
	- updated `Enrollments`, `Results`, and `Payments` Razor views for visual continuity with the global design system,
	- reformatted `Docs/Improved UI and look.md` so phase completion summaries are phase-local and markdown-lint clean.
- Validation Summary:
	- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
	- workspace diagnostics reported no errors in touched continuation views and redesign document.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - UI/UX Redesign Continuation (Students/Courses/Admin Users Polish)

- Recent request issue:
	- continuation requested further frontend page-level visual consistency after the initial redesign.
- Implementation Summary:
	- updated Students, Courses, and Admin Users Razor views and supporting CSS helper classes,
	- retained all existing backend interactions and form endpoints.
- Validation Summary:
	- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed,
	- update scope remains presentation-only.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - UI/UX Redesign Request (Execution Snapshot)

- Recent request issue:
	- the portal required a complete frontend-only UI/UX redesign so the application looks like a premium SaaS product without changing backend behavior or persistence structures.
- Implementation Summary:
	- updated the web layout, dashboard Razor view, site stylesheet, and frontend-only JavaScript to deliver the redesign,
	- preserved all controller/API/model/database behavior and did not introduce schema-affecting code changes.
- Validation Summary:
	- `dotnet build src/Tabsan.EduSphere.Web/Tabsan.EduSphere.Web.csproj -v minimal` passed after the redesign,
	- workspace diagnostics reported no errors in the touched frontend files,
	- verified the request is presentation-only.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - Documentation Synchronization Request (Execution Snapshot)

- Recent request issue:
	- a follow-up request required synchronized update coverage across PRD, Function List, Complete Functionality Reference, Development Plan, and Database Schema.
- Implementation Summary:
	- added aligned follow-up execution snapshot entries in all five requested documents,
	- normalized issue/implementation/validation wording for consistent governance evidence.
- Validation Summary:
	- verified all five requested documents now contain this follow-up dated entry,
	- verified no table/column/index/constraint/view/stored-procedure/migration changes were introduced by this update.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - Stage 40.1 PhoneNumber/SMS Recipient Dependency Completion (Execution Snapshot)

- Recent request issue:
	- notification SMS dispatch depended on persisted recipient phone numbers, but user profiles did not yet store phone values.
- Implementation Summary:
	- added optional `PhoneNumber` to user domain/EF mapping,
	- wired phone population paths through admin create/update, CSV import optional column handling, and student self-registration,
	- implemented active-user phone lookup in notification repository for SMS recipient resolution.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- `dotnet test Tabsan.EduSphere.sln -v minimal --filter "FullyQualifiedName~Phase28Stage2Tests|FullyQualifiedName~UserImportAndForceChangeIntegrationTests|FullyQualifiedName~StudentLifecycleIntegrationTests|FullyQualifiedName~StudentRegistration"` passed (`13/13`).
- Schema impact: `Schema updated`.
- EF migration impact:
	- added migration `20260518104000_Phase40_AddUserPhoneNumber`.
	- added `users.PhoneNumber` (`nvarchar(32)`, nullable).

- Recent request issue:
	- mandatory planning/execution documents required synchronized closeout updates for the latest request cycle.
- Implementation Summary:
	- updated schema tracker entry in parallel with PRD, consolidated issues, function list, full functionality reference, and development plan,
	- aligned this entry to the same issue/implementation/validation structure used across the six requested documents.
- Validation Summary:
	- verified all six requested docs now include matching 2026-05-18 synchronization entries,
	- verified this request introduced no table/column/index/migration changes.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - StudentLifecycle Notification Completion (Execution Snapshot)

- Recent request issue:
	- StudentLifecycle workflow notification backlog markers remained in service methods for state-transition and review outcomes.
- Implementation Summary:
	- implemented notification dispatch for graduation/promotion/status-change and request-review lifecycle actions.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter FullyQualifiedName~StudentLifecycleIntegrationTests -v minimal` passed (`7/7`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - DeepScan Phase 40 Closure and Production Readiness Revalidation (Execution Snapshot)

- Recent request issue:
	- post-remediation DeepScan closure required final evidence rerun and severity reclassification before production-readiness signoff.
- Implementation Summary:
	- executed post-remediation validation suites for the previously open DeepScan gaps,
	- updated `Docs/DeepScan.md` with task-by-task closure output and final readiness classification.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- targeted unit/integration revalidation suites passed for waitlist, MFA hardening, and strict import rollback behavior,
	- final DeepScan severity classification confirmed no unresolved critical/high functional gap.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - DeepScan Stage 39.4 EF Relationship and Query-Filter Warning Cleanup (Execution Snapshot)

- Recent request issue:
	- EF startup/runtime emitted warning set for required-relationship and query-filter mismatches, quiz shadow FK mapping conflict, and course enum default sentinel behavior.
- Implementation Summary:
	- updated EF configurations to align dependent filters with filtered required principals,
	- fixed quiz question relationship mapping to remove shadow foreign-key path,
	- removed course enum DB default configuration that caused sentinel warning behavior.
- Validation Summary:
	- `dotnet build Tabsan.EduSphere.sln -v minimal` passed,
	- `dotnet test tests/Tabsan.EduSphere.IntegrationTests/Tabsan.EduSphere.IntegrationTests.csproj --filter FullyQualifiedName~UserImportAndForceChangeIntegrationTests -v minimal` passed (`4/4`),
	- verified targeted EF warning set no longer appears in focused startup/runtime validation output.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - DeepScan Gap Phase/Stage Synchronization Request (Execution Snapshot)

- Recent request issue:
	- DeepScan-identified missing/partial items needed explicit phase/stage planning entries in governance docs, with synchronized schema-impact reporting.
- Implementation Summary:
	- updated schema tracker in sync with PRD, consolidated execution tracker, function list, complete functionality reference, and development plan,
	- recorded that DeepScan remediation planning was added as phase/stage entries in the consolidated tracker.
- Validation Summary:
	- verified all six mandatory documents include this dated synchronization snapshot,
	- verified no table/column/index/constraint/view/stored-procedure changes were introduced by this request.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - DeepScan Stage 39.2 Transactional CSV Import Strict Mode (Execution Snapshot)

- Recent request issue:
	- the user import flow needed an atomic strict mode to avoid partial persistence when a CSV contains mixed-validity rows.
- Implementation Summary:
	- updated the application/service/controller import path to support optional strict-mode rollback behavior,
	- added a strict-mode indicator to the import response payload.
- Validation Summary:
	- targeted integration suite passed for user import and force-change-password flows (`4/4`),
	- verified strict mode produces no persisted rows when mixed-validity input is supplied,
	- verified permissive import remains unchanged.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - DeepScan Stage 39.1 Enrollment Waitlist and Seat-Promotion Workflow (Execution Snapshot)

- Recent request issue:
	- waitlist handling for full course offerings needed to be added to the enrollment workflow.
- Implementation Summary:
	- updated the enrollment aggregate, repository contract, and service flow to support waitlisted records and deterministic promotion,
	- added repository retrieval for ordered waitlisted enrollments.
- Validation Summary:
	- targeted unit suite passed for waitlist creation and promotion (`2/2`),
	- verified no tables, columns, indexes, constraints, or migrations were required for this behavior update.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-18 Update - DeepScan Stage 39.3 MFA Hardening (TOTP + Recovery Codes) (Execution Snapshot)

- Recent request issue:
	- MFA enforcement required replacement of deployment demo-code checks with per-user TOTP and recovery-code persistence.
- Implementation Summary:
	- updated auth flow and API to support per-user TOTP enrollment and recovery-code lifecycle,
	- added migration-backed user MFA persistence fields.
- Validation Summary:
	- targeted auth unit suite passed (`7/7`),
	- targeted login/force-change integration suite passed (`4/4`).
- Schema impact: `Schema updated`.
- EF migration impact:
	- added migration `20260518091500_Phase39_MfaTotpRecoveryCodes`.
	- added `users.MfaIsEnabled` (`bit`, default `0`).
	- added `users.MfaTotpSecret` (`nvarchar(128)`, nullable).
	- added `users.MfaRecoveryCodesHashJson` (`nvarchar(4000)`, nullable).

## 2026-05-13 Update - Institute Parity Stage 0.1 (Execution Snapshot)

- Stage 0.1 completed as a schema/dependency audit baseline.
- Reviewed parity-related schema touchpoints through `ApplicationDbContext` and module repositories, including:
	- policy/config tables (`portal_settings`),
	- academic entities (departments/courses/offerings/enrollments),
	- assessments/results/quizzes,
	- lifecycle and payment entities,
	- timetable/building/room entities.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-14 Update - Phase 23 Stage 23.2 (Execution Snapshot)

- Stage 23.2 completed dynamic academic labels and context validation for policy-aware School/College/University terminology.
- API/application/web updates validated:
	- `ILabelService` and `LabelService` vocabulary resolution behavior,
	- authenticated `GET /api/v1/labels` endpoint in `LabelController`,
	- portal vocabulary consumption in web client and module composition surface.
- Validation evidence:
	- integration suite `DynamicLabelIntegrationTests` passed (`8/8`),
	- label-service unit tests remained passing.
- Documentation synchronization: Stage 23.2 schema-impact outcome is reflected in planning/command/functionality trackers.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 0.2 (Execution Snapshot)

- Stage 0.2 completed as a role/institute access matrix baseline on top of existing schema.
- Schema touchpoints reviewed for scope enforcement evidence:
	- policy flags in `portal_settings`,
	- role/user/assignment relations,
	- department and course-offering ownership relationships used by report/analytics guards.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 0.3 (Execution Snapshot)

- Stage 0.3 completed as report-failure inventory and root-cause classification.
- Schema/dependency touchpoints reviewed for report behavior:
	- report definition and role assignment entities,
	- course/department/offering joins used by report repository queries,
	- student-profile dependent transcript/report paths.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 0.4 (Execution Snapshot)

- Stage 0.4 completed as Phase 0 baseline exit sign-off.
- Schema perspective confirmed:
	- no additional unresolved schema blockers identified in Phase 0 inventories,
	- pending schema-affecting parity work remains planned in subsequent phases.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 1.1 (Execution Snapshot)

- Stage 1.1 implemented institute model normalization for department-level canonical institute scope.
- Schema update applied through EF migration:
	- `20260513121000_Phase1Stage11DepartmentInstitutionType`
	- Adds non-null `InstitutionType` (`int`) to `departments` with default `0` (University).
	- Adds index `IX_departments_institution_type` for institute-scoped lookup/query performance.
- Schema impact: `Schema updated`.
- EF migration impact: applied in source and ready for deployment migration pipeline.

## 2026-05-13 Update - Institute Parity Stage 1.2 (Execution Snapshot)

- Stage 1.2 implemented referential-integrity support and institute/report query indexing hardening.
- Schema update applied through EF migration:
	- `20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes`
	- Replaces program unique index with department-scoped unique index:
		- drop `IX_academic_programs_code`
		- add `IX_academic_programs_code_dept` (`Code`, `DepartmentId`, unique)
	- Adds operational indexes:
		- `IX_academic_programs_dept_active`
		- `IX_courses_dept_active`
		- `IX_course_offerings_semester_open`
		- `IX_course_offerings_faculty_open`
		- `IX_student_profiles_dept_status`
		- `IX_student_profiles_program_status`
		- `IX_enrollments_offering_status`
		- `IX_enrollments_student_status`
		- `IX_faculty_dept_assignments_active_lookup`
		- `IX_admin_dept_assignments_active_lookup`
	- Alters `enrollments.Status` to `nvarchar(32)` to support indexed status filters.
- Schema impact: `Schema updated`.
- EF migration impact: applied in source and validated via integration-test migration path.

## 2026-05-13 Update - Institute Parity Stage 1.3 (Execution Snapshot)

- Stage 1.3 completed schema-script hardening for parity-safe deployment replay.
- Script updates applied:
	- `Scripts/01-Schema-Current.sql`
		- appended idempotent Stage 1.1 + Stage 1.2 migration-equivalent DDL sections,
		- added migration-history inserts for:
			- `20260513121000_Phase1Stage11DepartmentInstitutionType`
			- `20260513124500_Phase1Stage12ReferentialIntegrityAndIndexes`.
	- `Scripts/04-Maintenance-Indexes-And-Views.sql`
		- added guarded parity index ensure/replacement logic.
	- `Scripts/05-PostDeployment-Checks.sql`
		- added explicit parity migration/column/index verification checks.
- Schema impact: `No additional model mutation beyond Stage 1.1/1.2`; this stage hardens script execution consistency.
- EF migration impact: none (script-hardening stage only).

## 2026-05-13 Update - Institute Parity Stage 1.4 (Execution Snapshot)

- Stage 1.4 completed Phase 1 exit-criteria verification enablement.
- Script updates applied:
	- `Scripts/05-PostDeployment-Checks.sql`
		- added department institute-type validity and coverage checks,
		- added orphan-count checks for institute-linked entities and assignment mappings.
- Schema impact: `No schema mutation`; verification coverage expanded for post-deployment integrity checks.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 2.1 (Execution Snapshot)

- Stage 2.1 completed SuperAdmin global capability hardening for assignment management.
- API updates applied:
	- `DepartmentController`:
		- added faculty department assignment endpoints (assign/remove/list/list users),
		- added institution-type compatibility checks for admin/faculty assignment operations.
	- `AcademicDtos`:
		- added `RemoveFacultyFromDepartmentRequest` contract.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 2.2 (Execution Snapshot)

- Stage 2.2 completed role-scoped institute enforcement hardening for report handlers.
- API/security updates applied:
	- `TokenService` now emits `institutionType` claim for explicitly assigned users.
	- `ReportController` now enforces institution compatibility for Admin/Faculty report requests alongside existing role scope checks.
	- Integration harness updated to support institution claim validation scenarios.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 2.3 (Execution Snapshot)

- Stage 2.3 completed menu/action guard consistency hardening.
- API/web updates applied:
	- `PortalController` now enforces sidebar visibility guard checks on mapped portal actions.
	- Direct access to hidden menu sections now redirects to allowed portal surfaces for constrained roles.
	- Added integration verification for hidden-menu endpoint denial and SuperAdmin-visible endpoint access.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 2.4 (Execution Snapshot)

- Stage 2.4 completed Phase 2 exit-criteria validation for role + institute authorization matrix behavior.
- Validation coverage executed against existing Stage 2 code paths:
	- SuperAdmin/Admin assignment authorization,
	- report institute-scope enforcement,
	- sidebar/menu action guard consistency.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 3.1 (Execution Snapshot)

- Stage 3.1 completed first Phase 3 module CRUD/workflow parity implementation slice.
- API/web contract updates applied:
	- portal department create/edit now submits explicit institution type,
	- web API client no longer silently forces University mode for department writes,
	- integration coverage validates School/College/University department+course CRUD execution paths under enabled policy.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 3.2 (Execution Snapshot)

- Stage 3.2 completed lifecycle institute-parity enforcement for student lifecycle endpoints and portal lifecycle filters.
- API/web contract updates applied:
	- lifecycle endpoints now enforce admin assignment + institution scope compatibility,
	- web session identity now projects JWT `institutionType` for institute-aware lifecycle filtering,
	- lifecycle portal actions now preserve selected department/semester route state.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 3.3 (Execution Snapshot)

- Stage 3.3 completed student submenu parity hardening for student-list data scope and institute-neutral submenu terminology.
- API/web contract updates applied:
	- student-list endpoint now enforces admin assignment + institution-claim scope compatibility,
	- student/enrollment submenu list labels use institute-neutral `Level` wording.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 3.4 (Execution Snapshot)

- Stage 3.4 completed Phase 3 exit-criteria validation for module CRUD/workflow parity.
- API/web contract updates applied:
	- shared portal lookup contract now includes optional `InstitutionType` metadata for institute-aware lifecycle/student filtering compile consistency.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 4.1 (Execution Snapshot)

- Stage 4.1 completed analytics filter expansion for institute-aware analytics queries and dashboard filters.
- API/web contract updates applied:
	- analytics API and web client now accept optional `institutionType` filter for analytics reads/exports,
	- portal analytics now carries institute + department filter state with role-aware defaults.
- Query/runtime update applied:
	- analytics query layer now filters by department and institution-type scope with scope-aware cache keys.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 4.2 (Execution Snapshot)

- Stage 4.2 completed reports filter expansion for institute-aware report reads/exports and report-center filters.
- API/web contract updates applied:
	- report API and web client now accept optional `institutionType` filter on report generation/export paths,
	- report portal page models/views now carry selected institution filter state.
- Query/runtime update applied:
	- report repository queries now apply institution-type scope filtering across report datasets.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 4.3 (Execution Snapshot)

- Stage 4.3 completed broken report reliability fixes for faculty report scope behavior.
- API/web contract updates applied:
	- report controller faculty scope checks now enforce department assignment boundaries for repaired report endpoints,
	- no DTO or schema contract expansion required.
- Query/runtime update applied:
	- report endpoint runtime guardrails now require explicit faculty filters where needed and deny unassigned department queries.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 4.4 (Execution Snapshot)

- Stage 4.4 completed Phase 4 exit-criteria validation.
- API/web contract updates applied:
	- no schema-facing contract changes required.
- Query/runtime update applied:
	- full integration regression validation confirmed no database-shape changes were needed for report/analytics parity closure.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 5.1 (Execution Snapshot)

- Stage 5.1 completed core seed coverage for institute-aware foundational data.
- API/web contract updates applied:
	- no runtime API/Web contract changes (script-only stage).
- Query/runtime update applied:
	- core seed script now initializes institution policy flags, institution-typed baseline departments, and current report key/role-access defaults.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 5.2 (Execution Snapshot)

- Stage 5.2 completed full dummy data coverage expansion for parity testing surfaces.
- API/web contract updates applied:
	- no runtime API/Web contract changes (script-only stage).
- Query/runtime update applied:
	- `Scripts/03-FullDummyData.sql` now seeds buildings/rooms/timetables/payments/lifecycle/report artifacts and deterministic institute-type alignment for demo users/departments.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 5.3 (Execution Snapshot)

- Stage 5.3 completed data quality and replay safety hardening for script-first parity rollout.
- API/web contract updates applied:
	- no runtime API/Web contract changes (script-only stage).
- Query/runtime update applied:
	- `Scripts/03-FullDummyData.sql` now aligns seeded user/department deterministic values on replay,
	- `Scripts/05-PostDeployment-Checks.sql` now emits institute-level parity counts, critical workflow entity counts, and duplicate-safety checks.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 5.4 (Execution Snapshot)

- Stage 5.4 completed Phase 5 exit criteria for one-run script-chain readiness.
- API/web contract updates applied:
	- no runtime API/Web contract changes (script-only stage).
- Query/runtime update applied:
	- executed full script order (`01` -> `02` -> `03` -> `05`) successfully,
	- added superadmin identity reuse in full dummy script to avoid replay collisions with preexisting baseline accounts.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 6.1 (Execution Snapshot)

- Stage 6.1 completed automated institute parity test expansion.
- API/web contract updates applied:
	- no runtime API/Web contract changes (test-only stage).
- Query/runtime update applied:
	- expanded integration-test coverage for lifecycle, student submenu, and enrollment-summary report matched-institute success-path validations.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 6.2 (Execution Snapshot)

- Stage 6.2 completed cross-role UAT matrix automation for institute parity.
- API/web contract updates applied:
	- no runtime API/Web contract changes (test-only stage).
- Query/runtime update applied:
	- expanded integration-test coverage for cross-role authorization and report-visibility behavior across institution claims `0/1/2`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 6.3 (Execution Snapshot)

- Stage 6.3 completed performance and query validation for institute parity.
- API/web contract updates applied:
	- no runtime API/Web contract changes (test-only stage).
- Query/runtime update applied:
	- added integration-test validation for parity index read-usage on institute-filtered query paths,
	- added integration-test latency budget checks for common Admin dashboard/report paths.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 6.4 (Execution Snapshot)

- Stage 6.4 completed Phase 6 exit criteria validation for institute parity.
- API/web contract updates applied:
	- no runtime API/Web contract changes (validation-only stage).
- Query/runtime update applied:
	- executed consolidated parity phase-exit regression suite over Stage 6 test scopes.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 7.1 (Execution Snapshot)

- Stage 7.1 completed deployment runbook finalization for parity release readiness.
- Deployment/script updates applied:
	- updated `Scripts/README.md` with deterministic execution order (`01 -> 02 -> 03 -> 04 -> 05`),
	- added environment notes for DB create/context switching, permissions, and cleanup fallback,
	- added rollback/verification checklist for backup, failure handling, and evidence capture.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Phase 23 Stage 23.1 (Execution Snapshot)

- Stage 23.1 completed institution-type foundation confirmation from `Docs/Advance-Enhancements.md`.
- Validation highlights:
	- confirmed policy persistence keys for School/College/University mode toggles,
	- confirmed University-default compatibility path remains active.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 7.2 (Execution Snapshot)

- Stage 7.2 completed functional documentation update for institute parity behavior.
- Documentation updates applied:
	- added role/institute parity behavior guidance in `Docs/Functionality.md`,
	- added role/institute filter behavior guidance across user guides (`User Guide/README.md`, `Admin-Guide.md`, `Faculty-Guide.md`, `Student-Guide.md`).
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 7.3 (Execution Snapshot)

- Stage 7.3 completed monitoring and support handover documentation for parity release readiness.
- Documentation updates applied:
	- added institute parity monitoring guidance in `Docs/Functionality.md`,
	- added institute parity support handover checklist in `User Guide/SuperAdmin-Guide.md`.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-13 Update - Institute Parity Stage 7.4 (Execution Snapshot)

- Stage 7.4 completed Phase 7 release exit criteria for institute parity.
- Documentation updates applied:
	- finalized Phase 7 exit criteria evidence in the institute parity phase tracker,
	- synchronized the mandatory release-readiness trackers used for phase handoff,
	- advanced the command pointer to the next roadmap stage after release closeout.
- Schema impact: `No schema mutation`.
- EF migration impact: none.

## 2026-05-12 Update — Institution License Validation Phase 7 (Execution Snapshot)

- Completed SuperAdmin permission matrix against existing management and policy tables.
- Evidence set: `Artifacts/Phase7/SuperAdmin/20260512-151302/RunSummary.json`.
- Validated operations touch existing entities and mappings:
	- Department lifecycle management (`departments`).
	- Admin-user lifecycle management (`users`, `roles`).
	- Institution policy mode switching (`portal_settings` policy flags).
	- Privileged cross-mode reporting and dashboard visibility over existing report/config tables.
- No schema mutation and no EF migration required for Phase 7.

## 2026-05-12 Update — Institution License Validation Phase 6 (Execution Snapshot)

- Completed role-scoped access-boundary validation using existing assignment and report-scope tables.
- Evidence set: `Artifacts/Phase6/Access/20260512-150824/RunSummary.json`.
- Validated scope enforcement against current schema relationships:
	- Admin department assignments (`admin_department_assignments`) gate report exports.
	- Faculty offering assignments (`course_offerings.faculty_user_id`) gate report exports.
	- Student role remains restricted from operational report export endpoints.
- No schema mutation and no EF migration required for Phase 6.

## 2026-05-12 Update — Institution License Validation Phase 5 (Execution Snapshot)

- Implemented explicit per-user institution assignment persistence.
- Schema update applied through EF migration:
	- `20260512043929_AddUserInstitutionTypeAssignment`
	- Adds nullable `InstitutionType` (`int`) to `users` table.
- Application contract impact:
	- manual admin create/list endpoints now persist and return per-user institution assignment,
	- CSV import supports optional `InstitutionType` column with policy validation.
- Validation artifacts: `Artifacts/Phase5/Api/*_20260512-144212.json`.

## 2026-05-12 Update — Institution License Validation Plan

- Added execution reference: `Docs/Institution-License-Validation-Phases.md`.
- This validation plan confirms schema-backed behavior for:
	- license state and institution mode enforcement,
	- institution-aware user assignment and CSV import mapping,
	- institution-scoped access for Student/Faculty/Admin,
	- mixed-scope (School/College/University) runtime behavior.
- Expected schema touchpoints for validation evidence include: `license_state`, `users`, role mapping tables, institutional policy/configuration settings, and enrollment/academic progression tables.
- Current update is documentation-only.
- No database schema change and no EF migration were required for this update.

## 2026-05-12 Update — Institution License Validation Phase 4 (Execution Snapshot)

- Completed School/College/University x role validation sweep for dashboard/menu/report behavior.
- Confirmed policy and vocabulary switching by mode through runtime policy state:
	- School labels: Grade/Promotion/Percentage/Subject/Class
	- College labels: Year/Progression/Percentage/Subject/Year-Group
	- University labels: Semester/Progression/GPA/CGPA/Course/Batch
- Confirmed scoped report access behavior with existing assignment tables and report guards:
	- Admin scope through admin-department assignment
	- Faculty scope through assigned course offering
	- Student restricted from operational report data/export endpoints
- Validation artifacts stored at `Artifacts/Phase4/ModeRole/20260512-142021`.
- No database schema change and no EF migration required.

## 2026-05-12 Update — Institution License Validation Phase 2 (Execution Snapshot)

- Completed School/College/University lifecycle-mode validation with persisted policy checks.
- Confirmed `portal_settings` now stores and updates:
	- `institution_include_school`
	- `institution_include_college`
	- `institution_include_university`
- Confirmed policy keys change per mode after license activation and are reflected in runtime endpoints.
- Code fix impact: application-layer persistence only (`SaveChangesAsync` call in institution policy service).
- No schema mutation and no EF migration required.
- Validation-only environment workaround used during sequential mode switching:
	- cleared `consumed_verification_keys` between uploads due verification-key reuse from current generator output.

## 2026-05-12 Update — Institution License Validation Phase 3 (Execution Snapshot)

- Completed multi-mode validation for School+College, School+University, College+University, and School+College+University.
- Confirmed persisted union state in `portal_settings` keys:
	- `institution_include_school`
	- `institution_include_college`
	- `institution_include_university`
- Confirmed capability-matrix union row exposure aligns with persisted policy keys for each mixed-mode license.
- No schema mutation and no EF migration required.
- Validation-only environment workaround continued for sequential uploads:
	- cleared `consumed_verification_keys` because current generator output reuses verification key material.

## 2026-05-12 Update — Institution License Validation Phase 1 (Execution Snapshot)

- Executed baseline runtime checks for license/policy endpoints with SuperAdmin authentication.
- Observed current state:
	- `license_status`: `Invalid` (pre-upload)
	- `license_details`: `None` (pre-upload)
	- policy flags: `includeUniversity=true`, `includeSchool=false`, `includeCollege=false`
- Initial upload failure was traced to legacy `license_state` non-null columns (`InstitutionScope`, `ExpiryType`) lacking defaults in the active database schema.
- Applied SQL defaults for those legacy columns in the validation environment.
- Re-ran upload successfully; post-upload state is `license_status=Active`.
- Final matrix validation confirmed policy-driven restriction (`school=false`, `college=false`, `university=true`).
- No EF migration was introduced in this phase; remediation was environment-level compatibility for legacy schema constraints.

## 2026-05-10 Update — Phase 32 Stage 32.1

- Stage 32.1 delivered report-catalog and report-route regression guardrail integration tests only.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 33 Stage 33.1

- Phase 33 was re-scoped to hosting configuration and security hardening.
- Stage 33.1 delivered startup/configuration foundation updates in API/Web/BackgroundJobs only.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 33 Stage 33.2

- Stage 33.2 delivered runtime hosting hardening in API/Web startup and Web runtime URL handling.
- Changes were limited to configuration and startup/runtime guards.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 33 Stage 33.3

- Stage 33.3 delivered DTO-level security hardening and executable validation coverage.
- Changes were limited to model validation attributes and unit tests.
- No database schema change and no EF migration were required.

## 2026-05-11 Update — Phase 3 Stage 3.3

- Stage 3.3 delivered transport optimization in API/Web startup only.
- Changes were limited to Kestrel transport configuration and compression settings.
- No database schema change and no EF migration were required.

## 2026-05-11 Update — Phase 4 Complete

- Phase 4 delivered API cache policy, static/edge caching behavior, and cache scope controls.
- Changes were limited to runtime caching and host configuration paths.
- No database schema change and no EF migration were required.

## 2026-05-11 Update — Phase 5 Complete

- Phase 5 delivered k6 load-model upgrades, distributed-generator controls, and output-discipline runner updates.
- Changes were limited to load-testing scripts and execution wrappers.
- No database schema change and no EF migration were required.

## 2026-05-11 Update — Phase 6 Complete

- Phase 6 delivered external dependency caching, integration circuit-breaker controls, and request-path blocking-risk cleanup.
- Changes were limited to application/integration runtime and configuration paths.
- No database schema change and no EF migration were required.

## 2026-05-11 Update — Phase 7 Complete

- Phase 7 delivered expanded queue offloading and queue-platform integration support for background email work.
- Changes were limited to background worker/queue runtime and configuration paths.
- No database schema change and no EF migration were required.

## 2026-05-11 Update — Phase 8 Complete

- Phase 8 delivered auto-scaling policy baselines, host-limit tuning, and network stack tuning across API/Web/BackgroundJobs.
- Changes were limited to startup/runtime configuration and host/network behavior controls.
- No database schema change and no EF migration were required.

## 2026-05-11 Update — Phase 9 Complete

- Phase 9 delivered observability metrics, latency SLO snapshots, and continuous runtime health monitoring.
- Changes were limited to API startup/runtime telemetry, health checks, and configuration paths.
- No database schema change and no EF migration were required.

## 2026-05-11 Update — Phase 10 Complete

- Phase 10 delivered progressive load gate orchestration, bottleneck classification, and fix-and-retest support.
- Changes were limited to load-testing scripts and execution orchestration only.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 32 Stage 32.2

- Stage 32.2 delivered report export action/endpoint regression guardrail integration tests only.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 32 Stage 32.3

- Stage 32.3 delivered sidebar-settings menu-assignability regression guardrail integration tests only.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 31 Stage 31.1

- Stage 31.1 delivered regression-matrix test coverage and matrix documentation only.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 31 Stage 31.2

- Stage 31.2 delivered authorization/data-exposure/audit hardening in API controllers and integration tests.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 31 Stage 31.3

- Stage 31.3 delivered performance-band load test and recovery-smoke certification scripts plus runbook updates.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 32 Stage 32.4


## 2026-05-10 Update — Phase 32 Stage 32.5

- Stage 32.5 delivered credential and run-command verification guardrail integration tests.
- Changes were limited to integration test coverage and operational verification documentation.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 31 Complete

- Phase 31 completed as release hardening (regression matrix, security hardening, and performance/reliability certification).
- No database schema change and no EF migration were required across Stage 31.1, 31.2, and 31.3.

## 2026-05-10 Update — Phase 30 Stage 30.1

- Stage 30.1 delivered a centralized outbound integration gateway layer (retry/timeout/dead-letter handling) and integration diagnostics endpoints.
- Changes were limited to application/service/integration runtime and configuration paths.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 30 Stage 30.2

- Stage 30.2 delivered tenant onboarding templates, subscription plan controls, and tenant profile settings operations.
- Persistence uses the existing `portal_settings` key-value table with new operational keys.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 30 Stage 30.3

- Stage 30.3 delivered feature-flag rollout controls and emergency rollback operations.
- Persistence uses existing `portal_settings` key-value entries for feature-flag state and rollback metadata.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 30 Complete

- Phase 30 completed with integration gateway controls, tenant/subscription operations, and rollback-safe feature-flag controls.
- No schema changes were required across Stage 30.1, 30.2, and 30.3.

## 2026-05-09 Update — Phase 28 Stage 28.1

- Stage 28.1 delivered runtime and hosting changes only.
- No database schema change and no EF migration were required.

## 2026-05-09 Update — Phase 28 Stage 28.2 Foundation

- Stage 28.2 foundation delivered cache and background-worker changes only.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.2 Completion

- Stage 28.2 completion delivered additional API queue-processing endpoints and workers for report generation and publish-all recalculation workloads.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 1

- Stage 28.3 slice 1 delivered a storage-provider abstraction for file/media persistence and migrated payment-proof uploads to provider-backed object-key references.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 2

- Stage 28.3 slice 2 migrated graduation certificate persistence to the shared storage-provider abstraction and added provider-backed read support.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 3

- Stage 28.3 slice 3 migrated license-upload temporary file handling to provider-backed storage operations and added bytes-based license activation.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 4

- Stage 28.3 slice 4 introduced configuration-driven storage provider selection and a blob-style storage adapter implementation.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 5

- Stage 28.3 slice 5 migrated portal logo uploads to provider-backed storage and added key-based logo streaming from the API.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 6

- Stage 28.3 slice 6 added temporary signed read URL support in the storage abstraction and provider adapters.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 7

- Stage 28.3 slice 7 enforced local signed URL validation (`exp`/`sig`) for portal logo reads with legacy unsigned-link signed redirects.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 8

- Stage 28.3 slice 8 added authenticated tokenized certificate streaming endpoint support and signed local certificate read validation.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 9

- Stage 28.3 slice 9 added provider-backed storage metadata lookup support (content type and length) for media objects.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Stage 28.3 Slice 10

- Stage 28.3 slice 10 added provider-persisted integrity/disposition metadata support (SHA-256 content hash and optional download filename) for media objects.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 28 Complete

- Phase 28 completed with runtime, cache/worker, and media-storage architecture changes only.
- No database schema change and no EF migration were required for Phase 28 completion.

## 2026-05-10 Update — Phase 29 Stage 29.1

- Stage 29.1 added composite indexes for graduation applications, support tickets, notification recipients, payment receipts, quiz attempts, and user sessions.
- EF migration added: `20260509155457_20260510_Phase29_IndexBaseline`.
- Current model audit found no `InstitutionId`, `YearId`, or `GradeId` columns, so no indexes were added for those keys in this stage.

## 2026-05-10 Update — Phase 29 Stage 29.2 Slice 1

- Stage 29.2 slice 1 added application/query contract pagination for helpdesk ticket lists only.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 29 Stage 29.2 Slice 2

- Stage 29.2 slice 2 added application/query contract pagination for graduation application lists.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 29 Stage 29.2 Slice 3

- Stage 29.2 slice 3 added application/query contract pagination for payment receipt lists.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 29 Stage 29.3

- Stage 29.3 delivered operational maintenance scripts for retention cleanup, index maintenance, and capacity-growth dashboards.
- Added scripts: `Scripts/3-Phase29-ArchivePolicy.sql`, `Scripts/4-Phase29-IndexMaintenance.sql`, `Scripts/5-Phase29-CapacityGrowthDashboard.sql`.
- No database schema change and no EF migration were required.

## 2026-05-10 Update — Phase 29 Complete

- Phase 29 is complete (Stages 29.1, 29.2 slices 1-3, and 29.3).
- Schema-impacting work concluded in Stage 29.1; Stage 29.2 and Stage 29.3 were application/operations-only updates.

---

# PART 1: UNIVERSITY PORTAL APPLICATION DATABASE

---

## 1. Identity & Access Control

### users
Stores all system users (students, faculty, admins, super admins).

- id (UUID, PK)
- username (unique)
- email (unique, nullable)
- password_hash
- role_id (FK → roles.id)
- department_id (FK → departments.id, nullable)
- is_active
- created_at
- updated_at
- last_login_at

---

### roles
Predefined system roles.

- id (PK)
- name (Student, Faculty, Admin, SuperAdmin)
- description
- is_system_role

---

## 2. Department & Academic Structure

### departments
Core academic departments.

- id (PK)
- name
- code (unique)
- is_active
- created_at

---

### programs
Academic programs offered by a department.

- id (PK)
- department_id (FK)
- name
- code
- duration_years
- is_active

---

### courses
Courses offered under programs.

- id (PK)
- department_id (FK)
- program_id (FK)
- code
- title
- credit_hours
- is_active

---

### semesters
Academic semesters.

- id (PK)
- name (e.g., Fall 2025)
- start_date
- end_date
- is_active

---

## 3. Student Information System (Permanent Records)

### students
Student core identity.

- id (UUID, PK)
- user_id (FK → users.id)
- registration_number (unique)
- program_id (FK)
- current_semester_id (FK)
- admission_date
- status

---

### student_semester_records
Complete academic history (never deleted).

- id (PK)
- student_id (FK)
- semester_id (FK)
- gpa
- cgpa
- academic_status
- created_at

---

### student_course_enrollments
Tracks course enrollment per semester.

- id (PK)
- student_id (FK)
- course_id (FK)
- semester_id (FK)
- status (enrolled, dropped, completed)

---

## 4. Assignments & Submissions

### assignments
Teacher-created assignments.

- id (PK)
- course_id (FK)
- faculty_id (FK → users.id)
- semester_id (FK)
- title
- description
- due_date
- is_published
- created_at

---

### assignment_submissions
Student submissions.

- id (PK)
- assignment_id (FK)
- student_id (FK)
- file_path
- submitted_at
- grade
- feedback

---

## 5. Quizzes & Assessments

### quizzes
Quiz definitions.

- id (PK)
- course_id (FK)
- semester_id (FK)
- title
- start_time
- end_time
- max_attempts

---

### quiz_attempts
Student quiz attempts.

- id (PK)
- quiz_id (FK)
- student_id (FK)
- score
- attempted_at

---

## 6. Attendance Management

### attendance_records
Daily attendance tracking.

- id (PK)
- student_id (FK)
- course_id (FK)
- semester_id (FK)
- attendance_date
- status (present, absent)

---

## 7. Results & Grades

### grades
Final course results.

- id (PK)
- student_id (FK)
- course_id (FK)
- semester_id (FK)
- grade

---

## 8. Notifications System

### notifications
Central notification messages.

- id (PK)
- title
- message
- type (assignment, quiz, result, attendance, fyp)
- created_by (FK → users.id)
- created_at

---

### notification_recipients
Per-user delivery tracking.

- id (PK)
- notification_id (FK)
- user_id (FK)
- is_read
- read_at

---

## 9. Final Year Project (FYP)

### fyp_projects
Student project records.

- id (PK)
- student_id (FK)
- title
- semester_id (FK)

---

### fyp_meetings
Scheduled project meetings.

- id (PK)
- fyp_project_id (FK)
- meeting_datetime
- department_id (FK)
- room_number

---

### fyp_panel_members
Faculty panel participants.

- id (PK)
- fyp_meeting_id (FK)
- faculty_id (FK)

---

## 10. UI Themes & Personalization

### themes
Available UI themes.

- id (PK)
- name
- is_dark
- is_accessible
- is_active

---

### user_theme_preferences
User-selected themes.

- id (PK)
- user_id (FK)
- theme_id (FK)

---

## 11. Module Control (Feature Toggles)

### modules
All selectable modules.

- id (PK)
- key (assignment, quiz, ai_chat, fyp, etc.)
- name
- is_mandatory

---

### module_status
Super Admin-controlled activation.

- id (PK)
- module_id (FK)
- is_active
- activated_at

---

## 12. License State (Application Side Only)

### license_state
Stores validated license status (no license creation).

- id (PK)
- license_hash
- license_type (yearly, permanent)
- status (active, expired)
- activated_at
- expires_at (nullable)

---

# PART 2: LICENSE CREATION TOOL DATABASE

*(Separate system, separate database)*

---

## 13. License Storage

### licenses
Only hashed keys stored.

- id (PK)
- license_key_hash (unique)
- license_type (yearly, permanent)
- issued_at
- expires_at (nullable)
- status (active, revoked)

---

## 14. License Issuance Logs

### license_issuance_logs
Tracks who generated licenses.

- id (PK)
- license_id (FK)
- issued_by_user
- issued_at
- notes

---

## 15. License Revocation

### license_revocations
Revoked licenses history.

- id (PK)
- license_id (FK)
- revoked_at
- reason

---

## 16. License Tool Users

### license_tool_users
Admins of license tool.

- id (PK)
- username
- password_hash
- role (SuperAdmin)
- created_at

---

## 17. License Audit Logs

### license_audit_logs
Full traceability.

- id (PK)
- action (create, revoke, view)
- license_id (FK)
- performed_by
- performed_at
- ip_address

---

## 18. Academic Calendar (Phase 12)

### academic_deadlines
Named academic deadlines and key dates attached to a semester. Used by the Academic Calendar portal page and the `DeadlineReminderJob` background service.

- id (UUID, PK)
- semester_id (FK → semesters.id, cascade delete)
- title (nvarchar 200, required)
- description (nvarchar 1000, nullable)
- deadline_date (datetime2)
- reminder_days_before (int, default 3 — 0 means day-of reminder only)
- is_active (bool, default true)
- last_reminder_sent_at (datetime2, nullable — set by DeadlineReminderJob when notification is dispatched)
- is_deleted (bool — soft delete via global query filter)
- deleted_at (datetime2, nullable)
- created_at (datetime2)
- updated_at (datetime2)
- row_version (rowversion / timestamp — optimistic concurrency)

**Indexes:**
- `IX_academic_deadlines_semester` on `semester_id`
- `IX_academic_deadlines_date_active` on `(deadline_date, is_active)`

**EF Migration:** `20260507_Phase12AcademicCalendar`

---

## 19. Global Search (Phase 13)

Phase 13 introduces no new database tables. All search queries execute against existing tables using EF Core LINQ joins:

| Entity searched | Table(s) queried |
|---|---|
| Students | `student_profiles` JOIN `users` |
| Courses | `courses` |
| Course Offerings | `course_offerings` JOIN `courses` JOIN `semesters` |
| Faculty | `users` JOIN `roles` (where `roles.name = 'Faculty'`) |
| Departments | `departments` |
| Student-enrolled offerings | `enrollments` JOIN `course_offerings` JOIN `courses` |

All queries respect global soft-delete query filters (`is_deleted = 0`) automatically.

Role-scoped filtering applied at the application service layer:
- **SuperAdmin** — all entities across all departments
- **Admin** — entities within their assigned departments
- **Faculty** — entities within their own department + their own course offerings
- **Student** — only their enrolled course offerings

**EF Migration:** None required (no schema changes)

---

## 18. Design Guarantees

- License contains **no university identity**
- License file is cryptographically protected
- License keys cannot be altered
- Academic data is never deleted
- Module-based feature control supported
- Fully offline-capable license validation

---

## 19. Implementation Conventions (ASP.NET + EF Core)

- Use GUID PKs for all user-facing and distributed entities (users, students, assignments, licenses)
- Use bigint PKs for high-volume append-only logs where sequential inserts are beneficial
- Add created_at, updated_at, and row_version (concurrency token) to mutable aggregates
- Use soft-delete columns (is_deleted, deleted_at) for operational entities; never soft-delete academic history tables
- Store all timestamps in UTC

---

## 20. Additional Core Tables Required for Build Readiness

### user_sessions
Tracks web/API sessions and refresh-token family state.

- id (UUID, PK)
- user_id (FK -> users.id)
- refresh_token_hash
- device_info
- ip_address
- expires_at
- revoked_at (nullable)

### faculty_department_assignments
Supports faculty mapped to one or more departments.

- id (PK)
- faculty_id (FK -> users.id)
- department_id (FK -> departments.id)
- is_primary

### course_offerings
Represents a course running in a specific semester and department context.

- id (PK)
- course_id (FK)
- semester_id (FK)
- department_id (FK)
- faculty_id (FK -> users.id)
- section
- capacity
- is_active

### registration_whitelist
Pre-approved registration numbers for controlled student signup.

- id (PK)
- registration_number (unique)
- program_id (FK)
- semester_id (FK)
- is_claimed
- claimed_by_student_id (FK, nullable)

### quiz_questions
Question bank entries tied to quizzes.

- id (PK)
- quiz_id (FK)
- question_text
- question_type (mcq, short, true_false)
- marks
- display_order

### quiz_question_options
Options for objective quiz questions.

- id (PK)
- quiz_question_id (FK)
- option_text
- is_correct

### quiz_attempt_answers
Submitted answers per attempt.

- id (PK)
- quiz_attempt_id (FK)
- quiz_question_id (FK)
- selected_option_id (FK, nullable)
- answer_text (nullable)
- awarded_marks

### transcript_exports
Tracks transcript generation history for compliance and auditability.

- id (PK)
- student_id (FK)
- exported_by (FK -> users.id)
- exported_at
- format (pdf, excel)

### audit_logs
Operational audit logs for privileged activities.

- id (bigint, PK)
- actor_user_id (FK -> users.id, nullable)
- action
- entity_name
- entity_id
- old_values_json
- new_values_json
- occurred_at
- ip_address

---

## 21. Constraint and Index Strategy

- users: unique indexes on username and email (filtered where email is not null)
- students: unique index on registration_number and unique index on user_id
- student_course_enrollments: unique composite index on (student_id, course_id, semester_id)
- attendance_records: unique composite index on (student_id, course_id, semester_id, attendance_date)
- assignment_submissions: unique composite index on (assignment_id, student_id)
- module_status: unique index on module_id to enforce single active status row
- notifications_recipients: index on (user_id, is_read)
- audit_logs: clustered index by occurred_at for time-range queries

---

## 22. Data Retention and Archival Rules

- Academic records: never deleted; archive to cold storage after policy threshold
- Audit logs: retain online for 24 months, archive for 7 years
- Notification delivery logs: retain online for 12 months, then archive
- Session and token records: purge expired and revoked entries after 90 days

---

## 23. Migration and Seeding Plan

- Baseline migration: identity, departments, SIS core, license_state
- Seed mandatory roles, modules, and default themes
- Seed Super Admin bootstrap user through secure deployment script
- Apply feature migrations per release train (v1, v1.1, v1.2)

---

## 24. Phase 20 LMS Tables (Migration `Phase20_LMS` — 2026-05-08)

### course_content_modules
Structured weekly learning modules created by faculty per `CourseOffering`.

- id (UUID, PK)
- offering_id (FK → course_offerings.id, ON DELETE CASCADE)
- title (varchar 300)
- week_number (int)
- body (nvarchar 50000)
- is_published (bool, default false)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

### content_videos
Video attachments linked to a `CourseContentModule`.

- id (UUID, PK)
- module_id (FK → course_content_modules.id, ON DELETE CASCADE)
- title (varchar 300)
- storage_url (varchar 1000, nullable)
- embed_url (varchar 1000, nullable)
- duration_seconds (int, default 0)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

### discussion_threads
Discussion threads opened within a `CourseOffering`.

- id (UUID, PK)
- offering_id (FK → course_offerings.id, ON DELETE CASCADE)
- title (varchar 500)
- author_id (FK → users.id, ON DELETE NO ACTION)
- is_pinned (bool, default false)
- is_closed (bool, default false)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

### discussion_replies
Replies within a `DiscussionThread`.

- id (UUID, PK)
- thread_id (FK → discussion_threads.id, ON DELETE CASCADE)
- author_id (FK → users.id, ON DELETE NO ACTION)
- body (nvarchar 10000)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

### course_announcements
Course-level announcements posted by faculty; triggers fan-out notification to enrolled students.

- id (UUID, PK)
- offering_id (FK → course_offerings.id, ON DELETE SET NULL, nullable)
- author_id (FK → users.id, ON DELETE NO ACTION)
- title (varchar 300)
- body (nvarchar 10000)
- posted_at (datetime)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

---

### study_plans
Student semester study plans. Phase 21 — Study Planner.

- id (UUID, PK)
- student_profile_id (FK → student_profiles.id, ON DELETE CASCADE)
- planned_semester_name (varchar 100)
- notes (nvarchar 2000, nullable)
- advisor_status (int, default 0 — 0=Pending, 1=Endorsed, 2=Rejected)
- advisor_notes (nvarchar 2000, nullable)
- reviewed_by_user_id (UUID, nullable)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

### study_plan_courses
Course line items within a study plan.

- id (UUID, PK)
- study_plan_id (FK → study_plans.id, ON DELETE CASCADE)
- course_id (FK → courses.id, ON DELETE RESTRICT)
- created_at / updated_at / row_version
- UQ_study_plan_courses_plan_course (study_plan_id, course_id)

---

### accreditation_templates
Accreditation / government report templates. Phase 22 — External Integrations.
EF migration: `Phase22_ExternalIntegrations`.

- id (UUID, PK)
- name (nvarchar 200)
- description (nvarchar 1000, nullable)
- field_mappings_json (nvarchar max) — JSON array of field mapping objects
- format (nvarchar 20) — "CSV" or "PDF"
- is_active (bool, default true)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

---

### Phase 23 — Core Policy Foundation (no new tables)
Institution type flags stored as rows in `portal_settings` under the keys:
- `institution_policy_school` (value: "true"/"false")
- `institution_policy_college` (value: "true"/"false")
- `institution_policy_university` (value: "true"/"false")

No EF migration required — reuses the existing `portal_settings` key-value store.

---

### Phase 24 — Dynamic Module and UI Composition (no new tables)
All Phase 24 services (`ModuleRegistryService`, `LabelService`, `DashboardCompositionService`) are stateless / in-process. No new tables were added.

---

### institution_grading_profiles
Per-institution-type grading configuration (pass threshold and grade bands). Phase 25 — Academic Engine Unification.
EF migration: `20260508152906_Phase25_AcademicEngineUnification`.

- id (UUID, PK)
- institution_type (int) — `InstitutionType` enum: 0=University, 1=School, 2=College
- pass_threshold (decimal(5,2)) — 0–4.0 for University; 0–100 for School/College
- grade_ranges_json (nvarchar max, nullable) — JSON grade band array, e.g. `[{"From":90,"To":100,"Label":"A+"},...]`; null uses built-in defaults
- is_active (bool, default true)
- created_at / updated_at

**Indexes:**
- `IX_institution_grading_profiles_type` — unique on `institution_type` (one profile per type)

---

### school_streams
School stream master records (Science/Commerce/Arts etc.). Phase 26 — School and College Functional Expansion.
EF migration: `20260509044437_Phase26_SchoolCollegeExpansion`.

- id (UUID, PK)
- name (nvarchar 120, required, unique)
- description (nvarchar 500, nullable)
- is_active (bool, default true)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

**Indexes:**
- `IX_school_streams_name` — unique on `name`

---

### student_stream_assignments
One-to-one stream assignment per student profile.

- id (UUID, PK)
- student_profile_id (UUID, FK -> student_profiles.id)
- school_stream_id (UUID, FK -> school_streams.id)
- assigned_by_user_id (UUID)
- assigned_at (datetime)
- created_at / updated_at

**Indexes:**
- `IX_student_stream_assignments_student` — unique on `student_profile_id`

---

### student_report_cards
Report-card snapshot store for school/college/university period exports.

- id (UUID, PK)
- student_profile_id (UUID, FK -> student_profiles.id)
- institution_type (int)
- period_label (nvarchar 80)
- payload_json (nvarchar max)
- generated_by_user_id (UUID)
- generated_at (datetime)
- created_at / updated_at

**Indexes:**
- `IX_student_report_cards_student_generated` — (`student_profile_id`, `generated_at`)

---

### bulk_promotion_batches
Header table for approval-based bulk promotion operations.

- id (UUID, PK)
- title (nvarchar 180)
- status (int) — `BulkPromotionStatus` enum
- created_by_user_id (UUID)
- approved_by_user_id (UUID, nullable)
- reviewed_at (datetime, nullable)
- applied_at (datetime, nullable)
- review_note (nvarchar 1000, nullable)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

**Indexes:**
- `IX_bulk_promotion_batches_status_created` — (`status`, `created_at`)

---

### bulk_promotion_entries
Per-student promote/hold decisions attached to a bulk promotion batch.

- id (UUID, PK)
- batch_id (UUID, FK -> bulk_promotion_batches.id)
- student_profile_id (UUID, FK -> student_profiles.id)
- decision (int) — `EntryDecision` enum
- reason (nvarchar 500, nullable)
- is_applied (bool)
- applied_at (datetime, nullable)
- created_at / updated_at

**Indexes:**
- `IX_bulk_promotion_entries_batch` — (`batch_id`)
- `IX_bulk_promotion_entries_batch_student` — unique (`batch_id`, `student_profile_id`)

---

### parent_student_links
Parent/guardian to student mapping used for parent-read portal access.

- id (UUID, PK)
- parent_user_id (UUID, FK -> users.id)
- student_profile_id (UUID, FK -> student_profiles.id)
- relationship (nvarchar 60, nullable)
- is_active (bool, default true)
- is_deleted (bool, default false)
- deleted_at (datetime, nullable)
- created_at / updated_at / row_version

**Indexes:**
- `IX_parent_student_links_parent_student` — unique (`parent_user_id`, `student_profile_id`)

---

### Phase 27 — University Portal Parity and Student Experience
Phase 27 (Stages 27.1, 27.2, 27.3) introduced service/API/web integration and security/abstraction contracts only.

- No new database tables.
- No altered columns.
- No new indexes.
- No EF migration required.