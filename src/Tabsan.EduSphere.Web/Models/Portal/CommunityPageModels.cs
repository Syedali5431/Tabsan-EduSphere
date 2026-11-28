using System.Globalization;
using Tabsan.EduSphere.Application.Community;

namespace Tabsan.EduSphere.Web.Models.Portal;

/// <summary>The news list, and for writers the posts they manage.</summary>
public sealed class NewsPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public bool CanWrite { get; set; }
    /// <summary>"manage" shows the writer's posts.</summary>
    public string Tab { get; set; } = "feed";
    public List<NewsSummaryDto> Feed { get; set; } = [];
    public List<NewsManageDto> Managed { get; set; } = [];
}

public sealed class NewsPostPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public NewsPostDto? Post { get; set; }
}

/// <summary>Writing or changing a post (also kept when the API refuses, so nothing written is lost).</summary>
public sealed class EditNewsPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public Guid? Id { get; set; }
    public AudienceOptionsDto? Options { get; set; }
    public NewsInput Input { get; set; } = new();
}

public sealed class NewsInput
{
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? Body { get; set; }
    public string? ImageUrl { get; set; }
    public AudienceInput Audience { get; set; } = new();
    /// <summary>UTC, ISO 8601 (the page fills it from the local time picked).</summary>
    public string? PublishAt { get; set; }
    public string? ExpiresAt { get; set; }
    public bool Pinned { get; set; }
}

/// <summary>Who an item is for, as the form posts it.</summary>
public sealed class AudienceInput
{
    public Guid? CampusId { get; set; }
    public Guid? SemesterId { get; set; }
    public Guid? CourseOfferingId { get; set; }
    public List<string> Roles { get; set; } = [];

    public AudienceRequest ToRequest() => new(CampusId, SemesterId, CourseOfferingId, Roles);

    public static AudienceInput From(AudienceDto a) => new() { CampusId = a.CampusId, SemesterId = a.SemesterId, CourseOfferingId = a.CourseOfferingId, Roles = a.Roles.ToList() };
}

public sealed class EventsPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public bool CanWrite { get; set; }
    public string Tab { get; set; } = "upcoming";
    public List<EventDto> Events { get; set; } = [];
}

public sealed class EditEventPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public Guid? Id { get; set; }
    public AudienceOptionsDto? Options { get; set; }
    public List<FormManageDto> Forms { get; set; } = [];
    public EventInput Input { get; set; } = new();
}

public sealed class EventInput
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string Category { get; set; } = "General";
    public string? StartsAt { get; set; }
    public string? EndsAt { get; set; }
    public bool AllDay { get; set; }
    public bool RepliesOpen { get; set; }
    public string? ReplyBy { get; set; }
    public int? Capacity { get; set; }
    public int MaxGuests { get; set; }
    public Guid? FormId { get; set; }
    public AudienceInput Audience { get; set; } = new();
}

public sealed class EventRepliesPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public EventRepliesDto? Replies { get; set; }
}

public sealed class FormsPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public bool CanWrite { get; set; }
    public string Tab { get; set; } = "fill";
    public List<FormToFillDto> ToFill { get; set; } = [];
    public List<FormManageDto> Managed { get; set; } = [];
}

public sealed class FillFormPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public FormDto? Form { get; set; }
    /// <summary>What was typed, kept when the API refuses the answers.</summary>
    public Dictionary<Guid, string> Typed { get; set; } = [];
}

public sealed class EditFormPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public Guid? Id { get; set; }
    public bool Answered { get; set; }
    public AudienceOptionsDto? Options { get; set; }
    public FormInput Input { get; set; } = new();
}

public sealed class FormInput
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? DueAt { get; set; }
    public AudienceInput Audience { get; set; } = new();
    public List<FieldInput> Fields { get; set; } = [];
}

public sealed class FieldInput
{
    public string Type { get; set; } = "ShortText";
    public string? Label { get; set; }
    public string? Help { get; set; }
    public bool Required { get; set; }
    /// <summary>One choice per line.</summary>
    public string? Options { get; set; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Label);

    public FormFieldRequest ToRequest()
        => new(Type, Label, Help, Required, (Options ?? "").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

    public static FieldInput From(FormFieldDto f) => new() { Type = f.Type, Label = f.Label, Help = f.Help, Required = f.Required, Options = string.Join("\n", f.Options) };
}

public sealed class FormResponsesPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public FormTrackingDto? Tracking { get; set; }
}

/// <summary>The audience fields of an editor: what may be chosen and what is chosen.</summary>
public sealed record AudiencePickerModel(AudienceOptionsDto? Options, AudienceInput Value, bool IsNew);

public static class CommunityFormat
{
    /// <summary>A UTC time for a hidden form field ("2026-12-05T04:00:00Z").</summary>
    public static string Iso(DateTime? value) => value is { } v ? DateTime.SpecifyKind(v, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture) : "";

    /// <summary>Reads a posted UTC time (ISO 8601; a time without a zone counts as UTC).</summary>
    public static DateTime? Parse(string? value)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var d)
            ? DateTime.SpecifyKind(d, DateTimeKind.Utc)
            : null;

}
