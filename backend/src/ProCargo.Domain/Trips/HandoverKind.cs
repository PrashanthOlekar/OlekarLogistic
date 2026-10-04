namespace ProCargo.Domain.Trips;

/// <summary>Which handover code the driver is entering.</summary>
public static class HandoverKind
{
    /// <summary>The sender reads it out at loading.</summary>
    public const string Pickup = "Pickup";

    /// <summary>The receiver reads it out at unloading.</summary>
    public const string Delivery = "Delivery";
}
