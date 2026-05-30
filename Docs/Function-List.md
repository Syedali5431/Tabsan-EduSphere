<!-- markdownlint-disable MD012 MD022 MD032 MD041 MD060 -->

## 2026-05-31 Update - Programs Demo Seed v29 and Filter Validation Synchronization

### Programs runtime additions
- No new public API/controller/service signature was introduced in this slice.
- Existing Programs runtime surface remains authoritative (no duplicate function inventory rows added):
	- PortalController.Programs
	- PortalController.CreateProgram
	- PortalController.UpdateProgram
	- PortalController.SetProgramActive
	- EduApiClient.GetProgramDetailsAsync
	- EduApiClient.CreateProgramAsync
	- EduApiClient.UpdateProgramAsync
	- EduApiClient.ActivateProgramAsync
	- EduApiClient.DeactivateProgramAsync
	- ProgramController.GetAll
	- ProgramController.Create
	- ProgramController.Update
	- ProgramController.Activate
	- ProgramController.Deactivate

### Programs validation summary
- Scripts/03-FullDummyData.sql now includes deterministic Programs filter demo rows and metadata marker FullDummyData-v29:
	- PRGFILENG (Dummy Engineering)
	- PRGFILBUS (School of Business Administration)
	- PRGFILMAT (Mathematics Department)
- Scripts/05-PostDeployment-Checks.sql now validates:
	- DummySeed_DemoDatasetVersionIsV29
	- DummySeed_ProgramsFilterDemo_ByIdCount
	- DummySeed_ProgramsFilterDemo_ActiveCount
	- DummySeed_ProgramsFilterDemo_DummyEngineeringCount
	- DummySeed_ProgramsFilterDemo_BusinessCount
	- DummySeed_ProgramsFilterDemo_MathCount
	- DummySeed_DemoDatasetVersion_v29
- Runtime verification confirmed Programs menu loads and department filtering isolates seeded demo rows correctly without page-load errors.

## 2026-05-31 Update - Degree Audit Demo Seed v28 and Filter Runtime Reliability Synchronization

### Degree Audit runtime additions
- No new public API route signature was introduced; existing runtime surfaces were stabilized for role-safe loading and deterministic demo/testing data:
	- PortalController.DegreeAudit
	- EduApiClient.GetMyDegreeAuditAsync
	- EduApiClient.GetStudentDegreeAuditAsync
	- DegreeAuditController.GetMyAudit
	- DegreeAuditController.GetStudentAudit
	- DegreeAuditService.GetAuditAsync
- Existing Degree Audit UI/runtime flow enhancements added in this slice:
	- non-student flow now loads a student picker and avoids student-only self-audit call path,
	- Degree Audit page exposes deterministic student selection for admin/superadmin filter validation,
	- institution-type guards were synchronized to avoid false negative checks when department navigation data is not populated.

### Degree Audit validation summary
- Scripts/03-FullDummyData.sql advanced to FullDummyData-v28 and now includes deterministic Degree Audit filter-demo data handling:
	- grade-point population for deterministic Degree Audit cohorts,
	- university deterministic grade-point coverage for menu/filter testing,
	- idempotent reseed behavior preserved.
- Scripts/05-PostDeployment-Checks.sql now validates:
	- DummySeed_DemoDatasetVersionIsV28,
	- DummySeed_DegreeAuditFilterDemo_ResultRowsCount,
	- DummySeed_DegreeAuditFilterDemo_GradePointPopulatedCount,
	- DummySeed_DegreeAuditUniversityDemo_ResultRowsCount,
	- DummySeed_DegreeAuditUniversityDemo_StudentProfileCount,
	- DummySeed_DegreeAuditUniversityDemo_GradePointPopulatedCount.
- Runtime verification confirmed Degree Audit menu loads, student filter selection changes context correctly, and completed-course/audit cards render seeded data for selected deterministic students.

## 2026-05-30 Update - Enrollments Demo Seed and Filter Reliability Synchronization

### Enrollments runtime additions
- No new public API/controller/service signature was introduced in this slice.
- Existing Enrollments runtime surface remains authoritative (no duplicate function inventory rows added):
	- PortalController.Enrollments
	- PortalController.SetEnrollmentActive
	- PortalController.EnrollStudent
	- PortalController.AdminDropEnrollment
	- PortalController.StudentEnroll
	- PortalController.StudentDropEnrollment
	- EduApiClient.GetEnrollmentRosterAsync
	- EduApiClient.AdminEnrollStudentAsync
	- EduApiClient.AdminDropEnrollmentAsync
	- EnrollmentController.GetRoster
- Internal synchronization added for reliability:
	- deterministic Enrollments filter-demo students extended to include DEMO-ENG-704 and DEMO-ENG-705,
	- deterministic Enrollments demo rows are normalized back to Active/DroppedAt=NULL on reseed for repeatable demo runs.

### Enrollments validation summary
- Scripts/03-FullDummyData.sql now includes deterministic Enrollments demo coverage:
	- DS-101 offering 66666666-6666-6666-6666-666666666661 with 3 active demo rows,
	- DB-201 offering 66666666-6666-6666-6666-666666666662 with 5 active demo rows.
- Scripts/05-PostDeployment-Checks.sql now validates:
	- DummySeed_EnrollmentsFilterDemo_StudentProfilesCount
	- DummySeed_EnrollmentsFilterDemo_DS101_ActiveCount
	- DummySeed_EnrollmentsFilterDemo_DB201_ActiveCount
	- dataset marker checks upgraded to FullDummyData-v27.
- Runtime verification confirmed Enrollments menu loads and offering filter changes roster counts from 3 (DS-101) to 5 (DB-201) with deterministic rows displayed.

## 2026-05-30 Update - Student Lifecycle Demo Seed and Filter Reliability Synchronization

### Student Lifecycle runtime additions
- No new public API/controller/service signature was introduced in this slice.
- Existing Student Lifecycle runtime surface remains authoritative (no duplicate function inventory rows added):
	- PortalController.StudentLifecycle
	- PortalController.GraduateStudent
	- PortalController.PromoteStudent
	- EduApiClient.GetGraduationCandidatesAsync
	- EduApiClient.GetStudentsByAcademicLevelAsync
	- StudentLifecycleController.GetGraduationCandidates
	- StudentLifecycleController.GetStudentsByAcademicLevel
- Internal synchronization added for reliability:
	- StudentLifecycleRepository compatibility handling for legacy active status value `0` plus canonical `1`.
	- StudentLifecycleService now resolves graduation candidate display names from linked user records.
	- StudentLifecycleService enforces active-status compatibility in promotion validation.

### Student Lifecycle validation summary
- Scripts/03-FullDummyData.sql now includes deterministic Student Lifecycle filter demo rows:
	- 98989898-9898-9898-9898-989898989811 (DEMO-LIFE-CS-801, CS, semester 8, active)
	- 98989898-9898-9898-9898-989898989812 (DEMO-LIFE-CS-101, CS, semester 1, active)
	- 98989898-9898-9898-9898-989898989813 (DEMO-LIFE-ENG-201, Engineering, semester 2, active)
- Scripts/05-PostDeployment-Checks.sql now validates:
	- DummySeed_StudentLifecycleFilterDemo_ProfileCount
	- DummySeed_StudentLifecycleFilterDemo_CSDeptCount
	- DummySeed_StudentLifecycleFilterDemo_EngineeringDeptCount
	- DummySeed_StudentLifecycleFilterDemo_StatusActiveCount
	- DummySeed_StudentLifecycleFilterDemo_GraduationCandidateCount
	- DummySeed_StudentLifecycleFilterDemo_Semester1Count
	- DummySeed_StudentLifecycleFilterDemo_Semester2Count
	- StudentLifecycle_LegacyStatus0Count
- Runtime verification confirmed Student Lifecycle menu loads, filter states change result sets by department/semester, and seeded rows appear without screen-load exceptions.

## 2026-05-30 Update - FYP Demo Seed and Menu Filter Synchronization

### FYP runtime additions
- No new API/controller/service method signature was introduced in this slice.
- Existing FYP runtime surface remains authoritative (no duplicate function inventory rows added):
  - PortalController.Fyp
  - PortalController.CreateFypProject
  - PortalController.AssignFypSupervisor
  - EduApiClient.GetAllFypProjectsAsync
  - EduApiClient.GetFypByDepartmentAsync
  - FypController.GetByDepartment

### FYP validation summary
- Scripts/03-FullDummyData.sql now includes deterministic FYP filter demo rows:
  - 61616161-6161-6161-6161-616161616101 (CS Proposed)
  - 61616161-6161-6161-6161-616161616102 (CS InProgress)
  - 61616161-6161-6161-6161-616161616103 (Engineering Completed)
- Scripts/03-FullDummyData.sql now includes one deterministic upcoming FYP meeting row:
  - 62626262-6262-6262-6262-626262626201
- Scripts/05-PostDeployment-Checks.sql now validates:
  - DummySeed_FypFilterDemoRows_ByIdCount
  - DummySeed_FypFilterDemo_CS_DepartmentCount
  - DummySeed_FypFilterDemo_Engineering_DepartmentCount
  - DummySeed_FypFilterDemo_UpcomingMeetingCount
- Runtime verification confirmed FYP menu load succeeds and department filters isolate deterministic CS and Engineering demo projects correctly.

## 2026-05-30 Update - Quizzes Demo Seed and Filter Reliability Synchronization

### Quizzes runtime additions
- No new API/controller/service method signature was introduced in this slice.
- Existing Quizzes runtime surface remains authoritative (no duplicate function inventory rows added):
  - PortalController.Quizzes
  - PortalController.SetQuizActive
  - EduApiClient.GetQuizzesByOfferingAsync
  - QuizController.GetByOffering

### Runtime behavior synchronization
- Existing mappings and payload handling were synchronized so Quizzes UI actions post stable values:
  - API quiz identifier mapping now prefers `quizId` for web quiz item identity.
  - Activate action now guards against empty quiz id payload before issuing API write.
  - Quizzes form hidden boolean payloads now submit explicit true/false values.
  - Quiz summary contract now carries `IsActive` in listing responses for consistent Activate/Deactivate rendering.

### Quizzes validation summary
- Scripts/03-FullDummyData.sql now includes deterministic offering-501 Quizzes filter demo rows:
  - 13131313-1313-1313-1313-131313131307 (active)
  - 13131313-1313-1313-1313-131313131308 (inactive)
- Scripts/05-PostDeployment-Checks.sql now validates:
  - DummySeed_QuizRows_FilterDemoByIdCount
  - DummySeed_QuizRows_Offering501_ActiveCount
  - DummySeed_QuizRows_Offering501_InactiveCount
- Runtime verification confirmed includeInactive=false excludes inactive demo quiz and includeInactive=true includes it.

## 2026-05-30 Update - Results Internal Demo Seed and Filter Scope Alignment

### Runtime additions
- No new API/controller method signature was introduced in this slice.
- Existing Results runtime surface remains authoritative (no duplicate function inventory rows added):
	- PortalController.Results
	- EduApiClient.GetResultsByOfferingAsync
	- ResultController.GetResultsByOffering

### Validation Summary
- Scripts/03-FullDummyData.sql now includes deterministic Internal Results demo rows for offering 55555555-5555-5555-5555-555555555501:
	- cccccccc-cccc-cccc-cccc-cccccccccc40
	- cccccccc-cccc-cccc-cccc-cccccccccc41
	- cccccccc-cccc-cccc-cccc-cccccccccc42
- Scripts/03-FullDummyData.sql includes a scope-alignment block for offering 555...501 so TenantId/CampusId/InstitutionType are set for scoped Results filtering.
- Scripts/05-PostDeployment-Checks.sql now validates:
	- DummySeed_ResultRows_InternalDemoRowCount
	- DummySeed_ResultRows_Offering501_InternalCount
	- DummySeed_ResultOffering501_ScopeAligned
- Runtime verification confirmed Results page renders Results 3 and includes registration numbers 2026-CS-0001, 2026-CS-0002, 2026-CS-0003 under Internal/Internal scoped filters.

## 2026-05-30 Update - Students Demo Seed v26 and Outage-Resilient Menu Load

### Runtime additions
- Added one new helper in existing web controller flow:
	- `PortalController.IsApiConnectivityException` (classifies HTTP/socket/timeout failures so menu guard and Students view handle temporary API outage gracefully).
- Existing Students runtime surface remains authoritative:
	- `PortalController.Students`
	- `EduApiClient.GetDepartmentsAsync`
	- `EduApiClient.GetStudentsAsync`

### Validation Summary
- `Scripts/03-FullDummyData.sql` advanced to `FullDummyData-v26`.
- Added deterministic Students filter demo records:
	- `STUFILT-CS-901`, `STUFILT-CS-902` (Computer Science department)
	- `STUFILT-BUS-903` (Business department)
- `Scripts/05-PostDeployment-Checks.sql` extended with Students filter demo assertions and v26 marker checks.
- Students page now shows friendly warning when API is unavailable instead of a developer exception page.

## 2026-05-30 Update - Student Timetable Demo Seed v25 Expansion

### Runtime additions
- No new API/controller method signature added in this slice.
- Existing Student Timetable runtime surface remains authoritative:
	- `PortalController.TimetableStudent`
	- `EduApiClient.GetTimetablesByDepartmentAsync`
	- `EduApiClient.GetTimetableByIdAsync`

### Validation Summary
- `Scripts/03-FullDummyData.sql` advanced to `FullDummyData-v25`.
- Student Timetable demo pack expanded for Dummy Engineering with one additional published timetable (`2525...2903`) and three additional entries (`2626...2904` to `2626...2906`) covering Tuesday/Friday/Saturday filters.
- `Scripts/05-PostDeployment-Checks.sql` extended with v25 and new day-of-week assertions for timetable `2525...2903`.

## 2026-05-30 Update - Student Timetable Demo Seed v24 and Error Message Normalization

### Runtime additions
- Added one new internal helper in existing API client flow (no endpoint contract change):
	- `EduApiClient.TryExtractApiErrorMessage` (extracts `message`/`title` from JSON API error payloads so portal alerts show readable text).
- Existing runtime surface retained for Student Timetable filter behavior:
	- `PortalController.TimetableStudent`
	- `EduApiClient.GetTimetablesByDepartmentAsync`
	- `EduApiClient.GetTimetableByIdAsync`

### Validation Summary
- `Scripts/03-FullDummyData.sql` advanced to `FullDummyData-v24` and now includes deterministic Student Timetable demo pack rows for Dummy Engineering.
- `Scripts/05-PostDeployment-Checks.sql` extended with v24 and Student Timetable demo assertions (timetable count + day-of-week entry checks).
- Menu-path runtime validation confirmed Student Timetable filter behavior for timetable switch and day-of-week narrowing.

## 2026-05-30 Update - Attendance Filter Demo Seed v23 and Student Filter Mapping Sync

### Runtime additions
- No new endpoint/method signature introduced in this slice.
- Existing attendance/enrollment runtime surfaces were synchronized to fix student filter behavior without creating duplicate inventory entries:
	- `EnrollmentController.GetRoster` (response contract extended with `StudentProfileId` while retaining enrollment `Id`)
	- `EduApiClient.GetEnrollmentRosterAsync` (maps `StudentProfileId` for web usage)
	- `PortalController.RenderAttendanceAsync` (student dropdown + roster filtering now use `StudentProfileId`)

### Validation Summary
- Deterministic attendance filter demo data added in `Scripts/03-FullDummyData.sql` with marker `FullDummyData-v23`.
- `Scripts/05-PostDeployment-Checks.sql` extended with v23 and attendance filter demo assertions.
- Attendance and Enter Attendance views now filter correctly when selecting a specific student.

## 2026-05-30 Update - Announcements Demo Seed v22 and Filter Binding Sync

### Runtime additions
- No new runtime function signature was introduced in this synchronization slice.
- Existing Announcements runtime surfaces remain authoritative (no duplicate function inventory rows added):
	- `PortalController.Announcements`
	- `PortalController.CreateAnnouncement`
	- `PortalController.SetAnnouncementActive`
	- `PortalController.DeleteAnnouncement`
	- `EduApiClient.GetAnnouncementsAsync`
	- `AnnouncementController.GetAnnouncements`

### Validation Summary
- `Scripts/03-FullDummyData.sql` upgraded to `FullDummyData-v22` and now includes deterministic active/inactive announcement seed rows for offering filter demos.
- `Scripts/05-PostDeployment-Checks.sql` now validates the v22 marker and deterministic announcements filter dataset counts.
- Announcements web filter binding fix is synchronized by removing conflicting `includeInactive=false` hidden input and defaulting `PortalController.Announcements` query flag to `false`.

## 2026-05-29 Update - Discussion Demo Seed v21 and Filter Verification Sync

### Runtime additions
- No new application/runtime function signature was added in this synchronization slice.
- Discussion menu/filter behavior continues to use existing runtime surfaces (no duplicate function inventory rows added):
	- `PortalController.Discussion`
	- `PortalController.DiscussionThreadDetail`
	- `EduApiClient.GetDiscussionThreadsAsync`
	- `DiscussionController.GetThreads`
	- `DiscussionService.GetThreadsAsync`

### Validation Summary
- `Scripts/03-FullDummyData.sql` dataset marker updated to `FullDummyData-v21`.
- Discussion demo seed expanded with additional offering-scoped threads/replies for filter demo/testing.
- `Scripts/05-PostDeployment-Checks.sql` now validates v21 marker and offering-specific discussion coverage counts.

## 2026-05-29 Update - Discussion Demo Seed v20 and Schema Sync

### Runtime additions
- No new C# application-layer function signature was introduced in this synchronization slice.
- Existing Discussion runtime surfaces remain the active behavior path (no duplicate function inventory entries added):
	- `PortalController.Discussion`
	- `PortalController.DiscussionThreadDetail`
	- `EduApiClient.GetDiscussionThreadsAsync`
	- `DiscussionController.GetThreads`
	- `DiscussionService.GetThreadsAsync`

### Validation Summary
- `Scripts/01-Schema-Current.sql` now includes Phase 31 Discussion enhancement migration coverage (`ThreadType`, `IssueSubType`, `IsSolved`, `ResolvedBy`, `ResolvedAt`, `TicketNumber`, `IsVisibleToAll`) and related indexes.
- `Scripts/03-FullDummyData.sql` upgraded dataset marker to `FullDummyData-v20` and expanded Discussion sample rows with mixed states (pinned/open, FAQ, solved/closed) for demo/testing.
- `Scripts/05-PostDeployment-Checks.sql` upgraded to v20 marker assertion and added resolved-discussion count verification.

## 2026-05-29 Update - LMS Demo Seed v18 Function Inventory Sync

### Runtime additions
- No new application-layer function signature was introduced in this update.
- Existing LMS runtime surfaces continue to be used:
	- `PortalController.LmsManage`
	- `EduApiClient.GetLmsModulesAsync`
	- `LmsController.GetModules`

### Validation Summary
- `Scripts/03-FullDummyData.sql` upgraded to `FullDummyData-v18` with LMS offering-513 draft/published module scenarios and additional videos.
- `Scripts/05-PostDeployment-Checks.sql` now validates LMS module/video coverage for offering 513.

## 2026-05-29 Update - Demo Seed Expansion (Rubric/Gradebook)

### Runtime additions
- No new application-layer function signature was added in this synchronization slice.
- Existing `GradebookService.GetGradebookAsync` fallback behavior is now backed by additional demo rows in SQL seed data for validation scenarios.

### Validation Summary
- Extended `Scripts/03-FullDummyData.sql` with offering `55555555-5555-5555-5555-555555555513` sample submissions and `Practical` result rows.
- Extended `Scripts/05-PostDeployment-Checks.sql` with rubric submission and offering-513 practical-result verification checks.

## 2026-05-29 Update - Institution-Aware Gradebook Function Inventory

### Runtime additions
| Function / Method | Purpose | File |
|---|---|---|
| IGradebookRepository.GetInstitutionTypeForOfferingAsync | Resolves offering-specific institution mode so gradebook output switches between GPA/CGPA and percentage behavior. | src/Tabsan.EduSphere.Domain/Interfaces/IGradebookRepository.cs |
| GradebookRepository.GetInstitutionTypeForOfferingAsync | Reads institution type from the selected course offering in one scoped query. | src/Tabsan.EduSphere.Infrastructure/Repositories/GradebookRubricRepositories.cs |
| IResultRepository.GetActiveComponentRulesAsync(InstitutionType, ...) | Returns active result components for the requested institution scope. | src/Tabsan.EduSphere.Domain/Interfaces/IResultRepository.cs |
| ResultRepository.GetActiveComponentRulesAsync(InstitutionType, ...) | Applies institution filtering for active component rules and keeps legacy fallback behavior. | src/Tabsan.EduSphere.Infrastructure/Repositories/AssignmentResultRepositories.cs |
| GradebookService.GetGradebookAsync | Builds institution-aware aggregates: University weighted-total to GPA + CGPA, School/College percentage; includes fallback component derivation from existing result types when configured rules are missing. | src/Tabsan.EduSphere.Application/Assignments/GradebookService.cs |

### Validation Summary
- Unit test suite passed (`197/197`) after repository-contract updates in test stubs.
- Runtime verification confirmed mixed behavior on seeded offerings:
	- university offering renders GPA and CGPA columns,
	- school/college offerings render Percentage column.

## 2026-05-28 Update - Enter Results Acceptance Criteria Function Closure

### Runtime additions
- No new runtime function introduced in this closure phase.
- Closure references existing runtime surfaces that satisfy acceptance criteria:
	- `PortalController.CreateResult`, `PortalController.ImportResultCsv`, `PortalController.PublishAllResults`, `PortalController.CorrectResult`, `PortalController.DownloadResultImportReport`.
	- `ResultsPageModel.CanWriteResults` and `CanPublishResults` for UI/action gating.

### Validation Summary
- Focused result-governance unit slice passed (`9/9`).
- Report-token web integration passed (`3/3`).
- Sidebar integration passed (`17/17`).

## 2026-05-28 Update - Enter Results Non-Functional Function Inventory

### Runtime additions
- `PortalController.CorrectResult` now applies `ValidateResultWriteScopeAsync` for consistent write-scope enforcement.
- `PortalController.CreateResult` and `PortalController.CorrectResult` now add defensive server-side mark-range validation messaging.
- `PortalController.CreateResult`, `PortalController.CorrectResult`, and `PortalController.PublishAllResults` now emit structured operational logs for success and blocked outcomes.

### Validation Summary
- Web build passed after non-functional hardening.
- Focused result-governance unit suite remained green (`9/9`).

## 2026-05-28 Update - Enter Results Test Requirements Function Inventory

### Runtime additions
- No new runtime endpoint introduced in this slice.
- Added focused unit test surface in `ResultServiceGovernanceTests` for:
	- `ResultService.CorrectAsync` draft-correction guard.
	- `ResultService.CorrectAsync` correction-reason audit propagation.
	- `ResultService.PublishAllForOfferingAsync` draft-only publish behavior.

### Validation Summary
- Focused result-governance unit suites passed (`9/9`).
- Web build passed after test-requirement expansion.

## 2026-05-28 Update - Enter Results Publishing Rules Function Inventory

### Runtime additions
- `ResultsPageModel.CanPublishResults` and `PublishResultDisabledReason` added for publish-role governance state.
- `PortalController.PublishAllResults` hardened with explicit Admin/SuperAdmin approval gating in web flow.
- `CorrectResultRequest` expanded to include correction `Reason` for audit payload capture.
- `Result.CorrectMarks` now enforces published-only correction invariant.
- `ResultService.CorrectAsync` now captures correction reason in audit metadata and blocks correction of draft rows.

### Validation Summary
- Web build passed after publishing-rule hardening.
- Focused unit tests passed (`6/6`) for publish-role and correction invariants.

## 2026-05-28 Update - Enter Results Phase 5 Function Inventory

### Runtime additions
- No new controller actions or API endpoints were introduced in this phase.
- Existing `ResultsPageModel.CanWriteResults` and `SaveResultDisabledReason` continue to drive write-action eligibility and UI guidance state.

### Validation Summary
- Web build passed after phase 5 result-entry table behavior update.
- Focused unit coverage added for result write-guard eligibility conditions.

## 2026-05-28 Update - Enter Results Phase 2 Filter Criteria Inventory

### Phase 2 Implementation Summary
- Phase 2 execution is filter-behavior and entry-scope enforcement alignment for Enter Results.
- Existing Enter Results runtime entry surface remains `PortalController.EnterResults` and existing Results flow contracts.
- No new controller, service, repository, or API function signature was introduced in this phase checkpoint.

### Phase 2 Validation Summary
- Function inventory reviewed and confirmed no additional runtime function entries are required for this phase.
- Existing Enter Results function entries from Phase 0/1 remain the authoritative runtime references.

## 2026-05-28 Update - Enter Results Phase 1 and Phase 2 Runtime Function Inventory

### Runtime additions
- `PortalController.DownloadResultCsvTemplate` added for Enter Results template generation and download.
- `PortalController.ImportResultCsv` added for Enter Results CSV import with strict/non-strict validation behavior.
- `PortalController.ValidateResultWriteScopeAsync` added for server-side required-filter and scope enforcement before result write actions.

### Validation Summary
- Web build passed with new Enter Results runtime function surfaces.
- Existing sidebar/menu integration matrix remained green.

## 2026-05-28 Update - Enter Results Phase 3 Template Function Inventory

### Phase 3 Implementation Summary
- `PortalController.DownloadResultCsvTemplate` now emits two explicit example rows in template CSV output.
- `PortalController.ImportResultCsv` now skips template example rows so they are not treated as production writes.

### Phase 3 Validation Summary
- Web build passed after template/import guidance-row behavior updates.

## 2026-05-28 Update - Enter Results Phase 4 Function Inventory

### Runtime additions
- `PortalController.DownloadResultImportReport` added for one-time, expiring import-report download.
- `PortalController.ImportResultCsv` extended with upload-audit emission and row-level report-token generation.
- `PortalController.ResultImportReports` and `ResultImportReportTtl` added for result report-token retention controls.

### Validation Summary
- Result import report web route integration tests passed (`3/3`).
- Web build passed after phase 4 runtime additions.

## 2026-05-28 Update - Enter Results Phase 0/1 Initial Inventory

### Phase 0/1 Implementation Summary
- Added governed sidebar key `enter_results` in menu seeding and role-access defaults.
- Added `PortalController.EnterResults` as dedicated entry route for Enter Results menu navigation.
- Added sidebar module-key mapping for `enter_results` to results-module entitlement behavior.

### Phase 0/1 Validation Summary
- Sidebar integration matrix passed with `enter_results` coverage.
- Full solution build passed.

## 2026-05-28 Update - Enter Attendance Phase 11 Integration Expectations Inventory

### Phase 11 Implementation Summary
- No new runtime function signatures were introduced in this closure slice.
- Confirmed existing Enter Attendance, reporting, and authorization function surfaces remain behaviorally consistent under cross-module regression checks.

### Phase 11 Validation Summary
- Attendance-focused unit matrix passed (`20/20`).
- Web route integration suite passed (`3/3`).
- Integration regression batch (Sidebar, Authorization, Report Exports, Parent Portal) passed (`117/117`).
- Full solution build passed.

## 2026-05-28 Update - Enter Attendance Phase 9 Database Compliance Inventory

### Phase 9 Implementation Summary
- Added attendance index hardening in `Scripts/04-Maintenance-Indexes-And-Views.sql` for offering/date, student, and unique student/offering/date safeguards.
- Added attendance index existence checks in `Scripts/05-PostDeployment-Checks.sql` and `Scripts/05-PostDeployment-Checks-Clean.sql`.
- Confirmed duplicate prevention at database level remains enforced through unique attendance composite key.

### Phase 9 Validation Summary
- Solution build passed.
- Attendance-focused unit matrix passed (`20/20`).
- Sidebar integration regression slice passed (`17/17`).

## 2026-05-28 Update - Enter Attendance Phase 8 Compliance Inventory

### Phase 8 Implementation Summary
- Confirmed attendance enhancements remain confined to attendance feature surface and existing architecture patterns.
- Confirmed existing sidebar governance and role restrictions remained unchanged.
- Confirmed tenant/campus/department/course/semester scope checks remain active in attendance write paths.

### Phase 8 Validation Summary
- Attendance-focused unit matrix passed (`20/20`).
- Web route integration suite passed (`3/3`).
- Sidebar integration regression slice passed (`17/17`).
- Full solution build passed.

## 2026-05-28 Update - Enter Attendance Phase 7 Validation Inventory

### Phase 7 Implementation Summary
- Hardened `PortalController.BulkMarkAttendance` manual-entry validation for empty submissions.
- Added duplicate Student+Date row detection for manual row submissions.
- Enforced required per-row dates before attendance batch writes.

### Phase 7 Validation Summary
- Attendance-focused unit matrix passed (`20/20`).
- Sidebar integration regression slice passed (`17/17`).

## 2026-05-28 Update - Enter Attendance Phase 6 Table Structure Inventory

### Phase 6 Implementation Summary
- Updated attendance row-entry table rendering to Student ID, Student Name, Date, and Present fields.
- Added generic disabled row-table structure for incomplete filter/offering state.
- Updated `PortalController.BulkMarkAttendance` to support and group per-row date submissions.

### Phase 6 Validation Summary
- Attendance-focused unit matrix passed (`18/18`).
- Sidebar integration regression slice passed (`17/17`).

## 2026-05-28 Update - Enter Attendance Phase 5 UI Save Guard Inventory

### Phase 5 Implementation Summary
- Added `AttendancePageModel` required-filter save-state helpers for Enter Attendance write actions.
- Updated attendance view to disable save/import controls until required filters are selected.
- Added focused unit tests for required-filter save-state behavior.

### Phase 5 Validation Summary
- Attendance-focused unit matrix passed (`16/16`).
- Sidebar integration regression slice passed (`17/17`).

## 2026-05-28 Update - Enter Attendance Phase 4.5 Web Integration Inventory

### Phase 4.5 Implementation Summary
- Added `Tabsan.EduSphere.WebIntegrationTests` project for real web-route verification.
- Added `WebEntryPointMarker` for MVC-host integration bootstrapping.
- Added route-level tests for `PortalController.DownloadAttendanceImportReport` valid, invalid, and expired token behavior.

### Phase 4.5 Validation Summary
- Attendance import unit matrix passed (`14/14`).
- Web integration route suite passed (`3/3`).
- Sidebar integration regression slice passed (`17/17`).

## 2026-05-28 Update - Enter Attendance Phase 4.4 UX Inventory

### Phase 4.4 Implementation Summary
- Added user-facing hint text near import report download action in attendance view.
- No controller/action contract changes.

### Phase 4.4 Validation Summary
- Attendance import unit matrix passed (`14/14`).
- Sidebar integration regression slice passed (`17/17`).

## 2026-05-28 Update - Enter Attendance Phase 4.3 Retention Inventory

### Phase 4.3 Implementation Summary
- Added report TTL and clock-provider controls for attendance import report lifecycle.
- Hardened report download endpoint to differentiate unavailable versus expired tokens.

### Phase 4.3 Validation Summary
- Attendance import unit matrix passed (`14/14`).
- Sidebar integration regression slice passed (`17/17`).

## 2026-05-28 Update - Enter Attendance Phase 4.2 Report Export Inventory

### Phase 4.2 Implementation Summary
- Extended `PortalController.ImportAttendanceCsv` to create report-token-linked CSV result artifacts.
- Added `PortalController.DownloadAttendanceImportReport` endpoint and attendance view model token wiring.

### Phase 4.2 Validation Summary
- Attendance import unit matrix passed (`12/12`).
- Sidebar integration regression slice passed (`17/17`).

## 2026-05-28 Update - Enter Attendance Phase 4.1 Audit Inventory

### Phase 4.1 Implementation Summary
- Added upload-audit metadata writing inside `PortalController.ImportAttendanceCsv`.
- Added logger dependency to `PortalController` and updated unit-test constructor call sites.

### Phase 4.1 Validation Summary
- Focused portal controller unit suite passed (`14/14`).
- Sidebar integration regression slice passed (`17/17`).

## 2026-05-28 Update - Enter Attendance Phase 3 Import UX Inventory

### Phase 3 Implementation Summary
- Extended `PortalController.ImportAttendanceCsv` with strict-mode and row-level validation detail handling.
- Extended `AttendancePageModel` and attendance view to render import detail messages for CSV row feedback.

### Phase 3 Validation Summary
- Focused attendance matrix passed with strict-mode and warning-surface coverage (`9/9`).
- Targeted sidebar integration suite passed (`17/17`).

## 2026-05-28 Update - Enter Attendance Phase 2 Filter Enforcement Inventory

### Phase 2 Implementation Summary
- Added attendance filter-state fields in `AttendancePageModel` for Department, Course, and Class/Semester selection.
- Added filter-context enforcement helper in `PortalController` for attendance write operations.
- Added expanded unit-test matrix coverage for missing-filter guard behavior.

### Phase 2 Validation Summary
- Focused attendance test matrix passed (`8/8`).
- Targeted sidebar integration suite passed (`17/17`).

## 2026-05-28 Update - Enter Attendance Roster Scope Hardening Inventory

### Phase 1 Implementation Summary
- Added roster-scope enforcement in `PortalController.BulkMarkAttendance` and `PortalController.CorrectAttendance`.
- Added focused unit-test coverage in `PortalAttendanceCsvImportTests` for CSV matrix and manual scope guard paths.

### Phase 1 Validation Summary
- Focused unit test suite passed (`7/7`) for attendance CSV and roster-scope enforcement behavior.
- Targeted sidebar integration suite remained green (`17/17`).

## 2026-05-28 Update - Enter Attendance CSV Function Inventory

### Phase 1 Implementation Summary
- Added `PortalController.DownloadAttendanceCsvTemplate` to generate/download the CSV template.
- Added `PortalController.ImportAttendanceCsv` to validate and import CSV attendance rows through existing bulk-mark paths.

### Phase 1 Validation Summary
- Web project build passed after CSV action and view wiring implementation.

## 2026-05-28 Update - Enter Attendance Phase 1 Start Function Inventory

### Phase 1 Implementation Summary
- Added `PortalController.EnterAttendance` as the dedicated portal entry action for the new `enter_attendance` menu.
- Updated `DatabaseSeeder.SeedSidebarMenusAsync(...)` so the new sidebar menu is seeded with Admin/Faculty access while keeping Student excluded.

### Phase 1 Validation Summary
- Focused sidebar integration coverage passed for the new menu visibility matrix.
- Function inventory is updated only for runtime surface introduced in this slice; CSV import functions remain pending.

## 2026-05-28 Update - Enter Attendance Phase 0 Function Inventory Note

### Implementation Summary
- Phase 0 for **Enter Attendance** is documentation-only and introduces no new runtime functions, controllers, services, repositories, or portal actions yet.
- The governed menu key planned for later implementation is `enter_attendance` with default access restricted to **SuperAdmin**, **Admin**, and **Faculty**.

### Validation Summary
- Function inventory intentionally remains unchanged for Phase 0 to avoid duplicating planned behavior as implemented runtime surface.
- Runtime function entries will be added in later phases when API, web, application, or infrastructure code is introduced.

| Function Name | Purpose | Location |
|--------------|--------|----------|
| ConfigurationBootstrapper.AddEduSphereConfigurationHierarchy | Loads layered configuration including parent/root `environments.json`, optional external profile file, and environment-variable overlays in deterministic order. | src/Tabsan.EduSphere.Application/Services/ConfigurationBootstrapper.cs |
| ConfigurationBootstrapper.InsertJsonSourceIfMissing | Adds JSON config sources idempotently and now supports absolute-path files through a physical file provider for reliable profile loading. | src/Tabsan.EduSphere.Application/Services/ConfigurationBootstrapper.cs |
| ResultCalculationController.Get | Returns institute-scoped result calculation settings using `institutionType` filter with safe university fallback. | src/Tabsan.EduSphere.API/Controllers/ResultCalculationController.cs |
| ResultCalculationController.Save | Validates institute type and persists institute-scoped GPA/component rules. | src/Tabsan.EduSphere.API/Controllers/ResultCalculationController.cs |
| ResultCalculationService.GetSettingsAsync | Retrieves institute-scoped GPA and component rules for result calculation settings. | src/Tabsan.EduSphere.Application/Assignments/ResultCalculationService.cs |
| ResultCalculationService.SaveSettingsAsync | Validates and saves institute-scoped result-calculation components and GPA rules with weight/threshold checks. | src/Tabsan.EduSphere.Application/Assignments/ResultCalculationService.cs |
| ResultRepository.GetAllComponentRulesAsync(InstitutionType, ...) | Returns all result component rules for the selected institution scope. | src/Tabsan.EduSphere.Infrastructure/Repositories/AssignmentResultRepositories.cs |
| ResultRepository.GetGpaScaleRulesAsync(InstitutionType, ...) | Returns GPA scale rules for the selected institution scope. | src/Tabsan.EduSphere.Infrastructure/Repositories/AssignmentResultRepositories.cs |
| ResultRepository.ReplaceCalculationRulesAsync(InstitutionType, ...) | Replaces existing result-calculation rules for only the selected institution scope. | src/Tabsan.EduSphere.Infrastructure/Repositories/AssignmentResultRepositories.cs |
| PortalController.GetCurrentUserId | Resolves current portal user id from claims for 2FA and secure user-context workflows. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| AnnouncementController.CreateAnnouncement | Normalizes invalid offering and runtime validation failures into consistent `400` responses instead of leaking unhandled API exceptions. | src/Tabsan.EduSphere.API/Controllers/AnnouncementController.cs |
| GraduationService.RejectInternalAsync | Converts optimistic-concurrency conflicts in graduation rejection flow into deterministic business error messaging for safe retries. | src/Tabsan.EduSphere.Application/Academic/GraduationService.cs |
| LmsService.CreateModuleAsync | Guards LMS module creation against invalid/offline offerings at both pre-check and save stages to avoid FK-driven `500` responses. | src/Tabsan.EduSphere.Application/Lms/LmsService.cs |
| PortalController.CreateAnnouncement | Blocks announcement posting when no valid offering is selected and returns a user-safe validation message. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| FypRepository.GetAllAsync(...) | Uses direct awaited EF execution (no `ContinueWith`) to avoid DbContext second-operation runtime faults in high-load FYP reads. | src/Tabsan.EduSphere.Infrastructure/Repositories/QuizFypRepositories.cs |
| FypPanelRole.Internal / FypPanelRole.External | Backward-compatible enum aliases that safely map legacy panel-role database string values. | src/Tabsan.EduSphere.Domain/Fyp/FypProject.cs |
| AddHealthChecks | Registers database, memory, CPU, network, and error-rate checks for continuous runtime health monitoring. | src/Tabsan.EduSphere.API/Program.cs |
| AddOpenTelemetry | Publishes ASP.NET Core, HttpClient, runtime, and process metrics and exposes Prometheus scraping support. | src/Tabsan.EduSphere.API/Program.cs |
| AddResponseCompression (API) | Keeps Brotli/Gzip compression enabled with Fastest level for HTTPS responses. | src/Tabsan.EduSphere.API/Program.cs |
| AddResponseCompression (Web) | Keeps Brotli/Gzip compression enabled with Fastest level for HTTPS responses. | src/Tabsan.EduSphere.Web/Program.cs |
| PortalController.BeginTwoFactorSetup | Starts the portal-side 2FA setup flow and reloads the settings page with QR/manual-key payloads. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.DisableTwoFactor | Disables portal-side 2FA and returns the user to the 2FA settings page with status messaging. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.TestTwoFactorLogin | Tests the portal-side 2FA login hand-off and returns the user to the 2FA settings page with status messaging. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.TwoFactorSettings | Renders the portal-side 2FA settings page and binds the current user context. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.VerifyTwoFactorSetup | Confirms the portal-side 2FA setup code and returns status messaging on the settings page. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| TwoFactorController.Disable | Disables add-on 2FA after validating the current TOTP code. | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TwoFactorController.LoginVerify | Verifies a pending login TOTP hand-off for the add-on 2FA flow. | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TwoFactorController.Setup | Starts add-on 2FA enrollment for the current signed-in user. | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TwoFactorController.Verify | Confirms add-on 2FA setup with the initial TOTP code. | src/Tabsan.EduSphere.API/Controllers/TwoFactorController.cs |
| TwoFactorService.BuildProvisioningUri | Builds an authenticator provisioning URI using the configured issuer and TOTP settings. | src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorService.cs |
| TwoFactorService.GenerateSecret | Generates a fresh Base32 TOTP secret for the add-on setup flow. | src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorService.cs |
| TwoFactorService.ValidateCode | Validates a TOTP code using the configured time-step and drift window. | src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorService.cs |
| TwoFactorSetupService.BeginSetupAsync | Starts 2FA enrollment, persists the protected secret, and returns QR/manual-key payloads. | src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorSetupService.cs |
| TwoFactorSetupService.DisableAsync | Disables 2FA after validating the current code against the stored secret. | src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorSetupService.cs |
| TwoFactorSetupService.VerifyLoginAsync | Verifies the login hand-off code for the add-on 2FA challenge. | src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorSetupService.cs |
| TwoFactorSetupService.VerifySetupAsync | Confirms the initial enrollment code before enabling 2FA. | src/Tabsan.EduSphere.API/Services/TwoFactor/TwoFactorSetupService.cs |
| AdminUserController.Create | Accepts optional `institutionType`, validates against active policy, persists to user, and returns assignment in response/list payloads. | src/Tabsan.EduSphere.API/Controllers/AdminUserController.cs |
| AiChatService.GetConversationAsync | Returns a full conversation thread with message history for the requesting user. | src/Tabsan.EduSphere.Application/AiChat/AiChatService.cs |
| AiChatService.GetConversationsAsync | Returns the requesting user's conversation list for the AI chat experience. | src/Tabsan.EduSphere.Application/AiChat/AiChatService.cs |
| AiChatService.SendMessageAsync | Sends a user message to the AI provider and persists the response in conversation history. | src/Tabsan.EduSphere.Application/AiChat/AiChatService.cs |
| AnalyticsController.GetPaymentStatus | Accepts and forwards course/semester filters for payment analytics requests. | src/Tabsan.EduSphere.API/Controllers/AnalyticsController.cs |
| AnalyticsService.BuildAnalyticsCacheKey(...) | Enforces cache scope boundaries by keying shared analytics cache entries to report type and department scope. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetAssignmentStatsAsync(...) | Adds short-TTL distributed cache policy for expensive assignment analytics reads and supports course/semester-scoped filtering. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetAttendanceReportAsync(...) | Adds short-TTL distributed cache policy for expensive attendance analytics reads and supports course/semester-scoped filtering. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetPaymentStatusReportAsync(...) | Aggregates scoped paid vs unpaid receipt counts/amounts and supports course/semester-scoped filtering. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetPerformanceReportAsync(...) | Adds short-TTL distributed cache policy for expensive department/all performance analytics reads and supports course/semester scope. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| AnalyticsService.GetQuizStatsAsync(...) | Adds short-TTL distributed cache policy for expensive quiz analytics reads. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| BuildingRoomRepository.GetAllBuildingsAsync(...) | Returns building lists with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/BuildingRoomRepository.cs |
| BuildingRoomRepository.GetAllRoomsAsync(...) | Returns room lists with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/BuildingRoomRepository.cs |
| BuildingRoomRepository.GetRoomsByBuildingAsync(...) | Returns building-scoped room lists with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/BuildingRoomRepository.cs |
| CanAccessExportJob | Enforces owner-or-superadmin and tenant/campus scope parity checks for export job access. | src/Tabsan.EduSphere.API/Controllers/AnalyticsController.cs |
| CourseController.GetOfferings | Exposes department metadata for deterministic dependent filtering in web layer. | src/Tabsan.EduSphere.API/Controllers/CourseController.cs |
| CourseMaterial.EnsureMaterialLocation | Enforces material-type-specific file/link requirements before persistence. | src/Tabsan.EduSphere.Domain/Lms/CourseMaterial.cs |
| CourseMaterial.EnsureRequiredScope | Prevents creation of unscoped material records by rejecting empty scope identifiers. | src/Tabsan.EduSphere.Domain/Lms/CourseMaterial.cs |
| CourseMaterial.UpdateLocation | Updates material storage location for file/link and validates required location by material type. | src/Tabsan.EduSphere.Domain/Lms/CourseMaterial.cs |
| CourseMaterial.UpdateMetadata | Updates material title and description metadata. | src/Tabsan.EduSphere.Domain/Lms/CourseMaterial.cs |
| CourseMaterialController.BuildScopedStorageCategory | Builds tenant/campus-aware storage category path for Course Material upload isolation. | src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs |
| CourseMaterialController.Create | Creates scoped material records for authorized Faculty/Admin/SuperAdmin users using caller identity. | src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs |
| CourseMaterialController.DownloadFile | Streams stored material files with metadata-aware content type and file name for authorized scoped users. | src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs |
| CourseMaterialController.GetAll | Returns course materials with strict repository-enforced tenant/campus filtering. | src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs |
| CourseMaterialController.Upload | Uploads validated material files and persists them in scoped storage for Faculty/Admin/SuperAdmin users. | src/Tabsan.EduSphere.API/Controllers/CourseMaterialController.cs |
| CourseMaterialRepository.ApplyTenantCampusScope | Enforces strict tenant/campus query isolation with SuperAdmin bypass behavior. | src/Tabsan.EduSphere.Infrastructure/Repositories/CourseMaterialRepository.cs |
| CourseMaterialRepository.IsScopeMissingForNonSuperAdmin | Short-circuits read operations early when tenant scope is missing for non-SuperAdmin callers. | src/Tabsan.EduSphere.Infrastructure/Repositories/CourseMaterialRepository.cs |
| DashboardCompositionController.GetContext(...) | Provides role- and policy-aware module/vocabulary/widget composition by aggregating visible modules, academic vocabulary, and widgets into one dashboard-context response. | src/Tabsan.EduSphere.API/Controllers/DashboardCompositionController.cs |
| DashboardCompositionService.GetWidgets(...) | Adds short-TTL in-memory cache for dashboard widget composition keyed by role and institution policy state to reduce repeated composition cost. | src/Tabsan.EduSphere.Application/Services/DashboardCompositionService.cs |
| DatabaseConnectionResolver.ResolveDefaultConnection | Resolves DB connection string from prioritized environment/deployment keys with backward-compatible fallback to legacy connection-string key. | src/Tabsan.EduSphere.Application/Services/DatabaseConnectionResolver.cs |
| DatabaseSeeder.SeedRolesAsync | Seeds `Finance` role additively alongside existing system roles during startup bootstrap. | src/Tabsan.EduSphere.Infrastructure/Persistence/DatabaseSeeder.cs |
| DatabaseSeeder.SeedSidebarMenusAsync(...) | Makes sidebar role seeding self-healing by updating existing role-access values, including the new `enter_attendance` menu, so corrected visibility rules are enforced on already-seeded databases. | src/Tabsan.EduSphere.Infrastructure/Persistence/DatabaseSeeder.cs |
| PortalController.EnterAttendance | Opens the new Enter Attendance menu route while reusing the current attendance screen and preserving guarded access. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.DownloadAttendanceCsvTemplate | Generates and downloads the Enter Attendance CSV template with required headers and sample rows. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.ImportAttendanceCsv | Validates Enter Attendance CSV rows and imports attendance using existing bulk-mark API calls grouped by date. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.WriteAttendanceImportAudit (local function) | Emits per-upload attendance CSV audit trail details with actor/time/strict-mode/row counts/error reasons. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.DownloadAttendanceImportReport | Serves one-time downloadable CSV import result reports using report tokens. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.AttendanceImportReportTtl / UtcNowProvider | Controls report retention window and deterministic time evaluation for token expiry decisions. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| AttendancePageModel.MessageDetails | Carries row-level CSV import feedback details for attendance UI rendering. | src/Tabsan.EduSphere.Web/Models/Portal/PortalViewModels.cs |
| AttendancePageModel.ImportReportToken | Carries report token used to render last import report download action in Enter Attendance UI. | src/Tabsan.EduSphere.Web/Models/Portal/PortalViewModels.cs |
| Attendance.cshtml import hint text | Explains one-time and 2-hour expiry behavior for import report links in UI. | src/Tabsan.EduSphere.Web/Views/Portal/Attendance.cshtml |
| PortalController.BulkMarkAttendance | Enforces roster-scoped student validation and normalized attendance statuses before submitting manual attendance entries. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.CorrectAttendance | Enforces roster-scoped student validation and normalized status values before submitting attendance corrections. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.ValidateAttendanceWriteScopeAsync | Validates selected offering against selected Department, Course, and Class/Semester under effective tenant/campus scope before attendance write operations. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| Department.SetTenantCampus(tenantId, campusId) | Assigns or clears tenant/campus ownership for department-level scoping while preserving InstitutionType behavior. | src/Tabsan.EduSphere.Domain/Academic/Department.cs |
| DepartmentRepository.ApplyTenantCampusScope | Applies tenant/campus query filtering for department reads with SuperAdmin bypass. | src/Tabsan.EduSphere.Infrastructure/Repositories/DepartmentRepository.cs |
| DeploymentTopologyResolver.Resolve | Resolves effective deployment mode, customer identity, domain, database name, and scaling settings from config and environment variables. | src/Tabsan.EduSphere.Application/Services/DeploymentTopologyResolver.cs |
| EduApiClient.BuildAnalyticsQuery | Builds analytics query string including optional `courseId` and `semesterId` filters. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GetDashboardCompositionContextAsync(...) | Deserializes the aggregated dashboard-context endpoint response into a single Web client model. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GetPaymentStatusAnalyticsAsync | Retrieves payment status analytics data for portal snapshots. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GetPaymentSummaryReportAsync | Retrieves payment summary report data for the portal. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.MapPayment | Consumes payment payload with compatibility fallback (`PaidDate ?? ConfirmedAt`) and update trail mapping. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.UpdatePaymentAsync | Web client method that calls the finance payment edit endpoint. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EnsureDefaultTenantCampusAsync | Guarantees default tenant (`DEFAULT`) and default campus (`MAIN`) are present and active at startup. | src/Tabsan.EduSphere.Infrastructure/Persistence/DatabaseSeeder.cs |
| EnsureTenantCampusBackfillAsync | Performs startup safety backfill for users/departments missing tenant/campus assignments. | src/Tabsan.EduSphere.Infrastructure/Persistence/DatabaseSeeder.cs |
| GetAnalyticsAccessScope | Resolves effective analytics access scope from caller tenant/campus claims with superadmin bypass handling. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| GetAssignmentStatsAsync | Computes assignment statistics from grouped submission/enrollment snapshots instead of per-assignment query loops. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| GetComparativeSummaryAsync | Computes comparative summary metrics via department-grouped batched queries. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| GetCurrentTenantId | Resolves caller tenant/campus claim scope used for export-job ownership checks. | src/Tabsan.EduSphere.API/Controllers/AnalyticsController.cs |
| GetPerformanceReportAsync | Builds performance report using batched results/submissions aggregation instead of per-student round-trips. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| GetQuizStatsAsync | Aggregates quiz attempt statistics in a grouped pass rather than per-quiz queries. | src/Tabsan.EduSphere.Infrastructure/Analytics/AnalyticsService.cs |
| GradebookService.GetGradebookAsync(...) | Removes sync-over-async `.Result` consumption from the gradebook request path by awaiting completed tasks. | src/Tabsan.EduSphere.Application/Assignments/GradebookService.cs |
| HttpTenantScopeResolver.GetTenantScopeKey() | Resolves tenant scope from JWT claims or `X-Tenant-Code` request header for API-request-scoped operations. | src/Tabsan.EduSphere.API/Services/HttpTenantScopeResolver.cs |
| IAnalyticsService.GetPaymentStatusReportAsync(..., courseId, semesterId) | Exposes filter-aware payment analytics contract including course/semester dimensions. | src/Tabsan.EduSphere.Application/Interfaces/IAnalyticsService.cs |
| IEduApiClient.DownloadCourseMaterialFileAsync | Downloads Course Material files from API for portal-proxied file delivery. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.GetCourseMaterialsAsync | Fetches scoped course materials for portal pages with optional active-only filtering. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.GetDashboardCompositionContextAsync(...) | Fetches the aggregated dashboard-context payload in a single API request for the web portal. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.UploadCourseMaterialFileAsync | Uploads Course Material files from portal to API multipart endpoint. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEnrollmentRepository.GetWaitlistedByOfferingAsync | Returns waitlisted enrollments in queue order so promotion can be deterministic. | src/Tabsan.EduSphere.Domain/Interfaces/IEnrollmentRepository.cs |
| INotificationRepository.GetForUserAsync(..., asNoTracking, ...) | Adds opt-in no-tracking control for read-heavy inbox retrieval while preserving tracked reads for mark-all-read operations. | src/Tabsan.EduSphere.Domain/Interfaces/INotificationRepository.cs |
| InstitutionPolicyController.Save | Used by SuperAdmin matrix run to switch School/College/University modes and validate privileged access continuity. | src/Tabsan.EduSphere.API/Controllers/InstitutionPolicyController.cs |
| InstitutionPolicyService.SavePolicyAsync | Persists institution policy flags to `portal_settings` by committing settings repository changes, enabling mode switches from uploaded licenses to be retained. | src/Tabsan.EduSphere.Application/Services/InstitutionPolicyService.cs |
| IReportRepository.GetPaymentSummaryDataAsync | Queries filtered payment receipt reporting rows with tenant/campus-safe scope and academic dimensions. | src/Tabsan.EduSphere.Domain/Interfaces/IReportRepository.cs |
| IStudentLifecycleService.UpdatePaymentReceiptAsync | Exposes finance receipt edit capability through the application contract. | src/Tabsan.EduSphere.Application/Interfaces/IStudentLifecycleService.cs |
| ITenantScopeResolver.GetTenantScopeKey() | Provides a tenant-scope abstraction for application-layer tenant-aware key resolution. | src/Tabsan.EduSphere.Application/Interfaces/ITenantScopeResolver.cs |
| LibraryService.BuildLoanLookupCacheKey(...) | Keys external-call cache entries by student identifier and integration configuration fingerprint to avoid stale cross-config reuse. | src/Tabsan.EduSphere.Application/Services/LibraryService.cs |
| LibraryService.GetLoansAsync(...) | Adds short-TTL distributed cache for safe external loan lookup reads to reduce repeated dependency calls. | src/Tabsan.EduSphere.Application/Services/LibraryService.cs |
| NotificationRepository.GetActiveUserPhoneNumbersAsync | Resolves distinct active recipient phone numbers by user IDs for SMS dispatch. | src/Tabsan.EduSphere.Infrastructure/Repositories/NotificationAttendanceRepositories.cs |
| NotificationRepository.GetForUserAsync(..., asNoTracking, ...) | Applies optional AsNoTracking to notification recipient + parent notification read path. | src/Tabsan.EduSphere.Infrastructure/Repositories/NotificationAttendanceRepositories.cs |
| NotificationRepository.GetUnreadCountAsync(...) | Removes unnecessary Include from unread count query to avoid extra query shaping overhead. | src/Tabsan.EduSphere.Infrastructure/Repositories/NotificationAttendanceRepositories.cs |
| NotificationService.BumpCacheVersion() | Invalidates notification read-cache windows by incrementing cache version after notification mutations. | src/Tabsan.EduSphere.Application/Notifications/NotificationService.cs |
| NotificationService.GetBadgeAsync(...) | Adds short-TTL in-memory cache for unread badge counts with cache-version keying. | src/Tabsan.EduSphere.Application/Notifications/NotificationService.cs |
| NotificationService.GetInboxAsync(...) | Adds short-TTL in-memory cache for paged inbox reads with cache-version keying for mutation-safe freshness. | src/Tabsan.EduSphere.Application/Notifications/NotificationService.cs |
| PaymentReceipt.UpdateDetails | Updates actionable receipt fields while preserving audit trail and blocking edits on Paid/Cancelled receipts. | src/Tabsan.EduSphere.Domain/StudentLifecycle/PaymentReceipt.cs |
| PaymentReceiptController.Delete | Explicitly rejects receipt deletion requests with `405` to preserve immutable payment history. | src/Tabsan.EduSphere.API/Controllers/PaymentReceiptController.cs |
| PaymentReceiptController.Update | API endpoint for finance/admin users to edit actionable payment receipts. | src/Tabsan.EduSphere.API/Controllers/PaymentReceiptController.cs |
| PortalController.BuildAnalyticsPageModelAsync | Loads payment status analytics into analytics page/snapshot model and summary cards. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.CreateCourseMaterial | Creates a course material from the portal manage page while preserving current filter state. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.DownloadCourseMaterialFile | Proxies material file download from API and preserves student/manage redirect context on failure. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.SetCourseMaterialActive | Toggles material active state from the portal manage page. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.UpdateCourseMaterial | Updates course material metadata/location from the portal manage page. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.UpdatePayment | Web action that posts finance payment edits from the payments page. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.UserImportTemplate(fileName) | Serves approved CSV template files from `User Import Sheets/` via filename allow-list with traversal-safe path resolution. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| QuizRepository.GetAllAttemptsForStudentAsync(...) | Returns student quiz attempts with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/QuizFypRepositories.cs |
| QuizRepository.GetAttemptsAsync(...) | Returns quiz attempts with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/QuizFypRepositories.cs |
| QuizRepository.GetByOfferingAsync(...) | Returns quiz lists with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/QuizFypRepositories.cs |
| refreshSnapshot | Fetches analytics snapshot and updates filters/cards/charts without full page reload. | src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml |
| renderCourseTrend | Renders course trend line based on average assignment marks. | src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml |
| renderDepartmentCounts | Renders department-wise student counts using a bar chart. | src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml |
| renderPaymentStatus | Renders the Paid vs Unpaid interactive pie chart on the analytics page. | src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml |
| renderSemesterTrend | Renders combined semester trend lines for marks and attendance where available. | src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml |
| renderStudentDistribution | Renders semester-based student distribution using a pie chart. | src/Tabsan.EduSphere.Web/Views/Portal/Analytics.cshtml |
| ReportController.ExportAssignmentSummary | Exports assignment summary report as Excel for scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportAssignmentSummaryCsv | Exports assignment summary report as CSV for scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportAssignmentSummaryPdf | Exports assignment summary report as PDF for scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportAttendanceSummary | Exports attendance summary report as Excel for scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportAttendanceSummaryCsv | Exports attendance summary report as CSV for scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportAttendanceSummaryPdf | Exports attendance summary report as PDF for scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportGpaReport | Exports GPA report as Excel for selected scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportPaymentSummary | Exports payment summary report as Excel for finance scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportPaymentSummaryCsv | Exports payment summary report as CSV for finance scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ExportPaymentSummaryPdf | Exports payment summary report as PDF for finance scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetAssignmentSummary | Returns assignment summary report data with role and scope enforcement. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetAttendanceSummary | Returns attendance summary report data with role and scope enforcement. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetCatalog | Returns report catalog entries available to the caller role. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetEnrollmentSummary | Returns enrollment summary report data with institution-aware filtering. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetFypStatusReport | Returns FYP status report data for scoped department and institution filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetGpaReport | Returns GPA report data for scoped program and department filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetInstitutionReportSections | Returns institution-aware report section visibility metadata. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetLowAttendanceWarning | Returns low-attendance warning report rows under scoped filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetPaymentSummary | Exposes finance/admin/superadmin payment summary report endpoint with optional filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetQuizSummary | Returns quiz summary report data with role and scope enforcement. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetResultSummary | Returns result summary report data with role and scope enforcement. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetSemesterResults | Returns semester results report data for scoped semester filters. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.GetStudentTranscript | Returns transcript report data for a specific student profile. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportRepository.GetPaymentSummaryDataAsync | Materializes filtered payment summary rows (tenant/campus/course/semester/level aware) for finance reporting. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportRepository.cs |
| ReportService.ExportAssignmentSummaryCsvAsync | Builds CSV export payload for assignment summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportAssignmentSummaryExcelAsync | Builds Excel export payload for assignment summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportAssignmentSummaryPdfAsync | Builds PDF export payload for assignment summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportAttendanceSummaryCsvAsync | Builds CSV export payload for attendance summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportAttendanceSummaryExcelAsync | Builds Excel export payload for attendance summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportAttendanceSummaryPdfAsync | Builds PDF export payload for attendance summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportGpaReportExcelAsync | Builds Excel export payload for GPA report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportPaymentSummaryCsvAsync | Builds CSV export payload for payment summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportPaymentSummaryExcelAsync | Builds Excel export payload for payment summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportPaymentSummaryPdfAsync | Builds PDF export payload for payment summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportQuizSummaryCsvAsync | Builds CSV export payload for quiz summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportQuizSummaryExcelAsync | Builds Excel export payload for quiz summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportQuizSummaryPdfAsync | Builds PDF export payload for quiz summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportResultSummaryCsvAsync | Builds CSV export payload for result summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportResultSummaryExcelAsync | Builds Excel export payload for result summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportResultSummaryPdfAsync | Builds PDF export payload for result summary report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.ExportTranscriptExcelAsync | Builds Excel export payload for student transcript report rows. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetAssignmentSummaryAsync | Aggregates assignment report rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetAttendanceSummaryAsync | Aggregates attendance report rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetCatalogAsync | Builds role-filtered report catalog response for API consumers. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetEnrollmentSummaryAsync | Aggregates enrollment report rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetFypStatusReportAsync | Aggregates FYP status report rows into response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetGpaReportAsync | Aggregates GPA report rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetLowAttendanceWarningAsync | Aggregates low-attendance report rows into response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetPaymentSummaryAsync | Aggregates payment report rows into finance-ready totals and report response shape. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetQuizSummaryAsync | Aggregates quiz report rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetResultSummaryAsync | Aggregates result report rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetSemesterResultsAsync | Aggregates semester result rows into summary totals and response payload. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| ReportService.GetStudentTranscriptAsync | Builds transcript response payload for a target student profile. | src/Tabsan.EduSphere.Infrastructure/Reporting/ReportService.cs |
| SettingsRepository.GetAllModuleRolesAsync(...) | Returns all module-role assignments with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs |
| SettingsRepository.GetAllReportsAsync(...) | Returns report definitions with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs |
| SettingsRepository.GetModuleRolesAsync(...) | Returns module-role assignments with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs |
| SettingsRepository.GetSubMenusAsync(...) | Uses AsNoTracking for read-only submenu retrieval. | src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs |
| SettingsRepository.GetTopLevelMenusAsync(...) | Uses AsNoTracking + AsSplitQuery for include-heavy top-level sidebar graph retrieval. | src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs |
| SettingsRepository.GetVisibleMenusForRoleAsync(...) | Uses AsNoTracking + AsSplitQuery for include-heavy role-scoped sidebar visibility reads. | src/Tabsan.EduSphere.Infrastructure/Repositories/SettingsRepository.cs |
| SidebarMenuService.GetTopLevelMenusAsync(...) | Adds short-TTL in-memory cache for top-level sidebar menu reads with versioned invalidation support. | src/Tabsan.EduSphere.Application/Services/SettingsServices.cs |
| SidebarMenuService.GetVisibleForRoleAsync(...) | Adds short-TTL in-memory cache for role-scoped visible sidebar reads with versioned invalidation support. | src/Tabsan.EduSphere.Application/Services/SettingsServices.cs |
| SidebarMenuService.InvalidateSidebarCache() | Bumps sidebar cache version after role/status mutations to force fresh reads on next request. | src/Tabsan.EduSphere.Application/Services/SettingsServices.cs |
| StudyPlanService.AddCourseAsync | Adds a course to a study plan with validation for duplicates and plan state. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.AdvisePlanAsync | Applies advisor endorsement decision and notes for a study plan. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.CreatePlanAsync | Creates a new study plan for a student profile and planned semester. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.DeletePlanAsync | Deletes a study plan owned by the target student profile. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.GetPlanAsync | Returns a single study plan with associated planned courses. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.GetPlansAsync | Returns all study plans for a specific student profile. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.GetPlansByDepartmentAsync | Returns department-scoped study plans for advisor/admin workflows. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.GetRecommendationsAsync | Generates recommendation payload for missing requirements and sequencing guidance. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StudyPlanService.RemoveCourseAsync | Removes a course from a study plan while preserving remaining entries. | src/Tabsan.EduSphere.Application/StudyPlanner/StudyPlanService.cs |
| StartupVisibilityReporter.DescribeDatabaseType | Classifies the resolved connection string into a safe database-type label for logging. | src/Tabsan.EduSphere.Application/Services/StartupVisibilityReporter.cs |
| StudentLifecycleRepository.ApplyPaymentAccessScope | Applies tenant/campus scope filtering to payment receipt lifecycle queries. | src/Tabsan.EduSphere.Infrastructure/Repositories/StudentLifecycleRepository.cs |
| StudentLifecycleRepository.ApplyStudentAccessScope | Applies tenant/campus scope filtering to student profile lifecycle queries. | src/Tabsan.EduSphere.Infrastructure/Repositories/StudentLifecycleRepository.cs |
| StudentLifecycleService.MapPaymentReceipt | Maps receipt state to output contract including `PaidDate` and `UpdatedAt` tracking fields. | src/Tabsan.EduSphere.Application/Services/StudentLifecycleService.cs |
| StudentLifecycleService.UpdatePaymentReceiptAsync | Applies finance receipt edits, persists them, and notifies the student of the update. | src/Tabsan.EduSphere.Application/Services/StudentLifecycleService.cs |
| TenantIsolationResolver.Resolve | Resolves tenant isolation mode, tenant code/name/domain/database, and tenant config path from config and environment variables. | src/Tabsan.EduSphere.Application/Services/TenantIsolationResolver.cs |
| TenantOperationsService.ReadSetting(all, rawKey, defaultValue) | Reads tenant-scoped values first and falls back to legacy unscoped keys for migration-safe compatibility. | src/Tabsan.EduSphere.Application/Services/SettingsServices.cs |
| TenantOperationsService.ScopedCacheKey(key) | Builds tenant-scoped distributed-cache keys to prevent cross-tenant cache collisions. | src/Tabsan.EduSphere.Application/Services/SettingsServices.cs |
| TenantOperationsService.ScopedSettingKey(key) | Builds tenant-scoped settings keys for onboarding/subscription/profile operations with default-tenant backward compatibility. | src/Tabsan.EduSphere.Application/Services/SettingsServices.cs |
| TimetableRepository.GetByDepartmentAsync(...) | Returns timetable lists with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/TimetableRepository.cs |
| TimetableRepository.GetEntriesByCourseOfferingAsync(...) | Returns course-offering timetable entries with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/TimetableRepository.cs |
| TimetableRepository.GetPublishedByDepartmentAsync(...) | Returns published timetable lists with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/TimetableRepository.cs |
| TimetableRepository.GetTeacherEntriesAsync(...) | Returns teacher timetable entries with direct async EF execution instead of `ContinueWith` bridging. | src/Tabsan.EduSphere.Infrastructure/Repositories/TimetableRepository.cs |
| UserImportController.ImportCsv | Accepts CSV uploads for bulk user import with optional strict mode and role-based access enforcement. | src/Tabsan.EduSphere.API/Controllers/UserImportController.cs |
| UserImportService.ImportFromCsvAsync | Parses CSV imports with role/identity validation, strict-mode rollback support, and additive mobile/campus column compatibility. | src/Tabsan.EduSphere.Application/Services/UserImportService.cs |
| UserImportService.IsValidMobileNumber | Validates optional mobile/phone values to accepted character set before user import persistence. | src/Tabsan.EduSphere.Application/Services/UserImportService.cs |
| UserImportService.ResolveCampusAssignmentsIndex | Resolves optional campus-assignment header aliases (`CampusAssignments`/`CampusIds`) for backward-compatible CSV parsing. | src/Tabsan.EduSphere.Application/Services/UserImportService.cs |
| UserImportService.ResolvePhoneNumberIndex | Resolves optional mobile/phone header aliases (`MobileNumber`/`PhoneNumber`) for backward-compatible CSV parsing. | src/Tabsan.EduSphere.Application/Services/UserImportService.cs |
| UserImportService.TryValidateCampusAssignments | Validates optional campus assignments as pipe-separated GUID values during CSV import. | src/Tabsan.EduSphere.Application/Services/UserImportService.cs |
| User.SetTenantCampus(tenantId, campusId) | Assigns or clears tenant/campus ownership for user-level scoping without breaking existing identity flows. | src/Tabsan.EduSphere.Domain/Identity/User.cs |
| User.ValidateTenantCampusPair | Prevents invalid user state by requiring TenantId and CampusId to be set/cleared together. | src/Tabsan.EduSphere.Domain/Identity/User.cs |
| UserRepository.ApplyTenantCampusScope | Applies tenant/campus query filtering for user reads with SuperAdmin bypass. | src/Tabsan.EduSphere.Infrastructure/Repositories/UserRepository.cs |
| DegreeController.Download | Downloads generated degree document and applies `.docx` fallback when requested PDF is unavailable. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| DegreeController.DownloadDefaultTemplate | Streams default degree `.docx` template from the degree/transcript generation export service. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| DegreeController.Generate | Triggers degree document generation workflow for admin users. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| DegreeController.StudentDegree | Returns generated degree artifacts for the current student route `/student/degree`. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| DegreeController.ResolveCurrentUserId | Resolves current caller user-id from NameIdentifier/sub claims for student artifact filtering. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| TranscriptController.Download | Downloads generated transcript document and applies `.docx` fallback when requested PDF is unavailable. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| TranscriptController.DownloadDefaultTemplate | Streams default transcript `.docx` template from the degree/transcript generation export service. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| TranscriptController.Generate | Triggers transcript document generation workflow for admin users. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| TranscriptController.StudentTranscript | Returns generated transcript artifacts for the current student route `/student/transcript`. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| TranscriptController.ResolveCurrentUserId | Resolves current caller user-id from NameIdentifier/sub claims for student artifact filtering. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| TemplateExportService.GetDegreeTemplateAsync | Generates default Degree Word template bytes with the degree/transcript placeholder contract. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateExportService.cs |
| TemplateExportService.GetTranscriptTemplateAsync | Generates default Transcript Word template bytes with the degree/transcript placeholder contract including `{{COURSE_TABLE}}`. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateExportService.cs |
| TemplateExportService.BuildTemplateDocument | Constructs in-memory `.docx` payload from template text lines using OpenXML. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateExportService.cs |
| DegreeController.UploadTemplate | Accepts isolated degree template `.docx` uploads and persists K4 template metadata. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| AcademicDocumentTemplate.Create | Creates isolated storage metadata for degree/transcript templates. | src/Tabsan.EduSphere.Domain/Assignments/AcademicDocumentStorage.cs |
| DegreeDocumentRecord.Create | Creates a persisted record for generated degree artifacts. | src/Tabsan.EduSphere.Domain/Assignments/AcademicDocumentStorage.cs |
| TranscriptDocumentRecord.Create | Creates a persisted record for generated transcript artifacts. | src/Tabsan.EduSphere.Domain/Assignments/AcademicDocumentStorage.cs |
| TranscriptController.UploadTemplate | Accepts isolated transcript template `.docx` uploads and persists K4 template metadata. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| QRCodeService.GeneratePng | Produces QR PNG byte array from verification payload using QRCoder. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/QRCodeService.cs |
| QRCodeService.GenerateDataUrl | Produces Base64 QR data URL for lightweight UI rendering support. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/QRCodeService.cs |
| TwoFactorStateStore.DisableAsync | Clears the stored 2FA secret and disables 2FA for the current user. | src/Tabsan.EduSphere.Infrastructure/Repositories/TwoFactorStateStore.cs |
| TwoFactorStateStore.EnableAsync | Marks 2FA as enabled after the confirmation code has been validated. | src/Tabsan.EduSphere.Infrastructure/Repositories/TwoFactorStateStore.cs |
| TwoFactorStateStore.GetAsync | Returns the current protected 2FA snapshot for a user, or null if the user is missing. | src/Tabsan.EduSphere.Infrastructure/Repositories/TwoFactorStateStore.cs |
| TwoFactorStateStore.SaveSetupAsync | Stores an encrypted 2FA secret for an in-progress setup. | src/Tabsan.EduSphere.Infrastructure/Repositories/TwoFactorStateStore.cs |
| IEduApiClient.BeginTwoFactorSetupAsync | Fetches the portal-side 2FA setup payload from the API. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.DisableTwoFactorAsync | Sends a portal-side 2FA disable request to the API. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.VerifyTwoFactorLoginAsync | Sends a portal-side 2FA login challenge verification request to the API. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.VerifyTwoFactorSetupAsync | Sends a portal-side 2FA setup verification request to the API. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| DegreeController.EnsureDegreeTranscriptGenerationEnabledAsync | Checks the degree/transcript generation rollout flag before allowing degree controller actions. | src/Tabsan.EduSphere.API/Controllers/DegreeController.cs |
| TranscriptController.EnsureDegreeTranscriptGenerationEnabledAsync | Checks the degree/transcript generation rollout flag before allowing transcript controller actions. | src/Tabsan.EduSphere.API/Controllers/TranscriptController.cs |
| TemplateProcessorService.PopulateTemplate | Applies degree/transcript placeholder replacement and transcript table rendering to `.docx` template bytes. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.ReplaceInMainBody | Replaces mapped placeholder tokens in main document body text nodes. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.ReplaceInHeadersAndFooters | Replaces mapped placeholder tokens in header and footer parts. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.ReplaceCourseTable | Replaces `{{COURSE_TABLE}}` marker with generated OpenXML table or empty-data text. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.BuildCourseTable | Builds transcript course table with required degree/transcript columns. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.BuildRow | Creates table row instances for course-table header and data rows. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| TemplateProcessorService.BuildCell | Creates formatted OpenXML table cell content (with bold header support). | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateProcessorService.cs |
| DocumentGenerationService.GenerateDegreeAsync | Orchestrates degree generation using export, processing, and QR services. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DocumentGenerationService.GenerateTranscriptAsync | Orchestrates transcript generation including course-table population. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DocumentGenerationService.GetAsync | Returns generated document metadata by document id, including database fallback. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DocumentGenerationService.ListByStudentAsync | Lists generated artifacts for a specific student id with database-backed recovery. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DocumentGenerationService.GenerateInternalAsync | Performs shared generation pipeline, invokes optional PDF adapter, persists outputs, and registers metadata. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DocumentGenerationService.PersistGeneratedDocumentAsync | Persists degree/transcript generation records into academic document storage tables. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DocumentGenerationService.GetFromDatabaseAsync | Retrieves a generated artifact from persisted academic document storage when it is not cached in memory. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DocumentGenerationService.ListByStudentFromDatabaseAsync | Merges cached and persisted generated artifacts for a student. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| DegreeGenerationRequest.ToPayload | Maps degree generation request data to the template payload shape with default serial/date assignment. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| TranscriptGenerationRequest.ToPayload | Maps transcript generation request data to the template payload shape with default serial/date assignment. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/DocumentGenerationService.cs |
| IPdfConverterAdapter.TryConvertToPdfAsync | Defines optional PDF conversion adapter contract for generated degree/transcript documents. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/PdfConverterAdapter.cs |
| NoOpPdfConverterAdapter.TryConvertToPdfAsync | Default no-op adapter that returns null to preserve guaranteed `.docx` fallback behavior. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/PdfConverterAdapter.cs |
| TemplateUploadService.UploadDegreeTemplateAsync | Validates and stores isolated degree template uploads in the academic document storage layer. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateUploadService.cs |
| TemplateUploadService.UploadTranscriptTemplateAsync | Validates and stores isolated transcript template uploads in the academic document storage layer. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateUploadService.cs |
| TemplateUploadService.UploadAsync | Shared upload pipeline for `.docx` templates, storage persistence, and metadata creation. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateUploadService.cs |
| TemplateUploadService.ValidateDocxAsync | Enforces `.docx`-only validation for isolated template uploads. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateUploadService.cs |
| TemplateUploadService.BuildVersion | Builds additive template version metadata for uploaded templates. | src/Tabsan.EduSphere.API/Services/DegreeTranscriptGeneration/TemplateUploadService.cs |
| CertificateGenerationController.GetGraduatedStudents | Returns university-only graduated students filtered by tenant, campus, department, and course with role scope enforcement. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.GenerateDegreeCertificate | Generates degree certificate document for a scoped graduated student (Admin/SuperAdmin only). | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.GenerateTranscriptCertificate | Generates transcript document for a scoped graduated student (Admin/SuperAdmin only). | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.GetStudentDocuments | Returns generated document history for a scoped student with role-aware read access checks. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.Download | Downloads generated certificate/transcript artifact with pdf/docx selection and scope validation. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.GetStudentInScopeAsync | Enforces university/license/tenant/campus/department scope before management operations. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.HasStudentReadScopeAsync | Enforces scoped read permissions for Admin, Faculty, and Student document access. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.ResolveStudentNameAsync | Resolves student display name from user identity with registration fallback. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.BuildTranscriptRowsAsync(...) | Builds transcript course rows from enrollments/course offerings and supports class/semester filtered transcript generation. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| PortalController.GenerateCertificates | Renders Tenant/Campus/Department/Course-filtered Generate Certificates page with role-based manage mode. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.GenerateDegreeCertificate | Web action that triggers scoped degree generation and preserves current filter context. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.GenerateTranscriptCertificate | Web action that triggers scoped transcript generation and preserves current filter context. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.DownloadGeneratedCertificateDocument | Proxies generated document download/print for scoped portal users. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| IEduApiClient.GetGraduatedCertificateStudentsAsync | Client contract for graduated-student certificate listing with tenant/campus/department/course filters. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.GenerateDegreeCertificateAsync | Client contract for scoped degree certificate generation. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.GenerateTranscriptCertificateAsync | Client contract for scoped transcript generation. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.DownloadGeneratedCertificateDocumentAsync | Client contract for generated document download/print retrieval. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GetGraduatedCertificateStudentsAsync | Calls graduated-student listing endpoint with dynamic filter query composition. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GenerateDegreeCertificateAsync | Calls API endpoint to generate degree certificate for selected graduated student. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GenerateTranscriptCertificateAsync | Calls API endpoint to generate transcript for selected graduated student. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.DownloadGeneratedCertificateDocumentAsync | Downloads generated certificate/transcript files by document id and requested format. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| ProgramController.GetAll | Returns program list with tenant/campus scope resolution and role-aware scope enforcement. | src/Tabsan.EduSphere.API/Controllers/ProgramController.cs |
| ProgramController.GetById | Returns a single program by id under validated tenant/campus scope. | src/Tabsan.EduSphere.API/Controllers/ProgramController.cs |
| ProgramController.Create | Creates a program only when target department is inside resolved tenant/campus scope. | src/Tabsan.EduSphere.API/Controllers/ProgramController.cs |
| ProgramController.Update | Updates a program name under resolved tenant/campus scope and department ownership checks. | src/Tabsan.EduSphere.API/Controllers/ProgramController.cs |
| ProgramController.Activate | Activates a program under resolved tenant/campus scope. | src/Tabsan.EduSphere.API/Controllers/ProgramController.cs |
| ProgramController.DeactivateAlias | Backward-compatible deactivate route alias for program status control under scope checks. | src/Tabsan.EduSphere.API/Controllers/ProgramController.cs |
| ProgramController.ResolveEffectiveScope | Resolves requested tenant/campus scope against caller claims and superadmin rules. | src/Tabsan.EduSphere.API/Controllers/ProgramController.cs |
| ProgramController.EnsureDepartmentIsInScopeAsync | Validates department belongs to current tenant/campus scope before program mutations. | src/Tabsan.EduSphere.API/Controllers/ProgramController.cs |
| ReportController.GetReportsStatus | Returns active/inactive report scope status for requested or claim-derived tenant/campus scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.ActivateReports | Activates report center visibility for the resolved tenant/campus scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.DeactivateReports | Deactivates report center visibility for the resolved tenant/campus scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| ReportController.IsReportsScopeActiveAsync | Evaluates effective report activation state from settings for tenant/campus scope. | src/Tabsan.EduSphere.API/Controllers/ReportController.cs |
| UserImportController.ResolveEffectiveScope | Resolves user-import tenant/campus scope from claims and requested query values. | src/Tabsan.EduSphere.API/Controllers/UserImportController.cs |
| IAcademicProgramRepository.SetActiveAsync | Contract for activating/deactivating program entities in scoped workflows. | src/Tabsan.EduSphere.Domain/Interfaces/IAcademicProgramRepository.cs |
| AcademicProgramRepository.SetActiveAsync | Persists active/inactive program state through repository layer. | src/Tabsan.EduSphere.Infrastructure/Repositories/AcademicRepositories.cs |
| IEduApiClient.ActivateProgramAsync | Web client contract for activating scoped programs from portal workflows. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.ActivateProgramAsync | Calls scoped activate program endpoint from web portal workflows. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GetReportsScopeActiveAsync | Retrieves report scope activation status for current tenant/campus context. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.ActivateReportsScopeAsync | Calls API to activate report scope for tenant/campus context. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.DeactivateReportsScopeAsync | Calls API to deactivate report scope for tenant/campus context. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| PortalController.Programs | Renders scoped programs page with tenant/campus filters and department context. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.CreateProgram | Creates scoped program from portal and preserves selected filter context. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.UpdateProgram | Updates scoped program name from portal and preserves selected filter context. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.SetProgramActive | Toggles program active/inactive state from portal under scope-aware API calls. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.SetReportsActive | Toggles report scope active/inactive state from portal settings surface. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| CertificateGenerationController.GetAdditionalCertificates | Returns non-university additional certificate metadata for a scoped student with role-aware access checks. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.UploadAdditionalCertificate | Uploads additional student certificate files for school/college scope with admin/superadmin authorization and tenant/campus checks. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.DownloadAdditionalCertificate | Downloads previously uploaded additional student certificate files under scoped authorization checks. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.GetNonUniversityStudentForAdminManagementAsync | Validates school/college admin management scope for additional certificate upload operations. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| CertificateGenerationController.HasAdditionalCertificateReadScopeAsync | Enforces scoped read permissions for additional school/college certificates across Admin, Faculty, and Student users. | src/Tabsan.EduSphere.API/Controllers/CertificateGenerationController.cs |
| PortalController.IsUniversityInstitutionType | Resolves institution type into university/non-university behavior for certificate workflow gating. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.ResolvePeriodFilterLabel | Resolves dynamic period label (`Class` for university, `Semester` otherwise) for certificate filters. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.ResolveCertificateInstitutionType | Resolves effective institution scope from selected department or identity for certificate page behavior. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.UploadStudentAdditionalCertificate | Uploads school/college additional certificates from portal with preserved tenant/campus/department/course filter context. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| PortalController.DownloadStudentAdditionalCertificate | Downloads uploaded school/college additional certificates from portal. | src/Tabsan.EduSphere.Web/Controllers/PortalController.cs |
| IEduApiClient.GetStudentAdditionalCertificatesAsync | Client contract for listing additional certificates uploaded for a student profile. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.UploadStudentAdditionalCertificateAsync | Client contract for uploading additional school/college certificate files per student profile. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| IEduApiClient.DownloadStudentAdditionalCertificateAsync | Client contract for downloading uploaded additional certificate files. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.GetStudentAdditionalCertificatesAsync | Calls API endpoint to retrieve additional school/college certificate metadata per student profile. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.UploadStudentAdditionalCertificateAsync | Uploads additional school/college certificate files through multipart API endpoint. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |
| EduApiClient.DownloadStudentAdditionalCertificateAsync | Downloads additional school/college certificate files by document id. | src/Tabsan.EduSphere.Web/Services/EduApiClient.cs |

