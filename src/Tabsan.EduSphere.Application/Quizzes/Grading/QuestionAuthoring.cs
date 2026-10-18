using System.Text.RegularExpressions;
using Tabsan.EduSphere.Domain.Quizzes;

namespace Tabsan.EduSphere.Application.Quizzes.Grading;

/// <summary>An option as entered by the author.</summary>
public sealed record AuthoredOption(string Text, bool IsCorrect);

/// <summary>
/// Plan M2.3: validates and normalises a question before it is saved (quiz or bank).
/// Returns the canonical ConfigJson (ids filled in, unknown fields dropped) or an error for the author.
/// </summary>
public static class QuestionAuthoring
{
    private static readonly Regex BlankPlaceholder = new(@"\[\[([A-Za-z0-9_-]{1,20})\]\]", RegexOptions.Compiled);
    private static readonly Regex VariableName = new("^[A-Za-z_][A-Za-z0-9_]{0,19}$", RegexOptions.Compiled);

    public static (string? ConfigJson, string? Error) Validate(
        QuestionType type, string text, decimal marks, string? configJson, IReadOnlyList<AuthoredOption> options)
    {
        if (string.IsNullOrWhiteSpace(text)) return (null, "Question text is required.");
        if (text.Length > 2000) return (null, "Question text must be 2000 characters or fewer.");
        if (marks < 0 || marks > 1000) return (null, "Marks must be between 0 and 1000.");
        if (!Enum.IsDefined(type)) return (null, "Unknown question type.");

        if (!QuestionGrading.UsesOptions(type) && options.Count > 0)
            return (null, $"{type} questions do not use answer options.");

        return type switch
        {
            QuestionType.MultipleChoice => Options(options, minOptions: 2, exactlyOneCorrect: true, config: null),
            QuestionType.TrueFalse => options.Count == 2 ? Options(options, 2, true, null) : (null, "True/False needs exactly two options."),
            QuestionType.MultipleAnswers => Options(options, 2, false,
                QuizJson.Write(QuizJson.Read<MultipleAnswersConfig>(configJson) ?? new MultipleAnswersConfig())),
            QuestionType.Matching => Matching(configJson),
            QuestionType.Ordering => Ordering(configJson),
            QuestionType.FillInBlank => FillInBlank(text, configJson),
            QuestionType.Numeric => Numeric(configJson),
            QuestionType.Formula => Formula(text, configJson),
            QuestionType.FileUpload => FileUpload(configJson),
            QuestionType.Essay => (QuizJson.Write(QuizJson.Read<EssayConfig>(configJson) ?? new EssayConfig()), null),
            QuestionType.ShortAnswer => (null, null),
            _ => (null, "Unknown question type.")
        };
    }

    private static (string?, string?) Options(IReadOnlyList<AuthoredOption> options, int minOptions, bool exactlyOneCorrect, string? config)
    {
        if (options.Count < minOptions) return (null, $"Add at least {minOptions} answer options.");
        if (options.Count > 20) return (null, "A question can have at most 20 options.");
        if (options.Any(o => string.IsNullOrWhiteSpace(o.Text))) return (null, "Answer options cannot be empty.");
        var correct = options.Count(o => o.IsCorrect);
        if (exactlyOneCorrect && correct != 1) return (null, "Mark exactly one option as correct.");
        if (!exactlyOneCorrect && correct < 1) return (null, "Mark at least one option as correct.");
        return (config, null);
    }

    private static (string?, string?) Matching(string? json)
    {
        var c = QuizJson.Read<MatchingConfig>(json);
        if (c?.Pairs is null || c.Pairs.Count < 2) return (null, "Matching needs at least two prompt/answer pairs.");
        if (c.Pairs.Count > 20) return (null, "Matching can have at most 20 pairs.");
        if (c.Pairs.Any(p => string.IsNullOrWhiteSpace(p.Prompt) || string.IsNullOrWhiteSpace(p.Answer)))
            return (null, "Every pair needs a prompt and an answer.");

        var pairs = c.Pairs.Select((p, i) => new MatchingPair($"p{i + 1}", p.Prompt.Trim(), p.Answer.Trim())).ToList();
        var distractors = (c.Distractors ?? []).Select(d => d.Trim()).Where(d => d.Length > 0)
            .Where(d => pairs.All(p => p.Answer != d)).Distinct().ToList();
        return (QuizJson.Write(new MatchingConfig(pairs, distractors, c.PartialCredit)), null);
    }

    private static (string?, string?) Ordering(string? json)
    {
        var c = QuizJson.Read<OrderingConfig>(json);
        if (c?.Items is null || c.Items.Count < 2) return (null, "Ordering needs at least two items.");
        if (c.Items.Count > 20) return (null, "Ordering can have at most 20 items.");
        if (c.Items.Any(i => string.IsNullOrWhiteSpace(i.Text))) return (null, "Ordering items cannot be empty.");
        var items = c.Items.Select((it, i) => new OrderingItem($"i{i + 1}", it.Text.Trim())).ToList();
        return (QuizJson.Write(new OrderingConfig(items, c.PartialCredit)), null);
    }

    private static (string?, string?) FillInBlank(string text, string? json)
    {
        var c = QuizJson.Read<FillInBlankConfig>(json);
        var placeholders = BlankPlaceholder.Matches(text).Select(m => m.Groups[1].Value).ToList();
        if (placeholders.Count == 0) return (null, "Put a placeholder such as [[1]] in the question text for each blank.");
        if (placeholders.Count != placeholders.Distinct().Count()) return (null, "Each blank placeholder must be used once.");
        if (c?.Blanks is null) return (null, "Add the accepted answers for each blank.");

        var blanks = new List<BlankDefinition>();
        foreach (var id in placeholders)
        {
            var def = c.Blanks.FirstOrDefault(b => b.Id == id);
            var answers = def?.Answers?.Select(a => a.Trim()).Where(a => a.Length > 0).Distinct().ToList() ?? [];
            if (answers.Count == 0) return (null, $"Blank [[{id}]] needs at least one accepted answer.");
            blanks.Add(new BlankDefinition(id, answers, def!.CaseSensitive));
        }
        return (QuizJson.Write(new FillInBlankConfig(blanks, c.PartialCredit)), null);
    }

    private static (string?, string?) Numeric(string? json)
    {
        var c = QuizJson.Read<NumericConfig>(json);
        if (c is null || (c.Answer is null && c.Min is null && c.Max is null))
            return (null, "Enter the correct answer (with an optional tolerance) or a range.");
        if (c.Tolerance < 0) return (null, "Tolerance cannot be negative.");
        if (c.Min.HasValue && c.Max.HasValue && c.Min > c.Max) return (null, "The range minimum must not exceed the maximum.");
        return (QuizJson.Write(c.Answer.HasValue ? c with { Min = null, Max = null } : c with { Tolerance = 0 }), null);
    }

    private static (string?, string?) Formula(string text, string? json)
    {
        var c = QuizJson.Read<FormulaConfig>(json);
        if (c is null || string.IsNullOrWhiteSpace(c.Expression)) return (null, "Enter the formula.");
        if (c.Variables is null || c.Variables.Count == 0) return (null, "Define at least one variable.");
        if (c.Variables.Count > 10) return (null, "A formula can use at most 10 variables.");
        if (c.Tolerance < 0) return (null, "Tolerance cannot be negative.");
        if (c.Decimals is < 0 or > 10) return (null, "Decimals must be between 0 and 10.");

        foreach (var v in c.Variables)
        {
            if (!VariableName.IsMatch(v.Name ?? string.Empty)) return (null, $"'{v.Name}' is not a valid variable name.");
            if (v.Min > v.Max) return (null, $"Variable {v.Name}: minimum exceeds maximum.");
            if (v.Decimals is < 0 or > 6) return (null, $"Variable {v.Name}: decimals must be between 0 and 6.");
        }
        if (c.Variables.Select(v => v.Name.ToLowerInvariant()).Distinct().Count() != c.Variables.Count)
            return (null, "Variable names must be unique.");

        var used = FormulaEvaluator.GetVariableNames(c.Expression);
        var missing = used.Where(u => c.Variables.All(v => !v.Name.Equals(u, StringComparison.OrdinalIgnoreCase))).ToList();
        if (missing.Count > 0) return (null, $"The formula uses undefined variable(s): {string.Join(", ", missing)}.");

        try
        {
            // Check the formula evaluates at both ends of every range.
            FormulaEvaluator.Evaluate(c.Expression, c.Variables.ToDictionary(v => v.Name, v => v.Min));
            FormulaEvaluator.Evaluate(c.Expression, c.Variables.ToDictionary(v => v.Name, v => v.Max));
        }
        catch (FormatException ex)
        {
            return (null, "Formula error: " + ex.Message);
        }

        var unshown = c.Variables.Where(v => !text.Contains($"[{v.Name}]", StringComparison.OrdinalIgnoreCase)).Select(v => v.Name).ToList();
        if (unshown.Count > 0) return (null, $"Show each variable in the question text as [name]; missing: {string.Join(", ", unshown)}.");

        var normalized = c with { Variables = c.Variables.Select(v => v with { Name = v.Name.Trim() }).ToList(), Expression = c.Expression.Trim() };
        return (QuizJson.Write(normalized), null);
    }

    private static (string?, string?) FileUpload(string? json)
    {
        var c = QuizJson.Read<FileUploadConfig>(json) ?? new FileUploadConfig();
        if (c.MaxSizeMb is < 1 or > 50) return (null, "Maximum file size must be between 1 and 50 MB.");
        var exts = (c.AllowedExtensions ?? [])
            .Select(e => e.Trim().ToLowerInvariant())
            .Where(e => e.Length > 0)
            .Select(e => e.StartsWith('.') ? e : "." + e)
            .Distinct().ToList();
        if (exts.Any(e => !Regex.IsMatch(e, @"^\.[a-z0-9]{1,8}$"))) return (null, "File extensions must look like .pdf or .docx.");
        return (QuizJson.Write(new FileUploadConfig(exts.Count == 0 ? null : exts, c.MaxSizeMb)), null);
    }
}
