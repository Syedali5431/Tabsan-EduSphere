using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// Plan M4.3 — Web Push subscriptions and per-type notification preferences.

internal sealed class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("push_subscriptions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Endpoint).IsRequired().HasMaxLength(1000);
        builder.Property(s => s.P256dh).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Auth).IsRequired().HasMaxLength(100);
        builder.Property(s => s.DeviceLabel).HasMaxLength(120);
        builder.HasIndex(s => s.Endpoint).IsUnique().HasDatabaseName("IX_push_subscriptions_endpoint");
        builder.HasIndex(s => new { s.UserId, s.IsActive }).HasDatabaseName("IX_push_subscriptions_user_active");
        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notification_preferences");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(p => new { p.UserId, p.Type }).IsUnique().HasDatabaseName("IX_notification_preferences_user_type");
        builder.HasOne<User>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
