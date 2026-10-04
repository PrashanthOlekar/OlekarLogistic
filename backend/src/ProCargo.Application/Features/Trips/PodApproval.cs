namespace ProCargo.Application.Features.Trips;

/// <summary>The POD approval and the GST invoice it issues.</summary>
public sealed record PodApproval(
    long TripId,
    long AdminUserId,
    string InvoiceNumber,
    string PlaceOfSupply,
    string? CustomerGstin,
    decimal TaxableAmount,
    decimal Cgst,
    decimal Sgst,
    decimal Igst,
    decimal TotalAmount);
