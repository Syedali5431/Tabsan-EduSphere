namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Resolves caller access scope from the current request identity.
/// SuperAdmin callers bypass tenant/campus scoping.
/// </summary>
public interface IAccessScopeResolver
{
    bool IsSuperAdmin();
    Guid? GetTenantId();
    Guid? GetCampusId();
}
