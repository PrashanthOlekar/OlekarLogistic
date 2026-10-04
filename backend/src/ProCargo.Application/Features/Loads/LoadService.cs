using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Application.Features.Profiles;
using ProCargo.Domain.Constants;
using ProCargo.Domain.Entities;

namespace ProCargo.Application.Features.Loads;

internal sealed class LoadService(
    ILoadRepository loads,
    IBookingRepository bookings,
    ICurrentProfiles profiles) : ILoadService
{
    public async Task<LoadsResponse> GetAvailableAsync(CancellationToken cancellationToken)
    {
        Owner owner = await profiles.GetOwnerAsync(cancellationToken);
        if (owner.KycStatus != KycStatus.Approved)
        {
            return new LoadsResponse(false, []);
        }

        AvailableLoadsData data = await loads.GetAvailableAsync(owner.OwnerId, cancellationToken);

        // Pair each load with my trucks that can carry it; keep loads with at least one match.
        List<AvailableLoad> matches = data.Loads
            .Select(load => new AvailableLoad(
                load.Id,
                load.BookingNumber,
                load.PickupDate,
                load.PickupSlot,
                load.WeightKg,
                load.Goods,
                load.From,
                load.FromAddress,
                load.To,
                load.VehicleType,
                load.DistanceKm,
                load.Payout,
                data.Vehicles
                    .Where(vehicle => vehicle.VehicleTypeId == load.VehicleTypeId && vehicle.CapacityKg >= load.WeightKg)
                    .Select(vehicle => new LoadVehicle(vehicle.Id, vehicle.RegistrationNumber, vehicle.CurrentDriverId))
                    .ToList()))
            .Where(load => load.Vehicles.Count > 0)
            .ToList();

        return new LoadsResponse(true, matches);
    }

    public async Task DeclineAsync(long bookingId, CancellationToken cancellationToken)
    {
        Owner owner = await profiles.GetOwnerAsync(cancellationToken);

        _ = await bookings.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new NotFoundException("Booking not found.");

        await loads.DeclineAsync(bookingId, owner.OwnerId, cancellationToken);
    }
}
