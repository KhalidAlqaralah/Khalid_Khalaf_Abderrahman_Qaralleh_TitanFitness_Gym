using MediatR;
using TitanFitness.Application.Common;

namespace TitanFitness.Application.Trainers.ListTrainers;

public sealed record ListTrainersQuery(bool ActiveOnly = false) : IRequest<IReadOnlyList<TrainerListItem>>;

public sealed class ListTrainersQueryHandler(IReadQueries queries)
    : IRequestHandler<ListTrainersQuery, IReadOnlyList<TrainerListItem>>
{
    public Task<IReadOnlyList<TrainerListItem>> Handle(ListTrainersQuery request, CancellationToken ct)
        => queries.ListTrainersAsync(request.ActiveOnly, ct);
}