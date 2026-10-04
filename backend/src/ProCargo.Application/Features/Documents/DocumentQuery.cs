using ProCargo.Application.Common.Paging;

namespace ProCargo.Application.Features.Documents;

/// <summary>Filters for GET /documents.</summary>
public sealed class DocumentQuery : ListQuery
{
    /// <summary>Pending, Verified, Rejected or Expired.</summary>
    public string? Status { get; set; }
}
