namespace Tabsan.EduSphere.Application.Dtos;

// ─────────────────────────────────────────────────────────────────────────────
// Report Settings DTOs
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Full report definition with its role assignments.</summary>
public record ReportDefinitionDto(
    Guid Id,
    string Key,
    string Name,
    string Purpose,
    bool IsActive,
    IList<string> AssignedRoles
);

/// <summary>Payload to create a new report definition.</summary>
public record CreateReportCommand(
    string Key,
    string Name,
    string Purpose
);

/// <summary>Payload to update an existing report definition.</summary>
public record UpdateReportCommand(
    string Name,
    string Purpose
);

/// <summary>Replaces all role assignments for a report or module (pass empty list to clear).</summary>
public record SetRolesCommand(IList<string> RoleNames);

// ─────────────────────────────────────────────────────────────────────────────
// Module Role Assignment DTOs
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Module with its currently assigned roles.</summary>
public record ModuleRolesDto(
    Guid ModuleId,
    string ModuleKey,
    string ModuleName,
    IList<string> AssignedRoles
);

/// <summary>Full module settings entry: identity, activation state and assigned roles.</summary>
public record ModuleSettingsDto(
    Guid Id,
    string Key,
    string Name,
    bool IsMandatory,
    bool IsActive,
    IList<string> AssignedRoles
);

// ─────────────────────────────────────────────────────────────────────────────
// Theme DTOs
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>The currently active theme for a user.</summary>
public record UserThemeDto(string? ThemeKey);

/// <summary>Payload to set the authenticated user's theme preference.</summary>
public record SetThemeCommand(string? ThemeKey);

// ─────────────────────────────────────────────────────────────────────────────
// Sidebar Menu Settings DTOs
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>A sidebar menu item with its role access list and action permissions.</summary>
public record SidebarMenuItemDto(
    Guid     Id,
    string   Key,
    string   Name,
    string   Purpose,
    Guid?    ParentId,
    int      DisplayOrder,
    bool     IsActive,
    bool     IsSystemMenu,
    IList<SidebarMenuRoleAccessDto> RoleAccesses,
    IList<SidebarMenuItemDto>       SubMenus
)
{
    /// <summary>Current user can view this resource.</summary>
    public bool CanView { get; init; }
    /// <summary>Current user can create/add new records.</summary>
    public bool CanAdd { get; init; }
    /// <summary>Current user can edit/update records.</summary>
    public bool CanEdit { get; init; }
    /// <summary>Current user can deactivate/delete records.</summary>
    public bool CanDeactivate { get; init; }
    /// <summary>Current user can export data from this resource.</summary>
    public bool CanExport { get; init; }
    /// <summary>Current user can import data into this resource.</summary>
    public bool CanImport { get; init; }
}

/// <summary>Role access entry for a sidebar menu item.</summary>
public record SidebarMenuRoleAccessDto(
    string RoleName,
    bool   IsAllowed
);

/// <summary>Payload to replace all role access records for a menu item.</summary>
public record SetSidebarMenuRolesCommand(IList<SidebarRoleAccessEntry> Entries);

/// <summary>Single role + allowed flag pair used when updating sidebar role access.</summary>
public record SidebarRoleAccessEntry(string RoleName, bool IsAllowed);

/// <summary>Payload to toggle the active status of a sidebar menu item.</summary>
public record SetSidebarMenuStatusCommand(bool IsActive);

// ─────────────────────────────────────────────────────────────────────────────
// Portal / Dashboard Settings DTOs
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>The full set of branding values for the portal dashboard.</summary>
public record PortalBrandingDto(
    string UniversityName,
    string PortalSubtitle,
    string FooterText,
    string? LogoImage,
    string? PrivacyPolicyUrl,
    string? PrivacyPolicyContent,
    string? FontFamily,
    string? FontSize
);

/// <summary>Payload to update the portal branding settings.</summary>
public record SavePortalBrandingCommand(
    string UniversityName,
    string PortalSubtitle,
    string FooterText,
    string? LogoImage = null,
    string? PrivacyPolicyUrl = null,
    string? PrivacyPolicyContent = null,
    string? FontFamily = null,
    string? FontSize = null
);

// ─────────────────────────────────────────────────────────────────────────────
// Tenant / Subscription Operations DTOs (Phase 30 Stage 30.2)
// ─────────────────────────────────────────────────────────────────────────────

public record TenantOnboardingTemplateDto(
    string TemplateName,
    string DefaultInstitutionMode,
    string DefaultAdminRole,
    string WelcomeMessage,
    string StarterModulesCsv
);

public record SaveTenantOnboardingTemplateCommand(
    string TemplateName,
    string DefaultInstitutionMode,
    string DefaultAdminRole,
    string WelcomeMessage,
    string StarterModulesCsv
);

public record TenantSubscriptionPlanDto(
    string PlanKey,
    string PlanName,
    decimal MonthlyPrice,
    int MaxUsers,
    bool EnableLms,
    bool EnablePayments,
    bool EnableIntegrations,
    bool IsActive
);

public record SaveTenantSubscriptionPlanCommand(
    string PlanKey,
    string PlanName,
    decimal MonthlyPrice,
    int MaxUsers,
    bool EnableLms,
    bool EnablePayments,
    bool EnableIntegrations,
    bool IsActive
);

public record TenantProfileSettingsDto(
    string TenantCode,
    string TenantDisplayName,
    string SupportEmail,
    string SupportPhone,
    string TimeZone,
    string Locale,
    string CurrencyCode,
    string BrandingTheme
);

public record SaveTenantProfileSettingsCommand(
    string TenantCode,
    string TenantDisplayName,
    string SupportEmail,
    string SupportPhone,
    string TimeZone,
    string Locale,
    string CurrencyCode,
    string BrandingTheme
);

// ─────────────────────────────────────────────────────────────────────────────
// Reliability / Rollback DTOs (Phase 30 Stage 30.3)
// ─────────────────────────────────────────────────────────────────────────────

public record FeatureFlagDto(
    string Key,
    bool IsEnabled,
    string? Description,
    DateTime UpdatedAtUtc
);

public record SaveFeatureFlagCommand(
    string Key,
    bool IsEnabled,
    string? Description = null
);

public record RollbackFeatureFlagsCommand(
    IList<string> Keys,
    string? Reason = null
);
