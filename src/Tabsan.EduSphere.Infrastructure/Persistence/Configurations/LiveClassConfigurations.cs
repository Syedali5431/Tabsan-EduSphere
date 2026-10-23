using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.LiveClasses;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// Plan M10 — conferencing settings, live sessions and joins. Keys are not store-generated.

internal sealed class ConferenceSettingConfiguration : IEntityTypeConfiguration<ConferenceSetting>
{
    public void Configure(EntityTypeBuilder<ConferenceSetting> b)
    {
        b.ToTable("conference_settings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Provider).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.BaseUrl).HasMaxLength(500);
        b.Property(x => x.SecretEncrypted).HasMaxLength(2000);
        b.Property(x => x.AccountId).HasMaxLength(200);
        b.Property(x => x.ClientId).HasMaxLength(200);
        b.Property(x => x.OrganizerUserId).HasMaxLength(200);
        b.Ignore(x => x.IsComplete);
        b.HasIndex(x => x.TenantId).IsUnique().HasDatabaseName("IX_conference_settings_tenant");
    }
}

internal sealed class LiveSessionConfiguration : IEntityTypeConfiguration<LiveSession>
{
    public void Configure(EntityTypeBuilder<LiveSession> b)
    {
        b.ToTable("live_sessions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Title).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Provider).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ExternalMeetingId).HasMaxLength(300);
        b.Property(x => x.JoinUrl).HasMaxLength(2000);
        b.Property(x => x.HostUrlEncrypted).HasMaxLength(4000);
        b.Property(x => x.RecordingUrl).HasMaxLength(2000);
        b.Property(x => x.ModeratorPassword).IsRequired().HasMaxLength(64);
        b.Property(x => x.AttendeePassword).IsRequired().HasMaxLength(64);
        b.Ignore(x => x.EndsAt);
        b.HasIndex(x => new { x.CourseOfferingId, x.StartsAt }).HasDatabaseName("IX_live_sessions_offering_start");
        b.HasIndex(x => new { x.Status, x.StartsAt }).HasDatabaseName("IX_live_sessions_status_start");
        b.HasOne<Domain.Academic.CourseOffering>().WithMany().HasForeignKey(x => x.CourseOfferingId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class LiveSessionAttendanceConfiguration : IEntityTypeConfiguration<LiveSessionAttendance>
{
    public void Configure(EntityTypeBuilder<LiveSessionAttendance> b)
    {
        b.ToTable("live_session_attendance");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => new { x.SessionId, x.UserId }).IsUnique().HasDatabaseName("IX_live_session_attendance_session_user");
        b.HasOne<LiveSession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
