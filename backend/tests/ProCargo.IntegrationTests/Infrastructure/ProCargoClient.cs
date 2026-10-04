using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProCargo.IntegrationTests.Infrastructure;

/// <summary>Signs people up and in through the real endpoints, and remembers their tokens.</summary>
internal sealed class ProCargoClient(SqlServerApiFactory factory)
{
    public const string AdminMobile = "9999999999";

    private static int _counter = Random.Shared.Next(100_000, 900_000);

    /// <summary>A mobile number no earlier test run has used.</summary>
    public static string NewMobile() =>
        "7" + (DateTime.UtcNow.Ticks % 100_000).ToString("00000") + Interlocked.Increment(ref _counter).ToString("0000")[^4..];

    public HttpClient Anonymous() => factory.CreateClient();

    public async Task<string> SendCodeAsync(string mobile, string purpose)
    {
        HttpResponseMessage response = await Anonymous().PostAsJsonAsync("/api/v1/auth/otp", new { mobile, purpose });
        JsonElement body = await response.ShouldBeAsync<JsonElement>(HttpStatusCode.OK);
        return body.GetProperty("devCode").GetString()!;
    }

    public async Task<SignedIn> SignInAsync(string mobile)
    {
        string code = await SendCodeAsync(mobile, "Login");
        HttpResponseMessage response = await Anonymous().PostAsJsonAsync("/api/v1/auth/login", new { mobile, code });
        return SignedIn.From(factory, await response.ShouldBeAsync<JsonElement>(HttpStatusCode.OK));
    }

    public async Task<SignedIn> RegisterAsync(string kind, object body, string mobile)
    {
        HttpResponseMessage response = await Anonymous().PostAsJsonAsync($"/api/v1/registrations/{kind}", body);
        return SignedIn.From(factory, await response.ShouldBeAsync<JsonElement>(HttpStatusCode.Created));
    }

    public async Task<SignedIn> RegisterCustomerAsync()
    {
        string mobile = NewMobile();
        string code = await SendCodeAsync(mobile, "Signup");
        return await RegisterAsync("customers", new { fullName = "Anitha Rao", mobile, code, email = (string?)null, companyName = "Rao Builders", gstin = (string?)null }, mobile);
    }

    public async Task<SignedIn> RegisterOwnerAsync()
    {
        string mobile = NewMobile();
        string code = await SendCodeAsync(mobile, "Signup");
        return await RegisterAsync("owners", new
        {
            fullName = "Manjunath Patil",
            mobile,
            code,
            email = (string?)null,
            businessName = "Patil Transport",
            pan = "ABCDE1234F",
            aadhaarLast4 = "1234",
            accountHolder = "Manjunath Patil",
            accountNumber = "123456789012",
            ifsc = "HDFC0001234",
            bankName = "HDFC Bank",
        }, mobile);
    }

    public async Task<SignedIn> RegisterDriverAsync(string ownerMobile)
    {
        string mobile = NewMobile();
        string code = await SendCodeAsync(mobile, "Signup");
        return await RegisterAsync("drivers", new
        {
            fullName = "Ramesh Gowda",
            mobile,
            code,
            email = (string?)null,
            licenceNumber = "KA" + mobile,
            licenceClass = "HGMV",
            licenceExpiry = "2031-12-31",
            aadhaarLast4 = "4321",
            emergencyContactName = (string?)null,
            emergencyContactPhone = (string?)null,
            ownerMobile,
        }, mobile);
    }

    /// <summary>A signed-in user: their HTTP client with the Bearer token, and their profile.</summary>
    public sealed record SignedIn(HttpClient Http, string Mobile, long UserId, string RefreshToken, JsonElement Profile)
    {
        public static SignedIn From(SqlServerApiFactory factory, JsonElement auth)
        {
            HttpClient http = factory.CreateClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetProperty("accessToken").GetString());

            JsonElement user = auth.GetProperty("user");
            return new SignedIn(
                http,
                user.GetProperty("mobile").GetString()!,
                user.GetProperty("id").GetInt64(),
                auth.GetProperty("refreshToken").GetString()!,
                user);
        }

        public long DetailId(string property) => Profile.GetProperty("detail").GetProperty(property).GetInt64();
    }
}
