using Microsoft.Extensions.Caching.Memory;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Services;

/// <summary>
/// Narrows the institution policy to the institution types that have data. The answer is cached for a minute, so data
/// added outside the portal (a SQL script) shows up within a minute; department changes made in the portal clear the
/// cache at once.
/// </summary>
public sealed class InstitutionVisibilityService : IInstitutionVisibilityService
{
    private const string CacheKey = "institution_visibility_snapshot";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1);

    private readonly IInstitutionPolicyService _policy;
    private readonly IDepartmentRepository _departments;
    private readonly IMemoryCache _cache;

    public InstitutionVisibilityService(IInstitutionPolicyService policy, IDepartmentRepository departments, IMemoryCache cache)
    {
        _policy = policy;
        _departments = departments;
        _cache = cache;
    }

    /// <inheritdoc/>
    public async Task<InstitutionPolicySnapshot> GetVisibilityAsync(CancellationToken ct = default)
    {
        var policy = await _policy.GetPolicyAsync(ct);
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<InstitutionType>? inUse) && inUse is not null)
            return Narrow(policy, inUse);

        inUse = await _departments.GetInstitutionTypesInUseAsync(ct);
        _cache.Set(CacheKey, inUse, CacheTtl);
        return Narrow(policy, inUse);
    }

    /// <inheritdoc/>
    public void InvalidateCache() => _cache.Remove(CacheKey);

    /// <summary>The policy's types that have data; the policy itself when none of them has data yet.</summary>
    public static InstitutionPolicySnapshot Narrow(InstitutionPolicySnapshot policy, IReadOnlyCollection<InstitutionType> inUse)
    {
        var narrowed = new InstitutionPolicySnapshot(
            IncludeSchool: policy.IncludeSchool && inUse.Contains(InstitutionType.School),
            IncludeCollege: policy.IncludeCollege && inUse.Contains(InstitutionType.College),
            IncludeUniversity: policy.IncludeUniversity && inUse.Contains(InstitutionType.University));
        return narrowed.IsValid ? narrowed : policy;
    }
}
