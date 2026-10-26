namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Which institution types the portal shows. The institution policy says which types are allowed; this narrows it to the
/// types that have data (active departments), so a School-only installation shows no College or University menus,
/// modules, filters or reports. When College or University departments are added later, those types appear again on
/// their own. Only what is displayed follows this: data entry and validation keep using
/// <see cref="IInstitutionPolicyService"/>, so the first College or University department can still be created.
/// </summary>
public interface IInstitutionVisibilityService
{
    /// <summary>
    /// The policy's institution types that have at least one active department. While none of them has data yet (a new
    /// installation), the policy itself, so the portal is never left without an institution type to show.
    /// </summary>
    Task<InstitutionPolicySnapshot> GetVisibilityAsync(CancellationToken ct = default);

    /// <summary>Forgets the cached answer (after departments are added, activated or deactivated).</summary>
    void InvalidateCache();
}
