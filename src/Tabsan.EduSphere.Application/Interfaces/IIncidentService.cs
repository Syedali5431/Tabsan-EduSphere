using Tabsan.EduSphere.Application.DTOs.Incidents;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>Phase 6: Incident management service — ISO 27001 A.16.1.5.</summary>
public interface IIncidentService
{
    Task<IList<IncidentItemDto>> GetAllAsync(CancellationToken ct = default);
    Task<IncidentItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IncidentItemDto> CreateAsync(CreateIncidentRequest request, Guid reportedBy, CancellationToken ct = default);
    Task<IncidentItemDto?> UpdateStatusAsync(Guid id, UpdateIncidentStatusRequest request, CancellationToken ct = default);
    Task<IncidentSummaryDto> GetSummaryAsync(CancellationToken ct = default);
}
