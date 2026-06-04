using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Activity;

/// <summary>
/// Phase 3: Immutable record of every login attempt (success or failure).
/// Provides structured queryable data for security monitoring, trend analysis,
/// and ISO 27001 A.12.4.1 event logging compliance.
/// Rows are append-only — never updated or deleted after creation.
/// </summary>
public class LoginActivityLog : BaseEntity
{
    /// <summary>FK to the user who attempted login. Null when username does not match any account.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>The username that was submitted in the login attempt.</summary>
    public string Username { get; private set; } = default!;

    /// <summary>UTC timestamp of the login attempt.</summary>
    public DateTime AttemptedAt { get; private set; }

    /// <summary>Client IP address at the time of the attempt.</summary>
    public string? IpAddress { get; private set; }

    /// <summary>Client user-agent string from request headers.</summary>
    public string? UserAgent { get; private set; }

    /// <summary>Client device information captured at login.</summary>
    public string? DeviceInfo { get; private set; }

    /// <summary>True if the login succeeded, false otherwise.</summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Reason for failure when IsSuccess is false.
    /// Values: InvalidCredentials, ConcurrencyLimitReached, MfaRequired, SessionRiskBlocked, AccountLocked, AccountInactive.
    /// Null for successful logins.
    /// </summary>
    public string? FailureReason { get; private set; }

    /// <summary>Session risk level at the time of this attempt: low, medium, or high.</summary>
    public string? RiskLevel { get; private set; }

    /// <summary>True when the user account was in a locked-out state at the time of this attempt.</summary>
    public bool UserIsLockedOut { get; private set; }

    private LoginActivityLog() { }

    /// <summary>Records a login attempt with all available context.</summary>
    public LoginActivityLog(
        Guid? userId,
        string username,
        DateTime attemptedAt,
        string? ipAddress,
        string? userAgent,
        string? deviceInfo,
        bool isSuccess,
        string? failureReason,
        string? riskLevel,
        bool userIsLockedOut)
    {
        UserId = userId;
        Username = username;
        AttemptedAt = attemptedAt;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        DeviceInfo = deviceInfo;
        IsSuccess = isSuccess;
        FailureReason = failureReason;
        RiskLevel = riskLevel;
        UserIsLockedOut = userIsLockedOut;
    }
}
