namespace ProCargo.Application.Features.Bookings;

/// <summary>Settings from the "Payments" section of appsettings.</summary>
public sealed class PaymentOptions
{
    public const string SectionName = "Payments";

    /// <summary>"Test" marks payments as paid straight away. Anything else needs a gateway (not connected yet).</summary>
    public string Mode { get; set; } = "Test";

    public bool IsTestMode => string.Equals(Mode, "Test", StringComparison.OrdinalIgnoreCase);
}
