using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Tabsan.EduSphere.IntegrationTests.Infrastructure;

namespace Tabsan.EduSphere.IntegrationTests;

/// <summary>
/// Integration tests that codify the sidebar role-visibility matrix and mutation behaviour.
/// A single <see cref="EduSphereWebFactory"/> is shared across all tests in this class;
/// mutating tests restore the original state in their finally-blocks so later tests
/// always start from the seeded baseline.
/// </summary>
[Collection(EduSphereCollection.Name)]
public class SidebarMenuIntegrationTests : IAsyncLifetime
{
    private readonly EduSphereWebFactory _factory;
    private readonly Dictionary<string, bool> _originalModuleStates = new(StringComparer.OrdinalIgnoreCase);

    private static readonly string[] SidebarControlledModuleKeys =
    {
        "departments",
        "sis",
        "courses",
        "assignments",
        "attendance",
        "results",
        "quizzes",
        "fyp",
        "notifications",
        "ai_chat",
        "reports",
        "themes"
    };

    public SidebarMenuIntegrationTests(EduSphereWebFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        using var superClient = CreateClient("SuperAdmin");

        foreach (var moduleKey in SidebarControlledModuleKeys)
        {
            var isActive = await GetModuleStatusAsync(superClient, moduleKey);
            _originalModuleStates[moduleKey] = isActive;

            if (!isActive)
                await SetModuleStatusAsync(superClient, moduleKey, isActive: true);
        }
    }

    public async Task DisposeAsync()
    {
        using var superClient = CreateClient("SuperAdmin");

        foreach (var kv in _originalModuleStates)
        {
            await SetModuleStatusAsync(superClient, kv.Key, kv.Value);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private HttpClient CreateClient(string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(role));
        return client;
    }

    private static async Task<List<MenuDto>> GetVisibleAsync(HttpClient client)
    {
        var response = await client.GetAsync("api/v1/sidebar-menu/my-visible");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<MenuDto>>()
               ?? new List<MenuDto>();
    }

    private static async Task<ReportCatalogDto> GetReportCatalogAsync(HttpClient client)
    {
        var response = await client.GetAsync("api/v1/reports");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ReportCatalogDto>()
               ?? new ReportCatalogDto();
    }

    private static async Task<bool> GetModuleStatusAsync(HttpClient client, string moduleKey)
    {
        var response = await client.GetAsync($"api/v1/module/{moduleKey}/status");
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("isActive").GetBoolean();
    }

    private static async Task SetModuleStatusAsync(HttpClient client, string moduleKey, bool isActive)
    {
        var endpoint = isActive
            ? $"api/v1/module/{moduleKey}/activate"
            : $"api/v1/module/{moduleKey}/deactivate";

        var response = await client.PostAsync(endpoint, content: null);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Flattens top-level + sub-menu keys into a single set.</summary>
    private static HashSet<string> FlatKeys(IEnumerable<MenuDto> menus)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in menus)
        {
            keys.Add(m.Key);
            foreach (var s in m.SubMenus)
                keys.Add(s.Key);
        }
        return keys;
    }

    /// <summary>
    /// Flattens top-level + sub-menu items (preserving their full <see cref="MenuDto"/>)
    /// so tests can look up a menu by key regardless of nesting depth.
    /// </summary>
    private static IEnumerable<MenuDto> FlatItems(IEnumerable<MenuDto> menus)
    {
        foreach (var m in menus)
        {
            yield return m;
            foreach (var s in m.SubMenus)
                yield return s;
        }
    }

    // ── Role matrix ───────────────────────────────────────────────────────────

    /// <summary>SuperAdmin should see every seeded menu item (29 keys total — all top-level and sub-menus).</summary>
    [Fact]
    public async Task GetVisible_SuperAdmin_ReturnsAllMenus()
    {
        using var client = CreateClient("SuperAdmin");
        var menus = await GetVisibleAsync(client);
        var keys  = FlatKeys(menus);

        Assert.Contains("dashboard",           keys);
        Assert.Contains("timetable_admin",     keys);
        Assert.Contains("timetable_teacher",   keys);
        Assert.Contains("timetable_student",   keys);
        Assert.Contains("lookups",             keys);
        Assert.Contains("buildings",           keys);
        Assert.Contains("rooms",               keys);
        Assert.Contains("system_settings",     keys);
        Assert.Contains("report_settings",     keys);
        Assert.Contains("sidebar_settings",    keys);
        Assert.Contains("theme_settings",      keys);
        Assert.Contains("license_update",      keys);
        Assert.Contains("dashboard_settings",  keys);
        Assert.Contains("result_calculation",  keys);
        Assert.Contains("notifications",       keys);
        Assert.Contains("students",            keys);
        Assert.Contains("departments",         keys);
        Assert.Contains("courses",             keys);
        Assert.Contains("assignments",         keys);
        Assert.Contains("attendance",          keys);
        Assert.Contains("results",             keys);
        Assert.Contains("quizzes",             keys);
        Assert.Contains("fyp",                 keys);
        Assert.Contains("analytics",           keys);
        Assert.Contains("ai_chat",             keys);
        Assert.Contains("student_lifecycle",   keys);
        Assert.Contains("payments",            keys);
        Assert.Contains("enrollments",         keys);
        Assert.Contains("report_center",       keys);
        Assert.Equal(30, keys.Count);
    }

    /// <summary>Admin should see all menus with IsAllowed=true for Admin role (19 total).</summary>
    [Fact]
    public async Task GetVisible_Admin_ReturnsAdminMenusOnly()
    {
        using var client = CreateClient("Admin");
        var menus = await GetVisibleAsync(client);
        var keys  = FlatKeys(menus);

        Assert.Contains("dashboard",          keys);
        Assert.Contains("timetable_admin",    keys);
        Assert.Contains("lookups",            keys);
        Assert.Contains("buildings",          keys);
        Assert.Contains("rooms",              keys);
        Assert.Contains("theme_settings",     keys);
        Assert.Contains("result_calculation", keys);
        Assert.Contains("notifications",      keys);
        Assert.Contains("students",           keys);
        Assert.Contains("departments",        keys);
        Assert.Contains("courses",            keys);
        Assert.Contains("results",            keys);
        Assert.Contains("analytics",          keys);
        Assert.Contains("student_lifecycle",  keys);
        Assert.Contains("payments",           keys);
        Assert.Contains("enrollments",        keys);
        Assert.Contains("report_center",      keys);
        Assert.Contains("system_settings",    keys); // parent carrier for theme_settings sub-menu
        Assert.Contains("theme_settings",      keys);
        Assert.DoesNotContain("module_settings",   keys); // SuperAdmin only (no visible sub-menus)
        Assert.DoesNotContain("sidebar_settings",  keys); // SuperAdmin only
        Assert.Equal(19, keys.Count);
    }

    /// <summary>Faculty should see all menus with IsAllowed=true for Faculty role (17 total).</summary>
    [Fact]
    public async Task GetVisible_Faculty_ReturnsFacultyMenusOnly()
    {
        using var client = CreateClient("Faculty");
        var menus = await GetVisibleAsync(client);
        var keys  = FlatKeys(menus);

        Assert.Contains("dashboard",         keys);
        Assert.Contains("timetable_teacher", keys);
        Assert.Contains("theme_settings",    keys);
        Assert.Contains("notifications",     keys);
        Assert.Contains("students",          keys);
        Assert.Contains("courses",           keys);
        Assert.Contains("assignments",       keys);
        Assert.Contains("attendance",        keys);
        Assert.Contains("results",           keys);
        Assert.Contains("quizzes",           keys);
        Assert.Contains("fyp",               keys);
        Assert.Contains("analytics",         keys);
        Assert.Contains("ai_chat",           keys);
        Assert.Contains("enrollments",       keys);
        Assert.Contains("report_center",     keys);
        Assert.Contains("system_settings",  keys); // parent carrier for theme_settings
        Assert.Contains("theme_settings",    keys);
        Assert.DoesNotContain("timetable_admin",   keys);
        Assert.DoesNotContain("module_settings",   keys); // SuperAdmin only
        Assert.Equal(17, keys.Count);
    }

    /// <summary>Student should see all menus with IsAllowed=true for Student role (13 total).</summary>
    [Fact]
    public async Task GetVisible_Student_ReturnsStudentMenusOnly()
    {
        using var client = CreateClient("Student");
        var menus = await GetVisibleAsync(client);
        var keys  = FlatKeys(menus);

        Assert.Contains("dashboard",         keys);
        Assert.Contains("timetable_student", keys);
        Assert.Contains("theme_settings",    keys);
        Assert.Contains("notifications",     keys);
        Assert.Contains("assignments",       keys);
        Assert.Contains("attendance",        keys);
        Assert.Contains("results",           keys);
        Assert.Contains("quizzes",           keys);
        Assert.Contains("fyp",               keys);
        Assert.Contains("ai_chat",           keys);
        Assert.Contains("payments",          keys);
        Assert.Contains("report_center",     keys);
        Assert.Contains("system_settings",  keys); // parent carrier for theme_settings
        Assert.Contains("theme_settings",    keys);
        Assert.DoesNotContain("timetable_admin",  keys);
        Assert.DoesNotContain("module_settings",  keys); // SuperAdmin only
        Assert.Equal(13, keys.Count);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Faculty")]
    [InlineData("Student")]
    public async Task ReportCenter_VisibleRoles_HaveMenuAndReachableCatalog(string role)
    {
        // Final-Touches Phase 32 Stage 32.4 — keep report-center sidebar visibility and report-link behavior aligned by role.
        using var client = CreateClient(role);

        var sidebarKeys = FlatKeys(await GetVisibleAsync(client));
        Assert.Contains("report_center", sidebarKeys);

        var catalog = await GetReportCatalogAsync(client);
        Assert.NotEmpty(catalog.Reports);
    }

    [Fact]
    public async Task SidebarSettings_HiddenForAdmin_IsForbiddenOnSettingsEndpoint()
    {
        // Stage 2.3 guard consistency: if a menu is hidden in sidebar, direct settings endpoint access must be forbidden.
        using var client = CreateClient("Admin");

        var sidebarKeys = FlatKeys(await GetVisibleAsync(client));
        Assert.DoesNotContain("sidebar_settings", sidebarKeys);

        var response = await client.GetAsync("api/v1/sidebar-menu");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SidebarSettings_VisibleForSuperAdmin_CanAccessSettingsEndpoint()
    {
        using var client = CreateClient("SuperAdmin");

        var sidebarKeys = FlatKeys(await GetVisibleAsync(client));
        Assert.Contains("sidebar_settings", sidebarKeys);

        var response = await client.GetAsync("api/v1/sidebar-menu");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DisabledCoursesModule_HidesCoursesFromAdminVisibleSidebar()
    {
        using var superClient = CreateClient("SuperAdmin");
        using var adminClient = CreateClient("Admin");
        const string moduleKey = "courses";

        var original = await GetModuleStatusAsync(superClient, moduleKey);

        try
        {
            await SetModuleStatusAsync(superClient, moduleKey, isActive: false);

            var keys = FlatKeys(await GetVisibleAsync(adminClient));
            Assert.DoesNotContain("courses", keys);
        }
        finally
        {
            await SetModuleStatusAsync(superClient, moduleKey, original);
        }
    }

    [Fact]
    public async Task DisabledReportsModule_HidesReportMenuEntriesFromAdminVisibleSidebar()
    {
        using var superClient = CreateClient("SuperAdmin");
        using var adminClient = CreateClient("Admin");
        const string moduleKey = "reports";

        var original = await GetModuleStatusAsync(superClient, moduleKey);

        try
        {
            await SetModuleStatusAsync(superClient, moduleKey, isActive: false);

            var keys = FlatKeys(await GetVisibleAsync(adminClient));
            Assert.DoesNotContain("report_center", keys);
            Assert.DoesNotContain("analytics", keys);
        }
        finally
        {
            await SetModuleStatusAsync(superClient, moduleKey, original);
        }
    }

    [Fact]
    public async Task DisabledThemesModule_HidesThemeSettingsFromStudentVisibleSidebar()
    {
        using var superClient = CreateClient("SuperAdmin");
        using var studentClient = CreateClient("Student");
        const string moduleKey = "themes";

        var original = await GetModuleStatusAsync(superClient, moduleKey);

        try
        {
            await SetModuleStatusAsync(superClient, moduleKey, isActive: false);

            var keys = FlatKeys(await GetVisibleAsync(studentClient));
            Assert.DoesNotContain("theme_settings", keys);
        }
        finally
        {
            await SetModuleStatusAsync(superClient, moduleKey, original);
        }
    }

    // ── Status toggle ─────────────────────────────────────────────────────────

    /// <summary>
    /// Disabling timetable_teacher removes it from the Faculty sidebar.
    /// Re-enabling restores it. State is always restored in the finally block.
    /// </summary>
    [Fact]
    public async Task SetStatus_DisableTimetableTeacher_RemovesFromFaculty_ThenRestore()
    {
        using var superClient   = CreateClient("SuperAdmin");
        using var facultyClient = CreateClient("Faculty");

        // Locate the menu item id via the SuperAdmin all-visible endpoint.
        var allMenus  = await GetVisibleAsync(superClient);
        var ttTeacher = FlatItems(allMenus).Single(m => m.Key == "timetable_teacher");

        try
        {
            // ── Disable ──
            var disableResp = await superClient.PutAsJsonAsync(
                $"api/v1/sidebar-menu/{ttTeacher.Id}/status",
                new { isActive = false });
            Assert.Equal(HttpStatusCode.NoContent, disableResp.StatusCode);

            // Faculty should now only see dashboard.
            var afterDisable = FlatKeys(await GetVisibleAsync(facultyClient));
            Assert.DoesNotContain("timetable_teacher", afterDisable);
            Assert.Contains("dashboard", afterDisable);
        }
        finally
        {
            // ── Restore ──
            await superClient.PutAsJsonAsync(
                $"api/v1/sidebar-menu/{ttTeacher.Id}/status",
                new { isActive = true });
        }

        // Confirm Faculty sees timetable_teacher again after restore.
        var afterRestore = FlatKeys(await GetVisibleAsync(facultyClient));
        Assert.Contains("timetable_teacher", afterRestore);
    }

    // ── Role access deny ──────────────────────────────────────────────────────

    /// <summary>
    /// Denying Student access to timetable_student removes it from the Student sidebar.
    /// Re-allowing restores it. State is always restored in the finally block.
    /// </summary>
    [Fact]
    public async Task SetRoles_DenyStudent_RemovesFromStudentVisible_ThenRestore()
    {
        using var superClient   = CreateClient("SuperAdmin");
        using var studentClient = CreateClient("Student");

        var allMenus  = await GetVisibleAsync(superClient);
        var ttStudent = FlatItems(allMenus).Single(m => m.Key == "timetable_student");

        try
        {
            // ── Deny ──
            var denyResp = await superClient.PutAsJsonAsync(
                $"api/v1/sidebar-menu/{ttStudent.Id}/roles",
                new { entries = new[] { new { roleName = "Student", isAllowed = false } } });
            Assert.Equal(HttpStatusCode.NoContent, denyResp.StatusCode);

            // Student should now only see dashboard.
            var afterDeny = FlatKeys(await GetVisibleAsync(studentClient));
            Assert.DoesNotContain("timetable_student", afterDeny);
            Assert.Contains("dashboard", afterDeny);
        }
        finally
        {
            // ── Restore ──
            await superClient.PutAsJsonAsync(
                $"api/v1/sidebar-menu/{ttStudent.Id}/roles",
                new { entries = new[] { new { roleName = "Student", isAllowed = true } } });
        }

        // Confirm Student sees timetable_student again after restore.
        var afterRestore = FlatKeys(await GetVisibleAsync(studentClient));
        Assert.Contains("timetable_student", afterRestore);
    }

    // ── System-menu protection ────────────────────────────────────────────────

    /// <summary>
    /// Attempting to deactivate a system menu (IsSystemMenu = true) must return 409 Conflict.
    /// sidebar_settings is seeded as IsSystemMenu = true.
    /// </summary>
    [Fact]
    public async Task SetStatus_SystemMenu_DeactivateAttempt_Returns409Conflict()
    {
        using var superClient = CreateClient("SuperAdmin");

        var allMenus       = await GetVisibleAsync(superClient);
        var sidebarSettings = FlatItems(allMenus).Single(m => m.Key == "sidebar_settings");

        var response = await superClient.PutAsJsonAsync(
            $"api/v1/sidebar-menu/{sidebarSettings.Id}/status",
            new { isActive = false });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>
    /// Every seeded menu item (top-level and sub-menu) must accept role-assignment updates
    /// through Sidebar Settings so menu assignment remains operational platform-wide.
    /// </summary>
    [Fact]
    public async Task SetRoles_AllSeededMenus_AreAssignable()
    {
        // Final-Touches Phase 32 Stage 32.3 — enforce Sidebar Settings assignability guardrail for all menu keys.
        using var superClient = CreateClient("SuperAdmin");

        var allMenus = await GetVisibleAsync(superClient);
        var allItems = FlatItems(allMenus).ToList();

        Assert.Equal(30, allItems.Count);

        foreach (var menu in allItems)
        {
            var payload = new
            {
                entries = menu.RoleAccesses
                    .Select(a => new { roleName = a.RoleName, isAllowed = a.IsAllowed })
                    .ToArray()
            };

            var response = await superClient.PutAsJsonAsync($"api/v1/sidebar-menu/{menu.Id}/roles", payload);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    // ── 401 for unauthenticated requests ─────────────────────────────────────

    /// <summary>Unauthenticated requests to my-visible must be rejected with 401.</summary>
    [Fact]
    public async Task GetVisible_NoToken_Returns401()
    {
        using var client   = _factory.CreateClient(); // no auth header
        var response       = await client.GetAsync("api/v1/sidebar-menu/my-visible");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Local DTOs (mirrors API response shape — case-insensitive deserialization) ──

    private sealed class MenuDto
    {
        public Guid         Id          { get; set; }
        public string       Key         { get; set; } = string.Empty;
        public string       Name        { get; set; } = string.Empty;
        public bool         IsActive    { get; set; }
        public bool         IsSystemMenu{ get; set; }
        public List<MenuDto>        SubMenus     { get; set; } = new();
        public List<RoleAccessDto>  RoleAccesses { get; set; } = new();
    }

    private sealed class RoleAccessDto
    {
        public string RoleName  { get; set; } = string.Empty;
        public bool   IsAllowed { get; set; }
    }

    private sealed class ReportCatalogDto
    {
        public List<ReportCatalogItemDto> Reports { get; set; } = new();
    }

    private sealed class ReportCatalogItemDto
    {
        public Guid   Id   { get; set; }
        public string Key  { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
