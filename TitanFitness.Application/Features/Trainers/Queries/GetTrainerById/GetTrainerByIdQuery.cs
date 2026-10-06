using MediatR;
using TitanFitness.Application.Features.Trainers.Contracts;
using TitanFitness.Domain.Abstractions;

namespace TitanFitness.Application.Features.Trainers.Queries.GetTrainerById;

public sealed record GetTrainerByIdQuery(Guid TrainerId) : IRequest<Result<TrainerResponse>>;
