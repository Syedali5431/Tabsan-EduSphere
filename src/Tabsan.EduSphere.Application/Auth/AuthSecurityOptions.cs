namespace Tabsan.EduSphere.Application.Auth;

// Phase 27.2 — Authentication and Security UX configuration contract.
public sealed class AuthSecurityOptions
{
    public const string SectionName = "AuthSecurity";

    public MfaSettings Mfa { get; init; } = new();
    public SsoSettings Sso { get; init; } = new();
    public SessionRiskSettings SessionRisk { get; init; } = new();
}

public sealed class MfaSettings
{
    public bool Enabled { get; init; }
    public bool RequireForPasswordLogin { get; init; }
    public bool RequireForPrivilegedRolesOnly { get; init; } = true;
    public string[] PrivilegedRoles { get; init; } = ["SuperAdmin", "Admin"];
    public string TotpIssuer { get; init; } = "Tabsan EduSphere";
    public int TotpDigits { get; init; } = 6;
    public int TotpStepSeconds { get; init; } = 30;
    public int TotpAllowedDriftWindows { get; init; } = 1;
    public int RecoveryCodeCount { get; init; } = 8;
}

public sealed class SsoSettings
{
    public bool Enabled { get; init; }
    public string Provider { get; init; } = "";
    public string LoginUrl { get; init; } = "";
}

public sealed class SessionRiskSettings
{
    public bool Enabled { get; init; } = true;
    public bool BlockHighRiskLogin { get; init; } = true;
    public bool AuditMediumRiskLogin { get; init; } = true;
}
