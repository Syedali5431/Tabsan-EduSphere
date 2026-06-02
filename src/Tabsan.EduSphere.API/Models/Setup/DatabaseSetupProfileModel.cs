namespace Tabsan.EduSphere.API.Models.Setup;

public sealed class DatabaseSetupProfileModel
{
    public int Version { get; set; } = 1;
    public string DatabaseType { get; set; } = "Local";
    public string LocalProvider { get; set; } = "SqlServerExpress";
    public string? ServerHost { get; set; }
    public string? DatabaseName { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int? Port { get; set; }
    public bool UseSsl { get; set; } = true;
    public bool TrustServerCertificate { get; set; } = true;
    public bool TrustedConnection { get; set; }
    public string? SqliteFilePath { get; set; }
    public DateTimeOffset ExportedUtc { get; set; } = DateTimeOffset.UtcNow;
}
