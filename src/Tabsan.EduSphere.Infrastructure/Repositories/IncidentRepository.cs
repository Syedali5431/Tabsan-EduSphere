using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Incidents;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

public class IncidentRepository : IIncidentRepository
{
    private readonly ApplicationDbContext _db;
    public IncidentRepository(ApplicationDbContext db) => _db = db;

    public Task<IncidentLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.IncidentLogs.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IList<IncidentLog>> GetAllAsync(CancellationToken ct = default)
        => await _db.IncidentLogs.OrderByDescending(x => x.ReportedAt).AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(IncidentLog entry, CancellationToken ct = default)
        => await _db.IncidentLogs.AddAsync(entry, ct);

    public void Update(IncidentLog entry) => _db.IncidentLogs.Update(entry);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
