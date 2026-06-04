# SuperAdmin User Guide

Version: 1.7  
Date: 04 June 2026  
Completion Status: Phase 38 complete + ISO 27001 & ISO 9001 Compliance (Phases 1-10)

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

## 9. Security and Audit Oversight (ISO 27001 Enhanced)

### 9.1 Audit Logging (ISO 27001 A.12.4)

The audit_logs table now captures full context for every auditable action:
- **ActorRole**: The role of the user performing the action.
- **UserAgent & DeviceInfo**: Client browser and device details.
- **CorrelationId**: Unique ID for tracing actions across distributed services.
- **Severity**: Event severity — Info, Warning, Error, or Critical.
- **EventCategory**: Event grouping — Security, Academic, Financial, System, Compliance, UserManagement.

Audit logs are **immutable** — records cannot be updated or deleted. Any attempt to modify audit data is blocked at the database level.

To review audit logs, use the audit dashboard at **Settings > Audit Logs** with filters for:
- Actor user, role, action, entity name
- Severity level and event category
- Date range and correlation ID
- Export to CSV, Excel, or PDF for compliance evidence

### 9.2 Password Policy & Session Security (ISO 27001 A.9.4)

- **Password Ageing**: Passwords expire after 90 days. Users are prompted to change on next login.
- **Password History**: The last 5 passwords cannot be reused. History entries auto-expire.
- **Account Lockout**: 5 failed attempts trigger account lockout.
- **MFA (Multi-Factor Authentication)**: TOTP-based MFA available; enable in user profile.
- **Idle Session Timeout**: Sessions without activity for 30 minutes are revoked.
- **Session Management**: View and revoke active sessions at **Settings > Account Security > Active Sessions**.

### 9.3 Login Activity Monitoring (ISO 27001 A.12.4.1)

Every login attempt is recorded in the `login_activity_logs` table with:
- Username, IP address, user agent, device info
- Success/failure status and failure reason (InvalidCredentials, MfaRequired, ConcurrencyLimitReached, SessionRiskBlocked)
- Risk level assessment (Low/Medium/High)
- Account lockout state at time of attempt

Review login activity at **Settings > Login Activity** with filters for user, status, risk level, date range.

### 9.4 Data Protection & Classification (ISO 27001 A.8.2, A.18.1.4)

- **Data Classification**: Entities tagged as Public, Internal, Confidential, or Restricted. View at **Settings > Data Protection > Classifications**.
- **Encryption Service**: AES-256-CBC encryption available for sensitive data. Access via Data Protection API.
- **Data Masking**: PII masking for UI display (email, phone, name).
- **GDPR Consent**: `ConsentToMonitoring` flag on user accounts. `DataRetentionDate` for data lifecycle management.

### 9.5 Incident Management (ISO 27001 A.16)

Full incident lifecycle tracking at **Settings > Incidents**:
- **Status flow**: Open → Investigating → Resolved → Closed (reopen supported)
- **Severity**: Low / Medium / High / Critical
- **Category**: Security / Breach / DataLoss / AccessViolation / System / Other
- **Assignment**: Assign incidents to investigators; track resolution timeline

### 9.6 Backup & Disaster Recovery (ISO 27001 A.17)

Backup operations logged in `backup_logs` with:
- Type (Full/Differential/Log), file path, size, duration
- Status (Started/Completed/Failed/Verified), checksum, error details
- Backup verification records in `backup_verification_logs` (IntegrityCheck, RestoreTest)

Review backup status at **Settings > Backup**, including latest status summary per backup type.

### 9.7 Policy Document Management (ISO 9001 7.5)

Version-controlled policy documents at **Settings > Policy Documents**:
- **Status**: Draft → Published → Archived
- **Access Level**: Public / Internal / Confidential / Restricted
- **Version History**: Every content update creates an immutable version record
- **Categories**: Security, Compliance, Academic, Operations

### 9.8 Compliance Dashboard (ISO 27001 + ISO 9001)

Aggregated compliance posture at **Settings > Compliance** with 7 sections:
1. **Audit**: Recent activity, severity distribution, event categories
2. **Security**: Active sessions, password policy compliance, MFA adoption
3. **Backup**: Latest operations, verification results, DR readiness
4. **Incidents**: Status breakdown, severity distribution, aging
5. **Activity**: Login trends, failure patterns, suspicious IPs
6. **Data Protection**: Classification coverage, consent tracking
7. **Documents**: Policy document status, version counts

### 9.9 Data Integrity Checks (ISO 27001 A.12.7)

Automated integrity verification at **Settings > Data Integrity** covers:
- Audit coverage for critical entities
- Orphaned active users without role assignments
- Student accounts without profiles
- Open offerings in closed semesters
- Unpublished results with pending marks
- Dropped enrollment consistency
- Pending modification requests

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

## 12. SuperAdmin Weekly Checklist (ISO Enhanced)

- Verify license health and upcoming expiry window
- Review module status and any pending change requests
- Review privileged access changes
- Validate report and sidebar permission consistency
- Check critical notifications and operational warnings
- **Review Compliance Dashboard**: Check all 7 sections for anomalies
- **Review Audit Logs**: Spot-check high-severity and security-category events
- **Review Login Activity**: Check for suspicious IP patterns or brute-force attempts
- **Review Open Incidents**: Ensure critical/high incidents are progressing
- **Verify Backup Status**: Confirm latest backup completed successfully

## 13. SuperAdmin Monthly Checklist (ISO Enhanced)

- Conduct role and permission audit
- Review module utilization and disable unused optional modules
- Validate backup and recovery readiness — verify at least one RestoreTest passed
- Review security findings and remediation progress
- **Review Incident Trends**: Analyze incident categories and resolution times
- **Review Password Policy Compliance**: Check password expiry status across users
- **Review Active Sessions**: Revoke stale or suspicious sessions
- **Review Data Classification Coverage**: Ensure all critical entities classified
- **Review Policy Documents**: Update and publish any documents in Draft for >30 days
- **Run Data Integrity Check**: Review findings and remediate warnings/errors
- **Archive Expired Audit Logs**: Per institutional retention policy

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

## Phase 40.3 Scoped Governance Update (2026-05-25)

- Program management governance now includes tenant and campus scoped create, update, activate, and deactivate controls.
- Report Center activation status is now managed per tenant and campus scope with explicit activate/deactivate operations.
- Sidebar governance synchronization now self-heals role-access and active-status mappings in existing environments.
- Settings governance catalog now explicitly includes Admin Users, Tenant Management, and Campus Management alongside Sidebar Settings, Report Settings, Institution Policy, and Module Composition.
- User import governance now enforces claim-aware effective scope resolution to prevent cross-scope onboarding mistakes.

## Phase 40.4 Institution-Aware Certificates Update (2026-05-26)

- Certificate governance now differentiates university vs non-university operational paths.
- University mode enables degree/transcript generation flows subject to role and scope controls.
- School/College mode enables additional student certificate upload/list/download flows under authorized operations.
- License/policy controls can disable university behavior; when disabled, degree/transcript surfaces are hidden.
- Scope validation for certificate operations remains tenant/campus and role-assignment aware.

## Phase 40.5 Results and Attendance Governance Update (2026-05-28)

- Results workflows now enforce explicit separation between entry and publication authority.
- Correction operations require reasoned audit context and published-state eligibility before change.
- Import report access is hardened through one-time token validation and expiry controls.
- Full dummy validation now expects `DemoDatasetVersion = FullDummyData-v9` with mixed results lifecycle and attendance timeline seed data.

## Phase 40.6 ISO 27001 + ISO 9001 Compliance Update (2026-06-04)

- **ISO 27001 (Information Security)** and **ISO 9001 (Quality Management)** compliance instrumentation deployed across 10 phases.
- New governance surfaces: Compliance Dashboard, Audit Logs (enhanced), Login Activity Monitor, Incident Management, Backup Status, Data Protection, Policy Documents, Data Integrity.
- Audit logging enhanced with CorrelationId, Severity, EventCategory, ActorRole, UserAgent, DeviceInfo — all audit records immutable.
- Security hardening: 90-day password ageing, password history with expiry, 30-minute idle session timeout, MFA support, session revocation.
- 7 new compliance tables and 20+ new indexes for optimized compliance queries.
- All changes are additive and backward-compatible. See Section 9 for full operational details.

## 15. SuperAdmin Governance Runbook

This runbook helps execute predictable and auditable governance changes.

### 15.1 Change Categories

1. License changes
- Activation, renewal, replacement, emergency rollback.

2. Module composition changes
- Enable/disable optional modules with approval and communication.

3. Access governance changes
- Sidebar and report role mapping updates.

4. Scope governance changes
- Tenant/campus-specific activation, deactivation, and access boundaries.

### 15.2 Required Approval Pattern

1. Raise change request with objective and impact scope.
2. Capture affected roles, modules, and tenant/campus targets.
3. Approve through designated authority.
4. Execute during defined maintenance window where applicable.
5. Validate using role-based smoke checklist.

### 15.3 Post-Change Validation

1. Validate menus per role.
2. Validate API and UI behavior parity.
3. Validate no unauthorized privilege expansion.
4. Confirm audit logs captured change events.

## 16. Audit and Compliance Rhythm

### 16.1 Daily

- High-risk action audit checks.
- Failed login and lockout review.
- Critical incident queue review.

### 16.2 Weekly

- Role-to-menu and role-to-report spot validation.
- Module activation consistency review.
- Random access-denial sample review to detect misconfiguration.

### 16.3 Monthly

- Full role entitlement audit.
- License and module alignment review.
- Tenant/campus scope conformance review.
- Security control effectiveness check.

## 17. Emergency Operations

### 17.1 License Incident

1. Confirm active license state and expiry.
2. Validate signature/public-key consistency.
3. Roll back to previous valid license only if approved.
4. Record incident timeline and preventive actions.

### 17.2 Unauthorized Access Suspicion

1. Contain affected account/session.
2. Capture audit logs and scope impact.
3. Review recent role/module/scope changes.
4. Apply corrective governance updates.
5. Notify stakeholders per policy.

### 17.3 Governance Drift

1. Reconcile module registry and sidebar/report settings.
2. Reapply intended role mappings.
3. Validate with role test accounts.
4. Document root cause and preventive control.

## 18. Release Governance Checklist

Before production release:

- License validity confirmed for deployment window.
- Required modules activated and optional modules intentionally configured.
- Sidebar settings validated for all privileged roles.
- Report settings validated for role-based least privilege.
- Institution policy and terminology mode confirmed.
- Tenant/campus scope behavior spot-tested.
- Training and user guides updated for release changes.

After production release:

- Role smoke tests passed.
- Critical workflows validated (auth, attendance, results, reports, notifications).
- No high-severity governance incidents in first 24h.
- Hypercare monitoring enabled with ownership assignment.

