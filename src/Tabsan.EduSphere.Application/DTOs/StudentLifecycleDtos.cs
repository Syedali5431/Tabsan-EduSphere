namespace Tabsan.EduSphere.Application.Dtos;

// ── Graduation DTOs ───────────────────────────────────────────────────

public record GraduateStudentRequest(
    Guid StudentProfileId
);

public record GraduationSummaryDto(
    Guid Id,
    string RegistrationNumber,
    string StudentName,
    string ProgramName,
    int CurrentSemester,
    DateTime? GraduatedDate
);

// ── Semester Promotion DTOs ───────────────────────────────────────────

/// <summary>Summary of a student eligible for semester promotion.</summary>
public record SemesterPromotionSummaryDto(
    Guid StudentProfileId,
    string RegistrationNumber,
    string ProgramName,
    int CurrentSemesterNumber
);

/// <summary>Batch promotion request: promote all supplied students to the next semester.</summary>
public record PromoteStudentsBatchRequest(
    IList<Guid> StudentProfileIds
);

/// <summary>Result of a batch promotion operation.</summary>
public record PromotionBatchResultDto(
    int Promoted,
    int Failed,
    IList<string> Errors
);

// ── Admin Change Request DTOs ─────────────────────────────────────────

public record CreateChangeRequestCommand(
    string ChangeDescription,
    string NewData,
    string? Reason = null
);

public record AdminChangeRequestDto(
    Guid Id,
    string ChangeDescription,
    string NewData,
    string? Reason,
    string Status,
    DateTime CreatedAt,
    string? AdminNotes = null,
    DateTime? ReviewedAt = null
);

public record ApproveChangeRequestCommand(
    Guid RequestId,
    string? AdminNotes = null
);

public record RejectChangeRequestCommand(
    Guid RequestId,
    string? AdminNotes = null
);

// ── Teacher Modification Request DTOs ─────────────────────────────────

public record CreateModificationRequestCommand(
    string ModificationType, // "Attendance" or "Result"
    Guid RecordId,
    string Reason,
    string ProposedData
);

public record TeacherModificationRequestDto(
    Guid Id,
    string ModificationType,
    Guid RecordId,
    string Reason,
    string ProposedData,
    string Status,
    DateTime CreatedAt,
    string? AdminNotes = null,
    DateTime? ReviewedAt = null
);

public record ApproveModificationRequestCommand(
    Guid RequestId,
    string? AdminNotes = null
);

public record RejectModificationRequestCommand(
    Guid RequestId,
    string? AdminNotes = null
);

// ── Payment Receipt DTOs ──────────────────────────────────────────────

public record CreatePaymentReceiptCommand(
    Guid StudentProfileId,
    decimal Amount,
    string Description,
    DateTime DueDate
);

public record PaymentReceiptDto(
    Guid Id,
    Guid StudentProfileId,
    string StudentName,
    decimal Amount,
    string Description,
    DateTime DueDate,
    string Status,
    string? ProofOfPaymentPath = null,
    DateTime? ProofUploadedAt = null,
    DateTime? PaidDate = null,
    DateTime? ConfirmedAt = null,
    DateTime? UpdatedAt = null,
    string? Notes = null,
    DateTime CreatedAt = default
);

public record SubmitProofCommand(
    Guid ReceiptId,
    string ProofPath
);

public record ConfirmPaymentCommand(
    Guid ReceiptId,
    string? Notes = null
);

public sealed record PaymentReceiptPageDto(
    IReadOnlyList<PaymentReceiptDto> Items,
    int Page,
    int PageSize,
    int TotalCount
);

public record StudentFeeStatusDto(
    Guid StudentProfileId,
    bool HasUnpaidReceipts,
    int UnpaidReceiptCount,
    decimal TotalUnpaid,
    IList<PaymentReceiptDto> UnpaidReceipts
);
