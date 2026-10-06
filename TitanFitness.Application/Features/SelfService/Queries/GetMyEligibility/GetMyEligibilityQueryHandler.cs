using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Memberships.Shared;
using TitanFitness.Application.Features.SelfService.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Members;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.Sessions;

namespace TitanFitness.Application.Features.SelfService.Queries.GetMyEligibility;

internal sealed class GetMyEligibilityQueryHandler(
    IReadRepository<Member> members,
    IReadRepository<Membership> memberships,
    IReadRepository<ClassSession> sessions,
    IReadRepository<Booking> bookings,
    IClock clock) : IRequestHandler<GetMyEligibilityQuery, Result<EligibilityResponse>>
{
    public async Task<Result<EligibilityResponse>> Handle(GetMyEligibilityQuery query, CancellationToken cancellationToken)
    {
        var member = await members.GetByIdAsync(query.MemberId, cancellationToken);
        if (member is null)
            return MemberErrors.NotFound;

        var held = await memberships.GetAll().Where(m => m.MemberId == member.Id).ToListAsync(cancellationToken);
        var today = clock.Today;
        var status = MembershipSelection.StatusOf(MembershipSelection.Current(held, today), held.Any(m => m.CancelledOn != null), today);

        var date = today;
        var branchId = member.HomeBranchId;
        BookingStatus? existing = null;

        if (query.SessionId is not null)
        {
            var session = await sessions.GetAll()
                .Where(s => s.Id == query.SessionId)
                .Select(s => new { s.BranchId, s.Slot.Date })
                .FirstOrDefaultAsync(cancellationToken);
            if (session is null)
                return ClassSessionErrors.NotFound;

            date = session.Date;
            branchId = session.BranchId;

            existing = await bookings.GetAll()
                .Where(b => b.SessionId == query.SessionId && b.MemberId == member.Id && b.Status != BookingStatus.Cancelled)
                .Select(b => (BookingStatus?)b.Status)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var access = MembershipSelection.CheckAccess(held, date, branchId, member.HomeBranchId);

        return access.IsSuccess
            ? new EligibilityResponse(member.Id, member.FullName, member.Number.Value, status, true, "Active Membership", "Cleared for booking", existing)
            : new EligibilityResponse(member.Id, member.FullName, member.Number.Value, status, false, "Not eligible", access.Error.Message, existing);
    }
}
