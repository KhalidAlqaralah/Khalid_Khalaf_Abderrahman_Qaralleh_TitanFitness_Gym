using TitanFitness.Domain.Plans;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Domain.Memberships;

public sealed class Membership
{
    private readonly List<Freeze> _freezes = [];
    private readonly List<GuestPass> _guestPasses = [];

    public Guid Id { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid PlanId { get; private set; }
    public DateTime PurchasedOn { get; private set; }
    public DateRange Period { get; private set; } = null!;
    public MembershipTerms Terms { get; private set; } = null!;
    public MembershipStatus Status { get; private set; }
    public DateOnly? CancelledOn { get; private set; }

    public IReadOnlyCollection<Freeze> Freezes => _freezes.AsReadOnly();
    public IReadOnlyCollection<GuestPass> GuestPasses => _guestPasses.AsReadOnly();

    private Membership() { }

    public static Membership Purchase(Guid memberId, Plan plan, DateOnly startDate, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (memberId == Guid.Empty)
            throw new ArgumentException("Member is required.", nameof(memberId));

        if (!plan.IsPublished)
            throw new InvalidOperationException("An unpublished plan cannot be sold.");

        var membership = new Membership
        {
            Id = Guid.CreateVersion7(),
            MemberId = memberId,
            PlanId = plan.Id,
            PurchasedOn = nowUtc,
            Terms = plan.Terms,
            Period = DateRange.ForMonths(startDate, plan.Terms.DurationInMonths)
        };

        membership.RefreshStatus(DateOnly.FromDateTime(nowUtc));
        return membership;
    }

    // ---------- derived facts ----------

    public int FreezeDaysUsed => _freezes.Sum(f => f.DaysUsed);
    public int RemainingFreezeDays => Terms.MaxFreezeDays - FreezeDaysUsed;
    public int RemainingFreezes => Terms.MaxFreezes - _freezes.Count;
    public int RemainingGuestPasses => Terms.GuestPassQuota - _guestPasses.Count;

    public void RefreshStatus(DateOnly today)
    {
        if (CancelledOn is not null) { Status = MembershipStatus.Cancelled; return; }
        if (today > Period.End) { Status = MembershipStatus.Expired; return; }
        if (today < Period.Start) { Status = MembershipStatus.Pending; return; }

        Status = _freezes.Any(f => f.IsActiveOn(today))
            ? MembershipStatus.Frozen
            : MembershipStatus.Active;
    }

    // ---------- freezing ----------

    public Freeze ApplyFreeze(
        DateOnly start,
        int durationInMonths,
        FreezeReason reason,
        string? notes,
        DateTime nowUtc)
    {
        var today = DateOnly.FromDateTime(nowUtc);
        RefreshStatus(today);

        if (Status is MembershipStatus.Cancelled or MembershipStatus.Expired)
            throw new InvalidOperationException($"A {Status} membership cannot be frozen.");

        if (RemainingFreezes <= 0)
            throw new InvalidOperationException(
                $"This membership allows at most {Terms.MaxFreezes} freezes.");

        var freeze = new Freeze(Id, start, durationInMonths, reason, notes, nowUtc);

        if (!Period.Contains(freeze.Period))
            throw new InvalidOperationException(
                "A freeze must fall entirely inside the membership period.");

        if (_freezes.Any(f => f.Period.Overlaps(freeze.Period)))
            throw new InvalidOperationException("This freeze overlaps an existing freeze.");

        if (FreezeDaysUsed + freeze.DaysUsed > Terms.MaxFreezeDays)
            throw new InvalidOperationException(
                $"This membership allows at most {Terms.MaxFreezeDays} frozen days " +
                $"({RemainingFreezeDays} remaining).");

        _freezes.Add(freeze);
        Period = Period.ExtendBy(freeze.DaysUsed);
        RefreshStatus(today);

        return freeze;
    }

    public void EndFreezeEarly(Guid freezeId, DateOnly endedOn, DateTime nowUtc)
    {
        var freeze = _freezes.SingleOrDefault(f => f.Id == freezeId)
            ?? throw new InvalidOperationException("That freeze does not belong to this membership.");

        var daysBefore = freeze.DaysUsed;
        freeze.EndEarly(endedOn);
        var reclaimed = daysBefore - freeze.DaysUsed;

        Period = new DateRange(Period.Start, Period.End.AddDays(-reclaimed));
        RefreshStatus(DateOnly.FromDateTime(nowUtc));
    }

    // ---------- guest passes ----------

    public GuestPass IssueGuestPass(DateTime nowUtc)
    {
        var today = DateOnly.FromDateTime(nowUtc);
        RefreshStatus(today);

        if (Status is not MembershipStatus.Active)
            throw new InvalidOperationException(
                $"Guest passes can only be issued on an active membership (this one is {Status}).");

        if (RemainingGuestPasses <= 0)
            throw new InvalidOperationException(
                $"This membership allows at most {Terms.GuestPassQuota} guest passes.");

        var pass = new GuestPass(Id, today);
        _guestPasses.Add(pass);
        return pass;
    }

    public void UseGuestPass(Guid guestPassId, string? guestName, DateTime nowUtc)
    {
        var today = DateOnly.FromDateTime(nowUtc);
        RefreshStatus(today);

        if (Status is not MembershipStatus.Active)
            throw new InvalidOperationException(
                $"A guest pass cannot be used on a {Status} membership.");

        var pass = _guestPasses.SingleOrDefault(p => p.Id == guestPassId)
            ?? throw new InvalidOperationException("That guest pass does not belong to this membership.");

        pass.Use(today, guestName);
    }

    // ---------- lifecycle ----------

    public void Cancel(DateOnly on)
    {
        if (CancelledOn is not null)
            throw new InvalidOperationException("This membership is already cancelled.");

        CancelledOn = on;
        Status = MembershipStatus.Cancelled;
    }

    public bool AllowsEntryOn(DateOnly date)
    {
        RefreshStatus(date);
        return Status is MembershipStatus.Active;
    }
}