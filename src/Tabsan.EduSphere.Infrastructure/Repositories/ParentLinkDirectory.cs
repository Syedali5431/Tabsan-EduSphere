using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Repositories;

/// <summary>Parent links screen — searches for Parent-role accounts and School students, and a parent's links with names.</summary>
public sealed class ParentLinkDirectory : IParentLinkDirectory
{
    private readonly ApplicationDbContext _db;

    public ParentLinkDirectory(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ParentAccountLookupDto>> SearchParentsAsync(string? search, ParentLinkScope scope, int take = 20, CancellationToken ct = default)
    {
        var term = Term(search);
        var parents = Parents(scope);
        if (term is not null)
            parents = parents.Where(u => u.Username.Contains(term)
                || (u.FullName != null && u.FullName.Contains(term))
                || (u.Email != null && u.Email.Contains(term))
                || (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));

        return await parents
            .OrderBy(u => u.FullName ?? u.Username).ThenBy(u => u.Username)
            .Take(Clamp(take))
            .Select(u => new ParentAccountLookupDto(u.Id, u.Username, u.FullName, u.Email, u.PhoneNumber, u.IsActive,
                _db.ParentStudentLinks.Count(l => l.ParentUserId == u.Id && !l.IsDeleted)))
            .ToListAsync(ct);
    }

    public Task<ParentAccountLookupDto?> GetParentAsync(Guid parentUserId, ParentLinkScope scope, CancellationToken ct = default)
        => Parents(scope)
            .Where(u => u.Id == parentUserId)
            .Select(u => new ParentAccountLookupDto(u.Id, u.Username, u.FullName, u.Email, u.PhoneNumber, u.IsActive,
                _db.ParentStudentLinks.Count(l => l.ParentUserId == u.Id && !l.IsDeleted)))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<SchoolStudentLookupDto>> SearchSchoolStudentsAsync(string? search, ParentLinkScope scope, int take = 20, CancellationToken ct = default)
    {
        var term = Term(search);
        var students = SchoolStudents(scope);
        if (term is not null)
            students = students.Where(s => s.Profile.RegistrationNumber.Contains(term)
                || s.User.Username.Contains(term)
                || (s.User.FullName != null && s.User.FullName.Contains(term)));

        var rows = await students
            .OrderBy(s => s.Profile.RegistrationNumber)
            .Take(Clamp(take))
            .Select(s => new
            {
                s.Profile.Id, s.Profile.RegistrationNumber, s.User.FullName, s.User.Username,
                DepartmentName = s.Profile.Department.Name, ProgramName = s.Profile.Program.Name,
                s.Profile.CurrentSemesterNumber, s.Profile.Status,
                LinkedParents = _db.ParentStudentLinks.Count(l => l.StudentProfileId == s.Profile.Id && !l.IsDeleted)
            })
            .ToListAsync(ct);

        return rows.Select(r => new SchoolStudentLookupDto(r.Id, r.RegistrationNumber, r.FullName ?? r.Username, r.DepartmentName,
            r.ProgramName, r.CurrentSemesterNumber, r.Status.ToString(), r.LinkedParents)).ToList();
    }

    public async Task<IReadOnlyList<ParentStudentLinkDetailDto>> GetParentLinksAsync(Guid parentUserId, ParentLinkScope scope, CancellationToken ct = default)
    {
        var rows = await _db.ParentStudentLinks.AsNoTracking()
            .Where(l => l.ParentUserId == parentUserId && !l.IsDeleted)
            .Join(SchoolStudents(scope), l => l.StudentProfileId, s => s.Profile.Id, (l, s) => new
            {
                l.ParentUserId, l.StudentProfileId, s.Profile.RegistrationNumber, s.User.FullName, s.User.Username,
                DepartmentName = s.Profile.Department.Name, ProgramName = s.Profile.Program.Name,
                s.Profile.CurrentSemesterNumber, l.Relationship, l.IsActive, l.CreatedAt
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new ParentStudentLinkDetailDto(r.ParentUserId, r.StudentProfileId, r.RegistrationNumber, r.FullName ?? r.Username,
                r.DepartmentName, r.ProgramName, r.CurrentSemesterNumber, r.Relationship, r.IsActive, r.CreatedAt))
            .OrderBy(r => r.StudentName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public Task<Guid?> GetStudentDepartmentIdAsync(Guid studentProfileId, CancellationToken ct = default)
        => _db.StudentProfiles.AsNoTracking()
            .Where(p => p.Id == studentProfileId)
            .Select(p => (Guid?)p.DepartmentId)
            .FirstOrDefaultAsync(ct);

    private IQueryable<Domain.Identity.User> Parents(ParentLinkScope scope)
    {
        var parents = _db.Users.AsNoTracking().Where(u => !u.IsDeleted && u.Role.Name == "Parent");
        // Another school's parents stay hidden; accounts without a tenant are visible to every administrator.
        if (scope.TenantId is { } tenantId)
            parents = parents.Where(u => u.TenantId == null || u.TenantId == tenantId);
        return parents;
    }

    private IQueryable<SchoolStudentRow> SchoolStudents(ParentLinkScope scope)
    {
        var profiles = _db.StudentProfiles.AsNoTracking().Where(p => p.Department.InstitutionType == InstitutionType.School);
        if (scope.DepartmentIds is { } departmentIds)
        {
            var allowed = departmentIds.ToList();
            profiles = profiles.Where(p => allowed.Contains(p.DepartmentId));
        }
        return profiles.Join(_db.Users.Where(u => !u.IsDeleted), p => p.UserId, u => u.Id, (p, u) => new SchoolStudentRow { Profile = p, User = u });
    }

    private static string? Term(string? search) => string.IsNullOrWhiteSpace(search) ? null : search.Trim();

    private static int Clamp(int take) => Math.Clamp(take, 1, IParentLinkDirectory.MaxResults);

    private sealed class SchoolStudentRow
    {
        public Domain.Academic.StudentProfile Profile { get; init; } = default!;
        public Domain.Identity.User User { get; init; } = default!;
    }
}
