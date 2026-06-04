using Tabsan.EduSphere.Application.DTOs.Backup;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Phase 4: Service for recording and querying backup operations.
/// ISO 27001 A.17.1.2 — documented backup procedures with verifiable logs.
/// </summary>
public interface IBackupService
{
    /// <summary>Returns paged backup logs with optional filters.</summary>
    Task<PagedBackupLogResult> GetLogsAsync(BackupLogFilterDto filter, CancellationToken ct = default);

    /// <summary>Records the start of a new backup operation.</summary>
    Task<BackupLogItemDto> RecordBackupStartAsync(RecordBackupRequest request, CancellationToken ct = default);

    /// <summary>Updates an existing backup record (completion, failure, verification).</summary>
    Task<BackupLogItemDto?> UpdateBackupStatusAsync(Guid id, UpdateBackupStatusRequest request, CancellationToken ct = default);

    /// <summary>Returns a summary of the latest backup status for each type.</summary>
    Task<BackupStatusSummaryDto> GetStatusSummaryAsync(CancellationToken ct = default);
}
