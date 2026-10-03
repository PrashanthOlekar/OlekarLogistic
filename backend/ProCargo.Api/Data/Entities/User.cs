using System.ComponentModel.DataAnnotations;

using ProCargo.Api.Domain;

namespace ProCargo.Api.Data.Entities;

/// <summary>
/// Anyone who signs in: a customer, lorry owner, driver or admin. Table: Users.
/// </summary>
public class User
{
    public long UserId { get; set; }

    public string Role { get; set; } = Roles.Customer;

    public string FullName { get; set; } = "";

    public string Mobile { get; set; } = "";

    public string? Email { get; set; }

    public string? PasswordHash { get; set; }

    public string PreferredLanguage { get; set; } = "en";

    // Active | PendingKyc | Blocked | Closed
    public string Status { get; set; } = "Active";

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
