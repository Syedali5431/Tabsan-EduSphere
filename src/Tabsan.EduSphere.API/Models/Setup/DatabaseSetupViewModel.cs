using System.ComponentModel.DataAnnotations;

namespace Tabsan.EduSphere.API.Models.Setup;

public sealed class DatabaseSetupViewModel
{
    [Required]
    public string DatabaseType { get; set; } = "Local"; // Local, Cloud, Custom

    [Required]
    public string LocalProvider { get; set; } = "SqlServerExpress"; // SqlServerExpress, LocalDb, Sqlite

    [Display(Name = "Server / Host")]
    public string? ServerHost { get; set; }

    [Display(Name = "Database Name")]
    public string? DatabaseName { get; set; }

    [Display(Name = "Username")]
    public string? Username { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? Password { get; set; }

    [Display(Name = "Port")]
    public int? Port { get; set; }

    [Display(Name = "Use SSL / Encrypt")]
    public bool UseSsl { get; set; } = true;

    [Display(Name = "Trust Server Certificate")]
    public bool TrustServerCertificate { get; set; } = true;

    [Display(Name = "Use Trusted Connection")]
    public bool TrustedConnection { get; set; }

    [Display(Name = "SQLite File Path")]
    public string? SqliteFilePath { get; set; }

    public string? ReturnUrl { get; set; }

    public bool RequiresSetup { get; set; }

    public string? CurrentProvider { get; set; }

    public string? StatusMessage { get; set; }
}
