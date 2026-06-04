using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Documents;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

public class PolicyDocumentRepository : IPolicyDocumentRepository
{
    private readonly ApplicationDbContext _db;
    public PolicyDocumentRepository(ApplicationDbContext db) => _db = db;

    public Task<PolicyDocument?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.PolicyDocuments.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IList<PolicyDocument>> GetAllAsync(CancellationToken ct = default)
        => await _db.PolicyDocuments.OrderByDescending(x => x.CreatedAt).AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(PolicyDocument doc, CancellationToken ct = default)
        => await _db.PolicyDocuments.AddAsync(doc, ct);

    public void Update(PolicyDocument doc) => _db.PolicyDocuments.Update(doc);

    public async Task AddVersionAsync(PolicyDocumentVersion version, CancellationToken ct = default)
        => await _db.PolicyDocumentVersions.AddAsync(version, ct);

    public async Task<IList<PolicyDocumentVersion>> GetVersionsAsync(Guid documentId, CancellationToken ct = default)
        => await _db.PolicyDocumentVersions.Where(x => x.DocumentId == documentId).OrderByDescending(x => x.VersionNumber).AsNoTracking().ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
