namespace ProCargo.Application.Features.Bookings;

/// <summary>Cancel a booking before the goods are loaded.</summary>
public sealed record CancelBookingRequest(string? Reason);
