namespace TitanFitness.Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; private set; }

    private Money() { }

    public Money(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Money cannot be negative.", nameof(amount));

        if (decimal.Round(amount, 2) != amount)
            throw new ArgumentException("Money cannot have more than 2 decimal places.", nameof(amount));

        Amount = amount;
    }

    public static Money Zero => new(0m);

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public override string ToString() => Amount.ToString("0.00");
}