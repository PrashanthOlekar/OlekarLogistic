namespace ProCargo.Application.Common.Exceptions;

/// <summary>The request breaks a business rule, for example paying an expired quote. Returned as HTTP 400. The message is shown to the user.</summary>
public sealed class BusinessRuleException(string message) : Exception(message);
