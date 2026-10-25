using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// Plan M13 — SCORM packages, attempts and the xAPI statement store. Keys are not store-generated.

internal sealed class ScormPackageConfiguration : IEntityTypeConfiguration<ScormPackage>
{
    public void Configure(EntityTypeBuilder<ScormPackage> b)
    {
        b.ToTable("scorm_packages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Title).IsRequired().HasMaxLength(ScormPackage.MaxTitleLength);
        b.Property(x => x.Version).HasConversion<string>().HasMaxLength(12);
        b.Property(x => x.Identifier).HasMaxLength(200);
        b.Property(x => x.LaunchPath).IsRequired().HasMaxLength(1000);
        b.Property(x => x.MasteryScore).HasPrecision(5, 2);
        b.Property(x => x.MaxMarks).HasPrecision(7, 2);
        b.Property(x => x.PartsJson); // nvarchar(max): the parts of a multi-part package
        b.Ignore(x => x.Parts);
        b.Property(x => x.SequencingJson); // nvarchar(max): the SCORM 2004 activity tree
        b.Ignore(x => x.Sequencing);
        b.HasIndex(x => new { x.OfferingId, x.IsRemoved }).HasDatabaseName("IX_scorm_packages_offering");
    }
}

internal sealed class ScormAttemptConfiguration : IEntityTypeConfiguration<ScormAttempt>
{
    public void Configure(EntityTypeBuilder<ScormAttempt> b)
    {
        b.ToTable("scorm_attempts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.CompletionStatus).IsRequired().HasMaxLength(20);
        b.Property(x => x.SuccessStatus).IsRequired().HasMaxLength(20);
        b.Property(x => x.ScoreRaw).HasPrecision(12, 4);
        b.Property(x => x.ScoreMin).HasPrecision(12, 4);
        b.Property(x => x.ScoreMax).HasPrecision(12, 4);
        b.Property(x => x.ScoreScaled).HasPrecision(7, 6);
        b.Property(x => x.ScorePercent).HasPrecision(5, 2);
        b.Property(x => x.Location).HasMaxLength(1000);
        b.Property(x => x.SuspendData).HasMaxLength(ScormAttempt.MaxSuspendDataLength);
        b.Property(x => x.Exit).HasMaxLength(20);
        b.Property(x => x.CmiJson); // nvarchar(max), capped in the entity
        b.Property(x => x.PartsStateJson); // nvarchar(max): each part's data (multi-part packages)
        b.Property(x => x.SequencingStateJson); // nvarchar(max): sequencing state (sequenced packages)
        b.HasIndex(x => new { x.PackageId, x.StudentProfileId, x.Number }).IsUnique().HasDatabaseName("IX_scorm_attempts_package_student_number");
        b.HasIndex(x => x.StudentProfileId).HasDatabaseName("IX_scorm_attempts_student");
    }
}

internal sealed class XapiCredentialConfiguration : IEntityTypeConfiguration<XapiCredential>
{
    public void Configure(EntityTypeBuilder<XapiCredential> b)
    {
        b.ToTable("xapi_credentials");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Key).IsRequired().HasMaxLength(64);
        b.Property(x => x.SecretHash).IsRequired().HasMaxLength(64);
        b.HasIndex(x => x.Key).IsUnique().HasDatabaseName("IX_xapi_credentials_key");
    }
}

internal sealed class XapiStatementConfiguration : IEntityTypeConfiguration<XapiStatement>
{
    public void Configure(EntityTypeBuilder<XapiStatement> b)
    {
        b.ToTable("xapi_statements");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.ActorKey).IsRequired().HasMaxLength(300);
        b.Property(x => x.VerbId).IsRequired().HasMaxLength(1000);
        b.Property(x => x.ObjectId).IsRequired().HasMaxLength(1000);
        b.Property(x => x.ScoreScaled).HasPrecision(7, 6);
        b.Property(x => x.Json).IsRequired();
        b.HasIndex(x => x.StatementId).IsUnique().HasDatabaseName("IX_xapi_statements_statement_id");
        b.HasIndex(x => new { x.ActorKey, x.Stored }).HasDatabaseName("IX_xapi_statements_actor_stored");
        b.HasIndex(x => x.Stored).HasDatabaseName("IX_xapi_statements_stored");
    }
}
