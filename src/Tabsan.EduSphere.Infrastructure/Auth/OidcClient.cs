using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.Infrastructure.Auth;

/// <summary>
/// Plan M1 Stage M1.2 — OpenID Connect authorization-code client with PKCE.
/// Discovery documents (and signing keys) are cached per authority and refreshed
/// automatically by <see cref="ConfigurationManager{T}"/>, including on key rollover.
/// </summary>
public sealed class OidcClient : IOidcClient
{
    private static readonly ConcurrentDictionary<string, IConfigurationManager<OpenIdConnectConfiguration>> Managers =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly HttpClient _http;

    public OidcClient(HttpClient http) => _http = http;

    public async Task<string> BuildAuthorizationUrlAsync(
        OidcProviderConfig provider, string redirectUri, string state, string nonce, string codeChallenge,
        CancellationToken ct = default)
    {
        var discovery = await GetDiscoveryAsync(provider.Authority, ct);
        if (string.IsNullOrWhiteSpace(discovery.AuthorizationEndpoint))
            throw new OidcException("Discovery document has no authorization_endpoint.");

        return QueryHelpers.AddQueryString(discovery.AuthorizationEndpoint, new Dictionary<string, string?>
        {
            ["client_id"] = provider.ClientId,
            ["response_type"] = "code",
            ["response_mode"] = "query",
            ["redirect_uri"] = redirectUri,
            ["scope"] = "openid profile email",
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["prompt"] = "select_account"
        });
    }

    public async Task<OidcIdentity> RedeemCodeAsync(
        OidcProviderConfig provider, string code, string redirectUri, string codeVerifier, string expectedNonce,
        CancellationToken ct = default)
    {
        var discovery = await GetDiscoveryAsync(provider.Authority, ct);
        if (string.IsNullOrWhiteSpace(discovery.TokenEndpoint))
            throw new OidcException("Discovery document has no token_endpoint.");

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = provider.ClientId,
            ["client_secret"] = provider.ClientSecret,
            ["code_verifier"] = codeVerifier
        });

        string idToken;
        try
        {
            using var response = await _http.PostAsync(discovery.TokenEndpoint, form, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new OidcException($"Token endpoint returned HTTP {(int)response.StatusCode}: {Truncate(body)}");

            using var json = JsonDocument.Parse(body);
            idToken = json.RootElement.TryGetProperty("id_token", out var t) && t.ValueKind == JsonValueKind.String
                ? t.GetString()!
                : throw new OidcException("Token response has no id_token.");
        }
        catch (HttpRequestException ex)
        {
            throw new OidcException("Token endpoint unreachable.", ex);
        }
        catch (JsonException ex)
        {
            throw new OidcException("Token response is not valid JSON.", ex);
        }

        var claims = await ValidateIdTokenAsync(provider, discovery, idToken, ct);

        var nonce = claims.TryGetValue("nonce", out var n) ? n as string : null;
        if (!string.Equals(nonce, expectedNonce, StringComparison.Ordinal))
            throw new OidcException("id_token nonce mismatch.");

        // Entra ID: 'oid' is the immutable per-directory object id; 'sub' is pairwise per app. Prefer oid.
        var subject = (claims.TryGetValue("oid", out var oid) ? oid as string : null)
                      ?? (claims.TryGetValue("sub", out var sub) ? sub as string : null);
        if (string.IsNullOrWhiteSpace(subject))
            throw new OidcException("id_token has no subject.");

        var email = claims.TryGetValue("email", out var e) ? e as string : null;
        if (string.IsNullOrWhiteSpace(email) && IsMicrosoftIssuer(discovery.Issuer))
        {
            // Entra ID only emits 'email' when configured as an optional claim; the UPN in
            // preferred_username is directory-controlled (authority is pinned to one tenant).
            var preferred = claims.TryGetValue("preferred_username", out var p) ? p as string : null;
            email = preferred is not null && preferred.Contains('@') ? preferred : null;
        }

        return new OidcIdentity(
            Subject: subject,
            Email: string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            EmailVerified: IsTrue(claims, "email_verified"),
            Name: claims.TryGetValue("name", out var name) ? name as string : null,
            MfaAsserted: HasMfaAmr(claims));
    }

    private async Task<IDictionary<string, object>> ValidateIdTokenAsync(
        OidcProviderConfig provider, OpenIdConnectConfiguration discovery, string idToken, CancellationToken ct)
    {
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = discovery.Issuer,
            ValidateIssuer = true,
            ValidAudience = provider.ClientId,
            ValidateAudience = true,
            IssuerSigningKeys = discovery.SigningKeys,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(idToken, parameters);

        if (!result.IsValid && result.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            // Provider rotated keys since discovery was cached — refresh once and retry.
            Managers[provider.Authority].RequestRefresh();
            discovery = await GetDiscoveryAsync(provider.Authority, ct);
            parameters.IssuerSigningKeys = discovery.SigningKeys;
            result = await handler.ValidateTokenAsync(idToken, parameters);
        }

        if (!result.IsValid)
            throw new OidcException("id_token validation failed: " + (result.Exception?.Message ?? "unknown"), result.Exception);

        return result.Claims;
    }

    private async Task<OpenIdConnectConfiguration> GetDiscoveryAsync(string authority, CancellationToken ct)
    {
        var manager = Managers.GetOrAdd(authority, a =>
        {
            var metadataAddress = a.TrimEnd('/') + "/.well-known/openid-configuration";
            return new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever(_http) { RequireHttps = true });
        });

        try
        {
            return await manager.GetConfigurationAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new OidcException($"Could not load OpenID configuration for {authority}.", ex);
        }
    }

    private static bool IsTrue(IDictionary<string, object> claims, string name)
        => claims.TryGetValue(name, out var v) && v switch
        {
            bool b => b,
            string s => bool.TryParse(s, out var p) && p,
            _ => false
        };

    private static bool HasMfaAmr(IDictionary<string, object> claims)
    {
        if (!claims.TryGetValue("amr", out var amr))
            return false;

        IEnumerable<string> values = amr switch
        {
            string s => [s],
            IEnumerable<object> list => list.Select(x => x?.ToString() ?? string.Empty),
            JsonElement { ValueKind: JsonValueKind.Array } arr => arr.EnumerateArray().Select(x => x.ToString()),
            _ => []
        };
        return values.Any(v => v.Equals("mfa", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsMicrosoftIssuer(string? issuer)
        => Uri.TryCreate(issuer, UriKind.Absolute, out var uri)
           && uri.Host.Equals("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase);

    private static string Truncate(string text) => text.Length <= 300 ? text : text[..300];

    /// <summary>Test hook: drops cached discovery documents.</summary>
    internal static void ResetCache() => Managers.Clear();
}
