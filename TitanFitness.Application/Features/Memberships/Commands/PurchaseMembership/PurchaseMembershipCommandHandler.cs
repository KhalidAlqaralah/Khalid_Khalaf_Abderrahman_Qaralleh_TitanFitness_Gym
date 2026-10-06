using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Memberships.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Members;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.Plans;

namespace TitanFitness.Application.Features.Memberships.Commands.PurchaseMembership;

internal sealed class PurchaseMembershipCommandHandler(
    IWriteRepository<Membership> memberships,
    IReadRepository<Member> members,
    IReadRepository<Plan> plans,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<PurchaseMembershipCommand, Result<MembershipCreatedResponse>>
{
    public async Task<Result<MembershipCreatedResponse>> Handle(PurchaseMembershipCommand command, CancellationToken cancellationToken)
    {
        if (!await members.GetAll().AnyAsync(m => m.Id == command.MemberId, cancellationToken))
            return MemberErrors.NotFound;

        var plan = await plans.GetByIdAsync(command.PlanId, cancellationToken);
        if (plan is null)
            return PlanErrors.NotFound;

        var membership = Membership.Purchase(command.MemberId, plan, command.StartDate, clock.Now);
        if (membership.IsFailure)
            return membership.Error;

        var period = membership.Value.Period;
        var overlaps = await memberships.GetAll().AnyAsync(m =>
            m.MemberId == command.MemberId
            && m.CancelledOn == null
            && m.Period.Start <= period.End
            && period.Start <= m.Period.End, cancellationToken);

        if (overlaps)
            return MembershipErrors.Overlapping;

        memberships.Add(membership.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new MembershipCreatedResponse(membership.Value.Id, period.Start, period.End);
    }
}
