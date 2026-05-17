using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Licensing;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the LicenseState table.</summary>
public class LicenseStateConfiguration : IEntityTypeConfiguration<LicenseState>
{
    public void Configure(EntityTypeBuilder<LicenseState> builder)
    {
        builder.ToTable("license_state");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.LicenseHash)
               .IsRequired()
               .HasMaxLength(128);

        // Store enum values as strings for readability in the database.
        builder.Property(l => l.LicenseType)
                     .HasConversion(
                            v => v.ToString(),
                            v => ParseLicenseType(v));

        builder.Property(l => l.Status)
               .HasConversion<string>();

        // P2-S1-01 / P2-S2-01: MaxUsers — 0 means unlimited.
        builder.Property(l => l.MaxUsers)
               .HasDefaultValue(0);

        // P2-S3-01 / P2-S3-02: ActivatedDomain — null until first activation.
        builder.Property(l => l.ActivatedDomain)
               .HasMaxLength(253) // max valid DNS name length
               .IsRequired(false);
    }

       private static LicenseType ParseLicenseType(string? raw)
       {
              // Backward compatibility: older rows may use legacy text values.
              if (string.Equals(raw, "Education", StringComparison.OrdinalIgnoreCase))
              {
                     return LicenseType.Yearly;
              }

              return Enum.TryParse<LicenseType>(raw, ignoreCase: true, out var parsed)
                     ? parsed
                     : LicenseType.Yearly;
       }
}
