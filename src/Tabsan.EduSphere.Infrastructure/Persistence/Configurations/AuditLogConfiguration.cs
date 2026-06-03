using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Auditing;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the append-only AuditLog table.</summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        // Use a database-generated bigint for the clustered PK.
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
               .ValueGeneratedOnAdd();

        builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
       builder.Property(a => a.ActorRole).HasMaxLength(64);
        builder.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.EntityId).HasMaxLength(100);
        builder.Property(a => a.IpAddress).HasMaxLength(64);
       builder.Property(a => a.UserAgent).HasMaxLength(1024);

        // Time-range queries on audit logs — clustered by OccurredAt.
        builder.HasIndex(a => a.OccurredAt)
               .HasDatabaseName("IX_audit_logs_occurred_at");

        // Filter by actor for per-user audit trail.
        builder.HasIndex(a => a.ActorUserId)
               .HasDatabaseName("IX_audit_logs_actor");

        // Optional role filter for compliance-focused investigations.
        builder.HasIndex(a => a.ActorRole)
               .HasDatabaseName("IX_audit_logs_actor_role");

        // Composite for entity-type audit trail pages (filter by EntityName, order by OccurredAt).
        builder.HasIndex(a => new { a.EntityName, a.OccurredAt })
               .HasDatabaseName("IX_audit_logs_entity_occurred_at");
    }
}
