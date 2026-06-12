using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Academic;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// Phase 26 — Stage 26.2

public class BulkPromotionBatchConfiguration : IEntityTypeConfiguration<BulkPromotionBatch>
{
    public void Configure(EntityTypeBuilder<BulkPromotionBatch> builder)
    {
        builder.ToTable("bulk_promotion_batches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(180);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.ReviewNote).HasMaxLength(1000);

        // Entries are managed as a separate aggregate via the repository pattern.
        // The domain model's _entries list is populated at runtime via AddEntry().
        // EF is configured to ignore the navigation to avoid field/property conflicts.
        builder.Ignore(x => x.Entries);

        builder.HasIndex(x => new { x.Status, x.CreatedAt })
            .HasDatabaseName("IX_bulk_promotion_batches_status_created");
    }
}
