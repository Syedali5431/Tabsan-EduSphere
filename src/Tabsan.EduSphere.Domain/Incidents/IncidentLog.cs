using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Incidents;

/// <summary>
/// Phase 6: Security incident record for ISO 27001 A.16.1.5 incident response compliance.
/// Tracks the full lifecycle: Open → Investigating → Resolved → Closed.
/// </summary>
public class IncidentLog : BaseEntity
{
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public string Severity { get; private set; } = "Medium"; // Low, Medium, High, Critical
    public string Category { get; private set; } = "Security"; // Security, Breach, DataLoss, AccessViolation, System, Other
    public string Status { get; private set; } = "Open"; // Open, Investigating, Resolved, Closed
    public Guid? ReportedBy { get; private set; }
    public DateTime ReportedAt { get; private set; }
    public Guid? AssignedTo { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? Resolution { get; private set; }

    private IncidentLog() { }

    public IncidentLog(string title, string severity, string category, Guid? reportedBy, string? description = null)
    {
        Title = title; Severity = severity; Category = category;
        ReportedBy = reportedBy; Description = description;
        ReportedAt = DateTime.UtcNow; Status = "Open";
    }

    public void StartInvestigation(Guid assignedTo)
    {
        Status = "Investigating"; AssignedTo = assignedTo; Touch();
    }

    public void Resolve(string resolution)
    {
        Status = "Resolved"; Resolution = resolution; ResolvedAt = DateTime.UtcNow; Touch();
    }

    public void Close()
    {
        Status = "Closed"; if (!ResolvedAt.HasValue) ResolvedAt = DateTime.UtcNow; Touch();
    }

    public void Reopen()
    {
        Status = "Open"; Resolution = null; ResolvedAt = null; AssignedTo = null; Touch();
    }
}
