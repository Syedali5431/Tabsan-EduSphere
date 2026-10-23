using Tabsan.EduSphere.Domain.Outcomes;

namespace Tabsan.EduSphere.Application.Outcomes;

// Plan M6 — learning outcomes & mastery contracts.

/// <summary>Who is acting (from the API token).</summary>
public sealed record OutcomeActor(Guid UserId, string Role, Guid? TenantId, Guid? CampusId, bool IsSuperAdmin)
{
    public bool IsAdmin => IsSuperAdmin || Role is "Admin" or "SuperAdmin";
    public bool IsStaff => IsAdmin || Role == "Faculty";
}

public sealed record OutcomeRatingDto(string Description, decimal Points);

public sealed record OutcomeResponse(Guid Id, string Level, Guid? ProgramId, string? ProgramName, Guid? CourseId, string? CourseName, Guid? ParentId,
    string Code, string Title, string? Description, decimal MasteryPoints, decimal MaxPoints, string CalculationMethod, int? CalculationParameter,
    bool IsActive, IReadOnlyList<OutcomeRatingDto> Ratings, IReadOnlyList<Guid> ProgramOutcomeIds, int AlignmentCount, bool IsGlobal);

public sealed record SaveOutcomeRequest(string? Level, Guid? ProgramId, Guid? CourseId, Guid? ParentId, string Code, string Title, string? Description,
    IReadOnlyList<OutcomeRatingDto>? Ratings, decimal? MasteryPoints, string? CalculationMethod, int? CalculationParameter);

public sealed record SetOutcomeMappingsRequest(IReadOnlyList<Guid> ProgramOutcomeIds);

/// <summary>One row of the import workbook.</summary>
public sealed record OutcomeImportRow(int RowNumber, string Level, string? ProgramCode, string? CourseCode, string Code, string Title, string? Description,
    decimal? MasteryPoints, string? Method, int? Parameter, string? ParentCode, string? MapsTo);

public sealed record OutcomeImportResult(int Created, int Updated, int Mapped, IReadOnlyList<string> Errors);

public sealed record OutcomeSummary(Guid Id, string Code, string Title, string Level);

public sealed record AlignmentTargetResponse(string TargetType, Guid TargetId, string TargetTitle, Guid? CourseOfferingId,
    IReadOnlyList<OutcomeSummary> Aligned, IReadOnlyList<OutcomeSummary> Available);

public sealed record SetAlignmentsRequest(string TargetType, Guid TargetId, IReadOnlyList<Guid> OutcomeIds);

// ── mastery ──

public sealed record MasteryCell(Guid OutcomeId, decimal? Score, bool Mastered, int Evidence, string? Rating, string Status);

public sealed record MasteryOutcomeColumn(Guid OutcomeId, string Code, string Title, string Level, decimal MasteryPoints, decimal MaxPoints,
    string CalculationMethod, int AssessedCount, int MasteredCount);

public sealed record MasteryStudentRow(Guid StudentProfileId, string Name, string? RegistrationNumber, IReadOnlyList<MasteryCell> Cells);

public sealed record MasteryGradebookResponse(Guid CourseOfferingId, string CourseCode, string CourseTitle, string? SemesterName,
    IReadOnlyList<MasteryOutcomeColumn> Outcomes, IReadOnlyList<MasteryStudentRow> Students);

public sealed record StudentEvidenceItem(string Source, string Title, decimal Score, decimal Percent, DateTime AssessedAt);

public sealed record StudentMasteryItem(Guid OutcomeId, string Code, string Title, string? Description, string Level, decimal? Score,
    decimal MasteryPoints, decimal MaxPoints, bool Mastered, string? Rating, string Status, IReadOnlyList<StudentEvidenceItem> Evidence);

public sealed record StudentMasteryCourse(Guid CourseOfferingId, string CourseCode, string CourseTitle, string? SemesterName, IReadOnlyList<StudentMasteryItem> Outcomes);

// ── program attainment (accreditation) ──

public sealed record AttainmentContribution(string CourseCode, string CourseTitle, string? SemesterName, int Students, decimal AveragePercent);

public sealed record ProgramOutcomeAttainment(Guid OutcomeId, string Code, string Title, string? Description, decimal MasteryPoints, decimal MaxPoints,
    string CalculationMethod, int StudentsAssessed, int StudentsMastered, decimal? AttainmentPercent, decimal? AverageScore, bool? TargetMet,
    IReadOnlyList<string> MappedCourseOutcomes, IReadOnlyList<AttainmentContribution> Contributions);

public sealed record ProgramAttainmentReport(Guid ProgramId, string ProgramCode, string ProgramName, string? DepartmentName, DateTime GeneratedAt,
    decimal TargetPercent, Guid? SemesterId, int StudentsInProgram, IReadOnlyList<ProgramOutcomeAttainment> Outcomes);

public sealed record OutcomeResult<T>(T? Value, int Status, string? Error)
{
    public bool Ok => Status is >= 200 and < 300;
    public static OutcomeResult<T> Success(T value, int status = 200) => new(value, status, null);
    public static OutcomeResult<T> Fail(int status, string error) => new(default, status, error);
}

/// <summary>Reads and writes outcome workbooks and documents (ClosedXML / QuestPDF in Infrastructure).</summary>
public interface IOutcomeDocuments
{
    /// <summary>Reads the import sheet; returns rows or an error message.</summary>
    (IReadOnlyList<OutcomeImportRow> Rows, string? Error) ReadImport(Stream workbook);
    byte[] ImportTemplate();
    byte[] MasteryWorkbook(MasteryGradebookResponse gradebook);
    byte[] AttainmentWorkbook(ProgramAttainmentReport report);
    byte[] AttainmentPdf(ProgramAttainmentReport report, string institutionName);
}

/// <summary>Recalculates outcome evidence from graded work. Never throws (apart from cancellation).</summary>
public interface IOutcomeResultSync
{
    /// <summary>
    /// Rebuilds the evidence of the given students (all students when null) in an offering from posted assignment grades,
    /// rubric criteria and finished quiz attempts. <paramref name="source"/> limits it to one kind of work.
    /// </summary>
    Task RefreshAsync(Guid courseOfferingId, IReadOnlyCollection<Guid>? studentProfileIds, OutcomeSourceType? source, CancellationToken ct = default);
}
