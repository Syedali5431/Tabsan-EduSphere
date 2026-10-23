using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Outcomes;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// Plan M6 — learning outcomes, mappings, alignments and mastery evidence. Keys are not store-generated.

internal sealed class OutcomeConfiguration : IEntityTypeConfiguration<Outcome>
{
    public void Configure(EntityTypeBuilder<Outcome> b)
    {
        b.ToTable("outcomes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Code).IsRequired().HasMaxLength(40);
        b.Property(x => x.Title).IsRequired().HasMaxLength(300);
        b.Property(x => x.Description).HasMaxLength(4000);
        b.Property(x => x.Level).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.CalculationMethod).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.MasteryPoints).HasPrecision(9, 2);
        b.Ignore(x => x.MaxPoints);
        b.HasIndex(x => new { x.TenantId, x.Level, x.ProgramId, x.CourseId, x.Code }).HasDatabaseName("IX_outcomes_scope_code");
        b.HasIndex(x => x.CourseId).HasDatabaseName("IX_outcomes_course");
        b.HasIndex(x => x.ProgramId).HasDatabaseName("IX_outcomes_program");
        b.HasMany(x => x.Ratings).WithOne().HasForeignKey(r => r.OutcomeId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Ratings).UsePropertyAccessMode(PropertyAccessMode.Field).HasField("_ratings");
    }
}

internal sealed class OutcomeRatingLevelConfiguration : IEntityTypeConfiguration<OutcomeRatingLevel>
{
    public void Configure(EntityTypeBuilder<OutcomeRatingLevel> b)
    {
        b.ToTable("outcome_rating_levels");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Description).IsRequired().HasMaxLength(100);
        b.Property(x => x.Points).HasPrecision(9, 2);
    }
}

internal sealed class OutcomeMappingConfiguration : IEntityTypeConfiguration<OutcomeMapping>
{
    public void Configure(EntityTypeBuilder<OutcomeMapping> b)
    {
        b.ToTable("outcome_mappings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => new { x.CourseOutcomeId, x.ProgramOutcomeId }).IsUnique().HasDatabaseName("IX_outcome_mappings_pair");
        b.HasIndex(x => x.ProgramOutcomeId).HasDatabaseName("IX_outcome_mappings_program_outcome");
        b.HasOne<Outcome>().WithMany().HasForeignKey(x => x.CourseOutcomeId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Outcome>().WithMany().HasForeignKey(x => x.ProgramOutcomeId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class OutcomeAlignmentConfiguration : IEntityTypeConfiguration<OutcomeAlignment>
{
    public void Configure(EntityTypeBuilder<OutcomeAlignment> b)
    {
        b.ToTable("outcome_alignments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TargetType).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => new { x.OutcomeId, x.TargetType, x.TargetId }).IsUnique().HasDatabaseName("IX_outcome_alignments_outcome_target");
        b.HasIndex(x => new { x.TargetType, x.TargetId }).HasDatabaseName("IX_outcome_alignments_target");
        b.HasIndex(x => x.CourseOfferingId).HasDatabaseName("IX_outcome_alignments_offering");
        b.HasOne<Outcome>().WithMany().HasForeignKey(x => x.OutcomeId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class StudentOutcomeResultConfiguration : IEntityTypeConfiguration<StudentOutcomeResult>
{
    public void Configure(EntityTypeBuilder<StudentOutcomeResult> b)
    {
        b.ToTable("student_outcome_results");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Percent).HasPrecision(9, 4);
        b.Property(x => x.Score).HasPrecision(9, 2);
        b.HasIndex(x => new { x.StudentProfileId, x.AlignmentId, x.SourceId }).IsUnique().HasDatabaseName("IX_student_outcome_results_unique");
        b.HasIndex(x => new { x.CourseOfferingId, x.StudentProfileId }).HasDatabaseName("IX_student_outcome_results_offering_student");
        b.HasIndex(x => new { x.OutcomeId, x.StudentProfileId }).HasDatabaseName("IX_student_outcome_results_outcome_student");
        // Removing an alignment removes its evidence.
        b.HasOne<OutcomeAlignment>().WithMany().HasForeignKey(x => x.AlignmentId).OnDelete(DeleteBehavior.Cascade);
    }
}
