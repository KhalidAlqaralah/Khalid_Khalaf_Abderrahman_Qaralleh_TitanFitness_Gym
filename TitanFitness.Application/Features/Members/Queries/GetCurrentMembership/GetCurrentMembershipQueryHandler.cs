using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Memberships.Contracts;
using TitanFitness.Application.Features.Memberships.Shared;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Members;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.Plans;

namespace TitanFitness.Application.Features.Members.Queries.GetCurrentMembership;

/// <summary>
/// The member's current membership (Current Plan card and Freeze screen). One query: memberships joined
/// to their plan, with freezes and guest passes loaded through the aggregate's auto-included children.
/// </summary>
internal sealed class GetCurrentMembershipQueryHandler(
    IReadRepository<Member> members,
    IReadRepository<Membership> memberships,
    IReadRepository<Plan> plans,
    IClock clock) : IRequestHandler<GetCurrentMembershipQuery, Result<MembershipResponse>>
{
    public async Task<Result<MembershipResponse>> Handle(GetCurrentMembershipQuery query, CancellationToken cancellationToken)
    {
        if (!await members.GetAll().AnyAsync(m => m.Id == query.MemberId, cancellationToken))
            return MemberErrors.NotFound;

        var held = await (
            from ms in memberships.GetAll()
            join p in plans.GetAll() on ms.PlanId equals p.Id
            where ms.MemberId == query.MemberId
            select new { Membership = ms, PlanName = p.Name }).ToListAsync(cancellationToken);

        var today = clock.Today;
        var current = MembershipSelection.Current(held.Select(h => h.Membership), today);

        if (current is null)
            return Error.NotFound("Membership.None", "This member has no current membership.");

        return MembershipResponse.From(current, held.First(h => h.Membership == current).PlanName, today);
    }
}
