using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.Members;

namespace TitanFitness.Application.Features.Members.Commands.UpdateMember;

/// <summary>Edit Member: name and home branch. Never touches the membership the member holds.</summary>
internal sealed class UpdateMemberCommandHandler(
    IWriteRepository<Member> members,
    IReadRepository<Branch> branches,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateMemberCommand, Result>
{
    public async Task<Result> Handle(UpdateMemberCommand command, CancellationToken cancellationToken)
    {
        var member = await members.GetByIdAsync(command.MemberId, cancellationToken);
        if (member is null)
            return MemberErrors.NotFound;

        if (!await branches.GetAll().AnyAsync(b => b.Id == command.HomeBranchId, cancellationToken))
            return Error.Validation("Branch.NotFound", "Choose a branch from the list.", "homeBranchId");

        var updated = member.Update(command.FullName, command.HomeBranchId);
        if (updated.IsFailure)
            return updated;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
