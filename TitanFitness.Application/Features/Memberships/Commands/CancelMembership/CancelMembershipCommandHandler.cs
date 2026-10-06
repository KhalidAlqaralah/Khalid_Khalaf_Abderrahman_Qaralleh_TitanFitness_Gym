using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.Features.Memberships.Commands.CancelMembership;

internal sealed class CancelMembershipCommandHandler(
    IWriteRepository<Membership> memberships,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<CancelMembershipCommand, Result>
{
    public async Task<Result> Handle(CancelMembershipCommand command, CancellationToken cancellationToken)
    {
        var membership = await memberships.GetByIdAsync(command.MembershipId, cancellationToken);
        if (membership is null)
            return MembershipErrors.NotFound;

        var cancelled = membership.Cancel(clock.Today);
        if (cancelled.IsFailure)
            return cancelled;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
