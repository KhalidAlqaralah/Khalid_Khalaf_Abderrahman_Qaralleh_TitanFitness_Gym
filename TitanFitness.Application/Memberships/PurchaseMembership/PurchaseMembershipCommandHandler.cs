using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Application.Memberships.PurchaseMembership;

public sealed class PurchaseMembershipCommandHandler(
    IMemberRepository members,
    IPlanRepository plans,
    IMembershipRepository memberships,
    IUnitOfWork unitOfWork) : IRequestHandler<PurchaseMembershipCommand, Guid>
{
    public async Task<Guid> Handle(PurchaseMembershipCommand request, CancellationToken cancellationToken)
    {
        var member = await members.GetByIdAsync(request.MemberId, cancellationToken)
            ?? throw new NotFoundException("Member", request.MemberId);

        var plan = await plans.GetByIdAsync(request.PlanId, cancellationToken)
            ?? throw new NotFoundException("Plan", request.PlanId);

        var period = DateRange.ForMonths(request.StartDate, plan.Terms.DurationInMonths);

        if (await memberships.HasOverlappingMembershipAsync(member.Id, period, cancellationToken))
            throw new InvalidOperationException(
                "This member already holds a membership covering those dates.");

        var membership = Membership.Purchase(member.Id, plan, request.StartDate, DateTime.Now);

        memberships.Add(membership);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return membership.Id;
    }
}