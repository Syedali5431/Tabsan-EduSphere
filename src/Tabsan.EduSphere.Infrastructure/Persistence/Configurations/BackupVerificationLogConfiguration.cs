using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Backup;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

public class BackupVerificationLogConfiguration : IEntityTypeConfiguration<BackupVerificationLog>
{
    public void Configure(EntityTypeBuilder<BackupVerificationLog> builder)
    {
        builder.ToTable("backup_verification_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VerificationType).IsRequired().HasMaxLength(20);
        builder.Property(x => x.VerifiedAt).IsRequired();
        builder.Property(x => x.VerifiedBy).HasMaxLength(100);
        builder.Property(x => x.Issues).HasMaxLength(2000);
        builder.Property(x => x.VerifiedChecksum).HasMaxLength(128);

        builder.HasIndex(x => new { x.BackupLogId, x.VerifiedAt }).HasDatabaseName("IX_backup_verification_backup_verified");
        builder.HasIndex(x => new { x.IsSuccessful, x.VerifiedAt }).HasDatabaseName("IX_backup_verification_success_verified");
    }
}
