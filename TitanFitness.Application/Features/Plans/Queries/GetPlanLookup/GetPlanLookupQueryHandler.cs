using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Features.Plans.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Plans;

namespace TitanFitness.Application.Features.Plans.Queries.GetPlanLookup;

internal sealed class GetPlanLookupQueryHandler(IReadRepository<Plan> plans)
    : IRequestHandler<GetPlanLookupQuery, IReadOnlyList<PlanResponse>>
{
    public async Task<IReadOnlyList<PlanResponse>> Handle(GetPlanLookupQuery query, CancellationToken cancellationToken) =>
        await plans.GetAll()
            .Where(p => p.IsPublished)
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
            .OrderBy(p => p.Price)
            .ToListAsync(cancellationToken);
}
