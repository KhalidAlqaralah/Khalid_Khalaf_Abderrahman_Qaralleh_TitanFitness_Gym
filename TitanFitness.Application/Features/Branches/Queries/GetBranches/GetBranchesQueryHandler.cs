using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Features.Branches.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;

namespace TitanFitness.Application.Features.Branches.Queries.GetBranches;

internal sealed class GetBranchesQueryHandler(IReadRepository<Branch> branches)
    : IRequestHandler<GetBranchesQuery, IReadOnlyList<BranchResponse>>
{
    public async Task<IReadOnlyList<BranchResponse>> Handle(GetBranchesQuery query, CancellationToken cancellationToken) =>
        await branches.GetAll()
            .OrderBy(b => b.Name)
            .Select(b => new BranchResponse(
                b.Id,
                b.Name,
                b.Address,
                b.Hours.Opens,
                b.Hours.Closes,
                b.Studios.OrderBy(s => s.Name).Select(s => new StudioResponse(s.Id, s.Name, s.Capacity)).ToList()))
            .ToListAsync(cancellationToken);
}
