// Final-Touches Phase 18 Stage 18.1/18.2 — Graduation Workflow API controller

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.API.Services;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Graduation workflow endpoints: submit application, multi-stage approval,
/// certificate generation and download.
/// </summary>
[ApiController]
[Route("api/v1/graduation")]
[Authorize]
public class GraduationController : ControllerBase
{
    private readonly IGraduationService         _graduation;
    private readonly IStudentProfileRepository  _studentRepo;
    private readonly IMediaStorageService _mediaStorage;
    private readonly MediaStorageOptions _mediaStorageOptions;
    private readonly IFacultyAssignmentRepository _facultyAssignments;

    public GraduationController(
        IGraduationService         graduation,
        IStudentProfileRepository  studentRepo,
        IMediaStorageService mediaStorage,
        IOptions<MediaStorageOptions> mediaStorageOptions,
        IFacultyAssignmentRepository facultyAssignments)
    {
        _facultyAssignments = facultyAssignments;
        _graduation  = graduation;
        _studentRepo = studentRepo;
        _mediaStorage = mediaStorage;
        _mediaStorageOptions = mediaStorageOptions.Value;
    }

    // ── Student: view own applications ────────────────────────────────────────

    // Final-Touches Phase 18 Stage 18.1 — student lists their own graduation applications
    /// <summary>Returns the current student's graduation applications.</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyApplications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var profile = await _studentRepo.GetByUserIdAsync(GetUserId(), ct);
        if (profile is null) return NotFound("Student profile not found.");
        var apps = await _graduation.GetMyApplicationsAsync(profile.Id, page, pageSize, ct);
        return Ok(apps);
    }

    // ── All authenticated: view application detail ─────────────────────────────

    // Final-Touches Phase 18 Stage 18.1 — any authorised user retrieves application detail
    /// <summary>Returns detail for a specific graduation application.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct)
    {
        try
        {
            var detail = await _graduation.GetApplicationDetailAsync(id, ct);
            return Ok(detail);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    // ── Admin / SuperAdmin: list applications ──────────────────────────────────

    // Final-Touches Phase 18 Stage 18.1 — admin/superadmin list applications
    /// <summary>Returns all graduation applications, optionally filtered by department and status.</summary>
    // Faculty approve the first stage, so they need to see the applications of their own departments.
    [HttpGet]
    [Authorize(Roles = "Faculty,Admin,SuperAdmin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? departmentId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (User.IsInRole("Faculty") && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
        {
            var userIdRaw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var allowed = Guid.TryParse(userIdRaw, out var userId)
                ? await _facultyAssignments.GetDepartmentIdsForFacultyAsync(userId, ct)
                : Array.Empty<Guid>();

            if (departmentId.HasValue && !allowed.Contains(departmentId.Value))
                return Forbid();

            departmentId ??= allowed.FirstOrDefault();
            if (departmentId is null || departmentId == Guid.Empty)
                return Ok(new GraduationApplicationPageDto([], page, pageSize, 0));
        }

        var apps = await _graduation.GetApplicationsAsync(departmentId, status, page, pageSize, ct);
        return Ok(apps);
    }

    // ── Student: submit application ────────────────────────────────────────────

    // Final-Touches Phase 18 Stage 18.1 — student submits graduation application
    /// <summary>Submits a new graduation application for the authenticated student.</summary>
    [HttpPost("submit")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitGraduationApplicationRequest request,
        CancellationToken ct)
    {
        var profile = await _studentRepo.GetByUserIdAsync(GetUserId(), ct);
        if (profile is null) return NotFound("Student profile not found.");

        try
        {
            var result = await _graduation.SubmitApplicationAsync(profile.Id, request, ct);
            return CreatedAtAction(nameof(GetDetail), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    // ── Faculty: approve ───────────────────────────────────────────────────────

    // Final-Touches Phase 18 Stage 18.1 — faculty reviews and approves/rejects
    /// <summary>Faculty reviews and approves (or rejects) a pending graduation application.</summary>
    [HttpPost("{id:guid}/faculty-approve")]
    [Authorize(Roles = "Faculty")]
    public async Task<IActionResult> FacultyApprove(
        Guid id,
        [FromBody] GraduationApprovalRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _graduation.FacultyApproveAsync(id, GetUserId(), request, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    // ── Admin: approve ─────────────────────────────────────────────────────────

    // Final-Touches Phase 18 Stage 18.1 — admin reviews and approves/rejects
    /// <summary>Admin reviews and approves (or rejects) an application forwarded from Faculty.</summary>
    [HttpPost("{id:guid}/admin-approve")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> AdminApprove(
        Guid id,
        [FromBody] GraduationApprovalRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _graduation.AdminApproveAsync(id, GetUserId(), request, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    // ── SuperAdmin: final approval ─────────────────────────────────────────────

    // Final-Touches Phase 18 Stage 18.1/18.2 — superadmin final approval + auto certificate
    /// <summary>SuperAdmin gives final approval, which triggers certificate generation.</summary>
    [HttpPost("{id:guid}/final-approve")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> FinalApprove(
        Guid id,
        [FromBody] GraduationApprovalRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _graduation.FinalApproveAsync(id, GetUserId(), request, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    // ── Any approver: reject ───────────────────────────────────────────────────

    // Final-Touches Phase 18 Stage 18.1 — role-aware rejection
    /// <summary>Rejects a graduation application at the caller's stage.</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Faculty,Admin,SuperAdmin")]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] GraduationApprovalRequest request,
        CancellationToken ct)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "SuperAdmin";
        try
        {
            var result = await _graduation.RejectAsync(id, GetUserId(), role, request, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    // ── Certificate endpoints ──────────────────────────────────────────────────

    // Final-Touches Phase 18 Stage 18.2 — student/admin downloads certificate PDF
    /// <summary>Downloads the PDF graduation certificate for an approved application.</summary>
    [HttpGet("{id:guid}/certificate")]
    [Authorize(Roles = "Student,Admin,SuperAdmin")]
    public async Task<IActionResult> DownloadCertificate(Guid id, CancellationToken ct)
    {
        Guid? requestingStudentProfileId = null;

        // Final-Touches Phase 28 Stage 28.3 — verify access first, then issue tokenized certificate read.
        GraduationApplicationDetail detail;
        try
        {
            detail = await _graduation.GetApplicationDetailAsync(id, ct);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }

        if (User.IsInRole("Student"))
        {
            var profile = await _studentRepo.GetByUserIdAsync(GetUserId(), ct);
            if (profile is null) return NotFound("Student profile not found.");
            if (profile.Id != detail.StudentProfileId) return Forbid();
            requestingStudentProfileId = profile.Id;
        }

        if (string.IsNullOrWhiteSpace(detail.CertificatePath))
            return NotFound("Certificate not yet generated or not found.");

        // Keep legacy /certificates/* records on original byte flow.
        if (detail.CertificatePath.StartsWith("/", StringComparison.Ordinal))
        {
            var studentProfileId = requestingStudentProfileId ?? detail.StudentProfileId;
            var legacyBytes = await _graduation.DownloadCertificateAsync(id, studentProfileId, ct);
            if (legacyBytes is null) return NotFound("Certificate not yet generated or not found.");
            return File(legacyBytes, "application/pdf", $"graduation_certificate_{id}.pdf");
        }

        var temporaryUrl = await _mediaStorage.GenerateTemporaryReadUrlAsync(detail.CertificatePath, TimeSpan.FromMinutes(10), ct);
        if (Uri.TryCreate(temporaryUrl, UriKind.Absolute, out _))
            return Redirect(temporaryUrl!);

        if (IsSignedReadRequired())
            return Redirect(BuildLocalSignedCertificateUrl(detail.CertificatePath, TimeSpan.FromMinutes(10)));

        var escapedKey = Uri.EscapeDataString(detail.CertificatePath).Replace("%2F", "/", StringComparison.OrdinalIgnoreCase);
        return Redirect($"/api/v1/graduation/certificate-files/{escapedKey}");
    }

    /// <summary>
    /// Streams a provider-backed graduation certificate by storage key.
    /// Requires authenticated role and (when configured) a valid signed URL.
    /// </summary>
    [HttpGet("certificate-files/{**storageKey}")]
    [Authorize(Roles = "Student,Admin,SuperAdmin")]
    public async Task<IActionResult> GetCertificateFile(
        string storageKey,
        [FromQuery] long? exp,
        [FromQuery] string? sig,
        [FromQuery] string? download,
        CancellationToken ct)
    {
        // Final-Touches Phase 28 Stage 28.3 — enforce signed certificate-file reads for local serving.
        if (string.IsNullOrWhiteSpace(storageKey))
            return NotFound();

        if (!storageKey.Contains("certificates", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        if (IsSignedReadRequired())
        {
            if (!exp.HasValue || string.IsNullOrWhiteSpace(sig))
                return Redirect(BuildLocalSignedCertificateUrl(storageKey, TimeSpan.FromMinutes(10)));

            if (exp.Value < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                return NotFound();

            if (!IsValidLocalSignature(storageKey, exp.Value, sig))
                return NotFound();
        }

        var metadata = await _mediaStorage.GetMetadataAsync(storageKey, ct);
        var bytes = await _mediaStorage.ReadAsBytesAsync(storageKey, ct);
        if (bytes is null) return NotFound("Certificate not yet generated or not found.");

        var fileName = !string.IsNullOrWhiteSpace(download)
            ? Path.GetFileName(download)
            : metadata?.DownloadFileName;

        if (!string.IsNullOrWhiteSpace(fileName))
            return File(bytes, metadata?.ContentType ?? "application/pdf", fileName);

        return File(bytes, metadata?.ContentType ?? "application/pdf");
    }

    // Final-Touches Phase 18 Stage 18.2 — admin/superadmin regenerates certificate
    /// <summary>Re-generates the certificate PDF for an approved application.</summary>
    [HttpPost("{id:guid}/regenerate-certificate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> RegenerateCertificate(Guid id, CancellationToken ct)
    {
        try
        {
            var path = await _graduation.GenerateCertificateAsync(id, ct);
            return Ok(new { Path = path });
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    // ── Helper ─────────────────────────────────────────────────────────────────

    private Guid GetUserId()
    {
        var raw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    private bool IsSignedReadRequired() => !string.IsNullOrWhiteSpace(_mediaStorageOptions.SignedUrlSecret);

    private string BuildLocalSignedCertificateUrl(string storageKey, TimeSpan ttl)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(ttl <= TimeSpan.Zero ? TimeSpan.FromMinutes(10) : ttl).ToUnixTimeSeconds();
        var signature = CreateSignature(storageKey, expiresAt);

        var escapedKey = Uri.EscapeDataString(storageKey).Replace("%2F", "/", StringComparison.OrdinalIgnoreCase);
        var metadata = _mediaStorage.GetMetadataAsync(storageKey).GetAwaiter().GetResult();
        var url = $"/api/v1/graduation/certificate-files/{escapedKey}?exp={expiresAt}&sig={Uri.EscapeDataString(signature)}";

        if (!string.IsNullOrWhiteSpace(metadata?.DownloadFileName))
            url += $"&download={Uri.EscapeDataString(metadata.DownloadFileName)}";

        return url;
    }

    private bool IsValidLocalSignature(string storageKey, long expiresAt, string providedSignature)
    {
        var expected = CreateSignature(storageKey, expiresAt);
        var normalized = providedSignature.Trim();

        if (!TryDecodeHex(expected, out var expectedBytes)) return false;
        if (!TryDecodeHex(normalized, out var providedBytes)) return false;

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }

    private string CreateSignature(string storageKey, long expiresAt)
    {
        var secret = _mediaStorageOptions.SignedUrlSecret;
        if (string.IsNullOrWhiteSpace(secret))
            return string.Empty;

        var payload = $"{storageKey}|{expiresAt}";
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        return Convert.ToHexString(hmac.ComputeHash(payloadBytes)).ToLowerInvariant();
    }

    private static bool TryDecodeHex(string value, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromHexString(value);
            return true;
        }
        catch
        {
            bytes = Array.Empty<byte>();
            return false;
        }
    }
}
