namespace Tabsan.EduSphere.Application.DTOs.Backup;

public sealed class CreateVerificationRequest
{
    public Guid BackupLogId { get; set; }
    public string VerificationType { get; set; } = "IntegrityCheck"; // IntegrityCheck or RestoreTest
    public bool IsSuccessful { get; set; } = true;
    public string? VerifiedBy { get; set; }
    public int? DurationSeconds { get; set; }
    public string? Issues { get; set; }
    public string? VerifiedChecksum { get; set; }
}

public sealed class BackupVerificationItemDto
{
    public Guid Id { get; set; }
    public Guid BackupLogId { get; set; }
    public string VerificationType { get; set; } = string.Empty;
    public DateTime VerifiedAt { get; set; }
    public string? VerifiedBy { get; set; }
    public bool IsSuccessful { get; set; }
    public int? DurationSeconds { get; set; }
    public string? Issues { get; set; }
    public string? VerifiedChecksum { get; set; }
}
