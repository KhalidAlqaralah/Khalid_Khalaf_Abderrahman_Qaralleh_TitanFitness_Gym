using FluentValidation;
using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;

namespace TitanFitness.Application.Memberships.CancelMembership;

public sealed record CancelMembershipCommand(Guid MembershipId) : IRequest<Unit>;

public sealed class CancelMembershipCommandValidator : AbstractValidator<CancelMembershipCommand>
{
    public CancelMembershipCommandValidator() => RuleFor(x => x.MembershipId).NotEmpty();
}

public sealed class CancelMembershipCommandHandler(
    IMembershipRepository memberships,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelMembershipCommand, Unit>
{
    public async Task<Unit> Handle(CancelMembershipCommand request, CancellationToken ct)
    {
        var membership = await memberships.GetByIdAsync(request.MembershipId, ct)
            ?? throw new NotFoundException("Membership", request.MembershipId);

        membership.Cancel(DateOnly.FromDateTime(DateTime.Now));
        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}