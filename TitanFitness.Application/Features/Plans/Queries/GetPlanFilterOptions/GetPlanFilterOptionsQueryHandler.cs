using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Features.Plans.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Plans;

namespace TitanFitness.Application.Features.Plans.Queries.GetPlanFilterOptions;

internal sealed class GetPlanFilterOptionsQueryHandler(IReadRepository<Plan> plans)
    : IRequestHandler<GetPlanFilterOptionsQuery, PlanFilterOptionsResponse>
{
    public async Task<PlanFilterOptionsResponse> Handle(GetPlanFilterOptionsQuery query, CancellationToken cancellationToken)
    {
        var durations = await plans.GetAll()
            .Select(p => p.Terms.DurationInMonths)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync(cancellationToken);

        var prices = await plans.GetAll()
            .GroupBy(_ => 1)
            .Select(g => new { Min = g.Min(p => p.Terms.Price.Amount), Max = g.Max(p => p.Terms.Price.Amount) })
            .FirstOrDefaultAsync(cancellationToken);

        return new PlanFilterOptionsResponse(durations, prices?.Min ?? 0, prices?.Max ?? 0);
    }
}
