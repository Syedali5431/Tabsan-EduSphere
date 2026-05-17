using FluentValidation;
using Tabsan.EduSphere.Application.DTOs.Auth;
using Tabsan.EduSphere.Application.Dtos;

namespace Tabsan.EduSphere.Application.Validators;

/// <summary>Validates <see cref="LoginRequest"/> before it reaches <c>IAuthService</c>.</summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(100).WithMessage("Username must not exceed 100 characters.")
            .Matches(@"^[^<>""';&\/\\]+$")
            .WithMessage("Username contains invalid characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.");

        RuleFor(x => x.MfaCode)
            .MaximumLength(16).WithMessage("MFA code must not exceed 16 characters.")
            .Matches("^[A-Za-z0-9-]*$")
            .When(x => !string.IsNullOrWhiteSpace(x.MfaCode))
            .WithMessage("MFA code can contain letters, digits, and dashes only.");
    }
}

/// <summary>Validates <see cref="ChangePasswordRequest"/>.</summary>
public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[!@#$%^&*()_\-+=\[\]{}|;:',.<>?/\\]")
            .WithMessage("Password must contain at least one special character.");
    }
}

/// <summary>Validates <see cref="AdminResetPasswordRequest"/>.</summary>
public sealed class AdminResetPasswordRequestValidator : AbstractValidator<AdminResetPasswordRequest>
{
    public AdminResetPasswordRequestValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("Target user ID is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[!@#$%^&*()_\-+=\[\]{}|;:',.<>?/\\]")
            .WithMessage("Password must contain at least one special character.");
    }
}
