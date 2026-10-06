using MediatR;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Branches.Commands.CreateBranch;

public sealed record CreateBranchCommand(string Name, string? Address, TimeOnly Opens, TimeOnly Closes) : IRequest<Result<Guid>>;
