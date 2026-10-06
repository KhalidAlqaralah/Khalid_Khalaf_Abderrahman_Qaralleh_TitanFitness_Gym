using MediatR;
using TitanFitness.Application.Features.Branches.Contracts;

namespace TitanFitness.Application.Features.Branches.Queries.GetBranches;

public sealed record GetBranchesQuery : IRequest<IReadOnlyList<BranchResponse>>;
