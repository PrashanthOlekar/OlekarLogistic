namespace ProCargo.Application.Abstractions;

/// <summary>Who is making the current request. Implemented by the API from the JWT.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>Users.UserId. Throws UnauthorizedAccessException when nobody is signed in.</summary>
    long UserId { get; }

    /// <summary>Customer, Owner, Driver or Admin. Empty when nobody is signed in.</summary>
    string Role { get; }

    bool IsInRole(string role);

    /// <summary>The caller's IP address, for the audit trail.</summary>
    string? IpAddress { get; }

    /// <summary>Browser or app description, stored with refresh tokens.</summary>
    string? DeviceInfo { get; }
}
