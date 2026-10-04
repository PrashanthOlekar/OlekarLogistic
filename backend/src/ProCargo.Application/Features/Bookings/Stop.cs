namespace ProCargo.Application.Features.Bookings;

/// <summary>A pickup or delivery point.</summary>
public sealed record Stop(string? City, string Address, string? Contact, string? Phone);
