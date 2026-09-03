namespace TitanFitness.Domain.Common;

internal static class Text
{
    public static string Required(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{field} is required.", field);

        var trimmed = value.Trim();

        if (trimmed.Length > maxLength)
            throw new ArgumentException($"{field} cannot exceed {maxLength} characters.", field);

        return trimmed;
    }

    public static string? Optional(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();

        if (trimmed.Length > maxLength)
            throw new ArgumentException($"{field} cannot exceed {maxLength} characters.", field);

        return trimmed;
    }
}