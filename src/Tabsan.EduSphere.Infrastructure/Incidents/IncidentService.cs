using Tabsan.EduSphere.Application.DTOs.Incidents;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Incidents;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Infrastructure.Incidents;

public class IncidentService : IIncidentService
{
    private readonly IIncidentRepository _repo;
    public IncidentService(IIncidentRepository repo) => _repo = repo;

    public async Task<IList<IncidentItemDto>> GetAllAsync(CancellationToken ct = default)
        => (await _repo.GetAllAsync(ct)).Select(Map).ToList();

    public async Task<IncidentItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => MapOrNull(await _repo.GetByIdAsync(id, ct));

    public async Task<IncidentItemDto> CreateAsync(CreateIncidentRequest r, Guid reportedBy, CancellationToken ct = default)
    {
        var entry = new IncidentLog(r.Title, r.Severity, r.Category, reportedBy, r.Description);
        await _repo.AddAsync(entry, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(entry);
    }

    public async Task<IncidentItemDto?> UpdateStatusAsync(Guid id, UpdateIncidentStatusRequest r, CancellationToken ct = default)
    {
        var entry = await _repo.GetByIdAsync(id, ct);
        if (entry is null) return null;
        switch (r.Action)
        {
            case "investigate": entry.StartInvestigation(r.AssignedTo ?? Guid.Empty); break;
            case "resolve": entry.Resolve(r.Resolution ?? "Resolved"); break;
            case "close": entry.Close(); break;
            case "reopen": entry.Reopen(); break;
            default: return null;
        }
        _repo.Update(entry);
        await _repo.SaveChangesAsync(ct);
        return Map(entry);
    }

    public async Task<IncidentSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var all = await _repo.GetAllAsync(ct);
        return new IncidentSummaryDto
        {
            OpenCount = all.Count(x => x.Status == "Open"),
            InvestigatingCount = all.Count(x => x.Status == "Investigating"),
            ResolvedCount = all.Count(x => x.Status is "Resolved" or "Closed"),
            TotalCount = all.Count,
            RecentIncidents = all.Take(10).Select(Map).ToList()
        };
    }

    private static IncidentItemDto Map(IncidentLog x) => new()
    {
        Id = x.Id, Title = x.Title, Description = x.Description, Severity = x.Severity,
        Category = x.Category, Status = x.Status, ReportedBy = x.ReportedBy, ReportedAt = x.ReportedAt,
        AssignedTo = x.AssignedTo, ResolvedAt = x.ResolvedAt, Resolution = x.Resolution
    };
    private static IncidentItemDto? MapOrNull(IncidentLog? x) => x is null ? null : Map(x);
}
