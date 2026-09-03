using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Domain.Memberships;

public sealed class Freeze
{
    public Guid Id { get; private set; }
    public Guid MembershipId { get; private set; }
    public DateRange Period { get; private set; } = null!;
    public int DurationInMonths { get; private set; }
    public FreezeReason Reason { get; private set; }
    public string? Notes { get; private set; }
    public DateTime RequestedOn { get; private set; }
    public DateOnly? EndedEarlyOn { get; private set; }

    private Freeze() { }

    internal Freeze(
        Guid membershipId,
        DateOnly start,
        int durationInMonths,
        FreezeReason reason,
        string? notes,
        DateTime nowUtc)
    {
        if (membershipId == Guid.Empty)
            throw new ArgumentException("Membership is required.", nameof(membershipId));

        if (start < DateOnly.FromDateTime(nowUtc))
            throw new InvalidOperationException("A freeze cannot begin in the past.");

        if (notes is { Length: > 0 } && notes.Trim().Length > 200)
            throw new ArgumentException("Notes cannot exceed 200 characters.", nameof(notes));

        Id = Guid.CreateVersion7();
        MembershipId = membershipId;
        Period = DateRange.ForMonths(start, durationInMonths);
        DurationInMonths = durationInMonths;
        Reason = reason;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        RequestedOn = nowUtc;
    }

    public DateOnly EffectiveEnd => EndedEarlyOn ?? Period.End;

    public int DaysUsed => EffectiveEnd.DayNumber - Period.Start.DayNumber + 1;

    public bool IsActiveOn(DateOnly date) => date >= Period.Start && date <= EffectiveEnd;

    internal void EndEarly(DateOnly on)
    {
        if (EndedEarlyOn is not null)
            throw new InvalidOperationException("This freeze has already been ended early.");

        if (!Period.Includes(on))
            throw new ArgumentException("The end date must fall inside the freeze period.", nameof(on));

        EndedEarlyOn = on;
    }
}