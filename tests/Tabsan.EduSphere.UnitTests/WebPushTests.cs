using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tabsan.EduSphere.Application.Notifications;
using Tabsan.EduSphere.Domain.Notifications;
using Tabsan.EduSphere.Infrastructure.Notifications;

namespace Tabsan.EduSphere.UnitTests;

/// <summary>Plan M4.3 — Web Push encryption (RFC 8291), VAPID (RFC 8292) and subscription rules.</summary>
public class WebPushTests
{
    // RFC 8291 Appendix A.
    private const string Plaintext = "V2hlbiBJIGdyb3cgdXAsIEkgd2FudCB0byBiZSBhIHdhdGVybWVsb24";
    private const string AsPublic = "BP4z9KsN6nGRTbVYI_c7VJSPQTBtkgcy27mlmlMoZIIgDll6e3vCYLocInmYWAmS6TlzAC8wEqKK6PBru3jl7A8";
    private const string AsPrivate = "yfWPiYE-n46HLnH0KqZOF1fJJU3MYrct3AELtAQ-oRw";
    private const string UaPublic = "BCVxsr7N_eNgVRqvHtD0zTZsEc6-VV-JvLexhqUzORcxaOzi6-AYWXvTBHm4bjyPjs7Vd8pZGH6SRpkNtoIAiw4";
    private const string UaPrivate = "q1dXpw3UpT5VOmu_cf_v6ih07Aems3njxI-JWgLcM94";
    private const string AuthSecret = "BTBZMqHH6r4Tts7J_aSIgg";
    private const string Salt = "DGv6ra1nlYgDCS1FRnbzlw";
    private const string Expected = "DGv6ra1nlYgDCS1FRnbzlwAAEABBBP4z9KsN6nGRTbVYI_c7VJSPQTBtkgcy27mlmlMoZIIgDll6e3vCYLocInmYWAmS6TlzAC8wEqKK6PBru3jl7A_yl95bQpu6cVPTpK4Mqgkf1CXztLVBSt2Ks3oZwbuwXPXLWyouBWLVWGNWQexSgSxsj_Qulcy4a-fN";

    private static ECDiffieHellman Key(string publicKey, string privateKey)
    {
        var q = Base64Url.Decode(publicKey);
        return ECDiffieHellman.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            D = Base64Url.Decode(privateKey),
            Q = new ECPoint { X = q[1..33], Y = q[33..65] }
        });
    }

    [Fact]
    public void Encryption_MatchesTheRfc8291TestVector()
    {
        using var asKey = Key(AsPublic, AsPrivate);
        var body = WebPushEncryption.Encrypt(Base64Url.Decode(UaPublic), Base64Url.Decode(AuthSecret), Base64Url.Decode(Plaintext), asKey, Base64Url.Decode(Salt));
        Assert.Equal(Expected, Base64Url.Encode(body));
    }

    [Fact]
    public void Encryption_RoundTrips_WithFreshKeysAndSalt()
    {
        using var browser = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var auth = RandomNumberGenerator.GetBytes(16);
        var message = Encoding.UTF8.GetBytes("{\"title\":\"Hi\",\"body\":\"Grades are posted\"}");
        var first = WebPushEncryption.Encrypt(WebPushEncryption.ExportPublic(browser), auth, message);
        var second = WebPushEncryption.Encrypt(WebPushEncryption.ExportPublic(browser), auth, message);
        Assert.NotEqual(first, second); // new server key and salt every time
        Assert.Equal(message, WebPushEncryption.Decrypt(browser, auth, first));
        Assert.Throws<ArgumentException>(() => WebPushEncryption.Encrypt(WebPushEncryption.ExportPublic(browser), auth, new byte[5000]));
    }

    [Fact]
    public void Vapid_SignsA12HourJwtForTheEndpointOrigin()
    {
        var keys = VapidKeys.Generate();
        var header = keys.AuthorizationHeader(new Uri("https://fcm.googleapis.com/fcm/send/abc"), "mailto:admin@uni.edu", DateTimeOffset.FromUnixTimeSeconds(1_800_000_000));
        Assert.StartsWith("vapid t=", header);
        var token = header["vapid t=".Length..header.IndexOf(',')];
        Assert.Equal(Base64Url.Encode(keys.PublicKey), header[(header.IndexOf("k=") + 2)..]);

        var parts = token.Split('.');
        using var claims = JsonDocument.Parse(Base64Url.Decode(parts[1]));
        Assert.Equal("https://fcm.googleapis.com", claims.RootElement.GetProperty("aud").GetString());
        Assert.Equal(1_800_000_000 + 12 * 3600, claims.RootElement.GetProperty("exp").GetInt64());
        Assert.Equal("mailto:admin@uni.edu", claims.RootElement.GetProperty("sub").GetString());

        using var verifier = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = keys.PublicKey[1..33], Y = keys.PublicKey[33..65] }
        });
        Assert.True(verifier.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), Base64Url.Decode(parts[2]),
            HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
    }

    [Theory]
    [InlineData("https://fcm.googleapis.com/fcm/send/abc", true)]
    [InlineData("https://updates.push.services.mozilla.com/wpush/v2/abc", true)]
    [InlineData("https://web.push.apple.com/QGuQyavXutnMH", true)]
    [InlineData("https://db5p.notify.windows.com/w/?token=abc", true)]
    [InlineData("http://fcm.googleapis.com/fcm/send/abc", false)]        // not https
    [InlineData("https://evil.example.com/fcm.googleapis.com", false)]   // other host
    [InlineData("https://fcm.googleapis.com.evil.com/x", false)]         // suffix trick
    [InlineData("https://push.services.mozilla.com/x", false)]           // ".suffix" needs a subdomain
    [InlineData("https://127.0.0.1/x", false)]                            // IP literal
    [InlineData("https://user:pw@fcm.googleapis.com/x", false)]          // credentials
    [InlineData("https://fcm.googleapis.com:8443/x", false)]             // odd port
    [InlineData("not a url", false)]
    public void Endpoints_AreLimitedToKnownPushServices(string endpoint, bool allowed)
    {
        var hosts = new Tabsan.EduSphere.Application.Interfaces.WebPushOptions().AllowedHosts;
        Assert.Equal(allowed, PushNotificationService.ValidateEndpoint(endpoint, hosts) is null);
    }

    [Fact]
    public void BrowserKeys_MustBeAValidPointAndSecret()
    {
        Assert.Null(PushNotificationService.ValidateKeys(UaPublic, AuthSecret));
        Assert.NotNull(PushNotificationService.ValidateKeys(UaPublic[..20], AuthSecret));
        Assert.NotNull(PushNotificationService.ValidateKeys(UaPublic, "short"));
        var notOnCurve = Base64Url.Decode(UaPublic);
        notOnCurve[40] ^= 0xFF;
        Assert.NotNull(PushNotificationService.ValidateKeys(Base64Url.Encode(notOnCurve), AuthSecret));
        Assert.NotNull(PushNotificationService.ValidateKeys("!!!", AuthSecret));
    }

    [Fact]
    public void Payload_IsShort_AndLinksStayInThePortal()
    {
        var payload = PushNotificationService.BuildPayload(NotificationType.Announcement, "Exam moved", new string('x', 5000), "//evil.example.com");
        using var doc = JsonDocument.Parse(payload);
        Assert.True(payload.Length < 1000);
        Assert.Equal("/Portal/Notifications", doc.RootElement.GetProperty("url").GetString());
        Assert.Equal("edusphere-announcement", doc.RootElement.GetProperty("tag").GetString());
        Assert.EndsWith("…", doc.RootElement.GetProperty("body").GetString());
    }

    [Fact]
    public void Subscriptions_SwitchOffAfterRepeatedFailures_AndMoveBetweenUsers()
    {
        var sub = new PushSubscription(Guid.NewGuid(), "https://fcm.googleapis.com/x", UaPublic, AuthSecret, "Chrome on Windows");
        for (var i = 0; i < 4; i++) sub.RecordFailure(DateTime.UtcNow, 5);
        Assert.True(sub.IsActive);
        sub.RecordSuccess(DateTime.UtcNow);
        for (var i = 0; i < 5; i++) sub.RecordFailure(DateTime.UtcNow, 5);
        Assert.False(sub.IsActive);
        var other = Guid.NewGuid();
        sub.Assign(other, UaPublic, AuthSecret, null);
        Assert.True(sub.IsActive);
        Assert.Equal(other, sub.UserId);
    }
}
