using FluentAssertions;
using Tabsan.EduSphere.Application.Quizzes;
using Tabsan.EduSphere.Application.Quizzes.Grading;
using Tabsan.EduSphere.Domain.Quizzes;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Plan M2 (quiz engine): formula evaluator, graders, authoring rules, presentation, IP rules.</summary>
public class QuizEngineTests
{
    // ═════════════════════════════════════════════════════════════════════════
    // FormulaEvaluator
    // ═════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("1 + 2 * 3", 7)]
    [InlineData("(1 + 2) * 3", 9)]
    [InlineData("2 ^ 3 ^ 2", 512)]          // right-associative
    [InlineData("-2 ^ 2", -4)]              // unary minus binds looser than ^
    [InlineData("10 % 4", 2)]
    [InlineData("sqrt(16) + abs(-3)", 7)]
    [InlineData("round(2.345, 2)", 2.35)]
    [InlineData("min(3, 1, 2) + max(4, 9)", 10)]
    [InlineData("pow(2, 10)", 1024)]
    [InlineData("1.5e2", 150)]
    public void Formula_EvaluatesArithmetic(string expression, double expected)
        => FormulaEvaluator.Evaluate(expression, new Dictionary<string, double>()).Should().BeApproximately(expected, 1e-9);

    [Fact]
    public void Formula_UsesVariablesCaseInsensitively_AndConstants()
    {
        var value = FormulaEvaluator.Evaluate("A * b + pi * 0", new Dictionary<string, double> { ["a"] = 3, ["B"] = 4 });
        value.Should().Be(12);
    }

    [Theory]
    [InlineData("1 / 0")]
    [InlineData("x + 1")]
    [InlineData("sqrt(-1)")]
    [InlineData("1 +")]
    [InlineData("foo(1)")]
    [InlineData("(1 + 2")]
    [InlineData("1 2")]
    public void Formula_RejectsInvalidInput(string expression)
        => FluentActions.Invoking(() => FormulaEvaluator.Evaluate(expression, new Dictionary<string, double>()))
            .Should().Throw<FormatException>();

    [Fact]
    public void Formula_ListsVariables_NotFunctionsOrConstants()
        => FormulaEvaluator.GetVariableNames("sqrt(a*a + b*b) + pi + e").Should().BeEquivalentTo(["a", "b"]);

    // ═════════════════════════════════════════════════════════════════════════
    // Graders
    // ═════════════════════════════════════════════════════════════════════════

    private static readonly Guid A = Guid.NewGuid(), B = Guid.NewGuid(), C = Guid.NewGuid(), D = Guid.NewGuid();

    private static GradableQuestion Q(QuestionType type, string? config = null, decimal marks = 10, IReadOnlyDictionary<string, double>? values = null, params (Guid Id, bool Correct)[] options)
        => new(Guid.NewGuid(), type, marks, config, options.Select(o => new GradableOption(o.Id, "opt", o.Correct)).ToList(), values);

    private static AnswerSubmission Picks(params Guid[] ids) => new(ids, null, null);
    private static AnswerSubmission Json(string json) => new([], null, json);

    [Fact]
    public void SingleChoice_FullOrZero()
    {
        var q = Q(QuestionType.MultipleChoice, options: [(A, true), (B, false)]);
        QuestionGrading.Grade(q, Picks(A)).Marks.Should().Be(10);
        QuestionGrading.Grade(q, Picks(B)).Marks.Should().Be(0);
        QuestionGrading.Grade(q, Picks(A, B)).Marks.Should().Be(0, "two picks on a single-answer question");
        QuestionGrading.Grade(q, AnswerSubmission.Empty).Marks.Should().Be(0);
    }

    [Fact]
    public void MultipleAnswers_PartialCredit_RightMinusWrong()
    {
        var q = Q(QuestionType.MultipleAnswers, options: [(A, true), (B, true), (C, false), (D, false)]);
        QuestionGrading.Grade(q, Picks(A, B)).Marks.Should().Be(10);
        QuestionGrading.Grade(q, Picks(A)).Marks.Should().Be(5);
        QuestionGrading.Grade(q, Picks(A, C)).Marks.Should().Be(0, "one right minus one wrong");
        QuestionGrading.Grade(q, Picks(C, D)).Marks.Should().Be(0, "never negative");
        QuestionGrading.Grade(q, Picks(A, B, C)).Marks.Should().Be(5);
    }

    [Fact]
    public void MultipleAnswers_AllOrNothing_WhenPartialCreditOff()
    {
        var q = Q(QuestionType.MultipleAnswers, """{"partialCredit":false}""", options: [(A, true), (B, true), (C, false)]);
        QuestionGrading.Grade(q, Picks(A)).Marks.Should().Be(0);
        QuestionGrading.Grade(q, Picks(A, B)).Marks.Should().Be(10);
    }

    [Fact]
    public void Matching_PartialPerPair()
    {
        var q = Q(QuestionType.Matching, """{"pairs":[{"id":"p1","prompt":"France","answer":"Paris"},{"id":"p2","prompt":"Italy","answer":"Rome"}],"distractors":["Berlin"]}""");
        QuestionGrading.Grade(q, Json("""{"matches":{"p1":"Paris","p2":"Rome"}}""")).Marks.Should().Be(10);
        QuestionGrading.Grade(q, Json("""{"matches":{"p1":"Paris","p2":"Berlin"}}""")).Marks.Should().Be(5);
        QuestionGrading.Grade(q, Json("not json")).Marks.Should().Be(0);
    }

    [Fact]
    public void Ordering_SharePerCorrectPosition()
    {
        var q = Q(QuestionType.Ordering, """{"items":[{"id":"i1","text":"a"},{"id":"i2","text":"b"},{"id":"i3","text":"c"},{"id":"i4","text":"d"}]}""");
        QuestionGrading.Grade(q, Json("""{"order":["i1","i2","i3","i4"]}""")).Marks.Should().Be(10);
        QuestionGrading.Grade(q, Json("""{"order":["i1","i2","i4","i3"]}""")).Marks.Should().Be(5);
        QuestionGrading.Grade(q, Json("""{"order":["i1"]}""")).Marks.Should().Be(2.5m);
    }

    [Fact]
    public void FillInBlank_NormalisesWhitespaceAndCase()
    {
        var q = Q(QuestionType.FillInBlank, """{"blanks":[{"id":"1","answers":["Paris"]},{"id":"2","answers":["H2O"],"caseSensitive":true}]}""");
        QuestionGrading.Grade(q, Json("""{"blanks":{"1":"  paris ","2":"H2O"}}""")).Marks.Should().Be(10);
        QuestionGrading.Grade(q, Json("""{"blanks":{"1":"Paris","2":"h2o"}}""")).Marks.Should().Be(5, "blank 2 is case-sensitive");
    }

    [Theory]
    [InlineData("3.14", 10)]
    [InlineData("3,15", 10)]   // comma decimal accepted
    [InlineData("3.2", 0)]
    [InlineData("abc", 0)]
    public void Numeric_ExactWithTolerance(string given, decimal expected)
    {
        var q = Q(QuestionType.Numeric, """{"answer":3.14,"tolerance":0.01}""");
        QuestionGrading.Grade(q, Json($$"""{"value":"{{given}}"}""")).Marks.Should().Be(expected);
    }

    [Fact]
    public void Numeric_Range()
    {
        var q = Q(QuestionType.Numeric, """{"min":1,"max":2}""");
        QuestionGrading.Grade(q, Json("""{"value":"1.5"}""")).Marks.Should().Be(10);
        QuestionGrading.Grade(q, Json("""{"value":"2.5"}""")).Marks.Should().Be(0);
    }

    [Fact]
    public void Formula_UsesThisAttemptsValues()
    {
        var config = """{"expression":"a*b","variables":[{"name":"a","min":1,"max":9},{"name":"b","min":1,"max":9}],"decimals":0}""";
        var q = Q(QuestionType.Formula, config, values: new Dictionary<string, double> { ["a"] = 6, ["b"] = 7 });
        QuestionGrading.Grade(q, Json("""{"value":"42"}""")).Marks.Should().Be(10);
        QuestionGrading.Grade(q, Json("""{"value":"41"}""")).Marks.Should().Be(0);
        QuestionGrading.Grade(Q(QuestionType.Formula, config), Json("""{"value":"42"}""")).Marks.Should().Be(0, "no generated values");
    }

    [Theory]
    [InlineData(QuestionType.ShortAnswer)]
    [InlineData(QuestionType.Essay)]
    [InlineData(QuestionType.FileUpload)]
    public void ManualTypes_NeedTeacher(QuestionType type)
        => QuestionGrading.Grade(Q(type), new AnswerSubmission([], "text", null)).Should().Be(GradeOutcome.Manual);

    // ═════════════════════════════════════════════════════════════════════════
    // Authoring rules
    // ═════════════════════════════════════════════════════════════════════════

    private static (string? Config, string? Error) Validate(QuestionType type, string text, string? config = null, params (string, bool)[] options)
        => QuestionAuthoring.Validate(type, text, 5, config, options.Select(o => new AuthoredOption(o.Item1, o.Item2)).ToList());

    [Fact]
    public void Authoring_ChoiceRules()
    {
        Validate(QuestionType.MultipleChoice, "Q", null, ("a", true), ("b", true)).Error.Should().Contain("exactly one");
        Validate(QuestionType.MultipleChoice, "Q", null, ("a", true)).Error.Should().Contain("at least 2");
        Validate(QuestionType.TrueFalse, "Q", null, ("T", true), ("F", false), ("X", false)).Error.Should().Contain("exactly two");
        Validate(QuestionType.MultipleAnswers, "Q", null, ("a", false), ("b", false)).Error.Should().Contain("at least one");
        Validate(QuestionType.MultipleAnswers, "Q", null, ("a", true), ("b", true)).Error.Should().BeNull();
        Validate(QuestionType.Numeric, "Q", """{"answer":1}""", ("a", true)).Error.Should().Contain("do not use answer options");
    }

    [Fact]
    public void Authoring_Matching_AssignsIds_AndDropsDuplicateDistractors()
    {
        var (config, error) = Validate(QuestionType.Matching, "Match",
            """{"pairs":[{"prompt":"France","answer":"Paris"},{"prompt":"Italy","answer":"Rome"}],"distractors":["Rome","Berlin",""]}""");
        error.Should().BeNull();
        var parsed = QuizJson.Read<MatchingConfig>(config)!;
        parsed.Pairs.Select(p => p.Id).Should().Equal("p1", "p2");
        parsed.Distractors.Should().Equal("Berlin");
    }

    [Fact]
    public void Authoring_FillInBlank_PlaceholdersMustMatchAnswers()
    {
        Validate(QuestionType.FillInBlank, "No blanks here", """{"blanks":[]}""").Error.Should().Contain("[[1]]");
        Validate(QuestionType.FillInBlank, "Capital: [[1]], river: [[2]]", """{"blanks":[{"id":"1","answers":["Paris"]}]}""").Error.Should().Contain("[[2]]");
        Validate(QuestionType.FillInBlank, "Capital: [[1]]", """{"blanks":[{"id":"1","answers":["Paris"]}]}""").Error.Should().BeNull();
    }

    [Fact]
    public void Authoring_Formula_ChecksVariablesAndText()
    {
        const string ok = """{"expression":"a*b","variables":[{"name":"a","min":1,"max":9},{"name":"b","min":1,"max":9}]}""";
        Validate(QuestionType.Formula, "What is [a] × [b]?", ok).Error.Should().BeNull();
        Validate(QuestionType.Formula, "What is [a] × b?", ok).Error.Should().Contain("missing: b");
        Validate(QuestionType.Formula, "[a]", """{"expression":"a*c","variables":[{"name":"a","min":1,"max":9}]}""").Error.Should().Contain("undefined variable");
        Validate(QuestionType.Formula, "[a]", """{"expression":"1/a","variables":[{"name":"a","min":0,"max":9}]}""").Error.Should().Contain("Division by zero");
    }

    [Fact]
    public void Authoring_FileUpload_NormalisesExtensions()
    {
        var (config, error) = Validate(QuestionType.FileUpload, "Upload", """{"allowedExtensions":["PDF",".docx"],"maxSizeMb":5}""");
        error.Should().BeNull();
        QuizJson.Read<FileUploadConfig>(config)!.AllowedExtensions.Should().Equal(".pdf", ".docx");
        Validate(QuestionType.FileUpload, "Upload", """{"maxSizeMb":500}""").Error.Should().Contain("50 MB");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Presentation (per-attempt snapshot and student view)
    // ═════════════════════════════════════════════════════════════════════════

    private static QuizQuestion QuizQ(QuestionType type, string text, string? config = null, params (string Text, bool Correct)[] options)
    {
        var q = new QuizQuestion(Guid.NewGuid(), text, type, 4, 1, config);
        var list = (List<QuizOption>)q.Options;
        for (var i = 0; i < options.Length; i++)
            list.Add(new QuizOption(q.Id, options[i].Text, options[i].Correct, i + 1));
        return q;
    }

    [Fact]
    public void Presentation_ShufflesOptions_ButKeepsTrueFalseOrder()
    {
        var mc = QuizQ(QuestionType.MultipleChoice, "Q", null, ("a", true), ("b", false), ("c", false), ("d", false), ("e", false));
        var orders = Enumerable.Range(0, 20).Select(seed => QuizPresentation.PlanItem(mc, true, new Random(seed)).PresentationJson).Distinct().Count();
        orders.Should().BeGreaterThan(1);

        var tf = QuizQ(QuestionType.TrueFalse, "Q", null, ("True", true), ("False", false));
        var view = QuizPresentation.ToStudentView(tf, 1, 4, QuizPresentation.PlanItem(tf, true, new Random(1)).PresentationJson, null, null, false);
        view.Options!.Select(o => o.Text).Should().Equal("True", "False");
    }

    [Fact]
    public void Presentation_StudentViewNeverCarriesCorrectness()
    {
        var mc = QuizQ(QuestionType.MultipleChoice, "Q", null, ("right", true), ("wrong", false));
        var view = QuizPresentation.ToStudentView(mc, 1, 4, null, null, null, false);
        System.Text.Json.JsonSerializer.Serialize(view).Should().NotContainEquivalentOf("isCorrect").And.NotContain("\"answer\"");
    }

    [Fact]
    public void Presentation_MatchingChoicesIncludeDistractors_AndOrderingIsScrambled()
    {
        var (config, _) = Validate(QuestionType.Matching, "M", """{"pairs":[{"prompt":"France","answer":"Paris"},{"prompt":"Italy","answer":"Rome"}],"distractors":["Berlin"]}""");
        var matching = QuizQ(QuestionType.Matching, "M", config);
        var view = QuizPresentation.ToStudentView(matching, 1, 4, QuizPresentation.PlanItem(matching, false, new Random(3)).PresentationJson, null, null, false);
        view.MatchingChoices.Should().BeEquivalentTo(["Paris", "Rome", "Berlin"]);
        System.Text.Json.JsonSerializer.Serialize(view.MatchingPrompts).Should().NotContain("Paris");

        var (ordering, _) = Validate(QuestionType.Ordering, "O", """{"items":[{"text":"1"},{"text":"2"},{"text":"3"}]}""");
        var oq = QuizQ(QuestionType.Ordering, "O", ordering);
        for (var seed = 0; seed < 10; seed++)
        {
            var presented = QuizPresentation.ToStudentView(oq, 1, 4, QuizPresentation.PlanItem(oq, false, new Random(seed)).PresentationJson, null, null, false);
            presented.OrderingItems!.Select(i => i.Id).Should().NotEqual(["i1", "i2", "i3"]);
        }
    }

    [Fact]
    public void Presentation_FormulaValuesInRange_AndSubstituted()
    {
        var (config, _) = Validate(QuestionType.Formula, "Compute [a] + [b]", """{"expression":"a+b","variables":[{"name":"a","min":10,"max":20},{"name":"b","min":0.5,"max":1,"decimals":1}],"decimals":1}""");
        var q = QuizQ(QuestionType.Formula, "Compute [a] + [b]", config);
        var (_, values) = QuizPresentation.PlanItem(q, false, new Random(7));
        var parsed = QuizPresentation.ReadValues(values)!;
        parsed["a"].Should().BeInRange(10, 20).And.Be(Math.Round(parsed["a"]));
        parsed["b"].Should().BeInRange(0.5, 1);

        var view = QuizPresentation.ToStudentView(q, 1, 4, null, values, null, false);
        view.Text.Should().NotContain("[a]").And.NotContain("[b]");
        QuizPresentation.DescribeCorrectAnswer(q, values).Should().Be((parsed["a"] + parsed["b"]).ToString("0.##########", System.Globalization.CultureInfo.InvariantCulture));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // IP allow-list
    // ═════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(null, "8.8.8.8", true)]
    [InlineData("10.0.0.0/8", "10.20.30.40", true)]
    [InlineData("10.0.0.0/8", "11.0.0.1", false)]
    [InlineData("192.168.1.5, 10.0.0.0/8", "192.168.1.5", true)]
    [InlineData("192.168.1.5", "::ffff:192.168.1.5", true)]   // IPv4-mapped IPv6
    [InlineData("10.0.0.0/8", null, false)]
    [InlineData("2001:db8::/32", "2001:db8::1", true)]
    public void IpAllowList(string? ranges, string? ip, bool allowed)
        => QuizService.IsIpAllowed(ranges, ip).Should().Be(allowed);

    [Fact]
    public void IpAllowList_ValidatesEntries()
    {
        QuizService.ValidateIpRanges("10.0.0.0/8, 192.168.1.1").Should().BeNull();
        QuizService.ValidateIpRanges("10.0.0.0/99").Should().Contain("not a valid");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Domain
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Answer_MultiSelect_And_SingleSelectMirror()
    {
        var answer = new QuizAnswer(Guid.NewGuid(), Guid.NewGuid());
        answer.SetResponse([A, B, A], null, null, singleSelection: false);
        answer.SelectedOptionIds.Should().BeEquivalentTo([A, B]);
        answer.SelectedOptionId.Should().BeNull();

        answer.SetResponse([C, D], null, null, singleSelection: true);
        answer.SelectedOptionIds.Should().Equal(C);
        answer.SelectedOptionId.Should().Be(C);
        answer.SavedAt.Should().NotBeNull();
    }

    [Fact]
    public void Accommodation_AppliesMultiplierThenExtraMinutes()
    {
        var acc = new QuizAccommodation(Guid.NewGuid(), null, Guid.NewGuid(), 10, 1.5m, 1, null);
        acc.ApplyTo(30).Should().Be(55);
        acc.ApplyTo(null).Should().BeNull();
        FluentActions.Invoking(() => acc.Update(0, 0.5m, 0, null)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Attempt_IsOpenUntilDeadlinePlusGrace()
    {
        var attempt = new QuizAttempt(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-1));
        attempt.IsOpenAt(DateTime.UtcNow).Should().BeFalse();
        new QuizAttempt(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddSeconds(-10)).IsOpenAt(DateTime.UtcNow).Should().BeTrue("30s network grace");
        new QuizAttempt(Guid.NewGuid(), Guid.NewGuid()).IsOpenAt(DateTime.UtcNow.AddYears(1)).Should().BeTrue("untimed");
    }
}
