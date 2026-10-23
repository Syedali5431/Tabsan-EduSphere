using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Infrastructure.Documents;

namespace Tabsan.EduSphere.API.Services;

/// <summary>
/// Plan M3.2: polls the submission conversion queue (DocumentConversion section). Does nothing until
/// <c>DocumentConversion:GotenbergUrl</c> is set. Several API nodes may run it: claims are guarded by the row version.
/// </summary>
public sealed class SubmissionConversionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly DocumentConversionOptions _options;
    private readonly ILogger<SubmissionConversionWorker> _logger;

    public SubmissionConversionWorker(IServiceScopeFactory scopes, IOptions<DocumentConversionOptions> options, ILogger<SubmissionConversionWorker> logger)
    {
        _scopes = scopes;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.WorkerEnabled || !Uri.TryCreate(_options.GotenbergUrl, UriKind.Absolute, out _))
        {
            _logger.LogInformation("Submission conversion worker is off (DocumentConversion:GotenbergUrl not set or WorkerEnabled=false). Office files stay download-only.");
            return;
        }

        var delay = TimeSpan.FromSeconds(Math.Clamp(_options.PollSeconds, 2, 600));
        while (!stoppingToken.IsCancellationRequested)
        {
            var converted = 0;
            try
            {
                using var scope = _scopes.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ISubmissionConversionProcessor>();
                converted = await processor.ProcessBatchAsync(_options.BatchSize, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Submission conversion batch failed.");
            }

            // Keep draining while there is work; otherwise wait for the next poll.
            if (converted == 0)
            {
                try { await Task.Delay(delay, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }
}
