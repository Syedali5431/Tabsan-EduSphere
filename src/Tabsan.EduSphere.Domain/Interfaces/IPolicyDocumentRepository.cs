using Tabsan.EduSphere.Domain.Documents;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>Phase 7: Repository contract for policy documents and versions.</summary>
public interface IPolicyDocumentRepository
{
    Task<PolicyDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IList<PolicyDocument>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(PolicyDocument doc, CancellationToken ct = default);
    void Update(PolicyDocument doc);
    Task AddVersionAsync(PolicyDocumentVersion version, CancellationToken ct = default);
    Task<IList<PolicyDocumentVersion>> GetVersionsAsync(Guid documentId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
