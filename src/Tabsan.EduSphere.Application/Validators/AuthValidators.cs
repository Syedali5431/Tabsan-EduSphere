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
            .Must(BeSafePassword)
            .WithMessage(PasswordRulesMessage);

        RuleFor(x => x)
            .Must(x => !string.Equals(x.CurrentPassword, x.NewPassword, StringComparison.Ordinal))
            .WithMessage("New password must be different from old password.");
    }

    private static bool BeSafePassword(string password) => PasswordPolicyRules.BeSafePassword(password);

    private const string PasswordRulesMessage = PasswordPolicyRules.PasswordRulesMessage;
}

/// <summary>Validates <see cref="ForceChangePasswordRequest"/>.</summary>
public sealed class ForceChangePasswordRequestValidator : AbstractValidator<ForceChangePasswordRequest>
{
    public ForceChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Old password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .Must(BeSafePassword)
            .WithMessage(PasswordRulesMessage);

        RuleFor(x => x)
            .Must(x => !string.Equals(x.CurrentPassword, x.NewPassword, StringComparison.Ordinal))
            .WithMessage("New password must be different from old password.");
    }

    private static bool BeSafePassword(string password) => PasswordPolicyRules.BeSafePassword(password);

    private const string PasswordRulesMessage = PasswordPolicyRules.PasswordRulesMessage;
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
            .Must(BeSafePassword)
            .WithMessage(PasswordRulesMessage);
    }

    private static bool BeSafePassword(string password) => PasswordPolicyRules.BeSafePassword(password);

    private const string PasswordRulesMessage = PasswordPolicyRules.PasswordRulesMessage;
}

internal static class PasswordPolicyRules
{
    public const string PasswordRulesMessage = "Password policy: 12-16 characters, include uppercase, lowercase, number, and symbol (! @ # $ % ^ & *), and avoid simple patterns like 123456, password, qwerty.";

    private static readonly string[] DisallowedPatterns =
    [
        "123456",
        "password",
        "qwerty",
        "abc123",
        "111111",
        "000000",
        "letmein",
        "welcome"
    ];

    public static bool BeSafePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        if (password.Length < 12 || password.Length > 16) return false;
        if (!password.Any(char.IsUpper)) return false;
        if (!password.Any(char.IsLower)) return false;
        if (!password.Any(char.IsDigit)) return false;
        if (!password.Any(c => "!@#$%^&*".Contains(c))) return false;

        var normalized = password.ToLowerInvariant();
        return !DisallowedPatterns.Any(normalized.Contains);
    }
}
