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
- Fix applied: Pending implementation for password change, Degree Rules routing, ISO/backup/document modules, and testing-guide alignment.
- Files modified: To be recorded after implementation.
- Lines changed: To be recorded after implementation.
- No regressions introduced: Pending validation after fix.
- All existing functionality preserved: Pending validation after fix.

#### Issue #4 — Change-password feature missing
- Planned scope:
  - Add a change-password form under User Settings or a dedicated page.
  - Require old password, new password, and confirm password.
  - Preserve existing UserSettings fields.

#### Issue #5 — Degree Rules page redirects to Dashboard
- Planned scope:
  - Remove the redirect behavior.
  - Ensure the route returns HTTP 200 and renders normally.
  - Preserve sidebar routing.

#### Issue #6 — ISO Compliance, Backup & DR, and Document Management modules absent
- Planned scope:
  - Restore missing routes/pages if intended for tenant admins.
  - Hide links from tenant admins if the modules are super-admin only.

#### Issue #7 — Testing Guide mismatches
- Planned scope:
  - Remove references to Create User and Subjects.
  - Keep User Import CSV as the only user creation method.
  - Preserve the academic hierarchy Department → Program → Course → Offering.

---

## Phase 4 — Low Severity Fixes

### Stage 4 — Low Severity Fixes
- Fix applied: Pending implementation for profile upload, demo receipts, and sidebar menu count.
- Files modified: To be recorded after implementation.
- Lines changed: To be recorded after implementation.
- No regressions introduced: Pending validation after fix.
- All existing functionality preserved: Pending validation after fix.

#### Issue #8 — Profile-picture upload missing
- Planned scope:
  - Add JPG/PNG upload support under 2MB.
  - Add preview, replace, and remove controls.
  - Preserve the fallback initial-letter avatar behavior.

#### Issue #9 — Payments demo receipts missing
- Planned scope:
  - Restore demo receipts for testing.
  - Avoid affecting real payment data.

#### Issue #10 — Sidebar item count incorrect
- Planned scope:
  - Restore the expected University admin menu count.
  - Keep role-based filtering intact.

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
