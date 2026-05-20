using Tabsan.EduSphere.Application.Dtos;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Service for Phase 8 student lifecycle operations:
/// Graduation, semester completion/promotion, dropouts, change requests, and payments.
/// </summary>
public interface IStudentLifecycleService
{
    // ── Graduation ────────────────────────────────────────────────────────
    /// <summary>Gets all students in a department eligible for graduation (final semester, Active status).</summary>
    Task<IList<GraduationSummaryDto>> GetGraduationCandidatesByDepartmentAsync(
        Guid departmentId,
        CancellationToken ct = default);

    /// <summary>Marks a student as Graduated.</summary>
    Task GraduateStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Marks multiple students as Graduated in batch.</summary>
    Task GraduateStudentsBatchAsync(IList<Guid> studentProfileIds, CancellationToken ct = default);

    // ── Academic-Level Progression & Promotion ────────────────────────────
    /// <summary>Gets all Active students in a department currently in the given academic level number.</summary>
    Task<IList<SemesterPromotionSummaryDto>> GetStudentsByAcademicLevelAsync(
        Guid departmentId,
        int levelNumber,
        CancellationToken ct = default);

    /// <summary>Gets all Active students in a department currently in the given semester number.</summary>
    Task<IList<SemesterPromotionSummaryDto>> GetStudentsBySemesterAsync(
        Guid departmentId,
        int semesterNumber,
        CancellationToken ct = default);

    /// <summary>Advances a single student to the next semester (increments CurrentSemesterNumber by 1).</summary>
    Task PromoteStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>
    /// Advances multiple students to the next semester in one operation.
    /// Returns a result with promoted count and any errors (e.g., student not found).
    /// </summary>
    Task<PromotionBatchResultDto> PromoteStudentsBatchAsync(
        IList<Guid> studentProfileIds,
        CancellationToken ct = default);

    // ── Student Status Management ──────────────────────────────────────────
    /// <summary>Marks a student as Inactive (dropout/leave). Student blocked from login.</summary>
    Task DeactivateStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Re-activates an inactive student account.</summary>
    Task ReactivateStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    // ── Admin Change Requests ──────────────────────────────────────────────
    /// <summary>Creates a new change request (student/teacher self-initiated).</summary>
    Task<AdminChangeRequestDto> CreateChangeRequestAsync(
        Guid requestorUserId,
        CreateChangeRequestCommand cmd,
        CancellationToken ct = default);

    /// <summary>Gets all pending change requests for admin review.</summary>
    Task<IList<AdminChangeRequestDto>> GetPendingChangeRequestsAsync(CancellationToken ct = default);

    /// <summary>Gets all change requests (all statuses) for a specific user.</summary>
    Task<IList<AdminChangeRequestDto>> GetChangeRequestsByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Gets a specific change request by ID.</summary>
    Task<AdminChangeRequestDto?> GetChangeRequestByIdAsync(Guid requestId, CancellationToken ct = default);

    /// <summary>Admin approves a change request. Caller responsible for applying the actual data changes.</summary>
    Task ApproveChangeRequestAsync(
        Guid requestId,
        Guid adminUserId,
        string? notes = null,
        CancellationToken ct = default);

    /// <summary>Admin rejects a change request.</summary>
    Task RejectChangeRequestAsync(
        Guid requestId,
        Guid adminUserId,
        string? notes = null,
        CancellationToken ct = default);

    // ── Teacher Modification Requests ──────────────────────────────────────
    /// <summary>Teacher creates a request to modify an attendance or result record.</summary>
    Task<TeacherModificationRequestDto> CreateModificationRequestAsync(
        Guid teacherUserId,
        CreateModificationRequestCommand cmd,
        CancellationToken ct = default);

    /// <summary>Gets all pending modification requests for admin review.</summary>
    Task<IList<TeacherModificationRequestDto>> GetPendingModificationRequestsAsync(CancellationToken ct = default);

    /// <summary>Gets all modification requests (all statuses) for a specific teacher.</summary>
    Task<IList<TeacherModificationRequestDto>> GetModificationRequestsByTeacherAsync(
        Guid teacherUserId,
        CancellationToken ct = default);

    /// <summary>Gets a specific modification request by ID.</summary>
    Task<TeacherModificationRequestDto?> GetModificationRequestByIdAsync(
        Guid requestId,
        CancellationToken ct = default);

    /// <summary>Admin approves a modification request. Caller responsible for applying the actual changes.</summary>
    Task ApproveModificationRequestAsync(
        Guid requestId,
        Guid adminUserId,
        string? notes = null,
        CancellationToken ct = default);

    /// <summary>Admin rejects a modification request.</summary>
    Task RejectModificationRequestAsync(
        Guid requestId,
        Guid adminUserId,
        string? notes = null,
        CancellationToken ct = default);

    // ── Payment Receipts ───────────────────────────────────────────────────
    /// <summary>Finance creates a new payment receipt for a student.</summary>
    Task<PaymentReceiptDto> CreatePaymentReceiptAsync(
        Guid financeUserId,
        CreatePaymentReceiptCommand cmd,
        CancellationToken ct = default);

    /// <summary>Finance edits an actionable payment receipt before it is finalized.</summary>
    Task<PaymentReceiptDto> UpdatePaymentReceiptAsync(
        Guid receiptId,
        Guid financeUserId,
        UpdatePaymentReceiptCommand cmd,
        CancellationToken ct = default);

    /// <summary>Gets a paged payment receipt slice across all students (admin view).</summary>
    Task<PaymentReceiptPageDto> GetAllReceiptsAsync(int page, int pageSize, CancellationToken ct = default);

    /// <summary>Gets a paged active payment receipt slice for the student linked to the given user account ID.</summary>
    Task<PaymentReceiptPageDto> GetReceiptsByUserAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Gets a paged active (unpaid) payment receipt slice for a student.</summary>
    Task<PaymentReceiptPageDto> GetActiveReceiptsByStudentAsync(
        Guid studentProfileId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Gets complete fee status for a student (paid + unpaid).</summary>
    Task<StudentFeeStatusDto> GetStudentFeeStatusAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Gets a specific payment receipt by ID.</summary>
    Task<PaymentReceiptDto?> GetPaymentReceiptByIdAsync(Guid receiptId, CancellationToken ct = default);

    /// <summary>Student uploads proof of payment.</summary>
    Task SubmitPaymentProofAsync(Guid receiptId, string proofPath, CancellationToken ct = default);

    /// <summary>Finance confirms payment received (marks receipt as Paid).</summary>
    Task ConfirmPaymentAsync(
        Guid receiptId,
        Guid financeUserId,
        string? notes = null,
        CancellationToken ct = default);

    /// <summary>Finance cancels a receipt (e.g., if issued in error).</summary>
    Task CancelReceiptAsync(
        Guid receiptId,
        Guid financeUserId,
        string? reason = null,
        CancellationToken ct = default);
}
