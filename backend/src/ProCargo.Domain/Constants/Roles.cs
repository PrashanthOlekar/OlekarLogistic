namespace ProCargo.Domain.Constants;

/// <summary>The four kinds of user, stored in Users.Role.</summary>
public static class Roles
{
    public const string Customer = "Customer";
    public const string Owner = "Owner";
    public const string Driver = "Driver";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = [Customer, Owner, Driver, Admin];
}
