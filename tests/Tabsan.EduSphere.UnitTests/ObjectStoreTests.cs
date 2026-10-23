using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Tabsan.EduSphere.API.Services;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Infrastructure.Lms;
using Tabsan.EduSphere.Infrastructure.Storage;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>
/// Plan M14.2 — shared object storage: the same behaviour from the local folder, Azure Blob Storage and S3 stores (the
/// cloud SDKs talk to in-memory fake services), media storage and SCORM files on top of them, signed read links.
/// </summary>
public class ObjectStoreTests
{
    // ── fake services ──────────────────────────────────────────────────────────────────────────

    private sealed record Stored(byte[] Body, string ContentType, Dictionary<string, string> Meta);

    /// <summary>Just enough of the Blob service REST API: container create, put/get/head/delete blob, list blobs.</summary>
    private sealed class FakeAzure : HttpMessageHandler
    {
        public readonly ConcurrentDictionary<string, Stored> Blobs = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = Uri.UnescapeDataString(request.RequestUri!.AbsolutePath); // /devstoreaccount1/container/key
            var query = request.RequestUri.Query;
            var parts = path.TrimStart('/').Split('/', 3);
            var key = parts.Length == 3 ? parts[2] : "";
            if (query.Contains("restype=container") && query.Contains("comp=list"))
            {
                var prefix = System.Web.HttpUtility.ParseQueryString(query)["prefix"] ?? "";
                var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"utf-8\"?><EnumerationResults><Blobs>");
                foreach (var name in Blobs.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)))
                    xml.Append($"<Blob><Name>{name}</Name><Properties><Content-Length>{Blobs[name].Body.Length}</Content-Length><BlobType>BlockBlob</BlobType></Properties></Blob>");
                xml.Append("</Blobs><NextMarker /></EnumerationResults>");
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(xml.ToString(), Encoding.UTF8, "application/xml") };
            }
            if (query.Contains("restype=container")) return new HttpResponseMessage(HttpStatusCode.Created);
            if (request.Method == HttpMethod.Put)
            {
                var body = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(ct);
                var meta = request.Headers.Where(h => h.Key.StartsWith("x-ms-meta-", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(h => h.Key[10..], h => h.Value.First());
                var type = request.Headers.TryGetValues("x-ms-blob-content-type", out var t) ? t.First() : "application/octet-stream";
                Blobs[key] = new Stored(body, type, meta);
                return Created();
            }
            if (!Blobs.TryGetValue(key, out var blob))
            {
                var missing = new HttpResponseMessage(HttpStatusCode.NotFound);
                missing.Headers.Add("x-ms-error-code", "BlobNotFound");
                return missing;
            }
            if (request.Method == HttpMethod.Delete) { Blobs.TryRemove(key, out _); return new HttpResponseMessage(HttpStatusCode.Accepted); }
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = request.Method == HttpMethod.Head ? new ByteArrayContent([]) : new ByteArrayContent(blob.Body)
            };
            response.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(blob.ContentType);
            response.Content.Headers.ContentLength = blob.Body.Length;
            response.Headers.Add("x-ms-blob-type", "BlockBlob");
            response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"0x1\"");
            response.Content.Headers.LastModified = DateTimeOffset.UtcNow;
            foreach (var (k, v) in blob.Meta) response.Headers.Add("x-ms-meta-" + k, v);
            return response;
        }

        private static HttpResponseMessage Created()
        {
            var r = new HttpResponseMessage(HttpStatusCode.Created) { Content = new ByteArrayContent([]) };
            r.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"0x1\"");
            r.Content.Headers.LastModified = DateTimeOffset.UtcNow;
            return r;
        }
    }

    /// <summary>Just enough of S3 (path style): put/get/head/delete object (incl. aws-chunked uploads), ListObjectsV2.</summary>
    private sealed class FakeS3 : HttpMessageHandler
    {
        public readonly ConcurrentDictionary<string, Stored> Objects = new();
        public readonly List<string> Authorizations = new();
        public readonly List<string> PayloadHashes = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Authorizations.Add(request.Headers.TryGetValues("Authorization", out var auth) ? string.Join(",", auth) : "");
            var payloadHash = request.Headers.TryGetValues("x-amz-content-sha256", out var p) ? p.First() : "";
            PayloadHashes.Add(payloadHash);
            var path = Uri.UnescapeDataString(request.RequestUri!.AbsolutePath).TrimStart('/'); // bucket/key
            var slash = path.IndexOf('/');
            var key = slash < 0 ? "" : path[(slash + 1)..];
            var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
            if (request.Method == HttpMethod.Get && query["list-type"] == "2")
            {
                var prefix = query["prefix"] ?? "";
                var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><ListBucketResult xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\"><Name>media</Name><IsTruncated>false</IsTruncated>");
                foreach (var name in Objects.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)))
                    xml.Append($"<Contents><Key>{name}</Key><Size>{Objects[name].Body.Length}</Size></Contents>");
                xml.Append("</ListBucketResult>");
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(xml.ToString(), Encoding.UTF8, "application/xml") };
            }
            if (request.Method == HttpMethod.Put)
            {
                var body = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(ct);
                if (payloadHash.StartsWith("STREAMING-", StringComparison.Ordinal)) body = DecodeChunks(body);
                var meta = request.Headers.Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
                    .Where(h => h.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase)).ToDictionary(h => h.Key[11..], h => h.Value.First());
                Objects[key] = new Stored(body, request.Content?.Headers.ContentType?.ToString() ?? "application/octet-stream", meta);
                var ok = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([]) };
                ok.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"etag\"");
                return ok;
            }
            if (request.Method == HttpMethod.Delete) { Objects.TryRemove(key, out _); return new HttpResponseMessage(HttpStatusCode.NoContent); }
            if (!Objects.TryGetValue(key, out var o))
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("<Error><Code>NoSuchKey</Code><Message>missing</Message></Error>", Encoding.UTF8, "application/xml")
                };
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(request.Method == HttpMethod.Head ? [] : o.Body) };
            response.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(o.ContentType);
            response.Content.Headers.ContentLength = o.Body.Length;
            // Like S3: the ETag of a simple upload is the MD5 of the body (the SDK checks downloads against it).
            response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"" + Convert.ToHexString(MD5.HashData(o.Body)).ToLowerInvariant() + "\"");
            foreach (var (k, v) in o.Meta) response.Headers.Add("x-amz-meta-" + k, v);
            return response;
        }

        /// <summary>aws-chunked: "&lt;hex size&gt;;chunk-signature=…\r\n&lt;data&gt;\r\n" repeated, ending with a zero-size chunk.</summary>
        private static byte[] DecodeChunks(byte[] raw)
        {
            var text = Encoding.Latin1.GetString(raw);
            var output = new MemoryStream();
            for (var i = 0; i < text.Length;)
            {
                var eol = text.IndexOf("\r\n", i, StringComparison.Ordinal);
                var size = Convert.ToInt32(text[i..eol].Split(';')[0], 16);
                if (size == 0) break;
                output.Write(raw, eol + 2, size);
                i = eol + 2 + size + 2;
            }
            return output.ToArray();
        }
    }

    private sealed class S3HttpFactory(HttpMessageHandler handler) : HttpClientFactory
    {
        public override HttpClient CreateHttpClient(IClientConfig clientConfig) => new(handler, disposeHandler: false);
        public override bool UseSDKHttpClientCaching(IClientConfig clientConfig) => false;
        public override bool DisposeHttpClientsAfterUse(IClientConfig clientConfig) => false;
    }

    // The well-known Azurite development key (public): lets the SDK sign requests and SAS links locally.
    private const string DevStorage = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";

    private static (IObjectStore Store, object Fake) Create(string provider, string root, bool unsignedPayload = false)
    {
        switch (provider)
        {
            case "FileSystem":
                return (new FileSystemObjectStore(root), new object());
            case "AzureBlob":
            {
                var fake = new FakeAzure();
                var options = new BlobClientOptions { Transport = new HttpClientTransport(new HttpClient(fake)), Retry = { MaxRetries = 0 } };
                return (new AzureBlobObjectStore(new AzureBlobStoreOptions { ConnectionString = DevStorage, Container = "media" }, options), fake);
            }
            default:
            {
                var fake = new FakeS3();
                var config = new AmazonS3Config { HttpClientFactory = new S3HttpFactory(fake), MaxErrorRetry = 0 };
                return (new S3ObjectStore(new S3StoreOptions
                {
                    Bucket = "media", ServiceUrl = unsignedPayload ? "https://s3.test" : "http://s3.test", AccessKeyId = "AKIATEST", SecretAccessKey = "secret",
                    DisablePayloadSigning = unsignedPayload
                }, config), fake);
            }
        }
    }

    private static string TempRoot() => Path.Combine(Path.GetTempPath(), "edusphere-objectstore-" + Guid.NewGuid().ToString("N"));

    // ── behaviour shared by every store ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("FileSystem")]
    [InlineData("AzureBlob")]
    [InlineData("S3")]
    public async Task Stores_PutReadDescribeAndDeleteObjects(string provider)
    {
        var root = TempRoot();
        var (store, _) = Create(provider, root);
        Assert.Equal(provider, store.Provider);
        var body = Encoding.UTF8.GetBytes("Hello, EduSphere");
        await store.PutAsync("submissions/2026/10/a.txt", new MemoryStream(body), "text/plain", new Dictionary<string, string> { ["content-sha256"] = "abc" });

        var info = await store.HeadAsync("submissions/2026/10/a.txt");
        Assert.NotNull(info);
        Assert.Equal(body.Length, info!.Length);
        Assert.StartsWith("text/plain", info.ContentType);
        Assert.Equal("abc", info.Metadata["content-sha256"]);

        await using (var item = await store.GetAsync("submissions/2026/10/a.txt"))
        {
            using var reader = new StreamReader(item!.Content);
            Assert.Equal("Hello, EduSphere", await reader.ReadToEndAsync());
        }
        Assert.Null(await store.GetAsync("submissions/missing.txt"));
        Assert.Null(await store.HeadAsync("submissions/missing.txt"));

        await store.PutAsync("scorm/p1/index.html", new MemoryStream([1]), "text/html");
        await store.PutAsync("scorm/p1/img/a.png", new MemoryStream([2]), "image/png");
        await store.PutAsync("scorm/p2/index.html", new MemoryStream([3]), "text/html");
        await store.DeletePrefixAsync("scorm/p1/");
        Assert.Null(await store.HeadAsync("scorm/p1/index.html"));
        Assert.Null(await store.HeadAsync("scorm/p1/img/a.png"));
        Assert.NotNull(await store.HeadAsync("scorm/p2/index.html"));

        await store.DeleteAsync("submissions/2026/10/a.txt");
        Assert.Null(await store.HeadAsync("submissions/2026/10/a.txt"));
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task FileSystemStore_RefusesKeysOutsideItsFolder()
    {
        var store = new FileSystemObjectStore(TempRoot());
        await Assert.ThrowsAsync<ArgumentException>(() => store.PutAsync("../escape.txt", new MemoryStream([1]), "text/plain"));
        Assert.Null(await store.GetReadUrlAsync("a.txt", TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public async Task CloudStores_IssueSignedReadLinks_AndSignRequests()
    {
        var (azure, _) = Create("AzureBlob", "");
        var sas = await azure.GetReadUrlAsync("submissions/a.pdf", TimeSpan.FromMinutes(10), "Essay.pdf");
        Assert.Contains("sig=", sas);
        Assert.Contains("sp=r", sas);
        Assert.Contains("submissions/a.pdf", sas);

        var (s3, fake) = Create("S3", "");
        var url = await s3.GetReadUrlAsync("submissions/a.pdf", TimeSpan.FromMinutes(10), "Essay.pdf");
        Assert.Contains("X-Amz-Signature=", url);
        Assert.Contains("response-content-disposition=", url);
        await s3.PutAsync("x.txt", new MemoryStream([1]), "text/plain");
        Assert.StartsWith("AWS4-HMAC-SHA256 Credential=AKIATEST/", ((FakeS3)fake).Authorizations.Last());
        Assert.StartsWith("STREAMING-", ((FakeS3)fake).PayloadHashes.Last());
    }

    [Fact]
    public async Task S3Store_CanUploadWithAnUnsignedPayload_ForS3CompatibleServices()
    {
        var (store, fake) = Create("S3", "", unsignedPayload: true);
        await store.PutAsync("r2/a.txt", new MemoryStream(Encoding.UTF8.GetBytes("plain body")), "text/plain");
        Assert.Equal("UNSIGNED-PAYLOAD", ((FakeS3)fake).PayloadHashes.Last());
        Assert.Equal("plain body", Encoding.UTF8.GetString(((FakeS3)fake).Objects["r2/a.txt"].Body));
    }

    // ── media storage and SCORM on top ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("AzureBlob")]
    [InlineData("S3")]
    public async Task MediaStorage_KeepsHashTypeAndFileName_InTheSharedStore(string provider)
    {
        var (store, _) = Create(provider, "");
        var media = new ObjectStoreMediaStorageService(store, Options.Create(new MediaStorageOptions { KeyPrefix = "tenant-a" }));
        var bytes = Encoding.UTF8.GetBytes("%PDF-1.7 essay");
        var saved = await media.SaveAsync(new MemoryStream(bytes), "submissions", "PDF", "application/pdf", "C:\\Users\\ann\\Essay final.pdf");
        Assert.Matches(@"^tenant-a/submissions/\d{4}/\d{2}/[0-9a-f]{32}\.pdf$", saved.StorageKey);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), saved.ContentHashSha256);
        Assert.Equal(bytes.Length, saved.Length);
        Assert.Equal("Essay final.pdf", saved.DownloadFileName);

        Assert.Equal(bytes, await media.ReadAsBytesAsync(saved.StorageKey));
        var meta = await media.GetMetadataAsync(saved.StorageKey);
        Assert.Equal(("application/pdf", "Essay final.pdf", saved.ContentHashSha256), (meta!.ContentType, meta.DownloadFileName, meta.ContentHashSha256));
        Assert.NotNull(await media.GenerateTemporaryReadUrlAsync(saved.StorageKey, TimeSpan.FromMinutes(5)));
        await media.DeleteAsync(saved.StorageKey);
        Assert.Null(await media.ReadAsBytesAsync(saved.StorageKey));
        Assert.Null(await media.GenerateTemporaryReadUrlAsync(saved.StorageKey, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public async Task ScormFiles_LiveUnderTheirPackage_InTheSharedStore()
    {
        var (store, fake) = Create("S3", "");
        var scorm = new ObjectStoreScormContentStore(store, "tenant-a");
        var package = Guid.NewGuid();
        await scorm.SaveAsync(package, "sco/index.html", new MemoryStream(Encoding.UTF8.GetBytes("<html>sco</html>")));
        Assert.Contains($"tenant-a/scorm/{package:N}/sco/index.html", ((FakeS3)fake).Objects.Keys);
        await using (var stream = await scorm.OpenAsync(package, "sco/index.html"))
        {
            using var reader = new StreamReader(stream!);
            Assert.Equal("<html>sco</html>", await reader.ReadToEndAsync());
        }
        Assert.Null(await scorm.OpenAsync(package, "missing.js"));
        await scorm.DeleteAsync(package);
        Assert.Empty(((FakeS3)fake).Objects);
    }
}
