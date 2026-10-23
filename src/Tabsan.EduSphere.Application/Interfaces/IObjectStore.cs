namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Plan M14.2 — a key/value object store (local folder, Azure Blob Storage, Amazon S3 or an S3-compatible service such as
/// MinIO, Cloudflare R2 or Wasabi). Media storage (submissions, conversions, uploads) and SCORM package files use it when
/// several API instances must share files. Keys use forward slashes.
/// </summary>
public interface IObjectStore
{
    /// <summary>"FileSystem", "AzureBlob" or "S3".</summary>
    string Provider { get; }

    /// <summary>Writes (or replaces) an object. Metadata keys are lower-case ASCII; values are short strings.</summary>
    Task PutAsync(string key, Stream content, string contentType, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken ct = default);

    /// <summary>The object's content (the caller disposes it), or null when it does not exist.</summary>
    Task<ObjectStoreItem?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>The object's properties without its content, or null when it does not exist.</summary>
    Task<ObjectStoreInfo?> HeadAsync(string key, CancellationToken ct = default);

    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>Deletes every object whose key starts with <paramref name="prefix"/>.</summary>
    Task DeletePrefixAsync(string prefix, CancellationToken ct = default);

    /// <summary>A time-limited read link (SAS / pre-signed URL); null when the store cannot issue one.</summary>
    Task<string?> GetReadUrlAsync(string key, TimeSpan lifetime, string? downloadFileName = null, CancellationToken ct = default);
}

public sealed record ObjectStoreInfo(string Key, string ContentType, long Length, IReadOnlyDictionary<string, string> Metadata);

public sealed class ObjectStoreItem(ObjectStoreInfo info, Stream content) : IAsyncDisposable, IDisposable
{
    public ObjectStoreInfo Info { get; } = info;
    public Stream Content { get; } = content;
    public ValueTask DisposeAsync() => Content.DisposeAsync();
    public void Dispose() => Content.Dispose();
}
