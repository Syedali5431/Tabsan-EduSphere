using FluentAssertions;
using Tabsan.EduSphere.Application.Modules;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Infrastructure.Modules;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Plan M1 Stage M1.1: SSO data model and module registration.</summary>
public class SsoFoundationTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static SsoProviderSetting CompleteMicrosoft()
    {
        var setting = new SsoProviderSetting(TenantId, SsoProvider.Microsoft, "Sign in with Microsoft");
        setting.ConfigureOidc("client-123", "https://login.microsoftonline.com/contoso/v2.0");
        setting.SetEncryptedClientSecret("ciphertext==");
        return setting;
    }

    // ── SsoProviderSetting ───────────────────────────────────────────────────

    [Fact]
    public void NewSetting_StartsDisabledWithSafeDefaults()
    {
        var setting = new SsoProviderSetting(TenantId, SsoProvider.Microsoft, "  Microsoft  ");

        setting.DisplayName.Should().Be("Microsoft");
        setting.IsEnabled.Should().BeFalse();
        setting.AutoLinkByEmail.Should().BeFalse();
        setting.EnforceSsoOnly.Should().BeFalse();
        setting.TrustIdpMfa.Should().BeFalse();
        setting.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void Enable_WhenIncomplete_Throws()
    {
        var setting = new SsoProviderSetting(TenantId, SsoProvider.Microsoft, "Microsoft");
        setting.ConfigureOidc("client-123", "https://login.microsoftonline.com/contoso/v2.0");

        var act = () => setting.Enable();

        act.Should().Throw<InvalidOperationException>();
        setting.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Enable_WhenOidcComplete_Succeeds()
    {
        var setting = CompleteMicrosoft();

        setting.Enable();

        setting.IsComplete.Should().BeTrue();
        setting.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void ClearingClientSecret_DisablesProvider()
    {
        var setting = CompleteMicrosoft();
        setting.Enable();

        setting.SetEncryptedClientSecret(null);

        setting.IsEnabled.Should().BeFalse();
        setting.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void Google_AlwaysUsesGoogleIssuer()
    {
        var setting = new SsoProviderSetting(TenantId, SsoProvider.Google, "Google");
        setting.Authority.Should().Be(SsoProviderSetting.GoogleAuthority);

        setting.ConfigureOidc("google-client", "https://evil.example.com");

        setting.Authority.Should().Be(SsoProviderSetting.GoogleAuthority);
    }

    [Theory]
    [InlineData("http://login.example.com")]
    [InlineData("not-a-url")]
    [InlineData("")]
    public void ConfigureOidc_RejectsNonHttpsAuthority(string authority)
    {
        var setting = new SsoProviderSetting(TenantId, SsoProvider.GenericOidc, "Okta");

        var act = () => setting.ConfigureOidc("client", authority);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Saml_RequiresMetadataUrlAndRejectsOidcCalls()
    {
        var setting = new SsoProviderSetting(TenantId, SsoProvider.Saml, "University SAML");

        setting.Invoking(s => s.ConfigureOidc("client", "https://idp.uni.edu")).Should().Throw<InvalidOperationException>();
        setting.Invoking(s => s.SetEncryptedClientSecret("x")).Should().Throw<InvalidOperationException>();
        setting.Invoking(s => s.Enable()).Should().Throw<InvalidOperationException>();

        setting.ConfigureSaml("https://idp.uni.edu/metadata.xml");
        setting.Enable();

        setting.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void SetPolicy_NormalisesEmailDomains()
    {
        var setting = CompleteMicrosoft();

        setting.SetPolicy(true, true, true, ["@Uni.EDU", " staff.uni.edu ", "uni.edu", ""]);

        setting.AllowedEmailDomains.Should().Be("uni.edu,staff.uni.edu");
        setting.AutoLinkByEmail.Should().BeTrue();
        setting.EnforceSsoOnly.Should().BeTrue();
        setting.TrustIdpMfa.Should().BeTrue();
    }

    [Theory]
    [InlineData("ali@uni.edu", true)]
    [InlineData("ALI@Staff.Uni.Edu", true)]
    [InlineData("ali@gmail.com", false)]
    [InlineData("ali@evil-uni.edu", false)]
    [InlineData("no-at-sign", false)]
    [InlineData(null, false)]
    public void IsEmailDomainAllowed_MatchesExactDomainsOnly(string? email, bool expected)
    {
        var setting = CompleteMicrosoft();
        setting.SetPolicy(false, false, false, ["uni.edu", "staff.uni.edu"]);

        setting.IsEmailDomainAllowed(email).Should().Be(expected);
    }

    [Fact]
    public void IsEmailDomainAllowed_WithNoRestriction_AllowsAny()
    {
        var setting = CompleteMicrosoft();

        setting.IsEmailDomainAllowed("anyone@anywhere.com").Should().BeTrue();
    }

    // ── ExternalLogin ────────────────────────────────────────────────────────

    [Fact]
    public void ExternalLogin_NormalisesAndRecordsLogin()
    {
        var userId = Guid.NewGuid();
        var link = new ExternalLogin(TenantId, userId, SsoProvider.Google, "  sub-42 ", " Ali@Uni.EDU ");

        link.ExternalSubjectId.Should().Be("sub-42");
        link.Email.Should().Be("ali@uni.edu");
        link.LastLoginAt.Should().BeNull();

        link.RecordLogin("new@uni.edu");

        link.LastLoginAt.Should().NotBeNull();
        link.Email.Should().Be("new@uni.edu");
    }

    [Fact]
    public void ExternalLogin_RequiresUserAndSubject()
    {
        FluentActions.Invoking(() => new ExternalLogin(TenantId, Guid.Empty, SsoProvider.Google, "sub", null))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new ExternalLogin(TenantId, Guid.NewGuid(), SsoProvider.Google, " ", null))
            .Should().Throw<ArgumentException>();
    }

    // ── Module registration ──────────────────────────────────────────────────

    [Fact]
    public void SsoModule_IsRegisteredAndLicenseGated()
    {
        var descriptor = ModuleRegistry.Get(KnownModuleKeys.Sso);

        descriptor.Should().NotBeNull();
        descriptor!.IsLicenseGated.Should().BeTrue();
        KnownModuleKeys.All.Should().Contain(KnownModuleKeys.Sso);
    }
}
