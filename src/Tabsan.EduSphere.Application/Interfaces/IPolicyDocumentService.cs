using Tabsan.EduSphere.Application.DTOs.Documents;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>Phase 7: Policy document management with version tracking — ISO 9001 7.5.</summary>
public interface IPolicyDocumentService
{
    Task<IList<PolicyDocumentItemDto>> GetAllAsync(CancellationToken ct = default);
    Task<PolicyDocumentItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PolicyDocumentItemDto> CreateAsync(CreatePolicyDocumentRequest r, Guid createdBy, CancellationToken ct = default);
    Task<PolicyDocumentItemDto?> UpdateAsync(Guid id, UpdatePolicyDocumentRequest r, Guid changedBy, CancellationToken ct = default);
    Task<PolicyDocumentItemDto?> PublishAsync(Guid id, CancellationToken ct = default);
    Task<PolicyDocumentItemDto?> ArchiveAsync(Guid id, CancellationToken ct = default);
    Task<IList<PolicyDocumentVersionItemDto>> GetVersionsAsync(Guid documentId, CancellationToken ct = default);
}
