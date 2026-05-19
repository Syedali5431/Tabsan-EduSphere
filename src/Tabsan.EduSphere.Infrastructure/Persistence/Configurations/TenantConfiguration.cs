using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Tenancy;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code)
               .IsRequired()
               .HasMaxLength(64);

        builder.Property(t => t.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(t => t.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(t => t.RowVersion)
               .IsRowVersion();

        builder.HasIndex(t => t.Code)
               .IsUnique()
               .HasDatabaseName("IX_tenants_code");

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
