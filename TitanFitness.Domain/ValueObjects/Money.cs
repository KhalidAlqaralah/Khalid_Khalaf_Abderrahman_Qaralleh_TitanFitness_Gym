using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Domain.ValueObjects;

/// <summary>
/// A non-negative amount with at most two decimal places.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; private init; }

    private Money(decimal amount) => Amount = amount;

    public static Money Zero { get; } = new(0m);

    public static Result<Money> Create(decimal amount, string field = "price")
    {
        if (amount < 0)
            return Error.Validation("Money.Negative", "Price cannot be negative.", field);

        if (decimal.Round(amount, 2) != amount)
            return Error.Validation("Money.Precision", "Price cannot have more than 2 decimal places.", field);

        return new Money(amount);
    }

    public override string ToString() => Amount.ToString("0.00");
}
