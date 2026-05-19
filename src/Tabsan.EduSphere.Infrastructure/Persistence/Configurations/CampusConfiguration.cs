using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Tenancy;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

public class CampusConfiguration : IEntityTypeConfiguration<Campus>
{
    public void Configure(EntityTypeBuilder<Campus> builder)
    {
        builder.ToTable("campuses");

        builder.HasKey(c => c.Id);

       builder.HasAlternateKey(c => new { c.Id, c.TenantId });

        builder.Property(c => c.TenantId)
               .IsRequired();

        builder.Property(c => c.Code)
               .IsRequired()
               .HasMaxLength(64);

        builder.Property(c => c.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(c => c.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(c => c.RowVersion)
               .IsRowVersion();

        builder.HasIndex(c => new { c.TenantId, c.Code })
               .IsUnique()
               .HasDatabaseName("IX_campuses_tenant_code");

        builder.HasIndex(c => c.TenantId)
               .HasDatabaseName("IX_campuses_tenant_id");

        builder.HasOne<Tenant>()
               .WithMany()
               .HasForeignKey(c => c.TenantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
