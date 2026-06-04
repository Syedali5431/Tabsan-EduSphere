# Function List — Tabsan EduSphere

> Auto-maintained registry of all implemented functions, services, and API endpoints. No summaries — clean index only.

---

## Audit Logging (Phase 1 — ISO Enhancement)

| Function Name | Purpose | Location |
|---------------|---------|----------|
| `AuditLog` (entity) | Immutable audit record with bigint PK, append-only | `Domain/Auditing/AuditLog.cs` |
| `AuditLog.ctor()` | Creates audit entry with all Phase 1 fields (CorrelationId, Severity, EventCategory) | `Domain/Auditing/AuditLog.cs` |
| `IAuditService.LogAsync` | Writes an audit log entry for a privileged action | `Domain/Interfaces/IAuditService.cs` |
| `IAuditService.SearchAsync` | Searches audit logs with Phase 1 filters (actorRole, severity, eventCategory, correlationId) | `Domain/Interfaces/IAuditService.cs` |
| `AuditService.LogAsync` | Enriches and persists audit entries with auto-resolved CorrelationId, IP, role, user-agent, device info | `Infrastructure/Auditing/AuditService.cs` |
| `AuditService.SearchAsync` | Queryable audit log search with Phase 1 filter parameters and free-text matching across new fields | `Infrastructure/Auditing/AuditService.cs` |
| `TryResolveActorUserId` | Resolves actor user ID from JWT claims | `Infrastructure/Auditing/AuditService.cs` |
| `TryResolveActorRole` | Resolves actor role from JWT claims | `Infrastructure/Auditing/AuditService.cs` |
| `TryResolveIpAddress` | Resolves client IP from X-Forwarded-For header or connection | `Infrastructure/Auditing/AuditService.cs` |
| `TryResolveUserAgent` | Resolves User-Agent from request headers | `Infrastructure/Auditing/AuditService.cs` |
| `TryResolveDeviceInfo` | Resolves device info from Sec-CH-UA headers or User-Agent | `Infrastructure/Auditing/AuditService.cs` |
| `TryResolveCorrelationId` | Resolves CorrelationId from X-Correlation-Id header or HttpContext.TraceIdentifier | `Infrastructure/Auditing/AuditService.cs` |
| `AuditLogConfiguration.Configure` | EF Core fluent config for audit_logs table, columns, and indexes | `Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs` |
| `EnforceImmutableAuditLogs` | Blocks UPDATE/DELETE on audit_logs at DbContext level | `Infrastructure/Persistence/ApplicationDbContext.cs` |
| `AuditLogs` (DbSet) | EF Core DbSet for audit_logs table | `Infrastructure/Persistence/ApplicationDbContext.cs` |
| `SearchLogs` (endpoint) | `GET /api/v1/audit/logs` — filtered audit log search with Phase 1 params | `API/Controllers/AuditController.cs` |
| `ExportLogs` (endpoint) | `GET /api/v1/audit/logs/export/{format}` — CSV/Excel/PDF export with Phase 1 fields | `API/Controllers/AuditController.cs` |
| `BuildCsv` | Generates CSV byte array with CorrelationId, Severity, EventCategory columns | `API/Controllers/AuditController.cs` |
| `BuildExcel` | Generates Excel byte array with CorrelationId, Severity, EventCategory columns | `API/Controllers/AuditController.cs` |
| `BuildPdf` | Generates PDF byte array with Severity, Category columns | `API/Controllers/AuditController.cs` |
