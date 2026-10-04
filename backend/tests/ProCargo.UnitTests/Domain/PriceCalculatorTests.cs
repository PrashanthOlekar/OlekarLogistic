using ProCargo.Domain.Entities;
using ProCargo.Domain.Pricing;

namespace ProCargo.UnitTests.Domain;

public sealed class PriceCalculatorTests
{
    private static readonly VehicleType FourteenFeet = new()
    {
        VehicleTypeId = 4,
        Name = "14 FT Truck",
        MaxLoadKg = 4000,
        RatePerKm = 32,
        MinFare = 1800,
        DriverBattaPerDay = 700,
    };

    private static readonly PricingRates Rates = new(CommissionPercent: 7, GstPercent: 5);

    [Fact]
    public void Long_trip_uses_rate_per_km_and_one_day_per_400_km()
    {
        PriceBreakdown price = PriceCalculator.Calculate(FourteenFeet, 500m, Rates);

        Assert.Equal(16000m, price.VehicleCost);      // 32 × 500
        Assert.Equal(2, price.Days);                  // ceil(500 / 400)
        Assert.Equal(1400m, price.DriverCost);        // 2 × 700
        Assert.Equal(17400m, price.Freight);
        Assert.Equal(870m, price.TaxAmount);          // 5 %
        Assert.Equal(18270m, price.TotalAmount);
        Assert.Equal(1218m, price.Commission);        // 7 %
        Assert.Equal(16182m, price.OwnerPayout);
    }

    [Fact]
    public void Short_trip_charges_the_minimum_fare_and_one_day()
    {
        PriceBreakdown price = PriceCalculator.Calculate(FourteenFeet, 18m, Rates);

        Assert.Equal(1800m, price.VehicleCost);
        Assert.Equal(1, price.Days);
        Assert.Equal(2500m, price.Freight);
    }

    [Fact]
    public void Customer_total_is_freight_plus_tax_and_owner_gets_freight_minus_commission()
    {
        PriceBreakdown price = PriceCalculator.Calculate(FourteenFeet, 333.3m, Rates);

        Assert.Equal(price.Freight + price.TaxAmount, price.TotalAmount);
        Assert.Equal(price.Freight - price.Commission, price.OwnerPayout);
    }
}
