using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Identity;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the UserSession (refresh-token) table.</summary>
public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.RefreshTokenHash)
               .IsRequired()
               .HasMaxLength(512);

        builder.Property(s => s.DeviceInfo).HasMaxLength(512);
        builder.Property(s => s.IpAddress).HasMaxLength(64);

        // Phase 2: Last activity tracking for idle session timeout
        builder.Property(s => s.LastActivityAt)
               .IsRequired(false);

        // Token hash lookup on every refresh request.
        builder.HasIndex(s => s.RefreshTokenHash)
               .IsUnique()
               .HasDatabaseName("IX_user_sessions_token_hash");

        // Find all active sessions for a user (e.g. logout-all).
        builder.HasIndex(s => s.UserId)
               .HasDatabaseName("IX_user_sessions_user_id");

        // Final-Touches Phase 29 Stage 29.1 — support most-recent session lookup per user.
        builder.HasIndex(s => new { s.UserId, s.CreatedAt })
               .HasDatabaseName("IX_user_sessions_user_created_at");

        // Phase 2: Filtered index for active session queries (admin screen, idle timeout enforcement).
        builder.HasIndex(s => s.UserId)
               .IncludeProperties(s => new { s.ExpiresAt, s.RevokedAt })
               .HasFilter("[RevokedAt] IS NULL")
               .HasDatabaseName("IX_user_sessions_active");

        builder.HasOne(s => s.User)
               .WithMany()
               .HasForeignKey(s => s.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
