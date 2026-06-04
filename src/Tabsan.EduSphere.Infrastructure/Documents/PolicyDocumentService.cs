using Tabsan.EduSphere.Application.DTOs.Documents;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Documents;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Infrastructure.Documents;

public class PolicyDocumentService : IPolicyDocumentService
{
    private readonly IPolicyDocumentRepository _repo;
    public PolicyDocumentService(IPolicyDocumentRepository repo) => _repo = repo;

    public async Task<IList<PolicyDocumentItemDto>> GetAllAsync(CancellationToken ct = default)
        => (await _repo.GetAllAsync(ct)).Select(Map).ToList();

    public async Task<PolicyDocumentItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => MapOrNull(await _repo.GetByIdAsync(id, ct));

    public async Task<PolicyDocumentItemDto> CreateAsync(CreatePolicyDocumentRequest r, Guid createdBy, CancellationToken ct = default)
    {
        var doc = new PolicyDocument(r.Title, r.Content, r.Category, r.AccessLevel, createdBy, r.Description);
        await _repo.AddAsync(doc, ct);
        await _repo.SaveChangesAsync(ct);
        // Record initial version
        await _repo.AddVersionAsync(new PolicyDocumentVersion(doc.Id, 1, r.Content, createdBy, "Initial version"), ct);
        await _repo.SaveChangesAsync(ct);
        return Map(doc);
    }

    public async Task<PolicyDocumentItemDto?> UpdateAsync(Guid id, UpdatePolicyDocumentRequest r, Guid changedBy, CancellationToken ct = default)
    {
        var doc = await _repo.GetByIdAsync(id, ct);
        if (doc is null) return null;
        var newVersion = doc.Version + 1;
        doc.UpdateContent(r.Content, newVersion);
        _repo.Update(doc);
        await _repo.AddVersionAsync(new PolicyDocumentVersion(id, newVersion, r.Content, changedBy, r.ChangeNotes), ct);
        await _repo.SaveChangesAsync(ct);
        return Map(doc);
    }

    public async Task<PolicyDocumentItemDto?> PublishAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _repo.GetByIdAsync(id, ct);
        if (doc is null) return null;
        doc.Publish(); _repo.Update(doc); await _repo.SaveChangesAsync(ct);
        return Map(doc);
    }

    public async Task<PolicyDocumentItemDto?> ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _repo.GetByIdAsync(id, ct);
        if (doc is null) return null;
        doc.Archive(); _repo.Update(doc); await _repo.SaveChangesAsync(ct);
        return Map(doc);
    }

    public async Task<IList<PolicyDocumentVersionItemDto>> GetVersionsAsync(Guid documentId, CancellationToken ct = default)
        => (await _repo.GetVersionsAsync(documentId, ct)).Select(v => new PolicyDocumentVersionItemDto
        {
            Id = v.Id, DocumentId = v.DocumentId, VersionNumber = v.VersionNumber,
            Content = v.Content, ChangedBy = v.ChangedBy, ChangedAt = v.ChangedAt, ChangeNotes = v.ChangeNotes
        }).ToList();

    private static PolicyDocumentItemDto Map(PolicyDocument x) => new()
    {
        Id = x.Id, Title = x.Title, Description = x.Description, Version = x.Version,
        Status = x.Status, Category = x.Category, AccessLevel = x.AccessLevel,
        CreatedBy = x.CreatedBy, PublishedAt = x.PublishedAt, CreatedAt = x.CreatedAt
    };
    private static PolicyDocumentItemDto? MapOrNull(PolicyDocument? x) => x is null ? null : Map(x);
}
