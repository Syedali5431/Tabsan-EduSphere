using System.ComponentModel.DataAnnotations;

namespace Tabsan.EduSphere.Application.DTOs.Auth;

public sealed record UserSettingsUserDto(
    Guid Id,
    string Username,
    string Role,
    string? Email,
    string? PhoneNumber,
    string? Address,
    bool IsActive,
    Guid? TenantId,
    Guid? CampusId,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? CourseId,
    string? CourseName);

public sealed record UpdateUserSettingsRequest(
    [property: EmailAddress]
    [property: StringLength(256)]
    string? Email,

    [property: Phone]
    [property: StringLength(32)]
    string? PhoneNumber,

    [property: StringLength(500)]
    string? Address);

public sealed record ChangeUserPasswordRequest(
    [property: Required]
    [property: StringLength(256, MinimumLength = 1)]
    string CurrentPassword,

    [property: Required]
    [property: StringLength(16, MinimumLength = 12)]
    string NewPassword,

    [property: Required]
    [property: StringLength(16, MinimumLength = 12)]
    string ConfirmNewPassword);

public sealed record ResetUserPasswordRequest(
    bool UseDefaultPassword = true);