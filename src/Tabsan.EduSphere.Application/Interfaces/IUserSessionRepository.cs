using Tabsan.EduSphere.Domain.Identity;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Application-layer contract for reading active user sessions (refresh tokens).
/// Keeps AuthService independent of EF Core DbContext directly.
/// </summary>
public interface IUserSessionRepository
{
    /// <summary>Returns the active session matching the token hash, or null.</summary>
    Task<UserSession?> GetActiveByHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Returns the most recently created session for a user (active or revoked).
    /// Used by login risk controls to compare current sign-in context.
    /// </summary>
    Task<UserSession?> GetMostRecentByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Persists a new session.</summary>
    Task AddAsync(UserSession session, CancellationToken ct = default);

    /// <summary>Marks a session as modified.</summary>
    void Update(UserSession session);

    /// <summary>Commits pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    // ── P2-S1-01: Concurrency limit ──────────────────────────────────────────

    /// <summary>
    /// Returns the number of sessions that are currently active across ALL users
    /// (RevokedAt is null AND ExpiresAt is in the future).
    /// Used to enforce the MaxUsers license limit before allowing a new login.
    /// </summary>
    Task<int> CountActiveSessionsAsync(CancellationToken ct = default);

    // ── Phase 2 - ISO Security: Session management ───────────────────────────

    /// <summary>Finds a session by its primary key for manual revocation.</summary>
    Task<UserSession?> GetByIdAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>Returns all active sessions with user info for the admin sessions screen.</summary>
    Task<IList<UserSession>> GetActiveSessionsAsync(CancellationToken ct = default);

    /// <summary>Returns all active sessions for a specific user.</summary>
    Task<IList<UserSession>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns active sessions that have been idle beyond the timeout window.</summary>
    Task<IList<UserSession>> GetIdleSessionsAsync(int idleTimeoutMinutes, CancellationToken ct = default);
}
