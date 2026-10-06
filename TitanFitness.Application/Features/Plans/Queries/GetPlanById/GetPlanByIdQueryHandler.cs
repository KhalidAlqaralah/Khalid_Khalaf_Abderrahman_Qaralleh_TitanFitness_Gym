using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Features.Plans.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Plans;

namespace TitanFitness.Application.Features.Plans.Queries.GetPlanById;

internal sealed class GetPlanByIdQueryHandler(IReadRepository<Plan> plans)
    : IRequestHandler<GetPlanByIdQuery, Result<PlanResponse>>
{
    public async Task<Result<PlanResponse>> Handle(GetPlanByIdQuery query, CancellationToken cancellationToken)
    {
        var plan = await plans.GetAll()
            .Where(p => p.Id == query.PlanId)
            .Select(p => new PlanResponse
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Terms.Price.Amount,
                DurationInMonths = p.Terms.DurationInMonths,
                MaxFreezeDays = p.Terms.MaxFreezeDays,
                MaxFreezes = p.Terms.MaxFreezes,
                GuestPassQuota = p.Terms.GuestPassQuota,
                AccessScope = p.Terms.AccessScope,
                IsPublished = p.IsPublished
            })
            .FirstOrDefaultAsync(cancellationToken);

        return plan is null ? PlanErrors.NotFound : plan;
    }
}
