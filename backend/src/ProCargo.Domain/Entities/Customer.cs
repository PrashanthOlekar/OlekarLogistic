namespace ProCargo.Domain.Entities;

/// <summary>A customer's profile (dbo.Customers). Company name and GSTIN are optional.</summary>
public sealed class Customer
{
    public long CustomerId { get; set; }

    public long UserId { get; set; }

    public string? CompanyName { get; set; }

    public string? Gstin { get; set; }

    public DateTime CreatedAt { get; set; }
}
