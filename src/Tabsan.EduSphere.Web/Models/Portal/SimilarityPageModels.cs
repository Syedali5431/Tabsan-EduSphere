using Tabsan.EduSphere.Application.Similarity;

namespace Tabsan.EduSphere.Web.Models.Portal;

// Plan M9 — similarity checking pages.

public sealed class SimilaritySettingsPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public bool ModuleOff { get; set; }
    public bool IsSuperAdmin { get; set; }
    public bool InstallationDefault { get; set; }
    public SimilaritySettingsDto? Settings { get; set; }
}

public sealed class SimilarityReportPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public SimilarityReportDto? Report { get; set; }
}

public static class SimilarityFormat
{
    /// <summary>Badge colour by score: green below 25%, amber below 50%, red from 50%.</summary>
    public static string BadgeClass(decimal? score) => score switch
    {
        null => "bg-secondary",
        < 25 => "bg-success",
        < 50 => "bg-warning text-dark",
        _ => "bg-danger"
    };

    public static string Status(string status, decimal? score) => status switch
    {
        "Completed" => $"{score:0.#}%",
        "Queued" or "Submitted" => "Checking…",
        "Skipped" => "No text",
        _ => "Failed"
    };
}
