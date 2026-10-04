using ProCargo.Domain.Constants;
using ProCargo.Domain.Trips;

namespace ProCargo.UnitTests.Domain;

public sealed class TripStepsTests
{
    [Theory]
    [InlineData(TripAction.EnRoute, TripStatus.Assigned, TripStatus.EnRouteToPickup)]
    [InlineData(TripAction.ReachedPickup, TripStatus.EnRouteToPickup, TripStatus.AtPickup)]
    [InlineData(TripAction.ReachedPickup, TripStatus.Assigned, TripStatus.AtPickup)]
    [InlineData(TripAction.StartTrip, TripStatus.Loaded, TripStatus.InTransit)]
    [InlineData(TripAction.ReachedDestination, TripStatus.InTransit, TripStatus.AtDestination)]
    public void Each_action_moves_one_step(string action, string from, string to)
    {
        TripStep step = TripSteps.Find(action)!;

        Assert.Contains(from, step.AllowedFrom);
        Assert.Equal(to, step.NextStatus);
    }

    [Fact]
    public void A_trip_cannot_start_before_the_pickup_code_is_confirmed()
    {
        Assert.DoesNotContain(TripStatus.AtPickup, TripSteps.Find(TripAction.StartTrip)!.AllowedFrom);
    }

    [Fact]
    public void Unknown_actions_are_not_found()
    {
        Assert.Null(TripSteps.Find("Teleport"));
        Assert.Null(TripSteps.Find(null));
    }

    [Theory]
    [InlineData(TripStatus.Assigned, true, true)]
    [InlineData(TripStatus.AtPickup, true, true)]
    [InlineData(TripStatus.Loaded, false, true)]
    [InlineData(TripStatus.AtDestination, false, true)]
    [InlineData(TripStatus.Delivered, false, false)]
    [InlineData(TripStatus.Completed, false, false)]
    public void Customer_sees_each_code_only_while_it_is_needed(string status, bool pickupShown, bool deliveryShown)
    {
        Assert.Equal(pickupShown, HandoverCodeVisibility.ShowPickupCode(status));
        Assert.Equal(deliveryShown, HandoverCodeVisibility.ShowDeliveryCode(status));
    }
}
