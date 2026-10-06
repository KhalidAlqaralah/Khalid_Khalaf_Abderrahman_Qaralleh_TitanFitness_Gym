using MediatR;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Branches.Commands.UpdateBranch;

public sealed record UpdateBranchCommand(Guid BranchId, string Name, string? Address, TimeOnly Opens, TimeOnly Closes) : IRequest<Result>;
