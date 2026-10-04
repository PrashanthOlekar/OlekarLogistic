namespace ProCargo.Application.Common.Paging;

/// <summary>
/// Paging and search sent on every list request, e.g. ?pageNumber=2&amp;pageSize=20&amp;search=KA01.
/// Each list adds its own filters by inheriting from this class.
/// </summary>
public class ListQuery
{
    /// <summary>Page to return, starting at 1.</summary>
    public int? PageNumber { get; set; }

    /// <summary>Rows per page, 1 to 100. Default 20.</summary>
    public int? PageSize { get; set; }

    /// <summary>Text to look for (numbers, names, cities...).</summary>
    public string? Search { get; set; }

    public PageRequest ToPage() => PageRequest.Create(PageNumber, PageSize);

    /// <summary>The search text trimmed, or null when empty.</summary>
    public string? CleanSearch => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
}
