using FluentValidation;
using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;

namespace TitanFitness.Application.Memberships.EndFreezeEarly;

public sealed record EndFreezeEarlyCommand(Guid MembershipId, Guid FreezeId, DateOnly EndedOn) : IRequest<Unit>;

public sealed class EndFreezeEarlyCommandValidator : AbstractValidator<EndFreezeEarlyCommand>
{
    public EndFreezeEarlyCommandValidator()
    {
        RuleFor(x => x.MembershipId).NotEmpty();
        RuleFor(x => x.FreezeId).NotEmpty();
        RuleFor(x => x.EndedOn).NotEmpty();
    }
}

public sealed class EndFreezeEarlyCommandHandler(
    IMembershipRepository memberships,
    IUnitOfWork unitOfWork) : IRequestHandler<EndFreezeEarlyCommand, Unit>
{
    public async Task<Unit> Handle(EndFreezeEarlyCommand request, CancellationToken ct)
    {
        var membership = await memberships.GetByIdAsync(request.MembershipId, ct)
            ?? throw new NotFoundException("Membership", request.MembershipId);

        membership.EndFreezeEarly(request.FreezeId, request.EndedOn, DateTime.Now);
        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}