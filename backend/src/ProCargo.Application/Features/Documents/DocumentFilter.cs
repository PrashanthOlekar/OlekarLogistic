namespace ProCargo.Application.Features.Documents;

/// <summary>Whose documents to list: one profile's, or everyone's except trip photos (admin).</summary>
public sealed record DocumentFilter(string? EntityType, long? EntityId, bool ExcludeTrips)
{
    public static DocumentFilter AllExceptTrips { get; } = new(null, null, true);

    public static DocumentFilter For(string entityType, long entityId) => new(entityType, entityId, false);
}
