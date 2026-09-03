namespace TitanFitness.Domain.ValueObjects;

public sealed record TimeSlot
{
    public DateOnly Date { get; private set; }
    public TimeOnly Start { get; private set; }
    public int DurationInMinutes { get; private set; }

    private TimeSlot() { }

    public TimeSlot(DateOnly date, TimeOnly start, int durationInMinutes)
    {
        if (durationInMinutes < 1)
            throw new ArgumentException("Duration must be at least 1 minute.", nameof(durationInMinutes));

        if (start.ToTimeSpan() + TimeSpan.FromMinutes(durationInMinutes) > TimeSpan.FromDays(1))
            throw new ArgumentException("A session cannot run past midnight.", nameof(durationInMinutes));

        Date = date;
        Start = start;
        DurationInMinutes = durationInMinutes;
    }

    public TimeOnly End => Start.AddMinutes(DurationInMinutes);
    public DateTime StartsAt => Date.ToDateTime(Start);
    public DateTime EndsAt => StartsAt.AddMinutes(DurationInMinutes);

    public bool Overlaps(TimeSlot other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return StartsAt < other.EndsAt && other.StartsAt < EndsAt;
    }

    public bool HasStartedBy(DateTime now) => now >= StartsAt;
    public bool HasFinishedBy(DateTime now) => now >= EndsAt;

    public override string ToString() => $"{Date:yyyy-MM-dd} {Start:HH\\:mm}–{End:HH\\:mm}";
}