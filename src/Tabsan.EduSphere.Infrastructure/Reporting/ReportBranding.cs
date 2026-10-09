namespace Tabsan.EduSphere.Infrastructure.Reporting;

/// <summary>
/// Shared branding assets (logo) for all generated report/certificate PDFs.
/// Logo file is a transparent-background PNG copied to the output directory
/// alongside the assembly (see Reporting/Assets/*.png in the csproj).
/// </summary>
public static class ReportBranding
{
    private static readonly string LogoPath =
        Path.Combine(AppContext.BaseDirectory, "Reporting", "Assets", "tabsan-logo.png");

    private static readonly Lazy<byte[]?> LogoBytesLazy = new(() =>
        File.Exists(LogoPath) ? File.ReadAllBytes(LogoPath) : null);

    /// <summary>Raw PNG bytes of the Tabsan logo (transparent background), or null if the asset is missing.</summary>
    public static byte[]? LogoBytes => LogoBytesLazy.Value;

    public static readonly string PrimaryHex = "#0E5A7A";
    public static readonly string PrimaryDarkHex = "#0A3F57";
    public static readonly string AccentHex = "#17A6D6";
    public static readonly string TextHex = "#22303A";
    public static readonly string MutedHex = "#66757F";
    public static readonly string RowAltHex = "#F2F7F9";
    public static readonly string BorderHex = "#D8E3E8";
}
