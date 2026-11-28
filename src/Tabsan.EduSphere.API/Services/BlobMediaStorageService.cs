using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Services;

/// <summary>
/// Object-storage style adapter that uses a dedicated root path and key semantics.
/// This keeps the contract ready for real cloud backends without changing callers.
/// </summary>
public sealed class BlobMediaStorageService : IMediaStorageService
{
    private readonly MediaStorageOptions _options;
    private readonly string _contentRootPath;

    public BlobMediaStorageService(
        IOptions<MediaStorageOptions> options,
        IHostEnvironment hostEnvironment)
    {
        _options = options.Value;
        _contentRootPath = hostEnvironment.ContentRootPath;
    }

    public async Task<MediaStorageSaveResult> SaveAsync(
        Stream content,
        string category,
        string fileExtension,
        string? contentType = null,
        string? downloadFileName = null,
        CancellationToken ct = default)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Category is required.", nameof(category));

        var extension = NormalizeExtension(fileExtension);
        var keyPrefix = NormalizePrefix(_options.KeyPrefix);
        var segment = NormalizePathSegment(category);
        var key = $"{keyPrefix}{segment}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}{extension}";

        var fullPath = ResolveFullPath(key);
        var parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        string? contentHashSha256;
        long length;
        await using (var destination = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            (contentHashSha256, length) = await CopyWithHashAsync(content, destination, ct);
            await destination.FlushAsync(ct);
        }

        var resolvedContentType = string.IsNullOrWhiteSpace(contentType)
            ? ResolveContentType(key)
            : contentType.Trim();

        await WriteMetadataAsync(
            key,
            new PersistedMediaMetadata(resolvedContentType, length, contentHashSha256, NormalizeDownloadFileName(downloadFileName)),
            ct);

        return new MediaStorageSaveResult(
            key,
            BuildReference(key),
            resolvedContentType,
            length,
            contentHashSha256,
            NormalizeDownloadFileName(downloadFileName));
    }

    public async Task<byte[]?> ReadAsBytesAsync(string storageKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return null;

        var fullPath = ResolveFullPath(storageKey);
        if (!File.Exists(fullPath)) return null;

        return await File.ReadAllBytesAsync(fullPath, ct);
    }

    public Task<MediaStorageObjectMetadata?> GetMetadataAsync(string storageKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return Task.FromResult<MediaStorageObjectMetadata?>(null);

        var fullPath = ResolveFullPath(storageKey);
        if (!File.Exists(fullPath))
            return Task.FromResult<MediaStorageObjectMetadata?>(null);

        return GetMetadataInternalAsync(storageKey, fullPath, ct);
    }

    public Task<string?> GenerateTemporaryReadUrlAsync(
        string storageKey,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return Task.FromResult<string?>(null);

        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            return Task.FromResult<string?>(null);

        // Final-Touches Phase 28 Stage 28.3 — generate provider-backed temporary signed URL.
        var baseUrl = _options.PublicBaseUrl!.TrimEnd('/');
        var expiresAt = DateTimeOffset.UtcNow.Add(ttl <= TimeSpan.Zero ? TimeSpan.FromMinutes(5) : ttl).ToUnixTimeSeconds();
        var metadata = GetMetadataAsync(storageKey, ct).GetAwaiter().GetResult();
        var unsignedUrl = $"{baseUrl}/{storageKey}?exp={expiresAt}";
        var signature = CreateSignature(storageKey, expiresAt);

        if (!string.IsNullOrWhiteSpace(metadata?.DownloadFileName))
            unsignedUrl += $"&download={Uri.EscapeDataString(metadata.DownloadFileName)}";

        var url = signature is null
            ? unsignedUrl
            : $"{unsignedUrl}&sig={Uri.EscapeDataString(signature)}";

        return Task.FromResult<string?>(url);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return Task.CompletedTask;

        var fullPath = ResolveFullPath(storageKey);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        var metadataPath = GetMetadataPath(fullPath);
        if (File.Exists(metadataPath))
            File.Delete(metadataPath);

        return Task.CompletedTask;
    }

    private async Task<MediaStorageObjectMetadata?> GetMetadataInternalAsync(string storageKey, string fullPath, CancellationToken ct)
    {
        var persisted = await ReadMetadataAsync(fullPath, ct);
        var info = new FileInfo(fullPath);

        return new MediaStorageObjectMetadata(
            storageKey,
            persisted?.ContentType ?? ResolveContentType(storageKey),
            info.Length,
            persisted?.ContentHashSha256,
            persisted?.DownloadFileName);
    }

    private async Task<PersistedMediaMetadata?> ReadMetadataAsync(string fullPath, CancellationToken ct)
    {
        var metadataPath = GetMetadataPath(fullPath);
        if (!File.Exists(metadataPath))
            return null;

        await using var stream = new FileStream(metadataPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await JsonSerializer.DeserializeAsync<PersistedMediaMetadata>(stream, cancellationToken: ct);
    }

    private async Task WriteMetadataAsync(string storageKey, PersistedMediaMetadata metadata, CancellationToken ct)
    {
        var fullPath = ResolveFullPath(storageKey);
        var metadataPath = GetMetadataPath(fullPath);
        await using var stream = new FileStream(metadataPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, metadata, cancellationToken: ct);
        await stream.FlushAsync(ct);
    }

    private static async Task<(string Hash, long Length)> CopyWithHashAsync(Stream source, Stream destination, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        var buffer = new byte[81920];
        long total = 0;

        while (true)
        {
            var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
            if (read == 0)
                break;

            await destination.WriteAsync(buffer.AsMemory(0, read), ct);
            sha256.TransformBlock(buffer, 0, read, null, 0);
            total += read;
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return (Convert.ToHexString(sha256.Hash!).ToLowerInvariant(), total);
    }

    private static string GetMetadataPath(string fullPath) => fullPath + ".meta.json";

    private static string? NormalizeDownloadFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        return Tabsan.EduSphere.Domain.Common.UploadFileName.Of(fileName.Trim());
    }

    private string ResolveFullPath(string key)
    {
        var root = GetRootPath();
        return Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar));
    }

    private string GetRootPath()
    {
        var configured = _options.BlobRootPath;
        if (string.IsNullOrWhiteSpace(configured))
            configured = "blob-storage";

        return Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(_contentRootPath, configured);
    }

    private string BuildReference(string key)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            return key;

        var baseUrl = _options.PublicBaseUrl!.TrimEnd('/');
        return $"{baseUrl}/{key}";
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return string.Empty;
        return extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
    }

    private static string NormalizePathSegment(string value)
    {
        var normalized = value.Trim().Replace('\\', '/').Trim('/');
        return string.IsNullOrWhiteSpace(normalized) ? "misc" : normalized;
    }

    private static string NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return string.Empty;
        var normalized = prefix.Trim().Replace('\\', '/').Trim('/');
        return string.IsNullOrWhiteSpace(normalized) ? string.Empty : normalized + "/";
    }

    private string? CreateSignature(string storageKey, long expiresAt)
    {
        var secret = _options.SignedUrlSecret;
        if (string.IsNullOrWhiteSpace(secret))
            return null;

        var payload = $"{storageKey}|{expiresAt}";
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ResolveContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".tablic" => "application/octet-stream",
            _ => "application/octet-stream"
        };
    }

    private sealed record PersistedMediaMetadata(
        string ContentType,
        long Length,
        string? ContentHashSha256,
        string? DownloadFileName);
}
