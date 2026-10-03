using Microsoft.EntityFrameworkCore;
using ProCargo.Api.Common;
using ProCargo.Api.Contracts;
using ProCargo.Api.Data;
using ProCargo.Api.Data.Entities;
using ProCargo.Api.Services;

namespace ProCargo.Api.Endpoints;

/// <summary>
/// Public endpoints: no sign-in needed.
///   GET  /api/meta             lists for forms (cities, vehicle types, goods types)
///   POST /api/quotes/estimate  instant price before booking
/// </summary>
public static class MetaEndpoints
{
    public static void Map(RouteGroupBuilder api)
    {
        api.MapGet("/meta", GetFormListsAsync);
        api.MapPost("/quotes/estimate", EstimatePriceAsync);
    }

    private static async Task<IResult> GetFormListsAsync(ProCargoDbContext db)
    {
        var cities = await db.Cities
            .Where(city => city.IsServiceable)
            .OrderBy(city => city.Name)
            .Select(city => new { id = city.CityId, city.Name, city.NameKn, city.State })
            .ToListAsync();

        var vehicleTypes = await db.VehicleTypes
            .Where(type => type.IsActive)
            .OrderBy(type => type.SortOrder)
            .Select(type => new
            {
                id = type.VehicleTypeId,
                type.Code,
                type.Name,
                type.BodyType,
                type.MaxLoadKg,
                type.LengthFt,
                type.WidthFt,
                type.HeightFt,
                type.RecommendedGoods,
            })
            .ToListAsync();

        var goods = await db.GoodsCategories
            .Where(category => category.IsActive)
            .OrderBy(category => category.GoodsCategoryId)
            .Select(category => new { id = category.GoodsCategoryId, category.Name })
            .ToListAsync();

        return Results.Ok(new { cities, vehicleTypes, goods });
    }

    private static async Task<IResult> EstimatePriceAsync(
        QuoteEstimateRequest request,
        ProCargoDbContext db,
        PricingService pricing)
    {
        City from = await db.Cities.FindAsync(request.PickupCityId) ?? throw ApiException.BadRequest("Choose a pickup city.");
        City to = await db.Cities.FindAsync(request.DropCityId) ?? throw ApiException.BadRequest("Choose a delivery city.");
        VehicleType vehicleType = await db.VehicleTypes.FindAsync(request.VehicleTypeId)
            ?? throw ApiException.BadRequest("Choose a vehicle type.");

        PriceBreakdown price = await pricing.CalculateAsync(vehicleType, PricingService.EstimateRoadKm(from, to));

        // If the load is too heavy, suggest the smallest vehicle that can carry it.
        bool overCapacity = request.WeightKg > vehicleType.MaxLoadKg;
        var suggested = overCapacity
            ? await db.VehicleTypes
                .Where(type => type.IsActive && type.MaxLoadKg >= request.WeightKg)
                .OrderBy(type => type.MaxLoadKg)
                .Select(type => new { id = type.VehicleTypeId, type.Name })
                .FirstOrDefaultAsync()
            : null;

        return Results.Ok(new
        {
            price.DistanceKm,
            price.Days,
            price.VehicleCost,
            price.DriverCost,
            price.Freight,
            price.TaxAmount,
            price.TotalAmount,
            price.GstPercent,
            overCapacity,
            maxLoadKg = vehicleType.MaxLoadKg,
            suggested,
        });
    }
}
