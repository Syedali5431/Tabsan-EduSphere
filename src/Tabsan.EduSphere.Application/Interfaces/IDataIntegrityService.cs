namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Phase 9: Data integrity verification service.
/// Ensures transaction safety and audit coverage for financial and academic actions.
/// </summary>
public interface IDataIntegrityService
{
    /// <summary>Runs a full integrity check across the system and returns findings.</summary>
    Task<DataIntegrityReport> RunIntegrityCheckAsync(CancellationToken ct = default);
}

/// <summary>Phase 9: Report of data integrity findings.</summary>
public sealed class DataIntegrityReport
{
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public bool IsHealthy { get; set; }
    public List<DataIntegrityFinding> Findings { get; set; } = new();
}

public sealed class DataIntegrityFinding
{
    public string Area { get; set; } = string.Empty;
    public string Status { get; set; } = "OK"; // OK, Warning, Error
    public string Message { get; set; } = string.Empty;
}
