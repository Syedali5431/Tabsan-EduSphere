using Tabsan.EduSphere.Application.DTOs.DataClassification;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>Phase 5: Service for managing data classification entries.</summary>
public interface IDataClassificationService
{
    Task<IList<DataClassificationItemDto>> GetAllAsync(CancellationToken ct = default);
    Task<IList<DataClassificationItemDto>> GetByEntityAsync(string entityName, string? entityId, CancellationToken ct = default);
    Task<DataClassificationItemDto> CreateAsync(CreateClassificationRequest request, Guid classifiedBy, CancellationToken ct = default);
}
