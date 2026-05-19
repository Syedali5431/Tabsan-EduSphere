// ── Builder configuration ──────────────────────────────────────────────────────

using System.Net;
using System.Text;
using System.Threading.RateLimiting;using FluentValidation;
using FluentValidation.AspNetCore;using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Tokens;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Application.Auth;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Modules;
using Tabsan.EduSphere.Application.Academic;
using Tabsan.EduSphere.Application.Assignments;
using Tabsan.EduSphere.Application.Attendance;
using Tabsan.EduSphere.Application.Fyp;
using Tabsan.EduSphere.Application.Notifications;
using Tabsan.EduSphere.Application.Quizzes;
using Tabsan.EduSphere.Application.AiChat;
using Tabsan.EduSphere.API.Middleware;
using Tabsan.EduSphere.Infrastructure.AiChat;
using Tabsan.EduSphere.Infrastructure.Analytics;
using Tabsan.EduSphere.BackgroundJobs;
using Tabsan.EduSphere.Infrastructure.Auditing;
using Tabsan.EduSphere.Infrastructure.Auth;
using Tabsan.EduSphere.Infrastructure.Licensing;
using Tabsan.EduSphere.Infrastructure.Modules;
using Tabsan.EduSphere.Infrastructure.Persistence;
using Tabsan.EduSphere.Infrastructure.Repositories;
using Tabsan.EduSphere.Application.Services;
using Tabsan.EduSphere.Infrastructure.Exporters;
using Tabsan.EduSphere.Infrastructure.Integrations;
using Tabsan.EduSphere.API.Services;
using Tabsan.EduSphere.Application.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Serilog;
using Serilog.Events;
using OpenTelemetry.Metrics;

static string RequireSecureStartupValue(IConfiguration configuration, IHostEnvironment environment, string settingPath, int minLength = 1)
    => SecureConfigurationValidator.RequireSecureValue(configuration, environment, settingPath, minLength);

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;
builder.Configuration.AddEduSphereConfigurationHierarchy(env);
var deploymentTopology = DeploymentTopologyResolver.Resolve(builder.Configuration, env);
var tenantIsolation = TenantIsolationResolver.Resolve(builder.Configuration, env, deploymentTopology);

Console.WriteLine($"[Startup] Environment: {env.EnvironmentName} | App: {env.ApplicationName}");
Console.WriteLine("[Startup] Configuration sources: appsettings.json, appsettings.{Environment}.json, environment variables");

// Final-Touches Phase 34 Stage 2.1 — per-instance identity baseline for horizontal API scale verification.
var configuredInstanceId = builder.Configuration["ScaleOut:InstanceId"];
var runtimeInstanceId = string.IsNullOrWhiteSpace(configuredInstanceId)
    ? $"{Environment.MachineName.ToLowerInvariant()}-p{Environment.ProcessId}"
    : configuredInstanceId.Trim();
var exposeInstanceHeader = builder.Configuration.GetValue("ScaleOut:ExposeInstanceHeader", true);
var processStartUtc = DateTimeOffset.UtcNow;
Console.WriteLine($"[Startup] ScaleOut InstanceId: {runtimeInstanceId} | ExposeInstanceHeader: {exposeInstanceHeader}");

// Final-Touches Phase 9 Stage 9.1 — shared observability state for Prometheus metrics and latency SLO snapshots.
builder.Services.AddSingleton(new ObservabilityMetrics(processStartUtc));
builder.Services.Configure<BackgroundJobReliabilityOptions>(builder.Configuration.GetSection(BackgroundJobReliabilityOptions.SectionName));
builder.Services.Configure<NotificationEmailOptions>(builder.Configuration.GetSection(NotificationEmailOptions.SectionName));
builder.Services.Configure<NotificationSmsOptions>(builder.Configuration.GetSection(NotificationSmsOptions.SectionName));
builder.Services.AddSingleton<BackgroundJobHealthTracker>();

// Final-Touches Phase 8 Stage 8.1 — auto-scaling policy metadata and startup guardrails.
var autoScalingEnabled = builder.Configuration.GetValue("InfrastructureTuning:AutoScaling:Enabled", true);
var autoScalingMinReplicas = Math.Max(1, builder.Configuration.GetValue("InfrastructureTuning:AutoScaling:MinReplicas", 2));
var autoScalingMaxReplicas = Math.Max(autoScalingMinReplicas, builder.Configuration.GetValue("InfrastructureTuning:AutoScaling:MaxReplicas", 12));
var autoScalingTargetCpuPercent = Math.Clamp(builder.Configuration.GetValue("InfrastructureTuning:AutoScaling:TargetCpuUtilizationPercent", 70), 30, 95);
var autoScalingTargetMemoryPercent = Math.Clamp(builder.Configuration.GetValue("InfrastructureTuning:AutoScaling:TargetMemoryUtilizationPercent", 75), 30, 95);
var autoScalingScaleOutCooldownSeconds = Math.Max(15, builder.Configuration.GetValue("InfrastructureTuning:AutoScaling:ScaleOutCooldownSeconds", 60));
var autoScalingScaleInCooldownSeconds = Math.Max(30, builder.Configuration.GetValue("InfrastructureTuning:AutoScaling:ScaleInCooldownSeconds", 180));
if (autoScalingEnabled && autoScalingMinReplicas > autoScalingMaxReplicas)
{
    throw new InvalidOperationException("InfrastructureTuning:AutoScaling min replicas cannot exceed max replicas.");
}
Console.WriteLine($"[Startup] Infrastructure auto-scaling policy: Enabled={autoScalingEnabled}, MinReplicas={autoScalingMinReplicas}, MaxReplicas={autoScalingMaxReplicas}, CpuTarget={autoScalingTargetCpuPercent}%, MemoryTarget={autoScalingTargetMemoryPercent}%");

// Final-Touches Phase 8 Stage 8.2 — host limits tuning for high-concurrency request handling.
var hostMinWorkerThreads = Math.Max(Environment.ProcessorCount * 8, builder.Configuration.GetValue("InfrastructureTuning:HostLimits:ThreadPoolMinWorkerThreads", Environment.ProcessorCount * 16));
var hostMinCompletionPortThreads = Math.Max(Environment.ProcessorCount * 8, builder.Configuration.GetValue("InfrastructureTuning:HostLimits:ThreadPoolMinCompletionPortThreads", Environment.ProcessorCount * 16));
var hostMaxConcurrentConnections = builder.Configuration.GetValue<long?>("InfrastructureTuning:HostLimits:MaxConcurrentConnections");
var hostMaxConcurrentUpgradedConnections = builder.Configuration.GetValue<long?>("InfrastructureTuning:HostLimits:MaxConcurrentUpgradedConnections");
ThreadPool.GetMinThreads(out var currentMinWorkerThreads, out var currentMinCompletionPortThreads);
if (hostMinWorkerThreads > currentMinWorkerThreads || hostMinCompletionPortThreads > currentMinCompletionPortThreads)
{
    var targetMinWorkerThreads = Math.Max(currentMinWorkerThreads, hostMinWorkerThreads);
    var targetMinCompletionPortThreads = Math.Max(currentMinCompletionPortThreads, hostMinCompletionPortThreads);
    ThreadPool.SetMinThreads(targetMinWorkerThreads, targetMinCompletionPortThreads);
    Console.WriteLine($"[Startup] ThreadPool min threads tuned: Worker={targetMinWorkerThreads}, IO={targetMinCompletionPortThreads}");
}

// Final-Touches Phase 8 Stage 8.3 — network stack tuning for high connection volume and outbound saturation control.
var networkKeepAliveTimeoutSeconds = Math.Max(30, builder.Configuration.GetValue("InfrastructureTuning:NetworkStack:KeepAliveTimeoutSeconds", 120));
var networkRequestHeadersTimeoutSeconds = Math.Max(5, builder.Configuration.GetValue("InfrastructureTuning:NetworkStack:RequestHeadersTimeoutSeconds", 20));
var networkHttp2KeepAlivePingDelaySeconds = Math.Max(5, builder.Configuration.GetValue("InfrastructureTuning:NetworkStack:Http2KeepAlivePingDelaySeconds", 30));
var networkHttp2KeepAlivePingTimeoutSeconds = Math.Max(5, builder.Configuration.GetValue("InfrastructureTuning:NetworkStack:Http2KeepAlivePingTimeoutSeconds", 10));
var networkHttp2MaxStreamsPerConnection = Math.Max(100, builder.Configuration.GetValue("InfrastructureTuning:NetworkStack:Http2MaxStreamsPerConnection", 200));
var networkOutboundMaxConnectionsPerServer = Math.Max(50, builder.Configuration.GetValue("InfrastructureTuning:NetworkStack:OutboundMaxConnectionsPerServer", 512));
var networkOutboundConnectTimeoutSeconds = Math.Max(2, builder.Configuration.GetValue("InfrastructureTuning:NetworkStack:OutboundConnectTimeoutSeconds", 10));

var databaseConnection = DatabaseConnectionResolver.ResolveDefaultConnection(builder.Configuration, env);
var configuredConnectionString = databaseConnection.ConnectionString;
StartupConfigurationFailSafeValidator.ValidateCommonStartupConfiguration(
    builder.Configuration,
    env,
    "Tabsan.EduSphere.API",
    databaseConnection,
    deploymentTopology,
    tenantIsolation);

var configurationSourceSummary = StartupVisibilityReporter.DescribeConfigurationSources(
    databaseConnection.Source,
    deploymentTopology.Source,
    tenantIsolation.Source);
var databaseType = StartupVisibilityReporter.DescribeDatabaseType(configuredConnectionString);

var useForwardedHeaders = builder.Configuration.GetValue<bool>("ReverseProxy:Enabled");
var configuredKnownProxies = builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [];

Console.WriteLine($"[Startup] Database type: {databaseType}");
Console.WriteLine($"[Startup] Configuration source summary: {configurationSourceSummary}");
Console.WriteLine($"[Startup] Database connection source: {databaseConnection.Source}");
Console.WriteLine($"[Startup] Deployment profile: Mode={deploymentTopology.Mode}, Customer={deploymentTopology.CustomerCode}, Domain={deploymentTopology.CustomerDomain}, Database={deploymentTopology.CustomerDatabaseName}, Scaling={deploymentTopology.ScalingEnabled} ({deploymentTopology.MinReplicas}-{deploymentTopology.MaxReplicas})");
Console.WriteLine($"[Startup] Tenant isolation: Enabled={tenantIsolation.Enabled}, Mode={tenantIsolation.Mode}, Tenant={tenantIsolation.TenantCode}, Domain={tenantIsolation.TenantDomain}, Database={tenantIsolation.TenantDatabaseName}, Strategy={tenantIsolation.IsolationStrategy}");

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

// Final-Touches Phase 9 Stage 9.1 — publish OpenTelemetry metrics with Prometheus scraping support.
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
        metrics.AddRuntimeInstrumentation();
        metrics.AddProcessInstrumentation();
        metrics.AddMeter(ObservabilityMetrics.MeterName);
        metrics.AddPrometheusExporter();
    });

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

// ── Serilog (structured logging — console + rolling file) ───────────────────────
builder.Host.UseSerilog((ctx, services, config) =>
{
    var isDev = ctx.HostingEnvironment.IsDevelopment();
    var minLevel = isDev ? LogEventLevel.Debug : LogEventLevel.Warning;

    config
        .MinimumLevel.Is(minLevel)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Tabsan.EduSphere.API")
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/app-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
});

// ── Database ────────────────────────────────────────────────────────────────────
// Reads the connection string from appsettings.json → ConnectionStrings:DefaultConnection.
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseSqlServer(configuredConnectionString,
        sql =>
        {
            sql.MigrationsAssembly("Tabsan.EduSphere.Infrastructure");
            sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
        }));

// ── JWT Authentication ──────────────────────────────────────────────────────────
// Binds JwtSettings section from appsettings.json so options are strongly-typed.
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<AuthSecurityOptions>(builder.Configuration.GetSection(AuthSecurityOptions.SectionName));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
var authSecuritySettings = builder.Configuration.GetSection(AuthSecurityOptions.SectionName).Get<AuthSecurityOptions>() ?? new AuthSecurityOptions();

// Final-Touches Phase 34 Stage 34.1 - block unsafe MFA bootstrap in non-dev/test environments.
if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
{
    if (authSecuritySettings.Mfa.Enabled)
    {
        if (string.IsNullOrWhiteSpace(authSecuritySettings.Mfa.TotpIssuer))
        {
            throw new InvalidOperationException("AuthSecurity:Mfa is enabled but AuthSecurity:Mfa:TotpIssuer is not configured.");
        }

        if (authSecuritySettings.Mfa.RequireForPrivilegedRolesOnly
            && (authSecuritySettings.Mfa.PrivilegedRoles is null || authSecuritySettings.Mfa.PrivilegedRoles.Length == 0))
        {
            throw new InvalidOperationException("AuthSecurity:Mfa:RequireForPrivilegedRolesOnly is enabled but no privileged roles are configured.");
        }
    }

    RequireSecureStartupValue(builder.Configuration, builder.Environment, "JwtSettings:SecretKey", minLength: 32);

    var notificationEmailEnabled = builder.Configuration.GetValue("NotificationEmail:Enabled", false);
    if (notificationEmailEnabled)
    {
        RequireSecureStartupValue(builder.Configuration, builder.Environment, "Email:Username");
        RequireSecureStartupValue(builder.Configuration, builder.Environment, "Email:Password");
    }

    RequireSecureStartupValue(builder.Configuration, builder.Environment, "ScaleOut:RedisConnectionString");
    RequireSecureStartupValue(builder.Configuration, builder.Environment, "MediaStorage:SignedUrlSecret");

    var queueProvider = builder.Configuration["QueuePlatform:Provider"]?.Trim() ?? "InMemory";
    var rabbitMqEnabled = builder.Configuration.GetValue("QueuePlatform:RabbitMq:Enabled", false)
        || string.Equals(queueProvider, "RabbitMq", StringComparison.OrdinalIgnoreCase);
    if (rabbitMqEnabled)
    {
        RequireSecureStartupValue(builder.Configuration, builder.Environment, "QueuePlatform:RabbitMq:ConnectionString");
    }
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSettings.Issuer,
            ValidAudience            = jwtSettings.Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero // no extra leeway on token expiry
        };
    });

// ── Authorization policies ──────────────────────────────────────────────────────
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("SuperAdmin", p => p.RequireRole("SuperAdmin"));
    opts.AddPolicy("Admin",      p => p.RequireRole("SuperAdmin", "Admin"));
    opts.AddPolicy("Faculty",    p => p.RequireRole("SuperAdmin", "Admin", "Faculty"));
    opts.AddPolicy("Student",    p => p.RequireRole("SuperAdmin", "Admin", "Faculty", "Student"));
});

// ── Infrastructure services ─────────────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILicenseRepository, LicenseRepository>();
builder.Services.AddScoped<IModuleRepository, ModuleRepository>();
builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantScopeResolver, HttpTenantScopeResolver>();
builder.Services.AddScoped<IAccessScopeResolver, HttpAccessScopeResolver>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<TokenService>(); // also registered directly for AuthController resolving
builder.Services.AddScoped<IPasswordHasher, Argon2idPasswordHasher>();
builder.Services.AddScoped<ITotpService, TotpService>();
builder.Services.AddScoped<LicenseValidationService>();

// ── Module entitlement ──────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();
var redisConnectionString = builder.Configuration["ScaleOut:RedisConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    // Final-Touches Phase 34 Stage 2.3 — force shared distributed cache in production so cached state stays node-agnostic.
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = builder.Configuration["ScaleOut:RedisInstanceName"] ?? "Tabsan.EduSphere:";
    });
}
else
{
    if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
    {
        throw new InvalidOperationException("ScaleOut:RedisConnectionString is required outside Development/Testing to keep distributed cache state stateless across API instances.");
    }

    // Final-Touches Phase 34 Stage 2.3 — allow local in-memory cache only for developer/test runs.
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.AddScoped<IModuleEntitlementResolver, ModuleEntitlementResolver>();
builder.Services.AddScoped<ModuleEntitlementResolver>(); // concrete needed by LicenseController
// Final-Touches Phase 28 Stage 28.3 — storage provider abstraction for file/media workflows.
builder.Services.AddConfiguredMediaStorage(builder.Configuration);

// ── Phase 2: Academic repositories ─────────────────────────────────────────────
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IAcademicProgramRepository, AcademicProgramRepository>();
builder.Services.AddScoped<ISemesterRepository, SemesterRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<IStudentProfileRepository, StudentProfileRepository>();
builder.Services.AddScoped<IRegistrationWhitelistRepository, RegistrationWhitelistRepository>();
builder.Services.AddScoped<IFacultyAssignmentRepository, FacultyAssignmentRepository>();
builder.Services.AddScoped<IAdminAssignmentRepository, AdminAssignmentRepository>();

// ── Application services ────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IModuleService, ModuleService>();
// Phase 2 application services
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IStudentRegistrationService, StudentRegistrationService>();

// ── Phase 3: Assignments and Results ───────────────────────────────────────────
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
builder.Services.AddScoped<IResultRepository, ResultRepository>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IResultService, ResultService>();
builder.Services.AddScoped<IResultCalculationService, ResultCalculationService>();

// ── Phase 4: Notifications and Attendance ─────────────────────────────────
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddSingleton<NotificationFanoutQueue>();
builder.Services.AddSingleton<INotificationFanoutQueue>(sp => sp.GetRequiredService<NotificationFanoutQueue>());
builder.Services.AddHostedService<NotificationFanoutWorker>();
builder.Services.AddSingleton<ResultPublishJobQueue>();
builder.Services.AddSingleton<ResultPublishJobStore>();
builder.Services.AddHostedService<ResultPublishJobWorker>();
builder.Services.AddSingleton<ReportExportJobQueue>();
builder.Services.AddSingleton<ReportExportJobStore>();
builder.Services.AddHostedService<ReportExportJobWorker>();

// Final-Touches Phase 34 Stage 7.2 — queue platform selection (InMemory vs RabbitMq) for account-security offload workers.
builder.Services.Configure<QueuePlatformOptions>(builder.Configuration.GetSection("QueuePlatform"));
var queuePlatformProvider = builder.Configuration["QueuePlatform:Provider"]?.Trim() ?? "InMemory";
var useRabbitQueuePlatform = string.Equals(queuePlatformProvider, "RabbitMq", StringComparison.OrdinalIgnoreCase);
if (useRabbitQueuePlatform)
{
    builder.Services.AddSingleton<IAccountSecurityEmailQueue, RabbitMqAccountSecurityEmailQueue>();
    builder.Services.AddHostedService<RabbitMqAccountSecurityEmailWorker>();
}
else
{
    builder.Services.AddSingleton<InMemoryAccountSecurityEmailQueue>();
    builder.Services.AddSingleton<IAccountSecurityEmailQueue>(sp => sp.GetRequiredService<InMemoryAccountSecurityEmailQueue>());
    builder.Services.AddHostedService<InMemoryAccountSecurityEmailWorker>();
}
// ── Phase 5: Quizzes and FYP ──────────────────────────────────────────
builder.Services.AddScoped<IQuizRepository, QuizRepository>();
builder.Services.AddScoped<IFypRepository, FypRepository>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IFypService, FypService>();

// ── Phase 6: AI Chat and Analytics ───────────────────────────────────
builder.Services.AddScoped<IAiChatRepository, AiChatRepository>();
builder.Services.AddScoped<IAiChatService, AiChatService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddSingleton<AnalyticsExportJobQueue>();
builder.Services.AddSingleton<AnalyticsExportJobStore>();
builder.Services.AddHostedService<AnalyticsExportJobWorker>();
// LLM HTTP client — base URL and API key from AiChat:BaseUrl / AiChat:ApiKey in config.
builder.Services.AddHttpClient<ILlmClient, OpenAiLlmClient>((sp, client) =>
{
    var cfg = sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
    client.BaseAddress = new Uri(cfg["AiChat:BaseUrl"] ?? "https://api.openai.com/");
    var apiKey = cfg["AiChat:ApiKey"] ?? string.Empty;
    if (!string.IsNullOrWhiteSpace(apiKey))
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
});

// ── Phase 8: Student Lifecycle & Account Security ───────────────────────
builder.Services.AddScoped<IStudentLifecycleRepository, StudentLifecycleRepository>();
builder.Services.AddScoped<IStudentLifecycleService, StudentLifecycleService>();
builder.Services.AddScoped<IAccountSecurityService, AccountSecurityService>();
builder.Services.AddScoped<ICsvRegistrationImportService, CsvRegistrationImportService>();

// ── Phase 4: CSV User Import (P4-S1-01) ──────────────────────────────────
builder.Services.AddScoped<IUserImportService, UserImportService>();

// ── Phase 9: Timetable, Report Settings, Module Roles, Theme ─────────────
builder.Services.AddScoped<ITimetableRepository, TimetableRepository>();
builder.Services.AddScoped<IBuildingRoomRepository, BuildingRoomRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
builder.Services.AddScoped<ITimetableExcelExporter, TimetableExcelExporter>();
builder.Services.AddScoped<ITimetablePdfExporter, TimetablePdfExporter>();
builder.Services.AddScoped<ITimetableService, TimetableService>();
builder.Services.AddScoped<IBuildingRoomService, BuildingRoomService>();
builder.Services.AddScoped<IReportSettingsService, ReportSettingsService>();
builder.Services.AddScoped<IModuleRolesService, ModuleRolesService>();
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<ISidebarMenuService, SidebarMenuService>();
builder.Services.AddScoped<IPortalBrandingService, PortalBrandingService>();
builder.Services.AddScoped<ITenantOperationsService, TenantOperationsService>();
builder.Services.AddScoped<IFeatureFlagService, FeatureFlagService>();

// ── Phase 10: Password history ────────────────────────────────────────────
builder.Services.AddScoped<IPasswordHistoryRepository, Tabsan.EduSphere.Infrastructure.Repositories.PasswordHistoryRepository>();

// ── Phase 12: Reporting ───────────────────────────────────────────────────
builder.Services.AddScoped<Tabsan.EduSphere.Domain.Interfaces.IReportRepository, Tabsan.EduSphere.Infrastructure.Reporting.ReportRepository>();
builder.Services.AddScoped<IReportService, Tabsan.EduSphere.Infrastructure.Reporting.ReportService>();

// ── Phase 12: Academic Calendar ───────────────────────────────────────────
builder.Services.AddScoped<IAcademicDeadlineRepository, AcademicDeadlineRepository>();
builder.Services.AddScoped<IAcademicCalendarService, AcademicCalendarService>();

// ── Phase 13: Global Search ───────────────────────────────────────────────
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.ISearchRepository, Tabsan.EduSphere.Infrastructure.Repositories.SearchRepository>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.ISearchService, Tabsan.EduSphere.Application.Search.SearchService>();

// ── Phase 14: Helpdesk / Support Ticketing ────────────────────────────────
builder.Services.AddScoped<Tabsan.EduSphere.Domain.Interfaces.IHelpdeskRepository, Tabsan.EduSphere.Infrastructure.Repositories.HelpdeskRepository>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IHelpdeskService, Tabsan.EduSphere.Application.Helpdesk.HelpdeskService>();

// ── Phase 15: Enrollment Rules Engine ────────────────────────────────────
builder.Services.AddScoped<Tabsan.EduSphere.Domain.Interfaces.IPrerequisiteRepository, Tabsan.EduSphere.Infrastructure.Repositories.PrerequisiteRepository>();

// ── Phase 16: Faculty Grading System ─────────────────────────────────────
builder.Services.AddScoped<Tabsan.EduSphere.Domain.Interfaces.IGradebookRepository, Tabsan.EduSphere.Infrastructure.Repositories.GradebookRepository>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IGradebookService, Tabsan.EduSphere.Application.Assignments.GradebookService>();
builder.Services.AddScoped<Tabsan.EduSphere.Domain.Interfaces.IRubricRepository, Tabsan.EduSphere.Infrastructure.Repositories.RubricRepository>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IRubricService, Tabsan.EduSphere.Application.Assignments.RubricService>();

// ── Phase 17: Degree Audit System ─────────────────────────────────────────
builder.Services.AddScoped<Tabsan.EduSphere.Domain.Interfaces.IDegreeAuditRepository, Tabsan.EduSphere.Infrastructure.Repositories.DegreeAuditRepository>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IDegreeAuditService, Tabsan.EduSphere.Application.Academic.DegreeAuditService>();

// ── Phase 18: Graduation Workflow ──────────────────────────────────────────
builder.Services.AddScoped<Tabsan.EduSphere.Domain.Interfaces.IGraduationRepository, Tabsan.EduSphere.Infrastructure.Repositories.GraduationRepository>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.ICertificateGenerator, Tabsan.EduSphere.Infrastructure.Services.CertificateGenerator>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IGraduationService, Tabsan.EduSphere.Application.Academic.GraduationService>();

// ── Phase 19: Advanced Course Creation & Grading Config ────────────────────
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.ICourseService, Tabsan.EduSphere.Application.Academic.CourseService>();
builder.Services.AddScoped<Tabsan.EduSphere.Domain.Interfaces.ICourseGradingRepository, Tabsan.EduSphere.Infrastructure.Repositories.CourseGradingRepository>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.ICourseGradingService, Tabsan.EduSphere.Application.Academic.CourseGradingService>();

// ── Phase 20: Learning Management System (LMS) ─────────────────────────────
builder.Services.AddScoped<Tabsan.EduSphere.Domain.Interfaces.ILmsRepository, Tabsan.EduSphere.Infrastructure.Repositories.LmsRepository>();
builder.Services.AddScoped<Tabsan.EduSphere.Domain.Interfaces.IDiscussionRepository, Tabsan.EduSphere.Infrastructure.Repositories.DiscussionRepository>();
builder.Services.AddScoped<Tabsan.EduSphere.Domain.Interfaces.IAnnouncementRepository, Tabsan.EduSphere.Infrastructure.Repositories.AnnouncementRepository>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.ILmsService, Tabsan.EduSphere.Application.Lms.LmsService>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IDiscussionService, Tabsan.EduSphere.Application.Lms.DiscussionService>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IAnnouncementService, Tabsan.EduSphere.Application.Lms.AnnouncementService>();

// ── Phase 21: Study Planner ──────────────────────────────────────────────────
builder.Services.AddScoped<Tabsan.EduSphere.Domain.Interfaces.IStudyPlanRepository, Tabsan.EduSphere.Infrastructure.Repositories.StudyPlanRepository>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IStudyPlanService, Tabsan.EduSphere.Application.StudyPlanner.StudyPlanService>();

// ── Phase 22: External Integrations ──────────────────────────────────────────
builder.Services.AddScoped<Tabsan.EduSphere.Domain.Interfaces.IAccreditationRepository, Tabsan.EduSphere.Infrastructure.Repositories.AccreditationRepository>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IAccreditationService, Tabsan.EduSphere.Application.Services.AccreditationService>();
builder.Services.AddHttpClient<Tabsan.EduSphere.Application.Interfaces.ILibraryService, Tabsan.EduSphere.Application.Services.LibraryService>();
// ── Phase 23: Core Policy Foundation ────────────────────────────────────────────
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IInstitutionPolicyService, Tabsan.EduSphere.Application.Services.InstitutionPolicyService>();
// ── Phase 24: Dynamic Module and UI Composition ───────────────────────────────
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IModuleRegistryService, Tabsan.EduSphere.Application.Modules.ModuleRegistryService>();
builder.Services.AddSingleton<Tabsan.EduSphere.Application.Interfaces.ILabelService, Tabsan.EduSphere.Application.Services.LabelService>();
builder.Services.AddSingleton<Tabsan.EduSphere.Application.Interfaces.IDashboardCompositionService, Tabsan.EduSphere.Application.Services.DashboardCompositionService>();
// ── Phase 25: Academic Engine Unification ─────────────────────────────────────
builder.Services.AddSingleton<Tabsan.EduSphere.Application.Interfaces.IResultStrategyResolver, Tabsan.EduSphere.Application.Academic.ResultStrategyResolver>();
builder.Services.AddScoped<Tabsan.EduSphere.Domain.Interfaces.IInstitutionGradingProfileRepository, Tabsan.EduSphere.Infrastructure.Repositories.InstitutionGradingProfileRepository>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IInstitutionGradingService, Tabsan.EduSphere.Application.Academic.InstitutionGradingService>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IProgressionService, Tabsan.EduSphere.Application.Academic.ProgressionService>();
// ── Phase 26: School and College Functional Expansion ───────────────────────
builder.Services.AddScoped<Tabsan.EduSphere.Domain.Interfaces.ISchoolStreamRepository, Tabsan.EduSphere.Infrastructure.Repositories.SchoolStreamRepository>();
builder.Services.AddScoped<Tabsan.EduSphere.Domain.Interfaces.IReportCardRepository, Tabsan.EduSphere.Infrastructure.Repositories.ReportCardRepository>();
builder.Services.AddScoped<Tabsan.EduSphere.Domain.Interfaces.IBulkPromotionRepository, Tabsan.EduSphere.Infrastructure.Repositories.BulkPromotionRepository>();
builder.Services.AddScoped<Tabsan.EduSphere.Domain.Interfaces.IParentStudentLinkRepository, Tabsan.EduSphere.Infrastructure.Repositories.ParentStudentLinkRepository>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.ISchoolStreamService, Tabsan.EduSphere.Application.Academic.SchoolStreamService>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IReportCardService, Tabsan.EduSphere.Application.Academic.ReportCardService>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IBulkPromotionService, Tabsan.EduSphere.Application.Academic.BulkPromotionService>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IParentPortalService, Tabsan.EduSphere.Application.Academic.ParentPortalService>();
// ── Phase 27: University Portal Parity and Student Experience ───────────────
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IPortalCapabilityMatrixService, Tabsan.EduSphere.Application.Services.PortalCapabilityMatrixService>();
// ── Phase 30: Integration Gateway Layer ─────────────────────────────────────
builder.Services.Configure<IntegrationGatewayOptions>(builder.Configuration.GetSection("IntegrationGateway"));
builder.Services.AddSingleton<Tabsan.EduSphere.Application.Interfaces.IOutboundIntegrationGateway, ResilientOutboundIntegrationGateway>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.ISupportTicketingProvider, Tabsan.EduSphere.Infrastructure.Integrations.InAppSupportTicketingProvider>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IAnnouncementBroadcastProvider, Tabsan.EduSphere.Infrastructure.Integrations.InAppAnnouncementBroadcastProvider>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.IEmailDeliveryProvider, Tabsan.EduSphere.Infrastructure.Integrations.SmtpEmailDeliveryProvider>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.ISmsDeliveryProvider, Tabsan.EduSphere.Infrastructure.Integrations.TwilioSmsDeliveryProvider>();
builder.Services.AddScoped<Tabsan.EduSphere.Application.Interfaces.ICommunicationIntegrationService, Tabsan.EduSphere.Application.Services.CommunicationIntegrationService>();
// ── Rate limiting (OWASP hardening) ─────────────────────────────────────
builder.Services.AddRateLimiter(opts =>
{
    // Global sliding window: 100 requests per minute per IP.
    opts.AddSlidingWindowLimiter("global", o =>
    {
        o.PermitLimit       = 100;
        o.Window            = TimeSpan.FromMinutes(1);
        o.SegmentsPerWindow = 6;
        o.QueueLimit        = 0;
    });
    // Stricter limit for authentication endpoints: 10 per minute per IP.
    opts.AddSlidingWindowLimiter("auth", o =>
    {
        o.PermitLimit       = 10;
        o.Window            = TimeSpan.FromMinutes(1);
        o.SegmentsPerWindow = 6;
        o.QueueLimit        = 0;
    });
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ── CORS (configured from AppSettings:CorsOrigins) ───────────────────────────
var corsOrigins = builder.Configuration.GetSection("AppSettings:CorsOrigins").Get<string[]>() ?? [];
if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing") && corsOrigins.Length == 0)
{
    throw new InvalidOperationException("AppSettings:CorsOrigins must include at least one origin outside Development.");
}
if (corsOrigins.Length > 0)
{
    builder.Services.AddCors(corsOpts =>
        corsOpts.AddPolicy("AllowConfiguredOrigins", policy =>
            policy.WithOrigins(corsOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()));
}

// ── Forwarded headers (reverse proxy: IIS / nginx / Cloudflare) ──────────────
if (useForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(fwdOpts =>
    {
        fwdOpts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        fwdOpts.RequireHeaderSymmetry = builder.Configuration.GetValue("ReverseProxy:RequireHeaderSymmetry", true);
        fwdOpts.ForwardLimit = builder.Configuration.GetValue<int?>("ReverseProxy:ForwardLimit") ?? 2;
        fwdOpts.KnownNetworks.Clear();
        fwdOpts.KnownProxies.Clear();

        foreach (var proxyIp in configuredKnownProxies)
        {
            if (IPAddress.TryParse(proxyIp, out var parsedIp))
            {
                fwdOpts.KnownProxies.Add(parsedIp);
            }
        }
    });
}

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── Request body size limits (5 MB max — OWASP resource protection) ──────────
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(
    opts => opts.Limits.MaxRequestBodySize = 5 * 1024 * 1024);
builder.Services.Configure<IISServerOptions>(
    opts => opts.MaxRequestBodySize = 5 * 1024 * 1024);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(
    opts => opts.MultipartBodyLengthLimit = 5 * 1024 * 1024);

builder.Services.AddHostedService<LicenseCheckWorker>();
builder.Services.AddHostedService<AttendanceAlertJob>();

// Final-Touches Phase 9 Stage 9.3 — full-stack health checks for database, memory, CPU, network, and error rate.
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseConnectivityHealthCheck>("database", tags: ["ready", "db"])
    .AddCheck<MemoryPressureHealthCheck>("memory", tags: ["live", "resource"])
    .AddCheck<CpuPressureHealthCheck>("cpu", tags: ["live", "resource"])
    .AddCheck<NetworkStackHealthCheck>("network", tags: ["live", "network"])
    .AddCheck<ErrorRateHealthCheck>("error-rate", tags: ["live", "slo"]);

// ── Email ─────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IEmailSender, Tabsan.EduSphere.Infrastructure.Email.MailKitEmailSender>();
builder.Services.AddSingleton<IEmailTemplateRenderer, Tabsan.EduSphere.Infrastructure.Email.EmailTemplateRenderer>();

// ── FluentValidation ───────────────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<Tabsan.EduSphere.Application.Validators.LoginRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// ── API infrastructure ──────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Tabsan EduSphere API", Version = "v1" });

    // Add JWT bearer button to Swagger UI for easy manual testing.
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description  = "Enter your JWT token (without the 'Bearer ' prefix)"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── Seed database on startup ─────────────────────────────────────────────────────
await DatabaseSeeder.SeedAsync(app.Services);

// ── HTTP pipeline ────────────────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (useForwardedHeaders)
{
    app.UseForwardedHeaders();
}

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("AppSettings:EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
// Final-Touches Phase 9 Stage 9.2 — capture rolling request timings for p50/p95/p99 SLO summaries.
app.Use(async (context, next) =>
{
    var observabilityMetrics = context.RequestServices.GetRequiredService<ObservabilityMetrics>();
    var startTimestamp = Stopwatch.GetTimestamp();

    try
    {
        await next();
    }
    finally
    {
        var duration = Stopwatch.GetElapsedTime(startTimestamp);
        observabilityMetrics.RecordRequest(duration, context.Response.StatusCode);
    }
});
// Serve uploaded branding assets (e.g., /portal-uploads/logo.png) from API wwwroot.
var apiWebRoot = app.Environment.WebRootPath;
if (string.IsNullOrWhiteSpace(apiWebRoot))
{
    apiWebRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
}
Directory.CreateDirectory(apiWebRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(apiWebRoot)
});
if (exposeInstanceHeader)
{
    // Final-Touches Phase 34 Stage 2.1 — emit node identity so balancer distribution can be observed.
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-EduSphere-Instance"] = runtimeInstanceId;
        await next();
    });
}
app.UseSecurityHeaders();
app.UseRateLimiter();
if (corsOrigins.Length > 0)
{
    app.UseCors("AllowConfiguredOrigins");
}
// P2-S3-02: Reject requests from domains that do not match the activated license domain.
app.UseMiddleware<LicenseDomainMiddleware>();
app.UseAuthentication();
app.UseMiddleware<Tabsan.EduSphere.API.Middleware.ModuleLicenseEnforcementMiddleware>();
app.UseAuthorization();
// Phase 23 — resolve institution policy snapshot once per request (after auth)
app.UseMiddleware<Tabsan.EduSphere.API.Middleware.InstitutionContextMiddleware>();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics").AllowAnonymous();
app.MapGet("/health/instance", () => Results.Ok(new
{
    status = "ok",
    instanceId = runtimeInstanceId,
    processId = Environment.ProcessId,
    machine = Environment.MachineName,
    uptimeSeconds = (long)(DateTimeOffset.UtcNow - processStartUtc).TotalSeconds,
    version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown"
})).AllowAnonymous();
app.MapGet("/health/observability", (ObservabilityMetrics observabilityMetrics) => Results.Ok(observabilityMetrics.GetSnapshot())).AllowAnonymous();
app.MapGet("/health/background-jobs", (
    BackgroundJobHealthTracker backgroundJobHealthTracker,
    IOptions<BackgroundJobReliabilityOptions> reliabilityOptions) => Results.Ok(new
{
    reliability = new
    {
        maxRetryAttempts = Math.Max(1, reliabilityOptions.Value.MaxRetryAttempts),
        baseDelayMilliseconds = Math.Max(25, reliabilityOptions.Value.BaseDelayMilliseconds),
        alertConsecutiveFailureThreshold = Math.Max(1, reliabilityOptions.Value.AlertConsecutiveFailureThreshold)
    },
    metrics = backgroundJobHealthTracker.GetSnapshot()
})).AllowAnonymous();
app.MapGet("/health/scaling", () => Results.Ok(new
{
    deployment = new
    {
        mode = deploymentTopology.Mode,
        customerCode = deploymentTopology.CustomerCode,
        customerDomain = deploymentTopology.CustomerDomain,
        customerDatabaseName = deploymentTopology.CustomerDatabaseName,
        scalingEnabled = deploymentTopology.ScalingEnabled,
        minReplicas = deploymentTopology.MinReplicas,
        maxReplicas = deploymentTopology.MaxReplicas,
        source = deploymentTopology.Source
    },
    tenantIsolation = new
    {
        enabled = tenantIsolation.Enabled,
        mode = tenantIsolation.Mode,
        tenantCode = tenantIsolation.TenantCode,
        tenantName = tenantIsolation.TenantName,
        tenantDomain = tenantIsolation.TenantDomain,
        tenantDatabaseName = tenantIsolation.TenantDatabaseName,
        configPath = string.IsNullOrWhiteSpace(tenantIsolation.TenantConfigPath) ? null : tenantIsolation.TenantConfigPath,
        isolationStrategy = tenantIsolation.IsolationStrategy,
        source = tenantIsolation.Source
    },
    autoScalingEnabled,
    minReplicas = autoScalingMinReplicas,
    maxReplicas = autoScalingMaxReplicas,
    targetCpuUtilizationPercent = autoScalingTargetCpuPercent,
    targetMemoryUtilizationPercent = autoScalingTargetMemoryPercent,
    scaleOutCooldownSeconds = autoScalingScaleOutCooldownSeconds,
    scaleInCooldownSeconds = autoScalingScaleInCooldownSeconds,
    threadPoolMinWorkerThreads = hostMinWorkerThreads,
    threadPoolMinCompletionPortThreads = hostMinCompletionPortThreads,
    maxConcurrentConnections = hostMaxConcurrentConnections,
    maxConcurrentUpgradedConnections = hostMaxConcurrentUpgradedConnections,
    http2MaxStreamsPerConnection = networkHttp2MaxStreamsPerConnection,
    outboundMaxConnectionsPerServer = networkOutboundMaxConnectionsPerServer
})).AllowAnonymous();

app.Run();
