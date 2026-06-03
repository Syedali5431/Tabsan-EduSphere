using System.ComponentModel.DataAnnotations;
using Tabsan.EduSphere.Domain.Enums;

namespace Tabsan.EduSphere.Application.DTOs.Auth;

/// <summary>Request body for creating an Admin user account.</summary>
public sealed record CreateAdminUserRequest(
	[property: Required]
	[property: StringLength(100, MinimumLength = 3)]
	[property: RegularExpression("^[a-zA-Z0-9._-]+$")]
	string Username,

	[property: EmailAddress]
	[property: StringLength(256)]
	string? Email,

	[property: Required]
	[property: StringLength(256, MinimumLength = 8)]
	string Password,

	InstitutionType? InstitutionType,

	[property: StringLength(200)]
	string? FullName = null,

	[property: StringLength(200)]
	string? FatherName = null,

	[property: Phone]
	[property: StringLength(32)]
	string? PhoneNumber = null,

	[property: StringLength(500)]
	string? Address = null);

/// <summary>Request body for updating an Admin user account.</summary>
public sealed record UpdateAdminUserRequest(
	[property: EmailAddress]
	[property: StringLength(256)]
	string? Email,

	bool IsActive,

	[property: StringLength(256, MinimumLength = 8)]
	string? NewPassword,

	[property: Phone]
	[property: StringLength(32)]
	string? PhoneNumber = null,

	[property: StringLength(500)]
	string? Address = null);
