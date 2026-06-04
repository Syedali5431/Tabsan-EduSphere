using Tabsan.EduSphere.Domain.Activity;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>Phase 3: EF Core implementation of ILoginActivityRepository.</summary>
public class LoginActivityRepository : ILoginActivityRepository
{
    private readonly ApplicationDbContext _db;

    public LoginActivityRepository(ApplicationDbContext db) => _db = db;

    public async Task AddAsync(LoginActivityLog entry, CancellationToken ct = default)
        => await _db.LoginActivityLogs.AddAsync(entry, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
