using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Incidents;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

public class IncidentLogConfiguration : IEntityTypeConfiguration<IncidentLog>
{
    public void Configure(EntityTypeBuilder<IncidentLog> builder)
    {
        builder.ToTable("incident_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Severity).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Category).IsRequired().HasMaxLength(30);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ReportedAt).IsRequired();
        builder.Property(x => x.Resolution).HasMaxLength(2000);

        builder.HasIndex(x => new { x.Status, x.ReportedAt }).HasDatabaseName("IX_incident_logs_status_reported");
        builder.HasIndex(x => new { x.Severity, x.Status }).HasDatabaseName("IX_incident_logs_severity_status");
        builder.HasIndex(x => x.ReportedBy).HasDatabaseName("IX_incident_logs_reported_by");
    }
}
