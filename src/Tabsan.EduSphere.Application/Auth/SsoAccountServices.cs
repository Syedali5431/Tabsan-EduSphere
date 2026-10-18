using System.Text.Json;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Auth;

/// <summary>
/// Plan M1 Stage M1.4 — "SSO only" enforcement for password sign-in.
/// Enforcement is skipped while the "sso" module is inactive (e.g. licence lapsed), so a
/// policy can never leave an institution with no working sign-in method.
/// </summary>
public sealed class SsoLoginPolicy : ISsoLoginPolicy
{
    internal const string SsoModuleKey = "sso";

    private readonly ISsoRepository _repo;
    private readonly IModuleEntitlementResolver _modules;

    public SsoLoginPolicy(ISsoRepository repo, IModuleEntitlementResolver modules)
    {
        _repo = repo;
        _modules = modules;
    }

    public async Task<bool> IsPasswordLoginBlockedAsync(User user, CancellationToken ct = default)
    {
        if (string.Equals(user.Role?.Name, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            return false; // break-glass account keeps password + TOTP

        if (!await _modules.IsActiveAsync(SsoModuleKey, ct))
            return false;

        var enforcing = await _repo.GetEnforcingSettingsAsync(user.TenantId, ct);
        return enforcing.Any(s => s.IsComplete);
    }
}

/// <summary>Plan M1 Stage M1.4 — listing and removing external account links, audited.</summary>
public sealed class SsoAccountService : ISsoAccountService
{
    private readonly ISsoRepository _repo;
    private readonly IUserRepository _users;
    private readonly ISsoLoginPolicy _policy;
    private readonly IAuditService _audit;

    public SsoAccountService(ISsoRepository repo, IUserRepository users, ISsoLoginPolicy policy, IAuditService audit)
    {
        _repo = repo;
        _users = users;
        _policy = policy;
        _audit = audit;
    }

    public async Task<IReadOnlyList<SsoLinkSummary>> GetLinksAsync(Guid userId, CancellationToken ct = default)
        => (await _repo.GetLinksForUserAsync(userId, ct))
            .Select(l => new SsoLinkSummary(l.Provider.ToString(), l.Email, l.LinkedAt, l.LastLoginAt))
            .ToList();

    public async Task<SsoUnlinkResult> UnlinkAsync(
        Guid userId, string provider, Guid actorUserId, bool isSelfService, string? ipAddress, CancellationToken ct = default)
    {
        if (!Enum.TryParse<SsoProvider>(provider, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            return SsoUnlinkResult.NotFound;

        // Tenant/campus-scoped lookup: an admin cannot reach users outside their scope.
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
            return SsoUnlinkResult.NotFound;

        var links = await _repo.GetLinksForUserAsync(userId, ct);
        var link = links.FirstOrDefault(l => l.Provider == parsed);
        if (link is null)
            return SsoUnlinkResult.NotFound;

        if (isSelfService && links.Count == 1 && await _policy.IsPasswordLoginBlockedAsync(user, ct))
            return SsoUnlinkResult.WouldLockOut;

        _repo.RemoveLink(link);
        await _repo.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditLog(
            "SsoAccountUnlinked", "User", userId.ToString(),
            actorUserId: actorUserId,
            oldValuesJson: JsonSerializer.Serialize(new { provider = parsed.ToString(), email = link.Email, linkedAt = link.LinkedAt }),
            newValuesJson: JsonSerializer.Serialize(new { selfService = isSelfService }),
            ipAddress: ipAddress), ct);

        return SsoUnlinkResult.Unlinked;
    }
}
