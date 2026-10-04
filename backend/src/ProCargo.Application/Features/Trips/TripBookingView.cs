using ProCargo.Application.Features.Bookings;

namespace ProCargo.Application.Features.Trips;

/// <summary>What the driver needs to know about the booking.</summary>
public sealed record TripBookingView(
    string BookingNumber,
    string GoodsDescription,
    int WeightKg,
    DateOnly PickupDate,
    string? PickupSlot,
    string? SpecialInstructions,
    Stop Pickup,
    Stop Drop);
