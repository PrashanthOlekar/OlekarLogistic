namespace ProCargo.Application.Features.Trips;

/// <summary>The driver enters the code the sender (pickup) or receiver (delivery) reads out.</summary>
/// <param name="Kind">Pickup or Delivery.</param>
/// <param name="Code">The 4-digit code.</param>
public sealed record HandoverRequest(string Kind, string Code);
