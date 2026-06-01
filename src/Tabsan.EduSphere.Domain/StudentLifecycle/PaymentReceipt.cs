using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Common;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;

namespace Tabsan.EduSphere.Domain.StudentLifecycle;

/// <summary>
/// Represents a fee receipt for a student. Finance role creates receipts and uploads proofs.
/// Students view receipts, upload proofs of payment, and mark as submitted.
/// Finance confirms payment received, changing status to Paid.
/// Until Paid, student operates in read-only mode.
/// All payment records are permanent and cannot be deleted.
/// </summary>
public class PaymentReceipt : AuditableEntity
{
    /// <summary>FK to the student's profile.</summary>
    public Guid StudentProfileId { get; private set; }

    /// <summary>Navigation to the student.</summary>
    public StudentProfile StudentProfile { get; private set; } = default!;

    /// <summary>FK to the finance staff member who created the receipt.</summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>Navigation to the user who created the receipt.</summary>
    public User CreatedByUser { get; private set; } = default!;

    /// <summary>Current payment status: Pending, Submitted, Paid, or Cancelled.</summary>
    public PaymentReceiptStatus Status { get; private set; } = PaymentReceiptStatus.Pending;

    /// <summary>Amount due in the receipt.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Institution or bank receipt number, unique across payment receipts.</summary>
    public string ReceiptNo { get; private set; } = default!;

    /// <summary>Description of the fees (e.g., "Semester 1 Tuition", "Exam Fee").</summary>
    public string Description { get; private set; } = default!;

    /// <summary>Due date for payment.</summary>
    public DateTime DueDate { get; private set; }

    /// <summary>Path or reference to the uploaded proof of payment document. Set by student when marking as Submitted.</summary>
    public string? ProofOfPaymentPath { get; private set; }

    /// <summary>UTC timestamp when the student uploaded their proof of payment.</summary>
    public DateTime? ProofUploadedAt { get; private set; }

    /// <summary>FK to the finance staff member who confirmed the payment. Null until Paid.</summary>
    public Guid? ConfirmedByUserId { get; private set; }

    /// <summary>Navigation to the user who confirmed payment.</summary>
    public User? ConfirmedByUser { get; private set; }

    /// <summary>UTC timestamp when payment was confirmed as Paid. Null if not yet paid.</summary>
    public DateTime? ConfirmedAt { get; private set; }

    /// <summary>Optional notes from finance staff.</summary>
    public string? Notes { get; private set; }

    private PaymentReceipt() { }

    /// <summary>Creates a new payment receipt.</summary>
    public PaymentReceipt(
        Guid studentProfileId,
        Guid createdByUserId,
        decimal amount,
        string receiptNo,
        string description,
        DateTime dueDate)
    {
        StudentProfileId = studentProfileId;
        CreatedByUserId = createdByUserId;
        Amount = amount;
        ReceiptNo = receiptNo;
        Description = description;
        DueDate = dueDate;
        Status = PaymentReceiptStatus.Pending;
    }

    /// <summary>Student uploads proof of payment, marking the receipt as Submitted.</summary>
    public void SubmitProof(string proofPath)
    {
        if (Status != PaymentReceiptStatus.Pending)
            throw new InvalidOperationException("Only Pending receipts can have proof submitted.");

        ProofOfPaymentPath = proofPath;
        ProofUploadedAt = DateTime.UtcNow;
        Status = PaymentReceiptStatus.Submitted;
        Touch();
    }

    /// <summary>Finance edits receipt details while it is still actionable.</summary>
    public void UpdateDetails(decimal amount, string receiptNo, string description, DateTime dueDate, string? notes = null)
    {
        if (Status is PaymentReceiptStatus.Paid or PaymentReceiptStatus.Cancelled)
            throw new InvalidOperationException("Paid or Cancelled receipts cannot be edited.");

        Amount = amount;
        ReceiptNo = receiptNo;
        Description = description;
        DueDate = dueDate;
        Notes = notes;
        Touch();
    }

    /// <summary>Finance confirms payment received, marking the receipt as Paid (final state).</summary>
    public void ConfirmPayment(Guid confirmedByUserId, string? notes = null)
    {
        if (Status == PaymentReceiptStatus.Paid)
        {
            if (!string.IsNullOrWhiteSpace(notes))
                Notes = notes;
            Touch();
            return;
        }

        if (Status != PaymentReceiptStatus.Submitted && Status != PaymentReceiptStatus.Pending)
            throw new InvalidOperationException("Only Pending or Submitted receipts can be confirmed as Paid.");

        Status = PaymentReceiptStatus.Paid;
        ConfirmedByUserId = confirmedByUserId;
        ConfirmedAt = DateTime.UtcNow;
        Notes = notes;
        Touch();
    }

    /// <summary>Finance cancels the receipt (e.g., if issued in error). Final state.</summary>
    public void Cancel(Guid cancelledByUserId, string? reason = null)
    {
        Status = PaymentReceiptStatus.Cancelled;
        ConfirmedByUserId = cancelledByUserId;
        Notes = reason;
        Touch();
    }
}
