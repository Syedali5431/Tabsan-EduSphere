// Final-Touches Phase 16 Stage 16.1/16.3 — GradebookService implementation

using System.Text;
using Tabsan.EduSphere.Application.DTOs.Assignments;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Assignments;

/// <summary>
/// Builds the gradebook grid from enrollments + results + component rules,
/// provides inline entry upsert, bulk CSV import/export, and publish-all.
/// </summary>
public class GradebookService : IGradebookService
{
    // Final-Touches Phase 16 Stage 16.1 — dependencies
    private readonly IGradebookRepository _gradebookRepo;
    private readonly IResultRepository    _resultRepo;

    public GradebookService(IGradebookRepository gradebookRepo, IResultRepository resultRepo)
    {
        _gradebookRepo = gradebookRepo;
        _resultRepo    = resultRepo;
    }

    // ── Stage 16.1: Gradebook grid ────────────────────────────────────────────

    public async Task<GradebookGridResponse> GetGradebookAsync(Guid courseOfferingId, CancellationToken ct = default)
    {
        var institutionType = await _gradebookRepo.GetInstitutionTypeForOfferingAsync(courseOfferingId, ct);
        var usesGpa = institutionType == InstitutionType.University;

        // Query sequentially because scoped repositories can share one DbContext instance.
        // Running these queries in parallel can trigger EF Core's "second operation" exception.
        var students = await _gradebookRepo.GetStudentsForOfferingAsync(courseOfferingId, ct);
        var components = (await _resultRepo.GetActiveComponentRulesAsync(institutionType, ct))
            .OrderBy(c => c.DisplayOrder)
            .ToList();
        var results = await _resultRepo.GetByOfferingAsync(courseOfferingId, ct);

        if (components.Count == 0)
        {
            var resultTypes = results
                .Select(r => r.ResultType)
                .Where(rt => !string.IsNullOrWhiteSpace(rt))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(rt => rt)
                .ToList();

            if (resultTypes.Count > 0)
            {
                var fallbackWeight = Math.Round(100m / resultTypes.Count, 2, MidpointRounding.AwayFromZero);
                components = resultTypes
                    .Select((rt, index) => new ResultComponentRule(rt, fallbackWeight, index + 1, true, institutionType))
                    .ToList();
            }
        }

        var gpaRules = usesGpa
            ? (await _resultRepo.GetGpaScaleRulesAsync(institutionType, ct))
                .OrderByDescending(r => r.MinimumScore)
                .ToList()
            : new List<GpaScaleRule>();

        // Index results by (studentProfileId, resultType) for O(1) lookup
        var resultIndex = results.ToDictionary(
            r => (r.StudentProfileId, r.ResultType),
            r => r);

        var columns = components.Select(c => new GradebookColumnDto
        {
            ComponentName = c.Name,
            Weightage     = c.Weightage
        }).ToList();

        var rows = students.Select(student =>
        {
            var cells = components.Select(comp =>
            {
                resultIndex.TryGetValue((student.StudentProfileId, comp.Name), out var result);
                return new GradebookCellDto
                {
                    ComponentName = comp.Name,
                    MarksObtained = result?.MarksObtained,
                    MaxMarks      = result?.MaxMarks,
                    IsPublished   = result?.IsPublished ?? false
                };
            }).ToList();

            // University: weighted aggregate converted to GPA + show stored CGPA.
            // School/College: percentage is computed from available scored components.
            decimal? weightedTotal = null;
            decimal? percentageTotal = null;
            decimal? gpa = null;

            if (usesGpa && components.Count > 0 && cells.All(c => c.MarksObtained.HasValue && c.MaxMarks.HasValue && c.MaxMarks > 0))
            {
                weightedTotal = 0m;
                foreach (var (comp, cell) in components.Zip(cells))
                {
                    weightedTotal += (cell.MarksObtained!.Value / cell.MaxMarks!.Value) * comp.Weightage;
                }

                var matchingRule = gpaRules.FirstOrDefault(r => weightedTotal.Value >= r.MinimumScore);
                gpa = matchingRule?.GradePoint;
            }

            if (!usesGpa)
            {
                var scoredCells = cells.Where(c => c.MarksObtained.HasValue && c.MaxMarks.HasValue && c.MaxMarks > 0).ToList();
                if (scoredCells.Count > 0)
                {
                    var marksSum = scoredCells.Sum(c => c.MarksObtained!.Value);
                    var maxSum = scoredCells.Sum(c => c.MaxMarks!.Value);
                    if (maxSum > 0)
                    {
                        percentageTotal = (marksSum / maxSum) * 100m;
                    }
                }
            }

            return new GradebookStudentRow
            {
                StudentProfileId   = student.StudentProfileId,
                RegistrationNumber = student.RegistrationNumber,
                StudentName        = student.StudentName,
                Cells              = cells,
                WeightedTotal      = weightedTotal,
                PercentageTotal    = percentageTotal,
                Gpa                = gpa,
                Cgpa               = usesGpa ? student.Cgpa : null
            };
        }).ToList();

        return new GradebookGridResponse
        {
            CourseOfferingId = courseOfferingId,
            InstitutionType  = (int)institutionType,
            UsesGpa          = usesGpa,
            Columns          = columns,
            Rows             = rows
        };
    }

    // ── Stage 16.1: Upsert single cell ────────────────────────────────────────

    public async Task UpsertEntryAsync(UpsertGradebookEntryRequest request, Guid facultyUserId, CancellationToken ct = default)
    {
        // Final-Touches Phase 16 Stage 16.1 — create or correct a result cell
        var exists = await _resultRepo.ExistsAsync(
            request.StudentProfileId, request.CourseOfferingId, request.ComponentName, ct);

        if (exists)
        {
            var existing = await _resultRepo.GetAsync(
                request.StudentProfileId, request.CourseOfferingId, request.ComponentName, ct);
            if (existing is null) return;

            existing.CorrectMarks(request.MarksObtained, request.MaxMarks);
            _resultRepo.Update(existing);
        }
        else
        {
            var result = new Result(
                request.StudentProfileId,
                request.CourseOfferingId,
                request.ComponentName,
                request.MarksObtained,
                request.MaxMarks);
            await _resultRepo.AddAsync(result, ct);
        }

        await _resultRepo.SaveChangesAsync(ct);
    }

    // ── Stage 16.1: Publish all ───────────────────────────────────────────────

    public async Task PublishAllAsync(Guid courseOfferingId, Guid facultyUserId, CancellationToken ct = default)
    {
        // Final-Touches Phase 16 Stage 16.1 — publish all draft results for offering
        var results = await _resultRepo.GetByOfferingAsync(courseOfferingId, ct);
        foreach (var result in results.Where(r => !r.IsPublished))
        {
            result.Publish(facultyUserId);
            _resultRepo.Update(result);
        }
        await _resultRepo.SaveChangesAsync(ct);
    }

    // ── Stage 16.3: CSV template ──────────────────────────────────────────────

    public async Task<byte[]> GetCsvTemplateAsync(Guid courseOfferingId, string componentName, CancellationToken ct = default)
    {
        // Final-Touches Phase 16 Stage 16.3 — build CSV template with student rows
        var students = await _gradebookRepo.GetStudentsForOfferingAsync(courseOfferingId, ct);

        var sb = new StringBuilder();
        sb.AppendLine("RegistrationNumber,StudentName,MarksObtained,MaxMarks");
        foreach (var s in students)
        {
            // Pre-fill MaxMarks as empty so faculty fills it in
            sb.AppendLine($"{Escape(s.RegistrationNumber)},{Escape(s.StudentName)},,");
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    // ── Stage 16.3: Parse CSV upload ──────────────────────────────────────────

    public async Task<BulkGradePreviewResponse> ParseBulkCsvAsync(
        Guid courseOfferingId,
        string componentName,
        Stream csvStream,
        CancellationToken ct = default)
    {
        // Final-Touches Phase 16 Stage 16.3 — parse CSV and validate rows
        var students = await _gradebookRepo.GetStudentsForOfferingAsync(courseOfferingId, ct);
        var regNumIndex = students.ToDictionary(
            s => s.RegistrationNumber.Trim().ToUpperInvariant(),
            s => s);

        using var reader = new StreamReader(csvStream, Encoding.UTF8, leaveOpen: true);
        var rows     = new List<BulkGradeRowDto>();
        var lineNum  = 0;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            lineNum++;
            if (lineNum == 1) continue; // skip header
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(',');
            if (parts.Length < 4)
            {
                rows.Add(new BulkGradeRowDto
                {
                    RegistrationNumber = parts.Length > 0 ? parts[0].Trim() : "",
                    ValidationError    = $"Line {lineNum}: expected 4 columns."
                });
                continue;
            }

            var regNum    = parts[0].Trim();
            var name      = parts[1].Trim();
            var marksStr  = parts[2].Trim();
            var maxStr    = parts[3].Trim();

            string? error = null;
            Guid?   profileId = null;

            if (!regNumIndex.TryGetValue(regNum.ToUpperInvariant(), out var studentInfo))
                error = $"Line {lineNum}: registration number '{regNum}' not found in offering.";
            else
                profileId = studentInfo.StudentProfileId;

            decimal? marks = null;
            decimal? max   = null;

            if (error is null)
            {
                if (!decimal.TryParse(marksStr, out var parsedMarks) || parsedMarks < 0)
                    error = $"Line {lineNum}: invalid MarksObtained '{marksStr}'.";
                else
                    marks = parsedMarks;

                if (error is null && (!decimal.TryParse(maxStr, out var parsedMax) || parsedMax <= 0))
                    error = $"Line {lineNum}: invalid MaxMarks '{maxStr}'.";
                else if (error is null)
                    max = decimal.Parse(maxStr);

                if (error is null && marks > max)
                    error = $"Line {lineNum}: MarksObtained ({marks}) exceeds MaxMarks ({max}).";
            }

            rows.Add(new BulkGradeRowDto
            {
                RegistrationNumber = regNum,
                StudentName        = name,
                StudentProfileId   = profileId,
                MarksObtained      = marks,
                MaxMarks           = max,
                ValidationError    = error
            });
        }

        return new BulkGradePreviewResponse
        {
            ComponentName = componentName,
            TotalRows     = rows.Count,
            ValidRows     = rows.Count(r => r.ValidationError is null),
            ErrorRows     = rows.Count(r => r.ValidationError is not null),
            Rows          = rows
        };
    }

    // ── Stage 16.3: Confirm bulk grade ────────────────────────────────────────

    public async Task ConfirmBulkGradeAsync(BulkGradeConfirmRequest request, Guid facultyUserId, CancellationToken ct = default)
    {
        // Final-Touches Phase 16 Stage 16.3 — apply valid rows from confirmed preview
        foreach (var row in request.ValidRows.Where(r => r.StudentProfileId.HasValue && r.MarksObtained.HasValue && r.MaxMarks.HasValue))
        {
            var studentId = row.StudentProfileId!.Value;
            var upsertReq = new UpsertGradebookEntryRequest
            {
                StudentProfileId = studentId,
                CourseOfferingId = request.CourseOfferingId,
                ComponentName    = request.ComponentName,
                MarksObtained    = row.MarksObtained!.Value,
                MaxMarks         = row.MaxMarks!.Value
            };
            await UpsertEntryAsync(upsertReq, facultyUserId, ct);
        }
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
