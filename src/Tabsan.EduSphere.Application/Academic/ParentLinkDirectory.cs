namespace Tabsan.EduSphere.Application.Academic;

// Parent links screen (2026-10-30) — administrators find Parent-role accounts and School students by name instead of ids.

/// <summary>
/// What an administrator may see on the parent links screen. Null members mean "no restriction" (SuperAdmin).
/// An Admin sees the students of the departments assigned to them, and the parent accounts of their tenant (any campus,
/// since brothers and sisters can be at different campuses of one school) or without a tenant.
/// </summary>
public sealed record ParentLinkScope(IReadOnlyCollection<Guid>? DepartmentIds, Guid? TenantId)
{
    public static readonly ParentLinkScope Unrestricted = new(null, null);

    public bool AllowsDepartment(Guid departmentId) => DepartmentIds is null || DepartmentIds.Contains(departmentId);
}

/// <summary>A Parent-role account found by the parent links search.</summary>
public sealed record ParentAccountLookupDto(
    Guid UserId,
    string Username,
    string? FullName,
    string? Email,
    string? PhoneNumber,
    bool IsActive,
    int LinkedStudents);

/// <summary>A School student found by the parent links search.</summary>
public sealed record SchoolStudentLookupDto(
    Guid StudentProfileId,
    string RegistrationNumber,
    string Name,
    string? DepartmentName,
    string? ProgramName,
    int ClassNumber,
    string Status,
    int LinkedParents);

/// <summary>One of a parent's links with the child's name and class.</summary>
public sealed record ParentStudentLinkDetailDto(
    Guid ParentUserId,
    Guid StudentProfileId,
    string RegistrationNumber,
    string StudentName,
    string? DepartmentName,
    string? ProgramName,
    int ClassNumber,
    string? Relationship,
    bool IsActive,
    DateTime LinkedAt);

/// <summary>Read side of the parent links screen (searches run in the database).</summary>
public interface IParentLinkDirectory
{
    /// <summary>Most results a search returns.</summary>
    const int MaxResults = 50;

    /// <summary>Parent-role accounts whose username, name, email or phone contains <paramref name="search"/> (all when empty), by name.</summary>
    Task<IReadOnlyList<ParentAccountLookupDto>> SearchParentsAsync(string? search, ParentLinkScope scope, int take = 20, CancellationToken ct = default);

    /// <summary>One Parent-role account, or null when it is not a parent or outside the scope.</summary>
    Task<ParentAccountLookupDto?> GetParentAsync(Guid parentUserId, ParentLinkScope scope, CancellationToken ct = default);

    /// <summary>School students whose registration number, name or username contains <paramref name="search"/>, by registration number.</summary>
    Task<IReadOnlyList<SchoolStudentLookupDto>> SearchSchoolStudentsAsync(string? search, ParentLinkScope scope, int take = 20, CancellationToken ct = default);

    /// <summary>The parent's links (removed ones excluded) to students inside the scope, by student name.</summary>
    Task<IReadOnlyList<ParentStudentLinkDetailDto>> GetParentLinksAsync(Guid parentUserId, ParentLinkScope scope, CancellationToken ct = default);

    /// <summary>The department of a student profile, or null when there is no such student.</summary>
    Task<Guid?> GetStudentDepartmentIdAsync(Guid studentProfileId, CancellationToken ct = default);
}
