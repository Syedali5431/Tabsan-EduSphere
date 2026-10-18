using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tabsan.EduSphere.Application.Quizzes.Grading;

// Plan M2.3 — JSON schemas stored in QuizQuestion.ConfigJson (configs) and QuizAnswer.ResponseJson (responses).
// All JSON is camelCase. Unknown properties are ignored so the schemas can grow.

public static class QuizJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static T? Read<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<T>(json, Options); }
        catch (JsonException) { return null; }
    }

    public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options);
}

// ── Configs ────────────────────────────────────────────────────────────────────

/// <summary>MultipleAnswers: <c>{"partialCredit":true}</c> (default true).</summary>
public sealed record MultipleAnswersConfig(bool PartialCredit = true);

/// <summary>Matching: prompts each with one correct answer, plus optional wrong answers.</summary>
public sealed record MatchingConfig(IReadOnlyList<MatchingPair> Pairs, IReadOnlyList<string>? Distractors = null, bool PartialCredit = true);
public sealed record MatchingPair(string Id, string Prompt, string Answer);

/// <summary>Ordering: items listed in the correct order.</summary>
public sealed record OrderingConfig(IReadOnlyList<OrderingItem> Items, bool PartialCredit = true);
public sealed record OrderingItem(string Id, string Text);

/// <summary>FillInBlank: the question text contains <c>[[id]]</c> for each blank.</summary>
public sealed record FillInBlankConfig(IReadOnlyList<BlankDefinition> Blanks, bool PartialCredit = true);
public sealed record BlankDefinition(string Id, IReadOnlyList<string> Answers, bool CaseSensitive = false);

/// <summary>Numeric: either <c>answer</c> (± <c>tolerance</c>) or a <c>min</c>–<c>max</c> range.</summary>
public sealed record NumericConfig(double? Answer = null, double Tolerance = 0, double? Min = null, double? Max = null);

/// <summary>Formula: an expression over variables generated per attempt; the text shows <c>[name]</c>.</summary>
public sealed record FormulaConfig(string Expression, IReadOnlyList<FormulaVariable> Variables, double Tolerance = 0, int Decimals = 2);
public sealed record FormulaVariable(string Name, double Min, double Max, int Decimals = 0);

/// <summary>FileUpload: allowed extensions (e.g. ".pdf") and a size cap.</summary>
public sealed record FileUploadConfig(IReadOnlyList<string>? AllowedExtensions = null, int MaxSizeMb = 10);

/// <summary>Essay: optional word limit (informational).</summary>
public sealed record EssayConfig(int? WordLimit = null);

// ── Responses ──────────────────────────────────────────────────────────────────

public sealed record MatchingResponse(IReadOnlyDictionary<string, string>? Matches);
public sealed record OrderingResponse(IReadOnlyList<string>? Order);
public sealed record FillInBlankResponse(IReadOnlyDictionary<string, string>? Blanks);
public sealed record NumericResponse(string? Value);
public sealed record FileUploadResponse(string StorageKey, string FileName, string? ContentType, long Size);
