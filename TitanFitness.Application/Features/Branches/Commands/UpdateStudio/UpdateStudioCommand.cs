using MediatR;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Branches.Commands.UpdateStudio;

public sealed record UpdateStudioCommand(Guid BranchId, Guid StudioId, string Name, int Capacity) : IRequest<Result>;
