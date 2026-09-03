using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;

namespace TitanFitness.Application.Memberships.GetMembership;

public sealed record GetMembershipQuery(Guid MembershipId) : IRequest<MembershipDetail>;

public sealed record FreezeDetail(
    Guid Id, DateOnly Start, DateOnly End, int DurationInMonths,
    string Reason, string? Notes, int DaysUsed);

public sealed record GuestPassDetail(Guid Id, DateOnly IssuedOn, DateOnly? UsedOn, string? GuestName);

public sealed record MembershipDetail(
    Guid Id, Guid MemberId, Guid PlanId, string Status,
    DateOnly StartDate, DateOnly EndDate, DateTime PurchasedOn,
    decimal AgreedPrice, int AgreedDurationInMonths, int AgreedMaxFreezeDays,
    int AgreedMaxFreezes, int AgreedGuestPassQuota, string AgreedAccessScope,
    int FreezeDaysUsed, int RemainingFreezeDays, int RemainingFreezes, int RemainingGuestPasses,
    IReadOnlyList<FreezeDetail> Freezes,
    IReadOnlyList<GuestPassDetail> GuestPasses);

public sealed class GetMembershipQueryHandler(IMembershipRepository memberships)
    : IRequestHandler<GetMembershipQuery, MembershipDetail>
{
    public async Task<MembershipDetail> Handle(GetMembershipQuery request, CancellationToken ct)
    {
        var m = await memberships.GetByIdAsync(request.MembershipId, ct)
            ?? throw new NotFoundException("Membership", request.MembershipId);

        m.RefreshStatus(DateOnly.FromDateTime(DateTime.Now));

        return new MembershipDetail(
            m.Id, m.MemberId, m.PlanId, m.Status.ToString(),
            m.Period.Start, m.Period.End, m.PurchasedOn,
            m.Terms.Price.Amount, m.Terms.DurationInMonths, m.Terms.MaxFreezeDays,
            m.Terms.MaxFreezes, m.Terms.GuestPassQuota, m.Terms.AccessScope.ToString(),
            m.FreezeDaysUsed, m.RemainingFreezeDays, m.RemainingFreezes, m.RemainingGuestPasses,
            m.Freezes.Select(f => new FreezeDetail(
                f.Id, f.Period.Start, f.Period.End, f.DurationInMonths,
                f.Reason.ToString(), f.Notes, f.DaysUsed)).ToList(),
            m.GuestPasses.Select(g => new GuestPassDetail(
                g.Id, g.IssuedOn, g.UsedOn, g.GuestName)).ToList());
    }
}