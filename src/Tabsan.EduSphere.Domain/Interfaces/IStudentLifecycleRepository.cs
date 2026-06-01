using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.StudentLifecycle;

namespace Tabsan.EduSphere.Domain.Interfaces;

/// <summary>
/// Repository contract for Phase 8 student lifecycle operations:
/// Graduation, semester promotion, dropouts, and related workflows.
/// </summary>
public interface IStudentLifecycleRepository
{
    // ── Graduation ────────────────────────────────────────────────────────
    /// <summary>Gets all students in a specific department who are in their final semester.</summary>
    Task<IList<StudentProfile>> GetFinalSemesterStudentsByDepartmentAsync(Guid departmentId, CancellationToken ct = default);

    /// <summary>Gets students by their status (Active, Inactive, Graduated) within a department.</summary>
    Task<IList<StudentProfile>> GetStudentsByStatusAsync(Guid departmentId, StudentStatus status, CancellationToken ct = default);

    // ── Semester Promotion ────────────────────────────────────────────────
    /// <summary>Gets all Active students in a department currently in the given semester number.</summary>
    Task<IList<StudentProfile>> GetActiveStudentsBySemesterAsync(Guid departmentId, int semesterNumber, CancellationToken ct = default);

    /// <summary>
    /// Gets all Active students in a department whose current semester falls within the
    /// inclusive [startSemesterNumber, endSemesterNumber] range.
    /// </summary>
    Task<IList<StudentProfile>> GetActiveStudentsBySemesterRangeAsync(
        Guid departmentId,
        int startSemesterNumber,
        int endSemesterNumber,
        CancellationToken ct = default);

    /// <summary>Gets a student by ID with Program and Department loaded.</summary>
    Task<StudentProfile?> GetByIdAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Updates a student profile and commits changes.</summary>
    Task UpdateAsync(StudentProfile student, CancellationToken ct = default);

    // ── Admin Change Requests ──────────────────────────────────────────────
    /// <summary>Gets all pending admin change requests.</summary>
    Task<IList<AdminChangeRequest>> GetPendingChangeRequestsAsync(CancellationToken ct = default);

    /// <summary>Gets pending admin change requests for a specific user (requestor).</summary>
    Task<IList<AdminChangeRequest>> GetPendingChangeRequestsByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Gets an admin change request by ID with related users loaded.</summary>
    Task<AdminChangeRequest?> GetChangeRequestByIdAsync(Guid requestId, CancellationToken ct = default);

    /// <summary>Gets all admin change requests (all statuses) for a specific user.</summary>
    Task<IList<AdminChangeRequest>> GetAllChangeRequestsByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Creates a new admin change request.</summary>
    Task AddChangeRequestAsync(AdminChangeRequest request, CancellationToken ct = default);

    /// <summary>Updates an existing admin change request.</summary>
    Task UpdateChangeRequestAsync(AdminChangeRequest request, CancellationToken ct = default);

    // ── Teacher Modification Requests ──────────────────────────────────────
    /// <summary>Gets all pending teacher modification requests.</summary>
    Task<IList<TeacherModificationRequest>> GetPendingModificationRequestsAsync(CancellationToken ct = default);

    /// <summary>Gets pending modification requests for a specific teacher.</summary>
    Task<IList<TeacherModificationRequest>> GetPendingModificationRequestsByTeacherAsync(Guid teacherUserId, CancellationToken ct = default);

    /// <summary>Gets a teacher modification request by ID with related users loaded.</summary>
    Task<TeacherModificationRequest?> GetModificationRequestByIdAsync(Guid requestId, CancellationToken ct = default);

    /// <summary>Gets all modification requests (all statuses) for a specific teacher.</summary>
    Task<IList<TeacherModificationRequest>> GetAllModificationRequestsByTeacherAsync(Guid teacherUserId, CancellationToken ct = default);

    /// <summary>Creates a new teacher modification request.</summary>
    Task AddModificationRequestAsync(TeacherModificationRequest request, CancellationToken ct = default);

    /// <summary>Updates an existing teacher modification request.</summary>
    Task UpdateModificationRequestAsync(TeacherModificationRequest request, CancellationToken ct = default);

    // ── Payment Receipts ───────────────────────────────────────────────────
    /// <summary>Gets all active (non-cancelled) payment receipts for a student.</summary>
    Task<IList<PaymentReceipt>> GetActiveReceiptsByStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Gets a paged active (non-cancelled) payment receipt slice for a student.</summary>
    Task<IList<PaymentReceipt>> GetActiveReceiptsByStudentPagedAsync(Guid studentProfileId, int skip, int take, CancellationToken ct = default);

    /// <summary>Counts all active (non-cancelled) payment receipts for a student.</summary>
    Task<int> CountActiveReceiptsByStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Gets all payment receipts (including cancelled) for a student.</summary>
    Task<IList<PaymentReceipt>> GetAllReceiptsByStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Gets a paged payment receipt slice (including cancelled) for a student.</summary>
    Task<IList<PaymentReceipt>> GetAllReceiptsByStudentPagedAsync(Guid studentProfileId, int skip, int take, CancellationToken ct = default);

    /// <summary>Counts all payment receipts (including cancelled) for a student.</summary>
    Task<int> CountAllReceiptsByStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Gets a specific payment receipt by ID with related data loaded.</summary>
    Task<PaymentReceipt?> GetReceiptByIdAsync(Guid receiptId, CancellationToken ct = default);

    /// <summary>Checks whether a receipt number already exists, excluding an optional receipt id.</summary>
    Task<bool> ReceiptNoExistsAsync(string receiptNo, Guid? excludeReceiptId = null, CancellationToken ct = default);

    /// <summary>Gets unpaid receipts (Pending or Submitted status) for a student.</summary>
    Task<IList<PaymentReceipt>> GetUnpaidReceiptsByStudentAsync(Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Gets all unpaid receipts across all students.</summary>
    Task<IList<PaymentReceipt>> GetAllUnpaidReceiptsAsync(CancellationToken ct = default);

    /// <summary>Gets a paged unpaid receipt slice across all students.</summary>
    Task<IList<PaymentReceipt>> GetAllUnpaidReceiptsPagedAsync(int skip, int take, CancellationToken ct = default);

    /// <summary>Counts unpaid receipts across all students.</summary>
    Task<int> CountAllUnpaidReceiptsAsync(CancellationToken ct = default);

    /// <summary>Gets all receipts across all students (for admin view).</summary>
    Task<IList<PaymentReceipt>> GetAllReceiptsAsync(CancellationToken ct = default);

    /// <summary>Gets a paged receipt slice across all students (for admin view).</summary>
    Task<IList<PaymentReceipt>> GetAllReceiptsPagedAsync(int skip, int take, CancellationToken ct = default);

    /// <summary>Counts receipts across all students (for admin view).</summary>
    Task<int> CountAllReceiptsAsync(CancellationToken ct = default);

    /// <summary>Gets a student profile by the linked user account ID.</summary>
    Task<StudentProfile?> GetStudentProfileByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Creates a new payment receipt.</summary>
    Task AddReceiptAsync(PaymentReceipt receipt, CancellationToken ct = default);

    /// <summary>Updates an existing payment receipt.</summary>
    Task UpdateReceiptAsync(PaymentReceipt receipt, CancellationToken ct = default);

    /// <summary>Commits all pending changes to the database.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
