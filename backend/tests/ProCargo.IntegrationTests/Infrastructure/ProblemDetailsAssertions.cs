using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProCargo.IntegrationTests.Infrastructure;

internal static class ProblemDetailsAssertions
{
    /// <summary>Checks the status code and that the body is ProblemDetails; returns the body.</summary>
    public static async Task<JsonElement> ShouldBeProblemAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"Expected {(int)expected} but got {(int)response.StatusCode}: {body}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        JsonElement problem = JsonDocument.Parse(body).RootElement;
        Assert.Equal((int)expected, problem.GetProperty("status").GetInt32());
        return problem;
    }

    public static async Task<T> ShouldBeAsync<T>(this HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            string body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected {(int)expected} but got {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    public static async Task ShouldBeAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            string body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected {(int)expected} but got {(int)response.StatusCode} on {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}: {body}");
        }
    }
}
