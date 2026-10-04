namespace ProCargo.Application.Features.Registration;

/// <summary>A cleaned-up customer registration, ready to save.</summary>
public sealed record NewCustomerAccount(string FullName, string Mobile, string? Email, string? CompanyName, string? Gstin);
