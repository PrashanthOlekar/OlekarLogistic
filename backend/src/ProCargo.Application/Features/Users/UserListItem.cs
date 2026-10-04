namespace ProCargo.Application.Features.Users;

public sealed class UserListItem
{
    public long Id { get; set; }

    public string Role { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Mobile { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }
}
