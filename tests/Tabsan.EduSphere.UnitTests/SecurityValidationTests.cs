using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Tabsan.EduSphere.Application.DTOs.Auth;

namespace Tabsan.EduSphere.UnitTests;

public class SecurityValidationTests
{
    [Fact]
    public void LoginRequest_InvalidUsernameAndShortPassword_FailsValidation()
    {
        var request = new LoginRequest("bad user!", "short");

        var results = Validate(request);

        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains("Username"));
        results.Should().Contain(r => r.MemberNames.Contains("Password"));
    }

    [Fact]
    public void LoginRequest_ValidValues_PassesValidation()
    {
        var request = new LoginRequest("student_01", "veryStrongPassword123!", "123456", "Mozilla/5.0");

        var results = Validate(request);

        results.Should().BeEmpty();
    }

    [Fact]
    public void CreateAdminUserRequest_InvalidEmail_FailsValidation()
    {
        var request = new CreateAdminUserRequest("admin.user", "not-an-email", "veryStrongPassword123!", null);

        var results = Validate(request);

        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.MemberNames.Contains("Email"));
    }

    [Fact]
    public void ForceChangePasswordRequest_EmptyPassword_FailsValidation()
    {
        var request = new ForceChangePasswordRequest("old-pass", string.Empty);

        var results = Validate(request);

        results.Should().NotBeEmpty();
    }

    private static List<ValidationResult> Validate(object instance)
    {
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }
}
