using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Memberships.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.Features.Memberships.Commands.IssueGuestPass;

internal sealed class IssueGuestPassCommandHandler(
    IWriteRepository<Membership> memberships,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<IssueGuestPassCommand, Result<GuestPassIssuedResponse>>
{
    public async Task<Result<GuestPassIssuedResponse>> Handle(IssueGuestPassCommand command, CancellationToken cancellationToken)
    {
        var membership = await memberships.GetByIdAsync(command.MembershipId, cancellationToken);
        if (membership is null)
            return MembershipErrors.NotFound;

        var pass = membership.IssueGuestPass(clock.Now);
        if (pass.IsFailure)
            return pass.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new GuestPassIssuedResponse(pass.Value.Id, membership.RemainingGuestPasses);
    }
}
