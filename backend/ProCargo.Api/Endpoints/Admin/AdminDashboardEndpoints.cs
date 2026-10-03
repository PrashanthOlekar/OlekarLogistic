using Microsoft.EntityFrameworkCore;
using ProCargo.Api.Common;
using ProCargo.Api.Data;
using ProCargo.Api.Domain;

namespace ProCargo.Api.Endpoints.Admin;

/// <summary>
/// The admin overview page.
///   GET /api/admin/summary   counters, work queues and chart data
/// </summary>
public static class AdminDashboardEndpoints
{
    public static void Map(RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/admin").RequireAuthorization(Roles.Admin);

        group.MapGet("/summary", GetSummaryAsync);
    }

    private static async Task<IResult> GetSummaryAsync(ProCargoDbContext db)
    {
        // "Today" and "this month" follow the Indian calendar.
        DateTime todayIst = IndianTime.Now.Date;
        DateTime todayStartUtc = IndianTime.StartOfDayUtc(todayIst);
        DateTime monthStartUtc = IndianTime.StartOfDayUtc(new DateTime(todayIst.Year, todayIst.Month, 1));

        return Results.Ok(new
        {
            // Counters
            bookingsToday = await db.Bookings.CountAsync(booking => booking.CreatedAt >= todayStartUtc),
            activeTrips = await db.Trips.CountAsync(trip => !TripStatus.Finished.Contains(trip.Status)),
            completedThisMonth = await db.Trips.CountAsync(trip =>
                trip.Status == TripStatus.Completed && trip.PodApprovedAt >= monthStartUtc),
            awaitingPayment = await db.Bookings.CountAsync(booking => booking.Status == BookingStatus.Quoted),
            awaitingTruck = await db.Bookings.CountAsync(booking => booking.Status == BookingStatus.Confirmed),

            // Money
            revenueThisMonth = await db.Payments
                .Where(payment => payment.Status == PaymentStatus.Captured && payment.PaidAt >= monthStartUtc)
                .SumAsync(payment => (decimal?)payment.Amount) ?? 0,
            commissionThisMonth = await db.Settlements
                .Where(settlement => settlement.CreatedAt >= monthStartUtc && settlement.Status != SettlementStatus.OnHold)
                .SumAsync(settlement => (decimal?)settlement.CommissionAmount) ?? 0,

            // Work queues
            pendingApprovals = await CountPendingApprovalsAsync(db),
            pendingDocuments = await db.Documents.CountAsync(document =>
                document.Status == DocumentStatus.Pending && document.EntityType != DocumentEntity.Trip),
            podToApprove = await db.Trips.CountAsync(trip => trip.Status == TripStatus.Delivered),
            settlementsToRelease = await db.Settlements.CountAsync(settlement => settlement.Status == SettlementStatus.Approved),

            // Charts
            last7 = await GetBookingsPerDayAsync(db, todayIst),
            routes = await GetTopRoutesAsync(db, monthStartUtc.AddMonths(-2)),
            cities = await GetTopPickupCitiesAsync(db),
            vehicles = await GetFleetAvailabilityAsync(db),
        });
    }

    private static async Task<int> CountPendingApprovalsAsync(ProCargoDbContext db)
    {
        int owners = await db.Owners.CountAsync(owner => owner.KycStatus == KycStatus.Pending);
        int drivers = await db.Drivers.CountAsync(driver => driver.KycStatus == KycStatus.Pending);
        int vehicles = await db.Vehicles.CountAsync(vehicle => vehicle.VerificationStatus == VehicleVerification.Pending);

        return owners + drivers + vehicles;
    }

    /// <summary>Bookings created on each of the last 7 Indian days, oldest first.</summary>
    private static async Task<object> GetBookingsPerDayAsync(ProCargoDbContext db, DateTime todayIst)
    {
        DateTime weekStartUtc = IndianTime.StartOfDayUtc(todayIst.AddDays(-6));

        List<DateTime> createdTimes = await db.Bookings
            .Where(booking => booking.CreatedAt >= weekStartUtc)
            .Select(booking => booking.CreatedAt)
            .ToListAsync();

        return Enumerable.Range(0, 7)
            .Select(daysAgo => todayIst.AddDays(daysAgo - 6))
            .Select(day => new
            {
                date = DateOnly.FromDateTime(day),
                count = createdTimes.Count(created => created.Add(IndianTime.Offset).Date == day),
            })
            .ToList();
    }

    private static async Task<object> GetTopRoutesAsync(ProCargoDbContext db, DateTime sinceUtc)
    {
        return await db.Bookings
            .Where(booking => booking.Status != BookingStatus.Cancelled && booking.CreatedAt >= sinceUtc)
            .GroupBy(booking => new { from = booking.PickupCity!.Name, to = booking.DropCity!.Name })
            .Select(route => new { route = route.Key.from + " → " + route.Key.to, count = route.Count() })
            .OrderByDescending(route => route.count)
            .Take(6)
            .ToListAsync();
    }

    private static async Task<object> GetTopPickupCitiesAsync(ProCargoDbContext db)
    {
        return await db.Bookings
            .Where(booking => booking.Status != BookingStatus.Cancelled)
            .GroupBy(booking => booking.PickupCity!.Name)
            .Select(city => new { city = city.Key, count = city.Count() })
            .OrderByDescending(city => city.count)
            .Take(7)
            .ToListAsync();
    }

    private static async Task<object> GetFleetAvailabilityAsync(ProCargoDbContext db)
    {
        return await db.Vehicles
            .Where(vehicle => vehicle.VerificationStatus == VehicleVerification.Approved)
            .GroupBy(vehicle => vehicle.AvailabilityStatus)
            .Select(group => new { status = group.Key, count = group.Count() })
            .ToListAsync();
    }
}
