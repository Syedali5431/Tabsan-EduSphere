using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Tabsan.EduSphere.Web.Models.Portal;
using Tabsan.EduSphere.Web.Services;

namespace Tabsan.EduSphere.Web.Controllers;

public class PortalController : Controller
{
    private readonly IEduApiClient _api;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PortalController> _logger;
    private static readonly ConcurrentDictionary<string, AttendanceImportReportPayload> AttendanceImportReports = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, ResultImportReportPayload> ResultImportReports = new(StringComparer.Ordinal);
    private static readonly TimeSpan AttendanceImportReportTtl = TimeSpan.FromHours(2);
    private static readonly TimeSpan ResultImportReportTtl = TimeSpan.FromHours(2);
    private static Func<DateTime> UtcNowProvider = static () => DateTime.UtcNow;

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
        [nameof(EnterResults)] = "enter_results",
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
        [nameof(GenerateCertificates)] = "generate_certificates",
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
        "enter_results",
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

    private sealed record AttendanceImportReportPayload(string FileName, string CsvContent, DateTime CreatedAtUtc);

    private sealed record ResultImportReportPayload(string FileName, string CsvContent, DateTime CreatedAtUtc);

    private sealed class AttendanceImportReportRow
    {
        public int RowNumber { get; init; }
        public string StudentId { get; init; } = string.Empty;
        public string StudentName { get; init; } = string.Empty;
        public string DateValue { get; init; } = string.Empty;
        public string PresentValue { get; init; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    private sealed class ResultImportReportRow
    {
        public int RowNumber { get; init; }
        public string StudentId { get; init; } = string.Empty;
        public string StudentName { get; init; } = string.Empty;
        public string ResultType { get; init; } = string.Empty;
        public string MarksObtained { get; init; } = string.Empty;
        public string MaxMarks { get; init; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public PortalController(IEduApiClient api, IWebHostEnvironment environment, ILogger<PortalController> logger)
    {
        _api = api;
        _environment = environment;
        _logger = logger;
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
            catch (Exception ex) when (IsApiConnectivityException(ex))
            {
                _logger.LogWarning(ex, "Unable to load visible menu keys for action {ActionName} due to API connectivity issue.", action);
                TempData["PortalMessage"] = "API service is temporarily unavailable. Access checks could not be completed.";
                context.Result = RedirectToAction(nameof(Dashboard));
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

    private static bool IsApiConnectivityException(Exception ex)
    {
        if (ex is HttpRequestException || ex is SocketException || ex is TimeoutException)
            return true;

        if (ex.InnerException is not null)
            return IsApiConnectivityException(ex.InnerException);

        return false;
    }

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

    private static bool IsUniversityDocumentType(string normalizedTemplateType)
        => string.Equals(normalizedTemplateType, "degree", StringComparison.OrdinalIgnoreCase)
           || string.Equals(normalizedTemplateType, "transcript", StringComparison.OrdinalIgnoreCase);

    private static bool IsNonUniversityDocumentType(string normalizedTemplateType)
        => string.Equals(normalizedTemplateType, "completion", StringComparison.OrdinalIgnoreCase)
           || string.Equals(normalizedTemplateType, "reportcard", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeCertificateTemplateType(string? templateType)
    {
        if (string.Equals(templateType, "transcript", StringComparison.OrdinalIgnoreCase))
            return "transcript";

        if (string.Equals(templateType, "completion", StringComparison.OrdinalIgnoreCase)
            || string.Equals(templateType, "completion-certificate", StringComparison.OrdinalIgnoreCase))
            return "completion";

        if (string.Equals(templateType, "reportcard", StringComparison.OrdinalIgnoreCase)
            || string.Equals(templateType, "report-card", StringComparison.OrdinalIgnoreCase)
            || string.Equals(templateType, "report_card", StringComparison.OrdinalIgnoreCase))
            return "reportcard";

        return "degree";
    }

    private static List<CertificateDocumentTypeOption> BuildCertificateDocumentTypes(int? institutionType)
        => IsUniversityInstitutionType(institutionType)
            ? new List<CertificateDocumentTypeOption>
            {
                new() { Value = "degree", Label = "Degree" },
                new() { Value = "transcript", Label = "Transcript" }
            }
            : new List<CertificateDocumentTypeOption>
            {
                new() { Value = "completion", Label = "Completion Certificate" },
                new() { Value = "reportcard", Label = "Report Card" }
            };

    private static string ResolvePeriodFilterLabel(int? institutionType)
        => IsUniversityInstitutionType(institutionType) ? "Semester" : "Class";

    private static string ResolvePeriodFilterLabel(
        IReadOnlyCollection<CertificateInstitutionOption>? institutionOptions,
        int? selectedInstitutionType)
    {
        var options = institutionOptions ?? Array.Empty<CertificateInstitutionOption>();
        var hasUniversity = options.Any(x => x.Value == 0);
        var hasSchoolOrCollege = options.Any(x => x.Value is 1 or 2);

        if (hasUniversity && hasSchoolOrCollege)
            return "Semester/Class";

        if (hasUniversity)
            return "Semester";

        if (hasSchoolOrCollege)
            return "Class";

        return IsUniversityInstitutionType(selectedInstitutionType) ? "Semester" : "Class";
    }

    private static string ResolvePeriodFilterPlaceholder(string periodFilterLabel)
        => periodFilterLabel switch
        {
            "Class" => "All Classes",
            "Semester/Class" => "All Semesters/Classes",
            _ => "All Semesters"
        };

    private static List<CertificateInstitutionOption> BuildLicensedInstitutionOptions(PortalCapabilityMatrixApiModel? matrix)
    {
        var options = new List<CertificateInstitutionOption>();
        var includeUniversity = matrix?.IncludeUniversity ?? true;
        var includeSchool = matrix?.IncludeSchool ?? true;
        var includeCollege = matrix?.IncludeCollege ?? true;

        if (includeUniversity)
            options.Add(new CertificateInstitutionOption { Value = 0, Label = "University" });
        if (includeSchool)
            options.Add(new CertificateInstitutionOption { Value = 1, Label = "School" });
        if (includeCollege)
            options.Add(new CertificateInstitutionOption { Value = 2, Label = "College" });

        if (options.Count == 0)
            options.Add(new CertificateInstitutionOption { Value = 0, Label = "University" });

        return options;
    }

    private static List<CertificateInstitutionOption> BuildLicensedPaymentsInstitutionOptions(PortalCapabilityMatrixApiModel? matrix)
    {
        // Payments data in this deployment uses legacy values: 0=School, 1=College, 2=University.
        var options = new List<CertificateInstitutionOption>();
        var includeUniversity = matrix?.IncludeUniversity ?? true;
        var includeSchool = matrix?.IncludeSchool ?? true;
        var includeCollege = matrix?.IncludeCollege ?? true;

        if (includeUniversity)
            options.Add(new CertificateInstitutionOption { Value = 2, Label = "University" });
        if (includeSchool)
            options.Add(new CertificateInstitutionOption { Value = 0, Label = "School" });
        if (includeCollege)
            options.Add(new CertificateInstitutionOption { Value = 1, Label = "College" });

        if (options.Count == 0)
            options.Add(new CertificateInstitutionOption { Value = 2, Label = "University" });

        return options;
    }

    private static int? ResolveLicensedInstitutionSelection(
        int? requestedInstitutionType,
        SessionIdentity? identity,
        IReadOnlyCollection<CertificateInstitutionOption> licensedOptions)
    {
        if (licensedOptions.Count == 1)
            return licensedOptions.First().Value;

        if (requestedInstitutionType.HasValue && licensedOptions.Any(x => x.Value == requestedInstitutionType.Value))
            return requestedInstitutionType.Value;

        if (identity?.IsSuperAdmin == true)
            return null;

        if (identity?.InstitutionType is int identityInstitutionType
            && licensedOptions.Any(x => x.Value == identityInstitutionType))
            return identityInstitutionType;

        return null;
    }

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
    public async Task<IActionResult> ForceChangePassword(string currentPassword, string newPassword, string confirmPassword, CancellationToken ct)
    {
        ViewData["Title"] = "Force Change Password";
        var model = new ForceChangePasswordPageModel { IsConnected = _api.IsConnected() };

        if (!model.IsConnected)
            return RedirectToAction("Index", "Login");

        if (string.IsNullOrWhiteSpace(currentPassword))
        {
            model.Message = "Old password is required.";
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            model.Message = "New password is required.";
            return View(model);
        }

        if (!TryValidateSafePassword(newPassword, out var passwordPolicyMessage))
        {
            model.Message = passwordPolicyMessage;
            return View(model);
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            model.Message = "Password confirmation does not match.";
            return View(model);
        }

        try
        {
            await _api.ForceChangePasswordAsync(currentPassword, newPassword, ct);
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

    private static bool TryValidateSafePassword(string password, out string message)
    {
        var issues = new List<string>();

        if (password.Length < 12 || password.Length > 16)
            issues.Add("Use 12-16 characters.");

        if (!password.Any(char.IsUpper))
            issues.Add("Include at least one uppercase letter (A-Z).");

        if (!password.Any(char.IsLower))
            issues.Add("Include at least one lowercase letter (a-z).");

        if (!password.Any(char.IsDigit))
            issues.Add("Include at least one number (0-9).");

        if (!password.Any(c => "!@#$%^&*".Contains(c)))
            issues.Add("Include at least one symbol (! @ # $ % ^ & *).");

        var normalized = password.ToLowerInvariant();
        var simplePatterns = new[] { "123456", "password", "qwerty", "abc123", "111111", "000000", "letmein", "welcome" };
        if (simplePatterns.Any(normalized.Contains))
            issues.Add("Do not use simple patterns like 123456, password, or qwerty.");

        if (issues.Count == 0)
        {
            message = string.Empty;
            return true;
        }

        message = "Password rules not met: " + string.Join(" ", issues);
        return false;
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
        var callerIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        return Guid.TryParse(callerIdStr, out var parsedUserId) ? parsedUserId : null;
    }

    private async Task<List<LookupItem>> GetOfferingFilterOptionsAsync(SessionIdentity? sessionIdentity, CancellationToken ct, Guid? tenantId = null, Guid? campusId = null, Guid? departmentId = null)
    {
        if (sessionIdentity?.IsAdmin == true || sessionIdentity?.IsSuperAdmin == true)
        {
            var offerings = await _api.GetCourseOfferingsAsync(departmentId, tenantId, campusId, null, ct);
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

        if (identity?.IsSuperAdmin != true)
        {
            vm.SelectedTenantId ??= identity?.TenantId;
            vm.SelectedCampusId ??= identity?.CampusId;
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
        Guid? departmentId,
        Guid? facultyUserId,
        bool includeInactive,
        CancellationToken ct)
    {
        ViewData["Title"] = "Teacher Timetable";
        var identity = _api.GetSessionIdentity();
        var canManage = identity?.IsAdmin == true || identity?.IsSuperAdmin == true;
        var vm = new TimetableTeacherPageModel
        {
            IsConnected = _api.IsConnected(),
            Identity = identity,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId,
            SelectedDepartmentId = departmentId,
            SelectedFacultyUserId = facultyUserId,
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

            if (canManage)
            {
                if (vm.SelectedCampusId.HasValue)
                    vm.Departments = await _api.GetDepartmentsAsync(vm.SelectedTenantId, vm.SelectedCampusId, ct);

                if (vm.SelectedDepartmentId.HasValue)
                    vm.Faculty = await _api.GetFacultyAsync(vm.SelectedTenantId, vm.SelectedCampusId, vm.SelectedDepartmentId, ct);

                if (!vm.SelectedFacultyUserId.HasValue)
                {
                    vm.Message ??= "Select a faculty to view timetable.";
                    return View(vm);
                }
            }

            vm.Entries = await _api.GetTeacherEntriesAsync(
                vm.SelectedTenantId,
                vm.SelectedCampusId,
                vm.SelectedDepartmentId,
                vm.SelectedFacultyUserId,
                vm.IncludeInactive,
                ct);
        }
        catch (Exception ex)
        {
            vm.Message = ex.Message;
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateTeacherTimetable(Guid timetableId, Guid? tenantId, Guid? campusId, Guid? departmentId, Guid? facultyUserId, bool includeInactive, CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity?.IsAdmin != true && identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only Admin or SuperAdmin can activate timetables.";
            return RedirectToAction(nameof(TimetableTeacher), new { tenantId, campusId, departmentId, facultyUserId, includeInactive });
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

        return RedirectToAction(nameof(TimetableTeacher), new { tenantId, campusId, departmentId, facultyUserId, includeInactive });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateTeacherTimetable(Guid timetableId, Guid? tenantId, Guid? campusId, Guid? departmentId, Guid? facultyUserId, bool includeInactive, CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();
        if (identity?.IsAdmin != true && identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only Admin or SuperAdmin can deactivate timetables.";
            return RedirectToAction(nameof(TimetableTeacher), new { tenantId, campusId, departmentId, facultyUserId, includeInactive });
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

        return RedirectToAction(nameof(TimetableTeacher), new { tenantId, campusId, departmentId, facultyUserId, includeInactive });
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

    // Final-Touches Phase 19 Stage 19.3 — web-side proxy endpoint for Result Calculation course-type filter.
    [HttpGet]
    public async Task<IActionResult> ResultCalculationCourseFilterData([FromQuery] bool? hasSemesters, CancellationToken ct)
    {
        if (!_api.IsConnected())
            return Unauthorized();

        try
        {
            var courses = hasSemesters.HasValue
                ? await _api.GetCourseDetailsByTypeAsync(hasSemesters.Value, ct)
                : await _api.GetCourseDetailsAsync(null, null, null, null, ct);

            var response = courses
                .Select(c => new { id = c.Id, code = c.Code, title = c.Title })
                .ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load courses for Result Calculation filter endpoint.");
            return StatusCode(500, new { message = "Failed to load courses." });
        }
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
        catch (Exception ex) when (IsApiConnectivityException(ex))
        {
            _logger.LogWarning(ex, "Students page failed to load because the API service is unreachable.");
            model.Message = "Unable to load students right now because the API service is temporarily unavailable. Please try again shortly.";
        }
        catch (Exception ex)
        {
            model.Message = ex.Message;
        }
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
    public async Task<IActionResult> GradingConfig(
        int? institutionType,
        Guid? tenantId,
        Guid? campusId,
        Guid? departmentId,
        Guid? courseId,
        Guid? semesterId,
        Guid? subjectOfferingId,
        CancellationToken ct)
    {
        var model = new GradingConfigPageModel { IsConnected = _api.IsConnected() };
        if (model.IsConnected)
        {
            try
            {
                var session = _api.GetSessionIdentity();
                model.CanManageInstitutionSections = session?.IsSuperAdmin == true;
                model.CanSelectTenantCampus = session?.IsSuperAdmin == true;
                model.SelectedInstitutionType = ResolveReportInstitutionType(institutionType) ?? 0;
                model.IsUniversitySelected = IsUniversityGradingInstitutionType(model.SelectedInstitutionType);
                model.SelectedTenantId = tenantId;
                model.SelectedCampusId = campusId;
                model.SelectedDepartmentId = departmentId;
                model.SelectedCourseId = courseId;
                model.SelectedSemesterId = semesterId;
                model.SelectedSubjectOfferingId = subjectOfferingId;

                if (model.CanSelectTenantCampus)
                {
                    model.Tenants = await _api.GetTenantsAsync(ct);
                    if (model.SelectedTenantId.HasValue)
                    {
                        model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);
                        if (!model.SelectedCampusId.HasValue && model.Campuses.Count == 1)
                            model.SelectedCampusId = model.Campuses[0].Id;
                    }
                }
                else
                {
                    model.SelectedTenantId ??= session?.TenantId;
                    model.SelectedCampusId ??= session?.CampusId;
                }

                if (model.CanSelectTenantCampus && model.SelectedTenantId.HasValue && !model.SelectedCampusId.HasValue)
                {
                    model.ErrorMessage ??= "Select a campus to continue loading dependent filters.";
                }
                else
                {
                    var departmentDetails = await _api.GetDepartmentDetailsAsync(model.SelectedTenantId, model.SelectedCampusId, ct);
                    model.Departments = departmentDetails
                        .Where(d => d.InstitutionType == model.SelectedInstitutionType)
                        .Select(d => new LookupItem
                        {
                            Id = d.Id,
                            Name = d.Name,
                            InstitutionType = d.InstitutionType
                        })
                        .ToList();

                    if (model.SelectedDepartmentId.HasValue && model.Departments.All(d => d.Id != model.SelectedDepartmentId.Value))
                        model.SelectedDepartmentId = null;

                    var offerings = await _api.GetCourseOfferingsAsync(
                        model.SelectedDepartmentId,
                        model.SelectedTenantId,
                        model.SelectedCampusId,
                        model.SelectedInstitutionType,
                        ct);

                    var allSemesters = await _api.GetSemestersAsync(ct);
                    var offeringSemesterIds = offerings
                        .Select(o => o.SemesterId)
                        .Distinct()
                        .ToHashSet();

                    model.Semesters = allSemesters
                        .Where(s => !offeringSemesterIds.Any() || offeringSemesterIds.Contains(s.Id))
                        .ToList();

                    if (model.SelectedSemesterId.HasValue && model.Semesters.All(s => s.Id != model.SelectedSemesterId.Value))
                        model.SelectedSemesterId = null;

                    var courses = await _api.GetCourseDetailsAsync(
                        model.SelectedDepartmentId,
                        model.SelectedTenantId,
                        model.SelectedCampusId,
                        model.SelectedInstitutionType,
                        ct);

                    if (model.SelectedSemesterId.HasValue)
                    {
                        var semesterCourseIds = offerings
                            .Where(o => o.SemesterId == model.SelectedSemesterId.Value)
                            .Select(o => o.CourseId)
                            .Distinct()
                            .ToHashSet();

                        courses = courses
                            .Where(c => semesterCourseIds.Contains(c.Id))
                            .ToList();
                    }

                    model.Courses = courses;
                    if (model.SelectedCourseId.HasValue && model.Courses.All(c => c.Id != model.SelectedCourseId.Value))
                        model.SelectedCourseId = null;

                    if (model.IsUniversitySelected)
                    {
                        var subjectOfferings = offerings.AsEnumerable();

                        if (model.SelectedSemesterId.HasValue)
                            subjectOfferings = subjectOfferings.Where(o => o.SemesterId == model.SelectedSemesterId.Value);

                        if (model.SelectedCourseId.HasValue)
                            subjectOfferings = subjectOfferings.Where(o => o.CourseId == model.SelectedCourseId.Value);

                        model.SubjectOfferings = subjectOfferings
                            .Select(o => new LookupItem
                            {
                                Id = o.Id,
                                Name = string.IsNullOrWhiteSpace(o.FacultyName)
                                    ? $"{o.CourseCode} - {o.CourseTitle} ({o.SemesterName})"
                                    : $"{o.CourseCode} - {o.CourseTitle} ({o.SemesterName}) [{o.FacultyName}]"
                            })
                            .ToList();

                        if (model.SelectedSubjectOfferingId.HasValue && model.SubjectOfferings.All(s => s.Id != model.SelectedSubjectOfferingId.Value))
                            model.SelectedSubjectOfferingId = null;

                        if (model.SelectedSubjectOfferingId.HasValue)
                        {
                            var selectedOffering = offerings.FirstOrDefault(o => o.Id == model.SelectedSubjectOfferingId.Value);
                            if (selectedOffering is not null)
                            {
                                model.SelectedCourseId = selectedOffering.CourseId;
                                model.SelectedSemesterId = selectedOffering.SemesterId;
                            }
                        }
                    }
                    else
                    {
                        model.SelectedSubjectOfferingId = null;
                    }
                }

                if (model.CanManageInstitutionSections)
                {
                    var profiles = await _api.GetInstitutionGradingProfilesAsync(ct);
                    model.InstitutionSections = BuildInstitutionGradingSections(profiles);
                }

                if (model.SelectedCourseId.HasValue)
                {
                    var config = await _api.GetCourseGradingConfigAsync(model.SelectedCourseId.Value, ct);
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
        int? selectedInstitutionType,
        Guid? tenantId,
        Guid? campusId,
        Guid? departmentId,
        Guid? courseId,
        Guid? semesterId,
        Guid? subjectOfferingId,
        CancellationToken ct)
    {
        if (!_api.IsConnected())
            return RedirectToAction(nameof(GradingConfig), new { institutionType = selectedInstitutionType, tenantId, campusId, departmentId, courseId, semesterId, subjectOfferingId });

        var session = _api.GetSessionIdentity();
        if (session?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only SuperAdmin can update institution grading sections.";
            return RedirectToAction(nameof(GradingConfig), new { institutionType = selectedInstitutionType, tenantId, campusId, departmentId, courseId, semesterId, subjectOfferingId });
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

        return RedirectToAction(nameof(GradingConfig), new { institutionType = selectedInstitutionType, tenantId, campusId, departmentId, courseId, semesterId, subjectOfferingId });
    }

    // Final-Touches Phase 19 Stage 19.4 — GradingConfig save (POST)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveGradingConfig(
        Guid courseId,
        decimal passThreshold,
        string gradingType,
        string? gradeRangesJson,
        int? institutionType,
        Guid? tenantId,
        Guid? campusId,
        Guid? departmentId,
        Guid? semesterId,
        Guid? subjectOfferingId,
        CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var selectedInstitutionType = ResolveReportInstitutionType(institutionType);

                if (IsUniversityGradingInstitutionType(selectedInstitutionType) || !departmentId.HasValue)
                {
                    await _api.SaveCourseGradingConfigAsync(courseId, passThreshold, gradingType, gradeRangesJson, ct);
                    TempData["PortalMessage"] = "Grading configuration saved.";
                }
                else
                {
                    var targetCourses = await _api.GetCourseDetailsAsync(departmentId, tenantId, campusId, selectedInstitutionType, ct);

                    if (semesterId.HasValue)
                    {
                        var offerings = await _api.GetCourseOfferingsAsync(departmentId, tenantId, campusId, selectedInstitutionType, ct);
                        var semesterCourseIds = offerings
                            .Where(o => o.SemesterId == semesterId.Value)
                            .Select(o => o.CourseId)
                            .Distinct()
                            .ToHashSet();

                        targetCourses = targetCourses
                            .Where(c => semesterCourseIds.Contains(c.Id))
                            .ToList();
                    }

                    var courseIds = targetCourses
                        .Select(c => c.Id)
                        .Distinct()
                        .ToList();

                    if (!courseIds.Contains(courseId))
                        courseIds.Add(courseId);

                    foreach (var targetCourseId in courseIds)
                        await _api.SaveCourseGradingConfigAsync(targetCourseId, passThreshold, gradingType, gradeRangesJson, ct);

                    TempData["PortalMessage"] = $"Grading configuration applied to {courseIds.Count} subject(s) for the selected school/college scope.";
                }
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(GradingConfig), new { institutionType, tenantId, campusId, departmentId, courseId, semesterId, subjectOfferingId });
    }

    private static bool IsUniversityGradingInstitutionType(int? institutionType)
        => !institutionType.HasValue || institutionType.Value == 0;

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
    public async Task<IActionResult> Assignments(Guid? offeringId, string? semesterName, Guid? selectedAssignmentId, Guid? tenantId, Guid? campusId, Guid? departmentId, bool includeInactive = false, CancellationToken ct = default)
    {
        ViewData["Title"] = "Assignments";
        var identity = _api.GetSessionIdentity();
        var model = new AssignmentsPageModel
        {
            IsConnected          = _api.IsConnected(),
            Identity             = identity,
            SelectedTenantId     = tenantId,
            SelectedCampusId     = campusId,
            SelectedDepartmentId = departmentId,
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
            var effectiveTenantId = sessionId?.IsSuperAdmin == true ? model.SelectedTenantId : sessionId?.TenantId;
            var effectiveCampusId = sessionId?.IsSuperAdmin == true ? model.SelectedCampusId : sessionId?.CampusId;

            if (sessionId?.IsSuperAdmin != true)
            {
                model.SelectedTenantId = effectiveTenantId;
                model.SelectedCampusId = effectiveCampusId;
            }

            if (sessionId?.IsSuperAdmin == true)
            {
                model.Tenants = await _api.GetTenantsAsync(ct);
                if (model.SelectedTenantId.HasValue)
                    model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);

                if (model.SelectedCampusId.HasValue && !model.Campuses.Any(c => c.Id == model.SelectedCampusId.Value))
                {
                    model.SelectedCampusId = null;
                    model.SelectedDepartmentId = null;
                    model.SelectedOfferingId = null;
                }
            }

            if (sessionId?.IsAdmin == true || sessionId?.IsSuperAdmin == true)
            {
                if (effectiveCampusId.HasValue)
                {
                    model.Departments = (await _api.GetDepartmentsAsync(effectiveTenantId, effectiveCampusId, ct))
                        .OrderBy(d => d.Name)
                        .Select(d => new LookupItem { Id = d.Id, Name = d.Name })
                        .ToList();
                }

                if (model.SelectedDepartmentId.HasValue && !model.Departments.Any(d => d.Id == model.SelectedDepartmentId.Value))
                {
                    model.SelectedDepartmentId = null;
                    model.SelectedOfferingId = null;
                }
            }

            if (sessionId?.IsStudent == true)
            {
                var allOfferings = await GetOfferingFilterOptionsAsync(sessionId, ct, effectiveTenantId, effectiveCampusId, model.SelectedDepartmentId);
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

                model.CourseOfferings = await GetOfferingFilterOptionsAsync(sessionId, ct, effectiveTenantId, effectiveCampusId, model.SelectedDepartmentId);
                if (!model.CourseOfferings.Any(o => o.Id == offeringId.Value))
                {
                    model.SelectedOfferingId = null;
                    model.Assignments.Clear();
                    model.Submissions.Clear();
                }
                model.SemesterOptions = BuildSemesterOptions(model.CourseOfferings);
            }
            else
            {
                if (sessionId?.IsAdmin == true || sessionId?.IsSuperAdmin == true)
                {
                    model.CourseOfferings = model.SelectedDepartmentId.HasValue
                        ? await GetOfferingFilterOptionsAsync(sessionId, ct, effectiveTenantId, effectiveCampusId, model.SelectedDepartmentId)
                        : new List<LookupItem>();
                }
                else
                {
                    model.CourseOfferings = await GetOfferingFilterOptionsAsync(sessionId, ct, effectiveTenantId, effectiveCampusId, model.SelectedDepartmentId);
                }

                model.SemesterOptions = BuildSemesterOptions(model.CourseOfferings);
            }
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View(model);
    }

    // ── Attendance ─────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Attendance(Guid? offeringId, Guid? tenantId, Guid? campusId, Guid? departmentId, Guid? courseId, Guid? studentId, string? semesterName, string? reportToken, CancellationToken ct)
        => await RenderAttendanceAsync(offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName, reportToken, nameof(Attendance), "Attendance", ct);

    [HttpGet]
    public async Task<IActionResult> EnterAttendance(Guid? offeringId, Guid? tenantId, Guid? campusId, Guid? departmentId, Guid? courseId, Guid? studentId, string? semesterName, string? reportToken, CancellationToken ct)
        => await RenderAttendanceAsync(offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName, reportToken, nameof(EnterAttendance), "Enter Attendance", ct);

    private async Task<IActionResult> RenderAttendanceAsync(
        Guid? offeringId,
        Guid? tenantId,
        Guid? campusId,
        Guid? departmentId,
        Guid? courseId,
        Guid? studentId,
        string? semesterName,
        string? reportToken,
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
            SelectedDepartmentId = departmentId,
            SelectedCourseId = courseId,
            SelectedStudentId = studentId,
            SelectedSemesterName = semesterName,
            ImportReportToken = reportToken,
            Message          = TempData["PortalMessage"]?.ToString()
        };
        var messageDetailsRaw = TempData["PortalMessageDetails"]?.ToString();
        if (!string.IsNullOrWhiteSpace(messageDetailsRaw))
        {
            model.MessageDetails = messageDetailsRaw
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }
        if (!model.IsConnected) return View(model);
        try
        {
            var sessionId = _api.GetSessionIdentity();
            var effectiveTenantId = sessionId?.IsSuperAdmin == true ? model.SelectedTenantId : sessionId?.TenantId;
            var effectiveCampusId = sessionId?.IsSuperAdmin == true ? model.SelectedCampusId : sessionId?.CampusId;

            if (sessionId?.IsSuperAdmin == true
                && (effectiveTenantId.HasValue ^ effectiveCampusId.HasValue))
            {
                model.Message = string.IsNullOrWhiteSpace(model.Message)
                    ? "Select both tenant and campus to apply scoped attendance filters."
                    : model.Message;
                effectiveTenantId = null;
                effectiveCampusId = null;
            }

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
            else
            {
                var departments = await _api.GetDepartmentsAsync(effectiveTenantId, effectiveCampusId, ct);
                model.Departments = departments
                    .OrderBy(d => d.Name)
                    .Select(d => new LookupItem { Id = d.Id, Name = d.Name })
                    .ToList();

                if (model.SelectedDepartmentId.HasValue && !model.Departments.Any(d => d.Id == model.SelectedDepartmentId.Value))
                    model.SelectedDepartmentId = null;

                var allOfferings = await _api.GetCourseOfferingsAsync(model.SelectedDepartmentId, effectiveTenantId, effectiveCampusId, null, ct);

                model.Courses = allOfferings
                    .GroupBy(o => o.CourseId)
                    .Select(g => g.First())
                    .OrderBy(o => o.CourseCode)
                    .ThenBy(o => o.CourseTitle)
                    .Select(o => new LookupItem
                    {
                        Id = o.CourseId,
                        Name = string.IsNullOrWhiteSpace(o.CourseCode) ? o.CourseTitle : $"{o.CourseCode} - {o.CourseTitle}"
                    })
                    .ToList();

                if (model.SelectedCourseId.HasValue && !model.Courses.Any(c => c.Id == model.SelectedCourseId.Value))
                    model.SelectedCourseId = null;

                var courseFilteredOfferings = model.SelectedCourseId.HasValue
                    ? allOfferings.Where(o => o.CourseId == model.SelectedCourseId.Value).ToList()
                    : allOfferings;

                model.SemesterOptions = courseFilteredOfferings
                    .Select(o => o.SemesterName)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s)
                    .Select(s => new LookupItem { Id = Guid.Empty, Name = s! })
                    .ToList();

                if (!string.IsNullOrWhiteSpace(model.SelectedSemesterName)
                    && !model.SemesterOptions.Any(s => string.Equals(s.Name, model.SelectedSemesterName, StringComparison.OrdinalIgnoreCase)))
                {
                    model.SelectedSemesterName = null;
                }

                var semesterFilteredOfferings = !string.IsNullOrWhiteSpace(model.SelectedSemesterName)
                    ? courseFilteredOfferings.Where(o => string.Equals(o.SemesterName, model.SelectedSemesterName, StringComparison.OrdinalIgnoreCase)).ToList()
                    : courseFilteredOfferings;

                model.CourseOfferings = semesterFilteredOfferings
                    .OrderBy(o => o.CourseCode)
                    .ThenBy(o => o.CourseTitle)
                    .ThenBy(o => o.SemesterName)
                    .Select(o => new LookupItem
                    {
                        Id = o.Id,
                        Name = string.IsNullOrWhiteSpace(o.CourseCode)
                            ? $"{o.CourseTitle} ({o.SemesterName})"
                            : $"{o.CourseCode} - {o.CourseTitle} ({o.SemesterName})"
                    })
                    .ToList();

                if (model.SelectedOfferingId.HasValue && !model.CourseOfferings.Any(o => o.Id == model.SelectedOfferingId.Value))
                {
                    model.SelectedOfferingId = null;
                    model.Message = string.IsNullOrWhiteSpace(model.Message)
                        ? "Selected offering is not valid for the current filter context."
                        : model.Message;
                }

                if (model.SelectedOfferingId.HasValue)
                {
                    model.Records = await _api.GetAttendanceByOfferingAsync(model.SelectedOfferingId.Value, effectiveTenantId, effectiveCampusId, ct);
                    model.Roster = await _api.GetEnrollmentRosterAsync(model.SelectedOfferingId.Value, effectiveTenantId, effectiveCampusId, ct);

                    model.Students = model.Roster
                        .OrderBy(s => s.StudentName)
                        .Select(s => new LookupItem
                        {
                            Id = s.StudentProfileId,
                            Name = string.IsNullOrWhiteSpace(s.RegistrationNumber)
                                ? s.StudentName
                                : $"{s.RegistrationNumber} - {s.StudentName}"
                        })
                        .ToList();

                    if (model.SelectedStudentId.HasValue && !model.Students.Any(s => s.Id == model.SelectedStudentId.Value))
                        model.SelectedStudentId = null;

                    if (model.SelectedStudentId.HasValue)
                    {
                        model.Records = model.Records
                            .Where(r => r.StudentProfileId == model.SelectedStudentId.Value)
                            .ToList();

                        model.Roster = model.Roster
                            .Where(r => r.StudentProfileId == model.SelectedStudentId.Value)
                            .ToList();
                    }

                    var selectedOffering = semesterFilteredOfferings.FirstOrDefault(o => o.Id == model.SelectedOfferingId.Value);

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
                                CourseName = selectedOffering is null
                                    ? string.Empty
                                    : string.IsNullOrWhiteSpace(selectedOffering.CourseCode)
                                        ? selectedOffering.CourseTitle
                                        : $"{selectedOffering.CourseCode} - {selectedOffering.CourseTitle}",
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
        }
        catch (Exception ex) { model.Message = ex.Message; }
        return View("Attendance", model);
    }

    // ── Results ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Results(
        Guid? offeringId,
        string? semesterName,
        Guid? tenantId,
        Guid? campusId,
        string? reportToken,
        Guid? departmentId,
        Guid? courseId,
        Guid? subjectOfferingId,
        string? examType,
        string? assessmentComponent,
        Guid? studentId,
        string? section,
        string? batch,
        CancellationToken ct)
        => await RenderResultsAsync(
            offeringId,
            semesterName,
            tenantId,
            campusId,
            reportToken,
            departmentId,
            courseId,
            subjectOfferingId,
            examType,
            assessmentComponent,
            studentId,
            section,
            batch,
            nameof(Results),
            "Results",
            ct);

    [HttpGet]
    public async Task<IActionResult> EnterResults(
        Guid? offeringId,
        string? semesterName,
        Guid? tenantId,
        Guid? campusId,
        string? reportToken,
        Guid? departmentId,
        Guid? courseId,
        Guid? subjectOfferingId,
        string? examType,
        string? assessmentComponent,
        Guid? studentId,
        string? section,
        string? batch,
        CancellationToken ct)
        => await RenderResultsAsync(
            offeringId,
            semesterName,
            tenantId,
            campusId,
            reportToken,
            departmentId,
            courseId,
            subjectOfferingId,
            examType,
            assessmentComponent,
            studentId,
            section,
            batch,
            nameof(EnterResults),
            "Enter Results",
            ct);

    private async Task<IActionResult> RenderResultsAsync(
        Guid? offeringId,
        string? semesterName,
        Guid? tenantId,
        Guid? campusId,
        string? reportToken,
        Guid? departmentId,
        Guid? courseId,
        Guid? subjectOfferingId,
        string? examType,
        string? assessmentComponent,
        Guid? studentId,
        string? section,
        string? batch,
        string resultsAction,
        string title,
        CancellationToken ct)
    {
        ViewData["Title"] = title;
        ViewData["ResultsAction"] = resultsAction;

        var identity = _api.GetSessionIdentity();
        var model = new ResultsPageModel
        {
            IsConnected = _api.IsConnected(),
            Identity = identity,
            SelectedOfferingId = offeringId,
            SelectedSemesterName = semesterName,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId,
            ImportReportToken = reportToken,
            SelectedDepartmentId = departmentId,
            SelectedCourseId = courseId,
            SelectedSubjectOfferingId = subjectOfferingId,
            SelectedExamType = examType,
            SelectedAssessmentComponent = assessmentComponent,
            SelectedStudentId = studentId,
            SelectedSection = section,
            SelectedBatch = batch,
            Message = TempData["PortalMessage"]?.ToString()
        };

        var messageDetailsRaw = TempData["PortalMessageDetails"]?.ToString();
        if (!string.IsNullOrWhiteSpace(messageDetailsRaw))
        {
            model.MessageDetails = messageDetailsRaw
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        model.ExamTypeOptions = BuildResultExamTypeOptions();

        if (!model.IsConnected)
            return View("Results", model);

        try
        {
            var effectiveTenantId = identity?.IsSuperAdmin == true ? model.SelectedTenantId : identity?.TenantId;
            var effectiveCampusId = identity?.IsSuperAdmin == true ? model.SelectedCampusId : identity?.CampusId;

            var hasPartialSuperAdminScope = identity?.IsSuperAdmin == true
                && (model.SelectedTenantId.HasValue ^ model.SelectedCampusId.HasValue);

            if (hasPartialSuperAdminScope)
            {
                // API scope contracts require tenant/campus to be provided together.
                effectiveTenantId = null;
                effectiveCampusId = null;
                model.Message = "Select both tenant and campus to apply scoped quiz filters.";
            }

            if (identity?.IsSuperAdmin == true)
            {
                model.Tenants = await _api.GetTenantsAsync(ct);
                if (model.SelectedTenantId.HasValue)
                    model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);
            }

            if (identity?.IsStudent == true)
            {
                var allOfferings = await GetOfferingFilterOptionsAsync(identity, ct, effectiveTenantId, effectiveCampusId);
                model.SemesterOptions = BuildSemesterOptions(allOfferings);
                model.Offerings = FilterOfferingsBySemester(allOfferings, semesterName);

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

                if (!string.IsNullOrWhiteSpace(model.SelectedExamType))
                {
                    model.Results = model.Results
                        .Where(r => string.Equals(r.ResultType, model.SelectedExamType, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

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
            else
            {
                model.Departments = (await _api.GetDepartmentsAsync(effectiveTenantId, effectiveCampusId, ct))
                    .OrderBy(d => d.Name)
                    .Select(d => new LookupItem { Id = d.Id, Name = d.Name })
                    .ToList();

                if (model.SelectedDepartmentId.HasValue && !model.Departments.Any(d => d.Id == model.SelectedDepartmentId.Value))
                    model.SelectedDepartmentId = null;

                var allOfferings = await _api.GetCourseOfferingsAsync(model.SelectedDepartmentId, effectiveTenantId, effectiveCampusId, null, ct);

                model.Courses = allOfferings
                    .GroupBy(o => o.CourseId)
                    .Select(g => g.First())
                    .OrderBy(o => o.CourseCode)
                    .ThenBy(o => o.CourseTitle)
                    .Select(o => new LookupItem
                    {
                        Id = o.CourseId,
                        Name = string.IsNullOrWhiteSpace(o.CourseCode) ? o.CourseTitle : $"{o.CourseCode} - {o.CourseTitle}"
                    })
                    .ToList();

                if (model.SelectedCourseId.HasValue && !model.Courses.Any(c => c.Id == model.SelectedCourseId.Value))
                    model.SelectedCourseId = null;

                var courseFilteredOfferings = model.SelectedCourseId.HasValue
                    ? allOfferings.Where(o => o.CourseId == model.SelectedCourseId.Value).ToList()
                    : allOfferings;

                model.SemesterOptions = courseFilteredOfferings
                    .Select(o => o.SemesterName)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s)
                    .Select(s => new LookupItem { Id = Guid.Empty, Name = s! })
                    .ToList();

                if (!string.IsNullOrWhiteSpace(model.SelectedSemesterName)
                    && !model.SemesterOptions.Any(s => string.Equals(s.Name, model.SelectedSemesterName, StringComparison.OrdinalIgnoreCase)))
                {
                    model.SelectedSemesterName = null;
                }

                var semesterFilteredOfferings = !string.IsNullOrWhiteSpace(model.SelectedSemesterName)
                    ? courseFilteredOfferings.Where(o => string.Equals(o.SemesterName, model.SelectedSemesterName, StringComparison.OrdinalIgnoreCase)).ToList()
                    : courseFilteredOfferings;

                model.Subjects = semesterFilteredOfferings
                    .OrderBy(o => o.CourseCode)
                    .ThenBy(o => o.CourseTitle)
                    .Select(o => new LookupItem
                    {
                        Id = o.Id,
                        Name = string.IsNullOrWhiteSpace(o.CourseCode)
                            ? o.CourseTitle
                            : $"{o.CourseCode} - {o.CourseTitle}"
                    })
                    .ToList();

                if (model.SelectedSubjectOfferingId.HasValue && !model.Subjects.Any(s => s.Id == model.SelectedSubjectOfferingId.Value))
                    model.SelectedSubjectOfferingId = null;

                if (!model.SelectedOfferingId.HasValue && model.SelectedSubjectOfferingId.HasValue)
                    model.SelectedOfferingId = model.SelectedSubjectOfferingId;

                if (!model.SelectedSubjectOfferingId.HasValue && model.SelectedOfferingId.HasValue)
                    model.SelectedSubjectOfferingId = model.SelectedOfferingId;

                model.Offerings = semesterFilteredOfferings
                    .OrderBy(o => o.CourseCode)
                    .ThenBy(o => o.CourseTitle)
                    .ThenBy(o => o.SemesterName)
                    .Select(o => new LookupItem
                    {
                        Id = o.Id,
                        Name = string.IsNullOrWhiteSpace(o.CourseCode)
                            ? $"{o.CourseTitle} ({o.SemesterName})"
                            : $"{o.CourseCode} - {o.CourseTitle} ({o.SemesterName})"
                    })
                    .ToList();

                if (model.SelectedOfferingId.HasValue && !model.Offerings.Any(o => o.Id == model.SelectedOfferingId.Value))
                {
                    model.SelectedOfferingId = null;
                    model.Message = string.IsNullOrWhiteSpace(model.Message)
                        ? "Selected offering is not valid for the current filter context."
                        : model.Message;
                }

                if (!string.IsNullOrWhiteSpace(model.SelectedExamType)
                    && !model.ExamTypeOptions.Any(x => string.Equals(x.Name, model.SelectedExamType, StringComparison.OrdinalIgnoreCase)))
                {
                    model.SelectedExamType = null;
                }

                model.AssessmentComponentOptions = BuildAssessmentComponentOptions(model.SelectedExamType);
                if (!string.IsNullOrWhiteSpace(model.SelectedAssessmentComponent)
                    && !model.AssessmentComponentOptions.Any(x => string.Equals(x.Name, model.SelectedAssessmentComponent, StringComparison.OrdinalIgnoreCase)))
                {
                    model.SelectedAssessmentComponent = null;
                }

                if (model.SelectedOfferingId.HasValue)
                {
                    model.Results = await _api.GetResultsByOfferingAsync(model.SelectedOfferingId.Value, effectiveTenantId, effectiveCampusId, ct);
                    model.Roster = await _api.GetEnrollmentRosterAsync(model.SelectedOfferingId.Value, effectiveTenantId, effectiveCampusId, ct);

                    if (model.SelectedStudentId.HasValue)
                    {
                        model.Results = model.Results
                            .Where(r => r.StudentProfileId == model.SelectedStudentId.Value)
                            .ToList();
                    }

                    if (!string.IsNullOrWhiteSpace(model.SelectedExamType))
                    {
                        model.Results = model.Results
                            .Where(r => string.Equals(r.ResultType, model.SelectedExamType, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }
                }
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
        catch (Exception ex)
        {
            model.Message = ex.Message;
        }

        return View("Results", model);
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

            var hasPartialSuperAdminScope = identity?.IsSuperAdmin == true
                && (model.SelectedTenantId.HasValue ^ model.SelectedCampusId.HasValue);

            if (hasPartialSuperAdminScope)
            {
                effectiveTenantId = null;
                effectiveCampusId = null;
                model.Message = "Select both tenant and campus to apply scoped quiz filters.";
            }

            if (identity?.IsSuperAdmin == true)
            {
                model.Tenants = await _api.GetTenantsAsync(ct);
                if (model.SelectedTenantId.HasValue)
                    model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);
            }

            var allOfferings = await GetOfferingFilterOptionsAsync(identity, ct, effectiveTenantId, effectiveCampusId);
            model.SemesterOptions = BuildSemesterOptions(allOfferings);
            model.CourseOfferings = FilterOfferingsBySemester(allOfferings, semesterName);

            if (offeringId.HasValue && !hasPartialSuperAdminScope)
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
        Guid? tenantId, Guid? campusId, Guid? departmentId,
        DateTime dueDate, decimal maxMarks, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.CreateAssignmentAsync(offeringId, title, description, dueDate, maxMarks, tenantId, campusId, ct); }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Assignments), new { offeringId, tenantId, campusId, departmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAssignment(
        Guid id, Guid offeringId, string title, string? description,
        Guid? tenantId, Guid? campusId, Guid? departmentId,
        DateTime dueDate, decimal maxMarks, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.UpdateAssignmentAsync(id, title, description, dueDate, maxMarks, tenantId, campusId, ct); TempData["PortalMessage"] = "Assignment updated."; }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Assignments), new { offeringId, tenantId, campusId, departmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishAssignment(Guid id, Guid? offeringId, Guid? tenantId, Guid? campusId, Guid? departmentId, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try { await _api.PublishAssignmentAsync(id, tenantId, campusId, ct); }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(nameof(Assignments), new { offeringId, tenantId, campusId, departmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAssignmentActive(Guid id, bool activate, Guid? offeringId, Guid? tenantId, Guid? campusId, Guid? departmentId, bool includeInactive = true, CancellationToken ct = default)
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
        return RedirectToAction(nameof(Assignments), new { offeringId, tenantId, campusId, departmentId, includeInactive });
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
    public async Task<IActionResult> DownloadAttendanceCsvTemplate(Guid? offeringId, Guid? tenantId, Guid? campusId, Guid? departmentId, Guid? courseId, Guid? studentId, string? semesterName, string? entryPoint, CancellationToken ct)
    {
        if (!_api.IsConnected())
        {
            TempData["PortalMessage"] = "Connect to the API before downloading the attendance template.";
            return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
        }

        var identity = _api.GetSessionIdentity();
        if (identity is null || !(identity.IsFaculty || identity.IsAdmin || identity.IsSuperAdmin))
            return Forbid();

        var effectiveTenantId = identity.IsSuperAdmin ? tenantId : identity.TenantId;
        var effectiveCampusId = identity.IsSuperAdmin ? campusId : identity.CampusId;

        var rows = new List<(Guid StudentId, string StudentName)>();
        if (offeringId.HasValue)
        {
            var scopeValidation = await ValidateAttendanceWriteScopeAsync(offeringId.Value, departmentId, courseId, semesterName, effectiveTenantId, effectiveCampusId, identity, ct);
            if (!scopeValidation.Allowed)
            {
                TempData["PortalMessage"] = scopeValidation.Message;
                return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
            }

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

    [HttpGet]
    public IActionResult DownloadAttendanceImportReport(
        string? reportToken,
        Guid? offeringId,
        Guid? tenantId,
        Guid? campusId,
        Guid? departmentId,
        Guid? courseId,
        Guid? studentId,
        string? semesterName,
        string? entryPoint)
    {
        if (string.IsNullOrWhiteSpace(reportToken) || !AttendanceImportReports.TryRemove(reportToken, out var payload))
        {
            TempData["PortalMessage"] = "Import report is not available. Please run CSV import again.";
            return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
        }

        if (payload.CreatedAtUtc + AttendanceImportReportTtl < UtcNowProvider())
        {
            TempData["PortalMessage"] = "Import report has expired. Please run CSV import again.";
            return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
        }

        var bytes = Encoding.UTF8.GetBytes(payload.CsvContent);
        return File(bytes, "text/csv", payload.FileName);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportAttendanceCsv(
        Guid? offeringId,
        Guid? tenantId,
        Guid? campusId,
        Guid? departmentId,
        Guid? courseId,
        Guid? studentId,
        string? semesterName,
        string? entryPoint,
        IFormFile? csvFile,
        CancellationToken ct,
        bool strictMode = true)
    {
        var importAuditErrorDetails = new List<string>();
        var totalRows = 0;
        var importedRows = 0;
        var reportRows = new List<AttendanceImportReportRow>();
        var pendingRows = new List<AttendanceImportReportRow>();

        void CleanupExpiredImportReports()
        {
            var threshold = UtcNowProvider().Subtract(AttendanceImportReportTtl);
            foreach (var item in AttendanceImportReports)
            {
                if (item.Value.CreatedAtUtc < threshold)
                    AttendanceImportReports.TryRemove(item.Key, out _);
            }
        }

        string StoreImportReportAndGetToken()
        {
            var csv = new StringBuilder();
            csv.AppendLine("RowNumber,StudentId,StudentName,Date,Present,Outcome,Reason");
            foreach (var row in reportRows.OrderBy(r => r.RowNumber))
            {
                csv.AppendLine(
                    $"{row.RowNumber},{EscapeCsvField(row.StudentId)},{EscapeCsvField(row.StudentName)},{EscapeCsvField(row.DateValue)},{EscapeCsvField(row.PresentValue)},{EscapeCsvField(row.Outcome)},{EscapeCsvField(row.Reason)}");
            }

            var token = Guid.NewGuid().ToString("N");
            AttendanceImportReports[token] = new AttendanceImportReportPayload(
                $"attendance-import-report-{UtcNowProvider():yyyyMMdd-HHmmss}.csv",
                csv.ToString(),
                UtcNowProvider());
            return token;
        }

        IActionResult RedirectWithContext(string? reportToken = null)
            => RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName, reportToken });

        void WriteAttendanceImportAudit(string outcome, SessionIdentity? actor, IReadOnlyCollection<string>? errorDetails = null)
        {
            var details = errorDetails?.Where(e => !string.IsNullOrWhiteSpace(e)).Take(5).ToList() ?? new List<string>();
            var auditLine = $"outcome={outcome}; uploadedBy={actor?.UserName ?? actor?.Email ?? "unknown"}; uploadedAtUtc={DateTime.UtcNow:O}; strictMode={strictMode}; offeringId={offeringId?.ToString() ?? "none"}; totalRows={totalRows}; importedRows={importedRows}; skippedRows={Math.Max(totalRows - importedRows, 0)}; errors={string.Join(" | ", details)}";

            TempData["PortalImportAudit"] = auditLine;
            _logger.LogInformation("Attendance CSV import audit {AuditLine}", auditLine);
        }

        CleanupExpiredImportReports();

        if (!_api.IsConnected())
        {
            TempData["PortalMessage"] = "Connect to the API before importing attendance CSV.";
            WriteAttendanceImportAudit("blocked-not-connected", actor: null);
            return RedirectWithContext();
        }

        var identity = _api.GetSessionIdentity();
        if (identity is null || !(identity.IsFaculty || identity.IsAdmin || identity.IsSuperAdmin))
        {
            WriteAttendanceImportAudit("blocked-forbidden", identity);
            return Forbid();
        }

        if (!offeringId.HasValue)
        {
            TempData["PortalMessage"] = "Select a course offering before importing attendance CSV.";
            WriteAttendanceImportAudit("blocked-missing-offering", identity);
            return RedirectWithContext();
        }

        if (csvFile is null || csvFile.Length == 0)
        {
            TempData["PortalMessage"] = "Please choose a non-empty CSV file.";
            WriteAttendanceImportAudit("blocked-empty-file", identity);
            return RedirectWithContext();
        }

        if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            TempData["PortalMessage"] = "Invalid file type. Only .csv files are accepted.";
            WriteAttendanceImportAudit("blocked-invalid-file-type", identity);
            return RedirectWithContext();
        }

        var effectiveTenantId = identity.IsSuperAdmin ? tenantId : identity.TenantId;
        var effectiveCampusId = identity.IsSuperAdmin ? campusId : identity.CampusId;

        var scopeValidation = await ValidateAttendanceWriteScopeAsync(offeringId.Value, departmentId, courseId, semesterName, effectiveTenantId, effectiveCampusId, identity, ct);
        if (!scopeValidation.Allowed)
        {
            TempData["PortalMessage"] = scopeValidation.Message;
            WriteAttendanceImportAudit("blocked-scope-validation", identity, new List<string> { scopeValidation.Message });
            return RedirectWithContext();
        }

        List<EnrollmentRosterItem> roster;
        try
        {
            roster = await _api.GetEnrollmentRosterAsync(offeringId.Value, effectiveTenantId, effectiveCampusId, ct);
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = $"Unable to load roster for selected offering: {ex.Message}";
            WriteAttendanceImportAudit("failed-roster-load", identity, new List<string> { ex.Message });
            return RedirectWithContext();
        }

        var rosterIds = roster.Select(r => r.Id).ToHashSet();
        if (rosterIds.Count == 0)
        {
            TempData["PortalMessage"] = "No students found in the selected offering roster.";
            WriteAttendanceImportAudit("blocked-empty-roster", identity);
            return RedirectWithContext();
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
                WriteAttendanceImportAudit("blocked-empty-header", identity);
                return RedirectWithContext();
            }

            var header = ParseCsvLine(headerLine);
            var expectedHeader = new[] { "StudentId", "StudentName", "Date", "Present" };
            if (header.Length != expectedHeader.Length || !header.Select(h => h.Trim()).SequenceEqual(expectedHeader, StringComparer.OrdinalIgnoreCase))
            {
                TempData["PortalMessage"] = "CSV header does not match the template. Expected: StudentId,StudentName,Date,Present.";
                WriteAttendanceImportAudit("blocked-invalid-header", identity, new List<string> { "CSV header does not match expected template." });
                return RedirectWithContext();
            }

            var rowNumber = 1;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                rowNumber++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                totalRows++;

                var cols = ParseCsvLine(line);
                if (cols.Length != 4)
                {
                    validationErrors.Add($"Row {rowNumber}: expected 4 columns.");
                    reportRows.Add(new AttendanceImportReportRow
                    {
                        RowNumber = rowNumber,
                        StudentId = cols.ElementAtOrDefault(0)?.Trim() ?? string.Empty,
                        StudentName = cols.ElementAtOrDefault(1)?.Trim() ?? string.Empty,
                        DateValue = cols.ElementAtOrDefault(2)?.Trim() ?? string.Empty,
                        PresentValue = cols.ElementAtOrDefault(3)?.Trim() ?? string.Empty,
                        Outcome = "Skipped",
                        Reason = "Expected 4 columns."
                    });
                    continue;
                }

                var studentIdRaw = cols[0].Trim();
                var studentNameRaw = cols[1].Trim();
                var dateRaw = cols[2].Trim();
                var presentRaw = cols[3].Trim();

                var reportRow = new AttendanceImportReportRow
                {
                    RowNumber = rowNumber,
                    StudentId = studentIdRaw,
                    StudentName = studentNameRaw,
                    DateValue = dateRaw,
                    PresentValue = presentRaw,
                    Outcome = "Pending"
                };
                reportRows.Add(reportRow);

                if (string.IsNullOrWhiteSpace(studentIdRaw) || string.IsNullOrWhiteSpace(studentNameRaw) || string.IsNullOrWhiteSpace(dateRaw) || string.IsNullOrWhiteSpace(presentRaw))
                {
                    validationErrors.Add($"Row {rowNumber}: required fields must not be empty.");
                    reportRow.Outcome = "Skipped";
                    reportRow.Reason = "Required fields must not be empty.";
                    continue;
                }

                if (!Guid.TryParse(studentIdRaw, out var rowStudentId))
                {
                    validationErrors.Add($"Row {rowNumber}: StudentId is invalid.");
                    reportRow.Outcome = "Skipped";
                    reportRow.Reason = "StudentId is invalid.";
                    continue;
                }

                if (!rosterIds.Contains(rowStudentId))
                {
                    validationErrors.Add($"Row {rowNumber}: StudentId does not belong to the selected offering roster.");
                    reportRow.Outcome = "Skipped";
                    reportRow.Reason = "StudentId does not belong to the selected offering roster.";
                    continue;
                }

                if (!TryParseAttendanceDate(dateRaw, out var date))
                {
                    validationErrors.Add($"Row {rowNumber}: Date must be a valid value (for example yyyy-MM-dd).");
                    reportRow.Outcome = "Skipped";
                    reportRow.Reason = "Date must be a valid value (for example yyyy-MM-dd).";
                    continue;
                }

                if (!TryParsePresentFlag(presentRaw, out var isPresent))
                {
                    validationErrors.Add($"Row {rowNumber}: Present must be true/false, yes/no, 1/0, or present/absent.");
                    reportRow.Outcome = "Skipped";
                    reportRow.Reason = "Present must be true/false, yes/no, 1/0, or present/absent.";
                    continue;
                }

                var duplicateKey = $"{rowStudentId:D}|{date:yyyy-MM-dd}";
                if (!seenStudentDate.Add(duplicateKey))
                {
                    validationErrors.Add($"Row {rowNumber}: duplicate StudentId + Date entry in import file.");
                    reportRow.Outcome = "Skipped";
                    reportRow.Reason = "Duplicate StudentId + Date entry in import file.";
                    continue;
                }

                parsedRows.Add((rowStudentId, date, isPresent ? "Present" : "Absent"));
                pendingRows.Add(reportRow);
            }
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = $"Unable to read CSV file: {ex.Message}";
            WriteAttendanceImportAudit("failed-file-read", identity, new List<string> { ex.Message });
            return RedirectWithContext();
        }

        importAuditErrorDetails = validationErrors.Take(5).ToList();

        if (validationErrors.Count > 0)
        {
            if (strictMode)
            {
                foreach (var row in pendingRows)
                {
                    row.Outcome = "Skipped";
                    row.Reason = "Strict mode rejected the import because other rows are invalid.";
                }

                var reportToken = reportRows.Count > 0 ? StoreImportReportAndGetToken() : null;
                TempData["PortalMessage"] = $"CSV validation failed in strict mode. Invalid rows: {validationErrors.Count}.";
                TempData["PortalMessageDetails"] = string.Join('\n', validationErrors.Take(20));
                WriteAttendanceImportAudit("failed-strict-validation", identity, importAuditErrorDetails);
                return RedirectWithContext(reportToken);
            }
        }

        if (parsedRows.Count == 0)
        {
            TempData["PortalMessage"] = "CSV file has no valid attendance rows to import.";
            if (validationErrors.Count > 0)
                TempData["PortalMessageDetails"] = string.Join('\n', validationErrors.Take(20));
            WriteAttendanceImportAudit("failed-no-valid-rows", identity, importAuditErrorDetails);
            var reportToken = reportRows.Count > 0 ? StoreImportReportAndGetToken() : null;
            return RedirectWithContext(reportToken);
        }

        string? finalReportToken;
        try
        {
            foreach (var byDate in parsedRows.GroupBy(r => r.Date.Date))
            {
                var entries = byDate.Select(r => (r.StudentId, r.Status));
                await _api.BulkMarkAttendanceAsync(offeringId.Value, byDate.Key, entries, effectiveTenantId, effectiveCampusId, ct);
                importedRows += byDate.Count();
            }

            foreach (var row in pendingRows)
            {
                row.Outcome = "Imported";
                row.Reason = string.Empty;
            }

            if (validationErrors.Count > 0)
            {
                TempData["PortalMessage"] = $"Attendance CSV import completed with warnings. Imported rows: {parsedRows.Count}. Skipped rows: {validationErrors.Count}.";
                TempData["PortalMessageDetails"] = string.Join('\n', validationErrors.Take(20));
            }
            else
            {
                TempData["PortalMessage"] = $"Attendance CSV import processed successfully. Rows processed: {parsedRows.Count}.";
            }

            WriteAttendanceImportAudit(validationErrors.Count > 0 ? "succeeded-with-warnings" : "succeeded", identity, importAuditErrorDetails);
            finalReportToken = StoreImportReportAndGetToken();
        }
        catch (Exception ex)
        {
            foreach (var row in pendingRows.Where(r => string.Equals(r.Outcome, "Pending", StringComparison.OrdinalIgnoreCase)))
            {
                row.Outcome = "Failed";
                row.Reason = ex.Message;
            }

            TempData["PortalMessage"] = $"Attendance CSV import failed: {ex.Message}";
            WriteAttendanceImportAudit("failed-bulk-import", identity, new List<string> { ex.Message });
            finalReportToken = reportRows.Count > 0 ? StoreImportReportAndGetToken() : null;
        }

        return RedirectWithContext(finalReportToken);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkMarkAttendance(
        Guid offeringId, DateTime? date,
        [FromForm] Guid[] studentIds, [FromForm] string[] statuses,
        [FromForm] DateTime[]? dates,
        Guid? tenantId, Guid? campusId, Guid? departmentId, Guid? courseId, Guid? studentId, string? semesterName, string? entryPoint,
        CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                if (studentIds.Length == 0)
                {
                    TempData["PortalMessage"] = "No attendance rows were submitted.";
                    return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
                }

                var sessionId = _api.GetSessionIdentity();
                var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
                var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;

                var scopeValidation = await ValidateAttendanceWriteScopeAsync(offeringId, departmentId, courseId, semesterName, effectiveTenantId, effectiveCampusId, sessionId, ct);
                if (!scopeValidation.Allowed)
                {
                    TempData["PortalMessage"] = scopeValidation.Message;
                    return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
                }

                if (studentIds.Length != statuses.Length)
                {
                    TempData["PortalMessage"] = "Invalid attendance submission: student and status counts do not match.";
                    return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
                }

                var hasPerRowDates = dates is { Length: > 0 };
                if (hasPerRowDates && dates!.Length != studentIds.Length)
                {
                    TempData["PortalMessage"] = "Invalid attendance submission: student and date counts do not match.";
                    return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
                }

                if (!hasPerRowDates && !date.HasValue)
                {
                    TempData["PortalMessage"] = "Attendance date is required.";
                    return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
                }

                var rosterIds = await GetRosterIdsAsync(offeringId, effectiveTenantId, effectiveCampusId, ct);
                var entriesByDate = new Dictionary<DateTime, List<(Guid StudentProfileId, string Status)>>();
                var invalidStudentIds = new List<Guid>();
                var duplicateKeys = new HashSet<(Guid StudentId, DateTime Date)>();

                for (var i = 0; i < studentIds.Length; i++)
                {
                    var rowStudentId = studentIds[i];
                    if (!rosterIds.Contains(rowStudentId))
                    {
                        invalidStudentIds.Add(rowStudentId);
                        continue;
                    }

                    if (!TryNormalizeAttendanceStatus(statuses[i], out var normalizedStatus))
                    {
                        TempData["PortalMessage"] = "Invalid attendance status. Allowed values: Present or Absent.";
                        return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
                    }

                    var entryDate = hasPerRowDates ? dates![i].Date : date!.Value.Date;
                    if (entryDate == default)
                    {
                        TempData["PortalMessage"] = "Attendance date is required for each row.";
                        return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
                    }

                    if (!duplicateKeys.Add((rowStudentId, entryDate)))
                    {
                        TempData["PortalMessage"] = "Duplicate attendance rows detected for the same student and date.";
                        return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
                    }

                    if (!entriesByDate.TryGetValue(entryDate, out var entries))
                    {
                        entries = new List<(Guid StudentProfileId, string Status)>();
                        entriesByDate[entryDate] = entries;
                    }

                    entries.Add((rowStudentId, normalizedStatus));
                }

                if (invalidStudentIds.Count > 0)
                {
                    TempData["PortalMessage"] = "Attendance submission contains students outside the selected offering roster.";
                    return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
                }

                if (entriesByDate.Count == 0)
                {
                    TempData["PortalMessage"] = "No valid attendance entries were submitted.";
                    return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
                }

                foreach (var batch in entriesByDate.OrderBy(x => x.Key))
                {
                    await _api.BulkMarkAttendanceAsync(offeringId, batch.Key, batch.Value, effectiveTenantId, effectiveCampusId, ct);
                }

                TempData["PortalMessage"] = "Attendance marked.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CorrectAttendance(
        Guid studentProfileId, Guid offeringId, DateTime date,
        string newStatus, string? remarks, Guid? tenantId, Guid? campusId, Guid? departmentId, Guid? courseId, Guid? studentId, string? semesterName, string? entryPoint, CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var sessionId = _api.GetSessionIdentity();
                var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
                var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;

                var scopeValidation = await ValidateAttendanceWriteScopeAsync(offeringId, departmentId, courseId, semesterName, effectiveTenantId, effectiveCampusId, sessionId, ct);
                if (!scopeValidation.Allowed)
                {
                    TempData["PortalMessage"] = scopeValidation.Message;
                    return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
                }

                if (!TryNormalizeAttendanceStatus(newStatus, out var normalizedStatus))
                {
                    TempData["PortalMessage"] = "Invalid attendance status. Allowed values: Present or Absent.";
                    return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
                }

                var rosterIds = await GetRosterIdsAsync(offeringId, effectiveTenantId, effectiveCampusId, ct);
                if (!rosterIds.Contains(studentProfileId))
                {
                    TempData["PortalMessage"] = "Attendance correction denied: selected student is not in the offering roster.";
                    return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
                }

                await _api.CorrectAttendanceAsync(studentProfileId, offeringId, date, normalizedStatus, remarks, effectiveTenantId, effectiveCampusId, ct);
                TempData["PortalMessage"] = "Attendance corrected.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(ResolveAttendanceEntryAction(entryPoint), new { offeringId, tenantId, campusId, departmentId, courseId, studentId, semesterName });
    }

    private static string ResolveAttendanceEntryAction(string? entryPoint)
        => string.Equals(entryPoint, nameof(EnterAttendance), StringComparison.OrdinalIgnoreCase)
            ? nameof(EnterAttendance)
            : nameof(Attendance);

    private async Task<HashSet<Guid>> GetRosterIdsAsync(Guid offeringId, Guid? tenantId, Guid? campusId, CancellationToken ct)
    {
        var roster = await _api.GetEnrollmentRosterAsync(offeringId, tenantId, campusId, ct);
        return roster.Select(r => r.Id).ToHashSet();
    }

    private async Task<(bool Allowed, string Message)> ValidateAttendanceWriteScopeAsync(
        Guid offeringId,
        Guid? departmentId,
        Guid? courseId,
        string? semesterName,
        Guid? effectiveTenantId,
        Guid? effectiveCampusId,
        SessionIdentity? identity,
        CancellationToken ct)
    {
        if (identity?.IsSuperAdmin == true && (!effectiveTenantId.HasValue || !effectiveCampusId.HasValue))
            return (false, "Select tenant and campus before performing attendance write operations.");

        if (!effectiveTenantId.HasValue || !effectiveCampusId.HasValue)
            return (false, "Tenant and campus scope is required for attendance write operations.");

        if (!departmentId.HasValue || !courseId.HasValue || string.IsNullOrWhiteSpace(semesterName))
            return (false, "Select department, course, and semester/class before performing attendance write operations.");

        var offerings = await _api.GetCourseOfferingsAsync(departmentId, effectiveTenantId, effectiveCampusId, null, ct);
        var selectedOffering = offerings.FirstOrDefault(o => o.Id == offeringId);
        if (selectedOffering is null)
            return (false, "Selected offering is not valid for the current filter scope.");

        if (selectedOffering.CourseId != courseId.Value)
            return (false, "Selected offering does not match the selected course.");

        if (!string.Equals(selectedOffering.SemesterName, semesterName, StringComparison.OrdinalIgnoreCase))
            return (false, "Selected offering does not match the selected semester/class.");

        return (true, string.Empty);
    }

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

    private static bool TryNormalizeAttendanceStatus(string input, out string normalizedStatus)
    {
        if (TryParsePresentFlag(input, out var isPresent))
        {
            normalizedStatus = isPresent ? "Present" : "Absent";
            return true;
        }

        normalizedStatus = string.Empty;
        return false;
    }

    private static string EscapeCsvField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return '"' + value.Replace("\"", "\"\"") + '"';

        return value;
    }

    private static readonly HashSet<Guid> ResultTemplateExampleStudentIds = new()
    {
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Guid.Parse("22222222-2222-2222-2222-222222222222")
    };

    private static List<LookupItem> BuildResultExamTypeOptions()
        => new()
        {
            new LookupItem { Id = Guid.Empty, Name = "Midterm" },
            new LookupItem { Id = Guid.Empty, Name = "Final" },
            new LookupItem { Id = Guid.Empty, Name = "Quiz" },
            new LookupItem { Id = Guid.Empty, Name = "Assignment" },
            new LookupItem { Id = Guid.Empty, Name = "Internal" }
        };

    private static List<LookupItem> BuildAssessmentComponentOptions(string? examType)
    {
        var options = string.Equals(examType, "Quiz", StringComparison.OrdinalIgnoreCase)
            ? new[] { "Theory", "Internal" }
            : string.Equals(examType, "Assignment", StringComparison.OrdinalIgnoreCase)
                ? new[] { "Internal", "Practical" }
                : new[] { "Theory", "Practical", "Internal" };

        return options
            .Select(x => new LookupItem { Id = Guid.Empty, Name = x })
            .ToList();
    }

    private static string ResolveResultsEntryAction(string? entryPoint)
        => string.Equals(entryPoint, nameof(EnterResults), StringComparison.OrdinalIgnoreCase)
            ? nameof(EnterResults)
            : nameof(Results);

    private async Task<(bool Allowed, string Message)> ValidateResultWriteScopeAsync(
        Guid offeringId,
        Guid? departmentId,
        Guid? courseId,
        Guid? subjectOfferingId,
        string? semesterName,
        string? examType,
        string? assessmentComponent,
        Guid? effectiveTenantId,
        Guid? effectiveCampusId,
        SessionIdentity? identity,
        CancellationToken ct)
    {
        if (identity?.IsSuperAdmin == true && (!effectiveTenantId.HasValue || !effectiveCampusId.HasValue))
            return (false, "Select tenant and campus before performing result write operations.");

        if (!effectiveTenantId.HasValue || !effectiveCampusId.HasValue)
            return (false, "Tenant and campus scope is required for result write operations.");

        if (!departmentId.HasValue || !courseId.HasValue || !subjectOfferingId.HasValue || string.IsNullOrWhiteSpace(semesterName)
            || string.IsNullOrWhiteSpace(examType) || string.IsNullOrWhiteSpace(assessmentComponent))
        {
            return (false, "Select department, course, subject, semester/class, exam type, and assessment component before performing result write operations.");
        }

        if (subjectOfferingId.Value != offeringId)
            return (false, "Selected subject does not match the selected offering.");

        var offerings = await _api.GetCourseOfferingsAsync(departmentId, effectiveTenantId, effectiveCampusId, null, ct);
        var selectedOffering = offerings.FirstOrDefault(o => o.Id == offeringId);
        if (selectedOffering is null)
            return (false, "Selected offering is not valid for the current filter scope.");

        if (selectedOffering.CourseId != courseId.Value)
            return (false, "Selected offering does not match the selected course.");

        if (!string.Equals(selectedOffering.SemesterName, semesterName, StringComparison.OrdinalIgnoreCase))
            return (false, "Selected offering does not match the selected semester/class.");

        return (true, string.Empty);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadResultCsvTemplate(
        Guid? offeringId,
        string? semesterName,
        Guid? tenantId,
        Guid? campusId,
        Guid? departmentId,
        Guid? courseId,
        Guid? subjectOfferingId,
        string? examType,
        string? assessmentComponent,
        Guid? studentId,
        string? section,
        string? batch,
        string? entryPoint,
        CancellationToken ct)
    {
        IActionResult RedirectWithContext() => RedirectToAction(
            ResolveResultsEntryAction(entryPoint),
            new
            {
                offeringId,
                semesterName,
                tenantId,
                campusId,
                departmentId,
                courseId,
                subjectOfferingId,
                examType,
                assessmentComponent,
                studentId,
                section,
                batch
            });

        if (!_api.IsConnected())
            return RedirectToAction(nameof(Dashboard));

        if (!offeringId.HasValue)
        {
            TempData["PortalMessage"] = "Select an offering before downloading the result CSV template.";
            return RedirectWithContext();
        }

        try
        {
            var sessionId = _api.GetSessionIdentity();
            var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
            var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;

            var roster = await _api.GetEnrollmentRosterAsync(offeringId.Value, effectiveTenantId, effectiveCampusId, ct);

            var builder = new StringBuilder();
            builder.AppendLine("StudentId,StudentName,ResultType,MarksObtained,MaxMarks");

            var templateResultType = string.IsNullOrWhiteSpace(examType) ? "Final" : examType;
            builder.AppendLine($"11111111-1111-1111-1111-111111111111,Example Row 1 - Replace Before Import,{EscapeCsvField(templateResultType)},75,100");
            builder.AppendLine($"22222222-2222-2222-2222-222222222222,Example Row 2 - Replace Before Import,{EscapeCsvField(templateResultType)},88,100");

            foreach (var student in roster.OrderBy(x => x.StudentName, StringComparer.OrdinalIgnoreCase))
            {
                builder.Append(EscapeCsvField(student.Id.ToString()));
                builder.Append(',');
                builder.Append(EscapeCsvField(student.StudentName ?? string.Empty));
                builder.Append(',');
                builder.Append(EscapeCsvField(templateResultType));
                builder.Append(",0,100");
                builder.AppendLine();
            }

            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            return File(bytes, "text/csv", $"result-entry-template-{offeringId.Value:D}.csv");
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = $"Unable to generate result CSV template: {ex.Message}";
            return RedirectWithContext();
        }
    }

    [HttpGet]
    public IActionResult DownloadResultImportReport(
        string? reportToken,
        Guid? offeringId,
        string? semesterName,
        Guid? tenantId,
        Guid? campusId,
        Guid? departmentId,
        Guid? courseId,
        Guid? subjectOfferingId,
        string? examType,
        string? assessmentComponent,
        Guid? studentId,
        string? section,
        string? batch,
        string? entryPoint)
    {
        IActionResult RedirectWithContext()
            => RedirectToAction(
                ResolveResultsEntryAction(entryPoint),
                new
                {
                    offeringId,
                    semesterName,
                    tenantId,
                    campusId,
                    departmentId,
                    courseId,
                    subjectOfferingId,
                    examType,
                    assessmentComponent,
                    studentId,
                    section,
                    batch
                });

        if (string.IsNullOrWhiteSpace(reportToken) || !ResultImportReports.TryRemove(reportToken, out var payload))
        {
            TempData["PortalMessage"] = "Import report is not available. Please run CSV import again.";
            return RedirectWithContext();
        }

        if (payload.CreatedAtUtc + ResultImportReportTtl < UtcNowProvider())
        {
            TempData["PortalMessage"] = "Import report has expired. Please run CSV import again.";
            return RedirectWithContext();
        }

        var bytes = Encoding.UTF8.GetBytes(payload.CsvContent);
        return File(bytes, "text/csv", payload.FileName);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportResultCsv(
        Guid? offeringId,
        string? semesterName,
        Guid? tenantId,
        Guid? campusId,
        Guid? departmentId,
        Guid? courseId,
        Guid? subjectOfferingId,
        string? examType,
        string? assessmentComponent,
        Guid? studentId,
        string? section,
        string? batch,
        IFormFile? csvFile,
        bool strictMode,
        string? entryPoint,
        CancellationToken ct)
    {
        var totalRows = 0;
        var importedCount = 0;
        var reportRows = new List<ResultImportReportRow>();

        void CleanupExpiredImportReports()
        {
            var threshold = UtcNowProvider().Subtract(ResultImportReportTtl);
            foreach (var item in ResultImportReports)
            {
                if (item.Value.CreatedAtUtc < threshold)
                    ResultImportReports.TryRemove(item.Key, out _);
            }
        }

        string StoreImportReportAndGetToken()
        {
            var csv = new StringBuilder();
            csv.AppendLine("RowNumber,StudentId,StudentName,ResultType,MarksObtained,MaxMarks,Outcome,Reason");
            foreach (var row in reportRows.OrderBy(r => r.RowNumber))
            {
                csv.AppendLine(
                    $"{row.RowNumber},{EscapeCsvField(row.StudentId)},{EscapeCsvField(row.StudentName)},{EscapeCsvField(row.ResultType)},{EscapeCsvField(row.MarksObtained)},{EscapeCsvField(row.MaxMarks)},{EscapeCsvField(row.Outcome)},{EscapeCsvField(row.Reason)}");
            }

            var token = Guid.NewGuid().ToString("N");
            ResultImportReports[token] = new ResultImportReportPayload(
                $"result-import-report-{UtcNowProvider():yyyyMMdd-HHmmss}.csv",
                csv.ToString(),
                UtcNowProvider());
            return token;
        }

        IActionResult RedirectWithContext(string? reportToken = null) => RedirectToAction(
            ResolveResultsEntryAction(entryPoint),
            new
            {
                offeringId,
                semesterName,
                tenantId,
                campusId,
                reportToken,
                departmentId,
                courseId,
                subjectOfferingId,
                examType,
                assessmentComponent,
                studentId,
                section,
                batch
            });

        void WriteResultImportAudit(string outcome, SessionIdentity? actor, IReadOnlyCollection<string>? errorDetails = null)
        {
            var details = errorDetails?.Where(e => !string.IsNullOrWhiteSpace(e)).Take(5).ToList() ?? new List<string>();
            var auditLine = $"outcome={outcome}; uploadedBy={actor?.UserName ?? actor?.Email ?? "unknown"}; uploadedAtUtc={DateTime.UtcNow:O}; strictMode={strictMode}; offeringId={offeringId?.ToString() ?? "none"}; totalRows={totalRows}; importedRows={importedCount}; skippedRows={Math.Max(totalRows - importedCount, 0)}; errors={string.Join(" | ", details)}";
            TempData["PortalImportAudit"] = auditLine;
            _logger.LogInformation("Result CSV import audit {AuditLine}", auditLine);
        }

        CleanupExpiredImportReports();

        if (!_api.IsConnected())
        {
            WriteResultImportAudit("blocked-not-connected", actor: null);
            return RedirectToAction(nameof(Dashboard));
        }

        if (!offeringId.HasValue)
        {
            TempData["PortalMessage"] = "Select an offering before importing result CSV.";
            WriteResultImportAudit("blocked-missing-offering", _api.GetSessionIdentity());
            return RedirectWithContext();
        }

        if (csvFile is null || csvFile.Length == 0)
        {
            TempData["PortalMessage"] = "Select a CSV file to import results.";
            WriteResultImportAudit("blocked-empty-file", _api.GetSessionIdentity());
            return RedirectWithContext();
        }

        var sessionId = _api.GetSessionIdentity();
        var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
        var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;
        var scopeValidation = await ValidateResultWriteScopeAsync(
            offeringId.Value,
            departmentId,
            courseId,
            subjectOfferingId,
            semesterName,
            examType,
            assessmentComponent,
            effectiveTenantId,
            effectiveCampusId,
            sessionId,
            ct);
        if (!scopeValidation.Allowed)
        {
            TempData["PortalMessage"] = scopeValidation.Message;
            WriteResultImportAudit("blocked-scope-validation", sessionId, new List<string> { scopeValidation.Message });
            return RedirectWithContext();
        }

        var roster = await _api.GetEnrollmentRosterAsync(offeringId.Value, effectiveTenantId, effectiveCampusId, ct);
        var rosterById = roster.ToDictionary(x => x.Id, x => x.StudentName ?? string.Empty);

        var validationErrors = new List<string>();
        var parsedRows = new List<(int RowNumber, Guid StudentId, string ResultType, decimal MarksObtained, decimal MaxMarks)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            await using var stream = csvFile.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            var headerLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                TempData["PortalMessage"] = "CSV file is empty.";
                WriteResultImportAudit("blocked-empty-header", sessionId);
                return RedirectWithContext();
            }

            var headers = ParseCsvLine(headerLine)
                .Select(h => h.Trim())
                .ToArray();

            var expected = new[] { "StudentId", "StudentName", "ResultType", "MarksObtained", "MaxMarks" };
            if (headers.Length != expected.Length || !headers.SequenceEqual(expected, StringComparer.OrdinalIgnoreCase))
            {
                TempData["PortalMessage"] = "Invalid CSV header. Expected: StudentId,StudentName,ResultType,MarksObtained,MaxMarks.";
                WriteResultImportAudit("blocked-invalid-header", sessionId, new List<string> { "CSV header does not match expected template." });
                return RedirectWithContext();
            }

            var rowNumber = 1;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                rowNumber++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                totalRows++;
                var cols = ParseCsvLine(line);
                if (cols.Length != 5)
                {
                    var reason = $"Row {rowNumber}: expected 5 columns.";
                    validationErrors.Add(reason);
                    reportRows.Add(new ResultImportReportRow
                    {
                        RowNumber = rowNumber,
                        Outcome = "Skipped",
                        Reason = reason
                    });
                    continue;
                }

                var studentIdRaw = cols[0].Trim();
                var studentNameRaw = cols[1].Trim();
                var resultTypeRaw = cols[2].Trim();
                var marksRaw = cols[3].Trim();
                var maxRaw = cols[4].Trim();

                var reportRow = new ResultImportReportRow
                {
                    RowNumber = rowNumber,
                    StudentId = studentIdRaw,
                    StudentName = studentNameRaw,
                    ResultType = resultTypeRaw,
                    MarksObtained = marksRaw,
                    MaxMarks = maxRaw,
                    Outcome = "Pending"
                };

                if (!Guid.TryParse(studentIdRaw, out var parsedStudentId))
                {
                    var reason = $"Row {rowNumber}: StudentId is invalid.";
                    validationErrors.Add(reason);
                    reportRow.Outcome = "Skipped";
                    reportRow.Reason = reason;
                    reportRows.Add(reportRow);
                    continue;
                }

                if (ResultTemplateExampleStudentIds.Contains(parsedStudentId)
                    || studentNameRaw.StartsWith("Example Row", StringComparison.OrdinalIgnoreCase))
                {
                    reportRow.Outcome = "Skipped";
                    reportRow.Reason = "Template example row skipped (guidance only).";
                    reportRows.Add(reportRow);
                    continue;
                }

                if (!rosterById.ContainsKey(parsedStudentId))
                {
                    var reason = $"Row {rowNumber}: StudentId does not belong to the selected offering roster.";
                    validationErrors.Add(reason);
                    reportRow.Outcome = "Skipped";
                    reportRow.Reason = reason;
                    reportRows.Add(reportRow);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(studentNameRaw)
                    && !string.Equals(studentNameRaw, rosterById[parsedStudentId], StringComparison.OrdinalIgnoreCase))
                {
                    var reason = $"Row {rowNumber}: StudentName does not match the selected offering roster.";
                    validationErrors.Add(reason);
                    reportRow.Outcome = "Skipped";
                    reportRow.Reason = reason;
                    reportRows.Add(reportRow);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(resultTypeRaw))
                {
                    var reason = $"Row {rowNumber}: ResultType is required.";
                    validationErrors.Add(reason);
                    reportRow.Outcome = "Skipped";
                    reportRow.Reason = reason;
                    reportRows.Add(reportRow);
                    continue;
                }

                if (!decimal.TryParse(marksRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var marksObtained)
                    && !decimal.TryParse(marksRaw, NumberStyles.Number, CultureInfo.CurrentCulture, out marksObtained))
                {
                    var reason = $"Row {rowNumber}: MarksObtained is invalid.";
                    validationErrors.Add(reason);
                    reportRow.Outcome = "Skipped";
                    reportRow.Reason = reason;
                    reportRows.Add(reportRow);
                    continue;
                }

                if (!decimal.TryParse(maxRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var maxMarks)
                    && !decimal.TryParse(maxRaw, NumberStyles.Number, CultureInfo.CurrentCulture, out maxMarks))
                {
                    var reason = $"Row {rowNumber}: MaxMarks is invalid.";
                    validationErrors.Add(reason);
                    reportRow.Outcome = "Skipped";
                    reportRow.Reason = reason;
                    reportRows.Add(reportRow);
                    continue;
                }

                if (marksObtained < 0 || maxMarks <= 0 || marksObtained > maxMarks)
                {
                    var reason = $"Row {rowNumber}: marks must be within 0 and MaxMarks.";
                    validationErrors.Add(reason);
                    reportRow.Outcome = "Skipped";
                    reportRow.Reason = reason;
                    reportRows.Add(reportRow);
                    continue;
                }

                var duplicateKey = $"{parsedStudentId:D}|{resultTypeRaw}|{assessmentComponent}";
                if (!seen.Add(duplicateKey))
                {
                    var reason = $"Row {rowNumber}: duplicate StudentId + ResultType + AssessmentComponent entry.";
                    validationErrors.Add(reason);
                    reportRow.Outcome = "Skipped";
                    reportRow.Reason = reason;
                    reportRows.Add(reportRow);
                    continue;
                }

                parsedRows.Add((rowNumber, parsedStudentId, resultTypeRaw, marksObtained, maxMarks));
                reportRows.Add(reportRow);
            }
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = $"Unable to read result CSV file: {ex.Message}";
            WriteResultImportAudit("failed-csv-read", sessionId, new List<string> { ex.Message });
            return RedirectWithContext();
        }

        if (validationErrors.Count > 0 && strictMode)
        {
            foreach (var row in reportRows.Where(r => string.Equals(r.Outcome, "Pending", StringComparison.OrdinalIgnoreCase)))
            {
                row.Outcome = "Skipped";
                row.Reason = "Skipped due to strict mode validation failure.";
            }

            TempData["PortalMessage"] = $"Result CSV validation failed in strict mode. Invalid rows: {validationErrors.Count}.";
            TempData["PortalMessageDetails"] = string.Join('\n', validationErrors.Take(20));
            WriteResultImportAudit("strict-validation-failed", sessionId, validationErrors);
            var strictReportToken = reportRows.Count > 0 ? StoreImportReportAndGetToken() : null;
            return RedirectWithContext(strictReportToken);
        }

        if (parsedRows.Count == 0)
        {
            TempData["PortalMessage"] = "CSV file has no valid result rows to import.";
            if (validationErrors.Count > 0)
                TempData["PortalMessageDetails"] = string.Join('\n', validationErrors.Take(20));

            WriteResultImportAudit("no-valid-rows", sessionId, validationErrors);
            var emptyReportToken = reportRows.Count > 0 ? StoreImportReportAndGetToken() : null;
            return RedirectWithContext(emptyReportToken);
        }

        var importErrors = new List<string>();
        foreach (var row in parsedRows)
        {
            try
            {
                await _api.CreateResultAsync(
                    row.StudentId,
                    offeringId.Value,
                    row.ResultType,
                    row.MarksObtained,
                    row.MaxMarks,
                    effectiveTenantId,
                    effectiveCampusId,
                    ct);

                importedCount++;
                var reportRow = reportRows.First(r => r.RowNumber == row.RowNumber);
                reportRow.Outcome = "Imported";
                reportRow.Reason = string.Empty;
            }
            catch (Exception ex)
            {
                var reason = $"Row {row.RowNumber}: {ex.Message}";
                importErrors.Add(reason);
                var reportRow = reportRows.First(r => r.RowNumber == row.RowNumber);
                reportRow.Outcome = "Skipped";
                reportRow.Reason = ex.Message;
            }
        }

        var warningCount = validationErrors.Count + importErrors.Count;
        if (warningCount == 0)
        {
            TempData["PortalMessage"] = $"Result CSV import completed successfully. Imported rows: {importedCount}.";
            WriteResultImportAudit("success", sessionId);
        }
        else
        {
            TempData["PortalMessage"] = $"Result CSV import completed with warnings. Imported rows: {importedCount}. Skipped/failed rows: {warningCount}.";
            TempData["PortalMessageDetails"] = string.Join('\n', validationErrors.Concat(importErrors).Take(20));
            WriteResultImportAudit("completed-with-warnings", sessionId, validationErrors.Concat(importErrors).ToList());
        }

        var reportToken = reportRows.Count > 0 ? StoreImportReportAndGetToken() : null;
        return RedirectWithContext(reportToken);
    }

    // ── Result write actions ────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateResult(
        Guid studentProfileId, Guid offeringId,
        string resultType, decimal marksObtained, decimal maxMarks,
        bool promote, Guid? tenantId, Guid? campusId,
        Guid? departmentId, Guid? courseId, Guid? subjectOfferingId, string? semesterName, string? examType, string? assessmentComponent,
        Guid? studentId, string? section, string? batch,
        string? entryPoint,
        CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var sessionId = _api.GetSessionIdentity();
                var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
                var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;

                if (marksObtained < 0 || maxMarks <= 0 || marksObtained > maxMarks)
                {
                    TempData["PortalMessage"] = "Marks must be within valid range (0 <= Marks Obtained <= Max Marks, and Max Marks > 0).";
                    return RedirectToAction(ResolveResultsEntryAction(entryPoint), new
                    {
                        offeringId,
                        semesterName,
                        tenantId,
                        campusId,
                        departmentId,
                        courseId,
                        subjectOfferingId,
                        examType,
                        assessmentComponent,
                        studentId,
                        section,
                        batch
                    });
                }

                var scopeValidation = await ValidateResultWriteScopeAsync(
                    offeringId,
                    departmentId,
                    courseId,
                    subjectOfferingId,
                    semesterName,
                    examType,
                    assessmentComponent,
                    effectiveTenantId,
                    effectiveCampusId,
                    sessionId,
                    ct);
                if (!scopeValidation.Allowed)
                {
                    _logger.LogWarning(
                        "Result create scope blocked. offeringId={OfferingId}; studentProfileId={StudentProfileId}; actor={Actor}; reason={Reason}",
                        offeringId,
                        studentProfileId,
                        sessionId?.UserName ?? sessionId?.Email ?? "unknown",
                        scopeValidation.Message);

                    TempData["PortalMessage"] = scopeValidation.Message;
                    return RedirectToAction(ResolveResultsEntryAction(entryPoint), new
                    {
                        offeringId,
                        semesterName,
                        tenantId,
                        campusId,
                        departmentId,
                        courseId,
                        subjectOfferingId,
                        examType,
                        assessmentComponent,
                        studentId,
                        section,
                        batch
                    });
                }

                var effectiveResultType = string.IsNullOrWhiteSpace(examType) ? resultType : examType;

                await _api.CreateResultAsync(studentProfileId, offeringId, effectiveResultType, marksObtained, maxMarks, effectiveTenantId, effectiveCampusId, ct);
                _logger.LogInformation(
                    "Result created. offeringId={OfferingId}; studentProfileId={StudentProfileId}; resultType={ResultType}; actor={Actor}",
                    offeringId,
                    studentProfileId,
                    effectiveResultType,
                    sessionId?.UserName ?? sessionId?.Email ?? "unknown");

                // Promotion is only offered in the UI for Final result type.
                // When the checkbox is checked the form sends promote=true;
                // unchecked checkboxes send nothing, so promote defaults to false.
                if (promote)
                    await _api.PromoteStudentAsync(studentProfileId, effectiveTenantId, effectiveCampusId, ct);
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(ResolveResultsEntryAction(entryPoint), new
        {
            offeringId,
            semesterName,
            tenantId,
            campusId,
            departmentId,
            courseId,
            subjectOfferingId,
            examType,
            assessmentComponent,
            studentId,
            section,
            batch
        });
    }

    // Standalone per-row Promote button in the Results table.
    // Reuses the existing POST api/v1/student-lifecycle/{id}/promote endpoint
    // without requiring a new result entry.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteStudentFromResult(
        Guid studentProfileId, Guid? offeringId, Guid? tenantId, Guid? campusId,
        Guid? departmentId, Guid? courseId, Guid? subjectOfferingId, string? semesterName, string? examType, string? assessmentComponent,
        Guid? studentId, string? section, string? batch,
        string? entryPoint,
        CancellationToken ct)
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
        return RedirectToAction(ResolveResultsEntryAction(entryPoint), new
        {
            offeringId,
            semesterName,
            tenantId,
            campusId,
            departmentId,
            courseId,
            subjectOfferingId,
            examType,
            assessmentComponent,
            studentId,
            section,
            batch
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CorrectResult(
        Guid studentProfileId, Guid offeringId, string resultType,
        decimal newMarksObtained, decimal newMaxMarks, string reason, Guid? tenantId, Guid? campusId,
        Guid? departmentId, Guid? courseId, Guid? subjectOfferingId, string? semesterName, string? examType, string? assessmentComponent,
        Guid? studentId, string? section, string? batch,
        string? entryPoint,
        CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var sessionId = _api.GetSessionIdentity();
                var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
                var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;

                if (newMarksObtained < 0 || newMaxMarks <= 0 || newMarksObtained > newMaxMarks)
                {
                    TempData["PortalMessage"] = "Correction values are invalid (0 <= New Marks Obtained <= New Max Marks, and New Max Marks > 0).";
                    return RedirectToAction(ResolveResultsEntryAction(entryPoint), new
                    {
                        offeringId,
                        semesterName,
                        tenantId,
                        campusId,
                        departmentId,
                        courseId,
                        subjectOfferingId,
                        examType,
                        assessmentComponent,
                        studentId,
                        section,
                        batch
                    });
                }

                if (string.IsNullOrWhiteSpace(reason))
                {
                    TempData["PortalMessage"] = "Correction reason is required for audit traceability.";
                    return RedirectToAction(ResolveResultsEntryAction(entryPoint), new
                    {
                        offeringId,
                        semesterName,
                        tenantId,
                        campusId,
                        departmentId,
                        courseId,
                        subjectOfferingId,
                        examType,
                        assessmentComponent,
                        studentId,
                        section,
                        batch
                    });
                }

                var scopeValidation = await ValidateResultWriteScopeAsync(
                    offeringId,
                    departmentId,
                    courseId,
                    subjectOfferingId,
                    semesterName,
                    examType,
                    assessmentComponent,
                    effectiveTenantId,
                    effectiveCampusId,
                    sessionId,
                    ct);
                if (!scopeValidation.Allowed)
                {
                    _logger.LogWarning(
                        "Result correction scope blocked. offeringId={OfferingId}; studentProfileId={StudentProfileId}; actor={Actor}; reason={Reason}",
                        offeringId,
                        studentProfileId,
                        sessionId?.UserName ?? sessionId?.Email ?? "unknown",
                        scopeValidation.Message);

                    TempData["PortalMessage"] = scopeValidation.Message;
                    return RedirectToAction(ResolveResultsEntryAction(entryPoint), new
                    {
                        offeringId,
                        semesterName,
                        tenantId,
                        campusId,
                        departmentId,
                        courseId,
                        subjectOfferingId,
                        examType,
                        assessmentComponent,
                        studentId,
                        section,
                        batch
                    });
                }

                await _api.CorrectResultAsync(studentProfileId, offeringId, resultType, newMarksObtained, newMaxMarks, reason, effectiveTenantId, effectiveCampusId, ct);
                _logger.LogInformation(
                    "Result corrected. offeringId={OfferingId}; studentProfileId={StudentProfileId}; resultType={ResultType}; actor={Actor}",
                    offeringId,
                    studentProfileId,
                    resultType,
                    sessionId?.UserName ?? sessionId?.Email ?? "unknown");
                TempData["PortalMessage"] = "Result corrected.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(ResolveResultsEntryAction(entryPoint), new
        {
            offeringId,
            semesterName,
            tenantId,
            campusId,
            departmentId,
            courseId,
            subjectOfferingId,
            examType,
            assessmentComponent,
            studentId,
            section,
            batch
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishAllResults(
        Guid offeringId, Guid? tenantId, Guid? campusId,
        Guid? departmentId, Guid? courseId, Guid? subjectOfferingId, string? semesterName, string? examType, string? assessmentComponent,
        Guid? studentId, string? section, string? batch,
        string? entryPoint,
        CancellationToken ct)
    {
        if (_api.IsConnected())
        {
            try
            {
                var sessionId = _api.GetSessionIdentity();
                if (sessionId?.IsAdmin != true && sessionId?.IsSuperAdmin != true)
                {
                    _logger.LogWarning(
                        "Result publish blocked due to role. offeringId={OfferingId}; actor={Actor}",
                        offeringId,
                        sessionId?.UserName ?? sessionId?.Email ?? "unknown");

                    TempData["PortalMessage"] = "Final publish requires Admin/SuperAdmin approval.";
                    return RedirectToAction(ResolveResultsEntryAction(entryPoint), new
                    {
                        offeringId,
                        semesterName,
                        tenantId,
                        campusId,
                        departmentId,
                        courseId,
                        subjectOfferingId,
                        examType,
                        assessmentComponent,
                        studentId,
                        section,
                        batch
                    });
                }

                var effectiveTenantId = sessionId?.IsSuperAdmin == true ? tenantId : sessionId?.TenantId;
                var effectiveCampusId = sessionId?.IsSuperAdmin == true ? campusId : sessionId?.CampusId;

                var scopeValidation = await ValidateResultWriteScopeAsync(
                    offeringId,
                    departmentId,
                    courseId,
                    subjectOfferingId,
                    semesterName,
                    examType,
                    assessmentComponent,
                    effectiveTenantId,
                    effectiveCampusId,
                    sessionId,
                    ct);
                if (!scopeValidation.Allowed)
                {
                    _logger.LogWarning(
                        "Result publish scope blocked. offeringId={OfferingId}; actor={Actor}; reason={Reason}",
                        offeringId,
                        sessionId?.UserName ?? sessionId?.Email ?? "unknown",
                        scopeValidation.Message);

                    TempData["PortalMessage"] = scopeValidation.Message;
                    return RedirectToAction(ResolveResultsEntryAction(entryPoint), new
                    {
                        offeringId,
                        semesterName,
                        tenantId,
                        campusId,
                        departmentId,
                        courseId,
                        subjectOfferingId,
                        examType,
                        assessmentComponent,
                        studentId,
                        section,
                        batch
                    });
                }

                await _api.PublishAllResultsAsync(offeringId, effectiveTenantId, effectiveCampusId, ct);
                _logger.LogInformation(
                    "All results published. offeringId={OfferingId}; actor={Actor}",
                    offeringId,
                    sessionId?.UserName ?? sessionId?.Email ?? "unknown");
                TempData["PortalMessage"] = "All results published.";
            }
            catch (Exception ex) { TempData["PortalMessage"] = $"Error: {ex.Message}"; }
        }
        return RedirectToAction(ResolveResultsEntryAction(entryPoint), new
        {
            offeringId,
            semesterName,
            tenantId,
            campusId,
            departmentId,
            courseId,
            subjectOfferingId,
            examType,
            assessmentComponent,
            studentId,
            section,
            batch
        });
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
        Guid? departmentId,
        Guid? courseId,
        Guid? subjectOfferingId,
        string? semesterName,
        string? examType,
        string? assessmentComponent,
        Guid? studentId,
        string? section,
        string? batch,
        string? entryPoint,
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

        return RedirectToAction(ResolveResultsEntryAction(entryPoint), new
        {
            offeringId,
            semesterName,
            tenantId,
            campusId,
            departmentId,
            courseId,
            subjectOfferingId,
            examType,
            assessmentComponent,
            studentId,
            section,
            batch
        });
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

                var correctionReason = string.IsNullOrWhiteSpace(req.Reason)
                    ? "Approved modification request correction."
                    : req.Reason;

                await _api.CorrectResultAsync(studentProfileId, courseOfferingId, resultType, newMarksObtained, newMaxMarks, correctionReason, requestedTenantId, requestedCampusId, ct);
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
        if (id == Guid.Empty)
        {
            TempData["PortalMessage"] = "Quiz identifier is missing. Refresh the Quizzes page and try again.";
            return RedirectToAction(nameof(Quizzes), new { offeringId, tenantId, campusId, includeInactive });
        }

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
            SelectedSemesterId = semesterId,
            AvailableInstitutionTypes = BuildLicensedInstitutionOptions(null)
        };

        if (!model.IsConnected)
        {
            return model;
        }

        try
        {
            var capabilityMatrix = await _api.GetPortalCapabilityMatrixAsync(ct);
            model.AvailableInstitutionTypes = BuildLicensedInstitutionOptions(capabilityMatrix);
            model.SelectedInstitutionType = ResolveLicensedInstitutionSelection(
                model.SelectedInstitutionType,
                identity,
                model.AvailableInstitutionTypes);

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
                model.AvailableInstitutionTypes = model.AvailableInstitutionTypes
                    .Where(x => x.Value == identity.InstitutionType.Value)
                    .ToList();
            }

            model.PeriodFilterLabel = ResolvePeriodFilterLabel(model.AvailableInstitutionTypes, model.SelectedInstitutionType);
            model.PeriodFilterPlaceholder = ResolvePeriodFilterPlaceholder(model.PeriodFilterLabel);

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
    public async Task<IActionResult> Payments(Guid? studentId, Guid? tenantId, Guid? campusId, int? institutionType, int page = 1, CancellationToken ct = default)
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
            SelectedInstitutionType = institutionType,
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
                var canManagePayments = identity?.IsSuperAdmin == true || identity?.IsAdmin == true || identity?.IsFinance == true;

                if (canManagePayments)
                {
                    var capabilityMatrix = await _api.GetPortalCapabilityMatrixAsync(ct);
                    model.AvailableInstitutionTypes = BuildLicensedPaymentsInstitutionOptions(capabilityMatrix);
                    model.SelectedInstitutionType = ResolveLicensedInstitutionSelection(model.SelectedInstitutionType, identity, model.AvailableInstitutionTypes);
                }

                if (identity?.IsSuperAdmin == true)
                {
                    model.Tenants = await _api.GetTenantsAsync(ct);
                    if (model.SelectedTenantId.HasValue)
                        model.Campuses = await _api.GetCampusesAsync(model.SelectedTenantId, ct);
                }

                model.Students = await LoadPaymentStudentsForScopeAsync(model.SelectedTenantId, model.SelectedCampusId, model.SelectedInstitutionType, ct);
                // Admin / Finance: load paged receipts and optionally filter by student
                PaymentReceiptPageItem pageResult;
                if (studentId.HasValue)
                    pageResult = await _api.GetPaymentsByStudentAsync(studentId.Value, model.Page, model.PageSize, model.SelectedTenantId, model.SelectedCampusId, model.SelectedInstitutionType, ct);
                else
                    pageResult = await _api.GetAllPaymentsAsync(model.Page, model.PageSize, model.SelectedTenantId, model.SelectedCampusId, model.SelectedInstitutionType, ct);

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
    public async Task<IActionResult> CreatePayment(CreatePaymentForm form, Guid? studentId, Guid? tenantId, Guid? campusId, int? institutionType, int page = 1, CancellationToken ct = default)
    {
        try
        {
            var identity = _api.GetSessionIdentity();
            if (!(identity?.IsSuperAdmin == true || identity?.IsAdmin == true || identity?.IsFinance == true))
            {
                TempData["PortalMessage"] = "Only Admin/Finance users can create payment receipts.";
                return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, institutionType, page });
            }

            var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
            var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
            await _api.CreatePaymentAsync(form.StudentProfileId, form.Amount, form.ReceiptNo, form.Description, form.DueDate, effectiveTenantId, effectiveCampusId, ct);
            TempData["PortalMessage"] = "Receipt created successfully.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, institutionType, page });
    }

    [HttpGet]
    public IActionResult ExportPaymentsCsvTemplate()
    {
        var lines = new[]
        {
            "ReceiptNo,RegistrationNumber,Amount,Description,DueDate",
            "RCPT-2026-0001,2026-CS-0001,5000.00,Semester 1 Tuition,2026-08-15",
            "RCPT-2026-0002,2026-COMM-0001,1200.00,Exam Fee,2026-08-20"
        };

        return File(Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, lines)), "text/csv", "payments-import-template.csv");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportPaymentsCsv(IFormFile csvFile, Guid? studentId, Guid? tenantId, Guid? campusId, int? institutionType, int page = 1, CancellationToken ct = default)
    {
        try
        {
            var identity = _api.GetSessionIdentity();
            if (!(identity?.IsSuperAdmin == true || identity?.IsAdmin == true || identity?.IsFinance == true))
            {
                TempData["PortalMessage"] = "Only Admin/Finance users can import payments.";
                return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, institutionType, page });
            }

            if (csvFile is null || csvFile.Length == 0)
            {
                TempData["PortalMessage"] = "Please select a CSV file.";
                return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, institutionType, page });
            }

            var rows = await ParsePaymentImportCsvAsync(csvFile, ct);
            if (rows.Count == 0)
            {
                TempData["PortalMessage"] = "CSV has no payment rows.";
                return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, institutionType, page });
            }

            var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
            var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;

            var students = await LoadPaymentStudentsForScopeAsync(effectiveTenantId, effectiveCampusId, institutionType, ct);
            var map = students
                .GroupBy(s => s.RegistrationNumber, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            foreach (var registration in rows.Select(r => r.RegistrationNumber).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!map.ContainsKey(registration))
                    throw new InvalidOperationException($"Student with registration '{registration}' was not found in current scope.");
            }

            await _api.CreatePaymentsBatchAsync(rows, map, effectiveTenantId, effectiveCampusId, ct);
            TempData["PortalMessage"] = $"Imported {rows.Count} payment receipts successfully.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, institutionType, page });
    }

    // Final-Touches Phase 7 Stage 7.2 — edit receipt (Admin/Finance)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePayment(Guid receiptId, decimal amount, string receiptNo, string description, DateTime dueDate, string? notes, Guid? studentId, Guid? tenantId, Guid? campusId, int? institutionType, int page = 1, CancellationToken ct = default)
    {
        try
        {
            var identity = _api.GetSessionIdentity();
            if (!(identity?.IsSuperAdmin == true || identity?.IsAdmin == true || identity?.IsFinance == true))
            {
                TempData["PortalMessage"] = "Only Admin/Finance users can update payment receipts.";
                return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, institutionType, page });
            }

            var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
            var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
            await _api.UpdatePaymentAsync(receiptId, amount, receiptNo, description, dueDate, notes, effectiveTenantId, effectiveCampusId, ct);
            TempData["PortalMessage"] = "Receipt updated successfully.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, institutionType, page });
    }

    // Final-Touches Phase 7 Stage 7.2 — confirm payment (Admin/Finance)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPayment(Guid receiptId, Guid? studentId, Guid? tenantId, Guid? campusId, int? institutionType, int page = 1, CancellationToken ct = default)
    {
        try
        {
            var identity = _api.GetSessionIdentity();
            if (!(identity?.IsSuperAdmin == true || identity?.IsAdmin == true || identity?.IsFinance == true))
            {
                TempData["PortalMessage"] = "Only Admin/Finance users can confirm payments.";
                return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, institutionType, page });
            }

            var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
            var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
            await _api.ConfirmPaymentAsync(receiptId, effectiveTenantId, effectiveCampusId, ct);
            TempData["PortalMessage"] = "Payment confirmed.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, institutionType, page });
    }

    // Final-Touches Phase 7 Stage 7.2 — cancel receipt (Admin/Finance)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelPayment(Guid receiptId, Guid? studentId, Guid? tenantId, Guid? campusId, int? institutionType, int page = 1, CancellationToken ct = default)
    {
        try
        {
            var identity = _api.GetSessionIdentity();
            if (!(identity?.IsSuperAdmin == true || identity?.IsAdmin == true || identity?.IsFinance == true))
            {
                TempData["PortalMessage"] = "Only Admin/Finance users can cancel receipts.";
                return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, institutionType, page });
            }

            var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
            var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
            await _api.CancelPaymentAsync(receiptId, effectiveTenantId, effectiveCampusId, ct);
            TempData["PortalMessage"] = "Receipt cancelled.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, institutionType, page });
    }

    // Final-Touches Phase 7 Stage 7.3 — student marks receipt as submitted
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitProof(Guid receiptId, string proofNote, Guid? studentId, Guid? tenantId, Guid? campusId, int? institutionType, int page = 1, CancellationToken ct = default)
    {
        try
        {
            var identity = _api.GetSessionIdentity();
            if (identity?.IsStudent != true)
            {
                TempData["PortalMessage"] = "Only students can submit payment proof.";
                return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, institutionType, page });
            }

            var effectiveTenantId = identity?.IsSuperAdmin == true ? tenantId : identity?.TenantId;
            var effectiveCampusId = identity?.IsSuperAdmin == true ? campusId : identity?.CampusId;
            await _api.SubmitProofAsync(receiptId, proofNote, effectiveTenantId, effectiveCampusId, ct);
            TempData["PortalMessage"] = "Proof of payment submitted.";
        }
        catch (Exception ex) { TempData["PortalMessage"] = ex.Message; }
        return RedirectToAction(nameof(Payments), new { studentId, tenantId, campusId, institutionType, page });
    }

    private async Task<List<StudentItem>> LoadPaymentStudentsForScopeAsync(Guid? tenantId, Guid? campusId, int? institutionType, CancellationToken ct)
    {
        if (!tenantId.HasValue && !campusId.HasValue && !institutionType.HasValue)
        {
            var students = await _api.GetStudentsAsync(null, ct);
            return students
                .OrderBy(s => s.RegistrationNumber)
                .ThenBy(s => s.FullName)
                .ToList();
        }

        var departmentDetails = await _api.GetDepartmentDetailsAsync(tenantId, campusId, ct);
        var filteredDepartments = departmentDetails
            .Where(d => !institutionType.HasValue || d.InstitutionType == institutionType.Value)
            .Select(d => d.Id)
            .Distinct()
            .ToList();

        if (filteredDepartments.Count == 0)
            return new List<StudentItem>();

        var studentsById = new Dictionary<Guid, StudentItem>();

        foreach (var departmentId in filteredDepartments)
        {
            var students = await _api.GetStudentsAsync(departmentId, ct);
            foreach (var student in students)
            {
                studentsById[student.Id] = student;
            }
        }

        return studentsById.Values
            .OrderBy(s => s.RegistrationNumber)
            .ThenBy(s => s.FullName)
            .ToList();
    }

    private async Task<List<PaymentImportCsvRow>> ParsePaymentImportCsvAsync(IFormFile csvFile, CancellationToken ct)
    {
        await using var stream = csvFile.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var headerLine = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(headerLine))
            throw new InvalidOperationException("CSV header is missing.");

        var headers = ParseCsvLine(headerLine)
            .Select(h => h.Trim())
            .ToArray();

        var required = new[] { "ReceiptNo", "RegistrationNumber", "Amount", "Description", "DueDate" };
        if (required.Any(x => !headers.Contains(x, StringComparer.OrdinalIgnoreCase)))
            throw new InvalidOperationException("CSV must contain headers: ReceiptNo, RegistrationNumber, Amount, Description, DueDate.");

        var indexMap = headers
            .Select((name, idx) => new { name, idx })
            .ToDictionary(x => x.name, x => x.idx, StringComparer.OrdinalIgnoreCase);

        var rows = new List<PaymentImportCsvRow>();
        string? line;
        var lineNo = 1;

        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            lineNo++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var cols = ParseCsvLine(line);

            string Read(string col)
            {
                var idx = indexMap[col];
                return idx < cols.Length ? cols[idx].Trim() : string.Empty;
            }

            var receiptNo = Read("ReceiptNo");
            var registrationNumber = Read("RegistrationNumber");
            var amountRaw = Read("Amount");
            var description = Read("Description");
            var dueDateRaw = Read("DueDate");

            if (string.IsNullOrWhiteSpace(receiptNo) || string.IsNullOrWhiteSpace(registrationNumber) || string.IsNullOrWhiteSpace(amountRaw) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(dueDateRaw))
                throw new InvalidOperationException($"CSV row {lineNo} is missing one or more required values.");

            if (!decimal.TryParse(amountRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                throw new InvalidOperationException($"CSV row {lineNo} has invalid Amount '{amountRaw}'.");

            var acceptedDateFormats = new[]
            {
                "yyyy-MM-dd",
                "dd/MM/yyyy",
                "d/M/yyyy",
                "MM/dd/yyyy",
                "M/d/yyyy",
                "dd-MM-yyyy",
                "d-M-yyyy"
            };

            if (!DateTime.TryParseExact(dueDateRaw, acceptedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dueDate)
                && !DateTime.TryParse(dueDateRaw, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dueDate)
                && !DateTime.TryParse(dueDateRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dueDate))
            {
                throw new InvalidOperationException($"CSV row {lineNo} has invalid DueDate '{dueDateRaw}'. Accepted formats include yyyy-MM-dd or dd/MM/yyyy.");
            }

            rows.Add(new PaymentImportCsvRow
            {
                ReceiptNo = receiptNo,
                RegistrationNumber = registrationNumber,
                Amount = amount,
                Description = description,
                DueDate = dueDate.Date
            });
        }

        return rows;
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
        var institutionFilter = await BuildReportInstitutionFilterAsync(ResolveReportInstitutionType(institutionType), ct);
        var selectedInstitutionType = institutionFilter.SelectedInstitutionType;
        ViewData["Title"] = "Attendance Summary Report";
        var model = new ReportAttendancePageModel
        {
            IsConnected  = _api.IsConnected(),
            SemesterId   = semesterId,
            DepartmentId = departmentId,
            OfferingId   = offeringId,
            StudentId    = studentId,
            InstitutionType = selectedInstitutionType,
            AvailableInstitutionTypes = institutionFilter.AvailableInstitutionTypes,
            PeriodFilterLabel = institutionFilter.PeriodFilterLabel,
            PeriodFilterPlaceholder = institutionFilter.PeriodFilterPlaceholder
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
        var institutionFilter = await BuildReportInstitutionFilterAsync(ResolveReportInstitutionType(institutionType), ct);
        var selectedInstitutionType = institutionFilter.SelectedInstitutionType;
        ViewData["Title"] = "Result Summary Report";
        var model = new ReportResultsPageModel
        {
            IsConnected  = _api.IsConnected(),
            SemesterId   = semesterId,
            DepartmentId = departmentId,
            OfferingId   = offeringId,
            StudentId    = studentId,
            InstitutionType = selectedInstitutionType,
            AvailableInstitutionTypes = institutionFilter.AvailableInstitutionTypes,
            PeriodFilterLabel = institutionFilter.PeriodFilterLabel,
            PeriodFilterPlaceholder = institutionFilter.PeriodFilterPlaceholder
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
        var institutionFilter = await BuildReportInstitutionFilterAsync(ResolveReportInstitutionType(institutionType), ct);
        var selectedInstitutionType = institutionFilter.SelectedInstitutionType;
        ViewData["Title"] = "Assignment Summary Report";
        var model = new ReportAssignmentsPageModel
        {
            IsConnected  = _api.IsConnected(),
            SemesterId   = semesterId,
            DepartmentId = departmentId,
            OfferingId   = offeringId,
            StudentId    = studentId,
            InstitutionType = selectedInstitutionType,
            AvailableInstitutionTypes = institutionFilter.AvailableInstitutionTypes,
            PeriodFilterLabel = institutionFilter.PeriodFilterLabel,
            PeriodFilterPlaceholder = institutionFilter.PeriodFilterPlaceholder
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
        var institutionFilter = await BuildReportInstitutionFilterAsync(ResolveReportInstitutionType(institutionType), ct);
        var selectedInstitutionType = institutionFilter.SelectedInstitutionType;
        ViewData["Title"] = "Quiz Summary Report";
        var model = new ReportQuizzesPageModel
        {
            IsConnected  = _api.IsConnected(),
            SemesterId   = semesterId,
            DepartmentId = departmentId,
            OfferingId   = offeringId,
            StudentId    = studentId,
            InstitutionType = selectedInstitutionType,
            AvailableInstitutionTypes = institutionFilter.AvailableInstitutionTypes,
            PeriodFilterLabel = institutionFilter.PeriodFilterLabel,
            PeriodFilterPlaceholder = institutionFilter.PeriodFilterPlaceholder
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
        var institutionFilter = await BuildReportInstitutionFilterAsync(ResolveReportInstitutionType(institutionType), ct);
        var selectedInstitutionType = institutionFilter.SelectedInstitutionType;
        ViewData["Title"] = "GPA & CGPA Report";
        var model = new ReportGpaPageModel
        {
            IsConnected  = _api.IsConnected(),
            DepartmentId = departmentId,
            ProgramId    = programId,
            InstitutionType = selectedInstitutionType,
            AvailableInstitutionTypes = institutionFilter.AvailableInstitutionTypes
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
        var institutionFilter = await BuildReportInstitutionFilterAsync(ResolveReportInstitutionType(institutionType), ct);
        var selectedInstitutionType = institutionFilter.SelectedInstitutionType;
        ViewData["Title"] = "Enrollment Summary Report";
        var model = new ReportEnrollmentPageModel
        {
            IsConnected  = _api.IsConnected(),
            SemesterId   = semesterId,
            DepartmentId = departmentId,
            InstitutionType = selectedInstitutionType,
            AvailableInstitutionTypes = institutionFilter.AvailableInstitutionTypes,
            PeriodFilterLabel = institutionFilter.PeriodFilterLabel,
            PeriodFilterPlaceholder = institutionFilter.PeriodFilterPlaceholder
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
        var institutionFilter = await BuildReportInstitutionFilterAsync(ResolveReportInstitutionType(institutionType), ct);
        var selectedInstitutionType = institutionFilter.SelectedInstitutionType;
        ViewData["Title"] = "Semester Results Report";
        var model = new ReportSemesterResultsPageModel
        {
            IsConnected  = _api.IsConnected(),
            SemesterId   = semesterId,
            DepartmentId = departmentId,
            InstitutionType = selectedInstitutionType,
            AvailableInstitutionTypes = institutionFilter.AvailableInstitutionTypes,
            PeriodFilterLabel = institutionFilter.PeriodFilterLabel,
            PeriodFilterPlaceholder = institutionFilter.PeriodFilterPlaceholder
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
        var institutionFilter = await BuildReportInstitutionFilterAsync(ResolveReportInstitutionType(institutionType), ct);
        var selectedInstitutionType = institutionFilter.SelectedInstitutionType;
        ViewData["Title"] = "Low Attendance Warning";
        var model = new ReportLowAttendancePageModel
        {
            IsConnected      = _api.IsConnected(),
            Threshold        = threshold,
            DepartmentId     = departmentId,
            CourseOfferingId = courseOfferingId,
            InstitutionType  = selectedInstitutionType,
            AvailableInstitutionTypes = institutionFilter.AvailableInstitutionTypes
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
        var institutionFilter = await BuildReportInstitutionFilterAsync(ResolveReportInstitutionType(institutionType), ct);
        var selectedInstitutionType = institutionFilter.SelectedInstitutionType;
        ViewData["Title"] = "FYP Status Report";
        var model = new ReportFypStatusPageModel
        {
            IsConnected    = _api.IsConnected(),
            DepartmentId   = departmentId,
            SelectedStatus = status,
            InstitutionType = selectedInstitutionType,
            AvailableInstitutionTypes = institutionFilter.AvailableInstitutionTypes
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
        var institutionFilter = await BuildReportInstitutionFilterAsync(ResolveReportInstitutionType(institutionType), ct);
        var selectedInstitutionType = institutionFilter.SelectedInstitutionType;
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
            InstitutionType = selectedInstitutionType,
            AvailableInstitutionTypes = institutionFilter.AvailableInstitutionTypes,
            PeriodFilterLabel = institutionFilter.PeriodFilterLabel,
            PeriodFilterPlaceholder = institutionFilter.PeriodFilterPlaceholder
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

    private async Task<(List<CertificateInstitutionOption> AvailableInstitutionTypes, int? SelectedInstitutionType, string PeriodFilterLabel, string PeriodFilterPlaceholder)> BuildReportInstitutionFilterAsync(int? requestedInstitutionType, CancellationToken ct)
    {
        var identity = _api.GetSessionIdentity();

        List<CertificateInstitutionOption> options;
        try
        {
            var matrix = await _api.GetPortalCapabilityMatrixAsync(ct);
            options = BuildLicensedInstitutionOptions(matrix);
        }
        catch
        {
            options = BuildLicensedInstitutionOptions(null);
        }

        var selected = ResolveLicensedInstitutionSelection(requestedInstitutionType, identity, options);

        if (identity is { IsSuperAdmin: false, InstitutionType: not null })
        {
            options = options.Where(x => x.Value == identity.InstitutionType.Value).ToList();
            if (options.Count == 0)
            {
                options.Add(new CertificateInstitutionOption
                {
                    Value = identity.InstitutionType.Value,
                    Label = identity.InstitutionType.Value switch
                    {
                        1 => "School",
                        2 => "College",
                        _ => "University"
                    }
                });
            }

            selected = identity.InstitutionType.Value;
        }

        var periodLabel = ResolvePeriodFilterLabel(options, selected);
        return (options, selected, periodLabel, ResolvePeriodFilterPlaceholder(periodLabel));
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
            var isStudent = User.IsInRole("Student");

            if (!isStudent)
            {
                model.Students = await _api.GetStudentsAsync(null, ct);
            }

            if (studentProfileId.HasValue)
            {
                model.SelectedStudentProfileId = studentProfileId;
                model.Audit = await _api.GetStudentDegreeAuditAsync(studentProfileId.Value, ct);
            }
            else if (isStudent)
            {
                // Student self-audit
                model.Audit = await _api.GetMyDegreeAuditAsync(ct);
            }
            else
            {
                TempData["Message"] = "Select a student to view degree audit.";
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

        if (!User.IsInRole("SuperAdmin"))
        {
            TempData["Message"] = "Only SuperAdmin can manage degree rules.";
            return RedirectToAction(nameof(Dashboard));
        }

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

        if (!User.IsInRole("SuperAdmin"))
        {
            TempData["Message"] = "Only SuperAdmin can manage degree rules.";
            return RedirectToAction(nameof(DegreeRules));
        }

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

        if (!User.IsInRole("SuperAdmin"))
        {
            TempData["Message"] = "Only SuperAdmin can manage degree rules.";
            return RedirectToAction(nameof(DegreeRules));
        }

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
        int? institutionType,
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
            SelectedInstitutionType = institutionType,
            Message = TempData["PortalMessage"]?.ToString()
        };

        if (!model.IsConnected)
            return View(model);

        try
        {
            var matrix = await _api.GetPortalCapabilityMatrixAsync(ct);
            model.AvailableInstitutionTypes = BuildLicensedInstitutionOptions(matrix);
            model.CanSelectInstitution = model.AvailableInstitutionTypes.Count > 1;
            model.SelectedInstitutionType = ResolveLicensedInstitutionSelection(
                model.SelectedInstitutionType,
                identity,
                model.AvailableInstitutionTypes);

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

            model.PeriodFilterLabel = ResolvePeriodFilterLabel(model.SelectedInstitutionType);
            model.Semesters = await _api.GetSemestersAsync(ct);
            model.AvailableDocumentTypes = BuildCertificateDocumentTypes(model.SelectedInstitutionType);

            model.Courses = await _api.GetCoursesAsync(model.SelectedDepartmentId, model.SelectedTenantId, model.SelectedCampusId, ct);

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
                model.SelectedSemesterId,
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
            return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId, institutionType });
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

        return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId, institutionType });
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
            return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId, institutionType });
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

        return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId, institutionType });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadCertificateTemplate(
        string? templateType,
        Guid? tenantId,
        Guid? campusId,
        Guid? departmentId,
        Guid? courseId,
        int? institutionType,
        Guid? semesterId,
        CancellationToken ct)
    {
        if (!_api.IsConnected())
            return RedirectToAction("Connect", "Home");

        var normalizedType = NormalizeCertificateTemplateType(templateType);
        var isUniversityScope = IsUniversityInstitutionType(institutionType);
        if (isUniversityScope && !IsUniversityDocumentType(normalizedType))
        {
            TempData["PortalMessage"] = "Selected template type is not available for university scope.";
            return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId, institutionType });
        }

        if (!isUniversityScope && !IsNonUniversityDocumentType(normalizedType))
        {
            TempData["PortalMessage"] = "Selected template type is not available for school/college scope.";
            return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId, institutionType });
        }

        try
        {
            var bytes = await _api.DownloadCertificateTemplateAsync(normalizedType, ct);
            if (bytes is null || bytes.Length == 0)
            {
                TempData["PortalMessage"] = "Certificate template is not available.";
                return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId, institutionType });
            }

            var fileName = normalizedType switch
            {
                "transcript" => "transcript-template.docx",
                "completion" => "completion-certificate-template.docx",
                "reportcard" => "report-card-template.docx",
                _ => "degree-template.docx"
            };
            return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
            return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId, institutionType });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadCertificateTemplate(
        string? templateType,
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

        var identity = _api.GetSessionIdentity();
        if (identity?.IsAdmin != true && identity?.IsSuperAdmin != true)
        {
            TempData["PortalMessage"] = "Only Admin or SuperAdmin can upload certificate templates.";
            return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId });
        }

        var normalizedType = NormalizeCertificateTemplateType(templateType);
        var isUniversityScope = IsUniversityInstitutionType(institutionType);
        if (isUniversityScope && !IsUniversityDocumentType(normalizedType))
        {
            TempData["PortalMessage"] = "Only Degree/Transcript templates are available for university scope.";
            return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId, institutionType });
        }

        if (!isUniversityScope && !IsNonUniversityDocumentType(normalizedType))
        {
            TempData["PortalMessage"] = "Only Completion Certificate and Report Card templates are available for school/college scope.";
            return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId, institutionType });
        }

        if (file is null || file.Length == 0)
        {
            TempData["PortalMessage"] = "Please select a .docx certificate template file to import.";
            return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId, institutionType });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            await _api.UploadCertificateTemplateAsync(normalizedType, stream, file.FileName, file.ContentType, ct);
            TempData["PortalMessage"] = normalizedType switch
            {
                "transcript" => "Transcript template imported successfully.",
                "completion" => "Completion certificate template imported successfully.",
                "reportcard" => "Report card template imported successfully.",
                _ => "Degree template imported successfully."
            };
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId, institutionType });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateAdditionalCertificate(
        Guid studentProfileId,
        string? documentType,
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
            TempData["PortalMessage"] = "Completion Certificate and Report Card generation is available only for school/college scope.";
            return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId, institutionType });
        }

        var normalizedType = NormalizeCertificateTemplateType(documentType);
        if (!IsNonUniversityDocumentType(normalizedType))
        {
            TempData["PortalMessage"] = "Please select Completion Certificate or Report Card before generating.";
            return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId, institutionType });
        }

        try
        {
            await _api.GenerateAdditionalCertificateAsync(studentProfileId, normalizedType, ct);
            TempData["PortalMessage"] = normalizedType == "completion"
                ? "Completion certificate generated successfully."
                : "Report card generated successfully.";
        }
        catch (Exception ex)
        {
            TempData["PortalMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId, institutionType });
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
            return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId, institutionType });
        }

        if (file is null || file.Length == 0)
        {
            TempData["PortalMessage"] = "Please select a certificate file to upload.";
            return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId, institutionType });
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

        return RedirectToAction(nameof(GenerateCertificates), new { tenantId, campusId, departmentId, courseId, semesterId, institutionType });
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
    public async Task<IActionResult> Announcements(Guid offeringId, bool includeInactive = false, CancellationToken ct = default)
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
    public async Task<IActionResult> StudyPlan(Guid? studentProfileId, Guid? tenantId, Guid? campusId, Guid? departmentId, CancellationToken ct)
    {
        ViewData["Title"] = "Study Plans";
        var identity = _api.GetSessionIdentity();
        var model = new StudyPlanPageModel
        {
            StudentProfileId = studentProfileId ?? Guid.Empty,
            SelectedStudentProfileId = studentProfileId,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId,
            SelectedDepartmentId = departmentId,
            IsConnected = _api.IsConnected()
        };
        if (TempData["SuccessMessage"] is string s) model.SuccessMessage = s;
        if (TempData["ErrorMessage"]   is string e) model.ErrorMessage   = e;
        if (_api.IsConnected())
        {
            try
            {
                if (identity?.IsSuperAdmin != true)
                {
                    model.SelectedTenantId ??= identity?.TenantId;
                    model.SelectedCampusId ??= identity?.CampusId;
                }

                if (identity?.IsSuperAdmin == true)
                {
                    model.Tenants = (await _api.GetTenantsAsync(ct))
                        .Select(t => new LookupItem { Id = t.Id, Name = t.Name })
                        .ToList();
                    if (model.SelectedTenantId.HasValue)
                        model.Campuses = (await _api.GetCampusesAsync(model.SelectedTenantId, ct))
                            .Select(c => new LookupItem { Id = c.Id, Name = c.Name })
                            .ToList();
                }

                model.Departments = await _api.GetDepartmentsAsync(model.SelectedTenantId, model.SelectedCampusId, ct);
                model.Students = await _api.GetStudentsAsync(model.SelectedDepartmentId, ct);

                if (!model.SelectedStudentProfileId.HasValue && model.StudentProfileId != Guid.Empty)
                    model.SelectedStudentProfileId = model.StudentProfileId;

                if (model.SelectedStudentProfileId.HasValue)
                    model.StudentProfileId = model.SelectedStudentProfileId.Value;

                List<StudyPlanApiModel> plans;
                if (model.StudentProfileId != Guid.Empty)
                {
                    plans = await _api.GetStudyPlansAsync(model.StudentProfileId, ct);
                }
                else if (model.SelectedDepartmentId.HasValue)
                {
                    plans = await _api.GetStudyPlansByDepartmentAsync(model.SelectedDepartmentId.Value, ct);
                }
                else
                {
                    plans = new List<StudyPlanApiModel>();
                }

                model.Plans = plans.Select(p => MapStudyPlanItem(p)).ToList();
            }
            catch (Exception ex) { model.ErrorMessage = ex.Message; }
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> StudyPlanDetail(Guid planId, Guid? studentProfileId, Guid? tenantId, Guid? campusId, Guid? departmentId, CancellationToken ct)
    {
        ViewData["Title"] = "Study Plan Detail";
        var model = new StudyPlanDetailPageModel
        {
            IsConnected = _api.IsConnected(),
            SelectedStudentProfileId = studentProfileId,
            SelectedTenantId = tenantId,
            SelectedCampusId = campusId,
            SelectedDepartmentId = departmentId
        };
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
    public async Task<IActionResult> CreateStudyPlan(Guid studentProfileId, Guid? tenantId, Guid? campusId, Guid? departmentId, string plannedSemesterName, string? notes, CancellationToken ct)
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
        return RedirectToAction(nameof(StudyPlan), new { studentProfileId, departmentId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddStudyPlanCourse(Guid planId, Guid courseId, Guid studentProfileId, Guid? tenantId, Guid? campusId, Guid? departmentId, CancellationToken ct)
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
        return RedirectToAction(nameof(StudyPlanDetail), new { planId, studentProfileId, departmentId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveStudyPlanCourse(Guid planId, Guid courseId, Guid studentProfileId, Guid? tenantId, Guid? campusId, Guid? departmentId, CancellationToken ct)
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
        return RedirectToAction(nameof(StudyPlanDetail), new { planId, studentProfileId, departmentId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStudyPlan(Guid planId, Guid studentProfileId, Guid? tenantId, Guid? campusId, Guid? departmentId, CancellationToken ct)
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
        return RedirectToAction(nameof(StudyPlan), new { studentProfileId, departmentId, tenantId, campusId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AdvisePlan(Guid planId, Guid studentProfileId, Guid? tenantId, Guid? campusId, Guid? departmentId, bool isEndorsed, string? advisorNotes, CancellationToken ct)
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
        return RedirectToAction(nameof(StudyPlanDetail), new { planId, studentProfileId, departmentId, tenantId, campusId });
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
                        CourseType  = NormalizeCourseTypeValue(r.CourseType),
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
        AdvisorStatus       = NormalizeAdvisorStatusValue(p.AdvisorStatus),
        AdvisorNotes        = p.AdvisorNotes,
        ReviewedByUserId    = p.ReviewedByUserId,
        TotalCreditHours    = p.TotalCreditHours,
        Courses             = p.Courses.Select(c => new StudyPlanCourseItem
        {
            CourseId    = c.CourseId,
            CourseCode  = c.CourseCode,
            CourseTitle = c.CourseTitle,
            CreditHours = c.CreditHours,
            CourseType  = NormalizeCourseTypeValue(c.CourseType)
        }).ToList(),
        CreatedAt = p.CreatedAt
    };

    private static string NormalizeAdvisorStatusValue(System.Text.Json.JsonElement value)
    {
        if (value.ValueKind == System.Text.Json.JsonValueKind.String)
            return value.GetString() ?? "Pending";

        if (value.ValueKind == System.Text.Json.JsonValueKind.Number && value.TryGetInt32(out var status))
        {
            return status switch
            {
                1 => "Endorsed",
                2 => "Rejected",
                _ => "Pending"
            };
        }

        return "Pending";
    }

    private static string NormalizeCourseTypeValue(System.Text.Json.JsonElement value)
    {
        if (value.ValueKind == System.Text.Json.JsonValueKind.String)
            return value.GetString() ?? string.Empty;

        if (value.ValueKind == System.Text.Json.JsonValueKind.Number && value.TryGetInt32(out var courseType))
        {
            return courseType switch
            {
                1 => "Elective",
                2 => "Lab",
                _ => "Core"
            };
        }

        return "Core";
    }

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

