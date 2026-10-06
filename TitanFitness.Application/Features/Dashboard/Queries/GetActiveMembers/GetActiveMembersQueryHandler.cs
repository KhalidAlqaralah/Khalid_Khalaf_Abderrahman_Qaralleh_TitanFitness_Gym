using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Dashboard.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.CheckIns;
using TitanFitness.Domain.Members;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.Features.Dashboard.Queries.GetActiveMembers;

/// <summary>
/// Active members: a membership covering today that is not cancelled and not frozen today (joined to a grouped
/// table of today's freezes, not a subquery per row). On the floor: distinct members checked in recently.
/// </summary>
internal sealed class GetActiveMembersQueryHandler(
    IReadRepository<Membership> memberships,
    IReadRepository<Freeze> freezes,
    IReadRepository<Member> members,
    IReadRepository<CheckIn> checkIns,
    IClock clock) : IRequestHandler<GetActiveMembersQuery, ActiveMembersResponse>
{
    public const int OnFloorWindowMinutes = 120;

    public async Task<ActiveMembersResponse> Handle(GetActiveMembersQuery query, CancellationToken cancellationToken)
    {
        var now = clock.Now;
        var today = DateOnly.FromDateTime(now);

        var frozenToday =
            from f in freezes.GetAll()
            where f.Period.Start <= today && (f.EndedEarlyOn ?? f.Period.End) >= today
            group f by f.MembershipId into g
            select (Guid?)g.Key;

        var memberSource = members.GetAll();
        if (query.BranchId is not null)
            memberSource = memberSource.Where(m => m.HomeBranchId == query.BranchId);

        var active = await (
            from ms in memberships.GetAll()
            join m in memberSource on ms.MemberId equals m.Id
            join fz in frozenToday on (Guid?)ms.Id equals fz into fj
            from fz in fj.DefaultIfEmpty()
            where ms.CancelledOn == null && ms.Period.Start <= today && ms.Period.End >= today && fz == null
            select ms.MemberId).Distinct().CountAsync(cancellationToken);

        var since = now.AddMinutes(-OnFloorWindowMinutes);
        var visits = checkIns.GetAll().Where(c => c.OccurredAt >= since && c.OccurredAt <= now);
        if (query.BranchId is not null)
            visits = visits.Where(c => c.BranchId == query.BranchId);

        var onFloor = await visits.Select(c => c.MemberId).Distinct().CountAsync(cancellationToken);

        return new ActiveMembersResponse(active, onFloor, OnFloorWindowMinutes);
    }
}
