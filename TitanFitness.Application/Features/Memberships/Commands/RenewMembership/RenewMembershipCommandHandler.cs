using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Memberships.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.Plans;

namespace TitanFitness.Application.Features.Memberships.Commands.RenewMembership;

internal sealed class RenewMembershipCommandHandler(
    IWriteRepository<Membership> memberships,
    IReadRepository<Plan> plans,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<RenewMembershipCommand, Result<MembershipCreatedResponse>>
{
    public async Task<Result<MembershipCreatedResponse>> Handle(RenewMembershipCommand command, CancellationToken cancellationToken)
    {
        var current = await memberships.GetByIdAsync(command.MembershipId, cancellationToken);
        if (current is null)
            return MembershipErrors.NotFound;

        if (current.CancelledOn is not null)
            return MembershipErrors.CancelledIsFinal;

        var plan = await plans.GetByIdAsync(command.PlanId ?? current.PlanId, cancellationToken);
        if (plan is null)
            return PlanErrors.NotFound;

        var start = current.Period.End.AddDays(1);
        if (start < clock.Today)
            start = clock.Today;

        var renewal = Membership.Purchase(current.MemberId, plan, start, clock.Now);
        if (renewal.IsFailure)
            return renewal.Error;

        var period = renewal.Value.Period;
        var overlaps = await memberships.GetAll().AnyAsync(m =>
            m.MemberId == current.MemberId
            && m.CancelledOn == null
            && m.Period.Start <= period.End
            && period.Start <= m.Period.End, cancellationToken);

        if (overlaps)
            return MembershipErrors.Overlapping;

        memberships.Add(renewal.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new MembershipCreatedResponse(renewal.Value.Id, period.Start, period.End);
    }
}
