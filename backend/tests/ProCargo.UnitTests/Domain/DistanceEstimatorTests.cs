using ProCargo.Domain.Entities;
using ProCargo.Domain.Pricing;

namespace ProCargo.UnitTests.Domain;

public sealed class DistanceEstimatorTests
{
    private static readonly City Bengaluru = new() { CityId = 1, Name = "Bengaluru", State = "Karnataka", Latitude = 12.9716m, Longitude = 77.5946m };
    private static readonly City Hubballi = new() { CityId = 6, Name = "Hubballi", State = "Karnataka", Latitude = 15.3647m, Longitude = 75.1240m };

    [Fact]
    public void Same_city_is_a_short_local_trip()
    {
        Assert.Equal(DistanceEstimator.SameCityDistanceKm, DistanceEstimator.EstimateRoadKm(Bengaluru, Bengaluru));
    }

    [Fact]
    public void Bengaluru_to_Hubballi_is_about_460_km_by_road()
    {
        decimal km = DistanceEstimator.EstimateRoadKm(Bengaluru, Hubballi);

        Assert.InRange(km, 430m, 490m);
        Assert.Equal(km, DistanceEstimator.EstimateRoadKm(Hubballi, Bengaluru));
    }
}
