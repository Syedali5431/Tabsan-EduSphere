using System.Security.Claims;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Services;

/// <summary>
/// Resolves role and tenant/campus scope from the current HTTP user claims.
/// </summary>
public sealed class HttpAccessScopeResolver : IAccessScopeResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpAccessScopeResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsSuperAdmin()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        return user.IsInRole("SuperAdmin")
            || string.Equals(user.FindFirstValue(ClaimTypes.Role), "SuperAdmin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(user.FindFirstValue("role"), "SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }

    public Guid? GetTenantId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return TryParseGuidClaim(user, "tenant_id", "tenantId", "tid");
    }

    public Guid? GetCampusId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return TryParseGuidClaim(user, "campus_id", "campusId", "cid");
    }

    private static Guid? TryParseGuidClaim(ClaimsPrincipal user, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var raw = user.FindFirstValue(claimType);
            if (Guid.TryParse(raw, out var parsed))
                return parsed;
        }

        return null;
    }
}
