using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.DataProtection;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>Phase 5: EF Core implementation of IDataClassificationRepository.</summary>
public class DataClassificationRepository : IDataClassificationRepository
{
    private readonly ApplicationDbContext _db;
    public DataClassificationRepository(ApplicationDbContext db) => _db = db;

    public async Task<IList<DataClassificationEntry>> GetByEntityAsync(string entityName, string? entityId, CancellationToken ct = default)
        => await _db.DataClassificationEntries
            .Where(x => x.EntityName == entityName && (entityId == null || x.EntityId == entityId))
            .OrderByDescending(x => x.ClassifiedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IList<DataClassificationEntry>> GetAllAsync(CancellationToken ct = default)
        => await _db.DataClassificationEntries
            .OrderByDescending(x => x.ClassifiedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task AddAsync(DataClassificationEntry entry, CancellationToken ct = default)
        => await _db.DataClassificationEntries.AddAsync(entry, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
