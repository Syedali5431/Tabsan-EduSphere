using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.DTOs.LoginActivity;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Activity;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Activity;

/// <summary>Phase 3: Query service for login activity logs and dashboard summaries.</summary>
public class LoginActivityService : ILoginActivityService
{
    private readonly ApplicationDbContext _db;

    public LoginActivityService(ApplicationDbContext db) => _db = db;

    public async Task<PagedLoginActivityResult> GetActivityAsync(LoginActivityFilterDto filter, CancellationToken ct = default)
    {
        if (filter.Page < 1) filter.Page = 1;
        filter.PageSize = Math.Clamp(filter.PageSize, 1, 200);

        IQueryable<LoginActivityLog> query = _db.LoginActivityLogs.AsNoTracking();

        if (filter.UserId.HasValue)
            query = query.Where(x => x.UserId == filter.UserId.Value);

        if (filter.IsSuccess.HasValue)
            query = query.Where(x => x.IsSuccess == filter.IsSuccess.Value);

        if (!string.IsNullOrWhiteSpace(filter.RiskLevel))
            query = query.Where(x => x.RiskLevel == filter.RiskLevel!.Trim());

        if (!string.IsNullOrWhiteSpace(filter.FailureReason))
            query = query.Where(x => x.FailureReason == filter.FailureReason!.Trim());

        if (filter.FromUtc.HasValue)
            query = query.Where(x => x.AttemptedAt >= filter.FromUtc.Value);

        if (filter.ToUtc.HasValue)
            query = query.Where(x => x.AttemptedAt <= filter.ToUtc.Value);

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var like = $"%{filter.Query.Trim()}%";
            query = query.Where(x =>
                EF.Functions.Like(x.Username, like)
                || (x.IpAddress != null && EF.Functions.Like(x.IpAddress, like)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.AttemptedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new LoginActivityItemDto
            {
                Id = x.Id,
                UserId = x.UserId,
                Username = x.Username,
                AttemptedAt = x.AttemptedAt,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent,
                DeviceInfo = x.DeviceInfo,
                IsSuccess = x.IsSuccess,
                FailureReason = x.FailureReason,
                RiskLevel = x.RiskLevel,
                UserIsLockedOut = x.UserIsLockedOut
            })
            .ToListAsync(ct);

        return new PagedLoginActivityResult
        {
            Items = items,
            Total = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<LoginActivitySummaryDto> GetSummaryAsync(DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default)
    {
        var start = fromUtc ?? DateTime.UtcNow.AddDays(-30);
        var end = toUtc ?? DateTime.UtcNow;

        var data = await _db.LoginActivityLogs
            .AsNoTracking()
            .Where(x => x.AttemptedAt >= start && x.AttemptedAt <= end)
            .ToListAsync(ct);

        var summary = new LoginActivitySummaryDto
        {
            TotalAttempts = data.Count,
            SuccessCount = data.Count(x => x.IsSuccess),
            FailureCount = data.Count(x => !x.IsSuccess),
            DailyBreakdown = data
                .GroupBy(x => x.AttemptedAt.Date)
                .Select(g => new DailyBreakdownDto
                {
                    Date = g.Key,
                    SuccessCount = g.Count(x => x.IsSuccess),
                    FailureCount = g.Count(x => !x.IsSuccess)
                })
                .OrderBy(x => x.Date)
                .ToList(),
            TopFailureReasons = data
                .Where(x => !x.IsSuccess && x.FailureReason != null)
                .GroupBy(x => x.FailureReason!)
                .Select(g => new TopFailureReasonDto { Reason = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList(),
            TopIps = data
                .Where(x => x.IpAddress != null)
                .GroupBy(x => x.IpAddress!)
                .Select(g => new TopIpDto { IpAddress = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList()
        };

        return summary;
    }
}
