using Tabsan.EduSphere.Domain.Common;
using Tabsan.EduSphere.Domain.Enums;

namespace Tabsan.EduSphere.Domain.Identity;

/// <summary>
/// Plan M1 (SSO): links an EduSphere user to an identity at an external provider.
/// The pair (TenantId, Provider, ExternalSubjectId) is unique, and a user can hold
/// at most one link per provider. After a successful external sign-in the platform
/// looks up this row and issues the normal EduSphere JWT for <see cref="UserId"/>.
/// </summary>
public class ExternalLogin : BaseEntity
{
    /// <summary>Tenant that owns the provider configuration. Null on single-tenant installs.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>FK to the linked EduSphere user.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Navigation to the linked user.</summary>
    public User User { get; private set; } = default!;

    /// <summary>Provider that issued the external identity.</summary>
    public SsoProvider Provider { get; private set; }

    /// <summary>
    /// Stable, provider-issued subject identifier (OIDC <c>sub</c> / <c>oid</c>, SAML NameID).
    /// Never the email address — emails can be reassigned at the IdP.
    /// </summary>
    public string ExternalSubjectId { get; private set; } = default!;

    /// <summary>Email asserted by the provider at the most recent sign-in (informational).</summary>
    public string? Email { get; private set; }

    /// <summary>UTC timestamp when the link was created.</summary>
    public DateTime LinkedAt { get; private set; }

    /// <summary>UTC timestamp of the most recent sign-in through this link.</summary>
    public DateTime? LastLoginAt { get; private set; }

    private ExternalLogin() { }

    public ExternalLogin(Guid? tenantId, Guid userId, SsoProvider provider, string externalSubjectId, string? email)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(externalSubjectId))
            throw new ArgumentException("External subject id is required.", nameof(externalSubjectId));

        TenantId = tenantId;
        UserId = userId;
        Provider = provider;
        ExternalSubjectId = externalSubjectId.Trim();
        Email = NormalizeEmail(email);
        LinkedAt = DateTime.UtcNow;
    }

    /// <summary>Records a successful sign-in and refreshes the asserted email.</summary>
    public void RecordLogin(string? email)
    {
        LastLoginAt = DateTime.UtcNow;
        var normalized = NormalizeEmail(email);
        if (normalized is not null)
            Email = normalized;
        Touch();
    }

    private static string? NormalizeEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}
