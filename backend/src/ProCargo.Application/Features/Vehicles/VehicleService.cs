using FluentValidation;
using Microsoft.Extensions.Logging;
using ProCargo.Application.Abstractions;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.AuditLogs;
using ProCargo.Application.Features.Common;
using ProCargo.Application.Features.Profiles;
using ProCargo.Domain.Common;
using ProCargo.Domain.Constants;
using ProCargo.Domain.Entities;

namespace ProCargo.Application.Features.Vehicles;

internal sealed class VehicleService(
    IVehicleRepository vehicles,
    IDriverRepository drivers,
    IReferenceDataRepository referenceData,
    ICurrentProfiles profiles,
    ICurrentUser currentUser,
    IAuditTrail auditTrail,
    IValidator<CreateVehicleRequest> createValidator,
    IValidator<UpdateVehicleRequest> updateValidator,
    IValidator<VerificationRequest> verificationValidator,
    ILogger<VehicleService> logger) : IVehicleService
{
    public async Task<PagedResult<VehicleListItem>> GetPagedAsync(VehicleQuery query, CancellationToken cancellationToken)
    {
        long? ownerId = currentUser.IsInRole(Roles.Admin)
            ? null
            : (await profiles.GetOwnerAsync(cancellationToken)).OwnerId;

        return await vehicles.GetPagedAsync(query, ownerId, cancellationToken);
    }

    public async Task<CreatedResource> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        Owner owner = await profiles.GetOwnerAsync(cancellationToken);

        VehicleType vehicleType = await referenceData.GetVehicleTypeAsync(request.VehicleTypeId, cancellationToken)
            ?? throw new BusinessRuleException("Choose the vehicle type.");

        string registration = IndianFormats.CleanRegistration(request.RegistrationNumber);

        var vehicle = new Vehicle
        {
            OwnerId = owner.OwnerId,
            VehicleTypeId = vehicleType.VehicleTypeId,
            RegistrationNumber = registration,
            CapacityKg = request.CapacityKg,
            MakeModel = Text.Clean(request.MakeModel),
            ManufactureYear = request.ManufactureYear,
            HomeCityId = request.HomeCityId,
            AvailabilityStatus = VehicleAvailability.Available,
            VerificationStatus = VehicleVerification.Pending,
        };

        long vehicleId = await vehicles.CreateAsync(vehicle, cancellationToken);

        await auditTrail.RecordAsync("Vehicle.Added", "Vehicle", vehicleId, new { registration }, cancellationToken);
        logger.LogInformation("Owner {OwnerId} added vehicle {VehicleId}", owner.OwnerId, vehicleId);

        return new CreatedResource(vehicleId);
    }

    public async Task UpdateAsync(long vehicleId, UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        Owner owner = await profiles.GetOwnerAsync(cancellationToken);

        Vehicle vehicle = await vehicles.GetByIdAsync(vehicleId, cancellationToken) is { } found && found.OwnerId == owner.OwnerId
            ? found
            : throw new NotFoundException("Vehicle not found.");

        if (request.AvailabilityStatus is not null && request.AvailabilityStatus != VehicleAvailability.Busy)
        {
            bool isOnTrip = await vehicles.HasActiveTripAsync(vehicle.VehicleId, cancellationToken);
            if (isOnTrip)
            {
                throw new BusinessRuleException("This vehicle is on a trip. It becomes available again after delivery.");
            }
        }

        if (request.CurrentDriverId is long driverId)
        {
            Driver? driver = await drivers.GetByIdAsync(driverId, cancellationToken);
            if (driver?.OwnerId != owner.OwnerId)
            {
                throw new BusinessRuleException("That driver is not linked to you.");
            }
        }

        bool updateDriver = request.RemoveDriver || request.CurrentDriverId is not null;
        await vehicles.PatchAsync(vehicle.VehicleId, request.AvailabilityStatus, updateDriver, request.CurrentDriverId, cancellationToken);
    }

    public async Task SetVerificationAsync(long vehicleId, VerificationRequest request, CancellationToken cancellationToken)
    {
        await verificationValidator.ValidateAndThrowAsync(request, cancellationToken);

        bool found = await vehicles.SetVerificationAsync(vehicleId, request.Approve, cancellationToken);
        if (!found)
        {
            throw new NotFoundException("Vehicle not found.");
        }

        string decision = request.Approve ? "Approved" : "Rejected";
        await auditTrail.RecordAsync($"Vehicle.{decision}", "Vehicle", vehicleId, new { request.Reason }, cancellationToken);
        logger.LogInformation("Vehicle {VehicleId} {Decision} by {AdminId}", vehicleId, decision, currentUser.UserId);
    }
}
