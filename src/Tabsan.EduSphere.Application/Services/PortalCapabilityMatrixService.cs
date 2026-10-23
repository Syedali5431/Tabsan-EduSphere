using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Modules;
using Tabsan.EduSphere.Domain.Enums;

namespace Tabsan.EduSphere.Application.Services;

// Phase 27 — University Portal Parity and Student Experience — Stage 27.1

public sealed class PortalCapabilityMatrixService : IPortalCapabilityMatrixService
{
    private readonly IModuleRegistryService _moduleRegistry;

    private static readonly string[] Roles = ["Student", "Faculty", "Admin", "SuperAdmin"];

    private sealed record CapabilitySpec(
        string Key,
        string Name,
        string ModuleKey,
        string Route,
        string Description);

    private static readonly CapabilitySpec[] Specs =
    [
        new("single_sign_on", "Single Sign-On", "sso", "/Portal/SsoSettings", "Sign in with Microsoft, Google or the institution's SAML identity provider."),
        new("external_tools", "External Tools (LTI 1.3)", "lti", "/Portal/LtiTools", "Launch LTI 1.3 tools (H5P, Zoom, Turnitin…) from courses, with grades and rosters."),
        new("learning_outcomes", "Learning Outcomes & Mastery", "outcomes", "/Portal/Outcomes", "Course and program outcomes aligned to rubrics and questions, mastery gradebook and accreditation attainment reports."),
        new("similarity_checking", "Similarity / Plagiarism Checking", "similarity", "/Portal/SimilaritySettings", "Built-in comparison with other submissions or Copyleaks, with scores and reports in the grading workspace."),
        new("live_classes", "Live Classes", "live_classes", "/Portal/LiveClasses", "Schedule BigBlueButton, Zoom or Teams classes from the timetable, join from the dashboard, attendance from who joined."),
        new("dashboard_overview", "Dashboard Overview", "authentication", "/Portal/Dashboard", "Role-aware dashboard with quick actions and summary tiles."),
        new("courses_enrollments", "Course and Enrollment Access", "courses", "/Portal/Courses", "Course catalog, offerings, and enrollment workflows."),
        new("assignments_workflow", "Assignments Workflow", "assignments", "/Portal/Assignments", "Assignment creation, submission, grading, and review."),
        new("quizzes_feedback", "Quiz and Feedback", "quizzes", "/Portal/Quizzes", "Quiz authoring/attempting and post-attempt feedback."),
        new("attendance_tracking", "Attendance Tracking", "attendance", "/Portal/Attendance", "Attendance mark/correction and student attendance view."),
        new("timetable_schedule", "Timetable and Exam Schedule", "courses", "/Portal/TimetableStudent", "Timetable views for student/faculty/admin schedule planning."),
        new("results_transcript", "Results and Transcript", "results", "/Portal/Results", "Result publishing/view and transcript access/export."),
        new("fees_payments", "Fees and Payments", "sis", "/Portal/Payments", "Fee receipt creation, proof submission, and payment status tracking."),
        new("notifications_messaging", "Notifications and Messaging", "notifications", "/Portal/Notifications", "In-app notifications, unread tracking, and communication alerts."),
        new("support_ticketing", "Support and Helpdesk", "notifications", "/Portal/Helpdesk", "Support ticketing, assignment, and threaded responses."),
        new("ai_assistant", "AI Assistant", "ai_chat", "/Portal/AiChat", "Context-aware AI chat assistance for portal users."),
        new("fyp_workspace", "FYP Workspace", "fyp", "/Portal/Fyp", "University-mode final year project workflows and supervision."),
        new("reports_analytics", "Reports and Analytics", "reports", "/Portal/ReportCenter", "Operational and academic report center with exports.")
    ];

    public PortalCapabilityMatrixService(IModuleRegistryService moduleRegistry)
    {
        _moduleRegistry = moduleRegistry;
    }

    public async Task<PortalCapabilityMatrixResponse> GetMatrixAsync(InstitutionPolicySnapshot policy, CancellationToken ct = default)
    {
        var byRole = new Dictionary<string, IReadOnlyDictionary<string, ModuleVisibilityResult>>(StringComparer.OrdinalIgnoreCase);

        foreach (var role in Roles)
        {
            var visible = await _moduleRegistry.GetVisibleModulesAsync(role, policy, ct);
            byRole[role] = visible.ToDictionary(x => x.Key, x => x, StringComparer.OrdinalIgnoreCase);
        }

        var rows = Specs.Select(spec => BuildRow(spec, byRole, policy)).ToList();

        return new PortalCapabilityMatrixResponse(
            policy.IncludeSchool,
            policy.IncludeCollege,
            policy.IncludeUniversity,
            rows);
    }

    private static PortalCapabilityMatrixRow BuildRow(
        CapabilitySpec spec,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, ModuleVisibilityResult>> byRole,
        InstitutionPolicySnapshot policy)
    {
        byRole["SuperAdmin"].TryGetValue(spec.ModuleKey, out var superAdminModule);
        var moduleName = superAdminModule?.Name ?? spec.ModuleKey;

        var descriptor = ModuleRegistry.Get(spec.ModuleKey);
        var isLicenseGated = descriptor?.IsLicenseGated ?? false;
        var isModuleActive = superAdminModule?.IsActive ?? false;

        static bool HasAccess(
            string role,
            string moduleKey,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, ModuleVisibilityResult>> map)
            => map.TryGetValue(role, out var modules)
               && modules.TryGetValue(moduleKey, out var m)
               && m.IsActive
               && m.IsAccessible;

        var university = (descriptor?.TypeMatches(InstitutionType.University) ?? true) && policy.IncludeUniversity;
        var school = (descriptor?.TypeMatches(InstitutionType.School) ?? true) && policy.IncludeSchool;
        var college = (descriptor?.TypeMatches(InstitutionType.College) ?? true) && policy.IncludeCollege;

        return new PortalCapabilityMatrixRow(
            spec.Key,
            spec.Name,
            spec.ModuleKey,
            moduleName,
            spec.Route,
            spec.Description,
            isModuleActive,
            isLicenseGated,
            HasAccess("Student", spec.ModuleKey, byRole),
            HasAccess("Faculty", spec.ModuleKey, byRole),
            HasAccess("Admin", spec.ModuleKey, byRole),
            HasAccess("SuperAdmin", spec.ModuleKey, byRole),
            university,
            school,
            college);
    }
}
