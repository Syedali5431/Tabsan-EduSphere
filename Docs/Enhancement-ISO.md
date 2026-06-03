# Enhancement ISO Roadmap

## Objective
Enhance the app for School, College and University management system to achieve ISO readiness (particularly ISO 9001 and ISO 27001 alignment) by implementing missing compliance, security, audit, and operational controls.

The system already supports multi-institute logic (School, College and University, College, School), strong validation, and role-based access. This phase focuses on adding governance, security, and compliance features required for certification.

## Phase 1: Audit Logging System (CRITICAL)
Implement a complete audit logging module:
- Capture:
  - User ID
  - Role
  - Action performed (Create, Update, Delete, View, Login, Logout, etc.)
  - Entity affected (Student, Course, Payment, Result, etc.)
  - Timestamp
  - IP Address
  - Device/User-Agent
  - Before/After values (for updates)
- Store logs in MSSQL table:
  - AuditLogs
- Features:
  - Filterable UI (by user, module, date)
  - Export to CSV/Excel/PDF
  - Immutable logs (no edit/delete allowed)

### Phase 1 Implementation Summary
- Enhanced existing audit domain model to capture additional ISO-relevant context:
  - ActorRole
  - UserAgent
- Implemented automatic runtime audit-context enrichment in infrastructure:
  - auto-captures ActorUserId from claims when missing
  - auto-captures ActorRole from claims when missing
  - auto-captures IP Address (X-Forwarded-For fallback + remote IP)
  - auto-captures User-Agent from request headers
- Enforced immutable audit behavior at persistence layer:
  - blocked Update/Delete operations for audit log rows in DbContext
- Extended API audit module:
  - searchable logs endpoint includes actor role and user-agent data
  - added export endpoints:
    - CSV
    - Excel
    - PDF
  - added audit trail entries for view/export operations on audit logs
- Added web/portal audit monitoring UI:
  - new Audit Logs page with filter controls:
    - query
    - actor user id
    - action
    - entity/module
    - date range (UTC)
    - pagination/page size
  - export actions from UI:
    - CSV
    - Excel
    - PDF
- Integrated sidebar/menu route support for advanced audit screen:
  - mapped advanced_audit key to portal Audit Logs action

### Phase 1 Validation Summary
- Solution build status: PASS
  - full solution compilation succeeded after Phase 1 changes
- Database migration status: PASS
  - generated EF migration: PhaseISO1AuditLoggingEnhancements
- Regression posture:
  - additive-only schema update
  - no route removals
  - no GPA/CGPA logic changes
  - tenant/campus isolation behavior preserved
  - backward compatibility maintained across School, College, and University modes

## Phase 2: Security and Access Control Enhancements
- Enforce strong password policy:
  - Minimum length
  - Upper/lowercase
  - Numbers + special characters
- Implement:
  - Account lockout after failed attempts
  - Session timeout
  - Optional 2FA (already partially supported)
- Add:
  - Role-based access review screen
  - Admin ability to revoke sessions

## Phase 3: User Activity Monitoring
- Track:
  - Login attempts (success/failure)
  - Suspicious activity (multiple failed logins)
  - Concurrent sessions
- Create:
  - Security dashboard showing:
    - Active sessions
    - Failed login trends

## Phase 4: Backup and Disaster Recovery
- Implement:
  - Automatic MSSQL backup scheduler
  - Backup logs table
- Provide:
  - Backup download option
  - Restore process (admin-only)
- Add:
  - Backup status monitoring UI

## Phase 5: Data Protection and Privacy
- Identify sensitive data fields:
  - Passwords
  - Personal info (CNIC, phone, email)
- Implement:
  - Encryption for sensitive fields (at rest where applicable)
  - Secure hashing for passwords
- Mask sensitive data in UI:
  - Show partial values only where needed

## Phase 6: Incident and Error Management
- Create incident logging system:
  - Capture errors/exceptions
  - Categorize severity (Low / Medium / High / Critical)
- Add:
  - Admin incident panel
  - Status tracking (Open, In Progress, Resolved)

## Phase 7: Compliance Documentation Support (SYSTEM LEVEL)
Add modules or storage for:
- Security Policy documents
- SOP (Standard Operating Procedures)
- Risk Assessment records
- Data classification definitions

Features:
- Upload/download documents
- Version tracking
- Access control

## Phase 8: Backup Validation and Testing Logs
- Record:
  - Backup success/failure logs
  - Restore test logs
- Ensure:
  - Ability to demonstrate recovery process

## Phase 9: Data Integrity and Validation Enhancements
- Ensure:
  - All critical operations are logged
  - Transactions are consistent (no partial failure states)
- Add:
  - Validation audit trails for financial and academic operations

## Phase 10: Reporting and Compliance Dashboard
Create ISO Compliance Dashboard:
- Show:
  - Audit logs summary
  - Security events
  - Backup status
  - Active users
  - Policy compliance indicators

## Phase 11: Maintain Existing Constraints
- DO NOT:
  - Break School, College and University behavior
  - Modify GPA/CGPA logic
  - Remove existing routes or APIs
- MUST:
  - Keep tenant/campus isolation
  - Keep additive-only changes (no breaking schema changes)
  - Maintain backward compatibility

## Phase 12: Database Requirements (MSSQL)
Add new tables:
- AuditLogs
- LoginActivityLogs
- IncidentLogs
- BackupLogs
- PolicyDocuments

Ensure:
- Indexed for performance
- Scalable for large data (Schools, Colleges and Universities)

## Validation Requirements
- All security features must be testable
- Audit logs must capture all critical actions
- Backup/restore must be verifiable
- No regression in existing modules
- Maintain full compatibility with School / College / School, College and University modes

## End Goal
System becomes ISO-ready by adding:
- Auditability
- Security controls
- Compliance tracking
- Operational governance

Ready for:
- School, College and University audits
- Enterprise deployment
- Future ISO 9001 / ISO 27001 certification process

Keep the app logic and working same and do not mess up the app.
