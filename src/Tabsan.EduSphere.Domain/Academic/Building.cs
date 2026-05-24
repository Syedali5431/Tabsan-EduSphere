using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Academic;

/// <summary>
/// A physical building on campus that can contain rooms used in timetable scheduling.
/// Admin and Super Admin manage the building catalogue.
/// </summary>
public class Building : AuditableEntity
{
    /// <summary>Owning tenant scope for this building.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Owning campus scope for this building.</summary>
    public Guid? CampusId { get; private set; }

    /// <summary>Full building name (e.g. "Computer Science Block").</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Short code used in timetable display (e.g. "CS-Block", "Main").</summary>
    public string Code { get; private set; } = default!;

    /// <summary>When false the building is hidden from timetable entry dropdowns.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Rooms located in this building.</summary>
    public ICollection<Room> Rooms { get; private set; } = new List<Room>();

    private Building() { }

    public Building(Guid? tenantId, Guid? campusId, string name, string code)
    {
        TenantId = tenantId;
        CampusId = campusId;
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
    }

    public void Update(string name, string code)
    {
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Touch();
    }

    public void Activate() { IsActive = true; Touch(); }
    public void Deactivate() { IsActive = false; Touch(); }
}
