using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.ClassSessions.Contracts;
using TitanFitness.Application.Features.ClassSessions.Shared;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.Sessions;
using TitanFitness.Domain.Trainers;

namespace TitanFitness.Application.Features.ClassSessions.Queries.GetClassSessionById;

internal sealed class GetClassSessionByIdQueryHandler(
    IReadRepository<ClassSession> sessions,
    IReadRepository<Booking> bookings,
    IReadRepository<Branch> branches,
    IReadRepository<Trainer> trainers,
    IReadRepository<Studio> studios,
    IClock clock) : IRequestHandler<GetClassSessionByIdQuery, Result<ClassSessionResponse>>
{
    public async Task<Result<ClassSessionResponse>> Handle(GetClassSessionByIdQuery query, CancellationToken cancellationToken)
    {
        var source = sessions.GetAll().Where(s => s.Id == query.SessionId);

        var row = await ClassSessionRows.Query(source, bookings.GetAll(), branches.GetAll(), trainers.GetAll(), studios.GetAll())
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? ClassSessionErrors.NotFound : row.WithState(clock.Now);
    }
}
