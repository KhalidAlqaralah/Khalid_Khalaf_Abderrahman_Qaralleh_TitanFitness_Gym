using FluentValidation;
using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Application.Memberships.ChangePlan;

public enum PlanChangeTiming
{
    AtRenewal = 1,
    Immediately = 2
}

public sealed record ChangeMembershipPlanCommand(
    Guid MembershipId,
    Guid NewPlanId,
    PlanChangeTiming Timing) : IRequest<Guid>;

public sealed class ChangeMembershipPlanCommandValidator : AbstractValidator<ChangeMembershipPlanCommand>
{
    public ChangeMembershipPlanCommandValidator()
    {
        RuleFor(x => x.MembershipId).NotEmpty();
        RuleFor(x => x.NewPlanId).NotEmpty();
        RuleFor(x => x.Timing).IsInEnum();
    }
}

public sealed class ChangeMembershipPlanCommandHandler(
    IMembershipRepository memberships,
    IPlanRepository plans,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangeMembershipPlanCommand, Guid>
{
    public async Task<Guid> Handle(ChangeMembershipPlanCommand request, CancellationToken ct)
    {
        var current = await memberships.GetByIdAsync(request.MembershipId, ct)
            ?? throw new NotFoundException("Membership", request.MembershipId);

        var plan = await plans.GetByIdAsync(request.NewPlanId, ct)
            ?? throw new NotFoundException("Plan", request.NewPlanId);

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        current.RefreshStatus(today);

        if (current.Status is MembershipStatus.Cancelled)
            throw new InvalidOperationException("A cancelled membership cannot be changed.");

        if (current.Status is MembershipStatus.Expired)
            throw new InvalidOperationException("An expired membership should be renewed, not changed.");

        DateOnly startDate;

        if (request.Timing is PlanChangeTiming.Immediately)
        {
            current.Cancel(today);
            startDate = today;
        }
        else
        {
            startDate = current.Period.End.AddDays(1);
        }

        var period = DateRange.ForMonths(startDate, plan.Terms.DurationInMonths);

        if (await memberships.HasOverlappingMembershipAsync(current.MemberId, period, ct))
            throw new InvalidOperationException(
                "This member already holds a membership covering those dates.");

        var replacement = Membership.Purchase(current.MemberId, plan, startDate, now);

        memberships.Add(replacement);
        await unitOfWork.SaveChangesAsync(ct);

        return replacement.Id;
    }
}