using MediatR;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Branches.Commands.AddStudio;

public sealed record AddStudioCommand(Guid BranchId, string Name, int Capacity) : IRequest<Result<Guid>>;
