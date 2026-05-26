using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Enums;

namespace Tabsan.EduSphere.API.Controllers;

[ApiController]
[Route("api/v1/result-calculation")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class ResultCalculationController : ControllerBase
{
    private readonly IResultCalculationService _service;

    public ResultCalculationController(IResultCalculationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? institutionType, CancellationToken ct)
    {
        var selectedType = Enum.IsDefined(typeof(InstitutionType), institutionType ?? (int)InstitutionType.University)
            ? (InstitutionType)(institutionType ?? (int)InstitutionType.University)
            : InstitutionType.University;

        var settings = await _service.GetSettingsAsync(selectedType, ct);
        return Ok(settings);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveResultCalculationSettingsRequest request, CancellationToken ct)
    {
        try
        {
            if (!Enum.IsDefined(typeof(InstitutionType), request.InstitutionType))
                return BadRequest(new { message = "Invalid institutionType." });

            await _service.SaveSettingsAsync(request, ct);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}