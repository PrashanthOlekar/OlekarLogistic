namespace ProCargo.API.ErrorHandling;

/// <summary>The "type" links in ProblemDetails responses (RFC 9457).</summary>
public static class ProblemTypes
{
    private const string Base = "https://procargo.in/errors/";

    public const string Validation = Base + "validation";
    public const string BusinessRule = Base + "business-rule";
    public const string Unauthorized = Base + "unauthorized";
    public const string Forbidden = Base + "forbidden";
    public const string NotFound = Base + "not-found";
    public const string Conflict = Base + "conflict";
    public const string TooManyRequests = Base + "too-many-requests";
    public const string NotImplemented = Base + "not-implemented";
    public const string ServerError = Base + "server-error";
}
