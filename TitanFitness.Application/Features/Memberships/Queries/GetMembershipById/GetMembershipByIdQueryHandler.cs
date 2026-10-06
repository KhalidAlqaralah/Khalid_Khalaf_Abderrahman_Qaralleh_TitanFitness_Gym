using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Memberships.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.Plans;

namespace TitanFitness.Application.Features.Memberships.Queries.GetMembershipById;

internal sealed class GetMembershipByIdQueryHandler(
    IReadRepository<Membership> memberships,
    IReadRepository<Plan> plans,
    IClock clock) : IRequestHandler<GetMembershipByIdQuery, Result<MembershipResponse>>
{
    public async Task<Result<MembershipResponse>> Handle(GetMembershipByIdQuery query, CancellationToken cancellationToken)
    {
        var row = await (
            from ms in memberships.GetAll()
            join p in plans.GetAll() on ms.PlanId equals p.Id
            where ms.Id == query.MembershipId
            select new { Membership = ms, PlanName = p.Name }).FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? MembershipErrors.NotFound
            : MembershipResponse.From(row.Membership, row.PlanName, clock.Today);
    }
}
