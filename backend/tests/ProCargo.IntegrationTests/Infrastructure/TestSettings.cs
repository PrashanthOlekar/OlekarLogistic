namespace ProCargo.IntegrationTests.Infrastructure;

/// <summary>
/// Settings for the API under test, set as environment variables before it starts
/// (they override appsettings.json, exactly as in production).
/// </summary>
internal static class TestSettings
{
    public const string SqlConnectionVariable = "PROCARGO_TEST_SQL";
    public const string SigningKey = "integration-tests-signing-key-0123456789-abcdefghij";

    private static readonly object Lock = new();
    private static bool _applied;

    public static string? SqlConnectionString => Environment.GetEnvironmentVariable(SqlConnectionVariable) is { Length: > 0 } value ? value : null;

    public static void Apply()
    {
        lock (Lock)
        {
            if (_applied)
            {
                return;
            }

            string folder = Path.Combine(Path.GetTempPath(), "procargo-tests", Guid.NewGuid().ToString("N"));

            Set("Database__ConnectionString", SqlConnectionString ?? "Server=unused;Database=unused;TrustServerCertificate=True");
            Set("Jwt__SigningKey", SigningKey);
            Set("Otp__ShowCodeInResponse", "true");
            Set("RateLimiting__AuthPermitsPerMinute", "100000");
            Set("RateLimiting__RequestsPerMinute", "100000");
            Set("Storage__LocalFolder", Path.Combine(folder, "uploads"));
            Set("Storage__KeysFolder", Path.Combine(folder, "keys"));
            _applied = true;
        }
    }

    private static void Set(string name, string value) => Environment.SetEnvironmentVariable(name, value);
}
