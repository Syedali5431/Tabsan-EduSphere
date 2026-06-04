using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Identity;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for PasswordHistoryEntry.
/// Maintains an indexed, append-only log of hashed passwords per user.
/// </summary>
public class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistoryEntry>
{
    public void Configure(EntityTypeBuilder<PasswordHistoryEntry> builder)
    {
        builder.ToTable("password_history");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
               .IsRequired();

        builder.Property(p => p.PasswordHash)
               .IsRequired()
               .HasMaxLength(512);

        builder.Property(p => p.CreatedAt)
               .IsRequired();

        // Phase 2: Optional archival expiry for automatic history pruning
        builder.Property(p => p.ExpiresAt)
               .IsRequired(false);

        // Index for fast lookup of recent entries per user (used in reuse check).
        builder.HasIndex(p => new { p.UserId, p.CreatedAt })
               .HasDatabaseName("IX_password_history_user_created");
    }
}
