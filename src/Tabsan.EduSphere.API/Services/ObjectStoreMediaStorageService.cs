using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Services;

/// <summary>
/// Plan M14.2 — media storage (submissions, conversions, uploads) in a shared object store (Azure Blob Storage or S3), so
/// several API instances see the same files. Keys look like the local provider's: {prefix}{category}/yyyy/MM/{guid}{ext}.
/// </summary>
public sealed class ObjectStoreMediaStorageService : IMediaStorageService
{
    private const string HashKey = "content-sha256";
    private const string FileNameKey = "download-filename";
    private readonly IObjectStore _store;
    private readonly MediaStorageOptions _options;

    public ObjectStoreMediaStorageService(IObjectStore store, IOptions<MediaStorageOptions> options)
    {
        _store = store;
        _options = options.Value;
    }

    public async Task<MediaStorageSaveResult> SaveAsync(Stream content, string category, string fileExtension, string? contentType = null,
        string? downloadFileName = null, CancellationToken ct = default)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Category is required.", nameof(category));
        var extension = string.IsNullOrWhiteSpace(fileExtension) ? "" : fileExtension.StartsWith('.') ? fileExtension.ToLowerInvariant() : "." + fileExtension.ToLowerInvariant();
        var prefix = string.IsNullOrWhiteSpace(_options.KeyPrefix) ? "" : _options.KeyPrefix.Trim().Replace('\\', '/').Trim('/') + "/";
        var segment = category.Trim().Replace('\\', '/').Trim('/') is { Length: > 0 } s ? s : "misc";
        var key = $"{prefix}{segment}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
        var type = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        var fileName = string.IsNullOrWhiteSpace(downloadFileName) ? null : Path.GetFileName(downloadFileName.Trim());

        // Hash while copying to a temporary buffer (object stores want the length up front; the hash goes into metadata).
        await using var buffer = new FileStream(Path.Combine(Path.GetTempPath(), $"edusphere-upload-{Guid.NewGuid():N}"), FileMode.CreateNew,
            FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, ct)) > 0)
        {
            sha.AppendData(chunk, 0, read);
            await buffer.WriteAsync(chunk.AsMemory(0, read), ct);
        }
        var hash = Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant();
        var length = buffer.Length;
        buffer.Position = 0;

        var metadata = new Dictionary<string, string> { [HashKey] = hash };
        if (fileName is not null) metadata[FileNameKey] = Uri.EscapeDataString(fileName);
        await _store.PutAsync(key, buffer, type, metadata, ct);
        var reference = string.IsNullOrWhiteSpace(_options.PublicBaseUrl) ? key : $"{_options.PublicBaseUrl!.TrimEnd('/')}/{key}";
        return new MediaStorageSaveResult(key, reference, type, length, hash, fileName);
    }

    public async Task<byte[]?> ReadAsBytesAsync(string storageKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return null;
        await using var item = await _store.GetAsync(storageKey, ct);
        if (item is null) return null;
        using var memory = new MemoryStream();
        await item.Content.CopyToAsync(memory, ct);
        return memory.ToArray();
    }

    public async Task<MediaStorageObjectMetadata?> GetMetadataAsync(string storageKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return null;
        var info = await _store.HeadAsync(storageKey, ct);
        return info is null ? null : new MediaStorageObjectMetadata(storageKey, info.ContentType, info.Length,
            info.Metadata.GetValueOrDefault(HashKey), info.Metadata.TryGetValue(FileNameKey, out var n) ? Uri.UnescapeDataString(n) : null);
    }

    public async Task<string?> GenerateTemporaryReadUrlAsync(string storageKey, TimeSpan ttl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return null;
        var metadata = await GetMetadataAsync(storageKey, ct);
        if (metadata is null) return null;
        return await _store.GetReadUrlAsync(storageKey, ttl <= TimeSpan.Zero ? TimeSpan.FromMinutes(5) : ttl, metadata.DownloadFileName, ct);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
        => string.IsNullOrWhiteSpace(storageKey) ? Task.CompletedTask : _store.DeleteAsync(storageKey, ct);
}
