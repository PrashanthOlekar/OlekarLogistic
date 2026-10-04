using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Infrastructure.Data;

namespace ProCargo.Infrastructure.Repositories;

internal sealed class SettingsRepository(StoredProcedureExecutor database) : ISettingsRepository
{
    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<SettingRow> rows = await database.QueryAsync<SettingRow>(StoredProcedures.SettingGetAll, null, cancellationToken);
        return rows.ToDictionary(row => row.SettingKey, row => row.SettingValue, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class SettingRow
    {
        public string SettingKey { get; set; } = string.Empty;

        public string SettingValue { get; set; } = string.Empty;
    }
}
