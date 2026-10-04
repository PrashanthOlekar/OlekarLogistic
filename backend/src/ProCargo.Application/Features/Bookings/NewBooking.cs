namespace ProCargo.Application.Features.Bookings;

/// <summary>A validated booking, ready to save.</summary>
public sealed record NewBooking(
    long CustomerId,
    int PickupCityId,
    string PickupAddressText,
    decimal PickupLatitude,
    decimal PickupLongitude,
    string? PickupContactName,
    string? PickupContactPhone,
    int DropCityId,
    string DropAddressText,
    decimal DropLatitude,
    decimal DropLongitude,
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
