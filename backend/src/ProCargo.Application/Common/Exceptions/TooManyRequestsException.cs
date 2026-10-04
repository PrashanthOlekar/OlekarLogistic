namespace ProCargo.Application.Common.Exceptions;

/// <summary>Too many attempts in a short time. Returned as HTTP 429. The message is shown to the user.</summary>
public sealed class TooManyRequestsException(string message) : Exception(message);
