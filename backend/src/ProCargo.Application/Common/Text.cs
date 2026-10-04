namespace ProCargo.Application.Common;

/// <summary>Small helpers for user-typed text.</summary>
public static class Text
{
    /// <summary>The text trimmed, or null when it is empty.</summary>
    public static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
