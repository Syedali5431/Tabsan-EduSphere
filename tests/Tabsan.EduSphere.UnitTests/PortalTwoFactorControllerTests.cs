using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Tabsan.EduSphere.Application.DTOs.TwoFactor;
using Tabsan.EduSphere.Web.Controllers;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;
using Xunit;

namespace Tabsan.EduSphere.UnitTests;

public class PortalTwoFactorControllerTests
{
    [Fact]
    public void TwoFactorSettings_ReturnsCurrentUserAndConnectionState()
    {
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000123");
        var (api, _) = CreateApiClient(isConnected: true, userId: userId);
        var sut = CreateSut(api, userId);

        var result = sut.TwoFactorSettings();

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<TwoFactorSettingsPageModel>().Subject;
        model.IsConnected.Should().BeTrue();
        model.CurrentUserId.Should().Be(Guid.Parse("00000000-0000-0000-0000-000000000123"));
        view.ViewName.Should().BeNull();
    }

    [Fact]
    public async Task BeginTwoFactorSetup_Connected_ReturnsSetupViewWithPayload()
    {
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000123");
        var (api, proxy) = CreateApiClient(
            isConnected: true,
            userId: userId,
            setupResponse: new TwoFactorSetupApiModel(
                false,
                "Tabsan EduSphere",
                "student1@tabsan.local",
                "ABCDEF123456",
                "otpauth://totp/Tabsan EduSphere:student1@tabsan.local?secret=ABCDEF123456",
                "data:image/png;base64,AAAA"));
        var sut = CreateSut(api, userId);

        var result = await sut.BeginTwoFactorSetup(CancellationToken.None);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<TwoFactorSettingsPageModel>().Subject;
        model.Message.Should().Be("2FA setup started. Scan the QR code or enter the manual key in your authenticator app.");
        model.TwoFactorEnabled.Should().BeFalse();
        model.Issuer.Should().Be("Tabsan EduSphere");
        model.ManualKey.Should().Be("ABCDEF123456");
        proxy.BeginSetupCalls.Should().Be(1);
    }

    [Fact]
    public async Task BeginTwoFactorSetup_Disconnected_RedirectsToLogin()
    {
        var (api, _) = CreateApiClient(isConnected: false);
        var sut = CreateSut(api, null);

        var result = await sut.BeginTwoFactorSetup(CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
    }

    private static PortalController CreateSut(IEduApiClient api, Guid? userId)
    {
        var sut = new PortalController(api, new TestWebHostEnvironment());
        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreatePrincipal(userId)
            }
        };

        return sut;
    }

    private static (IEduApiClient Client, TestEduApiClientProxy Proxy) CreateApiClient(
        bool isConnected,
        Guid? userId = null,
        TwoFactorSetupApiModel? setupResponse = null)
    {
        var api = DispatchProxy.Create<IEduApiClient, TestEduApiClientProxy>();
        var proxy = (TestEduApiClientProxy)(object)api;
        proxy.IsConnectedValue = isConnected;
        proxy.UserId = userId;
        proxy.SetupResponse = setupResponse;
        return (api, proxy);
    }

    private static System.Security.Claims.ClaimsPrincipal CreatePrincipal(Guid? userId)
    {
        var claims = new List<System.Security.Claims.Claim>();
        if (userId.HasValue)
        {
            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }

        var identity = new System.Security.Claims.ClaimsIdentity(claims, authenticationType: "TestAuth");
        return new System.Security.Claims.ClaimsPrincipal(identity);
    }
}

public class TestEduApiClientProxy : DispatchProxy
{
    public bool IsConnectedValue { get; set; }
    public Guid? UserId { get; set; }
    public TwoFactorSetupApiModel? SetupResponse { get; set; }
    public int BeginSetupCalls { get; private set; }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod is null)
            throw new NotSupportedException();

        return targetMethod.Name switch
        {
            nameof(IEduApiClient.IsConnected) => IsConnectedValue,
            nameof(IEduApiClient.BeginTwoFactorSetupAsync) => HandleBeginSetupAsync(),
            _ => throw new NotSupportedException($"Method '{targetMethod.Name}' is not configured for this test proxy.")
        };
    }

    private Task<TwoFactorSetupApiModel?> HandleBeginSetupAsync()
    {
        BeginSetupCalls++;
        return Task.FromResult(SetupResponse);
    }
}

file sealed class TestWebHostEnvironment : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "Tabsan.EduSphere.Web.Tests";
    public string WebRootPath { get; set; } = string.Empty;
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
