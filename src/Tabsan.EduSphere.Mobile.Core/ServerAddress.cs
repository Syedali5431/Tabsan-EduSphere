using System.Net;
using System.Net.Sockets;

namespace Tabsan.EduSphere.Mobile.Core;

/// <summary>
/// The institute's EduSphere server as people type it: "edu.myschool.com", "https://api.myschool.edu", "192.168.1.10:5181",
/// or an address copied from the browser with "/api/v1/…" or "/swagger" on the end. One app serves every institute: each
/// person enters their institute's server once (the institute's IT team gives it out), and the app checks it before use.
/// Plain http is accepted only for servers on the institute's own network (private addresses, intranet names);
/// servers on the internet must use https, because passwords and student records travel over the connection.
/// </summary>
public static class ServerAddress
{
    /// <summary>
    /// The addresses to try, best first (without a trailing slash), or why the text is not usable (an <see cref="AppText"/> key).
    /// Without a scheme, https is tried first; servers on the local network are also tried over http.
    /// </summary>
    public static (IReadOnlyList<string> Candidates, string? Error) Parse(string? text)
    {
        var raw = (text ?? "").Trim();
        if (raw.Length == 0) return ([], "EnterServer");
        if (raw.Any(char.IsWhiteSpace)) return ([], "InvalidServer");
        var hasScheme = raw.Contains("://", StringComparison.Ordinal);
        if (!Uri.TryCreate(hasScheme ? raw : "https://" + raw, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo))
            return ([], "InvalidServer");

        var local = IsLocalNetwork(uri);
        if (uri.Scheme == Uri.UriSchemeHttp && !local) return ([], "HttpsRequired");

        var path = BasePath(uri.AbsolutePath);
        string Address(string scheme) => $"{scheme}://{Authority(uri, scheme)}{path}";
        if (hasScheme) return ([Address(uri.Scheme)], null);
        return (local ? [Address(Uri.UriSchemeHttps), Address(Uri.UriSchemeHttp)] : [Address(Uri.UriSchemeHttps)], null);
    }

    /// <summary>The address as shown in the app: without "https://" (http stays visible: it is not encrypted).</summary>
    public static string Display(string? server)
        => string.IsNullOrWhiteSpace(server) ? "" : server.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? server[8..] : server;

    /// <summary>Whether the address is plain http (only allowed on the institute's own network).</summary>
    public static bool IsUnencrypted(string? server) => server?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// A server on the institute's own network: private and loopback IP addresses (also the Android emulator's 10.0.2.2),
    /// link-local and unique-local IPv6, carrier-grade NAT (VPNs such as Tailscale), intranet names without a dot and
    /// .local / .lan / .internal / .home.arpa / localhost names.
    /// </summary>
    public static bool IsLocalNetwork(Uri uri)
    {
        var host = uri.Host.Trim('[', ']').ToLowerInvariant();
        if (IPAddress.TryParse(host, out var ip)) return IsPrivate(ip);
        if (!host.Contains('.')) return true;
        return host is "localhost" || new[] { ".localhost", ".local", ".lan", ".internal", ".intranet", ".home.arpa" }.Any(host.EndsWith);
    }

    private static bool IsPrivate(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip)) return true;
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal;
        var b = ip.GetAddressBytes();
        return b[0] == 10
            || (b[0] == 172 && b[1] is >= 16 and <= 31)
            || (b[0] == 192 && b[1] == 168)
            || (b[0] == 169 && b[1] == 254)
            || (b[0] == 100 && b[1] is >= 64 and <= 127);
    }

    /// <summary>Host and port; a port only when one was typed (the scheme's own port is left out).</summary>
    private static string Authority(Uri uri, string scheme)
    {
        var host = uri.HostNameType == UriHostNameType.IPv6 && !uri.Host.StartsWith('[') ? $"[{uri.Host}]" : uri.Host;
        return uri.IsDefaultPort ? host : $"{host}:{uri.Port}";
    }

    /// <summary>The API's own path prefix (an API hosted under a folder), without what was copied from the browser after it.</summary>
    private static string BasePath(string path)
    {
        var p = path.TrimEnd('/');
        foreach (var marker in new[] { "/api/", "/swagger/" })
        {
            var at = (p + "/").IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (at >= 0) p = p[..at];
        }
        return p.TrimEnd('/');
    }
}

/// <summary>The outcome of checking a server address: the server to use and its app configuration, or the reason, for the user.</summary>
public sealed record ServerCheck(string? Server, MobileConfig? Config, string? Error)
{
    public bool IsEduSphere => Error is null && Server is not null;
}
