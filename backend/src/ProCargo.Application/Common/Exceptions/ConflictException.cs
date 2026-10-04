namespace ProCargo.Application.Common.Exceptions;

/// <summary>The record already exists or was changed by someone else. Returned as HTTP 409. The message is shown to the user.</summary>
public sealed class ConflictException(string message) : Exception(message);
