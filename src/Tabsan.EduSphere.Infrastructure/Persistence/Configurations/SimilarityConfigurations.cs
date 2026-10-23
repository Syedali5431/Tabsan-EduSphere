using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Similarity;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// Plan M9 — similarity settings, checks, documents and fingerprints. Keys are not store-generated.

internal sealed class SimilaritySettingConfiguration : IEntityTypeConfiguration<SimilaritySetting>
{
    public void Configure(EntityTypeBuilder<SimilaritySetting> b)
    {
        b.ToTable("similarity_settings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Provider).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Scope).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.AccountEmail).HasMaxLength(256);
        b.Property(x => x.ApiKeyEncrypted).HasMaxLength(1000);
        b.Ignore(x => x.IsComplete);
        b.HasIndex(x => x.TenantId).IsUnique().HasDatabaseName("IX_similarity_settings_tenant");
    }
}

internal sealed class SimilarityCheckConfiguration : IEntityTypeConfiguration<SimilarityCheck>
{
    public void Configure(EntityTypeBuilder<SimilarityCheck> b)
    {
        b.ToTable("similarity_checks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Provider).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Score).HasPrecision(5, 1);
        b.Property(x => x.ExternalId).HasMaxLength(100);
        b.Property(x => x.WebhookTokenHash).HasMaxLength(64);
        b.Property(x => x.ReportUrl).HasMaxLength(2000);
        b.Property(x => x.Error).HasMaxLength(1000);
        b.Ignore(x => x.IsFinished);
        b.HasIndex(x => new { x.Status, x.NextAttemptAt }).HasDatabaseName("IX_similarity_checks_queue");
        b.HasIndex(x => new { x.SubmissionId, x.AttemptNumber }).HasDatabaseName("IX_similarity_checks_submission");
        b.HasIndex(x => x.AssignmentId).HasDatabaseName("IX_similarity_checks_assignment");
        b.HasOne<AssignmentSubmission>().WithMany().HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SimilarityDocumentConfiguration : IEntityTypeConfiguration<SimilarityDocument>
{
    public void Configure(EntityTypeBuilder<SimilarityDocument> b)
    {
        b.ToTable("similarity_documents");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Text).IsRequired();
        b.HasIndex(x => new { x.SubmissionId, x.AttemptNumber }).IsUnique().HasDatabaseName("IX_similarity_documents_submission_attempt");
        b.HasIndex(x => x.CourseOfferingId).HasDatabaseName("IX_similarity_documents_offering");
        b.HasIndex(x => x.AssignmentId).HasDatabaseName("IX_similarity_documents_assignment");
        b.HasIndex(x => x.TenantId).HasDatabaseName("IX_similarity_documents_tenant");
        b.HasOne<AssignmentSubmission>().WithMany().HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SimilarityFingerprintConfiguration : IEntityTypeConfiguration<SimilarityFingerprint>
{
    public void Configure(EntityTypeBuilder<SimilarityFingerprint> b)
    {
        b.ToTable("similarity_fingerprints");
        b.HasKey(x => new { x.DocumentId, x.Position });
        b.HasIndex(x => x.Hash).HasDatabaseName("IX_similarity_fingerprints_hash");
        b.HasOne<SimilarityDocument>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
    }
}
