using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Application.Lms.Scorm;

namespace Tabsan.EduSphere.Infrastructure.Lms;

/// <summary>Plan M14.2 — SCORM package files in the shared object store (scorm/{packageId}/{path}) for several API instances.</summary>
public sealed class ObjectStoreScormContentStore(IObjectStore store, string? keyPrefix = null) : IScormContentStore
{
    private readonly string _prefix = string.IsNullOrWhiteSpace(keyPrefix) ? "scorm/" : keyPrefix.Trim().Trim('/') + "/scorm/";

    public Task SaveAsync(Guid packageId, string path, Stream content, CancellationToken ct = default)
        => store.PutAsync(Key(packageId, path), content, "application/octet-stream", null, ct);

    public async Task<Stream?> OpenAsync(Guid packageId, string path, CancellationToken ct = default)
        => (await store.GetAsync(Key(packageId, path), ct))?.Content;

    public Task DeleteAsync(Guid packageId, CancellationToken ct = default) => store.DeletePrefixAsync($"{_prefix}{packageId:N}/", ct);

    private string Key(Guid packageId, string path) => $"{_prefix}{packageId:N}/{path.Replace('\\', '/').TrimStart('/')}";
}
