using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NSubstitute;
using ProCargo.Application.Features.Bookings;
using ProCargo.Domain.Constants;
using ProCargo.Domain.Entities;
using ProCargo.IntegrationTests.Infrastructure;

namespace ProCargo.IntegrationTests.Api;

/// <summary>400, 404 and 409 as ProblemDetails, without a database.</summary>
public sealed class ValidationAndErrorTests(FakeRepositoriesApiFactory factory) : IClassFixture<FakeRepositoriesApiFactory>
{
    [Fact]
    public async Task Invalid_price_estimate_lists_every_field_problem()
    {
        HttpResponseMessage response = await factory.CreateClient()
            .PostAsJsonAsync("/api/v1/price-estimates", new { pickupCityId = 0, dropCityId = 0, vehicleTypeId = 0, weightKg = 0 });

        JsonElement problem = await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest);
        JsonElement errors = problem.GetProperty("errors");
        Assert.Equal("Validation failed", problem.GetProperty("title").GetString());
        Assert.Equal("Choose a pickup city.", errors.GetProperty("pickupCityId")[0].GetString());
        Assert.Equal("Enter the approximate weight in kg.", errors.GetProperty("weightKg")[0].GetString());
    }

    [Fact]
    public async Task Invalid_mobile_number_is_a_bad_request()
    {
        HttpResponseMessage response = await factory.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/otp", new { mobile = "12345", purpose = "Login" });

        JsonElement problem = await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal("Enter a valid 10-digit mobile number.", problem.GetProperty("errors").GetProperty("mobile")[0].GetString());
    }

    [Fact]
    public async Task Malformed_json_is_a_bad_request()
    {
        var content = new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json");

        HttpResponseMessage response = await factory.CreateClient().PostAsync("/api/v1/price-estimates", content);

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unknown_booking_is_not_found()
    {
        factory.Customers.GetByUserIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(new Customer { CustomerId = 7 });
        factory.Bookings.GetDetailAsync(424242, Arg.Any<CancellationToken>()).Returns((BookingDetailData?)null);

        HttpResponseMessage response = await factory.CreateClientAs(Roles.Customer).GetAsync("/api/v1/bookings/424242");

        JsonElement problem = await response.ShouldBeProblemAsync(HttpStatusCode.NotFound);
        Assert.Equal("Booking not found.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Paying_an_already_paid_booking_is_a_conflict()
    {
        factory.Customers.GetByUserIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(new Customer { CustomerId = 7 });
        factory.Bookings.GetByIdAsync(55, Arg.Any<CancellationToken>())
            .Returns(new Booking { BookingId = 55, CustomerId = 7, Status = BookingStatus.Confirmed });

        HttpResponseMessage response = await factory.CreateClientAs(Roles.Customer).PostAsJsonAsync("/api/v1/bookings/55/payments", new { method = "UPI" });

        JsonElement problem = await response.ShouldBeProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal("This booking is already paid or closed.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task An_admin_cannot_block_their_own_account()
    {
        HttpResponseMessage response = await factory.CreateClientAs(Roles.Admin, userId: 9)
            .PatchAsJsonAsync("/api/v1/users/9", new { status = "Blocked" });

        JsonElement problem = await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal("You can't block your own account.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Unknown_routes_are_not_found()
    {
        HttpResponseMessage response = await factory.CreateClientAs(Roles.Admin).GetAsync("/api/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/does-not-exist")]
    [InlineData("/favicon.ico")]
    [InlineData("/nothing/here")]
    public async Task Unknown_routes_are_not_found_even_when_signed_out(string url)
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync(url);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task The_root_address_opens_swagger_in_development()
    {
        HttpClient client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/swagger", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Server_errors_do_not_leak_details()
    {
        factory.ReferenceData.GetFormListsAsync(Arg.Any<CancellationToken>())
            .Returns<Task<Application.Features.ReferenceData.ReferenceDataResponse>>(_ => throw new InvalidOperationException("Connection string Server=secret;Password=hunter2"));

        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/v1/reference-data");

        JsonElement problem = await response.ShouldBeProblemAsync(HttpStatusCode.InternalServerError);
        string body = problem.GetRawText();
        Assert.DoesNotContain("hunter2", body);
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.Equal("Something went wrong on our side. Please try again.", problem.GetProperty("detail").GetString());
    }
}
