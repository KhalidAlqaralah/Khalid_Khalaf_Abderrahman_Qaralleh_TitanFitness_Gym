using MediatR;

namespace TitanFitness.Application.Features.Trainers.Queries.GetTrainerSpecialties;

/// <summary>The distinct specialties in the trainer records, for the Filter Trainers dialog.</summary>
public sealed record GetTrainerSpecialtiesQuery : IRequest<IReadOnlyList<string>>;
