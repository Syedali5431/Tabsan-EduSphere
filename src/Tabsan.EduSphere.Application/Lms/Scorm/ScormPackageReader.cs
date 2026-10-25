using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Application.Lms.Scorm;

/// <summary>A package that cannot be used; the message is shown to the teacher.</summary>
public sealed class ScormPackageException(string message) : Exception(message);

/// <summary>
/// What the manifest says: version, title, the SCO to launch first and its passing score, and every launchable part of the
/// default organization in order (more than one for multi-part packages).
/// </summary>
public sealed record ScormManifest(ScormVersion Version, string? Identifier, string? Title, string LaunchPath, decimal? MasteryScore, int ScoCount,
    IReadOnlyList<ScormPart>? Parts = null);

/// <summary>A file to extract: its path in the package (after the base folder) and its zip entry.</summary>
public sealed record ScormPackageEntry(string Path, ZipArchiveEntry Entry);

public sealed record ScormPackageContents(ScormManifest Manifest, IReadOnlyList<ScormPackageEntry> Files, long TotalBytes);

/// <summary>
/// Plan M13 — checks an uploaded SCORM zip and reads its imsmanifest.xml. Rejects paths outside the package (zip-slip),
/// absolute paths, too many files, too much data after extraction and suspicious compression ratios (zip bombs); the
/// manifest is read without DTDs or external entities.
/// </summary>
public static class ScormPackageReader
{
    private const string ManifestName = "imsmanifest.xml";
    private const long BombMinimumBytes = 10L * 1024 * 1024;
    private const int BombRatio = 200;

    public static ScormPackageContents Read(ZipArchive zip, ScormOptions limits)
    {
        if (zip.Entries.Count > limits.MaxFiles)
            throw new ScormPackageException($"The package has more than {limits.MaxFiles:N0} files.");

        var files = new List<(string Path, ZipArchiveEntry Entry)>();
        long total = 0;
        var maxTotal = (long)limits.MaxUncompressedMb * 1024 * 1024;
        foreach (var entry in zip.Entries)
        {
            var path = NormalizeEntryPath(entry.FullName);
            if (path.EndsWith('/')) continue; // a folder
            if (entry.Length < 0) throw new ScormPackageException("The package is damaged.");
            if (entry.Length > BombMinimumBytes && entry.CompressedLength > 0 && entry.Length / entry.CompressedLength > BombRatio)
                throw new ScormPackageException($"\"{path}\" is compressed suspiciously well; the package was rejected.");
            total += entry.Length;
            if (total > maxTotal) throw new ScormPackageException($"The package's files come to more than {limits.MaxUncompressedMb:N0} MB.");
            files.Add((path, entry));
        }

        // imsmanifest.xml at the top, or inside one folder when the package was zipped with its folder.
        var basePath = "";
        var manifest = files.FirstOrDefault(f => f.Path.Equals(ManifestName, StringComparison.OrdinalIgnoreCase));
        if (manifest.Entry is null)
        {
            var nested = files.Where(f => f.Path.EndsWith("/" + ManifestName, StringComparison.OrdinalIgnoreCase) && f.Path.Count(c => c == '/') == 1).ToList();
            if (nested.Count != 1) throw new ScormPackageException("This is not a SCORM package: imsmanifest.xml is missing.");
            manifest = nested[0];
            basePath = manifest.Path[..(manifest.Path.Length - ManifestName.Length)];
        }

        var contents = files.Where(f => f.Path.StartsWith(basePath, StringComparison.Ordinal))
            .Select(f => new ScormPackageEntry(f.Path[basePath.Length..], f.Entry)).ToList();
        var duplicate = contents.GroupBy(f => f.Path, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null) throw new ScormPackageException($"The package contains \"{duplicate.Key}\" twice.");

        XDocument document;
        using (var stream = manifest.Entry.Open())
            document = LoadManifest(stream);
        var parsed = ParseManifest(document);

        foreach (var launchPath in (parsed.Parts ?? []).Select(p => p.LaunchPath).Prepend(parsed.LaunchPath).Distinct())
        {
            var launchFile = Uri.UnescapeDataString(launchPath.Split('?', '#')[0]);
            if (!contents.Any(f => f.Path.Equals(launchFile, StringComparison.Ordinal)))
                throw new ScormPackageException($"The manifest launches \"{launchFile}\", which is not in the package.");
        }
        return new ScormPackageContents(parsed, contents, contents.Sum(f => f.Entry.Length));
    }

    /// <summary>Forward slashes, no "./"; rejects absolute paths, "..", drive letters and control characters.</summary>
    public static string NormalizeEntryPath(string name)
    {
        var path = name.Replace('\\', '/');
        while (path.StartsWith("./", StringComparison.Ordinal)) path = path[2..];
        if (path.Length == 0 || path.Length > 400 || path.StartsWith('/') || path.Contains(':') || path.Any(char.IsControl))
            throw new ScormPackageException($"The package contains an invalid file name: \"{Shorten(name)}\".");
        var segments = path.TrimEnd('/').Split('/');
        if (segments.Any(s => s is ".." or "." || s.Length == 0))
            throw new ScormPackageException($"The package contains a file outside its folder: \"{Shorten(name)}\".");
        return path;
    }

    public static XDocument LoadManifest(Stream stream)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 20_000_000,
            IgnoreComments = true
        };
        try
        {
            using var reader = XmlReader.Create(stream, settings);
            return XDocument.Load(reader);
        }
        catch (XmlException ex)
        {
            throw new ScormPackageException($"imsmanifest.xml cannot be read: {ex.Message}");
        }
    }

    public static ScormManifest ParseManifest(XDocument document)
    {
        var root = document.Root ?? throw new ScormPackageException("imsmanifest.xml is empty.");
        if (root.Name.LocalName != "manifest") throw new ScormPackageException("imsmanifest.xml does not describe a package.");

        var version = DetectVersion(root);
        var organizations = Child(root, "organizations");
        var resources = Child(root, "resources") ?? throw new ScormPackageException("imsmanifest.xml lists no resources.");
        var resourceList = Children(resources, "resource").ToList();
        var byId = resourceList.Where(r => Attr(r, "identifier") is not null).GroupBy(r => Attr(r, "identifier")!).ToDictionary(g => g.Key, g => g.First());

        bool IsSco(XElement r) => string.Equals(AdlcpAttr(r, "scormtype") ?? AdlcpAttr(r, "scormType"), "sco", StringComparison.OrdinalIgnoreCase);

        var organization = organizations is null ? null
            : Children(organizations, "organization").FirstOrDefault(o => Attr(o, "identifier") == Attr(organizations, "default"))
              ?? Children(organizations, "organization").FirstOrDefault();
        var items = organization is null ? [] : organization.Descendants().Where(e => e.Name.LocalName == "item").ToList();
        var launchable = items.Where(i => Attr(i, "identifierref") is { } r && byId.TryGetValue(r, out var res) && Attr(res, "href") is not null).ToList();
        var scoItems = launchable.Where(i => IsSco(byId[Attr(i, "identifierref")!])).ToList();
        var item = scoItems.FirstOrDefault() ?? launchable.FirstOrDefault();

        XElement resource;
        if (item is not null) resource = byId[Attr(item, "identifierref")!];
        else resource = resourceList.FirstOrDefault(r => IsSco(r) && Attr(r, "href") is not null)
                        ?? resourceList.FirstOrDefault(r => Attr(r, "href") is not null)
                        ?? throw new ScormPackageException("imsmanifest.xml does not say which file to launch.");
        var launch = LaunchOf(root, resources, resource, item);

        // Every launchable item is a part the learner can open (items without a resource only group others).
        if (launchable.Count > ScormPackage.MaxParts)
            throw new ScormPackageException($"The package has more than {ScormPackage.MaxParts} parts.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var parts = launchable.Select((i, n) =>
        {
            var res = byId[Attr(i, "identifierref")!];
            var id = Attr(i, "identifier") is { Length: > 0 and <= 100 } given ? given : $"part-{n + 1}";
            while (!ids.Add(id)) id += "-" + (n + 1);
            var partTitle = Child(i, "title")?.Value.Trim() is { Length: > 0 } pt ? (pt.Length > 200 ? pt[..200] : pt) : $"Part {n + 1}";
            return new ScormPart(id, partTitle, LaunchOf(root, resources, res, i), IsSco(res), MasteryScore(i, version));
        }).ToList();

        var title = (organization is null ? null : Child(organization, "title")?.Value.Trim()) is { Length: > 0 } t ? t : null;
        return new ScormManifest(version, Attr(root, "identifier"), title, launch, MasteryScore(item, version), Math.Max(1, scoItems.Count), parts);
    }

    /// <summary>A resource's launch path (xml:base applied, the item's parameters added), checked to stay inside the package.</summary>
    private static string LaunchOf(XElement root, XElement resources, XElement resource, XElement? item)
    {
        var href = Attr(resource, "href")!.Trim();
        if (Uri.TryCreate(href, UriKind.Absolute, out var absolute) && absolute.Scheme is "http" or "https" or "ftp" or "file" or "javascript" or "data")
            throw new ScormPackageException("The package launches content from another website. Upload a package that contains its own files.");
        var launch = CombineBase(XmlBase(root), XmlBase(resources), XmlBase(resource), href);
        if (item is not null && Attr(item, "parameters") is { Length: > 0 } parameters) launch = AppendParameters(launch, parameters.Trim());
        NormalizeEntryPath(Uri.UnescapeDataString(launch.Split('?', '#')[0]));
        return launch;
    }

    private static ScormVersion DetectVersion(XElement root)
    {
        var schemaVersion = root.Elements().FirstOrDefault(e => e.Name.LocalName == "metadata")?
            .Elements().FirstOrDefault(e => e.Name.LocalName == "schemaversion")?.Value.Trim();
        if (schemaVersion is { Length: > 0 })
        {
            if (schemaVersion.Contains("2004", StringComparison.Ordinal) || schemaVersion.Contains("1.3", StringComparison.Ordinal)) return ScormVersion.Scorm2004;
            if (schemaVersion.StartsWith("1.2", StringComparison.Ordinal)) return ScormVersion.Scorm12;
        }
        var namespaces = root.DescendantsAndSelf().Attributes().Where(a => a.IsNamespaceDeclaration).Select(a => a.Value).ToList();
        if (namespaces.Any(n => n.Contains("adlcp_v1p3", StringComparison.OrdinalIgnoreCase) || n.Contains("imsss", StringComparison.OrdinalIgnoreCase)))
            return ScormVersion.Scorm2004;
        return ScormVersion.Scorm12;
    }

    private static decimal? MasteryScore(XElement? item, ScormVersion version)
    {
        if (item is null) return null;
        if (version == ScormVersion.Scorm12)
        {
            var text = item.Elements().FirstOrDefault(e => e.Name.LocalName == "masteryscore")?.Value.Trim();
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var score) && score is >= 0 and <= 100 ? score : null;
        }
        var primary = item.Descendants().FirstOrDefault(e => e.Name.LocalName == "primaryObjective");
        if (primary is null || !string.Equals(Attr(primary, "satisfiedByMeasure"), "true", StringComparison.OrdinalIgnoreCase)) return null;
        var measure = primary.Elements().FirstOrDefault(e => e.Name.LocalName == "minNormalizedMeasure")?.Value.Trim();
        return decimal.TryParse(measure, NumberStyles.Number, CultureInfo.InvariantCulture, out var m) && m is >= -1 and <= 1 ? Math.Round(Math.Max(m, 0) * 100, 2) : null;
    }

    /// <summary>SCORM item parameters: "?a=1" joins an href that already has a query with "&amp;"; "#x" is appended.</summary>
    public static string AppendParameters(string href, string parameters)
    {
        if (parameters.StartsWith('#')) return href.Contains('#') ? href : href + parameters;
        var p = parameters.TrimStart('?', '&');
        if (p.Length == 0) return href;
        var hash = href.IndexOf('#');
        var (main, fragment) = hash >= 0 ? (href[..hash], href[hash..]) : (href, "");
        return main + (main.Contains('?') ? "&" : "?") + p + fragment;
    }

    private static string CombineBase(params string?[] parts)
    {
        var path = "";
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part)) continue;
            var p = part.Trim().Replace('\\', '/');
            path = p.StartsWith('/') ? p.TrimStart('/') : path.Length == 0 || path.EndsWith('/') ? path + p : path + "/" + p;
        }
        while (path.StartsWith("./", StringComparison.Ordinal)) path = path[2..];
        return path;
    }

    private static string? XmlBase(XElement e) => e.Attribute(XNamespace.Xml + "base")?.Value;

    private static XElement? Child(XElement e, string localName) => e.Elements().FirstOrDefault(x => x.Name.LocalName == localName);

    private static IEnumerable<XElement> Children(XElement e, string localName) => e.Elements().Where(x => x.Name.LocalName == localName);

    private static string? Attr(XElement e, string localName)
        => e.Attributes().FirstOrDefault(a => a.Name.LocalName == localName && a.Name.Namespace == XNamespace.None)?.Value;

    private static string? AdlcpAttr(XElement e, string localName)
        => e.Attributes().FirstOrDefault(a => a.Name.LocalName == localName && a.Name.Namespace != XNamespace.None)?.Value;

    private static string Shorten(string s) => s.Length > 80 ? s[..80] + "…" : s;
}
