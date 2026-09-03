namespace TitanFitness.Domain.ValueObjects;

public sealed record MembershipNumber
{
    public string Value { get; private set; } = null!;

    private MembershipNumber() { }

    public MembershipNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Membership number is required.", nameof(value));

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > 10)
            throw new ArgumentException("Membership number cannot exceed 10 characters.", nameof(value));

        Value = normalized;
    }

    public override string ToString() => Value;
}