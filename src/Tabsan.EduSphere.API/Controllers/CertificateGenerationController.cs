using System.Security.Claims;
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
        CancellationToken ct)
    {
        var policy = await _institutionPolicy.GetPolicyAsync(ct);
        if (!policy.IncludeUniversity)
            return Forbid();

        if (tenantId.HasValue != campusId.HasValue)
            return BadRequest(new { message = "TenantId and CampusId must be provided together." });

        var callerInstitutionType = GetInstitutionTypeFromClaims();
        if (!_accessScope.IsSuperAdmin() && callerInstitutionType.HasValue && callerInstitutionType.Value != (int)InstitutionType.University)
            return Forbid();

        var query = _db.StudentProfiles
            .AsNoTracking()
            .Where(s => s.Status == StudentStatus.Graduated && s.Department.InstitutionType == InstitutionType.University)
            .Include(s => s.Program)
            .Include(s => s.Department)
            .AsQueryable();

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
                LatestTranscriptGeneratedAtUtc = latestTranscript?.GeneratedAtUtc
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
    public async Task<IActionResult> GenerateTranscriptCertificate(Guid studentProfileId, CancellationToken ct)
    {
        var student = await GetStudentInScopeAsync(studentProfileId, ct);
        if (student is null)
            return Forbid();

        var transcriptRows = await BuildTranscriptRowsAsync(student.Id, ct);
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
}
