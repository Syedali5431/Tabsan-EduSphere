using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Assignments;

/// <summary>Plan M3.5: when students see their grades.</summary>
public enum GradePostingPolicy
{
    /// <summary>A grade is visible as soon as it is saved (the behaviour before Plan M3).</summary>
    Automatic = 0,

    /// <summary>Grades stay hidden until a grader posts them.</summary>
    Manual = 1
}

/// <summary>
/// An assignment created by a faculty member for a specific <see cref="CourseOffering"/>.
/// Once published it is visible to enrolled students. It can be soft-deleted (retracted)
/// before any submissions are made but never after.
/// </summary>
public class Assignment : AuditableEntity
{
    /// <summary>FK to the course offering this assignment belongs to.</summary>
    public Guid CourseOfferingId { get; private set; }

    /// <summary>Short title shown in student dashboards (e.g. "Lab 1 – Linked Lists").</summary>
    public string Title { get; private set; } = default!;

    /// <summary>Full instructions / problem statement. May contain Markdown.</summary>
    public string? Description { get; private set; }

    /// <summary>UTC deadline after which submissions are rejected.</summary>
    public DateTime DueDate { get; private set; }

    /// <summary>Maximum marks available. Used to validate awarded marks.</summary>
    public decimal MaxMarks { get; private set; }

    /// <summary>
    /// True once the assignment has been published and students can see and submit.
    /// Unpublished assignments are visible to faculty only.
    /// </summary>
    public bool IsPublished { get; private set; }

    /// <summary>UTC timestamp of when the assignment was published.</summary>
    public DateTime? PublishedAt { get; private set; }

    // ── Plan M3: submission settings and grading policies ───────────────────

    /// <summary>Hard limit on a single file, whatever the assignment allows.</summary>
    public const int MaxFileSizeLimitMb = 50;

    /// <summary>Hard limit on files per attempt.</summary>
    public const int MaxFilesPerAttempt = 10;

    /// <summary>Hard limit on all files of one attempt together.</summary>
    public const int MaxAttemptTotalMb = 100;

    /// <summary>File types accepted when the assignment does not narrow them. No HTML/SVG/scripts/executables.</summary>
    public static readonly IReadOnlyList<string> DefaultFileTypes =
    [
        ".pdf", ".doc", ".docx", ".odt", ".rtf", ".txt", ".ppt", ".pptx", ".odp", ".xls", ".xlsx", ".ods", ".csv",
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".zip", ".mp3", ".m4a", ".wav", ".mp4", ".webm"
    ];

    /// <summary>How many times a student may submit (resubmissions keep every attempt). 0 = unlimited.</summary>
    public int AllowedAttempts { get; private set; } = 1;

    /// <summary>Comma-separated extensions (".pdf,.docx"); null = <see cref="DefaultFileTypes"/>.</summary>
    public string? AllowedFileTypes { get; private set; }

    /// <summary>Largest file a student may upload, in MB.</summary>
    public int MaxFileSizeMb { get; private set; } = 10;

    /// <summary>Hide student names from graders until names are revealed (posting all grades reveals them).</summary>
    public bool AnonymousGrading { get; private set; }

    public DateTime? NamesRevealedAt { get; private set; }

    /// <summary>Automatic: students see a grade as soon as it is saved. Manual: only after "Post grades".</summary>
    public GradePostingPolicy PostingPolicy { get; private set; } = GradePostingPolicy.Automatic;

    /// <summary>Graders enter provisional grades; the moderator chooses the final grade.</summary>
    public bool ModeratedGrading { get; private set; }

    public Guid? ModeratorUserId { get; private set; }

    /// <summary>Plan M5: the LTI tool this assignment's scores come from (students work in the tool, not by upload).</summary>
    public Guid? ExternalToolId { get; private set; }

    /// <summary>Plan M5: an assignment created for an LTI line item. Published at once so tool scores can be recorded.</summary>
    public static Assignment ForExternalTool(Guid courseOfferingId, string title, decimal maxMarks, DateTime dueDate, Guid toolId)
    {
        var assignment = new Assignment(courseOfferingId, title, "Completed in an external tool.", dueDate, maxMarks) { ExternalToolId = toolId };
        assignment.Publish();
        return assignment;
    }

    /// <summary>Plan M13: the SCORM package whose best score this assignment holds (students work in the package, not by upload).</summary>
    public Guid? ScormPackageId { get; private set; }

    /// <summary>Plan M13: the gradebook assignment of a graded SCORM package. Published at once so scores can be recorded.</summary>
    public static Assignment ForScormPackage(Guid courseOfferingId, string title, decimal maxMarks, DateTime dueDate, Guid packageId)
    {
        var assignment = new Assignment(courseOfferingId, title, "Completed in a SCORM package.", dueDate, maxMarks) { ScormPackageId = packageId };
        assignment.Publish();
        return assignment;
    }

    /// <summary>Work done outside the hand-in page (an LTI tool or a SCORM package): no uploads, groups, peer review or similarity checks.</summary>
    public bool IsCompletedElsewhere => ExternalToolId is not null || ScormPackageId is not null;

    /// <summary>Plan M5/M13: keeps the external assignment's title and maximum in line with its line item or package.</summary>
    public void SyncExternal(string title, decimal maxMarks)
    {
        if (!IsCompletedElsewhere) throw new InvalidOperationException("Only external-tool and SCORM assignments follow their source.");
        Title = title.Trim();
        MaxMarks = maxMarks > 0 ? maxMarks : throw new ArgumentOutOfRangeException(nameof(maxMarks));
        Touch();
    }

    // ── Plan M8: availability, groups and peer review ──────────────────────

    /// <summary>Plan M8.1: students cannot submit before this time (null = as soon as it is published).</summary>
    public DateTime? AvailableFrom { get; private set; }

    /// <summary>Plan M8.1: hard cut-off; late submissions are accepted until then (null = no late submissions after the due date).</summary>
    public DateTime? AvailableUntil { get; private set; }

    /// <summary>Plan M8.3: a group assignment — one submission per group of this set.</summary>
    public Guid? GroupSetId { get; private set; }

    /// <summary>Plan M8.3: grade each member separately instead of giving the group one grade.</summary>
    public bool GradeGroupIndividually { get; private set; }

    /// <summary>Plan M8.4: peer reviews each student gives (0 = no peer review).</summary>
    public int PeerReviewCount { get; private set; }

    public DateTime? PeerReviewDueDate { get; private set; }

    /// <summary>Plan M8.4: reviewers and authors do not see each other's names.</summary>
    public bool PeerReviewAnonymous { get; private set; } = true;

    /// <summary>The last moment a submission is accepted without an override.</summary>
    public DateTime ClosesAt => AvailableUntil ?? DueDate;

    /// <summary>Plan M8.1: when submissions open and the late cut-off.</summary>
    public void ConfigureAvailability(DateTime? availableFrom, DateTime? availableUntil)
    {
        if (availableFrom is { } from && from > DueDate) throw new ArgumentException("Submissions must open before the due date.", nameof(availableFrom));
        if (availableUntil is { } until && until < DueDate) throw new ArgumentException("The late cut-off cannot be before the due date.", nameof(availableUntil));
        AvailableFrom = availableFrom;
        AvailableUntil = availableUntil;
        Touch();
    }

    /// <summary>Plan M8.3: makes this a group assignment (or an individual one with null).</summary>
    public void ConfigureGroups(Guid? groupSetId, bool gradeIndividually)
    {
        if (IsCompletedElsewhere && groupSetId is not null) throw new ArgumentException("External tool and SCORM assignments cannot be group assignments.", nameof(groupSetId));
        GroupSetId = groupSetId;
        GradeGroupIndividually = groupSetId is not null && gradeIndividually;
        Touch();
    }

    /// <summary>Plan M8.4: peer review settings.</summary>
    public void ConfigurePeerReview(int count, DateTime? dueDate, bool anonymous)
    {
        if (count is < 0 or > 5) throw new ArgumentException("Each student can review 0 to 5 submissions.", nameof(count));
        if (count > 0 && IsCompletedElsewhere) throw new ArgumentException("External tool and SCORM assignments cannot have peer reviews.", nameof(count));
        if (count > 0 && dueDate is { } d && d < DueDate) throw new ArgumentException("Peer reviews are due after the assignment.", nameof(dueDate));
        PeerReviewCount = count;
        PeerReviewDueDate = count > 0 ? dueDate : null;
        PeerReviewAnonymous = anonymous;
        Touch();
    }

    // ── Plan M9: similarity checking ─────────────────────────────────────────

    /// <summary>Every handed-in attempt is checked for similarity.</summary>
    public bool SimilarityCheckEnabled { get; private set; }

    /// <summary>Students see their own similarity score (never the matching sources).</summary>
    public bool SimilarityVisibleToStudents { get; private set; }

    public void ConfigureSimilarity(bool enabled, bool visibleToStudents)
    {
        if (enabled && IsCompletedElsewhere) throw new ArgumentException("External tool and SCORM assignments are not handed in here, so they cannot be checked.", nameof(enabled));
        SimilarityCheckEnabled = enabled;
        SimilarityVisibleToStudents = enabled && visibleToStudents;
        Touch();
    }

    /// <summary>True while graders must not see student names.</summary>
    public bool NamesHidden => AnonymousGrading && NamesRevealedAt is null;

    public IReadOnlyList<string> EffectiveFileTypes => ParseFileTypes(AllowedFileTypes) is { Count: > 0 } narrowed ? narrowed : DefaultFileTypes;

    /// <summary>Whether a student may make attempt number <paramref name="attemptNumber"/>.</summary>
    public bool AllowsAttempt(int attemptNumber) => AllowedAttempts == 0 || attemptNumber <= AllowedAttempts;

    public bool AcceptsFileType(string fileName)
        => EffectiveFileTypes.Contains(Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant());

    public void ConfigureSubmissions(int allowedAttempts, IEnumerable<string>? allowedFileTypes, int maxFileSizeMb)
    {
        if (allowedAttempts is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(allowedAttempts), "Allowed attempts must be between 0 (unlimited) and 100.");
        if (maxFileSizeMb is < 1 or > MaxFileSizeLimitMb)
            throw new ArgumentOutOfRangeException(nameof(maxFileSizeMb), $"Maximum file size must be between 1 and {MaxFileSizeLimitMb} MB.");

        var types = (allowedFileTypes ?? []).Select(NormalizeExtension).Where(t => t is not null).Select(t => t!).Distinct().ToList();
        var unsafeTypes = types.Where(t => !DefaultFileTypes.Contains(t)).ToList();
        if (unsafeTypes.Count > 0)
            throw new ArgumentException($"These file types cannot be accepted: {string.Join(", ", unsafeTypes)}.", nameof(allowedFileTypes));

        AllowedAttempts = allowedAttempts;
        AllowedFileTypes = types.Count == 0 ? null : string.Join(",", types);
        MaxFileSizeMb = maxFileSizeMb;
        Touch();
    }

    public void ConfigureGrading(bool anonymousGrading, GradePostingPolicy postingPolicy, bool moderatedGrading, Guid? moderatorUserId)
    {
        if (moderatedGrading && moderatorUserId is null)
            throw new ArgumentException("Moderated grading needs a moderator.", nameof(moderatorUserId));

        if (anonymousGrading && !AnonymousGrading) NamesRevealedAt = null;
        AnonymousGrading = anonymousGrading;
        PostingPolicy = postingPolicy;
        ModeratedGrading = moderatedGrading;
        ModeratorUserId = moderatedGrading ? moderatorUserId : null;
        Touch();
    }

    public void RevealNames(DateTime now)
    {
        if (!AnonymousGrading || NamesRevealedAt is not null) return;
        NamesRevealedAt = now;
        Touch();
    }

    public static IReadOnlyList<string> ParseFileTypes(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeExtension).Where(t => t is not null).Select(t => t!).Distinct().ToList();

    private static string? NormalizeExtension(string? value)
    {
        var t = value?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(t)) return null;
        return t.StartsWith('.') ? t : "." + t;
    }

    private Assignment() { }

    /// <summary>
    /// Creates a new, unpublished assignment.
    /// The assignment must be explicitly published before students can see it.
    /// </summary>
    public Assignment(Guid courseOfferingId, string title, string? description, DateTime dueDate, decimal maxMarks)
    {
        CourseOfferingId = courseOfferingId;
        Title = title.Trim();
        Description = description?.Trim();
        DueDate = dueDate;
        MaxMarks = maxMarks > 0
            ? maxMarks
            : throw new ArgumentOutOfRangeException(nameof(maxMarks), "MaxMarks must be greater than 0.");
    }

    /// <summary>
    /// Publishes the assignment so enrolled students can view and submit.
    /// Throws if already published.
    /// </summary>
    public void Publish()
    {
        if (IsPublished)
            throw new InvalidOperationException("Assignment is already published.");

        IsPublished = true;
        PublishedAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    /// Retracts a published assignment (sets IsPublished to false).
    /// Only allowed before any submissions exist — enforced at the service layer.
    /// </summary>
    public void Retract()
    {
        if (!IsPublished)
            throw new InvalidOperationException("Assignment is not published.");

        IsPublished = false;
        PublishedAt = null;
        Touch();
    }

    /// <summary>
    /// Updates the assignment's editable fields.
    /// Only allowed before publishing.
    /// </summary>
    public void Update(string title, string? description, DateTime dueDate, decimal maxMarks)
    {
        if (IsPublished)
            throw new InvalidOperationException("Cannot edit a published assignment.");

        Title = title.Trim();
        Description = description?.Trim();
        DueDate = dueDate;
        MaxMarks = maxMarks > 0
            ? maxMarks
            : throw new ArgumentOutOfRangeException(nameof(maxMarks), "MaxMarks must be greater than 0.");
        Touch();
    }
}
