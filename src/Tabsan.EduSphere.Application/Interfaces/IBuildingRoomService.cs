using Tabsan.EduSphere.Application.Dtos;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Service contract for managing the Building and Room catalogues.
/// Buildings and rooms are created by Admin/SuperAdmin and appear in timetable entry dropdowns.
/// </summary>
public interface IBuildingRoomService
{
    // ── Buildings ────────────────────────────────────────────────────────────

    /// <summary>Returns all buildings (active-only by default — for dropdown population).</summary>
    Task<IList<BuildingDto>> GetAllBuildingsAsync(bool activeOnly = true, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default);

    /// <summary>Returns a single building by ID (with its rooms).</summary>
    Task<BuildingDto> GetBuildingByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates a new building.</summary>
    Task<BuildingDto> CreateBuildingAsync(CreateBuildingCommand cmd, Guid? tenantId, Guid? campusId, CancellationToken ct = default);

    /// <summary>Updates a building's name and code.</summary>
    Task<BuildingDto> UpdateBuildingAsync(Guid id, UpdateBuildingCommand cmd, CancellationToken ct = default);

    /// <summary>Activates a building (makes it visible in dropdowns).</summary>
    Task ActivateBuildingAsync(Guid id, CancellationToken ct = default);

    /// <summary>Deactivates a building (hides it from dropdowns).</summary>
    Task DeactivateBuildingAsync(Guid id, CancellationToken ct = default);

    // ── Rooms ────────────────────────────────────────────────────────────────

    /// <summary>Returns all rooms across all buildings (active-only by default).</summary>
    Task<IList<RoomDto>> GetAllRoomsAsync(bool activeOnly = true, CancellationToken ct = default);

    /// <summary>Returns rooms for a specific building.</summary>
    Task<IList<RoomDto>> GetRoomsByBuildingAsync(Guid buildingId, bool activeOnly = true, CancellationToken ct = default);

    /// <summary>Returns a single room by ID.</summary>
    Task<RoomDto> GetRoomByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates a new room within a building.</summary>
    Task<RoomDto> CreateRoomAsync(CreateRoomCommand cmd, CancellationToken ct = default);

    /// <summary>Updates a room's number and capacity.</summary>
    Task<RoomDto> UpdateRoomAsync(Guid id, UpdateRoomCommand cmd, CancellationToken ct = default);

    /// <summary>Activates a room.</summary>
    Task ActivateRoomAsync(Guid id, CancellationToken ct = default);

    /// <summary>Deactivates a room (hides it from dropdowns).</summary>
    Task DeactivateRoomAsync(Guid id, CancellationToken ct = default);
}
