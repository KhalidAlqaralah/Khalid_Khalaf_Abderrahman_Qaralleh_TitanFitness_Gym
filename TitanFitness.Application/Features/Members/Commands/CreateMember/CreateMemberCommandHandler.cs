using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Members.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.Members;
using TitanFitness.Domain.ValueObjects;

namespace TitanFitness.Application.Features.Members.Commands.CreateMember;

/// <summary>
/// Registers a member. The system sets the next TF-NNNN number, today as the joined date,
/// and records the staff user who added the member and when.
/// </summary>
internal sealed class CreateMemberCommandHandler(
    IWriteRepository<Member> members,
    IReadRepository<Branch> branches,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser) : IRequestHandler<CreateMemberCommand, Result<MemberCreatedResponse>>
{
    public async Task<Result<MemberCreatedResponse>> Handle(CreateMemberCommand command, CancellationToken cancellationToken)
    {
        if (!await branches.GetAll().AnyAsync(b => b.Id == command.HomeBranchId, cancellationToken))
            return Error.Validation("Branch.NotFound", "Choose a branch from the list.", "homeBranchId");

        var lastNumber = await members.GetAll()
            .Where(m => m.Number.Value.StartsWith(MembershipNumber.Prefix))
            .OrderByDescending(m => m.Number.Value.Length)
            .ThenByDescending(m => m.Number.Value)
            .Select(m => m.Number.Value)
            .FirstOrDefaultAsync(cancellationToken);

        var next = (lastNumber is null ? null : MembershipNumber.SequenceOf(lastNumber)) ?? 1000;
        var number = MembershipNumber.FromSequence(next + 1);

        var member = Member.Create(number, command.FullName, command.HomeBranchId, clock.Today,
            currentUser.UserName, clock.Now, command.Email, command.Phone, command.Address);
        if (member.IsFailure)
            return member.Error;

        members.Add(member.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new MemberCreatedResponse(member.Value.Id, member.Value.Number.Value);
    }
}
