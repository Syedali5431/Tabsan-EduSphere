using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Community;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// School news, events with replies, and online forms with their answers.

internal static class CommunityItemMapping
{
    public static void MapItem<T>(this EntityTypeBuilder<T> b, string table) where T : CommunityItem
    {
        b.ToTable(table);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Title).IsRequired().HasMaxLength(CommunityItem.MaxTitleLength);
        b.Property(x => x.Roles).HasConversion<byte>();
        b.HasIndex(x => x.TenantId).HasDatabaseName($"IX_{table}_tenant");
    }
}

internal sealed class NewsPostConfiguration : IEntityTypeConfiguration<NewsPost>
{
    public void Configure(EntityTypeBuilder<NewsPost> b)
    {
        b.MapItem("news_posts");
        b.Property(x => x.Summary).HasMaxLength(NewsPost.MaxSummaryLength);
        b.Property(x => x.Body).IsRequired();
        b.Property(x => x.ImageUrl).HasMaxLength(1000);
        b.Property(x => x.Status).HasConversion<byte>();
        b.HasIndex(x => new { x.Status, x.PublishAt }).HasDatabaseName("IX_news_posts_status_publish");
    }
}

internal sealed class NewsReadConfiguration : IEntityTypeConfiguration<NewsRead>
{
    public void Configure(EntityTypeBuilder<NewsRead> b)
    {
        b.ToTable("news_reads");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => new { x.PostId, x.UserId }).IsUnique().HasDatabaseName("IX_news_reads_post_user");
        b.HasIndex(x => x.UserId).HasDatabaseName("IX_news_reads_user");
        b.HasOne<NewsPost>().WithMany().HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SchoolEventConfiguration : IEntityTypeConfiguration<SchoolEvent>
{
    public void Configure(EntityTypeBuilder<SchoolEvent> b)
    {
        b.MapItem("school_events");
        b.Property(x => x.Description).HasMaxLength(SchoolEvent.MaxDescriptionLength);
        b.Property(x => x.Location).HasMaxLength(SchoolEvent.MaxLocationLength);
        b.Property(x => x.CancelReason).HasMaxLength(500);
        b.Property(x => x.Category).HasConversion<byte>();
        b.Property(x => x.Status).HasConversion<byte>();
        b.HasIndex(x => new { x.StartsAt, x.EndsAt }).HasDatabaseName("IX_school_events_dates");
        b.HasIndex(x => x.FormId).HasDatabaseName("IX_school_events_form");
    }
}

internal sealed class EventReplyConfiguration : IEntityTypeConfiguration<EventReply>
{
    public void Configure(EntityTypeBuilder<EventReply> b)
    {
        b.ToTable("event_replies");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Answer).HasConversion<byte>();
        b.Property(x => x.Note).HasMaxLength(500);
        b.Ignore(x => x.Places);
        b.HasIndex(x => new { x.EventId, x.UserId }).IsUnique().HasDatabaseName("IX_event_replies_event_user");
        b.HasOne<SchoolEvent>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SchoolFormConfiguration : IEntityTypeConfiguration<SchoolForm>
{
    public void Configure(EntityTypeBuilder<SchoolForm> b)
    {
        b.MapItem("school_forms");
        b.Property(x => x.Description).HasMaxLength(SchoolForm.MaxDescriptionLength);
        b.Property(x => x.Status).HasConversion<byte>();
        b.HasIndex(x => new { x.Status, x.DueAt }).HasDatabaseName("IX_school_forms_status_due");
    }
}

internal sealed class FormFieldConfiguration : IEntityTypeConfiguration<FormField>
{
    public void Configure(EntityTypeBuilder<FormField> b)
    {
        b.ToTable("form_fields");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Type).HasConversion<byte>();
        b.Property(x => x.Label).IsRequired().HasMaxLength(FormField.MaxLabelLength);
        b.Property(x => x.Help).HasMaxLength(FormField.MaxHelpLength);
        b.Property(x => x.OptionsJson).HasMaxLength(8000);
        b.Ignore(x => x.Options);
        b.HasIndex(x => new { x.FormId, x.Position }).HasDatabaseName("IX_form_fields_form_position");
        b.HasOne<SchoolForm>().WithMany().HasForeignKey(x => x.FormId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class FormResponseConfiguration : IEntityTypeConfiguration<FormResponse>
{
    public void Configure(EntityTypeBuilder<FormResponse> b)
    {
        b.ToTable("form_responses");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.AnswersJson).IsRequired();
        b.Ignore(x => x.Answers);
        // A parent's answer is per child (either parent); a student's or staff member's per person.
        b.HasIndex(x => new { x.FormId, x.StudentProfileId }).IsUnique().HasFilter("[StudentProfileId] IS NOT NULL").HasDatabaseName("IX_form_responses_form_child");
        b.HasIndex(x => new { x.FormId, x.RespondentUserId }).IsUnique().HasFilter("[StudentProfileId] IS NULL").HasDatabaseName("IX_form_responses_form_person");
        b.HasOne<SchoolForm>().WithMany().HasForeignKey(x => x.FormId).OnDelete(DeleteBehavior.Cascade);
    }
}
