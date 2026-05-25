using QRCoder;

namespace Tabsan.EduSphere.API.Services.TwoFactor;

/// <summary>
/// Generates QR payloads for authenticator enrollment without touching legacy helpers.
/// </summary>
public sealed class QRCodeService
{
    public byte[] GeneratePng(string payload, int pixelsPerModule = 8)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(Math.Max(1, pixelsPerModule));
    }

    public string GenerateDataUrl(string payload, int pixelsPerModule = 8)
    {
        var bytes = GeneratePng(payload, pixelsPerModule);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}