using FluentValidation;
using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;

namespace TitanFitness.Application.Memberships.UseGuestPass;

public sealed record UseGuestPassCommand(Guid MembershipId, Guid GuestPassId, string? GuestName) : IRequest<Unit>;

public sealed class UseGuestPassCommandValidator : AbstractValidator<UseGuestPassCommand>
{
    public UseGuestPassCommandValidator()
    {
        RuleFor(x => x.MembershipId).NotEmpty();
        RuleFor(x => x.GuestPassId).NotEmpty();
        RuleFor(x => x.GuestName).MaximumLength(100);
    }
}

public sealed class UseGuestPassCommandHandler(
    IMembershipRepository memberships,
    IUnitOfWork unitOfWork) : IRequestHandler<UseGuestPassCommand, Unit>
{
    public async Task<Unit> Handle(UseGuestPassCommand request, CancellationToken ct)
    {
        var membership = await memberships.GetByIdAsync(request.MembershipId, ct)
            ?? throw new NotFoundException("Membership", request.MembershipId);

        membership.UseGuestPass(request.GuestPassId, request.GuestName, DateTime.Now);
        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}