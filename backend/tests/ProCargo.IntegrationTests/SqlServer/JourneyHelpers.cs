using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ProCargo.IntegrationTests.Infrastructure;

namespace ProCargo.IntegrationTests.SqlServer;

/// <summary>Small steps shared by the SQL Server journeys.</summary>
internal static class JourneyHelpers
{
    public static async Task<JsonElement> GetJsonAsync(this HttpClient http, string url)
    {
        HttpResponseMessage response = await http.GetAsync(url);
        return await response.ShouldBeAsync<JsonElement>(HttpStatusCode.OK);
    }

    public static IEnumerable<JsonElement> Items(this JsonElement page) => page.GetProperty("items").EnumerateArray();

    public static long Id(this JsonElement element) => element.GetProperty("id").GetInt64();

    public static string Text(this JsonElement element, string property) => element.GetProperty(property).GetString()!;

    /// <summary>A random registration number such as KA07QX5821.</summary>
    public static string NewRegistrationNumber()
    {
        char Letter() => (char)('A' + Random.Shared.Next(26));
        return $"KA{Random.Shared.Next(10, 99)}{Letter()}{Letter()}{Random.Shared.Next(1000, 9999)}";
    }

    public static async Task<HttpResponseMessage> UploadAsync(this HttpClient http, string url, string fileName, string contentType, params (string Name, string Value)[] fields)
    {
        using var form = new MultipartFormDataContent();
        foreach ((string name, string value) in fields)
        {
            form.Add(new StringContent(value), name);
        }

        var file = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46]);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);

        return await http.PostAsync(url, form);
    }

    public static async Task ExpectAsync(this Task<HttpResponseMessage> request, HttpStatusCode status) =>
        await (await request).ShouldBeAsync(status);

    public static Task<HttpResponseMessage> PutJsonAsync(this HttpClient http, string url, object body) => http.PutAsJsonAsync(url, body);
}
