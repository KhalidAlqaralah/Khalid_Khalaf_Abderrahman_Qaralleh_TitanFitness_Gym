using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Plans;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Domain.Memberships;

/// <summary>
/// What a member bought. Holds its own copy of the agreed terms and owns its freezes and guest passes,
/// so every rule that needs the whole collection (freeze caps, guest pass quota) is checked here.
/// </summary>
public sealed class Membership : AggregateRoot
{
    public const int MaxFreezeMonths = 12;

    private readonly List<Freeze> _freezes = [];
    private readonly List<GuestPass> _guestPasses = [];

    public Guid MemberId { get; private set; }
    public Guid PlanId { get; private set; }
    public DateTime PurchasedOn { get; private set; }
    public DateRange Period { get; private set; } = null!;
    public MembershipTerms Terms { get; private set; } = null!;
    public MembershipStatus Status { get; private set; }
    public DateOnly? CancelledOn { get; private set; }

    public IReadOnlyCollection<Freeze> Freezes => _freezes.AsReadOnly();
    public IReadOnlyCollection<GuestPass> GuestPasses => _guestPasses.AsReadOnly();

    private Membership()
    {
    }

    /// <summary>
    /// Sells <paramref name="plan"/> to a member. The plan's terms are copied now and never read again.
    /// </summary>
    public static Result<Membership> Purchase(Guid memberId, Plan plan, DateOnly startDate, DateTime now)
    {
        var member = Guard.RequiredId(memberId, "memberId", "Member");
        if (member.IsFailure)
            return member.Error;

        if (!plan.IsPublished)
            return MembershipErrors.PlanNotPublished;

        if (startDate < DateOnly.FromDateTime(now))
            return MembershipErrors.StartInPast;

        var period = DateRange.ForMonths(startDate, plan.Terms.DurationInMonths);
        if (period.IsFailure)
            return period.Error;

        var membership = new Membership
        {
            Id = Guid.CreateVersion7(),
            MemberId = memberId,
            PlanId = plan.Id,
            PurchasedOn = now,
            Terms = plan.Terms.Snapshot(),
            Period = period.Value
        };

        membership.RefreshStatus(DateOnly.FromDateTime(now));
        return membership;
    }

    // ---------- derived facts ----------

    public int FreezeDaysUsed => _freezes.Sum(f => f.AllowanceDays);

    public int RemainingFreezeDays => Math.Max(0, Terms.MaxFreezeDays - FreezeDaysUsed);

    public int RemainingFreezes => Math.Max(0, Terms.MaxFreezes - _freezes.Count);

    public int GuestPassesIssued => _guestPasses.Count;

    public int RemainingGuestPasses => Math.Max(0, Terms.GuestPassQuota - _guestPasses.Count);

    /// <summary>Recomputes the stored status from the dates. The only place that writes <see cref="Status"/>.</summary>
    public void RefreshStatus(DateOnly today)
    {
        Status = CancelledOn is not null ? MembershipStatus.Cancelled
               : today > Period.End ? MembershipStatus.Expired
               : today < Period.Start ? MembershipStatus.Pending
               : _freezes.Any(f => f.IsActiveOn(today)) ? MembershipStatus.Frozen
               : MembershipStatus.Active;
    }

    public bool IsActiveOn(DateOnly date)
    {
        RefreshStatus(date);
        return Status is MembershipStatus.Active;
    }

    /// <summary>
    /// Decides whether this membership lets its holder in at <paramref name="branchId"/> on <paramref name="date"/>.
    /// A failure carries the refusal reason shown at the desk.
    /// </summary>
    public Result CheckEntry(DateOnly date, Guid branchId, Guid homeBranchId)
    {
        RefreshStatus(date);

        return Status switch
        {
            MembershipStatus.Cancelled => MembershipErrors.EntryCancelled,
            MembershipStatus.Pending => MembershipErrors.EntryNotStarted,
            MembershipStatus.Expired => MembershipErrors.EntryExpired,
            MembershipStatus.Frozen => MembershipErrors.EntryFrozen,
            _ when Terms.AccessScope == AccessScope.HomeBranchOnly && branchId != homeBranchId => MembershipErrors.EntryHomeBranchOnly,
            _ => Result.Success()
        };
    }

    // ---------- freezing ----------

    /// <summary>
    /// Pauses the membership for whole months. Each month counts as 30 days against the freeze allowance;
    /// the end date moves forward by exactly the calendar days frozen, so the freeze always finishes
    /// before the (extended) end of the membership.
    /// </summary>
    public Result<Freeze> ApplyFreeze(DateOnly start, int durationInMonths, FreezeReason reason, string? notes, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        RefreshStatus(today);

        if (Status is MembershipStatus.Cancelled or MembershipStatus.Expired)
            return MembershipErrors.CannotFreeze(Status);

        if (durationInMonths is < 1 or > MaxFreezeMonths)
            return Error.Validation("Freeze.Duration", $"Freeze duration must be between 1 and {MaxFreezeMonths} months.", "durationInMonths");

        if (RemainingFreezes == 0)
            return MembershipErrors.NoFreezesRemaining(Terms.MaxFreezes);

        if (start < today)
            return MembershipErrors.FreezeInPast;

        if (start < Period.Start || start >= Period.End)
            return MembershipErrors.FreezeOutsidePeriod(Period.End);

        var freeze = Freeze.Create(Id, start, durationInMonths, reason, notes, now);
        if (freeze.IsFailure)
            return freeze.Error;

        if (_freezes.Any(f => f.EffectivePeriod.Overlaps(freeze.Value.Period)))
            return MembershipErrors.FreezeOverlaps;

        if (freeze.Value.AllowanceDays > RemainingFreezeDays)
            return MembershipErrors.FreezeDaysExceeded(Terms.MaxFreezeDays, RemainingFreezeDays);

        _freezes.Add(freeze.Value);
        Period = Period.ShiftEnd(freeze.Value.DaysUsed);
        RefreshStatus(today);

        return freeze.Value;
    }

    /// <summary>Ends a freeze before its planned end and gives back the days that were not used.</summary>
    public Result EndFreezeEarly(Guid freezeId, DateOnly endedOn, DateTime now)
    {
        var freeze = _freezes.SingleOrDefault(f => f.Id == freezeId);
        if (freeze is null)
            return MembershipErrors.FreezeNotFound;

        var daysBefore = freeze.DaysUsed;

        var ended = freeze.EndEarly(endedOn);
        if (ended.IsFailure)
            return ended;

        Period = Period.ShiftEnd(-(daysBefore - freeze.DaysUsed));
        RefreshStatus(DateOnly.FromDateTime(now));
        return Result.Success();
    }

    // ---------- guest passes ----------

    public Result<GuestPass> IssueGuestPass(DateTime now)
    {
        var today = DateOnly.FromDateTime(now);

        if (!IsActiveOn(today))
            return MembershipErrors.GuestPassNotActive(Status);

        if (RemainingGuestPasses == 0)
            return MembershipErrors.GuestPassQuotaReached(Terms.GuestPassQuota);

        var pass = GuestPass.Create(Id, today);
        _guestPasses.Add(pass);
        return pass;
    }

    public Result UseGuestPass(Guid guestPassId, string? guestName, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);

        if (!IsActiveOn(today))
            return MembershipErrors.GuestPassNotActive(Status);

        var pass = _guestPasses.SingleOrDefault(p => p.Id == guestPassId);
        if (pass is null)
            return MembershipErrors.GuestPassNotFound;

        return pass.Use(today, guestName);
    }

    // ---------- lifecycle ----------

    /// <summary>Cancellation is final: a cancelled membership cannot be resumed, renewed from or reversed.</summary>
    public Result Cancel(DateOnly on)
    {
        if (CancelledOn is not null)
            return MembershipErrors.AlreadyCancelled;

        CancelledOn = on;
        Status = MembershipStatus.Cancelled;
        return Result.Success();
    }
}

public static class MembershipErrors
{
    public static readonly Error NotFound = Error.NotFound("Membership.NotFound", "The membership was not found.");

    public static readonly Error PlanNotPublished =
        Error.Conflict("Membership.PlanNotPublished", "An unpublished plan cannot be sold.", "planId");

    public static readonly Error StartInPast =
        Error.Validation("Membership.StartInPast", "A membership cannot start in the past.", "startDate");

    public static readonly Error Overlapping =
        Error.Conflict("Membership.Overlapping", "This member already holds a membership covering those dates.", "startDate");

    public static readonly Error AlreadyCancelled =
        Error.Conflict("Membership.AlreadyCancelled", "This membership is already cancelled.");

    public static readonly Error CancelledIsFinal =
        Error.Conflict("Membership.CancelledIsFinal", "A cancelled membership cannot be renewed or changed.");

    public static readonly Error ExpiredUseRenew =
        Error.Conflict("Membership.ExpiredUseRenew", "An expired membership should be renewed, not changed.");

    public static readonly Error FreezeInPast =
        Error.Validation("Freeze.InPast", "A freeze cannot begin in the past.", "startDate");

    public static readonly Error FreezeOverlaps =
        Error.Conflict("Freeze.Overlaps", "This freeze overlaps a freeze that is already scheduled.", "startDate");

    public static readonly Error FreezeNotFound =
        Error.NotFound("Freeze.NotFound", "That freeze does not belong to this membership.");

    public static readonly Error GuestPassNotFound =
        Error.NotFound("GuestPass.NotFound", "That guest pass does not belong to this membership.");

    public static readonly Error EntryCancelled = Error.Conflict("Entry.Cancelled", "Membership was cancelled.");
    public static readonly Error EntryNotStarted = Error.Conflict("Entry.NotStarted", "Membership has not started yet.");
    public static readonly Error EntryExpired = Error.Conflict("Entry.Expired", "Membership has expired.");
    public static readonly Error EntryFrozen = Error.Conflict("Entry.Frozen", "Membership is frozen.");
    public static readonly Error EntryHomeBranchOnly = Error.Conflict("Entry.HomeBranchOnly", "Membership covers the home branch only.");
    public static readonly Error EntryNoMembership = Error.Conflict("Entry.NoMembership", "No active membership.");

    public static Error CannotFreeze(MembershipStatus status) =>
        Error.Conflict("Freeze.NotAllowed", $"A {status.ToString().ToLowerInvariant()} membership cannot be frozen.");

    public static Error NoFreezesRemaining(int max) =>
        Error.Conflict("Freeze.NoneRemaining", $"This membership allows at most {max} freezes and none remain.");

    public static Error FreezeOutsidePeriod(DateOnly end) =>
        Error.Validation("Freeze.OutsidePeriod",
            $"The freeze must start on or after the membership start and before its end date ({end:MMM d, yyyy}).", "startDate");

    public static Error FreezeDaysExceeded(int max, int remaining) =>
        Error.Validation("Freeze.DaysExceeded",
            $"This membership allows at most {max} frozen days ({remaining} remaining); each month of freeze counts as 30 days.", "durationInMonths");

    public static Error GuestPassNotActive(MembershipStatus status) =>
        Error.Conflict("GuestPass.NotActive",
            $"Guest passes need an active membership (this one is {status.ToString().ToLowerInvariant()}).");

    public static Error GuestPassQuotaReached(int quota) =>
        Error.Conflict("GuestPass.QuotaReached", $"This membership allows at most {quota} guest passes.");
}
