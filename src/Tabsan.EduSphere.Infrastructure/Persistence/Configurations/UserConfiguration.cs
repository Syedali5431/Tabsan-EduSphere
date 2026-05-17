using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Identity;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core fluent configuration for the User entity.
/// Defines table name, column constraints, indexes, and relationships.
/// Separating configuration from the entity keeps the Domain layer clean.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(u => u.Email)
               .HasMaxLength(256);

        builder.Property(u => u.PasswordHash)
               .IsRequired()
               .HasMaxLength(512);

        builder.Property(u => u.RowVersion)
               .IsRowVersion();

        // Account lockout fields
        builder.Property(u => u.FailedLoginAttempts)
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(u => u.IsLockedOut)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(u => u.LockedOutUntil)
               .IsRequired(false);

        // Filtered index on locked accounts — fast admin queries for locked users.
        builder.HasIndex(u => u.IsLockedOut)
               .HasFilter("[is_locked_out] = 1")
               .HasDatabaseName("IX_users_is_locked_out");

        // Unique index on username — login lookups always go through this index.
        builder.HasIndex(u => u.Username)
               .IsUnique()
               .HasDatabaseName("IX_users_username");

        // Filtered unique index on email — allows multiple null values (users without email).
        builder.HasIndex(u => u.Email)
               .IsUnique()
               .HasFilter("[email] IS NOT NULL")
               .HasDatabaseName("IX_users_email");

        // Many users belong to one role; role is required.
        builder.HasOne(u => u.Role)
               .WithMany()
               .HasForeignKey(u => u.RoleId)
               .OnDelete(DeleteBehavior.Restrict);

        // Phase 9: per-user theme preference — nullable, max 50 chars.
        builder.Property(u => u.ThemeKey)
               .HasMaxLength(50)
               .IsRequired(false);

        // Phase 4 (P4-S2-02): force password change on first login after CSV import.
        builder.Property(u => u.MustChangePassword)
               .IsRequired()
               .HasDefaultValue(false);

        // Phase 39.3: per-user MFA enrollment state and secrets.
        builder.Property(u => u.MfaIsEnabled)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(u => u.MfaTotpSecret)
               .HasMaxLength(128)
               .IsRequired(false);

        builder.Property(u => u.MfaRecoveryCodesHashJson)
               .HasMaxLength(4000)
               .IsRequired(false);

        // Phase 5: optional per-user institution assignment for manual/CSV provisioning flows.
        builder.Property(u => u.InstitutionType)
               .HasConversion<int?>()
               .IsRequired(false);
    }
}
