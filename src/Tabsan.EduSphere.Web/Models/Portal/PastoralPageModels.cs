using Tabsan.EduSphere.Application.Pastoral;

namespace Tabsan.EduSphere.Web.Models.Portal;

/// <summary>The pastoral overview for staff.</summary>
public sealed class PastoralPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public OverviewDto? Overview { get; set; }
    public Guid? CampusId { get; set; }
    public Guid? SemesterId { get; set; }
    public Guid? CourseOfferingId { get; set; }
    /// <summary>How many days back (7, 30, 90 or 365).</summary>
    public int Days { get; set; } = 30;
    public string? Search { get; set; }
    public List<StudentOptionDto> Found { get; set; } = [];

    public static readonly int[] DayChoices = [7, 30, 90, 365];
}

/// <summary>One student's pastoral page.</summary>
public sealed class PastoralStudentPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public StudentPastoralDto? Student { get; set; }
}

/// <summary>Writing a note (for one student, or for several from a class or a search) or changing one.</summary>
public sealed class PastoralNotePageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    /// <summary>The note being changed.</summary>
    public Guid? Id { get; set; }
    /// <summary>The one student the note is for (from their page, or when changing a note).</summary>
    public StudentPastoralDto? Student { get; set; }
    public FilterOptionsDto? Filters { get; set; }
    public Guid? ClassId { get; set; }
    public string? Search { get; set; }
    /// <summary>The class's students or the search results to choose from.</summary>
    public List<StudentOptionDto> Choices { get; set; } = [];
    public IReadOnlyList<CategoryDto> Categories { get; set; } = [];
    public PastoralNoteInput Input { get; set; } = new();
}

public sealed class PastoralNoteInput
{
    public List<Guid> StudentProfileIds { get; set; } = [];
    public Guid? CourseOfferingId { get; set; }
    public string Kind { get; set; } = "Behaviour";
    public string Tone { get; set; } = "Positive";
    public string Severity { get; set; } = "Low";
    public string? Category { get; set; }
    public string? Summary { get; set; }
    public string? Details { get; set; }
    public string? ActionTaken { get; set; }
    /// <summary>UTC, ISO 8601 (the page fills it from the local time picked).</summary>
    public string? OccurredAt { get; set; }
    public bool Confidential { get; set; }
    public bool ShareWithStudent { get; set; }
    public bool ShareWithParents { get; set; }
    public Guid? FollowUpUserId { get; set; }
    /// <summary>A date (yyyy-MM-dd).</summary>
    public string? FollowUpBy { get; set; }

    public SaveRecordRequest ToRequest()
        => new(StudentProfileIds, CourseOfferingId, Kind, Tone, Severity, Category, Summary, Details, ActionTaken, CommunityFormat.Parse(OccurredAt),
            Confidential, ShareWithStudent, ShareWithParents, FollowUpUserId, PastoralFormat.ParseDate(FollowUpBy));

    public static PastoralNoteInput From(PastoralRecordDto r) => new()
    {
        StudentProfileIds = [r.StudentProfileId], Kind = r.Kind, Tone = r.Tone, Severity = r.Severity, Category = r.Category, Summary = r.Summary,
        Details = r.Details, ActionTaken = r.ActionTaken, OccurredAt = CommunityFormat.Iso(r.OccurredAt), Confidential = r.Visibility == "Confidential",
        ShareWithStudent = r.SharedWithStudent, ShareWithParents = r.SharedWithParents, FollowUpUserId = r.FollowUp == "Open" ? r.FollowUpUserId : null,
        FollowUpBy = PastoralFormat.Date(r.FollowUp == "Open" ? r.FollowUpBy : null)
    };
}

/// <summary>Writing or changing a support plan.</summary>
public sealed class PastoralPlanPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public Guid? Id { get; set; }
    public StudentPastoralDto? Student { get; set; }
    public PastoralPlanInput Input { get; set; } = new();
}

public sealed class PastoralPlanInput
{
    public Guid StudentProfileId { get; set; }
    public string Kind { get; set; } = "LearningAdjustments";
    public string? Title { get; set; }
    public string? Needs { get; set; }
    public string? Strategies { get; set; }
    public string? StartsOn { get; set; }
    public string? ReviewOn { get; set; }
    public bool Confidential { get; set; }
    public bool ShareWithStudent { get; set; }
    public bool ShareWithParents { get; set; }

    public SavePlanRequest ToRequest()
        => new(StudentProfileId, Kind, Title, Needs, Strategies, PastoralFormat.ParseDate(StartsOn), PastoralFormat.ParseDate(ReviewOn), Confidential,
            ShareWithStudent, ShareWithParents);

    public static PastoralPlanInput From(SupportPlanDto p) => new()
    {
        StudentProfileId = p.StudentProfileId, Kind = p.Kind, Title = p.Title, Needs = p.Needs, Strategies = p.Strategies, StartsOn = PastoralFormat.Date(p.StartsOn),
        ReviewOn = PastoralFormat.Date(p.ReviewOn), Confidential = p.Visibility == "Confidential", ShareWithStudent = p.SharedWithStudent,
        ShareWithParents = p.SharedWithParents
    };
}

/// <summary>The wellbeing team, categories and settings of a school.</summary>
public sealed class PastoralSetupPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public PastoralSetupDto? Setup { get; set; }
}

/// <summary>A student's wellbeing page: their check-in and what the school shared with them.</summary>
public sealed class WellbeingPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public MyWellbeingDto? Mine { get; set; }
}

/// <summary>The words the pages show for the API's values (translated by the views).</summary>
public static class PastoralWords
{
    public static readonly string[] Kinds = ["Behaviour", "Wellbeing", "LearningSupport", "FamilyContact", "General"];
    public static readonly string[] Tones = ["Positive", "Concern", "Neutral"];
    public static readonly string[] Severities = ["Low", "Medium", "High"];
    public static readonly string[] PlanKinds = ["LearningAdjustments", "Behaviour", "Wellbeing", "Health"];

    public static string Kind(string? kind) => kind switch
    {
        "Behaviour" => "Behaviour",
        "Wellbeing" => "Wellbeing",
        "LearningSupport" => "Learning support",
        "FamilyContact" => "Contact with family",
        _ => "General"
    };

    public static string Tone(string? tone) => tone switch { "Positive" => "Praise", "Concern" => "Concern", _ => "Neither" };

    public static string Severity(string? severity) => severity switch { "High" => "Serious", "Medium" => "Moderate", _ => "Minor" };

    public static string PlanKind(string? kind) => kind switch
    {
        "Behaviour" => "Behaviour plan",
        "Wellbeing" => "Wellbeing plan",
        "Health" => "Health plan",
        _ => "Learning adjustments"
    };

    /// <summary>The badge colour for a note (Bootstrap 5.1 classes).</summary>
    public static string Badge(string? tone, string? severity) => tone switch
    {
        "Positive" => "bg-success",
        "Concern" => severity == "High" ? "bg-danger" : "bg-warning text-dark",
        _ => "bg-secondary"
    };
}

public static class PastoralFormat
{
    /// <summary>A date for a date picker ("2026-12-05").</summary>
    public static string Date(DateTime? value) => value is { } v ? v.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : "";

    /// <summary>Reads a posted date (yyyy-MM-dd) as that day in UTC.</summary>
    public static DateTime? ParseDate(string? value)
        => DateTime.TryParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var d)
            ? DateTime.SpecifyKind(d.Date, DateTimeKind.Utc)
            : null;

    /// <summary>The mood words, 1 (very low) to 5 (great).</summary>
    public static readonly string[] Moods = ["Very low", "Low", "Okay", "Good", "Great"];

    public static string MoodFace(int mood) => mood switch { 1 => "😢", 2 => "🙁", 3 => "😐", 4 => "🙂", _ => "😄" };

    public static string MoodWord(int mood) => mood is >= 1 and <= 5 ? Moods[mood - 1] : "";
}
