namespace ProCargo.Application.Common.Exceptions;

/// <summary>
/// The database failed for a reason the user can't fix. Returned as HTTP 500 with a safe message;
/// the inner exception is only logged.
/// </summary>
public sealed class DatabaseException(string message, Exception innerException) : Exception(message, innerException);
