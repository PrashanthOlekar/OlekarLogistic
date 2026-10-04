namespace ProCargo.IntegrationTests.Infrastructure;

/// <summary>A test that needs SQL Server. Skipped unless PROCARGO_TEST_SQL is set (CI sets it).</summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (TestSettings.SqlConnectionString is null)
        {
            Skip = $"Set {TestSettings.SqlConnectionVariable} to a ProCargo database to run this test.";
        }
    }
}
