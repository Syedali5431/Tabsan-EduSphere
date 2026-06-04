namespace Tabsan.EduSphere.Application.DTOs.Incidents;

public sealed class CreateIncidentRequest
{
    public string Title { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public string Category { get; set; } = "Security";
    public string? Description { get; set; }
}

public sealed class UpdateIncidentStatusRequest
{
    public string Action { get; set; } = string.Empty; // investigate, resolve, close, reopen
    public Guid? AssignedTo { get; set; }
    public string? Resolution { get; set; }
}

public sealed class IncidentItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? ReportedBy { get; set; }
    public DateTime ReportedAt { get; set; }
    public Guid? AssignedTo { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Resolution { get; set; }
}

public sealed class IncidentSummaryDto
{
    public int OpenCount { get; set; }
    public int InvestigatingCount { get; set; }
    public int ResolvedCount { get; set; }
    public int TotalCount { get; set; }
    public List<IncidentItemDto> RecentIncidents { get; set; } = new();
}
