using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;

namespace TitanFitness.Application.Memberships.FreezeMembership;

public sealed class FreezeMembershipCommandHandler(
    IMembershipRepository memberships,
    IUnitOfWork unitOfWork) : IRequestHandler<FreezeMembershipCommand, Guid>
{
    public async Task<Guid> Handle(FreezeMembershipCommand request, CancellationToken cancellationToken)
    {
        var membership = await memberships.GetByIdAsync(request.MembershipId, cancellationToken)
            ?? throw new NotFoundException("Membership", request.MembershipId);

        var freeze = membership.ApplyFreeze(
            request.StartDate,
            request.DurationInMonths,
            request.Reason,
            request.Notes,
            DateTime.Now);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return freeze.Id;
    }
}