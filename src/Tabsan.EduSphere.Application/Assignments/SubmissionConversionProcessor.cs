using Microsoft.Extensions.Logging;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Assignments;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Assignments;

/// <summary>
/// Plan M3.2: converts waiting office documents to PDF. The database is the queue: a file is claimed by
/// moving it to Converting (the row version makes a double claim by two workers fail for one of them),
/// and a claim older than <see cref="StaleAfter"/> is retried. After <see cref="MaxAttempts"/> failures
/// the file is marked Failed and stays download-only until someone re-queues it.
/// </summary>
public sealed class SubmissionConversionProcessor : ISubmissionConversionProcessor
{
    public static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(10);
    public const int MaxAttempts = 3;

    private readonly ISubmissionWorkspaceRepository _repo;
    private readonly IMediaStorageService _media;
    private readonly IDocumentConverter _converter;
    private readonly ILogger<SubmissionConversionProcessor>? _logger;

    public SubmissionConversionProcessor(ISubmissionWorkspaceRepository repo, IMediaStorageService media, IDocumentConverter converter,
        ILogger<SubmissionConversionProcessor>? logger = null)
    {
        _repo = repo;
        _media = media;
        _converter = converter;
        _logger = logger;
    }

    public async Task<int> ProcessBatchAsync(int batchSize, CancellationToken ct = default)
    {
        if (!_converter.IsEnabled) return 0;
        var now = DateTime.UtcNow;
        var candidates = await _repo.GetConversionCandidatesAsync(now - StaleAfter, Math.Clamp(batchSize, 1, 50), ct);
        var converted = 0;

        foreach (var file in candidates)
        {
            ct.ThrowIfCancellationRequested();
            if (!file.TryClaimForConversion(DateTime.UtcNow, StaleAfter)) continue;
            if (!await _repo.TrySaveClaimAsync(ct)) continue; // another worker claimed it first

            try
            {
                var original = await _media.ReadAsBytesAsync(file.StorageKey!, ct)
                    ?? throw new InvalidOperationException("The original file is missing from storage.");
                var pdf = await _converter.ConvertToPdfAsync(original, file.FileName, ct);
                if (pdf.Length < 5 || pdf[0] != (byte)'%' || pdf[1] != (byte)'P' || pdf[2] != (byte)'D' || pdf[3] != (byte)'F')
                    throw new InvalidOperationException("The conversion service did not return a PDF.");

                await using var stream = new MemoryStream(pdf);
                var saved = await _media.SaveAsync(stream, SubmissionWorkspaceService.RenditionCategory, ".pdf", "application/pdf",
                    Path.ChangeExtension(file.FileName, ".pdf"), ct);
                file.MarkConverted(saved.StorageKey, DateTime.UtcNow);
                converted++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogWarning(ex, "Converting submission file {FileId} ({FileName}) failed (attempt {Attempt}).", file.Id, file.FileName, file.ConversionAttempts);
                file.MarkConversionFailed(ex.Message, MaxAttempts);
            }
            await _repo.SaveChangesAsync(ct);
        }
        return converted;
    }
}
