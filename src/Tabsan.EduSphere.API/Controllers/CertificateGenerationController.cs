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

    public CertificateGenerationController(
        ApplicationDbContext db,
        IAccessScopeResolver accessScope,
        IAdminAssignmentRepository adminAssignments,
        IFacultyAssignmentRepository facultyAssignments,
        IStudentProfileRepository studentProfiles,
        IInstitutionPolicyService institutionPolicy,
        DocumentGenerationService documents)
    {
        _db = db;
        _accessScope = accessScope;
        _adminAssignments = adminAssignments;
        _facultyAssignments = facultyAssignments;
        _studentProfiles = studentProfiles;
        _institutionPolicy = institutionPolicy;
        _documents = documents;
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

        var request = new DegreeGenerationRequest(
            StudentId: student.Id,
            StudentName: await ResolveStudentNameAsync(student.UserId, student.RegistrationNumber, ct),
            FatherName: "-",
            DegreeTitle: $"{student.Program?.Name ?? "Degree"}",
            Cgpa: student.Cgpa.ToString("0.00"));

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

        var transcriptRows = await BuildTranscriptRowsAsync(student.Id, semesterId, ct);
        var request = new TranscriptGenerationRequest(
            StudentId: student.Id,
            StudentName: await ResolveStudentNameAsync(student.UserId, student.RegistrationNumber, ct),
            FatherName: "-",
            DegreeTitle: $"{student.Program?.Name ?? "Transcript"}",
            Cgpa: student.Cgpa.ToString("0.00"),
            Courses: transcriptRows);

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
    public async Task<IActionResult> GenerateAdditionalCertificate(Guid studentProfileId, [FromQuery] string? documentType, CancellationToken ct)
    {
        var normalizedType = NormalizeAdditionalDocumentType(documentType);
        if (normalizedType is null)
            return BadRequest(new { message = "Unsupported document type. Use completion or reportcard." });

        var student = await GetNonUniversityStudentForAdminManagementAsync(studentProfileId, ct);
        if (student is null)
            return Forbid();

        var templateBytes = await GetAdditionalTemplateBytesAsync(normalizedType, ct);
        var root = GetAdditionalCertificateStorageRoot();
        var studentFolder = Path.Combine(root, studentProfileId.ToString("N"));
        Directory.CreateDirectory(studentFolder);

        var fileName = normalizedType == CompletionDocumentType
            ? $"completion-certificate-{DateTime.UtcNow:yyyyMMddHHmmss}.docx"
            : $"report-card-{DateTime.UtcNow:yyyyMMddHHmmss}.docx";
        var fullPath = Path.Combine(studentFolder, fileName);

        await System.IO.File.WriteAllBytesAsync(fullPath, templateBytes, ct);

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
        var policy = await _institutionPolicy.GetPolicyAsync(ct);
        if (!policy.IncludeUniversity)
            return null;

        var student = await _db.StudentProfiles
            .Include(s => s.Department)
            .Include(s => s.Program)
            .FirstOrDefaultAsync(s => s.Id == studentProfileId && s.Status == StudentStatus.Graduated, ct);

        if (student is null || student.Department.InstitutionType != InstitutionType.University)
            return null;

        if (_accessScope.IsSuperAdmin())
            return student;

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
        var userName = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Username)
            .FirstOrDefaultAsync(ct);

        return string.IsNullOrWhiteSpace(userName) ? fallback : userName;
    }

    private async Task<IReadOnlyList<TranscriptCourseRow>> BuildTranscriptRowsAsync(Guid studentProfileId, CancellationToken ct)
    {
        var courseRows = await (
            from enrollment in _db.Enrollments.AsNoTracking()
            where enrollment.StudentProfileId == studentProfileId
            join offering in _db.CourseOfferings.AsNoTracking() on enrollment.CourseOfferingId equals offering.Id
            join course in _db.Courses.AsNoTracking() on offering.CourseId equals course.Id
            orderby course.Code
            select new TranscriptCourseRow(
                string.IsNullOrWhiteSpace(course.Code) ? course.Title : $"{course.Code} - {course.Title}",
                course.CreditHours,
                "N/A",
                "N/A"))
            .ToListAsync(ct);

        return courseRows;
    }

    private async Task<IReadOnlyList<TranscriptCourseRow>> BuildTranscriptRowsAsync(Guid studentProfileId, Guid? semesterId, CancellationToken ct)
    {
        var query =
            from enrollment in _db.Enrollments.AsNoTracking()
            where enrollment.StudentProfileId == studentProfileId
            join offering in _db.CourseOfferings.AsNoTracking() on enrollment.CourseOfferingId equals offering.Id
            join course in _db.Courses.AsNoTracking() on offering.CourseId equals course.Id
            select new { offering.SemesterId, course.Code, course.Title, course.CreditHours };

        if (semesterId.HasValue)
            query = query.Where(x => x.SemesterId == semesterId.Value);

        var rows = await query
            .OrderBy(x => x.Code)
            .Select(x => new TranscriptCourseRow(
                string.IsNullOrWhiteSpace(x.Code) ? x.Title : $"{x.Code} - {x.Title}",
                x.CreditHours,
                "N/A",
                "N/A"))
            .ToListAsync(ct);

        return rows;
    }

    private async Task<StudentProfile?> GetNonUniversityStudentForAdminManagementAsync(Guid studentProfileId, CancellationToken ct)
    {
        var student = await _db.StudentProfiles
            .Include(s => s.Department)
            .FirstOrDefaultAsync(s => s.Id == studentProfileId, ct);

        if (student is null || student.Department.InstitutionType == InstitutionType.University)
            return null;

        if (_accessScope.IsSuperAdmin())
            return student;

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
        var lines = normalizedType == CompletionDocumentType
            ? new[]
            {
                "Completion Certificate",
                string.Empty,
                "This certifies that {{StudentName}} ({{RegistrationNumber}})",
                "has successfully completed {{ProgramName}}.",
                "Department: {{DepartmentName}}",
                "Issued On: {{IssueDate}}"
            }
            : new[]
            {
                "Report Card",
                string.Empty,
                "Student: {{StudentName}}",
                "Registration #: {{RegistrationNumber}}",
                "Program/Class: {{ProgramName}}",
                "Department: {{DepartmentName}}",
                "Issued On: {{IssueDate}}"
            };

        using var stream = new MemoryStream();
        using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            foreach (var line in lines)
            {
                body.Append(new Paragraph(new Run(new Text(line) { Space = SpaceProcessingModeValues.Preserve })));
            }

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        return stream.ToArray();
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
}
