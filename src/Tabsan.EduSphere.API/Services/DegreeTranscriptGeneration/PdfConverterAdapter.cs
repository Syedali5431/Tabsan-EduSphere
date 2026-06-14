using System.Diagnostics;

namespace Tabsan.EduSphere.API.Services.DegreeTranscriptGeneration;

public interface IPdfConverterAdapter
{
    Task<string?> TryConvertToPdfAsync(string docxPath, CancellationToken ct = default);
}

public sealed class LibreOfficePdfConverterAdapter : IPdfConverterAdapter
{
    private readonly ILogger<LibreOfficePdfConverterAdapter> _logger;

    private static readonly string[] KnownPaths =
    [
        @"C:\Program Files\LibreOffice\program\soffice.exe",
        @"C:\Program Files (x86)\LibreOffice\program\soffice.exe",
        @"/usr/bin/soffice",
        @"/usr/lib/libreoffice/program/soffice"
    ];

    public LibreOfficePdfConverterAdapter(ILogger<LibreOfficePdfConverterAdapter> logger)
        => _logger = logger;

    public async Task<string?> TryConvertToPdfAsync(string docxPath, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var sofficePath = FindLibreOffice();
        if (sofficePath == null) return null;

        var outputDir = Path.GetDirectoryName(docxPath)!;
        var pdfPath = Path.ChangeExtension(docxPath, ".pdf");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = sofficePath,
                Arguments = $"--headless --convert-to pdf --outdir \"{outputDir}\" \"{docxPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi);
            if (process == null) return null;
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            if (process.ExitCode == 0 && File.Exists(pdfPath)) return pdfPath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LibreOffice PDF conversion failed for {DocxPath}", docxPath);
        }
        return null;
    }

    private static string? FindLibreOffice()
    {
        foreach (var path in KnownPaths)
            if (File.Exists(path)) return path;
        return null;
    }
}

public sealed class NoOpPdfConverterAdapter : IPdfConverterAdapter
{
    private readonly ILogger<NoOpPdfConverterAdapter> _logger;
    public NoOpPdfConverterAdapter(ILogger<NoOpPdfConverterAdapter> logger) => _logger = logger;
    public Task<string?> TryConvertToPdfAsync(string docxPath, CancellationToken ct = default)
    {
        _logger.LogDebug("NoOp PDF converter for {DocxPath}", docxPath);
        return Task.FromResult<string?>(null);
    }
}
