using FluentAssertions;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Services;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.UnitTests;

public class BuildingRoomServiceTests
{
    [Fact]
    public async Task CreateBuildingAsync_RejectsBlankNameBeforePersisting()
    {
        var repo = new FakeBuildingRoomRepository();
        var service = new BuildingRoomService(repo);

        var act = async () => await service.CreateBuildingAsync(
            new CreateBuildingCommand("   ", "ABC"),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        repo.SavedBuilding.Should().BeNull();
    }

    private sealed class FakeBuildingRoomRepository : IBuildingRoomRepository
    {
        public Building? SavedBuilding { get; private set; }

        public Task<IList<Building>> GetAllBuildingsAsync(bool activeOnly = true, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default)
            => Task.FromResult<IList<Building>>(new List<Building>());

        public Task<Building?> GetBuildingByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<Building?>(null);

        public Task AddBuildingAsync(Building building, CancellationToken ct = default)
        {
            SavedBuilding = building;
            return Task.CompletedTask;
        }

        public void UpdateBuilding(Building building)
        {
        }

        public Task<IList<Room>> GetAllRoomsAsync(bool activeOnly = true, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default)
            => Task.FromResult<IList<Room>>(new List<Room>());

        public Task<IList<Room>> GetRoomsByBuildingAsync(Guid buildingId, bool activeOnly = true, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default)
            => Task.FromResult<IList<Room>>(new List<Room>());

        public Task<Room?> GetRoomByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<Room?>(null);

        public Task AddRoomAsync(Room room, CancellationToken ct = default)
            => Task.CompletedTask;

        public void UpdateRoom(Room room)
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => Task.FromResult(1);
    }
}
