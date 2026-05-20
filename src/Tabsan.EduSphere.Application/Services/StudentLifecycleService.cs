using System.Text.Json;
using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Notifications;
using Tabsan.EduSphere.Domain.StudentLifecycle;

namespace Tabsan.EduSphere.Application.Services;

/// <summary>
/// Implementation of IStudentLifecycleService.
/// Manages graduation, status changes, change requests, modification workflows, and payment receipts.
/// Notifications are sent via INotificationService after state changes are persisted.
/// </summary>
public class StudentLifecycleService : IStudentLifecycleService
{
    private readonly IStudentLifecycleRepository _repository;
    private readonly IProgressionService _progression;
    private readonly INotificationService _notifications;
    private readonly IUserRepository _users;

    public StudentLifecycleService(
        IStudentLifecycleRepository repository,
        IProgressionService progression,
        INotificationService notifications,
        IUserRepository users)
    {
        _repository    = repository;
        _progression   = progression;
        _notifications = notifications;
        _users         = users;
    }

    // ── Graduation ────────────────────────────────────────────────────────
    public async Task<IList<GraduationSummaryDto>> GetGraduationCandidatesByDepartmentAsync(
        Guid departmentId,
        CancellationToken ct = default)
    {
        var students = await _repository.GetFinalSemesterStudentsByDepartmentAsync(departmentId, ct);
        
        return students.Select(s => new GraduationSummaryDto(
            s.Id,
            s.RegistrationNumber,
            "", // Student name loaded separately from User table via JOIN
            s.Program.Name,
            s.CurrentSemesterNumber,
            s.GraduatedDate
        )).ToList();
    }

    public async Task GraduateStudentAsync(Guid studentProfileId, CancellationToken ct = default)
    {
        var student = await _repository.GetByIdAsync(studentProfileId, ct);
        if (student == null)
            throw new KeyNotFoundException($"Student profile {studentProfileId} not found.");

        student.Graduate();
        await _repository.UpdateAsync(student, ct);

        await _notifications.SendSystemAsync(
            title:            "Graduation Approved",
            body:             "Congratulations! Your graduation has been processed successfully.",
            type:             NotificationType.System,
            recipientUserIds: new[] { student.UserId },
            ct:               ct);
    }

    public async Task GraduateStudentsBatchAsync(
        IList<Guid> studentProfileIds,
        CancellationToken ct = default)
    {
        foreach (var studentId in studentProfileIds)
        {
            await GraduateStudentAsync(studentId, ct);
        }
    }

    // ── Academic-Level Progression & Promotion ────────────────────────────
    public Task<IList<SemesterPromotionSummaryDto>> GetStudentsByAcademicLevelAsync(
        Guid departmentId,
        int levelNumber,
        CancellationToken ct = default)
        => GetStudentsByAcademicLevelInternalAsync(departmentId, levelNumber, ct);

    private async Task<IList<SemesterPromotionSummaryDto>> GetStudentsByAcademicLevelInternalAsync(
        Guid departmentId,
        int levelNumber,
        CancellationToken ct)
    {
        if (levelNumber <= 0)
            return [];

        var activeStudents = await _repository.GetStudentsByStatusAsync(departmentId, StudentStatus.Active, ct);
        var institutionType = activeStudents.FirstOrDefault()?.Department?.InstitutionType;

        IList<StudentProfile> students;

        if (institutionType == InstitutionType.College)
        {
            var startSemester = ((levelNumber - 1) * 2) + 1;
            var endSemester = startSemester + 1;
            students = await _repository.GetActiveStudentsBySemesterRangeAsync(departmentId, startSemester, endSemester, ct);
        }
        else
        {
            students = await _repository.GetActiveStudentsBySemesterAsync(departmentId, levelNumber, ct);
        }

        return students.Select(s => new SemesterPromotionSummaryDto(
            s.Id,
            s.RegistrationNumber,
            s.Program?.Name ?? string.Empty,
            s.CurrentSemesterNumber
        )).ToList();
    }

    public async Task<IList<SemesterPromotionSummaryDto>> GetStudentsBySemesterAsync(
        Guid departmentId,
        int semesterNumber,
        CancellationToken ct = default)
    {
        var students = await _repository.GetActiveStudentsBySemesterAsync(departmentId, semesterNumber, ct);
        return students.Select(s => new SemesterPromotionSummaryDto(
            s.Id,
            s.RegistrationNumber,
            s.Program?.Name ?? string.Empty,
            s.CurrentSemesterNumber
        )).ToList();
    }

    public async Task PromoteStudentAsync(Guid studentProfileId, CancellationToken ct = default)
    {
        var student = await _repository.GetByIdAsync(studentProfileId, ct);
        if (student == null)
            throw new KeyNotFoundException($"Student profile {studentProfileId} not found.");

        if (student.Status != Domain.Enums.StudentStatus.Active)
            throw new InvalidOperationException($"Only Active students can be promoted. Student {studentProfileId} has status {student.Status}.");

        if (student.Department?.InstitutionType is InstitutionType.School or InstitutionType.College)
        {
            await _progression.PromoteAsync(
                new ProgressionEvaluationRequest(studentProfileId, student.Department.InstitutionType),
                ct);
            return;
        }

        student.AdvanceSemester();
        await _repository.UpdateAsync(student, ct);

        await _notifications.SendSystemAsync(
            title:            "Academic Progress Updated",
            body:             "Your academic level has been advanced to the next semester.",
            type:             NotificationType.System,
            recipientUserIds: new[] { student.UserId },
            ct:               ct);
    }

    public async Task<PromotionBatchResultDto> PromoteStudentsBatchAsync(
        IList<Guid> studentProfileIds,
        CancellationToken ct = default)
    {
        int promoted = 0;
        var errors = new List<string>();

        foreach (var id in studentProfileIds)
        {
            try
            {
                await PromoteStudentAsync(id, ct);
                promoted++;
            }
            catch (KeyNotFoundException e)
            {
                errors.Add(e.Message);
            }
            catch (InvalidOperationException e)
            {
                errors.Add(e.Message);
            }
        }

        return new PromotionBatchResultDto(promoted, errors.Count, errors);
    }

    // ── Student Status Management ──────────────────────────────────────────
    public async Task DeactivateStudentAsync(Guid studentProfileId, CancellationToken ct = default)
    {
        var student = await _repository.GetByIdAsync(studentProfileId, ct);
        if (student == null)
            throw new KeyNotFoundException($"Student profile {studentProfileId} not found.");

        student.Deactivate();
        await _repository.UpdateAsync(student, ct);

        await _notifications.SendSystemAsync(
            title:            "Account Status Updated",
            body:             "Your student profile has been deactivated. Please contact administration for details.",
            type:             NotificationType.System,
            recipientUserIds: new[] { student.UserId },
            ct:               ct);
    }

    public async Task ReactivateStudentAsync(Guid studentProfileId, CancellationToken ct = default)
    {
        var student = await _repository.GetByIdAsync(studentProfileId, ct);
        if (student == null)
            throw new KeyNotFoundException($"Student profile {studentProfileId} not found.");

        student.Reactivate();
        await _repository.UpdateAsync(student, ct);

        await _notifications.SendSystemAsync(
            title:            "Account Reactivated",
            body:             "Your student profile has been reactivated and is now active.",
            type:             NotificationType.System,
            recipientUserIds: new[] { student.UserId },
            ct:               ct);
    }

    // ── Admin Change Requests ──────────────────────────────────────────────
    public async Task<AdminChangeRequestDto> CreateChangeRequestAsync(
        Guid requestorUserId,
        CreateChangeRequestCommand cmd,
        CancellationToken ct = default)
    {
        var request = new AdminChangeRequest(
            requestorUserId,
            cmd.ChangeDescription,
            cmd.NewData,
            cmd.Reason
        );

        await _repository.AddChangeRequestAsync(request, ct);

        await NotifyAdminsAsync(
            title: "Profile Change Request Pending",
            body:  "A new profile change request has been submitted and requires admin review.",
            ct:    ct);

        return MapChangeRequest(request);
    }

    public async Task<IList<AdminChangeRequestDto>> GetPendingChangeRequestsAsync(
        CancellationToken ct = default)
    {
        var requests = await _repository.GetPendingChangeRequestsAsync(ct);
        return requests.Select(MapChangeRequest).ToList();
    }

    public async Task<IList<AdminChangeRequestDto>> GetChangeRequestsByUserAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var requests = await _repository.GetAllChangeRequestsByUserAsync(userId, ct);
        return requests.Select(MapChangeRequest).ToList();
    }

    public async Task<AdminChangeRequestDto?> GetChangeRequestByIdAsync(Guid requestId, CancellationToken ct = default)
    {
        var request = await _repository.GetChangeRequestByIdAsync(requestId, ct);
        return request != null ? MapChangeRequest(request) : null;
    }

    public async Task ApproveChangeRequestAsync(
        Guid requestId,
        Guid adminUserId,
        string? notes = null,
        CancellationToken ct = default)
    {
        var request = await _repository.GetChangeRequestByIdAsync(requestId, ct);
        if (request == null)
            throw new KeyNotFoundException($"Change request {requestId} not found.");

        request.Approve(adminUserId, notes);
        await _repository.UpdateChangeRequestAsync(request, ct);

        await _notifications.SendSystemAsync(
            title:            "Change Request Approved",
            body:             "Your profile change request has been approved.",
            type:             NotificationType.System,
            recipientUserIds: new[] { request.RequestorUserId },
            ct:               ct);
    }

    public async Task RejectChangeRequestAsync(
        Guid requestId,
        Guid adminUserId,
        string? notes = null,
        CancellationToken ct = default)
    {
        var request = await _repository.GetChangeRequestByIdAsync(requestId, ct);
        if (request == null)
            throw new KeyNotFoundException($"Change request {requestId} not found.");

        request.Reject(adminUserId, notes);
        await _repository.UpdateChangeRequestAsync(request, ct);

        await _notifications.SendSystemAsync(
            title:            "Change Request Rejected",
            body:             $"Your profile change request was rejected. {BuildNotesSuffix(notes)}",
            type:             NotificationType.System,
            recipientUserIds: new[] { request.RequestorUserId },
            ct:               ct);
    }

    // ── Teacher Modification Requests ──────────────────────────────────────
    public async Task<TeacherModificationRequestDto> CreateModificationRequestAsync(
        Guid teacherUserId,
        CreateModificationRequestCommand cmd,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<ModificationType>(cmd.ModificationType, true, out var modType))
            throw new ArgumentException($"Invalid modification type: {cmd.ModificationType}");

        var request = new TeacherModificationRequest(
            teacherUserId,
            modType,
            cmd.RecordId,
            cmd.Reason,
            cmd.ProposedData
        );

        await _repository.AddModificationRequestAsync(request, ct);

        await NotifyAdminsAsync(
            title: "Modification Request Pending",
            body:  "A teacher modification request has been submitted and requires admin review.",
            ct:    ct);

        return MapModificationRequest(request);
    }

    public async Task<IList<TeacherModificationRequestDto>> GetPendingModificationRequestsAsync(
        CancellationToken ct = default)
    {
        var requests = await _repository.GetPendingModificationRequestsAsync(ct);
        return requests.Select(MapModificationRequest).ToList();
    }

    public async Task<IList<TeacherModificationRequestDto>> GetModificationRequestsByTeacherAsync(
        Guid teacherUserId,
        CancellationToken ct = default)
    {
        var requests = await _repository.GetAllModificationRequestsByTeacherAsync(teacherUserId, ct);
        return requests.Select(MapModificationRequest).ToList();
    }

    public async Task<TeacherModificationRequestDto?> GetModificationRequestByIdAsync(
        Guid requestId,
        CancellationToken ct = default)
    {
        var request = await _repository.GetModificationRequestByIdAsync(requestId, ct);
        return request != null ? MapModificationRequest(request) : null;
    }

    public async Task ApproveModificationRequestAsync(
        Guid requestId,
        Guid adminUserId,
        string? notes = null,
        CancellationToken ct = default)
    {
        var request = await _repository.GetModificationRequestByIdAsync(requestId, ct);
        if (request == null)
            throw new KeyNotFoundException($"Modification request {requestId} not found.");

        request.Approve(adminUserId, notes);
        await _repository.UpdateModificationRequestAsync(request, ct);

        await _notifications.SendSystemAsync(
            title:            "Modification Request Approved",
            body:             "Your modification request has been approved.",
            type:             NotificationType.System,
            recipientUserIds: new[] { request.TeacherUserId },
            ct:               ct);
    }

    public async Task RejectModificationRequestAsync(
        Guid requestId,
        Guid adminUserId,
        string? notes = null,
        CancellationToken ct = default)
    {
        var request = await _repository.GetModificationRequestByIdAsync(requestId, ct);
        if (request == null)
            throw new KeyNotFoundException($"Modification request {requestId} not found.");

        request.Reject(adminUserId, notes);
        await _repository.UpdateModificationRequestAsync(request, ct);

        await _notifications.SendSystemAsync(
            title:            "Modification Request Rejected",
            body:             $"Your modification request was rejected. {BuildNotesSuffix(notes)}",
            type:             NotificationType.System,
            recipientUserIds: new[] { request.TeacherUserId },
            ct:               ct);
    }

    // ── Payment Receipts ───────────────────────────────────────────────────
    // Final-Touches Phase 7 Stage 7.2 — GetAllReceiptsAsync + GetReceiptsByUserAsync
    public async Task<PaymentReceiptPageDto> GetAllReceiptsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = Math.Clamp(pageSize, 10, 100);
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var totalCount = await _repository.CountAllReceiptsAsync(ct);
        var receipts = await _repository.GetAllReceiptsPagedAsync(skip, normalizedPageSize, ct);
        var items = receipts.Select(r => MapPaymentReceipt(r, r.StudentProfile?.RegistrationNumber ?? "")).ToList();
        return new PaymentReceiptPageDto(items, normalizedPage, normalizedPageSize, totalCount);
    }

    public async Task<PaymentReceiptPageDto> GetReceiptsByUserAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var profile = await _repository.GetStudentProfileByUserIdAsync(userId, ct);
        if (profile is null) return new PaymentReceiptPageDto(Array.Empty<PaymentReceiptDto>(), 1, Math.Clamp(pageSize, 10, 100), 0);

        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = Math.Clamp(pageSize, 10, 100);
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var totalCount = await _repository.CountActiveReceiptsByStudentAsync(profile.Id, ct);
        var receipts = await _repository.GetActiveReceiptsByStudentPagedAsync(profile.Id, skip, normalizedPageSize, ct);
        var items = receipts.Select(r => MapPaymentReceipt(r, profile.RegistrationNumber)).ToList();
        return new PaymentReceiptPageDto(items, normalizedPage, normalizedPageSize, totalCount);
    }

    public async Task<PaymentReceiptDto> CreatePaymentReceiptAsync(
        Guid financeUserId,
        CreatePaymentReceiptCommand cmd,
        CancellationToken ct = default)
    {
        var receipt = new PaymentReceipt(
            cmd.StudentProfileId,
            financeUserId,
            cmd.Amount,
            cmd.Description,
            cmd.DueDate
        );

        await _repository.AddReceiptAsync(receipt, ct);

        // Final-Touches Phase 7 Stage 7.3 — notify student on receipt creation
        var student = await _repository.GetByIdAsync(cmd.StudentProfileId, ct);
        if (student is not null)
        {
            await _notifications.SendSystemAsync(
                title:            "New Fee Receipt",
                body:             $"A fee receipt of {cmd.Amount:N2} for '{cmd.Description}' has been created. Due: {cmd.DueDate:dd MMM yyyy}.",
                type:             NotificationType.System,
                recipientUserIds: new[] { student.UserId },
                ct:               ct);
        }
        return MapPaymentReceipt(receipt, student?.RegistrationNumber ?? "");
    }

    public async Task<PaymentReceiptPageDto> GetActiveReceiptsByStudentAsync(
        Guid studentProfileId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var student = await _repository.GetByIdAsync(studentProfileId, ct);
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = Math.Clamp(pageSize, 10, 100);
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var totalCount = await _repository.CountActiveReceiptsByStudentAsync(studentProfileId, ct);
        var receipts = await _repository.GetActiveReceiptsByStudentPagedAsync(studentProfileId, skip, normalizedPageSize, ct);
        var items = receipts.Select(r => MapPaymentReceipt(r, student?.RegistrationNumber ?? "")).ToList();
        return new PaymentReceiptPageDto(items, normalizedPage, normalizedPageSize, totalCount);
    }

    public async Task<StudentFeeStatusDto> GetStudentFeeStatusAsync(
        Guid studentProfileId,
        CancellationToken ct = default)
    {
        var unpaidReceipts = await _repository.GetUnpaidReceiptsByStudentAsync(studentProfileId, ct);
        var student = await _repository.GetByIdAsync(studentProfileId, ct);

        var dtos = unpaidReceipts.Select(r => MapPaymentReceipt(r, student?.RegistrationNumber ?? "")).ToList();

        return new StudentFeeStatusDto(
            studentProfileId,
            unpaidReceipts.Any(),
            unpaidReceipts.Count,
            unpaidReceipts.Sum(r => r.Amount),
            dtos
        );
    }

    public async Task<PaymentReceiptDto?> GetPaymentReceiptByIdAsync(Guid receiptId, CancellationToken ct = default)
    {
        var receipt = await _repository.GetReceiptByIdAsync(receiptId, ct);
        if (receipt == null)
            return null;

        return MapPaymentReceipt(receipt, receipt.StudentProfile?.RegistrationNumber ?? "");
    }

    public async Task SubmitPaymentProofAsync(Guid receiptId, string proofPath, CancellationToken ct = default)
    {
        var receipt = await _repository.GetReceiptByIdAsync(receiptId, ct);
        if (receipt == null)
            throw new KeyNotFoundException($"Receipt {receiptId} not found.");

        receipt.SubmitProof(proofPath);
        await _repository.UpdateReceiptAsync(receipt, ct);

        // Final-Touches Phase 7 Stage 7.3 — notify admins that proof was submitted
        var reviewerUsers = await _users.GetActiveUsersByRolesAsync(new[] { "Finance", "Admin", "SuperAdmin" }, ct);
        var reviewerIds = reviewerUsers.Select(u => u.Id).Distinct().ToList();

        if (reviewerIds.Count > 0)
        {
            await _notifications.SendSystemAsync(
                title:            "Payment Proof Submitted",
                body:             $"A student has submitted proof of payment for receipt {receiptId}. Please verify.",
                type:             NotificationType.System,
                recipientUserIds: reviewerIds,
                ct:               ct);
        }
    }

    public async Task ConfirmPaymentAsync(
        Guid receiptId,
        Guid financeUserId,
        string? notes = null,
        CancellationToken ct = default)
    {
        var receipt = await _repository.GetReceiptByIdAsync(receiptId, ct);
        if (receipt == null)
            throw new KeyNotFoundException($"Receipt {receiptId} not found.");

        receipt.ConfirmPayment(financeUserId, notes);
        await _repository.UpdateReceiptAsync(receipt, ct);

        // Final-Touches Phase 7 Stage 7.3 — notify student their payment was confirmed
        var student = await _repository.GetByIdAsync(receipt.StudentProfileId, ct);
        if (student is not null)
        {
            await _notifications.SendSystemAsync(
                title:            "Payment Confirmed",
                body:             $"Your payment for receipt {receiptId} has been confirmed as Paid.",
                type:             NotificationType.System,
                recipientUserIds: new[] { student.UserId },
                ct:               ct);
        }
    }

    public async Task CancelReceiptAsync(
        Guid receiptId,
        Guid financeUserId,
        string? reason = null,
        CancellationToken ct = default)
    {
        var receipt = await _repository.GetReceiptByIdAsync(receiptId, ct);
        if (receipt == null)
            throw new KeyNotFoundException($"Receipt {receiptId} not found.");

        receipt.Cancel(financeUserId, reason);
        await _repository.UpdateReceiptAsync(receipt, ct);

        // Final-Touches Phase 7 Stage 7.3 — notify student their receipt was cancelled
        var student = await _repository.GetByIdAsync(receipt.StudentProfileId, ct);
        if (student is not null)
        {
            await _notifications.SendSystemAsync(
                title:            "Receipt Cancelled",
                body:             $"Your payment receipt {receiptId} has been cancelled. Reason: {reason ?? "Not specified"}.",
                type:             NotificationType.System,
                recipientUserIds: new[] { student.UserId },
                ct:               ct);
        }
    }

    // ── Mapping helpers ────────────────────────────────────────────────────
    private static AdminChangeRequestDto MapChangeRequest(AdminChangeRequest request)
    {
        return new AdminChangeRequestDto(
            request.Id,
            request.ChangeDescription,
            request.NewData,
            request.Reason,
            request.Status.ToString(),
            request.CreatedAt,
            request.AdminNotes,
            request.ReviewedAt
        );
    }

    private static TeacherModificationRequestDto MapModificationRequest(TeacherModificationRequest request)
    {
        return new TeacherModificationRequestDto(
            request.Id,
            request.ModificationType.ToString(),
            request.RecordId,
            request.Reason,
            request.ProposedData,
            request.Status.ToString(),
            request.CreatedAt,
            request.AdminNotes,
            request.ReviewedAt
        );
    }

    private static PaymentReceiptDto MapPaymentReceipt(PaymentReceipt receipt, string studentName = "")
    {
        var status = Enum.IsDefined(typeof(PaymentReceiptStatus), receipt.Status)
            ? receipt.Status.ToString()
            : PaymentReceiptStatus.Pending.ToString();

        return new PaymentReceiptDto(
            receipt.Id,
            receipt.StudentProfileId,
            studentName,
            receipt.Amount,
            receipt.Description,
            receipt.DueDate,
            status,
            receipt.ProofOfPaymentPath,
            receipt.ProofUploadedAt,
            receipt.ConfirmedAt,
            receipt.ConfirmedAt,
            receipt.UpdatedAt,
            receipt.Notes,
            receipt.CreatedAt
        );
    }

    private async Task NotifyAdminsAsync(string title, string body, CancellationToken ct)
    {
        var adminUsers = await _users.GetActiveUsersByRolesAsync(new[] { "Admin", "SuperAdmin" }, ct);
        var adminIds = adminUsers.Select(u => u.Id).Distinct().ToList();

        if (adminIds.Count == 0)
            return;

        await _notifications.SendSystemAsync(
            title:            title,
            body:             body,
            type:             NotificationType.System,
            recipientUserIds: adminIds,
            ct:               ct);
    }

    private static string BuildNotesSuffix(string? notes)
        => string.IsNullOrWhiteSpace(notes) ? "Please contact administration for more details." : $"Notes: {notes}";
}
