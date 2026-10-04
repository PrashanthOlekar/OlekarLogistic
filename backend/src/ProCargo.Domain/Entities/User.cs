namespace ProCargo.Domain.Entities;

/// <summary>Everyone who signs in: customers, lorry owners, drivers and admins (dbo.Users).</summary>
public sealed class User
{
    public long UserId { get; set; }

    public string Role { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    /// <summary>10-digit mobile number, used to sign in.</summary>
    public string Mobile { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
