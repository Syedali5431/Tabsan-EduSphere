using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.Infrastructure.Documents;

/// <summary>Plan M3.2 settings (section "DocumentConversion").</summary>
public sealed class DocumentConversionOptions
{
    public const string SectionName = "DocumentConversion";

    /// <summary>Base URL of a Gotenberg 8 service, e.g. http://gotenberg:3000. Empty = conversion off.</summary>
    public string? GotenbergUrl { get; set; }

    /// <summary>Run the background worker in this process (turn off on all but one node if you prefer).</summary>
    public bool WorkerEnabled { get; set; } = true;

    public int PollSeconds { get; set; } = 10;

    public int BatchSize { get; set; } = 5;

    public int TimeoutSeconds { get; set; } = 120;
}

/// <summary>
/// Plan M3.2: converts office documents to PDF through Gotenberg's LibreOffice route
/// (<c>POST /forms/libreoffice/convert</c>, multipart field <c>files</c>).
/// </summary>
public sealed class GotenbergDocumentConverter : IDocumentConverter
{
    private readonly HttpClient _http;
    private readonly DocumentConversionOptions _options;

    public GotenbergDocumentConverter(HttpClient http, IOptions<DocumentConversionOptions> options)
    {
        _http = http;
        _options = options.Value;
        if (IsEnabled && _http.BaseAddress is null)
            _http.BaseAddress = new Uri(_options.GotenbergUrl!.TrimEnd('/') + "/");
    }

    public bool IsEnabled => Uri.TryCreate(_options.GotenbergUrl, UriKind.Absolute, out _);

    public async Task<byte[]> ConvertToPdfAsync(byte[] content, string fileName, CancellationToken ct = default)
    {
        if (!IsEnabled) throw new InvalidOperationException("Document conversion is not configured.");

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        // Gotenberg picks the converter from the extension, so the name must keep it.
        form.Add(file, "files", SafeName(fileName));

        using var response = await _http.PostAsync("forms/libreoffice/convert", form, ct);
        var body = await response.Content.ReadAsByteArrayAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            var detail = System.Text.Encoding.UTF8.GetString(body.AsSpan(0, Math.Min(body.Length, 300)));
            throw new InvalidOperationException($"Conversion service returned {(int)response.StatusCode}: {detail}");
        }
        return body;
    }

    private static string SafeName(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return "document" + (string.IsNullOrWhiteSpace(ext) ? ".bin" : ext.ToLowerInvariant());
    }
}
