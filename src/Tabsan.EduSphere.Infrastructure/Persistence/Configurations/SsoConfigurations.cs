using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Tenancy;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

/// <summary>Plan M1 Stage M1.1: EF Core configuration for external SSO login links.</summary>
public class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("external_logins");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).IsRequired(false);
        builder.Property(x => x.Provider).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ExternalSubjectId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.LinkedAt).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Tenant>()
               .WithMany()
               .HasForeignKey(x => x.TenantId)
               .OnDelete(DeleteBehavior.Restrict);

        // One external identity maps to exactly one user per tenant. No filter: single-tenant
        // installs (TenantId NULL) must be unique too — SQL Server treats NULLs as equal here.
        builder.HasIndex(x => new { x.TenantId, x.Provider, x.ExternalSubjectId })
               .IsUnique()
               .HasFilter(null)
               .HasDatabaseName("IX_external_logins_tenant_provider_subject");

        // A user may link at most one identity per provider.
        builder.HasIndex(x => new { x.UserId, x.Provider })
               .IsUnique()
               .HasDatabaseName("IX_external_logins_user_provider");
    }
}

/// <summary>Plan M1 Stage M1.1: EF Core configuration for per-tenant SSO provider settings.</summary>
public class SsoProviderSettingConfiguration : IEntityTypeConfiguration<SsoProviderSetting>
{
    public void Configure(EntityTypeBuilder<SsoProviderSetting> builder)
    {
        builder.ToTable("sso_provider_settings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).IsRequired(false);
        builder.Property(x => x.Provider).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.IsEnabled).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.ClientId).HasMaxLength(256);
        builder.Property(x => x.ClientSecretEncrypted).HasMaxLength(2048);
        builder.Property(x => x.Authority).HasMaxLength(500);
        builder.Property(x => x.MetadataUrl).HasMaxLength(500);
        builder.Property(x => x.AllowedEmailDomains).HasMaxLength(1000);
        builder.Property(x => x.AutoLinkByEmail).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.EnforceSsoOnly).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.TrustIdpMfa).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.Ignore(x => x.IsOidc);
        builder.Ignore(x => x.IsComplete);

        builder.HasOne<Tenant>()
               .WithMany()
               .HasForeignKey(x => x.TenantId)
               .OnDelete(DeleteBehavior.Restrict);

        // One live configuration per provider per tenant; soft-deleted rows don't block re-creation.
        builder.HasIndex(x => new { x.TenantId, x.Provider })
               .IsUnique()
               .HasFilter("[IsDeleted] = 0")
               .HasDatabaseName("IX_sso_provider_settings_tenant_provider");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
