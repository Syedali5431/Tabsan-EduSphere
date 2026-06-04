# Function List — Tabsan EduSphere

> Auto-maintained function/service/API registry.
> NO summaries — this file is a clean index of all implemented functionality.

---

## Audit Logging (Phase 1 — ISO Enhancement)

### Domain Layer

| File | Member | Signature |
|------|--------|-----------|
| `Domain/Auditing/AuditLog.cs` | `AuditLog` (class) | Immutable audit record with bigint PK, append-only |
| `Domain/Auditing/AuditLog.cs` | `.ctor()` | `AuditLog(string action, string entityName, string? entityId, Guid? actorUserId, string? oldValuesJson, string? newValuesJson, string? ipAddress, string? actorRole, string? userAgent, string? deviceInfo, string? correlationId, string severity, string? eventCategory)` |
| `Domain/Interfaces/IAuditService.cs` | `LogAsync` | `Task LogAsync(AuditLog entry, CancellationToken ct = default)` |
| `Domain/Interfaces/IAuditService.cs` | `SearchAsync` | `Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(string? query, Guid? actorUserId, string? action, string? entityName, DateTime? fromUtc, DateTime? toUtc, int page, int pageSize, string? actorRole, string? severity, string? eventCategory, string? correlationId, CancellationToken ct)` |

### Infrastructure Layer

| File | Member | Signature |
|------|--------|-----------|
| `Infrastructure/Auditing/AuditService.cs` | `LogAsync` | `Task LogAsync(AuditLog entry, CancellationToken ct = default)` |
| `Infrastructure/Auditing/AuditService.cs` | `SearchAsync` | `Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(string? query, Guid? actorUserId, string? action, string? entityName, DateTime? fromUtc, DateTime? toUtc, int page, int pageSize, string? actorRole, string? severity, string? eventCategory, string? correlationId, CancellationToken ct)` |
| `Infrastructure/Auditing/AuditService.cs` | `TryResolveActorUserId` | `static Guid? TryResolveActorUserId(ClaimsPrincipal? user)` |
| `Infrastructure/Auditing/AuditService.cs` | `TryResolveActorRole` | `static string? TryResolveActorRole(ClaimsPrincipal? user)` |
| `Infrastructure/Auditing/AuditService.cs` | `TryResolveIpAddress` | `static string? TryResolveIpAddress(HttpContext? context)` |
| `Infrastructure/Auditing/AuditService.cs` | `TryResolveUserAgent` | `static string? TryResolveUserAgent(HttpContext? context)` |
| `Infrastructure/Auditing/AuditService.cs` | `TryResolveDeviceInfo` | `static string? TryResolveDeviceInfo(HttpContext? context)` |
| `Infrastructure/Auditing/AuditService.cs` | `TryResolveCorrelationId` | `static string? TryResolveCorrelationId(HttpContext? context)` |
| `Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs` | `Configure` | `void Configure(EntityTypeBuilder<AuditLog> builder)` |
| `Infrastructure/Persistence/ApplicationDbContext.cs` | `EnforceImmutableAuditLogs` | `void EnforceImmutableAuditLogs()` |
| `Infrastructure/Persistence/ApplicationDbContext.cs` | `AuditLogs` (DbSet) | `DbSet<AuditLog> AuditLogs` |

### API Layer

| File | Endpoint | Method | Auth |
|------|----------|--------|------|
| `API/Controllers/AuditController.cs` | `GET /api/v1/audit/logs` | `SearchLogs` | SuperAdmin, Admin |
| `API/Controllers/AuditController.cs` | `GET /api/v1/audit/logs/export/{format}` | `ExportLogs` | SuperAdmin, Admin |
| `API/Controllers/AuditController.cs` | `BuildCsv` | `static byte[] BuildCsv(IReadOnlyList<AuditLog> items)` | — |
| `API/Controllers/AuditController.cs` | `BuildExcel` | `static byte[] BuildExcel(IReadOnlyList<AuditLog> items)` | — |
| `API/Controllers/AuditController.cs` | `BuildPdf` | `static byte[] BuildPdf(IReadOnlyList<AuditLog> items)` | — |

### Query Parameters (GET /api/v1/audit/logs)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `query` | `string?` | `null` | Free-text search across Action, EntityName, EntityId, IpAddress, ActorRole, Severity, EventCategory, CorrelationId |
| `actorUserId` | `Guid?` | `null` | Filter by specific user |
| `action` | `string?` | `null` | Exact match on action verb |
| `entityName` | `string?` | `null` | Exact match on entity type |
| `actorRole` | `string?` | `null` | Filter by actor role (Phase 1) |
| `severity` | `string?` | `null` | Filter by severity level (Phase 1) |
| `eventCategory` | `string?` | `null` | Filter by event category (Phase 1) |
| `correlationId` | `string?` | `null` | Filter by distributed trace ID (Phase 1) |
| `fromUtc` | `DateTime?` | `null` | Range start (UTC) |
| `toUtc` | `DateTime?` | `null` | Range end (UTC) |
| `page` | `int` | `1` | Page number |
| `pageSize` | `int` | `50` | Items per page (max 200) |

### Export Parameters (GET /api/v1/audit/logs/export/{format})

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `format` | `string` (route) | — | `csv`, `excel`, or `pdf` |
| `maxRows` | `int` | `2000` | Max rows to export (capped at 5000) |
| *(All filter params from /logs)* | — | — | Same filter parameters as search endpoint |

### Database Schema — audit_logs

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | `BIGINT` | PK, IDENTITY |
| `ActorUserId` | `UNIQUEIDENTIFIER` | NULL |
| `Action` | `NVARCHAR(100)` | NOT NULL |
| `ActorRole` | `NVARCHAR(64)` | NULL |
| `EntityName` | `NVARCHAR(100)` | NOT NULL |
| `EntityId` | `NVARCHAR(100)` | NULL |
| `OldValuesJson` | `NVARCHAR(MAX)` | NULL |
| `NewValuesJson` | `NVARCHAR(MAX)` | NULL |
| `OccurredAt` | `DATETIME2` | NOT NULL |
| `IpAddress` | `NVARCHAR(64)` | NULL |
| `UserAgent` | `NVARCHAR(1024)` | NULL |
| `DeviceInfo` | `NVARCHAR(1024)` | NULL |
| `CorrelationId` | `NVARCHAR(64)` | NULL (Phase 1) |
| `Severity` | `NVARCHAR(20)` | NOT NULL, DEFAULT 'Info' (Phase 1) |
| `EventCategory` | `NVARCHAR(50)` | NULL (Phase 1) |

### Database Indexes — audit_logs

| Index Name | Columns | Type |
|-----------|---------|------|
| `IX_audit_logs_occurred_at` | `OccurredAt` | Non-clustered |
| `IX_audit_logs_actor` | `ActorUserId` | Non-clustered |
| `IX_audit_logs_actor_role` | `ActorRole` | Non-clustered |
| `IX_audit_logs_entity_occurred_at` | `EntityName, OccurredAt` | Non-clustered composite |
| `IX_audit_logs_correlation_id` | `CorrelationId` | Filtered (Phase 1) |
| `IX_audit_logs_event_category` | `EventCategory` | Filtered (Phase 1) |
| `IX_audit_logs_severity_occurred_at` | `Severity, OccurredAt` | Filtered composite (Phase 1) |
| `IX_audit_logs_actor_role_occurred_at` | `ActorRole, OccurredAt` | Filtered composite (Phase 1) |

### Migrations

| Migration | Purpose |
|-----------|---------|
| `20260603134857_PhaseISO1AuditLoggingEnhancements.cs` | Added ActorRole, UserAgent columns + IX_audit_logs_actor_role index |
| `20260604043644_PhaseISO1AuditLoggingPart2.cs` | Added DeviceInfo, CorrelationId, Severity, EventCategory + 4 filtered indexes |
