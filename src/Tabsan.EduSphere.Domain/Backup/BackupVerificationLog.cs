using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Backup;

/// <summary>
/// Phase 8: Immutable record of each backup verification test.
/// Provides structured evidence for ISO 27001 A.12.3.1 backup restoration
/// testing and integrity validation compliance.
/// Rows are append-only — never updated or deleted after creation.
/// </summary>
public class BackupVerificationLog : BaseEntity
{
    /// <summary>FK to the backup_logs record being verified.</summary>
    public Guid BackupLogId { get; private set; }

    /// <summary>Type of verification: Checksum, Restore, IntegrityCheck.</summary>
    public string VerificationType { get; private set; } = default!;

    /// <summary>UTC timestamp when the verification was performed.</summary>
    public DateTime VerifiedAt { get; private set; }

    /// <summary>Identifier of the person or system that performed the verification.</summary>
    public string? VerifiedBy { get; private set; }

    /// <summary>True if the verification passed, false otherwise.</summary>
    public bool IsSuccessful { get; private set; }

    /// <summary>Duration of the verification process in seconds.</summary>
    public int? DurationSeconds { get; private set; }

    /// <summary>Description of any issues found during verification.</summary>
    public string? Issues { get; private set; }

    /// <summary>SHA-256 checksum verified against the backup file.</summary>
    public string? VerifiedChecksum { get; private set; }

    private BackupVerificationLog() { }

    /// <summary>Records a backup verification test.</summary>
    public BackupVerificationLog(
        Guid backupLogId,
        string verificationType,
        DateTime verifiedAt,
        bool isSuccessful,
        string? verifiedBy = null,
        int? durationSeconds = null,
        string? issues = null,
        string? verifiedChecksum = null)
    {
        BackupLogId = backupLogId;
        VerificationType = verificationType;
        VerifiedAt = verifiedAt;
        IsSuccessful = isSuccessful;
        VerifiedBy = verifiedBy;
        DurationSeconds = durationSeconds;
        Issues = issues;
        VerifiedChecksum = verifiedChecksum;
    }
}
