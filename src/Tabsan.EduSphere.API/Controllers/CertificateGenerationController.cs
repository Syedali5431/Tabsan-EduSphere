using System.Security.Claims;
using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabsan.EduSphere.API.Services.DegreeTranscriptGeneration;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Infrastructure.Persistence;

namespace Tabsan.EduSphere.API.Controllers;

[ApiController]
[Route("api/v1/certificate-generation")]
[Authorize]
public class CertificateGenerationController : ControllerBase
{
    private const string CompletionDocumentType = "completion";
    private const string ReportCardDocumentType = "reportcard";
    private const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const long MaxTemplateSizeBytes = 5 * 1024 * 1024;

    private readonly ApplicationDbContext _db;
    private readonly IAccessScopeResolver _accessScope;
    private readonly IAdminAssignmentRepository _adminAssignments;
    private readonly IFacultyAssignmentRepository _facultyAssignments;
    private readonly IStudentProfileRepository _studentProfiles;
    private readonly IInstitutionPolicyService _institutionPolicy;
    private readonly DocumentGenerationService _documents;
    private readonly TemplateProcessorService _templateProcessor;

    public CertificateGenerationController(
        ApplicationDbContext db,
        IAccessScopeResolver accessScope,
        IAdminAssignmentRepository adminAssignments,
        IFacultyAssignmentRepository facultyAssignments,
        IStudentProfileRepository studentProfiles,
        IInstitutionPolicyService institutionPolicy,
        DocumentGenerationService documents,
        TemplateProcessorService templateProcessor)
    {
        _db = db;
        _accessScope = accessScope;
        _adminAssignments = adminAssignments;
        _facultyAssignments = facultyAssignments;
        _studentProfiles = studentProfiles;
        _institutionPolicy = institutionPolicy;
        _documents = documents;
        _templateProcessor = templateProcessor;
    }

    [HttpGet("graduated-students")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty")]
    public async Task<IActionResult> GetGraduatedStudents(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? campusId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? courseId,
        [FromQuery] Guid? semesterId,
        [FromQuery] int? institutionType,
        CancellationToken ct)
    {
        var policy = await _institutionPolicy.GetPolicyAsync(ct);

        if (tenantId.HasValue != campusId.HasValue)
            return BadRequest(new { message = "TenantId and CampusId must be provided together." });

        var callerInstitutionType = GetInstitutionTypeFromClaims();
        if (!_accessScope.IsSuperAdmin() && callerInstitutionType.HasValue && institutionType.HasValue && callerInstitutionType.Value != institutionType.Value)
            return Forbid();

        var effectiveInstitutionType = institutionType ?? callerInstitutionType ?? (int)InstitutionType.University;
        var isUniversityScope = effectiveInstitutionType == (int)InstitutionType.University;
        if (isUniversityScope && !policy.IncludeUniversity)
            return Forbid();

        var query = _db.StudentProfiles
            .AsNoTracking()
            .Where(s => s.Department.InstitutionType == (InstitutionType)effectiveInstitutionType)
            .Include(s => s.Program)
            .Include(s => s.Department)
            .AsQueryable();

        if (isUniversityScope)
        {
            query = query.Where(s => s.Status == StudentStatus.Graduated);
        }
        else
        {
            query = query.Where(s => s.Status == StudentStatus.Active || s.Status == StudentStatus.Graduated);
        }

        if (_accessScope.IsSuperAdmin())
        {
            if (tenantId.HasValue)
                query = query.Where(s => s.Department.TenantId == tenantId.Value);
            if (campusId.HasValue)
                query = query.Where(s => s.Department.CampusId == campusId.Value);
        }
        else
        {
            var callerTenantId = _accessScope.GetTenantId();
            var callerCampusId = _accessScope.GetCampusId();

            if (callerTenantId.HasValue)
                query = query.Where(s => s.Department.TenantId == callerTenantId.Value);
            if (callerCampusId.HasValue)
                query = query.Where(s => s.Department.CampusId == callerCampusId.Value);

            if (tenantId.HasValue)
                query = query.Where(s => s.Department.TenantId == tenantId.Value);
            if (campusId.HasValue)
                query = query.Where(s => s.Department.CampusId == campusId.Value);

            var callerId = GetCurrentUserId();
            if (callerId == Guid.Empty)
                return Forbid();

            if (User.IsInRole("Admin"))
            {
                var allowedDepartmentIds = await _adminAssignments.GetDepartmentIdsForAdminAsync(callerId, ct);
                query = query.Where(s => allowedDepartmentIds.Contains(s.DepartmentId));
            }
            else if (User.IsInRole("Faculty"))
            {
                var allowedDepartmentIds = await _facultyAssignments.GetDepartmentIdsForFacultyAsync(callerId, ct);
                query = query.Where(s => allowedDepartmentIds.Contains(s.DepartmentId));
            }
        }

        if (departmentId.HasValue)
            query = query.Where(s => s.DepartmentId == departmentId.Value);

        if (courseId.HasValue)
        {
            var matchingStudentIds = await _db.Enrollments
                .AsNoTracking()
                .Where(e => e.CourseOffering.CourseId == courseId.Value)
                .Select(e => e.StudentProfileId)
                .Distinct()
                .ToListAsync(ct);
            query = query.Where(s => matchingStudentIds.Contains(s.Id));
        }

        if (semesterId.HasValue)
        {
            var matchingStudentIds = await _db.Enrollments
                .AsNoTracking()
                .Where(e => e.CourseOffering.SemesterId == semesterId.Value)
                .Select(e => e.StudentProfileId)
                .Distinct()
                .ToListAsync(ct);
            query = query.Where(s => matchingStudentIds.Contains(s.Id));
        }

        if (User.IsInRole("Faculty"))
        {
            var facultyId = GetCurrentUserId();
            if (facultyId == Guid.Empty)
                return Forbid();

            var taughtStudentIds = await _db.Enrollments
                .AsNoTracking()
                .Where(e => e.CourseOffering.FacultyUserId == facultyId)
                .Select(e => e.StudentProfileId)
                .Distinct()
                .ToListAsync(ct);

            query = query.Where(s => taughtStudentIds.Contains(s.Id));
        }

        var students = await query
            .OrderBy(s => s.RegistrationNumber)
            .Select(s => new
            {
                StudentProfileId = s.Id,
                StudentUserId = s.UserId,
                s.RegistrationNumber,
                DepartmentId = s.DepartmentId,
                DepartmentName = s.Department.Name,
                ProgramId = s.ProgramId,
                ProgramName = s.Program.Name,
                TenantId = s.Department.TenantId,
                CampusId = s.Department.CampusId,
                CourseId = courseId,
                s.Cgpa,
                s.GraduatedDate
            })
            .ToListAsync(ct);

        var userIds = students.Select(s => s.StudentUserId).Distinct().ToList();
        var userNames = await _db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Username })
            .ToDictionaryAsync(u => u.Id, u => u.Username, ct);

        var additionalByStudent = new Dictionary<Guid, List<object>>();
        if (!isUniversityScope)
        {
            var customDocs = await ListAdditionalCertificatesAsync(ct);
            additionalByStudent = customDocs
                .GroupBy(d => d.StudentProfileId)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .OrderByDescending(x => x.UploadedAtUtc)
                        .Select(x => (object)new
                        {
                            documentId = x.DocumentId,
                            studentProfileId = x.StudentProfileId,
                            title = x.Title,
                            documentType = x.DocumentType,
                            fileName = x.FileName,
                            contentType = x.ContentType,
                            uploadedAtUtc = x.UploadedAtUtc
                        })
                        .ToList());
        }

        var response = new List<object>(students.Count);
        foreach (var s in students)
        {
            var docs = await _documents.ListByStudentAsync(s.StudentProfileId, ct);
            var latestDegree = docs
                .Where(d => d.Type == AcademicDocumentType.Degree)
                .OrderByDescending(d => d.GeneratedAtUtc)
                .FirstOrDefault();
            var latestTranscript = docs
                .Where(d => d.Type == AcademicDocumentType.Transcript)
                .OrderByDescending(d => d.GeneratedAtUtc)
                .FirstOrDefault();

            response.Add(new
            {
                s.StudentProfileId,
                s.StudentUserId,
                StudentName = userNames.GetValueOrDefault(s.StudentUserId, s.RegistrationNumber),
                s.RegistrationNumber,
                s.DepartmentId,
                s.DepartmentName,
                s.ProgramId,
                s.ProgramName,
                s.TenantId,
                s.CampusId,
                s.CourseId,
                s.Cgpa,
                s.GraduatedDate,
                LatestDegreeDocumentId = latestDegree?.DocumentId,
                LatestDegreeGeneratedAtUtc = latestDegree?.GeneratedAtUtc,
                LatestTranscriptDocumentId = latestTranscript?.DocumentId,
                LatestTranscriptGeneratedAtUtc = latestTranscript?.GeneratedAtUtc,
                AdditionalCertificates = additionalByStudent.TryGetValue(s.StudentProfileId, out var additionalDocs)
                    ? additionalDocs
                    : new List<object>()
            });
        }

        return Ok(response);
    }

    [HttpPost("students/{studentProfileId:guid}/degree")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GenerateDegreeCertificate(Guid studentProfileId, CancellationToken ct)
    {
        var student = await GetStudentInScopeAsync(studentProfileId, ct);
        if (student is null)
            return Forbid();

        // Only University students can get a degree
        if (student.Department.InstitutionType != InstitutionType.University)
            return BadRequest(new { message = "Degree certificates are only available for university students. School/College students should use Completion Certificate." });

        // Check if student has enough published results to qualify
        var publishedResultCount = await _db.Results
            .AsNoTracking()
            .CountAsync(r => r.StudentProfileId == studentProfileId && r.IsPublished, ct);

        if (publishedResultCount == 0)
            return BadRequest(new { message = "No published results found. Degree requires completed course results." });

        var isUniversity = student.Department.InstitutionType == InstitutionType.University;
        var cgpaValue = isUniversity ? student.Cgpa.ToString("0.00") : "";

        var request = new DegreeGenerationRequest(
            StudentId: student.Id,
            StudentName: await ResolveStudentNameAsync(student.UserId, student.RegistrationNumber, ct),
            FatherName: await ResolveFatherNameAsync(student.UserId, ct),
            DegreeTitle: $"{student.Program?.Name ?? "Degree"}",
            Cgpa: cgpaValue,
            RegistrationNumber: student.RegistrationNumber,
            DepartmentName: student.Department.Name,
            ProgramName: student.Program?.Name,
            FinalGpa: cgpaValue,
            InstitutionType: (int)student.Department.InstitutionType);

        var doc = await _documents.GenerateDegreeAsync(request, ct);
        return Ok(doc);
    }

    [HttpPost("students/{studentProfileId:guid}/transcript")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GenerateTranscriptCertificate(Guid studentProfileId, [FromQuery] Guid? semesterId, CancellationToken ct)
    {
        var student = await GetStudentInScopeAsync(studentProfileId, ct);
        if (student is null)
            return Forbid();

        // For transcript, include ALL published results (all completed semesters).
        var transcriptContext = await BuildTranscriptContextAsync(student, ct);
        if (transcriptContext.Rows.Count == 0)
            return BadRequest(new { message = "No published results found for this student." });

        var studentName = await ResolveStudentNameAsync(student.UserId, student.RegistrationNumber, ct);
        var request = new TranscriptGenerationRequest(
            StudentId: student.Id,
            StudentName: studentName,
            FatherName: await ResolveFatherNameAsync(student.UserId, ct),
            DegreeTitle: $"{student.Program?.Name ?? "Transcript"}",
            Cgpa: transcriptContext.FinalCgpa,
            Courses: transcriptContext.Rows,
            RegistrationNumber: student.RegistrationNumber,
            DepartmentName: student.Department.Name,
            ProgramName: student.Program?.Name,
            ClassName: "All Semesters",
            FinalGpa: transcriptContext.FinalCgpa,
            SemesterGpaSummary: transcriptContext.SemesterGpaSummary,
            InstitutionType: (int)student.Department.InstitutionType);

        var doc = await _documents.GenerateTranscriptAsync(request, ct);
        return Ok(doc);
    }

    [HttpGet("students/{studentProfileId:guid}/documents")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty,Student")]
    public async Task<IActionResult> GetStudentDocuments(Guid studentProfileId, CancellationToken ct)
    {
        var hasScope = await HasStudentReadScopeAsync(studentProfileId, ct);
        if (!hasScope)
            return Forbid();

        var docs = await _documents.ListByStudentAsync(studentProfileId, ct);
        return Ok(docs.OrderByDescending(d => d.GeneratedAtUtc));
    }

    [HttpGet("documents/{documentId:guid}/download")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty,Student")]
    public async Task<IActionResult> Download(Guid documentId, [FromQuery] string format = "docx", CancellationToken ct = default)
    {
        var doc = await _documents.GetAsync(documentId, ct);
        if (doc is null)
            return NotFound();

        var hasScope = await HasStudentReadScopeAsync(doc.StudentId, ct);
        if (!hasScope)
            return Forbid();

        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(doc.PdfPath)
            && System.IO.File.Exists(doc.PdfPath))
        {
            var pdfBytes = await System.IO.File.ReadAllBytesAsync(doc.PdfPath, ct);
            return File(pdfBytes, "application/pdf", Path.GetFileName(doc.PdfPath));
        }

        if (!System.IO.File.Exists(doc.DocxPath))
            return NotFound();

        var docxBytes = await System.IO.File.ReadAllBytesAsync(doc.DocxPath, ct);
        return File(
            docxBytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            Path.GetFileName(doc.DocxPath));
    }

    [HttpGet("students/{studentProfileId:guid}/additional-certificates")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty,Student")]
    public async Task<IActionResult> GetAdditionalCertificates(Guid studentProfileId, CancellationToken ct)
    {
        var hasScope = await HasAdditionalCertificateReadScopeAsync(studentProfileId, ct);
        if (!hasScope)
            return Forbid();

        var docs = await ListAdditionalCertificatesAsync(ct);
        var result = docs
            .Where(d => d.StudentProfileId == studentProfileId)
            .OrderByDescending(d => d.UploadedAtUtc)
            .Select(d => new
            {
                d.DocumentId,
                d.StudentProfileId,
                d.Title,
                d.DocumentType,
                d.FileName,
                d.ContentType,
                d.UploadedAtUtc
            })
            .ToList();

        return Ok(result);
    }

    [HttpPost("students/{studentProfileId:guid}/additional-certificates/upload")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> UploadAdditionalCertificate(Guid studentProfileId, [FromQuery] string? title, IFormFile? file, CancellationToken ct)
    {
        var student = await GetNonUniversityStudentForAdminManagementAsync(studentProfileId, ct);
        if (student is null)
            return Forbid();

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Certificate file is required." });

        var safeTitle = string.IsNullOrWhiteSpace(title) ? "Certificate" : title.Trim();
        var root = GetAdditionalCertificateStorageRoot();
        var studentFolder = Path.Combine(root, studentProfileId.ToString("N"));
        Directory.CreateDirectory(studentFolder);

        var originalFileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(originalFileName);
        var generatedName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(studentFolder, generatedName);

        await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(fs, ct);
        }

        var index = await ListAdditionalCertificatesAsync(ct);
        var currentUser = GetCurrentUserId();
        index.Add(new AdditionalCertificateIndexEntry
        {
            DocumentId = Guid.NewGuid(),
            StudentProfileId = studentProfileId,
            Title = safeTitle,
            DocumentType = null,
            FileName = originalFileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            FilePath = fullPath,
            UploadedAtUtc = DateTime.UtcNow,
            UploadedByUserId = currentUser == Guid.Empty ? null : currentUser
        });
        await SaveAdditionalCertificatesAsync(index, ct);

        return Ok(new { message = "Certificate uploaded successfully." });
    }

    [HttpGet("templates/{templateType}/default")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public IActionResult DownloadDefaultAdditionalTemplate(string templateType)
    {
        var normalizedType = NormalizeAdditionalDocumentType(templateType);
        if (normalizedType is null)
            return BadRequest(new { message = "Unsupported template type. Use completion or reportcard." });

        var bytes = BuildAdditionalTemplateDocument(normalizedType);
        var fileName = normalizedType == CompletionDocumentType
            ? "completion-certificate-template.docx"
            : "report-card-template.docx";

        return File(bytes, DocxContentType, fileName);
    }

    [HttpPost("templates/{templateType}/upload")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadAdditionalTemplate(string templateType, [FromForm] IFormFile? file, CancellationToken ct)
    {
        var normalizedType = NormalizeAdditionalDocumentType(templateType);
        if (normalizedType is null)
            return BadRequest(new { message = "Unsupported template type. Use completion or reportcard." });

        var validationError = await ValidateDocxTemplateAsync(file);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        var templateRoot = GetAdditionalTemplateStorageRoot();
        var templatePath = Path.Combine(templateRoot, $"{normalizedType}.docx");

        await using (var stream = file!.OpenReadStream())
        await using (var output = new FileStream(templatePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(output, ct);
        }

        return Ok(new
        {
            message = normalizedType == CompletionDocumentType
                ? "Completion certificate template uploaded successfully."
                : "Report card template uploaded successfully."
        });
    }

    [HttpPost("students/{studentProfileId:guid}/additional-certificates/generate")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GenerateAdditionalCertificate(
        Guid studentProfileId,
        [FromQuery] string? documentType,
        [FromQuery] Guid? semesterId,
        CancellationToken ct)
    {
        var normalizedType = NormalizeAdditionalDocumentType(documentType);
        if (normalizedType is null)
            return BadRequest(new { message = "Unsupported document type. Use completion or reportcard." });

        var student = await GetNonUniversityStudentForAdminManagementAsync(studentProfileId, ct);
        if (student is null)
            return Forbid();

        if (normalizedType == CompletionDocumentType && !IsCompletionEligible(student))
            return BadRequest(new { message = "Completion certificate can be generated only after completing all classes: Class 10 (School) or Class 11 & 12 (College)." });

        var reportRows = await BuildNonUniversityReportRowsAsync(student.Id, semesterId, ct);
        if (reportRows.Count == 0)
            return BadRequest(new { message = "No published result rows found for the selected student/class." });

        var studentName = await ResolveStudentNameAsync(student.UserId, student.RegistrationNumber, ct);
        var fatherName = await ResolveFatherNameAsync(student.UserId, ct);

        var semesterNames = reportRows.Select(r => r.SemesterName).Distinct().ToList();
        var className = await ResolveClassNameAsync(semesterNames, semesterId, ct);

        // For completion certificates, use only the final class results for percentage calculation.
        IReadOnlyCollection<TranscriptResultProjection> completionRows = reportRows;
        if (normalizedType == CompletionDocumentType)
        {
            className = student.Department.InstitutionType switch
            {
                InstitutionType.School => $"Class {student.CurrentSemesterNumber} (2026)",
                InstitutionType.College => $"Class {student.CurrentSemesterNumber + 10} (2026)",
                _ => className
            };

            // Filter to only the final class results for the completion percentage.
            var finalClassLabel = student.Department.InstitutionType switch
            {
                InstitutionType.School => "Class 10",
                InstitutionType.College => "Class 12",
                _ => ""
            };
            completionRows = reportRows
                .Where(r => r.SemesterName.StartsWith(finalClassLabel, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (completionRows.Count == 0)
                completionRows = reportRows; // fallback if no final class results found
        }

        var resultSummary = BuildResultSummary(completionRows);

        // For marks sheets with multiple classes, show the range.
        if (normalizedType != CompletionDocumentType && semesterNames.Count > 1)
        {
            var sorted = semesterNames.OrderBy(n => n).ToList();
            className = $"{sorted.First()} to {sorted.Last()}";
        }

        var templateBytes = await GetAdditionalTemplateBytesAsync(normalizedType, ct);
        var institutionType = (int)student.Department.InstitutionType;
        var isUniversity = institutionType == 0;
        var payload = new DocumentTemplatePayload(
            StudentName: studentName,
            FatherName: fatherName,
            RegistrationNumber: student.RegistrationNumber,
            DepartmentName: student.Department.Name,
            ProgramName: student.Program?.Name ?? "",
            ClassName: className,
            DegreeTitle: student.Program?.Name ?? "",
            Cgpa: isUniversity ? student.Cgpa.ToString("0.00") : "",
            FinalPercentage: !isUniversity ? resultSummary.FinalPercentage : "",
            FinalGpa: isUniversity ? student.Cgpa.ToString("0.00") : "",
            SemesterGpaSummary: resultSummary.SemesterSummary,
            IssueDate: DateTime.UtcNow.ToString("yyyy-MM-dd"),
            SerialNumber: $"{(normalizedType == CompletionDocumentType ? "CMP" : "RPT")}-{DateTime.UtcNow:yyyyMMddHHmmss}",
            VerificationUrl: "N/A",
            InstitutionType: institutionType);

        // For completion certificates, no marks table — only student info, percentage, and class.
        // For marks sheets, include all results across all completed classes.
        var reportTableRows = normalizedType == CompletionDocumentType
            ? new List<TranscriptCourseRow>()
            : reportRows
                .OrderBy(r => r.SemesterName)
                .ThenBy(r => r.CourseCode)
                .Select((r, index) => new TranscriptCourseRow(
                    SerialNumber: (index + 1).ToString(),
                    CourseName: string.IsNullOrWhiteSpace(r.CourseCode) ? r.CourseTitle : $"{r.CourseCode} - {r.CourseTitle}",
                    CreditHours: 0,
                    ObtainedMarks: r.MarksObtained.ToString("0.##"),
                    TotalMarks: r.MaxMarks.ToString("0.##"),
                    SgpaOrMarks: $"{r.Percentage:0.##}%",
                    SemesterName: r.SemesterName))
                .ToList();

        var generatedBytes = _templateProcessor.PopulateTemplate(templateBytes, payload, reportTableRows);

        var root = GetAdditionalCertificateStorageRoot();
        var studentFolder = Path.Combine(root, studentProfileId.ToString("N"));
        Directory.CreateDirectory(studentFolder);

        var fileName = normalizedType == CompletionDocumentType
            ? $"completion-certificate-{DateTime.UtcNow:yyyyMMddHHmmss}.docx"
            : $"report-card-{DateTime.UtcNow:yyyyMMddHHmmss}.docx";
        var fullPath = Path.Combine(studentFolder, fileName);

        await System.IO.File.WriteAllBytesAsync(fullPath, generatedBytes, ct);

        var index = await ListAdditionalCertificatesAsync(ct);
        var currentUser = GetCurrentUserId();
        index.Add(new AdditionalCertificateIndexEntry
        {
            DocumentId = Guid.NewGuid(),
            StudentProfileId = studentProfileId,
            Title = normalizedType == CompletionDocumentType ? "Completion Certificate" : "Report Card",
            DocumentType = normalizedType,
            FileName = fileName,
            ContentType = DocxContentType,
            FilePath = fullPath,
            UploadedAtUtc = DateTime.UtcNow,
            UploadedByUserId = currentUser == Guid.Empty ? null : currentUser
        });
        await SaveAdditionalCertificatesAsync(index, ct);

        return Ok(new
        {
            message = normalizedType == CompletionDocumentType
                ? "Completion certificate generated successfully."
                : "Report card generated successfully."
        });
    }

    [HttpGet("documents/custom/{documentId:guid}/download")]
    [Authorize(Roles = "SuperAdmin,Admin,Faculty,Student")]
    public async Task<IActionResult> DownloadAdditionalCertificate(Guid documentId, CancellationToken ct)
    {
        var index = await ListAdditionalCertificatesAsync(ct);
        var doc = index.FirstOrDefault(d => d.DocumentId == documentId);
        if (doc is null)
            return NotFound();

        var hasScope = await HasAdditionalCertificateReadScopeAsync(doc.StudentProfileId, ct);
        if (!hasScope)
            return Forbid();

        if (!System.IO.File.Exists(doc.FilePath))
            return NotFound();

        var bytes = await System.IO.File.ReadAllBytesAsync(doc.FilePath, ct);
        return File(bytes, string.IsNullOrWhiteSpace(doc.ContentType) ? "application/octet-stream" : doc.ContentType, doc.FileName);
    }

    private async Task<StudentProfile?> GetStudentInScopeAsync(Guid studentProfileId, CancellationToken ct)
    {
        // SuperAdmin bypasses all scope/policy checks.
        if (_accessScope.IsSuperAdmin())
            return await _db.StudentProfiles
                .Include(s => s.Department)
                .Include(s => s.Program)
                .FirstOrDefaultAsync(s => s.Id == studentProfileId, ct);

        var policy = await _institutionPolicy.GetPolicyAsync(ct);
        if (!policy.IncludeUniversity)
            return null;

        var student = await _db.StudentProfiles
            .Include(s => s.Department)
            .Include(s => s.Program)
            .FirstOrDefaultAsync(s => s.Id == studentProfileId && s.Status == StudentStatus.Graduated, ct);

        if (student is null || student.Department.InstitutionType != InstitutionType.University)
            return null;

        var callerId = GetCurrentUserId();
        if (callerId == Guid.Empty)
            return null;

        var callerTenantId = _accessScope.GetTenantId();
        var callerCampusId = _accessScope.GetCampusId();

        if (callerTenantId.HasValue && student.Department.TenantId != callerTenantId.Value)
            return null;
        if (callerCampusId.HasValue && student.Department.CampusId != callerCampusId.Value)
            return null;

        if (User.IsInRole("Admin"))
        {
            var allowedDepartmentIds = await _adminAssignments.GetDepartmentIdsForAdminAsync(callerId, ct);
            if (!allowedDepartmentIds.Contains(student.DepartmentId))
                return null;

            return student;
        }

        return null;
    }

    private async Task<bool> HasStudentReadScopeAsync(Guid studentProfileId, CancellationToken ct)
    {
        var student = await _db.StudentProfiles
            .AsNoTracking()
            .Include(s => s.Department)
            .FirstOrDefaultAsync(s => s.Id == studentProfileId, ct);

        if (student is null || student.Department.InstitutionType != InstitutionType.University)
            return false;

        var policy = await _institutionPolicy.GetPolicyAsync(ct);
        if (!policy.IncludeUniversity)
            return false;

        if (_accessScope.IsSuperAdmin())
            return true;

        var callerTenantId = _accessScope.GetTenantId();
        var callerCampusId = _accessScope.GetCampusId();
        if (callerTenantId.HasValue && student.Department.TenantId != callerTenantId.Value)
            return false;
        if (callerCampusId.HasValue && student.Department.CampusId != callerCampusId.Value)
            return false;

        var callerId = GetCurrentUserId();
        if (callerId == Guid.Empty)
            return false;

        if (User.IsInRole("Admin"))
        {
            var allowedDepartmentIds = await _adminAssignments.GetDepartmentIdsForAdminAsync(callerId, ct);
            return allowedDepartmentIds.Contains(student.DepartmentId);
        }

        if (User.IsInRole("Faculty"))
        {
            var allowedDepartmentIds = await _facultyAssignments.GetDepartmentIdsForFacultyAsync(callerId, ct);
            if (!allowedDepartmentIds.Contains(student.DepartmentId))
                return false;

            var taughtStudentIds = await _db.Enrollments
                .AsNoTracking()
                .Where(e => e.CourseOffering.FacultyUserId == callerId)
                .Select(e => e.StudentProfileId)
                .Distinct()
                .ToListAsync(ct);
            return taughtStudentIds.Contains(studentProfileId);
        }

        if (User.IsInRole("Student"))
        {
            var profile = await _studentProfiles.GetByUserIdAsync(callerId, ct);
            return profile is not null && profile.Id == studentProfileId;
        }

        return false;
    }

    private async Task<string> ResolveStudentNameAsync(Guid userId, string fallback, CancellationToken ct)
    {
        var fullName = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(ct);

        return string.IsNullOrWhiteSpace(fullName) ? fallback : fullName;
    }

    private async Task<string> ResolveFatherNameAsync(Guid userId, CancellationToken ct)
    {
        var fatherName = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.FatherName)
            .FirstOrDefaultAsync(ct);

        return string.IsNullOrWhiteSpace(fatherName) ? "-" : fatherName;
    }

    private async Task<TranscriptBuildContext> BuildTranscriptContextAsync(StudentProfile student, CancellationToken ct)
    {
        // Include ALL published results for this student — transcript covers all completed semesters.
        var publishedRows = await (
            from result in _db.Results.AsNoTracking()
            where result.StudentProfileId == student.Id && result.IsPublished
            join offering in _db.CourseOfferings.AsNoTracking() on result.CourseOfferingId equals offering.Id
            join course in _db.Courses.AsNoTracking() on offering.CourseId equals course.Id
            join semester in _db.Semesters.AsNoTracking() on offering.SemesterId equals semester.Id
            select new TranscriptResultProjection(
                offering.Id,
                offering.SemesterId,
                semester.Name,
                course.Code,
                course.Title,
                course.CreditHours,
                result.ResultType,
                result.MarksObtained,
                result.MaxMarks,
                result.GradePoint))
            .ToListAsync(ct);

        if (publishedRows.Count == 0)
            return new TranscriptBuildContext(new List<TranscriptCourseRow>(), student.Cgpa.ToString("0.00"), "");

        var byCourse = publishedRows
            .GroupBy(x => new { x.CourseOfferingId, x.SemesterId, x.SemesterName, x.CourseCode, x.CourseTitle, x.CreditHours })
            .OrderBy(g => g.Key.SemesterName)
            .ThenBy(g => g.Key.CourseCode)
            .ToList();

        var tableRows = new List<TranscriptCourseRow>(byCourse.Count);
        var semesterBuckets = new Dictionary<Guid, List<decimal>>();

        for (var index = 0; index < byCourse.Count; index++)
        {
            var g = byCourse[index];
            var totalRow = g.FirstOrDefault(r => string.Equals(r.ResultType, "Total", StringComparison.OrdinalIgnoreCase));

            var obtained = totalRow is null ? g.Sum(x => x.MarksObtained) : totalRow.MarksObtained;
            var max = totalRow is null ? g.Sum(x => x.MaxMarks) : totalRow.MaxMarks;
            var gradePoint = totalRow?.GradePoint
                ?? g.Select(x => x.GradePoint).Where(x => x.HasValue).Select(x => x!.Value).DefaultIfEmpty(max > 0 ? (obtained / max) * 4m : 0m).Max();

            if (!semesterBuckets.TryGetValue(g.Key.SemesterId, out var semesterValues))
            {
                semesterValues = new List<decimal>();
                semesterBuckets[g.Key.SemesterId] = semesterValues;
            }
            semesterValues.Add(gradePoint);

            tableRows.Add(new TranscriptCourseRow(
                SerialNumber: (index + 1).ToString(),
                CourseName: string.IsNullOrWhiteSpace(g.Key.CourseCode)
                    ? g.Key.CourseTitle
                    : $"{g.Key.CourseCode} - {g.Key.CourseTitle}",
                CreditHours: g.Key.CreditHours,
                ObtainedMarks: obtained.ToString("0.##"),
                TotalMarks: max.ToString("0.##"),
                SgpaOrMarks: gradePoint.ToString("0.00"),
                SemesterName: g.Key.SemesterName));
        }

        var semesterLabels = publishedRows
            .GroupBy(x => x.SemesterId)
            .ToDictionary(g => g.Key, g => g.First().SemesterName);

        var cumulative = new List<decimal>();
        var summaryLines = new List<string>();
        foreach (var semester in semesterBuckets.Keys.OrderBy(k => semesterLabels.GetValueOrDefault(k, string.Empty)))
        {
            var sgpa = semesterBuckets[semester].Count == 0 ? 0m : semesterBuckets[semester].Average();
            cumulative.Add(sgpa);
            var cgpa = cumulative.Average();
            summaryLines.Add($"{semesterLabels.GetValueOrDefault(semester, "Semester")}: GPA {sgpa:0.00}, CGPA {cgpa:0.00}");
        }

        var finalCgpa = cumulative.Count == 0 ? student.Cgpa : cumulative.Average();
        summaryLines.Add($"Final GPA: {(cumulative.Count == 0 ? student.CurrentSemesterGpa : cumulative.Last()):0.00}");

        return new TranscriptBuildContext(
            tableRows,
            finalCgpa.ToString("0.00"),
            string.Join(" | ", summaryLines));
    }

    private async Task<List<TranscriptResultProjection>> BuildNonUniversityReportRowsAsync(Guid studentProfileId, Guid? semesterId, CancellationToken ct)
    {
        // For marks sheets / report cards, include ALL published results across all completed classes.
        return await (
            from result in _db.Results.AsNoTracking()
            where result.StudentProfileId == studentProfileId && result.IsPublished
            join offering in _db.CourseOfferings.AsNoTracking() on result.CourseOfferingId equals offering.Id
            join course in _db.Courses.AsNoTracking() on offering.CourseId equals course.Id
            join semester in _db.Semesters.AsNoTracking() on offering.SemesterId equals semester.Id
            where !semesterId.HasValue || offering.SemesterId == semesterId.Value
            select new TranscriptResultProjection(
                offering.Id,
                offering.SemesterId,
                semester.Name,
                course.Code,
                course.Title,
                course.CreditHours,
                result.ResultType,
                result.MarksObtained,
                result.MaxMarks,
                result.GradePoint))
            .ToListAsync(ct);
    }

    private bool IsCompletionEligible(StudentProfile student)
    {
        if (student.Status == StudentStatus.Graduated)
            return true;

        return student.Department.InstitutionType switch
        {
            InstitutionType.School => student.CurrentSemesterNumber >= 10,
            InstitutionType.College => HasCompletedBothClassesAsync(student.Id).GetAwaiter().GetResult(),
            _ => false
        };
    }

    private async Task<bool> HasCompletedBothClassesAsync(Guid studentProfileId)
    {
        // College requires published results in BOTH Class 11 and Class 12
        var classSemesters = await _db.Results
            .AsNoTracking()
            .Where(r => r.StudentProfileId == studentProfileId && r.IsPublished)
            .Join(_db.CourseOfferings, r => r.CourseOfferingId, co => co.Id, (r, co) => new { r, co.SemesterId })
            .Join(_db.Semesters, x => x.SemesterId, s => s.Id, (x, s) => s.Name)
            .Distinct()
            .ToListAsync();

        var hasClass11 = classSemesters.Any(n => n.Contains("Class 11", StringComparison.OrdinalIgnoreCase));
        var hasClass12 = classSemesters.Any(n => n.Contains("Class 12", StringComparison.OrdinalIgnoreCase));

        return hasClass11 && hasClass12;
    }

    private static ResultSummary BuildResultSummary(IReadOnlyCollection<TranscriptResultProjection> rows)
    {
        if (rows.Count == 0)
            return new ResultSummary("0.00", "");

        var grouped = rows
            .GroupBy(r => r.SemesterName)
            .OrderBy(g => g.Key)
            .ToList();

        var summaryLines = new List<string>(grouped.Count);
        decimal totalObtained = 0;
        decimal totalMax = 0;

        foreach (var group in grouped)
        {
            var obtained = group.Sum(x => x.MarksObtained);
            var max = group.Sum(x => x.MaxMarks);
            totalObtained += obtained;
            totalMax += max;
            var pct = max <= 0 ? 0m : (obtained / max) * 100m;
            summaryLines.Add($"{group.Key}: {pct:0.##}%");
        }

        var finalPct = totalMax <= 0 ? 0m : (totalObtained / totalMax) * 100m;
        return new ResultSummary(finalPct.ToString("0.##"), string.Join(" | ", summaryLines));
    }

    private async Task<string> ResolveClassNameAsync(IReadOnlyCollection<string> semesterNames, Guid? semesterId, CancellationToken ct)
    {
        if (semesterId.HasValue)
        {
            var selected = await _db.Semesters.AsNoTracking().Where(s => s.Id == semesterId.Value).Select(s => s.Name).FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(selected))
                return selected;
        }

        if (semesterNames.Count == 0)
            return "N/A";

        return semesterNames.Count == 1 ? semesterNames.First() : "Multiple Classes";
    }

    private async Task<StudentProfile?> GetNonUniversityStudentForAdminManagementAsync(Guid studentProfileId, CancellationToken ct)
    {
        // SuperAdmin bypasses all scope checks.
        if (_accessScope.IsSuperAdmin())
            return await _db.StudentProfiles
                .Include(s => s.Department)
                .Include(s => s.Program)
                .FirstOrDefaultAsync(s => s.Id == studentProfileId, ct);

        var student = await _db.StudentProfiles
            .Include(s => s.Department)
            .Include(s => s.Program)
            .FirstOrDefaultAsync(s => s.Id == studentProfileId, ct);

        if (student is null || student.Department.InstitutionType == InstitutionType.University)
            return null;

        var callerId = GetCurrentUserId();
        if (callerId == Guid.Empty)
            return null;

        var callerTenantId = _accessScope.GetTenantId();
        var callerCampusId = _accessScope.GetCampusId();

        if (callerTenantId.HasValue && student.Department.TenantId != callerTenantId.Value)
            return null;
        if (callerCampusId.HasValue && student.Department.CampusId != callerCampusId.Value)
            return null;

        if (!User.IsInRole("Admin"))
            return null;

        var allowedDepartmentIds = await _adminAssignments.GetDepartmentIdsForAdminAsync(callerId, ct);
        return allowedDepartmentIds.Contains(student.DepartmentId) ? student : null;
    }

    private async Task<bool> HasAdditionalCertificateReadScopeAsync(Guid studentProfileId, CancellationToken ct)
    {
        var student = await _db.StudentProfiles
            .AsNoTracking()
            .Include(s => s.Department)
            .FirstOrDefaultAsync(s => s.Id == studentProfileId, ct);

        if (student is null || student.Department.InstitutionType == InstitutionType.University)
            return false;

        if (_accessScope.IsSuperAdmin())
            return true;

        var callerTenantId = _accessScope.GetTenantId();
        var callerCampusId = _accessScope.GetCampusId();
        if (callerTenantId.HasValue && student.Department.TenantId != callerTenantId.Value)
            return false;
        if (callerCampusId.HasValue && student.Department.CampusId != callerCampusId.Value)
            return false;

        var callerId = GetCurrentUserId();
        if (callerId == Guid.Empty)
            return false;

        if (User.IsInRole("Admin"))
        {
            var allowedDepartmentIds = await _adminAssignments.GetDepartmentIdsForAdminAsync(callerId, ct);
            return allowedDepartmentIds.Contains(student.DepartmentId);
        }

        if (User.IsInRole("Faculty"))
        {
            var allowedDepartmentIds = await _facultyAssignments.GetDepartmentIdsForFacultyAsync(callerId, ct);
            if (!allowedDepartmentIds.Contains(student.DepartmentId))
                return false;

            var taughtStudentIds = await _db.Enrollments
                .AsNoTracking()
                .Where(e => e.CourseOffering.FacultyUserId == callerId)
                .Select(e => e.StudentProfileId)
                .Distinct()
                .ToListAsync(ct);
            return taughtStudentIds.Contains(studentProfileId);
        }

        if (User.IsInRole("Student"))
        {
            var profile = await _studentProfiles.GetByUserIdAsync(callerId, ct);
            return profile is not null && profile.Id == studentProfileId;
        }

        return false;
    }

    private static string GetAdditionalCertificateStorageRoot()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "Artifacts", "Certificate-Uploads");
        Directory.CreateDirectory(root);
        return root;
    }

    private static string GetAdditionalCertificateIndexPath()
        => Path.Combine(GetAdditionalCertificateStorageRoot(), "index.json");

    private static string GetAdditionalTemplateStorageRoot()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "Artifacts", "Certificate-Templates");
        Directory.CreateDirectory(root);
        return root;
    }

    private static async Task<byte[]> GetAdditionalTemplateBytesAsync(string normalizedType, CancellationToken ct)
    {
        var path = Path.Combine(GetAdditionalTemplateStorageRoot(), $"{normalizedType}.docx");
        if (System.IO.File.Exists(path))
            return await System.IO.File.ReadAllBytesAsync(path, ct);

        return BuildAdditionalTemplateDocument(normalizedType);
    }

    private static string? NormalizeAdditionalDocumentType(string? templateType)
    {
        if (string.Equals(templateType, CompletionDocumentType, StringComparison.OrdinalIgnoreCase)
            || string.Equals(templateType, "completion-certificate", StringComparison.OrdinalIgnoreCase))
            return CompletionDocumentType;

        if (string.Equals(templateType, ReportCardDocumentType, StringComparison.OrdinalIgnoreCase)
            || string.Equals(templateType, "report-card", StringComparison.OrdinalIgnoreCase)
            || string.Equals(templateType, "report_card", StringComparison.OrdinalIgnoreCase))
            return ReportCardDocumentType;

        return null;
    }

    private static async Task<string?> ValidateDocxTemplateAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return "No file uploaded.";

        if (file.Length > MaxTemplateSizeBytes)
            return "File exceeds the maximum allowed size of 5 MB.";

        var ext = Path.GetExtension(file.FileName);
        if (!string.Equals(ext, ".docx", StringComparison.OrdinalIgnoreCase))
            return "Template file must be a .docx file.";

        if (!string.Equals(file.ContentType, DocxContentType, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(file.ContentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
            return "MIME type must be application/vnd.openxmlformats-officedocument.wordprocessingml.document.";

        await using var stream = file.OpenReadStream();
        var header = new byte[4];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
        if (read < 4 || header[0] != 0x50 || header[1] != 0x4B || header[2] != 0x03 || header[3] != 0x04)
            return "File content does not match the expected .docx format.";

        return null;
    }

    private static byte[] BuildAdditionalTemplateDocument(string normalizedType)
    {
        // Brand colors
        const string NavyBlue = "1A3A5C";
        const string Gold = "C9A84C";
        const string White = "FFFFFF";
        const string LightGray = "F5F6FA";
        const string DarkText = "1A1A1A";
        const string MutedText = "666666";

        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            // Page setup
            body.Append(new SectionProperties(
                new PageMargin { Top = 720, Right = 720, Bottom = 720, Left = 720, Header = 360, Footer = 360 },
                new PageBorders(
                    new TopBorder { Val = BorderValues.Double, Size = 24, Color = NavyBlue, Space = 12 },
                    new BottomBorder { Val = BorderValues.Double, Size = 24, Color = NavyBlue, Space = 12 },
                    new LeftBorder { Val = BorderValues.Double, Size = 24, Color = NavyBlue, Space = 12 },
                    new RightBorder { Val = BorderValues.Double, Size = 24, Color = NavyBlue, Space = 12 })));

            if (normalizedType == CompletionDocumentType)
            {
                BuildCompletionTemplate(body, NavyBlue, Gold, White, DarkText, MutedText);
            }
            else
            {
                BuildReportCardTemplate(body, NavyBlue, Gold, White, LightGray, DarkText, MutedText);
            }

            // Footer
            var footerPart = mainPart.AddNewPart<FooterPart>();
            footerPart.Footer = new Footer();
            footerPart.Footer.Append(new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                new Run(
                    new RunProperties(new RunFonts { Ascii = "Calibri" }, new FontSize { Val = "16" }, new Color { Val = MutedText }),
                    new Text("Tabsan EduSphere — Official Document  |  Generated on {{IssueDate}}  |  Electronically generated") { Space = SpaceProcessingModeValues.Preserve })));
            footerPart.Footer.Save();

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }
        return stream.ToArray();
    }

    private static void BuildCompletionTemplate(Body body, string navy, string gold, string white, string dark, string muted)
    {
        // Header
        AddCenteredPara(body, "★ T A B S A N   E D U S P H E R E ★", "Georgia", "14", muted, spacingAfter: "60");
        AddCenteredPara(body, "{{DepartmentName}}", "Calibri", "32", navy, bold: true, spacingAfter: "200");

        // Badge
        var badgePara = new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }, new SpacingBetweenLines { After = "200" }),
            new Run(new RunProperties(new RunFonts { Ascii = "Calibri" }, new Bold(), new FontSize { Val = "22" }, new Color { Val = white }, new Shading { Val = ShadingPatternValues.Clear, Fill = gold }),
                new Text("CERTIFICATE OF COMPLETION") { Space = SpaceProcessingModeValues.Preserve }));
        body.Append(badgePara);
        AddCenteredPara(body, "", "Calibri", "12", white, spacingAfter: "100");

        // Title
        AddCenteredPara(body, "Completion Certificate", "Georgia", "48", navy, bold: true, spacingAfter: "60");
        AddCenteredPara(body, "This is proudly presented to", "Georgia", "24", muted, italic: true, spacingAfter: "200");

        // Student Name
        AddCenteredPara(body, "{{StudentName}}", "Georgia", "44", "8B0000", bold: true, spacingAfter: "60");
        AddCenteredPara(body, "Son / Daughter of {{FatherName}}", "Calibri", "20", muted, spacingAfter: "300");

        // Details
        AddCenteredPara(body, "for successfully completing", "Calibri", "22", dark, spacingAfter: "60");
        AddCenteredPara(body, "{{ProgramName}}", "Georgia", "28", navy, bold: true, spacingAfter: "60");
        AddCenteredPara(body, "{{ClassName}}", "Calibri", "20", dark, spacingAfter: "300");

        // Info rows
        AddInfoRow(body, "Registration No", "{{RegistrationNumber}}", navy, muted);
        AddInfoRow(body, "Final Percentage", "{{FinalPercentage}}%", navy, muted);
        AddCenteredPara(body, "", "Calibri", "12", white, spacingAfter: "200");

        // Date & Serial
        AddCenteredPara(body, "Issue Date: {{IssueDate}}", "Calibri", "20", muted, spacingAfter: "60");
        AddCenteredPara(body, "Certificate No: {{SerialNumber}}", "Calibri", "20", muted, spacingAfter: "300");

        // Signature block
        AddCenteredPara(body, "___________________________________", "Calibri", "20", gold, spacingAfter: "200");
        var sigTable = new Table(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableBorders(new InsideHorizontalBorder { Val = BorderValues.None }, new InsideVerticalBorder { Val = BorderValues.None },
                new TopBorder { Val = BorderValues.None }, new BottomBorder { Val = BorderValues.None },
                new LeftBorder { Val = BorderValues.None }, new RightBorder { Val = BorderValues.None }),
            new TableJustification { Val = TableRowAlignmentValues.Center }));
        var sr = new TableRow();
        sr.Append(SigCell("________________________", navy));
        sr.Append(SigCell("________________________", navy));
        sr.Append(SigCell("________________________", navy));
        sigTable.Append(sr);
        var nr = new TableRow();
        nr.Append(SigNameCell("Principal / Director", dark));
        nr.Append(SigNameCell("Class Teacher", dark));
        nr.Append(SigNameCell("Examination Controller", dark));
        sigTable.Append(nr);
        body.Append(sigTable);
    }

    private static void BuildReportCardTemplate(Body body, string navy, string gold, string white, string lightGray, string dark, string muted)
    {
        // Header
        var hdrTable = new Table(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableBorders(new BottomBorder { Val = BorderValues.Single, Size = 12, Color = gold }),
            new Shading { Val = ShadingPatternValues.Clear, Fill = navy },
            new TableJustification { Val = TableRowAlignmentValues.Center }));
        var hr = new TableRow();
        var leftCell = new TableCell(
            new TableCellProperties(new TableCellWidth { Width = "3500", Type = TableWidthUnitValues.Pct }),
            new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "40" }),
                new Run(new RunProperties(new RunFonts { Ascii = "Georgia" }, new Bold(), new FontSize { Val = "36" }, new Color { Val = white }),
                    new Text("{{DepartmentName}}") { Space = SpaceProcessingModeValues.Preserve })),
            new Paragraph(new Run(new RunProperties(new RunFonts { Ascii = "Calibri" }, new FontSize { Val = "20" }, new Color { Val = gold }),
                new Text("Academic Report Card  |  {{ProgramName}}") { Space = SpaceProcessingModeValues.Preserve })));
        hr.Append(leftCell);
        var rightCell = new TableCell(
            new TableCellProperties(new TableCellWidth { Width = "1500", Type = TableWidthUnitValues.Pct }, new Shading { Val = ShadingPatternValues.Clear, Fill = gold }),
            new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                new Run(new RunProperties(new RunFonts { Ascii = "Calibri" }, new Bold(), new FontSize { Val = "22" }, new Color { Val = navy }),
                    new Text("REPORT CARD") { Space = SpaceProcessingModeValues.Preserve })),
            new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                new Run(new RunProperties(new RunFonts { Ascii = "Calibri" }, new FontSize { Val = "16" }, new Color { Val = navy }),
                    new Text("{{SerialNumber}}") { Space = SpaceProcessingModeValues.Preserve })));
        hr.Append(rightCell);
        hdrTable.Append(hr);
        body.Append(hdrTable);

        // Student info grid
        var infoTable = new Table(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableBorders(new BottomBorder { Val = BorderValues.Single, Size = 6, Color = gold }),
            new Shading { Val = ShadingPatternValues.Clear, Fill = lightGray },
            new TableJustification { Val = TableRowAlignmentValues.Center }));
        var ir = new TableRow();
        ir.Append(InfoCell("Student Name", "{{StudentName}}", navy, muted));
        ir.Append(InfoCell("Father Name", "{{FatherName}}", navy, muted));
        ir.Append(InfoCell("Registration No", "{{RegistrationNumber}}", navy, muted));
        ir.Append(InfoCell("Class / Year", "{{ClassName}}", navy, muted));
        infoTable.Append(ir);
        body.Append(infoTable);

        // Summary cards
        var sumTable = new Table(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableBorders(new BottomBorder { Val = BorderValues.Single, Size = 6, Color = gold }),
            new Shading { Val = ShadingPatternValues.Clear, Fill = navy },
            new TableJustification { Val = TableRowAlignmentValues.Center }));
        var sr2 = new TableRow();
        sr2.Append(SummaryCell("{{FinalPercentage}}%", "Final Percentage", gold, white));
        sr2.Append(SummaryCell("{{FinalGpa}}", "Final GPA", gold, white));
        sr2.Append(SummaryCell("{{ClassName}}", "Duration", gold, white));
        sr2.Append(SummaryCell("{{IssueDate}}", "Issue Date", gold, white));
        sumTable.Append(sr2);
        body.Append(sumTable);

        // Section title
        body.Append(new Paragraph(new ParagraphProperties(
            new ParagraphBorders(new BottomBorder { Val = BorderValues.Single, Size = 8, Color = gold, Space = 6 }),
            new Shading { Val = ShadingPatternValues.Clear, Fill = lightGray },
            new SpacingBetweenLines { Before = "200", After = "100" }),
            new Run(new RunProperties(new RunFonts { Ascii = "Georgia" }, new Bold(), new FontSize { Val = "24" }, new Color { Val = navy }),
                new Text("📚  Academic Record") { Space = SpaceProcessingModeValues.Preserve })));

        // Course table placeholder
        AddCenteredPara(body, "{{COURSE_TABLE}}", "Calibri", "22", dark, spacingAfter: "200");

        // Semester summary
        AddCenteredPara(body, "{{SemesterGpaSummary}}", "Calibri", "20", muted, spacingAfter: "200");

        // Footer info
        AddCenteredPara(body, "Issue Date: {{IssueDate}}  |  Certificate No: {{SerialNumber}}", "Calibri", "16", muted, spacingAfter: "100");
    }

    private static void AddCenteredPara(Body body, string text, string font, string fontSize, string color,
        bool bold = false, bool italic = false, string spacingAfter = "120")
    {
        var rp = new RunProperties(new RunFonts { Ascii = font, HighAnsi = font }, new FontSize { Val = fontSize }, new Color { Val = color });
        if (bold) rp.Append(new Bold());
        if (italic) rp.Append(new Italic());
        body.Append(new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }, new SpacingBetweenLines { After = spacingAfter }),
            new Run(rp, new Text(text) { Space = SpaceProcessingModeValues.Preserve })));
    }

    private static void AddInfoRow(Body body, string label, string value, string navy, string muted)
    {
        body.Append(new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }, new SpacingBetweenLines { After = "60" }),
            new Run(new RunProperties(new RunFonts { Ascii = "Calibri" }, new FontSize { Val = "20" }, new Color { Val = muted }),
                new Text(label + ": ") { Space = SpaceProcessingModeValues.Preserve }),
            new Run(new RunProperties(new RunFonts { Ascii = "Georgia" }, new FontSize { Val = "22" }, new Bold(), new Color { Val = navy }),
                new Text(value) { Space = SpaceProcessingModeValues.Preserve })));
    }

    private static TableCell SigCell(string text, string color)
    {
        return new TableCell(
            new TableCellProperties(new TableCellWidth { Width = "1600", Type = TableWidthUnitValues.Pct }),
            new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center }, new SpacingBetweenLines { Before = "200", After = "60" }),
                new Run(new RunProperties(new RunFonts { Ascii = "Calibri" }, new FontSize { Val = "18" }, new Color { Val = color }),
                    new Text(text) { Space = SpaceProcessingModeValues.Preserve })));
    }

    private static TableCell SigNameCell(string role, string dark)
    {
        return new TableCell(
            new TableCellProperties(new TableCellWidth { Width = "1600", Type = TableWidthUnitValues.Pct }),
            new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                new Run(new RunProperties(new RunFonts { Ascii = "Georgia" }, new FontSize { Val = "20" }, new Bold(), new Color { Val = dark }),
                    new Text(role) { Space = SpaceProcessingModeValues.Preserve })));
    }

    private static TableCell InfoCell(string label, string value, string navy, string muted)
    {
        return new TableCell(
            new TableCellProperties(new TableCellWidth { Width = "1250", Type = TableWidthUnitValues.Pct }),
            new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center }, new SpacingBetweenLines { Before = "40", After = "20" }),
                new Run(new RunProperties(new RunFonts { Ascii = "Calibri" }, new FontSize { Val = "14" }, new Color { Val = muted }),
                    new Text(label) { Space = SpaceProcessingModeValues.Preserve })),
            new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center }, new SpacingBetweenLines { After = "40" }),
                new Run(new RunProperties(new RunFonts { Ascii = "Georgia" }, new Bold(), new FontSize { Val = "20" }, new Color { Val = navy }),
                    new Text(value) { Space = SpaceProcessingModeValues.Preserve })));
    }

    private static TableCell SummaryCell(string value, string label, string gold, string white)
    {
        return new TableCell(
            new TableCellProperties(new TableCellWidth { Width = "1250", Type = TableWidthUnitValues.Pct }),
            new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center }, new SpacingBetweenLines { Before = "60", After = "20" }),
                new Run(new RunProperties(new RunFonts { Ascii = "Georgia" }, new Bold(), new FontSize { Val = "36" }, new Color { Val = gold }),
                    new Text(value) { Space = SpaceProcessingModeValues.Preserve })),
            new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center }, new SpacingBetweenLines { After = "60" }),
                new Run(new RunProperties(new RunFonts { Ascii = "Calibri" }, new FontSize { Val = "16" }, new Color { Val = white }),
                    new Text(label) { Space = SpaceProcessingModeValues.Preserve })));
    }

    private static async Task<List<AdditionalCertificateIndexEntry>> ListAdditionalCertificatesAsync(CancellationToken ct)
    {
        var path = GetAdditionalCertificateIndexPath();
        if (!System.IO.File.Exists(path))
            return new List<AdditionalCertificateIndexEntry>();

        try
        {
            var json = await System.IO.File.ReadAllTextAsync(path, ct);
            return JsonSerializer.Deserialize<List<AdditionalCertificateIndexEntry>>(json) ?? new List<AdditionalCertificateIndexEntry>();
        }
        catch
        {
            return new List<AdditionalCertificateIndexEntry>();
        }
    }

    private static async Task SaveAdditionalCertificatesAsync(List<AdditionalCertificateIndexEntry> entries, CancellationToken ct)
    {
        var path = GetAdditionalCertificateIndexPath();
        var json = JsonSerializer.Serialize(entries);
        await System.IO.File.WriteAllTextAsync(path, json, ct);
    }

    private Guid GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    private int? GetInstitutionTypeFromClaims()
    {
        var raw = User.FindFirst("institutionType")?.Value;
        return int.TryParse(raw, out var value) ? value : null;
    }

    private sealed class AdditionalCertificateIndexEntry
    {
        public Guid DocumentId { get; set; }
        public Guid StudentProfileId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? DocumentType { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadedAtUtc { get; set; }
        public Guid? UploadedByUserId { get; set; }
    }

    private sealed record TranscriptResultProjection(
        Guid CourseOfferingId,
        Guid SemesterId,
        string SemesterName,
        string CourseCode,
        string CourseTitle,
        int CreditHours,
        string ResultType,
        decimal MarksObtained,
        decimal MaxMarks,
        decimal? GradePoint)
    {
        public decimal Percentage => MaxMarks <= 0 ? 0m : (MarksObtained / MaxMarks) * 100m;
    }

    private sealed record TranscriptBuildContext(
        IReadOnlyList<TranscriptCourseRow> Rows,
        string FinalCgpa,
        string SemesterGpaSummary);

    private sealed record ResultSummary(
        string FinalPercentage,
        string SemesterSummary);
}
