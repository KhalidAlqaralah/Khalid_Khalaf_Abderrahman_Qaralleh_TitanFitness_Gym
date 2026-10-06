using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Domain.Memberships;

/// <summary>
/// A pause in a membership, in whole months. Owned by <see cref="Membership"/>:
/// it is only created or ended early through its membership.
/// </summary>
public sealed class Freeze : Entity
{
    public const int NotesMaxLength = 500;

    /// <summary>Each month of freeze counts as 30 days against the plan's freeze-day allowance.</summary>
    public const int AllowanceDaysPerMonth = 30;

    public Guid MembershipId { get; private set; }
    public DateRange Period { get; private set; } = null!;
    public int DurationInMonths { get; private set; }
    public FreezeReason Reason { get; private set; }
    public string? Notes { get; private set; }
    public DateTime RequestedOn { get; private set; }
    public DateOnly? EndedEarlyOn { get; private set; }

    private Freeze()
    {
    }

    internal static Result<Freeze> Create(
        Guid membershipId,
        DateOnly start,
        int durationInMonths,
        FreezeReason reason,
        string? notes,
        DateTime now)
    {
        if (!Enum.IsDefined(reason))
            return Error.Validation("Freeze.Reason", "Reason for freeze is not valid.", "reason");

        var cleanNotes = Guard.Optional(notes, NotesMaxLength, "notes", "Notes");
        if (cleanNotes.IsFailure)
            return cleanNotes.Error;

        var period = DateRange.ForMonths(start, durationInMonths);
        if (period.IsFailure)
            return period.Error;

        return new Freeze
        {
            Id = Guid.CreateVersion7(),
            MembershipId = membershipId,
            Period = period.Value,
            DurationInMonths = durationInMonths,
            Reason = reason,
            Notes = cleanNotes.Value,
            RequestedOn = now
        };
    }

    public DateOnly EffectiveEnd => EndedEarlyOn ?? Period.End;

    public DateRange EffectivePeriod => Period.EndingOn(EffectiveEnd);

    /// <summary>Calendar days the membership is actually paused; the end date moves out by this many days.</summary>
    public int DaysUsed => EffectiveEnd.DayNumber - Period.Start.DayNumber + 1;

    /// <summary>
    /// Days counted against the freeze allowance: 30 per month booked (so a 60-day plan allows a 2-month freeze),
    /// or the days actually used when the freeze was ended early.
    /// </summary>
    public int AllowanceDays => Math.Min(DaysUsed, DurationInMonths * AllowanceDaysPerMonth);

    public bool IsActiveOn(DateOnly date) => date >= Period.Start && date <= EffectiveEnd;

    internal Result EndEarly(DateOnly on)
    {
        if (EndedEarlyOn is not null)
            return Error.Conflict("Freeze.AlreadyEnded", "This freeze has already been ended early.");

        if (!Period.Includes(on))
            return Error.Validation("Freeze.EndOutside", "The end date must fall inside the freeze period.", "endedOn");

        EndedEarlyOn = on;
        return Result.Success();
    }
}
