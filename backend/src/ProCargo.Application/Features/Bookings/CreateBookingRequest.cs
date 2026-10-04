namespace ProCargo.Application.Features.Bookings;

/// <summary>A new booking. It is priced straight away and waits for payment.</summary>
/// <param name="PickupSlot">For example "Morning (6–10 am)".</param>
/// <param name="GoodsValue">Value of the goods in ₹, needed for an e-way bill.</param>
public sealed record CreateBookingRequest(
    int PickupCityId,
    string PickupAddress,
    string? PickupContactName,
    string? PickupContactPhone,
    int DropCityId,
    string DropAddress,
    string? DropContactName,
    string? DropContactPhone,
    int GoodsCategoryId,
    string GoodsDescription,
    int WeightKg,
    decimal? GoodsValue,
    int VehicleTypeId,
    DateOnly PickupDate,
    string? PickupSlot,
    string? SpecialInstructions);
