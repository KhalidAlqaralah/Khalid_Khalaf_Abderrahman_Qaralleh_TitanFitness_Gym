using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Domain.ValueObjects;

/// <summary>
/// When a class runs: a date, a start time and a length in minutes.
/// A slot never runs past midnight.
/// </summary>
public sealed record TimeSlot
{
    public DateOnly Date { get; private set; }
    public TimeOnly Start { get; private set; }
    public int DurationInMinutes { get; private set; }

    private TimeSlot()
    {
    }

    private TimeSlot(DateOnly date, TimeOnly start, int durationInMinutes)
    {
        Date = date;
        Start = start;
        DurationInMinutes = durationInMinutes;
    }

    public static Result<TimeSlot> Create(DateOnly date, TimeOnly start, int durationInMinutes)
    {
        if (durationInMinutes < 1)
            return Error.Validation("TimeSlot.Duration", "Duration must be at least 1 minute.", "durationInMinutes");

        if (start.ToTimeSpan() + TimeSpan.FromMinutes(durationInMinutes) > TimeSpan.FromDays(1))
            return Error.Validation("TimeSlot.PastMidnight", "A class cannot run past midnight.", "startTime");

        return new TimeSlot(date, start, durationInMinutes);
    }

    public TimeOnly End => Start.AddMinutes(DurationInMinutes);

    public DateTime StartsAt => Date.ToDateTime(Start);

    public DateTime EndsAt => StartsAt.AddMinutes(DurationInMinutes);

    /// <summary>Back-to-back slots (09:00-10:00 and 10:00-11:00) do not overlap.</summary>
    public bool Overlaps(TimeSlot other) => StartsAt < other.EndsAt && other.StartsAt < EndsAt;

    public bool HasStartedBy(DateTime now) => now >= StartsAt;

    public bool HasFinishedBy(DateTime now) => now >= EndsAt;

    public override string ToString() => $"{Date:yyyy-MM-dd} {Start:HH\\:mm}-{End:HH\\:mm}";
}
