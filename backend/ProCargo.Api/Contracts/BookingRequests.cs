namespace ProCargo.Api.Contracts;

// What the portal sends to the /api/quotes and /api/bookings endpoints.

public record QuoteEstimateRequest(int PickupCityId, int DropCityId, int VehicleTypeId, int WeightKg);

public record CreateBookingRequest(
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

/// <summary>Method: UPI, CreditCard, DebitCard, NetBanking or Wallet.</summary>
public record PayRequest(string Method);

public record CancelBookingRequest(string? Reason);
