using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Fyp;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the <see cref="FypProject"/> entity.</summary>
internal sealed class FypProjectConfiguration : IEntityTypeConfiguration<FypProject>
{
    public void Configure(EntityTypeBuilder<FypProject> builder)
    {
        builder.ToTable("fyp_projects");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.Property(p => p.Title).HasMaxLength(500).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(8000).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.CoordinatorRemarks).HasMaxLength(2000);
        builder.Property(p => p.FypGradePoint).HasColumnType("decimal(5,2)");
        builder.Property(p => p.FypMarks).HasColumnType("decimal(7,2)");
        builder.Property(p => p.FypMaxMarks).HasColumnType("decimal(7,2)");

        builder.HasIndex(p => p.StudentProfileId);
        builder.HasIndex(p => new { p.DepartmentId, p.Status });
        builder.HasIndex(p => p.SupervisorUserId);
    }
}

/// <summary>EF Core configuration for the <see cref="FypPanelMember"/> entity.</summary>
internal sealed class FypPanelMemberConfiguration : IEntityTypeConfiguration<FypPanelMember>
{
    public void Configure(EntityTypeBuilder<FypPanelMember> builder)
    {
        builder.ToTable("fyp_panel_members");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.RowVersion).IsRowVersion();

        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(m => m.Project)
               .WithMany(p => p.PanelMembers)
               .HasForeignKey(m => m.FypProjectId)
               .OnDelete(DeleteBehavior.Cascade);

        // One role per user per project
        builder.HasIndex(m => new { m.FypProjectId, m.UserId, m.Role }).IsUnique();
        builder.HasIndex(m => m.UserId);
    }
}

/// <summary>EF Core configuration for the <see cref="FypMeeting"/> entity.</summary>
internal sealed class FypMeetingConfiguration : IEntityTypeConfiguration<FypMeeting>
{
    public void Configure(EntityTypeBuilder<FypMeeting> builder)
    {
        builder.ToTable("fyp_meetings");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.RowVersion).IsRowVersion();

        builder.Property(m => m.Venue).HasMaxLength(500).IsRequired();
        builder.Property(m => m.Agenda).HasMaxLength(4000);
        builder.Property(m => m.Minutes).HasMaxLength(8000);
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(m => m.Project)
               .WithMany(p => p.Meetings)
               .HasForeignKey(m => m.FypProjectId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.FypProjectId, m.ScheduledAt });
        builder.HasIndex(m => new { m.OrganiserUserId, m.Status });
    }
}
