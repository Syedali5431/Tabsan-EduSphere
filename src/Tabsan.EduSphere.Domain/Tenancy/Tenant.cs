using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Tenancy;

/// <summary>
/// Represents a top-level customer organization in the multi-tenant model.
/// </summary>
public class Tenant : AuditableEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    private Tenant() { }

    public Tenant(string code, string name)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
    }

    public void Rename(string name)
    {
        Name = name.Trim();
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}
