using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Services;

/// <summary>
/// Manages the Building and Room catalogues used as timetable entry dropdown sources.
/// </summary>
public class BuildingRoomService : IBuildingRoomService
{
    private readonly IBuildingRoomRepository _repo;

    public BuildingRoomService(IBuildingRoomRepository repo) => _repo = repo;

    // ── Buildings ────────────────────────────────────────────────────────────

    public async Task<IList<BuildingDto>> GetAllBuildingsAsync(bool activeOnly = true, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default)
    {
        var buildings = await _repo.GetAllBuildingsAsync(activeOnly, tenantId, campusId, ct);
        return buildings.Select(MapBuilding).ToList();
    }

    public async Task<BuildingDto> GetBuildingByIdAsync(Guid id, CancellationToken ct = default)
    {
        var b = await _repo.GetBuildingByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Building {id} not found.");
        return MapBuilding(b);
    }

    public async Task<BuildingDto> CreateBuildingAsync(CreateBuildingCommand cmd, Guid? tenantId, Guid? campusId, CancellationToken ct = default)
    {
        var building = new Building(tenantId, campusId, cmd.Name, cmd.Code);
        await _repo.AddBuildingAsync(building, ct);
        await _repo.SaveChangesAsync(ct);
        return MapBuilding(building);
    }

    public async Task<BuildingDto> UpdateBuildingAsync(Guid id, UpdateBuildingCommand cmd, CancellationToken ct = default)
    {
        var b = await _repo.GetBuildingByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Building {id} not found.");
        b.Update(cmd.Name, cmd.Code);
        _repo.UpdateBuilding(b);
        await _repo.SaveChangesAsync(ct);
        return MapBuilding(b);
    }

    public async Task ActivateBuildingAsync(Guid id, CancellationToken ct = default)
    {
        var b = await _repo.GetBuildingByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Building {id} not found.");
        b.Activate();
        _repo.UpdateBuilding(b);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task DeactivateBuildingAsync(Guid id, CancellationToken ct = default)
    {
        var b = await _repo.GetBuildingByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Building {id} not found.");
        b.Deactivate();
        _repo.UpdateBuilding(b);
        await _repo.SaveChangesAsync(ct);
    }

    // ── Rooms ────────────────────────────────────────────────────────────────

    public async Task<IList<RoomDto>> GetAllRoomsAsync(bool activeOnly = true, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default)
    {
        var rooms = await _repo.GetAllRoomsAsync(activeOnly, tenantId, campusId, ct);
        return rooms.Select(MapRoom).ToList();
    }

    public async Task<IList<RoomDto>> GetRoomsByBuildingAsync(Guid buildingId, bool activeOnly = true, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default)
    {
        var rooms = await _repo.GetRoomsByBuildingAsync(buildingId, activeOnly, tenantId, campusId, ct);
        return rooms.Select(MapRoom).ToList();
    }

    public async Task<RoomDto> GetRoomByIdAsync(Guid id, CancellationToken ct = default)
    {
        var r = await _repo.GetRoomByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Room {id} not found.");
        return MapRoom(r);
    }

    public async Task<RoomDto> CreateRoomAsync(CreateRoomCommand cmd, Guid? tenantId, Guid? campusId, CancellationToken ct = default)
    {
        // Validate building exists
        var building = await _repo.GetBuildingByIdAsync(cmd.BuildingId, ct)
            ?? throw new KeyNotFoundException($"Building {cmd.BuildingId} not found.");

        var room = new Room(cmd.BuildingId, cmd.Number, cmd.Capacity, tenantId, campusId);
        await _repo.AddRoomAsync(room, ct);
        await _repo.SaveChangesAsync(ct);

        // Re-fetch to get building navigation loaded
        var loaded = await _repo.GetRoomByIdAsync(room.Id, ct)!;
        return MapRoom(loaded!);
    }

    public async Task<RoomDto> UpdateRoomAsync(Guid id, UpdateRoomCommand cmd, CancellationToken ct = default)
    {
        var r = await _repo.GetRoomByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Room {id} not found.");
        r.Update(cmd.Number, cmd.Capacity);
        _repo.UpdateRoom(r);
        await _repo.SaveChangesAsync(ct);
        return MapRoom(r);
    }

    public async Task ActivateRoomAsync(Guid id, CancellationToken ct = default)
    {
        var r = await _repo.GetRoomByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Room {id} not found.");
        r.Activate();
        _repo.UpdateRoom(r);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task DeactivateRoomAsync(Guid id, CancellationToken ct = default)
    {
        var r = await _repo.GetRoomByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Room {id} not found.");
        r.Deactivate();
        _repo.UpdateRoom(r);
        await _repo.SaveChangesAsync(ct);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static BuildingDto MapBuilding(Building b) => new(b.Id, b.TenantId, b.CampusId, b.Name, b.Code, b.IsActive);

    private static RoomDto MapRoom(Room r) => new(
        r.Id,
        r.TenantId,
        r.CampusId,
        r.BuildingId,
        r.Building?.Name ?? string.Empty,
        r.Building?.Code ?? string.Empty,
        r.Number,
        r.Capacity,
        r.IsActive
    );
}
