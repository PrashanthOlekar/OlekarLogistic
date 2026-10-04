using System.ComponentModel.DataAnnotations;

namespace ProCargo.Infrastructure.Data;

/// <summary>The "Database" section of appsettings. Keep the connection string out of source control.</summary>
public sealed class DatabaseSettings
{
    public const string SectionName = "Database";

    [Required(AllowEmptyStrings = false, ErrorMessage = "Set Database:ConnectionString (user-secrets or the Database__ConnectionString environment variable).")]
    public string ConnectionString { get; set; } = string.Empty;

    [Range(5, 300)]
    public int CommandTimeoutSeconds { get; set; } = 30;
}
