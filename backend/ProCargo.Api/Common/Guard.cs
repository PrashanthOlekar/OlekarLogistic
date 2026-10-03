namespace ProCargo.Api.Common;

/// <summary>
/// Short checks for request validation.
/// Example: Guard.Require(request.WeightKg > 0, "Enter the weight in kg.");
/// </summary>
public static class Guard
{
    public static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw ApiException.BadRequest(message);
        }
    }
}
