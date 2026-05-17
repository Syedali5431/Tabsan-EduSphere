using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.StudyPlanner;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// Final-Touches Phase 21 Stage 21.1 — EF configuration for StudyPlan and StudyPlanCourse

/// <summary>EF Core configuration for the <see cref="StudyPlan"/> aggregate root.</summary>
public class StudyPlanConfiguration : IEntityTypeConfiguration<StudyPlan>
{
    public void Configure(EntityTypeBuilder<StudyPlan> builder)
    {
        builder.ToTable("study_plans");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PlannedSemesterName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.AdvisorStatus).IsRequired();
        builder.Property(p => p.AdvisorNotes).HasMaxLength(2000);
        builder.Property(p => p.RowVersion).IsRowVersion();

        // FK: student_profiles (Cascade — deleting a profile removes its plans)
        builder.HasOne<Domain.Academic.StudentProfile>()
               .WithMany()
               .HasForeignKey(p => p.StudentProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        // One-to-many with plan courses
        builder.HasMany(p => p.Courses)
               .WithOne()
               .HasForeignKey(c => c.StudyPlanId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}

/// <summary>EF Core configuration for the <see cref="StudyPlanCourse"/> line item.</summary>
public class StudyPlanCourseConfiguration : IEntityTypeConfiguration<StudyPlanCourse>
{
    public void Configure(EntityTypeBuilder<StudyPlanCourse> builder)
    {
        builder.ToTable("study_plan_courses");
        builder.HasKey(c => c.Id);

        // Unique constraint: one course per plan
        builder.HasIndex(c => new { c.StudyPlanId, c.CourseId })
               .IsUnique()
               .HasDatabaseName("UQ_study_plan_courses_plan_course");

        // FK: courses (Restrict — prevent accidental course deletion while planned)
        builder.HasOne(c => c.Course)
               .WithMany()
               .HasForeignKey(c => c.CourseId)
               .OnDelete(DeleteBehavior.Restrict);

        // Match principal Course soft-delete filter to avoid required-relationship filter warnings.
        builder.HasQueryFilter(c => !c.Course.IsDeleted);
    }
}
