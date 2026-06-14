using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Backup;

/// <summary>
/// Phase 4: Immutable record of each database backup operation.
/// Provides structured queryable data for disaster recovery monitoring
/// and ISO 27001 A.12.3.1 backup compliance.
/// Rows are append-only — never updated or deleted after creation.
/// </summary>
public class BackupLog : BaseEntity
{
    /// <summary>Type of backup: Full, Differential, or TransactionLog.</summary>
    public string BackupType { get; private set; } = default!;

    /// <summary>Name of the backup file produced.</summary>
    public string FileName { get; private set; } = default!;

    /// <summary>Full path where the backup file was written.</summary>
    public string? FilePath { get; private set; }

    /// <summary>Size of the backup file in bytes.</summary>
    public long? FileSizeBytes { get; private set; }

    /// <summary>Duration of the backup operation in seconds.</summary>
    public int? DurationSeconds { get; private set; }

    /// <summary>Status of the backup: Success, Failed, or InProgress.</summary>
    public string Status { get; private set; } = default!;

    /// <summary>UTC timestamp when the backup operation started.</summary>
    public DateTime StartedAt { get; private set; }

    /// <summary>UTC timestamp when the backup operation completed.</summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>Error message if the backup failed.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>SHA-256 checksum of the backup file for integrity verification.</summary>
    public string? Checksum { get; private set; }

    /// <summary>Identifier of the user or system that initiated the backup.</summary>
    public string? InitiatedBy { get; private set; }

    private BackupLog() { }

    /// <summary>Records a backup operation with all available context.</summary>
    public BackupLog(
        string backupType,
        string fileName,
        string? filePath,
        string status,
        DateTime startedAt,
        string? initiatedBy = null)
    {
        BackupType = backupType;
        FileName = fileName;
        FilePath = filePath;
        Status = status;
        StartedAt = startedAt;
        InitiatedBy = initiatedBy;
    }

    /// <summary>Marks the backup as completed with file metadata.</summary>
    public void Complete(long fileSizeBytes, int durationSeconds, string? checksum = null)
    {
        FileSizeBytes = fileSizeBytes;
        DurationSeconds = durationSeconds;
        Checksum = checksum;
        Status = "Success";
        CompletedAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>Marks the backup as failed with an error reason.</summary>
    public void Fail(string errorMessage)
    {
        ErrorMessage = errorMessage;
        Status = "Failed";
        CompletedAt = DateTime.UtcNow;
        Touch();
    }
}
