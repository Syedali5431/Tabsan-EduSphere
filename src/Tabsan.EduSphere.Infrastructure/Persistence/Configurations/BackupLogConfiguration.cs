using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Backup;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

/// <summary>Phase 4: EF Core configuration for the backup_logs table.</summary>
public class BackupLogConfiguration : IEntityTypeConfiguration<BackupLog>
{
    public void Configure(EntityTypeBuilder<BackupLog> builder)
    {
        builder.ToTable("backup_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BackupType).IsRequired().HasMaxLength(20);
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(500);
        builder.Property(x => x.FilePath).HasMaxLength(1000);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.Checksum).HasMaxLength(128);
        builder.Property(x => x.InitiatedBy).HasMaxLength(100);

        // Filter by status over time for monitoring dashboard
        builder.HasIndex(x => new { x.Status, x.StartedAt })
               .HasDatabaseName("IX_backup_logs_status_started");

        // Filter by backup type over time
        builder.HasIndex(x => new { x.BackupType, x.StartedAt })
               .HasDatabaseName("IX_backup_logs_type_started");
    }
}
