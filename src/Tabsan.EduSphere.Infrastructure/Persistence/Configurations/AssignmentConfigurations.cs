using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Assignments;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the Assignment entity.</summary>
public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("assignments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).IsRequired().HasMaxLength(300);
        builder.Property(a => a.Description).HasMaxLength(4000);
        builder.Property(a => a.MaxMarks).HasColumnType("decimal(8,2)");
        builder.Property(a => a.RowVersion).IsRowVersion();

        // Index for fast listing of assignments per offering.
        builder.HasIndex(a => a.CourseOfferingId)
               .HasDatabaseName("IX_assignments_offering_id");

        // Composite covering index: narrows "published assignments for offering" in one seek.
        builder.HasIndex(a => new { a.CourseOfferingId, a.IsPublished })
               .HasDatabaseName("IX_assignments_offering_published");

        // Soft-delete filter — retracted/deleted assignments are hidden from normal queries.
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}

/// <summary>EF Core configuration for the AssignmentSubmission entity.</summary>
public class AssignmentSubmissionConfiguration : IEntityTypeConfiguration<AssignmentSubmission>
{
    public void Configure(EntityTypeBuilder<AssignmentSubmission> builder)
    {
        builder.ToTable("assignment_submissions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.FileUrl).HasMaxLength(2048);
        builder.Property(s => s.TextContent).HasMaxLength(8000);
        builder.Property(s => s.Feedback).HasMaxLength(2000);
        builder.Property(s => s.MarksAwarded).HasColumnType("decimal(8,2)");
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(32);

        // One submission per student per assignment — the unique index enforces the business rule.
        builder.HasIndex(s => new { s.AssignmentId, s.StudentProfileId })
               .IsUnique()
               .HasDatabaseName("IX_assignment_submissions_assignment_student");

         // Stage 5.2 — analytics aggregate paths frequently group by assignment and submission status.
         builder.HasIndex(s => new { s.AssignmentId, s.Status })
             .HasDatabaseName("IX_assignment_submissions_assignment_status");

         // Stage 5.2 — per-student submission analytics and history rollups.
         builder.HasIndex(s => s.StudentProfileId)
             .HasDatabaseName("IX_assignment_submissions_student_id");

        builder.HasOne(s => s.Assignment)
               .WithMany()
               .HasForeignKey(s => s.AssignmentId)
               .OnDelete(DeleteBehavior.Restrict);

        // Match principal Assignment soft-delete filter to avoid required-relationship filter warnings.
        builder.HasQueryFilter(s => !s.Assignment.IsDeleted);

        // No soft-delete filter — submissions are permanent academic evidence.
    }
}

/// <summary>EF Core configuration for the Result entity.</summary>
public class ResultConfiguration : IEntityTypeConfiguration<Result>
{
    public void Configure(EntityTypeBuilder<Result> builder)
    {
        builder.ToTable("results");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.MarksObtained).HasColumnType("decimal(8,2)");
        builder.Property(r => r.MaxMarks).HasColumnType("decimal(8,2)");
        builder.Property(r => r.ResultType).IsRequired().HasMaxLength(100);
        builder.Property(r => r.GradePoint).HasColumnType("decimal(4,2)");
        builder.Property(r => r.RowVersion).IsRowVersion();

        // At most one result per (student, offering, type) — prevents duplicate entries.
        builder.HasIndex(r => new { r.StudentProfileId, r.CourseOfferingId, r.ResultType })
               .IsUnique()
               .HasDatabaseName("IX_results_student_offering_type");

        // Index for fast per-offering queries (faculty publishes results for the whole class).
        builder.HasIndex(r => r.CourseOfferingId)
               .HasDatabaseName("IX_results_offering_id");

         // Stage 5.2 — analytics query paths filter published results by offering and recency windows.
         builder.HasIndex(r => new { r.CourseOfferingId, r.IsPublished })
             .HasDatabaseName("IX_results_offering_published");

         builder.HasIndex(r => new { r.IsPublished, r.PublishedAt })
             .HasDatabaseName("IX_results_published_published_at");

        // No soft-delete filter — results must remain for audit and transcript purposes.
    }
}

/// <summary>EF Core configuration for the TranscriptExportLog entity.</summary>
public class TranscriptExportLogConfiguration : IEntityTypeConfiguration<TranscriptExportLog>
{
    public void Configure(EntityTypeBuilder<TranscriptExportLog> builder)
    {
        builder.ToTable("transcript_export_logs");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Format).IsRequired().HasMaxLength(10);
        builder.Property(t => t.DocumentUrl).HasMaxLength(2048);
        builder.Property(t => t.IpAddress).HasMaxLength(45);

        // Index for fast per-student history lookups.
        builder.HasIndex(t => t.StudentProfileId)
               .HasDatabaseName("IX_transcript_export_logs_student_id");

        // No soft-delete — logs are append-only for compliance.
    }
}
