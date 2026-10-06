using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Domain.ValueObjects;

/// <summary>
/// An inclusive range of calendar days: both Start and End are part of the range.
/// 1 Jan to 31 Jan is 31 days.
/// </summary>
public sealed record DateRange
{
    public DateOnly Start { get; private set; }
    public DateOnly End { get; private set; }

    private DateRange()
    {
    }

    private DateRange(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    public static Result<DateRange> Create(DateOnly start, DateOnly end, string field = "endDate")
    {
        if (end < start)
            return Error.Validation("DateRange.EndBeforeStart", "End date cannot be before start date.", field);

        return new DateRange(start, end);
    }

    /// <summary>
    /// A range that starts on <paramref name="start"/> and lasts <paramref name="months"/> calendar months.
    /// Starting 15 Jan for one month gives 15 Jan to 14 Feb.
    /// </summary>
    public static Result<DateRange> ForMonths(DateOnly start, int months, string field = "durationInMonths")
    {
        if (months < 1)
            return Error.Validation("DateRange.Months", "Duration must be at least 1 month.", field);

        return new DateRange(start, start.AddMonths(months).AddDays(-1));
    }

    public int TotalDays => End.DayNumber - Start.DayNumber + 1;

    public bool Includes(DateOnly date) => date >= Start && date <= End;

    public bool Contains(DateRange other) => other.Start >= Start && other.End <= End;

    public bool Overlaps(DateRange other) => Start <= other.End && other.Start <= End;

    /// <summary>The same range with the end moved by <paramref name="days"/> (negative moves it back).</summary>
    internal DateRange ShiftEnd(int days) => new(Start, End.AddDays(days));

    /// <summary>The same range cut short so it ends on <paramref name="end"/>.</summary>
    internal DateRange EndingOn(DateOnly end) => new(Start, end);

    public override string ToString() => $"{Start:yyyy-MM-dd} to {End:yyyy-MM-dd}";
}
