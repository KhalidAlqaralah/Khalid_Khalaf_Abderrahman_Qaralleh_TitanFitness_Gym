using FluentValidation;
using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.CheckIns;
using TitanFitness.Domain.Common;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Application.CheckIns.RecordCheckIn;

public sealed record RecordCheckInCommand(Guid MemberId, Guid BranchId) : IRequest<CheckInResponse>;

public sealed record CheckInResponse(Guid Id, CheckInResult Result, string? RefusalReason);

public sealed class RecordCheckInCommandValidator : AbstractValidator<RecordCheckInCommand>
{
    public RecordCheckInCommandValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
    }
}

public sealed class RecordCheckInCommandHandler(
    IMemberRepository members,
    IBranchRepository branches,
    IMembershipRepository memberships,
    ICheckInRepository checkIns,
    IUnitOfWork unitOfWork) : IRequestHandler<RecordCheckInCommand, CheckInResponse>
{
    public async Task<CheckInResponse> Handle(RecordCheckInCommand request, CancellationToken ct)
    {
        var member = await members.GetByIdAsync(request.MemberId, ct)
            ?? throw new NotFoundException("Member", request.MemberId);

        _ = await branches.GetByIdAsync(request.BranchId, ct)
            ?? throw new NotFoundException("Branch", request.BranchId);

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        var membership = await memberships.GetActiveForMemberAsync(member.Id, today, ct);

        string? refusal =
            membership is null
                ? "No active membership."
                : !membership.AllowsEntryOn(today)
                    ? membership.Status switch
                    {
                        MembershipStatus.Frozen => "Membership is frozen.",
                        MembershipStatus.Pending => "Membership has not started yet.",
                        MembershipStatus.Expired => "Membership has expired.",
                        MembershipStatus.Cancelled => "Membership was cancelled.",
                        _ => "Membership does not permit entry."
                    }
                    : membership.Terms.AccessScope == AccessScope.HomeBranchOnly
                      && member.HomeBranchId != request.BranchId
                        ? "Membership covers the home branch only."
                        : null;

        var checkIn = refusal is null
            ? CheckIn.Admit(member.Id, request.BranchId, now)
            : CheckIn.Refuse(member.Id, request.BranchId, now, refusal);

        checkIns.Add(checkIn);
        await unitOfWork.SaveChangesAsync(ct);

        return new CheckInResponse(checkIn.Id, checkIn.Result, checkIn.RefusalReason);
    }
}