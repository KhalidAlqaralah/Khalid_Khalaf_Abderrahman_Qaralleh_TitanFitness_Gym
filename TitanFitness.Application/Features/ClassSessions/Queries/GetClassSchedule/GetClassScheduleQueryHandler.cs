using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.ClassSessions.Contracts;
using TitanFitness.Application.Features.ClassSessions.Shared;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.Sessions;
using TitanFitness.Domain.Trainers;

namespace TitanFitness.Application.Features.ClassSessions.Queries.GetClassSchedule;

/// <summary>Class Schedule list for one day or a week from the chosen date, optionally for one branch.</summary>
internal sealed class GetClassScheduleQueryHandler(
    IReadRepository<ClassSession> sessions,
    IReadRepository<Booking> bookings,
    IReadRepository<Branch> branches,
    IReadRepository<Trainer> trainers,
    IReadRepository<Studio> studios,
    IClock clock) : IRequestHandler<GetClassScheduleQuery, IReadOnlyList<ClassSessionResponse>>
{
    public async Task<IReadOnlyList<ClassSessionResponse>> Handle(GetClassScheduleQuery query, CancellationToken cancellationToken)
    {
        var from = query.Date ?? clock.Today;
        var to = from.AddDays(query.Days);

        var source = sessions.GetAll().Where(s => s.Slot.Date >= from && s.Slot.Date < to);
        if (query.BranchId is not null)
            source = source.Where(s => s.BranchId == query.BranchId);

        var rows = ClassSessionRows.Query(source, bookings.GetAll(), branches.GetAll(), trainers.GetAll(), studios.GetAll());

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            rows = rows.Where(r => r.ClassName.Contains(term)
                                || (r.TrainerName != null && r.TrainerName.Contains(term))
                                || (r.StudioName != null && r.StudioName.Contains(term)));
        }

        var list = await rows.OrderBy(r => r.Date).ThenBy(r => r.StartTime).ThenBy(r => r.ClassName).ToListAsync(cancellationToken);

        var now = clock.Now;
        return list.Select(r => r.WithState(now)).ToList();
    }
}
