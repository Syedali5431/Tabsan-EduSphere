using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.Application.DTOs.Auth;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

[ApiController]
[Route("api/v1/user-settings")]
[Authorize]
public class UserSettingsController : ControllerBase
{
    private const string ResetPasswordDefault = "EduSphere147";

    private readonly ApplicationDbContext _db;
    private readonly IUserRepository _users;
    private readonly IStudentProfileRepository _studentProfiles;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordHistoryRepository _passwordHistory;
    private readonly IAuditService _audit;
    private readonly IAccessScopeResolver _accessScope;

    public UserSettingsController(
        ApplicationDbContext db,
        IUserRepository users,
        IStudentProfileRepository studentProfiles,
        IPasswordHasher passwordHasher,
        IPasswordHistoryRepository passwordHistory,
        IAuditService audit,
        IAccessScopeResolver accessScope)
    {
        _db = db;
        _users = users;
        _studentProfiles = studentProfiles;
        _passwordHasher = passwordHasher;
        _passwordHistory = passwordHistory;
        _audit = audit;
        _accessScope = accessScope;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var user = await GetCurrentUserAsync(ct);
        if (user is null)
            return NotFound(new { message = "Current user was not found." });

        return Ok(await BuildDtoAsync(user, ct));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserSettingsRequest request, CancellationToken ct)
    {
        var user = await GetCurrentUserAsync(ct);
        if (user is null)
            return NotFound(new { message = "Current user was not found." });

        return await UpdateUserAsync(user, request, ct);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseId,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var currentUser = await GetCurrentUserAsync(ct);
        if (currentUser is null)
            return Forbid();

        var scope = ResolveEffectiveScope(tenantId, campusId, _accessScope.IsSuperAdmin());
        if (scope.Error is not null)
            return scope.Error;

        var userQuery = _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .AsQueryable();

        if (scope.TenantId.HasValue)
            userQuery = userQuery.Where(u => u.TenantId == scope.TenantId);

        if (scope.CampusId.HasValue)
            userQuery = userQuery.Where(u => u.CampusId == scope.CampusId);

        var users = await userQuery.OrderBy(u => u.Username).ToListAsync(ct);
        var results = new List<UserSettingsUserDto>(users.Count);

        foreach (var user in users)
        {
            if (!string.Equals(currentUser.Role.Name, "SuperAdmin", StringComparison.OrdinalIgnoreCase)
                && string.Equals(user.Role.Name, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dto = await BuildDtoAsync(user, ct);
            if (departmentId.HasValue && dto.DepartmentId != departmentId.Value)
                continue;

            if (courseId.HasValue && dto.CourseId != courseId.Value)
                continue;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                if (!Contains(dto.Username, term) && !Contains(dto.Email, term) && !Contains(dto.PhoneNumber, term) && !Contains(dto.Address, term))
                    continue;
            }

            results.Add(dto);
        }

        return Ok(results);
    }

    [HttpGet("{userId:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetById(Guid userId, CancellationToken ct)
    {
        var currentUser = await GetCurrentUserAsync(ct);
        if (currentUser is null)
            return Forbid();

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
            return NotFound(new { message = "User not found." });

        if (!string.Equals(currentUser.Role.Name, "SuperAdmin", StringComparison.OrdinalIgnoreCase)
            && string.Equals(user.Role.Name, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(await BuildDtoAsync(user, ct));
    }

    [HttpPut("{userId:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateById(Guid userId, [FromBody] UpdateUserSettingsRequest request, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
            return NotFound(new { message = "User not found." });

        var currentUser = await GetCurrentUserAsync(ct);
        if (currentUser is null)
            return Forbid();

        if (!await CanManageTargetAsync(currentUser, user, ct))
            return Forbid();

        return await UpdateUserAsync(user, request, ct);
    }

    [HttpPost("{userId:guid}/reset-password")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ResetPassword(Guid userId, CancellationToken ct)
    {
        var currentUser = await GetCurrentUserAsync(ct);
        if (currentUser is null)
            return Forbid();

        var target = await _users.GetByIdAsync(userId, ct);
        if (target is null)
            return NotFound(new { message = "User not found." });

        if (!await CanManageTargetAsync(currentUser, target, ct))
            return Forbid();

        var newHash = _passwordHasher.Hash(ResetPasswordDefault);
        target.UpdatePasswordHash(newHash);
        target.RequirePasswordChange();
        if (target.IsLockedOut)
            target.UnlockAccount();

        _users.Update(target);
        await _users.SaveChangesAsync(ct);

        await _passwordHistory.AddAsync(new PasswordHistoryEntry(target.Id, newHash), ct);
        await _passwordHistory.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditLog(
            action: "ResetUserSettingsPassword",
            entityName: "User",
            entityId: target.Id.ToString(),
            actorUserId: currentUser.Id,
            oldValuesJson: null,
            newValuesJson: "{\"passwordReset\":\"EduSphere147\",\"mustChangePassword\":true}",
            ipAddress: null), ct);

        return NoContent();
    }

    private async Task<IActionResult> UpdateUserAsync(User user, UpdateUserSettingsRequest request, CancellationToken ct)
    {
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var phone = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        var address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();

        if (!string.IsNullOrWhiteSpace(email))
        {
            var existing = await _users.GetByEmailAsync(email, ct);
            if (existing is not null && existing.Id != user.Id)
                return Conflict(new { message = "Email already exists." });
        }

        user.UpdateEmail(email);
        user.UpdatePhoneNumber(phone);
        user.UpdateAddress(address);
        _users.Update(user);
        await _users.SaveChangesAsync(ct);

        return Ok(await BuildDtoAsync(user, ct));
    }

    private async Task<UserSettingsUserDto> BuildDtoAsync(User user, CancellationToken ct)
    {
        var student = await _studentProfiles.GetByUserIdAsync(user.Id, ct);
        string? departmentName = null;
        Guid? departmentId = user.DepartmentId;
        Guid? courseId = null;
        string? courseName = null;

        if (student is not null)
        {
            departmentId = student.DepartmentId;
            departmentName = student.Department?.Name;

            var latestEnrollment = await _db.Enrollments
                .AsNoTracking()
                .Include(e => e.CourseOffering)
                    .ThenInclude(o => o.Course)
                .Where(e => e.StudentProfileId == student.Id)
                .OrderByDescending(e => e.EnrolledAt)
                .FirstOrDefaultAsync(ct);

            if (latestEnrollment?.CourseOffering is not null)
            {
                courseId = latestEnrollment.CourseOffering.CourseId;
                courseName = latestEnrollment.CourseOffering.Course?.Title;
            }
        }
        else if (departmentId.HasValue)
        {
            departmentName = await _db.Departments
                .AsNoTracking()
                .Where(d => d.Id == departmentId.Value)
                .Select(d => d.Name)
                .FirstOrDefaultAsync(ct);
        }

        if (courseId is null && string.Equals(user.Role.Name, "Faculty", StringComparison.OrdinalIgnoreCase))
        {
            var facultyCourse = await _db.CourseOfferings
                .AsNoTracking()
                .Include(o => o.Course)
                .Where(o => o.FacultyUserId == user.Id)
                .OrderByDescending(o => o.SemesterId)
                .Select(o => new { o.CourseId, CourseName = o.Course.Title })
                .FirstOrDefaultAsync(ct);

            if (facultyCourse is not null)
            {
                courseId = facultyCourse.CourseId;
                courseName = facultyCourse.CourseName;
            }
        }

        return new UserSettingsUserDto(
            user.Id,
            user.Username,
            user.Role.Name,
            user.Email,
            user.PhoneNumber,
            user.Address,
            user.IsActive,
            user.TenantId,
            user.CampusId,
            departmentId,
            departmentName,
            courseId,
            courseName);
    }

    private async Task<User?> GetCurrentUserAsync(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return null;

        return await _users.GetByIdAsync(userId, ct);
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    private (Guid? TenantId, Guid? CampusId, IActionResult? Error) ResolveEffectiveScope(Guid? requestedTenantId, Guid? requestedCampusId, bool allowIndependentFilters)
    {
        Guid? effectiveTenantId = requestedTenantId;
        Guid? effectiveCampusId = requestedCampusId;

        if (!_accessScope.IsSuperAdmin())
        {
            var callerTenantId = _accessScope.GetTenantId();
            var callerCampusId = _accessScope.GetCampusId();

            if (callerTenantId.HasValue)
            {
                if (requestedTenantId.HasValue && requestedTenantId.Value != callerTenantId.Value)
                    return (null, null, Forbid());

                effectiveTenantId = callerTenantId.Value;
            }

            if (callerCampusId.HasValue)
            {
                if (requestedCampusId.HasValue && requestedCampusId.Value != callerCampusId.Value)
                    return (null, null, Forbid());

                effectiveCampusId = callerCampusId.Value;
            }
        }

        if (!allowIndependentFilters && effectiveTenantId.HasValue != effectiveCampusId.HasValue)
            return (null, null, BadRequest(new { message = "TenantId and CampusId must be provided together." }));

        return (effectiveTenantId, effectiveCampusId, null);
    }

    private async Task<bool> CanManageTargetAsync(User currentUser, User target, CancellationToken ct)
    {
        if (currentUser.Role.Name == "SuperAdmin")
            return true;

        if (!string.Equals(currentUser.Role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.Equals(target.Role.Name, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            return false;

        if (currentUser.TenantId.HasValue && target.TenantId.HasValue && currentUser.TenantId != target.TenantId)
            return false;

        if (currentUser.CampusId.HasValue && target.CampusId.HasValue && currentUser.CampusId != target.CampusId)
            return false;

        var targetStudent = await _studentProfiles.GetByUserIdAsync(target.Id, ct);
        if (targetStudent is null)
            return true;

        if (!currentUser.DepartmentId.HasValue)
            return true;

        return targetStudent.DepartmentId == currentUser.DepartmentId.Value;
    }

    private static bool Contains(string? source, string term)
        => !string.IsNullOrWhiteSpace(source) && source.Contains(term, StringComparison.OrdinalIgnoreCase);
}