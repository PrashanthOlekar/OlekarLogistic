namespace ProCargo.Api.Domain;

/// <summary>
/// The four kinds of user. Stored in Users.Role and used to protect endpoints.
/// </summary>
public static class Roles
{
    public const string Customer = "Customer";
    public const string Owner = "Owner";
    public const string Driver = "Driver";
    public const string Admin = "Admin";
}
