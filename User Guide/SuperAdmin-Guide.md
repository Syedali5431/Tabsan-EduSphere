# SuperAdmin User Guide

Version: 1.5  
Date: 15 May 2026  
Completion Status: Phase 38 complete (final separation baseline)

## 1. Purpose

This guide is for platform-level administrators responsible for licensing, module governance, role access controls, and cross-department platform operations in Tabsan EduSphere.

## 1.1 What's New (May 2026)

### Governance and Enforcement (Phase 24)
- Backend module enforcement and module-aware sidebar visibility now operate together for safer governance (Phase 24.2-24.3).
- Institution policy and report/analytics scoping behavior has been hardened for parity and authorization consistency.

### Analytics and Reporting (Phase 31)
- Advanced analytics now include top performers ranking, performance trend analysis, and comparative department metrics for admin/faculty/student surfaces (Phase 31.2).
- Institution-specific report sections for School, College, and University contexts automatically filter available reports (Phase 31.1).
- Standardized analytics export metadata across all export surfaces with support for queued jobs on large exports (Phase 31.3).
- New analytics endpoints for top-performers, performance-trends, and comparative-summary with institution-scoped authorization.

### Portal and Infrastructure
- Parent portal capabilities can now include linked-student read surfaces and parent notification fan-out.
- Phase 33 tenant-scope isolation was delivered for tenant operations settings (onboarding, subscription, tenant profile).
- Institution-aware academic vocabulary (Semester/Grade/Year, GPA/Percentage/Marks) configured via license policy.

## 1.2 Documentation Baseline (15 May 2026)

- User import templates are now role-specific under User Import Sheets: faculty-admin-import-template.csv and students-import-template.csv.
- Standard DB deployment run path is Scripts/01 through Scripts/05.
- Consolidated cross-phase planning and enhancement record is maintained in Docs/Consolidated-Execution-Enhancements-Issues.md.

## 1.3 Final Release Packaging Update (Phase 37/38)

- Runtime app and license app are published as separate deliverables.
- Non-runtime documents (including user guides and training assets) are packaged separately.
- Execution evidence:
	- Artifacts/Phase37/Publish-Separation-20260515.md
	- Artifacts/Phase38/NonRuntime-Asset-Separation-20260515.md

## 2. SuperAdmin Responsibilities

Primary responsibilities:
- Platform bootstrap and security posture oversight
- License activation and renewal lifecycle
- Global module activation/deactivation
- Role-level menu and report access governance
- High-impact operational approvals and audit review

SuperAdmin actions can affect all users. Use change control and approval procedures before major updates.

## 3. Initial Environment Readiness

Before onboarding users, confirm:
- License state is active
- Mandatory modules are enabled
- Roles and baseline permissions are seeded
- Department foundations are created
- Authentication and email settings are valid

Recommended startup sequence:
1. Activate valid license.
2. Verify module status.
3. Verify role and sidebar access.
4. Create department admins.
5. Hand over daily operations to Admins.

## 4. License Management

Path: Sidebar > System Settings > License Update

Typical flow:
1. Generate license file using KeyGen utility.
2. Upload license file in License Update screen.
3. Verify status, type, activation time, and expiry details.

Operational controls:
- Keep license private keys outside application servers.
- Store generated licenses in secure vaults.
- Track renewal windows proactively.

## 5. Module Governance

Path: Sidebar > System Settings > Module Settings

You can:
- Enable or disable optional modules
- Keep mandatory modules always active
- Align module rollout with institutional readiness

Change policy recommendation:
- Announce activation/deactivation before applying
- Validate role assignments after any module change
- Monitor logs for post-change errors

## 6. Sidebar and Feature Access Control

Path: Sidebar > System Settings > Sidebar Settings

Use this area to:
- Grant or revoke role access to specific menus
- Keep sensitive menu items restricted
- Validate visibility for each role

After access changes:
- Test as each role (or use delegated test accounts)
- Confirm no unintended privilege exposure

## 7. Report Access Governance

Path: Sidebar > System Settings > Report Settings

You can configure:
- Which reports exist and are active
- Which roles can access each report

Governance best practice:
- Grant least privilege
- Review report permissions monthly

## 8. Theme and Global UX Policy

Path: Sidebar > System Settings > Theme Settings

You may define default theme behavior and user personalization policy depending on deployment settings.

For accessibility:
- Keep high-contrast option available
- Avoid theme policies that reduce readability

## 9. Security and Audit Oversight

Areas to monitor:
- Account lockout trends
- Password policy enforcement
- Privileged changes and publication events
- Administrative request lifecycle

If Advanced Audit module is active:
- Review high-risk action logs regularly
- Archive logs per policy
- Investigate anomalies quickly

## 10. Operational Workflows

### 10.1 New Department Onboarding

1. Create department metadata.
2. Assign department admins.
3. Validate academic program and semester setup readiness.
4. Confirm module and menu access.
5. Notify stakeholders.

### 10.2 Semester Transition

1. Verify current semester closure process.
2. Open next semester windows.
3. Confirm timetable readiness and offering assignments.
4. Validate reporting and notification configuration.

### 10.3 Incident Response (Access or Data)

1. Contain impact (disable affected accounts if needed).
2. Review audit events.
3. Coordinate with department admin and infrastructure owner.
4. Apply corrective changes.
5. Document timeline and controls improved.

## 11. Common SuperAdmin Issues

1. License appears active but feature unavailable:
- Verify module is enabled
- Verify role access for feature menus

2. Admin cannot see expected settings:
- Confirm role assignment and restricted menu policy

3. Reports visible to wrong role:
- Re-check Report Settings role mapping

4. Unexpected failed logins:
- Inspect lockout and failed attempt controls
- Validate identity provider/config state

## 12. SuperAdmin Weekly Checklist

- Verify license health and upcoming expiry window
- Review module status and any pending change requests
- Review privileged access changes
- Validate report and sidebar permission consistency
- Check critical notifications and operational warnings

## 13. SuperAdmin Monthly Checklist

- Conduct role and permission audit
- Review module utilization and disable unused optional modules
- Validate backup and recovery readiness
- Review security findings and remediation progress

## 14. Institute Parity Support Handover Checklist

When reviewing School, College, or University parity incidents:
1. Confirm the caller role, institution type, and active department/offering scope.
2. Verify whether the issue is an expected authorization denial or a true defect.
3. Check the selected report or analytics filter values and reproduce the request with the same context.
4. Confirm the target module is active and the visible menu matches the requested route.
5. Capture endpoint name, request time, status code, and any correlation/request identifiers.
6. Escalate only after confirming assignment, institute compatibility, and module availability.

Common parity-scope signals to record:
- report endpoint returns `400/403/500`
- analytics data appears empty only under one institute context
- direct URL access differs from sidebar visibility
- Student can see an operational route that should be restricted

## Phase 40.2 Unified Update (2026-05-21)

- Added alignment note for expanded School/College/University operations and Finance workflows.
- Confirmed mobile-ready user handling for import and seeded data (MobileNumber/PhoneNumber).
- Confirmed campus-assignment aware import compatibility (CampusAssignments, pipe-separated GUIDs).
- Confirmed reporting baseline includes payment summary support for Finance role.
- Clarified release policy: upcoming Mobile APP features are roadmap items and do not change current subscription pricing.
- Pricing policy remains unchanged; newly introduced platform enhancements are included free for existing subscribed plans.
