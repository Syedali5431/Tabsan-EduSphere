using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.StudentLifecycle;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the PaymentReceipt entity.
/// Indexes applied on StudentProfileId and Status for query performance.
/// </summary>
public class PaymentReceiptConfiguration : IEntityTypeConfiguration<PaymentReceipt>
{
    public void Configure(EntityTypeBuilder<PaymentReceipt> builder)
    {
        builder.ToTable("payment_receipts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.StudentProfileId).IsRequired();
        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.ConfirmedByUserId).IsRequired(false);
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.Amount).IsRequired().HasPrecision(10, 2);
        builder.Property(x => x.ReceiptNo).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DueDate).IsRequired();
        builder.Property(x => x.ProofOfPaymentPath).IsRequired(false).HasMaxLength(500);
        builder.Property(x => x.ProofUploadedAt).IsRequired(false);
        builder.Property(x => x.ConfirmedAt).IsRequired(false);
        builder.Property(x => x.Notes).IsRequired(false).HasMaxLength(2000);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        // Foreign keys
        builder.HasOne(x => x.StudentProfile)
            .WithMany()
            .HasForeignKey(x => x.StudentProfileId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pr_student_profile");

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pr_created_by_user");

        builder.HasOne(x => x.ConfirmedByUser)
            .WithMany()
            .HasForeignKey(x => x.ConfirmedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pr_confirmed_by_user");

        // Indexes
        builder.HasIndex(x => x.StudentProfileId).HasDatabaseName("ix_pr_student_profile_id");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_pr_status");
        builder.HasIndex(x => new { x.StudentProfileId, x.Status }).HasDatabaseName("ix_pr_student_status");
        // Final-Touches Phase 29 Stage 29.1 — support student receipt history and unpaid queues.
        builder.HasIndex(x => new { x.StudentProfileId, x.CreatedAt }).HasDatabaseName("ix_pr_student_created_at");
        builder.HasIndex(x => new { x.Status, x.DueDate }).HasDatabaseName("ix_pr_status_due_date");
        builder.HasIndex(x => x.DueDate).HasDatabaseName("ix_pr_due_date");
        builder.HasIndex(x => x.ReceiptNo).IsUnique().HasDatabaseName("ix_pr_receipt_no");

        // Match principal StudentProfile soft-delete filter to avoid required-relationship filter warnings.
        builder.HasQueryFilter(x => !x.StudentProfile.IsDeleted);
    }
}
