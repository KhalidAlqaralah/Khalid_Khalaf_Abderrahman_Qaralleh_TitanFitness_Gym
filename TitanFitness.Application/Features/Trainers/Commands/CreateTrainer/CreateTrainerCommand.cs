using MediatR;
using TitanFitness.Application.Features.Trainers.Contracts;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Trainers.Commands.CreateTrainer;

public sealed record CreateTrainerCommand(string Name, string? Specialty, Guid BranchId, string Email, string? Phone, bool IsActive)
    : IRequest<Result<TrainerCreatedResponse>>;
