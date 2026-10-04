namespace ProCargo.Application.Features.Registration;

/// <summary>A new customer account. Code is the one-time code sent with purpose "Signup".</summary>
public sealed record RegisterCustomerRequest(
    string FullName,
    string Mobile,
    string Code,
    string? Email,
    string? CompanyName,
    string? Gstin);
