using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.Infrastructure.Storage;

// Plan M14.2 — object stores behind IObjectStore: a local folder (single server), Azure Blob Storage and Amazon S3 /
// S3-compatible services (for several API instances sharing submissions, conversions, uploads and SCORM packages).

/// <summary>Settings (section "MediaStorage"); the provider is chosen with MediaStorage:Provider.</summary>
public sealed class ObjectStoreOptions
{
    public AzureBlobStoreOptions AzureBlob { get; set; } = new();
    public S3StoreOptions S3 { get; set; } = new();
}

public sealed class AzureBlobStoreOptions
{
    /// <summary>Storage account connection string (or use <see cref="ServiceUri"/> with a SAS token).</summary>
    public string? ConnectionString { get; set; }
    public string? ServiceUri { get; set; }
    public string? SasToken { get; set; }
    public string Container { get; set; } = "edusphere";
    /// <summary>Create the container (on the first write) when it is missing.</summary>
    public bool CreateContainer { get; set; } = true;
}

public sealed class S3StoreOptions
{
    public string Bucket { get; set; } = "";
    /// <summary>AWS region (e.g. ap-southeast-2); ignored when <see cref="ServiceUrl"/> is set.</summary>
    public string? Region { get; set; }
    /// <summary>S3-compatible endpoint (MinIO, Cloudflare R2, Wasabi …).</summary>
    public string? ServiceUrl { get; set; }
    /// <summary>Path-style addressing (needed by most S3-compatible services).</summary>
    public bool ForcePathStyle { get; set; }
    /// <summary>Access keys; leave empty to use the default AWS credential chain (instance role, environment, profile).</summary>
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }
    /// <summary>Upload with an unsigned payload over HTTPS instead of streaming (chunk-signed) uploads — needed by Cloudflare R2 and
    /// some other S3-compatible services.</summary>
    public bool DisablePayloadSigning { get; set; }
}

/// <summary>Objects as files under a root folder (metadata in a side file). For one server or a shared network folder.</summary>
public sealed class FileSystemObjectStore : IObjectStore
{
    private readonly string _root;

    public FileSystemObjectStore(string rootPath) => _root = Path.GetFullPath(rootPath);

    public string Provider => "FileSystem";

    public async Task PutAsync(string key, Stream content, string contentType, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken ct = default)
    {
        var path = PathOf(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using (var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            await content.CopyToAsync(file, ct);
        var meta = new Dictionary<string, string>(metadata ?? new Dictionary<string, string>()) { ["content-type"] = contentType };
        await File.WriteAllTextAsync(path + ".meta.json", JsonSerializer.Serialize(meta), ct);
    }

    public async Task<ObjectStoreItem?> GetAsync(string key, CancellationToken ct = default)
    {
        var info = await HeadAsync(key, ct);
        return info is null ? null : new ObjectStoreItem(info, new FileStream(PathOf(key), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));
    }

    public async Task<ObjectStoreInfo?> HeadAsync(string key, CancellationToken ct = default)
    {
        var path = PathOf(key);
        if (!File.Exists(path)) return null;
        var meta = File.Exists(path + ".meta.json")
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(path + ".meta.json", ct)) ?? new()
            : new Dictionary<string, string>();
        var type = meta.Remove("content-type", out var t) ? t : "application/octet-stream";
        return new ObjectStoreInfo(key, type, new FileInfo(path).Length, meta);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var path = PathOf(key);
        File.Delete(path);
        File.Delete(path + ".meta.json");
        return Task.CompletedTask;
    }

    public Task DeletePrefixAsync(string prefix, CancellationToken ct = default)
    {
        var folder = PathOf(prefix.TrimEnd('/'));
        if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        return Task.CompletedTask;
    }

    public Task<string?> GetReadUrlAsync(string key, TimeSpan lifetime, string? downloadFileName = null, CancellationToken ct = default)
        => Task.FromResult<string?>(null);

    /// <summary>The file for a key, refusing keys that leave the root (.., absolute paths).</summary>
    private string PathOf(string key)
    {
        var full = Path.GetFullPath(Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(_root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The key leaves the storage folder.", nameof(key));
        return full;
    }
}

/// <summary>Azure Blob Storage (block blobs in one container; metadata as blob metadata).</summary>
public sealed class AzureBlobObjectStore : IObjectStore
{
    private readonly BlobContainerClient _container;
    private readonly bool _createContainer;
    private volatile bool _containerReady;

    public AzureBlobObjectStore(AzureBlobStoreOptions options, BlobClientOptions? clientOptions = null)
    {
        BlobServiceClient service;
        if (!string.IsNullOrWhiteSpace(options.ConnectionString)) service = new BlobServiceClient(options.ConnectionString, clientOptions);
        else if (!string.IsNullOrWhiteSpace(options.ServiceUri) && !string.IsNullOrWhiteSpace(options.SasToken))
            service = new BlobServiceClient(new Uri(options.ServiceUri), new AzureSasCredential(options.SasToken), clientOptions);
        else throw new InvalidOperationException("MediaStorage:AzureBlob needs ConnectionString, or ServiceUri with SasToken.");
        _container = service.GetBlobContainerClient(options.Container);
        _createContainer = options.CreateContainer;
    }

    public string Provider => "AzureBlob";

    public async Task PutAsync(string key, Stream content, string contentType, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken ct = default)
    {
        // The container is created on the first write (not at start-up, so a storage outage cannot stop the API starting).
        if (_createContainer && !_containerReady)
        {
            await _container.CreateIfNotExistsAsync(cancellationToken: ct);
            _containerReady = true;
        }
        await _container.GetBlobClient(key).UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            Metadata = metadata?.ToDictionary(m => Safe(m.Key), m => m.Value)
        }, ct);
    }

    public async Task<ObjectStoreItem?> GetAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var result = await _container.GetBlobClient(key).DownloadStreamingAsync(cancellationToken: ct);
            var d = result.Value.Details;
            return new ObjectStoreItem(new ObjectStoreInfo(key, d.ContentType ?? "application/octet-stream", d.ContentLength, Unsafe(d.Metadata)), result.Value.Content);
        }
        catch (RequestFailedException ex) when (ex.Status == 404) { return null; }
    }

    public async Task<ObjectStoreInfo?> HeadAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var p = (await _container.GetBlobClient(key).GetPropertiesAsync(cancellationToken: ct)).Value;
            return new ObjectStoreInfo(key, p.ContentType ?? "application/octet-stream", p.ContentLength, Unsafe(p.Metadata));
        }
        catch (RequestFailedException ex) when (ex.Status == 404) { return null; }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
        => await _container.GetBlobClient(key).DeleteIfExistsAsync(cancellationToken: ct);

    public async Task DeletePrefixAsync(string prefix, CancellationToken ct = default)
    {
        await foreach (var blob in _container.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, ct))
            await _container.GetBlobClient(blob.Name).DeleteIfExistsAsync(cancellationToken: ct);
    }

    public Task<string?> GetReadUrlAsync(string key, TimeSpan lifetime, string? downloadFileName = null, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(key);
        if (!blob.CanGenerateSasUri) return Task.FromResult<string?>(null);
        var sas = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(lifetime))
        {
            BlobContainerName = _container.Name,
            BlobName = key,
            ContentDisposition = downloadFileName is null ? null : $"attachment; filename=\"{downloadFileName.Replace("\"", "")}\""
        };
        return Task.FromResult<string?>(blob.GenerateSasUri(sas).ToString());
    }

    // Blob metadata names must be C# identifiers: "content-hash" ↔ "content_hash".
    private static string Safe(string name) => name.Replace('-', '_');

    private static IReadOnlyDictionary<string, string> Unsafe(IDictionary<string, string>? m)
        => m is null ? new Dictionary<string, string>() : m.ToDictionary(x => x.Key.Replace('_', '-'), x => x.Value, StringComparer.OrdinalIgnoreCase);
}

/// <summary>Amazon S3 or an S3-compatible service (metadata as x-amz-meta-* headers).</summary>
public sealed class S3ObjectStore : IObjectStore
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly bool _unsignedPayload;

    public S3ObjectStore(S3StoreOptions options, AmazonS3Config? config = null)
    {
        if (string.IsNullOrWhiteSpace(options.Bucket)) throw new InvalidOperationException("MediaStorage:S3:Bucket is required.");
        _bucket = options.Bucket;
        _unsignedPayload = options.DisablePayloadSigning;
        config ??= new AmazonS3Config();
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl)) config.ServiceURL = options.ServiceUrl;
        else if (!string.IsNullOrWhiteSpace(options.Region)) config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region);
        config.ForcePathStyle = options.ForcePathStyle || !string.IsNullOrWhiteSpace(options.ServiceUrl);
        // Checksums only when the operation needs them (S3-compatible services differ in their support).
        config.RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED;
        config.ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED;
        _s3 = !string.IsNullOrWhiteSpace(options.AccessKeyId)
            ? new AmazonS3Client(new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey), config)
            : new AmazonS3Client(config);
    }

    public string Provider => "S3";

    public async Task PutAsync(string key, Stream content, string contentType, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken ct = default)
    {
        var request = new PutObjectRequest { BucketName = _bucket, Key = key, InputStream = content, ContentType = contentType, AutoCloseStream = false };
        if (_unsignedPayload) { request.DisablePayloadSigning = true; request.UseChunkEncoding = false; }
        foreach (var (k, v) in metadata ?? new Dictionary<string, string>()) request.Metadata.Add(k, v);
        await _s3.PutObjectAsync(request, ct);
    }

    public async Task<ObjectStoreItem?> GetAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var o = await _s3.GetObjectAsync(new GetObjectRequest { BucketName = _bucket, Key = key }, ct);
            return new ObjectStoreItem(new ObjectStoreInfo(key, o.Headers.ContentType ?? "application/octet-stream", o.ContentLength, Meta(o.Metadata)), o.ResponseStream);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { return null; }
    }

    public async Task<ObjectStoreInfo?> HeadAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var o = await _s3.GetObjectMetadataAsync(new GetObjectMetadataRequest { BucketName = _bucket, Key = key }, ct);
            return new ObjectStoreInfo(key, o.Headers.ContentType ?? "application/octet-stream", o.ContentLength, Meta(o.Metadata));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { return null; }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
        => await _s3.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucket, Key = key }, ct);

    public async Task DeletePrefixAsync(string prefix, CancellationToken ct = default)
    {
        string? token = null;
        do
        {
            var page = await _s3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = _bucket, Prefix = prefix, ContinuationToken = token }, ct);
            foreach (var o in page.S3Objects ?? []) await DeleteAsync(o.Key, ct);
            token = page.IsTruncated == true ? page.NextContinuationToken : null;
        } while (token is not null);
    }

    public async Task<string?> GetReadUrlAsync(string key, TimeSpan lifetime, string? downloadFileName = null, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest { BucketName = _bucket, Key = key, Verb = HttpVerb.GET, Expires = DateTime.UtcNow.Add(lifetime) };
        if (downloadFileName is not null) request.ResponseHeaderOverrides.ContentDisposition = $"attachment; filename=\"{downloadFileName.Replace("\"", "")}\"";
        return await _s3.GetPreSignedURLAsync(request);
    }

    private static IReadOnlyDictionary<string, string> Meta(MetadataCollection m)
        => m.Keys.ToDictionary(k => k.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase) ? k[11..] : k, k => m[k], StringComparer.OrdinalIgnoreCase);
}
