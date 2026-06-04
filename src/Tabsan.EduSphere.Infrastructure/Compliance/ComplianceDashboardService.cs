using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Compliance;

/// <summary>Phase 10: Compliance dashboard aggregates all ISO phase data into one view.</summary>
public class ComplianceDashboardService : IComplianceDashboardService
{
    private readonly ApplicationDbContext _db;
    public ComplianceDashboardService(ApplicationDbContext db) => _db = db;

    public async Task<ComplianceDashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var dto = new ComplianceDashboardDto { GeneratedAt = DateTime.UtcNow };

        // ── Audit ──────────────────────────────────────────────────────────
        dto.Audit.TotalAuditLogs = await _db.AuditLogs.LongCountAsync(ct);
        dto.Audit.TodayAuditLogs = await _db.AuditLogs.LongCountAsync(x => x.OccurredAt >= today, ct);
        dto.Audit.TopActions = await _db.AuditLogs.GroupBy(x => x.Action)
            .OrderByDescending(g => g.Count()).Select(g => g.Key).Take(5).ToListAsync(ct);

        // ── Security ───────────────────────────────────────────────────────
        dto.Security.LockedAccounts = await _db.Users.CountAsync(x => x.IsLockedOut, ct);
        dto.Security.MfaEnabledUsers = await _db.Users.CountAsync(x => x.MfaIsEnabled, ct);
        dto.Security.ActiveSessions = await _db.UserSessions.CountAsync(x => x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow, ct);
        dto.Security.PasswordExpiredUsers = await _db.Users.CountAsync(x => x.LastPasswordChangedAt.HasValue
            && x.LastPasswordChangedAt.Value.AddDays(90) < DateTime.UtcNow, ct);

        // ── Backup ─────────────────────────────────────────────────────────
        dto.Backup.TotalBackups = await _db.BackupLogs.CountAsync(ct);
        dto.Backup.SuccessfulBackups = await _db.BackupLogs.CountAsync(x => x.Status == "Completed" || x.Status == "Verified", ct);
        dto.Backup.FailedBackups = await _db.BackupLogs.CountAsync(x => x.Status == "Failed", ct);
        dto.Backup.VerifiedBackups = await _db.BackupLogs.CountAsync(x => x.Status == "Verified", ct);
        dto.Backup.LastBackupStatus = await _db.BackupLogs.OrderByDescending(x => x.StartedAt).Select(x => x.Status).FirstOrDefaultAsync(ct);

        // ── Incidents ──────────────────────────────────────────────────────
        dto.Incidents.OpenIncidents = await _db.IncidentLogs.CountAsync(x => x.Status == "Open", ct);
        dto.Incidents.InvestigatingIncidents = await _db.IncidentLogs.CountAsync(x => x.Status == "Investigating", ct);
        dto.Incidents.ResolvedIncidents = await _db.IncidentLogs.CountAsync(x => x.Status == "Resolved" || x.Status == "Closed", ct);
        dto.Incidents.TotalIncidents = await _db.IncidentLogs.CountAsync(ct);

        // ── Activity ───────────────────────────────────────────────────────
        dto.Activity.LoginAttemptsToday = await _db.LoginActivityLogs.CountAsync(x => x.AttemptedAt >= today, ct);
        dto.Activity.FailedLoginsToday = await _db.LoginActivityLogs.CountAsync(x => x.AttemptedAt >= today && !x.IsSuccess, ct);
        dto.Activity.HighRiskLoginsToday = await _db.LoginActivityLogs.CountAsync(x => x.AttemptedAt >= today && x.RiskLevel == "high", ct);

        // ── Data Protection ────────────────────────────────────────────────
        dto.DataProtection.ClassificationEntries = await _db.DataClassificationEntries.CountAsync(ct);
        dto.DataProtection.UsersWithConsent = await _db.Users.CountAsync(x => x.ConsentToMonitoring == true, ct);
        dto.DataProtection.UsersWithRetentionDate = await _db.Users.CountAsync(x => x.DataRetentionDate.HasValue, ct);

        // ── Documents ──────────────────────────────────────────────────────
        dto.Documents.TotalDocuments = await _db.PolicyDocuments.CountAsync(ct);
        dto.Documents.PublishedDocuments = await _db.PolicyDocuments.CountAsync(x => x.Status == "Published", ct);
        dto.Documents.DraftDocuments = await _db.PolicyDocuments.CountAsync(x => x.Status == "Draft", ct);

        return dto;
    }
}
