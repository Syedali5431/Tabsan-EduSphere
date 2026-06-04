namespace Tabsan.EduSphere.Application.DTOs.LoginActivity;

/// <summary>Phase 3: Filter parameters for login activity queries.</summary>
public sealed class LoginActivityFilterDto
{
    public string? Query { get; set; }
    public Guid? UserId { get; set; }
    public bool? IsSuccess { get; set; }
    public string? RiskLevel { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>Phase 3: Flat response DTO for a single login activity record.</summary>
public sealed class LoginActivityItemDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime AttemptedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceInfo { get; set; }
    public bool IsSuccess { get; set; }
    public string? FailureReason { get; set; }
    public string? RiskLevel { get; set; }
    public bool UserIsLockedOut { get; set; }
}

/// <summary>Phase 3: Paged result wrapper for login activity queries.</summary>
public sealed class PagedLoginActivityResult
{
    public List<LoginActivityItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}

/// <summary>Phase 3: Aggregated summary for login activity dashboard.</summary>
public sealed class LoginActivitySummaryDto
{
    public int TotalAttempts { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<DailyBreakdownDto> DailyBreakdown { get; set; } = new();
    public List<TopFailureReasonDto> TopFailureReasons { get; set; } = new();
    public List<TopIpDto> TopIps { get; set; } = new();
}

public sealed class DailyBreakdownDto
{
    public DateTime Date { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}

public sealed class TopFailureReasonDto
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class TopIpDto
{
    public string IpAddress { get; set; } = string.Empty;
    public int Count { get; set; }
}
