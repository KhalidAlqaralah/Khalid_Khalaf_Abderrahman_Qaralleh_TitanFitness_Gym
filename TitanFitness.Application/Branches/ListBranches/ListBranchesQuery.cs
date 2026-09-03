using MediatR;
using TitanFitness.Application.Common;

namespace TitanFitness.Application.Branches.ListBranches;

public sealed record ListBranchesQuery : IRequest<IReadOnlyList<BranchListItem>>;

public sealed class ListBranchesQueryHandler(IReadQueries queries)
    : IRequestHandler<ListBranchesQuery, IReadOnlyList<BranchListItem>>
{
    public Task<IReadOnlyList<BranchListItem>> Handle(ListBranchesQuery request, CancellationToken ct)
        => queries.ListBranchesAsync(ct);
}