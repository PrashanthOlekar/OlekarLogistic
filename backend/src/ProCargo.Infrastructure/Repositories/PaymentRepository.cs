using System.Data;
using Dapper;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Payments;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class PaymentRepository(StoredProcedureExecutor database) : IPaymentRepository
{
    public Task<PagedResult<PaymentListItem>> GetPagedAsync(PaymentQuery query, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Status", query.Status, DbType.String);
        parameters.Add("Search", query.CleanSearch, DbType.String);

        return database.QueryPagedAsync<PaymentListItem>(StoredProcedures.PaymentGetPaged, parameters, query.ToPage(), cancellationToken);
    }
}
