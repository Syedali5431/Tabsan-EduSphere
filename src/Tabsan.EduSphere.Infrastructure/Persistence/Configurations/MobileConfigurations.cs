using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Mobile;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// Plan M12 — mobile app devices (native push) and crash reports. Keys are not store-generated.

internal sealed class MobileDeviceConfiguration : IEntityTypeConfiguration<MobileDevice>
{
    public void Configure(EntityTypeBuilder<MobileDevice> b)
    {
        b.ToTable("mobile_devices");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Platform).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.Token).IsRequired().HasMaxLength(MobileDevice.MaxTokenLength);
        b.Property(x => x.AppVersion).HasMaxLength(32);
        b.Property(x => x.DeviceModel).HasMaxLength(100);
        b.Property(x => x.Language).HasMaxLength(8);
        b.HasIndex(x => x.Token).IsUnique().HasDatabaseName("IX_mobile_devices_token");
        b.HasIndex(x => new { x.UserId, x.IsActive }).HasDatabaseName("IX_mobile_devices_user_active");
    }
}

internal sealed class MobileCrashReportConfiguration : IEntityTypeConfiguration<MobileCrashReport>
{
    public void Configure(EntityTypeBuilder<MobileCrashReport> b)
    {
        b.ToTable("mobile_crash_reports");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Platform).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.AppVersion).IsRequired().HasMaxLength(32);
        b.Property(x => x.OsVersion).HasMaxLength(64);
        b.Property(x => x.DeviceModel).HasMaxLength(100);
        b.Property(x => x.ExceptionType).IsRequired().HasMaxLength(200);
        b.Property(x => x.Message).IsRequired().HasMaxLength(1000);
        b.Property(x => x.StackTrace).IsRequired().HasMaxLength(MobileCrashReport.MaxStackTraceLength);
        b.Property(x => x.Screen).HasMaxLength(200);
        b.HasIndex(x => x.OccurredAt).HasDatabaseName("IX_mobile_crash_reports_occurred");
        b.HasIndex(x => new { x.AppVersion, x.ExceptionType }).HasDatabaseName("IX_mobile_crash_reports_version_type");
    }
}
