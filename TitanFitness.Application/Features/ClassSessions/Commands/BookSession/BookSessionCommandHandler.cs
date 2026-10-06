using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.ClassSessions.Contracts;
using TitanFitness.Application.Features.Memberships.Shared;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Members;
using TitanFitness.Domain.Memberships;
using TitanFitness.Domain.Sessions;

namespace TitanFitness.Application.Features.ClassSessions.Commands.BookSession;

/// <summary>
/// Books a member onto a class. Eligibility (a membership valid on the class date at that branch) is checked
/// here because it needs the Membership aggregate; capacity and the waitlist are the session's own rules.
/// </summary>
internal sealed class BookSessionCommandHandler(
    IWriteRepository<ClassSession> sessions,
    IReadRepository<Member> members,
    IReadRepository<Membership> memberships,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<BookSessionCommand, Result<BookingResultResponse>>
{
    public async Task<Result<BookingResultResponse>> Handle(BookSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await sessions.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null)
            return ClassSessionErrors.NotFound;

        var member = await members.GetByIdAsync(command.MemberId, cancellationToken);
        if (member is null)
            return Error.Validation("Member.NotFound", "Select a member from the list.", "memberId");

        var held = await memberships.GetAll().Where(m => m.MemberId == member.Id).ToListAsync(cancellationToken);

        var access = MembershipSelection.CheckAccess(held, session.Slot.Date, session.BranchId, member.HomeBranchId);
        if (access.IsFailure)
            return Error.Conflict(access.Error.Code, $"{member.FullName} cannot book this class: {access.Error.Message}", "memberId");

        var booking = session.Book(member.Id, command.Note, clock.Now);
        if (booking.IsFailure)
            return booking.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var b = booking.Value;
        int? waitlistPosition = b.Status == BookingStatus.Waitlisted
            ? session.Waitlist.ToList().FindIndex(w => w.Id == b.Id) + 1
            : null;

        return new BookingResultResponse(b.Id, b.Status, waitlistPosition);
    }
}
