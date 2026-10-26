using Microsoft.Extensions.Caching.Memory;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Services;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Which institution types the portal shows: the policy narrowed to the types that have active departments.</summary>
public class InstitutionVisibilityTests
{
    private static readonly InstitutionPolicySnapshot AllEnabled = new(IncludeSchool: true, IncludeCollege: true, IncludeUniversity: true);

    [Fact]
    public void SchoolDataOnly_ShowsSchoolOnly()
    {
        var shown = InstitutionVisibilityService.Narrow(AllEnabled, [InstitutionType.School]);
        Assert.Equal((true, false, false), (shown.IncludeSchool, shown.IncludeCollege, shown.IncludeUniversity));
    }

    [Fact]
    public void EveryTypeWithData_ShowsWhatThePolicyAllows()
    {
        var all = new[] { InstitutionType.School, InstitutionType.College, InstitutionType.University };
        Assert.Equal(AllEnabled, InstitutionVisibilityService.Narrow(AllEnabled, all));

        // A type switched off in the policy stays hidden even with data.
        var noCollege = new InstitutionPolicySnapshot(IncludeSchool: true, IncludeCollege: false, IncludeUniversity: true);
        Assert.Equal(noCollege, InstitutionVisibilityService.Narrow(noCollege, all));
    }

    [Fact]
    public void NoEnabledTypeHasData_ThePolicyAppliesAsItIs()
    {
        // A new installation without departments.
        Assert.Equal(AllEnabled, InstitutionVisibilityService.Narrow(AllEnabled, []));
        // School data while the policy allows University only: the portal is never left with nothing to show.
        Assert.Equal(InstitutionPolicySnapshot.Default, InstitutionVisibilityService.Narrow(InstitutionPolicySnapshot.Default, [InstitutionType.School]));
    }

    [Fact]
    public async Task CollegeAddedLater_IsShownOnceTheCachedAnswerIsRefreshed()
    {
        var departments = new StubDepartments(InstitutionType.School);
        var service = new InstitutionVisibilityService(new StubPolicy(AllEnabled), departments, new MemoryCache(new MemoryCacheOptions()));

        var before = await service.GetVisibilityAsync();
        Assert.Equal((true, false, false), (before.IncludeSchool, before.IncludeCollege, before.IncludeUniversity));

        // The answer is cached: a College department added in the database is not seen until the cache is refreshed.
        departments.Types.Add(InstitutionType.College);
        Assert.False((await service.GetVisibilityAsync()).IncludeCollege);
        Assert.Equal(1, departments.Reads);

        service.InvalidateCache(); // what adding a department in the portal does
        var after = await service.GetVisibilityAsync();
        Assert.Equal((true, true, false), (after.IncludeSchool, after.IncludeCollege, after.IncludeUniversity));
        Assert.Equal(2, departments.Reads);
    }

    private sealed class StubPolicy(InstitutionPolicySnapshot snapshot) : IInstitutionPolicyService
    {
        public Task<InstitutionPolicySnapshot> GetPolicyAsync(CancellationToken ct = default) => Task.FromResult(snapshot);
        public Task SavePolicyAsync(SaveInstitutionPolicyCommand cmd, CancellationToken ct = default) => Task.CompletedTask;
        public void InvalidateCache() { }
    }

    private sealed class StubDepartments(params InstitutionType[] types) : IDepartmentRepository
    {
        public List<InstitutionType> Types { get; } = [.. types];
        public int Reads { get; private set; }

        public Task<IReadOnlyList<InstitutionType>> GetInstitutionTypesInUseAsync(CancellationToken ct = default)
        {
            Reads++;
            return Task.FromResult<IReadOnlyList<InstitutionType>>(Types.ToList());
        }

        public Task<IReadOnlyList<Department>> GetAllAsync(Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Department?> GetByIdAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> CodeExistsAsync(string code, Guid? tenantId = null, Guid? campusId = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(Department department, CancellationToken ct = default) => throw new NotSupportedException();
        public void Update(Department department) => throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }
}
