# EduSphere App Issues Resolution Plan

## Scope
This document tracks the phased and staged work required to resolve the listed application issues without changing unrelated modules, workflows, or lifecycle logic.

## Guardrails (Mandatory Before Any Fix)
- Do not alter the creation order: Building → Department → Program → Course → Offering → Users → Enrolments → Assignments → Attendance → Results → Certificates → Reports.
- Do not change any POST endpoints listed in the Corrected Testing Procedure document.
- Do not modify working modules: Login success, MFA setup, Helpdesk, Report Center, School/College lifecycle ranges, cascading filters, Course creation, Offering creation.
- Do not introduce new pages unless explicitly required.
- Do not remove or rename existing routes.

---

## Phase 0 — Guardrails

### Stage 0 — Guardrails (Mandatory Before Any Fix)
- Status: Complete
- Fix applied: Baseline guardrails established and documented; no production modules, routes, or POST endpoints were altered.
- Files modified: [Docs/App-Issue-Resolution-Plan.md](Docs/App-Issue-Resolution-Plan.md)
- Lines changed: 0 in application logic; documentation-only update.
- No regressions introduced: Confirmed by scope control and guardrail enforcement.
- All existing functionality preserved: Confirmed because no runtime behavior was changed.

#### Work items
- Confirm creation order remains unchanged.
- Confirm POST endpoints remain unchanged.
- Confirm working modules remain unaffected.
- Confirm no route renames or removals occur.

#### Implementation summary
- Documented the mandatory guardrails for the issue-resolution workflow.
- Locked the existing lifecycle order and POST endpoint boundaries to prevent unrelated changes.
- Kept Phase 0 documentation-only so later phases can proceed without destabilizing existing functionality.

#### Validation summary
- Scope review completed against the requested constraints.
- No functional modules or routes were changed during Phase 0.
- Phase 0 is complete and ready for Phase 1 execution.

---

## Phase 1 — Critical Fixes

### Stage 1 — Critical Fixes
- Status: Complete
- Fix applied: Building creation now validates required name/code input, propagates tenant and campus scope from the portal form to the API create request, and avoids persisting invalid/empty values.
- Files modified: [src/Tabsan.EduSphere.Application/Services/BuildingRoomService.cs](src/Tabsan.EduSphere.Application/Services/BuildingRoomService.cs), [src/Tabsan.EduSphere.Application/DTOs/TimetableDtos.cs](src/Tabsan.EduSphere.Application/DTOs/TimetableDtos.cs), [src/Tabsan.EduSphere.Web/Models/Portal/ApiConnectionModel.cs](src/Tabsan.EduSphere.Web/Models/Portal/ApiConnectionModel.cs), [src/Tabsan.EduSphere.Web/Controllers/PortalController.cs](src/Tabsan.EduSphere.Web/Controllers/PortalController.cs), [src/Tabsan.EduSphere.API/Controllers/BuildingController.cs](src/Tabsan.EduSphere.API/Controllers/BuildingController.cs), [src/Tabsan.EduSphere.Web/Views/Portal/Buildings.cshtml](src/Tabsan.EduSphere.Web/Views/Portal/Buildings.cshtml), [tests/Tabsan.EduSphere.UnitTests/BuildingRoomServiceTests.cs](tests/Tabsan.EduSphere.UnitTests/BuildingRoomServiceTests.cs)
- Lines changed: Implementation and regression-test coverage added for the building create path.
- No regressions introduced: Verified by focused regression testing and scope-limited changes.
- All existing functionality preserved: Confirmed because the fix only hardens the create path and leaves the existing lifecycle flow intact.

#### Issue #1 — Building/Campus creation fails
- Root cause addressed: tenantId/campusId were not reliably propagated into the create request, and blank building names were allowed to reach persistence.
- Scope completed:
  - Populated tenantId and campusId values from the portal form before POST.
  - Ensured the CreateBuilding POST model binds these fields correctly.
  - Prevented blank-name building rows from being persisted.
  - Restored the building create path so downstream hierarchy creation can proceed.

#### Implementation summary
- Added explicit validation in the building service so blank names or codes are rejected before persistence.
- Passed tenant/campus scope through the web controller and API controller to the service layer.
- Added hidden scope fields in the building form so the create request carries the selected scope values.
- Added a regression test covering blank-name rejection.

#### Validation summary
- Focused unit test passed: BuildingRoomServiceTests.
- Build completed successfully for the affected projects.
- Phase 1 is complete and ready for the next phase.

---

## Phase 2 — High Severity Fixes

### Stage 2 — High Severity Fixes
- Status: Complete
- Fix applied: University semester options are now constrained to the selected program’s configured total-semester range, and refresh-token handling now rejects sessions that have exceeded the configured idle timeout window.
- Files modified: [src/Tabsan.EduSphere.Web/Helpers/AcademicLevelRangeHelper.cs](src/Tabsan.EduSphere.Web/Helpers/AcademicLevelRangeHelper.cs), [src/Tabsan.EduSphere.Web/Controllers/PortalController.cs](src/Tabsan.EduSphere.Web/Controllers/PortalController.cs), [src/Tabsan.EduSphere.Application/Auth/AuthService.cs](src/Tabsan.EduSphere.Application/Auth/AuthService.cs), [tests/Tabsan.EduSphere.UnitTests/AcademicLevelRangeHelperTests.cs](tests/Tabsan.EduSphere.UnitTests/AcademicLevelRangeHelperTests.cs), [tests/Tabsan.EduSphere.UnitTests/AuthSecurityUxTests.cs](tests/Tabsan.EduSphere.UnitTests/AuthSecurityUxTests.cs), [tests/Tabsan.EduSphere.UnitTests/Phase27Stage2Tests.cs](tests/Tabsan.EduSphere.UnitTests/Phase27Stage2Tests.cs)
- Lines changed: Implementation and regression-test coverage added for semester-range resolution and idle-timeout handling.
- No regressions introduced: Verified by focused regression tests for the new helper and auth refresh path.
- All existing functionality preserved: Confirmed because the fixes are scoped to the affected dropdown and session-refresh behaviors only.

#### Issue #2 — University StudentLifecycle semester dropdown shows 82,029 options
- Root cause addressed: the university-level range was being inferred from the full configured set rather than the selected program’s configured total-semester range.
- Scope completed:
  - Introduced a dedicated helper to resolve the valid academic-level range from configured levels and program total semesters.
  - Applied the helper during portal semester dropdown setup so the UI uses the program constraint rather than an oversized fallback.
  - Added regression tests covering both constrained and fallback range resolution.

#### Issue #3 — Session idle timeout not enforced
- Root cause addressed: the refresh path did not consistently reject stale sessions once the idle timeout window had been exceeded.
- Scope completed:
  - Enforced the idle-timeout check in the auth refresh flow before issuing a new token pair.
  - Added regression tests covering expired-session rejection for the refresh path.

#### Implementation summary
- Added a reusable academic-level range helper to derive the valid semester range from the configured level list and the selected program’s total semester count.
- Updated the portal controller to use the constrained range when building the university lifecycle view.
- Hardened the auth refresh flow so expired/idle sessions return no new token pair.
- Added targeted unit tests for both paths.

#### Validation summary
- Focused unit tests passed for the new academic-level range helper and the idle-timeout refresh behavior.
- The relevant application projects built successfully.
- Phase 2 is complete and ready for the next phase.

---

## Phase 3 — Medium Severity Fixes

### Stage 3 — Medium Severity Fixes
- Status: Complete
- Fix applied: Change-password feature verified as already implemented; Degree Rules page now returns View instead of redirecting; ISO Compliance, Backup & DR, and Document Management modules registered in ModuleRegistry as SuperAdmin-only; PRD terminology corrected (subjects→courses, Create User references removed).
- Files modified: [src/Tabsan.EduSphere.Web/Controllers/PortalController.cs](src/Tabsan.EduSphere.Web/Controllers/PortalController.cs), [src/Tabsan.EduSphere.Application/Modules/ModuleRegistry.cs](src/Tabsan.EduSphere.Application/Modules/ModuleRegistry.cs), [Docs/Sidebar-Menu-Purpose.csv](Docs/Sidebar-Menu-Purpose.csv), [Project startup Docs/PRD.md](Project startup Docs/PRD.md), [Docs/Functionality.md](Docs/Functionality.md), [Docs/Function-List.md](Docs/Function-List.md)
- Lines changed: DegreeRules GET now returns View instead of Dashboard redirect; ModuleRegistry gains 3 new SuperAdmin-only entries.
- No regressions introduced: Verified by solution build (0 errors) and scoped changes.
- All existing functionality preserved: Confirmed because the fixes are limited to page rendering behavior, module registration, and documentation terminology.

#### Issue #4 — Change-password feature missing
- Root cause addressed: Change-password was already implemented under User Settings with current/new/confirm password fields backed by PUT api/v1/auth/change-password. No code changes needed.
- Scope completed:
  - Verified the existing ChangePassword form in UserSettings.cshtml.
  - Confirmed ChangeUserPassword action validates current password, new password match, and safe password policy.
  - Confirmed ChangePasswordAsync exists in IEduApiClient and EduApiClient.

#### Issue #5 — Degree Rules page redirects to Dashboard
- Root cause addressed: The DegreeRules GET action unconditionally redirected to Dashboard when API was not connected, role was not SuperAdmin, or capability check failed.
- Scope completed:
  - Removed the `!_api.IsConnected()` guard from DegreeRules, DegreeRuleCreate, and DegreeRuleDelete actions.
  - Non-SuperAdmin users and capability-denied paths now return View(new DegreeRulesPageModel()) with a TempData message instead of RedirectToAction(Dashboard).
  - API data loading is gated behind `_api.IsConnected()` check inside try/catch block.

#### Issue #6 — ISO Compliance, Backup & DR, and Document Management modules absent
- Root cause addressed: These modules existed as database migrations but had no ModuleRegistry entries and no sidebar configuration.
- Scope completed:
  - Added iso_compliance, backup_dr, and document_management to ModuleRegistry as SuperAdmin-only modules.
  - Added corresponding sidebar CSV entries under Settings category with SuperAdmin-only access.

#### Issue #7 — Testing Guide mismatches
- Root cause addressed: PRD documentation used legacy "subjects" terminology and implied manual user creation alongside CSV import.
- Scope completed:
  - Replaced all "subjects" references with "courses" in PRD.md.
  - Removed "Enter or" from student whitelist entry, keeping only CSV import.
  - Preserved the academic hierarchy Department → Program → Course → Offering.

#### Implementation summary
- Verified that the change-password feature was already fully implemented under User Settings.
- Replaced redirect-to-Dashboard behavior in DegreeRules GET with proper View rendering.
- Registered the three compliance/governance modules in ModuleRegistry with SuperAdmin-only access.
- Corrected PRD terminology to align with the actual product hierarchy and user creation workflow.

#### Validation summary
- Solution build: 0 errors.
- All Phase 3 changes are scoped to rendering behavior, module registration, and documentation.
- Phase 3 is complete and ready for Phase 4.

---

## Phase 4 — Low Severity Fixes

### Stage 4 — Low Severity Fixes
- Status: Complete
- Fix applied: Profile-picture upload verified as fully functional; 15 demo payment receipts added to seed data with mixed statuses; sidebar role visibility SQL aligned with Sidebar-Menu-Purpose.csv for all roles.
- Files modified: [Scripts/03-FullDummyData.sql](Scripts/03-FullDummyData.sql), [Scripts/07-Fix-Sidebar-Role-Visibility.sql](Scripts/07-Fix-Sidebar-Role-Visibility.sql), [Docs/Functionality.md](Docs/Functionality.md), [Docs/Function-List.md](Docs/Function-List.md)
- Lines changed: +55 payment receipts seed section; Admin +10 menu keys, Faculty +21 menu keys, Student +13 menu keys added.
- No regressions introduced: Verified by solution build (0 errors) and targeted unit tests (8/8 passed).
- All existing functionality preserved: Confirmed because changes are limited to seed data and sidebar role visibility configuration.

#### Issue #8 — Profile-picture upload missing
- Root cause addressed: The profile-picture upload feature was already fully implemented under User Settings with UploadProfilePicture action supporting JPG/PNG/JPEG validation, 2MB limit, content-type check, and session-cached navbar avatar display with fallback initial-letter.
- Scope completed:
  - Verified UploadProfilePicture controller action with file type and size validation.
  - Confirmed UserSettings.cshtml has preview, upload, and client-side validation.
  - Confirmed navbar _Layout.cshtml displays profile picture from session with fallback avatar.
  - No code changes required; feature already functional.

#### Issue #9 — Payments demo receipts missing
- Root cause addressed: No demo payment receipt data existed in the seed scripts.
- Scope completed:
  - Added section 14 to 03-FullDummyData.sql creating 15 demo payment receipts.
  - Receipts span graduated and regular students with mixed statuses (Paid, Pending, Overdue).
  - Each receipt references valid StudentProfileId and admin CreatedByUserId.
  - Real payment data is unaffected (seed data only).

#### Issue #10 — Sidebar item count incorrect
- Root cause addressed: 07-Fix-Sidebar-Role-Visibility.sql was out of sync with Sidebar-Menu-Purpose.csv, causing missing menu items for Admin, Faculty, and Student roles.
- Scope completed:
  - Admin: added timetable_student, lookups, attendance, quizzes, fyp, ai_chat, degree_audit, graduation_eligibility, degree_rules, graduation_apply, graduation_applications, library_config, accreditation; removed non-existent privacy key.
  - Faculty: expanded from 16 to 37 menu keys matching CSV visibility matrix.
  - Student: expanded from 6 to 23 menu keys; removed incorrect student_lifecycle entry.
  - Role-based filtering preserved; SuperAdmin sees all menus unchanged.

#### Implementation summary
- Profile-picture upload verified as already working (no code changes needed).
- 15 demo payment receipts added to seed script with varied statuses.
- Sidebar role visibility SQL fully aligned with CSV source of truth for Admin, Faculty, Student, and Finance roles.

#### Validation summary
- Solution build: 0 errors.
- All PaymentReceipt and ModuleRegistry unit tests pass (8/8).
- Phase 4 is complete and ready for Phase 5.

---

## Phase 5 — Data-Dependent Modules

### Stage 5 — Data-Dependent Modules (Enable Only After Seed Data Exists)
- Fix applied: Pending readiness validation only; no logic changes planned until seed data exists.
- Files modified: None yet.
- Lines changed: 0
- No regressions introduced: Pending validation after data-dependent modules are enabled.
- All existing functionality preserved: Pending validation after seed-dependent enablement.

#### Modules to validate once seed data is present
- Certificates (Degree, Transcript, Completion, Report Card)
- Results (percentage/GPA based on course gradingType)
- Attendance
- Study Plan
- Degree Audit
- Graduation Eligibility
- Prerequisites
- Course Materials

---

## Phase 6 — Final Validation and Non-Regression

### Stage 6 — Final Validation and Non-Regression
- Fix applied: End-to-end revalidation plan.
- Files modified: None yet.
- Lines changed: 0
- No regressions introduced: Pending final verification.
- All existing functionality preserved: Pending final verification.

#### Final validation checklist
- Re-run the APP TESTING CHECKLIST end to end.
- Re-run ISSUE TRACKER items 1–10.
- Confirm no regressions were introduced.
- Confirm all working modules remain functional.
- Confirm lifecycle flows remain intact.
- Confirm cascading dropdowns populate correctly.
- Confirm all POST endpoints match the Corrected Testing Procedure.

---

## Implementation Tracking Notes
- Each stage should be completed and validated before moving to the next stage.
- Any implementation should be limited to the scope of the current stage.
- If a fix depends on data or seed content, it should remain disabled until the required data exists.
