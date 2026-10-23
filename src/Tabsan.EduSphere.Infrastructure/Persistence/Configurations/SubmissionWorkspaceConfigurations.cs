using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Assignments;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// Plan M3 — submission history, files, comments, annotations, provisional grades.

internal sealed class SubmissionAttemptConfiguration : IEntityTypeConfiguration<SubmissionAttempt>
{
    public void Configure(EntityTypeBuilder<SubmissionAttempt> builder)
    {
        builder.ToTable("submission_attempts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.TextContent).HasMaxLength(8000);
        builder.HasIndex(a => new { a.SubmissionId, a.AttemptNumber }).IsUnique().HasDatabaseName("IX_submission_attempts_submission_number");

        builder.HasMany(a => a.Files)
               .WithOne()
               .HasForeignKey(f => f.AttemptId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(a => a.Files).UsePropertyAccessMode(PropertyAccessMode.Field).HasField("_files");
    }
}

internal sealed class SubmissionFileConfiguration : IEntityTypeConfiguration<SubmissionFile>
{
    public void Configure(EntityTypeBuilder<SubmissionFile> builder)
    {
        builder.ToTable("submission_files");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.Property(f => f.FileName).IsRequired().HasMaxLength(260);
        builder.Property(f => f.ContentType).IsRequired().HasMaxLength(200);
        builder.Property(f => f.StorageKey).HasMaxLength(500);
        builder.Property(f => f.LegacyUrl).HasMaxLength(2048);
        builder.Property(f => f.Sha256).HasMaxLength(64);
        builder.Property(f => f.PdfStorageKey).HasMaxLength(500);
        builder.Property(f => f.ConversionError).HasMaxLength(1000);
        builder.Property(f => f.ConversionStatus).HasConversion<string>().HasMaxLength(20);
        builder.Ignore(f => f.Extension);
        builder.Ignore(f => f.IsPdf);
        builder.Ignore(f => f.IsImage);
        builder.Ignore(f => f.ViewerKind);
        builder.HasIndex(f => f.AttemptId).HasDatabaseName("IX_submission_files_attempt");
        // The conversion worker polls by status.
        builder.HasIndex(f => new { f.ConversionStatus, f.ConversionStartedAt }).HasDatabaseName("IX_submission_files_conversion");
    }
}

internal sealed class SubmissionCommentConfiguration : IEntityTypeConfiguration<SubmissionComment>
{
    public void Configure(EntityTypeBuilder<SubmissionComment> builder)
    {
        builder.ToTable("submission_comments");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Body).HasMaxLength(4000);
        builder.Property(c => c.MediaStorageKey).HasMaxLength(500);
        builder.Property(c => c.MediaContentType).HasMaxLength(200);
        builder.Property(c => c.MediaFileName).HasMaxLength(260);
        builder.HasOne<AssignmentSubmission>().WithMany().HasForeignKey(c => c.SubmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => new { c.SubmissionId, c.CreatedAt }).HasDatabaseName("IX_submission_comments_submission");
    }
}

internal sealed class SubmissionAnnotationConfiguration : IEntityTypeConfiguration<SubmissionAnnotation>
{
    public void Configure(EntityTypeBuilder<SubmissionAnnotation> builder)
    {
        builder.ToTable("submission_annotations");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.GeometryJson).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(a => a.Comment).HasMaxLength(2000);
        builder.Property(a => a.Color).IsRequired().HasMaxLength(9);
        builder.HasOne<SubmissionFile>().WithMany().HasForeignKey(a => a.SubmissionFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.SubmissionFileId, a.PageNumber }).HasDatabaseName("IX_submission_annotations_file_page");
    }
}

internal sealed class ProvisionalGradeConfiguration : IEntityTypeConfiguration<ProvisionalGrade>
{
    public void Configure(EntityTypeBuilder<ProvisionalGrade> builder)
    {
        builder.ToTable("submission_provisional_grades");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();
        builder.Property(g => g.Marks).HasColumnType("decimal(8,2)");
        builder.Property(g => g.Feedback).HasMaxLength(2000);
        builder.HasOne<AssignmentSubmission>().WithMany().HasForeignKey(g => g.SubmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(g => new { g.SubmissionId, g.GraderUserId }).IsUnique().HasDatabaseName("IX_submission_provisional_grades_grader");
    }
}
