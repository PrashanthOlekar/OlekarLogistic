using ProCargo.Domain.Entities;

namespace ProCargo.Domain.Pricing;

/// <summary>
/// Estimated road distance between two cities: straight-line distance × 1.24.
/// Replace with a Google Maps or Azure Maps call for exact road distances.
/// </summary>
public static class DistanceEstimator
{
    public const decimal SameCityDistanceKm = 18m;
    private const double RoadToStraightLineRatio = 1.24;
    private const double EarthRadiusKm = 6371;

    public static decimal EstimateRoadKm(City from, City to)
    {
        if (from.CityId == to.CityId)
        {
            return SameCityDistanceKm;
        }

        const double toRadians = Math.PI / 180;

        double lat1 = (double)from.Latitude * toRadians;
        double lat2 = (double)to.Latitude * toRadians;
        double deltaLat = lat2 - lat1;
        double deltaLng = ((double)to.Longitude - (double)from.Longitude) * toRadians;

        double a = Math.Pow(Math.Sin(deltaLat / 2), 2)
                 + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(deltaLng / 2), 2);
        double straightLineKm = 2 * EarthRadiusKm * Math.Asin(Math.Sqrt(a));

        return Math.Round((decimal)(straightLineKm * RoadToStraightLineRatio), 1);
    }
}
