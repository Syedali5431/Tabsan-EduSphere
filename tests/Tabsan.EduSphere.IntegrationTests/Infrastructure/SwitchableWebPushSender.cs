using Microsoft.Extensions.Options;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Infrastructure.Notifications;

namespace Tabsan.EduSphere.IntegrationTests.Infrastructure;

/// <summary>
/// Plan M4.3: test-only <see cref="IWebPushSender"/>. Uses the real sender (and real VAPID key store) unless a test
/// installs <see cref="Inner"/>, so push delivery can be observed without a second test host or the network.
/// </summary>
public sealed class SwitchableWebPushSender : IWebPushSender
{
    private readonly WebPushSender _real;

    public SwitchableWebPushSender(VapidKeyStore keys, IOptions<WebPushOptions> options)
        => _real = new WebPushSender(new HttpClient { Timeout = TimeSpan.FromSeconds(5) }, keys, options);

    public IWebPushSender? Inner { get; set; }

    private IWebPushSender Current => Inner ?? _real;

    public bool IsConfigured => Current.IsConfigured;
    public string? PublicKey => Current.PublicKey;

    public Task<WebPushSendResult> SendAsync(PushTarget target, byte[] payload, int ttlSeconds, CancellationToken ct = default)
        => Current.SendAsync(target, payload, ttlSeconds, ct);
}
