using Tabsan.EduSphere.Domain.Academic;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>
/// Repository contract for Building and Room catalogue operations.
/// Buildings and rooms are managed by Admin/SuperAdmin and used in timetable entry dropdowns.
/// </summary>
public interface IBuildingRoomRepository
{
    // ── Buildings ────────────────────────────────────────────────────────────

    /// <summary>Returns all buildings, optionally filtering to active-only records.</summary>
    Task<IList<Building>> GetAllBuildingsAsync(bool activeOnly = true, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default);

    /// <summary>Returns a building by ID (with its rooms collection loaded).</summary>
    Task<Building?> GetBuildingByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Persists a new building.</summary>
    Task AddBuildingAsync(Building building, CancellationToken ct = default);

    /// <summary>Marks a building entity as modified.</summary>
    void UpdateBuilding(Building building);

    // ── Rooms ────────────────────────────────────────────────────────────────

    /// <summary>Returns all rooms, optionally filtering to active-only records. Includes building name.</summary>
    Task<IList<Room>> GetAllRoomsAsync(bool activeOnly = true, CancellationToken ct = default);

    /// <summary>Returns rooms belonging to a specific building.</summary>
    Task<IList<Room>> GetRoomsByBuildingAsync(Guid buildingId, bool activeOnly = true, CancellationToken ct = default);

    /// <summary>Returns a room by ID (with Building navigation loaded).</summary>
    Task<Room?> GetRoomByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Persists a new room.</summary>
    Task AddRoomAsync(Room room, CancellationToken ct = default);

    /// <summary>Marks a room entity as modified.</summary>
    void UpdateRoom(Room room);

    /// <summary>Commits all pending changes.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
