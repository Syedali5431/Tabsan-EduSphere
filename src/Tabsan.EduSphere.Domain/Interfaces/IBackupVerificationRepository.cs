using Tabsan.EduSphere.Domain.Backup;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>Phase 8: Repository contract for backup verification logs.</summary>
public interface IBackupVerificationRepository
{
    Task<IList<BackupVerificationLog>> GetByBackupLogIdAsync(Guid backupLogId, CancellationToken ct = default);
    Task<IList<BackupVerificationLog>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(BackupVerificationLog entry, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
