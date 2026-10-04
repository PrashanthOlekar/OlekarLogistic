namespace ProCargo.Application.Features.Users;

/// <summary>Block or unblock an account.</summary>
/// <param name="Status">"Blocked" or "Active".</param>
public sealed record UpdateUserRequest(string Status);
