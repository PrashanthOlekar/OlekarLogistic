using System.Data;
using Dapper;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Features.Registration;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class RegistrationRepository(StoredProcedureExecutor database) : IRegistrationRepository
{
    public Task<long> CreateCustomerAsync(NewCustomerAccount account, CancellationToken cancellationToken) =>
        database.ExecuteScalarAsync<long>(StoredProcedures.RegistrationCreateCustomer, new
        {
            account.FullName,
            account.Mobile,
            account.Email,
            account.CompanyName,
            account.Gstin,
        }, cancellationToken);

    public Task<long> CreateOwnerAsync(NewOwnerAccount account, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("FullName", account.FullName, DbType.String);
        parameters.Add("Mobile", account.Mobile, DbType.AnsiString);
        parameters.Add("Email", account.Email, DbType.String);
        parameters.Add("BusinessName", account.BusinessName, DbType.String);
        parameters.Add("PanEncrypted", account.PanEncrypted, DbType.Binary);
        parameters.Add("PanLast4", account.PanLast4, DbType.AnsiStringFixedLength);
        parameters.Add("AadhaarLast4", account.AadhaarLast4, DbType.AnsiStringFixedLength);
        parameters.Add("AccountHolder", account.AccountHolder, DbType.String);
        parameters.Add("AccountNumberEncrypted", account.AccountNumberEncrypted, DbType.Binary);
        parameters.Add("AccountLast4", account.AccountLast4, DbType.AnsiStringFixedLength);
        parameters.Add("Ifsc", account.Ifsc, DbType.AnsiStringFixedLength);
        parameters.Add("BankName", account.BankName, DbType.String);

        return database.ExecuteScalarAsync<long>(StoredProcedures.RegistrationCreateOwner, parameters, cancellationToken);
    }

    public Task<long> CreateDriverAsync(NewDriverAccount account, CancellationToken cancellationToken) =>
        database.ExecuteScalarAsync<long>(StoredProcedures.RegistrationCreateDriver, new
        {
            account.FullName,
            account.Mobile,
            account.Email,
            account.OwnerId,
            account.LicenceNumber,
            account.LicenceClass,
            account.LicenceExpiry,
            account.AadhaarLast4,
            account.EmergencyContactName,
            account.EmergencyContactPhone,
        }, cancellationToken);

    public Task<bool> LicenceExistsAsync(string licenceNumber, CancellationToken cancellationToken) =>
        database.ExecuteScalarAsync<bool>(StoredProcedures.DriverLicenceExists, new { LicenceNumber = licenceNumber }, cancellationToken);
}
