using ProCargo.Application.Features.Bookings;

namespace ProCargo.Application.Features.Trips;

public sealed record TripDetailData(TripDetailRow Trip, IReadOnlyList<TripEventItem> Events);
