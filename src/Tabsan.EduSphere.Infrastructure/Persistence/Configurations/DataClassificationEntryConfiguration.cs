using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.DataProtection;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

/// <summary>Phase 5: EF Core configuration for the data_classification_entries table.</summary>
public class DataClassificationEntryConfiguration : IEntityTypeConfiguration<DataClassificationEntry>
{
    public void Configure(EntityTypeBuilder<DataClassificationEntry> builder)
    {
        builder.ToTable("data_classification_entries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.EntityId).HasMaxLength(100);
        builder.Property(x => x.ClassificationLevel).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ClassifiedBy).IsRequired();
        builder.Property(x => x.ClassifiedAt).IsRequired();
        builder.Property(x => x.Justification).HasMaxLength(500);

        // Lookup classifications for a specific entity
        builder.HasIndex(x => new { x.EntityName, x.EntityId })
               .HasDatabaseName("IX_data_classification_entity");

        // Filter by classification level
        builder.HasIndex(x => new { x.ClassificationLevel, x.ClassifiedAt })
               .HasDatabaseName("IX_data_classification_level_classified");
    }
}
