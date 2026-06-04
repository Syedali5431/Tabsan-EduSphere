using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Backup;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>Phase 4: EF Core implementation of IBackupLogRepository.</summary>
public class BackupLogRepository : IBackupLogRepository
{
    private readonly ApplicationDbContext _db;
    public BackupLogRepository(ApplicationDbContext db) => _db = db;

    public Task<BackupLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.BackupLogs.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(BackupLog entry, CancellationToken ct = default)
        => await _db.BackupLogs.AddAsync(entry, ct);

    public void Update(BackupLog entry) => _db.BackupLogs.Update(entry);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
