using Tabsan.EduSphere.Domain.Backup;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>Phase 4: Repository contract for backup operation logging.</summary>
public interface IBackupLogRepository
{
    Task<BackupLog?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(BackupLog entry, CancellationToken ct = default);
    void Update(BackupLog entry);
    Task SaveChangesAsync(CancellationToken ct = default);
}
