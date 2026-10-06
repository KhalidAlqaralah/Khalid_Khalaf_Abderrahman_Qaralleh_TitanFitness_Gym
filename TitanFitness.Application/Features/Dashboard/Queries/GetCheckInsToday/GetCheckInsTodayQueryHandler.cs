using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Dashboard.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.CheckIns;

namespace TitanFitness.Application.Features.Dashboard.Queries.GetCheckInsToday;

internal sealed class GetCheckInsTodayQueryHandler(IReadRepository<CheckIn> checkIns, IClock clock)
    : IRequestHandler<GetCheckInsTodayQuery, CheckInsTodayResponse>
{
    public async Task<CheckInsTodayResponse> Handle(GetCheckInsTodayQuery query, CancellationToken cancellationToken)
    {
        var todayStart = clock.Today.ToDateTime(TimeOnly.MinValue);
        var lastWeekStart = todayStart.AddDays(-7);

        var source = checkIns.GetAll();
        if (query.BranchId is not null)
            source = source.Where(c => c.BranchId == query.BranchId);

        var today = await source.CountAsync(c => c.OccurredAt >= todayStart && c.OccurredAt < todayStart.AddDays(1), cancellationToken);
        var lastWeek = await source.CountAsync(c => c.OccurredAt >= lastWeekStart && c.OccurredAt < lastWeekStart.AddDays(1), cancellationToken);

        decimal? change = lastWeek == 0 ? null : Math.Round((today - lastWeek) * 100m / lastWeek, 1);
        return new CheckInsTodayResponse(today, lastWeek, change);
    }
}
