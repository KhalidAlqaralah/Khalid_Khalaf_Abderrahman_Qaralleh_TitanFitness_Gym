using System.Text.RegularExpressions;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Domain.ValueObjects;

/// <summary>
/// A syntactically valid email address, stored lower case so uniqueness checks are reliable.
/// </summary>
public sealed partial record EmailAddress
{
    public const int MaxLength = 100;

    public string Value { get; private init; }

    private EmailAddress(string value) => Value = value;

    public static Result<EmailAddress> Create(string? value, string field = "email")
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("Email.Required", "Email is required.", field);

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
            return Error.Validation("Email.TooLong", $"Email cannot exceed {MaxLength} characters.", field);

        if (!Pattern().IsMatch(normalized))
            return Error.Validation("Email.Invalid", "Enter a valid email address.", field);

        return new EmailAddress(normalized);
    }

    /// <summary>Creates the value object when the email is optional: blank input gives null.</summary>
    public static Result<EmailAddress?> CreateOptional(string? value, string field = "email")
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Success<EmailAddress?>(null);

        var email = Create(value, field);
        return email.IsSuccess ? Result.Success<EmailAddress?>(email.Value) : Result.Failure<EmailAddress?>(email.Error);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex Pattern();
}
