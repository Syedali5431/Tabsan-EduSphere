using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Modules;
using Tabsan.EduSphere.Domain.Settings;
using Tabsan.EduSphere.Domain.Tenancy;
using Tabsan.EduSphere.Infrastructure.Modules;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.Infrastructure.Persistence;

/// <summary>
/// Runs on application startup to ensure the database contains required seed data.
/// Idempotent — all inserts are guarded by existence checks so re-running is safe.
///
/// Seed order:
/// 1. Roles         (lookup rows — must exist before Users)
/// 2. Modules       (feature module definitions)
/// 3. ModuleStatus  (one status row per module)
/// 4. Super Admin   (bootstrap account from environment variables)
/// </summary>
public static class DatabaseSeeder
{
    private const string DefaultTenantCode = "DEFAULT";
    private const string DefaultTenantName = "Default Tenant";
    private const string DefaultCampusCode = "MAIN";
    private const string DefaultCampusName = "Main Campus";

    /// <summary>
    /// Entry point called from Program.cs after EF migrations have been applied.
    /// Resolves all dependencies from the DI container via a scoped service provider.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // Retry once on "Database already exists" (SQL error 1801), which can happen
        // when two processes race to create the same LocalDB database in test scenarios.
        // On retry MigrateAsync is idempotent because __EFMigrationsHistory is checked.
        try
        {
            await db.Database.MigrateAsync();
        }
        catch (SqlException ex) when (ex.Number == 1801)
        {
            // Another process won the race and created the DB; wait briefly then migrate
            // (which will be a no-op since all migrations are already recorded).
            await Task.Delay(2000);
            await db.Database.MigrateAsync();
        }

        await SeedRolesAsync(db);
        await SeedModulesAsync(db);
        var (defaultTenantId, defaultCampusId) = await EnsureDefaultTenantCampusAsync(db);
        await SeedSuperAdminAsync(db, hasher, defaultTenantId, defaultCampusId);
        await SeedAcademicDocumentTemplatesAsync(db);
        await SeedSidebarMenusAsync(db);
        await SeedReportDefinitionsAsync(db);
        await EnsureTenantCampusBackfillAsync(db, defaultTenantId, defaultCampusId);

        await db.SaveChangesAsync();
    }

    // ── Tenant and Campus Defaults (Phase 2) ─────────────────────────────────

    private static async Task<(Guid TenantId, Guid CampusId)> EnsureDefaultTenantCampusAsync(ApplicationDbContext db)
    {
        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Code == DefaultTenantCode);

        if (tenant is null)
        {
            tenant = new Tenant(DefaultTenantCode, DefaultTenantName);
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        else
        {
            if (tenant.IsDeleted)
                tenant.Restore();
            if (!tenant.IsActive)
                tenant.Activate();
        }

        var campus = await db.Campuses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == tenant.Id && c.Code == DefaultCampusCode);

        if (campus is null)
        {
            campus = new Campus(tenant.Id, DefaultCampusCode, DefaultCampusName);
            db.Campuses.Add(campus);
            await db.SaveChangesAsync();
        }
        else
        {
            if (campus.IsDeleted)
                campus.Restore();
            if (!campus.IsActive)
                campus.Activate();
        }

        return (tenant.Id, campus.Id);
    }

    private static async Task EnsureTenantCampusBackfillAsync(
        ApplicationDbContext db,
        Guid defaultTenantId,
        Guid defaultCampusId)
    {
        await db.Users
            .Where(u => u.TenantId == null || u.CampusId == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.TenantId, u => u.TenantId ?? defaultTenantId)
                .SetProperty(u => u.CampusId, u => u.CampusId ?? defaultCampusId));

        await db.Departments
            .Where(d => d.TenantId == null || d.CampusId == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.TenantId, d => d.TenantId ?? defaultTenantId)
                .SetProperty(d => d.CampusId, d => d.CampusId ?? defaultCampusId));
    }

    private static async Task SeedAcademicDocumentTemplatesAsync(ApplicationDbContext db)
    {
        if (!await TableExistsAsync(db, "academic_document_templates"))
            return;

        var existing = await db.AcademicDocumentTemplates
            .IgnoreQueryFilters()
            .Select(t => new { t.TemplateType, t.Version })
            .ToListAsync();

        var seed = new[]
        {
            AcademicDocumentTemplate.Create(AcademicDocumentTemplateType.Degree, "Default Degree Template", "default"),
            AcademicDocumentTemplate.Create(AcademicDocumentTemplateType.Transcript, "Default Transcript Template", "default")
        };

        foreach (var template in seed)
        {
            if (!existing.Any(x => x.TemplateType == template.TemplateType && x.Version == template.Version))
            {
                db.AcademicDocumentTemplates.Add(template);
            }
        }
    }

    private static async Task<bool> TableExistsAsync(ApplicationDbContext db, string tableName)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;

        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT CASE WHEN OBJECT_ID(@tableName, 'U') IS NULL THEN 0 ELSE 1 END";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@tableName";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) == 1;
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    // ── Roles ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Inserts the system roles if they do not already exist.
    /// Roles use an int PK so their IDs are stable across environments.
    /// </summary>
    private static async Task SeedRolesAsync(ApplicationDbContext db)
    {
        var existing = await db.Roles.Select(r => r.Name).ToListAsync();

        var seed = new[]
        {
            new Role("SuperAdmin", "Full platform access — manages license and all settings.", isSystemRole: true),
            new Role("Admin",      "Department-level admin — manages users and courses.",       isSystemRole: true),
            new Role("Faculty",    "Teaches courses and manages academic content.",              isSystemRole: true),
            new Role("Student",    "Enrolled student — accesses course and academic content.",  isSystemRole: true),
            new Role("Finance",    "Handles payment operations, payment tracking, and finance reporting.", isSystemRole: true),
        };

        foreach (var role in seed)
        {
            if (!existing.Contains(role.Name))
                db.Roles.Add(role);
        }
    }

    // ── Modules ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Inserts all known module definitions and creates a default ModuleStatus row
    /// for each one. Mandatory modules are activated by default; optional modules
    /// start inactive so the Super Admin explicitly enables them.
    /// </summary>
    private static async Task SeedModulesAsync(ApplicationDbContext db)
    {
        var existingKeys = await db.Modules.Select(m => m.Key).ToListAsync();

        var definitions = new[]
        {
            // key,                      name,                   isMandatory
            (KnownModuleKeys.Authentication, "Authentication",           true),
            (KnownModuleKeys.Departments,    "Departments",              true),
            (KnownModuleKeys.Sis,            "Student Information",      true),
            (KnownModuleKeys.Courses,        "Courses",                  false),
            (KnownModuleKeys.Assignments,    "Assignments",              false),
            (KnownModuleKeys.Quizzes,        "Quizzes",                  false),
            (KnownModuleKeys.Attendance,     "Attendance",               false),
            (KnownModuleKeys.Results,        "Results / Grades",         false),
            (KnownModuleKeys.Notifications,  "Notifications",            false),
            (KnownModuleKeys.Fyp,            "Final Year Projects",      false),
            (KnownModuleKeys.AiChat,         "AI Chatbot",               false),
            (KnownModuleKeys.Reports,        "Reports",                  false),
            (KnownModuleKeys.Themes,         "UI Themes",                false),
            (KnownModuleKeys.AdvancedAudit,  "Advanced Audit Logging",   false),
        };

        foreach (var (key, name, mandatory) in definitions)
        {
            if (existingKeys.Contains(key)) continue;

            var module = new Module(key, name, mandatory);
            db.Modules.Add(module);

            // Create a matching status row.
            // Mandatory modules start active; optional modules start inactive.
            var status = new ModuleStatus(module.Id, mandatory, source: mandatory ? "mandatory" : "seed");
            db.ModuleStatuses.Add(status);
        }
    }

    // ── Super Admin Bootstrap ─────────────────────────────────────────────────

    /// <summary>
    /// Creates the initial Super Admin account from environment variables:
    ///   TABSAN_SUPER_USERNAME   — username (defaults to "superadmin")
    ///   TABSAN_SUPER_PASSWORD   — plain-text password (REQUIRED in production)
    ///   TABSAN_SUPER_EMAIL      — optional email address
    ///
    /// The account is only created when no user with the SuperAdmin role exists,
    /// so this is safe to run on every startup.
    /// </summary>
    private static async Task SeedSuperAdminAsync(
        ApplicationDbContext db,
        IPasswordHasher hasher,
        Guid defaultTenantId,
        Guid defaultCampusId)
    {
        var superAdminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
        if (superAdminRole is null) return; // roles not seeded yet — will run on next start

        var alreadyExists = await db.Users.AnyAsync(u => u.RoleId == superAdminRole.Id);
        if (alreadyExists) return;

        var username = Environment.GetEnvironmentVariable("TABSAN_SUPER_USERNAME") ?? "superadmin";
        var password = Environment.GetEnvironmentVariable("TABSAN_SUPER_PASSWORD")
            ?? throw new InvalidOperationException(
                "TABSAN_SUPER_PASSWORD environment variable is required for initial seeding.");
        var email    = Environment.GetEnvironmentVariable("TABSAN_SUPER_EMAIL");

        var hash = hasher.Hash(password);
        var superAdmin = new User(
            username,
            hash,
            superAdminRole.Id,
            tenantId: defaultTenantId,
            campusId: defaultCampusId);

        if (!string.IsNullOrWhiteSpace(email))
            superAdmin.UpdateEmail(email);

        db.Users.Add(superAdmin);
    }

    // ── Sidebar Menu Items ────────────────────────────────────────────────────

    /// <summary>
    /// Seeds and synchronizes the default sidebar navigation structure.
    /// Idempotent — missing items are added and soft-deleted items are restored.
    /// </summary>
    private static async Task SeedSidebarMenusAsync(ApplicationDbContext db)
    {
        // ── Helper: upsert by key ─────────────────────────────────────────────
        async Task<SidebarMenuItem> Upsert(
            string key, string label, string description, int order,
            Guid? parentId = null, bool isSystemMenu = false)
        {
            var existing = await db.SidebarMenuItems
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Key == key);
            if (existing is not null)
            {
                if (existing.IsDeleted)
                    existing.Restore();

                if (!string.Equals(existing.Name, label, StringComparison.Ordinal)
                    || !string.Equals(existing.Purpose, description, StringComparison.Ordinal)
                    || existing.DisplayOrder != order)
                {
                    existing.Update(label, description, order);
                }

                db.SidebarMenuItems.Update(existing);
                return existing;
            }

            var item = new SidebarMenuItem(key, label, description, order, parentId: parentId, isSystemMenu: isSystemMenu);
            db.SidebarMenuItems.Add(item);
            return item;
        }

        void EnsureRoleAccess(Guid itemId, string role, bool isAllowed)
        {
            var local = db.SidebarMenuRoleAccesses.Local
                .FirstOrDefault(r => r.SidebarMenuItemId == itemId && r.RoleName == role);
            if (local is not null)
            {
                if (local.IsAllowed != isAllowed)
                    local.SetAllowed(isAllowed);
                return;
            }

            var existing = db.SidebarMenuRoleAccesses
                .FirstOrDefault(r => r.SidebarMenuItemId == itemId && r.RoleName == role);
            if (existing is not null)
            {
                if (existing.IsAllowed != isAllowed)
                    existing.SetAllowed(isAllowed);
                return;
            }

            db.SidebarMenuRoleAccesses.Add(new SidebarMenuRoleAccess(itemId, role, isAllowed));
        }

        // ── Top-level menus ──────────────────────────────────────────────────
        var dashboard        = await Upsert("dashboard",         "Dashboard",          "Main landing page",                               1);
        var timetableAdmin   = await Upsert("timetable_admin",   "Timetable Admin",    "Manage timetables (Admin/SuperAdmin)",             2);
        var timetableFaculty = await Upsert("timetable_teacher", "Teacher Timetable",  "View own teaching timetable",                     3);
        var timetableStudent = await Upsert("timetable_student", "Student Timetable",  "View own class timetable",                        4);
        var lookups          = await Upsert("lookups",           "Lookups",            "Reference data management (Admin/SuperAdmin)",    5, isSystemMenu: false);
        var systemSettings   = await Upsert("system_settings",   "System Settings",   "Platform configuration — SuperAdmin only",        6, isSystemMenu: true);
        var resultCalculation= await Upsert("result_calculation", "Result Calculation", "Configure GPA scale and assessment weights",      7, isSystemMenu: false);
        var notifications    = await Upsert("notifications",     "Notifications",      "View system and academic notifications",           8);
        var students         = await Upsert("students",          "Students",           "Manage student profiles",                         9);
        var userImport       = await Upsert("user_import",       "User Import",        "Bulk-import user accounts from CSV",             10);
        var departments      = await Upsert("departments",       "Departments",        "Manage academic departments",                    10);
        var programs         = await Upsert("programs",          "Programs",           "Manage degree programs",                         11);
        var courses          = await Upsert("courses",           "Courses",            "Manage courses and offerings",                   12);
        var prerequisites    = await Upsert("prerequisites",     "Prerequisites",      "Configure course prerequisite rules",            13);
        var assignments      = await Upsert("assignments",       "Assignments",        "Manage and submit assignments",                  14);
        var attendance       = await Upsert("attendance",        "Attendance",         "Record and view attendance",                     15);
        var enterAttendance  = await Upsert("enter_attendance",  "Enter Attendance",   "Manual attendance entry and CSV import workflow", 15);
        var results          = await Upsert("results",           "Results",            "View and publish academic results",              16);
        var enterResults     = await Upsert("enter_results",     "Enter Results",      "Scoped result entry workflow for faculty and admins", 16);
        var gradebook        = await Upsert("gradebook",         "Gradebook",          "Review and publish gradebook entries",           17);
        var rubricManage     = await Upsert("rubric_manage",     "Rubric Management",  "Manage grading rubrics",                         18);
        var quizzes          = await Upsert("quizzes",           "Quizzes",            "Manage and attempt quizzes",                     19);
        var fyp              = await Upsert("fyp",               "FYP",                "Final Year Projects management",                 20);
        var analytics        = await Upsert("analytics",         "Analytics",          "Academic analytics and dashboards",              21);
        var aiChat           = await Upsert("ai_chat",           "AI Chat",            "AI-powered academic assistant",                  22);
        var studentLifecycle = await Upsert("student_lifecycle", "Student Lifecycle",  "Manage promotions, holds and withdrawals",       23);
        var helpdesk         = await Upsert("helpdesk",          "Helpdesk",           "Raise and track support issues",                 24);
        var payments         = await Upsert("payments",          "Payments",           "Manage and view fee payment records",            25);
        var enrollments      = await Upsert("enrollments",       "Enrollments",        "Manage course enrollments and rosters",          26);
        var reportCenter     = await Upsert("report_center",     "Report Center",      "Generate and export academic reports",           27);
        var generateCertificates = await Upsert("generate_certificates", "Generate Certificates", "Generate degree/transcript documents for graduated university students", 28);
        var adminUsers       = await Upsert("admin_users",       "Admin Users",        "Manage admin accounts and department assignments", 29);

        await db.SaveChangesAsync(); // ensure IDs are set before use as parentId

        // ── Sub-menus of Lookups ─────────────────────────────────────────────
        var buildings = await Upsert("buildings", "Buildings", "Manage campus buildings",       1, parentId: lookups.Id);
        var rooms     = await Upsert("rooms",     "Rooms",     "Manage rooms within buildings", 2, parentId: lookups.Id);

        // ── Sub-menus of System Settings ─────────────────────────────────────
        var reportSettings      = await Upsert("report_settings",      "Report Settings",      "Configure report definitions",        1, parentId: systemSettings.Id, isSystemMenu: true);
        var moduleComposition   = await Upsert("module_composition",   "Module Composition",   "Manage module activation and role visibility", 2, parentId: systemSettings.Id, isSystemMenu: true);
        var sidebarSettings     = await Upsert("sidebar_settings",     "Sidebar Settings",     "Control sidebar visibility per role", 3, parentId: systemSettings.Id, isSystemMenu: true);
        var themeSettings       = await Upsert("theme_settings",       "Theme Settings",       "Choose the portal colour theme",      4, parentId: systemSettings.Id, isSystemMenu: false);
        var licenseUpdate       = await Upsert("license_update",       "License Update",       "Upload and review the product license", 5, parentId: systemSettings.Id, isSystemMenu: true);
        var dashboardSettings   = await Upsert("dashboard_settings",   "Dashboard Settings",   "Customise portal branding and name",  6, parentId: systemSettings.Id, isSystemMenu: true);
        var institutionPolicy   = await Upsert("institution_policy",   "Institution Policy",   "Configure enabled institution types", 7, parentId: systemSettings.Id, isSystemMenu: true);
        var tenantManagement    = await Upsert("tenant_management",    "Tenant Management",    "Manage tenants and activation status", 8, parentId: systemSettings.Id, isSystemMenu: true);
        var campusManagement    = await Upsert("campus_management",    "Campus Management",    "Manage campuses and activation status", 9, parentId: systemSettings.Id, isSystemMenu: true);
        var libraryConfig       = await Upsert("library_config",       "Library Config",       "Configure external library integration", 10, parentId: systemSettings.Id, isSystemMenu: true);
        var accreditation       = await Upsert("accreditation",        "Accreditation",        "Manage accreditation templates and exports", 11, parentId: systemSettings.Id, isSystemMenu: true);

        // Remove legacy Module Settings menu if present from older seeds.
        var legacyModuleSettings = await db.SidebarMenuItems
            .IgnoreQueryFilters()
            .Include(m => m.RoleAccesses)
            .FirstOrDefaultAsync(m => m.Key == "module_settings");
        if (legacyModuleSettings is not null)
        {
            foreach (var access in legacyModuleSettings.RoleAccesses)
                access.SetAllowed(false);

            if (!legacyModuleSettings.IsDeleted)
                legacyModuleSettings.SoftDelete();

            db.SidebarMenuItems.Update(legacyModuleSettings);
        }

        await db.SaveChangesAsync(); // flush new items before adding role rows

        // ── Default role access ───────────────────────────────────────────────
        // Dashboard: all roles
        foreach (var role in new[] { "Admin", "Faculty", "Student" })
            EnsureRoleAccess(dashboard.Id, role, isAllowed: true);

        // Timetable Admin: Admin only
        EnsureRoleAccess(timetableAdmin.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(timetableAdmin.Id, "Faculty", isAllowed: false);
        EnsureRoleAccess(timetableAdmin.Id, "Student", isAllowed: false);

        // Teacher Timetable: Faculty only
        EnsureRoleAccess(timetableFaculty.Id, "Admin",   isAllowed: false);
        EnsureRoleAccess(timetableFaculty.Id, "Faculty", isAllowed: true);
        EnsureRoleAccess(timetableFaculty.Id, "Student", isAllowed: false);

        // Student Timetable: Student only
        EnsureRoleAccess(timetableStudent.Id, "Admin",   isAllowed: false);
        EnsureRoleAccess(timetableStudent.Id, "Faculty", isAllowed: false);
        EnsureRoleAccess(timetableStudent.Id, "Student", isAllowed: true);

        // Lookups + sub-menus: Admin only
        foreach (var id in new[] { lookups.Id, buildings.Id, rooms.Id })
        {
            EnsureRoleAccess(id, "Admin",   isAllowed: true);
            EnsureRoleAccess(id, "Faculty", isAllowed: false);
            EnsureRoleAccess(id, "Student", isAllowed: false);
        }

        EnsureRoleAccess(resultCalculation.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(resultCalculation.Id, "Faculty", isAllowed: false);
        EnsureRoleAccess(resultCalculation.Id, "Student", isAllowed: false);

        // System Settings + sub-menus: SuperAdmin only; other roles explicitly false
        foreach (var id in new[]
        {
            systemSettings.Id,
            reportSettings.Id,
            moduleComposition.Id,
            sidebarSettings.Id,
            licenseUpdate.Id,
            dashboardSettings.Id,
            institutionPolicy.Id,
            tenantManagement.Id,
            campusManagement.Id,
            libraryConfig.Id,
            accreditation.Id
        })
        {
            EnsureRoleAccess(id, "Admin",   isAllowed: false);
            EnsureRoleAccess(id, "Faculty", isAllowed: false);
            EnsureRoleAccess(id, "Student", isAllowed: false);
        }

        // Theme Settings: all roles may access (personal preference)
        foreach (var role in new[] { "Admin", "Faculty", "Student" })
            EnsureRoleAccess(themeSettings.Id, role, isAllowed: true);

        // Notifications: all roles
        foreach (var role in new[] { "Admin", "Faculty", "Student" })
            EnsureRoleAccess(notifications.Id, role, isAllowed: true);

        // Students: Admin + Faculty (view); not Student
        EnsureRoleAccess(students.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(students.Id, "Faculty", isAllowed: true);
        EnsureRoleAccess(students.Id, "Student", isAllowed: false);

        // User import: Admin only
        EnsureRoleAccess(userImport.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(userImport.Id, "Faculty", isAllowed: false);
        EnsureRoleAccess(userImport.Id, "Student", isAllowed: false);

        // Departments: Admin only
        EnsureRoleAccess(departments.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(departments.Id, "Faculty", isAllowed: false);
        EnsureRoleAccess(departments.Id, "Student", isAllowed: false);

        // Programs: Admin only
        EnsureRoleAccess(programs.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(programs.Id, "Faculty", isAllowed: false);
        EnsureRoleAccess(programs.Id, "Student", isAllowed: false);

        // Courses: Admin + Faculty
        EnsureRoleAccess(courses.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(courses.Id, "Faculty", isAllowed: true);
        EnsureRoleAccess(courses.Id, "Student", isAllowed: false);

        // Prerequisites: Admin only
        EnsureRoleAccess(prerequisites.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(prerequisites.Id, "Faculty", isAllowed: false);
        EnsureRoleAccess(prerequisites.Id, "Student", isAllowed: false);

        // Assignments: Faculty + Student
        EnsureRoleAccess(assignments.Id, "Admin",   isAllowed: false);
        EnsureRoleAccess(assignments.Id, "Faculty", isAllowed: true);
        EnsureRoleAccess(assignments.Id, "Student", isAllowed: true);

        // Attendance: Faculty (record) + Student (view)
        EnsureRoleAccess(attendance.Id, "Admin",   isAllowed: false);
        EnsureRoleAccess(attendance.Id, "Faculty", isAllowed: true);
        EnsureRoleAccess(attendance.Id, "Student", isAllowed: true);

        // Enter Attendance: Admin + Faculty; SuperAdmin accesses via override; students are excluded.
        EnsureRoleAccess(enterAttendance.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(enterAttendance.Id, "Faculty", isAllowed: true);
        EnsureRoleAccess(enterAttendance.Id, "Student", isAllowed: false);

        // Enter Results: Admin + Faculty; SuperAdmin accesses via override; students are excluded.
        EnsureRoleAccess(enterResults.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(enterResults.Id, "Faculty", isAllowed: true);
        EnsureRoleAccess(enterResults.Id, "Student", isAllowed: false);

        // Results: Admin + Faculty + Student
        foreach (var role in new[] { "Admin", "Faculty", "Student" })
            EnsureRoleAccess(results.Id, role, isAllowed: true);

        // Gradebook and rubric management: Admin + Faculty
        EnsureRoleAccess(gradebook.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(gradebook.Id, "Faculty", isAllowed: true);
        EnsureRoleAccess(gradebook.Id, "Student", isAllowed: false);

        EnsureRoleAccess(rubricManage.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(rubricManage.Id, "Faculty", isAllowed: true);
        EnsureRoleAccess(rubricManage.Id, "Student", isAllowed: false);

        // Quizzes: Faculty + Student
        EnsureRoleAccess(quizzes.Id, "Admin",   isAllowed: false);
        EnsureRoleAccess(quizzes.Id, "Faculty", isAllowed: true);
        EnsureRoleAccess(quizzes.Id, "Student", isAllowed: true);

        // FYP: Faculty + Student
        EnsureRoleAccess(fyp.Id, "Admin",   isAllowed: false);
        EnsureRoleAccess(fyp.Id, "Faculty", isAllowed: true);
        EnsureRoleAccess(fyp.Id, "Student", isAllowed: true);

        // Analytics: Admin + Faculty
        EnsureRoleAccess(analytics.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(analytics.Id, "Faculty", isAllowed: true);
        EnsureRoleAccess(analytics.Id, "Student", isAllowed: false);

        // AI Chat: Faculty + Student
        EnsureRoleAccess(aiChat.Id, "Admin",   isAllowed: false);
        EnsureRoleAccess(aiChat.Id, "Faculty", isAllowed: true);
        EnsureRoleAccess(aiChat.Id, "Student", isAllowed: true);

        // Student Lifecycle: Admin only
        EnsureRoleAccess(studentLifecycle.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(studentLifecycle.Id, "Faculty", isAllowed: false);
        EnsureRoleAccess(studentLifecycle.Id, "Student", isAllowed: false);

        // Helpdesk: all roles
        EnsureRoleAccess(helpdesk.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(helpdesk.Id, "Faculty", isAllowed: true);
        EnsureRoleAccess(helpdesk.Id, "Student", isAllowed: true);

        // Payments: Admin + Student
        EnsureRoleAccess(payments.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(payments.Id, "Faculty", isAllowed: false);
        EnsureRoleAccess(payments.Id, "Student", isAllowed: true);
        EnsureRoleAccess(payments.Id, "Finance", isAllowed: true);

        // Enrollments: Admin + Faculty
        EnsureRoleAccess(enrollments.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(enrollments.Id, "Faculty", isAllowed: true);
        EnsureRoleAccess(enrollments.Id, "Student", isAllowed: false);

        // Report Center: Admin + Faculty + Student
        EnsureRoleAccess(reportCenter.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(reportCenter.Id, "Faculty", isAllowed: true);
        EnsureRoleAccess(reportCenter.Id, "Student", isAllowed: true);
        EnsureRoleAccess(reportCenter.Id, "Finance", isAllowed: true);

        // Generate Certificates: Admin + Faculty (view), no student/finance access by default.
        EnsureRoleAccess(generateCertificates.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(generateCertificates.Id, "Faculty", isAllowed: true);
        EnsureRoleAccess(generateCertificates.Id, "Student", isAllowed: false);
        EnsureRoleAccess(generateCertificates.Id, "Finance", isAllowed: false);

        // Analytics: Admin + Faculty + Finance
        EnsureRoleAccess(analytics.Id, "Admin",   isAllowed: true);
        EnsureRoleAccess(analytics.Id, "Faculty", isAllowed: true);
        EnsureRoleAccess(analytics.Id, "Student", isAllowed: false);
        EnsureRoleAccess(analytics.Id, "Finance", isAllowed: true);

        // Theme Settings: allow Finance to personalize theme without broader settings access
        EnsureRoleAccess(themeSettings.Id, "Finance", isAllowed: true);

        // Admin Users and super-admin settings menus are hidden for Finance by default.
        EnsureRoleAccess(adminUsers.Id, "Finance", isAllowed: false);
        foreach (var id in new[]
        {
            reportSettings.Id,
            moduleComposition.Id,
            sidebarSettings.Id,
            licenseUpdate.Id,
            dashboardSettings.Id,
            institutionPolicy.Id,
            tenantManagement.Id,
            campusManagement.Id,
            libraryConfig.Id,
            accreditation.Id
        })
        {
            EnsureRoleAccess(id, "Finance", isAllowed: false);
        }

        EnsureRoleAccess(userImport.Id, "Finance", isAllowed: false);
        EnsureRoleAccess(programs.Id, "Finance", isAllowed: false);
        EnsureRoleAccess(prerequisites.Id, "Finance", isAllowed: false);
        EnsureRoleAccess(gradebook.Id, "Finance", isAllowed: false);
        EnsureRoleAccess(rubricManage.Id, "Finance", isAllowed: false);
        EnsureRoleAccess(helpdesk.Id, "Finance", isAllowed: true);

        // Admin Users: SuperAdmin management view only.
        EnsureRoleAccess(adminUsers.Id, "Admin", isAllowed: false);
        EnsureRoleAccess(adminUsers.Id, "Faculty", isAllowed: false);
        EnsureRoleAccess(adminUsers.Id, "Student", isAllowed: false);

        await db.SaveChangesAsync();
    }

    // ── Report Definitions ─────────────────────────────────────────────────────

    /// <summary>
    /// Seeds the five standard Phase 12 report definitions and their default role assignments.
    /// Idempotent — guarded by key lookup.
    /// </summary>
    private static async Task SeedReportDefinitionsAsync(ApplicationDbContext db)
    {
        var existingKeys = await db.ReportDefinitions.Select(r => r.Key).ToListAsync();

        var definitions = new[]
        {
            (ReportKeys.AttendanceSummary,    "Attendance Summary",        "Per-student attendance percentage per course offering, filterable by semester and department."),
            (ReportKeys.ResultSummary,        "Result Summary",            "All published result entries with marks and percentage, filterable by semester, offering, or student."),
            (ReportKeys.GpaReport,            "GPA & CGPA Report",         "Per-student current semester GPA and cumulative CGPA, filterable by department and program."),
            (ReportKeys.EnrollmentSummary,    "Enrollment Summary",        "Course offering seat utilisation showing enrolled count versus maximum capacity."),
            (ReportKeys.SemesterResults,      "Semester Results",          "Full published result set for a selected semester with optional department filter."),
            (ReportKeys.StudentTranscript,    "Student Transcript",        "Full academic record for a selected student including all result components."),
            (ReportKeys.LowAttendanceWarning, "Low Attendance Warning",    "Students whose attendance falls below a configurable threshold."),
            (ReportKeys.FypStatus,            "FYP Status Report",         "Final Year Project status overview filterable by department and project status."),
            (ReportKeys.PaymentSummary,       "Payment Summary",           "Finance-ready payment receipts with month, department, course, level, and semester filters."),
        };

        foreach (var (key, name, purpose) in definitions)
        {
            if (existingKeys.Contains(key))
                continue;

            var report = new ReportDefinition(key, name, purpose);
            db.ReportDefinitions.Add(report);
            // Default role assignments: SuperAdmin, Admin and Faculty can view all reports
            db.Set<ReportRoleAssignment>().Add(new ReportRoleAssignment(report.Id, "SuperAdmin"));
            db.Set<ReportRoleAssignment>().Add(new ReportRoleAssignment(report.Id, "Admin"));
            if (key != ReportKeys.PaymentSummary)
                db.Set<ReportRoleAssignment>().Add(new ReportRoleAssignment(report.Id, "Faculty"));
            if (key == ReportKeys.PaymentSummary)
                db.Set<ReportRoleAssignment>().Add(new ReportRoleAssignment(report.Id, "Finance"));
            // Student Transcript is also accessible by students
            if (key == ReportKeys.StudentTranscript)
                db.Set<ReportRoleAssignment>().Add(new ReportRoleAssignment(report.Id, "Student"));
        }
    }
}
