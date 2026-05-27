using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Globalization;
using System.Text;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

public class PortalController : Controller
{
    private readonly IEduApiClient _api;
    private readonly IWebHostEnvironment _environment;

    private static readonly Dictionary<string, string> ActionMenuKeyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(ModuleComposition)] = "module_composition",
        [nameof(TimetableAdmin)] = "timetable_admin",
        [nameof(TimetableStudent)] = "timetable_student",
        [nameof(TimetableTeacher)] = "timetable_teacher",
        [nameof(Buildings)] = "buildings",
        [nameof(Rooms)] = "rooms",
        [nameof(ReportSettings)] = "report_settings",
        [nameof(ResultCalculation)] = "result_calculation",
        [nameof(SidebarSettings)] = "sidebar_settings",
        [nameof(LicenseUpdate)] = "license_update",
        [nameof(ThemeSettings)] = "theme_settings",
        [nameof(Notifications)] = "notifications",
        [nameof(Students)] = "students",
        [nameof(UserImport)] = "user_import",
        [nameof(Departments)] = "departments",
        [nameof(TenantManagement)] = "tenant_management",
        [nameof(CampusManagement)] = "campus_management",
        [nameof(AdminUsers)] = "admin_users",
        [nameof(Courses)] = "courses",
        [nameof(Assignments)] = "assignments",
        [nameof(Attendance)] = "attendance",
        [nameof(EnterAttendance)] = "enter_attendance",
        [nameof(Results)] = "results",
        [nameof(Quizzes)] = "quizzes",
        [nameof(Fyp)] = "fyp",
        [nameof(Helpdesk)] = "helpdesk",
        [nameof(Prerequisites)] = "prerequisites",
        [nameof(Gradebook)] = "gradebook",
        [nameof(RubricManage)] = "rubric_manage",
        [nameof(Analytics)] = "analytics",
        [nameof(AiChat)] = "ai_chat",
        [nameof(StudentLifecycle)] = "student_lifecycle",
        [nameof(Payments)] = "payments",
        [nameof(Enrollments)] = "enrollments",
        [nameof(ReportCenter)] = "report_center",
        [nameof(ReportPayments)] = "report_center",
        [nameof(DashboardSettings)] = "dashboard_settings",
        [nameof(DegreeAudit)] = "degree_audit",
        [nameof(GraduationEligibility)] = "graduation_eligibility",
        [nameof(DegreeRules)] = "degree_rules",
        [nameof(GraduationApply)] = "graduation_apply",
        [nameof(GraduationApplications)] = "graduation_applications",
        [nameof(GradingConfig)] = "grading_config",
        [nameof(LmsManage)] = "lms_manage",
        [nameof(CourseMaterial)] = "course_material",
        [nameof(Discussion)] = "discussion",
        [nameof(Announcements)] = "announcements",
        [nameof(StudyPlan)] = "study_plan",
        [nameof(LibraryConfig)] = "library_config",
        [nameof(InstitutionPolicy)] = "institution_policy"
    };

    private static readonly HashSet<string> FinanceBlockedAcademicMenuKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "timetable_admin",
        "timetable_teacher",
        "timetable_student",
        "buildings",
        "rooms",
        "students",
        "user_import",
        "departments",
        "courses",
        "assignments",
        "attendance",
        "enter_attendance",
        "results",
        "quizzes",
        "fyp",
        "prerequisites",
        "gradebook",
        "rubric_manage",
        "student_lifecycle",
        "enrollments",
        "degree_audit",
        "graduation_eligibility",
        "degree_rules",
        "graduation_apply",
        "graduation_applications",
        "grading_config",
        "lms_manage",
        "course_material",
        "discussion",
        "announcements",
        "study_plan"
    };

    public PortalController(IEduApiClient api, IWebHostEnvironment environment)
    {
        _api = api;
        _environment = environment;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var action = context.RouteData.Values["action"]?.ToString();
        var isForceChangeAction = string.Equals(action, nameof(ForceChangePassword), StringComparison.OrdinalIgnoreCase);

        if (!isForceChangeAction && _api.IsConnected() && _api.IsForcePasswordChangeRequired())
        {
            context.Result = RedirectToAction(nameof(ForceChangePassword));
            return;
        }

        if (ShouldBlockFinanceAcademicAction(action))
        {
            TempData["PortalMessage"] = "Finance access is limited to finance modules. Academic sections are blocked.";
            context.Result = RedirectToAction(nameof(Payments));
            return;
        }

        if (ShouldEnforceSidebarGuard(action) && !CanBypassSidebarGuard())
        {
            var requiredMenuKey = ActionMenuKeyMap[action!];
            HashSet<string> visibleMenuKeys;
            try
            {
                visibleMenuKeys = await GetVisibleMenuKeysAsync(context.HttpContext.RequestAborted);
            }
            catch (InvalidOperationException ex) when (IsUnauthorizedApiException(ex))
            {
                _api.SaveConnection(new ApiConnectionModel());
                _api.SetForcePasswordChangeRequired(false);
                context.Result = RedirectToAction("Index", "Login", new { returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString });
                return;
            }

            if (!visibleMenuKeys.Contains(requiredMenuKey))
            {
                TempData["PortalMessage"] = "Access denied for this section based on your current role and menu permissions.";
                context.Result = RedirectToAction(nameof(Dashboard));
                return;
            }
        }

        await base.OnActionExecutionAsync(context, next);
    }

    private bool ShouldBlockFinanceAcademicAction(string? actionName)
    {
        if (!_api.IsConnected() || string.IsNullOrWhiteSpace(actionName))
            return false;

        if (!ActionMenuKeyMap.TryGetValue(actionName, out var menuKey))
            return false;

        var identity = _api.GetSessionIdentity();
        if (identity is null || !identity.IsFinance || identity.IsAdmin || identity.IsSuperAdmin)
            return false;

        return FinanceBlockedAcademicMenuKeys.Contains(menuKey);
    }

    private bool ShouldEnforceSidebarGuard(string? actionName)
        => _api.IsConnected()
           && !string.IsNullOrWhiteSpace(actionName)
           && ActionMenuKeyMap.ContainsKey(actionName)
           && !string.Equals(actionName, nameof(Dashboard), StringComparison.OrdinalIgnoreCase);

    private bool CanBypassSidebarGuard()
    {
        var identity = _api.GetSessionIdentity();
        return identity?.IsSuperAdmin == true;
    }

    private async Task<HashSet<string>> GetVisibleMenuKeysAsync(CancellationToken ct)
    {
        var visibleMenus = await _api.GetVisibleSidebarMenusForCurrentUserAsync(ct);
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var menu in visibleMenus)
        {
            keys.Add(menu.Key);
            foreach (var subMenu in menu.SubMenus)
                keys.Add(subMenu.Key);
        }

        return keys;
    }

    private static bool IsUnauthorizedApiException(InvalidOperationException ex)
        => ex.Message.Contains("status 401", StringComparison.OrdinalIgnoreCase)
           || ex.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);

    private async Task<(bool Allowed, string Message)> CanUseDegreeAuditAsync(CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity is { IsSuperAdmin: false, InstitutionType: not null } && identity.InstitutionType.Value != 0)
            return (false, "Degree Audit is available only for university institution type.");

        try
        {
            var matrix = await _api.GetPortalCapabilityMatrixAsync(ct);
            if (matrix is not null && !matrix.IncludeUniversity)
                return (false, "Degree Audit is hidden because University is disabled by the current license policy.");
        }
        catch
        {
            // API endpoints also enforce this; fail-open in UI when capability matrix cannot be loaded.
        }

        return (true, string.Empty);
    }

    private static bool IsUniversityInstitutionType(int? institutionType)
        => institutionType.HasValue && institutionType.Value == 0;

    private static string ResolvePeriodFilterLabel(int? institutionType)
        => IsUniversityInstitutionType(institutionType) ? "Class" : "Semester";

    private static int? ResolveCertificateInstitutionType(SessionIdentity? identity, List<DepartmentItem> departments, Guid? selectedDepartmentId)
    {
        if (selectedDepartmentId.HasValue)
        {
            return departments.FirstOrDefault(d => d.Id == selectedDepartmentId.Value)?.InstitutionType;
        }

        if (identity is { IsSuperAdmin: false, InstitutionType: not null })
            return identity.InstitutionType.Value;

        var distinctInstitutionTypes = departments
            .Select(d => d.InstitutionType)
            .Distinct()
            .ToList();

        return distinctInstitutionTypes.Count == 1
            ? distinctInstitutionTypes[0]
            : null;
    }

    [HttpGet]
    public IActionResult ForceChangePassword()
    {
        ViewData["Title"] = "Force Change Password";
        if (!_api.IsConnected())
            return RedirectToAction("Index", "Login");

        if (!_api.IsForcePasswordChangeRequired())
            return RedirectToAction(nameof(Dashboard));

        return View(new ForceChangePasswordPageModel { IsConnected = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForceChangePassword(string newPassword, string confirmPassword, CancellationToken ct)
    {
        ViewData["Title"] = "Force Change Password";
        var model = new ForceChangePasswordPageModel { IsConnected = _api.IsConnected() };

        if (!model.IsConnected)
            return RedirectToAction("Index", "Login");

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            model.Message = "New password is required.";
            return View(model);
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            model.Message = "Password confirmation does not match.";
            return View(model);
        }

        try
        {
            await _api.ForceChangePasswordAsync(newPassword, ct);
            _api.SetForcePasswordChangeRequired(false);
            TempData["PortalMessage"] = "Password changed successfully.";
            return RedirectToAction(nameof(Dashboard));
        }
        catch (Exception ex)
        {
            model.Message = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult TwoFactorSettings()
    {
        ViewData["Title"] = "Two-Factor Authentication";
        var model = new TwoFactorSettingsPageModel
        {
            IsConnected = _api.IsConnected(),
            CurrentUserId = GetCurrentUserId()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BeginTwoFactorSetup(CancellationToken ct)
    {
        ViewData["Title"] = "Two-Factor Authentication";
        if (!_api.IsConnected())
            return RedirectToAction("Index", "Login");

        var model = new TwoFactorSettingsPageModel
        {
            IsConnected = true,
            CurrentUserId = GetCurrentUserId()
        };

        try
        {
            var setup = await _api.BeginTwoFactorSetupAsync(ct);
            if (setup is null)
            {
                model.Message = "Unable to start 2FA setup right now. Please try again.";
                return View(nameof(TwoFactorSettings), model);
            }

            model.TwoFactorEnabled = setup.TwoFactorEnabled;
            model.Issuer = setup.Issuer;
            model.AccountName = setup.AccountName;
            model.ManualKey = setup.ManualKey;
            model.ProvisioningUri = setup.ProvisioningUri;
            model.QrCodeDataUrl = setup.QrCodeDataUrl;
            model.Message = "2FA setup started. Scan the QR code or enter the manual key in your authenticator app.";
        }
        catch (Exception ex)
        {
            model.Message = ex.Message;
        }

        return View(nameof(TwoFactorSettings), model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyTwoFactorSetup(string code, CancellationToken ct)
    {
        ViewData["Title"] = "Two-Factor Authentication";
        if (!_api.IsConnected())
            return RedirectToAction("Index", "Login");

        var model = new TwoFactorSettingsPageModel
        {
            IsConnected = true,
            CurrentUserId = GetCurrentUserId()
        };

        if (string.IsNullOrWhiteSpace(code))
        {
            model.Message = "Enter an authenticator code to verify setup.";
            return View(nameof(TwoFactorSettings), model);
        }

        try
        {
            var result = await _api.VerifyTwoFactorSetupAsync(code, ct);
            if (result is null)
            {
                model.Message = "Unable to verify 2FA setup right now. Please try again.";
                return View(nameof(TwoFactorSettings), model);
            }

            model.TwoFactorEnabled = result.TwoFactorEnabled;
            model.Message = result.Message;
        }
        catch (Exception ex)
        {
            model.Message = ex.Message;
        }

        return View(nameof(TwoFactorSettings), model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableTwoFactor(string code, CancellationToken ct)
    {
        ViewData["Title"] = "Two-Factor Authentication";
        if (!_api.IsConnected())
            return RedirectToAction("Index", "Login");

        var model = new TwoFactorSettingsPageModel
        {
            IsConnected = true,
            CurrentUserId = GetCurrentUserId()
        };

        if (string.IsNullOrWhiteSpace(code))
        {
            model.Message = "Enter an authenticator code to disable 2FA.";
            return View(nameof(TwoFactorSettings), model);
        }

        try
        {
            var result = await _api.DisableTwoFactorAsync(code, ct);
            if (result is null)
            {
                model.Message = "Unable to disable 2FA right now. Please try again.";
                return View(nameof(TwoFactorSettings), model);
            }

            model.TwoFactorEnabled = result.TwoFactorEnabled;
            model.Message = result.Message;
        }
        catch (Exception ex)
        {
            model.Message = ex.Message;
        }

        return View(nameof(TwoFactorSettings), model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestTwoFactorLogin(string code, CancellationToken ct)
    {
        ViewData["Title"] = "Two-Factor Authentication";
        if (!_api.IsConnected())
            return RedirectToAction("Index", "Login");

        var model = new TwoFactorSettingsPageModel
        {
            IsConnected = true,
            CurrentUserId = GetCurrentUserId()
        };

        if (!model.CurrentUserId.HasValue)
        {
            model.Message = "Unable to determine current user. Sign in again and retry.";
            return View(nameof(TwoFactorSettings), model);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            model.Message = "Enter an authenticator code to test login verification.";
            return View(nameof(TwoFactorSettings), model);
        }

        try
        {
            var result = await _api.VerifyTwoFactorLoginAsync(model.CurrentUserId.Value, code, ct);
            if (result is null)
            {
                model.Message = "Unable to validate 2FA login right now. Please try again.";
                return View(nameof(TwoFactorSettings), model);
            }

            model.TwoFactorEnabled = result.TwoFactorEnabled;
            model.Message = result.Message;
        }
        catch (Exception ex)
        {
            model.Message = ex.Message;
        }

        return View(nameof(TwoFactorSettings), model);
    }

    private Guid? GetCurrentUserId()
    {
        var callerIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(callerIdStr, out var parsedUserId) ? parsedUserId : null;
    }

    private async Task<List<LookupItem>> GetOfferingFilterOptionsAsync(SessionIdentity? sessionIdentity, CancellationToken ct, Guid? tenantId = null, Guid? campusId = null)
    {
        if (sessionIdentity?.IsAdmin == true || sessionIdentity?.IsSuperAdmin == true)
        {
            var offerings = await _api.GetCourseOfferingsAsync(null, tenantId, campusId, null, ct);
            return offerings.Select(o => new LookupItem
            {
                Id = o.Id,
                Name = string.IsNullOrWhiteSpace(o.CourseCode)
                    ? $"{o.CourseTitle} ({o.SemesterName})"
                    : $"{o.CourseCode} — {o.CourseTitle} ({o.SemesterName})"
            }).ToList();
        }

        return await _api.GetMyOfferingsAsync(ct);
    }

    private static List<LookupItem> BuildSemesterOptions(IEnumerable<LookupItem> offerings)
    {
        return offerings
            .Select(o => ExtractSemesterName(o.Name))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(s => new LookupItem { Id = Guid.Empty, Name = s })
            .ToList();
    }

    private static List<LookupItem> FilterOfferingsBySemester(IEnumerable<LookupItem> offerings, string? semesterName)
    {
        if (string.IsNullOrWhiteSpace(semesterName))
        {
            return offerings.ToList();
        }

        return offerings
            .Where(o => string.Equals(ExtractSemesterName(o.Name), semesterName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static string? ExtractSemesterName(string? offeringDisplayName)
    {
        if (string.IsNullOrWhiteSpace(offeringDisplayName))
        {
            return null;
        }

        var start = offeringDisplayName.LastIndexOf(" (", StringComparison.Ordinal);
        if (start < 0 || !offeringDisplayName.EndsWith(")", StringComparison.Ordinal))
        {
            return null;
        }

        var value = offeringDisplayName.Substring(start + 2, offeringDisplayName.Length - start - 3);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private async Task<Guid?> GetEffectiveStudentDepartmentIdAsync(CancellationToken ct)
    {
        Guid? profileDepartmentId = null;
        try
        {
            var profile = await _api.GetMyStudentProfileAsync(ct);
            if (profile is not null && profile.DepartmentId != Guid.Empty)
                profileDepartmentId = profile.DepartmentId;
        }
        catch
        {
            // Keep timetable usable even if profile endpoint is temporarily unavailable.
        }

        if (profileDepartmentId.HasValue)
            return profileDepartmentId;

        var fallback = _api.GetConnection().DefaultDepartmentId;
        return fallback.HasValue && fallback.Value != Guid.Empty ? fallback : null;
    }

    // ── Dashboard / Connection ──────────────────────────────────────────────

    [HttpGet]
    public IActionResult Dashboard()
    {
        ViewData["Title"] = "Dashboard";
        var vm = _api.GetConnection();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveConnection(ApiConnectionModel model)
    {
        _api.SaveConnection(model);
        TempData["PortalMessage"] = "API connection saved in session.";
        return RedirectToAction(nameof(Dashboard));
    }

    // ── Phase 24 — Module Composition Panel ────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> ModuleComposition(CancellationToken ct)
    {
        var model = new DashboardCompositionModel { IsConnected = _api.IsConnected() };
        if (!model.IsConnected) return View(model);
        try
        {
            var identity = _api.GetSessionIdentity();
            model.CanManageModules = identity?.IsSuperAdmin == true;

            var context = await _api.GetDashboardCompositionContextAsync(ct);
            if (context is null)
            {
                model.Message = "Dashboard composition is unavailable right now.";
                return View(model);
            }

            model.Modules = context.Modules.Select(m => new ModuleVisibilityItem
            {
                Key = m.Key, Name = m.Name,
                IsActive = m.IsActive, IsAccessible = m.IsAccessible
            }).ToList();

            model.Widgets = context.Widgets.Select(w => new WidgetItem
            {
                Key = w.Key, Title = w.Title,
                Icon = w.Icon, Order = w.Order
            }).ToList();

            var vocab = context.Vocabulary;
            if (vocab is not null)
            {
                model.PeriodLabel       = vocab.PeriodLabel;
                model.ProgressionLabel  = vocab.ProgressionLabel;
                model.GradingLabel      = vocab.GradingLabel;
                model.CourseLabel       = vocab.CourseLabel;
                model.StudentGroupLabel = vocab.StudentGroupLabel;
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCompositionModule(string key, bool activate, CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity?.IsSuperAdmin != true)
        {
            TempData["Message"] = "Only SuperAdmin can activate or deactivate modules globally.";
            return RedirectToAction(nameof(ModuleComposition));
        }

        if (_api.IsConnected())
        {
            try
            {
                await _api.SetModuleActiveAsync(key, activate, ct);
                TempData["Message"] = activate ? "Module activated for all users." : "Module deactivated for all users.";
            }
            catch (Exception ex) { TempData["Message"] = $"Error: {ex.Message}"; }
        }

        return RedirectToAction(nameof(ModuleComposition));
    }

    // ── Phase 27 — Student Portal Capability Matrix ───────────────────────

    [HttpGet]
    public async Task<IActionResult> PortalCapabilityMatrix(CancellationToken ct)
    {
        ViewData["Title"] = "Portal Capability Matrix";

        var model = new PortalCapabilityMatrixPageModel
        {
            IsConnected = _api.IsConnected(),
            Message = TempData["PortalMessage"]?.ToString()
        };

        if (!model.IsConnected)
        {
            model.Message ??= "Configure API connection on Dashboard first.";
            return View(model);
        }

        try
        {
            var matrix = await _api.GetPortalCapabilityMatrixAsync(ct);
            if (matrix is null)
            {
                model.Message = "Capability matrix is unavailable right now.";
                return View(model);
            }

            model.IncludeSchool = matrix.IncludeSchool;
            model.IncludeCollege = matrix.IncludeCollege;
            model.IncludeUniversity = matrix.IncludeUniversity;
            model.Rows = matrix.Rows
                .Select(r => new PortalCapabilityMatrixItem
                {
                    CapabilityKey = r.CapabilityKey,
                    CapabilityName = r.CapabilityName,
                    ModuleKey = r.ModuleKey,
                    ModuleName = r.ModuleName,
                    Route = r.Route,
                    Description = r.Description,
                    IsModuleActive = r.IsModuleActive,
                    IsLicenseGated = r.IsLicenseGated,
                    Student = r.Student,
                    Faculty = r.Faculty,
                    Admin = r.Admin,
                    SuperAdmin = r.SuperAdmin,
                    University = r.University,
                    School = r.School,
                    College = r.College
                })
                .OrderBy(r => r.CapabilityName)
                .ToList();
        }
        catch (Exception ex)
        {
            model.Message = ex.Message;
        }

        return View(model);
    }

    // ── Timetable Admin ─────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> TimetableAdmin(
        Guid? timetableId,
        Guid? tenantId,
        Guid? campusId,
        Guid? departmentId,
        CancellationToken ct)
    {
        ViewData["Title"] = "Manage Timetables";
        var identity = _api.GetSessionIdentity();
        var vm = new TimetableAdminPageModel
        {
            IsConnected = _api.IsConnected(),
            Connection = _api.GetConnection(),
            Identity = identity,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId,
            SelectedDepartmentId = departmentId,
            Message = TempData["PortalMessage"]?.ToString()
        };

        if (!vm.IsConnected)
        {
            vm.Message ??= "Configure API connection on Dashboard first.";
            return View(vm);
        }

        try
        {
            if (identity?.IsSuperAdmin == true)
            {
                vm.Tenants = await _api.GetTenantsAsync(ct);
                if (vm.SelectedTenantId.HasValue)
                    vm.Campuses = await _api.GetCampusesAsync(vm.SelectedTenantId, ct);

                vm.Departments = await _api.GetDepartmentsAsync(vm.SelectedTenantId, vm.SelectedCampusId, ct);
            }
            else
            {
                vm.Departments = await _api.GetDepartmentsAsync(ct);
            }

            var selectedDepartment = vm.SelectedDepartmentId;
            if (!selectedDepartment.HasValue || vm.Departments.All(d => d.Id != selectedDepartment.Value))
                selectedDepartment = vm.Connection.DefaultDepartmentId ?? vm.Departments.FirstOrDefault()?.Id;

            vm.SelectedDepartmentId = selectedDepartment;
            vm.CreateForm.DepartmentId = selectedDepartment ?? Guid.Empty;

            vm.Programs = await _api.GetProgramsAsync(selectedDepartment, ct);
            vm.Semesters = await _api.GetSemestersAsync(ct);
            vm.Courses = await _api.GetCoursesAsync(selectedDepartment, ct);
            vm.Faculty = await _api.GetFacultyAsync(vm.SelectedTenantId, vm.SelectedCampusId, selectedDepartment, ct);
            vm.Buildings = await _api.GetBuildingsAsync(vm.SelectedTenantId, vm.SelectedCampusId, ct);
            vm.Rooms = await _api.GetRoomsAsync(ct);

            if (selectedDepartment.HasValue)
                vm.Timetables = await _api.GetTimetablesByDepartmentAsync(selectedDepartment.Value, vm.SelectedTenantId, vm.SelectedCampusId, ct);

            var activeTimetableId = timetableId ?? vm.Timetables.FirstOrDefault()?.Id;
            if (activeTimetableId.HasValue)
            {
                vm.SelectedTimetable = await _api.GetTimetableByIdAsync(activeTimetableId.Value, vm.SelectedTenantId, vm.SelectedCampusId, ct);
                vm.EntryForm.TimetableId = activeTimetableId.Value;
            }
        }
        catch (Exception ex)
        {
            vm.Message = ex.Message;
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTimetable(
        CreateTimetableForm form,
        Guid? tenantId,
        Guid? campusId,
        Guid? departmentId,
        CancellationToken ct)
    {
        try
        {
            var id = await _api.CreateTimetableAsync(form, tenantId, campusId, ct);
            TempData["PortalMessage"] = "Timetable created successfully.";
            return RedirectToAction(nameof(TimetableAdmin), new { timetableId = id, tenantId, campusId, departmentId = form.DepartmentId });
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
            return RedirectToAction(nameof(TimetableAdmin), new { tenantId, campusId, departmentId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTimetableEntry(
        AddTimetableEntryForm form,
        Guid? tenantId,
        Guid? campusId,
        Guid? departmentId,
        CancellationToken ct)
    {
        try
        {
            await _api.AddTimetableEntryAsync(form, tenantId, campusId, ct);
            TempData["PortalMessage"] = "Timetable entry added.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(TimetableAdmin), new { timetableId = form.TimetableId, tenantId, campusId, departmentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishTimetable(
        Guid timetableId,
        Guid? tenantId,
        Guid? campusId,
        Guid? departmentId,
        CancellationToken ct)
    {
        try
        {
            await _api.PublishTimetableAsync(timetableId, tenantId, campusId, ct);
            TempData["PortalMessage"] = "Timetable published.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(TimetableAdmin), new { timetableId, tenantId, campusId, departmentId });
    }

    // ── Timetable Student ───────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> TimetableStudent(
        Guid? departmentId,
        Guid? timetableId,
        Guid? tenantId,
        Guid? campusId,
        int? dayOfWeek,
        CancellationToken ct)
    {
        ViewData["Title"] = "Student Timetable";
        var identity = _api.GetSessionIdentity();
        var vm = new TimetableStudentPageModel
        {
            IsConnected = _api.IsConnected(),
            Identity = identity,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId,
            SelectedTimetableId = timetableId,
            SelectedDayOfWeek = dayOfWeek,
            Message = TempData["PortalMessage"]?.ToString()
        };

        if (!vm.IsConnected)
        {
            vm.Message ??= "Configure API connection on Dashboard first.";
            return View(vm);
        }

        try
        {
            var effectiveTenantId = identity?.IsSuperAdmin == true ? vm.SelectedTenantId : identity?.TenantId;
            var effectiveCampusId = identity?.IsSuperAdmin == true ? vm.SelectedCampusId : identity?.CampusId;

            if (identity?.IsSuperAdmin == true)
            {
                vm.Tenants = await _api.GetTenantsAsync(ct);
                if (vm.SelectedTenantId.HasValue)
                    vm.Campuses = await _api.GetCampusesAsync(vm.SelectedTenantId, ct);
            }

            vm.Departments = await _api.GetDepartmentsAsync(effectiveTenantId, effectiveCampusId, ct);

            vm.DepartmentId = departmentId ?? await GetEffectiveStudentDepartmentIdAsync(ct);
            if (vm.DepartmentId.HasValue && vm.Departments.All(d => d.Id != vm.DepartmentId.Value))
                vm.DepartmentId = vm.Departments.FirstOrDefault()?.Id;

            if (!vm.DepartmentId.HasValue)
            {
                vm.Message = "Department is required. Set default department in Dashboard connection.";
                return View(vm);
            }

            vm.Timetables = await _api.GetTimetablesByDepartmentAsync(vm.DepartmentId.Value, effectiveTenantId, effectiveCampusId, ct);
            var activeTimetableId = vm.SelectedTimetableId ?? vm.Timetables.FirstOrDefault()?.Id;
            vm.SelectedTimetableId = activeTimetableId;
            if (activeTimetableId.HasValue)
            {
                vm.SelectedTimetable = await _api.GetTimetableByIdAsync(activeTimetableId.Value, effectiveTenantId, effectiveCampusId, ct);

                if (vm.SelectedTimetable is not null && vm.SelectedDayOfWeek.HasValue)
                {
                    vm.SelectedTimetable.Entries = vm.SelectedTimetable.Entries
                        .Where(e => e.DayOfWeek == vm.SelectedDayOfWeek.Value)
                        .ToList();
                }
            }
        }
        catch (Exception ex)
        {
            vm.Message = ex.Message;
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateStudentTimetable(
        Guid timetableId,
        Guid? departmentId,
        Guid? tenantId,
        Guid? campusId,
        int? dayOfWeek,
        CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity?.IsAdmin != true && identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only Admin or SuperAdmin can activate student timetables.";
            return RedirectToAction(nameof(TimetableStudent), new { departmentId, timetableId, tenantId, campusId, dayOfWeek });
        }

        try
        {
            await _api.PublishTimetableAsync(timetableId, tenantId, campusId, ct);
            TempData["PortalMessage"] = "Student timetable activated.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(TimetableStudent), new { departmentId, timetableId, tenantId, campusId, dayOfWeek });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateStudentTimetable(
        Guid timetableId,
        Guid? departmentId,
        Guid? tenantId,
        Guid? campusId,
        int? dayOfWeek,
        CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity?.IsAdmin != true && identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only Admin or SuperAdmin can deactivate student timetables.";
            return RedirectToAction(nameof(TimetableStudent), new { departmentId, timetableId, tenantId, campusId, dayOfWeek });
        }

        try
        {
            await _api.UnpublishTimetableAsync(timetableId, tenantId, campusId, ct);
            TempData["PortalMessage"] = "Student timetable deactivated.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(TimetableStudent), new { departmentId, timetableId, tenantId, campusId, dayOfWeek });
    }

    // ── Timetable Teacher ───────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> TimetableTeacher(
        Guid? tenantId,
        Guid? campusId,
        bool includeInactive,
        CancellationToken ct)
    {
        ViewData["Title"] = "Teacher Timetable";
        var identity = _api.GetSessionIdentity();
        var vm = new TimetableTeacherPageModel
        {
            IsConnected = _api.IsConnected(),
            Identity = identity,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId,
            IncludeInactive = includeInactive,
            Message = TempData["PortalMessage"]?.ToString()
        };

        if (!vm.IsConnected)
        {
            vm.Message ??= "Configure API connection on Dashboard first.";
            return View(vm);
        }

        try
        {
            if (identity?.IsSuperAdmin == true)
            {
                vm.Tenants = await _api.GetTenantsAsync(ct);
                if (vm.SelectedTenantId.HasValue)
                    vm.Campuses = await _api.GetCampusesAsync(vm.SelectedTenantId, ct);
            }

            vm.Entries = await _api.GetTeacherEntriesAsync(vm.SelectedTenantId, vm.SelectedCampusId, vm.IncludeInactive, ct);
        }
        catch (Exception ex)
        {
            vm.Message = ex.Message;
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateTeacherTimetable(Guid timetableId, Guid? tenantId, Guid? campusId, bool includeInactive, CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity?.IsAdmin != true && identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only Admin or SuperAdmin can activate timetables.";
            return RedirectToAction(nameof(TimetableTeacher), new { tenantId, campusId, includeInactive });
        }

        try
        {
            await _api.PublishTimetableAsync(timetableId, tenantId, campusId, ct);
            TempData["PortalMessage"] = "Teacher timetable activated.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(TimetableTeacher), new { tenantId, campusId, includeInactive });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateTeacherTimetable(Guid timetableId, Guid? tenantId, Guid? campusId, bool includeInactive, CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity?.IsAdmin != true && identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only Admin or SuperAdmin can deactivate timetables.";
            return RedirectToAction(nameof(TimetableTeacher), new { tenantId, campusId, includeInactive });
        }

        try
        {
            await _api.UnpublishTimetableAsync(timetableId, tenantId, campusId, ct);
            TempData["PortalMessage"] = "Teacher timetable deactivated.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(TimetableTeacher), new { tenantId, campusId, includeInactive });
    }

    // ── Buildings ───────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Buildings(Guid? selectedId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        ViewData["Title"] = "Buildings";
        var identity = _api.GetSessionIdentity();
        var vm = new BuildingsPageModel
        {
            IsConnected = _api.IsConnected(),
            Message = TempData["PortalMessage"]?.ToString(),
            Identity = identity,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId
        };

        if (!vm.IsConnected)
        {
            vm.Message ??= "Configure API connection on Dashboard first.";
            return View(vm);
        }

        try
        {
            if (identity?.IsSuperAdmin == true)
            {
                vm.Tenants = await _api.GetTenantsAsync(ct);
                if (vm.SelectedTenantId.HasValue)
                    vm.Campuses = await _api.GetCampusesAsync(vm.SelectedTenantId, ct);
            }

            vm.Buildings = await _api.GetAllBuildingsAsync(activeOnly: false, vm.SelectedTenantId, vm.SelectedCampusId, ct);
            if (selectedId.HasValue)
            {
                vm.SelectedBuilding = vm.Buildings.FirstOrDefault(b => b.Id == selectedId);
                if (vm.SelectedBuilding is not null)
                {
                    vm.EditForm.Name = vm.SelectedBuilding.Name;
                    vm.EditForm.Code = vm.SelectedBuilding.Code;
                }
            }
        }
        catch (Exception ex)
        {
            vm.Message = ex.Message;
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBuilding(BuildingFormModel form, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        try
        {
            var created = await _api.CreateBuildingAsync(form, tenantId, campusId, ct);
            TempData["PortalMessage"] = $"Building '{created.Name}' created.";
            return RedirectToAction(nameof(Buildings), new { selectedId = created.Id, tenantId, campusId });
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
            return RedirectToAction(nameof(Buildings), new { tenantId, campusId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBuilding(Guid id, BuildingFormModel form, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        try
        {
            var updated = await _api.UpdateBuildingAsync(id, form, tenantId, campusId, ct);
            TempData["PortalMessage"] = $"Building '{updated.Name}' updated.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Buildings), new { selectedId = id, tenantId, campusId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetBuildingActive(Guid id, bool activate, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        try
        {
            if (activate)
                await _api.ActivateBuildingAsync(id, tenantId, campusId, ct);
            else
                await _api.DeactivateBuildingAsync(id, tenantId, campusId, ct);

            TempData["PortalMessage"] = activate ? "Building activated." : "Building deactivated.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Buildings), new { selectedId = id, tenantId, campusId });
    }

    // ── Rooms ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Rooms(Guid? buildingId, Guid? selectedId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        ViewData["Title"] = "Rooms";
        var identity = _api.GetSessionIdentity();
        var vm = new RoomsPageModel
        {
            IsConnected = _api.IsConnected(),
            Message = TempData["PortalMessage"]?.ToString(),
            Identity = identity,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId,
            SelectedBuildingId = buildingId
        };

        if (!vm.IsConnected)
        {
            vm.Message ??= "Configure API connection on Dashboard first.";
            return View(vm);
        }

        try
        {
            if (identity?.IsSuperAdmin == true)
            {
                vm.Tenants = await _api.GetTenantsAsync(ct);
                if (vm.SelectedTenantId.HasValue)
                    vm.Campuses = await _api.GetCampusesAsync(vm.SelectedTenantId, ct);
            }

            vm.Buildings = await _api.GetAllBuildingsAsync(activeOnly: false, vm.SelectedTenantId, vm.SelectedCampusId, ct);

            var activeBuildingId = buildingId ?? vm.Buildings.FirstOrDefault()?.Id;
            vm.SelectedBuildingId = activeBuildingId;

            if (activeBuildingId.HasValue)
            {
                vm.Rooms = await _api.GetRoomsForBuildingAsync(activeBuildingId.Value, activeOnly: false, vm.SelectedTenantId, vm.SelectedCampusId, ct);
                vm.CreateForm.BuildingId = activeBuildingId.Value;
            }

            if (selectedId.HasValue)
            {
                vm.SelectedRoom = vm.Rooms.FirstOrDefault(r => r.Id == selectedId);
                if (vm.SelectedRoom is not null)
                {
                    vm.EditForm.BuildingId = vm.SelectedRoom.BuildingId;
                    vm.EditForm.Number     = vm.SelectedRoom.Number;
                    vm.EditForm.Capacity   = vm.SelectedRoom.Capacity;
                }
            }
        }
        catch (Exception ex)
        {
            vm.Message = ex.Message;
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRoom(RoomFormModel form, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        try
        {
            var created = await _api.CreateRoomAsync(form, tenantId, campusId, ct);
            TempData["PortalMessage"] = $"Room '{created.Number}' created.";
            return RedirectToAction(nameof(Rooms), new { buildingId = created.BuildingId, selectedId = created.Id, tenantId, campusId });
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
            return RedirectToAction(nameof(Rooms), new { buildingId = form.BuildingId, tenantId, campusId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRoom(Guid id, Guid buildingId, RoomFormModel form, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        try
        {
            var updated = await _api.UpdateRoomAsync(id, form, tenantId, campusId, ct);
            TempData["PortalMessage"] = $"Room '{updated.Number}' updated.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Rooms), new { buildingId, selectedId = id, tenantId, campusId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRoomActive(Guid id, Guid buildingId, bool activate, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        try
        {
            if (activate)
                await _api.ActivateRoomAsync(id, tenantId, campusId, ct);
            else
                await _api.DeactivateRoomAsync(id, tenantId, campusId, ct);

            TempData["PortalMessage"] = activate ? "Room activated." : "Room deactivated.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Rooms), new { buildingId, selectedId = id, tenantId, campusId });
    }

    // ── License Update ─────────────────────────────────────────────────────

    public async Task<IActionResult> LicenseUpdate(CancellationToken ct)
    {
        var model = new LicenseUpdatePageModel { IsConnected = _api.IsConnected() };
        if (model.IsConnected)
        {
            try
            {
                var details = await _api.GetLicenseDetailsAsync(ct);
                model.Status       = details.Status;
                model.LicenseType  = details.LicenseType;
                model.ActivatedAt  = details.ActivatedAt;
                model.ExpiresAt    = details.ExpiresAt;
                model.UpdatedAt    = details.UpdatedAt;
                model.RemainingDays= details.RemainingDays;
                model.Message      = details.Message;
            }
            catch (Exception ex)
            {
                model.Message = $"Error loading license details: {ex.Message}";
            }
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadLicense(IFormFile licenseFile, CancellationToken ct)
    {
        if (licenseFile is null || licenseFile.Length == 0)
        {
            TempData["Message"] = "Please select a valid .tablic file.";
            return RedirectToAction(nameof(LicenseUpdate));
        }
        if (!licenseFile.FileName.EndsWith(".tablic", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Message"] = "Invalid file type. Only .tablic files are accepted.";
            return RedirectToAction(nameof(LicenseUpdate));
        }
        if (_api.IsConnected())
        {
            try
            {
                using var stream = licenseFile.OpenReadStream();
                var result = await _api.UploadLicenseAsync(stream, licenseFile.FileName, ct);
                TempData["Message"] = result;
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Upload error: {ex.Message}";
            }
        }
        return RedirectToAction(nameof(LicenseUpdate));
    }

    // ── Theme Settings ─────────────────────────────────────────────────────

    public async Task<IActionResult> ThemeSettings(CancellationToken ct)
    {
        var model = new ThemeSettingsPageModel { IsConnected = _api.IsConnected() };
        if (model.IsConnected)
        {
            try { model.CurrentTheme = await _api.GetCurrentThemeAsync(ct); }
            catch (Exception ex) { model.Message = $"Error loading theme: {ex.Message}"; }
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetTheme(string? themeKey, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                await _api.SetThemeAsync(themeKey, ct);
                TempData["Message"] = "Theme updated.";
            }
            catch (Exception ex) { TempData["Message"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(ThemeSettings));
    }

    // ── Report Settings ────────────────────────────────────────────────────

    public async Task<IActionResult> ReportSettings(CancellationToken ct)
    {
        var model = new ReportSettingsPageModel { IsConnected = _api.IsConnected() };
        if (model.IsConnected)
        {
            try { model.Reports = await _api.GetReportDefinitionsAsync(ct); }
            catch (Exception ex) { model.Message = $"Error loading reports: {ex.Message}"; }
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReport(CreateReportForm form, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.CreateReportDefinitionAsync(form, ct); TempData["Message"] = "Report created."; }
            catch (Exception ex) { TempData["Message"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(ReportSettings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleReport(Guid id, bool activate, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.SetReportActiveAsync(id, activate, ct); }
            catch (Exception ex) { TempData["Message"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(ReportSettings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateReportRoles(Guid id, [FromForm] bool adminAllowed,
        [FromForm] bool facultyAllowed, [FromForm] bool studentAllowed, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var roles = new List<string>();
                if (adminAllowed)   roles.Add("Admin");
                if (facultyAllowed) roles.Add("Faculty");
                if (studentAllowed) roles.Add("Student");
                await _api.SetReportRolesAsync(id, roles, ct);
                TempData["Message"] = "Roles updated.";
            }
            catch (Exception ex) { TempData["Message"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(ReportSettings));
    }

    // ── Module Settings ────────────────────────────────────────────────────

    public async Task<IActionResult> ModuleSettings(CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity?.IsSuperAdmin != true)
            return Forbid();

        var model = new ModuleSettingsPageModel { IsConnected = _api.IsConnected() };
        if (model.IsConnected)
        {
            try { model.Modules = await _api.GetModuleSettingsAsync(ct); }
            catch (Exception ex) { model.Message = $"Error loading modules: {ex.Message}"; }
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleModule(string key, bool activate, CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity?.IsSuperAdmin != true)
            return Forbid();

        if (_api.IsConnected())
        {
            try { await _api.SetModuleActiveAsync(key, activate, ct); }
            catch (Exception ex) { TempData["Message"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(ModuleSettings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateModuleRoles(string key, [FromForm] bool adminAllowed,
        [FromForm] bool facultyAllowed, [FromForm] bool studentAllowed, CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity?.IsSuperAdmin != true)
            return Forbid();

        if (_api.IsConnected())
        {
            try
            {
                var roles = new List<string>();
                if (adminAllowed)   roles.Add("Admin");
                if (facultyAllowed) roles.Add("Faculty");
                if (studentAllowed) roles.Add("Student");
                await _api.SetModuleRolesAsync(key, roles, ct);
                TempData["Message"] = "Roles updated.";
            }
            catch (Exception ex) { TempData["Message"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(ModuleSettings));
    }

    // ── Result Calculation ────────────────────────────────────────────────

    public async Task<IActionResult> ResultCalculation(int? selectedInstitutionType, CancellationToken ct)
    {
        var resolvedInstitutionType = selectedInstitutionType switch
        {
            1 => 1, // School
            2 => 2, // College
            _ => 0  // University
        };

        var model = new ResultCalculationSettingsPageModel
        {
            IsConnected = _api.IsConnected(),
            SelectedInstitutionType = resolvedInstitutionType
        };
        if (model.IsConnected)
        {
            try
            {
                model = await _api.GetResultCalculationSettingsAsync(resolvedInstitutionType, ct);
                model.IsConnected = true;
            }
            catch (Exception ex)
            {
                model.Message = $"Error loading result calculation settings: {ex.Message}";
            }
        }

        if (model.GpaRules.Count == 0)
        {
            model.GpaRules.Add(new ResultCalculationGpaRuleItem { DisplayOrder = 1, GradePoint = 2.0m, MinimumScore = 60m });
            model.GpaRules.Add(new ResultCalculationGpaRuleItem { DisplayOrder = 2, GradePoint = 2.5m, MinimumScore = 65m });
        }

        if (model.ComponentRules.Count == 0)
        {
            model.ComponentRules.Add(new ResultCalculationComponentRuleItem { DisplayOrder = 1, Name = "Quizzes", Weightage = 20m, IsActive = true });
            model.ComponentRules.Add(new ResultCalculationComponentRuleItem { DisplayOrder = 2, Name = "Midterms", Weightage = 30m, IsActive = true });
            model.ComponentRules.Add(new ResultCalculationComponentRuleItem { DisplayOrder = 3, Name = "Finals", Weightage = 50m, IsActive = true });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveResultCalculation(ResultCalculationSettingsPageModel model, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                model.GpaRules = model.GpaRules
                    .Where(r => r.GradePoint > 0 || r.MinimumScore > 0)
                    .ToList();
                model.ComponentRules = model.ComponentRules
                    .Where(r => !string.IsNullOrWhiteSpace(r.Name) && r.Weightage > 0)
                    .ToList();

                await _api.SaveResultCalculationSettingsAsync(model, model.SelectedInstitutionType, ct);
                TempData["Message"] = "Result calculation settings updated.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error: {ex.Message}";
            }
        }

        return RedirectToAction(nameof(ResultCalculation), new { selectedInstitutionType = model.SelectedInstitutionType });
    }

    // ── Sidebar Settings ────────────────────────────────────────────────────

    public async Task<IActionResult> SidebarSettings(CancellationToken ct)
    {
        var model = new SidebarSettingsPageModel { IsConnected = _api.IsConnected() };
        if (model.IsConnected)
        {
            try
            {
                model.TopLevelMenus = await _api.GetSidebarMenusAsync(ct);
            }
            catch (Exception ex)
            {
                model.Message = $"Error loading sidebar menus: {ex.Message}";
            }
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSidebarMenuRoles(Guid menuId, string menuName,
        [FromForm] bool adminAllowed, [FromForm] bool facultyAllowed, [FromForm] bool studentAllowed,
        CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var roles = new Dictionary<string, bool>
                {
                    ["Admin"]   = adminAllowed,
                    ["Faculty"] = facultyAllowed,
                    ["Student"] = studentAllowed
                };
                await _api.SetSidebarMenuRolesAsync(menuId, roles, ct);
                TempData["Message"] = $"Roles updated for \"{menuName}\".";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error: {ex.Message}";
            }
        }
        return RedirectToAction(nameof(SidebarSettings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSidebarMenuStatus(Guid menuId, string menuName,
        [FromForm] bool isActive, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                await _api.SetSidebarMenuStatusAsync(menuId, isActive, ct);
                TempData["Message"] = $"Status updated for \"{menuName}\".";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error: {ex.Message}";
            }
        }
        return RedirectToAction(nameof(SidebarSettings));
    }

    private IActionResult Section(string title, string description)
    {
        ViewData["Title"] = title;
        ViewData["SectionDescription"] = description;
        return View("Section");
    }

    // ── Notifications ──────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Notifications(CancellationToken ct)
    {
        ViewData["Title"] = "Notifications";
        var model = new NotificationsPageModel { IsConnected = _api.IsConnected() };
        if (!model.IsConnected) return View(model);
        try
        {
            model.Notifications = await _api.GetNotificationsAsync(ct);
            model.UnreadCount   = await _api.GetUnreadNotificationCountAsync(ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.MarkAllNotificationsReadAsync(ct); }
            catch { /* swallow */ }
        }
        return RedirectToAction(nameof(Notifications));
    }

    // Final-Touches Phase 6 Stage 6.1 — mark individual notification as read
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationRead(Guid id, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.MarkNotificationReadAsync(id, ct); }
            catch { /* swallow */ }
        }
        return RedirectToAction(nameof(Notifications));
    }

    // ── Students ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Students(Guid? departmentId, CancellationToken ct)
    {
        ViewData["Title"] = "Students";
        var model = new StudentsPageModel { IsConnected = _api.IsConnected(), SelectedDepartmentId = departmentId };
        if (!model.IsConnected) return View(model);
        try
        {
            model.Departments = await _api.GetDepartmentsAsync(ct);
            model.Students    = await _api.GetStudentsAsync(departmentId, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    // ── User Import ───────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> UserImport(Guid? tenantId, Guid? campusId, string? generatedSampleFileName, CancellationToken ct)
    {
        ViewData["Title"] = "User Import";
        var identity = _api.GetSessionIdentity();
        var model = new UserImportPageModel
        {
            IsConnected = _api.IsConnected(),
            Identity = identity,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId,
            GeneratedSampleFileName = generatedSampleFileName,
            Message = TempData["PortalMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);

        var canImport = identity?.IsAdmin == true || identity?.IsSuperAdmin == true;
        if (!canImport)
            model.Message = "Only Admin or SuperAdmin can import users.";

        if (identity?.IsSuperAdmin == true)
        {
            model.Tenants = await _api.GetTenantsAsync(ct);
            if (model.SelectedTenantId.HasValue)
                model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportUsersCsv(IFormFile? csvFile, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        ViewData["Title"] = "User Import";
        var identity = _api.GetSessionIdentity();
        var model = new UserImportPageModel
        {
            IsConnected = _api.IsConnected(),
            Identity = identity,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId
        };
        if (!model.IsConnected)
            return View("UserImport", model);

        var canImport = identity?.IsAdmin == true || identity?.IsSuperAdmin == true;
        if (!canImport)
        {
            model.Message = "Only Admin or SuperAdmin can import users.";
            return View("UserImport", model);
        }

        if (identity?.IsSuperAdmin == true)
        {
            model.Tenants = await _api.GetTenantsAsync(ct);
            if (model.SelectedTenantId.HasValue)
                model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);
        }

        if (csvFile is null || csvFile.Length == 0)
        {
            model.Message = "Please choose a non-empty CSV file.";
            return View("UserImport", model);
        }

        if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            model.Message = "Invalid file type. Only .csv files are accepted.";
            return View("UserImport", model);
        }

        try
        {
            using var stream = csvFile.OpenReadStream();
            model.Result = await _api.ImportUsersCsvAsync(stream, csvFile.FileName, tenantId, campusId, ct);
            model.Message = "CSV import completed.";
        }
        catch (Exception ex)
        {
            model.Message = $"Import failed: {ex.Message}";
        }

        return View("UserImport", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateUserImportSampleCsv(Guid? tenantId, Guid? campusId)
    {
        var identity = _api.GetSessionIdentity();
        var canImport = identity?.IsAdmin == true || identity?.IsSuperAdmin == true;
        if (!canImport)
            return Forbid();

        if (tenantId.HasValue != campusId.HasValue)
        {
            TempData["PortalMessage"] = "TenantId and CampusId must be provided together.";
            return RedirectToAction(nameof(UserImport), new { tenantId, campusId });
        }

        var templateRoot = GetUserImportSheetsRoot();
        Directory.CreateDirectory(templateRoot);

        var fileName = $"user-import-sample-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        var filePath = Path.Combine(templateRoot, fileName);

        var lines = new[]
        {
            "Username,Email,FullName,Role,DepartmentId,InstitutionType,MobileNumber,CampusAssignments",
            "admin1,admin1@tabsan.local,Admin One,Admin,,University,+61412345678,",
            "faculty1,faculty1@tabsan.local,Faculty One,Faculty,{valid-department-guid},University,+61412345679,{campus-guid-1}",
            "student1,student1@tabsan.local,Student One,Student,{valid-department-guid},School,+61412345680,{campus-guid-1}|{campus-guid-2}"
        };

        System.IO.File.WriteAllLines(filePath, lines);

        TempData["PortalMessage"] = $"Sample CSV created in User Import Sheets: {fileName}";
        return RedirectToAction(nameof(UserImport), new { tenantId, campusId, generatedSampleFileName = fileName });
    }

    [HttpGet]
    public IActionResult UserImportTemplate(string fileName)
    {
        var identity = _api.GetSessionIdentity();
        var canImport = identity?.IsAdmin == true || identity?.IsSuperAdmin == true;
        if (!canImport)
            return Forbid();

        if (string.IsNullOrWhiteSpace(fileName))
            return NotFound();

        var allowedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "faculty-admin-import-template.csv",
            "students-import-template.csv"
        };

        if (!allowedFileNames.Contains(fileName))
            return NotFound();

        var templateRoot = GetUserImportSheetsRoot();
        var candidatePath = Path.GetFullPath(Path.Combine(templateRoot, fileName));
        if (!candidatePath.StartsWith(templateRoot, StringComparison.OrdinalIgnoreCase))
            return NotFound();

        if (!System.IO.File.Exists(candidatePath))
            return NotFound();

        return PhysicalFile(candidatePath, "text/csv", fileName);
    }

    [HttpGet]
    public IActionResult DownloadGeneratedUserImportSample(string fileName)
    {
        var identity = _api.GetSessionIdentity();
        var canImport = identity?.IsAdmin == true || identity?.IsSuperAdmin == true;
        if (!canImport)
            return Forbid();

        if (string.IsNullOrWhiteSpace(fileName) ||
            !fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
            !fileName.StartsWith("user-import-sample-", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        var templateRoot = GetUserImportSheetsRoot();
        var candidatePath = Path.GetFullPath(Path.Combine(templateRoot, fileName));
        if (!candidatePath.StartsWith(templateRoot, StringComparison.OrdinalIgnoreCase))
            return NotFound();

        if (!System.IO.File.Exists(candidatePath))
            return NotFound();

        return PhysicalFile(candidatePath, "text/csv", fileName);
    }

    private string GetUserImportSheetsRoot()
        => Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "..", "User Import Sheets"));

    // ── Departments ────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Departments(Guid? selectedAdminUserId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        ViewData["Title"] = "Departments";
        var identity = _api.GetSessionIdentity();
        var model = new DepartmentsPageModel
        {
            IsConnected = _api.IsConnected(),
            Identity = identity,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId
        };
        if (!model.IsConnected) return View(model);
        try
        {
            if (identity?.IsSuperAdmin == true)
            {
                model.Tenants = await _api.GetTenantsAsync(ct);
                if (model.SelectedTenantId.HasValue)
                    model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);
            }

            model.Departments = await _api.GetDepartmentDetailsAsync(model.SelectedTenantId, model.SelectedCampusId, ct);

            if (identity?.IsSuperAdmin == true)
            {
                model.AdminUsers = (await _api.GetAdminUsersAsync(ct)).Where(a => a.IsActive).ToList();
                if (model.AdminUsers.Count > 0)
                {
                    model.SelectedAdminUserId = selectedAdminUserId.HasValue && model.AdminUsers.Any(a => a.Id == selectedAdminUserId.Value)
                        ? selectedAdminUserId
                        : model.AdminUsers[0].Id;

                    model.AssignedDepartmentIds = await _api.GetAdminDepartmentIdsAsync(model.SelectedAdminUserId.Value, ct);
                }
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        model.Message ??= TempData["PortalMessage"]?.ToString();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDepartment(string name, string code, int institutionType, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.CreateDepartmentAsync(name, code, institutionType, tenantId, campusId, ct); TempData["PortalMessage"] = $"Department '{name}' created."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Departments), new { tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDepartment(Guid id, string newName, int? institutionType, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.UpdateDepartmentAsync(id, newName, institutionType, tenantId, campusId, ct); TempData["PortalMessage"] = $"Department updated to '{newName}'."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Departments), new { tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDepartmentActive(Guid id, bool activate, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                if (activate)
                    await _api.ActivateDepartmentAsync(id, tenantId, campusId, ct);
                else
                    await _api.DeactivateDepartmentAsync(id, tenantId, campusId, ct);
                TempData["PortalMessage"] = activate ? "Department activated." : "Department deactivated.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Departments), new { tenantId, campusId });
    }

    [HttpGet]
    public async Task<IActionResult> TenantManagement(CancellationToken ct)
    {
        ViewData["Title"] = "Tenant Management";
        var model = new TenantManagementPageModel { IsConnected = _api.IsConnected() };
        if (!model.IsConnected) return View(model);

        var identity = _api.GetSessionIdentity();
        if (identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only SuperAdmin can manage tenants.";
            return RedirectToAction(nameof(Dashboard));
        }

        try { model.Tenants = await _api.GetTenantsAsync(ct); }
        catch (Exception ex) { model.Message = ex.Message; }
        model.Message ??= TempData["PortalMessage"]?.ToString();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTenant(string code, string name, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.CreateTenantAsync(code, name, ct); TempData["PortalMessage"] = "Tenant created."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(TenantManagement));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTenant(Guid id, string newName, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.UpdateTenantAsync(id, newName, ct); TempData["PortalMessage"] = "Tenant updated."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(TenantManagement));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTenant(Guid id, bool activate, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.SetTenantActiveAsync(id, activate, ct); }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(TenantManagement));
    }

    [HttpGet]
    public async Task<IActionResult> CampusManagement(Guid? tenantId, CancellationToken ct)
    {
        ViewData["Title"] = "Campus Management";
        var model = new CampusManagementPageModel { IsConnected = _api.IsConnected(), SelectedTenantId = tenantId };
        if (!model.IsConnected) return View(model);

        var identity = _api.GetSessionIdentity();
        if (identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only SuperAdmin can manage campuses.";
            return RedirectToAction(nameof(Dashboard));
        }

        try
        {
            model.Tenants = await _api.GetTenantsAsync(ct);
            model.Campuses = await _api.GetCampusesAsync(tenantId, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        model.Message ??= TempData["PortalMessage"]?.ToString();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCampus(Guid tenantId, string code, string name, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.CreateCampusAsync(tenantId, code, name, ct); TempData["PortalMessage"] = "Campus created."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(CampusManagement), new { tenantId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCampus(Guid id, Guid tenantId, string newName, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.UpdateCampusAsync(id, newName, ct); TempData["PortalMessage"] = "Campus updated."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(CampusManagement), new { tenantId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCampus(Guid id, Guid tenantId, bool activate, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.SetCampusActiveAsync(id, activate, ct); }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(CampusManagement), new { tenantId });
    }

    [HttpGet]
    public async Task<IActionResult> Programs(Guid? tenantId, Guid? campusId, Guid? departmentId, CancellationToken ct)
    {
        ViewData["Title"] = "Programs";
        var identity = _api.GetSessionIdentity();
        var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
        var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;

        var model = new ProgramsPageModel
        {
            IsConnected = _api.IsConnected(),
            Identity = identity,
            SelectedTenantId = effectiveTenantId,
            SelectedCampusId = effectiveCampusId,
            SelectedDepartmentId = departmentId
        };
        if (!model.IsConnected) return View(model);

        if (identity?.IsAdmin != true && identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only Admin or SuperAdmin can manage programs.";
            return RedirectToAction(nameof(Dashboard));
        }

        if (model.SelectedTenantId.HasValue != model.SelectedCampusId.HasValue)
        {
            model.Message = "Tenant and campus must be selected together.";
            return View(model);
        }

        try
        {
            if (identity?.IsSuperAdmin == true)
            {
                model.Tenants = await _api.GetTenantsAsync(ct);
                if (model.SelectedTenantId.HasValue)
                    model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);
            }

            model.Departments = await _api.GetDepartmentsAsync(model.SelectedTenantId, model.SelectedCampusId, ct);

            if (departmentId.HasValue && model.Departments.All(d => d.Id != departmentId.Value))
                model.SelectedDepartmentId = null;

            var programs = await _api.GetProgramDetailsAsync(model.SelectedDepartmentId, model.SelectedTenantId, model.SelectedCampusId, ct);
            var deptMap = model.Departments.ToDictionary(d => d.Id, d => d.Name);
            model.Programs = programs.Select(p =>
            {
                p.DepartmentName = deptMap.TryGetValue(p.DepartmentId, out var name) ? name : "Unknown";
                return p;
            }).ToList();
        }
        catch (Exception ex) { model.Message = ex.Message; }
        model.Message ??= TempData["PortalMessage"]?.ToString();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProgram(
        string name,
        string code,
        Guid departmentId,
        int totalSemesters,
        Guid? tenantId,
        Guid? campusId,
        Guid? filterDepartmentId,
        CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity?.IsAdmin != true && identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only Admin or SuperAdmin can create programs.";
            return RedirectToAction(nameof(Dashboard));
        }

        if (_api.IsConnected())
        {
            try
            {
                var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
                var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;

                await _api.CreateProgramAsync(name, code, departmentId, totalSemesters, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = $"Program '{name}' created.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Programs), new { tenantId, campusId, departmentId = filterDepartmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProgram(Guid id, string newName, Guid? tenantId, Guid? campusId, Guid? filterDepartmentId, CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity?.IsAdmin != true && identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only Admin or SuperAdmin can update programs.";
            return RedirectToAction(nameof(Dashboard));
        }

        if (_api.IsConnected())
        {
            try
            {
                var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
                var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;

                await _api.UpdateProgramAsync(id, newName, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "Program updated.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Programs), new { tenantId, campusId, departmentId = filterDepartmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetProgramActive(Guid id, bool activate, Guid? tenantId, Guid? campusId, Guid? filterDepartmentId, CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only SuperAdmin can change program status.";
            return RedirectToAction(nameof(Dashboard));
        }

        if (_api.IsConnected())
        {
            try
            {
                var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
                var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;

                if (activate)
                {
                    await _api.ActivateProgramAsync(id, effectiveTenantId, effectiveCampusId, ct);
                    TempData["PortalMessage"] = "Program activated.";
                }
                else
                {
                    await _api.DeactivateProgramAsync(id, effectiveTenantId, effectiveCampusId, ct);
                    TempData["PortalMessage"] = "Program deactivated.";
                }
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Programs), new { tenantId, campusId, departmentId = filterDepartmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAdminDepartmentAssignments(Guid adminUserId, Guid[] departmentIds, CancellationToken ct)
    {
        if (!_api.IsConnected())
            return RedirectToAction(nameof(Departments));

        var identity = _api.GetSessionIdentity();
        if (identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only SuperAdmin can update admin department assignments.";
            return RedirectToAction(nameof(Departments));
        }

        if (adminUserId == Guid.Empty)
        {
            TempData["PortalMessage"] = "Select an Admin user first.";
            return RedirectToAction(nameof(Departments));
        }

        try
        {
            await SyncAdminDepartmentAssignmentsAsync(adminUserId, departmentIds, ct);

            TempData["PortalMessage"] = "Admin department assignments updated.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToAction(nameof(Departments), new { selectedAdminUserId = adminUserId });
    }

    [HttpGet]
    public async Task<IActionResult> AdminUsers(Guid? selectedAdminUserId, CancellationToken ct)
    {
        ViewData["Title"] = "Admin Users";
        var model = new AdminUsersPageModel { IsConnected = _api.IsConnected() };
        if (!model.IsConnected) return View(model);

        var identity = _api.GetSessionIdentity();
        if (identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only SuperAdmin can manage Admin users.";
            return RedirectToAction(nameof(Dashboard));
        }

        try
        {
            model.AdminUsers = await _api.GetAdminUsersAsync(ct);
            model.Departments = await _api.GetDepartmentDetailsAsync(ct);
            if (model.AdminUsers.Count > 0)
            {
                model.SelectedAdminUserId = selectedAdminUserId.HasValue && model.AdminUsers.Any(a => a.Id == selectedAdminUserId.Value)
                    ? selectedAdminUserId
                    : model.AdminUsers[0].Id;

                model.AssignedDepartmentIds = await _api.GetAdminDepartmentIdsAsync(model.SelectedAdminUserId.Value, ct);
            }
        }
        catch (Exception ex)
        {
            model.Message = ex.Message;
        }

        model.Message ??= TempData["PortalMessage"]?.ToString();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAdminUser(string username, string? email, string password, int? institutionType, Guid[] departmentIds, CancellationToken ct)
    {
        if (!_api.IsConnected())
            return RedirectToAction(nameof(AdminUsers));

        var identity = _api.GetSessionIdentity();
        if (identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only SuperAdmin can create Admin users.";
            return RedirectToAction(nameof(AdminUsers));
        }

        try
        {
            var createdId = await _api.CreateAdminUserAsync(username, email, password, institutionType, ct);
            await SyncAdminDepartmentAssignmentsAsync(createdId, departmentIds, ct);
            TempData["PortalMessage"] = $"Admin user '{username}' created.";
            return RedirectToAction(nameof(AdminUsers), new { selectedAdminUserId = createdId });
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = $"Error: {ex.Message}";
            return RedirectToAction(nameof(AdminUsers));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAdminUser(Guid userId, string? email, bool isActive, string? newPassword, Guid[] departmentIds, CancellationToken ct)
    {
        if (!_api.IsConnected())
            return RedirectToAction(nameof(AdminUsers));

        var identity = _api.GetSessionIdentity();
        if (identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only SuperAdmin can update Admin users.";
            return RedirectToAction(nameof(AdminUsers));
        }

        if (userId == Guid.Empty)
        {
            TempData["PortalMessage"] = "Select an Admin user first.";
            return RedirectToAction(nameof(AdminUsers));
        }

        try
        {
            await _api.UpdateAdminUserAsync(userId, email, isActive, newPassword, ct);
            await SyncAdminDepartmentAssignmentsAsync(userId, departmentIds, ct);
            TempData["PortalMessage"] = "Admin user updated.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToAction(nameof(AdminUsers), new { selectedAdminUserId = userId });
    }

    private async Task SyncAdminDepartmentAssignmentsAsync(Guid adminUserId, IEnumerable<Guid>? departmentIds, CancellationToken ct)
    {
        var selected = departmentIds?.Where(x => x != Guid.Empty).Distinct().ToHashSet() ?? new HashSet<Guid>();
        var current = (await _api.GetAdminDepartmentIdsAsync(adminUserId, ct)).ToHashSet();

        foreach (var departmentId in selected.Except(current))
            await _api.AssignAdminToDepartmentAsync(adminUserId, departmentId, ct);

        foreach (var departmentId in current.Except(selected))
            await _api.RemoveAdminFromDepartmentAsync(adminUserId, departmentId, ct);
    }

    // ── Courses ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Courses(Guid? tenantId, Guid? campusId, Guid? departmentId, CancellationToken ct)
    {
        ViewData["Title"] = "Courses & Offerings";
        var sessionId = _api.GetSessionIdentity();
        var model = new CoursesPageModel
        {
            IsConnected = _api.IsConnected(),
            Identity = sessionId,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId,
            SelectedDepartmentId = departmentId
        };
        if (!model.IsConnected) return View(model);
        try
        {
            if (sessionId?.IsSuperAdmin == true)
            {
                model.Tenants = await _api.GetTenantsAsync(ct);
                if (model.SelectedTenantId.HasValue)
                    model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);

                model.Departments = await _api.GetDepartmentsAsync(model.SelectedTenantId, model.SelectedCampusId, ct);
            }
            else
            {
                model.Departments = await _api.GetDepartmentsAsync(ct);
            }

            if (model.SelectedDepartmentId.HasValue && model.Departments.All(d => d.Id != model.SelectedDepartmentId.Value))
                model.SelectedDepartmentId = null;

            model.Semesters   = await _api.GetSemestersAsync(ct);
            if (sessionId?.IsAdmin == true || sessionId?.IsSuperAdmin == true)
                model.Faculty = await _api.GetFacultyAsync(model.SelectedTenantId, model.SelectedCampusId, model.SelectedDepartmentId, ct);

            var selectedInstitutionType = model.Departments
                .FirstOrDefault(d => d.Id == model.SelectedDepartmentId)?.InstitutionType;

            model.Courses = await _api.GetCourseDetailsAsync(model.SelectedDepartmentId, model.SelectedTenantId, model.SelectedCampusId, selectedInstitutionType, ct);
            model.Offerings = await _api.GetCourseOfferingsAsync(model.SelectedDepartmentId, model.SelectedTenantId, model.SelectedCampusId, selectedInstitutionType, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        model.Message ??= TempData["PortalMessage"]?.ToString();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCourse(
        string code, string title, int creditHours, Guid departmentId, Guid? filterDepartmentId, Guid? tenantId, Guid? campusId,
        bool hasSemesters, int? totalSemesters, int? durationValue, string? durationUnit, string? gradingType,
        CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                await _api.CreateCourseAsync(code, title, creditHours, departmentId,
                    hasSemesters, totalSemesters, durationValue, durationUnit, gradingType,
                    tenantId, campusId, null, ct);
                TempData["PortalMessage"] = $"Course '{code}' created.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Courses), new { tenantId, campusId, departmentId = filterDepartmentId });
    }

    // Final-Touches Phase 19 Stage 19.4 — GradingConfig page (GET)
    public async Task<IActionResult> GradingConfig(Guid? courseId, CancellationToken ct)
    {
        var model = new GradingConfigPageModel { IsConnected = _api.IsConnected() };
        if (model.IsConnected)
        {
            try
            {
                var session = _api.GetSessionIdentity();
                model.CanManageInstitutionSections = session?.IsSuperAdmin == true;

                if (model.CanManageInstitutionSections)
                {
                    var profiles = await _api.GetInstitutionGradingProfilesAsync(ct);
                    model.InstitutionSections = BuildInstitutionGradingSections(profiles);
                }

                var courses = await _api.GetCourseDetailsAsync(null, null, null, null, ct);
                model.Courses = courses;
                model.SelectedCourseId = courseId;
                if (courseId.HasValue)
                {
                    var config = await _api.GetCourseGradingConfigAsync(courseId.Value, ct);
                    if (config != null)
                    {
                        model.PassThreshold = config.PassThreshold;
                        model.GradingType   = config.GradingType ?? "GPA";
                        if (!string.IsNullOrWhiteSpace(config.GradeRangesJson))
                            model.GradeRanges = System.Text.Json.JsonSerializer.Deserialize<List<GradeRangeItem>>(config.GradeRangesJson) ?? new();
                    }
                }
            }
            catch (Exception ex)
            {
                model.ErrorMessage = ex.Message;
            }
        }
        model.SuccessMessage = TempData["PortalMessage"] as string;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveInstitutionGradingProfile(
        int institutionType,
        decimal passThreshold,
        string? gradeRangesJson,
        bool isActive,
        Guid? courseId,
        CancellationToken ct)
    {
        if (!_api.IsConnected())
            return RedirectToAction(nameof(GradingConfig), new { courseId });

        var session = _api.GetSessionIdentity();
        if (session?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only SuperAdmin can update institution grading sections.";
            return RedirectToAction(nameof(GradingConfig), new { courseId });
        }

        try
        {
            await _api.SaveInstitutionGradingProfileAsync(institutionType, passThreshold, gradeRangesJson, isActive, ct);
            TempData["PortalMessage"] = "Institution grading section saved.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToAction(nameof(GradingConfig), new { courseId });
    }

    // Final-Touches Phase 19 Stage 19.4 — GradingConfig save (POST)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveGradingConfig(Guid courseId, decimal passThreshold, string gradingType, string? gradeRangesJson, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                await _api.SaveCourseGradingConfigAsync(courseId, passThreshold, gradingType, gradeRangesJson, ct);
                TempData["PortalMessage"] = "Grading configuration saved.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(GradingConfig), new { courseId });
    }

    private static List<InstitutionGradingSectionItem> BuildInstitutionGradingSections(
        IReadOnlyCollection<InstitutionGradingProfileApiModel> profiles)
    {
        return
        [
            BuildInstitutionSection(1, "School Grading", 40m, 0m, 100m, profiles),
            BuildInstitutionSection(2, "College Grading", 40m, 0m, 100m, profiles),
            BuildInstitutionSection(3, "University Grading", 2m, 0m, 4m, profiles)
        ];
    }

    private static InstitutionGradingSectionItem BuildInstitutionSection(
        int institutionType,
        string title,
        decimal defaultThreshold,
        decimal min,
        decimal max,
        IReadOnlyCollection<InstitutionGradingProfileApiModel> profiles)
    {
        var existing = profiles.FirstOrDefault(p => p.InstitutionType == institutionType);
        return new InstitutionGradingSectionItem
        {
            InstitutionType = institutionType,
            Title = title,
            PassThreshold = existing?.PassThreshold ?? defaultThreshold,
            MinThreshold = min,
            MaxThreshold = max,
            IsActive = existing?.IsActive ?? true,
            GradeRangesJson = existing?.GradeRangesJson
        };
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOffering(Guid courseId, Guid semesterId, int maxEnrollment, Guid? facultyUserId, Guid? tenantId, Guid? campusId, Guid? departmentId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.CreateOfferingAsync(courseId, semesterId, maxEnrollment, facultyUserId == Guid.Empty ? null : facultyUserId, tenantId, campusId, null, ct); TempData["PortalMessage"] = "Course offering created."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Courses), new { tenantId, campusId, departmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateCourse(Guid id, Guid? tenantId, Guid? campusId, Guid? departmentId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.DeactivateCourseAsync(id, tenantId, campusId, null, ct); TempData["PortalMessage"] = "Course deactivated."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Courses), new { tenantId, campusId, departmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOffering(Guid id, Guid? tenantId, Guid? campusId, Guid? departmentId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.DeleteOfferingAsync(id, tenantId, campusId, null, ct); TempData["PortalMessage"] = "Offering deleted."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Courses), new { tenantId, campusId, departmentId });
    }

    // ── Assignments ────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Assignments(Guid? offeringId, string? semesterName, Guid? selectedAssignmentId, Guid? tenantId, Guid? campusId, bool includeInactive = false, CancellationToken ct = default)
    {
        ViewData["Title"] = "Assignments";
        var identity = _api.GetSessionIdentity();
        var model = new AssignmentsPageModel
        {
            IsConnected          = _api.IsConnected(),
            Identity             = identity,
            SelectedTenantId     = tenantId,
            SelectedCampusId     = campusId,
            IncludeInactive      = includeInactive,
            SelectedOfferingId   = offeringId,
            SelectedSemesterName = semesterName,
            SelectedAssignmentId = selectedAssignmentId,
            Message              = TempData["PortalMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var sessionId = identity;
            if (sessionId?.IsSuperAdmin == true)
            {
                model.Tenants = await _api.GetTenantsAsync(ct);
                if (model.SelectedTenantId.HasValue)
                    model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);
            }

            if (sessionId?.IsStudent == true)
            {
                var allOfferings = await GetOfferingFilterOptionsAsync(sessionId, ct, model.SelectedTenantId, model.SelectedCampusId);
                model.SemesterOptions = BuildSemesterOptions(allOfferings);
                model.CourseOfferings = FilterOfferingsBySemester(allOfferings, semesterName);

                if (offeringId.HasValue)
                {
                    model.Assignments = await _api.GetAssignmentsByOfferingAsync(offeringId.Value, false, model.SelectedTenantId, model.SelectedCampusId, ct);
                }
                else if (!string.IsNullOrWhiteSpace(semesterName))
                {
                    var assignmentBatches = await Task.WhenAll(
                        model.CourseOfferings.Select(o => _api.GetAssignmentsByOfferingAsync(o.Id, false, model.SelectedTenantId, model.SelectedCampusId, ct)));

                    model.Assignments = assignmentBatches
                        .SelectMany(a => a)
                        .GroupBy(a => a.Id)
                        .Select(g => g.First())
                        .ToList();
                }
                else
                {
                    model.Assignments = await _api.GetMyAssignmentsAsync(ct);
                }

                var mySubmissions = await _api.GetMyAssignmentSubmissionsAsync(ct);
                var submissionByAssignment = mySubmissions
                    .OrderByDescending(s => s.SubmittedAt)
                    .GroupBy(s => s.AssignmentId)
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var assignment in model.Assignments)
                {
                    if (submissionByAssignment.TryGetValue(assignment.Id, out var submission))
                    {
                        assignment.IsSubmitted = true;
                        assignment.MarksAwarded = submission.MarksAwarded;
                    }
                }
            }
            else if (offeringId.HasValue)
            {
                model.Assignments = await _api.GetAssignmentsByOfferingAsync(offeringId.Value, model.IncludeInactive, model.SelectedTenantId, model.SelectedCampusId, ct);
                if (selectedAssignmentId.HasValue)
                    model.Submissions = await _api.GetSubmissionsForAssignmentAsync(selectedAssignmentId.Value, ct);

                model.CourseOfferings = await GetOfferingFilterOptionsAsync(sessionId, ct, model.SelectedTenantId, model.SelectedCampusId);
                model.SemesterOptions = BuildSemesterOptions(model.CourseOfferings);
            }
            else
            {
                model.CourseOfferings = await GetOfferingFilterOptionsAsync(sessionId, ct, model.SelectedTenantId, model.SelectedCampusId);
                model.SemesterOptions = BuildSemesterOptions(model.CourseOfferings);
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    // ── Attendance ─────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Attendance(Guid? offeringId, Guid? tenantId, Guid? campusId, CancellationToken ct)
        => await RenderAttendanceAsync(offeringId, tenantId, campusId, nameof(Attendance), "Attendance", ct);

    [HttpGet]
    public async Task<IActionResult> EnterAttendance(Guid? offeringId, Guid? tenantId, Guid? campusId, CancellationToken ct)
        => await RenderAttendanceAsync(offeringId, tenantId, campusId, nameof(EnterAttendance), "Enter Attendance", ct);

    private async Task<IActionResult> RenderAttendanceAsync(
        Guid? offeringId,
        Guid? tenantId,
        Guid? campusId,
        string attendanceAction,
        string title,
        CancellationToken ct)
    {
        ViewData["Title"] = title;
        ViewData["AttendanceAction"] = attendanceAction;
        var model = new AttendancePageModel
        {
            IsConnected      = _api.IsConnected(),
            SelectedOfferingId = offeringId,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId,
            Message          = TempData["PortalMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var sessionId = _api.GetSessionIdentity();
            var effectiveTenantId = sessionId?.IsSuperAdmin == true ? model.SelectedTenantId : sessionId?.TenantId;
            var effectiveCampusId = sessionId?.IsSuperAdmin == true ? model.SelectedCampusId : sessionId?.CampusId;

            if (sessionId?.IsSuperAdmin == true)
            {
                model.Tenants = await _api.GetTenantsAsync(ct);
                if (model.SelectedTenantId.HasValue)
                    model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);
            }

            if (sessionId?.IsStudent == true)
            {
                model.Summary = await _api.GetMyAttendanceSummaryAsync(ct);
                model.LowAttendance = model.Summary
                    .Where(s => s.AttendancePercentage < 75)
                    .OrderBy(s => s.AttendancePercentage)
                    .ToList();
            }
            else if (offeringId.HasValue)
            {
                model.Records = await _api.GetAttendanceByOfferingAsync(offeringId.Value, effectiveTenantId, effectiveCampusId, ct);
                model.Roster  = await _api.GetEnrollmentRosterAsync(offeringId.Value, effectiveTenantId, effectiveCampusId, ct);

                model.Summary = model.Records
                    .GroupBy(r => new { r.StudentProfileId, r.StudentName, r.RegistrationNumber })
                    .Select(g =>
                    {
                        var total = g.Count();
                        var present = g.Count(x => string.Equals(x.Status, "Present", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(x.Status, "Late", StringComparison.OrdinalIgnoreCase));
                        var pct = total > 0 ? Math.Round((double)present / total * 100, 2) : 0.0;
                        return new AttendanceSummaryItem
                        {
                            StudentId = g.Key.StudentProfileId,
                            StudentName = g.Key.StudentName,
                            RegistrationNumber = g.Key.RegistrationNumber,
                            CourseName = model.CourseOfferings.FirstOrDefault(o => o.Id == offeringId.Value)?.Name ?? string.Empty,
                            TotalClasses = total,
                            PresentCount = present,
                            AttendancePercentage = pct
                        };
                    })
                    .OrderBy(s => s.StudentName)
                    .ToList();

                model.LowAttendance = model.Summary
                    .Where(s => s.AttendancePercentage < 75)
                    .OrderBy(s => s.AttendancePercentage)
                    .ToList();
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    // ── Results ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Results(Guid? offeringId, string? semesterName, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        ViewData["Title"] = "Results";
        var identity = _api.GetSessionIdentity();
        var model = new ResultsPageModel
        {
            IsConnected      = _api.IsConnected(),
            Identity         = identity,
            SelectedOfferingId = offeringId,
            SelectedSemesterName = semesterName,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId,
            Message          = TempData["PortalMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var effectiveTenantId = identity?.IsSuperAdmin == true ? model.SelectedTenantId : identity?.TenantId;
            var effectiveCampusId = identity?.IsSuperAdmin == true ? model.SelectedCampusId : identity?.CampusId;

            if (identity?.IsSuperAdmin == true)
            {
                model.Tenants = await _api.GetTenantsAsync(ct);
                if (model.SelectedTenantId.HasValue)
                    model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);
            }

            var allOfferings = await GetOfferingFilterOptionsAsync(identity, ct, effectiveTenantId, effectiveCampusId);
            model.SemesterOptions = BuildSemesterOptions(allOfferings);
            model.Offerings = FilterOfferingsBySemester(allOfferings, semesterName);

            if (identity?.IsStudent == true)
            {
                model.Results = await _api.GetMyResultsAsync(effectiveTenantId, effectiveCampusId, ct);

                if (offeringId.HasValue)
                {
                    model.Results = model.Results
                        .Where(r => r.CourseOfferingId == offeringId.Value)
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(semesterName))
                {
                    model.Results = model.Results
                        .Where(r => string.Equals(r.SemesterName, semesterName, StringComparison.OrdinalIgnoreCase)
                                    || string.IsNullOrWhiteSpace(r.SemesterName))
                        .ToList();
                }

                // Stage 4.6: Completed FYP should be visible in student results.
                // Represent it as a published synthetic result row when no offering filter is active.
                if (!offeringId.HasValue)
                {
                    var myProjects = await _api.GetMyFypProjectsAsync(effectiveTenantId, effectiveCampusId, ct);
                    var completedFyp = myProjects.FirstOrDefault(p => string.Equals(p.Status, "Completed", StringComparison.OrdinalIgnoreCase));
                    if (completedFyp is not null)
                    {
                        var shouldShowForSemester = string.IsNullOrWhiteSpace(semesterName)
                            || semesterName.Contains("8", StringComparison.OrdinalIgnoreCase)
                            || semesterName.Contains("final", StringComparison.OrdinalIgnoreCase);

                        if (shouldShowForSemester)
                        {
                            var exists = model.Results.Any(r => string.Equals(r.ResultType, "FYP", StringComparison.OrdinalIgnoreCase));
                            if (!exists)
                            {
                                model.Results.Add(new ResultItem
                                {
                                    Id = completedFyp.Id,
                                    StudentProfileId = Guid.Empty,
                                    ResultType = "FYP",
                                    CourseName = completedFyp.Title,
                                    CourseCode = "FYP",
                                    MarksObtained = null,
                                    TotalMarks = 0,
                                    LetterGrade = string.IsNullOrWhiteSpace(completedFyp.FinalResult) ? "Completed" : completedFyp.FinalResult,
                                    IsPublished = true,
                                    SemesterName = "Semester 8",
                                    StudentName = completedFyp.StudentName,
                                    RegistrationNumber = string.Empty
                                });
                            }
                        }
                    }
                }

                model.MyRecheckRequests = await _api.GetMyResultRecheckRequestsAsync(ct);
            }
            else if (offeringId.HasValue)
            {
                model.Results   = await _api.GetResultsByOfferingAsync(offeringId.Value, effectiveTenantId, effectiveCampusId, ct);
                model.Roster    = await _api.GetEnrollmentRosterAsync(offeringId.Value, effectiveTenantId, effectiveCampusId, ct);
            }

            if (identity?.IsFaculty == true)
            {
                model.MyModificationRequests = await _api.GetMyResultModificationRequestsAsync(ct);
            }

            if (identity?.IsAdmin == true || identity?.IsSuperAdmin == true)
            {
                model.PendingModificationRequests = await _api.GetPendingResultModificationRequestsAsync(ct);
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    // ── Quizzes ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Quizzes(Guid? offeringId, string? semesterName, Guid? tenantId, Guid? campusId, bool includeInactive, CancellationToken ct)
    {
        ViewData["Title"] = "Quizzes";
        var identity = _api.GetSessionIdentity();
        var model = new QuizzesPageModel
        {
            IsConnected      = _api.IsConnected(),
            Identity         = identity,
            SelectedOfferingId = offeringId,
            SelectedSemesterName = semesterName,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId,
            IncludeInactive = includeInactive,
            Message          = TempData["PortalMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var effectiveTenantId = identity?.IsSuperAdmin == true ? model.SelectedTenantId : identity?.TenantId;
            var effectiveCampusId = identity?.IsSuperAdmin == true ? model.SelectedCampusId : identity?.CampusId;

            if (identity?.IsSuperAdmin == true)
            {
                model.Tenants = await _api.GetTenantsAsync(ct);
                if (model.SelectedTenantId.HasValue)
                    model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);
            }

            var allOfferings = await GetOfferingFilterOptionsAsync(identity, ct, effectiveTenantId, effectiveCampusId);
            model.SemesterOptions = BuildSemesterOptions(allOfferings);
            model.CourseOfferings = FilterOfferingsBySemester(allOfferings, semesterName);

            if (offeringId.HasValue)
                model.Quizzes = await _api.GetQuizzesByOfferingAsync(offeringId.Value, effectiveTenantId, effectiveCampusId, model.IncludeInactive, ct);
            else if (identity?.IsStudent == true && !string.IsNullOrWhiteSpace(semesterName))
            {
                var quizBatches = await Task.WhenAll(
                    model.CourseOfferings.Select(o => _api.GetQuizzesByOfferingAsync(o.Id, effectiveTenantId, effectiveCampusId, false, ct)));

                model.Quizzes = quizBatches
                    .SelectMany(q => q)
                    .GroupBy(q => q.Id)
                    .Select(g => g.First())
                    .ToList();
            }

            if (identity?.IsStudent == true)
                model.MyAttempts = await _api.GetMyAttemptsAsync(ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    // ── FYP ────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Fyp(Guid? departmentId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        ViewData["Title"] = "FYP Management";
        var identity = _api.GetSessionIdentity();
        var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
        var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
        var model = new FypPageModel
        {
            IsConnected         = _api.IsConnected(),
            Identity            = identity,
            SelectedTenantId    = tenantId,
            SelectedCampusId    = campusId,
            SelectedDepartmentId = departmentId,
            Message             = TempData["PortalMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            if (identity?.IsSuperAdmin == true)
            {
                model.Tenants = await _api.GetTenantsAsync(ct);
                if (model.SelectedTenantId.HasValue)
                    model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);
            }

            var sessionId = identity;
            if (sessionId?.IsStudent == true)
            {
                var profile = await _api.GetMyStudentProfileAsync(ct);
                if (profile is null)
                {
                    TempData["PortalMessage"] = "Student profile not found.";
                    return RedirectToAction(nameof(Dashboard));
                }

                if (profile.InstitutionType != 0)
                {
                    TempData["PortalMessage"] = "FYP is available for University students only.";
                    return RedirectToAction(nameof(Dashboard));
                }

                if (profile.TotalSemesters <= 0 || profile.CurrentSemesterNumber != profile.TotalSemesters)
                {
                    TempData["PortalMessage"] = $"FYP is available only in last semester (Semester {profile.TotalSemesters}).";
                    return RedirectToAction(nameof(Dashboard));
                }

                model.Projects = await _api.GetMyFypProjectsAsync(effectiveTenantId, effectiveCampusId, ct);
            }
            // Issue-Fix Phase 3 Stage 3.8 — Faculty FYP workflow: load supervised projects + student list for FYP creation.
            else if (sessionId?.IsFaculty == true)
            {
                model.Departments = await _api.GetDepartmentsAsync(effectiveTenantId, effectiveCampusId, ct);
                model.UpcomingMeetings = await _api.GetUpcomingMeetingsAsync(effectiveTenantId, effectiveCampusId, ct);
                model.Projects = await _api.GetMySupervisedProjectsAsync(effectiveTenantId, effectiveCampusId, ct);

                // Load students so faculty can create an FYP record for a student.
                var deptToLoad = departmentId ?? model.Departments.FirstOrDefault()?.Id;
                if (deptToLoad.HasValue)
                    model.Students = await _api.GetStudentsAsync(deptToLoad, ct);
            }
            else if (departmentId.HasValue)
            {
                model.Departments = await _api.GetDepartmentsAsync(effectiveTenantId, effectiveCampusId, ct);
                model.Faculty = await _api.GetFacultyAsync(ct);
                model.Students = await _api.GetStudentsAsync(departmentId, ct);
                model.UpcomingMeetings = await _api.GetUpcomingMeetingsAsync(effectiveTenantId, effectiveCampusId, ct);
                model.Projects = await _api.GetFypByDepartmentAsync(departmentId.Value, effectiveTenantId, effectiveCampusId, ct);
            }
            else if (sessionId?.IsAdmin == true || sessionId?.IsSuperAdmin == true)
            {
                model.Departments = await _api.GetDepartmentsAsync(effectiveTenantId, effectiveCampusId, ct);
                model.Faculty = await _api.GetFacultyAsync(ct);
                model.Students = await _api.GetStudentsAsync(null, ct);
                model.UpcomingMeetings = await _api.GetUpcomingMeetingsAsync(effectiveTenantId, effectiveCampusId, ct);
                model.Projects = await _api.GetAllFypProjectsAsync(effectiveTenantId, effectiveCampusId, ct);
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    // ── Analytics ──────────────────────────────────────────────────────────

    // ── Assignment write actions ────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitAssignment(
        Guid assignmentId, Guid? offeringId, string? semesterName, string? textContent, IFormFile? submissionFile, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                string? fileUrl = null;

                if (submissionFile is { Length: > 0 })
                {
                    // Validate before writing to disk — size, extension, and MIME check
                    const long maxSubmissionBytes = 5 * 1024 * 1024;
                    var allowedSubmissionExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                        { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };

                    if (submissionFile.Length > maxSubmissionBytes)
                    {
                        TempData["PortalMessage"] = "File exceeds the maximum allowed size of 5 MB.";
                        return RedirectToAction(nameof(Assignments), new { offeringId, semesterName });
                    }

                    var submissionExt = Path.GetExtension(submissionFile.FileName);
                    if (string.IsNullOrEmpty(submissionExt) || !allowedSubmissionExts.Contains(submissionExt))
                    {
                        TempData["PortalMessage"] = $"File type '{submissionExt}' is not permitted. Allowed: .pdf, .jpg, .jpeg, .png, .doc, .docx";
                        return RedirectToAction(nameof(Assignments), new { offeringId, semesterName });
                    }

                    var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "assignment-submissions");
                    Directory.CreateDirectory(uploadsRoot);

                    var fileName = $"{Guid.NewGuid()}{submissionExt}";
                    var physicalPath = Path.Combine(uploadsRoot, fileName);

                    await using var stream = System.IO.File.Create(physicalPath);
                    await submissionFile.CopyToAsync(stream, ct);

                    fileUrl = $"/uploads/assignment-submissions/{fileName}";
                }

                if (string.IsNullOrWhiteSpace(fileUrl) && string.IsNullOrWhiteSpace(textContent))
                {
                    TempData["PortalMessage"] = "Attach a file or add submission text before submitting.";
                    return RedirectToAction(nameof(Assignments), new { offeringId, semesterName });
                }

                await _api.SubmitAssignmentAsync(assignmentId, fileUrl, textContent, ct);
                TempData["PortalMessage"] = "Assignment submitted for faculty review.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }

        return RedirectToAction(nameof(Assignments), new { offeringId, semesterName });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAssignment(
        Guid offeringId, string title, string? description,
        Guid? tenantId, Guid? campusId,
        DateTime dueDate, decimal maxMarks, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.CreateAssignmentAsync(offeringId, title, description, dueDate, maxMarks, tenantId, campusId, ct); }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Assignments), new { offeringId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAssignment(
        Guid id, Guid offeringId, string title, string? description,
        Guid? tenantId, Guid? campusId,
        DateTime dueDate, decimal maxMarks, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.UpdateAssignmentAsync(id, title, description, dueDate, maxMarks, tenantId, campusId, ct); TempData["PortalMessage"] = "Assignment updated."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Assignments), new { offeringId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishAssignment(Guid id, Guid? offeringId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.PublishAssignmentAsync(id, tenantId, campusId, ct); }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Assignments), new { offeringId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAssignmentActive(Guid id, bool activate, Guid? offeringId, Guid? tenantId, Guid? campusId, bool includeInactive = true, CancellationToken ct = default)
    {
        if (_api.IsConnected())
        {
            try
            {
                if (activate)
                    await _api.ActivateAssignmentAsync(id, tenantId, campusId, ct);
                else
                    await _api.DeactivateAssignmentAsync(id, tenantId, campusId, ct);
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Assignments), new { offeringId, tenantId, campusId, includeInactive });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GradeSubmission(
        Guid assignmentId, Guid studentProfileId, Guid? offeringId,
        decimal marksAwarded, string? feedback, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.GradeSubmissionAsync(assignmentId, studentProfileId, marksAwarded, feedback, ct); }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Assignments), new { offeringId, selectedAssignmentId = assignmentId });
    }

    // ── Attendance write actions ────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> DownloadAttendanceCsvTemplate(Guid? offeringId, Guid? tenantId, Guid? campusId, string? entryPoint, CancellationToken ct)
    {
        if (!_api.IsConnected())
        {
            TempData["PortalMessage"] = "Connect to the API before downloading the attendance template.";
            return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId });
        }

        var identity = _api.GetSessionIdentity();
        if (identity is null || !(identity.IsFaculty || identity.IsAdmin || identity.IsSuperAdmin))
            return Forbid();

        var effectiveTenantId = identity.IsSuperAdmin ? tenantId : identity.TenantId;
        var effectiveCampusId = identity.IsSuperAdmin ? campusId : identity.CampusId;

        var rows = new List<(Guid StudentId, string StudentName)>();
        if (offeringId.HasValue)
        {
            try
            {
                var roster = await _api.GetEnrollmentRosterAsync(offeringId.Value, effectiveTenantId, effectiveCampusId, ct);
                rows = roster
                    .Take(2)
                    .Select(r => (r.Id, r.StudentName))
                    .ToList();
            }
            catch
            {
                // Keep template generation resilient even if roster lookup fails.
            }
        }

        while (rows.Count < 2)
            rows.Add((Guid.NewGuid(), rows.Count == 0 ? "Student One" : "Student Two"));

        var today = DateTime.UtcNow.Date;
        var csv = new StringBuilder();
        csv.AppendLine("StudentId,StudentName,Date,Present");
        csv.AppendLine($"{rows[0].StudentId},{EscapeCsvField(rows[0].StudentName)},{today:yyyy-MM-dd},true");
        csv.AppendLine($"{rows[1].StudentId},{EscapeCsvField(rows[1].StudentName)},{today:yyyy-MM-dd},false");

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"enter-attendance-template-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportAttendanceCsv(
        Guid? offeringId,
        Guid? tenantId,
        Guid? campusId,
        string? entryPoint,
        IFormFile? csvFile,
        CancellationToken ct)
    {
        if (!_api.IsConnected())
        {
            TempData["PortalMessage"] = "Connect to the API before importing attendance CSV.";
            return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId });
        }

        var identity = _api.GetSessionIdentity();
        if (identity is null || !(identity.IsFaculty || identity.IsAdmin || identity.IsSuperAdmin))
            return Forbid();

        if (!offeringId.HasValue)
        {
            TempData["PortalMessage"] = "Select a course offering before importing attendance CSV.";
            return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId });
        }

        if (csvFile is null || csvFile.Length == 0)
        {
            TempData["PortalMessage"] = "Please choose a non-empty CSV file.";
            return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId });
        }

        if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            TempData["PortalMessage"] = "Invalid file type. Only .csv files are accepted.";
            return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId });
        }

        var effectiveTenantId = identity.IsSuperAdmin ? tenantId : identity.TenantId;
        var effectiveCampusId = identity.IsSuperAdmin ? campusId : identity.CampusId;

        List<EnrollmentRosterItem> roster;
        try
        {
            roster = await _api.GetEnrollmentRosterAsync(offeringId.Value, effectiveTenantId, effectiveCampusId, ct);
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = $"Unable to load roster for selected offering: {ex.Message}";
            return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId });
        }

        var rosterIds = roster.Select(r => r.Id).ToHashSet();
        if (rosterIds.Count == 0)
        {
            TempData["PortalMessage"] = "No students found in the selected offering roster.";
            return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId });
        }

        var validationErrors = new List<string>();
        var parsedRows = new List<(Guid StudentId, DateTime Date, string Status)>();
        var seenStudentDate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var stream = csvFile.OpenReadStream();
            using var reader = new StreamReader(stream);

            var headerLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                TempData["PortalMessage"] = "CSV is empty or missing the header row.";
                return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId });
            }

            var header = ParseCsvLine(headerLine);
            var expectedHeader = new[] { "StudentId", "StudentName", "Date", "Present" };
            if (header.Length != expectedHeader.Length || !header.Select(h => h.Trim()).SequenceEqual(expectedHeader, StringComparer.OrdinalIgnoreCase))
            {
                TempData["PortalMessage"] = "CSV header does not match the template. Expected: StudentId,StudentName,Date,Present.";
                return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId });
            }

            var rowNumber = 1;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                rowNumber++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var cols = ParseCsvLine(line);
                if (cols.Length != 4)
                {
                    validationErrors.Add($"Row {rowNumber}: expected 4 columns.");
                    continue;
                }

                var studentIdRaw = cols[0].Trim();
                var studentNameRaw = cols[1].Trim();
                var dateRaw = cols[2].Trim();
                var presentRaw = cols[3].Trim();

                if (string.IsNullOrWhiteSpace(studentIdRaw) || string.IsNullOrWhiteSpace(studentNameRaw) || string.IsNullOrWhiteSpace(dateRaw) || string.IsNullOrWhiteSpace(presentRaw))
                {
                    validationErrors.Add($"Row {rowNumber}: required fields must not be empty.");
                    continue;
                }

                if (!Guid.TryParse(studentIdRaw, out var studentId))
                {
                    validationErrors.Add($"Row {rowNumber}: StudentId is invalid.");
                    continue;
                }

                if (!rosterIds.Contains(studentId))
                {
                    validationErrors.Add($"Row {rowNumber}: StudentId does not belong to the selected offering roster.");
                    continue;
                }

                if (!TryParseAttendanceDate(dateRaw, out var date))
                {
                    validationErrors.Add($"Row {rowNumber}: Date must be a valid value (for example yyyy-MM-dd).");
                    continue;
                }

                if (!TryParsePresentFlag(presentRaw, out var isPresent))
                {
                    validationErrors.Add($"Row {rowNumber}: Present must be true/false, yes/no, 1/0, or present/absent.");
                    continue;
                }

                var duplicateKey = $"{studentId:D}|{date:yyyy-MM-dd}";
                if (!seenStudentDate.Add(duplicateKey))
                {
                    validationErrors.Add($"Row {rowNumber}: duplicate StudentId + Date entry in import file.");
                    continue;
                }

                parsedRows.Add((studentId, date, isPresent ? "Present" : "Absent"));
            }
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = $"Unable to read CSV file: {ex.Message}";
            return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId });
        }

        if (validationErrors.Count > 0)
        {
            TempData["PortalMessage"] = "CSV validation failed: " + string.Join(" ", validationErrors.Take(5)) + (validationErrors.Count > 5 ? " ..." : string.Empty);
            return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId });
        }

        if (parsedRows.Count == 0)
        {
            TempData["PortalMessage"] = "CSV file has no valid attendance rows to import.";
            return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId });
        }

        try
        {
            foreach (var byDate in parsedRows.GroupBy(r => r.Date.Date))
            {
                var entries = byDate.Select(r => (r.StudentId, r.Status));
                await _api.BulkMarkAttendanceAsync(offeringId.Value, byDate.Key, entries, effectiveTenantId, effectiveCampusId, ct);
            }

            TempData["PortalMessage"] = $"Attendance CSV import processed successfully. Rows processed: {parsedRows.Count}.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = $"Attendance CSV import failed: {ex.Message}";
        }

        return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkMarkAttendance(
        Guid offeringId, DateTime date,
        [FromForm] Guid[] studentIds, [FromForm] string[] statuses,
        Guid? tenantId, Guid? campusId, string? entryPoint,
        CancellationToken ct)
    {
        if (_api.IsConnected() && studentIds.Length > 0)
        {
            try
            {
                var sessionId = _api.GetSessionIdentity();
                var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
                var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;
                var entries = studentIds
                    .Zip(statuses, (sid, s) => (StudentProfileId: sid, Status: s));
                await _api.BulkMarkAttendanceAsync(offeringId, date, entries, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "Attendance marked.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CorrectAttendance(
        Guid studentProfileId, Guid offeringId, DateTime date,
        string newStatus, string? remarks, Guid? tenantId, Guid? campusId, string? entryPoint, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var sessionId = _api.GetSessionIdentity();
                var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
                var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;
                await _api.CorrectAttendanceAsync(studentProfileId, offeringId, date, newStatus, remarks, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "Attendance corrected.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId });
    }

    private static string ResolveAttendanceEntryAction(string? entryPoint)
        => string.Equals(entryPoint, nameof(EnterAttendance), StringComparison.OrdinalIgnoreCase)
            ? nameof(EnterAttendance)
            : nameof(Attendance);

    private static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        values.Add(current.ToString());
        return values.ToArray();
    }

    private static bool TryParseAttendanceDate(string input, out DateTime date)
    {
        var formats = new[] { "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy" };
        return DateTime.TryParseExact(input, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
               || DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static bool TryParsePresentFlag(string input, out bool isPresent)
    {
        switch (input.Trim().ToLowerInvariant())
        {
            case "true":
            case "1":
            case "yes":
            case "y":
            case "present":
            case "p":
                isPresent = true;
                return true;
            case "false":
            case "0":
            case "no":
            case "n":
            case "absent":
            case "a":
                isPresent = false;
                return true;
            default:
                isPresent = false;
                return false;
        }
    }

    private static string EscapeCsvField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return '"' + value.Replace("\"", "\"\"") + '"';

        return value;
    }

    // ── Result write actions ────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateResult(
        Guid studentProfileId, Guid offeringId,
        string resultType, decimal marksObtained, decimal maxMarks,
        bool promote, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var sessionId = _api.GetSessionIdentity();
                var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
                var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;

                await _api.CreateResultAsync(studentProfileId, offeringId, resultType, marksObtained, maxMarks, effectiveTenantId, effectiveCampusId, ct);

                // Promotion is only offered in the UI for Final result type.
                // When the checkbox is checked the form sends promote=true;
                // unchecked checkboxes send nothing, so promote defaults to false.
                if (promote)
                    await _api.PromoteStudentAsync(studentProfileId, effectiveTenantId, effectiveCampusId, ct);
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Results), new { offeringId, tenantId, campusId });
    }

    // Standalone per-row Promote button in the Results table.
    // Reuses the existing POST api/v1/student-lifecycle/{id}/promote endpoint
    // without requiring a new result entry.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteStudentFromResult(Guid studentProfileId, Guid? offeringId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var sessionId = _api.GetSessionIdentity();
                var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
                var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;
                await _api.PromoteStudentAsync(studentProfileId, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "Student promoted to next semester.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Results), new { offeringId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CorrectResult(
        Guid studentProfileId, Guid offeringId, string resultType,
        decimal newMarksObtained, decimal newMaxMarks, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var sessionId = _api.GetSessionIdentity();
                var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
                var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;

                await _api.CorrectResultAsync(studentProfileId, offeringId, resultType, newMarksObtained, newMaxMarks, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "Result corrected.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Results), new { offeringId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishAllResults(Guid offeringId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var sessionId = _api.GetSessionIdentity();
                var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
                var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;
                await _api.PublishAllResultsAsync(offeringId, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "All results published.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Results), new { offeringId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestResultModification(
        Guid resultId,
        Guid studentProfileId,
        Guid offeringId,
        string resultType,
        decimal newMarksObtained,
        decimal newMaxMarks,
        string reason,
        Guid? tenantId,
        Guid? campusId,
        CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var sessionId = _api.GetSessionIdentity();
                var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
                var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;

                await _api.CreateResultModificationRequestAsync(
                    resultId,
                    studentProfileId,
                    offeringId,
                    resultType,
                    newMarksObtained,
                    newMaxMarks,
                    reason,
                    effectiveTenantId,
                    effectiveCampusId,
                    ct);

                TempData["PortalMessage"] = "Result modification request submitted for Admin/SuperAdmin approval.";
            }
            catch (Exception ex)
            {
                TempData["PortalMessage"] = $"Error: {ex.Message}";
            }
        }

        return RedirectToAction(nameof(Results), new { offeringId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveResultModificationRequest(Guid requestId, Guid? offeringId, Guid? tenantId, Guid? campusId, string? notes, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var req = await _api.GetResultModificationRequestByIdAsync(requestId, ct);
                if (req is null)
                    throw new InvalidOperationException("Modification request not found.");

                using var doc = System.Text.Json.JsonDocument.Parse(req.ProposedData);
                var root = doc.RootElement;

                var studentProfileId = root.GetProperty("studentProfileId").GetGuid();
                var courseOfferingId = root.GetProperty("courseOfferingId").GetGuid();
                var resultType = root.GetProperty("resultType").GetString() ?? "Final";
                var newMarksObtained = root.GetProperty("newMarksObtained").GetDecimal();
                var newMaxMarks = root.GetProperty("newMaxMarks").GetDecimal();

                Guid? requestedTenantId = null;
                Guid? requestedCampusId = null;

                if (root.TryGetProperty("tenantId", out var tenantNode) && tenantNode.ValueKind == System.Text.Json.JsonValueKind.String)
                    requestedTenantId = Guid.TryParse(tenantNode.GetString(), out var parsedTenant) ? parsedTenant : null;
                if (root.TryGetProperty("campusId", out var campusNode) && campusNode.ValueKind == System.Text.Json.JsonValueKind.String)
                    requestedCampusId = Guid.TryParse(campusNode.GetString(), out var parsedCampus) ? parsedCampus : null;

                await _api.CorrectResultAsync(studentProfileId, courseOfferingId, resultType, newMarksObtained, newMaxMarks, requestedTenantId, requestedCampusId, ct);
                await _api.ApproveResultModificationRequestAsync(requestId, notes, ct);
                TempData["PortalMessage"] = "Result modification request approved and applied.";
            }
            catch (Exception ex)
            {
                TempData["PortalMessage"] = $"Error: {ex.Message}";
            }
        }

        return RedirectToAction(nameof(Results), new { offeringId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectResultModificationRequest(Guid requestId, Guid? offeringId, Guid? tenantId, Guid? campusId, string? notes, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                await _api.RejectResultModificationRequestAsync(requestId, notes, ct);
                TempData["PortalMessage"] = "Result modification request rejected.";
            }
            catch (Exception ex)
            {
                TempData["PortalMessage"] = $"Error: {ex.Message}";
            }
        }

        return RedirectToAction(nameof(Results), new { offeringId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestResultRecheck(
        Guid resultId,
        Guid offeringId,
        string resultType,
        string courseName,
        string reason,
        Guid? tenantId,
        Guid? campusId,
        CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var newData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    resultId,
                    offeringId,
                    resultType,
                    courseName,
                    tenantId,
                    campusId,
                    action = "RecheckRequest"
                });

                var description = $"Result re-check request for {courseName} ({resultType})";
                await _api.CreateResultRecheckRequestAsync(description, newData, reason, ct);
                TempData["PortalMessage"] = "Re-check request submitted. Admin/SuperAdmin will review it.";
            }
            catch (Exception ex)
            {
                TempData["PortalMessage"] = $"Error: {ex.Message}";
            }
        }

        return RedirectToAction(nameof(Results), new { offeringId, tenantId, campusId });
    }

    // ── Quiz write actions ──────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQuiz(
        Guid offeringId, string title, string? instructions,
        int? timeLimitMinutes, int maxAttempts, Guid? tenantId, Guid? campusId, bool includeInactive, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var sessionId = _api.GetSessionIdentity();
                var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
                var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;
                await _api.CreateQuizAsync(offeringId, title, instructions, timeLimitMinutes, maxAttempts, effectiveTenantId, effectiveCampusId, ct);
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Quizzes), new { offeringId, tenantId, campusId, includeInactive });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuiz(
        Guid id, Guid offeringId, string title, string? instructions,
        int? timeLimitMinutes, int maxAttempts, Guid? tenantId, Guid? campusId, bool includeInactive, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var sessionId = _api.GetSessionIdentity();
                var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
                var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;
                await _api.UpdateQuizAsync(id, title, instructions, timeLimitMinutes, maxAttempts, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "Quiz updated.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Quizzes), new { offeringId, tenantId, campusId, includeInactive });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishQuiz(Guid id, Guid? offeringId, Guid? tenantId, Guid? campusId, bool includeInactive, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var sessionId = _api.GetSessionIdentity();
                var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
                var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;
                await _api.PublishQuizAsync(id, effectiveTenantId, effectiveCampusId, ct);
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Quizzes), new { offeringId, tenantId, campusId, includeInactive });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetQuizActive(Guid id, bool activate, Guid? offeringId, Guid? tenantId, Guid? campusId, bool includeInactive = true, CancellationToken ct = default)
    {
        if (_api.IsConnected())
        {
            try
            {
                var sessionId = _api.GetSessionIdentity();
                var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
                var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;
                await _api.SetQuizActiveAsync(id, activate, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = activate ? "Quiz activated." : "Quiz deactivated.";
            }
            catch (Exception ex)
            {
                TempData["PortalMessage"] = $"Error: {ex.Message}";
            }
        }
        return RedirectToAction(nameof(Quizzes), new { offeringId, tenantId, campusId, includeInactive });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuiz(Guid id, Guid? offeringId, Guid? tenantId, Guid? campusId, bool includeInactive, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var sessionId = _api.GetSessionIdentity();
                var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
                var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;
                await _api.DeleteQuizAsync(id, effectiveTenantId, effectiveCampusId, ct);
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Quizzes), new { offeringId, tenantId, campusId, includeInactive });
    }

    // ── FYP write actions ───────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ProposeFypProject(
        Guid departmentId, string title, string description, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var identity = _api.GetSessionIdentity();
                var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
                var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
                await _api.ProposeFypProjectAsync(departmentId, title, description, effectiveTenantId, effectiveCampusId, ct);
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Fyp), new { departmentId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFypProject(
        Guid studentProfileId, Guid departmentId, string title, string description, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var identity = _api.GetSessionIdentity();
                var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
                var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
                await _api.CreateFypProjectAsync(studentProfileId, departmentId, title, description, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "FYP project created.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Fyp), new { departmentId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFypProject(Guid id, Guid? departmentId, string title, string description, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var identity = _api.GetSessionIdentity();
                var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
                var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
                await _api.UpdateFypProjectAsync(id, title, description, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "FYP project updated.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Fyp), new { departmentId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveFypProject(Guid id, string? remarks, Guid? departmentId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var identity = _api.GetSessionIdentity();
                var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
                var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
                await _api.ApproveFypProjectAsync(id, remarks, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "Project approved.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Fyp), new { departmentId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectFypProject(Guid id, string remarks, Guid? departmentId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var identity = _api.GetSessionIdentity();
                var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
                var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
                await _api.RejectFypProjectAsync(id, remarks, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "Project rejected.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Fyp), new { departmentId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignFypSupervisor(Guid id, Guid supervisorUserId, Guid? departmentId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var identity = _api.GetSessionIdentity();
                var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
                var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
                await _api.AssignFypSupervisorAsync(id, supervisorUserId, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "Supervisor assigned.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Fyp), new { departmentId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteFypProject(Guid id, Guid? departmentId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var identity = _api.GetSessionIdentity();
                var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
                var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
                await _api.CompleteFypProjectAsync(id, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "Project marked as complete.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Fyp), new { departmentId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EnterFypResult(Guid id, Guid? departmentId, string result, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var identity = _api.GetSessionIdentity();
                var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
                var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
                await _api.EnterFypResultAsync(id, result, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "FYP result saved.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Fyp), new { departmentId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestFypCompletion(Guid id, Guid? departmentId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var identity = _api.GetSessionIdentity();
                var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
                var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
                await _api.RequestFypCompletionAsync(id, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "Completion request sent to assigned faculty for approvals.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }

        return RedirectToAction(nameof(Fyp), new { departmentId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveFypCompletion(Guid id, Guid? departmentId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var identity = _api.GetSessionIdentity();
                var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
                var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
                await _api.ApproveFypCompletionAsync(id, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "Completion approval submitted.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }

        return RedirectToAction(nameof(Fyp), new { departmentId, tenantId, campusId });
    }

    [HttpGet]
    public async Task<IActionResult> Analytics(int? institutionType, Guid? tenantId, Guid? campusId, Guid? departmentId, Guid? courseId, Guid? semesterId, CancellationToken ct)
    {
        ViewData["Title"] = "Analytics";
        var model = await BuildAnalyticsPageModelAsync(institutionType, tenantId, campusId, departmentId, courseId, semesterId, ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AnalyticsSnapshot(int? institutionType, Guid? tenantId, Guid? campusId, Guid? departmentId, Guid? courseId, Guid? semesterId, CancellationToken ct)
    {
        var model = await BuildAnalyticsPageModelAsync(institutionType, tenantId, campusId, departmentId, courseId, semesterId, ct);
        return Json(new
        {
            success = model.IsConnected,
            isFinanceOnly = model.IsFinanceOnly,
            isAnalyticsActive = model.IsAnalyticsActive,
            message = model.Message,
            selectedInstitutionType = model.SelectedInstitutionType,
            selectedTenantId = model.SelectedTenantId,
            selectedCampusId = model.SelectedCampusId,
            selectedDepartmentId = model.SelectedDepartmentId,
            selectedCourseId = model.SelectedCourseId,
            selectedSemesterId = model.SelectedSemesterId,
            tenants = model.Tenants.Select(t => new { id = t.Id, name = t.Name, code = t.Code }).ToList(),
            campuses = model.Campuses.Select(c => new { id = c.Id, name = c.Name, code = c.Code }).ToList(),
            departments = model.Departments.Select(d => new { id = d.Id, name = d.Name, institutionType = d.InstitutionType }).ToList(),
            courses = model.Courses.Select(c => new { id = c.Id, name = c.Name }).ToList(),
            semesters = model.Semesters.Select(s => new { id = s.Id, name = s.Name }).ToList(),
            cards = model.Cards.Select(c => new
            {
                label = c.Label,
                value = c.Value,
                subText = c.SubText,
                colorClass = c.ColorClass,
                icon = c.Icon
            }).ToList(),
            performance = model.Performance,
            attendance = model.Attendance,
            assignments = model.Assignments,
            paymentStatus = model.PaymentStatus
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAnalyticsActive(bool isActive, int? institutionType, Guid? tenantId, Guid? campusId, Guid? departmentId, Guid? courseId, Guid? semesterId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                if (isActive)
                    await _api.ActivateAnalyticsScopeAsync(tenantId, campusId, ct);
                else
                    await _api.DeactivateAnalyticsScopeAsync(tenantId, campusId, ct);
                TempData["PortalMessage"] = isActive ? "Analytics activated for selected scope." : "Analytics deactivated for selected scope.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }

        return RedirectToAction(nameof(Analytics), new { institutionType, tenantId, campusId, departmentId, courseId, semesterId });
    }

    private async Task<AnalyticsPageModel> BuildAnalyticsPageModelAsync(int? institutionType, Guid? tenantId, Guid? campusId, Guid? departmentId, Guid? courseId, Guid? semesterId, CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        var model = new AnalyticsPageModel
        {
            IsConnected = _api.IsConnected(),
            IsFinanceOnly = identity is { IsFinance: true, IsAdmin: false, IsSuperAdmin: false },
            Identity = identity,
            SelectedInstitutionType = institutionType,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId,
            SelectedDepartmentId = departmentId,
            SelectedCourseId = courseId,
            SelectedSemesterId = semesterId
        };

        if (!model.IsConnected)
        {
            return model;
        }

        try
        {
            if (identity?.IsSuperAdmin == true)
            {
                model.Tenants = await _api.GetTenantsAsync(ct);
                if (model.SelectedTenantId.HasValue)
                    model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);
            }

            model.IsAnalyticsActive = await _api.GetAnalyticsScopeActiveAsync(model.SelectedTenantId, model.SelectedCampusId, ct);
            if (!model.IsAnalyticsActive)
            {
                model.Message = "Analytics is currently deactivated for the selected tenant/campus scope.";
                return model;
            }

            model.Departments = await _api.GetDepartmentsAsync(ct);
            var allSemesters = await _api.GetSemestersAsync(ct);
            var offerings = await _api.GetCourseOfferingsAsync(model.SelectedDepartmentId, model.SelectedTenantId, model.SelectedCampusId, null, ct);

            // Constrained roles are auto-scoped to their institute claim for analytics filters.
            if (identity is { IsSuperAdmin: false, InstitutionType: not null })
            {
                model.SelectedInstitutionType = identity.InstitutionType.Value;
            }

            if (model.SelectedInstitutionType.HasValue)
            {
                model.Departments = model.Departments
                    .Where(d => !d.InstitutionType.HasValue || d.InstitutionType.Value == model.SelectedInstitutionType.Value)
                    .ToList();

                var allowedDepartmentIds = model.Departments.Select(d => d.Id).ToHashSet();
                offerings = offerings.Where(o => allowedDepartmentIds.Contains(o.DepartmentId)).ToList();
            }

            if (model.SelectedDepartmentId.HasValue && model.Departments.All(d => d.Id != model.SelectedDepartmentId.Value))
            {
                model.Message = "Selected department is outside your current analytics scope.";
                model.SelectedDepartmentId = null;
                model.SelectedCourseId = null;
                model.SelectedSemesterId = null;
                offerings = await _api.GetCourseOfferingsAsync(null, model.SelectedTenantId, model.SelectedCampusId, null, ct);
                if (model.SelectedInstitutionType.HasValue)
                {
                    var allowedDepartmentIds = model.Departments.Select(d => d.Id).ToHashSet();
                    offerings = offerings.Where(o => allowedDepartmentIds.Contains(o.DepartmentId)).ToList();
                }
            }

            if (model.SelectedDepartmentId.HasValue)
            {
                offerings = offerings.Where(o => o.DepartmentId == model.SelectedDepartmentId.Value).ToList();
            }

            model.Courses = offerings
                .GroupBy(o => new { o.CourseId, o.CourseTitle })
                .Select(g => new LookupItem { Id = g.Key.CourseId, Name = g.Key.CourseTitle })
                .OrderBy(c => c.Name)
                .ToList();

            if (model.SelectedCourseId.HasValue)
            {
                if (model.Courses.All(c => c.Id != model.SelectedCourseId.Value))
                {
                    model.Message = "Selected course is outside your current analytics scope.";
                    model.SelectedCourseId = null;
                    model.SelectedSemesterId = null;
                }
                else
                {
                    offerings = offerings.Where(o => o.CourseId == model.SelectedCourseId.Value).ToList();
                }
            }

            model.Semesters = offerings
                .GroupBy(o => new { o.SemesterId, o.SemesterName })
                .Select(g => new LookupItem { Id = g.Key.SemesterId, Name = g.Key.SemesterName })
                .OrderBy(s => s.Name)
                .ToList();

            // When there are no offerings yet for current scope, fall back to catalog semesters.
            if (model.Semesters.Count == 0)
            {
                model.Semesters = allSemesters;
            }

            if (model.SelectedSemesterId.HasValue && model.Semesters.All(s => s.Id != model.SelectedSemesterId.Value))
            {
                model.Message = "Selected semester is outside your current analytics scope.";
                model.SelectedSemesterId = null;
            }

            // Final-Touches Phase 6 Stage 6.2 — fetch typed DTOs instead of raw JSON.
            // Finance-only users can access payment analytics without academic report permissions.
            if (!model.IsFinanceOnly)
            {
                model.Performance = await _api.GetPerformanceAnalyticsAsync(model.SelectedDepartmentId, model.SelectedInstitutionType, model.SelectedCourseId, model.SelectedSemesterId, model.SelectedTenantId, model.SelectedCampusId, ct);
                model.Attendance = await _api.GetAttendanceAnalyticsAsync(model.SelectedDepartmentId, model.SelectedInstitutionType, model.SelectedCourseId, model.SelectedSemesterId, model.SelectedTenantId, model.SelectedCampusId, ct);
                model.Assignments = await _api.GetAssignmentAnalyticsAsync(model.SelectedDepartmentId, model.SelectedInstitutionType, model.SelectedCourseId, model.SelectedSemesterId, model.SelectedTenantId, model.SelectedCampusId, ct);
            }

            model.PaymentStatus = await _api.GetPaymentStatusAnalyticsAsync(model.SelectedDepartmentId, model.SelectedInstitutionType, model.SelectedCourseId, model.SelectedSemesterId, model.SelectedTenantId, model.SelectedCampusId, ct);

            // Populate summary cards from real data.
            if (model.Performance is not null)
            {
                model.Cards.Add(new AnalyticsSummaryCard
                {
                    Label = "Avg. Marks",
                    Value = $"{model.Performance.AverageMarks:F1}%",
                    SubText = $"{model.Performance.TotalStudents} students · {model.Performance.DepartmentName}",
                    ColorClass = "text-primary",
                    Icon = "📊"
                });
            }
            if (model.Attendance is not null)
            {
                model.Cards.Add(new AnalyticsSummaryCard
                {
                    Label = "Avg. Attendance",
                    Value = $"{model.Attendance.OverallAttendancePercentage:F1}%",
                    SubText = model.Attendance.DepartmentName,
                    ColorClass = "text-success",
                    Icon = "📋"
                });
            }
            if (model.Assignments is not null)
            {
                model.Cards.Add(new AnalyticsSummaryCard
                {
                    Label = "Assignments",
                    Value = model.Assignments.Assignments.Count.ToString(),
                    SubText = model.Assignments.DepartmentName,
                    ColorClass = "text-warning",
                    Icon = "📝"
                });
            }
            if (model.PaymentStatus is not null)
            {
                var total = model.PaymentStatus.PaidCount + model.PaymentStatus.UnpaidCount;
                model.Cards.Add(new AnalyticsSummaryCard
                {
                    Label = "Payment Status",
                    Value = $"{model.PaymentStatus.PaidCount}/{total}",
                    SubText = $"Paid vs total · {model.PaymentStatus.DepartmentName}",
                    ColorClass = "text-info",
                    Icon = "💳"
                });
            }
        }
        catch (Exception ex)
        {
            model.Message = ex.Message;
        }

        return model;
    }

    // ── AI Chat ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> AiChat(Guid? conversationId, CancellationToken ct)
    {
        ViewData["Title"] = "AI Assistant";
        var model = new AiChatPageModel
        {
            IsConnected          = _api.IsConnected(),
            ActiveConversationId = conversationId
        };
        if (!model.IsConnected) return View(model);
        try
        {
            model.Conversations = await _api.GetChatConversationsAsync(ct);
            if (conversationId.HasValue)
                model.CurrentMessages = await _api.GetChatMessagesAsync(conversationId.Value, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AiChatSend(Guid? conversationId, string message, CancellationToken ct)
    {
        if (!_api.IsConnected() || string.IsNullOrWhiteSpace(message))
            return RedirectToAction(nameof(AiChat), new { conversationId });
        try
        {
            var reply = await _api.SendChatMessageAsync(conversationId, message, ct);
            conversationId = reply?.ConversationId ?? conversationId;
        }
        catch { /* errors handled gracefully — just reload */ }
        return RedirectToAction(nameof(AiChat), new { conversationId });
    }

    [HttpGet]
    public async Task<IActionResult> AiChatWidgetState(Guid? conversationId, CancellationToken ct)
    {
        if (!_api.IsConnected())
        {
            return Json(new
            {
                isConnected = false,
                activeConversationId = (Guid?)null,
                messages = Array.Empty<AiChatMessageItem>(),
                message = "Not connected."
            });
        }

        try
        {
            var conversations = await _api.GetChatConversationsAsync(ct);
            var messages = conversationId.HasValue
                ? await _api.GetChatMessagesAsync(conversationId.Value, ct)
                : new List<AiChatMessageItem>();

            return Json(new
            {
                isConnected = true,
                activeConversationId = conversationId,
                messages,
                conversations = conversations
                    .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                    .Select(c => new
                    {
                        c.Id,
                        c.Title,
                        c.CreatedAt,
                        c.LastMessageAt
                    })
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                isConnected = true,
                activeConversationId = (Guid?)null,
                messages = Array.Empty<AiChatMessageItem>(),
                conversations = Array.Empty<object>(),
                message = ex.Message
            });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AiChatWidgetSend(Guid? conversationId, string message, CancellationToken ct)
    {
        if (!_api.IsConnected())
            return Json(new { success = false, error = "Not connected." });

        if (string.IsNullOrWhiteSpace(message))
            return Json(new { success = false, error = "Message is required." });

        try
        {
            var reply = await _api.SendChatMessageAsync(conversationId, message.Trim(), ct);
            if (reply is null)
                return Json(new { success = false, error = "AI assistant is currently unavailable." });

            return Json(new
            {
                success = true,
                conversationId = reply.ConversationId,
                assistantMessage = reply.AssistantMessage
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // ── Student Lifecycle ──────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> StudentLifecycle(Guid? departmentId, Guid? tenantId, Guid? campusId, int semester = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Student Lifecycle";
        var identity = _api.GetSessionIdentity();
        var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
        var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
        var model = new StudentLifecyclePageModel
        {
            IsConnected         = _api.IsConnected(),
            Identity            = identity,
            SelectedTenantId    = tenantId,
            SelectedCampusId    = campusId,
            SelectedDepartmentId = departmentId,
            SelectedSemester    = semester,
            PeriodLabel         = "Semester",
            Message             = TempData["PortalMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var vocabulary = await _api.GetVocabularyAsync(ct);
            model.PeriodLabel = vocabulary?.PeriodLabel ?? "Semester";

            if (identity?.IsSuperAdmin == true)
            {
                model.Tenants = await _api.GetTenantsAsync(ct);
                if (model.SelectedTenantId.HasValue)
                    model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);
            }

            model.Departments = await _api.GetDepartmentsAsync(effectiveTenantId, effectiveCampusId, ct);

            if (identity is { IsSuperAdmin: false, InstitutionType: not null })
            {
                model.Departments = model.Departments
                    .Where(d => d.InstitutionType == identity.InstitutionType.Value)
                    .ToList();

                if (model.SelectedDepartmentId.HasValue && model.Departments.All(d => d.Id != model.SelectedDepartmentId.Value))
                {
                    model.Message = "Selected department is outside your current institute scope.";
                    model.SelectedDepartmentId = null;
                }
            }

            if (departmentId.HasValue)
            {
                if (model.Departments.Any(d => d.Id == departmentId.Value))
                {
                    model.GraduationCandidates = await _api.GetGraduationCandidatesAsync(departmentId.Value, effectiveTenantId, effectiveCampusId, ct);
                    model.StudentsBySemester   = await _api.GetStudentsByAcademicLevelAsync(departmentId.Value, semester, effectiveTenantId, effectiveCampusId, ct);
                }
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GraduateStudent(Guid studentId, Guid? departmentId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var identity = _api.GetSessionIdentity();
                var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
                var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
                await _api.GraduateStudentAsync(studentId, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "Student graduated.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(StudentLifecycle), new { departmentId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteStudent(Guid studentId, Guid? departmentId, Guid? tenantId, Guid? campusId, int semester, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var identity = _api.GetSessionIdentity();
                var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
                var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
                await _api.PromoteStudentAsync(studentId, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "Student promoted to the next academic level.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(StudentLifecycle), new { departmentId, tenantId, campusId, semester });
    }

    // ── Payments ───────────────────────────────────────────────────────────
    // Final-Touches Phase 7 — admin all-receipts view + student own receipts

    [HttpGet]
    public async Task<IActionResult> Payments(Guid? studentId, Guid? tenantId, Guid? campusId, int page = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Payments";
        var identity = _api.GetSessionIdentity();
        var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
        var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
        var model = new PaymentsPageModel
        {
            IsConnected       = _api.IsConnected(),
            Identity          = identity,
            SelectedStudentId = studentId,
            SelectedTenantId  = effectiveTenantId,
            SelectedCampusId  = effectiveCampusId,
            Page = page < 1 ? 1 : page,
            Message           = TempData["PortalMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            // Student role: load their own receipts via JWT
            if (identity?.IsStudent == true)
            {
                var pageResult = await _api.GetMyPaymentsAsync(model.Page, model.PageSize, ct);
                model.Payments = pageResult.Items;
                model.TotalCount = pageResult.TotalCount;
                model.Page = pageResult.Page;
                model.PageSize = pageResult.PageSize;
            }
            else
            {
                if (identity?.IsSuperAdmin == true)
                {
                    model.Tenants = await _api.GetTenantsAsync(ct);
                    if (model.SelectedTenantId.HasValue)
                        model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);
                }

                model.Students = await _api.GetStudentsAsync(null, ct);
                // Admin / Finance: load paged receipts and optionally filter by student
                PaymentReceiptPageItem pageResult;
                if (studentId.HasValue)
                    pageResult = await _api.GetPaymentsByStudentAsync(studentId.Value, model.Page, model.PageSize, model.SelectedTenantId, model.SelectedCampusId, ct);
                else
                    pageResult = await _api.GetAllPaymentsAsync(model.Page, model.PageSize, model.SelectedTenantId, model.SelectedCampusId, ct);

                model.Payments = pageResult.Items;
                model.TotalCount = pageResult.TotalCount;
                model.Page = pageResult.Page;
                model.PageSize = pageResult.PageSize;
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    // Final-Touches Phase 7 Stage 7.2 — create receipt (Admin/Finance)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePayment(CreatePaymentForm form, Guid? studentId, Guid? tenantId, Guid? campusId, int page = 1, CancellationToken ct = default)
    {
        try
        {
            var identity = _api.GetSessionIdentity();
            var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
            var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
            await _api.CreatePaymentAsync(form.StudentProfileId, form.Amount, form.Description, form.DueDate, effectiveTenantId, effectiveCampusId, ct);
            TempData["PortalMessage"] = "Receipt created successfully.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, page });
    }

    // Final-Touches Phase 7 Stage 7.2 — edit receipt (Admin/Finance)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePayment(Guid receiptId, decimal amount, string description, DateTime dueDate, string? notes, Guid? studentId, Guid? tenantId, Guid? campusId, int page = 1, CancellationToken ct = default)
    {
        try
        {
            var identity = _api.GetSessionIdentity();
            var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
            var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
            await _api.UpdatePaymentAsync(receiptId, amount, description, dueDate, notes, effectiveTenantId, effectiveCampusId, ct);
            TempData["PortalMessage"] = "Receipt updated successfully.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, page });
    }

    // Final-Touches Phase 7 Stage 7.2 — confirm payment (Admin/Finance)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPayment(Guid receiptId, Guid? studentId, Guid? tenantId, Guid? campusId, int page = 1, CancellationToken ct = default)
    {
        try
        {
            var identity = _api.GetSessionIdentity();
            var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
            var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
            await _api.ConfirmPaymentAsync(receiptId, effectiveTenantId, effectiveCampusId, ct);
            TempData["PortalMessage"] = "Payment confirmed.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, page });
    }

    // Final-Touches Phase 7 Stage 7.2 — cancel receipt (Admin/Finance)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelPayment(Guid receiptId, Guid? studentId, Guid? tenantId, Guid? campusId, int page = 1, CancellationToken ct = default)
    {
        try
        {
            var identity = _api.GetSessionIdentity();
            var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
            var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
            await _api.CancelPaymentAsync(receiptId, effectiveTenantId, effectiveCampusId, ct);
            TempData["PortalMessage"] = "Receipt cancelled.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, page });
    }

    // Final-Touches Phase 7 Stage 7.3 — student marks receipt as submitted
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitProof(Guid receiptId, string proofNote, Guid? studentId, Guid? tenantId, Guid? campusId, int page = 1, CancellationToken ct = default)
    {
        try
        {
            var identity = _api.GetSessionIdentity();
            var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
            var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
            await _api.SubmitProofAsync(receiptId, proofNote, effectiveTenantId, effectiveCampusId, ct);
            TempData["PortalMessage"] = "Proof of payment submitted.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, page });
    }

    // ── Enrollments ────────────────────────────────────────────────────────

    // Final-Touches Phase 8 Stage 8.1+8.2 — student sees own courses; admin sees offering roster + students list
    // Issue-Fix Phase 3 Stage 3.3 — Faculty: load offerings via GetMyOfferings (dept-scoped) + show roster when offering selected.
    [HttpGet]
    public async Task<IActionResult> Enrollments(Guid? tenantId, Guid? campusId, Guid? offeringId, CancellationToken ct)
    {
        ViewData["Title"] = "Enrollments";
        var isStudent = User.IsInRole("Student");
        var identity = _api.GetSessionIdentity();
        var model = new EnrollmentsPageModel
        {
            IsConnected        = _api.IsConnected(),
            SelectedOfferingId = offeringId,
            SelectedTenantId   = tenantId,
            SelectedCampusId   = campusId,
            IsStudent          = isStudent,
            Identity           = identity,
            Message            = TempData["PortalMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var isAdmin = identity?.IsAdmin == true || identity?.IsSuperAdmin == true;

            if (identity?.IsSuperAdmin == true)
            {
                model.Tenants = await _api.GetTenantsAsync(ct);
                if (model.SelectedTenantId.HasValue)
                    model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);
            }

            model.IsEnrollmentActive = await _api.GetEnrollmentScopeActiveAsync(model.SelectedTenantId, model.SelectedCampusId, ct);
            if (!model.IsEnrollmentActive)
            {
                model.Message ??= "Enrollment is currently deactivated for the selected tenant/campus scope.";
            }

            // Issue-Fix Phase 3 Stage 3.3 — Use GetCourseOfferingsAsync for all roles; API filters by dept for Faculty.
            model.Offerings = await _api.GetCourseOfferingsAsync(null, model.SelectedTenantId, model.SelectedCampusId, null, ct);

            if (!model.IsEnrollmentActive)
            {
                if (isStudent)
                    model.MyCourses = await _api.GetMyEnrollmentsAsync(ct);
            }
            else if (isStudent)
            {
                model.MyCourses = await _api.GetMyEnrollmentsAsync(ct);
            }
            else
            {
                if (isAdmin)
                    model.Students = await _api.GetStudentsAsync(null, ct);

                if (offeringId.HasValue)
                    model.Roster = await _api.GetEnrollmentRosterAsync(offeringId.Value, model.SelectedTenantId, model.SelectedCampusId, ct);
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetEnrollmentActive(bool isActive, Guid? tenantId, Guid? campusId, Guid? offeringId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                if (isActive)
                    await _api.ActivateEnrollmentScopeAsync(tenantId, campusId, ct);
                else
                    await _api.DeactivateEnrollmentScopeAsync(tenantId, campusId, ct);

                TempData["PortalMessage"] = isActive
                    ? "Enrollment activated for selected scope."
                    : "Enrollment deactivated for selected scope.";
            }
            catch (Exception ex)
            {
                TempData["PortalMessage"] = ex.Message;
            }
        }

        return RedirectToAction(nameof(Enrollments), new { tenantId, campusId, offeringId });
    }

    // Final-Touches Phase 8 Stage 8.2 — admin enrolls a student
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EnrollStudent(Guid studentProfileId, Guid courseOfferingId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        try
        {
            await _api.AdminEnrollStudentAsync(studentProfileId, courseOfferingId, ct);
            TempData["PortalMessage"] = "Student enrolled successfully.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Enrollments), new { tenantId, campusId, offeringId = courseOfferingId });
    }

    // Final-Touches Phase 8 Stage 8.2 — admin drops any enrollment by ID
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminDropEnrollment(Guid enrollmentId, Guid offeringId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        try
        {
            await _api.AdminDropEnrollmentAsync(enrollmentId, ct);
            TempData["PortalMessage"] = "Enrollment dropped.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Enrollments), new { tenantId, campusId, offeringId });
    }

    // Final-Touches Phase 8 Stage 8.2 — student self-enrolls in a course offering
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StudentEnroll(Guid courseOfferingId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        try
        {
            await _api.StudentEnrollAsync(courseOfferingId, ct);
            TempData["PortalMessage"] = "Enrolled successfully.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Enrollments), new { tenantId, campusId });
    }

    // Final-Touches Phase 8 Stage 8.2 — student drops their own enrollment
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StudentDropEnrollment(Guid courseOfferingId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        try
        {
            await _api.StudentDropEnrollmentAsync(courseOfferingId, ct);
            TempData["PortalMessage"] = "Course dropped.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Enrollments), new { tenantId, campusId });
    }

    // ── Reports ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> ReportCenter(Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        ViewData["Title"] = "Report Center";
        var identity = _api.GetSessionIdentity();
        var model = new ReportCenterPageModel
        {
            IsConnected = _api.IsConnected(),
            Identity = identity,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId,
            Message = TempData["PortalMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            if (identity?.IsSuperAdmin == true)
            {
                model.Tenants = await _api.GetTenantsAsync(ct);
                if (model.SelectedTenantId.HasValue)
                    model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);
            }

            model.IsReportsActive = await _api.GetReportsScopeActiveAsync(model.SelectedTenantId, model.SelectedCampusId, ct);
            model.Reports = await _api.GetReportCatalogAsync(ct);
            if (!model.IsReportsActive)
                model.Message ??= "Reports are currently deactivated for the selected tenant/campus scope.";
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetReportsActive(bool isActive, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                if (isActive)
                    await _api.ActivateReportsScopeAsync(tenantId, campusId, ct);
                else
                    await _api.DeactivateReportsScopeAsync(tenantId, campusId, ct);

                TempData["PortalMessage"] = isActive
                    ? "Reports activated for selected scope."
                    : "Reports deactivated for selected scope.";
            }
            catch (Exception ex)
            {
                TempData["PortalMessage"] = ex.Message;
            }
        }

        return RedirectToAction(nameof(ReportCenter), new { tenantId, campusId });
    }

    [HttpGet]
    public async Task<IActionResult> ReportAttendance(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var selectedInstitutionType = ResolveReportInstitutionType(institutionType);
        ViewData["Title"] = "Attendance Summary Report";
        var model = new ReportAttendancePageModel
        {
            IsConnected  = _api.IsConnected(),
            SemesterId   = semesterId,
            DepartmentId = departmentId,
            OfferingId   = offeringId,
            StudentId    = studentId,
            InstitutionType = selectedInstitutionType
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var identity = _api.GetSessionIdentity();
            var isFacultyOnly = identity?.IsFaculty == true && !identity.IsAdmin && !identity.IsSuperAdmin;
            var isAdminOnly   = identity?.IsAdmin == true && !identity.IsSuperAdmin;
            model.Semesters    = await _api.GetSemestersAsync(ct);
            model.Departments  = await _api.GetDepartmentsAsync(ct);
            model.Offerings    = await _api.GetCourseOfferingsAsync(null, null, null, null, ct);
            model.Departments  = FilterDepartmentsByInstitution(model.Departments, selectedInstitutionType);
            if (isFacultyOnly && !offeringId.HasValue && (semesterId.HasValue || departmentId.HasValue || studentId.HasValue))
            {
                model.Message = "Faculty must select a course offering before generating report data.";
            }
            else if (isAdminOnly && !departmentId.HasValue && !offeringId.HasValue)
            {
                model.Message = "Admin must select a department or course offering before generating report data.";
            }
            else if (semesterId.HasValue || departmentId.HasValue || offeringId.HasValue || studentId.HasValue)
                model.Report = await _api.GetAttendanceSummaryReportAsync(semesterId, departmentId, offeringId, studentId, selectedInstitutionType, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ReportResults(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var selectedInstitutionType = ResolveReportInstitutionType(institutionType);
        ViewData["Title"] = "Result Summary Report";
        var model = new ReportResultsPageModel
        {
            IsConnected  = _api.IsConnected(),
            SemesterId   = semesterId,
            DepartmentId = departmentId,
            OfferingId   = offeringId,
            StudentId    = studentId,
            InstitutionType = selectedInstitutionType
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var identity = _api.GetSessionIdentity();
            var isFacultyOnly = identity?.IsFaculty == true && !identity.IsAdmin && !identity.IsSuperAdmin;
            var isAdminOnly   = identity?.IsAdmin == true && !identity.IsSuperAdmin;
            model.Semesters   = await _api.GetSemestersAsync(ct);
            model.Departments = await _api.GetDepartmentsAsync(ct);
            model.Offerings   = await _api.GetCourseOfferingsAsync(null, null, null, null, ct);
            model.Departments = FilterDepartmentsByInstitution(model.Departments, selectedInstitutionType);
            if (isFacultyOnly && !offeringId.HasValue && (semesterId.HasValue || departmentId.HasValue || studentId.HasValue))
            {
                model.Message = "Faculty must select a course offering before generating report data.";
            }
            else if (isAdminOnly && !departmentId.HasValue && !offeringId.HasValue)
            {
                model.Message = "Admin must select a department or course offering before generating report data.";
            }
            else if (semesterId.HasValue || departmentId.HasValue || offeringId.HasValue || studentId.HasValue)
                model.Report = await _api.GetResultSummaryReportAsync(semesterId, departmentId, offeringId, studentId, selectedInstitutionType, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ReportAssignments(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var selectedInstitutionType = ResolveReportInstitutionType(institutionType);
        ViewData["Title"] = "Assignment Summary Report";
        var model = new ReportAssignmentsPageModel
        {
            IsConnected  = _api.IsConnected(),
            SemesterId   = semesterId,
            DepartmentId = departmentId,
            OfferingId   = offeringId,
            StudentId    = studentId,
            InstitutionType = selectedInstitutionType
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var identity = _api.GetSessionIdentity();
            var isFacultyOnly = identity?.IsFaculty == true && !identity.IsAdmin && !identity.IsSuperAdmin;
            var isAdminOnly   = identity?.IsAdmin == true && !identity.IsSuperAdmin;
            model.Semesters   = await _api.GetSemestersAsync(ct);
            model.Departments = await _api.GetDepartmentsAsync(ct);
            model.Offerings   = await _api.GetCourseOfferingsAsync(null, null, null, null, ct);
            model.Departments = FilterDepartmentsByInstitution(model.Departments, selectedInstitutionType);
            if (isFacultyOnly && !offeringId.HasValue && (semesterId.HasValue || departmentId.HasValue || studentId.HasValue))
            {
                model.Message = "Faculty must select a course offering before generating report data.";
            }
            else if (isAdminOnly && !departmentId.HasValue && !offeringId.HasValue)
            {
                model.Message = "Admin must select a department or course offering before generating report data.";
            }
            else if (semesterId.HasValue || departmentId.HasValue || offeringId.HasValue || studentId.HasValue)
                model.Report = await _api.GetAssignmentSummaryReportAsync(semesterId, departmentId, offeringId, studentId, selectedInstitutionType, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ReportQuizzes(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        var selectedInstitutionType = ResolveReportInstitutionType(institutionType);
        ViewData["Title"] = "Quiz Summary Report";
        var model = new ReportQuizzesPageModel
        {
            IsConnected  = _api.IsConnected(),
            SemesterId   = semesterId,
            DepartmentId = departmentId,
            OfferingId   = offeringId,
            StudentId    = studentId,
            InstitutionType = selectedInstitutionType
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var identity = _api.GetSessionIdentity();
            var isFacultyOnly = identity?.IsFaculty == true && !identity.IsAdmin && !identity.IsSuperAdmin;
            var isAdminOnly   = identity?.IsAdmin == true && !identity.IsSuperAdmin;
            model.Semesters   = await _api.GetSemestersAsync(ct);
            model.Departments = await _api.GetDepartmentsAsync(ct);
            model.Offerings   = await _api.GetCourseOfferingsAsync(null, null, null, null, ct);
            model.Departments = FilterDepartmentsByInstitution(model.Departments, selectedInstitutionType);
            if (isFacultyOnly && !offeringId.HasValue && (semesterId.HasValue || departmentId.HasValue || studentId.HasValue))
            {
                model.Message = "Faculty must select a course offering before generating report data.";
            }
            else if (isAdminOnly && !departmentId.HasValue && !offeringId.HasValue)
            {
                model.Message = "Admin must select a department or course offering before generating report data.";
            }
            else if (semesterId.HasValue || departmentId.HasValue || offeringId.HasValue || studentId.HasValue)
                model.Report = await _api.GetQuizSummaryReportAsync(semesterId, departmentId, offeringId, studentId, selectedInstitutionType, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ReportGpa(Guid? departmentId, Guid? programId, int? institutionType, CancellationToken ct)
    {
        var selectedInstitutionType = ResolveReportInstitutionType(institutionType);
        ViewData["Title"] = "GPA & CGPA Report";
        var model = new ReportGpaPageModel
        {
            IsConnected  = _api.IsConnected(),
            DepartmentId = departmentId,
            ProgramId    = programId,
            InstitutionType = selectedInstitutionType
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var isAdminOnly = _api.GetSessionIdentity() is { } id && id.IsAdmin && !id.IsSuperAdmin;
            model.Departments = await _api.GetDepartmentsAsync(ct);
            model.Departments = FilterDepartmentsByInstitution(model.Departments, selectedInstitutionType);
            model.Programs    = await _api.GetProgramsAsync(null, ct);
            if (isAdminOnly && !departmentId.HasValue)
                model.Message = "Admin must select a department before generating report data.";
            else if (departmentId.HasValue || programId.HasValue)
                model.Report = await _api.GetGpaReportAsync(departmentId, programId, selectedInstitutionType, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ReportEnrollment(Guid? semesterId, Guid? departmentId, int? institutionType, CancellationToken ct)
    {
        var selectedInstitutionType = ResolveReportInstitutionType(institutionType);
        ViewData["Title"] = "Enrollment Summary Report";
        var model = new ReportEnrollmentPageModel
        {
            IsConnected  = _api.IsConnected(),
            SemesterId   = semesterId,
            DepartmentId = departmentId,
            InstitutionType = selectedInstitutionType
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var isAdminOnly = _api.GetSessionIdentity() is { } id && id.IsAdmin && !id.IsSuperAdmin;
            model.Semesters   = await _api.GetSemestersAsync(ct);
            model.Departments = await _api.GetDepartmentsAsync(ct);
            model.Departments = FilterDepartmentsByInstitution(model.Departments, selectedInstitutionType);
            if (isAdminOnly && !departmentId.HasValue)
                model.Message = "Admin must select a department before generating report data.";
            else
                model.Report = await _api.GetEnrollmentSummaryReportAsync(semesterId, departmentId, selectedInstitutionType, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ReportSemesterResults(Guid? semesterId, Guid? departmentId, int? institutionType, CancellationToken ct)
    {
        var selectedInstitutionType = ResolveReportInstitutionType(institutionType);
        ViewData["Title"] = "Semester Results Report";
        var model = new ReportSemesterResultsPageModel
        {
            IsConnected  = _api.IsConnected(),
            SemesterId   = semesterId,
            DepartmentId = departmentId,
            InstitutionType = selectedInstitutionType
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var isAdminOnly = _api.GetSessionIdentity() is { } id && id.IsAdmin && !id.IsSuperAdmin;
            model.Semesters   = await _api.GetSemestersAsync(ct);
            model.Departments = await _api.GetDepartmentsAsync(ct);
            model.Departments = FilterDepartmentsByInstitution(model.Departments, selectedInstitutionType);

            // API requires a non-empty semesterId; do not query until one is selected.
            if (semesterId.HasValue)
            {
                if (isAdminOnly && !departmentId.HasValue)
                    model.Message = "Admin must select a department before generating report data.";
                else
                    model.Report = await _api.GetSemesterResultsReportAsync(semesterId.Value, departmentId, selectedInstitutionType, ct);
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    // Excel export actions — these act as portal-side proxies:
    // they call the API export endpoint, receive the .xlsx bytes, and
    // stream the file directly to the browser. On failure they fall back
    // to the report view with a TempData error message.

    [HttpGet]
    public async Task<IActionResult> ExportAttendanceSummary(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(ReportAttendance));
        try
        {
            var bytes = await _api.ExportAttendanceSummaryAsync(semesterId, departmentId, offeringId, studentId, ResolveReportInstitutionType(institutionType), ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "attendance-summary.xlsx");
        }
        catch (Exception ex) { TempData["PortalMessage"] = $"Export failed: {ex.Message}"; }
        return RedirectToAction(nameof(ReportAttendance), new { semesterId, departmentId, offeringId, studentId, institutionType = ResolveReportInstitutionType(institutionType) });
    }

    [HttpGet]
    public async Task<IActionResult> ExportAttendanceSummaryCsv(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(ReportAttendance));
        try
        {
            var bytes = await _api.ExportAttendanceSummaryCsvAsync(semesterId, departmentId, offeringId, studentId, ResolveReportInstitutionType(institutionType), ct);
            return File(bytes, "text/csv", "attendance-summary.csv");
        }
        catch (Exception ex) { TempData["PortalMessage"] = $"Export CSV failed: {ex.Message}"; }
        return RedirectToAction(nameof(ReportAttendance), new { semesterId, departmentId, offeringId, studentId, institutionType = ResolveReportInstitutionType(institutionType) });
    }

    [HttpGet]
    public async Task<IActionResult> ExportAttendanceSummaryPdf(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(ReportAttendance));
        try
        {
            var bytes = await _api.ExportAttendanceSummaryPdfAsync(semesterId, departmentId, offeringId, studentId, ResolveReportInstitutionType(institutionType), ct);
            return File(bytes, "application/pdf", "attendance-summary.pdf");
        }
        catch (Exception ex) { TempData["PortalMessage"] = $"Export PDF failed: {ex.Message}"; }
        return RedirectToAction(nameof(ReportAttendance), new { semesterId, departmentId, offeringId, studentId, institutionType = ResolveReportInstitutionType(institutionType) });
    }

    [HttpGet]
    public async Task<IActionResult> ExportResultSummary(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(ReportResults));
        try
        {
            var bytes = await _api.ExportResultSummaryAsync(semesterId, departmentId, offeringId, studentId, ResolveReportInstitutionType(institutionType), ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "result-summary.xlsx");
        }
        catch (Exception ex) { TempData["PortalMessage"] = $"Export failed: {ex.Message}"; }
        return RedirectToAction(nameof(ReportResults), new { semesterId, departmentId, offeringId, studentId, institutionType = ResolveReportInstitutionType(institutionType) });
    }

    [HttpGet]
    public async Task<IActionResult> ExportResultSummaryCsv(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(ReportResults));
        try
        {
            var bytes = await _api.ExportResultSummaryCsvAsync(semesterId, departmentId, offeringId, studentId, ResolveReportInstitutionType(institutionType), ct);
            return File(bytes, "text/csv", "result-summary.csv");
        }
        catch (Exception ex) { TempData["PortalMessage"] = $"Export CSV failed: {ex.Message}"; }
        return RedirectToAction(nameof(ReportResults), new { semesterId, departmentId, offeringId, studentId, institutionType = ResolveReportInstitutionType(institutionType) });
    }

    [HttpGet]
    public async Task<IActionResult> ExportResultSummaryPdf(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(ReportResults));
        try
        {
            var bytes = await _api.ExportResultSummaryPdfAsync(semesterId, departmentId, offeringId, studentId, ResolveReportInstitutionType(institutionType), ct);
            return File(bytes, "application/pdf", "result-summary.pdf");
        }
        catch (Exception ex) { TempData["PortalMessage"] = $"Export PDF failed: {ex.Message}"; }
        return RedirectToAction(nameof(ReportResults), new { semesterId, departmentId, offeringId, studentId, institutionType = ResolveReportInstitutionType(institutionType) });
    }

    [HttpGet]
    public async Task<IActionResult> ExportAssignmentSummary(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(ReportAssignments));
        try
        {
            var bytes = await _api.ExportAssignmentSummaryAsync(semesterId, departmentId, offeringId, studentId, ResolveReportInstitutionType(institutionType), ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "assignment-summary.xlsx");
        }
        catch (Exception ex) { TempData["PortalMessage"] = $"Export failed: {ex.Message}"; }
        return RedirectToAction(nameof(ReportAssignments), new { semesterId, departmentId, offeringId, studentId, institutionType = ResolveReportInstitutionType(institutionType) });
    }

    [HttpGet]
    public async Task<IActionResult> ExportAssignmentSummaryCsv(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(ReportAssignments));
        try
        {
            var bytes = await _api.ExportAssignmentSummaryCsvAsync(semesterId, departmentId, offeringId, studentId, ResolveReportInstitutionType(institutionType), ct);
            return File(bytes, "text/csv", "assignment-summary.csv");
        }
        catch (Exception ex) { TempData["PortalMessage"] = $"Export CSV failed: {ex.Message}"; }
        return RedirectToAction(nameof(ReportAssignments), new { semesterId, departmentId, offeringId, studentId, institutionType = ResolveReportInstitutionType(institutionType) });
    }

    [HttpGet]
    public async Task<IActionResult> ExportAssignmentSummaryPdf(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(ReportAssignments));
        try
        {
            var bytes = await _api.ExportAssignmentSummaryPdfAsync(semesterId, departmentId, offeringId, studentId, ResolveReportInstitutionType(institutionType), ct);
            return File(bytes, "application/pdf", "assignment-summary.pdf");
        }
        catch (Exception ex) { TempData["PortalMessage"] = $"Export PDF failed: {ex.Message}"; }
        return RedirectToAction(nameof(ReportAssignments), new { semesterId, departmentId, offeringId, studentId, institutionType = ResolveReportInstitutionType(institutionType) });
    }

    [HttpGet]
    public async Task<IActionResult> ExportQuizSummary(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(ReportQuizzes));
        try
        {
            var bytes = await _api.ExportQuizSummaryAsync(semesterId, departmentId, offeringId, studentId, ResolveReportInstitutionType(institutionType), ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "quiz-summary.xlsx");
        }
        catch (Exception ex) { TempData["PortalMessage"] = $"Export failed: {ex.Message}"; }
        return RedirectToAction(nameof(ReportQuizzes), new { semesterId, departmentId, offeringId, studentId, institutionType = ResolveReportInstitutionType(institutionType) });
    }

    [HttpGet]
    public async Task<IActionResult> ExportQuizSummaryCsv(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(ReportQuizzes));
        try
        {
            var bytes = await _api.ExportQuizSummaryCsvAsync(semesterId, departmentId, offeringId, studentId, ResolveReportInstitutionType(institutionType), ct);
            return File(bytes, "text/csv", "quiz-summary.csv");
        }
        catch (Exception ex) { TempData["PortalMessage"] = $"Export CSV failed: {ex.Message}"; }
        return RedirectToAction(nameof(ReportQuizzes), new { semesterId, departmentId, offeringId, studentId, institutionType = ResolveReportInstitutionType(institutionType) });
    }

    [HttpGet]
    public async Task<IActionResult> ExportQuizSummaryPdf(
        Guid? semesterId, Guid? departmentId, Guid? offeringId, Guid? studentId, int? institutionType, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(ReportQuizzes));
        try
        {
            var bytes = await _api.ExportQuizSummaryPdfAsync(semesterId, departmentId, offeringId, studentId, ResolveReportInstitutionType(institutionType), ct);
            return File(bytes, "application/pdf", "quiz-summary.pdf");
        }
        catch (Exception ex) { TempData["PortalMessage"] = $"Export PDF failed: {ex.Message}"; }
        return RedirectToAction(nameof(ReportQuizzes), new { semesterId, departmentId, offeringId, studentId, institutionType = ResolveReportInstitutionType(institutionType) });
    }

    [HttpGet]
    public async Task<IActionResult> ExportGpaReport(Guid? departmentId, Guid? programId, int? institutionType, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(ReportGpa));
        try
        {
            var bytes = await _api.ExportGpaReportAsync(departmentId, programId, ResolveReportInstitutionType(institutionType), ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "gpa-report.xlsx");
        }
        catch (Exception ex) { TempData["PortalMessage"] = $"Export failed: {ex.Message}"; }
        return RedirectToAction(nameof(ReportGpa), new { departmentId, programId, institutionType = ResolveReportInstitutionType(institutionType) });
    }

    // ── Stage 4.2: Additional Reports ─────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> ReportTranscript(Guid? studentProfileId, CancellationToken ct)
    {
        ViewData["Title"] = "Student Transcript";
        var model = new ReportTranscriptPageModel
        {
            IsConnected      = _api.IsConnected(),
            StudentProfileId = studentProfileId
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var students = await _api.GetStudentsAsync(null, ct);
            model.Students = students.Select(s => new LookupItem { Id = s.Id, Name = $"{s.FullName} ({s.RegistrationNumber})" }).ToList();
            if (studentProfileId.HasValue)
                model.Report = await _api.GetStudentTranscriptReportAsync(studentProfileId.Value, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportStudentTranscript(Guid studentProfileId, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(ReportTranscript));
        try
        {
            var bytes = await _api.ExportStudentTranscriptAsync(studentProfileId, ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "student-transcript.xlsx");
        }
        catch (Exception ex) { TempData["PortalMessage"] = $"Export failed: {ex.Message}"; }
        return RedirectToAction(nameof(ReportTranscript), new { studentProfileId });
    }

    [HttpGet]
    public async Task<IActionResult> ReportLowAttendance(
        decimal threshold = 75m, Guid? departmentId = null, Guid? courseOfferingId = null, int? institutionType = null, CancellationToken ct = default)
    {
        threshold = 75m;
        var selectedInstitutionType = ResolveReportInstitutionType(institutionType);
        ViewData["Title"] = "Low Attendance Warning";
        var model = new ReportLowAttendancePageModel
        {
            IsConnected      = _api.IsConnected(),
            Threshold        = threshold,
            DepartmentId     = departmentId,
            CourseOfferingId = courseOfferingId,
            InstitutionType  = selectedInstitutionType
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var isAdminOnly = _api.GetSessionIdentity() is { } id && id.IsAdmin && !id.IsSuperAdmin;
            model.Departments     = await _api.GetDepartmentsAsync(ct);
            model.Departments     = FilterDepartmentsByInstitution(model.Departments, selectedInstitutionType);
            var sessionIdentity = _api.GetSessionIdentity();
            var effectiveTenantId = sessionIdentity?.TenantId;
            var effectiveCampusId = sessionIdentity?.CampusId;
            var scopedOfferings = await _api.GetCourseOfferingsAsync(departmentId, effectiveTenantId, effectiveCampusId, selectedInstitutionType, ct);
            model.CourseOfferings = scopedOfferings
                .Select(o => new LookupItem { Id = o.Id, Name = $"{o.CourseCode} - {o.CourseTitle}" })
                .ToList();
            if (isAdminOnly && !departmentId.HasValue && !courseOfferingId.HasValue)
                model.Message = "Admin must select a department or course offering before generating report data.";
            else
                model.Report = await _api.GetLowAttendanceReportAsync(75m, departmentId, courseOfferingId, selectedInstitutionType, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ReportFypStatus(
        Guid? departmentId = null, string? status = null, int? institutionType = null, CancellationToken ct = default)
    {
        var selectedInstitutionType = ResolveReportInstitutionType(institutionType);
        ViewData["Title"] = "FYP Status Report";
        var model = new ReportFypStatusPageModel
        {
            IsConnected    = _api.IsConnected(),
            DepartmentId   = departmentId,
            SelectedStatus = status,
            InstitutionType = selectedInstitutionType
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var isAdminOnly = _api.GetSessionIdentity() is { } id && id.IsAdmin && !id.IsSuperAdmin;
            model.Departments = await _api.GetDepartmentsAsync(ct);
            model.Departments = FilterDepartmentsByInstitution(model.Departments, selectedInstitutionType);
            if (isAdminOnly && !departmentId.HasValue)
                model.Message = "Admin must select a department before generating report data.";
            else
                model.Report = await _api.GetFypStatusReportAsync(departmentId, status, selectedInstitutionType, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ReportPayments(
        int? year,
        int? month,
        Guid? semesterId,
        Guid? departmentId,
        Guid? courseId,
        int? levelNumber,
        int? institutionType,
        CancellationToken ct)
    {
        var selectedInstitutionType = ResolveReportInstitutionType(institutionType);
        ViewData["Title"] = "Payment Summary Report";
        var model = new ReportPaymentsPageModel
        {
            IsConnected = _api.IsConnected(),
            Year = year,
            Month = month,
            SemesterId = semesterId,
            DepartmentId = departmentId,
            CourseId = courseId,
            LevelNumber = levelNumber,
            InstitutionType = selectedInstitutionType
        };
        if (!model.IsConnected) return View(model);

        try
        {
            model.Semesters = await _api.GetSemestersAsync(ct);
            model.Departments = await _api.GetDepartmentsAsync(ct);
            model.Departments = FilterDepartmentsByInstitution(model.Departments, selectedInstitutionType);
            model.Courses = await _api.GetCoursesAsync(departmentId, ct);

            if (year.HasValue || month.HasValue || semesterId.HasValue || departmentId.HasValue || courseId.HasValue || levelNumber.HasValue)
            {
                model.Report = await _api.GetPaymentSummaryReportAsync(
                    year,
                    month,
                    semesterId,
                    departmentId,
                    courseId,
                    levelNumber,
                    selectedInstitutionType,
                    ct);
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportPaymentSummary(
        int? year,
        int? month,
        Guid? semesterId,
        Guid? departmentId,
        Guid? courseId,
        int? levelNumber,
        int? institutionType,
        CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(ReportPayments));
        try
        {
            var bytes = await _api.ExportPaymentSummaryAsync(year, month, semesterId, departmentId, courseId, levelNumber, ResolveReportInstitutionType(institutionType), ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "payment-summary.xlsx");
        }
        catch (Exception ex) { TempData["PortalMessage"] = $"Export failed: {ex.Message}"; }
        return RedirectToAction(nameof(ReportPayments), new { year, month, semesterId, departmentId, courseId, levelNumber, institutionType = ResolveReportInstitutionType(institutionType) });
    }

    [HttpGet]
    public async Task<IActionResult> ExportPaymentSummaryCsv(
        int? year,
        int? month,
        Guid? semesterId,
        Guid? departmentId,
        Guid? courseId,
        int? levelNumber,
        int? institutionType,
        CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(ReportPayments));
        try
        {
            var bytes = await _api.ExportPaymentSummaryCsvAsync(year, month, semesterId, departmentId, courseId, levelNumber, ResolveReportInstitutionType(institutionType), ct);
            return File(bytes, "text/csv", "payment-summary.csv");
        }
        catch (Exception ex) { TempData["PortalMessage"] = $"Export CSV failed: {ex.Message}"; }
        return RedirectToAction(nameof(ReportPayments), new { year, month, semesterId, departmentId, courseId, levelNumber, institutionType = ResolveReportInstitutionType(institutionType) });
    }

    [HttpGet]
    public async Task<IActionResult> ExportPaymentSummaryPdf(
        int? year,
        int? month,
        Guid? semesterId,
        Guid? departmentId,
        Guid? courseId,
        int? levelNumber,
        int? institutionType,
        CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(ReportPayments));
        try
        {
            var bytes = await _api.ExportPaymentSummaryPdfAsync(year, month, semesterId, departmentId, courseId, levelNumber, ResolveReportInstitutionType(institutionType), ct);
            return File(bytes, "application/pdf", "payment-summary.pdf");
        }
        catch (Exception ex) { TempData["PortalMessage"] = $"Export PDF failed: {ex.Message}"; }
        return RedirectToAction(nameof(ReportPayments), new { year, month, semesterId, departmentId, courseId, levelNumber, institutionType = ResolveReportInstitutionType(institutionType) });
    }

    private int? ResolveReportInstitutionType(int? requestedInstitutionType)
    {
        var identity = _api.GetSessionIdentity();
        if (identity is { IsSuperAdmin: false, InstitutionType: not null })
            return identity.InstitutionType.Value;

        return requestedInstitutionType;
    }

    private static List<LookupItem> FilterDepartmentsByInstitution(List<LookupItem> departments, int? institutionType)
    {
        if (!institutionType.HasValue)
            return departments;

        return departments
            .Where(d => !d.InstitutionType.HasValue || d.InstitutionType.Value == institutionType.Value)
            .ToList();
    }

    // ── Dashboard Settings ────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> DashboardSettings(CancellationToken ct)
    {
        var model = new DashboardSettingsPageModel { IsConnected = _api.IsConnected() };
        if (model.IsConnected)
        {
            try { model.Branding = await _api.GetPortalBrandingAsync(ct); }
            catch (Exception ex) { model.Message = ex.Message; }
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DashboardSettings(DashboardSettingsPageModel model, CancellationToken ct)
    {
        if (!_api.IsConnected())
        {
            TempData["Message"] = "Not connected to API.";
            return RedirectToAction(nameof(DashboardSettings));
        }
        try
        {
            // Handle logo file upload if provided
            if (model.LogoFile is { Length: > 0 })
            {
                await using var stream = model.LogoFile.OpenReadStream();
                var logoUrl = await _api.UploadLogoAsync(stream, model.LogoFile.FileName, ct);
                if (logoUrl is not null)
                    model.Branding.LogoImage = logoUrl;
            }

            await _api.SavePortalBrandingAsync(model.Branding, ct);
            TempData["Message"] = "Portal branding saved successfully.";
        }
        catch (Exception ex)
        {
            TempData["Message"] = "Error: " + ex.Message;
        }
        return RedirectToAction(nameof(DashboardSettings));
    }

    // ── Phase 12: Academic Calendar ────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> AcademicCalendar(Guid? semesterId, CancellationToken ct)
    {
        ViewData["Title"] = "Academic Calendar";
        var model = new AcademicCalendarPageModel
        {
            IsConnected        = _api.IsConnected(),
            SelectedSemesterId = semesterId
        };
        if (!model.IsConnected) return View(model);
        try
        {
            model.Semesters = await _api.GetSemestersAsync(ct);
            model.Deadlines = await _api.GetCalendarDeadlinesAsync(semesterId, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AcademicDeadlines(Guid? semesterId, CancellationToken ct)
    {
        ViewData["Title"] = "Manage Deadlines";
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        var identity = _api.GetSessionIdentity();
        if (identity is null || (!identity.IsAdmin && !identity.IsSuperAdmin))
            return Forbid();

        var model = new AcademicDeadlinesPageModel
        {
            IsConnected        = true,
            SelectedSemesterId = semesterId
        };
        try
        {
            model.Semesters = await _api.GetSemestersAsync(ct);
            model.Deadlines = await _api.GetCalendarDeadlinesAsync(semesterId, ct);
            if (semesterId.HasValue)
                model.Form.SemesterId = semesterId.Value;
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDeadline(DeadlineFormModel form, Guid? semesterId, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        try
        {
            await _api.CreateCalendarDeadlineAsync(form, ct);
            TempData["Message"] = "Deadline created successfully.";
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return RedirectToAction(nameof(AcademicDeadlines), new { semesterId = form.SemesterId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDeadline(Guid id, DeadlineFormModel form, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        try
        {
            await _api.UpdateCalendarDeadlineAsync(id, form, ct);
            TempData["Message"] = "Deadline updated successfully.";
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return RedirectToAction(nameof(AcademicDeadlines), new { semesterId = form.SemesterId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDeadline(Guid id, Guid? semesterId, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        try
        {
            await _api.DeleteCalendarDeadlineAsync(id, ct);
            TempData["Message"] = "Deadline deleted.";
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return RedirectToAction(nameof(AcademicDeadlines), new { semesterId });
    }

    // ── Phase 13: Global Search ───────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Search(string? q, int limit = 20, CancellationToken ct = default)
    {
        var model = new SearchPageModel { IsConnected = _api.IsConnected(), Query = q?.Trim() ?? "" };

        if (!model.IsConnected || string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return View(model);

        try
        {
            model.Response = await _api.SearchAsync(q.Trim(), Math.Clamp(limit, 1, 50), ct);
        }
        catch (Exception ex)
        {
            model.Response = new SearchWebResponse(q.Trim(), 0, new());
            TempData["SearchError"] = ex.Message;
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SearchTypeahead(string? q, CancellationToken ct = default)
    {
        if (!_api.IsConnected() || string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Json(new List<object>());

        try
        {
            var response = await _api.SearchAsync(q.Trim(), 5, ct);
            var items = response.Results.Select(r => new
            {
                r.Type,
                r.Id,
                r.Label,
                r.SubLabel,
                r.Url
            });
            return Json(items);
        }
        catch
        {
            return Json(new List<object>());
        }
    }

    // ── Phase 14: Helpdesk / Support Ticketing ────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Helpdesk(TicketStatusWeb? status, int page = 1, CancellationToken ct = default)
    {
        var session = _api.GetSessionIdentity();
        var callerIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid.TryParse(callerIdStr, out var callerId);
        var model = new HelpdeskListPageModel
        {
            IsConnected = _api.IsConnected(),
            CallerRole = session?.Roles.FirstOrDefault() ?? string.Empty,
            CallerId = callerId,
            StatusFilter = status,
            Page = page < 1 ? 1 : page
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var ticketPage = await _api.GetTicketsAsync(status, model.Page, model.PageSize, ct);
            model.Tickets = ticketPage.Items;
            model.Page = ticketPage.Page;
            model.PageSize = ticketPage.PageSize;
            model.TotalCount = ticketPage.TotalCount;
            // Load staff users for assign dropdown (Admin/SuperAdmin only)
            if (model.CallerRole is "SuperAdmin" or "Admin")
                model.StaffUsers = (await _api.GetFacultyAsync(ct))
                    .Select(f => new LookupItem { Id = f.Id, Name = f.DisplayName })
                    .ToList();
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> HelpdeskCreate(CancellationToken ct = default)
    {
        var model = new HelpdeskCreatePageModel { IsConnected = _api.IsConnected() };
        if (!model.IsConnected) return View(model);

        model.Departments = await _api.GetDepartmentsAsync(ct);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HelpdeskCreate(
        Guid? departmentId, TicketCategoryWeb category,
        string subject, string body, CancellationToken ct = default)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        try
        {
            var id = await _api.CreateTicketAsync(departmentId, category, subject, body, ct);
            TempData["Message"] = "Ticket submitted successfully.";
            return RedirectToAction(nameof(HelpdeskDetail), new { id });
        }
        catch (Exception ex)
        {
            TempData["Message"] = "Error: " + ex.Message;
            return RedirectToAction(nameof(HelpdeskCreate));
        }
    }

    [HttpGet]
    public async Task<IActionResult> HelpdeskDetail(Guid id, CancellationToken ct = default)
    {
        var session = _api.GetSessionIdentity();
        var callerIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid.TryParse(callerIdStr, out var callerId);
        var model = new HelpdeskDetailPageModel
        {
            IsConnected = _api.IsConnected(),
            CallerRole  = session?.Roles.FirstOrDefault() ?? "",
            CallerId    = callerId
        };

        if (!model.IsConnected) return View(model);

        try
        {
            model.Ticket = await _api.GetTicketDetailAsync(id, ct);
            if (model.CallerRole is "SuperAdmin" or "Admin")
                model.StaffUsers = (await _api.GetFacultyAsync(ct))
                    .Select(f => new LookupItem { Id = f.Id, Name = f.DisplayName })
                    .ToList();
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HelpdeskAddMessage(Guid ticketId, string body,
        bool isInternalNote = false, CancellationToken ct = default)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        try
        {
            await _api.AddTicketMessageAsync(ticketId, body, isInternalNote, ct);
            TempData["Message"] = "Reply posted.";
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return RedirectToAction(nameof(HelpdeskDetail), new { id = ticketId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HelpdeskAssign(Guid ticketId, Guid assignedToId, CancellationToken ct = default)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        try
        {
            await _api.AssignTicketAsync(ticketId, assignedToId, ct);
            TempData["Message"] = "Ticket assigned.";
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return RedirectToAction(nameof(HelpdeskDetail), new { id = ticketId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HelpdeskResolve(Guid ticketId, CancellationToken ct = default)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        try
        {
            await _api.ResolveTicketAsync(ticketId, ct);
            TempData["Message"] = "Ticket resolved.";
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return RedirectToAction(nameof(HelpdeskDetail), new { id = ticketId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HelpdeskClose(Guid ticketId, CancellationToken ct = default)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        try
        {
            await _api.CloseTicketAsync(ticketId, ct);
            TempData["Message"] = "Ticket closed.";
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return RedirectToAction(nameof(HelpdeskDetail), new { id = ticketId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HelpdeskReopen(Guid ticketId, CancellationToken ct = default)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        try
        {
            await _api.ReopenTicketAsync(ticketId, ct);
            TempData["Message"] = "Ticket re-opened.";
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return RedirectToAction(nameof(HelpdeskDetail), new { id = ticketId });
    }

    // ── Phase 15: Enrollment Rules — Prerequisites ─────────────────────────────────

    // Final-Touches Phase 15 Stage 15.1 — Prerequisites: Admin/SuperAdmin manage course prerequisites
    [HttpGet]
    public async Task<IActionResult> Prerequisites(Guid? departmentId, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));

        var departments = await _api.GetDepartmentsAsync(ct);
        var model = new PrerequisitesPageModel
        {
            IsConnected          = true,
            Message              = TempData["Message"] as string,
            Departments          = departments.Select(d => new LookupItem { Id = d.Id, Name = d.Name }).ToList(),
            SelectedDepartmentId = departmentId
        };

        var courses = await _api.GetCourseDetailsAsync(departmentId, null, null, null, ct);
        model.Courses = courses;

        var groups = new List<CoursePrerequisiteGroup>();
        foreach (var course in courses)
        {
            var prereqs = await _api.GetPrerequisitesAsync(course.Id, ct);
            groups.Add(new CoursePrerequisiteGroup
            {
                CourseId      = course.Id,
                CourseCode    = course.Code,
                CourseTitle   = course.Title,
                Prerequisites = prereqs
            });
        }
        model.CourseGroups = groups;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PrerequisiteAdd(Guid courseId, Guid prerequisiteCourseId, Guid? departmentId, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        try
        {
            await _api.AddPrerequisiteAsync(courseId, prerequisiteCourseId, ct);
            TempData["Message"] = "Prerequisite added.";
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return RedirectToAction(nameof(Prerequisites), new { departmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PrerequisiteRemove(Guid courseId, Guid prerequisiteCourseId, Guid? departmentId, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        try
        {
            await _api.RemovePrerequisiteAsync(courseId, prerequisiteCourseId, ct);
            TempData["Message"] = "Prerequisite removed.";
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return RedirectToAction(nameof(Prerequisites), new { departmentId });
    }

    // ── Phase 16: Faculty Grading System ──────────────────────────────────────────

    // Final-Touches Phase 16 Stage 16.1 — Gradebook grid for faculty
    public async Task<IActionResult> Gradebook(Guid? offeringId, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        var model = new GradebookPageModel { IsConnected = true };
        try
        {
            model.Offerings = await _api.GetMyOfferingsAsync(ct);
            model.SelectedOffering = offeringId;
            if (offeringId.HasValue)
                model.Grid = await _api.GetGradebookAsync(offeringId.Value, ct);
        }
        catch (Exception ex) { model.Message = "Error: " + ex.Message; }
        return View(model);
    }

    // Final-Touches Phase 16 Stage 16.1 — AJAX endpoint: upsert one result cell inline
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GradebookUpsertEntry(
        Guid offeringId,
        Guid studentProfileId,
        string componentName,
        decimal marksObtained,
        decimal maxMarks,
        CancellationToken ct)
    {
        if (!_api.IsConnected()) return Json(new { success = false, error = "Not connected." });
        try
        {
            await _api.UpsertGradebookEntryAsync(offeringId, studentProfileId, componentName, marksObtained, maxMarks, ct);
            return Json(new { success = true });
        }
        catch (Exception ex) { return Json(new { success = false, error = ex.Message }); }
    }

    // Final-Touches Phase 16 Stage 16.1 — publish all results for an offering
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GradebookPublishAll(Guid offeringId, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        try
        {
            await _api.PublishGradebookAllAsync(offeringId, ct);
            TempData["Message"] = "All results published.";
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return RedirectToAction(nameof(Gradebook), new { offeringId });
    }

    // Final-Touches Phase 16 Stage 16.3 — download CSV template
    public async Task<IActionResult> GradebookCsvTemplate(Guid offeringId, string component, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        var bytes = await _api.GetGradebookCsvTemplateAsync(offeringId, component, ct);
        return File(bytes, "text/csv", $"gradebook-{component}-template.csv");
    }

    // Final-Touches Phase 16 Stage 16.3 — upload CSV preview
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GradebookBulkUpload(
        Guid offeringId,
        string componentName,
        Microsoft.AspNetCore.Http.IFormFile file,
        CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        if (file is null || file.Length == 0)
        {
            TempData["Message"] = "No file selected.";
            return RedirectToAction(nameof(Gradebook), new { offeringId });
        }
        try
        {
            await using var stream = file.OpenReadStream();
            var preview = await _api.UploadBulkGradeCsvAsync(offeringId, componentName, stream, file.FileName, ct);
            TempData["BulkPreview"] = System.Text.Json.JsonSerializer.Serialize(preview);
            TempData["BulkOffering"] = offeringId.ToString();
            TempData["BulkComponent"] = componentName;
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return RedirectToAction(nameof(Gradebook), new { offeringId });
    }

    // Final-Touches Phase 16 Stage 16.3 — confirm bulk grade
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GradebookBulkConfirm(
        Guid offeringId,
        string previewJson,
        CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        try
        {
            var preview = System.Text.Json.JsonSerializer.Deserialize<BulkGradePreviewWebModel>(previewJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (preview is null) throw new InvalidOperationException("Invalid preview data.");
            var confirm = new BulkGradeConfirmWebRequest
            {
                CourseOfferingId = offeringId,
                ComponentName    = preview.ComponentName,
                ValidRows        = preview.Rows.Where(r => r.ValidationError is null).ToList()
            };
            await _api.ConfirmBulkGradeAsync(offeringId, confirm, ct);
            TempData["Message"] = $"Applied {confirm.ValidRows.Count} grade(s).";
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return RedirectToAction(nameof(Gradebook), new { offeringId });
    }

    // Final-Touches Phase 16 Stage 16.2 — rubric management (Faculty/Admin)
    public async Task<IActionResult> RubricManage(Guid? offeringId, Guid? assignmentId, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        var model = new RubricManagePageModel { IsConnected = true };
        try
        {
            model.Offerings = await _api.GetMyOfferingsAsync(ct);
            model.SelectedOffering = offeringId;
            if (offeringId.HasValue)
            {
                model.Assignments = await _api.GetAssignmentsByOfferingAsync(offeringId.Value, ct);
                model.SelectedAssignment = assignmentId;
                if (assignmentId.HasValue)
                {
                    try { model.Rubric = await _api.GetRubricByAssignmentAsync(assignmentId.Value, ct); }
                    catch { /* no rubric yet — model.Rubric stays null */ }
                }
            }
        }
        catch (Exception ex) { model.Message = "Error: " + ex.Message; }
        return View(model);
    }

    // Final-Touches Phase 16 Stage 16.2 — create rubric POST handler
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RubricCreate(CreateRubricWebRequest request, Guid? offeringId, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        try
        {
            await _api.CreateRubricAsync(request, ct);
            TempData["Message"] = "Rubric created.";
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return RedirectToAction(nameof(RubricManage), new { offeringId, assignmentId = request.AssignmentId });
    }

    // Final-Touches Phase 16 Stage 16.2 — delete (deactivate) rubric
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RubricDelete(Guid rubricId, Guid? offeringId, Guid? assignmentId, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        try
        {
            await _api.DeleteRubricAsync(rubricId, ct);
            TempData["Message"] = "Rubric deleted.";
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return RedirectToAction(nameof(RubricManage), new { offeringId, assignmentId });
    }

    // Final-Touches Phase 16 Stage 16.2 — student rubric grade view
    public async Task<IActionResult> RubricView(Guid rubricId, Guid submissionId, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
        var model = new RubricViewPageModel { IsConnected = true };
        try
        {
            model.Grade = await _api.GetRubricGradeAsync(rubricId, submissionId, ct);
        }
        catch (Exception ex) { model.Message = "Error: " + ex.Message; }
        return View(model);
    }

    // ── Phase 17: Degree Audit System ─────────────────────────────────────────

    // Final-Touches Phase 17 Stage 17.1 — student views own degree audit
    public async Task<IActionResult> DegreeAudit(Guid? studentProfileId, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));

        var access = await CanUseDegreeAuditAsync(ct);
        if (!access.Allowed)
        {
            TempData["Message"] = access.Message;
            return RedirectToAction(nameof(Dashboard));
        }

        var model = new DegreeAuditPageModel();
        try
        {
            if (studentProfileId.HasValue)
            {
                model.SelectedStudentProfileId = studentProfileId;
                model.Audit = await _api.GetStudentDegreeAuditAsync(studentProfileId.Value, ct);
            }
            else
            {
                // Student self-audit
                model.Audit = await _api.GetMyDegreeAuditAsync(ct);
            }
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return View(model);
    }

    // Final-Touches Phase 17 Stage 17.2 — admin views eligibility list
    public async Task<IActionResult> GraduationEligibility(Guid? departmentId, Guid? programId, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));

        var access = await CanUseDegreeAuditAsync(ct);
        if (!access.Allowed)
        {
            TempData["Message"] = access.Message;
            return RedirectToAction(nameof(Dashboard));
        }

        var model = new EligibilityPageModel { DepartmentId = departmentId, ProgramId = programId };
        try
        {
            model.Items = await _api.GetEligibilityListAsync(departmentId, programId, ct);
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GraduationEligibilityGraduate(
        Guid studentId,
        Guid? departmentId,
        Guid? programId,
        CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));

        var access = await CanUseDegreeAuditAsync(ct);
        if (!access.Allowed)
        {
            TempData["Message"] = access.Message;
            return RedirectToAction(nameof(GraduationEligibility), new { departmentId, programId });
        }

        try
        {
            await _api.GraduateStudentAsync(studentId, null, null, ct);
            TempData["Message"] = "Student graduated successfully.";
        }
        catch (Exception ex)
        {
            TempData["Message"] = "Error: " + ex.Message;
        }

        return RedirectToAction(nameof(GraduationEligibility), new { departmentId, programId });
    }

    // Final-Touches Phase 17 Stage 17.2 — SuperAdmin manages degree rules
    public async Task<IActionResult> DegreeRules(CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));

        var access = await CanUseDegreeAuditAsync(ct);
        if (!access.Allowed)
        {
            TempData["Message"] = access.Message;
            return RedirectToAction(nameof(Dashboard));
        }

        var model = new DegreeRulesPageModel();
        try
        {
            model.Rules    = await _api.GetAllDegreeRulesAsync(ct);
            model.Programs = await _api.GetProgramsAsync(null, ct);
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return View(model);
    }

    // Final-Touches Phase 17 Stage 17.2 — POST create degree rule
    [HttpPost]
    public async Task<IActionResult> DegreeRuleCreate(CreateDegreeRuleWebRequest request, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));

        var access = await CanUseDegreeAuditAsync(ct);
        if (!access.Allowed)
        {
            TempData["Message"] = access.Message;
            return RedirectToAction(nameof(DegreeRules));
        }

        try
        {
            await _api.CreateDegreeRuleAsync(request, ct);
            TempData["Message"] = "Degree rule created successfully.";
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return RedirectToAction(nameof(DegreeRules));
    }

    // Final-Touches Phase 17 Stage 17.2 — POST delete degree rule
    [HttpPost]
    public async Task<IActionResult> DegreeRuleDelete(Guid ruleId, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));

        var access = await CanUseDegreeAuditAsync(ct);
        if (!access.Allowed)
        {
            TempData["Message"] = access.Message;
            return RedirectToAction(nameof(DegreeRules));
        }

        try
        {
            await _api.DeleteDegreeRuleAsync(ruleId, ct);
            TempData["Message"] = "Degree rule deleted.";
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return RedirectToAction(nameof(DegreeRules));
    }

    // Final-Touches Phase 17 Stage 17.3 — AJAX POST to tag course type
    [HttpPost]
    public async Task<IActionResult> CourseSetType(Guid courseId, string courseType, CancellationToken ct)
    {
        if (!_api.IsConnected()) return Json(new { success = false });

        var access = await CanUseDegreeAuditAsync(ct);
        if (!access.Allowed)
            return Json(new { success = false, message = access.Message });

        try
        {
            await _api.SetCourseTypeAsync(courseId, courseType, ct);
            return Json(new { success = true });
        }
        catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
    }

    // ── Phase 18: Graduation Workflow ─────────────────────────────────────────

    // Final-Touches Phase 18 Stage 18.1 — student views own graduation applications + submit form
    public async Task<IActionResult> GraduationApply(int page = 1, CancellationToken ct = default)
    {
        if (!_api.IsConnected()) return RedirectToAction("Connect", "Home");
        var model = new GraduationApplyPageModel { Page = page < 1 ? 1 : page };
        try
        {
            var pageResult = await _api.GetMyGraduationApplicationsAsync(model.Page, model.PageSize, ct);
            model.Applications = pageResult?.Items ?? new();
            model.TotalCount = pageResult?.TotalCount ?? 0;
            model.PageSize = pageResult?.PageSize ?? model.PageSize;
            model.Page = pageResult?.Page ?? model.Page;

            var firstPage = await _api.GetMyGraduationApplicationsAsync(1, 1, ct);
            var latest = firstPage?.Items ?? new List<GraduationApplicationWebModel>();
            model.CanSubmitNew = latest.Count == 0 || latest.All(a => a.Status == "Rejected" || a.Status == "Approved");

            if (TempData["SuccessMessage"] is string s) model.SuccessMessage = s;
            if (TempData["ErrorMessage"]   is string e) model.ErrorMessage   = e;
        }
        catch { model.ErrorMessage = "Could not load applications."; }
        return View(model);
    }

    // Final-Touches Phase 18 Stage 18.1 — POST: student submits graduation application
    [HttpPost]
    public async Task<IActionResult> GraduationSubmit(string? studentNote, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction("Connect", "Home");
        try
        {
            await _api.SubmitGraduationApplicationAsync(studentNote, ct);
            TempData["SuccessMessage"] = "Application submitted successfully.";
        }
        catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(GraduationApply));
    }

    // Final-Touches Phase 18 Stage 18.1 — admin/faculty views application list
    public async Task<IActionResult> GraduationApplications(string? status, Guid? departmentId, int page = 1, CancellationToken ct = default)
    {
        if (!_api.IsConnected()) return RedirectToAction("Connect", "Home");
        var model = new GraduationApplicationsPageModel
        {
            StatusFilter     = status,
            DepartmentFilter = departmentId,
            Page = page < 1 ? 1 : page
        };
        try
        {
            var pageResult = await _api.GetGraduationApplicationsAsync(departmentId, status, model.Page, model.PageSize, ct);
            model.Applications = pageResult?.Items ?? new();
            model.TotalCount = pageResult?.TotalCount ?? 0;
            model.PageSize = pageResult?.PageSize ?? model.PageSize;
            model.Page = pageResult?.Page ?? model.Page;

            var depts = await _api.GetDepartmentsAsync(ct);
            model.Departments = depts.Select(d => new LookupItem { Id = d.Id, Name = d.Name }).ToList();
        }
        catch { }
        return View(model);
    }

    // Final-Touches Phase 18 Stage 18.1/18.2 — view application detail
    public async Task<IActionResult> GraduationApplicationDetail(Guid id, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction("Connect", "Home");
        var model = new GraduationApplicationDetailPageModel();
        try
        {
            model.Application  = await _api.GetGraduationApplicationDetailAsync(id, ct);
            if (TempData["SuccessMessage"] is string s) model.SuccessMessage = s;
            if (TempData["ErrorMessage"]   is string e) model.ErrorMessage   = e;
        }
        catch (Exception ex) { model.ErrorMessage = ex.Message; }
        return View(model);
    }

    // Final-Touches Phase 18 Stage 18.1 — POST: approve or reject at the right stage
    [HttpPost]
    public async Task<IActionResult> GraduationApprove(Guid id, string action, string? note, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction("Connect", "Home");
        bool isApproved = action == "approve";
        var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        try
        {
            var detail = await _api.GetGraduationApplicationDetailAsync(id, ct);
            var status = detail?.Status ?? string.Empty;

            if (roleClaim == "Faculty")
                await _api.FacultyApproveApplicationAsync(id, isApproved, note, ct);
            else if (roleClaim == "SuperAdmin" && isApproved && status == "PendingFinalApproval")
                await _api.FinalApproveApplicationAsync(id, true, note, ct);
            else if ((roleClaim == "Admin" || roleClaim == "SuperAdmin") && (status == "PendingAdmin" || !isApproved))
                await _api.AdminApproveApplicationAsync(id, isApproved, note, ct);
            else
                throw new InvalidOperationException("You are not allowed to approve this application at its current stage.");

            TempData["SuccessMessage"] = isApproved ? "Application approved." : "Application rejected.";
        }
        catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(GraduationApplicationDetail), new { id });
    }

    // Final-Touches Phase 18 Stage 18.1 — POST: explicit reject
    [HttpPost]
    public async Task<IActionResult> GraduationReject(Guid id, string? reason, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction("Connect", "Home");
        try
        {
            await _api.RejectApplicationAsync(id, reason, ct);
            TempData["SuccessMessage"] = "Application rejected.";
        }
        catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(GraduationApplicationDetail), new { id });
    }

    // Final-Touches Phase 18 Stage 18.2 — download certificate
    public async Task<IActionResult> GraduationCertificateDownload(Guid id, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction("Connect", "Home");
        var bytes = await _api.DownloadCertificateAsync(id, ct);
        if (bytes is null) return NotFound("Certificate not found.");
        return File(bytes, "application/pdf", $"certificate_{id}.pdf");
    }

    // Final-Touches Phase 18 Stage 18.2 — POST: regenerate certificate (admin)
    [HttpPost]
    public async Task<IActionResult> GraduationRegenerateCertificate(Guid id, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction("Connect", "Home");
        try
        {
            await _api.RegenerateCertificateAsync(id, ct);
            TempData["SuccessMessage"] = "Certificate regenerated.";
        }
        catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(GraduationApplicationDetail), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GenerateCertificates(
        Guid? tenantId,
        Guid? campusId,
        Guid? departmentId,
        Guid? courseId,
        Guid? semesterId,
        CancellationToken ct)
    {
        ViewData["Title"] = "Generate Certificates";

        var identity = _api.GetSessionIdentity();
        var model = new GenerateCertificatesPageModel
        {
            IsConnected = _api.IsConnected(),
            Identity = identity,
            CanManage = identity?.IsAdmin == true || identity?.IsSuperAdmin == true,
            CanUploadAdditionalCertificates = identity?.IsAdmin == true || identity?.IsSuperAdmin == true,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId,
            SelectedDepartmentId = departmentId,
            SelectedCourseId = courseId,
            SelectedSemesterId = semesterId,
            Message = TempData["PortalMessage"]?.ToString()
        };

        if (!model.IsConnected)
            return View(model);

        try
        {
            if (identity?.IsSuperAdmin == true)
            {
                model.Tenants = await _api.GetTenantsAsync(ct);

                if (model.SelectedTenantId.HasValue)
                {
                    model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId.Value, ct);
                }
            }
            else
            {
                model.SelectedTenantId ??= identity?.TenantId;
                model.SelectedCampusId ??= identity?.CampusId;
            }

            var departmentDetails = await _api.GetDepartmentDetailsAsync(model.SelectedTenantId, model.SelectedCampusId, ct);
            model.Departments = departmentDetails
                .Select(d => new LookupItem { Id = d.Id, Name = d.Name })
                .ToList();

            model.SelectedInstitutionType = ResolveCertificateInstitutionType(identity, departmentDetails, model.SelectedDepartmentId);
            model.PeriodFilterLabel = ResolvePeriodFilterLabel(model.SelectedInstitutionType);
            model.Semesters = await _api.GetSemestersAsync(ct);

            model.Courses = await _api.GetCoursesAsync(model.SelectedDepartmentId, model.SelectedTenantId, model.SelectedCampusId, ct);

            var matrix = await _api.GetPortalCapabilityMatrixAsync(ct);
            model.ShowUniversityCertificates = IsUniversityInstitutionType(model.SelectedInstitutionType) && (matrix?.IncludeUniversity ?? true);

            if (IsUniversityInstitutionType(model.SelectedInstitutionType) && !model.ShowUniversityCertificates)
            {
                model.Message = "Degree and transcript options are hidden because University is disabled by the current license policy.";
            }

            model.Students = await _api.GetGraduatedCertificateStudentsAsync(
                model.SelectedTenantId,
                model.SelectedCampusId,
                model.SelectedDepartmentId,
                model.SelectedCourseId,
                model.SelectedInstitutionType,
                ct);
        }
        catch (Exception ex)
        {
            model.Message = ex.Message;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateDegreeCertificate(
        Guid studentProfileId,
        Guid? tenantId,
        Guid? campusId,
        Guid? departmentId,
        Guid? courseId,
        Guid? semesterId,
        int? institutionType,
        CancellationToken ct)
    {
        if (!_api.IsConnected())
            return RedirectToAction("Connect", "Home");

        if (!IsUniversityInstitutionType(institutionType))
        {
            TempData["PortalMessage"] = "Degree generation is available only for university scope.";
            return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId });
        }

        try
        {
            await _api.GenerateDegreeCertificateAsync(studentProfileId, ct);
            TempData["PortalMessage"] = "Degree generated successfully.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateTranscriptCertificate(
        Guid studentProfileId,
        Guid? tenantId,
        Guid? campusId,
        Guid? departmentId,
        Guid? courseId,
        Guid? semesterId,
        int? institutionType,
        CancellationToken ct)
    {
        if (!_api.IsConnected())
            return RedirectToAction("Connect", "Home");

        if (!IsUniversityInstitutionType(institutionType))
        {
            TempData["PortalMessage"] = "Transcript generation is available only for university scope.";
            return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId });
        }

        try
        {
            await _api.GenerateTranscriptCertificateAsync(studentProfileId, semesterId, ct);
            TempData["PortalMessage"] = semesterId.HasValue
                ? "Transcript generated for selected class/semester."
                : "Transcript generated successfully.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadGeneratedCertificateDocument(Guid documentId, string format, CancellationToken ct)
    {
        if (!_api.IsConnected())
            return RedirectToAction("Connect", "Home");

        try
        {
            var bytes = await _api.DownloadGeneratedCertificateDocumentAsync(documentId, format, ct);
            if (bytes is null || bytes.Length == 0)
            {
                TempData["PortalMessage"] = "Certificate document not found.";
                return RedirectToAction(nameof(GenerateCertificates));
            }

            var normalizedFormat = string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase) ? "pdf" : "docx";
            var contentType = normalizedFormat == "pdf"
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            return File(bytes, contentType, $"certificate-{documentId:N}.{normalizedFormat}");
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
            return RedirectToAction(nameof(GenerateCertificates));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadStudentAdditionalCertificate(
        Guid studentProfileId,
        string? title,
        IFormFile? file,
        Guid? tenantId,
        Guid? campusId,
        Guid? departmentId,
        Guid? courseId,
        Guid? semesterId,
        int? institutionType,
        CancellationToken ct)
    {
        if (!_api.IsConnected())
            return RedirectToAction("Connect", "Home");

        if (IsUniversityInstitutionType(institutionType))
        {
            TempData["PortalMessage"] = "Additional certificate upload is available for school/college scope.";
            return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId });
        }

        if (file is null || file.Length == 0)
        {
            TempData["PortalMessage"] = "Please select a certificate file to upload.";
            return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            await _api.UploadStudentAdditionalCertificateAsync(
                studentProfileId,
                string.IsNullOrWhiteSpace(title) ? "Certificate" : title,
                stream,
                file.FileName,
                file.ContentType,
                ct);

            TempData["PortalMessage"] = "Additional certificate uploaded successfully.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadStudentAdditionalCertificate(Guid documentId, CancellationToken ct)
    {
        if (!_api.IsConnected())
            return RedirectToAction("Connect", "Home");

        try
        {
            var bytes = await _api.DownloadStudentAdditionalCertificateAsync(documentId, ct);
            if (bytes is null || bytes.Length == 0)
            {
                TempData["PortalMessage"] = "Additional certificate file not found.";
                return RedirectToAction(nameof(GenerateCertificates));
            }

            return File(bytes, "application/octet-stream", $"certificate-{documentId:N}");
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
            return RedirectToAction(nameof(GenerateCertificates));
        }
    }

    // ── Phase 20: Learning Management System (LMS) ────────────────────────────

    // Final-Touches Phase 20 Stage 20.1 — student LMS view
    [HttpGet]
    public async Task<IActionResult> CourseLms(Guid offeringId, CancellationToken ct)
    {
        ViewData["Title"] = "Course Content";
        var model = new CourseLmsPageModel { OfferingId = offeringId, IsConnected = _api.IsConnected() };
        if (!model.IsConnected) return View(model);
        try
        {
            var identity = _api.GetSessionIdentity();
            bool isStudent = identity?.IsStudent == true;
            var modules = await _api.GetLmsModulesAsync(offeringId, isStudent, ct);
            model.Modules = modules.Select(m => new LmsModuleItem
            {
                Id = m.Id, OfferingId = m.OfferingId, Title = m.Title,
                WeekNumber = m.WeekNumber, Body = m.Body,
                IsPublished = m.IsPublished, PublishedAt = m.PublishedAt,
                Videos = m.Videos.Select(v => new LmsVideoItem
                {
                    Id = v.Id, ModuleId = v.ModuleId, Title = v.Title,
                    StorageUrl = v.StorageUrl, EmbedUrl = v.EmbedUrl,
                    DurationSeconds = v.DurationSeconds
                }).ToList()
            }).ToList();
        }
        catch (Exception ex) { ViewData["Error"] = ex.Message; }
        return View(model);
    }

    // Final-Touches Phase 20 Stage 20.1 — faculty LMS management view
    [HttpGet]
    public async Task<IActionResult> LmsManage(Guid offeringId, CancellationToken ct)
    {
        ViewData["Title"] = "Manage Course Content";
        var model = new LmsManagePageModel
        {
            OfferingId     = offeringId,
            IsConnected    = _api.IsConnected(),
            SuccessMessage = TempData["SuccessMessage"]?.ToString(),
            ErrorMessage   = TempData["ErrorMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var modules = await _api.GetLmsModulesAsync(offeringId, false, ct);
            model.Modules = modules.Select(m => new LmsModuleItem
            {
                Id = m.Id, OfferingId = m.OfferingId, Title = m.Title,
                WeekNumber = m.WeekNumber, Body = m.Body,
                IsPublished = m.IsPublished, PublishedAt = m.PublishedAt,
                Videos = m.Videos.Select(v => new LmsVideoItem
                {
                    Id = v.Id, ModuleId = v.ModuleId, Title = v.Title,
                    StorageUrl = v.StorageUrl, EmbedUrl = v.EmbedUrl,
                    DurationSeconds = v.DurationSeconds
                }).ToList()
            }).ToList();
        }
        catch (Exception ex) { model.ErrorMessage = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLmsModule(Guid offeringId, string title, int weekNumber, string? body, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.CreateLmsModuleAsync(offeringId, title, weekNumber, body, ct); TempData["SuccessMessage"] = "Module created."; }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(LmsManage), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLmsModule(Guid moduleId, Guid offeringId, string title, int weekNumber, string? body, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.UpdateLmsModuleAsync(moduleId, title, weekNumber, body, ct); TempData["SuccessMessage"] = "Module updated."; }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(LmsManage), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishLmsModule(Guid moduleId, Guid offeringId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.PublishLmsModuleAsync(moduleId, ct); TempData["SuccessMessage"] = "Module published."; }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(LmsManage), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UnpublishLmsModule(Guid moduleId, Guid offeringId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.UnpublishLmsModuleAsync(moduleId, ct); TempData["SuccessMessage"] = "Module unpublished."; }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(LmsManage), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLmsModule(Guid moduleId, Guid offeringId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.DeleteLmsModuleAsync(moduleId, ct); TempData["SuccessMessage"] = "Module deleted."; }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(LmsManage), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLmsVideo(Guid moduleId, Guid offeringId, string title, string? storageUrl, string? embedUrl, int? durationSeconds, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.AddLmsVideoAsync(moduleId, title, storageUrl, embedUrl, durationSeconds, ct); TempData["SuccessMessage"] = "Video added."; }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(LmsManage), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLmsVideo(Guid videoId, Guid offeringId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.DeleteLmsVideoAsync(videoId, ct); TempData["SuccessMessage"] = "Video deleted."; }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(LmsManage), new { offeringId });
    }

    [HttpGet]
    public async Task<IActionResult> CourseMaterial(
        Guid? departmentId,
        Guid? academicProgramId,
        Guid? semesterId,
        Guid? courseId,
        bool activeOnly = true,
        CancellationToken ct = default)
    {
        ViewData["Title"] = "Course Materials";

        var session = _api.GetSessionIdentity();
        var canManage = session?.IsAdmin == true || session?.IsSuperAdmin == true || session?.IsFaculty == true;
        if (!canManage)
        {
            return RedirectToAction(nameof(CourseMaterialStudent), new { departmentId, academicProgramId, semesterId, courseId });
        }

        var model = new CourseMaterialManagePageModel
        {
            IsConnected = _api.IsConnected(),
            SelectedDepartmentId = departmentId,
            SelectedAcademicProgramId = academicProgramId,
            SelectedSemesterId = semesterId,
            SelectedCourseId = courseId,
            ActiveOnly = activeOnly,
            CanManage = canManage
        };

        if (!model.IsConnected) return View(model);

        try
        {
            var tenantId = session?.TenantId;
            var campusId = session?.CampusId;

            model.Departments = await _api.GetDepartmentsAsync(tenantId, campusId, ct);
            model.Semesters = await _api.GetSemestersAsync(ct);

            model.Programs = await _api.GetProgramsAsync(departmentId, tenantId, campusId, ct);
            model.Courses = await _api.GetCoursesAsync(departmentId, tenantId, campusId, ct);

            if (academicProgramId.HasValue && !model.Programs.Any(p => p.Id == academicProgramId.Value))
                academicProgramId = null;

            if (courseId.HasValue && !model.Courses.Any(c => c.Id == courseId.Value))
                courseId = null;

            model.SelectedAcademicProgramId = academicProgramId;
            model.SelectedCourseId = courseId;

            var materials = await _api.GetCourseMaterialsAsync(
                departmentId,
                academicProgramId,
                semesterId,
                courseId,
                tenantId,
                campusId,
                activeOnly,
                ct);

            model.Materials = materials.Select(m => new CourseMaterialItem
            {
                Id = m.Id,
                DepartmentId = m.DepartmentId,
                AcademicProgramId = m.AcademicProgramId,
                SemesterId = m.SemesterId,
                CourseId = m.CourseId,
                MaterialType = m.MaterialType,
                Title = m.Title,
                Description = m.Description,
                ExternalUrl = m.ExternalUrl,
                BlobPath = m.BlobPath,
                FileName = m.FileName,
                FileSizeBytes = m.FileSizeBytes,
                IsActive = m.IsActive,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            model.Message = ex.Message;
        }

        model.Message ??= TempData["PortalMessage"]?.ToString();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CourseMaterialStudent(
        Guid? departmentId,
        Guid? academicProgramId,
        Guid? semesterId,
        Guid? courseId,
        CancellationToken ct = default)
    {
        ViewData["Title"] = "Course Materials";
        var model = new CourseMaterialStudentPageModel
        {
            IsConnected = _api.IsConnected(),
            SelectedDepartmentId = departmentId,
            SelectedAcademicProgramId = academicProgramId,
            SelectedSemesterId = semesterId,
            SelectedCourseId = courseId
        };

        if (!model.IsConnected) return View(model);

        try
        {
            var session = _api.GetSessionIdentity();
            var tenantId = session?.TenantId;
            var campusId = session?.CampusId;

            model.Departments = await _api.GetDepartmentsAsync(tenantId, campusId, ct);
            model.Semesters = await _api.GetSemestersAsync(ct);

            model.Programs = await _api.GetProgramsAsync(departmentId, tenantId, campusId, ct);
            model.Courses = await _api.GetCoursesAsync(departmentId, tenantId, campusId, ct);

            if (academicProgramId.HasValue && !model.Programs.Any(p => p.Id == academicProgramId.Value))
                academicProgramId = null;

            if (courseId.HasValue && !model.Courses.Any(c => c.Id == courseId.Value))
                courseId = null;

            model.SelectedAcademicProgramId = academicProgramId;
            model.SelectedCourseId = courseId;

            var materials = await _api.GetCourseMaterialsAsync(
                departmentId,
                academicProgramId,
                semesterId,
                courseId,
                tenantId,
                campusId,
                true,
                ct);

            model.Materials = materials.Select(m => new CourseMaterialItem
            {
                Id = m.Id,
                DepartmentId = m.DepartmentId,
                AcademicProgramId = m.AcademicProgramId,
                SemesterId = m.SemesterId,
                CourseId = m.CourseId,
                MaterialType = m.MaterialType,
                Title = m.Title,
                Description = m.Description,
                ExternalUrl = m.ExternalUrl,
                BlobPath = m.BlobPath,
                FileName = m.FileName,
                FileSizeBytes = m.FileSizeBytes,
                IsActive = m.IsActive,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            model.Message = ex.Message;
        }

        model.Message ??= TempData["PortalMessage"]?.ToString();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCourseMaterial(
        Guid departmentId,
        Guid academicProgramId,
        Guid semesterId,
        Guid courseId,
        string materialType,
        string title,
        string? description,
        string? externalUrl,
        string? blobPath,
        IFormFile? materialFile,
        string? fileName,
        long? fileSizeBytes,
        bool isActive,
        Guid? selectedDepartmentId,
        Guid? selectedAcademicProgramId,
        Guid? selectedSemesterId,
        Guid? selectedCourseId,
        bool activeOnly,
        CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                if (materialFile is { Length: > 0 })
                {
                    await using var uploadStream = materialFile.OpenReadStream();
                    var uploaded = await _api.UploadCourseMaterialFileAsync(uploadStream, materialFile.FileName, ct);
                    if (uploaded is not null)
                    {
                        blobPath = uploaded.BlobPath;
                        fileName = uploaded.FileName;
                        fileSizeBytes = uploaded.FileSizeBytes;
                    }
                }

                await _api.CreateCourseMaterialAsync(
                    departmentId,
                    academicProgramId,
                    semesterId,
                    courseId,
                    materialType,
                    title,
                    description,
                    externalUrl,
                    blobPath,
                    fileName,
                    fileSizeBytes,
                    isActive,
                    ct);
                TempData["PortalMessage"] = "Course material created.";
            }
            catch (Exception ex)
            {
                TempData["PortalMessage"] = $"Error: {ex.Message}";
            }
        }

        return RedirectToAction(nameof(CourseMaterial), new
        {
            departmentId = selectedDepartmentId,
            academicProgramId = selectedAcademicProgramId,
            semesterId = selectedSemesterId,
            courseId = selectedCourseId,
            activeOnly
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCourseMaterial(
        Guid id,
        string materialType,
        string title,
        string? description,
        string? externalUrl,
        string? blobPath,
        IFormFile? materialFile,
        string? fileName,
        long? fileSizeBytes,
        bool isActive,
        Guid? selectedDepartmentId,
        Guid? selectedAcademicProgramId,
        Guid? selectedSemesterId,
        Guid? selectedCourseId,
        bool activeOnly,
        CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                if (materialFile is { Length: > 0 })
                {
                    await using var uploadStream = materialFile.OpenReadStream();
                    var uploaded = await _api.UploadCourseMaterialFileAsync(uploadStream, materialFile.FileName, ct);
                    if (uploaded is not null)
                    {
                        blobPath = uploaded.BlobPath;
                        fileName = uploaded.FileName;
                        fileSizeBytes = uploaded.FileSizeBytes;
                    }
                }

                await _api.UpdateCourseMaterialAsync(
                    id,
                    materialType,
                    title,
                    description,
                    externalUrl,
                    blobPath,
                    fileName,
                    fileSizeBytes,
                    isActive,
                    ct);
                TempData["PortalMessage"] = "Course material updated.";
            }
            catch (Exception ex)
            {
                TempData["PortalMessage"] = $"Error: {ex.Message}";
            }
        }

        return RedirectToAction(nameof(CourseMaterial), new
        {
            departmentId = selectedDepartmentId,
            academicProgramId = selectedAcademicProgramId,
            semesterId = selectedSemesterId,
            courseId = selectedCourseId,
            activeOnly
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCourseMaterialActive(
        Guid id,
        bool isActive,
        Guid? selectedDepartmentId,
        Guid? selectedAcademicProgramId,
        Guid? selectedSemesterId,
        Guid? selectedCourseId,
        bool activeOnly,
        CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                await _api.SetCourseMaterialActiveAsync(id, isActive, ct);
                TempData["PortalMessage"] = isActive ? "Course material activated." : "Course material deactivated.";
            }
            catch (Exception ex)
            {
                TempData["PortalMessage"] = $"Error: {ex.Message}";
            }
        }

        return RedirectToAction(nameof(CourseMaterial), new
        {
            departmentId = selectedDepartmentId,
            academicProgramId = selectedAcademicProgramId,
            semesterId = selectedSemesterId,
            courseId = selectedCourseId,
            activeOnly
        });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadCourseMaterialFile(Guid id, string? returnTo, CancellationToken ct)
    {
        if (!_api.IsConnected())
            return RedirectToAction(nameof(Dashboard));

        var redirectAction = string.Equals(returnTo, "student", StringComparison.OrdinalIgnoreCase)
            ? nameof(CourseMaterialStudent)
            : nameof(CourseMaterial);

        try
        {
            var file = await _api.DownloadCourseMaterialFileAsync(id, ct);
            if (file is null || file.Content.Length == 0)
            {
                TempData["PortalMessage"] = "Error: Material file is not available.";
                return RedirectToAction(redirectAction);
            }

            return File(file.Content, file.ContentType, file.FileName);
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = $"Error: {ex.Message}";
            return RedirectToAction(redirectAction);
        }
    }

    // Final-Touches Phase 20 Stage 20.3 — discussion forum
    [HttpGet]
    public async Task<IActionResult> Discussion(Guid offeringId, CancellationToken ct)
    {
        ViewData["Title"] = "Discussion";
        var session = _api.GetSessionIdentity();
        var canModerate = session?.IsFaculty == true || session?.IsAdmin == true || session?.IsSuperAdmin == true;
        var callerIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid? currentUserId = Guid.TryParse(callerIdStr, out var parsedUserId) ? parsedUserId : null;

        var model = new DiscussionPageModel
        {
            OfferingId     = offeringId,
            CurrentUserId  = currentUserId,
            CanModerate    = canModerate,
            IsConnected    = _api.IsConnected(),
            SuccessMessage = TempData["SuccessMessage"]?.ToString(),
            ErrorMessage   = TempData["ErrorMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var threads = await _api.GetDiscussionThreadsAsync(offeringId, ct);
            model.Threads = threads.Select(t => new DiscussionThreadItem
            {
                Id = t.Id, OfferingId = t.OfferingId, AuthorId = t.AuthorId, Title = t.Title,
                AuthorName = t.AuthorName, IsPinned = t.IsPinned,
                IsClosed = t.IsClosed, IsSolved = t.IsSolved,
                ResolvedByName = t.ResolvedByName, ResolvedAt = t.ResolvedAt,
                TicketNumber = t.TicketNumber,
                ReplyCount = t.ReplyCount, CreatedAt = t.CreatedAt
            }).ToList();
        }
        catch (Exception ex) { model.ErrorMessage = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDiscussionThread(Guid offeringId, string title, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                await _api.CreateDiscussionThreadAsync(offeringId, Guid.Empty, title, ct);
                TempData["SuccessMessage"] = "Thread created.";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(Discussion), new { offeringId });
    }

    [HttpGet]
    public async Task<IActionResult> DiscussionThreadDetail(Guid threadId, Guid offeringId, CancellationToken ct)
    {
        ViewData["Title"] = "Thread";
        var session = _api.GetSessionIdentity();
        var canModerate = session?.IsFaculty == true || session?.IsAdmin == true || session?.IsSuperAdmin == true;
        var callerIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid? currentUserId = Guid.TryParse(callerIdStr, out var parsedUserId) ? parsedUserId : null;

        var model = new DiscussionDetailPageModel
        {
            OfferingId  = offeringId,
            CurrentUserId = currentUserId,
            CanModerate = canModerate,
            IsConnected = _api.IsConnected(),
            SuccessMessage = TempData["SuccessMessage"]?.ToString(),
            ErrorMessage   = TempData["ErrorMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var t = await _api.GetDiscussionThreadAsync(threadId, ct);
            if (t is not null)
            {
                model.Thread = new DiscussionThreadItem
                {
                    Id = t.Id,
                    OfferingId = t.OfferingId,
                    AuthorId = t.AuthorId,
                    Title = t.Title,
                    AuthorName = t.AuthorName,
                    IsPinned = t.IsPinned,
                    IsClosed = t.IsClosed,
                    IsSolved = t.IsSolved,
                    ResolvedByName = t.ResolvedByName,
                    ResolvedAt = t.ResolvedAt,
                    TicketNumber = t.TicketNumber,
                    ReplyCount = t.ReplyCount,
                    CreatedAt = t.CreatedAt,
                    Replies = t.Replies.Select(r => new DiscussionReplyItem
                    {
                        Id = r.Id,
                        ThreadId = r.ThreadId,
                        AuthorId = r.AuthorId,
                        AuthorName = r.AuthorName,
                        Body = r.Body,
                        CreatedAt = r.CreatedAt
                    }).ToList()
                };
            }

        }
        catch (Exception ex) { model.ErrorMessage = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDiscussionReply(Guid threadId, Guid offeringId, string body, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                await _api.AddDiscussionReplyAsync(threadId, Guid.Empty, body, ct);
                TempData["SuccessMessage"] = "Reply posted.";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(DiscussionThreadDetail), new { threadId, offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkDiscussionSolved(Guid threadId, Guid offeringId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            var session = _api.GetSessionIdentity();
            var canModerate = session?.IsFaculty == true || session?.IsAdmin == true || session?.IsSuperAdmin == true;
            if (!canModerate)
            {
                TempData["ErrorMessage"] = "Only Faculty/Admin/SuperAdmin can resolve discussions.";
                return RedirectToAction(nameof(DiscussionThreadDetail), new { threadId, offeringId });
            }

            try { await _api.MarkDiscussionSolvedAsync(threadId, ct); TempData["SuccessMessage"] = "Discussion marked as resolved."; }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }

        return RedirectToAction(nameof(DiscussionThreadDetail), new { threadId, offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkDiscussionUnresolved(Guid threadId, Guid offeringId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            var session = _api.GetSessionIdentity();
            var canModerate = session?.IsFaculty == true || session?.IsAdmin == true || session?.IsSuperAdmin == true;
            if (!canModerate)
            {
                TempData["ErrorMessage"] = "Only Faculty/Admin/SuperAdmin can unresolve discussions.";
                return RedirectToAction(nameof(DiscussionThreadDetail), new { threadId, offeringId });
            }

            try { await _api.MarkDiscussionUnresolvedAsync(threadId, ct); TempData["SuccessMessage"] = "Discussion marked as unresolved."; }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }

        return RedirectToAction(nameof(DiscussionThreadDetail), new { threadId, offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDiscussionThread(Guid threadId, Guid offeringId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.DeleteDiscussionThreadAsync(threadId, ct); TempData["SuccessMessage"] = "Thread deleted."; }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(Discussion), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDiscussionReply(Guid replyId, Guid threadId, Guid offeringId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.DeleteDiscussionReplyAsync(replyId, ct); TempData["SuccessMessage"] = "Reply deleted."; }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(DiscussionThreadDetail), new { threadId, offeringId });
    }

    // Final-Touches Phase 20 Stage 20.4 — announcements
    [HttpGet]
    public async Task<IActionResult> Announcements(Guid offeringId, bool includeInactive = true, CancellationToken ct = default)
    {
        ViewData["Title"] = "Announcements";
        var session = _api.GetSessionIdentity();
        var canManage = session?.IsFaculty == true || session?.IsAdmin == true || session?.IsSuperAdmin == true;
        var tenantId = session?.TenantId;
        var campusId = session?.CampusId;

        var model = new AnnouncementsPageModel
        {
            OfferingId     = offeringId,
            IncludeInactive = includeInactive,
            CanManage      = canManage,
            IsConnected    = _api.IsConnected(),
            SuccessMessage = TempData["SuccessMessage"]?.ToString(),
            ErrorMessage   = TempData["ErrorMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var items = await _api.GetAnnouncementsAsync(offeringId, includeInactive, tenantId, campusId, ct);
            model.Announcements = items.Select(a => new AnnouncementItem
            {
                Id = a.Id, OfferingId = a.OfferingId, Title = a.Title,
                Body = a.Body, AuthorName = a.AuthorName, IsActive = a.IsActive, PostedAt = a.PostedAt
            }).ToList();
        }
        catch (Exception ex) { model.ErrorMessage = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAnnouncement(Guid offeringId, string title, string body, bool includeInactive, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
                    throw new InvalidOperationException("Title and body are required.");

                var session = _api.GetSessionIdentity();
                await _api.CreateAnnouncementAsync(offeringId, Guid.Empty, title, body, session?.TenantId, session?.CampusId, ct);
                TempData["SuccessMessage"] = "Announcement posted.";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(Announcements), new { offeringId, includeInactive });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAnnouncementActive(Guid announcementId, Guid offeringId, bool isActive, bool includeInactive, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var session = _api.GetSessionIdentity();
                await _api.SetAnnouncementActiveAsync(announcementId, isActive, session?.TenantId, session?.CampusId, ct);
                TempData["SuccessMessage"] = isActive ? "Announcement activated." : "Announcement deactivated.";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(Announcements), new { offeringId, includeInactive });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAnnouncement(Guid announcementId, Guid offeringId, bool includeInactive, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var session = _api.GetSessionIdentity();
                await _api.DeleteAnnouncementAsync(announcementId, session?.TenantId, session?.CampusId, ct);
                TempData["SuccessMessage"] = "Announcement deleted.";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(Announcements), new { offeringId, includeInactive });
    }

    // ── Phase 21 Stage 21.1/21.2 — Study Planner ─────────────────────────────

    [HttpGet]
    public async Task<IActionResult> StudyPlan(Guid studentProfileId, CancellationToken ct)
    {
        ViewData["Title"] = "Study Plans";
        var model = new StudyPlanPageModel { StudentProfileId = studentProfileId, IsConnected = _api.IsConnected() };
        if (TempData["SuccessMessage"] is string s) model.SuccessMessage = s;
        if (TempData["ErrorMessage"]   is string e) model.ErrorMessage   = e;
        if (_api.IsConnected())
        {
            try
            {
                var plans = await _api.GetStudyPlansAsync(studentProfileId, ct);
                model.Plans = plans.Select(p => MapStudyPlanItem(p)).ToList();
            }
            catch (Exception ex) { model.ErrorMessage = ex.Message; }
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> StudyPlanDetail(Guid planId, CancellationToken ct)
    {
        ViewData["Title"] = "Study Plan Detail";
        var model = new StudyPlanDetailPageModel { IsConnected = _api.IsConnected() };
        if (TempData["SuccessMessage"] is string s) model.SuccessMessage = s;
        if (TempData["ErrorMessage"]   is string e) model.ErrorMessage   = e;
        if (_api.IsConnected())
        {
            try
            {
                var plan = await _api.GetStudyPlanAsync(planId, ct);
                if (plan is null) return NotFound();
                model.Plan = MapStudyPlanItem(plan);
            }
            catch (Exception ex) { model.ErrorMessage = ex.Message; }
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStudyPlan(Guid studentProfileId, string plannedSemesterName, string? notes, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                await _api.CreateStudyPlanAsync(studentProfileId, plannedSemesterName, notes, ct);
                TempData["SuccessMessage"] = "Study plan created.";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(StudyPlan), new { studentProfileId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddStudyPlanCourse(Guid planId, Guid courseId, Guid studentProfileId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                await _api.AddStudyPlanCourseAsync(planId, courseId, ct);
                TempData["SuccessMessage"] = "Course added to plan.";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(StudyPlanDetail), new { planId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveStudyPlanCourse(Guid planId, Guid courseId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                await _api.RemoveStudyPlanCourseAsync(planId, courseId, ct);
                TempData["SuccessMessage"] = "Course removed from plan.";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(StudyPlanDetail), new { planId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStudyPlan(Guid planId, Guid studentProfileId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                await _api.DeleteStudyPlanAsync(planId, ct);
                TempData["SuccessMessage"] = "Study plan deleted.";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(StudyPlan), new { studentProfileId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AdvisePlan(Guid planId, bool isEndorsed, string? advisorNotes, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                await _api.AdvisePlanAsync(planId, isEndorsed, advisorNotes, ct);
                TempData["SuccessMessage"] = isEndorsed ? "Plan endorsed." : "Plan rejected.";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(StudyPlanDetail), new { planId });
    }

    [HttpGet]
    public async Task<IActionResult> StudyPlanRecommendations(Guid studentProfileId, string plannedSemesterName = "Next Semester", CancellationToken ct = default)
    {
        ViewData["Title"] = "Course Recommendations";
        var model = new RecommendationsPageModel
        {
            StudentProfileId    = studentProfileId,
            PlannedSemesterName = plannedSemesterName,
            IsConnected         = _api.IsConnected()
        };
        if (_api.IsConnected())
        {
            try
            {
                var rec = await _api.GetStudyPlanRecommendationsAsync(studentProfileId, plannedSemesterName, ct);
                if (rec is not null)
                {
                    model.MaxCreditLoad            = rec.MaxCreditLoad;
                    model.RecommendedTotalCredits   = rec.RecommendedTotalCredits;
                    model.Recommendations = rec.Recommendations.Select(r => new RecommendationItem
                    {
                        CourseId    = r.CourseId,
                        CourseCode  = r.CourseCode,
                        CourseTitle = r.CourseTitle,
                        CreditHours = r.CreditHours,
                        CourseType  = r.CourseType,
                        Reason      = r.Reason
                    }).ToList();
                }
            }
            catch (Exception ex) { model.ErrorMessage = ex.Message; }
        }
        return View(model);
    }

    // ── Phase 21 helper ───────────────────────────────────────────────────────

    private static StudyPlanItem MapStudyPlanItem(StudyPlanApiModel p) => new()
    {
        Id                  = p.Id,
        StudentProfileId    = p.StudentProfileId,
        PlannedSemesterName = p.PlannedSemesterName,
        Notes               = p.Notes,
        AdvisorStatus       = p.AdvisorStatus,
        AdvisorNotes        = p.AdvisorNotes,
        ReviewedByUserId    = p.ReviewedByUserId,
        TotalCreditHours    = p.TotalCreditHours,
        Courses             = p.Courses.Select(c => new StudyPlanCourseItem
        {
            CourseId    = c.CourseId,
            CourseCode  = c.CourseCode,
            CourseTitle = c.CourseTitle,
            CreditHours = c.CreditHours,
            CourseType  = c.CourseType
        }).ToList(),
        CreatedAt = p.CreatedAt
    };

    // ── Phase 22: External Integrations ─────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> LibraryConfig(CancellationToken ct)
    {
        var model = new LibraryConfigPageModel { IsConnected = _api.IsConnected() };
        if (!model.IsConnected) return View(model);
        try
        {
            var config = await _api.GetLibraryConfigAsync(ct);
            model.CatalogueUrl = config?.CatalogueUrl;
            model.ApiToken     = config?.ApiToken;
            model.LoanApiUrl   = config?.LoanApiUrl;
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> LibraryConfig(LibraryConfigPageModel form, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(LibraryConfig));
        try
        {
            await _api.SaveLibraryConfigAsync(new LibraryConfigApiModel
            {
                CatalogueUrl = form.CatalogueUrl,
                ApiToken     = form.ApiToken,
                LoanApiUrl   = form.LoanApiUrl
            }, ct);
            TempData["Success"] = "Library configuration saved.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(LibraryConfig));
    }

    [HttpGet]
    public async Task<IActionResult> AccreditationTemplates(CancellationToken ct)
    {
        await Task.CompletedTask;
        var model = new AccreditationTemplatesPageModel { IsConnected = true };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAccreditationTemplate(CancellationToken ct)
    {
        await Task.CompletedTask;
        try
        {
            var content = string.Join(Environment.NewLine, new[]
            {
                "Name: Simple Accreditation Template",
                "Description: Basic template for upload and download",
                "Format: CSV",
                "FieldMappings: enrollment,results,faculty"
            }) + Environment.NewLine;

            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            return File(bytes, "text/plain", "accreditation-template-sample.txt");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(AccreditationTemplates));
        }
    }

    [HttpPost]
    [RequestFormLimits(MultipartBodyLengthLimit = 1048576)]
    public async Task<IActionResult> UploadAccreditationTemplate(IFormFile? templateFile, CancellationToken ct)
    {
        await Task.CompletedTask;

        if (templateFile == null || templateFile.Length == 0)
        {
            TempData["Error"] = "Please choose a template file to upload.";
            return RedirectToAction(nameof(AccreditationTemplates));
        }

        var extension = Path.GetExtension(templateFile.FileName);
        if (!string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only .txt or .csv template files are supported.";
            return RedirectToAction(nameof(AccreditationTemplates));
        }

        if (templateFile.Length > 1048576)
        {
            TempData["Error"] = "Template file is too large. Maximum size is 1 MB.";
            return RedirectToAction(nameof(AccreditationTemplates));
        }

        try
        {
            var uploadFolder = Path.Combine(Path.GetTempPath(), "Tabsan.EduSphere", "AccreditationTemplates");
            Directory.CreateDirectory(uploadFolder);

            var safeName = Path.GetFileNameWithoutExtension(templateFile.FileName);
            if (string.IsNullOrWhiteSpace(safeName))
                safeName = "accreditation-template";

            var safeExt = string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase) ? ".csv" : ".txt";
            var savedFileName = $"{safeName}-{DateTime.UtcNow:yyyyMMddHHmmss}{safeExt}";
            var savedPath = Path.Combine(uploadFolder, savedFileName);

            await using var stream = templateFile.OpenReadStream();
            await using var fileStream = new FileStream(savedPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fileStream, ct);

            TempData["Success"] = $"Template uploaded successfully: {savedFileName}";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(AccreditationTemplates));
    }

    // ── Phase 23 — Institution Policy ─────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> InstitutionPolicy(CancellationToken ct)
    {
        var model = new InstitutionPolicyPageModel { IsConnected = _api.IsConnected() };
        if (!model.IsConnected) return View(model);
        try
        {
            var policy = await _api.GetInstitutionPolicyAsync(ct);
            model.IncludeSchool     = policy?.IncludeSchool     ?? false;
            model.IncludeCollege    = policy?.IncludeCollege    ?? false;
            model.IncludeUniversity = policy?.IncludeUniversity ?? true;
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> InstitutionPolicy(
        InstitutionPolicyPageModel form, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(InstitutionPolicy));
        try
        {
            await _api.SaveInstitutionPolicyAsync(new InstitutionPolicyApiModel
            {
                IncludeSchool     = form.IncludeSchool,
                IncludeCollege    = form.IncludeCollege,
                IncludeUniversity = form.IncludeUniversity
            }, ct);
            TempData["Success"] = "Institution policy saved.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(InstitutionPolicy));
    }
}

