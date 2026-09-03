using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Application.Memberships.PreviewFreeze;

public sealed record PreviewFreezeQuery(Guid MembershipId, DateOnly StartDate, int DurationInMonths)
    : IRequest<FreezePreview>;

public sealed record FreezePreview(
    DateOnly CurrentEndDate,
    DateOnly FreezeStart,
    DateOnly FreezeEnd,
    int DaysConsumed,
    DateOnly ProjectedEndDate,
    int FreezeDaysRemainingAfter,
    int FreezesRemainingAfter,
    bool IsAllowed,
    string? Reason);

public sealed class PreviewFreezeQueryHandler(IMembershipRepository memberships)
    : IRequestHandler<PreviewFreezeQuery, FreezePreview>
{
    public async Task<FreezePreview> Handle(PreviewFreezeQuery request, CancellationToken ct)
    {
        var membership = await memberships.GetByIdAsync(request.MembershipId, ct)
            ?? throw new NotFoundException("Membership", request.MembershipId);

        var today = DateOnly.FromDateTime(DateTime.Now);
        membership.RefreshStatus(today);

        var period = DateRange.ForMonths(request.StartDate, request.DurationInMonths);
        var days = period.TotalDays;

        string? reason =
            request.StartDate < today ? "A freeze cannot begin in the past."
            : membership.RemainingFreezes <= 0 ? $"This membership allows at most {membership.Terms.MaxFreezes} freezes."
            : !membership.Period.Contains(period) ? "A freeze must fall entirely inside the membership period."
            : membership.Freezes.Any(f => f.Period.Overlaps(period)) ? "This freeze overlaps an existing freeze."
            : membership.FreezeDaysUsed + days > membership.Terms.MaxFreezeDays
                ? $"Only {membership.RemainingFreezeDays} freeze days remain."
                : null;

        return new FreezePreview(
            membership.Period.End,
            period.Start,
            period.End,
            days,
            reason is null ? membership.Period.End.AddDays(days) : membership.Period.End,
            reason is null ? membership.RemainingFreezeDays - days : membership.RemainingFreezeDays,
            reason is null ? membership.RemainingFreezes - 1 : membership.RemainingFreezes,
            reason is null,
            reason);
    }
}