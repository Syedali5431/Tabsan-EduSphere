namespace Tabsan.EduSphere.Domain.Identity;

/// <summary>
/// Stores the hashed representation of a user's previous passwords.
/// Used to enforce the "no last-5 reuse" password policy.
/// Only the hash is stored — the plain-text password is never persisted.
/// </summary>
public class PasswordHistoryEntry
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>FK to the user who owns this history entry.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Argon2id hash of the historical password.</summary>
    public string PasswordHash { get; private set; } = default!;

    /// <summary>UTC time when this entry was recorded (i.e., when this password was set).</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Phase 2: Optional UTC timestamp when this history entry is eligible for archival/pruning.</summary>
    public DateTime? ExpiresAt { get; private set; }

    private PasswordHistoryEntry() { }

    /// <summary>Creates a new history entry for the given user and hash.</summary>
    public PasswordHistoryEntry(Guid userId, string passwordHash, DateTime? expiresAt = null)
    {
        UserId       = userId;
        PasswordHash = passwordHash;
        CreatedAt    = DateTime.UtcNow;
        ExpiresAt    = expiresAt;
    }
}
