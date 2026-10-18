using System.Globalization;
using System.Text.RegularExpressions;
using Tabsan.EduSphere.Application.DTOs.Quizzes;
using Tabsan.EduSphere.Application.Quizzes.Grading;
using Tabsan.EduSphere.Domain.Quizzes;

namespace Tabsan.EduSphere.Application.Quizzes;

/// <summary>
/// Plan M2.4: builds each attempt's question snapshot (order, shuffled options, generated formula
/// values) and turns questions into student views that never include correct answers.
/// </summary>
public static class QuizPresentation
{
    private static readonly Regex FormulaPlaceholder = new(@"\[([A-Za-z_][A-Za-z0-9_]{0,19})\]", RegexOptions.Compiled);
    private static readonly Regex BlankPlaceholder = new(@"\[\[([A-Za-z0-9_-]{1,20})\]\]", RegexOptions.Compiled);

    /// <summary>Presentation data stored on <see cref="QuizAttemptItem.PresentationJson"/>.</summary>
    public sealed record ItemPresentation(IReadOnlyList<string>? Order = null, IReadOnlyList<string>? Choices = null);

    /// <summary>Creates the snapshot for one question in a new attempt.</summary>
    public static (string? PresentationJson, string? GeneratedValuesJson) PlanItem(QuizQuestion q, bool shuffleOptions, Random rng)
    {
        switch (q.Type)
        {
            case QuestionType.MultipleChoice:
            case QuestionType.MultipleAnswers:
            {
                var ids = q.Options.OrderBy(o => o.OrderIndex).Select(o => o.Id.ToString()).ToList();
                return (QuizJson.Write(new ItemPresentation(shuffleOptions ? Shuffle(ids, rng) : ids)), null);
            }
            case QuestionType.TrueFalse:
                return (QuizJson.Write(new ItemPresentation(q.Options.OrderBy(o => o.OrderIndex).Select(o => o.Id.ToString()).ToList())), null);
            case QuestionType.Matching:
            {
                var c = QuizJson.Read<MatchingConfig>(q.ConfigJson);
                if (c is null) return (null, null);
                var prompts = c.Pairs.Select(p => p.Id).ToList();
                // Answer choices are always shuffled — listing them in prompt order would give the answers away.
                var choices = c.Pairs.Select(p => p.Answer).Concat(c.Distractors ?? []).Distinct().ToList();
                return (QuizJson.Write(new ItemPresentation(shuffleOptions ? Shuffle(prompts, rng) : prompts, Shuffle(choices, rng))), null);
            }
            case QuestionType.Ordering:
            {
                var c = QuizJson.Read<OrderingConfig>(q.ConfigJson);
                if (c is null) return (null, null);
                var correct = c.Items.Select(i => i.Id).ToList();
                var shuffled = Shuffle(correct, rng);
                for (var tries = 0; tries < 5 && shuffled.SequenceEqual(correct) && correct.Count > 1; tries++)
                    shuffled = Shuffle(correct, rng);
                return (QuizJson.Write(new ItemPresentation(shuffled)), null);
            }
            case QuestionType.Formula:
            {
                var c = QuizJson.Read<FormulaConfig>(q.ConfigJson);
                if (c is null) return (null, null);
                var values = c.Variables.ToDictionary(
                    v => v.Name,
                    v => Math.Round(v.Min + rng.NextDouble() * (v.Max - v.Min), Math.Clamp(v.Decimals, 0, 6), MidpointRounding.AwayFromZero));
                return (null, QuizJson.Write(values));
            }
            default:
                return (null, null);
        }
    }

    public static IReadOnlyDictionary<string, double>? ReadValues(string? generatedValuesJson)
        => QuizJson.Read<Dictionary<string, double>>(generatedValuesJson);

    /// <summary>Builds what a student sees for one question (no correctness data).</summary>
    public static StudentQuestionView ToStudentView(
        QuizQuestion q, int order, decimal marks, string? presentationJson, string? generatedValuesJson, QuizAnswer? answer, bool locked)
    {
        var presentation = QuizJson.Read<ItemPresentation>(presentationJson);
        var text = q.Type == QuestionType.Formula ? SubstituteValues(q.Text, ReadValues(generatedValuesJson)) : q.Text;
        var saved = answer is null ? null : new SavedAnswerView(answer.SelectedOptionIds, answer.TextResponse, answer.ResponseJson,
            q.Type == QuestionType.FileUpload ? QuizJson.Read<FileUploadResponse>(answer.ResponseJson)?.FileName : null);

        var view = new StudentQuestionView(order, q.Id, q.Type.ToString(), text, marks, Saved: saved, Locked: locked);

        switch (q.Type)
        {
            case QuestionType.MultipleChoice:
            case QuestionType.MultipleAnswers:
            case QuestionType.TrueFalse:
            {
                var byId = q.Options.ToDictionary(o => o.Id.ToString());
                var ids = presentation?.Order?.Where(byId.ContainsKey).ToList()
                          ?? q.Options.OrderBy(o => o.OrderIndex).Select(o => o.Id.ToString()).ToList();
                // Options added after the attempt started are appended so nothing disappears.
                ids.AddRange(q.Options.OrderBy(o => o.OrderIndex).Select(o => o.Id.ToString()).Where(id => !ids.Contains(id)));
                return view with { Options = ids.Select(id => new StudentChoice(id, byId[id].Text)).ToList() };
            }
            case QuestionType.Matching:
            {
                var c = QuizJson.Read<MatchingConfig>(q.ConfigJson);
                if (c is null) return view;
                var byId = c.Pairs.ToDictionary(p => p.Id);
                var prompts = (presentation?.Order?.Where(byId.ContainsKey) ?? c.Pairs.Select(p => p.Id))
                    .Select(id => new StudentChoice(id, byId[id].Prompt)).ToList();
                var choices = presentation?.Choices?.ToList()
                              ?? c.Pairs.Select(p => p.Answer).Concat(c.Distractors ?? []).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
                return view with { MatchingPrompts = prompts, MatchingChoices = choices };
            }
            case QuestionType.Ordering:
            {
                var c = QuizJson.Read<OrderingConfig>(q.ConfigJson);
                if (c is null) return view;
                var byId = c.Items.ToDictionary(i => i.Id);
                var savedOrder = QuizJson.Read<OrderingResponse>(answer?.ResponseJson)?.Order?.Where(byId.ContainsKey).ToList();
                var ids = savedOrder is { Count: > 0 } && savedOrder.Count == byId.Count
                    ? savedOrder
                    : presentation?.Order?.Where(byId.ContainsKey).ToList() ?? c.Items.Select(i => i.Id).ToList();
                return view with { OrderingItems = ids.Select(id => new StudentChoice(id, byId[id].Text)).ToList() };
            }
            case QuestionType.FillInBlank:
                return view with { BlankIds = BlankPlaceholder.Matches(q.Text).Select(m => m.Groups[1].Value).ToList() };
            case QuestionType.FileUpload:
            {
                var c = QuizJson.Read<FileUploadConfig>(q.ConfigJson) ?? new FileUploadConfig();
                return view with { AllowedExtensions = c.AllowedExtensions, MaxFileSizeMb = c.MaxSizeMb };
            }
            case QuestionType.Essay:
                return view with { WordLimit = QuizJson.Read<EssayConfig>(q.ConfigJson)?.WordLimit };
            default:
                return view;
        }
    }

    /// <summary>A human-readable statement of the correct answer (review with "show correct answers").</summary>
    public static string? DescribeCorrectAnswer(QuizQuestion q, string? generatedValuesJson)
    {
        switch (q.Type)
        {
            case QuestionType.MultipleChoice:
            case QuestionType.TrueFalse:
            case QuestionType.MultipleAnswers:
                return string.Join("; ", q.Options.Where(o => o.IsCorrect).OrderBy(o => o.OrderIndex).Select(o => o.Text));
            case QuestionType.Matching:
                return QuizJson.Read<MatchingConfig>(q.ConfigJson) is { } m ? string.Join("; ", m.Pairs.Select(p => $"{p.Prompt} → {p.Answer}")) : null;
            case QuestionType.Ordering:
                return QuizJson.Read<OrderingConfig>(q.ConfigJson) is { } o ? string.Join(" → ", o.Items.Select(i => i.Text)) : null;
            case QuestionType.FillInBlank:
                return QuizJson.Read<FillInBlankConfig>(q.ConfigJson) is { } f ? string.Join("; ", f.Blanks.Select(b => $"[{b.Id}] {string.Join(" / ", b.Answers)}")) : null;
            case QuestionType.Numeric:
                return QuizJson.Read<NumericConfig>(q.ConfigJson) is { } n
                    ? n.Answer.HasValue
                        ? n.Tolerance > 0 ? $"{Fmt(n.Answer.Value)} ± {Fmt(n.Tolerance)}" : Fmt(n.Answer.Value)
                        : $"between {(n.Min.HasValue ? Fmt(n.Min.Value) : "-∞")} and {(n.Max.HasValue ? Fmt(n.Max.Value) : "∞")}"
                    : null;
            case QuestionType.Formula:
            {
                var c = QuizJson.Read<FormulaConfig>(q.ConfigJson);
                var values = ReadValues(generatedValuesJson);
                if (c is null || values is null) return null;
                try
                {
                    var expected = Math.Round(FormulaEvaluator.Evaluate(c.Expression, values), Math.Clamp(c.Decimals, 0, 10), MidpointRounding.AwayFromZero);
                    return c.Tolerance > 0 ? $"{Fmt(expected)} ± {Fmt(c.Tolerance)}" : Fmt(expected);
                }
                catch (FormatException) { return null; }
            }
            default:
                return null; // manually graded
        }
    }

    public static string SubstituteValues(string text, IReadOnlyDictionary<string, double>? values)
        => values is null
            ? text
            : FormulaPlaceholder.Replace(text, m =>
                values.FirstOrDefault(kv => kv.Key.Equals(m.Groups[1].Value, StringComparison.OrdinalIgnoreCase)) is { Key: not null } hit
                    ? Fmt(hit.Value)
                    : m.Value);

    private static string Fmt(double v) => v.ToString("0.##########", CultureInfo.InvariantCulture);

    private static List<T> Shuffle<T>(IReadOnlyList<T> source, Random rng)
    {
        var list = source.ToList();
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }
}
