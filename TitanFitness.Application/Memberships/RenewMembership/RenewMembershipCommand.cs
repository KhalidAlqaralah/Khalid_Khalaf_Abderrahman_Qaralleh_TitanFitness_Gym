using FluentValidation;
using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Application.Memberships.RenewMembership;

public sealed record RenewMembershipCommand(Guid MembershipId, Guid? PlanId) : IRequest<Guid>;

public sealed class RenewMembershipCommandValidator : AbstractValidator<RenewMembershipCommand>
{
    public RenewMembershipCommandValidator() => RuleFor(x => x.MembershipId).NotEmpty();
}

public sealed class RenewMembershipCommandHandler(
    IMembershipRepository memberships,
    IPlanRepository plans,
    IUnitOfWork unitOfWork) : IRequestHandler<RenewMembershipCommand, Guid>
{
    public async Task<Guid> Handle(RenewMembershipCommand request, CancellationToken ct)
    {
        var current = await memberships.GetByIdAsync(request.MembershipId, ct)
            ?? throw new NotFoundException("Membership", request.MembershipId);

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        current.RefreshStatus(today);

        if (current.Status is MembershipStatus.Cancelled)
            throw new InvalidOperationException("A cancelled membership cannot be renewed from.");

        var planId = request.PlanId ?? current.PlanId;

        var plan = await plans.GetByIdAsync(planId, ct)
            ?? throw new NotFoundException("Plan", planId);

        var dayAfter = current.Period.End.AddDays(1);
        var startDate = dayAfter > today ? dayAfter : today;

        var period = DateRange.ForMonths(startDate, plan.Terms.DurationInMonths);

        if (await memberships.HasOverlappingMembershipAsync(current.MemberId, period, ct))
            throw new InvalidOperationException(
                "This member already holds a membership covering those dates.");

        var renewal = Membership.Purchase(current.MemberId, plan, startDate, now);

        memberships.Add(renewal);
        await unitOfWork.SaveChangesAsync(ct);

        return renewal.Id;
    }
}