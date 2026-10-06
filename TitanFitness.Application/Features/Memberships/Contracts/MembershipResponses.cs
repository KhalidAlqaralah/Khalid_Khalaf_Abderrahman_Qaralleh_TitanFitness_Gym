using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.Features.Memberships.Contracts;

public sealed record FreezeResponse(
    Guid Id,
    DateOnly StartDate,
    DateOnly EndDate,
    int DurationInMonths,
    FreezeReason Reason,
    string? Notes,
    DateOnly? EndedEarlyOn,
    int DaysUsed,
    int AllowanceDays);

public sealed record GuestPassResponse(Guid Id, DateOnly IssuedOn, DateOnly? UsedOn, string? GuestName);

/// <summary>
/// A membership with its usage worked out against the terms it was sold under:
/// the Current Plan card, Freezes Used, Guest Passes and the Freeze Membership screen all read this.
/// </summary>
public sealed record MembershipResponse
{
    public Guid Id { get; init; }
    public Guid MemberId { get; init; }
    public Guid PlanId { get; init; }
    public string PlanName { get; init; } = null!;
    public decimal Price { get; init; }
    public int DurationInMonths { get; init; }
    public AccessScope AccessScope { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public MembershipStatus Status { get; init; }
    public DateOnly? CancelledOn { get; init; }

    public int MaxFreezes { get; init; }
    public int FreezesUsed { get; init; }
    public int RemainingFreezes { get; init; }
    public int MaxFreezeDays { get; init; }
    public int FreezeDaysUsed { get; init; }
    public int RemainingFreezeDays { get; init; }

    public int GuestPassQuota { get; init; }
    public int GuestPassesIssued { get; init; }
    public int RemainingGuestPasses { get; init; }

    /// <summary>True when the Freeze Membership screen can be used; otherwise <see cref="CannotFreezeReason"/> says why.</summary>
    public bool CanFreeze { get; init; }
    public string? CannotFreezeReason { get; init; }

    public IReadOnlyList<FreezeResponse> Freezes { get; init; } = [];
    public IReadOnlyList<GuestPassResponse> GuestPasses { get; init; } = [];

    public static MembershipResponse From(Membership m, string planName, DateOnly today)
    {
        m.RefreshStatus(today);

        var reason = m.Status switch
        {
            MembershipStatus.Frozen => "This membership is already frozen.",
            MembershipStatus.Expired => "This membership has expired.",
            MembershipStatus.Cancelled => "This membership was cancelled.",
            _ when m.Terms.MaxFreezes == 0 => "This plan does not include freezes.",
            _ when m.RemainingFreezes == 0 => "No freezes remaining on this membership.",
            _ when m.RemainingFreezeDays == 0 => "No freeze days remaining on this membership.",
            MembershipStatus.Pending => "This membership has not started yet.",
            _ => null
        };

        return new MembershipResponse
        {
            Id = m.Id,
            MemberId = m.MemberId,
            PlanId = m.PlanId,
            PlanName = planName,
            Price = m.Terms.Price.Amount,
            DurationInMonths = m.Terms.DurationInMonths,
            AccessScope = m.Terms.AccessScope,
            StartDate = m.Period.Start,
            EndDate = m.Period.End,
            Status = m.Status,
            CancelledOn = m.CancelledOn,
            MaxFreezes = m.Terms.MaxFreezes,
            FreezesUsed = m.Freezes.Count,
            RemainingFreezes = m.RemainingFreezes,
            MaxFreezeDays = m.Terms.MaxFreezeDays,
            FreezeDaysUsed = m.FreezeDaysUsed,
            RemainingFreezeDays = m.RemainingFreezeDays,
            GuestPassQuota = m.Terms.GuestPassQuota,
            GuestPassesIssued = m.GuestPassesIssued,
            RemainingGuestPasses = m.RemainingGuestPasses,
            CanFreeze = reason is null,
            CannotFreezeReason = reason,
            Freezes = m.Freezes
                .OrderBy(f => f.Period.Start)
                .Select(f => new FreezeResponse(f.Id, f.Period.Start, f.EffectiveEnd, f.DurationInMonths, f.Reason, f.Notes, f.EndedEarlyOn, f.DaysUsed, f.AllowanceDays))
                .ToList(),
            GuestPasses = m.GuestPasses
                .OrderBy(g => g.IssuedOn)
                .Select(g => new GuestPassResponse(g.Id, g.IssuedOn, g.UsedOn, g.GuestName))
                .ToList()
        };
    }
}

public sealed record MembershipCreatedResponse(Guid Id, DateOnly StartDate, DateOnly EndDate);

public sealed record FreezeAppliedResponse(Guid FreezeId, DateOnly StartDate, DateOnly EndDate, int DaysUsed, DateOnly NewEndDate);

public sealed record GuestPassIssuedResponse(Guid GuestPassId, int RemainingGuestPasses);
