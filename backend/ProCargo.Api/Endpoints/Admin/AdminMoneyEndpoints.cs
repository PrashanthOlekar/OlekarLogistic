using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProCargo.Api.Common;
using ProCargo.Api.Contracts;
using ProCargo.Api.Data;
using ProCargo.Api.Data.Entities;
using ProCargo.Api.Domain;
using ProCargo.Api.Services;

namespace ProCargo.Api.Endpoints.Admin;

/// <summary>
/// Money in (customer payments) and money out (owner payouts).
///   GET  /api/admin/payments                       customer payments
///   GET  /api/admin/settlements?status=Approved    owner payouts
///   POST /api/admin/settlements/{id}/release       record a payout sent from the bank
/// </summary>
public static class AdminMoneyEndpoints
{
    public static void Map(RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/admin").RequireAuthorization(Roles.Admin);

        group.MapGet("/payments", ListPaymentsAsync);
        group.MapGet("/settlements", ListSettlementsAsync);
        group.MapPost("/settlements/{id:long}/release", ReleasePayoutAsync);
    }

    private static async Task<IResult> ListPaymentsAsync(ProCargoDbContext db)
    {
        var payments = await db.Payments
            .OrderByDescending(payment => payment.CreatedAt)
            .Take(200)
            .Join(
                db.Bookings,
                payment => payment.BookingId,
                booking => booking.BookingId,
                (payment, booking) => new
                {
                    id = payment.PaymentId,
                    booking.BookingNumber,
                    customer = booking.Customer.CompanyName ?? booking.Customer.User.FullName,
                    payment.Amount,
                    payment.Method,
                    payment.Gateway,
                    payment.Status,
                    payment.PaidAt,
                    payment.GatewayPaymentId,
                })
            .ToListAsync();

        return Results.Ok(payments);
    }

    private static async Task<IResult> ListSettlementsAsync(string? status, ProCargoDbContext db)
    {
        var settlements = await db.Settlements
            .Where(settlement => status == null || settlement.Status == status)
            .OrderByDescending(settlement => settlement.CreatedAt)
            .Take(200)
            .Select(settlement => new
            {
                id = settlement.SettlementId,
                trip = settlement.Trip.TripNumber,
                owner = settlement.Owner.BusinessName ?? settlement.Owner.User.FullName,
                bank = db.OwnerBankAccounts
                    .Where(account => account.OwnerBankAccountId == settlement.OwnerBankAccountId)
                    .Select(account => account.IFSC + " ••" + account.AccountLast4)
                    .FirstOrDefault(),
                settlement.GrossAmount,
                settlement.CommissionAmount,
                settlement.TdsAmount,
                settlement.NetAmount,
                settlement.Status,
                settlement.UTR,
                settlement.ReleasedAt,
                settlement.CreatedAt,
            })
            .ToListAsync();

        return Results.Ok(settlements);
    }

    /// <summary>
    /// Records a payout you have already sent from the bank (or the payment gateway's payout screen).
    /// The UTR is the bank's reference number for that transfer.
    /// </summary>
    private static async Task<IResult> ReleasePayoutAsync(
        long id,
        ReleasePayoutRequest request,
        ClaimsPrincipal principal,
        ProCargoDbContext db,
        HttpContext http)
    {
        Settlement settlement = await db.Settlements.FindAsync(id)
            ?? throw ApiException.NotFound("Settlement not found.");

        Guard.Require(settlement.Status == SettlementStatus.Approved, "Approve the trip's POD before releasing the payout.");
        Guard.Require(!string.IsNullOrWhiteSpace(request.Utr), "Enter the bank transfer reference (UTR).");

        settlement.Status = SettlementStatus.Released;
        settlement.UTR = request.Utr!.Trim();
        settlement.ReleasedBy = principal.GetUserId();
        settlement.ReleasedAt = DateTime.UtcNow;

        AuditLogger.Log(db, http, "Settlement.Released", "Settlement", id, new { settlement.NetAmount, settlement.UTR });
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
}
