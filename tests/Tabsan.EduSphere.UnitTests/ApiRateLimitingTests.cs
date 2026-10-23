using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.API.Services;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Plan M14.2 — which allowance a request counts against: its API token, its user, or its address (exempt addresses and ranges).</summary>
public class ApiRateLimitingTests
{
    private static HttpContext Context(string ip, params Claim[] claims)
    {
        var services = new ServiceCollection();
        services.Configure<ApiRateLimitOptions>(o => o.ExemptIps = ["127.0.0.1", "::1", "10.0.0.0/8"]);
        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        if (claims.Length > 0) context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        return context;
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("::ffff:10.20.30.40", true)]
    [InlineData("10.255.0.1", true)]
    [InlineData("11.0.0.1", false)]
    [InlineData("192.168.1.5", false)]
    public void AnonymousRequests_AreLimitedPerAddress_UnlessTheAddressOrItsRangeIsExempt(string ip, bool exempt)
        => Assert.Equal(exempt, ApiRateLimiting.Partition(Context(ip)).PartitionKey == "exempt");

    [Fact]
    public void SignedInRequests_CountPerUser_AndTokenRequestsPerToken_EvenFromExemptAddresses()
    {
        var user = ApiRateLimiting.Partition(Context("127.0.0.1", new Claim(ClaimTypes.NameIdentifier, "u1")));
        Assert.StartsWith("user:u1:", user.PartitionKey);
        var token = ApiRateLimiting.Partition(Context("10.0.0.1", new Claim(ClaimTypes.NameIdentifier, "u1"), new Claim(ApiRateLimiting.TokenIdClaim, "t1")));
        Assert.StartsWith("token:t1:", token.PartitionKey);
        Assert.StartsWith("ip:192.168.1.5:", ApiRateLimiting.Partition(Context("192.168.1.5")).PartitionKey);
    }
}
