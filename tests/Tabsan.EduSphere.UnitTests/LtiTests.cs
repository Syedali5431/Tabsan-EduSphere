using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Tabsan.EduSphere.Application.Lti;
using Tabsan.EduSphere.Domain.Lti;
using Tabsan.EduSphere.Infrastructure.Lti;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Plan M5 — LTI 1.3 platform rules: roles, custom parameters, tool validation, platform keys, launches and tokens.</summary>
public class LtiTests
{
    private static SaveLtiToolRequest Tool(string? jwks = "https://tool.test/jwks", string? pem = null, string privacy = "Anonymous",
        string login = "https://tool.test/login", IReadOnlyList<string>? redirects = null)
        => new("Tool", null, login, "https://tool.test/launch", null, redirects ?? ["https://tool.test/launch"], jwks, pem, null, privacy, true, false);

    [Theory]
    [InlineData("Student", LtiConstants.RoleLearner)]
    [InlineData("Faculty", LtiConstants.RoleInstructor)]
    [InlineData("Admin", LtiConstants.RoleAdministrator)]
    [InlineData("SuperAdmin", LtiConstants.SystemAdministrator)]
    public void RolesFor_MapsEduSphereRolesToLisRoles(string role, string expected)
        => Assert.Contains(expected, LtiConstants.RolesFor(role, false));

    [Fact]
    public void RolesFor_OtherStaffAreInstructorsOnlyWhenTeachingTheCourse()
    {
        Assert.Contains(LtiConstants.RoleInstructor, LtiConstants.RolesFor("Finance", true));
        Assert.DoesNotContain(LtiConstants.RoleInstructor, LtiConstants.RolesFor("Finance", false));
        Assert.DoesNotContain(LtiConstants.RoleLearner, LtiConstants.RolesFor("Faculty", true));
    }

    [Fact]
    public void ParseCustom_KeepsWellFormedPairsOnly()
    {
        var custom = LtiLaunchService.ParseCustom("chapter = 3\r\nbad key=x\n=novalue\nnoequals\nlevel=a=b\nchapter=4");
        Assert.Equal("4", custom["chapter"]); // the last value wins
        Assert.Equal("a=b", custom["level"]);
        Assert.Equal(2, custom.Count);
        Assert.Empty(LtiLaunchService.ParseCustom(null));
    }

    [Theory]
    [InlineData("https://tool.test/login", true)]
    [InlineData("http://localhost:8000/login", true)]
    [InlineData("http://127.0.0.1/login", true)]
    [InlineData("http://tool.test/login", false)]
    [InlineData("https://user:secret@tool.test/login", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("/relative", false)]
    [InlineData(null, false)]
    public void SafeUrl_AllowsHttpsAndLoopbackOnly(string? url, bool ok)
        => Assert.Equal(ok, LtiAdminService.SafeUrl(url));

    [Fact]
    public void Validate_RequiresHttpsUrlsKeysAndAKnownPrivacyLevel()
    {
        Assert.Null(LtiAdminService.Validate(Tool()));
        Assert.Contains("login initiation", LtiAdminService.Validate(Tool(login: "http://tool.test/login")));
        Assert.Contains("redirect", LtiAdminService.Validate(Tool(redirects: [])));
        Assert.Contains("redirect", LtiAdminService.Validate(Tool(redirects: ["http://evil.test/cb"])));
        Assert.Contains("JWKS URL or its public key", LtiAdminService.Validate(Tool(jwks: null)));
        Assert.Contains("Privacy", LtiAdminService.Validate(Tool(privacy: "Everything")));
        Assert.Contains("not a valid PEM", LtiAdminService.Validate(Tool(jwks: null, pem: "-----BEGIN PUBLIC KEY-----\nnope\n-----END PUBLIC KEY-----")));

        using var weak = RSA.Create(1024);
        Assert.Contains("2048", LtiAdminService.Validate(Tool(jwks: null, pem: weak.ExportSubjectPublicKeyInfoPem())));
        using var strong = RSA.Create(2048);
        Assert.Null(LtiAdminService.Validate(Tool(jwks: null, pem: strong.ExportSubjectPublicKeyInfoPem())));
    }

    private static string TempKeyFile() => Path.Combine(Path.GetTempPath(), "edusphere-lti-tests", Guid.NewGuid().ToString("N"), "keys.json");

    private static LtiPlatformKeyStore Store(string file, int retiredKeyDays = 7)
        => new(Options.Create(new LtiOptions { PlatformUrl = "https://platform.test", KeyFile = file, RetiredKeyDays = retiredKeyDays }));

    private static async Task<TokenValidationResult> VerifyAsync(string jwt, string jwksJson)
        => await new JsonWebTokenHandler().ValidateTokenAsync(jwt, new TokenValidationParameters
        {
            IssuerSigningKeys = new JsonWebKeySet(jwksJson).GetSigningKeys(),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
        });

    [Fact]
    public async Task PlatformKeys_SignTokensThatVerifyAgainstThePublishedJwks()
    {
        var file = TempKeyFile();
        var store = Store(file);
        var jwt = store.Sign(JsonSerializer.Serialize(new { iss = "https://platform.test", sub = "u1" }));

        var result = await VerifyAsync(jwt, store.JwksJson());
        Assert.True(result.IsValid, result.Exception?.Message);
        Assert.Equal(store.Keys.Single(k => k.Active).Kid, ((JsonWebToken)result.SecurityToken).Kid);

        // The key file holds private material only on disk; the JWKS publishes public parts only.
        Assert.True(File.Exists(file));
        Assert.DoesNotContain("\"d\"", store.JwksJson());
    }

    [Fact]
    public async Task PlatformKeys_RotationKeepsTheOldKeyPublishedAndPersists()
    {
        var file = TempKeyFile();
        var store = Store(file);
        var before = store.Sign("{\"n\":1}");
        var oldKid = store.Keys.Single().Kid;

        store.Rotate();
        var after = store.Sign("{\"n\":2}");
        Assert.Equal(2, store.Keys.Count);
        Assert.NotEqual(oldKid, store.Keys.Single(k => k.Active).Kid);
        Assert.True((await VerifyAsync(before, store.JwksJson())).IsValid); // still verifies during the grace period
        Assert.True((await VerifyAsync(after, store.JwksJson())).IsValid);

        // Another node reading the same file signs with the same active key.
        var other = Store(file);
        Assert.Equal(store.Keys.Single(k => k.Active).Kid, other.Keys.Single(k => k.Active).Kid);
    }

    [Fact]
    public async Task PlatformKeys_RetiredKeysLeaveTheJwksAfterTheGracePeriod()
    {
        var store = Store(TempKeyFile(), retiredKeyDays: 0);
        var before = store.Sign("{}");
        store.Rotate();
        await Task.Delay(20);
        Assert.False((await VerifyAsync(before, store.JwksJson())).IsValid);
        Assert.Single(JsonDocument.Parse(store.JwksJson()).RootElement.GetProperty("keys").EnumerateArray());
    }

    [Fact]
    public void Launch_IsSingleUseAndShortLived()
    {
        var now = DateTime.UtcNow;
        var launch = new LtiLaunch(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), LtiConstants.DeepLinkingRequest, null, Guid.NewGuid(), null, null, now.AddMinutes(5));
        Assert.True(launch.CanAuthorize(now));
        Assert.False(launch.CanAuthorize(now.AddMinutes(6)));
        Assert.False(launch.CanComplete(now)); // a selection can only follow an authorization

        launch.MarkAuthorized(now);
        Assert.False(launch.CanAuthorize(now)); // the id_token is issued once
        Assert.True(launch.CanComplete(now.AddMinutes(30)));
        Assert.False(launch.CanComplete(now.AddHours(2)));

        launch.MarkCompleted(now);
        Assert.False(launch.CanComplete(now)); // the selection is accepted once
    }

    [Fact]
    public void Deployments_OutsideTheInstallationNeedTheirScope()
    {
        var tool = new LtiTool("Tool", Guid.NewGuid());
        Assert.Throws<ArgumentException>(() => tool.AddDeployment(LtiDeploymentScope.Department, null));
        var whole = tool.AddDeployment(LtiDeploymentScope.Installation, Guid.NewGuid());
        Assert.Null(whole.ScopeId);
        Assert.StartsWith("edusphere-", tool.ClientId);
        Assert.NotEqual(whole.DeploymentId, tool.AddDeployment(LtiDeploymentScope.Installation, null).DeploymentId);
    }

    [Fact]
    public void AccessTokens_StoreOnlyAHashAndCheckScopes()
    {
        var hash = LtiTokenService.Hash("secret-token");
        Assert.Equal(64, hash.Length);
        Assert.Equal(hash, LtiTokenService.Hash("secret-token"));
        Assert.Equal(hash.ToLowerInvariant(), hash);
        Assert.NotEqual(hash, LtiTokenService.Hash("secret-token2"));

        var token = new LtiAccessToken(Guid.NewGuid(), hash, [LtiConstants.ScopeScore, LtiConstants.ScopeLineItemReadOnly], DateTime.UtcNow.AddHours(1));
        Assert.True(token.Allows(LtiConstants.ScopeScore));
        Assert.False(token.Allows(LtiConstants.ScopeLineItem)); // read-only is not read-write
        Assert.False(token.Allows(LtiConstants.ScopeMemberships));
    }

    [Fact]
    public void UnverifiedIssuer_ReadsIssWithoutTrustingIt()
    {
        static string Part(object o) => Base64UrlEncoder.Encode(JsonSerializer.Serialize(o));
        Assert.Equal("edusphere-abc", LtiLaunchService.UnverifiedIssuer($"{Part(new { alg = "RS256" })}.{Part(new { iss = "edusphere-abc" })}.sig"));
        Assert.Null(LtiLaunchService.UnverifiedIssuer("not-a-jwt"));
        Assert.Null(LtiLaunchService.UnverifiedIssuer("a.%%%.c"));
        Assert.Null(LtiLaunchService.UnverifiedIssuer($"{Part(new { })}.{Part(new { iss = 5 })}.s"));
    }
}
