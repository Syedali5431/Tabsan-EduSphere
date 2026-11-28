using Tabsan.EduSphere.Application.Lms;

namespace Tabsan.EduSphere.Web.Models.Portal;

/// <summary>Copying content into a course: where from, what, dates and visibility; the copies made so far; template sharing.</summary>
public sealed class CourseCopyPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public Guid OfferingId { get; set; }
    public Guid? SourceId { get; set; }
    public CourseCopySourceDto? Target { get; set; }
    public List<CourseCopySourceDto> Sources { get; set; } = [];
    public CourseCopyPreviewDto? Preview { get; set; }
    public List<CourseCopyRunDto> History { get; set; } = [];
    /// <summary>The copy just made (shown once after it).</summary>
    public CourseCopyResultDto? Result { get; set; }
    /// <summary>Admins and SuperAdmins share courses as templates.</summary>
    public bool CanShareTemplate { get; set; }
    public CourseTemplateDto? Template { get; set; }
}
