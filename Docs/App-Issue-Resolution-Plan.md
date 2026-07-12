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
- Fix applied: Pending implementation for Building/Campus creation failure.
- Files modified: To be recorded after implementation.
- Lines changed: To be recorded after implementation.
- No regressions introduced: Pending validation after fix.
- All existing functionality preserved: Pending validation after fix.

#### Issue #1 — Building/Campus creation fails
- Root cause to address: tenantId/campusId not being populated before save.
- Planned scope:
  - Populate hidden tenantId and campusId values in the client before POST.
  - Ensure the CreateBuilding POST model binds these fields correctly.
  - Prevent orphaned empty-name rows from being created.
  - Restore Building creation so downstream hierarchy creation can continue.

---

## Phase 2 — High Severity Fixes

### Stage 2 — High Severity Fixes
- Fix applied: Pending implementation for semester binding and idle timeout behavior.
- Files modified: To be recorded after implementation.
- Lines changed: To be recorded after implementation.
- No regressions introduced: Pending validation after fix.
- All existing functionality preserved: Pending validation after fix.

#### Issue #2 — University StudentLifecycle semester dropdown shows 82,029 options
- Planned scope:
  - Limit semester options to the Program’s Total Semesters range (1–8).
  - Remove dependency on record count or max ID.
  - Restore page payload size close to the expected smaller footprint.

#### Issue #3 — Session idle timeout not enforced
- Planned scope:
  - Enforce a 5-minute idle timeout across all admin roles.
  - Align session cookie/token expiry with the configured timeout.

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
