using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.Features.Memberships.Shared;

/// <summary>What the directory and the profile show as a member's status.</summary>
public enum MemberStatus
{
    Active = 1,
    Frozen = 2,
    Expired = 3,
    Pending = 4,
    Cancelled = 5,
    None = 6
}

/// <summary>
/// One rule, used everywhere, for which membership is a member's "current" one:
/// the latest non-cancelled membership that has started; otherwise the next one that is still to start.
/// The member directory query applies the same rule in SQL.
/// </summary>
public static class MembershipSelection
{
    public static Membership? Current(IEnumerable<Membership> memberships, DateOnly today)
    {
        var live = memberships.Where(m => m.CancelledOn is null).ToList();

        return live.Where(m => m.Period.Start <= today).OrderByDescending(m => m.Period.Start).FirstOrDefault()
            ?? live.Where(m => m.Period.Start > today).OrderBy(m => m.Period.Start).FirstOrDefault();
    }

    public static MemberStatus StatusOf(Membership? current, bool hasCancelled, DateOnly today)
    {
        if (current is null)
            return hasCancelled ? MemberStatus.Cancelled : MemberStatus.None;

        current.RefreshStatus(today);
        return current.Status switch
        {
            MembershipStatus.Active => MemberStatus.Active,
            MembershipStatus.Frozen => MemberStatus.Frozen,
            MembershipStatus.Expired => MemberStatus.Expired,
            MembershipStatus.Pending => MemberStatus.Pending,
            _ => MemberStatus.Cancelled
        };
    }

    /// <summary>
    /// Whether the member may enter <paramref name="branchId"/> (or book a class there) on <paramref name="date"/>.
    /// </summary>
    public static Result CheckAccess(IEnumerable<Membership> memberships, DateOnly date, Guid branchId, Guid homeBranchId)
    {
        var current = Current(memberships, date);
        return current is null ? MembershipErrors.EntryNoMembership : current.CheckEntry(date, branchId, homeBranchId);
    }
}
