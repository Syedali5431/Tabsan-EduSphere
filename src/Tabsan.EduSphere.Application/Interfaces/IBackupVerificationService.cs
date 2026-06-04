using Tabsan.EduSphere.Application.DTOs.Backup;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>Phase 8: Backup verification service — ISO 27001 A.17.1.3.</summary>
public interface IBackupVerificationService
{
    Task<IList<BackupVerificationItemDto>> GetAllAsync(CancellationToken ct = default);
    Task<IList<BackupVerificationItemDto>> GetByBackupLogIdAsync(Guid backupLogId, CancellationToken ct = default);
    Task<BackupVerificationItemDto> CreateAsync(CreateVerificationRequest request, CancellationToken ct = default);
}
