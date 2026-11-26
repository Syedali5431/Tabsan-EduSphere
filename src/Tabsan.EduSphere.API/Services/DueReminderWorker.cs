using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Calendar;

namespace Tabsan.EduSphere.API.Services;

/// <summary>
/// Every few minutes (Calendar:Reminders:IntervalMinutes) reminds students whose homework is due, or whose quiz closes, within
/// Calendar:Reminders:HoursBefore hours and who have not handed it in or taken it yet — once per due date, in the app,
/// by push and by e-mail/SMS where those are switched on. Homework and quizzes follow their licensed modules.
/// </summary>
public sealed class DueReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IOptions<CalendarOptions> _options;
    private readonly ILogger<DueReminderWorker> _logger;

    public DueReminderWorker(IServiceScopeFactory scopes, IOptions<CalendarOptions> options, ILogger<DueReminderWorker> logger)
    {
        _scopes = scopes;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the application finish starting (and migrations run) first.
        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_options.Value.Reminders.Enabled)
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var reminded = await scope.ServiceProvider.GetRequiredService<CalendarService>().SendDueRemindersAsync(DateTime.UtcNow, stoppingToken);
                    if (reminded > 0) _logger.LogInformation("Due-date reminders: {Count} student(s) reminded.", reminded);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex) { _logger.LogWarning(ex, "Due-date reminder run failed."); }
            }

            try { await Task.Delay(TimeSpan.FromMinutes(Math.Clamp(_options.Value.Reminders.IntervalMinutes, 1, 24 * 60)), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
