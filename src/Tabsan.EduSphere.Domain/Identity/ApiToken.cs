using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Identity;

/// <summary>
/// Plan M14.3 — a personal access token for integrations (SIS sync, reporting tools, scripts). It acts as its owner with
/// the owner's role and scope. Only a SHA-256 hash is stored; the token itself is shown once when created.
/// </summary>
public class ApiToken : BaseEntity
{
    public const string Prefix = "esk_";

    public Guid UserId { get; private set; }
    public string Name { get; private set; } = default!;
    /// <summary>The first characters of the token, to recognise it in lists ("esk_ab12cd…").</summary>
    public string Hint { get; private set; } = default!;
    /// <summary>SHA-256 of the token (hex, lower case).</summary>
    public string TokenHash { get; private set; } = default!;
    /// <summary>Only GET/HEAD requests.</summary>
    public bool ReadOnly { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public string? LastUsedIp { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public Guid? RevokedByUserId { get; private set; }

    private ApiToken() { }

    public ApiToken(Guid userId, string name, string hint, string tokenHash, bool readOnly, DateTime? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Enter a name for the token.", nameof(name));
        UserId = userId;
        Name = name.Trim().Length > 120 ? name.Trim()[..120] : name.Trim();
        Hint = hint;
        TokenHash = tokenHash;
        ReadOnly = readOnly;
        ExpiresAt = expiresAt;
    }

    public bool IsActive(DateTime now) => RevokedAt is null && (ExpiresAt is null || ExpiresAt > now);

    public void Used(DateTime now, string? ip)
    {
        LastUsedAt = now;
        LastUsedIp = ip is { Length: > 45 } ? ip[..45] : ip;
    }

    public void Revoke(Guid byUserId, DateTime now)
    {
        if (RevokedAt is not null) return;
        RevokedAt = now;
        RevokedByUserId = byUserId;
        Touch();
    }
}
