using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Tenancy;

/// <summary>
/// Represents a tenant-owned campus (physical or logical location).
/// </summary>
public class Campus : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    private Campus() { }

    public Campus(Guid tenantId, string code, string name)
    {
        TenantId = tenantId;
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
