using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Activity;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

/// <summary>Phase 3: EF Core configuration for the append-only LoginActivityLog table.</summary>
public class LoginActivityLogConfiguration : IEntityTypeConfiguration<LoginActivityLog>
{
    public void Configure(EntityTypeBuilder<LoginActivityLog> builder)
    {
        builder.ToTable("login_activity_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired(false);
        builder.Property(x => x.Username).IsRequired().HasMaxLength(100);
        builder.Property(x => x.AttemptedAt).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(1024);
        builder.Property(x => x.DeviceInfo).HasMaxLength(1024);
        builder.Property(x => x.IsSuccess).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(50);
        builder.Property(x => x.RiskLevel).HasMaxLength(20);

        // Filter by user for per-user activity history.
        builder.HasIndex(x => x.UserId)
               .HasDatabaseName("IX_login_activity_user_id");

        // Time-range queries for dashboard trends.
        builder.HasIndex(x => x.AttemptedAt)
               .HasDatabaseName("IX_login_activity_attempted_at");

        // Filter by success/failure status with time ordering.
        builder.HasIndex(x => new { x.IsSuccess, x.AttemptedAt })
               .HasDatabaseName("IX_login_activity_success_attempted");

        // IP-based pattern detection.
        builder.HasIndex(x => new { x.IpAddress, x.AttemptedAt })
               .HasDatabaseName("IX_login_activity_ip_attempted");
    }
}
