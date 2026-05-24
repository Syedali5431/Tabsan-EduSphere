using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.BackgroundJobs;

/// <summary>
/// Background hosted service that runs daily to detect students with low attendance
/// and dispatches AttendanceAlert notifications to them and their department admins.
///
/// Configuration (appsettings.json → AttendanceAlert):
///   Threshold      — minimum attendance percentage before alert fires (default 75.0)
///   CronHour       — UTC hour to run the check (default 2, i.e. 02:00 UTC)
///   IntervalHours  — repeat interval in hours (default 24)
/// </summary>
public class AttendanceAlertJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AttendanceAlertJob> _logger;
    private readonly double _threshold;
    private readonly TimeSpan _interval;

    public AttendanceAlertJob(
        IServiceProvider services,
        ILogger<AttendanceAlertJob> logger,
        IConfiguration config)
    {
        _services  = services;
        _logger    = logger;
        _threshold = config.GetValue<double>("AttendanceAlert:Threshold", 75.0);
        _interval  = TimeSpan.FromHours(config.GetValue<double>("AttendanceAlert:IntervalHours", 24));
    }

    /// <summary>
    /// Main execution loop. Waits 60 s at startup then runs the check on the configured interval.
    /// A new DI scope is created per iteration so the DbContext is not held across runs.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AttendanceAlertJob started (threshold={Threshold}%, interval={Interval}h).",
            _threshold, _interval.TotalHours);

        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCheckAsync(stoppingToken);
            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("AttendanceAlertJob stopping.");
    }

    /// <summary>
    /// Resolves scoped services, finds students below the threshold, and dispatches notifications.
    /// Exceptions are caught and logged to prevent the job from crashing on transient errors.
    /// </summary>
    private async Task RunCheckAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _services.CreateScope();
            var attendanceService    = scope.ServiceProvider.GetRequiredService<IAttendanceService>();
            var notificationService  = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var parentLinkRepository = scope.ServiceProvider.GetRequiredService<IParentStudentLinkRepository>();

            var belowThreshold = await attendanceService.GetBelowThresholdAsync(_threshold, ct: ct);

            if (belowThreshold.Count == 0)
            {
                _logger.LogInformation("AttendanceAlertJob: no students below {Threshold}%.", _threshold);
                return;
            }

            _logger.LogInformation("AttendanceAlertJob: {Count} student-offering pairs below threshold.", belowThreshold.Count);

            foreach (var (studentProfileId, courseOfferingId, percent) in belowThreshold)
            {
                // Each alert is a separate notification so the student sees per-course detail.
                await notificationService.SendSystemAsync(
                    title: "Low Attendance Warning",
                    body:  $"Your attendance in course offering {courseOfferingId} is {percent:F1}%, which is below the required {_threshold}%. Please attend sessions to avoid being de-enrolled.",
                    type:  NotificationType.AttendanceAlert,
                    recipientUserIds: new[] { studentProfileId },   // student profile ID doubles as user lookup key; controller resolves to user ID
                    ct: ct);

                var parentRecipientIds = await parentLinkRepository.GetActiveParentUserIdsByStudentAsync(studentProfileId, ct);
                if (parentRecipientIds.Count > 0)
                {
                    await notificationService.SendSystemAsync(
                        title: "Child Attendance Warning",
                        body:  $"Attendance warning: your linked student in course offering {courseOfferingId} is currently at {percent:F1}%, below the {_threshold}% threshold.",
                        type:  NotificationType.AttendanceAlert,
                        recipientUserIds: parentRecipientIds,
                        ct: ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AttendanceAlertJob encountered an error during the check.");
        }
    }
}
