using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Modules;
using Tabsan.EduSphere.Application.Services;
using Tabsan.EduSphere.Domain.Enums;
using Xunit;

namespace Tabsan.EduSphere.UnitTests;

// ── Stage 24.2 — LabelService tests ──────────────────────────────────────────

public class LabelServiceTests
{
    private static readonly ILabelService _svc = new LabelService();

    [Fact]
    public void University_ReturnsDefaultVocabulary()
    {
        var snap = new InstitutionPolicySnapshot(false, false, true);
        var v = _svc.GetVocabulary(snap);
        v.PeriodLabel.Should().Be("Semester");
        v.GradingLabel.Should().Be("GPA/CGPA");
        v.CourseLabel.Should().Be("Course");
    }

    [Fact]
    public void School_ReturnsSchoolVocabulary()
    {
        var snap = new InstitutionPolicySnapshot(true, false, false);
        var v = _svc.GetVocabulary(snap);
        v.PeriodLabel.Should().Be("Grade");
        v.GradingLabel.Should().Be("Percentage");
        v.CourseLabel.Should().Be("Subject");
        v.StudentGroupLabel.Should().Be("Class");
    }

    [Fact]
    public void College_ReturnsCollegeVocabulary()
    {
        var snap = new InstitutionPolicySnapshot(false, true, false);
        var v = _svc.GetVocabulary(snap);
        v.PeriodLabel.Should().Be("Year");
        v.GradingLabel.Should().Be("Percentage");
        v.StudentGroupLabel.Should().Be("Year-Group");
    }

    [Fact]
    public void MixedWithUniversity_PrefersUniversityVocabulary()
    {
        // When University is one of the enabled types, keep University vocab
        var snap = new InstitutionPolicySnapshot(true, true, true);
        var v = _svc.GetVocabulary(snap);
        v.PeriodLabel.Should().Be("Semester");
    }
}

// ── Stage 24.3 — DashboardCompositionService tests ───────────────────────────

public class DashboardCompositionTests
{
    private static readonly IDashboardCompositionService _svc =
        new DashboardCompositionService(new MemoryCache(new MemoryCacheOptions()));

    private static InstitutionPolicySnapshot UnivPolicy
        => new(false, false, true);

    private static InstitutionPolicySnapshot SchoolPolicy
        => new(true, false, false);

    [Fact]
    public void SuperAdmin_University_Gets_SystemHealth_And_EnrollmentStats()
    {
        var widgets = _svc.GetWidgets("SuperAdmin", UnivPolicy);
        widgets.Select(w => w.Key).Should().Contain("system_health");
        widgets.Select(w => w.Key).Should().Contain("enrollment_stats");
    }

    [Fact]
    public void Faculty_University_Gets_FypPanel()
    {
        var widgets = _svc.GetWidgets("Faculty", UnivPolicy);
        widgets.Select(w => w.Key).Should().Contain("fyp_panel");
    }

    [Fact]
    public void Faculty_School_DoesNotGet_FypPanel()
    {
        var widgets = _svc.GetWidgets("Faculty", SchoolPolicy);
        widgets.Select(w => w.Key).Should().NotContain("fyp_panel");
    }

    [Fact]
    public void Student_Gets_MyCourses_And_AttendanceSummary()
    {
        var widgets = _svc.GetWidgets("Student", UnivPolicy);
        widgets.Select(w => w.Key).Should().Contain("my_courses");
        widgets.Select(w => w.Key).Should().Contain("attendance_summary");
    }

    [Fact]
    public void Student_DoesNotGet_SystemHealth()
    {
        var widgets = _svc.GetWidgets("Student", UnivPolicy);
        widgets.Select(w => w.Key).Should().NotContain("system_health");
    }

    [Fact]
    public void All_Roles_Get_AiAssistant()
    {
        foreach (var role in new[] { "SuperAdmin", "Admin", "Faculty", "Student" })
        {
            var widgets = _svc.GetWidgets(role, UnivPolicy);
            widgets.Select(w => w.Key).Should().Contain("ai_assistant",
                because: $"role {role} should always see ai_assistant");
        }
    }

    [Fact]
    public void Widgets_Are_Ordered_By_Order_Property()
    {
        var widgets = _svc.GetWidgets("SuperAdmin", UnivPolicy).ToList();
        for (int i = 1; i < widgets.Count; i++)
            widgets[i].Order.Should().BeGreaterThanOrEqualTo(widgets[i - 1].Order);
    }
}

// ── Stage 24.1 — ModuleRegistry static catalogue tests ───────────────────────

public class ModuleRegistryTests
{
    [Fact]
    public void Registry_ContainsAllKnownKeys()
    {
        var expected = new[]
        {
            "authentication", "departments", "courses", "sis",
            "assignments", "attendance", "results", "quizzes",
            "fyp", "notifications", "ai_chat", "reports",
            "themes", "advanced_audit",
            "iso_compliance", "backup_dr", "document_management"
        };

        foreach (var key in expected)
            ModuleRegistry.Get(key).Should().NotBeNull(because: $"'{key}' must be registered");
    }

    [Fact]
    public void Fyp_AllowedTypes_UniversityOnly()
    {
        var d = ModuleRegistry.Get("fyp")!;
        d.AllowedTypes.Should().ContainSingle(t => t == InstitutionType.University);
        d.TypeMatches(InstitutionType.School).Should().BeFalse();
        d.TypeMatches(InstitutionType.College).Should().BeFalse();
    }

    [Fact]
    public void AiChat_IsLicenseGated()
        => ModuleRegistry.Get("ai_chat")!.IsLicenseGated.Should().BeTrue();

    [Fact]
    public void AdvancedAudit_OnlyAccessibleBySuperAdmin()
    {
        var d = ModuleRegistry.Get("advanced_audit")!;
        d.RoleMatches("SuperAdmin").Should().BeTrue();
        d.RoleMatches("Admin").Should().BeFalse();
        d.RoleMatches("Faculty").Should().BeFalse();
        d.RoleMatches("Student").Should().BeFalse();
    }

    [Fact]
    public void Authentication_AccessibleByAllRoles()
    {
        var d = ModuleRegistry.Get("authentication")!;
        foreach (var role in new[] { "SuperAdmin", "Admin", "Faculty", "Student" })
            d.RoleMatches(role).Should().BeTrue(because: role);
    }

    [Fact]
    public void UnknownKey_ReturnsNull()
        => ModuleRegistry.Get("nonexistent_module").Should().BeNull();
}
