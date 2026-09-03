namespace TitanFitness.Domain.ValueObjects;

public sealed record DateRange
{
    public DateOnly Start { get; private set; }
    public DateOnly End { get; private set; }

    private DateRange() { }

    public DateRange(DateOnly start, DateOnly end)
    {
        if (end < start)
            throw new ArgumentException("End date cannot be before start date.", nameof(end));

        Start = start;
        End = end;
    }

    public static DateRange ForMonths(DateOnly start, int months)
    {
        if (months < 1)
            throw new ArgumentException("Duration must be at least 1 month.", nameof(months));

        return new DateRange(start, start.AddMonths(months).AddDays(-1));
    }

    public int TotalDays => End.DayNumber - Start.DayNumber + 1;

    public bool Includes(DateOnly date) => date >= Start && date <= End;

    public bool Contains(DateRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return other.Start >= Start && other.End <= End;
    }

    public bool Overlaps(DateRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Start <= other.End && other.Start <= End;
    }

    public DateRange ExtendBy(int days)
    {
        if (days < 0)
            throw new ArgumentException("Cannot extend by a negative number of days.", nameof(days));

        return new DateRange(Start, End.AddDays(days));
    }

    public override string ToString() => $"{Start:yyyy-MM-dd} → {End:yyyy-MM-dd}";
}