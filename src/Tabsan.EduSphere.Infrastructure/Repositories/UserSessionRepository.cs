using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IUserSessionRepository.
/// Manages the UserSession rows used for refresh-token tracking.
/// </summary>
public class UserSessionRepository : IUserSessionRepository
{
    private readonly ApplicationDbContext _db;

    public UserSessionRepository(ApplicationDbContext db) => _db = db;

    /// <summary>
    /// Returns the session whose token hash matches AND has not been revoked.
    /// Returns null when no active matching session exists.
    /// </summary>
    public Task<UserSession?> GetActiveByHashAsync(string tokenHash, CancellationToken ct = default)
        => _db.UserSessions
              .FirstOrDefaultAsync(s => s.RefreshTokenHash == tokenHash && s.RevokedAt == null, ct);

    public Task<UserSession?> GetMostRecentByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _db.UserSessions
              .Where(s => s.UserId == userId)
              .OrderByDescending(s => s.CreatedAt)
              .FirstOrDefaultAsync(ct);

    /// <summary>Queues the new session for insertion.</summary>
    public async Task AddAsync(UserSession session, CancellationToken ct = default)
        => await _db.UserSessions.AddAsync(session, ct);

    /// <summary>Marks the session entity as modified.</summary>
    public void Update(UserSession session) => _db.UserSessions.Update(session);

    /// <summary>Commits all pending changes to the database.</summary>
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    // ── P2-S1-01: Concurrency limit ──────────────────────────────────────────

    /// <summary>
    /// Returns the count of sessions that are currently active (not revoked, not expired).
    /// Used to enforce the MaxUsers license limit prior to allowing a new login.
    /// </summary>
    public Task<int> CountActiveSessionsAsync(CancellationToken ct = default)
        => _db.UserSessions
              .CountAsync(s => s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow, ct);

    // ── Phase 2 - ISO Security: Session management ─────────────────────────

    public Task<UserSession?> GetByIdAsync(Guid sessionId, CancellationToken ct = default)
        => _db.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);

    public async Task<IList<UserSession>> GetActiveSessionsAsync(CancellationToken ct = default)
        => await _db.UserSessions
            .Where(s => s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow)
            .Include(s => s.User).ThenInclude(u => u.Role)
            .OrderByDescending(s => s.LastActivityAt ?? s.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IList<UserSession>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _db.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IList<UserSession>> GetIdleSessionsAsync(int idleTimeoutMinutes, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-idleTimeoutMinutes);
        return await _db.UserSessions
            .Where(s => s.RevokedAt == null
                        && s.ExpiresAt > DateTime.UtcNow
                        && (s.LastActivityAt ?? s.CreatedAt) < cutoff)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
