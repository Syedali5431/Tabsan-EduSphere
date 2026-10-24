using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Tabsan.EduSphere.Infrastructure.Auth;

/// <summary>
/// The SAML service provider's own certificate (RSA-2048, self-signed): signs AuthnRequests and decrypts encrypted
/// assertions. Created on first use and kept outside the repository (<c>AuthSecurity:Sso:SpCertificateFile</c>, default
/// &lt;app-data&gt;/Tabsan/saml-sp-certificate.json). Every API node must share the file; replacing it means the
/// service-provider metadata has to be registered at the identity providers again.
/// </summary>
public sealed class SamlServiceProviderCertificate
{
    private readonly string _file;
    private readonly ILogger<SamlServiceProviderCertificate>? _logger;
    private readonly object _gate = new();
    private X509Certificate2? _certificate;

    public SamlServiceProviderCertificate(string? file = null, ILogger<SamlServiceProviderCertificate>? logger = null)
    {
        _file = string.IsNullOrWhiteSpace(file) ? DefaultFile() : file;
        _logger = logger;
    }

    public static string DefaultFile()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "Tabsan", "saml-sp-certificate.json");

    /// <summary>The certificate with its private key.</summary>
    public X509Certificate2 Certificate
    {
        get
        {
            lock (_gate)
                return _certificate ??= Load();
        }
    }

    /// <summary>The public part only (for metadata).</summary>
    public X509Certificate2 PublicCertificate => new(Certificate.Export(X509ContentType.Cert));

    private sealed class Stored
    {
        public string CertificatePem { get; set; } = default!;
        public string PrivateKeyPem { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
    }

    private X509Certificate2 Load()
    {
        Stored? stored = null;
        if (File.Exists(_file))
            stored = JsonSerializer.Deserialize<Stored>(File.ReadAllText(_file));

        if (stored is null)
        {
            stored = Generate();
            Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
            var temp = _file + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(stored));
            File.Move(temp, _file, overwrite: true);
            _logger?.LogInformation("Generated the SAML service-provider certificate in {File}. Back this file up.", _file);
        }

        using var pem = X509Certificate2.CreateFromPem(stored.CertificatePem, stored.PrivateKeyPem);
        // Round-trip through PFX so the key is usable by SignedXml / EncryptedXml on every platform.
        return new X509Certificate2(pem.Export(X509ContentType.Pfx), (string?)null, X509KeyStorageFlags.Exportable);
    }

    private static Stored Generate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Tabsan EduSphere SAML service provider", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: false));
        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(10));
        return new Stored
        {
            CertificatePem = cert.ExportCertificatePem(),
            PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
