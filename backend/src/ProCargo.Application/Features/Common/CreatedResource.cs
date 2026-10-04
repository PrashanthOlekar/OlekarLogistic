namespace ProCargo.Application.Features.Common;

/// <summary>The id of a record that was just created (returned with HTTP 201).</summary>
public sealed record CreatedResource(long Id);
