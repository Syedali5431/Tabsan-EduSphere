using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Backup;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

public class BackupVerificationRepository : IBackupVerificationRepository
{
    private readonly ApplicationDbContext _db;
    public BackupVerificationRepository(ApplicationDbContext db) => _db = db;

    public async Task<IList<BackupVerificationLog>> GetByBackupLogIdAsync(Guid backupLogId, CancellationToken ct = default)
        => await _db.BackupVerificationLogs.Where(x => x.BackupLogId == backupLogId).OrderByDescending(x => x.VerifiedAt).AsNoTracking().ToListAsync(ct);

    public async Task<IList<BackupVerificationLog>> GetAllAsync(CancellationToken ct = default)
        => await _db.BackupVerificationLogs.OrderByDescending(x => x.VerifiedAt).AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(BackupVerificationLog entry, CancellationToken ct = default)
        => await _db.BackupVerificationLogs.AddAsync(entry, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
