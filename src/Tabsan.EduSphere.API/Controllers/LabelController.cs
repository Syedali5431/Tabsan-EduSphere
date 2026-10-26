using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.API.Middleware;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Returns the academic vocabulary appropriate for the current institution policy.
/// Consumed by the web layer to replace hardcoded "Semester", "Course" etc. labels.
/// </summary>
[ApiController]
[Route("api/v1/labels")]
[Authorize]
public sealed class LabelController : ControllerBase
{
    private readonly ILabelService _labels;

    public LabelController(ILabelService labels) => _labels = labels;

    /// <summary>Returns the current institution-mode vocabulary.</summary>
    [HttpGet]
    public IActionResult GetVocabulary()
    {
        var policy = HttpContext.GetInstitutionVisibility();
        var vocab  = _labels.GetVocabulary(policy);

        return Ok(new
        {
            vocab.PeriodLabel,
            vocab.ProgressionLabel,
            vocab.GradingLabel,
            vocab.CourseLabel,
            vocab.StudentGroupLabel
        });
    }
}
