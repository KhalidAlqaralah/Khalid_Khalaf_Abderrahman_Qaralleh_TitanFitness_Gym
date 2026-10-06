using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.ClassSessions.Contracts;
using TitanFitness.Application.Features.ClassSessions.Shared;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.Sessions;
using TitanFitness.Domain.Trainers;

namespace TitanFitness.Application.Features.Dashboard.Queries.GetUpcomingClasses;

/// <summary>Today's classes that have not finished yet (running ones first), for the Upcoming Classes card.</summary>
internal sealed class GetUpcomingClassesQueryHandler(
    IReadRepository<ClassSession> sessions,
    IReadRepository<Booking> bookings,
    IReadRepository<Branch> branches,
    IReadRepository<Trainer> trainers,
    IReadRepository<Studio> studios,
    IClock clock) : IRequestHandler<GetUpcomingClassesQuery, IReadOnlyList<ClassSessionResponse>>
{
    private static readonly TimeSpan LongestClass = TimeSpan.FromMinutes(ClassSession.AllowedDurations.Max());

    public async Task<IReadOnlyList<ClassSessionResponse>> Handle(GetUpcomingClassesQuery query, CancellationToken cancellationToken)
    {
        var now = clock.Now;
        var today = DateOnly.FromDateTime(now);

        // A class still running started at most one class-length ago.
        var earliestStart = now.TimeOfDay > LongestClass ? TimeOnly.FromTimeSpan(now.TimeOfDay - LongestClass) : TimeOnly.MinValue;

        var source = sessions.GetAll().Where(s => s.Slot.Date == today && s.Slot.Start >= earliestStart);
        if (query.BranchId is not null)
            source = source.Where(s => s.BranchId == query.BranchId);

        var rows = await ClassSessionRows.Query(source, bookings.GetAll(), branches.GetAll(), trainers.GetAll(), studios.GetAll())
            .OrderBy(r => r.StartTime)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => r.WithState(now))
            .Where(r => r.State != ClassState.Completed)
            .Take(query.Take)
            .ToList();
    }
}
