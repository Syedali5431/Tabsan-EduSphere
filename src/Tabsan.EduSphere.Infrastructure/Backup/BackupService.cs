using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.DTOs.Backup;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Backup;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Backup;

/// <summary>
/// Phase 4: Service for recording and querying backup operations.
/// ISO 27001 A.17.1.2 — documented backup procedures with verifiable logs.
/// </summary>
public class BackupService : IBackupService
{
    private readonly ApplicationDbContext _db;
    private readonly IBackupLogRepository _repo;

    public BackupService(ApplicationDbContext db, IBackupLogRepository repo)
    {
        _db = db;
        _repo = repo;
    }

    public async Task<PagedBackupLogResult> GetLogsAsync(BackupLogFilterDto filter, CancellationToken ct = default)
    {
        var query = _db.BackupLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.BackupType))
            query = query.Where(b => b.BackupType == filter.BackupType);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(b => b.Status == filter.Status);

        if (filter.FromUtc.HasValue)
            query = query.Where(b => b.StartedAt >= filter.FromUtc.Value);

        if (filter.ToUtc.HasValue)
            query = query.Where(b => b.StartedAt <= filter.ToUtc.Value);

        var total = await query.CountAsync(ct);
        var logs = await query
            .OrderByDescending(b => b.StartedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(b => new BackupLogItemDto
            {
                Id = b.Id,
                BackupType = b.BackupType,
                FileName = b.FileName,
                FilePath = b.FilePath,
                FileSizeBytes = b.FileSizeBytes,
                DurationSeconds = b.DurationSeconds,
                Status = b.Status,
                StartedAt = b.StartedAt,
                CompletedAt = b.CompletedAt,
                ErrorMessage = b.ErrorMessage,
                Checksum = b.Checksum,
                InitiatedBy = b.InitiatedBy
            })
            .ToListAsync(ct);

        return new PagedBackupLogResult { Items = logs, Total = total, Page = filter.Page, PageSize = filter.PageSize };
    }

    public async Task<BackupLogItemDto> RecordBackupStartAsync(RecordBackupRequest request, CancellationToken ct = default)
    {
        var log = new BackupLog(
            request.BackupType,
            request.FileName,
            request.FilePath,
            "InProgress",
            DateTime.UtcNow,
            request.InitiatedBy);

        _db.BackupLogs.Add(log);
        await _db.SaveChangesAsync(ct);

        return new BackupLogItemDto
        {
            Id = log.Id,
            BackupType = log.BackupType,
            FileName = log.FileName,
            FilePath = log.FilePath,
            Status = log.Status,
            StartedAt = log.StartedAt,
            InitiatedBy = log.InitiatedBy
        };
    }

    public async Task<BackupLogItemDto?> UpdateBackupStatusAsync(Guid id, UpdateBackupStatusRequest request, CancellationToken ct = default)
    {
        var log = await _db.BackupLogs.FindAsync(new object[] { id }, ct);
        if (log is null) return null;

        switch (request.Status?.ToLowerInvariant())
        {
            case "completed":
                log.Complete(request.FileSizeBytes ?? 0, request.DurationSeconds ?? 0, request.Checksum);
                break;
            case "failed":
                log.Fail(request.ErrorMessage ?? "Backup operation failed.");
                break;
            case "verified":
                log.Verify(request.Checksum);
                break;
            default:
                return null;
        }

        await _db.SaveChangesAsync(ct);

        return new BackupLogItemDto
        {
            Id = log.Id,
            BackupType = log.BackupType,
            FileName = log.FileName,
            FilePath = log.FilePath,
            FileSizeBytes = log.FileSizeBytes,
            DurationSeconds = log.DurationSeconds,
            Status = log.Status,
            StartedAt = log.StartedAt,
            CompletedAt = log.CompletedAt,
            ErrorMessage = log.ErrorMessage,
            Checksum = log.Checksum,
            InitiatedBy = log.InitiatedBy
        };
    }

    public async Task<BackupStatusSummaryDto> GetStatusSummaryAsync(CancellationToken ct = default)
    {
        var lastFull = await GetLatestByTypeAsync("Full", ct);
        var lastDiff = await GetLatestByTypeAsync("Differential", ct);
        var lastLog = await GetLatestByTypeAsync("TransactionLog", ct);

        return new BackupStatusSummaryDto
        {
            LastFullBackup = lastFull,
            LastDifferentialBackup = lastDiff,
            LastLogBackup = lastLog
        };
    }

    private async Task<BackupLogItemDto?> GetLatestByTypeAsync(string backupType, CancellationToken ct)
    {
        return await _db.BackupLogs
            .AsNoTracking()
            .Where(b => b.BackupType == backupType)
            .OrderByDescending(b => b.StartedAt)
            .Select(b => new BackupLogItemDto
            {
                Id = b.Id,
                BackupType = b.BackupType,
                FileName = b.FileName,
                FilePath = b.FilePath,
                FileSizeBytes = b.FileSizeBytes,
                DurationSeconds = b.DurationSeconds,
                Status = b.Status,
                StartedAt = b.StartedAt,
                CompletedAt = b.CompletedAt,
                ErrorMessage = b.ErrorMessage,
                Checksum = b.Checksum,
                InitiatedBy = b.InitiatedBy
            })
            .FirstOrDefaultAsync(ct);
    }
}
