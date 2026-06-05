using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Permissions;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the role_resource_permissions table.</summary>
public class RoleResourcePermissionConfiguration : IEntityTypeConfiguration<RoleResourcePermission>
{
    public void Configure(EntityTypeBuilder<RoleResourcePermission> builder)
    {
        builder.ToTable("role_resource_permissions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RoleName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ResourceKey).IsRequired().HasMaxLength(100);

        builder.Property(x => x.CanView).IsRequired();
        builder.Property(x => x.CanAdd).IsRequired();
        builder.Property(x => x.CanEdit).IsRequired();
        builder.Property(x => x.CanDeactivate).IsRequired();
        builder.Property(x => x.CanExport).IsRequired();
        builder.Property(x => x.CanImport).IsRequired();

        builder.HasIndex(x => new { x.RoleName, x.ResourceKey })
               .IsUnique()
               .HasDatabaseName("IX_rrp_role_resource");
    }
}
