namespace Tabsan.EduSphere.Application.Dtos;

/// <summary>Request body for admin to unlock a user account.</summary>
public record UnlockAccountRequest(Guid TargetUserId);

/// <summary>Request body for admin to reset a user's password.</summary>
public record AdminResetPasswordRequest(
    Guid TargetUserId,
    string NewPassword
);

/// <summary>Account lockout status for a user.</summary>
public record AccountLockoutStatusDto(
    Guid UserId,
    string Username,
    bool IsLockedOut,
    int FailedAttempts,
    DateTime? LockedOutUntil
);

// ── Phase 2 - ISO Security: Session management DTOs ──────────────────────

/// <summary>Summary of an active user session for the admin sessions screen.</summary>
public sealed record ActiveSessionDto(
    Guid SessionId,
    Guid UserId,
    string Username,
    string? FullName,
    string? Role,
    string? DeviceInfo,
    string? IpAddress,
    DateTime CreatedAt,
    DateTime? LastActivityAt,
    DateTime ExpiresAt
);

/// <summary>Request body for admin to revoke a specific session.</summary>
public sealed record RevokeSessionRequest(Guid SessionId);
