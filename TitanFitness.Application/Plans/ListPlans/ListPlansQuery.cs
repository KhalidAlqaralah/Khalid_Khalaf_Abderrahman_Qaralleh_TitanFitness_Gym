using MediatR;
using TitanFitness.Application.Common;

namespace TitanFitness.Application.Plans.ListPlans;

public sealed record ListPlansQuery(bool PublishedOnly = false) : IRequest<IReadOnlyList<PlanListItem>>;

public sealed class ListPlansQueryHandler(IReadQueries queries)
    : IRequestHandler<ListPlansQuery, IReadOnlyList<PlanListItem>>
{
    public Task<IReadOnlyList<PlanListItem>> Handle(ListPlansQuery request, CancellationToken ct)
        => queries.ListPlansAsync(request.PublishedOnly, ct);
}