using Tabsan.EduSphere.Application.Academic;

namespace Tabsan.EduSphere.Web.Models.Portal;

// Parent Links screen (2026-10-30): administrators link Parent-role accounts to School students.

public sealed class ParentLinksPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public bool MessageIsError { get; set; }
    public string? ParentSearch { get; set; }
    public string? StudentSearch { get; set; }
    public List<ParentAccountLookupDto> Parents { get; set; } = [];

    /// <summary>The parent being worked on; null until one is chosen.</summary>
    public ParentAccountLookupDto? Parent { get; set; }
    public List<ParentStudentLinkDetailDto> Links { get; set; } = [];
    public List<SchoolStudentLookupDto> Students { get; set; } = [];
    public bool StudentSearchRan { get; set; }

    public bool IsLinked(Guid studentProfileId) => Links.Any(l => l.StudentProfileId == studentProfileId);
}

public static class ParentLinkRelationships
{
    /// <summary>The choices on the screen, stored in English (the API accepts any text up to 60 characters).</summary>
    public static readonly IReadOnlyList<string> Standard = ["Father", "Mother", "Guardian", "Grandparent", "Sibling", "Other"];

    /// <summary>The standard choices, plus <paramref name="current"/> when a link was saved with another word (through the API).</summary>
    public static IReadOnlyList<string> ChoicesFor(string? current)
    {
        var value = Normalize(current);
        return value is null || Standard.Contains(value, StringComparer.OrdinalIgnoreCase) ? Standard : [.. Standard, value];
    }

    /// <summary>Whether <paramref name="choice"/> is the saved relationship (case-insensitive).</summary>
    public static bool IsSelected(string choice, string? current) => string.Equals(choice, Normalize(current), StringComparison.OrdinalIgnoreCase);

    /// <summary>Trimmed text, or null when empty.</summary>
    public static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
