using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.CheckIns.Contracts;
using TitanFitness.Application.Features.Memberships.Shared;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Branches;
using TitanFitness.Domain.CheckIns;
using TitanFitness.Domain.Members;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.Features.CheckIns.Commands.RecordCheckIn;

/// <summary>
/// Records a facility entry. The member's membership must allow entry at this branch on that date
/// (not frozen, expired, cancelled or home-branch-only elsewhere); otherwise nothing is saved and the reason is returned.
/// </summary>
internal sealed class RecordCheckInCommandHandler(
    IWriteRepository<CheckIn> checkIns,
    IReadRepository<Member> members,
    IReadRepository<Branch> branches,
    IReadRepository<Membership> memberships,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser) : IRequestHandler<RecordCheckInCommand, Result<CheckInResponse>>
{
    public async Task<Result<CheckInResponse>> Handle(RecordCheckInCommand command, CancellationToken cancellationToken)
    {
        var member = await members.GetByIdAsync(command.MemberId, cancellationToken);
        if (member is null)
            return Error.Validation("Member.NotFound", "Select a member from the list.", "memberId");

        var branch = await branches.GetByIdAsync(command.BranchId, cancellationToken);
        if (branch is null)
            return Error.Validation("Branch.NotFound", "Choose a branch from the list.", "branchId");

        var held = await memberships.GetAll().Where(m => m.MemberId == member.Id).ToListAsync(cancellationToken);

        var access = MembershipSelection.CheckAccess(held, command.Date, branch.Id, member.HomeBranchId);
        if (access.IsFailure)
            return Error.Conflict(access.Error.Code, $"{member.FullName} cannot check in: {access.Error.Message}", "memberId");

        var checkIn = CheckIn.Record(member.Id, branch.Id, command.Date.ToDateTime(command.Time), command.Notes,
            currentUser.UserName, clock.Now);
        if (checkIn.IsFailure)
            return checkIn.Error;

        checkIns.Add(checkIn.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CheckInResponse(checkIn.Value.Id, member.Id, member.FullName, branch.Id, branch.Name, checkIn.Value.OccurredAt);
    }
}
