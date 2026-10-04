namespace ProCargo.Application.Common.Paging;

/// <summary>One page of a list, as every list endpoint returns it.</summary>
public sealed class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public required int PageNumber { get; init; }

    public required int PageSize { get; init; }

    public required long TotalRecords { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRecords / (double)PageSize);

    public static PagedResult<T> Create(IReadOnlyList<T> items, PageRequest page, long totalRecords) => new()
    {
        Items = items,
        PageNumber = page.PageNumber,
        PageSize = page.PageSize,
        TotalRecords = totalRecords,
    };
}
