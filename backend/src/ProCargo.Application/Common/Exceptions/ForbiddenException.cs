namespace ProCargo.Application.Common.Exceptions;

/// <summary>The caller is signed in but may not do this. Returned as HTTP 403. The message is shown to the user.</summary>
public sealed class ForbiddenException(string message) : Exception(message);
