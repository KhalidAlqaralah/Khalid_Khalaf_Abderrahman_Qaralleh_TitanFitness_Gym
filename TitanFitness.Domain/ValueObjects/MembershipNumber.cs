using System.Globalization;
using System.Text.RegularExpressions;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Domain.ValueObjects;

/// <summary>
/// The business identity staff use to find a member, e.g. TF-8932.
/// Always upper case, letters / digits / hyphen, at most 10 characters.
/// </summary>
public sealed partial record MembershipNumber
{
    public const string Prefix = "TF-";
    public const int MaxLength = 10;

    public string Value { get; private set; } = null!;

    private MembershipNumber()
    {
    }

    private MembershipNumber(string value) => Value = value;

    public static Result<MembershipNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("MembershipNumber.Required", "Membership number is required.", "membershipNumber");

        var normalized = value.Trim().TrimStart('#').ToUpperInvariant();

        if (normalized.Length > MaxLength)
            return Error.Validation("MembershipNumber.TooLong",
                $"Membership number cannot exceed {MaxLength} characters.", "membershipNumber");

        if (!AllowedCharacters().IsMatch(normalized))
            return Error.Validation("MembershipNumber.Invalid",
                "Membership number may only contain letters, digits and hyphens.", "membershipNumber");

        return new MembershipNumber(normalized);
    }

    /// <summary>Builds the next number in the TF-NNNN sequence.</summary>
    public static MembershipNumber FromSequence(int sequence) =>
        new($"{Prefix}{sequence.ToString("D4", CultureInfo.InvariantCulture)}");

    /// <summary>The numeric part of a TF-NNNN number, or null for numbers outside the sequence.</summary>
    public static int? SequenceOf(string value) =>
        value.StartsWith(Prefix, StringComparison.Ordinal)
        && int.TryParse(value.AsSpan(Prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var n)
            ? n
            : null;

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9-]+$")]
    private static partial Regex AllowedCharacters();
}
