# Tabsan EduSphere User Lifecycle

## 2026-06-02 Lifecycle Synchronization Summary (Domain Script Packs and Full Dummy Coverage)

- Lifecycle demo/validation data is now organized in domain-specific script packs:
  - Scripts/School Scripts,
  - Scripts/College Scripts,
  - Scripts/University Scripts.
- School lifecycle demo data now covers Class 1 to Class 10 for all scoped school students with deterministic report-card and result rows.
- College lifecycle demo data now covers Class 11 and Class 12 for all scoped college students with deterministic report-card and result rows.
- University lifecycle demo data now covers Semester 1 to Semester 8 for all scoped university students and includes deterministic enrollment, attendance, and FYP lifecycle rows.
- University post-deployment lifecycle checks now include strict attendance and FYP coverage assertions in addition to semester report-card/result checks.

## 2026-05-26 Lifecycle Synchronization Summary

- Governance lifecycle includes institution-aware result-calculation control (GPA/component rules) under privileged settings access.
- Certificate lifecycle is explicitly institution-aware:
  - University: degree + transcript generation by graduation/pass and period scope.
  - School/College: additional certificate upload/list/download lifecycle.
- Authentication lifecycle retains operational portal 2FA setup/verify/disable/test flows for user-level security hardening.
- Validation snapshot is synchronized: build/runtime healthy, with remaining known sidebar assertion drift tracked for test-matrix refactor.

## 2026-05-27 Lifecycle Synchronization Summary

- Startup lifecycle reliability is hardened by deterministic environment-profile loading from layered configuration sources (including parent/root profile file paths).
- Development runtime lifecycle now uses profile-resolved LocalDB-compatible defaults for stable API/Web startup checks.
- Operational lifecycle note: local API startup requires an available SQL target before startup seeding is executed.

## Purpose
This document describes the full lifecycle of platform roles, including each role's purpose, what it does, and how user/account states evolve from onboarding to offboarding.

## System Roles
Core roles are system-defined and seeded at startup.

| Role | Primary Purpose | What the Role Does |
|---|---|---|
| SuperAdmin | Platform governance and system control | Manages license, module activation, sidebar/report governance, branding, and high-impact settings across all departments. |
| Admin | Department-level operations | Manages users, departments/programs/courses, enrollments, student lifecycle actions, and operational reporting in assigned scope. |
| Faculty | Teaching and academic delivery | Manages assignments/quizzes/attendance/results/FYP and teaching workflows for assigned offerings. |
| Student | Learning and progression | Consumes academic services: timetable, assignments, quizzes, attendance, results, transcript, notifications, and student-facing workflows. |

## End-to-End Role Lifecycle

### 1. Provisioning and Seed Stage
- Roles are created as protected system roles.
- Bootstrap SuperAdmin account is created from environment variables during initial seed.
- Role definitions are stable and intended to remain immutable in normal operations.

### 2. Onboarding Stage
- SuperAdmin:
  - Created as bootstrap account by seed process.
  - Used to initialize governance settings and create delegated administrators.
- Admin and Faculty:
  - Created through admin/superadmin managed user flows.
  - Assigned role and optional department scope based on organizational policy.
- Student:
  - Created either by self-registration via whitelist or through admin-managed onboarding/import.
  - Linked to student profile and academic context (department/program/level).

### 3. First-Access and Authentication Stage
- User logs in with credentials.
- If password-change enforcement is enabled for that account, user must complete forced password change before normal dashboard access.
- Account security checks apply:
  - active/inactive check,
  - lockout checks after repeated failed attempts,
  - MFA checks when enabled.

### 4. Active Operation Stage (Role Duties)
- SuperAdmin operational duties:
  - license lifecycle management,
  - module and menu governance,
  - global policy/branding/report access control,
  - security and audit oversight.
- Admin operational duties:
  - department operations,
  - user lifecycle administration,
  - student lifecycle actions (promotion/deactivation/reactivation/graduation workflows),
  - reports/analytics in authorized scope.
- Faculty operational duties:
  - teaching delivery and evaluations,
  - attendance and grading,
  - student communication and academic guidance,
  - assigned analytics/reports.
- Student operational duties:
  - course participation,
  - assignment/quiz submission,
  - attendance and result tracking,
  - transcript and notification usage.

### 5. Security Lifecycle Stage
- Failed login attempts increment per account.
- Threshold breach causes lockout for configured duration.
- Lockout can expire automatically or be cleared by admin action.
- MFA lifecycle:
  - enrollment setup,
  - enablement after verification,
  - recovery-code regeneration and one-time consumption.

### 6. Student Academic Lifecycle Stage
Student profile status transitions are explicit and policy-driven.

| Status | Meaning | Typical Trigger |
|---|---|---|
| Active | Student is enrolled and participating | Default operational state after onboarding/activation |
| Inactive | Student cannot continue normal access/participation | Deactivation due to leave/dropout/administrative action |
| Graduated | Student completed program; treated as completed profile | Graduation approval workflow completion |

Common lifecycle actions in student flow:
- promotion to next academic level (semester/grade/year by institution policy),
- graduation processing for final-level candidates,
- admin-reviewed change/modification request handling,
- deactivation/reactivation with user notification.

### 7. Governance and Visibility Lifecycle
- Route and menu visibility are controlled by role permission + module entitlement + sidebar status.
- SuperAdmin has governance override visibility for protected system settings.
- Role visibility and permissions can be adjusted without deleting user records.
- Settings/governance menus (for example Sidebar Settings, Report Settings, Institution Policy, Module Composition, Tenant/Campus Management) are restricted to privileged governance roles.
- Program and report-center operational visibility is now also tenant/campus scoped, so activation state can differ by organizational scope.

### 7.1 Program and Reporting Scope Lifecycle
- Program lifecycle actions (create, update, activate, deactivate) are validated against effective tenant/campus scope and department ownership.
- Report center lifecycle state is explicitly managed through scoped activation/deactivation controls.
- SuperAdmin can operate across scopes using explicit scope selection; non-superadmin roles operate within claim-derived scope.
- Scope-aware controls preserve data and workflow continuity while allowing controlled operational disable/enable by tenant/campus.

### 8. Offboarding and Recovery Stage
- Soft offboarding is done by deactivation (data preserved, login blocked).
- Reactivation restores operational access without recreating account data.
- Password reset and forced change can be used for secure account recovery.

## 9. Runtime and Compatibility Safeguards (2026-05-26)

- Graduation lifecycle rejection/approval endpoints now surface controlled concurrency conflict feedback instead of unhandled failures.
- LMS lifecycle content-module creation now returns deterministic validation feedback for invalid or missing offering context.
- Announcement lifecycle now rejects invalid offering references with user-safe bad-request messaging across API/portal surface.
- FYP lifecycle now accepts legacy panel role values (`Internal`/`External`) through compatibility aliases to avoid migration-time enum failures.
- FYP repository lifecycle read paths now avoid continuation-based asynchronous execution patterns that can trigger DbContext multi-operation runtime errors.

## 10. Certificate Lifecycle by Institution (2026-05-26)

- University certificate lifecycle:
  - Degree generation is restricted to university scope and graduate/pass-ready students.
  - Transcript generation is available with class/semester contextual filtering.
- School and College certificate lifecycle:
  - Admin/SuperAdmin can upload multiple additional certificates per student.
  - Student can view and download own uploaded additional certificates.
- License and policy controls:
  - Degree/transcript actions are hidden when university mode is disabled by active policy/license.
- Scope and access controls:
  - Certificate operations follow tenant/campus and role assignment boundaries for manage/read access.

## Role-to-Lifecycle Responsibility Matrix

| Lifecycle Phase | SuperAdmin | Admin | Faculty | Student |
|---|---|---|---|---|
| Bootstrap and governance | Owns | Supports (delegated) | No | No |
| User/account onboarding | Owns global setup | Owns department execution | No | Self-service only where allowed |
| Daily operations | Platform-wide | Department-wide | Offering-level | Personal academic workflow |
| Security response | Owns policy escalation | Handles account-level actions | Reports issues | Reports issues |
| Student progression | Oversight | Primary executor | Contributor (academic evidence) | Subject of workflow |
| Offboarding/reactivation | Oversight | Primary executor | No | N/A |

## Practical Notes
- Missing access is usually caused by one of: role assignment, module inactive, menu inactive, or role-menu access restriction.
- Deactivation is preferred over deletion for auditability and recovery.
- Student lifecycle operations should be accompanied by notifications and audit-aligned administrative rationale.
