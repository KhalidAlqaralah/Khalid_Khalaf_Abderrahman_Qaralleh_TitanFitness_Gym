using MediatR;
using TitanFitness.Application.Common;

namespace TitanFitness.Application.Sessions.GetSchedule;

public sealed record GetScheduleQuery(Guid? BranchId, DateOnly Date) : IRequest<IReadOnlyList<ScheduleItem>>;

public sealed class GetScheduleQueryHandler(IReadQueries queries)
    : IRequestHandler<GetScheduleQuery, IReadOnlyList<ScheduleItem>>
{
    public Task<IReadOnlyList<ScheduleItem>> Handle(GetScheduleQuery request, CancellationToken ct) =>
        queries.GetScheduleAsync(request.BranchId, request.Date, DateTime.Now, ct);
}