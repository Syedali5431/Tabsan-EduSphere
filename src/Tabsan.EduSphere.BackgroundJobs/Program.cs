using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;
using Tabsan.EduSphere.Application.Academic;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Notifications;
using Tabsan.EduSphere.BackgroundJobs;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Application.Services;
using Tabsan.EduSphere.Infrastructure.Email;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.Infrastructure.Repositories;

var builder = Host.CreateApplicationBuilder(args);

var env = builder.Environment;
builder.Configuration.AddEduSphereConfigurationHierarchy(env);
var deploymentTopology = DeploymentTopologyResolver.Resolve(builder.Configuration, env);
var tenantIsolation = TenantIsolationResolver.Resolve(builder.Configuration, env, deploymentTopology);

Console.WriteLine($"[BackgroundJobs] Environment: {env.EnvironmentName} | App: {env.ApplicationName}");
Console.WriteLine($"[BackgroundJobs] Deployment profile: Mode={deploymentTopology.Mode}, Customer={deploymentTopology.CustomerCode}, Domain={deploymentTopology.CustomerDomain}, Database={deploymentTopology.CustomerDatabaseName}, Scaling={deploymentTopology.ScalingEnabled} ({deploymentTopology.MinReplicas}-{deploymentTopology.MaxReplicas})");
Console.WriteLine($"[BackgroundJobs] Tenant isolation: Enabled={tenantIsolation.Enabled}, Mode={tenantIsolation.Mode}, Tenant={tenantIsolation.TenantCode}, Domain={tenantIsolation.TenantDomain}, Database={tenantIsolation.TenantDatabaseName}, Strategy={tenantIsolation.IsolationStrategy}");

// Final-Touches Phase 8 Stage 8.1 — auto-scaling policy metadata and startup guardrails.
var autoScalingEnabled = builder.Configuration.GetValue("InfrastructureTuning:AutoScaling:Enabled", true);
var autoScalingMinReplicas = Math.Max(1, builder.Configuration.GetValue("InfrastructureTuning:AutoScaling:MinReplicas", 1));
var autoScalingMaxReplicas = Math.Max(autoScalingMinReplicas, builder.Configuration.GetValue("InfrastructureTuning:AutoScaling:MaxReplicas", 6));
if (autoScalingEnabled && autoScalingMinReplicas > autoScalingMaxReplicas)
{
    throw new InvalidOperationException("InfrastructureTuning:AutoScaling min replicas cannot exceed max replicas.");
}
Console.WriteLine($"[BackgroundJobs] Infrastructure auto-scaling policy: Enabled={autoScalingEnabled}, MinReplicas={autoScalingMinReplicas}, MaxReplicas={autoScalingMaxReplicas}");

// Final-Touches Phase 8 Stage 8.2 — host limits tuning for sustained worker throughput.
var hostMinWorkerThreads = Math.Max(Environment.ProcessorCount * 8, builder.Configuration.GetValue("InfrastructureTuning:HostLimits:ThreadPoolMinWorkerThreads", Environment.ProcessorCount * 12));
var hostMinCompletionPortThreads = Math.Max(Environment.ProcessorCount * 8, builder.Configuration.GetValue("InfrastructureTuning:HostLimits:ThreadPoolMinCompletionPortThreads", Environment.ProcessorCount * 12));
ThreadPool.GetMinThreads(out var currentMinWorkerThreads, out var currentMinCompletionPortThreads);
if (hostMinWorkerThreads > currentMinWorkerThreads || hostMinCompletionPortThreads > currentMinCompletionPortThreads)
{
    var targetMinWorkerThreads = Math.Max(currentMinWorkerThreads, hostMinWorkerThreads);
    var targetMinCompletionPortThreads = Math.Max(currentMinCompletionPortThreads, hostMinCompletionPortThreads);
    ThreadPool.SetMinThreads(targetMinWorkerThreads, targetMinCompletionPortThreads);
    Console.WriteLine($"[BackgroundJobs] ThreadPool min threads tuned: Worker={targetMinWorkerThreads}, IO={targetMinCompletionPortThreads}");
}

// Final-Touches Phase 8 Stage 8.3 — outbound network stack tuning for queue and notification integrations.
var networkOutboundMaxConnectionsPerServer = Math.Max(25, builder.Configuration.GetValue("InfrastructureTuning:NetworkStack:OutboundMaxConnectionsPerServer", 256));
var networkOutboundConnectTimeoutSeconds = Math.Max(2, builder.Configuration.GetValue("InfrastructureTuning:NetworkStack:OutboundConnectTimeoutSeconds", 10));
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

// ── Database ──────────────────────────────────────────────────────────────────
var databaseConnection = DatabaseConnectionResolver.ResolveDefaultConnection(builder.Configuration, env);
var connectionString = databaseConnection.ConnectionString;
StartupConfigurationFailSafeValidator.ValidateCommonStartupConfiguration(
    builder.Configuration,
    env,
    "Tabsan.EduSphere.BackgroundJobs",
    databaseConnection,
    deploymentTopology,
    tenantIsolation);
Console.WriteLine($"[BackgroundJobs] Database connection source: {databaseConnection.Source}");
builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connectionString));

// ── Repository + notification services ───────────────────────────────────────
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IParentStudentLinkRepository, ParentStudentLinkRepository>();
builder.Services.AddScoped<IEmailSender, MailKitEmailSender>();
builder.Services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();

// ── Phase 12: Academic Calendar ───────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAcademicDeadlineRepository, AcademicDeadlineRepository>();
builder.Services.AddScoped<IAcademicCalendarService, AcademicCalendarService>();

// ── Background jobs ───────────────────────────────────────────────────────────
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<LicenseExpiryWarningJob>();
builder.Services.AddHostedService<DeadlineReminderJob>();

var host = builder.Build();
host.Run();
