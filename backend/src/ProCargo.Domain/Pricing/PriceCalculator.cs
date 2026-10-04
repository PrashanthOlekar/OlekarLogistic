using ProCargo.Domain.Entities;

namespace ProCargo.Domain.Pricing;

/// <summary>
/// Works out a trip's price from the rate card (VehicleTypes) and the Settings percentages.
///
///   Vehicle cost = max(minimum fare, rate per km × distance)
///   Driver cost  = driver allowance per day × days (one day per 400 km)
///   Freight      = vehicle cost + driver cost
///   Customer pays: freight + GST
///   Owner gets:    freight − commission
/// </summary>
public static class PriceCalculator
{
    public const decimal KmPerDrivingDay = 400m;

    public static PriceBreakdown Calculate(VehicleType vehicleType, decimal distanceKm, PricingRates rates)
    {
        decimal vehicleCost = Math.Round(Math.Max(vehicleType.MinFare, vehicleType.RatePerKm * distanceKm), 0);
        int days = Math.Max(1, (int)Math.Ceiling(distanceKm / KmPerDrivingDay));
        decimal driverCost = days * vehicleType.DriverBattaPerDay;
        decimal freight = vehicleCost + driverCost;

        decimal tax = Math.Round(freight * rates.GstPercent / 100m, 2);
        decimal commission = Math.Round(freight * rates.CommissionPercent / 100m, 2);

        return new PriceBreakdown(
            DistanceKm: distanceKm,
            Days: days,
            VehicleCost: vehicleCost,
            DriverCost: driverCost,
            Freight: freight,
            GstPercent: rates.GstPercent,
            TaxAmount: tax,
            TotalAmount: freight + tax,
            CommissionPercent: rates.CommissionPercent,
            Commission: commission,
            OwnerPayout: freight - commission);
    }
}
