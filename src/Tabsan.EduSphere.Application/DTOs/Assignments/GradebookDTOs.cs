// Final-Touches Phase 16 Stage 16.1/16.3 — DTOs for gradebook grid, upsert, and CSV bulk grading

namespace Tabsan.EduSphere.Application.DTOs.Assignments;

// ── Stage 16.1: Gradebook grid ────────────────────────────────────────────────

/// <summary>
/// Full gradebook grid for a course offering.
/// Columns are the active result-component rule names; rows are enrolled students.
/// </summary>
public sealed class GradebookGridResponse
{
    // Final-Touches Phase 16 Stage 16.1 — grid metadata
    public Guid                      CourseOfferingId { get; init; }
    public int                       InstitutionType  { get; init; }
    public bool                      UsesGpa          { get; init; }
    public List<GradebookColumnDto>  Columns          { get; init; } = new();
    public List<GradebookStudentRow> Rows             { get; init; } = new();
}

/// <summary>Represents one result-component column in the grid.</summary>
public sealed class GradebookColumnDto
{
    public string  ComponentName { get; init; } = string.Empty;
    public decimal Weightage     { get; init; }
}

/// <summary>One student's row in the gradebook grid.</summary>
public sealed class GradebookStudentRow
{
    public Guid                         StudentProfileId   { get; init; }
    public string                       RegistrationNumber { get; init; } = string.Empty;
    public string                       StudentName        { get; init; } = string.Empty;
    public List<GradebookCellDto>       Cells              { get; init; } = new();
    public decimal?                     WeightedTotal      { get; init; }
    public decimal?                     PercentageTotal    { get; init; }
    public decimal?                     Gpa                { get; init; }
    public decimal?                     Cgpa               { get; init; }
}

/// <summary>One result-component cell value for a student.</summary>
public sealed class GradebookCellDto
{
    public string   ComponentName  { get; init; } = string.Empty;
    public decimal? MarksObtained  { get; init; }
    public decimal? MaxMarks       { get; init; }
    public bool     IsPublished    { get; init; }
}

// ── Stage 16.1: Upsert single entry ──────────────────────────────────────────

public sealed class UpsertGradebookEntryRequest
{
    public Guid    StudentProfileId { get; init; }
    public Guid    CourseOfferingId { get; init; }
    public string  ComponentName    { get; init; } = string.Empty;
    public decimal MarksObtained    { get; init; }
    public decimal MaxMarks         { get; init; }
}

// ── Stage 16.3: CSV bulk grading ─────────────────────────────────────────────

/// <summary>One row parsed from an uploaded CSV for bulk grading preview.</summary>
public sealed class BulkGradeRowDto
{
    public string   RegistrationNumber { get; init; } = string.Empty;
    public string   StudentName        { get; init; } = string.Empty;
    public Guid?    StudentProfileId   { get; init; }
    public decimal? MarksObtained      { get; init; }
    public decimal? MaxMarks           { get; init; }
    public string?  ValidationError    { get; init; }
}

/// <summary>Preview response after parsing a bulk-grade CSV upload.</summary>
public sealed class BulkGradePreviewResponse
{
    // Final-Touches Phase 16 Stage 16.3 — CSV preview before confirm
    public string               ComponentName  { get; init; } = string.Empty;
    public int                  TotalRows      { get; init; }
    public int                  ValidRows      { get; init; }
    public int                  ErrorRows      { get; init; }
    public List<BulkGradeRowDto> Rows          { get; init; } = new();
}

/// <summary>Request body to confirm and apply a previously validated bulk-grade set.</summary>
public sealed class BulkGradeConfirmRequest
{
    public Guid                  CourseOfferingId { get; init; }
    public string                ComponentName    { get; init; } = string.Empty;
    public decimal               MaxMarks         { get; init; }
    public List<BulkGradeRowDto> ValidRows        { get; init; } = new();
}
