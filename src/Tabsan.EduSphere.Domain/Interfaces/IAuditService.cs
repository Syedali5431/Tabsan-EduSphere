using Tabsan.EduSphere.Domain.Auditing;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>
/// Service interface for writing audit log entries.
/// Abstracted here so the Application layer can fire audit events
/// without depending on any infrastructure concern.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Writes an audit log entry for a privileged action.
    /// This is a fire-and-forget async operation — callers do not need to await
    /// completion before returning a response to the client.
    /// </summary>
    Task LogAsync(AuditLog entry, CancellationToken ct = default);

    /// <summary>
    /// Searches audit logs using optional filters and returns paged results.
    /// Intended for compliance/audit-history views.
    /// </summary>
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(
        string? query = null,
        Guid? actorUserId = null,
        string? action = null,
        string? entityName = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int page = 1,
        int pageSize = 50,
        // Phase 1 - ISO Audit Enhancement filter parameters
        string? actorRole = null,
        string? severity = null,
        string? eventCategory = null,
        string? correlationId = null,
        CancellationToken ct = default);
}

