using FluentAssertions;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Plan M1 (deferred item, 2026-10-24): institution-ID and role attribute mapping rules on a provider setting.</summary>
public class SsoAttributeMappingTests
{
    private static SsoProviderSetting Saml() => new(null, SsoProvider.Saml, "Institution");

    [Fact]
    public void RoleMappings_AreNormalised_AndMapAssertedValues()
    {
        var s = Saml();
        s.SetAttributeMapping(" employeeNumber ", linkById: true, "roles", "student = student ;FACULTY=faculty|admin\nstaff=Finance, faculty=Admin", requireRoleMatch: true);

        s.IdClaim.Should().Be("employeeNumber");
        s.RoleMappings.Should().Be("student=Student; FACULTY=Faculty|Admin; staff=Finance");
        s.MapRoles(["Student"]).Should().Equal("Student");
        s.MapRoles(["faculty@uni.edu", "member@uni.edu"]).Should().Equal("Faculty", "Admin");
        s.MapRoles(["alumni", "", "  "]).Should().BeEmpty();
        s.GetRoleMappings().Should().ContainKey("Faculty");
    }

    [Theory]
    [InlineData("student", "not a role mapping")]
    [InlineData("=Student", "not a role mapping")]
    [InlineData("student=", "not a role mapping")]
    [InlineData("admin=SuperAdmin", "not a role that can be mapped")]
    [InlineData("x=Teacher", "not a role that can be mapped")]
    public void InvalidRoleMappings_AreRefusedWithAMessage(string mapping, string message)
    {
        var act = () => Saml().SetAttributeMapping(null, false, "roles", mapping, false);
        act.Should().Throw<ArgumentException>().WithMessage("*" + message + "*");
    }

    [Fact]
    public void Switches_NeedTheirAttributes()
    {
        var s = Saml();
        s.Invoking(x => x.SetAttributeMapping(null, linkById: true, null, null, false)).Should().Throw<ArgumentException>();
        s.Invoking(x => x.SetAttributeMapping(null, false, "roles", null, requireRoleMatch: true)).Should().Throw<ArgumentException>();
        s.Invoking(x => x.SetAttributeMapping(null, false, null, "student=Student", requireRoleMatch: true)).Should().Throw<ArgumentException>();
        s.Invoking(x => x.SetAttributeMapping(new string('a', 257), false, null, null, false)).Should().Throw<ArgumentException>();

        s.SetAttributeMapping("", false, " ", "", false);
        (s.IdClaim, s.RoleClaim, s.RoleMappings).Should().Be((null, null, null));
    }

    [Fact]
    public void SamlSecurity_IsOnlyForSamlProviders()
    {
        var oidc = new SsoProviderSetting(null, SsoProvider.Google, "Google");
        oidc.Invoking(x => x.SetSamlSecurity(true, false)).Should().Throw<InvalidOperationException>();
        oidc.SetSamlSecurity(false, false);

        var saml = Saml();
        saml.SetSamlSecurity(true, true);
        (saml.SignAuthnRequests, saml.RequireEncryptedAssertions).Should().Be((true, true));
    }

    [Fact]
    public void ExternalIdentity_ValuesAreLookedUpCaseInsensitively()
    {
        var identity = new ExternalIdentity("s", null, false, null, false,
            new Dictionary<string, string[]> { ["Roles"] = ["a", "b"] });
        identity.Values("roles").Should().Equal("a", "b");
        identity.Values(null).Should().BeEmpty();
        new ExternalIdentity("s", null, false, null, false).Values("roles").Should().BeEmpty();
    }
}
