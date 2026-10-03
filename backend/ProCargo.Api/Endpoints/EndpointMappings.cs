using ProCargo.Api.Data;
using ProCargo.Api.Endpoints.Admin;

namespace ProCargo.Api.Endpoints;

/// <summary>
/// Every URL the API answers, all under /api.
/// Each area lives in its own file in this folder.
/// </summary>
public static class EndpointMappings
{
    public static void MapProCargoEndpoints(this WebApplication app)
    {
        RouteGroupBuilder api = app.MapGroup("/api");

        api.MapGet("/health", CheckHealthAsync);

        MetaEndpoints.Map(api);        // /api/meta, /api/quotes/estimate      (public)
        AuthEndpoints.Map(api);        // /api/auth/...                        (sign-in, registration)
        BookingEndpoints.Map(api);     // /api/bookings/...                    (customers)
        OwnerEndpoints.Map(api);       // /api/owner/...                       (lorry owners)
        DriverEndpoints.Map(api);      // /api/driver/...                      (drivers)
        DocumentEndpoints.Map(api);    // /api/documents/...                   (uploads, all roles)

        AdminDashboardEndpoints.Map(api);   // /api/admin/summary
        AdminApprovalEndpoints.Map(api);    // /api/admin/approvals, /api/admin/documents
        AdminBookingEndpoints.Map(api);     // /api/admin/bookings, /api/admin/trips
        AdminMoneyEndpoints.Map(api);       // /api/admin/payments, /api/admin/settlements
        AdminUserEndpoints.Map(api);        // /api/admin/users, /api/admin/audit
    }

    /// <summary>GET /api/health → { "ok": true } when the database can be reached.</summary>
    private static async Task<IResult> CheckHealthAsync(ProCargoDbContext db)
    {
        bool canConnect = await db.Database.CanConnectAsync();
        return Results.Ok(new { ok = canConnect });
    }
}
