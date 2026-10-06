using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.Features.Memberships.Commands.EndFreezeEarly;

internal sealed class EndFreezeEarlyCommandHandler(
    IWriteRepository<Membership> memberships,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<EndFreezeEarlyCommand, Result>
{
    public async Task<Result> Handle(EndFreezeEarlyCommand command, CancellationToken cancellationToken)
    {
        var membership = await memberships.GetByIdAsync(command.MembershipId, cancellationToken);
        if (membership is null)
            return MembershipErrors.NotFound;

        var ended = membership.EndFreezeEarly(command.FreezeId, command.EndedOn, clock.Now);
        if (ended.IsFailure)
            return ended;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
