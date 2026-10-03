using Microsoft.EntityFrameworkCore;

namespace ProCargo.Api.Data.Entities;

/// <summary>
/// Profile of a customer who books trucks. Table: Customers.
/// </summary>
public class Customer
{
    public long CustomerId { get; set; }

    public long UserId { get; set; }

    public User User { get; set; } = null!;

    public string? CompanyName { get; set; }

    public string? GSTIN { get; set; }

    public long? BillingAddressId { get; set; }

    [Precision(12, 2)]
    public decimal CreditLimit { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
