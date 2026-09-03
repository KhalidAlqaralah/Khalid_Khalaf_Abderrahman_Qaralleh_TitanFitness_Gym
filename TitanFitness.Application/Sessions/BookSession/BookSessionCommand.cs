using FluentValidation;
using MediatR;
using TitanFitness.Application.Common;
using TitanFitness.Domain.Common;
using TitanFitness.Domain.Sessions;

namespace TitanFitness.Application.Sessions.BookSession;

public sealed record BookSessionCommand(Guid SessionId, Guid MemberId, string? Note) : IRequest<BookingResponse>;

public sealed record BookingResponse(Guid Id, BookingStatus Status, int Position);

public sealed class BookSessionCommandValidator : AbstractValidator<BookSessionCommand>
{
    public BookSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public sealed class BookSessionCommandHandler(
    IClassSessionRepository sessions,
    IMemberRepository members,
    IMembershipRepository memberships,
    IUnitOfWork unitOfWork) : IRequestHandler<BookSessionCommand, BookingResponse>
{
    public async Task<BookingResponse> Handle(BookSessionCommand request, CancellationToken ct)
    {
        var session = await sessions.GetByIdAsync(request.SessionId, ct)
            ?? throw new NotFoundException("Session", request.SessionId);

        var member = await members.GetByIdAsync(request.MemberId, ct)
            ?? throw new NotFoundException("Member", request.MemberId);

        var membership = await memberships.GetActiveForMemberAsync(member.Id, session.Slot.Date, ct);

        if (membership is null || !membership.AllowsEntryOn(session.Slot.Date))
            throw new InvalidOperationException("This member has no active membership on that date.");

        if (await sessions.MemberHasOverlappingBookingAsync(member.Id, session.Slot, ct))
            throw new InvalidOperationException("This member is already booked onto an overlapping session.");

        var booking = session.Book(member.Id, request.Note, DateTime.Now);
        await unitOfWork.SaveChangesAsync(ct);

        return new BookingResponse(booking.Id, booking.Status, booking.Position);
    }
}