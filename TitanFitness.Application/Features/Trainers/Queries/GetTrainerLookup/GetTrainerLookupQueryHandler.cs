using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Features.Trainers.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Trainers;

namespace TitanFitness.Application.Features.Trainers.Queries.GetTrainerLookup;

internal sealed class GetTrainerLookupQueryHandler(IReadRepository<Trainer> trainers)
    : IRequestHandler<GetTrainerLookupQuery, IReadOnlyList<TrainerLookupResponse>>
{
    public async Task<IReadOnlyList<TrainerLookupResponse>> Handle(GetTrainerLookupQuery query, CancellationToken cancellationToken)
    {
        var source = trainers.GetAll();

        if (query.BranchId is not null)
            source = source.Where(t => t.BranchId == query.BranchId);

        if (query.ActiveOnly)
            source = source.Where(t => t.IsActive);

        return await source
            .OrderBy(t => t.Name)
            .Select(t => new TrainerLookupResponse(t.Id, t.Name, t.BranchId, t.IsActive))
            .ToListAsync(cancellationToken);
    }
}
