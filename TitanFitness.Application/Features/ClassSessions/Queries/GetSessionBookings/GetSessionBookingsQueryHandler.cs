using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanFitness.Application.Features.ClassSessions.Contracts;
using TitanFitness.Domain.Abstractions;
using TitanFitness.Domain.Members;
using TitanFitness.Domain.Sessions;

namespace TitanFitness.Application.Features.ClassSessions.Queries.GetSessionBookings;

/// <summary>The class roster: bookings joined to members, confirmed first, then the waitlist in order.</summary>
internal sealed class GetSessionBookingsQueryHandler(
    IReadRepository<ClassSession> sessions,
    IReadRepository<Booking> bookings,
    IReadRepository<Member> members) : IRequestHandler<GetSessionBookingsQuery, Result<IReadOnlyList<SessionBookingResponse>>>
{
    public async Task<Result<IReadOnlyList<SessionBookingResponse>>> Handle(GetSessionBookingsQuery query, CancellationToken cancellationToken)
    {
        if (!await sessions.GetAll().AnyAsync(s => s.Id == query.SessionId, cancellationToken))
            return ClassSessionErrors.NotFound;

        var roster = await (
            from b in bookings.GetAll()
            join m in members.GetAll() on b.MemberId equals m.Id
            where b.SessionId == query.SessionId
            orderby b.Status, b.Position
            select new SessionBookingResponse
            {
                BookingId = b.Id,
                MemberId = m.Id,
                MemberName = m.FullName,
                MembershipNumber = m.Number.Value,
                Status = b.Status,
                Position = b.Position,
                Note = b.Note,
                BookedOn = b.BookedOn
            }).ToListAsync(cancellationToken);

        return roster;
    }
}
