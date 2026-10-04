using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProCargo.IntegrationTests.Infrastructure;

/// <summary>The whole API on a real SQL Server database: controllers → services → Dapper → stored procedures.</summary>
public sealed class SqlServerApiFactory : WebApplicationFactory<Program>
{
    public SqlServerApiFactory()
    {
        TestSettings.Apply();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development returns sign-in codes in the response, so tests can sign in without SMS.
        builder.UseEnvironment("Development");
    }
}
