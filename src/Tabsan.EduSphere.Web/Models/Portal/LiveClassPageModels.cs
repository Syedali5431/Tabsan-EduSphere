using Tabsan.EduSphere.Application.LiveClasses;

namespace Tabsan.EduSphere.Web.Models.Portal;

// Plan M10 — live classes pages.

public sealed class LiveClassesPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public bool ModuleOff { get; set; }
    public bool IsStaff { get; set; }
    public Guid OfferingId { get; set; }
    public List<(Guid Id, string Label)> Offerings { get; set; } = [];
    public List<LiveSessionDto> Sessions { get; set; } = [];
    public List<LiveTimetableSlotDto> Slots { get; set; } = [];
    public Guid? EditingId { get; set; }
}

public sealed class LiveSessionAttendancePageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public Guid OfferingId { get; set; }
    public LiveAttendanceReportDto? Report { get; set; }
}

public sealed class LiveClassSettingsPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public bool ModuleOff { get; set; }
    public bool IsSuperAdmin { get; set; }
    public bool InstallationDefault { get; set; }
    public ConferenceSettingsDto? Settings { get; set; }
}

/// <summary>The "Live classes" card on the dashboard and calendar.</summary>
public sealed class UpcomingLiveClassesModel
{
    public List<LiveSessionDto> Sessions { get; set; } = [];
    public string ReturnUrl { get; set; } = "/";
}

public static class LiveFormat
{
    public static string When(LiveSessionDto s) => $"{s.StartsAt.ToUniversalTime():ddd dd MMM, HH:mm}–{s.EndsAt.ToUniversalTime():HH:mm} UTC";

    public static string StateBadge(string state) => state switch
    {
        "Live" => "bg-danger",
        "Upcoming" => "bg-primary",
        "Cancelled" => "bg-secondary",
        _ => "bg-light text-dark border"
    };
}
