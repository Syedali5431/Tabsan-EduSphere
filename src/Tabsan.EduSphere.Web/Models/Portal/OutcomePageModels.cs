using Tabsan.EduSphere.Application.Outcomes;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Web.Models.Portal;

// Plan M6 — learning outcomes & mastery pages.

public sealed class OutcomesPageModel
{
    public bool IsConnected { get; set; }
    public bool ModuleOff { get; set; }
    public bool IsAdmin { get; set; }
    public string? Message { get; set; }
    public string? Level { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? CourseId { get; set; }
    public bool IncludeInactive { get; set; }
    public IReadOnlyList<OutcomeResponse> Outcomes { get; set; } = [];
    public OutcomeResponse? Editing { get; set; }
    /// <summary>Program outcomes a course outcome can be mapped to (and parents for program outcomes).</summary>
    public IReadOnlyList<OutcomeResponse> ProgramOutcomes { get; set; } = [];
    public IReadOnlyList<OutcomeProgramInfo> Programs { get; set; } = [];
    public IReadOnlyList<LookupItem> Courses { get; set; } = [];
    public IReadOnlyList<LookupItem> Offerings { get; set; } = [];
    public OutcomeImportResult? Import { get; set; }
}

/// <summary>The outcome form (rating scale rows come as parallel lists).</summary>
public sealed class OutcomeFormModel
{
    public Guid? Id { get; set; }
    public string? Level { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? CourseId { get; set; }
    public Guid? ParentId { get; set; }
    public string? Code { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<string?> RatingDescription { get; set; } = [];
    public List<decimal?> RatingPoints { get; set; } = [];
    public decimal? MasteryPoints { get; set; }
    public string? CalculationMethod { get; set; }
    public int? CalculationParameter { get; set; }
}

public sealed class OutcomeMasteryPageModel
{
    public bool IsConnected { get; set; }
    public bool ModuleOff { get; set; }
    public string? Message { get; set; }
    public Guid OfferingId { get; set; }
    public IReadOnlyList<LookupItem> Offerings { get; set; } = [];
    public MasteryGradebookResponse? Gradebook { get; set; }
}

public sealed class OutcomeAttainmentPageModel
{
    public bool IsConnected { get; set; }
    public bool ModuleOff { get; set; }
    public string? Message { get; set; }
    public Guid ProgramId { get; set; }
    public decimal Target { get; set; } = 70;
    public IReadOnlyList<OutcomeProgramInfo> Programs { get; set; } = [];
    public ProgramAttainmentReport? Report { get; set; }
}

public sealed class MyOutcomesPageModel
{
    public bool IsConnected { get; set; }
    public bool ModuleOff { get; set; }
    public string? Message { get; set; }
    public IReadOnlyList<StudentMasteryCourse> Courses { get; set; } = [];
}
