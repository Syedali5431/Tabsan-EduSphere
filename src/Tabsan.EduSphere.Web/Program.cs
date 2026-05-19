using Tabsan.EduSphere.Web.Services;
using System.Security.Claims;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Tabsan.EduSphere.Application.Services;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;
builder.Configuration.AddEduSphereConfigurationHierarchy(env);
var deploymentTopology = DeploymentTopologyResolver.Resolve(builder.Configuration, env);
var tenantIsolation = TenantIsolationResolver.Resolve(builder.Configuration, env, deploymentTopology);
StartupConfigurationFailSafeValidator.ValidateCommonStartupConfiguration(
    builder.Configuration,
    env,
    "Tabsan.EduSphere.Web",
    deploymentTopology,
    tenantIsolation);

Console.WriteLine($"[Web] Environment: {env.EnvironmentName} | App: {env.ApplicationName}");
Console.WriteLine($"[Web] Deployment profile: Mode={deploymentTopology.Mode}, Customer={deploymentTopology.CustomerCode}, Domain={deploymentTopology.CustomerDomain}, Database={deploymentTopology.CustomerDatabaseName}, Scaling={deploymentTopology.ScalingEnabled} ({deploymentTopology.MinReplicas}-{deploymentTopology.MaxReplicas})");
Console.WriteLine($"[Web] Tenant isolation: Enabled={tenantIsolation.Enabled}, Mode={tenantIsolation.Mode}, Tenant={tenantIsolation.TenantCode}, Domain={tenantIsolation.TenantDomain}, Database={tenantIsolation.TenantDatabaseName}, Strategy={tenantIsolation.IsolationStrategy}");

// Final-Touches Phase 8 Stage 8.1 — auto-scaling policy metadata and startup guardrails.
var autoScalingEnabled = builder.Configuration.GetValue("InfrastructureTuning:AutoScaling:Enabled", true);
var autoScalingMinReplicas = Math.Max(1, builder.Configuration.GetValue("InfrastructureTuning:AutoScaling:MinReplicas", 2));
var autoScalingMaxReplicas = Math.Max(autoScalingMinReplicas, builder.Configuration.GetValue("InfrastructureTuning:AutoScaling:MaxReplicas", 10));
if (autoScalingEnabled && autoScalingMinReplicas > autoScalingMaxReplicas)
{
    throw new InvalidOperationException("InfrastructureTuning:AutoScaling min replicas cannot exceed max replicas.");
}
Console.WriteLine($"[Web] Infrastructure auto-scaling policy: Enabled={autoScalingEnabled}, MinReplicas={autoScalingMinReplicas}, MaxReplicas={autoScalingMaxReplicas}");

// Final-Touches Phase 8 Stage 8.2 — host limits tuning for high-concurrency request handling.
var hostMinWorkerThreads = Math.Max(Environment.ProcessorCount * 8, builder.Configuration.GetValue("InfrastructureTuning:HostLimits:ThreadPoolMinWorkerThreads", Environment.ProcessorCount * 12));
var hostMinCompletionPortThreads = Math.Max(Environment.ProcessorCount * 8, builder.Configuration.GetValue("InfrastructureTuning:HostLimits:ThreadPoolMinCompletionPortThreads", Environment.ProcessorCount * 12));
var hostMaxConcurrentConnections = builder.Configuration.GetValue<long?>("InfrastructureTuning:HostLimits:MaxConcurrentConnections");
var hostMaxConcurrentUpgradedConnections = builder.Configuration.GetValue<long?>("InfrastructureTuning:HostLimits:MaxConcurrentUpgradedConnections");
ThreadPool.GetMinThreads(out var currentMinWorkerThreads, out var currentMinCompletionPortThreads);
if (hostMinWorkerThreads > currentMinWorkerThreads || hostMinCompletionPortThreads > currentMinCompletionPortThreads)
{
    var targetMinWorkerThreads = Math.Max(currentMinWorkerThreads, hostMinWorkerThreads);
    var targetMinCompletionPortThreads = Math.Max(currentMinCompletionPortThreads, hostMinCompletionPortThreads);
    ThreadPool.SetMinThreads(targetMinWorkerThreads, targetMinCompletionPortThreads);
    Console.WriteLine($"[Web] ThreadPool min threads tuned: Worker={targetMinWorkerThreads}, IO={targetMinCompletionPortThreads}");
}

// Final-Touches Phase 8 Stage 8.3 — network stack tuning for high connection volume and outbound saturation control.
var networkKeepAliveTimeoutSeconds = Math.Max(30, builder.Configuration.GetValue("InfrastructureTuning:NetworkStack:KeepAliveTimeoutSeconds", 120));
var networkRequestHeadersTimeoutSeconds = Math.Max(5, builder.Configuration.GetValue("InfrastructureTuning:NetworkStack:RequestHeadersTimeoutSeconds", 20));
var networkHttp2KeepAlivePingDelaySeconds = Math.Max(5, builder.Configuration.GetValue("InfrastructureTuning:NetworkStack:Http2KeepAlivePingDelaySeconds", 30));
var networkHttp2KeepAlivePingTimeoutSeconds = Math.Max(5, builder.Configuration.GetValue("InfrastructureTuning:NetworkStack:Http2KeepAlivePingTimeoutSeconds", 10));
var networkHttp2MaxStreamsPerConnection = Math.Max(100, builder.Configuration.GetValue("InfrastructureTuning:NetworkStack:Http2MaxStreamsPerConnection", 200));
var networkOutboundMaxConnectionsPerServer = Math.Max(25, builder.Configuration.GetValue("InfrastructureTuning:NetworkStack:OutboundMaxConnectionsPerServer", 256));
var networkOutboundConnectTimeoutSeconds = Math.Max(2, builder.Configuration.GetValue("InfrastructureTuning:NetworkStack:OutboundConnectTimeoutSeconds", 10));

var eduApiBaseUrl = builder.Configuration["EduApi:BaseUrl"];
if (string.IsNullOrWhiteSpace(eduApiBaseUrl))
{
    throw new InvalidOperationException("EduApi:BaseUrl is required for Tabsan.EduSphere.Web startup.");
}
StartupConfigurationFailSafeValidator.ValidateRequiredSetting(builder.Environment, "Tabsan.EduSphere.Web", "EduApi:BaseUrl", eduApiBaseUrl);
var useForwardedHeaders = builder.Configuration.GetValue<bool>("ReverseProxy:Enabled");
var configuredKnownProxies = builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [];
// Final-Touches Phase 34 Stage 4.2 — edge/static caching controls for CDN-friendly web assets.
var staticAssetCachingEnabled = builder.Configuration.GetValue("StaticAssetCaching:Enabled", true);
var staticAssetMaxAgeSeconds = Math.Max(0, builder.Configuration.GetValue("StaticAssetCaching:MaxAgeSeconds", 86400));

// Add services to the container.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});
// Final-Touches Phase 8 Stage 8.3 — stabilize outbound HTTP under high parallel fan-out.
builder.Services.ConfigureHttpClientDefaults(httpClientBuilder =>
{
    httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.SocketsHttpHandler
    {
        MaxConnectionsPerServer = networkOutboundMaxConnectionsPerServer,
        EnableMultipleHttp2Connections = true,
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        ConnectTimeout = TimeSpan.FromSeconds(networkOutboundConnectTimeoutSeconds)
    });
});
// Final-Touches Phase 34 Stage 2.3 — require shared data-protection keys in production so auth cookies work across web nodes.
var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName("Tabsan.EduSphere.Web");
var sharedKeyRingPath = builder.Configuration["ScaleOut:SharedDataProtectionKeyRingPath"];
if (!string.IsNullOrWhiteSpace(sharedKeyRingPath))
{
    Directory.CreateDirectory(sharedKeyRingPath);
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(sharedKeyRingPath));
}
else if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
{
    throw new InvalidOperationException("ScaleOut:SharedDataProtectionKeyRingPath is required outside Development/Testing to keep web auth cookies valid across instances.");
}

StartupConfigurationFailSafeValidator.ValidateRequiredSetting(builder.Environment, "Tabsan.EduSphere.Web", "ScaleOut:SharedDataProtectionKeyRingPath", sharedKeyRingPath);

// Final-Touches Phase 34 Stage 3.3 — transport tuning for keep-alive and HTTP/2-friendly connection handling.
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(networkKeepAliveTimeoutSeconds);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(networkRequestHeadersTimeoutSeconds);
    options.Limits.Http2.KeepAlivePingDelay = TimeSpan.FromSeconds(networkHttp2KeepAlivePingDelaySeconds);
    options.Limits.Http2.KeepAlivePingTimeout = TimeSpan.FromSeconds(networkHttp2KeepAlivePingTimeoutSeconds);
    options.Limits.Http2.MaxStreamsPerConnection = networkHttp2MaxStreamsPerConnection;
    if (hostMaxConcurrentConnections is > 0)
    {
        options.Limits.MaxConcurrentConnections = hostMaxConcurrentConnections;
    }

    if (hostMaxConcurrentUpgradedConnections is > 0)
    {
        options.Limits.MaxConcurrentUpgradedConnections = hostMaxConcurrentUpgradedConnections;
    }
});

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".Tabsan.EduSphere.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("EduApi");
builder.Services.AddScoped<IEduApiClient, EduApiClient>();

if (useForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        options.RequireHeaderSymmetry = builder.Configuration.GetValue("ReverseProxy:RequireHeaderSymmetry", true);
        options.ForwardLimit = builder.Configuration.GetValue<int?>("ReverseProxy:ForwardLimit") ?? 2;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();

        foreach (var proxyIp in configuredKnownProxies)
        {
            if (IPAddress.TryParse(proxyIp, out var parsedIp))
            {
                options.KnownProxies.Add(parsedIp);
            }
        }
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (useForwardedHeaders)
{
    app.UseForwardedHeaders();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
// Final-Touches Phase 34 Stage 4.2 — apply cache headers only to static files, not authenticated/dynamic MVC responses.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        if (!staticAssetCachingEnabled)
        {
            return;
        }

        context.Context.Response.Headers["Cache-Control"] = $"public,max-age={staticAssetMaxAgeSeconds}";
    }
});

app.UseRouting();

app.UseAuthentication();

// Build a request principal from protected cookie identity so User.IsInRole works across stateless web nodes.
app.Use(async (context, next) =>
{
    var api = context.RequestServices.GetRequiredService<IEduApiClient>();
    var identity = api.GetSessionIdentity();

    if (identity is not null)
    {
        try
        {
            var claims = new List<Claim>();

            if (!string.IsNullOrWhiteSpace(identity.UserName))
            {
                claims.Add(new Claim(ClaimTypes.Name, identity.UserName));
            }

            if (!string.IsNullOrWhiteSpace(identity.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, identity.Email));
            }

            foreach (var role in identity.Roles)
            {
                if (string.IsNullOrWhiteSpace(role))
                    continue;

                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role));
            }

            if (claims.Count > 0)
            {
                var principalIdentity = new ClaimsIdentity(claims, authenticationType: "SessionJwt");
                context.User = new ClaimsPrincipal(principalIdentity);
            }
        }
        catch
        {
            // Ignore malformed identity cookies and continue without overriding principal.
        }
    }

    await next();
});

app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
