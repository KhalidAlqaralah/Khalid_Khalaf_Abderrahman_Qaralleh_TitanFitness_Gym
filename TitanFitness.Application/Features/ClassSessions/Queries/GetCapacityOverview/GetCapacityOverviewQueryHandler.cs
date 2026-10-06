using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.ClassSessions.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Sessions;

namespace TitanFitness.Application.Features.ClassSessions.Queries.GetCapacityOverview;

/// <summary>Capacity Overview card: places taken across the non-cancelled classes and their average fill rate.</summary>
internal sealed class GetCapacityOverviewQueryHandler(
    IReadRepository<ClassSession> sessions,
    IReadRepository<Booking> bookings,
    IClock clock) : IRequestHandler<GetCapacityOverviewQuery, CapacityOverviewResponse>
{
    public async Task<CapacityOverviewResponse> Handle(GetCapacityOverviewQuery query, CancellationToken cancellationToken)
    {
        var from = query.Date ?? clock.Today;
        var to = from.AddDays(query.Days);

        var source = sessions.GetAll().Where(s => s.Slot.Date >= from && s.Slot.Date < to && s.CancelledAt == null);
        if (query.BranchId is not null)
            source = source.Where(s => s.BranchId == query.BranchId);

        var taken =
            from b in bookings.GetAll()
            where b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Attended || b.Status == BookingStatus.NoShow
            group b by b.SessionId into g
            select new { SessionId = (Guid?)g.Key, Count = (int?)g.Count() };

        var rows = await (
            from s in source
            join t in taken on (Guid?)s.Id equals t.SessionId into tj
            from t in tj.DefaultIfEmpty()
            select new { s.CapacityLimit, Enrolled = t == null ? 0 : t.Count ?? 0 }).ToListAsync(cancellationToken);

        var fill = rows.Count == 0 ? 0m : Math.Round(rows.Average(r => (decimal)r.Enrolled / r.CapacityLimit), 4);
        return new CapacityOverviewResponse(rows.Count, rows.Sum(r => r.Enrolled), fill);
    }
}
