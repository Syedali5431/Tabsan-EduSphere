using System.Text.Json;

namespace Tabsan.Lic.Crypto;

/// <summary>
/// Signing key material for Tabsan-Lic.
///
/// The RSA private key (signs license payloads) and the AES-256 key (encrypts the payload)
/// are NOT compiled into source control. They are loaded at runtime from a vendor-only
/// JSON file:
///
///   1. the path in the <c>TABSAN_LIC_KEYS</c> environment variable, or
///   2. <c>%APPDATA%\Tabsan\signing-keys.json</c> (default, next to tabsan_lic.db).
///
/// File shape: <c>{ "rsaPrivateKeyPem": "-----BEGIN RSA PRIVATE KEY-----...", "aesKeyBase64": "..." }</c>
///
/// Keep this file (and a secure offline backup of it) only on vendor-controlled machines.
/// Anyone holding the private key can forge licenses; losing it means no new licenses can be
/// issued for already-deployed EduSphere builds.
/// </summary>
internal static class EmbeddedKeys
{
    internal const string KeysPathEnvVar = "TABSAN_LIC_KEYS";

    private static readonly Lazy<KeyFile> _keys = new(Load);

    /// <summary>RSA-2048 private key in PKCS#1 PEM format.</summary>
    internal static string RsaPrivateKeyPem => _keys.Value.RsaPrivateKeyPem!;

    /// <summary>AES-256 key shared with EduSphere (Base64, 32 bytes).</summary>
    internal static string AesKeyBase64 => _keys.Value.AesKeyBase64!;

    internal static string ResolveKeysPath()
    {
        var fromEnv = Environment.GetEnvironmentVariable(KeysPathEnvVar);
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv;

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Tabsan", "signing-keys.json");
    }

    private static KeyFile Load()
    {
        var path = ResolveKeysPath();
        if (!File.Exists(path))
            throw new InvalidOperationException(
                $"Signing key file not found at '{path}'. Copy the vendor signing-keys.json there " +
                $"or set the {KeysPathEnvVar} environment variable to its location.");

        var keys = JsonSerializer.Deserialize<KeyFile>(File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (keys is null || string.IsNullOrWhiteSpace(keys.RsaPrivateKeyPem) || string.IsNullOrWhiteSpace(keys.AesKeyBase64))
            throw new InvalidOperationException($"Signing key file '{path}' is missing rsaPrivateKeyPem or aesKeyBase64.");

        if (Convert.FromBase64String(keys.AesKeyBase64).Length != 32)
            throw new InvalidOperationException($"Signing key file '{path}' has an AES key that is not 256 bits.");

        return keys;
    }

    private sealed class KeyFile
    {
        public string? RsaPrivateKeyPem { get; set; }
        public string? AesKeyBase64 { get; set; }
    }
}
