using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Lti;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lti;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Plan M5.1 — LTI Dynamic Registration 1.0: start (token + URL), platform configuration, the tool's registration.</summary>
public class LtiDynamicRegistrationTests
{
    private const string Platform = "https://platform.test";

    private sealed class Harness
    {
        public readonly List<object> Added = [];
        public readonly List<AuditLog> Audit = [];
        public readonly LtiDynamicRegistrationService Service;

        public Harness(string? platformUrl = Platform)
        {
            var repo = SsoSignInTests.Proxy<ILtiRepository>.Create(new() { ["Add"] = a => { Added.Add(a[0]!); return null; } });
            var audit = SsoSignInTests.Proxy<IAuditService>.Create(new() { ["LogAsync"] = a => { Audit.Add((AuditLog)a[0]!); return Task.CompletedTask; } });
            Service = new LtiDynamicRegistrationService(repo, audit,
                new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
                Options.Create(new LtiOptions { PlatformUrl = platformUrl }));
        }

        public LtiTool Tool => Added.OfType<LtiTool>().Single();

        public async Task<string> TokenAsync()
        {
            var start = await Service.StartAsync("https://tool.example.com/lti/register", Guid.NewGuid());
            return QueryHelpers.ParseQuery(new Uri(start.Value!.Url).Query)["registration_token"].ToString();
        }
    }

    /// <summary>A registration like the 1EdTech specification's example.</summary>
    private static JsonObject Registration(Action<JsonObject>? change = null)
    {
        var json = JsonNode.Parse("""
        {
          "application_type": "web",
          "response_types": ["id_token"],
          "grant_types": ["implicit", "client_credentials"],
          "initiate_login_uri": "https://tool.example.com/lti/login",
          "redirect_uris": ["https://tool.example.com/lti/launch", "https://tool.example.com/lti/deep"],
          "client_name": "Example Tool",
          "jwks_uri": "https://tool.example.com/.well-known/jwks.json",
          "logo_uri": "https://tool.example.com/logo.png",
          "token_endpoint_auth_method": "private_key_jwt",
          "scope": "https://purl.imsglobal.org/spec/lti-ags/scope/score https://purl.imsglobal.org/spec/lti-ags/scope/lineitem https://purl.imsglobal.org/spec/lti-nrps/scope/contextmembership.readonly https://example.com/not-supported",
          "https://purl.imsglobal.org/spec/lti-tool-configuration": {
            "domain": "tool.example.com",
            "description": "Interactive practice questions",
            "target_link_uri": "https://tool.example.com/lti/launch",
            "custom_parameters": { "course": "$CourseSection.sourcedId" },
            "claims": ["iss", "sub", "name", "given_name", "family_name", "email"],
            "messages": [ { "type": "LtiDeepLinkingRequest", "target_link_uri": "https://tool.example.com/lti/deep", "label": "Add content" } ]
          }
        }
        """)!.AsObject();
        change?.Invoke(json);
        return json;
    }

    [Fact]
    public async Task Start_OpensTheToolWithThePlatformConfiguration_AndAOneTimeToken()
    {
        var h = new Harness();
        var start = await h.Service.StartAsync("https://tool.example.com/register?tenant=uni", Guid.NewGuid());

        Assert.True(start.Ok);
        var query = QueryHelpers.ParseQuery(new Uri(start.Value!.Url).Query);
        Assert.Equal("uni", query["tenant"]);
        Assert.Equal(Platform + "/lti/openid-configuration", query["openid_configuration"]);
        Assert.True(query["registration_token"].ToString().Length >= 40);
        Assert.Equal("https://tool.example.com", start.Value.ToolOrigin);
        Assert.Contains(h.Audit, a => a.Action == "LtiDynamicRegistrationStarted");

        Assert.Equal(400, (await h.Service.StartAsync("http://tool.example.com/register", Guid.NewGuid())).Status);
        Assert.Equal(400, (await new Harness(platformUrl: null).Service.StartAsync("https://tool.example.com/register", Guid.NewGuid())).Status);
    }

    [Fact]
    public void OpenIdConfiguration_DescribesThePlatformForTheTool()
    {
        using var config = JsonDocument.Parse(new Harness().Service.OpenIdConfigurationJson());
        var root = config.RootElement;
        Assert.Equal(Platform, root.GetProperty("issuer").GetString());
        Assert.Equal(Platform + "/lti/authorize", root.GetProperty("authorization_endpoint").GetString());
        Assert.Equal(Platform + "/lti/register", root.GetProperty("registration_endpoint").GetString());
        Assert.Equal(Platform + "/lti/jwks", root.GetProperty("jwks_uri").GetString());
        Assert.Equal(Platform + "/lti/token", root.GetProperty("token_endpoint").GetString());
        Assert.Contains("private_key_jwt", root.GetProperty("token_endpoint_auth_methods_supported").EnumerateArray().Select(e => e.GetString()));
        var scopes = root.GetProperty("scopes_supported").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains(LtiConstants.ScopeScore, scopes);
        Assert.Contains(LtiConstants.ScopeMemberships, scopes);
        var platform = root.GetProperty(LtiDynamicRegistrationService.PlatformConfigurationClaim);
        Assert.Equal("tabsan-edusphere", platform.GetProperty("product_family_code").GetString());
        Assert.Equal(new[] { "LtiResourceLinkRequest", "LtiDeepLinkingRequest" },
            platform.GetProperty("messages_supported").EnumerateArray().Select(m => m.GetProperty("type").GetString()));
    }

    [Fact]
    public async Task Register_CreatesASwitchedOffTool_AndAnswersWithClientAndDeploymentIds()
    {
        var h = new Harness();
        var result = await h.Service.RegisterAsync("Bearer " + await h.TokenAsync(), Registration().ToJsonString());

        Assert.True(result.Ok, result.Error);
        var tool = h.Tool;
        Assert.Equal("Example Tool", tool.Name);
        Assert.False(tool.IsActive);
        Assert.NotNull(tool.RegisteredDynamicallyAt);
        Assert.Equal(LtiPrivacyLevel.Public, tool.PrivacyLevel);
        Assert.True(tool.AllowGradeService);
        Assert.True(tool.AllowRosterService);
        Assert.Equal("https://tool.example.com/lti/deep", tool.DeepLinkingUrl);
        Assert.Equal("https://tool.example.com/.well-known/jwks.json", tool.JwksUrl);
        Assert.Equal("course=$CourseSection.sourcedId", tool.CustomParameters);
        Assert.Equal(2, tool.RedirectUriList.Count);
        var deployment = h.Added.OfType<LtiToolDeployment>().Single();
        Assert.Equal(LtiDeploymentScope.Installation, deployment.Scope);

        using var answer = JsonDocument.Parse(result.Value!);
        Assert.Equal(tool.ClientId, answer.RootElement.GetProperty("client_id").GetString());
        Assert.Equal(deployment.DeploymentId, answer.RootElement.GetProperty(LtiDynamicRegistrationService.ToolConfigurationClaim).GetProperty("deployment_id").GetString());
        var granted = answer.RootElement.GetProperty("scope").GetString()!.Split(' ');
        Assert.Equal(3, granted.Length);
        Assert.DoesNotContain("https://example.com/not-supported", granted);
        Assert.Contains(h.Audit, a => a.Action == "LtiToolRegisteredDynamically");
    }

    [Fact]
    public async Task Register_PrivacyAndServicesFollowWhatTheToolAsksFor()
    {
        var h = new Harness();
        var result = await h.Service.RegisterAsync("Bearer " + await h.TokenAsync(), Registration(r =>
        {
            r.Remove("scope");
            r[LtiDynamicRegistrationService.ToolConfigurationClaim]!["claims"] = new JsonArray("iss", "sub");
            r[LtiDynamicRegistrationService.ToolConfigurationClaim]!.AsObject().Remove("messages");
        }).ToJsonString());

        Assert.True(result.Ok, result.Error);
        Assert.Equal(LtiPrivacyLevel.Anonymous, h.Tool.PrivacyLevel);
        Assert.False(h.Tool.AllowGradeService);
        Assert.False(h.Tool.AllowRosterService);
        Assert.Null(h.Tool.DeepLinkingUrl);
    }

    [Fact]
    public async Task Register_NeedsAValidSingleUseToken()
    {
        var h = new Harness();
        var token = await h.TokenAsync();
        Assert.Equal(401, (await h.Service.RegisterAsync(null, Registration().ToJsonString())).Status);
        Assert.Equal(401, (await h.Service.RegisterAsync("Bearer made-up", Registration().ToJsonString())).Status);
        Assert.True((await h.Service.RegisterAsync("Bearer " + token, Registration().ToJsonString())).Ok);
        var again = await h.Service.RegisterAsync("Bearer " + token, Registration().ToJsonString());
        Assert.Equal(401, again.Status);
        Assert.StartsWith("invalid_token:", again.Error);
    }

    public static TheoryData<string, string> InvalidRegistrations => new()
    {
        { "no-tool-configuration", "invalid_client_metadata" },
        { "http-redirect", "invalid_redirect_uri" },
        { "no-client-credentials", "invalid_client_metadata" },
        { "client-secret", "invalid_client_metadata" },
        { "no-jwks", "invalid_client_metadata" },
        { "no-domain", "invalid_client_metadata" },
        { "no-name", "invalid_client_metadata" },
        { "native", "invalid_client_metadata" },
        { "not-json", "invalid_client_metadata" }
    };

    [Theory]
    [MemberData(nameof(InvalidRegistrations))]
    public async Task Register_RefusesInvalidRegistrations(string problem, string code)
    {
        var h = new Harness();
        var body = problem == "not-json" ? "[1,2]" : Registration(r =>
        {
            var tool = r[LtiDynamicRegistrationService.ToolConfigurationClaim]!.AsObject();
            switch (problem)
            {
                case "no-tool-configuration": r.Remove(LtiDynamicRegistrationService.ToolConfigurationClaim); break;
                case "http-redirect": r["redirect_uris"] = new JsonArray("http://tool.example.com/launch"); break;
                case "no-client-credentials": r["grant_types"] = new JsonArray("implicit"); break;
                case "client-secret": r["token_endpoint_auth_method"] = "client_secret_basic"; break;
                case "no-jwks": r.Remove("jwks_uri"); break;
                case "no-domain": tool.Remove("domain"); break;
                case "no-name": r.Remove("client_name"); break;
                case "native": r["application_type"] = "native"; break;
            }
        }).ToJsonString();

        var result = await h.Service.RegisterAsync("Bearer " + await h.TokenAsync(), body);

        Assert.Equal(400, result.Status);
        Assert.StartsWith(code + ":", result.Error);
        Assert.Empty(h.Added);
    }
}
