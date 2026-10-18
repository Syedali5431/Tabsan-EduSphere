using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tabsan.EduSphere.Domain.Quizzes;

namespace Tabsan.EduSphere.Application.Quizzes.Grading;

/// <summary>A question as the graders see it (quiz or bank question, with attempt-specific marks/values).</summary>
public sealed record GradableQuestion(
    Guid QuestionId,
    QuestionType Type,
    decimal Marks,
    string? ConfigJson,
    IReadOnlyList<GradableOption> Options,
    IReadOnlyDictionary<string, double>? GeneratedValues = null);

public sealed record GradableOption(Guid Id, string Text, bool IsCorrect);

/// <summary>What the student submitted for one question.</summary>
public sealed record AnswerSubmission(IReadOnlyList<Guid> SelectedOptionIds, string? TextResponse, string? ResponseJson)
{
    public static readonly AnswerSubmission Empty = new([], null, null);
}

/// <summary>Result of grading one answer.</summary>
public sealed record GradeOutcome(decimal? Marks, bool RequiresManualGrading)
{
    public static GradeOutcome Manual => new(null, true);
    public static GradeOutcome Auto(decimal marks) => new(marks, false);
}

/// <summary>Plan M2.3: strategy interface — one grader per question type.</summary>
public interface IQuestionGrader
{
    IReadOnlyCollection<QuestionType> Types { get; }
    GradeOutcome Grade(GradableQuestion question, AnswerSubmission answer);
}

/// <summary>Resolves the grader for a question type and grades an answer.</summary>
public static class QuestionGrading
{
    private static readonly IReadOnlyDictionary<QuestionType, IQuestionGrader> Graders = BuildRegistry(
        new SingleChoiceGrader(), new MultipleAnswersGrader(), new MatchingGrader(), new OrderingGrader(),
        new FillInBlankGrader(), new NumericGrader(), new FormulaGrader(), new ManualGrader());

    public static GradeOutcome Grade(GradableQuestion question, AnswerSubmission answer)
        => Graders.TryGetValue(question.Type, out var grader) ? grader.Grade(question, answer) : GradeOutcome.Manual;

    public static bool IsManual(QuestionType type) => type is QuestionType.ShortAnswer or QuestionType.Essay or QuestionType.FileUpload;

    public static bool UsesOptions(QuestionType type) => type is QuestionType.MultipleChoice or QuestionType.TrueFalse or QuestionType.MultipleAnswers;

    /// <summary>Rounds a fraction of the marks to 2 decimals, clamped to [0, marks].</summary>
    internal static decimal Fraction(decimal marks, double fraction)
        => Math.Round(marks * (decimal)Math.Clamp(fraction, 0, 1), 2, MidpointRounding.AwayFromZero);

    private static IReadOnlyDictionary<QuestionType, IQuestionGrader> BuildRegistry(params IQuestionGrader[] graders)
    {
        var map = new Dictionary<QuestionType, IQuestionGrader>();
        foreach (var g in graders)
            foreach (var t in g.Types)
                map[t] = g;
        return map;
    }
}

/// <summary>MultipleChoice / TrueFalse: full marks when the single selected option is correct.</summary>
internal sealed class SingleChoiceGrader : IQuestionGrader
{
    public IReadOnlyCollection<QuestionType> Types => [QuestionType.MultipleChoice, QuestionType.TrueFalse];

    public GradeOutcome Grade(GradableQuestion q, AnswerSubmission a)
    {
        if (a.SelectedOptionIds.Count != 1) return GradeOutcome.Auto(0);
        var chosen = q.Options.FirstOrDefault(o => o.Id == a.SelectedOptionIds[0]);
        return GradeOutcome.Auto(chosen?.IsCorrect == true ? q.Marks : 0);
    }
}

/// <summary>
/// MultipleAnswers: with partial credit, (correct picks − wrong picks) / number of correct options
/// (never below zero); without it, all-or-nothing.
/// </summary>
internal sealed class MultipleAnswersGrader : IQuestionGrader
{
    public IReadOnlyCollection<QuestionType> Types => [QuestionType.MultipleAnswers];

    public GradeOutcome Grade(GradableQuestion q, AnswerSubmission a)
    {
        var config = QuizJson.Read<MultipleAnswersConfig>(q.ConfigJson) ?? new MultipleAnswersConfig();
        var correct = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
        if (correct.Count == 0) return GradeOutcome.Auto(0);

        var picked = a.SelectedOptionIds.Where(id => q.Options.Any(o => o.Id == id)).ToHashSet();
        var right = picked.Count(correct.Contains);
        var wrong = picked.Count - right;

        if (!config.PartialCredit)
            return GradeOutcome.Auto(right == correct.Count && wrong == 0 ? q.Marks : 0);

        return GradeOutcome.Auto(QuestionGrading.Fraction(q.Marks, (right - wrong) / (double)correct.Count));
    }
}

/// <summary>Matching: one share of the marks per correctly matched prompt (or all-or-nothing).</summary>
internal sealed class MatchingGrader : IQuestionGrader
{
    public IReadOnlyCollection<QuestionType> Types => [QuestionType.Matching];

    public GradeOutcome Grade(GradableQuestion q, AnswerSubmission a)
    {
        var config = QuizJson.Read<MatchingConfig>(q.ConfigJson);
        if (config is null || config.Pairs.Count == 0) return GradeOutcome.Auto(0);
        var matches = QuizJson.Read<MatchingResponse>(a.ResponseJson)?.Matches ?? new Dictionary<string, string>();

        var right = config.Pairs.Count(p =>
            matches.TryGetValue(p.Id, out var given) && string.Equals(given?.Trim(), p.Answer.Trim(), StringComparison.Ordinal));
        return GradeOutcome.Auto(config.PartialCredit
            ? QuestionGrading.Fraction(q.Marks, right / (double)config.Pairs.Count)
            : right == config.Pairs.Count ? q.Marks : 0);
    }
}

/// <summary>Ordering: share of items placed in their correct position (or all-or-nothing).</summary>
internal sealed class OrderingGrader : IQuestionGrader
{
    public IReadOnlyCollection<QuestionType> Types => [QuestionType.Ordering];

    public GradeOutcome Grade(GradableQuestion q, AnswerSubmission a)
    {
        var config = QuizJson.Read<OrderingConfig>(q.ConfigJson);
        if (config is null || config.Items.Count == 0) return GradeOutcome.Auto(0);
        var order = QuizJson.Read<OrderingResponse>(a.ResponseJson)?.Order ?? [];

        var right = config.Items.Select((item, i) => i < order.Count && order[i] == item.Id).Count(ok => ok);
        return GradeOutcome.Auto(config.PartialCredit
            ? QuestionGrading.Fraction(q.Marks, right / (double)config.Items.Count)
            : right == config.Items.Count ? q.Marks : 0);
    }
}

/// <summary>FillInBlank: share per blank whose answer matches an accepted answer (whitespace-normalised).</summary>
internal sealed class FillInBlankGrader : IQuestionGrader
{
    public IReadOnlyCollection<QuestionType> Types => [QuestionType.FillInBlank];

    public GradeOutcome Grade(GradableQuestion q, AnswerSubmission a)
    {
        var config = QuizJson.Read<FillInBlankConfig>(q.ConfigJson);
        if (config is null || config.Blanks.Count == 0) return GradeOutcome.Auto(0);
        var given = QuizJson.Read<FillInBlankResponse>(a.ResponseJson)?.Blanks ?? new Dictionary<string, string>();

        var right = config.Blanks.Count(b =>
            given.TryGetValue(b.Id, out var value)
            && b.Answers.Any(accepted => string.Equals(Normalize(value), Normalize(accepted),
                b.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)));
        return GradeOutcome.Auto(config.PartialCredit
            ? QuestionGrading.Fraction(q.Marks, right / (double)config.Blanks.Count)
            : right == config.Blanks.Count ? q.Marks : 0);
    }

    internal static string Normalize(string? s) => Regex.Replace(s ?? string.Empty, @"\s+", " ").Trim();
}

/// <summary>Numeric: exact answer ± tolerance, or inside [min, max].</summary>
internal sealed class NumericGrader : IQuestionGrader
{
    public IReadOnlyCollection<QuestionType> Types => [QuestionType.Numeric];

    public GradeOutcome Grade(GradableQuestion q, AnswerSubmission a)
    {
        var config = QuizJson.Read<NumericConfig>(q.ConfigJson);
        if (config is null || !NumberParsing.TryParse(QuizJson.Read<NumericResponse>(a.ResponseJson)?.Value, out var value))
            return GradeOutcome.Auto(0);

        var ok = config.Answer.HasValue
            ? Math.Abs(value - config.Answer.Value) <= Math.Abs(config.Tolerance) + 1e-9
            : (!config.Min.HasValue || value >= config.Min.Value - 1e-9) && (!config.Max.HasValue || value <= config.Max.Value + 1e-9);
        return GradeOutcome.Auto(ok ? q.Marks : 0);
    }
}

/// <summary>Formula: evaluates the expression with this attempt's generated values; correct within tolerance.</summary>
internal sealed class FormulaGrader : IQuestionGrader
{
    public IReadOnlyCollection<QuestionType> Types => [QuestionType.Formula];

    public GradeOutcome Grade(GradableQuestion q, AnswerSubmission a)
    {
        var config = QuizJson.Read<FormulaConfig>(q.ConfigJson);
        if (config is null || q.GeneratedValues is null
            || !NumberParsing.TryParse(QuizJson.Read<NumericResponse>(a.ResponseJson)?.Value, out var value))
            return GradeOutcome.Auto(0);

        double expected;
        try { expected = FormulaEvaluator.Evaluate(config.Expression, q.GeneratedValues); }
        catch (FormatException) { return GradeOutcome.Manual; } // broken formula — let a teacher decide

        expected = Math.Round(expected, Math.Clamp(config.Decimals, 0, 10), MidpointRounding.AwayFromZero);
        var tolerance = Math.Max(Math.Abs(config.Tolerance), Math.Pow(10, -Math.Clamp(config.Decimals, 0, 10)) / 2);
        return GradeOutcome.Auto(Math.Abs(value - expected) <= tolerance + 1e-9 ? q.Marks : 0);
    }
}

/// <summary>ShortAnswer, Essay and FileUpload are graded by faculty.</summary>
internal sealed class ManualGrader : IQuestionGrader
{
    public IReadOnlyCollection<QuestionType> Types => [QuestionType.ShortAnswer, QuestionType.Essay, QuestionType.FileUpload];

    public GradeOutcome Grade(GradableQuestion q, AnswerSubmission a) => GradeOutcome.Manual;
}

internal static class NumberParsing
{
    /// <summary>Parses a student's number, accepting "." or "," as the decimal separator.</summary>
    public static bool TryParse(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var normalized = text.Trim().Replace(" ", string.Empty);
        if (normalized.Count(c => c == ',') == 1 && !normalized.Contains('.'))
            normalized = normalized.Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
               && !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
