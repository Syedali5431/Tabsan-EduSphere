using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.Auth;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

[ApiController]
[Route("api/v1/admin-user")]
[Authorize(Roles = "SuperAdmin")]
public class AdminUserController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IInstitutionPolicyService _institutionPolicyService;

    public AdminUserController(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IInstitutionPolicyService institutionPolicyService)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _institutionPolicyService = institutionPolicyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await _users.GetUsersByRolesAsync(new[] { "Admin" }, includeInactive: true, ct);
        return Ok(users.Select(u => new
        {
            u.Id,
            u.Username,
            u.Email,
            u.PhoneNumber,
            u.IsActive,
            u.InstitutionType,
            Role = u.Role.Name
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdminUserRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Username and password are required.");

        var username = request.Username.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        if (await _users.UsernameExistsAsync(username, ct))
            return Conflict("Username already exists.");

        if (!string.IsNullOrWhiteSpace(email))
        {
            var existingByEmail = await _users.GetByEmailAsync(email, ct);
            if (existingByEmail is not null)
                return Conflict("Email already exists.");
        }

        var adminRole = await _users.GetRoleByNameAsync("Admin", ct);
        if (adminRole is null)
            return BadRequest("Admin role was not found.");

        if (request.InstitutionType is not null)
        {
            var policy = await _institutionPolicyService.GetPolicyAsync(ct);
            if (!policy.IsEnabled(request.InstitutionType.Value))
                return BadRequest($"Institution type '{request.InstitutionType}' is not enabled by the current license policy.");
        }

        var hash = _passwordHasher.Hash(request.Password);
        var user = new User(
            username,
            hash,
            adminRole.Id,
            email,
            departmentId: null,
            mustChangePassword: false,
            institutionType: request.InstitutionType,
            phoneNumber: phoneNumber);
        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        return Ok(new { user.Id, user.Username, user.Email, user.PhoneNumber, user.IsActive, user.InstitutionType, Role = "Admin" });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAdminUserRequest request, CancellationToken ct)
    {
        if (id == Guid.Empty)
            return BadRequest("id is required.");

        var user = await _users.GetByIdAsync(id, ct);
        if (user is null)
            return NotFound("Admin user not found.");

        if (!string.Equals(user.Role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only Admin users can be updated from this endpoint.");

        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        if (!string.IsNullOrWhiteSpace(email))
        {
            var existingByEmail = await _users.GetByEmailAsync(email, ct);
            if (existingByEmail is not null && existingByEmail.Id != user.Id)
                return Conflict("Email already exists.");
        }

        user.UpdateEmail(email);
        user.UpdatePhoneNumber(phoneNumber);

        if (request.IsActive)
            user.Activate();
        else
            user.Deactivate();

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
            user.UpdatePasswordHash(_passwordHasher.Hash(request.NewPassword));

        _users.Update(user);
        await _users.SaveChangesAsync(ct);

        return NoContent();
    }
}
