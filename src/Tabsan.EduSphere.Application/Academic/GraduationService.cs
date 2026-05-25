// Final-Touches Phase 18 Stage 18.1/18.2 — Graduation workflow service implementation

using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.Application.Academic;

/// <summary>
/// Orchestrates the graduation application lifecycle (Stage 18.1)
/// and certificate PDF generation (Stage 18.2).
/// </summary>
public class GraduationService : IGraduationService
{
    private readonly IGraduationRepository  _repo;
    private readonly INotificationService   _notifications;
    private readonly ISettingsRepository    _settings;
    private readonly IStudentLifecycleRepository _lifecycleRepo;
    private readonly ICertificateGenerator  _certGenerator;
    private readonly IMediaStorageService _mediaStorage;

    // Final-Touches Phase 18 Stage 18.2 — portal setting key for the certificate body text
    private const string CertTemplateSetting = "graduation_certificate_template";

    // Final-Touches Phase 18 Stage 18.2 — subfolder within wwwroot for generated certificates
    private const string CertFolder = "certificates";

    public GraduationService(
        IGraduationRepository      repo,
        INotificationService       notifications,
        ISettingsRepository        settings,
        IStudentLifecycleRepository lifecycleRepo,
        ICertificateGenerator      certGenerator,
        IMediaStorageService mediaStorage)
    {
        _repo          = repo;
        _notifications = notifications;
        _settings      = settings;
        _lifecycleRepo = lifecycleRepo;
        _certGenerator = certGenerator;
        _mediaStorage = mediaStorage;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GraduationApplicationSummary ToSummary(GraduationApplication a, string name, string regNo, string program)
        => new(a.Id, a.StudentProfileId, name, regNo, program, a.Status.ToString(),
               a.SubmittedAt, a.UpdatedAt, a.CertificatePath is not null);

    // Final-Touches Phase 18 Stage 18.1 — map full detail
    private static GraduationApplicationDetail ToDetail(
        GraduationApplication a, string name, string regNo, string program,
        IReadOnlyList<ApprovalHistoryItem> history)
        => new(a.Id, a.StudentProfileId, name, regNo, program, a.Status.ToString(),
               a.StudentNote, a.SubmittedAt, a.UpdatedAt, a.CertificatePath is not null,
               a.CertificatePath, history);

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<GraduationApplicationPageDto> GetMyApplicationsAsync(
        Guid studentProfileId, int page, int pageSize, CancellationToken ct = default)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = Math.Clamp(pageSize, 10, 100);
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var totalCount = await _repo.CountByStudentAsync(studentProfileId, ct);
        var apps  = await _repo.GetByStudentPagedAsync(studentProfileId, skip, normalizedPageSize, ct);
        var name  = await _repo.GetStudentDisplayNameAsync(studentProfileId, ct);
        var regNo = await _repo.GetStudentRegistrationNumberAsync(studentProfileId, ct);
        var prog  = await _repo.GetStudentProgramNameAsync(studentProfileId, ct);
        var summaries = apps.Select(a => ToSummary(a, name, regNo, prog)).ToList();
        return new GraduationApplicationPageDto(summaries, normalizedPage, normalizedPageSize, totalCount);
    }

    public async Task<GraduationApplicationDetail> GetApplicationDetailAsync(
        Guid applicationId, CancellationToken ct = default)
    {
        var app = await _repo.GetByIdAsync(applicationId, ct)
            ?? throw new KeyNotFoundException($"Application {applicationId} not found.");

        var name  = await _repo.GetStudentDisplayNameAsync(app.StudentProfileId, ct);
        var regNo = await _repo.GetStudentRegistrationNumberAsync(app.StudentProfileId, ct);
        var prog  = await _repo.GetStudentProgramNameAsync(app.StudentProfileId, ct);

        // Final-Touches Phase 18 Stage 18.1 — build approval history
        var history = app.Approvals
            .OrderBy(a => a.ActedAt)
            .Select(a => new ApprovalHistoryItem(
                a.Stage.ToString(),
                "User",   // approver name: kept simple; controller can enrich if needed
                a.IsApproved,
                a.Note,
                a.ActedAt))
            .ToList();

        return ToDetail(app, name, regNo, prog, history);
    }

    public async Task<GraduationApplicationPageDto> GetApplicationsAsync(
        Guid? departmentId, string? statusFilter, int page, int pageSize, CancellationToken ct = default)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = Math.Clamp(pageSize, 10, 100);
        var skip = (normalizedPage - 1) * normalizedPageSize;

        GraduationApplicationStatus? status = statusFilter is null ? null
            : Enum.TryParse<GraduationApplicationStatus>(statusFilter, true, out var s) ? s : null;

        IReadOnlyList<GraduationApplication> apps;
        int totalCount;
        if (departmentId.HasValue)
        {
            totalCount = await _repo.CountByDepartmentAsync(departmentId.Value, status, ct);
            apps = await _repo.GetByDepartmentPagedAsync(departmentId.Value, status, skip, normalizedPageSize, ct);
        }
        else
        {
            totalCount = await _repo.CountAllAsync(status, ct);
            apps = await _repo.GetAllPagedAsync(status, skip, normalizedPageSize, ct);
        }

        var result = new List<GraduationApplicationSummary>();
        foreach (var a in apps)
        {
            var name  = await _repo.GetStudentDisplayNameAsync(a.StudentProfileId, ct);
            var regNo = await _repo.GetStudentRegistrationNumberAsync(a.StudentProfileId, ct);
            var prog  = await _repo.GetStudentProgramNameAsync(a.StudentProfileId, ct);
            result.Add(ToSummary(a, name, regNo, prog));
        }
        return new GraduationApplicationPageDto(result, normalizedPage, normalizedPageSize, totalCount);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    // Final-Touches Phase 18 Stage 18.1 — student submits application
    public async Task<GraduationApplicationSummary> SubmitApplicationAsync(
        Guid studentProfileId, SubmitGraduationApplicationRequest request, CancellationToken ct = default)
    {
        var student = await _lifecycleRepo.GetByIdAsync(studentProfileId, ct)
            ?? throw new KeyNotFoundException($"Student profile {studentProfileId} not found.");

        if (student.Department?.InstitutionType != InstitutionType.University)
            throw new InvalidOperationException("Graduation apply is available only for university students.");

        if (student.Status != StudentStatus.Active)
            throw new InvalidOperationException("Only active students can apply for graduation.");

        var totalSemesters = student.Program?.TotalSemesters ?? 0;
        if (totalSemesters <= 0 || student.CurrentSemesterNumber < totalSemesters)
            throw new InvalidOperationException("Graduation apply is allowed only in the final semester.");

        var hasCompletedFyp = await _repo.HasCompletedFypProjectAsync(studentProfileId, ct);
        if (!hasCompletedFyp)
            throw new InvalidOperationException("Complete your FYP before submitting graduation apply.");

        // Guard: only one active application at a time
        var existing = await _repo.GetActiveByStudentAsync(studentProfileId, ct);
        if (existing is not null &&
            existing.Status != GraduationApplicationStatus.Rejected)
            throw new InvalidOperationException("An active graduation application already exists.");

        var app = GraduationApplication.Create(studentProfileId, request.StudentNote);
        app.Submit();
        await _repo.AddAsync(app, ct);
        await _repo.SaveChangesAsync(ct);

        // Final-Touches Phase 18 Stage 18.1 — notify faculty of new application
        var facultyIds = await _repo.GetFacultyUserIdsByDepartmentAsync(student.DepartmentId, ct);
        if (facultyIds.Count > 0)
            await _notifications.SendSystemAsync(
                "Graduation Application Submitted",
                "A graduation application has been submitted and is awaiting your review.",
                NotificationType.General,
                facultyIds, ct);

        var name  = await _repo.GetStudentDisplayNameAsync(studentProfileId, ct);
        var regNo = await _repo.GetStudentRegistrationNumberAsync(studentProfileId, ct);
        var prog  = await _repo.GetStudentProgramNameAsync(studentProfileId, ct);
        return ToSummary(app, name, regNo, prog);
    }

    // Final-Touches Phase 18 Stage 18.1 — faculty approves
    public async Task<GraduationApplicationSummary> FacultyApproveAsync(
        Guid applicationId, Guid approverUserId, GraduationApprovalRequest request, CancellationToken ct = default)
    {
        var app = await _repo.GetByIdAsync(applicationId, ct)
            ?? throw new KeyNotFoundException($"Application {applicationId} not found.");

        if (!request.IsApproved)
            return await RejectInternalAsync(app, approverUserId, ApprovalStage.Faculty, request.Note, ct);

        app.FacultyApprove(approverUserId, request.Note);
        _repo.Update(app);
        await _repo.SaveChangesAsync(ct);

        // Final-Touches Phase 18 Stage 18.1 — notify admin
        var student  = await _lifecycleRepo.GetByIdAsync(app.StudentProfileId, ct);
        if (student is not null)
        {
            var adminIds = await _repo.GetAdminUserIdsByDepartmentAsync(student.DepartmentId, ct);
            if (adminIds.Count > 0)
                await _notifications.SendSystemAsync(
                    "Graduation Application Pending Admin Approval",
                    "A graduation application has been approved by Faculty and requires your review.",
                    NotificationType.General, adminIds, ct);
        }

        var name  = await _repo.GetStudentDisplayNameAsync(app.StudentProfileId, ct);
        var regNo = await _repo.GetStudentRegistrationNumberAsync(app.StudentProfileId, ct);
        var prog  = await _repo.GetStudentProgramNameAsync(app.StudentProfileId, ct);
        return ToSummary(app, name, regNo, prog);
    }

    // Final-Touches Phase 18 Stage 18.1 — admin approves
    public async Task<GraduationApplicationSummary> AdminApproveAsync(
        Guid applicationId, Guid approverUserId, GraduationApprovalRequest request, CancellationToken ct = default)
    {
        var app = await _repo.GetByIdAsync(applicationId, ct)
            ?? throw new KeyNotFoundException($"Application {applicationId} not found.");

        if (!request.IsApproved)
            return await RejectInternalAsync(app, approverUserId, ApprovalStage.Admin, request.Note, ct);

        app.AdminApprove(approverUserId, request.Note);
        _repo.Update(app);
        await _repo.SaveChangesAsync(ct);

        // Final-Touches Phase 18 Stage 18.1 — notify superadmin
        var superAdminIds = await _repo.GetSuperAdminUserIdsAsync(ct);
        if (superAdminIds.Count > 0)
            await _notifications.SendSystemAsync(
                "Graduation Application Pending Final Approval",
                "A graduation application has been approved by Admin and requires your final confirmation.",
                NotificationType.General, superAdminIds, ct);

        var name  = await _repo.GetStudentDisplayNameAsync(app.StudentProfileId, ct);
        var regNo = await _repo.GetStudentRegistrationNumberAsync(app.StudentProfileId, ct);
        var prog  = await _repo.GetStudentProgramNameAsync(app.StudentProfileId, ct);
        return ToSummary(app, name, regNo, prog);
    }

    // Final-Touches Phase 18 Stage 18.1/18.2 — superadmin final approval + certificate + graduation
    public async Task<GraduationApplicationSummary> FinalApproveAsync(
        Guid applicationId, Guid approverUserId, GraduationApprovalRequest request, CancellationToken ct = default)
    {
        var app = await _repo.GetByIdAsync(applicationId, ct)
            ?? throw new KeyNotFoundException($"Application {applicationId} not found.");

        if (!request.IsApproved)
            return await RejectInternalAsync(app, approverUserId, ApprovalStage.SuperAdmin, request.Note, ct);

        app.FinalApprove(approverUserId, request.Note);
        _repo.Update(app);
        try
        {
            await _repo.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (string.Equals(ex.GetType().Name, "DbUpdateConcurrencyException", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("This graduation application was modified by another user. Refresh and try again.");
        }

        // Final-Touches Phase 18 Stage 18.2 — generate certificate
        await GenerateCertificateAsync(applicationId, ct);

        // Final-Touches Phase 18 Stage 18.1 — trigger student graduation lifecycle
        var student = await _lifecycleRepo.GetByIdAsync(app.StudentProfileId, ct);
        if (student is not null)
        {
            student.Graduate();
            await _lifecycleRepo.UpdateAsync(student, ct);

            // Notify student
            await _notifications.SendSystemAsync(
                "Congratulations! Graduation Approved",
                "Your graduation application has been fully approved. Your certificate is ready for download.",
                NotificationType.General,
                new[] { student.UserId }, ct);
        }

        var name  = await _repo.GetStudentDisplayNameAsync(app.StudentProfileId, ct);
        var regNo = await _repo.GetStudentRegistrationNumberAsync(app.StudentProfileId, ct);
        var prog  = await _repo.GetStudentProgramNameAsync(app.StudentProfileId, ct);
        return ToSummary(app, name, regNo, prog);
    }

    public async Task<GraduationApplicationSummary> RejectAsync(
        Guid applicationId, Guid approverUserId, string approverRole,
        GraduationApprovalRequest request, CancellationToken ct = default)
    {
        var app = await _repo.GetByIdAsync(applicationId, ct)
            ?? throw new KeyNotFoundException($"Application {applicationId} not found.");

        var stage = approverRole switch
        {
            "Faculty"    => ApprovalStage.Faculty,
            "Admin"      => ApprovalStage.Admin,
            _            => ApprovalStage.SuperAdmin
        };

        return await RejectInternalAsync(app, approverUserId, stage, request.Note, ct);
    }

    private async Task<GraduationApplicationSummary> RejectInternalAsync(
        GraduationApplication app, Guid approverUserId, ApprovalStage stage, string? note, CancellationToken ct)
    {
        app.Reject(approverUserId, stage, note);
        _repo.Update(app);
        try
        {
            await _repo.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (string.Equals(ex.GetType().Name, "DbUpdateConcurrencyException", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("This graduation application was modified by another user. Refresh and try again.");
        }

        // Final-Touches Phase 18 Stage 18.1 — notify student of rejection
        var student = await _lifecycleRepo.GetByIdAsync(app.StudentProfileId, ct);
        if (student is not null)
            await _notifications.SendSystemAsync(
                "Graduation Application Rejected",
                $"Your graduation application was rejected at the {stage} review stage. Reason: {note ?? "No reason provided."}",
                NotificationType.General,
                new[] { student.UserId }, ct);

        var name  = await _repo.GetStudentDisplayNameAsync(app.StudentProfileId, ct);
        var regNo = await _repo.GetStudentRegistrationNumberAsync(app.StudentProfileId, ct);
        var prog  = await _repo.GetStudentProgramNameAsync(app.StudentProfileId, ct);
        return ToSummary(app, name, regNo, prog);
    }

    // ── Certificate Generation ────────────────────────────────────────────────

    // Final-Touches Phase 18 Stage 18.2 — generate QuestPDF certificate via infrastructure abstraction
    public async Task<string> GenerateCertificateAsync(Guid applicationId, CancellationToken ct = default)
    {
        var app = await _repo.GetByIdAsync(applicationId, ct)
            ?? throw new KeyNotFoundException($"Application {applicationId} not found.");

        if (app.Status != GraduationApplicationStatus.Approved)
            throw new InvalidOperationException("Certificate can only be generated for Approved applications.");

        var studentName = await _repo.GetStudentDisplayNameAsync(app.StudentProfileId, ct);
        var regNo       = await _repo.GetStudentRegistrationNumberAsync(app.StudentProfileId, ct);
        var programName = await _repo.GetStudentProgramNameAsync(app.StudentProfileId, ct);

        // Final-Touches Phase 18 Stage 18.2 — optional custom headline from portal settings
        var headline = await _settings.GetPortalSettingAsync(CertTemplateSetting, ct);

        byte[] pdfBytes = await _certGenerator.GeneratePdfAsync(studentName, regNo, programName, headline, ct);

        // Final-Touches Phase 28 Stage 28.3 — persist certificates through storage abstraction.
        await using var stream = new MemoryStream(pdfBytes);
        var stored = await _mediaStorage.SaveAsync(
            stream,
            CertFolder,
            ".pdf",
            "application/pdf",
            $"graduation_certificate_{applicationId}.pdf",
            ct);

        app.AttachCertificate(stored.StorageKey);
        _repo.Update(app);
        await _repo.SaveChangesAsync(ct);

        return stored.StorageKey;
    }

    // Final-Touches Phase 18 Stage 18.2 — return PDF bytes for download
    public async Task<byte[]?> DownloadCertificateAsync(
        Guid applicationId, Guid requestingStudentProfileId, CancellationToken ct = default)
    {
        var app = await _repo.GetByIdAsync(applicationId, ct);
        if (app is null) return null;

        // Students can only download their own certificate
        if (app.StudentProfileId != requestingStudentProfileId) return null;
        if (app.CertificatePath is null) return null;

        // Final-Touches Phase 28 Stage 28.3 — support legacy /certificates/* paths during transition.
        if (app.CertificatePath.StartsWith("/", StringComparison.Ordinal))
        {
            var wwwRoot  = Directory.GetCurrentDirectory();
            var fullPath = Path.Combine(wwwRoot, "wwwroot", app.CertificatePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath)) return null;
            return await File.ReadAllBytesAsync(fullPath, ct);
        }

        return await _mediaStorage.ReadAsBytesAsync(app.CertificatePath, ct);
    }
}
