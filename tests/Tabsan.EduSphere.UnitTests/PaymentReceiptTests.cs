using FluentAssertions;
using Tabsan.EduSphere.Domain.StudentLifecycle;

namespace Tabsan.EduSphere.UnitTests;

public class PaymentReceiptTests
{
    [Fact]
    public void UpdateDetails_WhenPending_UpdatesFieldsAndTouchesAuditTrail()
    {
        var receipt = new PaymentReceipt(
            studentProfileId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            amount: 100m,
            receiptNo: "UT-001",
            description: "Semester Tuition",
            dueDate: new DateTime(2026, 6, 1));

        receipt.UpdateDetails(
            amount: 125m,
            receiptNo: "UT-002",
            description: "Semester Tuition - Revised",
            dueDate: new DateTime(2026, 6, 15),
            notes: "Updated after finance review");

        receipt.Amount.Should().Be(125m);
        receipt.Description.Should().Be("Semester Tuition - Revised");
        receipt.DueDate.Should().Be(new DateTime(2026, 6, 15));
        receipt.Notes.Should().Be("Updated after finance review");
        receipt.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateDetails_WhenPaid_ThrowsInvalidOperationException()
    {
        var receipt = new PaymentReceipt(
            studentProfileId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            amount: 100m,
            receiptNo: "UT-101",
            description: "Semester Tuition",
            dueDate: new DateTime(2026, 6, 1));

        receipt.ConfirmPayment(Guid.NewGuid(), "paid");

        var act = () => receipt.UpdateDetails(110m, "UT-102", "Revised", new DateTime(2026, 6, 10), "should fail");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be edited*");
    }
}
