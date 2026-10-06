using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Memberships.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.Features.Memberships.Commands.FreezeMembership;

/// <summary>Confirm Freeze. Every freeze rule lives in <see cref="Membership.ApplyFreeze"/>; this only loads and saves.</summary>
internal sealed class FreezeMembershipCommandHandler(
    IWriteRepository<Membership> memberships,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<FreezeMembershipCommand, Result<FreezeAppliedResponse>>
{
    public async Task<Result<FreezeAppliedResponse>> Handle(FreezeMembershipCommand command, CancellationToken cancellationToken)
    {
        var membership = await memberships.GetByIdAsync(command.MembershipId, cancellationToken);
        if (membership is null)
            return MembershipErrors.NotFound;

        var freeze = membership.ApplyFreeze(command.StartDate, command.DurationInMonths, command.Reason, command.Notes, clock.Now);
        if (freeze.IsFailure)
            return freeze.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var f = freeze.Value;
        return new FreezeAppliedResponse(f.Id, f.Period.Start, f.Period.End, f.DaysUsed, membership.Period.End);
    }
}
