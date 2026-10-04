namespace ProCargo.Application.Features.Bookings;

/// <summary>Pay the open quote.</summary>
/// <param name="Method">UPI, CreditCard, DebitCard, NetBanking or Wallet.</param>
public sealed record PayBookingRequest(string Method);
