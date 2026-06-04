using Tabsan.EduSphere.Domain.DataProtection;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>Phase 5: Repository contract for data classification entries.</summary>
public interface IDataClassificationRepository
{
    Task<IList<DataClassificationEntry>> GetByEntityAsync(string entityName, string? entityId, CancellationToken ct = default);
    Task<IList<DataClassificationEntry>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(DataClassificationEntry entry, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
