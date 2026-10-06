using MediatR;
using TitanFitness.Application.Features.Trainers.Contracts;

namespace TitanFitness.Application.Features.Trainers.Queries.GetTrainerLookup;

public sealed record GetTrainerLookupQuery(Guid? BranchId, bool ActiveOnly) : IRequest<IReadOnlyList<TrainerLookupResponse>>;
