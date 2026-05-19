using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Tenancy;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the Department entity.</summary>
public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");

              builder.ToTable(t => t.HasCheckConstraint(
                     "CK_departments_tenant_campus_pair",
                     "([TenantId] IS NULL AND [CampusId] IS NULL) OR ([TenantId] IS NOT NULL AND [CampusId] IS NOT NULL)"));

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(d => d.Code)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(d => d.TenantId)
               .IsRequired(false);

        builder.Property(d => d.CampusId)
               .IsRequired(false);

        builder.Property(d => d.InstitutionType)
               .HasConversion<int>()
               .HasDefaultValue(InstitutionType.University)
               .IsRequired();

        builder.Property(d => d.RowVersion).IsRowVersion();

        // Unique department code used throughout the system.
        builder.HasIndex(d => d.Code)
               .IsUnique()
               .HasDatabaseName("IX_departments_code");

        builder.HasIndex(d => d.InstitutionType)
               .HasDatabaseName("IX_departments_institution_type");

        builder.HasIndex(d => d.TenantId)
               .HasDatabaseName("IX_departments_tenant_id");

        builder.HasIndex(d => d.CampusId)
               .HasDatabaseName("IX_departments_campus_id");

        builder.HasOne<Tenant>()
               .WithMany()
               .HasForeignKey(d => d.TenantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Campus>()
               .WithMany()
               .HasForeignKey(d => new { d.CampusId, d.TenantId })
               .HasPrincipalKey(c => new { c.Id, c.TenantId })
               .OnDelete(DeleteBehavior.Restrict);

        // Soft-delete filter — normal queries never see deleted departments.
        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}
