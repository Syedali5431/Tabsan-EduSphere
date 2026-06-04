namespace Tabsan.EduSphere.Application.DTOs.Backup;

/// <summary>Phase 4: Filter parameters for backup log queries.</summary>
public sealed class BackupLogFilterDto
{
    public string? BackupType { get; set; }
    public string? Status { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>Phase 4: Flat response DTO for a backup log record.</summary>
public sealed class BackupLogItemDto
{
    public Guid Id { get; set; }
    public string BackupType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public long? FileSizeBytes { get; set; }
    public int? DurationSeconds { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Checksum { get; set; }
    public string? InitiatedBy { get; set; }
}

/// <summary>Phase 4: Paged result wrapper for backup log queries.</summary>
public sealed class PagedBackupLogResult
{
    public List<BackupLogItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}

/// <summary>Phase 4: Request to record a new backup operation.</summary>
public sealed class RecordBackupRequest
{
    public string BackupType { get; set; } = "Full";
    public string FileName { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public string? InitiatedBy { get; set; }
}

/// <summary>Phase 4: Request to update a backup operation status.</summary>
public sealed class UpdateBackupStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public long? FileSizeBytes { get; set; }
    public int? DurationSeconds { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Checksum { get; set; }
}

/// <summary>Phase 4: Latest backup status summary for monitoring dashboard.</summary>
public sealed class BackupStatusSummaryDto
{
    public BackupLogItemDto? LastFullBackup { get; set; }
    public BackupLogItemDto? LastDifferentialBackup { get; set; }
    public BackupLogItemDto? LastLogBackup { get; set; }
    public bool HasRecentFullBackup => LastFullBackup?.CompletedAt >= DateTime.UtcNow.AddDays(-1);
    public bool HasFailedBackup => LastFullBackup?.Status == "Failed"
                                || LastDifferentialBackup?.Status == "Failed"
                                || LastLogBackup?.Status == "Failed";
}
