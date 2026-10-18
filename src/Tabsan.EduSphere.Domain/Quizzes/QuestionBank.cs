using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Quizzes;

/// <summary>
/// Plan M2.2: a reusable pool of questions. Ownership decides who can use it:
/// a course offering (that course's staff), a department (shared across its courses),
/// or the whole tenant when both are null and <see cref="IsShared"/> is set.
/// Questions are <see cref="QuizQuestion"/> rows with <see cref="QuizQuestion.QuestionBankId"/> set.
/// </summary>
public class QuestionBank : AuditableEntity
{
    /// <summary>Tenant scope (null on single-tenant installs).</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Owning course offering, when the bank belongs to one course.</summary>
    public Guid? CourseOfferingId { get; private set; }

    /// <summary>Owning department, when the bank is shared across a department's courses.</summary>
    public Guid? DepartmentId { get; private set; }

    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }

    /// <summary>Visible to other staff beyond the owning course/department scope.</summary>
    public bool IsShared { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public IReadOnlyCollection<QuizQuestion> Questions { get; private set; } = new List<QuizQuestion>();

    private QuestionBank() { }

    public QuestionBank(Guid? tenantId, Guid? courseOfferingId, Guid? departmentId, string title, string? description, bool isShared, Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("A bank title is required.", nameof(title));

        TenantId = tenantId;
        CourseOfferingId = courseOfferingId;
        DepartmentId = departmentId;
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsShared = isShared;
        CreatedByUserId = createdByUserId;
    }

    public void Update(string title, string? description, bool isShared)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("A bank title is required.", nameof(title));

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsShared = isShared;
        Touch();
    }
}

/// <summary>
/// Plan M2.2: "pick <see cref="PickCount"/> random questions from bank X" inside a quiz.
/// Each attempt draws its own questions; every drawn question is worth <see cref="MarksPerQuestion"/>.
/// </summary>
public class QuizQuestionGroup : BaseEntity
{
    public Guid QuizId { get; private set; }
    public Quiz Quiz { get; private set; } = default!;

    public Guid QuestionBankId { get; private set; }
    public QuestionBank QuestionBank { get; private set; } = default!;

    public string Title { get; private set; } = default!;
    public int PickCount { get; private set; }
    public decimal MarksPerQuestion { get; private set; }

    /// <summary>Position among the quiz's questions and groups.</summary>
    public int OrderIndex { get; private set; }

    /// <summary>Optional: only draw bank questions carrying this tag.</summary>
    public string? TagFilter { get; private set; }

    private QuizQuestionGroup() { }

    public QuizQuestionGroup(Guid quizId, Guid questionBankId, string title, int pickCount, decimal marksPerQuestion, int orderIndex, string? tagFilter = null)
    {
        QuizId = quizId;
        QuestionBankId = questionBankId;
        Update(title, pickCount, marksPerQuestion, orderIndex, tagFilter);
    }

    public void Update(string title, int pickCount, decimal marksPerQuestion, int orderIndex, string? tagFilter)
    {
        if (pickCount < 1)
            throw new ArgumentException("Pick at least one question.", nameof(pickCount));
        if (marksPerQuestion < 0)
            throw new ArgumentException("Marks cannot be negative.", nameof(marksPerQuestion));

        Title = string.IsNullOrWhiteSpace(title) ? "Question group" : title.Trim();
        PickCount = pickCount;
        MarksPerQuestion = marksPerQuestion;
        OrderIndex = orderIndex;
        TagFilter = string.IsNullOrWhiteSpace(tagFilter) ? null : tagFilter.Trim().ToLowerInvariant();
        Touch();
    }
}

/// <summary>
/// Plan M2.5: extra time and/or attempts for one student — on one quiz, or (when
/// <see cref="QuizId"/> is null) on every quiz in the course offering.
/// </summary>
public class QuizAccommodation : BaseEntity
{
    public Guid CourseOfferingId { get; private set; }

    /// <summary>Null = applies to every quiz in the offering.</summary>
    public Guid? QuizId { get; private set; }

    public Guid StudentProfileId { get; private set; }

    /// <summary>Minutes added to the time limit.</summary>
    public int ExtraTimeMinutes { get; private set; }

    /// <summary>Multiplier applied to the time limit before extra minutes (e.g. 1.5 for time-and-a-half).</summary>
    public decimal TimeMultiplier { get; private set; } = 1m;

    /// <summary>Attempts added to the quiz's attempt cap.</summary>
    public int ExtraAttempts { get; private set; }

    public string? Note { get; private set; }

    private QuizAccommodation() { }

    public QuizAccommodation(Guid courseOfferingId, Guid? quizId, Guid studentProfileId, int extraTimeMinutes, decimal timeMultiplier, int extraAttempts, string? note)
    {
        CourseOfferingId = courseOfferingId;
        QuizId = quizId;
        StudentProfileId = studentProfileId;
        Update(extraTimeMinutes, timeMultiplier, extraAttempts, note);
    }

    public void Update(int extraTimeMinutes, decimal timeMultiplier, int extraAttempts, string? note)
    {
        if (extraTimeMinutes < 0 || extraAttempts < 0)
            throw new ArgumentException("Extra time and attempts cannot be negative.");
        if (timeMultiplier < 1m || timeMultiplier > 5m)
            throw new ArgumentException("Time multiplier must be between 1 and 5.", nameof(timeMultiplier));

        ExtraTimeMinutes = extraTimeMinutes;
        TimeMultiplier = timeMultiplier;
        ExtraAttempts = extraAttempts;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        Touch();
    }

    /// <summary>Applies this accommodation to a base time limit (minutes); null limits stay unlimited.</summary>
    public int? ApplyTo(int? baseMinutes)
        => baseMinutes is null ? null : (int)Math.Ceiling(baseMinutes.Value * TimeMultiplier) + ExtraTimeMinutes;
}

/// <summary>Plan M2.5: a different availability window for one student on one quiz (e.g. a make-up sitting).</summary>
public class QuizAvailabilityOverride : BaseEntity
{
    public Guid QuizId { get; private set; }
    public Guid StudentProfileId { get; private set; }
    public DateTime? AvailableFrom { get; private set; }
    public DateTime? AvailableUntil { get; private set; }
    public string? Note { get; private set; }

    private QuizAvailabilityOverride() { }

    public QuizAvailabilityOverride(Guid quizId, Guid studentProfileId, DateTime? availableFrom, DateTime? availableUntil, string? note)
    {
        QuizId = quizId;
        StudentProfileId = studentProfileId;
        Update(availableFrom, availableUntil, note);
    }

    public void Update(DateTime? availableFrom, DateTime? availableUntil, string? note)
    {
        if (availableFrom.HasValue && availableUntil.HasValue && availableUntil <= availableFrom)
            throw new ArgumentException("The window must end after it starts.");

        AvailableFrom = availableFrom;
        AvailableUntil = availableUntil;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        Touch();
    }
}
