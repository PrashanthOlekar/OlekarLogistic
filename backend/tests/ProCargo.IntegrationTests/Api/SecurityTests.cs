using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NSubstitute;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Users;
using ProCargo.Domain.Constants;
using ProCargo.Domain.Entities;
using ProCargo.IntegrationTests.Infrastructure;

namespace ProCargo.IntegrationTests.Api;

/// <summary>401, 403 and the security headers, without a database.</summary>
public sealed class SecurityTests(FakeRepositoriesApiFactory factory) : IClassFixture<FakeRepositoriesApiFactory>
{
    [Theory]
    [InlineData("/api/v1/bookings")]
    [InlineData("/api/v1/trips")]
    [InlineData("/api/v1/auth/me")]
    [InlineData("/api/v1/documents")]
    public async Task Protected_endpoints_need_a_token(string url)
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync(url);

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_forged_token_is_rejected()
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIiwicm9sZSI6IkFkbWluIn0.bad-signature");

        HttpResponseMessage response = await client.GetAsync("/api/v1/approvals");

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(Roles.Customer, "/api/v1/approvals")]
    [InlineData(Roles.Owner, "/api/v1/users")]
    [InlineData(Roles.Driver, "/api/v1/bookings")]
    [InlineData(Roles.Customer, "/api/v1/loads")]
    [InlineData(Roles.Owner, "/api/v1/dashboards/admin")]
    [InlineData(Roles.Customer, "/api/v1/payments")]
    public async Task Each_role_is_kept_to_its_own_endpoints(string role, string url)
    {
        HttpResponseMessage response = await factory.CreateClientAs(role).GetAsync(url);

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admins_cannot_create_bookings_for_customers()
    {
        HttpResponseMessage response = await factory.CreateClientAs(Roles.Admin)
            .PostAsJsonAsync("/api/v1/bookings", new { pickupCityId = 1 });

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_blocked_account_is_stopped_even_with_a_valid_token()
    {
        factory.Users.GetByIdAsync(666, Arg.Any<CancellationToken>())
            .Returns(new User { UserId = 666, Status = UserStatus.Blocked, Role = Roles.Customer });

        HttpResponseMessage response = await factory.CreateClientAs(Roles.Customer, userId: 666).GetAsync("/api/v1/auth/me");

        JsonElement problem = await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden);
        Assert.Equal("This account is not active. Contact ProCargo support.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Admins_can_list_users_as_a_page()
    {
        factory.Users.GetPagedAsync(Arg.Any<UserQuery>(), Arg.Any<CancellationToken>())
            .Returns(PagedResult<UserListItem>.Create(
                [new UserListItem { Id = 2, Role = Roles.Owner, FullName = "Lakshmi Naik", Mobile = "9845012345", Status = UserStatus.Active }],
                Application.Common.Paging.PageRequest.Create(1, 20),
                totalRecords: 41));

        HttpResponseMessage response = await factory.CreateClientAs(Roles.Admin).GetAsync("/api/v1/users?pageNumber=1&pageSize=20&role=Owner");

        JsonElement page = await response.ShouldBeAsync<JsonElement>(HttpStatusCode.OK);
        Assert.Equal(41, page.GetProperty("totalRecords").GetInt64());
        Assert.Equal(3, page.GetProperty("totalPages").GetInt32());
        Assert.Equal("Lakshmi Naik", page.GetProperty("items")[0].GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task Responses_carry_security_headers()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/v1/bookings");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }
}
