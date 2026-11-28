using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// Course templates (offerings shared for others to copy) and the history of copies into each offering.

internal sealed class CourseTemplateConfiguration : IEntityTypeConfiguration<CourseTemplate>
{
    public void Configure(EntityTypeBuilder<CourseTemplate> b)
    {
        b.ToTable("course_templates");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Name).IsRequired().HasMaxLength(CourseTemplate.MaxNameLength);
        b.Property(x => x.Description).HasMaxLength(CourseTemplate.MaxDescriptionLength);
        b.Property(x => x.Scope).HasConversion<byte>();
        b.HasIndex(x => x.OfferingId).IsUnique().HasDatabaseName("IX_course_templates_offering");
        b.HasOne<CourseOffering>().WithMany().HasForeignKey(x => x.OfferingId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CourseCopyRunConfiguration : IEntityTypeConfiguration<CourseCopyRun>
{
    public void Configure(EntityTypeBuilder<CourseCopyRun> b)
    {
        b.ToTable("course_copy_runs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.SummaryJson).IsRequired().HasMaxLength(CourseCopyRun.MaxSummaryLength);
        b.HasIndex(x => new { x.TargetOfferingId, x.CreatedAt }).HasDatabaseName("IX_course_copy_runs_target_created");
        // The source may be deleted later; its copies keep their history.
        b.HasOne<CourseOffering>().WithMany().HasForeignKey(x => x.TargetOfferingId).OnDelete(DeleteBehavior.Cascade);
    }
}
