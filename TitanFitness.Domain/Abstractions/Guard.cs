using System.Text.RegularExpressions;

namespace TitanFitness.Domain.Abstractions;

/// <summary>
/// Small reusable checks used by factories. Each returns a Result instead of throwing.
/// </summary>
internal static partial class Guard
{
    public static Result<string> Required(string? value, int maxLength, string field, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation($"{label}.Required", $"{label} is required.", field);

        var trimmed = value.Trim();

        if (trimmed.Length > maxLength)
            return Error.Validation($"{label}.TooLong", $"{label} cannot exceed {maxLength} characters.", field);

        return trimmed;
    }

    public static Result<string?> Optional(string? value, int maxLength, string field, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Success<string?>(null);

        var trimmed = value.Trim();

        if (trimmed.Length > maxLength)
            return Error.Validation($"{label}.TooLong", $"{label} cannot exceed {maxLength} characters.", field);

        return Result.Success<string?>(trimmed);
    }

    public static Result<Guid> RequiredId(Guid id, string field, string label)
    {
        if (id == Guid.Empty)
            return Error.Validation($"{label}.Required", $"{label} is required.", field);

        return id;
    }

    public static Result<string?> OptionalPhone(string? value, string field)
    {
        var text = Optional(value, 20, field, "Phone");
        if (text.IsFailure)
            return text;

        if (text.Value is not null && !PhonePattern().IsMatch(text.Value))
            return Error.Validation("Phone.Invalid", "Phone must contain digits and may use + ( ) - and spaces.", field);

        return text;
    }

    [GeneratedRegex(@"^\+?[0-9\s\-()]{6,20}$")]
    private static partial Regex PhonePattern();
}
