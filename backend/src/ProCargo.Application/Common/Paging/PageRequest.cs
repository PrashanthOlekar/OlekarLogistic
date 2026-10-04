using ProCargo.Domain.Constants;

namespace ProCargo.Application.Common.Paging;

/// <summary>Which page to return. Out-of-range values are clamped rather than rejected.</summary>
public sealed record PageRequest
{
    public const int DefaultPageSize = 20;

    private PageRequest(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public int PageNumber { get; }

    public int PageSize { get; }

    public static PageRequest Create(int? pageNumber, int? pageSize) => new(
        Math.Max(1, pageNumber ?? 1),
        Math.Clamp(pageSize ?? DefaultPageSize, 1, FieldLimits.MaxPageSize));
}
