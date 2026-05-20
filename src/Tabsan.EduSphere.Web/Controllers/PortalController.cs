using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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

    private async Task<List<LookupItem>> GetOfferingFilterOptionsAsync(SessionIdentity? sessionIdentity, CancellationToken ct)
    {
        if (sessionIdentity?.IsAdmin == true || sessionIdentity?.IsSuperAdmin == true)
        {
            var offerings = await _api.GetCourseOfferingsAsync(null, ct);
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
    public async Task<IActionResult> TimetableAdmin(Guid? timetableId, CancellationToken ct)
    {
        ViewData["Title"] = "Manage Timetables";
        var vm = new TimetableAdminPageModel
        {
            IsConnected = _api.IsConnected(),
            Connection = _api.GetConnection(),
            Message = TempData["PortalMessage"]?.ToString()
        };

        if (!vm.IsConnected)
        {
            vm.Message ??= "Configure API connection on Dashboard first.";
            return View(vm);
        }

        try
        {
            vm.Departments = await _api.GetDepartmentsAsync(ct);

            var selectedDepartment = vm.Connection.DefaultDepartmentId ?? vm.Departments.FirstOrDefault()?.Id;
            vm.CreateForm.DepartmentId = selectedDepartment ?? Guid.Empty;

            vm.Programs = await _api.GetProgramsAsync(selectedDepartment, ct);
            vm.Semesters = await _api.GetSemestersAsync(ct);
            vm.Courses = await _api.GetCoursesAsync(selectedDepartment, ct);
            vm.Faculty = await _api.GetFacultyAsync(ct);
            vm.Buildings = await _api.GetBuildingsAsync(ct);
            vm.Rooms = await _api.GetRoomsAsync(ct);

            if (selectedDepartment.HasValue)
                vm.Timetables = await _api.GetTimetablesByDepartmentAsync(selectedDepartment.Value, ct);

            var activeTimetableId = timetableId ?? vm.Timetables.FirstOrDefault()?.Id;
            if (activeTimetableId.HasValue)
            {
                vm.SelectedTimetable = await _api.GetTimetableByIdAsync(activeTimetableId.Value, ct);
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
    public async Task<IActionResult> CreateTimetable(CreateTimetableForm form, CancellationToken ct)
    {
        try
        {
            var id = await _api.CreateTimetableAsync(form, ct);
            TempData["PortalMessage"] = "Timetable created successfully.";
            return RedirectToAction(nameof(TimetableAdmin), new { timetableId = id });
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
            return RedirectToAction(nameof(TimetableAdmin));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTimetableEntry(AddTimetableEntryForm form, CancellationToken ct)
    {
        try
        {
            await _api.AddTimetableEntryAsync(form, ct);
            TempData["PortalMessage"] = "Timetable entry added.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(TimetableAdmin), new { timetableId = form.TimetableId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishTimetable(Guid timetableId, CancellationToken ct)
    {
        try
        {
            await _api.PublishTimetableAsync(timetableId, ct);
            TempData["PortalMessage"] = "Timetable published.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(TimetableAdmin), new { timetableId });
    }

    // ── Timetable Student ───────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> TimetableStudent(Guid? departmentId, Guid? timetableId, CancellationToken ct)
    {
        ViewData["Title"] = "Student Timetable";
        var vm = new TimetableStudentPageModel
        {
            IsConnected = _api.IsConnected(),
            Message = TempData["PortalMessage"]?.ToString()
        };

        if (!vm.IsConnected)
        {
            vm.Message ??= "Configure API connection on Dashboard first.";
            return View(vm);
        }

        try
        {
            vm.DepartmentId = departmentId ?? await GetEffectiveStudentDepartmentIdAsync(ct);
            if (!vm.DepartmentId.HasValue)
            {
                vm.Message = "Department is required. Set default department in Dashboard connection.";
                return View(vm);
            }

            vm.Timetables = await _api.GetTimetablesByDepartmentAsync(vm.DepartmentId.Value, ct);
            var activeTimetableId = timetableId ?? vm.Timetables.FirstOrDefault()?.Id;
            if (activeTimetableId.HasValue)
                vm.SelectedTimetable = await _api.GetTimetableByIdAsync(activeTimetableId.Value, ct);
        }
        catch (Exception ex)
        {
            vm.Message = ex.Message;
        }

        return View(vm);
    }

    // ── Timetable Teacher ───────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> TimetableTeacher(CancellationToken ct)
    {
        ViewData["Title"] = "Teacher Timetable";
        var vm = new TimetableTeacherPageModel
        {
            IsConnected = _api.IsConnected(),
            Message = TempData["PortalMessage"]?.ToString()
        };

        if (!vm.IsConnected)
        {
            vm.Message ??= "Configure API connection on Dashboard first.";
            return View(vm);
        }

        try
        {
            vm.Entries = await _api.GetTeacherEntriesAsync(ct);
        }
        catch (Exception ex)
        {
            vm.Message = ex.Message;
        }

        return View(vm);
    }

    // ── Buildings ───────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Buildings(Guid? selectedId, CancellationToken ct)
    {
        ViewData["Title"] = "Buildings";
        var vm = new BuildingsPageModel
        {
            IsConnected = _api.IsConnected(),
            Message = TempData["PortalMessage"]?.ToString()
        };

        if (!vm.IsConnected)
        {
            vm.Message ??= "Configure API connection on Dashboard first.";
            return View(vm);
        }

        try
        {
            vm.Buildings = await _api.GetAllBuildingsAsync(activeOnly: false, ct);
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
    public async Task<IActionResult> CreateBuilding(BuildingFormModel form, CancellationToken ct)
    {
        try
        {
            var created = await _api.CreateBuildingAsync(form, ct);
            TempData["PortalMessage"] = $"Building '{created.Name}' created.";
            return RedirectToAction(nameof(Buildings), new { selectedId = created.Id });
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
            return RedirectToAction(nameof(Buildings));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBuilding(Guid id, BuildingFormModel form, CancellationToken ct)
    {
        try
        {
            var updated = await _api.UpdateBuildingAsync(id, form, ct);
            TempData["PortalMessage"] = $"Building '{updated.Name}' updated.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Buildings), new { selectedId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetBuildingActive(Guid id, bool activate, CancellationToken ct)
    {
        try
        {
            if (activate)
                await _api.ActivateBuildingAsync(id, ct);
            else
                await _api.DeactivateBuildingAsync(id, ct);

            TempData["PortalMessage"] = activate ? "Building activated." : "Building deactivated.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Buildings), new { selectedId = id });
    }

    // ── Rooms ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Rooms(Guid? buildingId, Guid? selectedId, CancellationToken ct)
    {
        ViewData["Title"] = "Rooms";
        var vm = new RoomsPageModel
        {
            IsConnected = _api.IsConnected(),
            Message = TempData["PortalMessage"]?.ToString(),
            SelectedBuildingId = buildingId
        };

        if (!vm.IsConnected)
        {
            vm.Message ??= "Configure API connection on Dashboard first.";
            return View(vm);
        }

        try
        {
            vm.Buildings = await _api.GetAllBuildingsAsync(activeOnly: false, ct);

            var activeBuildingId = buildingId ?? vm.Buildings.FirstOrDefault()?.Id;
            vm.SelectedBuildingId = activeBuildingId;

            if (activeBuildingId.HasValue)
            {
                vm.Rooms = await _api.GetRoomsForBuildingAsync(activeBuildingId.Value, activeOnly: false, ct);
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
    public async Task<IActionResult> CreateRoom(RoomFormModel form, CancellationToken ct)
    {
        try
        {
            var created = await _api.CreateRoomAsync(form, ct);
            TempData["PortalMessage"] = $"Room '{created.Number}' created.";
            return RedirectToAction(nameof(Rooms), new { buildingId = created.BuildingId, selectedId = created.Id });
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
            return RedirectToAction(nameof(Rooms), new { buildingId = form.BuildingId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRoom(Guid id, Guid buildingId, RoomFormModel form, CancellationToken ct)
    {
        try
        {
            var updated = await _api.UpdateRoomAsync(id, form, ct);
            TempData["PortalMessage"] = $"Room '{updated.Number}' updated.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Rooms), new { buildingId, selectedId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRoomActive(Guid id, Guid buildingId, bool activate, CancellationToken ct)
    {
        try
        {
            if (activate)
                await _api.ActivateRoomAsync(id, ct);
            else
                await _api.DeactivateRoomAsync(id, ct);

            TempData["PortalMessage"] = activate ? "Room activated." : "Room deactivated.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Rooms), new { buildingId, selectedId = id });
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

    public async Task<IActionResult> ResultCalculation(CancellationToken ct)
    {
        var model = new ResultCalculationSettingsPageModel { IsConnected = _api.IsConnected() };
        if (model.IsConnected)
        {
            try
            {
                model = await _api.GetResultCalculationSettingsAsync(ct);
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

                await _api.SaveResultCalculationSettingsAsync(model, ct);
                TempData["Message"] = "Result calculation settings updated.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error: {ex.Message}";
            }
        }

        return RedirectToAction(nameof(ResultCalculation));
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
    public IActionResult UserImport()
    {
        ViewData["Title"] = "User Import";
        var model = new UserImportPageModel { IsConnected = _api.IsConnected() };
        if (!model.IsConnected) return View(model);

        var identity = _api.GetSessionIdentity();
        var canImport = identity?.IsAdmin == true || identity?.IsSuperAdmin == true;
        if (!canImport)
            model.Message = "Only Admin or SuperAdmin can import users.";

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportUsersCsv(IFormFile? csvFile, CancellationToken ct)
    {
        ViewData["Title"] = "User Import";
        var model = new UserImportPageModel { IsConnected = _api.IsConnected() };
        if (!model.IsConnected)
            return View("UserImport", model);

        var identity = _api.GetSessionIdentity();
        var canImport = identity?.IsAdmin == true || identity?.IsSuperAdmin == true;
        if (!canImport)
        {
            model.Message = "Only Admin or SuperAdmin can import users.";
            return View("UserImport", model);
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
            model.Result = await _api.ImportUsersCsvAsync(stream, csvFile.FileName, ct);
            model.Message = "CSV import completed.";
        }
        catch (Exception ex)
        {
            model.Message = $"Import failed: {ex.Message}";
        }

        return View("UserImport", model);
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

        var templateRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "..", "User Import Sheets"));
        var candidatePath = Path.GetFullPath(Path.Combine(templateRoot, fileName));
        if (!candidatePath.StartsWith(templateRoot, StringComparison.OrdinalIgnoreCase))
            return NotFound();

        if (!System.IO.File.Exists(candidatePath))
            return NotFound();

        return PhysicalFile(candidatePath, "text/csv", fileName);
    }

    // ── Departments ────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Departments(Guid? selectedAdminUserId, CancellationToken ct)
    {
        ViewData["Title"] = "Departments";
        var model = new DepartmentsPageModel { IsConnected = _api.IsConnected() };
        if (!model.IsConnected) return View(model);
        try
        {
            model.Departments = await _api.GetDepartmentDetailsAsync(ct);

            var identity = _api.GetSessionIdentity();
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
    public async Task<IActionResult> CreateDepartment(string name, string code, int institutionType, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.CreateDepartmentAsync(name, code, institutionType, ct); TempData["PortalMessage"] = $"Department '{name}' created."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Departments));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDepartment(Guid id, string newName, int? institutionType, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.UpdateDepartmentAsync(id, newName, institutionType, ct); TempData["PortalMessage"] = $"Department updated to '{newName}'."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Departments));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateDepartment(Guid id, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.DeactivateDepartmentAsync(id, ct); TempData["PortalMessage"] = "Department deactivated."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Departments));
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
    public async Task<IActionResult> Programs(Guid? departmentId, CancellationToken ct)
    {
        ViewData["Title"] = "Programs";
        var model = new ProgramsPageModel { IsConnected = _api.IsConnected(), SelectedDepartmentId = departmentId };
        if (!model.IsConnected) return View(model);

        var identity = _api.GetSessionIdentity();
        if (identity?.IsAdmin != true && identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only Admin or SuperAdmin can manage programs.";
            return RedirectToAction(nameof(Dashboard));
        }

        try
        {
            model.Departments = await _api.GetDepartmentsAsync(ct);
            var programs = await _api.GetProgramDetailsAsync(departmentId, ct);
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
                await _api.CreateProgramAsync(name, code, departmentId, totalSemesters, ct);
                TempData["PortalMessage"] = $"Program '{name}' created.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Programs), new { departmentId = filterDepartmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProgram(Guid id, string newName, Guid? filterDepartmentId, CancellationToken ct)
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
                await _api.UpdateProgramAsync(id, newName, ct);
                TempData["PortalMessage"] = "Program updated.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Programs), new { departmentId = filterDepartmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateProgram(Guid id, Guid? filterDepartmentId, CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only SuperAdmin can deactivate programs.";
            return RedirectToAction(nameof(Dashboard));
        }

        if (_api.IsConnected())
        {
            try
            {
                await _api.DeactivateProgramAsync(id, ct);
                TempData["PortalMessage"] = "Program deactivated.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Programs), new { departmentId = filterDepartmentId });
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
    public async Task<IActionResult> Courses(Guid? departmentId, CancellationToken ct)
    {
        ViewData["Title"] = "Courses & Offerings";
        var model = new CoursesPageModel { IsConnected = _api.IsConnected(), SelectedDepartmentId = departmentId };
        if (!model.IsConnected) return View(model);
        try
        {
            var sessionId = _api.GetSessionIdentity();
            model.Departments = await _api.GetDepartmentsAsync(ct);
            model.Semesters   = await _api.GetSemestersAsync(ct);
            if (sessionId?.IsAdmin == true || sessionId?.IsSuperAdmin == true)
                model.Faculty = await _api.GetFacultyAsync(ct);
            model.Courses     = await _api.GetCourseDetailsAsync(departmentId, ct);
            model.Offerings   = await _api.GetCourseOfferingsAsync(departmentId, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        model.Message ??= TempData["PortalMessage"]?.ToString();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCourse(
        string code, string title, int creditHours, Guid departmentId, Guid? filterDepartmentId,
        bool hasSemesters, int? totalSemesters, int? durationValue, string? durationUnit, string? gradingType,
        CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                await _api.CreateCourseAsync(code, title, creditHours, departmentId,
                    hasSemesters, totalSemesters, durationValue, durationUnit, gradingType, ct);
                TempData["PortalMessage"] = $"Course '{code}' created.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Courses), new { departmentId = filterDepartmentId });
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

                var courses = await _api.GetCourseDetailsAsync(null, ct);
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
    public async Task<IActionResult> CreateOffering(Guid courseId, Guid semesterId, int maxEnrollment, Guid? facultyUserId, Guid? departmentId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.CreateOfferingAsync(courseId, semesterId, maxEnrollment, facultyUserId == Guid.Empty ? null : facultyUserId, ct); TempData["PortalMessage"] = "Course offering created."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Courses), new { departmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateCourse(Guid id, Guid? departmentId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.DeactivateCourseAsync(id, ct); TempData["PortalMessage"] = "Course deactivated."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Courses), new { departmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOffering(Guid id, Guid? departmentId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.DeleteOfferingAsync(id, ct); TempData["PortalMessage"] = "Offering deleted."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Courses), new { departmentId });
    }

    // ── Assignments ────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Assignments(Guid? offeringId, string? semesterName, Guid? selectedAssignmentId, CancellationToken ct)
    {
        ViewData["Title"] = "Assignments";
        var model = new AssignmentsPageModel
        {
            IsConnected          = _api.IsConnected(),
            SelectedOfferingId   = offeringId,
            SelectedSemesterName = semesterName,
            SelectedAssignmentId = selectedAssignmentId,
            Message              = TempData["PortalMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var sessionId = _api.GetSessionIdentity();
            if (sessionId?.IsStudent == true)
            {
                var allOfferings = await GetOfferingFilterOptionsAsync(sessionId, ct);
                model.SemesterOptions = BuildSemesterOptions(allOfferings);
                model.CourseOfferings = FilterOfferingsBySemester(allOfferings, semesterName);

                if (offeringId.HasValue)
                {
                    model.Assignments = await _api.GetAssignmentsByOfferingAsync(offeringId.Value, ct);
                }
                else if (!string.IsNullOrWhiteSpace(semesterName))
                {
                    var assignmentBatches = await Task.WhenAll(
                        model.CourseOfferings.Select(o => _api.GetAssignmentsByOfferingAsync(o.Id, ct)));

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
                model.Assignments = await _api.GetAssignmentsByOfferingAsync(offeringId.Value, ct);
                if (selectedAssignmentId.HasValue)
                    model.Submissions = await _api.GetSubmissionsForAssignmentAsync(selectedAssignmentId.Value, ct);

                model.CourseOfferings = await GetOfferingFilterOptionsAsync(sessionId, ct);
                model.SemesterOptions = BuildSemesterOptions(model.CourseOfferings);
            }
            else
            {
                model.CourseOfferings = await GetOfferingFilterOptionsAsync(sessionId, ct);
                model.SemesterOptions = BuildSemesterOptions(model.CourseOfferings);
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    // ── Attendance ─────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Attendance(Guid? offeringId, CancellationToken ct)
    {
        ViewData["Title"] = "Attendance";
        var model = new AttendancePageModel
        {
            IsConnected      = _api.IsConnected(),
            SelectedOfferingId = offeringId,
            Message          = TempData["PortalMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var sessionId = _api.GetSessionIdentity();
            if (sessionId?.IsStudent == true)
            {
                model.Summary = await _api.GetMyAttendanceSummaryAsync(ct);
            }
            else if (offeringId.HasValue)
            {
                model.Records = await _api.GetAttendanceByOfferingAsync(offeringId.Value, ct);
                model.Roster  = await _api.GetEnrollmentRosterAsync(offeringId.Value, ct);
            }

            model.CourseOfferings = await GetOfferingFilterOptionsAsync(sessionId, ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    // ── Results ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Results(Guid? offeringId, string? semesterName, CancellationToken ct)
    {
        ViewData["Title"] = "Results";
        var model = new ResultsPageModel
        {
            IsConnected      = _api.IsConnected(),
            SelectedOfferingId = offeringId,
            SelectedSemesterName = semesterName,
            Message          = TempData["PortalMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var sessionId = _api.GetSessionIdentity();
            var allOfferings = await GetOfferingFilterOptionsAsync(sessionId, ct);
            model.SemesterOptions = BuildSemesterOptions(allOfferings);
            model.Offerings = FilterOfferingsBySemester(allOfferings, semesterName);
            if (sessionId?.IsStudent == true)
            {
                model.Results = await _api.GetMyResultsAsync(ct);

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
                    var myProjects = await _api.GetMyFypProjectsAsync(ct);
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
                                    LetterGrade = "Completed",
                                    IsPublished = true,
                                    SemesterName = "Semester 8",
                                    StudentName = completedFyp.StudentName,
                                    RegistrationNumber = string.Empty
                                });
                            }
                        }
                    }
                }
            }
            else if (offeringId.HasValue)
            {
                model.Results   = await _api.GetResultsByOfferingAsync(offeringId.Value, ct);
                model.Roster    = await _api.GetEnrollmentRosterAsync(offeringId.Value, ct);
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    // ── Quizzes ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Quizzes(Guid? offeringId, string? semesterName, CancellationToken ct)
    {
        ViewData["Title"] = "Quizzes";
        var model = new QuizzesPageModel
        {
            IsConnected      = _api.IsConnected(),
            SelectedOfferingId = offeringId,
            SelectedSemesterName = semesterName,
            Message          = TempData["PortalMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var sessionId = _api.GetSessionIdentity();
            var allOfferings = await GetOfferingFilterOptionsAsync(sessionId, ct);
            model.SemesterOptions = BuildSemesterOptions(allOfferings);
            model.CourseOfferings = FilterOfferingsBySemester(allOfferings, semesterName);

            if (offeringId.HasValue)
                model.Quizzes = await _api.GetQuizzesByOfferingAsync(offeringId.Value, ct);
            else if (sessionId?.IsStudent == true && !string.IsNullOrWhiteSpace(semesterName))
            {
                var quizBatches = await Task.WhenAll(
                    model.CourseOfferings.Select(o => _api.GetQuizzesByOfferingAsync(o.Id, ct)));

                model.Quizzes = quizBatches
                    .SelectMany(q => q)
                    .GroupBy(q => q.Id)
                    .Select(g => g.First())
                    .ToList();
            }

            if (sessionId?.IsStudent == true)
                model.MyAttempts = await _api.GetMyAttemptsAsync(ct);
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    // ── FYP ────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Fyp(Guid? departmentId, CancellationToken ct)
    {
        ViewData["Title"] = "FYP Management";
        var model = new FypPageModel
        {
            IsConnected         = _api.IsConnected(),
            SelectedDepartmentId = departmentId,
            Message             = TempData["PortalMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var sessionId = _api.GetSessionIdentity();
            if (sessionId?.IsStudent == true)
            {
                var profile = await _api.GetMyStudentProfileAsync(ct);
                if ((profile?.CurrentSemesterNumber ?? 0) < 8)
                {
                    TempData["PortalMessage"] = "FYP becomes available from semester 8.";
                    return RedirectToAction(nameof(Dashboard));
                }

                model.Projects = await _api.GetMyFypProjectsAsync(ct);
            }
            // Issue-Fix Phase 3 Stage 3.8 — Faculty FYP workflow: load supervised projects + student list for FYP creation.
            else if (sessionId?.IsFaculty == true)
            {
                model.Departments = await _api.GetDepartmentsAsync(ct);
                model.UpcomingMeetings = await _api.GetUpcomingMeetingsAsync(ct);
                model.Projects = await _api.GetMySupervisedProjectsAsync(ct);

                // Load students so faculty can create an FYP record for a student.
                var deptToLoad = departmentId ?? model.Departments.FirstOrDefault()?.Id;
                if (deptToLoad.HasValue)
                    model.Students = await _api.GetStudentsAsync(deptToLoad, ct);
            }
            else if (departmentId.HasValue)
            {
                model.Departments = await _api.GetDepartmentsAsync(ct);
                model.Faculty = await _api.GetFacultyAsync(ct);
                model.Students = await _api.GetStudentsAsync(departmentId, ct);
                model.UpcomingMeetings = await _api.GetUpcomingMeetingsAsync(ct);
                model.Projects = await _api.GetFypByDepartmentAsync(departmentId.Value, ct);
            }
            else if (sessionId?.IsAdmin == true || sessionId?.IsSuperAdmin == true)
            {
                model.Departments = await _api.GetDepartmentsAsync(ct);
                model.Faculty = await _api.GetFacultyAsync(ct);
                model.Students = await _api.GetStudentsAsync(null, ct);
                model.UpcomingMeetings = await _api.GetUpcomingMeetingsAsync(ct);
                model.Projects = await _api.GetAllFypProjectsAsync(ct);
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
        DateTime dueDate, decimal maxMarks, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.CreateAssignmentAsync(offeringId, title, description, dueDate, maxMarks, ct); }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Assignments), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAssignment(
        Guid id, Guid offeringId, string title, string? description,
        DateTime dueDate, decimal maxMarks, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.UpdateAssignmentAsync(id, title, description, dueDate, maxMarks, ct); TempData["PortalMessage"] = "Assignment updated."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Assignments), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishAssignment(Guid id, Guid? offeringId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.PublishAssignmentAsync(id, ct); }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Assignments), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAssignment(Guid id, Guid? offeringId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.DeleteAssignmentAsync(id, ct); }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Assignments), new { offeringId });
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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkMarkAttendance(
        Guid offeringId, DateTime date,
        [FromForm] Guid[] studentIds, [FromForm] string[] statuses,
        CancellationToken ct)
    {
        if (_api.IsConnected() && studentIds.Length > 0)
        {
            try
            {
                var entries = studentIds
                    .Zip(statuses, (sid, s) => (StudentProfileId: sid, Status: s));
                await _api.BulkMarkAttendanceAsync(offeringId, date, entries, ct);
                TempData["PortalMessage"] = "Attendance marked.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Attendance), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CorrectAttendance(
        Guid studentProfileId, Guid offeringId, DateTime date,
        string newStatus, string? remarks, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.CorrectAttendanceAsync(studentProfileId, offeringId, date, newStatus, remarks, ct); TempData["PortalMessage"] = "Attendance corrected."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Attendance), new { offeringId });
    }

    // ── Result write actions ────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateResult(
        Guid studentProfileId, Guid offeringId,
        string resultType, decimal marksObtained, decimal maxMarks,
        bool promote, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                await _api.CreateResultAsync(studentProfileId, offeringId, resultType, marksObtained, maxMarks, ct);

                // Promotion is only offered in the UI for Final result type.
                // When the checkbox is checked the form sends promote=true;
                // unchecked checkboxes send nothing, so promote defaults to false.
                if (promote)
                    await _api.PromoteStudentAsync(studentProfileId, ct);
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Results), new { offeringId });
    }

    // Standalone per-row Promote button in the Results table.
    // Reuses the existing POST api/v1/student-lifecycle/{id}/promote endpoint
    // without requiring a new result entry.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteStudentFromResult(Guid studentProfileId, Guid? offeringId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.PromoteStudentAsync(studentProfileId, ct); TempData["PortalMessage"] = "Student promoted to next semester."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Results), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CorrectResult(
        Guid studentProfileId, Guid offeringId, string resultType,
        decimal newMarksObtained, decimal newMaxMarks, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.CorrectResultAsync(studentProfileId, offeringId, resultType, newMarksObtained, newMaxMarks, ct); TempData["PortalMessage"] = "Result corrected."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Results), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishAllResults(Guid offeringId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.PublishAllResultsAsync(offeringId, ct); TempData["PortalMessage"] = "All results published."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Results), new { offeringId });
    }

    // ── Quiz write actions ──────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQuiz(
        Guid offeringId, string title, string? instructions,
        int? timeLimitMinutes, int maxAttempts, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.CreateQuizAsync(offeringId, title, instructions, timeLimitMinutes, maxAttempts, ct); }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Quizzes), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuiz(
        Guid id, Guid offeringId, string title, string? instructions,
        int? timeLimitMinutes, int maxAttempts, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.UpdateQuizAsync(id, title, instructions, timeLimitMinutes, maxAttempts, ct); TempData["PortalMessage"] = "Quiz updated."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Quizzes), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishQuiz(Guid id, Guid? offeringId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.PublishQuizAsync(id, ct); }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Quizzes), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuiz(Guid id, Guid? offeringId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.DeleteQuizAsync(id, ct); }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Quizzes), new { offeringId });
    }

    // ── FYP write actions ───────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ProposeFypProject(
        Guid departmentId, string title, string description, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.ProposeFypProjectAsync(departmentId, title, description, ct); }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Fyp));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFypProject(
        Guid studentProfileId, Guid departmentId, string title, string description, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.CreateFypProjectAsync(studentProfileId, departmentId, title, description, ct); TempData["PortalMessage"] = "FYP project created."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Fyp), new { departmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFypProject(Guid id, Guid? departmentId, string title, string description, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.UpdateFypProjectAsync(id, title, description, ct); TempData["PortalMessage"] = "FYP project updated."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Fyp), new { departmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveFypProject(Guid id, string? remarks, Guid? departmentId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.ApproveFypProjectAsync(id, remarks, ct); TempData["PortalMessage"] = "Project approved."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Fyp), new { departmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectFypProject(Guid id, string remarks, Guid? departmentId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.RejectFypProjectAsync(id, remarks, ct); TempData["PortalMessage"] = "Project rejected."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Fyp), new { departmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignFypSupervisor(Guid id, Guid supervisorUserId, Guid? departmentId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.AssignFypSupervisorAsync(id, supervisorUserId, ct); TempData["PortalMessage"] = "Supervisor assigned."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Fyp), new { departmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteFypProject(Guid id, Guid? departmentId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.CompleteFypProjectAsync(id, ct); TempData["PortalMessage"] = "Project marked as complete."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Fyp), new { departmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestFypCompletion(Guid id, Guid? departmentId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                await _api.RequestFypCompletionAsync(id, ct);
                TempData["PortalMessage"] = "Completion request sent to assigned faculty for approvals.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }

        return RedirectToAction(nameof(Fyp), new { departmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveFypCompletion(Guid id, Guid? departmentId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                await _api.ApproveFypCompletionAsync(id, ct);
                TempData["PortalMessage"] = "Completion approval submitted.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }

        return RedirectToAction(nameof(Fyp), new { departmentId });
    }

    [HttpGet]
    public async Task<IActionResult> Analytics(int? institutionType, Guid? departmentId, Guid? courseId, Guid? semesterId, CancellationToken ct)
    {
        ViewData["Title"] = "Analytics";
        var model = await BuildAnalyticsPageModelAsync(institutionType, departmentId, courseId, semesterId, ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AnalyticsSnapshot(int? institutionType, Guid? departmentId, Guid? courseId, Guid? semesterId, CancellationToken ct)
    {
        var model = await BuildAnalyticsPageModelAsync(institutionType, departmentId, courseId, semesterId, ct);
        return Json(new
        {
            success = model.IsConnected,
            isFinanceOnly = model.IsFinanceOnly,
            message = model.Message,
            selectedInstitutionType = model.SelectedInstitutionType,
            selectedDepartmentId = model.SelectedDepartmentId,
            selectedCourseId = model.SelectedCourseId,
            selectedSemesterId = model.SelectedSemesterId,
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

    private async Task<AnalyticsPageModel> BuildAnalyticsPageModelAsync(int? institutionType, Guid? departmentId, Guid? courseId, Guid? semesterId, CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        var model = new AnalyticsPageModel
        {
            IsConnected = _api.IsConnected(),
            IsFinanceOnly = identity is { IsFinance: true, IsAdmin: false, IsSuperAdmin: false },
            SelectedInstitutionType = institutionType,
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
            model.Departments = await _api.GetDepartmentsAsync(ct);
            var allSemesters = await _api.GetSemestersAsync(ct);
            var offerings = await _api.GetCourseOfferingsAsync(model.SelectedDepartmentId, ct);

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
                offerings = await _api.GetCourseOfferingsAsync(null, ct);
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
                model.Performance = await _api.GetPerformanceAnalyticsAsync(model.SelectedDepartmentId, model.SelectedInstitutionType, model.SelectedCourseId, model.SelectedSemesterId, ct);
                model.Attendance = await _api.GetAttendanceAnalyticsAsync(model.SelectedDepartmentId, model.SelectedInstitutionType, model.SelectedCourseId, model.SelectedSemesterId, ct);
                model.Assignments = await _api.GetAssignmentAnalyticsAsync(model.SelectedDepartmentId, model.SelectedInstitutionType, model.SelectedCourseId, model.SelectedSemesterId, ct);
            }

            model.PaymentStatus = await _api.GetPaymentStatusAnalyticsAsync(model.SelectedDepartmentId, model.SelectedInstitutionType, model.SelectedCourseId, model.SelectedSemesterId, ct);

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
    public async Task<IActionResult> StudentLifecycle(Guid? departmentId, int semester = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Student Lifecycle";
        var identity = _api.GetSessionIdentity();
        var model = new StudentLifecyclePageModel
        {
            IsConnected         = _api.IsConnected(),
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
            model.Departments = await _api.GetDepartmentsAsync(ct);

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
                    model.GraduationCandidates = await _api.GetGraduationCandidatesAsync(departmentId.Value, ct);
                    model.StudentsBySemester   = await _api.GetStudentsByAcademicLevelAsync(departmentId.Value, semester, ct);
                }
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GraduateStudent(Guid studentId, Guid? departmentId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.GraduateStudentAsync(studentId, ct); TempData["PortalMessage"] = "Student graduated."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(StudentLifecycle), new { departmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteStudent(Guid studentId, Guid? departmentId, int semester, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.PromoteStudentAsync(studentId, ct); TempData["PortalMessage"] = "Student promoted to the next academic level."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(StudentLifecycle), new { departmentId, semester });
    }

    // ── Payments ───────────────────────────────────────────────────────────
    // Final-Touches Phase 7 — admin all-receipts view + student own receipts

    [HttpGet]
    public async Task<IActionResult> Payments(Guid? studentId, int page = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Payments";
        var model = new PaymentsPageModel
        {
            IsConnected       = _api.IsConnected(),
            SelectedStudentId = studentId,
            Page = page < 1 ? 1 : page,
            Message           = TempData["PortalMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            // Student role: load their own receipts via JWT
            var identity = _api.GetSessionIdentity();
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
                model.Students = await _api.GetStudentsAsync(null, ct);
                // Admin / Finance: load paged receipts and optionally filter by student
                PaymentReceiptPageItem pageResult;
                if (studentId.HasValue)
                    pageResult = await _api.GetPaymentsByStudentAsync(studentId.Value, model.Page, model.PageSize, ct);
                else
                    pageResult = await _api.GetAllPaymentsAsync(model.Page, model.PageSize, ct);

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
    public async Task<IActionResult> CreatePayment(CreatePaymentForm form, CancellationToken ct)
    {
        try
        {
            await _api.CreatePaymentAsync(form.StudentProfileId, form.Amount, form.Description, form.DueDate, ct);
            TempData["PortalMessage"] = "Receipt created successfully.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Payments));
    }

    // Final-Touches Phase 7 Stage 7.2 — edit receipt (Admin/Finance)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePayment(Guid receiptId, decimal amount, string description, DateTime dueDate, string? notes, CancellationToken ct)
    {
        try
        {
            await _api.UpdatePaymentAsync(receiptId, amount, description, dueDate, notes, ct);
            TempData["PortalMessage"] = "Receipt updated successfully.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Payments));
    }

    // Final-Touches Phase 7 Stage 7.2 — confirm payment (Admin/Finance)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPayment(Guid receiptId, CancellationToken ct)
    {
        try
        {
            await _api.ConfirmPaymentAsync(receiptId, ct);
            TempData["PortalMessage"] = "Payment confirmed.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Payments));
    }

    // Final-Touches Phase 7 Stage 7.2 — cancel receipt (Admin/Finance)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelPayment(Guid receiptId, CancellationToken ct)
    {
        try
        {
            await _api.CancelPaymentAsync(receiptId, ct);
            TempData["PortalMessage"] = "Receipt cancelled.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Payments));
    }

    // Final-Touches Phase 7 Stage 7.3 — student marks receipt as submitted
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitProof(Guid receiptId, string proofNote, CancellationToken ct)
    {
        try
        {
            await _api.SubmitProofAsync(receiptId, proofNote, ct);
            TempData["PortalMessage"] = "Proof of payment submitted.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Payments));
    }

    // ── Enrollments ────────────────────────────────────────────────────────

    // Final-Touches Phase 8 Stage 8.1+8.2 — student sees own courses; admin sees offering roster + students list
    // Issue-Fix Phase 3 Stage 3.3 — Faculty: load offerings via GetMyOfferings (dept-scoped) + show roster when offering selected.
    [HttpGet]
    public async Task<IActionResult> Enrollments(Guid? offeringId, CancellationToken ct)
    {
        ViewData["Title"] = "Enrollments";
        var isStudent = User.IsInRole("Student");
        var model = new EnrollmentsPageModel
        {
            IsConnected        = _api.IsConnected(),
            SelectedOfferingId = offeringId,
            IsStudent          = isStudent,
            Message            = TempData["PortalMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var sessionId = _api.GetSessionIdentity();
            var isAdmin = sessionId?.IsAdmin == true || sessionId?.IsSuperAdmin == true;

            // Issue-Fix Phase 3 Stage 3.3 — Use GetCourseOfferingsAsync for all roles; API filters by dept for Faculty.
            model.Offerings = await _api.GetCourseOfferingsAsync(null, ct);

            if (isStudent)
            {
                model.MyCourses = await _api.GetMyEnrollmentsAsync(ct);
            }
            else
            {
                if (isAdmin)
                    model.Students = await _api.GetStudentsAsync(null, ct);

                if (offeringId.HasValue)
                    model.Roster = await _api.GetEnrollmentRosterAsync(offeringId.Value, ct);
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    // Final-Touches Phase 8 Stage 8.2 — admin enrolls a student
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EnrollStudent(Guid studentProfileId, Guid courseOfferingId, CancellationToken ct)
    {
        try
        {
            await _api.AdminEnrollStudentAsync(studentProfileId, courseOfferingId, ct);
            TempData["PortalMessage"] = "Student enrolled successfully.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Enrollments), new { offeringId = courseOfferingId });
    }

    // Final-Touches Phase 8 Stage 8.2 — admin drops any enrollment by ID
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminDropEnrollment(Guid enrollmentId, Guid offeringId, CancellationToken ct)
    {
        try
        {
            await _api.AdminDropEnrollmentAsync(enrollmentId, ct);
            TempData["PortalMessage"] = "Enrollment dropped.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Enrollments), new { offeringId });
    }

    // Final-Touches Phase 8 Stage 8.2 — student self-enrolls in a course offering
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StudentEnroll(Guid courseOfferingId, CancellationToken ct)
    {
        try
        {
            await _api.StudentEnrollAsync(courseOfferingId, ct);
            TempData["PortalMessage"] = "Enrolled successfully.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Enrollments));
    }

    // Final-Touches Phase 8 Stage 8.2 — student drops their own enrollment
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StudentDropEnrollment(Guid courseOfferingId, CancellationToken ct)
    {
        try
        {
            await _api.StudentDropEnrollmentAsync(courseOfferingId, ct);
            TempData["PortalMessage"] = "Course dropped.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Enrollments));
    }

    // ── Reports ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> ReportCenter(CancellationToken ct)
    {
        ViewData["Title"] = "Report Center";
        var model = new ReportCenterPageModel { IsConnected = _api.IsConnected() };
        if (!model.IsConnected) return View(model);
        try { model.Reports = await _api.GetReportCatalogAsync(ct); }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
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
            model.Offerings    = await _api.GetCourseOfferingsAsync(null, ct);
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
            model.Offerings   = await _api.GetCourseOfferingsAsync(null, ct);
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
            model.Offerings   = await _api.GetCourseOfferingsAsync(null, ct);
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
            model.Offerings   = await _api.GetCourseOfferingsAsync(null, ct);
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
            model.CourseOfferings = await _api.GetCoursesAsync(departmentId, ct);
            if (isAdminOnly && !departmentId.HasValue && !courseOfferingId.HasValue)
                model.Message = "Admin must select a department or course offering before generating report data.";
            else
                model.Report = await _api.GetLowAttendanceReportAsync(threshold, departmentId, courseOfferingId, selectedInstitutionType, ct);
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

        var courses = await _api.GetCourseDetailsAsync(departmentId, ct);
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
        var model = new EligibilityPageModel { DepartmentId = departmentId, ProgramId = programId };
        try
        {
            model.Items = await _api.GetEligibilityListAsync(departmentId, programId, ct);
        }
        catch (Exception ex) { TempData["Message"] = "Error: " + ex.Message; }
        return View(model);
    }

    // Final-Touches Phase 17 Stage 17.2 — SuperAdmin manages degree rules
    public async Task<IActionResult> DegreeRules(CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(Dashboard));
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
            if (roleClaim == "Faculty")
                await _api.FacultyApproveApplicationAsync(id, isApproved, note, ct);
            else if (roleClaim == "SuperAdmin" && isApproved)
                await _api.FinalApproveApplicationAsync(id, isApproved, note, ct);
            else
                await _api.AdminApproveApplicationAsync(id, isApproved, note, ct);

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
            model.Departments = await _api.GetDepartmentsAsync(ct);
            model.Semesters = await _api.GetSemestersAsync(ct);

            if (departmentId.HasValue)
            {
                model.Programs = await _api.GetProgramsAsync(departmentId, ct);
                model.Courses = await _api.GetCoursesAsync(departmentId, ct);
            }

            var materials = await _api.GetCourseMaterialsAsync(
                departmentId,
                academicProgramId,
                semesterId,
                courseId,
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
            model.Departments = await _api.GetDepartmentsAsync(ct);
            model.Semesters = await _api.GetSemestersAsync(ct);

            if (departmentId.HasValue)
            {
                model.Programs = await _api.GetProgramsAsync(departmentId, ct);
                model.Courses = await _api.GetCoursesAsync(departmentId, ct);
            }

            var materials = await _api.GetCourseMaterialsAsync(
                departmentId,
                academicProgramId,
                semesterId,
                courseId,
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
        var model = new DiscussionPageModel
        {
            OfferingId     = offeringId,
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
                Id = t.Id, OfferingId = t.OfferingId, Title = t.Title,
                AuthorName = t.AuthorName, IsPinned = t.IsPinned,
                IsClosed = t.IsClosed, ReplyCount = t.ReplyCount, CreatedAt = t.CreatedAt
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
        var model = new DiscussionDetailPageModel
        {
            OfferingId  = offeringId,
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
                    Title = t.Title,
                    AuthorName = t.AuthorName,
                    IsPinned = t.IsPinned,
                    IsClosed = t.IsClosed,
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
                var callerIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(callerIdStr, out var authorId))
                {
                    await _api.AddDiscussionReplyAsync(threadId, authorId, body, ct);
                    TempData["SuccessMessage"] = "Reply posted.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to resolve the current user.";
                }
            }
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
    public async Task<IActionResult> Announcements(Guid offeringId, CancellationToken ct)
    {
        ViewData["Title"] = "Announcements";
        var model = new AnnouncementsPageModel
        {
            OfferingId     = offeringId,
            IsConnected    = _api.IsConnected(),
            SuccessMessage = TempData["SuccessMessage"]?.ToString(),
            ErrorMessage   = TempData["ErrorMessage"]?.ToString()
        };
        if (!model.IsConnected) return View(model);
        try
        {
            var items = await _api.GetAnnouncementsAsync(offeringId, ct);
            model.Announcements = items.Select(a => new AnnouncementItem
            {
                Id = a.Id, OfferingId = a.OfferingId, Title = a.Title,
                Body = a.Body, AuthorName = a.AuthorName, PostedAt = a.PostedAt
            }).ToList();
        }
        catch (Exception ex) { model.ErrorMessage = ex.Message; }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAnnouncement(Guid offeringId, string title, string body, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                await _api.CreateAnnouncementAsync(offeringId, Guid.Empty, title, body, ct);
                TempData["SuccessMessage"] = "Announcement posted.";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(Announcements), new { offeringId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAnnouncement(Guid announcementId, Guid offeringId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.DeleteAnnouncementAsync(announcementId, ct); TempData["SuccessMessage"] = "Announcement deleted."; }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        return RedirectToAction(nameof(Announcements), new { offeringId });
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
        var model = new AccreditationTemplatesPageModel { IsConnected = _api.IsConnected() };
        if (!model.IsConnected) return View(model);
        try
        {
            var items = await _api.GetAccreditationTemplatesAsync(ct);
            model.Templates = items.Select(t => new AccreditationTemplateRow
            {
                Id                = t.Id,
                Name              = t.Name,
                Description       = t.Description,
                Format            = t.Format,
                FieldMappingsJson = t.FieldMappingsJson,
                IsActive          = t.IsActive,
                CreatedAt         = t.CreatedAt
            }).ToList();
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccreditationTemplate(AccreditationTemplateFormModel form, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(AccreditationTemplates));
        try
        {
            await _api.CreateAccreditationTemplateAsync(new CreateAccreditationTemplateForm
            {
                Name              = form.Name,
                Description       = form.Description,
                Format            = form.Format,
                FieldMappingsJson = form.FieldMappingsJson
            }, ct);
            TempData["Success"] = "Accreditation template created.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(AccreditationTemplates));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateAccreditationTemplate(AccreditationTemplateFormModel form, CancellationToken ct)
    {
        if (!_api.IsConnected() || form.Id == null) return RedirectToAction(nameof(AccreditationTemplates));
        try
        {
            await _api.UpdateAccreditationTemplateAsync(form.Id.Value, new UpdateAccreditationTemplateForm
            {
                Name              = form.Name,
                Description       = form.Description,
                Format            = form.Format,
                FieldMappingsJson = form.FieldMappingsJson,
                IsActive          = form.IsActive
            }, ct);
            TempData["Success"] = "Accreditation template updated.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(AccreditationTemplates));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAccreditationTemplate(Guid id, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(AccreditationTemplates));
        try
        {
            await _api.DeleteAccreditationTemplateAsync(id, ct);
            TempData["Success"] = "Accreditation template deleted.";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(AccreditationTemplates));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAccreditationReport(Guid id, CancellationToken ct)
    {
        if (!_api.IsConnected()) return RedirectToAction(nameof(AccreditationTemplates));
        try
        {
            var (content, contentType, fileName) = await _api.GenerateAccreditationReportAsync(id, ct);
            if (content == null) return RedirectToAction(nameof(AccreditationTemplates));
            return File(content, contentType, fileName);
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(AccreditationTemplates));
        }
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

