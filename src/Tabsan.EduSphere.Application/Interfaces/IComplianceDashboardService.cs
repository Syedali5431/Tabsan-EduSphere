namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Phase 10: Compliance dashboard aggregating all ISO phases.
/// Provides a single read-only view of audit, security, backup, and incident posture.
/// </summary>
public interface IComplianceDashboardService
{
    Task<ComplianceDashboardDto> GetDashboardAsync(CancellationToken ct = default);
}

public sealed class ComplianceDashboardDto
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public AuditSummaryDto Audit { get; set; } = new();
    public SecuritySummaryDto Security { get; set; } = new();
    public BackupSummaryDto Backup { get; set; } = new();
    public DashboardIncidentSummaryDto Incidents { get; set; } = new();
    public ActivitySummaryDto Activity { get; set; } = new();
    public DataProtectionSummaryDto DataProtection { get; set; } = new();
    public DocumentSummaryDto Documents { get; set; } = new();
}

public sealed class AuditSummaryDto
{
    public long TotalAuditLogs { get; set; }
    public long TodayAuditLogs { get; set; }
    public List<string> TopActions { get; set; } = new();
    public int ImmutableLogEnforced { get; set; } = 1;
}

public sealed class SecuritySummaryDto
{
    public int LockedAccounts { get; set; }
    public int MfaEnabledUsers { get; set; }
    public int ActiveSessions { get; set; }
    public int PasswordExpiredUsers { get; set; }
    public bool PasswordPolicyEnforced { get; set; } = true;
    public bool SessionTimeoutEnabled { get; set; } = true;
}

public sealed class BackupSummaryDto
{
    public int TotalBackups { get; set; }
    public int SuccessfulBackups { get; set; }
    public int FailedBackups { get; set; }
    public int VerifiedBackups { get; set; }
    public string? LastBackupStatus { get; set; }
}

public sealed class DashboardIncidentSummaryDto
{
    public int OpenIncidents { get; set; }
    public int InvestigatingIncidents { get; set; }
    public int ResolvedIncidents { get; set; }
    public int TotalIncidents { get; set; }
}

public sealed class ActivitySummaryDto
{
    public int LoginAttemptsToday { get; set; }
    public int FailedLoginsToday { get; set; }
    public int HighRiskLoginsToday { get; set; }
}

public sealed class DataProtectionSummaryDto
{
    public int ClassificationEntries { get; set; }
    public int UsersWithConsent { get; set; }
    public int UsersWithRetentionDate { get; set; }
}

public sealed class DocumentSummaryDto
{
    public int TotalDocuments { get; set; }
    public int PublishedDocuments { get; set; }
    public int DraftDocuments { get; set; }
}
