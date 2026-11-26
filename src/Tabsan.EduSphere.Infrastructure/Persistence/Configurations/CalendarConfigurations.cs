using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Calendar;
using Tabsan.EduSphere.Domain.Identity;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// The personal calendar feed (subscribed by Google Calendar, Outlook, phones) and the due-date reminders already sent.

internal sealed class CalendarFeedTokenConfiguration : IEntityTypeConfiguration<CalendarFeedToken>
{
    public void Configure(EntityTypeBuilder<CalendarFeedToken> b)
    {
        b.ToTable("calendar_feed_tokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
        b.Property(x => x.TokenEncrypted).IsRequired().HasMaxLength(500);
        b.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("IX_calendar_feed_tokens_hash");
        b.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("IX_calendar_feed_tokens_user");
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class DueReminderLogConfiguration : IEntityTypeConfiguration<DueReminderLog>
{
    public void Configure(EntityTypeBuilder<DueReminderLog> b)
    {
        b.ToTable("due_reminder_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Kind).HasConversion<byte>();
        b.HasIndex(x => new { x.Kind, x.ItemId, x.StudentProfileId, x.DueAt }).IsUnique().HasDatabaseName("IX_due_reminder_logs_item_student_due");
        b.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_due_reminder_logs_created");
    }
}
