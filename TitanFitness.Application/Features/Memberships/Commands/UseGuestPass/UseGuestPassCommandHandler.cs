using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.Features.Memberships.Commands.UseGuestPass;

internal sealed class UseGuestPassCommandHandler(
    IWriteRepository<Membership> memberships,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<UseGuestPassCommand, Result>
{
    public async Task<Result> Handle(UseGuestPassCommand command, CancellationToken cancellationToken)
    {
        var membership = await memberships.GetByIdAsync(command.MembershipId, cancellationToken);
        if (membership is null)
            return MembershipErrors.NotFound;

        var used = membership.UseGuestPass(command.GuestPassId, command.GuestName, clock.Now);
        if (used.IsFailure)
            return used;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
