using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Pastoral;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// Pastoral care and wellbeing: notes about students with follow-up notes, support plans, wellbeing check-ins, the
// wellbeing team, categories and each school's settings.

internal sealed class PastoralRecordConfiguration : IEntityTypeConfiguration<PastoralRecord>
{
    public void Configure(EntityTypeBuilder<PastoralRecord> b)
    {
        b.ToTable("pastoral_records");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Kind).HasConversion<byte>();
        b.Property(x => x.Tone).HasConversion<byte>();
        b.Property(x => x.Severity).HasConversion<byte>();
        b.Property(x => x.Visibility).HasConversion<byte>();
        b.Property(x => x.FollowUp).HasConversion<byte>();
        b.Property(x => x.Category).HasMaxLength(PastoralRecord.MaxCategoryLength);
        b.Property(x => x.Summary).IsRequired().HasMaxLength(PastoralRecord.MaxSummaryLength);
        b.Property(x => x.Details).HasMaxLength(PastoralRecord.MaxDetailsLength);
        b.Property(x => x.ActionTaken).HasMaxLength(PastoralRecord.MaxActionLength);
        b.Property(x => x.RemovedReason).HasMaxLength(PastoralRecord.MaxReasonLength);
        b.HasIndex(x => new { x.StudentProfileId, x.OccurredAt }).HasDatabaseName("IX_pastoral_records_student");
        b.HasIndex(x => new { x.TenantId, x.OccurredAt }).HasDatabaseName("IX_pastoral_records_tenant");
        b.HasIndex(x => new { x.FollowUp, x.FollowUpUserId }).HasDatabaseName("IX_pastoral_records_follow_up");
    }
}

internal sealed class PastoralNoteConfiguration : IEntityTypeConfiguration<PastoralNote>
{
    public void Configure(EntityTypeBuilder<PastoralNote> b)
    {
        b.ToTable("pastoral_notes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Text).IsRequired().HasMaxLength(PastoralNote.MaxTextLength);
        b.HasOne<PastoralRecord>().WithMany().HasForeignKey(x => x.RecordId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.RecordId).HasDatabaseName("IX_pastoral_notes_record");
    }
}

internal sealed class SupportPlanConfiguration : IEntityTypeConfiguration<SupportPlan>
{
    public void Configure(EntityTypeBuilder<SupportPlan> b)
    {
        b.ToTable("support_plans");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Kind).HasConversion<byte>();
        b.Property(x => x.Status).HasConversion<byte>();
        b.Property(x => x.Visibility).HasConversion<byte>();
        b.Property(x => x.Title).IsRequired().HasMaxLength(SupportPlan.MaxTitleLength);
        b.Property(x => x.Needs).HasMaxLength(SupportPlan.MaxTextLength);
        b.Property(x => x.Strategies).IsRequired().HasMaxLength(SupportPlan.MaxTextLength);
        b.HasIndex(x => x.StudentProfileId).HasDatabaseName("IX_support_plans_student");
        b.HasIndex(x => new { x.TenantId, x.Status }).HasDatabaseName("IX_support_plans_tenant");
    }
}

internal sealed class WellbeingCheckInConfiguration : IEntityTypeConfiguration<WellbeingCheckIn>
{
    public void Configure(EntityTypeBuilder<WellbeingCheckIn> b)
    {
        b.ToTable("wellbeing_check_ins");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Mood).HasConversion<byte>();
        b.Property(x => x.Note).HasMaxLength(WellbeingCheckIn.MaxNoteLength);
        b.Property(x => x.AcknowledgeNote).HasMaxLength(WellbeingCheckIn.MaxNoteLength);
        b.Ignore(x => x.NeedsAttention);
        b.Ignore(x => x.Urgent);
        b.HasIndex(x => new { x.StudentProfileId, x.CheckedInAt }).HasDatabaseName("IX_wellbeing_check_ins_student");
        b.HasIndex(x => new { x.TenantId, x.CheckedInAt }).HasDatabaseName("IX_wellbeing_check_ins_tenant");
    }
}

internal sealed class PastoralTeamMemberConfiguration : IEntityTypeConfiguration<PastoralTeamMember>
{
    public void Configure(EntityTypeBuilder<PastoralTeamMember> b)
    {
        b.ToTable("pastoral_team_members");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Title).IsRequired().HasMaxLength(PastoralTeamMember.MaxTitleLength);
        b.HasIndex(x => x.UserId).HasDatabaseName("IX_pastoral_team_members_user");
        b.HasIndex(x => x.TenantId).HasDatabaseName("IX_pastoral_team_members_tenant");
    }
}

internal sealed class PastoralCategoryConfiguration : IEntityTypeConfiguration<PastoralCategory>
{
    public void Configure(EntityTypeBuilder<PastoralCategory> b)
    {
        b.ToTable("pastoral_categories");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Kind).HasConversion<byte>();
        b.Property(x => x.Tone).HasConversion<byte>();
        b.Property(x => x.Name).IsRequired().HasMaxLength(PastoralRecord.MaxCategoryLength);
        b.HasIndex(x => x.TenantId).HasDatabaseName("IX_pastoral_categories_tenant");
    }
}

internal sealed class PastoralSettingsConfiguration : IEntityTypeConfiguration<PastoralSettings>
{
    public void Configure(EntityTypeBuilder<PastoralSettings> b)
    {
        b.ToTable("pastoral_settings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => x.TenantId).IsUnique().HasDatabaseName("UX_pastoral_settings_tenant");
    }
}
