using FluentValidation;
using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;

namespace TitanFitness.Application.Members.UpdateMember;

public sealed record UpdateMemberCommand(
    Guid MemberId,
    string FullName,
    string? Email,
    string? Phone,
    string? Address,
    string? PhotoUrl,
    Guid HomeBranchId) : IRequest<Unit>;

public sealed class UpdateMemberCommandValidator : AbstractValidator<UpdateMemberCommand>
{
    public UpdateMemberCommandValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).MaximumLength(100).EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Address).MaximumLength(200);
        RuleFor(x => x.PhotoUrl).MaximumLength(500);
        RuleFor(x => x.HomeBranchId).NotEmpty();
    }
}

public sealed class UpdateMemberCommandHandler(
    IMemberRepository members,
    IBranchRepository branches,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateMemberCommand, Unit>
{
    public async Task<Unit> Handle(UpdateMemberCommand request, CancellationToken ct)
    {
        var member = await members.GetByIdAsync(request.MemberId, ct)
            ?? throw new NotFoundException("Member", request.MemberId);

        var branch = await branches.GetByIdAsync(request.HomeBranchId, ct)
            ?? throw new NotFoundException("Branch", request.HomeBranchId);

        member.Rename(request.FullName);
        member.UpdateContactDetails(request.Email, request.Phone, request.Address);
        member.SetPhoto(request.PhotoUrl);
        member.TransferToBranch(branch.Id);

        await unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}