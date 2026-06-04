deepSeekYou are a senior enterprise .NET + MSSQL architect working on an Educational ERP system that must become ISO 9001 and ISO 27001 compliant.

You are given:
1. ISO enhancement roadmap (Enhancement-ISO.md)
2. Existing full database schema
3. Existing repository structure with documentation files

Your job:
- Implement ISO enhancements phase-by-phase
- Maintain full backward compatibility
- Apply ONLY additive (non-breaking) changes
- Keep system stable and production-safe
- Automatically update documentation and repository after each phase

---------------------------------------------------------------------

STRICT RULES:

1. DO NOT:
- Break existing APIs or routes
- Modify GPA/CGPA logic
- Remove or destructively alter schema
- Break tenant/campus isolation

2. ONLY:
- Add new tables
- Add new columns
- Add indexes, constraints
- Extend logic safely

3. ALWAYS:
- Use EF Core migration style
- Add performance indexes
- Ensure auditability + traceability
- Follow secure coding practices
- Keep changes enterprise-grade and scalable

---------------------------------------------------------------------

MANDATORY DOCUMENTATION & REPO RULES (VERY IMPORTANT)

After COMPLETING EACH PHASE:

1. ✅ UPDATE Enhancement-ISO.md:
   - Add:
     ✔ Implementation Summary
     ✔ Validation Summary
   - Then mark phase as:
     ✅ COMPLETED

2. ✅ UPDATE Function_list.md:
   - Add ALL newly created functions/services/APIs
   - Avoid duplicate functions
   - Keep it clean (NO summaries here)

3. ✅ REMOVE Implementation & Validation summaries FROM:
   - Function_list.md
   - command.md
   - Functionality.md
   - PRD.md
   - Modules.md
   - Development Plan - ASP.NET.md
   - Database Schema.md

   (These files must only contain permanent design info, NOT summaries)

4. ✅ KEEP ALL FILES CONSISTENT:
   - No duplication
   - No outdated info
   - Sync naming across files

5. ✅ GIT OPERATIONS (MANDATORY):
   After each phase:
   - Commit all changes
   - Use commit message:
     "ISO Phase X Completed - [Phase Name]"
   - Push to repository
   - Pull latest to sync

   (Simulate these operations logically if execution not possible)

---------------------------------------------------------------------

OUTPUT FORMAT (FOR EVERY PHASE)

Provide:

1. Schema Changes
   - SQL
   - EF Core model updates
   - Indexes and constraints

2. Backend Implementation
   - C# services / logic (or clear pseudo-code)

3. API Design
   - Endpoints
   - Request/response

4. UI Suggestions (if required)

5. Validation Checklist

6. Documentation Updates:
   - EXACT content to append in Enhancement-ISO.md
   - EXACT Function_list.md updates

7. Git Step Summary:
   - Files changed
   - Commit message

---------------------------------------------------------------------

PHASE 0 — GAP ANALYSIS (START FIRST)

Analyze schema vs ISO roadmap.

Current known:
- audit_logs exists but missing ActorRole, UserAgent, DeviceInfo
- users has MFA + lockout features
- password_history exists
- user_sessions exists

Identify:
- Missing ISO features
- Missing tables
- Missing audit coverage
- Security gaps

Output:
✔ Gap analysis
✔ Required new tables
✔ Missing fields
✔ Risk areas

DO NOT IMPLEMENT YET.

---------------------------------------------------------------------

## PHASE 0 — GAP ANALYSIS ✅ COMPLETED

### ✅ Implementation Summary

### 1. Schema Audit — Existing Tables

| Table | Columns | ISO Coverage | Gaps |
|-------|---------|-------------|------|
| **audit_logs** | Id (bigint PK), ActorUserId, Action, ActorRole, EntityName, EntityId, OldValuesJson, NewValuesJson, OccurredAt, IpAddress, UserAgent, DeviceInfo | ✅ ActorRole, UserAgent, DeviceInfo already present (NVARCHAR(64), NVARCHAR(1024), NVARCHAR(1024)). ✅ Indexes on: OccurredAt, ActorUserId, ActorRole, (EntityName+OccurredAt). ✅ Immutable enforcement in DbContext. | Missing CorrelationId for distributed tracing. Missing Severity level (Info/Warning/Error/Critical). Missing EventCategory for grouping (Security, Academic, Financial, System). |
| **users** | Id, Username, Email, PasswordHash, RoleId, IsActive, LastLoginAt, FailedLoginAttempts, IsLockedOut, LockedOutUntil, MfaIsEnabled, MfaTotpSecret, MfaRecoveryCodesHashJson, MustChangePassword, TenantId, CampusId, DepartmentId, FullName, FatherName, PhoneNumber, Address, InstitutionType, ThemeKey | ✅ MFA fields (ISO 27001 A.9.4.2). ✅ Account lockout with configurable threshold (ISO 27001 A.9.4.1). ✅ Account activation/deactivation (ISO 27001 A.9.2.6). ✅ Role-based access control. ✅ Tenant/Campus isolation. | Missing LastPasswordChangedAt for password ageing policy (ISO 27001 A.9.4.3). Missing ConsentToMonitoring flag (GDPR/privacy). Missing DataRetentionDate for data lifecycle management. |
| **password_history** | Id, UserId, PasswordHash, CreatedAt | ✅ Password history for reuse prevention (ISO 27001 A.9.4.3). | Missing ExpiresAt for automatic history archival. Missing composite index on (UserId, CreatedAt DESC) for fast last-N query. |
| **user_sessions** | Id, UserId, RefreshTokenHash, DeviceInfo, IpAddress, ExpiresAt, RevokedAt | ✅ Session tracking (ISO 27001 A.9.2.5). ✅ Session revocation support. ✅ Device/IP audit. ✅ IsActive computed property. | Missing LastActivityAt for idle session timeout enforcement. Missing filtered index for active sessions queries. |
| **outbound_email_logs** | (exists) | ✅ Outbound email audit trail. | Needs index on (SentAt, Status) for compliance queries. |
| **notifications** / **notification_recipients** | (exist) | ✅ ISO 9001 communication tracking. | Needs read-receipt confirmation tracking (partially covered). |

### 2. ISO 27001 Security Control Gaps

| Control | Requirement | Current State | Gap |
|---------|------------|---------------|-----|
| **A.5.1.1** | Information security policy document | No formal policy document repository | ❌ Missing — Phase 7 |
| **A.8.2.1** | Classification of information | No data classification on entities | ❌ Missing — Phase 5 |
| **A.8.2.3** | Handling of assets | No data retention/deletion schedules | ❌ Missing — Phase 5 |
| **A.9.2.1** | User registration and de-registration | User lifecycle exists but no formal access request workflow | ⚠️ Partial |
| **A.9.2.2** | Access provisioning | CSV import exists but no approval workflow | ⚠️ Partial |
| **A.9.2.4** | Management of secret authentication information | Password reset exists, no formal password policy configuration object | ⚠️ Partial |
| **A.9.2.5** | Review of user access rights | No periodic access review workflow | ❌ Missing |
| **A.9.4.1** | Information access restriction | JWT + role-based access exists | ✅ Complete |
| **A.9.4.2** | Secure log-on procedures | MFA + lockout exists | ✅ Complete |
| **A.9.4.3** | Password management system | Password history exists; no formal password complexity policy object | ⚠️ Partial — Phase 2 |
| **A.12.4.1** | Event logging | audit_logs exists, audit service with auto-enrichment exists | ✅ Mostly Complete |
| **A.12.4.2** | Protection of log information | Append-only enforced in DbContext SaveChangesAsync | ✅ Complete |
| **A.12.4.3** | Administrator and operator logs | Already captured via audit service | ✅ Complete |
| **A.12.6.1** | Management of technical vulnerabilities | No vulnerability/incident tracking system | ❌ Missing — Phase 6 |
| **A.12.7.1** | Information systems audit controls | Audit log search API exists (AuditController with filters + export) | ✅ Complete |
| **A.16.1.1** | Responsibilities and procedures for incident management | No incident management system | ❌ Missing — Phase 6 |
| **A.16.1.5** | Response to information security incidents | No incident response workflow | ❌ Missing — Phase 6 |
| **A.17.1.1** | Planning information security continuity | No backup/DR documentation or logging | ❌ Missing — Phase 4 |
| **A.17.1.2** | Implementing information security continuity | No redundant infrastructure logging | ❌ Missing — Phase 4 |
| **A.17.1.3** | Verify, review and evaluate information security continuity | No backup verification/restore testing | ❌ Missing — Phase 8 |
| **A.18.1.1** | Identification of applicable legislation | No compliance mapping database | ❌ Missing — Phase 10 |
| **A.18.1.4** | Privacy and protection of PII | ConsentToMonitoring field missing from users | ❌ Missing — Phase 5 |

### 3. ISO 9001 Quality Management Gaps

| Clause | Requirement | Current State | Gap |
|--------|------------|---------------|-----|
| **7.5.1** | Documented information (general) | No policy document management system | ❌ Missing — Phase 7 |
| **7.5.2** | Creating and updating documents | No version control for institutional documents | ❌ Missing — Phase 7 |
| **7.5.3** | Control of documented information | No access control or approval workflow for documents | ❌ Missing — Phase 7 |
| **9.1.1** | Monitoring, measurement, analysis and evaluation | No compliance dashboard | ❌ Missing — Phase 10 |
| **10.2.1** | Nonconformity and corrective action | No incident/corrective action tracking | ❌ Missing — Phase 6 |

### 4. Required New Tables

| Table Name | Purpose | ISO Reference | Phase |
|-----------|---------|---------------|-------|
| **login_activity_logs** | Track all login attempts (success/failure), source IP, user agent, device info, geolocation, and risk score | ISO 27001 A.12.4.1 — Event logging | Phase 3 |
| **backup_logs** | Record all backup operations: type, size, duration, status, verification result, checksum | ISO 27001 A.17.1.2 — Redundancy | Phase 4 |
| **incident_logs** | Security incidents: severity, category, status, reported by, assigned to, resolution, timeline | ISO 27001 A.16.1.5 — Response | Phase 6 |
| **policy_documents** | ISO 9001 documented information: versioned policy documents with access control, approval workflow | ISO 9001 7.5 — Documented Information | Phase 7 |
| **backup_verification_logs** | Verify backup integrity: restore test results, checksums, timestamps, duration | ISO 27001 A.17.1.3 — Verification | Phase 8 |
| **data_classification_entries** | Tag entities with classification: Public, Internal, Confidential, Restricted | ISO 27001 A.8.2.1 — Classification | Phase 5 |

### 5. New Columns to Add to Existing Tables

| Table | Column to Add | Type | Purpose | Phase |
|-------|--------------|------|---------|-------|
| audit_logs | CorrelationId | NVARCHAR(64) NULL | Distributed tracing across services | Phase 1 |
| audit_logs | Severity | NVARCHAR(20) NULL DEFAULT 'Info' | Event severity classification | Phase 1 |
| audit_logs | EventCategory | NVARCHAR(50) NULL | Security/Academic/Financial/System | Phase 1 |
| users | LastPasswordChangedAt | DATETIME2 NULL | Password ageing policy | Phase 2 |
| users | ConsentToMonitoring | BIT NULL DEFAULT 0 | GDPR/privacy consent | Phase 5 |
| users | DataRetentionDate | DATETIME2 NULL | Data lifecycle management | Phase 5 |
| password_history | ExpiresAt | DATETIME2 NULL | Automatic history archival | Phase 2 |
| user_sessions | LastActivityAt | DATETIME2 NULL | Idle session timeout tracking | Phase 2 |

### 6. New Indexes Required

| Table | Index Name | Columns | Type | Purpose |
|-------|-----------|---------|------|---------|
| audit_logs | IX_audit_logs_event_category | EventCategory INCLUDE (OccurredAt) | Non-clustered | Compliance filtering by event type |
| audit_logs | IX_audit_logs_correlation_id | CorrelationId | Non-clustered | Distributed trace lookup |
| audit_logs | IX_audit_logs_severity_occurred_at | Severity, OccurredAt | Non-clustered | Severity-based filtering |
| password_history | IX_password_history_user_created | (UserId, CreatedAt DESC) | Non-clustered | Fast last-N query for reuse check |
| user_sessions | IX_user_sessions_active | UserId INCLUDE (ExpiresAt, RevokedAt) FILTER (RevokedAt IS NULL) | Filtered non-clustered | Active session queries |
| outbound_email_logs | IX_outbound_email_sent_at_status | SentAt, Status | Non-clustered | Email compliance queries |

### 7. Risk Assessment

| Risk | Severity | Impact | Mitigation | Phase |
|------|----------|--------|------------|-------|
| No incident tracking capability | **High** | Cannot demonstrate ISO 27001 A.16 compliance | Create incident_logs table + management UI | Phase 6 |
| No backup/DR logging | **High** | Cannot demonstrate ISO 27001 A.17 compliance | Create backup_logs table + scheduler design | Phase 4 |
| No formal password policy object | **Medium** | Weak password enforcement | Create PasswordPolicy configuration service | Phase 2 |
| No idle session timeout enforcement | **Medium** | Session hijacking risk | Add LastActivityAt tracking + timeout check | Phase 2 |
| No data classification scheme | **Medium** | Cannot demonstrate ISO 27001 A.8 compliance | Create data_classification_entries table | Phase 5 |
| No policy document management | **Medium** | Cannot demonstrate ISO 9001 7.5 compliance | Create policy_documents table + version control | Phase 7 |
| Missing CorrelationId in audit_logs | **Low** | Harder to trace cross-service flows | Add CorrelationId column to audit_logs | Phase 1 |
| Missing severity/category in audit_logs | **Low** | Harder to filter compliance events | Add Severity + EventCategory columns | Phase 1 |

### ✅ Validation Summary

- **Existing audit_logs table** already includes ActorRole (NVARCHAR(64)), UserAgent (NVARCHAR(1024)), DeviceInfo (NVARCHAR(512)) — Phase 1 audit enhancement requirements are substantially met already.
- **Audit immutability** is already enforced at the DbContext level via `EnforceImmutableAuditLogs()` — rows cannot be updated or deleted.
- **AuditController** already provides GET /audit/logs with filters (query, actorUserId, action, entityName, fromUtc, toUtc, page, pageSize) and GET /audit/logs/export/{format} (CSV, Excel, PDF).
- **AuditService** already auto-enriches records with ActorUserId, ActorRole, IP, User-Agent from runtime HttpContext.
- **MFA, account lockout, password history** are already implemented in the User entity.
- **SecurityHeadersMiddleware** already active with HSTS, CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy.
- **Rate limiting** is configured (global: 100 req/min, auth: 10 req/min) with sliding window.
- **6 new tables** needed across ISO phases.
- **8 new columns** to add to 4 existing tables.
- **6 new indexes** to optimize compliance queries.
- **Phase dependency graph**: Phase 1 (Audit enhancements) → Phase 2 (Security) ← Phase 3 (Activity Monitoring) → Phase 6 (Incidents) can be independent. Phase 4 (Backup) and Phase 7 (Documents) are independent. Phase 5 (Data Protection) needs Phase 1 audit foundations. Phase 8 (Backup Validation) depends on Phase 4. Phase 10 (Compliance Dashboard) depends on all prior phases.
- **No breaking changes** required — all changes are additive (new columns, new tables, new indexes).

---------------------------------------------------------------------

PHASE 1 — AUDIT LOGGING (CRITICAL)

Enhance audit_logs:

ADD:
- ActorRole NVARCHAR(50)
- UserAgent NVARCHAR(512)
- DeviceInfo NVARCHAR(512)

Ensure:
- Index on ActorRole
- Optimized composite indexes

Backend:
- Auto capture:
  ✔ UserId
  ✔ Role
  ✔ IP address
  ✔ User-Agent

Enforce:
- Immutable logs (block UPDATE/DELETE)

APIs:
- GET /audit-logs (with filters)
- Export (CSV, Excel, PDF)

UI:
- Audit dashboard with filters

Validation:
- Logs auto-recorded
- No modification allowed

---------------------------------------------------------------------

PHASE 2 — SECURITY

Implement:
- Strong password policy
- Prevent password reuse (password_history)
- Session revocation (using user_sessions)
- Session timeout
- Login/logout audit tracking

Admin:
- Active sessions screen

---------------------------------------------------------------------

PHASE 3 — USER ACTIVITY MONITORING

Create:
LoginActivityLogs

Track:
- Failed logins
- Suspicious activity

Dashboard:
- Trends
- Active sessions

---------------------------------------------------------------------

PHASE 4 — BACKUP & DR

Create:
BackupLogs

Add:
- Scheduler design
- Restore API
- Monitoring UI

---------------------------------------------------------------------

PHASE 5 — DATA PROTECTION

Implement:
- Encryption service (design)
- Data masking in UI

DO NOT break schema

---------------------------------------------------------------------

PHASE 6 — INCIDENT MANAGEMENT

Create:
IncidentLogs

Add:
- Severity
- Status flow
- Admin panel

---------------------------------------------------------------------

PHASE 7 — DOCUMENT MANAGEMENT

Create:
PolicyDocuments

Add:
- Version tracking
- Access control

---------------------------------------------------------------------

PHASE 8 — BACKUP VALIDATION

Add:
- Backup verification logs
- Restore test logs

---------------------------------------------------------------------

PHASE 9 — DATA INTEGRITY

Ensure:
- Transaction safety
- Audit financial + academic actions

---------------------------------------------------------------------

PHASE 10 — COMPLIANCE DASHBOARD

Create dashboard:
- Audit summary
- Security alerts
- Backup status
- Active users

---------------------------------------------------------------------

FINAL VALIDATION

Ensure:
✅ No regression
✅ Tenant isolation intact
✅ Audit coverage complete
✅ Security enforced
✅ Performance optimized
✅ EF migrations valid

---------------------------------------------------------------------

EXECUTION INSTRUCTION:

Start from PHASE 0.
Execute phases sequentially.
After EACH phase:
✔ Complete implementation
✔ Update documentation
✔ Simulate commit + push + pull
✔ Then move to next phase

DO NOT SKIP ANY STEP.
KEEP THE SYSTEM STABLE.
