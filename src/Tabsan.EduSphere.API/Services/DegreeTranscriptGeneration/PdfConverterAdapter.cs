namespace Tabsan.EduSphere.API.Services.DegreeTranscriptGeneration;

/// <summary>
/// Optional PDF conversion adapter contract for degree/transcript generated documents.
/// Returning null preserves the mandatory .docx fallback behavior.
/// </summary>
public interface IPdfConverterAdapter
{
    Task<string?> TryConvertToPdfAsync(string docxPath, CancellationToken ct = default);
}

/// <summary>
/// Default no-op PDF converter adapter. This is intentionally safe and non-invasive;
/// deployments can replace it with a real converter implementation without changing
/// existing generation modules.
/// </summary>
public sealed class NoOpPdfConverterAdapter : IPdfConverterAdapter
{
    private readonly ILogger<NoOpPdfConverterAdapter> _logger;

    public NoOpPdfConverterAdapter(ILogger<NoOpPdfConverterAdapter> logger)
    {
        _logger = logger;
    }

    public Task<string?> TryConvertToPdfAsync(string docxPath, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Degree/Transcript PDF adapter is using NoOp converter for {DocxPath}; returning null to keep .docx fallback.",
            docxPath);

        return Task.FromResult<string?>(null);
    }
}
