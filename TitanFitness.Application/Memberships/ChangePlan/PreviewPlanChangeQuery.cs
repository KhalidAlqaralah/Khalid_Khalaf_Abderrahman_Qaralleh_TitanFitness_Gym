using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Application.Memberships.ChangePlan;

public sealed record PreviewPlanChangeQuery(
    Guid MembershipId,
    Guid NewPlanId,
    PlanChangeTiming Timing) : IRequest<PlanChangePreview>;

public sealed record PlanChangePreview(
    string CurrentStatus,
    DateOnly CurrentEndDate,
    string NewPlanName,
    DateOnly NewStartDate,
    DateOnly NewEndDate,
    decimal NewPrice,
    int NewDurationInMonths,
    int NewMaxFreezeDays,
    int NewMaxFreezes,
    int NewGuestPassQuota,
    string NewAccessScope,
    bool CurrentWillBeCancelled,
    bool IsAllowed,
    string? Reason);

public sealed class PreviewPlanChangeQueryHandler(
    IMembershipRepository memberships,
    IPlanRepository plans) : IRequestHandler<PreviewPlanChangeQuery, PlanChangePreview>
{
    public async Task<PlanChangePreview> Handle(PreviewPlanChangeQuery request, CancellationToken ct)
    {
        var current = await memberships.GetByIdAsync(request.MembershipId, ct)
            ?? throw new NotFoundException("Membership", request.MembershipId);

        var plan = await plans.GetByIdAsync(request.NewPlanId, ct)
            ?? throw new NotFoundException("Plan", request.NewPlanId);

        var today = DateOnly.FromDateTime(DateTime.Now);
        current.RefreshStatus(today);

        var immediate = request.Timing is PlanChangeTiming.Immediately;
        var startDate = immediate ? today : current.Period.End.AddDays(1);
        var period = DateRange.ForMonths(startDate, plan.Terms.DurationInMonths);

        string? reason =
            current.Status is MembershipStatus.Cancelled ? "A cancelled membership cannot be changed."
            : current.Status is MembershipStatus.Expired ? "An expired membership should be renewed, not changed."
            : !plan.IsPublished ? "That plan is not published."
            : null;

        return new PlanChangePreview(
            current.Status.ToString(),
            current.Period.End,
            plan.Name,
            startDate,
            period.End,
            plan.Terms.Price.Amount,
            plan.Terms.DurationInMonths,
            plan.Terms.MaxFreezeDays,
            plan.Terms.MaxFreezes,
            plan.Terms.GuestPassQuota,
            plan.Terms.AccessScope.ToString(),
            immediate,
            reason is null,
            reason);
    }
}