# EduSphere — Audit Logging

This document describes how Tabsan EduSphere captures, stores, and protects audit logs for compliance and operational transparency.

---

## 1. Overview

EduSphere maintains two separate logging systems:

| System | Purpose | Storage |
|---|---|---|
| **Business Audit Logs** | Compliance-grade record of every privileged action (who did what, when, from where) | MSSQL `audit_logs` table |
| **Operational Logs** | General application diagnostics, errors, and tracing | Serilog → Console + rolling files (`logs/app-.log`) |

Only the **Business Audit Logs** are considered the official compliance audit trail.

---

## 2. Audit Log Storage

### 2.1 Database Table: `audit_logs`

All audit records are written to the **`audit_logs`** table in the application's MSSQL database. This table is **append-only** — rows can never be updated or deleted. Any attempt to modify or remove an audit record throws an exception and is blocked at the database context level.

**Connection:** The database is specified by `ConnectionStrings:DefaultConnection` in the API's `appsettings.Production.json` (or via an environment variable).

### 2.2 Table Schema

| Column | Type | Description |
|---|---|---|
| `Id` | `bigint IDENTITY` | Sequential primary key |
| `ActorUserId` | `uniqueidentifier` (nullable) | Who performed the action (null for system-initiated events) |
| `ActorRole` | `nvarchar(64)` (nullable) | Role at time of action (SuperAdmin, Admin, Faculty, Student, etc.) |
| `Action` | `nvarchar(100)` | Verb describing the operation (e.g., Login, PublishResult, UploadLicense, ImportAttendance) |
| `EntityName` | `nvarchar(100)` | Type of entity affected (e.g., User, Assignment, LicenseState, AuditLog) |
| `EntityId` | `nvarchar(100)` (nullable) | Primary key of the affected entity |
| `OldValuesJson` | `nvarchar(max)` (nullable) | JSON snapshot of the entity **before** the action (null for creates) |
| `NewValuesJson` | `nvarchar(max)` (nullable) | JSON snapshot of the entity **after** the action (null for deletes) |
| `OccurredAt` | `datetime2` | UTC timestamp of the event |
| `IpAddress` | `nvarchar(64)` (nullable) | Client IP address captured from the HTTP request |
| `UserAgent` | `nvarchar(1024)` (nullable) | Browser or client user-agent string |
| `DeviceInfo` | `nvarchar(1024)` (nullable) | Device information (browser/OS/app) |
| `CorrelationId` | `nvarchar(64)` (nullable) | Correlation ID for distributed tracing across services |
| `Severity` | `nvarchar(20)` | Classification: `Info`, `Warning`, `Error`, `Critical` |
| `EventCategory` | `nvarchar(50)` (nullable) | Grouping: `Security`, `Academic`, `Financial`, `System`, `Compliance`, `UserManagement` |

### 2.3 Indexes

| Index Name | Columns | Purpose |
|---|---|---|
| `PK_audit_logs` | `Id` | Clustered primary key |
| `IX_audit_logs_actor` | `ActorUserId` | Lookup by user |
| `IX_audit_logs_occurred_at` | `OccurredAt` | Time-range queries |
| `IX_audit_logs_entity_occurred_at` | `EntityName`, `OccurredAt` | Per-entity timeline queries |
| `IX_audit_logs_event_category` | `EventCategory` | Filter by event category (filtered index, excludes nulls) |
| `IX_audit_logs_correlation_id` | `CorrelationId` | Distributed tracing (filtered index, excludes nulls) |
| `IX_audit_logs_severity_occurred_at` | `Severity`, `OccurredAt` | Severity-based time-range queries (filtered index, excludes nulls) |

---

## 3. Immutability Guarantee

The `audit_logs` table is protected at the application level:

- The `ApplicationDbContext` overrides `SaveChangesAsync` and calls `EnforceImmutableAuditLogs()` before every write.
- Any entity tracked as `Modified` or `Deleted` on the `AuditLog` type immediately throws an `InvalidOperationException`.
- Only `Added` state is permitted — rows are append-only by design.
- There are no `UPDATE` or `DELETE` permissions needed for this table in normal operation.

```csharp
// src/Tabsan.EduSphere.Infrastructure/Persistence/ApplicationDbContext.cs
private void EnforceImmutableAuditLogs()
{
    foreach (var entry in ChangeTracker.Entries<AuditLog>())
    {
        if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
        {
            throw new InvalidOperationException(
                "Audit logs are immutable and cannot be updated or deleted.");
        }
    }
}
```

---

## 4. What Gets Audited

Examples of audited actions (not exhaustive):

| Event Category | Example Actions |
|---|---|
| **Security** | Login, Logout, FailedLogin, PasswordChange, MfaEnroll, MfaDisable |
| **UserManagement** | CreateUser, UpdateUser, DeleteUser, AssignRole, RevokeRole |
| **Academic** | PublishResult, ImportAttendance, CreateAssignment, SubmitGrade |
| **Financial** | CreatePayment, RefundPayment, ApplyDiscount |
| **System** | UploadLicense, DatabaseMigration, ConfigurationChange |
| **Compliance** | Export (audit log exports themselves are audited) |

Each entry captures the full before/after state of the affected entity in JSON format (`OldValuesJson` / `NewValuesJson`).

---

## 5. Accessing Audit Logs

### 5.1 Via the Web Portal

Users with the **SuperAdmin** or **Admin** role can access audit logs through the web portal:

- Navigate to the **Audit Logs** page in the portal
- Filter by: free-text search, actor user, action, entity name, date range, severity, event category, correlation ID
- Export to **CSV**, **Excel (.xlsx)**, or **PDF**

### 5.2 Via the API

```
GET /api/v1/audit
Authorization: Bearer <token>
Roles required: SuperAdmin, Admin
```

Query parameters: `query`, `actorUserId`, `action`, `entityName`, `fromUtc`, `toUtc`, `page`, `pageSize`, `actorRole`, `severity`, `eventCategory`, `correlationId`

Export endpoints:
- `GET /api/v1/audit/export/csv`
- `GET /api/v1/audit/export/excel`
- `GET /api/v1/audit/export/pdf`

### 5.3 Direct Database Access

For DBA-level queries, the `audit_logs` table can be queried directly in MSSQL:

```sql
SELECT TOP 100 *
FROM audit_logs
WHERE OccurredAt >= DATEADD(day, -7, GETUTCDATE())
ORDER BY OccurredAt DESC;
```

---

## 6. Retention Policy

| Tier | Duration | Location |
|---|---|---|
| **Online** | 24 months | `audit_logs` table in primary database |
| **Archive** | 7 years | Recommended to archive to cold storage / backup after 24 months |

> **Note:** Automated archival is not built into the application. The DBA should set up a scheduled job (e.g., SQL Agent) to move records older than 24 months to an archive table or external storage.

---

## 7. Operational Logging (Separate from Audit)

EduSphere also uses **Serilog** for general application diagnostics. These are NOT compliance audit records.

| Sink | Path/Configuration | Retention |
|---|---|---|
| **Console** | Standard output | Ephemeral (container/process lifetime) |
| **Rolling File** | `logs/app-.log` | Daily rotation, 30 files retained |

---

## 8. Key Source Files

| File | Purpose |
|---|---|
| `src/Tabsan.EduSphere.Domain/Auditing/AuditLog.cs` | Domain entity (immutable record) |
| `src/Tabsan.EduSphere.Domain/Interfaces/IAuditService.cs` | Service interface |
| `src/Tabsan.EduSphere.Infrastructure/Auditing/AuditService.cs` | Implementation: writes to DB, enriches with HTTP context |
| `src/Tabsan.EduSphere.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs` | EF Core table/index mapping |
| `src/Tabsan.EduSphere.Infrastructure/Persistence/ApplicationDbContext.cs` | Immutability enforcement |
| `src/Tabsan.EduSphere.API/Controllers/AuditController.cs` | Search & export endpoints |
| `src/Tabsan.EduSphere.Web/Controllers/PortalController.cs` | Web portal audit views & exports |
| `Scripts/01-Schema-Current.sql` | SQL table definition |

---

## 9. Frequently Asked Questions

**Q: Can audit logs be deleted?**
A: No. The application enforces immutability. Any attempt to delete or modify an audit row throws an error. Direct database access would be required to remove records, and such access should be restricted to DBAs.

**Q: Where are the logs physically stored?**
A: In the same MSSQL database as the rest of the EduSphere application data, under the `audit_logs` table.

**Q: Are audit logs included in database backups?**
A: Yes, since they reside in the same database, they are included in all standard database backups.

**Q: How do I prove logs haven't been tampered with?**
A: Audit logs are append-only and the application blocks modifications. For additional tamper-proofing, consider enabling SQL Server Auditing at the database level or using ledger tables (SQL Server 2022+).
