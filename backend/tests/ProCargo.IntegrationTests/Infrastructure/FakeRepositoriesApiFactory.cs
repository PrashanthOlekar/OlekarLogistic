using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ProCargo.Application.Abstractions.Persistence;
using ProCargo.Application.Abstractions.Security;
using ProCargo.Domain.Constants;
using ProCargo.Domain.Entities;

namespace ProCargo.IntegrationTests.Infrastructure;

/// <summary>
/// The real API with its repositories replaced by NSubstitute fakes, so no database is needed.
/// Authentication, authorization, validation, routing and error handling are all real.
/// </summary>
public sealed class FakeRepositoriesApiFactory : WebApplicationFactory<Program>
{
    public FakeRepositoriesApiFactory()
    {
        TestSettings.Apply();
    }

    public IUserRepository Users { get; } = Substitute.For<IUserRepository>();

    public IBookingRepository Bookings { get; } = Substitute.For<IBookingRepository>();

    public ICustomerRepository Customers { get; } = Substitute.For<ICustomerRepository>();

    public IReferenceDataRepository ReferenceData { get; } = Substitute.For<IReferenceDataRepository>();

    public IAuditLogRepository AuditLogs { get; } = Substitute.For<IAuditLogRepository>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Every user is active unless a test says otherwise.
        Users.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(call => new User { UserId = call.Arg<long>(), Status = UserStatus.Active, Role = Roles.Customer, FullName = "Test" });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserRepository>().AddSingleton(Users);
            services.RemoveAll<IBookingRepository>().AddSingleton(Bookings);
            services.RemoveAll<ICustomerRepository>().AddSingleton(Customers);
            services.RemoveAll<IReferenceDataRepository>().AddSingleton(ReferenceData);
            services.RemoveAll<IAuditLogRepository>().AddSingleton(AuditLogs);
        });
    }

    /// <summary>An HTTP client signed in as a user with this role.</summary>
    public HttpClient CreateClientAs(string role, long userId = 1)
    {
        ITokenService tokens = Services.GetRequiredService<ITokenService>();
        string token = tokens.CreateAccessToken(new User { UserId = userId, Role = role, FullName = role + " user" }).Token;

        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
