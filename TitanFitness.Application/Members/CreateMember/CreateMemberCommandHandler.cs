using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;
using TitanFitness.Domain.Members;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Application.Members.CreateMember;

public sealed class CreateMemberCommandHandler(
    IMemberRepository members,
    IBranchRepository branches,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateMemberCommand, Guid>
{
    public async Task<Guid> Handle(CreateMemberCommand request, CancellationToken cancellationToken)
    {
        var branch = await branches.GetByIdAsync(request.HomeBranchId, cancellationToken)
            ?? throw new NotFoundException("Branch", request.HomeBranchId);

        var number = new MembershipNumber(request.MembershipNumber);

        if (await members.NumberExistsAsync(number, cancellationToken))
            throw new InvalidOperationException($"Membership number '{number}' is already in use.");

        var member = new Member(number, request.FullName, request.JoinedOn, branch.Id);
        member.UpdateContactDetails(request.Email, request.Phone, request.Address);

        members.Add(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return member.Id;
    }
}