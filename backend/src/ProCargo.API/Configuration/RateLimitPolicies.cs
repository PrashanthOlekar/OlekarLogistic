namespace ProCargo.API.Configuration;

public static class RateLimitPolicies
{
    /// <summary>Stricter limit for sign-in codes, sign-in, refresh and registration.</summary>
    public const string Auth = "auth";
}
