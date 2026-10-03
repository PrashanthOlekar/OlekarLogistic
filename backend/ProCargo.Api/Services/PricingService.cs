using ProCargo.Api.Data.Entities;
using ProCargo.Api.Domain;

namespace ProCargo.Api.Services;

/// <summary>
/// Works out trip prices from the rate card (VehicleTypes) and the Settings table.
///
///   Vehicle cost = max(minimum fare, rate per km × distance)
///   Driver cost  = driver allowance per day × days (one day per 400 km)
///   Freight      = vehicle cost + driver cost
///   Customer pays: freight + GST
///   Owner gets:    freight − commission
/// </summary>
public class PricingService
{
    private const decimal KmPerDrivingDay = 400m;
    private const decimal SameCityDistanceKm = 18m;
    private const double RoadToStraightLineRatio = 1.24;

    private readonly SettingsService _settings;

    public PricingService(SettingsService settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Estimated road distance between two cities: straight-line distance × 1.24.
    /// Replace with a Google Maps or Azure Maps call for exact road distances.
    /// </summary>
    public static decimal EstimateRoadKm(City from, City to)
    {
        if (from.CityId == to.CityId)
        {
            return SameCityDistanceKm;
        }

        const double earthRadiusKm = 6371;
        const double toRadians = Math.PI / 180;

        double lat1 = (double)from.Latitude * toRadians;
        double lat2 = (double)to.Latitude * toRadians;
        double deltaLat = lat2 - lat1;
        double deltaLng = ((double)to.Longitude - (double)from.Longitude) * toRadians;

        double a = Math.Pow(Math.Sin(deltaLat / 2), 2)
                 + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(deltaLng / 2), 2);
        double straightLineKm = 2 * earthRadiusKm * Math.Asin(Math.Sqrt(a));

        return Math.Round((decimal)(straightLineKm * RoadToStraightLineRatio), 1);
    }

    public async Task<PriceBreakdown> CalculateAsync(VehicleType vehicleType, decimal distanceKm)
    {
        decimal commissionPercent = await _settings.GetDecimalAsync("CommissionPercent", 7);
        decimal gstPercent = await _settings.GetDecimalAsync("GstOnFreightPercent", 5);

        decimal vehicleCost = Math.Round(Math.Max(vehicleType.MinFare, vehicleType.RatePerKm * distanceKm), 0);
        int days = Math.Max(1, (int)Math.Ceiling(distanceKm / KmPerDrivingDay));
        decimal driverCost = days * vehicleType.DriverBattaPerDay;
        decimal freight = vehicleCost + driverCost;

        decimal tax = Math.Round(freight * gstPercent / 100m, 2);
        decimal commission = Math.Round(freight * commissionPercent / 100m, 2);

        return new PriceBreakdown(
            DistanceKm: distanceKm,
            Days: days,
            VehicleCost: vehicleCost,
            DriverCost: driverCost,
            Freight: freight,
            GstPercent: gstPercent,
            TaxAmount: tax,
            TotalAmount: freight + tax,
            CommissionPercent: commissionPercent,
            Commission: commission,
            OwnerPayout: freight - commission);
    }

    /// <summary>Prices a trip and returns a new quote, valid for QuoteValidityMinutes.</summary>
    public async Task<Quote> CreateQuoteAsync(VehicleType vehicleType, decimal distanceKm)
    {
        PriceBreakdown price = await CalculateAsync(vehicleType, distanceKm);
        int validMinutes = await _settings.GetIntAsync("QuoteValidityMinutes", 30);

        return new Quote
        {
            DistanceKm = price.DistanceKm,
            VehicleCost = price.VehicleCost,
            DriverCost = price.DriverCost,
            LoadingCharges = 0,
            PlatformFee = price.Commission,
            TaxAmount = price.TaxAmount,
            TotalAmount = price.TotalAmount,
            OwnerPayout = price.OwnerPayout,
            ValidUntil = DateTime.UtcNow.AddMinutes(validMinutes),
            Status = QuoteStatus.Sent,
        };
    }
}
