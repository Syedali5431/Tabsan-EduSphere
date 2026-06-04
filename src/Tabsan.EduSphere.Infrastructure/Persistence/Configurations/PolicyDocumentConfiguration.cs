using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Documents;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

public class PolicyDocumentConfiguration : IEntityTypeConfiguration<PolicyDocument>
{
    public void Configure(EntityTypeBuilder<PolicyDocument> builder)
    {
        builder.ToTable("policy_documents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Content).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Category).IsRequired().HasMaxLength(50);
        builder.Property(x => x.AccessLevel).IsRequired().HasMaxLength(20);

        builder.HasIndex(x => new { x.Status, x.Category }).HasDatabaseName("IX_policy_documents_status_category");
        builder.HasIndex(x => x.AccessLevel).HasDatabaseName("IX_policy_documents_access_level");
    }
}

public class PolicyDocumentVersionConfiguration : IEntityTypeConfiguration<PolicyDocumentVersion>
{
    public void Configure(EntityTypeBuilder<PolicyDocumentVersion> builder)
    {
        builder.ToTable("policy_document_versions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Content).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(x => x.ChangeNotes).HasMaxLength(500);

        builder.HasIndex(x => new { x.DocumentId, x.VersionNumber }).IsUnique().HasDatabaseName("IX_policy_doc_versions_doc_version");
    }
}
