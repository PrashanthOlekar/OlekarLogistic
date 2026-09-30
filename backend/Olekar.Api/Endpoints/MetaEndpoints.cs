using Microsoft.EntityFrameworkCore;
using Olekar.Api.Data;
using Olekar.Api.Services;

namespace Olekar.Api.Endpoints;

public record QuoteEstimateRequest(int PickupCityId, int DropCityId, int VehicleTypeId, int WeightKg);

public static class MetaEndpoints
{
    public static void MapMetaEndpoints(this RouteGroupBuilder api)
    {
        // Master data for forms: cities, vehicle classes, goods categories. Public.
        api.MapGet("/meta", async (OlekarDbContext db) => new
        {
            cities = await db.Cities.Where(c => c.IsServiceable).OrderBy(c => c.Name)
                .Select(c => new { id = c.CityId, c.Name, c.NameKn, c.State }).ToListAsync(),
            vehicleTypes = await db.VehicleTypes.Where(v => v.IsActive).OrderBy(v => v.SortOrder)
                .Select(v => new { id = v.VehicleTypeId, v.Code, v.Name, v.BodyType, v.MaxLoadKg, v.LengthFt, v.WidthFt, v.HeightFt, v.RecommendedGoods })
                .ToListAsync(),
            goods = await db.GoodsCategories.Where(g => g.IsActive).OrderBy(g => g.GoodsCategoryId)
                .Select(g => new { id = g.GoodsCategoryId, g.Name }).ToListAsync()
        });

        // Instant price before login.
        api.MapPost("/quotes/estimate", async (QuoteEstimateRequest r, OlekarDbContext db, PricingService pricing) =>
        {
            var from = await db.Cities.FindAsync(r.PickupCityId) ?? throw ApiException.BadRequest("Choose a pickup city.");
            var to = await db.Cities.FindAsync(r.DropCityId) ?? throw ApiException.BadRequest("Choose a delivery city.");
            var vt = await db.VehicleTypes.FindAsync(r.VehicleTypeId) ?? throw ApiException.BadRequest("Choose a vehicle type.");
            var price = await pricing.PriceAsync(vt, PricingService.RoadKm(from, to));
            var suggested = r.WeightKg > vt.MaxLoadKg
                ? await db.VehicleTypes.Where(v => v.IsActive && v.MaxLoadKg >= r.WeightKg).OrderBy(v => v.MaxLoadKg)
                    .Select(v => new { id = v.VehicleTypeId, v.Name }).FirstOrDefaultAsync()
                : null;
            return new
            {
                price.DistanceKm, price.Days, price.VehicleCost, price.DriverCost, price.Freight,
                price.TaxAmount, price.TotalAmount, price.GstPercent,
                overCapacity = r.WeightKg > vt.MaxLoadKg, maxLoadKg = vt.MaxLoadKg, suggested
            };
        });
    }
}

public static class Seed
{
    /// <summary>Creates the first admin from configuration (Seed:AdminMobile, Seed:AdminName) if no admin exists.</summary>
    public static async Task EnsureAdminAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlekarDbContext>();
        var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        try
        {
            if (!await db.Database.CanConnectAsync())
            {
                app.Logger.LogWarning("Cannot reach SQL Server. Check ConnectionStrings:Olekar and that OlekarLogistics_schema.sql has been run.");
                return;
            }
            if (await db.Users.AnyAsync(u => u.Role == Roles.Admin)) return;
            var mobile = Mobile.Normalize(cfg["Seed:AdminMobile"] ?? "");
            db.Users.Add(new User { Role = Roles.Admin, FullName = cfg["Seed:AdminName"] ?? "Olekar Admin", Mobile = mobile, Status = "Active" });
            await db.SaveChangesAsync();
            app.Logger.LogInformation("Created admin user for mobile {Mobile}", mobile);
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Admin seed skipped.");
        }
    }
}

public static class Mobile
{
    /// <summary>Keeps the last 10 digits of an Indian mobile number and checks it starts with 6–9.</summary>
    public static string Normalize(string input)
    {
        var digits = new string((input ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length > 10) digits = digits[^10..];
        Guard.Require(digits.Length == 10 && "6789".Contains(digits[0]), "Enter a valid 10-digit mobile number.");
        return digits;
    }
}
