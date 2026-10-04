namespace ProCargo.Application.Features.Auth;

/// <summary>What the portal needs to know about the signed-in user.</summary>
public sealed class UserProfile
{
    public long Id { get; set; }

    public string Role { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Mobile { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string Status { get; set; } = string.Empty;

    /// <summary>CustomerProfile, OwnerProfile or DriverProfile, depending on the role. Null for admins.</summary>
    public object? Detail { get; set; }
}
