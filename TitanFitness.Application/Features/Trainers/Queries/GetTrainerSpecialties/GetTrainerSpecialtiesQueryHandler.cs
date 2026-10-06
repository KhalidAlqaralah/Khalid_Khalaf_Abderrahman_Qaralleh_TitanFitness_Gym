using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Trainers;

namespace TitanFitness.Application.Features.Trainers.Queries.GetTrainerSpecialties;

internal sealed class GetTrainerSpecialtiesQueryHandler(IReadRepository<Trainer> trainers)
    : IRequestHandler<GetTrainerSpecialtiesQuery, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(GetTrainerSpecialtiesQuery query, CancellationToken cancellationToken) =>
        await trainers.GetAll()
            .Where(t => t.Specialty != null)
            .Select(t => t.Specialty!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(cancellationToken);
}
