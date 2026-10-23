using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Lms.Scorm;

namespace Tabsan.EduSphere.Infrastructure.Lms;

/// <summary>
/// Plan M13 — extracted SCORM packages on disk: {StoragePath}/{packageId}/{path}. Every path is resolved and checked to stay
/// inside the package's folder.
/// </summary>
public sealed class LocalScormContentStore : IScormContentStore
{
    private readonly string _root;

    public LocalScormContentStore(IOptions<ScormOptions> options, string contentRootPath)
    {
        var configured = options.Value.StoragePath;
        _root = Path.GetFullPath(Path.IsPathRooted(configured) ? configured : Path.Combine(contentRootPath, configured));
    }

    public async Task SaveAsync(Guid packageId, string path, Stream content, CancellationToken ct = default)
    {
        var full = Resolve(packageId, path) ?? throw new IOException("Invalid path in the package.");
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var file = new FileStream(full, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, ct);
    }

    public Task<Stream?> OpenAsync(Guid packageId, string path, CancellationToken ct = default)
    {
        var full = Resolve(packageId, path);
        if (full is null || !File.Exists(full)) return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));
    }

    public Task DeleteAsync(Guid packageId, CancellationToken ct = default)
    {
        var folder = Path.Combine(_root, packageId.ToString("N"));
        if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        return Task.CompletedTask;
    }

    private string? Resolve(Guid packageId, string path)
    {
        var folder = Path.Combine(_root, packageId.ToString("N")) + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(Path.Combine(folder, path.Replace('/', Path.DirectorySeparatorChar)));
        return full.StartsWith(folder, StringComparison.OrdinalIgnoreCase) ? full : null;
    }
}
