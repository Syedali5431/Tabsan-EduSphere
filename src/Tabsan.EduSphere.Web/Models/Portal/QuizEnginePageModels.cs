using Tabsan.EduSphere.Application.DTOs.Quizzes;
using Tabsan.EduSphere.Application.Quizzes;

namespace Tabsan.EduSphere.Web.Models.Portal;

// Plan M2 — quiz engine pages.

/// <summary>Question editor form (builder and banks). Options/config are JSON built by quiz-editor.js.</summary>
public sealed class QuizQuestionFormModel
{
    public string Type { get; set; } = "MultipleChoice";
    public string Text { get; set; } = string.Empty;
    public decimal Marks { get; set; } = 1;
    public int OrderIndex { get; set; } = 1;
    /// <summary>JSON array: [{"text":"…","isCorrect":true}]</summary>
    public string? OptionsJson { get; set; }
    public string? ConfigJson { get; set; }
    public string? Tags { get; set; }
    public int? Difficulty { get; set; }
}

/// <summary>Drives the shared question editor partial.</summary>
public sealed class QuestionEditorModel
{
    public required string FormAction { get; init; }
    public required string FormId { get; init; }
    public Dictionary<string, string> Hidden { get; init; } = new();
    public QuestionResponse? Existing { get; init; }
    public int NextOrderIndex { get; init; } = 1;
    public bool ShowBankMetadata { get; init; }
    public string SubmitLabel { get; init; } = "Add question";
}

public sealed class QuizBuilderPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public QuizDetailResponse? Quiz { get; set; }
    public List<BankSummaryResponse> Banks { get; set; } = new();
    public BankDetailResponse? CopyBank { get; set; }
    public List<EnrollmentRosterItem> Roster { get; set; } = new();
    public List<AccommodationResponse> Accommodations { get; set; } = new();
    public List<AvailabilityOverrideResponse> Overrides { get; set; } = new();

    public static readonly (string Value, string Label)[] QuestionTypes =
    [
        ("MultipleChoice", "Multiple choice (one answer)"),
        ("MultipleAnswers", "Multiple answers"),
        ("TrueFalse", "True / False"),
        ("Matching", "Matching"),
        ("Ordering", "Ordering"),
        ("FillInBlank", "Fill in the blanks"),
        ("Numeric", "Numeric answer"),
        ("Formula", "Formula (calculated)"),
        ("ShortAnswer", "Short answer (manual)"),
        ("Essay", "Essay (manual)"),
        ("FileUpload", "File upload (manual)")
    ];

    public static string TypeLabel(string type)
        => QuestionTypes.FirstOrDefault(t => t.Value == type).Label ?? type;
}

public sealed class QuestionBanksPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public Guid? OfferingId { get; set; }
    public List<LookupItem> Offerings { get; set; } = new();
    public List<BankSummaryResponse> Banks { get; set; } = new();
    public BankDetailResponse? SelectedBank { get; set; }
    public List<string>? ImportErrors { get; set; }
}

public sealed class TakeQuizPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public StudentQuizInfoResponse? Info { get; set; }
    public List<QuizAttemptItem> Attempts { get; set; } = new();
    public QuizAttemptItem? InProgress => Attempts.FirstOrDefault(a => string.Equals(a.Status, "InProgress", StringComparison.OrdinalIgnoreCase));
    public bool AttemptsLeft => Info is not null && (Info.MaxAttempts == 0 || Attempts.Count < Info.MaxAttempts);
}

public sealed class QuizPaperPageModel
{
    public StudentAttemptPaper Paper { get; set; } = default!;
}

public sealed class QuizReviewPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public string? QuizTitle { get; set; }
    public AttemptDetailResponse? Review { get; set; }
    /// <summary>Faculty viewing a student's attempt (grading screen link back).</summary>
    public bool IsFacultyView { get; set; }
    public Guid? QuizId { get; set; }
}

public sealed class QuizGradingPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public QuizDetailResponse? Quiz { get; set; }
    public List<GradingQueueItem> Queue { get; set; } = new();
    public List<Tabsan.EduSphere.Web.Services.QuizAttemptRow> Attempts { get; set; } = new();
}

public sealed class QuizStatisticsPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public QuizDetailResponse? Quiz { get; set; }
    public QuizStatisticsResponse? Stats { get; set; }
}
