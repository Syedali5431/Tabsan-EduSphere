using Tabsan.EduSphere.Application.DTOs.DataClassification;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.DataProtection;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Infrastructure.DataProtection;

/// <summary>Phase 5: Service for data classification management.</summary>
public class DataClassificationService : IDataClassificationService
{
    private readonly IDataClassificationRepository _repo;

    public DataClassificationService(IDataClassificationRepository repo) => _repo = repo;

    public async Task<IList<DataClassificationItemDto>> GetAllAsync(CancellationToken ct = default)
    {
        var entries = await _repo.GetAllAsync(ct);
        return entries.Select(Map).ToList();
    }

    public async Task<IList<DataClassificationItemDto>> GetByEntityAsync(string entityName, string? entityId, CancellationToken ct = default)
    {
        var entries = await _repo.GetByEntityAsync(entityName, entityId, ct);
        return entries.Select(Map).ToList();
    }

    public async Task<DataClassificationItemDto> CreateAsync(CreateClassificationRequest request, Guid classifiedBy, CancellationToken ct = default)
    {
        var entry = new DataClassificationEntry(request.EntityName, request.EntityId, request.ClassificationLevel, classifiedBy, request.Justification);
        await _repo.AddAsync(entry, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(entry);
    }

    private static DataClassificationItemDto Map(DataClassificationEntry x) => new()
    {
        Id = x.Id, EntityName = x.EntityName, EntityId = x.EntityId,
        ClassificationLevel = x.ClassificationLevel, ClassifiedBy = x.ClassifiedBy,
        ClassifiedAt = x.ClassifiedAt, Justification = x.Justification
    };
}
