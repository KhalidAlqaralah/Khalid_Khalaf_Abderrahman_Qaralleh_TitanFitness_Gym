using FluentValidation;
using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;

namespace TitanFitness.Application.Memberships.IssueGuestPass;

public sealed record IssueGuestPassCommand(Guid MembershipId) : IRequest<Guid>;

public sealed class IssueGuestPassCommandValidator : AbstractValidator<IssueGuestPassCommand>
{
    public IssueGuestPassCommandValidator() => RuleFor(x => x.MembershipId).NotEmpty();
}

public sealed class IssueGuestPassCommandHandler(
    IMembershipRepository memberships,
    IUnitOfWork unitOfWork) : IRequestHandler<IssueGuestPassCommand, Guid>
{
    public async Task<Guid> Handle(IssueGuestPassCommand request, CancellationToken ct)
    {
        var membership = await memberships.GetByIdAsync(request.MembershipId, ct)
            ?? throw new NotFoundException("Membership", request.MembershipId);

        var pass = membership.IssueGuestPass(DateTime.Now);
        await unitOfWork.SaveChangesAsync(ct);

        return pass.Id;
    }
}