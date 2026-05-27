using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
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
    private Dictionary<string, HashSet<string>> _sidebarRoleAllowMatrix = new(StringComparer.OrdinalIgnoreCase);

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
        _sidebarRoleAllowMatrix = LoadSidebarRoleAllowMatrix();

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

    private static string ResolveRepoFilePath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Unable to resolve required file '{relativePath}' from test base directory.");
    }

    private static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        values.Add(current.ToString().Trim());
        return values.ToArray();
    }

    private static Dictionary<string, HashSet<string>> LoadSidebarRoleAllowMatrix()
    {
        var filePath = ResolveRepoFilePath(Path.Combine("Docs", "Sidebar-Menu-Purpose.csv"));
        var roleToAllowedKeys = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["SuperAdmin"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ["Admin"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ["Faculty"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ["Student"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };

        foreach (var rawLine in File.ReadLines(filePath).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            var cols = ParseCsvLine(rawLine);
            if (cols.Length < 9)
                continue;

            var menuKey = cols[1];
            if (string.IsNullOrWhiteSpace(menuKey))
                continue;

            if (!cols[4].Equals("No Access", StringComparison.OrdinalIgnoreCase))
                roleToAllowedKeys["SuperAdmin"].Add(menuKey);
            if (!cols[5].Equals("No Access", StringComparison.OrdinalIgnoreCase))
                roleToAllowedKeys["Admin"].Add(menuKey);
            if (!cols[6].Equals("No Access", StringComparison.OrdinalIgnoreCase))
                roleToAllowedKeys["Faculty"].Add(menuKey);
            if (!cols[7].Equals("No Access", StringComparison.OrdinalIgnoreCase))
                roleToAllowedKeys["Student"].Add(menuKey);
        }

        return roleToAllowedKeys;
    }

    private HashSet<string> BuildExpectedVisibleKeysForRole(string role, IEnumerable<MenuDto> superAdminMenus)
    {
        var allSeededItems = FlatItems(superAdminMenus).ToList();
        var seededKeys = allSeededItems
            .Select(m => m.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var expected = _sidebarRoleAllowMatrix.TryGetValue(role, out var allowedByRole)
            ? allowedByRole
                .Where(k => seededKeys.Contains(k))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Parent carriers can be visible even when only child keys are role-allowed.
        foreach (var parent in superAdminMenus)
        {
            if (parent.SubMenus.Count == 0)
                continue;

            if (parent.SubMenus.Any(child => expected.Contains(child.Key)))
                expected.Add(parent.Key);
        }

        return expected;
    }

    private static void AssertActualIsSubsetOfExpected(HashSet<string> actual, HashSet<string> expected)
    {
        var unexpected = actual.Except(expected, StringComparer.OrdinalIgnoreCase).ToArray();
        Assert.True(unexpected.Length == 0, "Unexpected menu keys: " + string.Join(", ", unexpected));
    }

    // ── Role matrix ───────────────────────────────────────────────────────────

    /// <summary>SuperAdmin should see every seeded menu item and all privileged governance menus.</summary>
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
        Assert.Contains("enter_attendance",    keys);
        Assert.Contains("results",             keys);
        Assert.Contains("quizzes",             keys);
        Assert.Contains("fyp",                 keys);
        Assert.Contains("analytics",           keys);
        Assert.Contains("ai_chat",             keys);
        Assert.Contains("student_lifecycle",   keys);
        Assert.Contains("payments",            keys);
        Assert.Contains("enrollments",         keys);
        Assert.Contains("report_center",       keys);
        Assert.Contains("report_settings",     keys);
        Assert.Contains("license_update",      keys);
        Assert.Contains("admin_users",         keys);
        Assert.Contains("tenant_management",   keys);
        Assert.Contains("campus_management",   keys);
    }

    /// <summary>Admin should match CSV allow-matrix visibility for seeded keys.</summary>
    [Fact]
    public async Task GetVisible_Admin_ReturnsAdminMenusOnly()
    {
        using var superClient = CreateClient("SuperAdmin");
        using var client = CreateClient("Admin");

        var superMenus = await GetVisibleAsync(superClient);
        var expected = BuildExpectedVisibleKeysForRole("Admin", superMenus);

        var menus = await GetVisibleAsync(client);
        var keys  = FlatKeys(menus);

        AssertActualIsSubsetOfExpected(keys, expected);
        Assert.Contains("dashboard", keys);
        Assert.Contains("enter_attendance", keys);
        Assert.Contains("result_calculation", keys);
        Assert.Contains("report_center", keys);
        Assert.DoesNotContain("sidebar_settings", keys);
        Assert.DoesNotContain("report_settings", keys);
        Assert.DoesNotContain("license_update", keys);
        Assert.DoesNotContain("admin_users", keys);
        Assert.DoesNotContain("tenant_management", keys);
        Assert.DoesNotContain("campus_management", keys);
    }

    /// <summary>Faculty should match CSV allow-matrix visibility for seeded keys.</summary>
    [Fact]
    public async Task GetVisible_Faculty_ReturnsFacultyMenusOnly()
    {
        using var superClient = CreateClient("SuperAdmin");
        using var client = CreateClient("Faculty");

        var superMenus = await GetVisibleAsync(superClient);
        var expected = BuildExpectedVisibleKeysForRole("Faculty", superMenus);

        var menus = await GetVisibleAsync(client);
        var keys  = FlatKeys(menus);

        AssertActualIsSubsetOfExpected(keys, expected);
        Assert.Contains("dashboard", keys);
        Assert.Contains("timetable_teacher", keys);
        Assert.Contains("assignments", keys);
        Assert.Contains("enter_attendance", keys);
        Assert.Contains("report_center", keys);
        Assert.DoesNotContain("sidebar_settings", keys);
        Assert.DoesNotContain("report_settings", keys);
        Assert.DoesNotContain("license_update", keys);
        Assert.DoesNotContain("admin_users", keys);
        Assert.DoesNotContain("tenant_management", keys);
        Assert.DoesNotContain("campus_management", keys);
    }

    /// <summary>Student should match CSV allow-matrix visibility for seeded keys.</summary>
    [Fact]
    public async Task GetVisible_Student_ReturnsStudentMenusOnly()
    {
        using var superClient = CreateClient("SuperAdmin");
        using var client = CreateClient("Student");

        var superMenus = await GetVisibleAsync(superClient);
        var expected = BuildExpectedVisibleKeysForRole("Student", superMenus);

        var menus = await GetVisibleAsync(client);
        var keys  = FlatKeys(menus);

        AssertActualIsSubsetOfExpected(keys, expected);
        Assert.Contains("dashboard", keys);
        Assert.Contains("timetable_student", keys);
        Assert.Contains("assignments", keys);
        Assert.Contains("report_center", keys);
        Assert.DoesNotContain("sidebar_settings", keys);
        Assert.DoesNotContain("report_settings", keys);
        Assert.DoesNotContain("license_update", keys);
        Assert.DoesNotContain("admin_users", keys);
        Assert.DoesNotContain("tenant_management", keys);
        Assert.DoesNotContain("campus_management", keys);
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
        Assert.NotEmpty(allItems);

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
