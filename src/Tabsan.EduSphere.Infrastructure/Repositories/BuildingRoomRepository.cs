using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IBuildingRoomRepository.
/// Buildings and rooms use soft-delete via AuditableEntity; query filters are applied in EF config.
/// </summary>
public class BuildingRoomRepository : IBuildingRoomRepository
{
    private readonly ApplicationDbContext _db;

    public BuildingRoomRepository(ApplicationDbContext db) => _db = db;

    // ── Buildings ────────────────────────────────────────────────────────────

    public async Task<IList<Building>> GetAllBuildingsAsync(bool activeOnly = true, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default)
        => await _db.Buildings
              .Where(b => !activeOnly || b.IsActive)
              .Where(b => !tenantId.HasValue || b.TenantId == tenantId.Value)
              .Where(b => !campusId.HasValue || b.CampusId == campusId.Value)
              .OrderBy(b => b.Name)
              .ToListAsync(ct);

    public Task<Building?> GetBuildingByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Buildings
              .Include(b => b.Rooms.Where(r => r.IsActive))
              .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task AddBuildingAsync(Building building, CancellationToken ct = default)
        => await _db.Buildings.AddAsync(building, ct);

    public void UpdateBuilding(Building building) => _db.Buildings.Update(building);

    // ── Rooms ────────────────────────────────────────────────────────────────

    public async Task<IList<Room>> GetAllRoomsAsync(bool activeOnly = true, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default)
        => await _db.Rooms
              .Include(r => r.Building)
              .Where(r => !activeOnly || r.IsActive)
              .Where(r => !tenantId.HasValue || r.TenantId == tenantId.Value)
              .Where(r => !campusId.HasValue || r.CampusId == campusId.Value)
              .OrderBy(r => r.Building.Name)
              .ThenBy(r => r.Number)
              .ToListAsync(ct);

    public async Task<IList<Room>> GetRoomsByBuildingAsync(Guid buildingId, bool activeOnly = true, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default)
        => await _db.Rooms
              .Include(r => r.Building)
              .Where(r => r.BuildingId == buildingId && (!activeOnly || r.IsActive))
              .Where(r => !tenantId.HasValue || r.TenantId == tenantId.Value)
              .Where(r => !campusId.HasValue || r.CampusId == campusId.Value)
              .OrderBy(r => r.Number)
              .ToListAsync(ct);

    public Task<Room?> GetRoomByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Rooms
              .Include(r => r.Building)
              .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddRoomAsync(Room room, CancellationToken ct = default)
        => await _db.Rooms.AddAsync(room, ct);

    public void UpdateRoom(Room room) => _db.Rooms.Update(room);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
