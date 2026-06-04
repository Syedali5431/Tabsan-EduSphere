using Tabsan.EduSphere.Domain.Incidents;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>Phase 6: Repository contract for incident management.</summary>
public interface IIncidentRepository
{
    Task<IncidentLog?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IList<IncidentLog>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(IncidentLog entry, CancellationToken ct = default);
    void Update(IncidentLog entry);
    Task SaveChangesAsync(CancellationToken ct = default);
}
