using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Academic;

/// <summary>
/// A room or hall within a building used for scheduling class sessions.
/// Admin and Super Admin manage the room catalogue.
/// </summary>
public class Room : AuditableEntity
{
    /// <summary>Tenant scope for this room (nullable for global/system data).</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Campus scope for this room (nullable for global/system data).</summary>
    public Guid? CampusId { get; private set; }

    /// <summary>Room identifier displayed in timetables (e.g. "101", "CS-Lab-2").</summary>
    public string Number { get; private set; } = default!;

    /// <summary>FK to the building this room belongs to.</summary>
    public Guid BuildingId { get; private set; }

    /// <summary>Navigation to the building.</summary>
    public Building Building { get; private set; } = default!;

    /// <summary>Optional seating capacity.</summary>
    public int? Capacity { get; private set; }

    /// <summary>When false the room is hidden from timetable entry dropdowns.</summary>
    public bool IsActive { get; private set; } = true;

    private Room() { }

    public Room(Guid buildingId, string number, int? capacity = null, Guid? tenantId = null, Guid? campusId = null)
    {
        TenantId = tenantId;
        CampusId = campusId;
        BuildingId = buildingId;
        Number = number.Trim();
        Capacity = capacity;
    }

    public void Update(string number, int? capacity)
    {
        Number = number.Trim();
        Capacity = capacity;
        Touch();
    }

    public void Activate() { IsActive = true; Touch(); }
    public void Deactivate() { IsActive = false; Touch(); }
}
