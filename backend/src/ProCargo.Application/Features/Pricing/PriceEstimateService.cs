using FluentValidation;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Exceptions;
using ProCargo.Domain.Entities;
using ProCargo.Domain.Pricing;

namespace ProCargo.Application.Features.Pricing;

internal sealed class PriceEstimateService(
    IReferenceDataRepository referenceData,
    IQuotePricer pricer,
    IValidator<PriceEstimateRequest> validator) : IPriceEstimateService
{
    public async Task<PriceEstimateResponse> EstimateAsync(PriceEstimateRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        City from = await referenceData.GetCityAsync(request.PickupCityId, cancellationToken)
            ?? throw new BusinessRuleException("Choose a pickup city.");
        City to = await referenceData.GetCityAsync(request.DropCityId, cancellationToken)
            ?? throw new BusinessRuleException("Choose a delivery city.");
        VehicleType vehicleType = await referenceData.GetVehicleTypeAsync(request.VehicleTypeId, cancellationToken)
            ?? throw new BusinessRuleException("Choose a vehicle type.");

        PriceBreakdown price = await pricer.PriceAsync(vehicleType, from, to, cancellationToken);

        // If the load is too heavy, suggest the smallest vehicle that can carry it.
        bool overCapacity = request.WeightKg > vehicleType.MaxLoadKg;
        SuggestedVehicleType? suggested = null;
        if (overCapacity)
        {
            VehicleType? bigger = await referenceData.GetSmallestVehicleTypeForAsync(request.WeightKg, cancellationToken);
            suggested = bigger is null ? null : new SuggestedVehicleType(bigger.VehicleTypeId, bigger.Name);
        }

        return new PriceEstimateResponse(
            price.DistanceKm,
            price.Days,
            price.VehicleCost,
            price.DriverCost,
            price.Freight,
            price.TaxAmount,
            price.TotalAmount,
            price.GstPercent,
            overCapacity,
            vehicleType.MaxLoadKg,
            suggested);
    }
}
