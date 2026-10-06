using MediatR;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Trainers.Commands.UpdateTrainer;

public sealed record UpdateTrainerCommand(Guid TrainerId, string Name, string? Specialty, Guid BranchId, string Email, string? Phone, bool IsActive)
    : IRequest<Result>;
