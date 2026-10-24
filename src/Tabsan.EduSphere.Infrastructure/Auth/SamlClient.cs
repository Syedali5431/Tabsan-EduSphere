using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using ITfoxtec.Identity.Saml2.Cryptography;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using Microsoft.IdentityModel.Tokens.Saml2;
using Tabsan.EduSphere.Application.Interfaces;
using SamlHttpRequest = ITfoxtec.Identity.Saml2.Http.HttpRequest;

namespace Tabsan.EduSphere.Infrastructure.Auth;

/// <summary>
/// Plan M1 Stage M1.3 — SAML 2.0 service provider built on ITfoxtec.Identity.Saml2.
/// Trust is anchored in the IdP metadata document configured by the institution admin
/// (signing certificates + entity ID); metadata is cached per URL and refreshed hourly,
/// and once immediately when a signature fails to validate (IdP certificate rollover).
///
/// On top of the library's checks (signature, issuer, audience, lifetime, replay) this class
/// enforces success status, Destination, and that a signed bearer SubjectConfirmation carries
/// our ACS as Recipient and our request ID as InResponseTo — IdP-initiated (unsolicited)
/// responses are therefore rejected.
///
/// With the service-provider certificate (<see cref="SamlServiceProviderCertificate"/>) AuthnRequests can be signed for
/// IdPs that require it, encrypted assertions are decrypted, and the metadata publishes the certificate for both uses.
/// </summary>
public sealed class SamlClient : ISamlClient
{
    private static readonly ConcurrentDictionary<string, (EntityDescriptor Descriptor, DateTimeOffset FetchedAt)> MetadataCache =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan MetadataTtl = TimeSpan.FromHours(1);

    // Attribute names seen across Entra ID, ADFS, Shibboleth / eduPerson and Google Workspace.
    private static readonly string[] EmailAttributes =
    [
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress",
        "urn:oid:0.9.2342.19200300.100.1.3",
        "email", "mail", "emailAddress"
    ];
    private static readonly string[] NameAttributes =
    [
        "http://schemas.microsoft.com/identity/claims/displayname",
        "urn:oid:2.16.840.1.113730.3.1.241",
        "displayName", "name",
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
    ];
    private static readonly string[] StableIdAttributes =
    [
        "http://schemas.microsoft.com/identity/claims/objectidentifier", // Entra object id
        "urn:oid:1.3.6.1.4.1.5923.1.1.1.10",                              // eduPersonTargetedID
        "urn:oid:1.3.6.1.4.1.5923.1.1.1.6"                                // eduPersonPrincipalName
    ];
    private const string EntraAuthMethodsAttribute = "http://schemas.microsoft.com/claims/authnmethodsreferences";

    private readonly HttpClient _http;
    private readonly SamlServiceProviderCertificate? _spCertificate;

    public SamlClient(HttpClient http, SamlServiceProviderCertificate? spCertificate = null)
    {
        _http = http;
        _spCertificate = spCertificate;
    }

    // ── AuthnRequest (HTTP-Redirect) ─────────────────────────────────────────

    public async Task<SamlAuthnRequest> BuildAuthnRequestAsync(
        string idpMetadataUrl, SamlServiceProvider sp, string relayState, bool signRequest = false, CancellationToken ct = default)
    {
        var idp = await GetIdpAsync(idpMetadataUrl, forceRefresh: false, ct);
        var sso = idp.IdPSsoDescriptor.SingleSignOnServices?
            .FirstOrDefault(s => s.Binding?.OriginalString == ProtocolBindings.HttpRedirect.OriginalString)
            ?? throw new SsoProtocolException("IdP metadata has no HTTP-Redirect SingleSignOnService.");

        var config = CreateConfig(idp, sp);
        config.SingleSignOnDestination = sso.Location;
        if (signRequest)
        {
            config.SigningCertificate = (_spCertificate ?? throw new SsoProtocolException("No SAML service-provider certificate is configured for signing.")).Certificate;
            config.SignAuthnRequest = true;
        }

        var request = new Saml2AuthnRequest(config)
        {
            AssertionConsumerServiceUrl = new Uri(sp.AcsUrl),
            ProtocolBinding = ProtocolBindings.HttpPost,
            NameIdPolicy = new NameIdPolicy { AllowCreate = true }
        };

        var binding = new Saml2RedirectBinding { RelayState = relayState };
        binding.Bind(request);

        return new SamlAuthnRequest(binding.RedirectLocation.OriginalString, request.IdAsString);
    }

    // ── Response (HTTP-POST) ─────────────────────────────────────────────────

    public async Task<ExternalIdentity> ValidateResponseAsync(
        string idpMetadataUrl, SamlServiceProvider sp, string samlResponse, string expectedRequestId,
        bool requireEncryptedAssertion = false, CancellationToken ct = default)
    {
        if (requireEncryptedAssertion && !HasEncryptedAssertion(samlResponse))
            throw new SsoProtocolException("SAML assertion is not encrypted, but this provider requires encrypted assertions.");

        var idp = await GetIdpAsync(idpMetadataUrl, forceRefresh: false, ct);
        try
        {
            return Validate(idp, sp, samlResponse, expectedRequestId, _spCertificate);
        }
        catch (InvalidSignatureException) when (!IsFresh(idpMetadataUrl))
        {
            // Signature did not match cached certificates — the IdP may have rolled its key.
            idp = await GetIdpAsync(idpMetadataUrl, forceRefresh: true, ct);
            return Validate(idp, sp, samlResponse, expectedRequestId, _spCertificate);
        }
        catch (InvalidSignatureException ex)
        {
            throw new SsoProtocolException("SAML signature validation failed: " + ex.Message, ex);
        }
    }

    private static ExternalIdentity Validate(EntityDescriptor idp, SamlServiceProvider sp, string samlResponse, string expectedRequestId,
        SamlServiceProviderCertificate? spCertificate)
    {
        var config = CreateConfig(idp, sp);
        if (spCertificate is not null)
            config.DecryptionCertificates.Add(spCertificate.Certificate);
        var request = new SamlHttpRequest
        {
            Method = "POST",
            Form = new NameValueCollection { ["SAMLResponse"] = samlResponse }
        };

        var binding = new Saml2PostBinding();
        var response = new Saml2AuthnResponse(config);

        try
        {
            binding.ReadSamlResponse(request, response);
            if (response.Status != Saml2StatusCodes.Success)
                throw new SsoProtocolException($"IdP returned SAML status {response.Status}.");

            binding.Unbind(request, response); // signature, issuer, audience, lifetime, replay
        }
        catch (SsoProtocolException) { throw; }
        catch (InvalidSignatureException) { throw; }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (ex.InnerException is InvalidSignatureException sig)
                throw sig;
            throw new SsoProtocolException("SAML response rejected: " + ex.Message, ex);
        }

        var assertion = response.Saml2SecurityToken?.Assertion
                        ?? throw new SsoProtocolException("SAML response has no assertion.");

        if (response.Destination is not null && !SameUrl(response.Destination.OriginalString, sp.AcsUrl))
            throw new SsoProtocolException("SAML response Destination does not match this ACS.");

        if (!string.IsNullOrEmpty(response.InResponseToAsString)
            && !string.Equals(response.InResponseToAsString, expectedRequestId, StringComparison.Ordinal))
            throw new SsoProtocolException("SAML response InResponseTo does not match the request.");

        // The assertion (not only the outer Response) must be bound to our request and ACS.
        var confirmed = assertion.Subject?.SubjectConfirmations?.Any(c =>
            c.Method?.OriginalString == "urn:oasis:names:tc:SAML:2.0:cm:bearer"
            && c.SubjectConfirmationData is { } data
            && string.Equals(data.InResponseTo?.Value, expectedRequestId, StringComparison.Ordinal)
            && data.Recipient is not null && SameUrl(data.Recipient.OriginalString, sp.AcsUrl)
            && (data.NotOnOrAfter is null || data.NotOnOrAfter > DateTime.UtcNow.AddMinutes(-2))) ?? false;
        if (!confirmed)
            throw new SsoProtocolException("SAML assertion has no bearer confirmation for this request and ACS.");

        return ToIdentity(response.ClaimsIdentity, assertion);
    }

    private static ExternalIdentity ToIdentity(ClaimsIdentity claims, Saml2Assertion assertion)
    {
        string? First(IEnumerable<string> types) => types
            .Select(t => claims.FindFirst(t)?.Value)
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        var nameId = assertion.Subject?.NameId;
        var nameIdFormat = nameId?.Format?.OriginalString;
        var nameIdUsable = nameId is not null
                           && !string.IsNullOrWhiteSpace(nameId.Value)
                           && nameIdFormat != NameIdentifierFormats.Transient.OriginalString;

        // Prefer an immutable directory id; a transient NameID can never identify a returning user.
        var subject = First(StableIdAttributes) ?? (nameIdUsable ? nameId!.Value : null);
        if (string.IsNullOrWhiteSpace(subject))
            throw new SsoProtocolException("SAML assertion has no persistent subject identifier.");

        var email = First(EmailAttributes)
                    ?? (nameIdFormat == NameIdentifierFormats.Email.OriginalString ? nameId!.Value : null);

        var authnContexts = assertion.Statements.OfType<Saml2AuthenticationStatement>()
            .Select(s => s.AuthenticationContext?.ClassReference?.OriginalString)
            .Concat(claims.FindAll(EntraAuthMethodsAttribute).Select(c => c.Value))
            .Where(v => !string.IsNullOrEmpty(v))
            .ToList();
        var mfa = authnContexts.Any(v =>
            v!.Contains("multipleauthn", StringComparison.OrdinalIgnoreCase)
            || v.Equals("https://refeds.org/profile/mfa", StringComparison.OrdinalIgnoreCase)
            || v.Contains("TwoFactor", StringComparison.OrdinalIgnoreCase)
            || v.Contains("MultiFactor", StringComparison.OrdinalIgnoreCase));

        // Every attribute (multi-valued ones such as eduPersonAffiliation keep all values) for ID / role mapping.
        var attributes = claims.Claims
            .GroupBy(c => c.Type, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(c => c.Value).ToArray(), StringComparer.OrdinalIgnoreCase);

        // The IdP is the institution's own, configured by its administrator, so its email
        // assertions are treated as verified (the email-domain allow-list still applies).
        return new ExternalIdentity(
            Subject: subject.Trim(),
            Email: string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            EmailVerified: !string.IsNullOrWhiteSpace(email),
            Name: First(NameAttributes),
            MfaAsserted: mfa,
            Attributes: attributes);
    }

    /// <summary>True when the response carries an EncryptedAssertion and no plain Assertion (DTDs refused).</summary>
    private static bool HasEncryptedAssertion(string samlResponse)
    {
        try
        {
            var bytes = Convert.FromBase64String(samlResponse);
            var settings = new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit, XmlResolver = null };
            using var reader = System.Xml.XmlReader.Create(new MemoryStream(bytes), settings);
            var doc = new System.Xml.XmlDocument { XmlResolver = null };
            doc.Load(reader);
            const string ns = "urn:oasis:names:tc:SAML:2.0:assertion";
            return doc.GetElementsByTagName("EncryptedAssertion", ns).Count > 0 && doc.GetElementsByTagName("Assertion", ns).Count == 0;
        }
        catch (Exception ex) when (ex is FormatException or System.Xml.XmlException)
        {
            throw new SsoProtocolException("SAML response is not valid base64 XML.", ex);
        }
    }

    // ── SP metadata ──────────────────────────────────────────────────────────

    public string BuildServiceProviderMetadata(SamlServiceProvider sp, bool authnRequestsSigned = false)
    {
        var descriptor = new EntityDescriptor(new Saml2Configuration { Issuer = sp.EntityId }, signMetadata: false)
        {
            SPSsoDescriptor = new SPSsoDescriptor
            {
                AuthnRequestsSigned = authnRequestsSigned && _spCertificate is not null,
                WantAssertionsSigned = true,
                NameIDFormats = [NameIdentifierFormats.Persistent, NameIdentifierFormats.Email],
                AssertionConsumerServices =
                [
                    new AssertionConsumerService
                    {
                        Binding = ProtocolBindings.HttpPost,
                        Location = new Uri(sp.AcsUrl),
                        IsDefault = true,
                        Index = 0
                    }
                ]
            }
        };
        if (_spCertificate is not null)
        {
            // One certificate for both uses: IdPs verify signed requests and encrypt assertions with it.
            var publicCertificate = _spCertificate.PublicCertificate;
            descriptor.SPSsoDescriptor.SigningCertificates = [publicCertificate];
            descriptor.SPSsoDescriptor.EncryptionCertificates = [publicCertificate];
            descriptor.SPSsoDescriptor.SetDefaultEncryptionMethods();
        }
        return descriptor.ToXmlDocument().OuterXml;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Saml2Configuration CreateConfig(EntityDescriptor idp, SamlServiceProvider sp)
    {
        var config = new Saml2Configuration
        {
            Issuer = sp.EntityId,
            AllowedIssuer = idp.EntityId,
            AudienceRestricted = true,
            // Trust comes from the admin-configured metadata, not from a CA chain (IdP certs are usually self-signed).
            CertificateValidationMode = X509CertificateValidationMode.None,
            RevocationMode = X509RevocationMode.NoCheck
        };
        config.AllowedAudienceUris.Add(sp.EntityId);
        config.SignatureValidationCertificates.AddRange(idp.IdPSsoDescriptor.SigningCertificates);
        return config;
    }

    private async Task<EntityDescriptor> GetIdpAsync(string metadataUrl, bool forceRefresh, CancellationToken ct)
    {
        if (!forceRefresh
            && MetadataCache.TryGetValue(metadataUrl, out var cached)
            && DateTimeOffset.UtcNow - cached.FetchedAt < MetadataTtl)
            return cached.Descriptor;

        if (!Uri.TryCreate(metadataUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new SsoProtocolException("SAML metadata URL must be an absolute https URL.");

        string xml;
        try
        {
            using var response = await _http.GetAsync(uri, ct);
            if (!response.IsSuccessStatusCode)
                throw new SsoProtocolException($"SAML metadata returned HTTP {(int)response.StatusCode}.");
            xml = await response.Content.ReadAsStringAsync(ct);
        }
        catch (HttpRequestException ex)
        {
            throw new SsoProtocolException("SAML metadata unreachable.", ex);
        }

        EntityDescriptor descriptor;
        try
        {
            descriptor = new EntityDescriptor().ReadIdPSsoDescriptor(xml);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new SsoProtocolException("SAML metadata could not be parsed.", ex);
        }

        if (descriptor.IdPSsoDescriptor is null
            || descriptor.IdPSsoDescriptor.SigningCertificates?.Any() != true
            || string.IsNullOrWhiteSpace(descriptor.EntityId))
            throw new SsoProtocolException("SAML metadata has no IdP descriptor, entity ID or signing certificate.");

        MetadataCache[metadataUrl] = (descriptor, DateTimeOffset.UtcNow);
        return descriptor;
    }

    private static bool IsFresh(string metadataUrl)
        => MetadataCache.TryGetValue(metadataUrl, out var cached)
           && DateTimeOffset.UtcNow - cached.FetchedAt < TimeSpan.FromMinutes(1);

    private static bool SameUrl(string a, string b)
        => string.Equals(a.TrimEnd('/'), b.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
}
