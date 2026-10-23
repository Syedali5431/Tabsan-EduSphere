using Tabsan.EduSphere.Application.Assignments;
using Tabsan.EduSphere.Application.DTOs.Assignments;

namespace Tabsan.EduSphere.Web.Models.Portal;

// Plan M8 — late & missing policy, groups, assignment options/overrides and peer review pages.

/// <summary>Course-level page: late &amp; missing policy and group sets.</summary>
public sealed class AssignmentPoliciesPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public Guid OfferingId { get; set; }
    public List<(Guid Id, string Label)> Offerings { get; set; } = [];
    public LatePolicyDto? LatePolicy { get; set; }
    public List<GroupSetDto> GroupSets { get; set; } = [];
    public string OfferingLabel => Offerings.FirstOrDefault(o => o.Id == OfferingId).Label ?? "Course";
}

/// <summary>Assignment-level page: availability, group work, peer review, "Assign to" dates and peer-review overview.</summary>
public sealed class AssignmentOptionsPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public Guid AssignmentId { get; set; }
    public string Title { get; set; } = "Assignment";
    public Guid OfferingId { get; set; }
    public decimal MaxMarks { get; set; }
    public AssignmentOptionsDto? Options { get; set; }
    public List<AssignmentOverrideDto> Overrides { get; set; } = [];
    public List<GroupSetDto> GroupSets { get; set; } = [];
    public List<GroupMemberDto> Roster { get; set; } = [];
    public PeerReviewOverviewDto? PeerReviews { get; set; }
    /// <summary>Plan M9: similarity option (null when the similarity module is not licensed).</summary>
    public Tabsan.EduSphere.Application.Similarity.AssignmentSimilarityDto? Similarity { get; set; }
}

/// <summary>Student page: their groups in a course, joining and leaving self sign-up groups.</summary>
public sealed class MyGroupsPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public Guid OfferingId { get; set; }
    public List<(Guid Id, string Label)> Offerings { get; set; } = [];
    public List<MyGroupDto> Groups { get; set; } = [];
}

/// <summary>Student page: one peer review to write.</summary>
public sealed class PeerReviewPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public PeerReviewTaskDto? Task { get; set; }
}

/// <summary>Formatting shared by the M8 pages (dates are entered and shown in UTC, like module unlock dates).</summary>
public static class AssignmentPolicyFormat
{
    public static DateTime Utc(DateTime v) => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime();

    public static string Input(DateTime? value) => value is { } v ? Utc(v).ToString("yyyy-MM-ddTHH:mm") : string.Empty;

    public static string Show(DateTime? value) => value is { } v ? Utc(v).ToString("dd MMM yyyy HH:mm") + " UTC" : "—";

    public static string Marks(decimal value) => value.ToString("0.##");

    public static string Policy(LatePolicyDto? p)
    {
        if (p is null) return "No late policy.";
        var parts = new List<string>();
        parts.Add(p.LateDeductionEnabled
            ? $"Late work loses {Marks(p.DeductionPercent)}% of the points per {(p.Interval == "Hour" ? "hour" : "day")} late" + (p.LowestGradePercent > 0 ? $" (never below {Marks(p.LowestGradePercent)}%)" : string.Empty) + "."
            : "No deduction for late work.");
        if (p.MissingEnabled) parts.Add($"Work never handed in is graded {Marks(p.MissingGradePercent)}%.");
        return string.Join(" ", parts);
    }
}
