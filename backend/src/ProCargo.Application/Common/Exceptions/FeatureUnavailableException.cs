namespace ProCargo.Application.Common.Exceptions;

/// <summary>The feature isn't switched on yet, for example live payments. Returned as HTTP 501. The message is shown to the user.</summary>
public sealed class FeatureUnavailableException(string message) : Exception(message);
