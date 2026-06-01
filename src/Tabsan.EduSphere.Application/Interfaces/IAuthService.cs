using Tabsan.EduSphere.Application.DTOs.Auth;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Contract for authentication operations executed by the Application layer.
/// The Infrastructure layer provides token and hashing services;
/// this interface orchestrates them without depending on any specific implementation.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Validates credentials and, on success, creates a new session returning
    /// an access token and a refresh token.
    /// Returns a LoginResult with IsSuccess=false when credentials are invalid,
    /// the account is inactive, or the license concurrency limit has been reached (P2-S1-01).
    /// </summary>
    Task<LoginResult> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default);

    /// <summary>
    /// Returns deployment-auth security capabilities (MFA, SSO, and risk control flags)
    /// so clients can render adaptive login UX.
    /// </summary>
    Task<AuthSecurityProfileResponse> GetSecurityProfileAsync(CancellationToken ct = default);

    /// <summary>
    /// Validates the presented refresh token, rotates it (old token revoked, new issued),
    /// and returns a new access + refresh token pair.
    /// Returns null when the token is invalid, expired, or already revoked.
    /// </summary>
    Task<LoginResponse?> RefreshAsync(string rawRefreshToken, string? ipAddress, CancellationToken ct = default);

    /// <summary>
    /// Revokes the session associated with the presented refresh token.
    /// Used by the logout endpoint.
    /// </summary>
    Task LogoutAsync(string rawRefreshToken, CancellationToken ct = default);

    /// <summary>
    /// Verifies the current password and replaces it with the new one.
    /// Returns false when the current password does not match.
    /// </summary>
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);

    /// <summary>
    /// Sets a new password for a user who was flagged as MustChangePassword (P4-S2-02).
    /// Clears the MustChangePassword flag on success.
    /// Returns false when the user is not found or the new password is invalid.
    /// </summary>
    Task<bool> ForceChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);

    /// <summary>
    /// Starts or rotates MFA enrollment for the user and returns bootstrap secret and one-time recovery codes.
    /// </summary>
    Task<MfaSetupResponse?> BeginMfaSetupAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Enables MFA after validating a TOTP verification code against the enrolled secret.
    /// </summary>
    Task<bool> EnableMfaAsync(Guid userId, EnableMfaRequest request, CancellationToken ct = default);

    /// <summary>
    /// Generates a fresh set of one-time MFA recovery codes for an already enabled MFA enrollment.
    /// </summary>
    Task<MfaRecoveryCodesResponse?> RegenerateRecoveryCodesAsync(Guid userId, CancellationToken ct = default);
}
