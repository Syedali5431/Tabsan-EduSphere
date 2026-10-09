using FluentAssertions;
using Tabsan.EduSphere.Application.Academic;
using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Xunit;

namespace Tabsan.EduSphere.UnitTests;

// Phase 25 — Academic Engine Unification Unit Tests

// ── Stage 25.1 — GpaResultStrategy ───────────────────────────────────────────

public class GpaResultStrategyTests
{
    private readonly GpaResultStrategy _sut = new();

    private static readonly IReadOnlyList<GpaScaleRuleEntry> DefaultScale =
    [
        new(4.0m, 90m),
        new(3.5m, 80m),
        new(3.0m, 70m),
        new(2.5m, 60m),
        new(2.0m, 50m),
        new(1.0m, 40m),
    ];

    [Fact]
    public void AppliesTo_ReturnsUniversity()
        => _sut.AppliesTo.Should().Be(InstitutionType.University);

    [Fact]
    public void EmptyMarks_ReturnsZeroSummary()
    {
        var result = _sut.Calculate([], DefaultScale, 2.0m, null);
        result.TotalScore.Should().Be(0m);
        result.GradePoint.Should().BeNull();
        result.IsPassing.Should().BeFalse();
    }

    [Fact]
    public void FullMarks_Returns4Point0Gpa()
    {
        var marks = new[]
        {
            new ComponentMark("Quiz", 100m, 100m, 20m),
            new ComponentMark("Midterm", 100m, 100m, 30m),
            new ComponentMark("Final", 100m, 100m, 50m),
        };
        var result = _sut.Calculate(marks, DefaultScale, 2.0m, null);
        result.GradePoint.Should().Be(4.0m);
        result.IsPassing.Should().BeTrue();
    }

    [Fact]
    public void LowMarks_BelowPass_ReturnsNotPassing()
    {
        // 30% total → below minimum scale threshold (40) → null GPA → failing
        var marks = new[]
        {
            new ComponentMark("Final", 30m, 100m, 100m),
        };
        var result = _sut.Calculate(marks, DefaultScale, 2.0m, null);
        result.GradePoint.Should().BeNull();
        result.IsPassing.Should().BeFalse();
        result.GradeLabel.Should().Be("F");
    }

    [Fact]
    public void ExactPassThreshold_ReturnsPassingTrue()
    {
        // 50% → GPA 2.0, pass threshold 2.0
        var marks = new[]
        {
            new ComponentMark("Final", 50m, 100m, 100m),
        };
        var result = _sut.Calculate(marks, DefaultScale, 2.0m, null);
        result.GradePoint.Should().Be(2.0m);
        result.IsPassing.Should().BeTrue();
    }

    [Fact]
    public void MultipleComponents_ComputesWeightedPercentageCorrectly()
    {
        // Quiz 20/20 (100%), Midterm 15/30 (50%), Final 35/50 (70%)
        // Weighted: (100*20 + 50*30 + 70*50) / 100 = (2000+1500+3500)/100 = 70%
        var marks = new[]
        {
            new ComponentMark("Quiz", 20m, 20m, 20m),
            new ComponentMark("Midterm", 15m, 30m, 30m),
            new ComponentMark("Final", 35m, 50m, 50m),
        };
        var result = _sut.Calculate(marks, DefaultScale, 2.0m, null);
        result.GradePoint.Should().Be(3.0m); // 70% → 3.0
        result.IsPassing.Should().BeTrue();
    }
}

// ── Stage 25.1 — PercentageResultStrategy ────────────────────────────────────

public class PercentageResultStrategyTests
{
    private readonly PercentageResultStrategy _school = new(InstitutionType.School);
    private readonly PercentageResultStrategy _college = new(InstitutionType.College);

    [Fact]
    public void School_AppliesTo_ReturnsSchool()
        => _school.AppliesTo.Should().Be(InstitutionType.School);

    [Fact]
    public void College_AppliesTo_ReturnsCollege()
        => _college.AppliesTo.Should().Be(InstitutionType.College);

    [Fact]
    public void University_ThrowsArgumentException()
    {
        var act = () => new PercentageResultStrategy(InstitutionType.University);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FullMarks_ReturnsAPlus()
    {
        var marks = new[] { new ComponentMark("Total", 100m, 100m, 100m) };
        var result = _school.Calculate(marks, [], 40m, null);
        result.GradeLabel.Should().Be("A+");
        result.IsPassing.Should().BeTrue();
        result.GradePoint.Should().BeNull();
    }

    [Fact]
    public void BelowPassThreshold_ReturnsFAndFailing()
    {
        var marks = new[] { new ComponentMark("Total", 30m, 100m, 100m) };
        var result = _school.Calculate(marks, [], 40m, null);
        result.IsPassing.Should().BeFalse();
        result.GradeLabel.Should().Be("F");
    }

    [Fact]
    public void CustomGradeBands_AreUsed()
    {
        var bands = "[{\"From\":70,\"To\":100,\"Label\":\"Distinction\"},{\"From\":50,\"To\":69,\"Label\":\"Merit\"},{\"From\":0,\"To\":49,\"Label\":\"Fail\"}]";
        var marks = new[] { new ComponentMark("Total", 75m, 100m, 100m) };
        var result = _college.Calculate(marks, [], 50m, bands);
        result.GradeLabel.Should().Be("Distinction");
        result.IsPassing.Should().BeTrue();
    }

    [Fact]
    public void InvalidGradeBandsJson_FallsBackToDefaults()
    {
        var marks = new[] { new ComponentMark("Total", 85m, 100m, 100m) };
        var result = _school.Calculate(marks, [], 40m, "not-valid-json");
        result.GradeLabel.Should().Be("A"); // default band for 80–89.99
    }
}

// ── Stage 25.1 — ResultStrategyResolver ──────────────────────────────────────

public class ResultStrategyResolverTests
{
    private readonly ResultStrategyResolver _sut = new();

    [Fact]
    public void University_ReturnsGpaStrategy()
        => _sut.Resolve(InstitutionType.University).AppliesTo.Should().Be(InstitutionType.University);

    [Fact]
    public void School_ReturnsPercentageStrategy()
        => _sut.Resolve(InstitutionType.School).AppliesTo.Should().Be(InstitutionType.School);

    [Fact]
    public void College_ReturnsPercentageStrategy()
        => _sut.Resolve(InstitutionType.College).AppliesTo.Should().Be(InstitutionType.College);

    [Fact]
    public void Resolver_ReturnsDistinctInstancesForSchoolAndCollege()
    {
        var school = _sut.Resolve(InstitutionType.School);
        var college = _sut.Resolve(InstitutionType.College);
        school.Should().NotBeSameAs(college);
    }
}

// ── Stage 25.2 — InstitutionGradingProfile domain entity ─────────────────────

public class InstitutionGradingProfileTests
{
    [Fact]
    public void University_ValidThreshold_Constructs()
    {
        var p = new InstitutionGradingProfile(InstitutionType.University, 2.0m, null);
        p.PassThreshold.Should().Be(2.0m);
        p.IsActive.Should().BeTrue();
    }

    [Fact]
    public void School_ValidThreshold_Constructs()
    {
        var p = new InstitutionGradingProfile(InstitutionType.School, 40m, null);
        p.PassThreshold.Should().Be(40m);
    }

    [Fact]
    public void University_ThresholdAbove4_Throws()
    {
        var act = () => new InstitutionGradingProfile(InstitutionType.University, 5.0m, null);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void School_ThresholdAbove100_Throws()
    {
        var act = () => new InstitutionGradingProfile(InstitutionType.School, 101m, null);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Update_ChangesValues()
    {
        var p = new InstitutionGradingProfile(InstitutionType.College, 40m, null);
        p.Update(50m, "[{}]", false);
        p.PassThreshold.Should().Be(50m);
        p.IsActive.Should().BeFalse();
    }
}

// ── Stage 25.3 — ProgressionService (in-memory stubs) ────────────────────────

file sealed class StubStudentProfileRepository : Tabsan.EduSphere.Domain.Interfaces.IStudentProfileRepository
{
    private readonly StudentProfile _student;
    public StubStudentProfileRepository(StudentProfile student) => _student = student;

    public Task<StudentProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult<StudentProfile?>(null);
    public Task<StudentProfile?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<StudentProfile?>(_student);
    public Task<StudentProfile?> GetByRegistrationNumberAsync(string reg, CancellationToken ct) => Task.FromResult<StudentProfile?>(null);
    public Task<IReadOnlyList<StudentProfile>> GetAllAsync(Guid? deptId, CancellationToken ct) => Task.FromResult<IReadOnlyList<StudentProfile>>([]);
    public Task<bool> RegistrationNumberExistsAsync(string reg, CancellationToken ct) => Task.FromResult(false);
    public Task AddAsync(StudentProfile p, CancellationToken ct) => Task.CompletedTask;
    public void Update(StudentProfile p) { }
    public Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(1);
}

file sealed class StubGradingProfileRepository : Tabsan.EduSphere.Domain.Interfaces.IInstitutionGradingProfileRepository
{
    private readonly InstitutionGradingProfile? _profile;
    public StubGradingProfileRepository(InstitutionGradingProfile? profile) => _profile = profile;

    public Task<IReadOnlyList<InstitutionGradingProfile>> GetAllAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<InstitutionGradingProfile>>([]);
    public Task<InstitutionGradingProfile?> GetByTypeAsync(InstitutionType type, CancellationToken ct) => Task.FromResult(_profile);
    public Task<InstitutionGradingProfile?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(_profile);
    public Task AddAsync(InstitutionGradingProfile p, CancellationToken ct) => Task.CompletedTask;
    public void Update(InstitutionGradingProfile p) { }
    public Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(1);
}

public class ProgressionServiceTests
{
    private static StudentProfile MakeStudent(decimal cgpa, decimal semGpa, int semesterNum = 1)
    {
        // Use reflection to bypass private constructor in tests.
        var s = (StudentProfile)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(StudentProfile));
        // Set private backing fields via reflection.
        SetPrivate(s, "Cgpa", cgpa);
        SetPrivate(s, "CurrentSemesterGpa", semGpa);
        SetPrivate(s, "CurrentSemesterNumber", semesterNum);
        SetPrivate(s, "Id", Guid.NewGuid());
        SetPrivate(s, "Status", StudentStatus.Active);
        return s;
    }

    private static void SetPrivate(object obj, string propName, object value)
    {
        var prop = obj.GetType().GetProperty(propName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (prop != null)
        {
            prop.SetValue(obj, value);
            return;
        }
        var field = obj.GetType().GetField($"<{propName}>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(obj, value);
    }

    [Fact]
    public async Task University_AboveThreshold_CanProgress()
    {
        var student = MakeStudent(cgpa: 3.0m, semGpa: 3.0m);
        var gradingProfile = new InstitutionGradingProfile(InstitutionType.University, 2.0m, null);
        var svc = new ProgressionService(
            new StubStudentProfileRepository(student),
            new StubGradingProfileRepository(gradingProfile));

        var decision = await svc.EvaluateAsync(
            new ProgressionEvaluationRequest(student.Id, InstitutionType.University));

        decision.CanProgress.Should().BeTrue();
        decision.AchievedScore.Should().Be(3.0m);
    }

    [Fact]
    public async Task University_BelowThreshold_CannotProgress()
    {
        var student = MakeStudent(cgpa: 1.5m, semGpa: 1.5m);
        var gradingProfile = new InstitutionGradingProfile(InstitutionType.University, 2.0m, null);
        var svc = new ProgressionService(
            new StubStudentProfileRepository(student),
            new StubGradingProfileRepository(gradingProfile));

        var decision = await svc.EvaluateAsync(
            new ProgressionEvaluationRequest(student.Id, InstitutionType.University));

        decision.CanProgress.Should().BeFalse();
    }

    [Fact]
    public async Task School_AboveThreshold_CanProgress()
    {
        var student = MakeStudent(cgpa: 0m, semGpa: 65m);
        var gradingProfile = new InstitutionGradingProfile(InstitutionType.School, 40m, null);
        var svc = new ProgressionService(
            new StubStudentProfileRepository(student),
            new StubGradingProfileRepository(gradingProfile));

        var decision = await svc.EvaluateAsync(
            new ProgressionEvaluationRequest(student.Id, InstitutionType.School));

        decision.CanProgress.Should().BeTrue();
        decision.InstitutionType.Should().Be(InstitutionType.School);
    }

    [Fact]
    public async Task NoGradingProfile_UsesDefaultThreshold_University()
    {
        var student = MakeStudent(cgpa: 2.5m, semGpa: 2.5m);
        var svc = new ProgressionService(
            new StubStudentProfileRepository(student),
            new StubGradingProfileRepository(null)); // no profile → uses default 2.0

        var decision = await svc.EvaluateAsync(
            new ProgressionEvaluationRequest(student.Id, InstitutionType.University));

        decision.CanProgress.Should().BeTrue();
        decision.RequiredScore.Should().Be(2.0m);
    }

    [Fact]
    public async Task Promote_WhenEligible_AdvancesStudentAndReturnsSemester2Label()
    {
        var student = MakeStudent(cgpa: 3.0m, semGpa: 3.0m, semesterNum: 1);
        var gradingProfile = new InstitutionGradingProfile(InstitutionType.University, 2.0m, null);
        var svc = new ProgressionService(
            new StubStudentProfileRepository(student),
            new StubGradingProfileRepository(gradingProfile));

        var decision = await svc.PromoteAsync(
            new ProgressionEvaluationRequest(student.Id, InstitutionType.University));

        // After promotion the student advances to semester 2.
        decision.CurrentPeriodLabel.Should().Be("Semester 2");
    }

    [Fact]
    public async Task College_Promote_WhenEligible_AdvancesOneClass()
    {
        // College uses absolute class numbers (11, 12); one promotion moves the student up one class.
        var student = MakeStudent(cgpa: 0m, semGpa: 65m, semesterNum: 11);
        var gradingProfile = new InstitutionGradingProfile(InstitutionType.College, 40m, null);
        var svc = new ProgressionService(
            new StubStudentProfileRepository(student),
            new StubGradingProfileRepository(gradingProfile));

        var decision = await svc.PromoteAsync(
            new ProgressionEvaluationRequest(student.Id, InstitutionType.College));

        decision.CurrentPeriodLabel.Should().Be("Class 12");
    }

    [Fact]
    public async Task College_GpaScaleStanding_IsNormalizedToPercentage()
    {
        var student = MakeStudent(cgpa: 0m, semGpa: 2.0m, semesterNum: 1);
        var gradingProfile = new InstitutionGradingProfile(InstitutionType.College, 40m, null);
        var svc = new ProgressionService(
            new StubStudentProfileRepository(student),
            new StubGradingProfileRepository(gradingProfile));

        var decision = await svc.EvaluateAsync(
            new ProgressionEvaluationRequest(student.Id, InstitutionType.College));

        decision.AchievedScore.Should().Be(50m);
        decision.CanProgress.Should().BeTrue();
    }

    [Fact]
    public async Task Promote_WhenNotEligible_ThrowsInvalidOperationException()
    {
        var student = MakeStudent(cgpa: 1.0m, semGpa: 1.0m);
        var gradingProfile = new InstitutionGradingProfile(InstitutionType.University, 2.0m, null);
        var svc = new ProgressionService(
            new StubStudentProfileRepository(student),
            new StubGradingProfileRepository(gradingProfile));

        var act = () => svc.PromoteAsync(
            new ProgressionEvaluationRequest(student.Id, InstitutionType.University));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Evaluate_StudentNotFound_ThrowsKeyNotFoundException()
    {
        var notFoundRepo = new StubStudentProfileRepositoryNotFound();
        var svc = new ProgressionService(
            notFoundRepo,
            new StubGradingProfileRepository(null));

        var act = () => svc.EvaluateAsync(
            new ProgressionEvaluationRequest(Guid.NewGuid(), InstitutionType.University));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

file sealed class StubStudentProfileRepositoryNotFound : Tabsan.EduSphere.Domain.Interfaces.IStudentProfileRepository
{
    public Task<StudentProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult<StudentProfile?>(null);
    public Task<StudentProfile?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<StudentProfile?>(null);
    public Task<StudentProfile?> GetByRegistrationNumberAsync(string reg, CancellationToken ct) => Task.FromResult<StudentProfile?>(null);
    public Task<IReadOnlyList<StudentProfile>> GetAllAsync(Guid? deptId, CancellationToken ct) => Task.FromResult<IReadOnlyList<StudentProfile>>([]);
    public Task<bool> RegistrationNumberExistsAsync(string reg, CancellationToken ct) => Task.FromResult(false);
    public Task AddAsync(StudentProfile p, CancellationToken ct) => Task.CompletedTask;
    public void Update(StudentProfile p) { }
    public Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(0);
}
