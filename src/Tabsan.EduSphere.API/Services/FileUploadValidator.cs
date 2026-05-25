namespace Tabsan.EduSphere.API.Services;

/// <summary>
/// Validates uploaded files by checking size, extension, MIME type, and magic bytes.
/// Prevents malicious file uploads (OWASP A04 — Insecure Design).
/// </summary>
public static class FileUploadValidator
{
    private const long MaxFileSizeBytes      = 5 * 1024 * 1024; // 5 MB
    private const long MaxLogoSizeBytes      = 2 * 1024 * 1024; // 2 MB

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".csv", ".txt", ".rtf", ".odt", ".ods", ".odp"
    };

    private static readonly HashSet<string> AllowedLogoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp"
    };

    private static readonly Dictionary<string, string[]> AllowedMimeTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"]  = ["application/pdf"],
            [".jpg"]  = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".png"]  = ["image/png"],
            [".doc"]  = ["application/msword"],
            [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
            [".xls"]  = ["application/vnd.ms-excel"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
            [".ppt"]  = ["application/vnd.ms-powerpoint"],
            [".pptx"] = ["application/vnd.openxmlformats-officedocument.presentationml.presentation"],
            [".csv"]  = ["text/csv", "application/csv", "application/vnd.ms-excel"],
            [".txt"]  = ["text/plain"],
            [".rtf"]  = ["application/rtf", "text/rtf"],
            [".odt"]  = ["application/vnd.oasis.opendocument.text"],
            [".ods"]  = ["application/vnd.oasis.opendocument.spreadsheet"],
            [".odp"]  = ["application/vnd.oasis.opendocument.presentation"]
        };

    private static readonly Dictionary<string, string[]> AllowedLogoMimeTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"]  = ["image/png"],
            [".jpg"]  = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".gif"]  = ["image/gif"],
            [".svg"]  = ["image/svg+xml", "text/plain"],  // SVG sometimes served as text/plain
            [".webp"] = ["image/webp"]
        };

    // Magic bytes: extension → expected header bytes
    private static readonly Dictionary<string, byte[][]> MagicBytes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"]  = [[0x25, 0x50, 0x44, 0x46]],                                     // %PDF
            [".jpg"]  = [[0xFF, 0xD8, 0xFF]],                                            // JPEG SOI
            [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
            [".png"]  = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],            // PNG
            [".doc"]  = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],            // OLE CFB
            [".docx"] = [[0x50, 0x4B, 0x03, 0x04]]                                       // ZIP (OOXML)
        };

    private static readonly Dictionary<string, byte[][]> LogoMagicBytes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"]  = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],            // PNG
            [".jpg"]  = [[0xFF, 0xD8, 0xFF]],                                            // JPEG SOI
            [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
            [".gif"]  = [[0x47, 0x49, 0x46, 0x38]],                                      // GIF8
            [".webp"] = [[0x52, 0x49, 0x46, 0x46]]                                       // RIFF (WebP)
            // SVG is XML/text — skip magic-bytes check; rely on extension + MIME
        };

    /// <summary>
    /// Validates an academic document upload (PDF, Word, JPEG, PNG). Returns null on success.
    /// </summary>
    public static async Task<string?> ValidateAsync(IFormFile file)
        => await ValidateCoreAsync(file, AllowedExtensions, AllowedMimeTypes, MagicBytes, MaxFileSizeBytes);

    /// <summary>
    /// Validates a logo/image upload (PNG, JPG, GIF, SVG, WebP ≤ 2 MB). Returns null on success.
    /// </summary>
    public static async Task<string?> ValidateImageAsync(IFormFile file)
        => await ValidateCoreAsync(file, AllowedLogoExtensions, AllowedLogoMimeTypes, LogoMagicBytes, MaxLogoSizeBytes);

    private static async Task<string?> ValidateCoreAsync(
        IFormFile file,
        HashSet<string> allowedExtensions,
        Dictionary<string, string[]> allowedMimes,
        Dictionary<string, byte[][]> magicMap,
        long maxBytes)
    {
        if (file is null || file.Length == 0)
            return "No file uploaded.";

        if (file.Length > maxBytes)
            return $"File exceeds the maximum allowed size of {maxBytes / (1024 * 1024)} MB.";

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            return $"File type '{ext}' is not permitted. Allowed: {string.Join(", ", allowedExtensions)}.";

        var contentType = file.ContentType?.Trim() ?? string.Empty;
        if (allowedMimes.TryGetValue(ext, out var mimes)
            && !string.IsNullOrWhiteSpace(contentType)
            && !mimes.Any(m => contentType.StartsWith(m, StringComparison.OrdinalIgnoreCase)))
        {
            return $"MIME type '{contentType}' does not match the file extension '{ext}'.";
        }

        if (magicMap.TryGetValue(ext, out var magicOptions))
        {
            var headerLength = magicOptions.Max(m => m.Length);
            var header       = new byte[headerLength];

            await using var stream = file.OpenReadStream();
            var read = await stream.ReadAsync(header.AsMemory(0, headerLength));

            var matchesAny = magicOptions.Any(magic =>
                read >= magic.Length &&
                magic.Select((b, i) => header[i] == b).All(x => x));

            if (!matchesAny)
                return $"File content does not match the expected format for '{ext}'.";
        }

        return null; // valid
    }
}
