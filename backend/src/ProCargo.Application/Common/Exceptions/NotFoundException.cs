namespace ProCargo.Application.Common.Exceptions;

/// <summary>The record doesn't exist, or the caller may not see it. Returned as HTTP 404. The message is shown to the user.</summary>
public sealed class NotFoundException(string message) : Exception(message);
