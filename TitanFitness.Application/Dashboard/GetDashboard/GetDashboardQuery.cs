using MediatR;
using TitanFitness.Application.Common;

namespace TitanFitness.Application.Dashboard.GetDashboard;

public sealed record GetDashboardQuery(Guid? BranchId) : IRequest<DashboardSnapshot>;

public sealed class GetDashboardQueryHandler(IReadQueries queries)
    : IRequestHandler<GetDashboardQuery, DashboardSnapshot>
{
    public Task<DashboardSnapshot> Handle(GetDashboardQuery request, CancellationToken ct) =>
        queries.GetDashboardAsync(request.BranchId, DateTime.Now, ct);
}