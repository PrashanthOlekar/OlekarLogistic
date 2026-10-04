namespace ProCargo.Application.Features.Auth;

public sealed class CustomerProfile
{
    public long CustomerId { get; set; }

    public string? CompanyName { get; set; }

    public string? Gstin { get; set; }
}
