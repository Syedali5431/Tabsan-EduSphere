using Tabsan.EduSphere.Application.Similarity;

namespace Tabsan.EduSphere.IntegrationTests.Infrastructure;

/// <summary>
/// Plan M9: test-only <see cref="ICopyleaksClient"/> that never calls Copyleaks. It records sign-ins and scans so tests can
/// play Copyleaks' webhook back to the API.
/// </summary>
public sealed class SwitchableCopyleaksClient : ICopyleaksClient
{
    private readonly object _gate = new();
    private readonly List<(string Email, string Key)> _logins = [];
    private readonly List<(string ScanId, string Text, string Webhook, bool Sandbox)> _scans = [];

    public IReadOnlyList<(string Email, string Key)> Logins { get { lock (_gate) return _logins.ToList(); } }
    public IReadOnlyList<(string ScanId, string Text, string Webhook, bool Sandbox)> Scans { get { lock (_gate) return _scans.ToList(); } }

    public Task<string> LoginAsync(string email, string apiKey, CancellationToken ct = default)
    {
        lock (_gate) _logins.Add((email, apiKey));
        return Task.FromResult("test-token");
    }

    public Task SubmitTextAsync(string accessToken, string scanId, string text, string statusWebhookUrl, bool sandbox, CancellationToken ct = default)
    {
        lock (_gate) _scans.Add((scanId, text, statusWebhookUrl, sandbox));
        return Task.CompletedTask;
    }
}
